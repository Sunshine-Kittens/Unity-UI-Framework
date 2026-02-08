using System.Collections.Generic;

using UIFramework.Collectors;
using UIFramework.Interfaces;

namespace UIFramework.Controllers
{
    public class ScreenController : Controller<IScreen>
    {
        public readonly struct NavigationResponse
        {
            public readonly NavigationResult<TWidget> NavigationResult;
            
            private readonly Awaitable _completionTask;

            public NavigationResponse(in NavigationResult<TWidget> navigationResult, Awaitable completionTask)
            {
                NavigationResult = navigationResult;
                if (completionTask != null)
                {
                    _completionTask = completionTask;    
                }
                else
                {
                    AwaitableCompletionSource completionSource = new();
                    completionSource.SetResult();
                    _completionTask = completionSource.Awaitable;
                }
            }
            
            public Awaitable GetCompletionTask() => _completionTask;
            public Awaitable.Awaiter GetAwaiter() => _completionTask.GetAwaiter();
        }
        
        public bool Active => gameObject.activeInHierarchy;
        public bool IsInitialized => State == ControllerState.Initialized;
        public ControllerState State { get; private set; } = ControllerState.Uninitialized;
        public bool IsVisible => Opacity > 0.0F;
        
        public float Opacity => _opacity;
        private float _opacity = 1.0F;

        public TWidget ActiveWidget => _navigationManager.Active;
        public TWidget PreviousWidget => _navigationManager.PeekHistory();
        
        public IScalarFlag IsEnabled => _isEnabled;
        private readonly ScalarFlag _isEnabled = new(true);
        
        public IScalarFlag IsInteractable => _isInteractable;
        private readonly ScalarFlag _isInteractable = new(true);

        public virtual TimeMode TimeMode 
        { 
            get => _timeMode;
            protected set => _timeMode = value;
        }
        
        [SerializeField] private TimeMode _timeMode = TimeMode.Scaled;

        protected abstract IEnumerable<WidgetCollector<TWidget>> Collectors { get; }

        public event Action Shown;
        public event Action Hidden;
        public event WidgetAction WidgetShown;
        public event WidgetAction WidgetHidden;

        private WidgetRegistry<TWidget> _registry;
        private NavigationManager<TWidget> _navigationManager;
        private TransitionManager _transitionManager;
        private NavigationCoordinator<TWidget> _navigationCoordinator;
        private ReturnCoordinator<TWidget> _returnCoordinator;
        
        // Unity Messages
#if UNITY_EDITOR
        protected virtual void OnValidate()
        {

        }
#endif

        protected virtual void Awake()
        {
            UpdateManager.AddUpdatable(this);
        }

        protected virtual void Start()
        {
            
        }

        protected virtual void OnEnable()
        {

        }        

        public void ManagedUpdate()
        {
            if (IsVisible)
            {
                float deltaTime;
                switch (TimeMode)
                {
                    default:
                    case TimeMode.Scaled:
                        deltaTime = Time.deltaTime;
                        break;
                    case TimeMode.Unscaled:
                        deltaTime = Time.unscaledDeltaTime;
                        break;
                }

                if (_registry != null)
                {
                    foreach (TWidget widget in _registry.Widgets)
                    {
                        if (widget.IsVisible)
                        {
                            widget.UpdateWidget(deltaTime);
                        }
                    }   
                }
                OnUpdate(deltaTime);
            }
        }

        protected virtual void OnDisable()
        {
            // Close All and tear down
        }

        protected virtual void OnDestroy()
        {
            if(_registry != null)
            {
                foreach (TWidget widget in _registry.Widgets)
                {
                    widget.Shown -= OnWidgetShown;
                    widget.Hidden -= OnWidgetHidden;
                }
            }            
            UpdateManager.RemoveUpdatable(this);
        }

        protected virtual void OnApplicationFocus(bool hasFocus)
        {

        }

        // Controller
        public void Initialize()
        {
            if (State == ControllerState.Initialized)
            {
                throw new InvalidOperationException("Controller already initialized.");
            }

            if (Collectors == null)
            {
                throw new InvalidOperationException("screenCollectors are null.");
            }
            
            void OnInit(IWidget widget)
            {
                widget.Shown += OnWidgetShown;
                widget.Hidden += OnWidgetHidden;
                widget.SetVisibility(WidgetVisibility.Hidden);
                widget.SetOpacity(_opacity);
                widget.IsEnabled.Value = _isEnabled.Value;
                widget.IsInteractable.Value = _isInteractable.Value;
            }
            
            _registry = new WidgetRegistry<TWidget>(OnInit);
            _registry.Collect(Collectors);
            _navigationManager = new NavigationManager<TWidget>(_registry);
            _navigationManager.OnNavigationUpdate += NavigationManagerUpdate;
            
            _transitionManager = new TransitionManager(TimeMode);
            _navigationCoordinator = new NavigationCoordinator<TWidget>(TimeMode, _registry, _navigationManager, _transitionManager);
            
            _isEnabled.OnUpdate += OnIsEnabledUpdated;
            _isInteractable.OnUpdate += OnIsInteractableUpdated;
            
            OnInitialize();
            State = ControllerState.Initialized; 
        }

        public void Terminate()
        {
            if (State != ControllerState.Initialized)
            {
                throw new InvalidOperationException("Controller cannot be terminated.");
            }

            HideAll();
            ClearHistory();
            if(_registry != null)
            {
                foreach (TWidget widget in _registry.Widgets)
                {
                    if (widget.State == WidgetState.Initialized)
                    {
                        widget.Terminate();
                    }
                }
            } 
            _registry = null;
            _navigationCoordinator = null;
            _navigationManager.OnNavigationUpdate -= NavigationManagerUpdate;
            _navigationManager = null;
            _transitionManager = null;
            _isEnabled.OnUpdate -= OnIsEnabledUpdated;
            _isEnabled.Reset(true);
            _isInteractable.OnUpdate += OnIsInteractableUpdated;
            _isInteractable.Reset(true);
            
            OnTerminate();
            State = ControllerState.Terminated;
        }

        protected virtual void OnInitialize() { }
        protected virtual void OnUpdate(float deltaTime) { }
        protected virtual void OnTerminate() { }

        protected virtual void OnShow() { }
        protected virtual void OnHide() { }

        public NavigationRequest<TWidget> CreateNavigationRequest(TWidget widget)
        {
            return _navigationCoordinator.CreateNavigationRequest(widget);
        }

        public NavigationRequest<TWidget> CreateNavigationRequest<TTarget>() where TTarget : class, TWidget
        {
            return _navigationCoordinator.CreateNavigationRequest<TTarget>();
        }

        public ReturnRequest<TWidget> CreateReturnRequest()
        {
            return _returnCoordinator.CreateReturnRequest();
        }
        
        public NavigationResponse HideAll(float animationLength = 0.0F, EasingMode easingMode = EasingMode.Linear, 
            CancellationToken cancellationToken = default)
        {
            NavigationManager<IScreen>.Result navigationResult = _navigationManager.Clear();
            Awaitable awaitable = null;
            if (navigationResult.Success)
            {
                IAnimation animation = navigationResult.Previous.GetDefaultAnimation(WidgetVisibility.Hidden);
                WidgetAnimationRef animationRef = animation != null ? WidgetAnimationRef.FromExplicit(animation) : default;
                awaitable = HideAllInternal(navigationResult.Previous, in animationRef, animationLength, easingMode, cancellationToken); 
            }
            return new NavigationResponse(navigationResult, awaitable);
        }

        public NavigationResponse HideAll(in WidgetAnimationRef animationRef, float animationLength, EasingMode easingMode = EasingMode.Linear,
            CancellationToken cancellationToken = default)
        {
            NavigationManager<IScreen>.Result navigationResult = _navigationManager.Clear();
            Awaitable awaitable = null;
            if (navigationResult.Success)
            {
                awaitable = HideAllInternal(navigationResult.Previous, in animationRef, animationLength, easingMode, cancellationToken); 
            }
            return new NavigationResponse(navigationResult, awaitable);
        }

        private Awaitable HideAllInternal(IScreen screen, in WidgetAnimationRef animationRef, float animationLength, EasingMode easingMode, 
            CancellationToken cancellationToken)
        {
            Awaitable awaitable = null;
            _transitionManager.Terminate();
            if (animationLength > 0.0F)
            {
                AnimationPlayable playable = animationRef.Resolve(screen, WidgetVisibility.Hidden)
                    .ToPlayable()
                    .WithLength(animationLength)
                    .WithEasingMode(easingMode)
                    .WithTimeMode(TimeMode)
                    .Create();
                awaitable = screen.AnimateVisibility(WidgetVisibility.Visible, playable, InterruptBehavior.Immediate, cancellationToken);
            }
            else
            {
                screen.SetVisibility(WidgetVisibility.Hidden);
            }
            Hidden?.Invoke();
            OnHide();
            return awaitable;
        }

        public void SetOpacity(float opacity)
        {
            _opacity = opacity;
            if (_registry != null)
            {
                foreach (TWidget widget in _registry.Widgets)
                {
                    widget.SetOpacity(opacity);
                }   
            }
        }
        
        public void StartNewHistoryGroup()
        {
            _navigationManager.StartNewHistoryGroup();
            _transitionHistory.StartNewGroup();
        }

        public void ClearLatestHistoryGroup()
        {
            NavigationManager<IScreen>.Result navigationResult = _navigationManager.ClearLatestHistoryGroup();
            if (navigationResult.Success)
            {
                _transitionHistory.ClearLatestGroup();
            }
        }

        public void InsertHistory<TScreenType>(in VisibilityTransitionParams transitionPlayable) where TScreenType : IScreen
        {
            NavigationManager<IScreen>.Result navigationResult = _navigationManager.InsertHistory<TScreenType>();
            if (navigationResult.Success)
            {
                _transitionHistory.Push(transitionPlayable);
            }
        }

        public void ClearHistory()
        {
            NavigationManager<IScreen>.Result navigationResult = _navigationManager.ClearHistory();
            if (navigationResult.Success)
            {
                _transitionHistory.Clear();
            }
        }

        private void NavigationManagerUpdate(NavigationResult<TWidget> navigationResult)
        {
            if (navigationResult.Success)
            {
                bool backButtonActive = navigationResult.HistoryCount > 0;
                SetBackButtonActive(backButtonActive);
            }
        }

        private void OnWidgetShown(IWidget widget)
        {
            WidgetShown?.Invoke(widget);
        }

        private void OnWidgetHidden(IWidget widget)
        {
            WidgetHidden?.Invoke(widget);
        }
        
        private void OnIsEnabledUpdated(bool value)
        {
            for (int i = 0; i < _screens.Length; i++)
            {
                _screens[i].IsEnabled.Value = value;
            }
        }
        
        private void OnIsInteractableUpdated(bool value)
        {
            for (int i = 0; i < _screens.Length; i++)
            {
                _screens[i].IsInteractable.Value = value;
            }
        }

        protected virtual void SetBackButtonActive(bool active)
        {
            
        }
    }
}
