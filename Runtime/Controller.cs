using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

using UnityEngine;
using UnityEngine.Extension;

namespace UIFramework
{
    public enum ControllerState
    {
        Uninitialized,
        Initialized,
        Terminated
    }
    
    public abstract class Controller : MonoBehaviour, IUpdatable
    {
        public readonly struct NavigationResponse
        {
            public readonly Navigation<IScreen>.Event NavigationEvent;
            private readonly Awaitable _awaitable;

            public NavigationResponse(in Navigation<IScreen>.Event navigationEvent, Awaitable awaitable)
            {
                NavigationEvent = navigationEvent;
                _awaitable = awaitable;
            }
            
            public Awaiter GetAwaiter() => new Awaiter(_awaitable);

            public class Awaiter : INotifyCompletion
            {
                private readonly Awaitable _awaitable;
                
                public Awaiter(Awaitable awaitable)
                {
                    _awaitable = awaitable;
                }

                public bool IsCompleted => _awaitable == null || _awaitable.IsCompleted;

                public void OnCompleted(Action continuation)
                {
                    if (continuation == null) throw new ArgumentNullException(nameof(continuation));
                    if (_awaitable != null)
                        _awaitable.GetAwaiter().OnCompleted(continuation);
                    else
                        continuation();
                }

                public void GetResult()
                {
                    if(_awaitable != null)
                        _awaitable.GetAwaiter().GetResult();
                }
            }
        }
        
        public bool Active => gameObject.activeInHierarchy;
        public bool IsInitialized => State == ControllerState.Initialized;
        public ControllerState State { get; private set; } = ControllerState.Uninitialized;
        public bool IsVisible => Opacity > 0.0F;
        public virtual float Opacity { get; }
        private float _opacity = 1.0F;

        public IScreen ActiveScreen => _navigation.Active;
        public IScreen PreviousScreen => _navigation.PeekHistory();
        
        public IScalarFlag IsEnabled => _isEnabled;
        private readonly ScalarFlag _isEnabled = new(true);
        
        public IScalarFlag IsInteractable => _isInteractable;
        private readonly ScalarFlag _isInteractable = new(true);

        public virtual TimeMode TimeMode { get => _timeMode;
            protected set => _timeMode = value;
        }
        [SerializeField] private TimeMode _timeMode = TimeMode.Scaled;

        [SerializeField] protected ScreenCollector[] ScreenCollectors = null;

        public event Action Shown;
        public event Action Hidden;
        public event WidgetAction ScreenShown;
        public event WidgetAction ScreenHidden;

        private Navigation<IScreen> _navigation;
        private TransitionManager _transitionManager;
        private readonly History<VisibilityTransitionParams> _transitionHistory = new (16);

        private IScreen[] _screens = null;
        
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

                if (_screens != null)
                {
                    for (int i = 0; i < _screens.Length; i++)
                    {
                        if (_screens[i].IsVisible)
                        {
                            _screens[i].UpdateWidget(deltaTime);
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
            if(_navigation != null)
            {
                foreach (KeyValuePair<Type, IScreen> pair in _navigation.Navigables)
                {
                    if (pair.Value != null)
                    {
                        pair.Value.Shown -= OnScreenShown;
                        pair.Value.Hidden -= OnScreenHidden;
                    }
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

            if (ScreenCollectors == null)
            {
                throw new InvalidOperationException("screenCollectors are null.");
            }

            List<IScreen> screenList = new List<IScreen>();
            for (int i = 0; i < ScreenCollectors.Length; i++)
            {
                if (ScreenCollectors[i] == null)
                {
                    throw new NullReferenceException("Null screen collector on controller.");
                }
                screenList.AddRange(ScreenCollectors[i].Collect());
            }

            _screens = screenList.ToArray();
            Dictionary<Type, IScreen> screenDictionary = new Dictionary<Type, IScreen>();
            for (int i = 0; i < _screens.Length; i++)
            {
                Type type = _screens[i].GetType();
                if (!screenDictionary.ContainsKey(type))
                {
                    screenDictionary.Add(type, _screens[i]);
                    _screens[i].Shown += OnScreenShown;
                    _screens[i].Hidden += OnScreenHidden;
                    _screens[i].Initialize();
                    _screens[i].SetController(this);
                    _screens[i].SetVisibility(WidgetVisibility.Hidden);
                    _screens[i].SetOpacity(_opacity);
                    _screens[i].IsEnabled.Value = _isEnabled.Value;
                    _screens[i].IsInteractable.Value = _isInteractable.Value;
                }
                else
                {
                    throw new InvalidOperationException("Multiple instances of the same screen type have been found, " +
                        "please ensure all instances are of a unique type.");
                }
            }

            _navigation = new Navigation<IScreen>(screenDictionary);
            _navigation.OnNavigationUpdate += OnNavigationUpdate;

            _transitionManager = new TransitionManager(TimeMode);
            _isEnabled.OnUpdate += OnIsEnabledUpdated;
            _isInteractable.OnUpdate += OnIsInteractableUpdated;
            
            OnInitialize();
            SetBackButtonActive(false);

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
            _navigation.OnNavigationUpdate -= OnNavigationUpdate;
            _navigation = null;
            _transitionManager = null;
            _isEnabled.OnUpdate -= OnIsEnabledUpdated;
            _isEnabled.Reset(true);
            _isInteractable.OnUpdate += OnIsInteractableUpdated;
            _isInteractable.Reset(true);
            for(int i = 0; i < _screens.Length; i++)
            {
                if (_screens[i].State == WidgetState.Initialized)
                {
                    _screens[i].Terminate();
                }
            }
            _screens = null;
            
            OnTerminate();
            State = ControllerState.Terminated;
        }

        protected virtual void OnInitialize() { }
        protected virtual void OnUpdate(float deltaTime) { }
        protected virtual void OnTerminate() { }

        protected abstract void SetBackButtonActive(bool active);

        protected virtual void OnShow() { }
        protected virtual void OnHide() { }
        
#region Default Animation Implementations
        public NavigationResponse ShowScreen<TScreenType>(float animationLength = 0.0F, EasingMode easingMode = EasingMode.Linear, 
            bool excludeCurrentFromHistory = false, CancellationToken cancellationToken = default) where TScreenType : IScreen
        {
            Navigation<IScreen>.Event navigationEvent = NavigateToScreen<TScreenType>(excludeCurrentFromHistory);
            Awaitable awaitable = null;
            if (navigationEvent.Success)
            {
                awaitable = ShowScreen(in navigationEvent, null, animationLength, easingMode, excludeCurrentFromHistory, cancellationToken);
            }
            return new NavigationResponse(navigationEvent, awaitable);
        }
        
        public NavigationResponse ShowScreen<TScreenType>(object data, float animationLength = 0.0F, EasingMode easingMode = EasingMode.Linear,
            bool excludeCurrentFromHistory = false, CancellationToken cancellationToken = default) where TScreenType : IScreen
        {
            Navigation<IScreen>.Event navigationEvent = NavigateToScreen<TScreenType>(excludeCurrentFromHistory);
            Awaitable awaitable = null;
            if (navigationEvent.Success)
            {
                awaitable = ShowScreen(in navigationEvent, data, animationLength, easingMode, excludeCurrentFromHistory, cancellationToken);
            }
            return new NavigationResponse(navigationEvent, awaitable);
        }
        
        public NavigationResponse ShowScreen(IScreen screen, float animationLength = 0.0F, EasingMode easingMode = EasingMode.Linear,
            bool excludeCurrentFromHistory = false, CancellationToken cancellationToken = default)
        {
            Navigation<IScreen>.Event navigationEvent = NavigateToScreen(screen, excludeCurrentFromHistory);
            Awaitable awaitable = null;
            if (navigationEvent.Success)
            {
                awaitable = ShowScreen(in navigationEvent, null, animationLength, easingMode, excludeCurrentFromHistory, cancellationToken);
            }
            return new NavigationResponse(navigationEvent, awaitable);
        }
        
        public NavigationResponse ShowScreen(IScreen screen, object data, float animationLength = 0.0F, EasingMode easingMode = EasingMode.Linear,
            bool excludeCurrentFromHistory = false, CancellationToken cancellationToken = default)
        {
            Navigation<IScreen>.Event navigationEvent = NavigateToScreen(screen, excludeCurrentFromHistory);
            Awaitable awaitable = null;
            if (navigationEvent.Success)
            {
                awaitable = ShowScreen(in navigationEvent, data, animationLength, easingMode, excludeCurrentFromHistory, cancellationToken);
            }
            return new NavigationResponse(navigationEvent, awaitable);
        }
        
        private Awaitable ShowScreen(in Navigation<IScreen>.Event navigationEvent, object data, float animationLength, EasingMode easingMode,
            bool excludeCurrentFromHistory, CancellationToken cancellationToken) 
        {
            IAnimation sourceAnimation = navigationEvent.Previous != null ? navigationEvent.Previous.GetDefaultAnimation(WidgetVisibility.Hidden) : null;
            IAnimation targetAnimation = navigationEvent.Active.GetDefaultAnimation(WidgetVisibility.Visible);

            VisibilityTransitionParams transitionPlayable = Transition.Custom(animationLength, easingMode, sourceAnimation, targetAnimation, 
                TransitionSortPriority.Target);

            return ShowScreenInternal(in transitionPlayable, navigationEvent.Active, navigationEvent.Previous, data, 
                excludeCurrentFromHistory, cancellationToken); 
        }
#endregion

#region Animation Reference Implementations
        public NavigationResponse ShowScreen<TScreenType>(in WidgetAnimationRef animationRef, float animationLength, 
            EasingMode easingMode = EasingMode.Linear, bool excludeCurrentFromHistory = false, CancellationToken cancellationToken = default) 
            where TScreenType : IScreen
        {
            Navigation<IScreen>.Event navigationEvent = NavigateToScreen<TScreenType>(excludeCurrentFromHistory);
            Awaitable awaitable = null;
            if (navigationEvent.Success)
            {
                awaitable = ShowScreen(in navigationEvent, null, animationRef, animationLength, easingMode, excludeCurrentFromHistory, cancellationToken);
            }
            return new NavigationResponse(navigationEvent, awaitable);                
        }
        
        public NavigationResponse ShowScreen<TScreenType>(object data, in WidgetAnimationRef animationRef, float animationLength, 
            EasingMode easingMode = EasingMode.Linear, bool excludeCurrentFromHistory = false, CancellationToken cancellationToken = default) 
            where TScreenType : IScreen
        {
            Navigation<IScreen>.Event navigationEvent = NavigateToScreen<TScreenType>(excludeCurrentFromHistory);
            Awaitable awaitable = null;
            if (navigationEvent.Success)
            {
                awaitable = ShowScreen(in navigationEvent, data, in animationRef, animationLength, easingMode, excludeCurrentFromHistory, cancellationToken);
            } 
            return new NavigationResponse(navigationEvent, awaitable);        
        }
        
        public NavigationResponse ShowScreen(IScreen screen, in WidgetAnimationRef animationRef, float animationLength, 
            EasingMode easingMode = EasingMode.Linear, bool excludeCurrentFromHistory = false, CancellationToken cancellationToken = default)
        {
            Navigation<IScreen>.Event navigationEvent = NavigateToScreen(screen, excludeCurrentFromHistory);
            Awaitable awaitable = null;
            if (navigationEvent.Success)
            {
                awaitable = ShowScreen(in navigationEvent, null, in animationRef, animationLength, easingMode, excludeCurrentFromHistory, 
                    cancellationToken);
            }
            return new NavigationResponse(navigationEvent, awaitable);        
        }

        public NavigationResponse ShowScreen(IScreen screen, object data, in WidgetAnimationRef animationRef, float animationLength, 
            EasingMode easingMode = EasingMode.Linear, bool excludeCurrentFromHistory = false, CancellationToken cancellationToken = default)
        {
            Navigation<IScreen>.Event navigationEvent = NavigateToScreen(screen, excludeCurrentFromHistory);
            Awaitable awaitable = null;
            if (navigationEvent.Success)
            {
                awaitable = ShowScreen(in navigationEvent, data, in animationRef, animationLength, easingMode, excludeCurrentFromHistory, 
                    cancellationToken);
            } 
            return new NavigationResponse(navigationEvent, awaitable);
        }
        
        private Awaitable ShowScreen(in Navigation<IScreen>.Event navigationEvent, object data, in WidgetAnimationRef animationRef, 
            float animationLength, EasingMode easingMode, bool excludeCurrentFromHistory, CancellationToken cancellationToken)
        {
            IAnimation sourceAnimation = navigationEvent.Previous != null ? navigationEvent.Previous.GetDefaultAnimation(WidgetVisibility.Hidden) : null;
            VisibilityTransitionParams transitionPlayable = Transition.Custom(animationLength, easingMode, sourceAnimation,
                in animationRef, TransitionSortPriority.Target);

            return ShowScreenInternal(in transitionPlayable, navigationEvent.Active, navigationEvent.Previous, data, 
                excludeCurrentFromHistory, cancellationToken);
        }
#endregion
        
#region Transition Implementations
        public NavigationResponse ShowScreen<TScreenType>(in VisibilityTransitionParams transitionPlayable, bool excludeCurrentFromHistory = false,
            CancellationToken cancellationToken = default) 
            where TScreenType : IScreen
        {
            Navigation<IScreen>.Event navigationEvent = NavigateToScreen<TScreenType>(excludeCurrentFromHistory);
            Awaitable awaitable = null;
            if (navigationEvent.Success)
            {
                awaitable = ShowScreenInternal(in transitionPlayable, navigationEvent.Active, navigationEvent.Previous, null, 
                    excludeCurrentFromHistory, cancellationToken);
            }
            return new NavigationResponse(navigationEvent, awaitable);
        }

        public NavigationResponse ShowScreen<TScreenType>(object data, in VisibilityTransitionParams transitionPlayable, 
            bool excludeCurrentFromHistory = false, CancellationToken cancellationToken = default) where TScreenType : IScreen
        {
            Navigation<IScreen>.Event navigationEvent = NavigateToScreen<TScreenType>(excludeCurrentFromHistory);
            Awaitable awaitable = null;
            if (navigationEvent.Success)
            {
                awaitable = ShowScreenInternal(in transitionPlayable, navigationEvent.Active, navigationEvent.Previous, data, 
                    excludeCurrentFromHistory, cancellationToken);
            }
            return new NavigationResponse(navigationEvent, awaitable);
        }
        
        public NavigationResponse ShowScreen(IScreen screen, in VisibilityTransitionParams transitionPlayable, bool excludeCurrentFromHistory = false,
            CancellationToken cancellationToken = default) 
        {
            Navigation<IScreen>.Event navigationEvent = NavigateToScreen(screen, excludeCurrentFromHistory);
            Awaitable awaitable = null;
            if (navigationEvent.Success)
            {
                awaitable = ShowScreenInternal(in transitionPlayable, navigationEvent.Active, navigationEvent.Previous, null, 
                    excludeCurrentFromHistory, cancellationToken);
            }
            return new NavigationResponse(navigationEvent, awaitable);
        }

        public NavigationResponse ShowScreen(IScreen screen, object data, in VisibilityTransitionParams transitionPlayable, 
            bool excludeCurrentFromHistory = false, CancellationToken cancellationToken = default)
        {
            Navigation<IScreen>.Event navigationEvent = NavigateToScreen(screen, excludeCurrentFromHistory);
            Awaitable awaitable = null;
            if (navigationEvent.Success)
            {
                awaitable = ShowScreenInternal(in transitionPlayable, navigationEvent.Active, navigationEvent.Previous, data, 
                    excludeCurrentFromHistory, cancellationToken);
            }
            return new NavigationResponse(navigationEvent, awaitable);
        }
        
#endregion
        
        private Awaitable ShowScreenInternal(in VisibilityTransitionParams transitionPlayable, IScreen targetScreen, IScreen sourceScreen, object data, 
            bool excludeCurrentFromHistory, CancellationToken cancellationToken)
        {
            Awaitable awaitable = null;
            if (data != null)
                targetScreen.SetData(data);
            
            if (sourceScreen != null)
            {
                if (transitionPlayable.Length > 0.0F && (transitionPlayable.EntryAnimationRef.IsValid || transitionPlayable.ExitAnimationRef.IsValid))
                {
                    awaitable = _transitionManager.Transition(transitionPlayable, sourceScreen, targetScreen, cancellationToken);
                }
                else
                {
                    awaitable = _transitionManager.Transition(Transition.None(), sourceScreen, targetScreen, cancellationToken);
                }

                if (!excludeCurrentFromHistory)
                {
                    _transitionHistory.Push(transitionPlayable);
                }
            }
            else
            {
                if (transitionPlayable.Length > 0.0F && transitionPlayable.EntryAnimationRef.IsValid)
                {
                    AnimationPlayable playable = transitionPlayable.EntryAnimationRef.Resolve(targetScreen, WidgetVisibility.Visible).
                        Playable(transitionPlayable.Length, PlaybackMode.Forward, transitionPlayable.EasingMode, TimeMode);
                    awaitable = targetScreen.AnimateVisibility(WidgetVisibility.Visible, playable, InterruptBehavior.Immediate, cancellationToken);
                }
                else
                {
                    targetScreen.SetVisibility(WidgetVisibility.Visible);
                }
                Shown?.Invoke();
                OnShow();
            }
            return awaitable;
        }

        private Navigation<IScreen>.Event NavigateToScreen<TScreenType>(bool excludeCurrentFromHistory) where TScreenType : IScreen
        {
            return _navigation.Travel<TScreenType>(excludeCurrentFromHistory);
        }

        private Navigation<IScreen>.Event NavigateToScreen(IScreen screen, bool excludeCurrentFromHistory)
        {
            return _navigation.Travel(screen, excludeCurrentFromHistory);
        }

        public void NavigateBack()
        {
            _ = HideActiveScreen(CancellationToken.None);
        }
        
        public NavigationResponse HideActiveScreen(CancellationToken cancellationToken = default)
        {
            Navigation<IScreen>.Event navigationEvent = _navigation.Back();
            Awaitable awaitable = null;
            if (navigationEvent.Success)
            {
                VisibilityTransitionParams visibilityTransition = _transitionHistory.Pop().Invert();
                awaitable = _transitionManager.Transition(visibilityTransition, navigationEvent.Active, navigationEvent.Previous, 
                    cancellationToken);
            }
            return new NavigationResponse(navigationEvent, awaitable);
        }
        
        public NavigationResponse HideActiveScreen(in WidgetAnimationRef animationRef, float animationLength, EasingMode easingMode = EasingMode.Linear, 
            CancellationToken cancellationToken = default)
        {
            Navigation<IScreen>.Event navigationEvent = _navigation.Back();
            Awaitable awaitable = null;
            if (navigationEvent.Success)
            {
                VisibilityTransitionParams previousTransition = _transitionHistory.Pop();
                VisibilityTransitionParams transitionPlayable = Transition.Custom(animationLength, easingMode, in animationRef, 
                    in previousTransition.ExitAnimationRef, TransitionSortPriority.Target);
                
                awaitable = _transitionManager.Transition(transitionPlayable, navigationEvent.Active, navigationEvent.Previous, 
                    cancellationToken);
            }
            return new NavigationResponse(navigationEvent, awaitable);
        }
        
        public NavigationResponse HideActiveScreen(in VisibilityTransitionParams transitionPlayable, CancellationToken cancellationToken = default)
        {
            Navigation<IScreen>.Event navigationEvent = _navigation.Back();
            Awaitable awaitable = null;
            if (navigationEvent.Success)
            {
                _ = _transitionHistory.Pop();
                awaitable = _transitionManager.Transition(transitionPlayable, navigationEvent.Active, navigationEvent.Previous, 
                    cancellationToken);
            }
            return new NavigationResponse(navigationEvent, awaitable);
        }
        
        public NavigationResponse HideAll(float animationLength = 0.0F, EasingMode easingMode = EasingMode.Linear, 
            CancellationToken cancellationToken = default)
        {
            Navigation<IScreen>.Event navigationEvent = _navigation.Clear();
            Awaitable awaitable = null;
            if (navigationEvent.Success)
            {
                IAnimation animation = navigationEvent.Previous.GetDefaultAnimation(WidgetVisibility.Hidden);
                WidgetAnimationRef animationRef = animation != null ? WidgetAnimationRef.FromExplicit(animation) : default;
                awaitable = HideAllInternal(navigationEvent.Previous, in animationRef, animationLength, easingMode, cancellationToken); 
            }
            return new NavigationResponse(navigationEvent, awaitable);
        }

        public NavigationResponse HideAll(in WidgetAnimationRef animationRef, float animationLength, EasingMode easingMode = EasingMode.Linear,
            CancellationToken cancellationToken = default)
        {
            Navigation<IScreen>.Event navigationEvent = _navigation.Clear();
            Awaitable awaitable = null;
            if (navigationEvent.Success)
            {
                awaitable = HideAllInternal(navigationEvent.Previous, in animationRef, animationLength, easingMode, cancellationToken); 
            }
            return new NavigationResponse(navigationEvent, awaitable);
        }

        private Awaitable HideAllInternal(IScreen screen, in WidgetAnimationRef animationRef, float animationLength, EasingMode easingMode, 
            CancellationToken cancellationToken)
        {
            Awaitable awaitable = null;
            _transitionManager.Terminate();
            if (animationLength > 0.0F)
            {
                AnimationPlayable playable = animationRef.Resolve(screen, WidgetVisibility.Hidden).
                    Playable(animationLength, PlaybackMode.Forward, easingMode, TimeMode);
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
            for (int i = 0; i < _screens.Length; i++)
            {
                _screens[i].SetOpacity(opacity);
            }
        }
        
        public void StartNewHistoryGroup()
        {
            _navigation.StartNewHistoryGroup();
            _transitionHistory.StartNewGroup();
        }

        public void ClearLatestHistoryGroup()
        {
            Navigation<IScreen>.Event navigationEvent = _navigation.ClearLatestHistoryGroup();
            if (navigationEvent.Success)
            {
                _transitionHistory.ClearLatestGroup();
            }
        }

        public void InsertHistory<TScreenType>(in VisibilityTransitionParams transitionPlayable) where TScreenType : IScreen
        {
            Navigation<IScreen>.Event navigationEvent = _navigation.InsertHistory<TScreenType>();
            if (navigationEvent.Success)
            {
                _transitionHistory.Push(transitionPlayable);
            }
        }

        public void ClearHistory()
        {
            Navigation<IScreen>.Event navigationEvent = _navigation.ClearHistory();
            if (navigationEvent.Success)
            {
                _transitionHistory.Clear();
            }
        }

        private void OnNavigationUpdate(Navigation<IScreen>.Event navigationEvent)
        {
            if (navigationEvent.Success)
            {
                bool backButtonActive = navigationEvent.HistoryCount > 0;
                SetBackButtonActive(backButtonActive);
            }
        }

        private void OnScreenShown(IWidget widget)
        {
            ScreenShown?.Invoke(widget);
        }

        private void OnScreenHidden(IWidget widget)
        {
            ScreenHidden?.Invoke(widget);
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
    }
}