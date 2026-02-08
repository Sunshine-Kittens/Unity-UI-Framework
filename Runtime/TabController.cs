using UnityEngine.Extension;

using UIFramework.Coordinators;
using UIFramework.Interfaces;
using UIFramework.Navigation;
using UIFramework.Registry;
using UIFramework.WidgetTransition;

namespace UIFramework
{
    public sealed class TabController<TWidget> : 
        IActivateRequestFactory<TWidget>,
        IActivateIndexRequestFactory<TWidget>
        where TWidget : class, IWidget
    {
        public IWidgetRegistry<TWidget> Registry => _registry;
        
        public IWidget ActiveWidget => _activator.Active;
        public int ActiveIndex => _activator.GetActiveIndex();
        
        public event WidgetAction WidgetShown;
        public event WidgetAction WidgetHidden;
        
        private readonly WidgetRegistry<TWidget> _registry;
        private readonly WidgetActivator<TWidget> _activator;
        private readonly ActivateCoordinator<TWidget> _activateCoordinator;
        private readonly ActivateIndexCoordinator<TWidget> _activateIndexCoordinator;
        
        private TabController() { }

        public TabController(TimeMode timeMode, TWidget[] widgets, int activeTabIndex = 0)
        {
            void OnInit(IWidget widget)
            {
                widget.Shown += OnWidgetShown;
                widget.Hidden += OnWidgetHidden;
                widget.SetVisibility(WidgetVisibility.Hidden);
            }
            
            _registry = new WidgetRegistry<TWidget>(OnInit);
            _registry.WidgetRegistered += OnWidgetRegistered;
            _registry.WidgetUnregistered += OnWidgetUnregistered;
            
            _activator = new WidgetActivator<TWidget>(_registry);
            TransitionManager transitionManager = new TransitionManager(timeMode);
            ActivateRequestProcessor<TWidget> processor = new ActivateRequestProcessor<TWidget>(timeMode, _activator, transitionManager);
            _activateCoordinator = new ActivateCoordinator<TWidget>(processor, _registry, _activator);
            _activateIndexCoordinator = new ActivateIndexCoordinator<TWidget>(processor, _registry, _activator);
        }

        public ActivateRequest<TWidget> CreateActivateRequest(TWidget widget)
        {
            return _activateCoordinator.CreateActivateRequest(widget);
        }
        
        public ActivateRequest<TWidget> CreateActivateRequest<TTarget>() where TTarget : class, TWidget
        {
            return _activateCoordinator.CreateActivateRequest<TTarget>();
        }
        
        public ActivateRequest<TWidget> CreateActivateRequest(int index)
        {
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
        
        private void OnWidgetRegistered(TWidget widget)
        {
            
        }

        private void OnWidgetUnregistered(TWidget widget)
        {
            
        }
    }
}