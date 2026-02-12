using System;

using UIFramework.Core.Interfaces;
using UIFramework.Navigation;
using UIFramework.Navigation.Interfaces;
using UIFramework.Registry;

using UnityEngine.Extension;

namespace UIFramework.Coordinators
{
    public class ActivateIndexCoordinator<TWindow> : IActivateIndexRequestFactory<TWindow>, IActivateRequestProcessor<TWindow> where TWindow : class, IWindow
    {
        private readonly IActivateRequestProcessor<TWindow> _processor;
        private readonly WidgetRegistry<TWindow> _registry;
        private readonly WindowActivator<TWindow> _windowActivator;
        
        public ActivateIndexCoordinator(IActivateRequestProcessor<TWindow> processor, WidgetRegistry<TWindow> registry, WindowActivator<TWindow> windowActivator)
        {
            _processor = processor;
            _registry = registry;
            _windowActivator = windowActivator;
        }
        
        public ActivateRequest<TWindow> CreateActivateRequest(int index)
        {
            if (!_registry.Widgets.IsValidIndex(index))
                throw new IndexOutOfRangeException();
            
            return new ActivateRequest<TWindow>(_windowActivator, this, _windowActivator.Active, _registry.Widgets[index]);
        }
        
        public ActivateResponse<TWindow> ProcessActivateRequest(in ActivateRequest<TWindow> request)
        {
            return _processor.ProcessActivateRequest(request);
        }
    }
}
