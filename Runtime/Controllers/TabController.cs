using System;

using UIFramework.Coordinators;
using UIFramework.Core.Interfaces;
using UIFramework.Navigation;
using UIFramework.Navigation.Interfaces;
using UIFramework.Registry;
using UIFramework.Transitioning;

using UnityEngine.Extension;

namespace UIFramework.Controllers
{
    public sealed class TabController<TWindow> : IActivateRequestFactory<TWindow>, IActivateIndexRequestFactory<TWindow> where TWindow : class, IWindow
    {
        public IWidgetRegistry<TWindow> Registry => _registry;
        
        public IWidget ActiveWindow => _activator.Active;
        public int ActiveIndex => _activator.GetActiveIndex();

        public event WindowIndexAction TabAdded;
        public event WindowIndexAction TabRemoved;
        
        public event WindowAction WindowShown;
        public event WindowAction WindowHidden;
        
        public bool IsInitialized => _registry.IsInitialized;
        
        private readonly WidgetRegistry<TWindow> _registry;
        private readonly WindowActivator<TWindow> _activator;
        private readonly TransitionManager _transitionManager;
        private readonly ActivateCoordinator<TWindow> _activateCoordinator;
        private readonly ActivateIndexCoordinator<TWindow> _activateIndexCoordinator;
        
        private TabController() { }

        public TabController(TimeMode timeMode, TWindow[] windows, int activeTabIndex = 0)
        {
            void OnInitializeWindow(IWidget window)
            {
                window.Shown += OnWindowShown;
                window.Hidden += OnWindowHidden;
                window.SetVisibility(WidgetVisibility.Hidden);
            }
            
            void OnTerminateWindow(IWidget window)
            {
                window.Shown -= OnWindowShown;
                window.Hidden -= OnWindowHidden;
            }
            
            _registry = new WidgetRegistry<TWindow>(OnInitializeWindow, OnTerminateWindow);
            foreach (TWindow window in windows)
            {
                _registry.Register(window);
            }
            
            _activator = new WindowActivator<TWindow>(_registry);
            _transitionManager = new TransitionManager(timeMode);
            ActivateRequestProcessor<TWindow> processor = new ActivateRequestProcessor<TWindow>(timeMode, _activator, _transitionManager);
            _activateCoordinator = new ActivateCoordinator<TWindow>(processor, _registry, _activator);
            _activateIndexCoordinator = new ActivateIndexCoordinator<TWindow>(processor, _registry, _activator);
        }

        public void Initialize()
        {
            if(IsInitialized)
                throw new InvalidOperationException("TabController is already initialized.");
            _registry.WidgetRegistered += OnWindowRegistered;
            _registry.WidgetUnregistered += OnWindowUnregistered;
            _registry.Initialize();
        }

        public void Terminate()
        {
            if(!IsInitialized)
                throw new InvalidOperationException("TabController is not initialized.");
            _registry.WidgetRegistered -= OnWindowRegistered;
            _registry.WidgetUnregistered -= OnWindowUnregistered;
            _registry.Terminate();
            _transitionManager.Terminate();
        }
        
        public ActivateRequest<TWindow> CreateActivateRequest(TWindow window)
        {
            if(!IsInitialized)
                throw new InvalidOperationException("TabController is  not initialized.");
            return _activateCoordinator.CreateActivateRequest(window);
        }
        
        public ActivateRequest<TWindow> CreateActivateRequest<TTarget>() where TTarget : class, TWindow
        {
            if(!IsInitialized)
                throw new InvalidOperationException("TabController is  not initialized.");
            return _activateCoordinator.CreateActivateRequest<TTarget>();
        }
        
        public ActivateRequest<TWindow> CreateActivateRequest(int index)
        {
            if(!IsInitialized)
                throw new InvalidOperationException("TabController is  not initialized.");
            return _activateIndexCoordinator.CreateActivateRequest(index);
        }
        
        private void OnWindowShown(IWidget window)
        {
            WindowShown?.Invoke(window as IWindow);
        }

        private void OnWindowHidden(IWidget window)
        {
            WindowHidden?.Invoke(window as IWindow);
        }
        
        private void OnWindowRegistered(TWindow window, int index)
        {
            TabAdded?.Invoke(window, index);
        }

        private void OnWindowUnregistered(TWindow window, int index)
        {
            TabRemoved?.Invoke(window, index);
        }
    }
}