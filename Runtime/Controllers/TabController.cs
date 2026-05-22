using System;
using System.Collections.Generic;

using UIFramework.Coordinators;
using UIFramework.Core.Interfaces;
using UIFramework.Navigation;
using UIFramework.Navigation.Interfaces;
using UIFramework.Registry;
using UIFramework.Transitioning;

using UnityEngine.Extension;

namespace UIFramework.Controllers
{
    public sealed class TabController<TWindow> : INavigateToRequestFactory<TWindow>, INavigateToIndexRequestFactory<TWindow> where TWindow : class, IWindow
    {
        public IWidgetRegistry<TWindow> Registry => _registry;
        public TWindow ActiveWindow => _navigator.ActiveInstance;
        public int ActiveIndex => _navigator.ActiveIndex;
        public bool IsInitialized => _registry.IsInitialized;

        public event WindowIndexAction TabAdded;
        public event WindowIndexAction TabRemoved;
        public event WindowAction WindowShown;
        public event WindowAction WindowHidden;

        private readonly WidgetRegistry<TWindow> _registry;
        private readonly WindowNavigator<TWindow> _navigator;
        private readonly TransitionManager _transitionManager;
        private readonly NavigateToCoordinator<TWindow> _coordinator;

        public TabController(TimeMode timeMode, IEnumerable<TWindow> windows)
        {
            if (windows == null) throw new ArgumentNullException(nameof(windows));

            void OnWindowInitialize(IWidget widget)
            {
                widget.Shown += OnWindowShown;
                widget.Hidden += OnWindowHidden;
                widget.SetVisibility(WidgetVisibility.Hidden);
            }

            void OnWindowTerminate(IWidget widget)
            {
                widget.Shown -= OnWindowShown;
                widget.Hidden -= OnWindowHidden;
            }

            _registry = new WidgetRegistry<TWindow>(OnWindowInitialize, OnWindowTerminate);
            foreach (TWindow window in windows)
                _registry.Register(window);

            _navigator = new WindowNavigator<TWindow>(_registry);
            _transitionManager = new TransitionManager(timeMode);
            // Tabs have no back-stack — history is null so the coordinator skips history tracking.
            _coordinator = new NavigateToCoordinator<TWindow>(timeMode, _navigator, null, _transitionManager);
        }

        public void Initialize()
        {
            if (IsInitialized)
                throw new InvalidOperationException("TabController is already initialized.");
            _registry.WidgetRegistered += OnWindowRegistered;
            _registry.WidgetUnregistered += OnWindowUnregistered;
            _registry.Initialize();
        }

        public void Terminate()
        {
            if (!IsInitialized)
                throw new InvalidOperationException("TabController is not initialized.");
            _registry.WidgetRegistered -= OnWindowRegistered;
            _registry.WidgetUnregistered -= OnWindowUnregistered;
            _registry.Terminate();
            _transitionManager.Terminate();
        }

        public NavigateToRequest<TWindow> CreateNavigateToRequest(TWindow window)
        {
            if (!IsInitialized)
                throw new InvalidOperationException("TabController is not initialized.");
            return new NavigateToRequest<TWindow>(_navigator, _coordinator, _navigator.ActiveInstance, window);
        }

        public NavigateToRequest<TWindow> CreateNavigateToRequest<TTarget>() where TTarget : class, TWindow
        {
            if (!IsInitialized)
                throw new InvalidOperationException("TabController is not initialized.");
            return new NavigateToRequest<TWindow>(_navigator, _coordinator, _navigator.ActiveInstance, _registry.Get<TTarget>());
        }

        public NavigateToRequest<TWindow> CreateNavigateToRequest(int index)
        {
            if (!IsInitialized)
                throw new InvalidOperationException("TabController is not initialized.");
            return new NavigateToRequest<TWindow>(_navigator, _coordinator, _navigator.ActiveInstance, _registry.Widgets[index]);
        }

        private void OnWindowShown(IWidget widget) => WindowShown?.Invoke(widget as IWindow);
        private void OnWindowHidden(IWidget widget) => WindowHidden?.Invoke(widget as IWindow);

        private void OnWindowRegistered(TWindow window, int index) => TabAdded?.Invoke(window, index);
        private void OnWindowUnregistered(TWindow window, int index) => TabRemoved?.Invoke(window, index);
    }
}
