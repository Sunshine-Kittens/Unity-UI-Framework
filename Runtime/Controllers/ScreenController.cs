using System;
using System.Collections.Generic;
using System.Threading;

using UIFramework.Collectors;
using UIFramework.Controllers.Interfaces;
using UIFramework.Coordinators;
using UIFramework.Core;
using UIFramework.Core.Interfaces;
using UIFramework.Navigation;
using UIFramework.Registry;
using UIFramework.Transitioning;

using UnityEngine;
using UnityEngine.Extension;

namespace UIFramework.Controllers
{
    public class ScreenController : IScreenController
    {
        private enum VisibilityState
        {
            Entering = 1,
            Entered = 2,
            Exiting = 3,
            Exited = 4
        }

        public bool Active => IsInitialized && IsVisible;

        public bool IsInitialized => State == ScreenControllerState.Initialized;
        public ScreenControllerState State { get; private set; } = ScreenControllerState.Uninitialized;
        public bool IsVisible => _visibilityState > 0 && _visibilityState < VisibilityState.Exited && Opacity > 0.0F;
        
        public float Opacity => _opacity;
        private float _opacity = 1.0F;

        public IScreen ActiveScreen => _navigator.Active;

        public IScreen PreviousScreen
        {
            get
            {
                IHistoryEntry entry = _history.Peek();
                if (entry != null)
                {
                    if(entry.TryGetEvent(out NavigationHistoryEvent navigationEvent))
                    {
                        _registry.TryGet(navigationEvent.WindowType, out IScreen screen);
                        return screen;
                    }
                }
                return null;
            }
        }
        
        public IScalarFlag IsEnabled => _isEnabled;
        private readonly ScalarFlag _isEnabled = new(true);
        
        public IScalarFlag IsInteractable => _isInteractable;
        private readonly ScalarFlag _isInteractable = new(true);

        public virtual TimeMode TimeMode 
        { 
            get => _timeMode;
            protected set => _timeMode = value;
        }
        private TimeMode _timeMode;

        public IHistoryGroups HistoryGroups => _history;
        
        protected IEnumerable<WidgetCollector<IScreen>> Collectors { get; }

        public event Action Entering;
        public event Action Entered;
        public event Action Exiting;
        public event Action Exited;
        
        public event ScreenAction ScreenShowing;
        public event ScreenAction ScreenShown;
        public event ScreenAction ScreenHiding;
        public event ScreenAction ScreenHidden;

        private readonly WidgetRegistry<IScreen> _registry;
        private readonly History _history;
        private readonly WindowNavigator<IScreen> _navigator;
        private readonly TransitionManager _transitionManager;
        
        private readonly NavigationCoordinator<IScreen> _navigationCoordinator;
        private readonly ReturnCoordinator<IScreen> _returnCoordinator;
        private readonly ExitCoordinator<IScreen> _exitCoordinator;
        
        private VisibilityState _visibilityState = 0;

        public ScreenController(IEnumerable<WidgetCollector<IScreen>> collectors, TimeMode timeMode)
        {
            Collectors = collectors ?? throw new ArgumentNullException(nameof(collectors));
            _timeMode = timeMode;
            
            void OnScreenInitialize(IWidget widget)
            {
                IScreen screen = widget as IScreen ?? throw new NullReferenceException(nameof(widget));
                screen.Showing += OnScreenShowing;
                screen.Shown += OnScreenShown;
                screen.Hiding += OnScreenHiding;
                screen.Hidden += OnScreenHidden;
                screen.SetVisibility(WidgetVisibility.Hidden);
                screen.SetOpacity(_opacity);
                screen.IsEnabled.Value = _isEnabled.Value;
                screen.IsInteractable.Value = _isInteractable.Value;
                screen.SetController(this);
            }
            
            void OnScreenTerminate(IWidget widget)
            {
                IScreen screen = widget as IScreen ?? throw new NullReferenceException(nameof(widget));
                screen.Showing -= OnScreenShowing;
                screen.Shown -= OnScreenShown;
                screen.Hiding -= OnScreenHiding;
                screen.Hidden -= OnScreenHidden;
                screen.ClearController();
            }
            
            _registry = new WidgetRegistry<IScreen>(OnScreenInitialize, OnScreenTerminate);
            _history = new History(_registry.Widgets.Count, _registry.Widgets.Count);
            
            _navigator = new WindowNavigator<IScreen>(_registry, _history);
            
            _transitionManager = new TransitionManager(_timeMode);
            _navigationCoordinator = new NavigationCoordinator<IScreen>(_timeMode, _registry, _navigator, _history, _transitionManager);
            _returnCoordinator = new ReturnCoordinator<IScreen>(_navigator, _history, _transitionManager);
            _exitCoordinator = new ExitCoordinator<IScreen>(_timeMode, _navigator, _transitionManager);
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
                    foreach (IScreen screen in _registry.Widgets)
                    {
                        if (screen.IsVisible)
                        {
                            screen.Tick(deltaTime);
                        }
                    }   
                }
            }
        }
        
        public void Initialize()
        {
            if (State == ScreenControllerState.Initialized)
            {
                throw new InvalidOperationException("Controller already initialized.");
            }
            
            _registry.Initialize();
            _registry.Collect(Collectors);
            _navigator.OnNavigationUpdate += OnNavigationUpdate;
            
            _isEnabled.OnUpdate += OnIsEnabledUpdated;
            _isInteractable.OnUpdate += OnIsInteractableUpdated;
            
            UpdateManager.AddUpdatable(this);
            OnInitialize();
            State = ScreenControllerState.Initialized; 
        }

        public void Terminate()
        {
            if (State != ScreenControllerState.Initialized)
            {
                throw new InvalidOperationException("Controller cannot be terminated.");
            }

            _exitCoordinator.Exit(new ExitRequest());
            
            _history.Clear();
            _registry.Terminate(); 
            _navigator.OnNavigationUpdate -= OnNavigationUpdate;
            _transitionManager.Terminate();
            _isEnabled.OnUpdate -= OnIsEnabledUpdated;
            _isEnabled.Reset(true);
            _isInteractable.OnUpdate += OnIsInteractableUpdated;
            _isInteractable.Reset(true);
            
            UpdateManager.RemoveUpdatable(this);
            OnTerminate();
            State = ScreenControllerState.Terminated;
        }

        protected virtual void OnInitialize() { }
        protected virtual void OnTerminate() { }

        protected virtual void OnEnter() { }
        protected virtual void OnEntered() { }
        
        protected virtual void OnExit() { }
        protected virtual void OnExited() { }

        public NavigationRequest<IScreen> CreateNavigationRequest(IScreen screen)
        {
            return _navigationCoordinator.CreateNavigationRequest(screen);
        }
        
        public NavigationRequest<IScreen> CreateNavigationRequest<TTarget>() where TTarget : class, IScreen
        {
            return _navigationCoordinator.CreateNavigationRequest<TTarget>();
        }
        
        public NavigationResponse<IScreen> Return(CancellationToken cancellationToken = default)
        {
            return _returnCoordinator.Return(cancellationToken);
        }
        
        public NavigationResponse<IScreen> Exit(in ExitRequest request)
        {
            return _exitCoordinator.Exit(in request);
        }
        
        public void SetOpacity(float opacity)
        {
            _opacity = opacity;
            if (_registry != null)
            {
                foreach (IScreen screen in _registry.Widgets)
                {
                    screen.SetOpacity(opacity);
                }   
            }
        }

        protected virtual void OnNavigationUpdate(NavigationResult<IScreen> result)
        {
            if (result.Success)
            {
                if (result.Previous == null && result.Active != null)
                {
                    _visibilityState = VisibilityState.Entering;
                    Entering?.Invoke();
                    OnEnter();
                }
                else if (result.Previous != null && result.Active == null)
                {
                    _visibilityState = VisibilityState.Exiting;
                    Exiting?.Invoke();
                    OnExit();
                }
            }
        }

        private void OnScreenShowing(IWidget widget)
        {
            ScreenShowing?.Invoke(widget as IScreen);
        }
        
        private void OnScreenShown(IWidget widget)
        {
            if (_visibilityState == VisibilityState.Entering)
            {
                _visibilityState = VisibilityState.Entered;
                Entered?.Invoke();
                OnEnter();
            }
            ScreenShown?.Invoke(widget as IScreen);
        }

        private void OnScreenHiding(IWidget widget)
        {
            ScreenHiding?.Invoke(widget as IScreen);
        }
        
        private void OnScreenHidden(IWidget widget)
        {
            if (_visibilityState == VisibilityState.Exiting)
            {
                _visibilityState = VisibilityState.Exited;
                Exited?.Invoke();
                OnExit();
            }
            ScreenHidden?.Invoke(widget as IScreen);
        }
        
        private void OnIsEnabledUpdated(bool value)
        {
            for (int i = 0; i < _registry.Widgets.Count; i++)
            {
                _registry.Widgets[i].IsEnabled.Value = value;
            }
        }
        
        private void OnIsInteractableUpdated(bool value)
        {
            for (int i = 0; i < _registry.Widgets.Count; i++)
            {
                _registry.Widgets[i].IsInteractable.Value = value;
            }
        }
    }
}