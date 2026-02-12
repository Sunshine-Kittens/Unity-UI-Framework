using UIFramework.Core.Interfaces;
using UIFramework.Navigation;
using UIFramework.Navigation.Interfaces;
using UIFramework.Registry;

namespace UIFramework.Coordinators
{
    public class ActivateCoordinator<TWindow> : IActivateRequestFactory<TWindow>, IActivateRequestProcessor<TWindow> where TWindow : class, IWindow
    {
        private readonly IActivateRequestProcessor<TWindow> _processor;
        private readonly WidgetRegistry<TWindow> _registry;
        private readonly WindowActivator<TWindow> _windowActivator;
        
        public ActivateCoordinator(IActivateRequestProcessor<TWindow> processor, WidgetRegistry<TWindow> registry, WindowActivator<TWindow> windowActivator)
        {
            _processor = processor;
            _registry = registry;
            _windowActivator = windowActivator;
        }
        
        public ActivateRequest<TWindow> CreateActivateRequest(TWindow window)
        {
            return new ActivateRequest<TWindow>(_windowActivator, this, _windowActivator.Active, window);
        }
        
        public ActivateRequest<TWindow> CreateActivateRequest<TTarget>() where TTarget : class, TWindow
        {
            return new ActivateRequest<TWindow>(_windowActivator, this, _windowActivator.Active, _registry.Get<TTarget>());
        }
        
        public ActivateResponse<TWindow> ProcessActivateRequest(in ActivateRequest<TWindow> request)
        {
            return _processor.ProcessActivateRequest(request);
        }
    }
}
