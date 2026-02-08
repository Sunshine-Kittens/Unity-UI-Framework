using UIFramework.Interfaces;
using UIFramework.Navigation;
using UIFramework.Registry;

namespace UIFramework.Coordinators
{
    public class ActivateCoordinator<TWidget> : IActivateRequestFactory<TWidget>, IActivateRequestProcessor<TWidget> where TWidget : class, IWidget
    {
        private readonly IActivateRequestProcessor<TWidget> _processor;
        private readonly WidgetRegistry<TWidget> _registry;
        private readonly WidgetActivator<TWidget> _widgetActivator;
        
        public ActivateCoordinator(IActivateRequestProcessor<TWidget> processor, WidgetRegistry<TWidget> registry, WidgetActivator<TWidget> widgetActivator)
        {
            _processor = processor;
            _registry = registry;
            _widgetActivator = widgetActivator;
        }
        
        public ActivateRequest<TWidget> CreateActivateRequest(TWidget widget)
        {
            return new ActivateRequest<TWidget>(_widgetActivator, this, _widgetActivator.Active, widget);
        }
        
        public ActivateRequest<TWidget> CreateActivateRequest<TTarget>() where TTarget : class, TWidget
        {
            return new ActivateRequest<TWidget>(_widgetActivator, this, _widgetActivator.Active, _registry.Get<TTarget>());
        }
        
        public ActivateResponse<TWidget> ProcessActivateRequest(in ActivateRequest<TWidget> request)
        {
            return _processor.ProcessActivateRequest(request);
        }
    }
}
