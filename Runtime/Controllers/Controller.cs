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
    public class Controller<TWidget> : IController<TWidget> where TWidget : class, IWidget
    {
        private enum VisibilityState
        {
            Entering = 1,
            Entered = 2,
            Exiting = 3,
            Exited = 4
        }

        public bool Active => IsInitialized && IsVisible;

        public bool IsInitialized => State == ControllerState.Initialized;
        public ControllerState State { get; private set; } = ControllerState.Uninitialized;
        public bool IsVisible => _visibilityState > 0 && _visibilityState < VisibilityState.Exited && Opacity > 0.0F;
        
        public float Opacity => _opacity;
        private float _opacity = 1.0F;

        public TWidget ActiveWidget => _widgetNavigator.Active;

        public TWidget PreviousWidget
        {
            get
            {
                IHistoryEntry entry = _history.Peek();
                if (entry != null)
                {
                    if(entry.TryGetEvent(out NavigationHistoryEvent navigationEvent))
                    {
                        _registry.TryGet(navigationEvent.WidgetType, out TWidget widget);
                        return widget;
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
        
        protected IEnumerable<WidgetCollector<TWidget>> Collectors { get; }

        public event Action Entering;
        public event Action Entered;
        public event Action Exiting;
        public event Action Exited;
        
        public event WidgetAction WidgetShowing;
        public event WidgetAction WidgetShown;
        public event WidgetAction WidgetHiding;
        public event WidgetAction WidgetHidden;

        private readonly WidgetRegistry<TWidget> _registry;
        private readonly History _history;
        private readonly WidgetNavigator<TWidget> _widgetNavigator;
        private readonly TransitionManager _transitionManager;
        
        private readonly NavigationCoordinator<TWidget> _navigationCoordinator;
        private readonly ReturnCoordinator<TWidget> _returnCoordinator;
        private readonly ExitCoordinator<TWidget> _exitCoordinator;
        
        private VisibilityState _visibilityState = 0;

        protected Controller(IEnumerable<WidgetCollector<TWidget>> collectors, TimeMode timeMode)
        {
            Collectors = collectors ?? throw new ArgumentNullException(nameof(collectors));
            _timeMode = timeMode;
            
            void OnWidgetInitialize(IWidget widget)
            {
                widget.Showing += OnWidgetShowing;
                widget.Shown += OnWidgetShown;
                widget.Hiding += OnWidgetHiding;
                widget.Hidden += OnWidgetHidden;
                widget.SetVisibility(WidgetVisibility.Hidden);
                widget.SetOpacity(_opacity);
                widget.IsEnabled.Value = _isEnabled.Value;
                widget.IsInteractable.Value = _isInteractable.Value;
            }
            
            void OnWidgetTerminate(IWidget widget)
            {
                widget.Showing -= OnWidgetShowing;
                widget.Shown -= OnWidgetShown;
                widget.Hiding -= OnWidgetHiding;
                widget.Hidden -= OnWidgetHidden;
            }
            
            _registry = new WidgetRegistry<TWidget>(OnWidgetInitialize, OnWidgetTerminate);
            _history = new History(_registry.Widgets.Count, _registry.Widgets.Count);
            
            _widgetNavigator = new WidgetNavigator<TWidget>(_registry, _history);
            
            _transitionManager = new TransitionManager(_timeMode);
            _navigationCoordinator = new NavigationCoordinator<TWidget>(_timeMode, _registry, _widgetNavigator, _history, _transitionManager);
            _returnCoordinator = new ReturnCoordinator<TWidget>(_widgetNavigator, _history, _transitionManager);
            _exitCoordinator = new ExitCoordinator<TWidget>(_timeMode, _widgetNavigator, _transitionManager);
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
            }
        }
        
        public void Initialize()
        {
            if (State == ControllerState.Initialized)
            {
                throw new InvalidOperationException("Controller already initialized.");
            }
            
            _registry.Initialize();
            _registry.Collect(Collectors);
            _widgetNavigator.OnNavigationUpdate += OnNavigationUpdate;
            
            _isEnabled.OnUpdate += OnIsEnabledUpdated;
            _isInteractable.OnUpdate += OnIsInteractableUpdated;
            
            UpdateManager.AddUpdatable(this);
            OnInitialize();
            State = ControllerState.Initialized; 
        }

        public void Terminate()
        {
            if (State != ControllerState.Initialized)
            {
                throw new InvalidOperationException("Controller cannot be terminated.");
            }

            _exitCoordinator.Exit(new ExitRequest());
            
            _history.Clear();
            _registry.Terminate(); 
            _widgetNavigator.OnNavigationUpdate -= OnNavigationUpdate;
            _transitionManager.Terminate();
            _isEnabled.OnUpdate -= OnIsEnabledUpdated;
            _isEnabled.Reset(true);
            _isInteractable.OnUpdate += OnIsInteractableUpdated;
            _isInteractable.Reset(true);
            
            UpdateManager.RemoveUpdatable(this);
            OnTerminate();
            State = ControllerState.Terminated;
        }

        protected virtual void OnInitialize() { }
        protected virtual void OnTerminate() { }

        protected virtual void OnEnter() { }
        protected virtual void OnEntered() { }
        
        protected virtual void OnExit() { }
        protected virtual void OnExited() { }

        public NavigationRequest<TWidget> CreateNavigationRequest(TWidget widget)
        {
            return _navigationCoordinator.CreateNavigationRequest(widget);
        }
        
        public NavigationRequest<TWidget> CreateNavigationRequest<TTarget>() where TTarget : class, TWidget
        {
            return _navigationCoordinator.CreateNavigationRequest<TTarget>();
        }
        
        public NavigationResponse<TWidget> Return(CancellationToken cancellationToken = default)
        {
            return _returnCoordinator.Return(cancellationToken);
        }
        
        public NavigationResponse<TWidget> Exit(in ExitRequest request)
        {
            return _exitCoordinator.Exit(in request);
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

        protected virtual void OnNavigationUpdate(NavigationResult<TWidget> result)
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

        private void OnWidgetShowing(IWidget widget)
        {
            WidgetShowing?.Invoke(widget);
        }
        
        private void OnWidgetShown(IWidget widget)
        {
            if (_visibilityState == VisibilityState.Entering)
            {
                _visibilityState = VisibilityState.Entered;
                Entered?.Invoke();
                OnEnter();
            }
            WidgetShown?.Invoke(widget);
        }

        private void OnWidgetHiding(IWidget widget)
        {
            WidgetHiding?.Invoke(widget);
        }
        
        private void OnWidgetHidden(IWidget widget)
        {
            if (_visibilityState == VisibilityState.Exiting)
            {
                _visibilityState = VisibilityState.Exited;
                Exited?.Invoke();
                OnExit();
            }
            WidgetHidden?.Invoke(widget);
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