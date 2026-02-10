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
    public sealed class TabController<TWidget> : IActivateRequestFactory<TWidget>, IActivateIndexRequestFactory<TWidget> where TWidget : class, IWidget
    {
        public IWidgetRegistry<TWidget> Registry => _registry;
        
        public IWidget ActiveWidget => _activator.Active;
        public int ActiveIndex => _activator.GetActiveIndex();

        public event Action<TWidget, int> TabAdded;
        public event Action<TWidget, int> TabRemoved;
        
        public event WidgetAction WidgetShown;
        public event WidgetAction WidgetHidden;
        
        public bool IsInitialized => _registry.IsInitialized;
        
        private readonly WidgetRegistry<TWidget> _registry;
        private readonly WidgetActivator<TWidget> _activator;
        private readonly TransitionManager _transitionManager;
        private readonly ActivateCoordinator<TWidget> _activateCoordinator;
        private readonly ActivateIndexCoordinator<TWidget> _activateIndexCoordinator;
        
        private TabController() { }

        public TabController(TimeMode timeMode, TWidget[] widgets, int activeTabIndex = 0)
        {
            void OnInitializeWidget(IWidget widget)
            {
                widget.Shown += OnWidgetShown;
                widget.Hidden += OnWidgetHidden;
                widget.SetVisibility(WidgetVisibility.Hidden);
            }
            
            void OnTerminateWidget(IWidget widget)
            {
                widget.Shown -= OnWidgetShown;
                widget.Hidden -= OnWidgetHidden;
            }
            
            _registry = new WidgetRegistry<TWidget>(OnInitializeWidget, OnTerminateWidget);
            foreach (TWidget widget in widgets)
            {
                _registry.Register(widget);
            }
            
            _activator = new WidgetActivator<TWidget>(_registry);
            _transitionManager = new TransitionManager(timeMode);
            ActivateRequestProcessor<TWidget> processor = new ActivateRequestProcessor<TWidget>(timeMode, _activator, _transitionManager);
            _activateCoordinator = new ActivateCoordinator<TWidget>(processor, _registry, _activator);
            _activateIndexCoordinator = new ActivateIndexCoordinator<TWidget>(processor, _registry, _activator);
        }

        public void Initialize()
        {
            if(IsInitialized)
                throw new InvalidOperationException("TabController is already initialized.");
            _registry.WidgetRegistered += OnWidgetRegistered;
            _registry.WidgetUnregistered += OnWidgetUnregistered;
            _registry.Initialize();
        }

        public void Terminate()
        {
            if(!IsInitialized)
                throw new InvalidOperationException("TabController is not initialized.");
            _registry.WidgetRegistered -= OnWidgetRegistered;
            _registry.WidgetUnregistered -= OnWidgetUnregistered;
            _registry.Terminate();
            _transitionManager.Terminate();
        }
        
        public ActivateRequest<TWidget> CreateActivateRequest(TWidget widget)
        {
            if(!IsInitialized)
                throw new InvalidOperationException("TabController is  not initialized.");
            return _activateCoordinator.CreateActivateRequest(widget);
        }
        
        public ActivateRequest<TWidget> CreateActivateRequest<TTarget>() where TTarget : class, TWidget
        {
            if(!IsInitialized)
                throw new InvalidOperationException("TabController is  not initialized.");
            return _activateCoordinator.CreateActivateRequest<TTarget>();
        }
        
        public ActivateRequest<TWidget> CreateActivateRequest(int index)
        {
            if(!IsInitialized)
                throw new InvalidOperationException("TabController is  not initialized.");
            return _activateIndexCoordinator.CreateActivateRequest(index);
        }
        
        private void OnWidgetShown(IWidget widget)
        {
            WidgetShown?.Invoke(widget);
        }

        private void OnWidgetHidden(IWidget widget)
        {
            WidgetHidden?.Invoke(widget);
        }
        
        private void OnWidgetRegistered(TWidget widget, int index)
        {
            TabAdded?.Invoke(widget, index);
        }

        private void OnWidgetUnregistered(TWidget widget, int index)
        {
            TabRemoved?.Invoke(widget, index);
        }
    }
}