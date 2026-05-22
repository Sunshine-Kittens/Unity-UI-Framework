using System;
using System.Collections.Generic;
using System.Threading;

using UIFramework.Collectors;
using UIFramework.Controllers.Interfaces;
using UIFramework.Core;
using UIFramework.Core.Interfaces;
using UIFramework.Groups;
using UIFramework.Navigation;
using UIFramework.Navigation.Interfaces;

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

        public bool IsInitialized => State == InitializationState.Initialized;
        public InitializationState State { get; private set; } = InitializationState.Uninitialized;
        public bool IsVisible => _visibilityState > 0 && _visibilityState < VisibilityState.Exited && Opacity > 0.0F;

        public float Opacity => _opacity;
        private float _opacity = 1.0F;

        public IScreen ActiveScreen => _group.ActiveScreen;
        public IScreen PreviousScreen => _group.PreviousScreen;

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

        public event Action Entering;
        public event Action Entered;
        public event Action Exiting;
        public event Action Exited;

        public event ScreenAction ScreenShowing;
        public event ScreenAction ScreenShown;
        public event ScreenAction ScreenHiding;
        public event ScreenAction ScreenHidden;

        private readonly ScreenGroup _group;
        private VisibilityState _visibilityState = 0;

        public ScreenController(IEnumerable<WidgetCollector<IScreen>> collectors, TimeMode timeMode)
        {
            _timeMode = timeMode;
            _group = new ScreenGroup(collectors, timeMode);
        }

        public void ManagedUpdate() => _group.ManagedUpdate();

        public void Initialize()
        {
            if (State == InitializationState.Initialized)
                throw new InvalidOperationException("Controller already initialized.");

            _group.Entering += OnGroupEntering;
            _group.Entered += OnGroupEntered;
            _group.Exiting += OnGroupExiting;
            _group.Exited += OnGroupExited;
            _group.ScreenShowing += OnScreenShowing;
            _group.ScreenShown += OnScreenShown;
            _group.ScreenHiding += OnScreenHiding;
            _group.ScreenHidden += OnScreenHidden;

            _group.Initialize();

            _isEnabled.OnUpdate += OnIsEnabledUpdated;
            _isInteractable.OnUpdate += OnIsInteractableUpdated;

            UpdateManager.AddUpdatable(this);
            OnInitialize();
            State = InitializationState.Initialized;
        }

        public void Terminate()
        {
            if (State != InitializationState.Initialized)
                throw new InvalidOperationException("Controller cannot be terminated.");

            _group.Entering -= OnGroupEntering;
            _group.Entered -= OnGroupEntered;
            _group.Exiting -= OnGroupExiting;
            _group.Exited -= OnGroupExited;
            _group.ScreenShowing -= OnScreenShowing;
            _group.ScreenShown -= OnScreenShown;
            _group.ScreenHiding -= OnScreenHiding;
            _group.ScreenHidden -= OnScreenHidden;

            _group.Terminate();

            _isEnabled.OnUpdate -= OnIsEnabledUpdated;
            _isEnabled.Reset(true);
            _isInteractable.OnUpdate -= OnIsInteractableUpdated;
            _isInteractable.Reset(true);

            UpdateManager.RemoveUpdatable(this);
            OnTerminate();
            State = InitializationState.Terminated;
        }

        protected virtual void OnInitialize() { }
        protected virtual void OnTerminate() { }

        protected virtual void OnEnter() { }
        protected virtual void OnEntered() { }
        protected virtual void OnExit() { }
        protected virtual void OnExited() { }

        public NavigateToRequest<IScreen> CreateNavigateToRequest(IScreen screen)
            => _group.CreateNavigateToRequest(screen);

        public NavigateToRequest<IScreen> CreateNavigateToRequest<TTarget>() where TTarget : class, IScreen
            => _group.CreateNavigateToRequest<TTarget>();

        public NavigateToResponse<IScreen> Return(CancellationToken cancellationToken = default)
            => _group.Return(cancellationToken);

        public NavigateToResponse<IScreen> Exit(in ExitRequest request)
            => _group.Exit(in request);

        public void SetOpacity(float opacity)
        {
            _opacity = opacity;
            _group.SetOpacity(opacity);
        }

        private void OnGroupEntering() { _visibilityState = VisibilityState.Entering; Entering?.Invoke(); OnEnter(); }
        private void OnGroupEntered() { _visibilityState = VisibilityState.Entered; Entered?.Invoke(); OnEntered(); }
        private void OnGroupExiting() { _visibilityState = VisibilityState.Exiting; Exiting?.Invoke(); OnExit(); }
        private void OnGroupExited() { _visibilityState = VisibilityState.Exited; Exited?.Invoke(); OnExited(); }

        private void OnScreenShowing(IScreen screen) => ScreenShowing?.Invoke(screen);
        private void OnScreenShown(IScreen screen) => ScreenShown?.Invoke(screen);
        private void OnScreenHiding(IScreen screen) => ScreenHiding?.Invoke(screen);
        private void OnScreenHidden(IScreen screen) => ScreenHidden?.Invoke(screen);

        private void OnIsEnabledUpdated(bool value) => _group.IsEnabled.Value = value;
        private void OnIsInteractableUpdated(bool value) => _group.IsInteractable.Value = value;
    }
}
