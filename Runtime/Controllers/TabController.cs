using System;
using System.Collections.Generic;

using UIFramework.Controllers.Interfaces;
using UIFramework.Coordinators;
using UIFramework.Core.Interfaces;
using UIFramework.Navigation;
using UIFramework.Transitioning;

using UnityEngine.Extension;

namespace UIFramework.Controllers
{
    public sealed class TabController<TWindow> : Controller<TWindow>, ITabController<TWindow>
        where TWindow : class, IWindow
    {
        public IReadOnlyList<TWindow> Tabs => Registry.Widgets;

        public TWindow ActiveWindow => _navigator.ActiveInstance;
        public int ActiveIndex => _navigator.ActiveIndex;

        public event WindowIndexAction TabAdded;
        public event WindowIndexAction TabIndexChanged;
        public event WindowIndexAction TabRemoved;
        public event WindowAction WindowShown;
        public event WindowAction WindowHidden;

        private readonly WindowNavigator<TWindow> _navigator;
        private readonly TransitionManager _transitionManager;
        private readonly NavigateToCoordinator<TWindow> _coordinator;

        public TabController(TimeMode timeMode, IEnumerable<TWindow> windows)
            : base(timeMode)
        {
            if (windows == null) throw new ArgumentNullException(nameof(windows));

            foreach (TWindow window in windows)
                Registry.Register(window);

            _navigator = new WindowNavigator<TWindow>(Registry);
            _transitionManager = new TransitionManager(timeMode);
            // Tabs have no back-stack — history is null so the coordinator skips history tracking.
            _coordinator = new NavigateToCoordinator<TWindow>(timeMode, _navigator, null, _transitionManager);
        }

        protected override void OnWidgetInitialize(TWindow window)
        {
            window.Shown += OnWindowShown;
            window.Hidden += OnWindowHidden;
            window.SetVisibility(WidgetVisibility.Hidden);
        }

        protected override void OnWidgetTerminate(TWindow window)
        {
            window.Shown -= OnWindowShown;
            window.Hidden -= OnWindowHidden;
        }

        protected override void OnInitialize()
        {
            Registry.WidgetRegistered += OnWindowRegistered;
            Registry.WidgetIndexChanged += OnWindowIndexChanged;
            Registry.WidgetUnregistered += OnWindowUnregistered;
        }

        protected override void OnTerminate()
        {
            Registry.WidgetRegistered -= OnWindowRegistered;
            Registry.WidgetIndexChanged -= OnWindowIndexChanged;
            Registry.WidgetUnregistered -= OnWindowUnregistered;
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
            return new NavigateToRequest<TWindow>(_navigator, _coordinator, _navigator.ActiveInstance, Registry.Get<TTarget>());
        }

        public NavigateToRequest<TWindow> CreateNavigateToRequest(string identifier)
        {
            if (!IsInitialized)
                throw new InvalidOperationException("TabController is not initialized.");
            return new NavigateToRequest<TWindow>(_navigator, _coordinator, _navigator.ActiveInstance, Registry.Get(identifier));
        }

        public NavigateToRequest<TWindow> CreateNavigateToRequest(int index)
        {
            if (!IsInitialized)
                throw new InvalidOperationException("TabController is not initialized.");
            return new NavigateToRequest<TWindow>(_navigator, _coordinator, _navigator.ActiveInstance, Registry.Widgets[index]);
        }

        private void OnWindowShown(IWidget widget) => WindowShown?.Invoke(widget as IWindow);
        private void OnWindowHidden(IWidget widget) => WindowHidden?.Invoke(widget as IWindow);
        private void OnWindowRegistered(TWindow window, int index) => TabAdded?.Invoke(window, index);
        private void OnWindowIndexChanged(TWindow window, int index) => TabIndexChanged?.Invoke(window, index);
        private void OnWindowUnregistered(TWindow window, int index) => TabRemoved?.Invoke(window, index);

        public void AddTabWindow(TWindow widget)
        {
            if (!IsInitialized)
                throw new InvalidOperationException("TabController is not initialized.");
            Registry.Register(widget);
        }

        public void SetTabIndex(TWindow widget, int index)
        {
            if (!IsInitialized)
                throw new InvalidOperationException("TabController is not initialized.");
            Registry.SetIndex(widget, index);
        }

        public void RemoveTabWindow(TWindow widget)
        {
            if (!IsInitialized)
                throw new InvalidOperationException("TabController is not initialized.");
            Registry.Unregister(widget);
        }

        public void RemoveTabWindowAt(int index)
        {
            if (!IsInitialized)
                throw new InvalidOperationException("TabController is not initialized.");
            Registry.Unregister(Registry.Widgets[index]);
        }

        public void ClearTabs()
        {
            if (!IsInitialized)
                throw new InvalidOperationException("TabController is not initialized.");
            Registry.Clear();
        }
    }
}
