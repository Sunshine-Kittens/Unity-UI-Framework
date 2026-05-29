using System;
using System.Collections.Generic;
using System.Threading;

using UIFramework.Collectors;
using UIFramework.Coordinators;
using UIFramework.Core;
using UIFramework.Core.Interfaces;
using UIFramework.Navigation;
using UIFramework.Navigation.History;
using UIFramework.Registry;
using UIFramework.Transitioning;

using UnityEngine;
using UnityEngine.Extension;

namespace UIFramework.Groups
{
    public abstract class ScreenGroupBase : IScreenGroupBase
    {
        public bool IsInitialized => State == InitializationState.Initialized;
        public InitializationState State { get; private set; } = InitializationState.Uninitialized;
        public bool IsVisible => _presentationState > 0 && _presentationState < GroupPresentationState.Exited && _opacity > 0f;

        public float Opacity => _opacity;
        private float _opacity = 1f;

        public IScreen ActiveScreen => _navigator.ActiveInstance;

        public IScreen PreviousScreen
        {
            get
            {
                if (_history == null) return null;
                if (_history.TryPeek(out HistoryEventView events) && events.TryGetEvent(out NavigationHistoryEvent navEvent))
                {
                    _registry.TryGet(navEvent.WindowType, out IScreen screen);
                    return screen;
                }
                return null;
            }
        }

        public IScalarFlag IsEnabled => _isEnabled;
        private readonly ScalarFlag _isEnabled = new(true);

        public IScalarFlag IsInteractable => _isInteractable;
        private readonly ScalarFlag _isInteractable = new(true);

        protected IEnumerable<WidgetCollector<IScreen>> Collectors { get; }

        public event Action Entering;
        public event Action Entered;
        public event Action Exiting;
        public event Action Exited;

        public event ScreenAction ScreenShowing;
        public event ScreenAction ScreenShown;
        public event ScreenAction ScreenHiding;
        public event ScreenAction ScreenHidden;

        public virtual TimeMode TimeMode { get => _timeMode; protected set => _timeMode = value; }
        private TimeMode _timeMode;
        private readonly WidgetRegistry<IScreen> _registry;
        private readonly WindowNavigator<IScreen> _navigator;
        private readonly TransitionManager _transitionManager;
        private readonly ExitCoordinator<IScreen> _exitCoordinator;
        
        private History _history;
        private NavigateToCoordinator<IScreen> _navigateToCoordinator;
        private ReturnCoordinator<IScreen> _returnCoordinator;

        private GroupPresentationState _presentationState;

        protected IEnumerable<IScreen> Screens => _registry.Widgets;
        protected float DeltaTime => TimeMode == TimeMode.Unscaled ? Time.unscaledDeltaTime : Time.deltaTime;
        
        public ScreenGroupBase(IEnumerable<WidgetCollector<IScreen>> collectors, TimeMode timeMode)
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
                screen.SetNavigator(this);
            }

            void OnScreenTerminate(IWidget widget)
            {
                IScreen screen = widget as IScreen ?? throw new NullReferenceException(nameof(widget));
                screen.Showing -= OnScreenShowing;
                screen.Shown -= OnScreenShown;
                screen.Hiding -= OnScreenHiding;
                screen.Hidden -= OnScreenHidden;
                screen.ClearNavigator();
            }

            _registry = new WidgetRegistry<IScreen>(OnScreenInitialize, OnScreenTerminate);
            _navigator = new WindowNavigator<IScreen>(_registry);
            _transitionManager = new TransitionManager(timeMode);
            _exitCoordinator = new ExitCoordinator<IScreen>(timeMode, _navigator, _transitionManager);
        }

        public void Initialize()
        {
            if (State == InitializationState.Initialized)
                throw new InvalidOperationException("ScreenGroup is already initialized.");

            _registry.Initialize();
            _registry.Collect(Collectors);

            _history = new History(_registry.Widgets.Count);
            _navigateToCoordinator = new NavigateToCoordinator<IScreen>(_timeMode, _navigator, _history, _transitionManager);
            _returnCoordinator = new ReturnCoordinator<IScreen>(_navigator, _registry, _history, _transitionManager);

            _navigator.OnNavigationUpdate += NavigationUpdate;
            _isEnabled.OnUpdate += OnIsEnabledUpdated;
            _isInteractable.OnUpdate += OnIsInteractableUpdated;

            OnInitialize();
            State = InitializationState.Initialized;
        }

        public void Terminate()
        {
            if (State != InitializationState.Initialized)
                throw new InvalidOperationException("ScreenGroup cannot be terminated.");

            _exitCoordinator.Exit(new ExitRequest());

            _history?.Clear();
            _registry.Terminate();
            _navigator.OnNavigationUpdate -= NavigationUpdate;
            _transitionManager.Terminate();
            _isEnabled.OnUpdate -= OnIsEnabledUpdated;
            _isEnabled.Reset(true);
            _isInteractable.OnUpdate -= OnIsInteractableUpdated;
            _isInteractable.Reset(true);

            OnTerminate();
            State = InitializationState.Terminated;
        }

        public NavigateToRequest<IScreen> CreateNavigateToRequest(IScreen screen)
        {
            if (!IsInitialized)
                throw new InvalidOperationException("ScreenGroup is not initialized.");
            return new NavigateToRequest<IScreen>(_navigator, _navigateToCoordinator, _navigator.ActiveInstance, screen);
        }

        public NavigateToRequest<IScreen> CreateNavigateToRequest<TTarget>() where TTarget : class, IScreen
        {
            if (!IsInitialized)
                throw new InvalidOperationException("ScreenGroup is not initialized.");
            return new NavigateToRequest<IScreen>(_navigator, _navigateToCoordinator, _navigator.ActiveInstance, _registry.Get<TTarget>());
        }

        public NavigateToResponse<IScreen> Return(CancellationToken cancellationToken = default)
        {
            return _returnCoordinator.Return(cancellationToken);
        }

        public NavigateToResponse<IScreen> Exit(in ExitRequest request)
        {
            return _exitCoordinator.Exit(in request);
        }

        public void SetOpacity(float opacity)
        {
            _opacity = opacity;
            foreach (IScreen screen in _registry.Widgets)
                screen.SetOpacity(opacity);
        }

        private void NavigationUpdate(NavigateToResult<IScreen> result)
        {
            if (result.Success)
            {
                if (result.Previous == null && result.Active != null)
                {
                    _presentationState = GroupPresentationState.Entering;
                    Entering?.Invoke();
                    OnEnter();
                }
                else if (result.Previous != null && result.Active == null)
                {
                    _presentationState = GroupPresentationState.Exiting;
                    Exiting?.Invoke();
                    OnExit();
                }
            }
            OnNavigationUpdate(result);
        }

        private void OnScreenShowing(IWidget widget) => ScreenShowing?.Invoke(widget as IScreen);

        private void OnScreenShown(IWidget widget)
        {
            if (_presentationState == GroupPresentationState.Entering)
            {
                _presentationState = GroupPresentationState.Entered;
                Entered?.Invoke();
                OnEntered();
            }
            ScreenShown?.Invoke(widget as IScreen);
        }

        private void OnScreenHiding(IWidget widget) => ScreenHiding?.Invoke(widget as IScreen);

        private void OnScreenHidden(IWidget widget)
        {
            if (_presentationState == GroupPresentationState.Exiting)
            {
                _presentationState = GroupPresentationState.Exited;
                Exited?.Invoke();
                OnExited();
            }
            ScreenHidden?.Invoke(widget as IScreen);
        }
        
        private void OnIsEnabledUpdated(bool value)
        {
            for (int i = 0; i < _registry.Widgets.Count; i++)
                _registry.Widgets[i].IsEnabled.Value = value;
        }

        private void OnIsInteractableUpdated(bool value)
        {
            for (int i = 0; i < _registry.Widgets.Count; i++)
                _registry.Widgets[i].IsInteractable.Value = value;
        }

        protected virtual void OnInitialize() { }
        protected virtual void OnTerminate() { }
        
        protected virtual void OnEnter() { }
        protected virtual void OnEntered() { }
        protected virtual void OnExit() { }
        protected virtual void OnExited() { }
        
        protected virtual void OnNavigationUpdate(NavigateToResult<IScreen> result) { }
    }
}
