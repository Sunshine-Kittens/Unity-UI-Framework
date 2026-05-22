using System;
using System.Collections.Generic;

using UIFramework.Collectors;
using UIFramework.Controllers.Interfaces;
using UIFramework.Core;
using UIFramework.Core.Interfaces;
using UIFramework.Groups;

using UnityEngine.Extension;

namespace UIFramework.Controllers
{
    public class ScreenGroupController : IScreenGroupController
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

        public IReadOnlyList<IScreenGroup> Groups => _groups;
        private readonly List<ScreenGroup> _groups = new();

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

        private VisibilityState _visibilityState = 0;
        private int _visibleGroupCount = 0;

        public ScreenGroupController(TimeMode timeMode)
        {
            _timeMode = timeMode;
        }

        public IScreenGroup AddGroup(IEnumerable<WidgetCollector<IScreen>> collectors)
        {
            if (IsInitialized)
                throw new InvalidOperationException("Cannot add groups after initialization.");
            ScreenGroup group = new ScreenGroup(collectors, _timeMode);
            _groups.Add(group);
            return group;
        }

        public void ManagedUpdate()
        {
            if (IsVisible)
            {
                foreach (ScreenGroup group in _groups)
                    group.ManagedUpdate();
            }
        }

        public void Initialize()
        {
            if (State == InitializationState.Initialized)
                throw new InvalidOperationException("ScreenGroupController already initialized.");

            foreach (ScreenGroup group in _groups)
            {
                group.Entering += OnGroupEntering;
                group.Entered += OnGroupEntered;
                group.Exiting += OnGroupExiting;
                group.Exited += OnGroupExited;
                group.Initialize();
            }

            _isEnabled.OnUpdate += OnIsEnabledUpdated;
            _isInteractable.OnUpdate += OnIsInteractableUpdated;

            UpdateManager.AddUpdatable(this);
            OnInitialize();
            State = InitializationState.Initialized;
        }

        public void Terminate()
        {
            if (State != InitializationState.Initialized)
                throw new InvalidOperationException("ScreenGroupController cannot be terminated.");

            foreach (ScreenGroup group in _groups)
            {
                group.Entering -= OnGroupEntering;
                group.Entered -= OnGroupEntered;
                group.Exiting -= OnGroupExiting;
                group.Exited -= OnGroupExited;
                group.Terminate();
            }

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

        public void SetOpacity(float opacity)
        {
            _opacity = opacity;
            foreach (ScreenGroup group in _groups)
                group.SetOpacity(opacity);
        }

        private void OnGroupEntering()
        {
            _visibleGroupCount++;
            if (_visibleGroupCount == 1)
            {
                _visibilityState = VisibilityState.Entering;
                Entering?.Invoke();
                OnEnter();
            }
        }

        private void OnGroupEntered()
        {
            if (_visibilityState == VisibilityState.Entering)
            {
                _visibilityState = VisibilityState.Entered;
                Entered?.Invoke();
                OnEntered();
            }
        }

        private void OnGroupExiting()
        {
            if (_visibleGroupCount == 1)
            {
                _visibilityState = VisibilityState.Exiting;
                Exiting?.Invoke();
                OnExit();
            }
        }

        private void OnGroupExited()
        {
            _visibleGroupCount--;
            if (_visibleGroupCount == 0)
            {
                _visibilityState = VisibilityState.Exited;
                Exited?.Invoke();
                OnExited();
            }
        }

        private void OnIsEnabledUpdated(bool value)
        {
            foreach (ScreenGroup group in _groups)
                group.IsEnabled.Value = value;
        }

        private void OnIsInteractableUpdated(bool value)
        {
            foreach (ScreenGroup group in _groups)
                group.IsInteractable.Value = value;
        }
    }
}
