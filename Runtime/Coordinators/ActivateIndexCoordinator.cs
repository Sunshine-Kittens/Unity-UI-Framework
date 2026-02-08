using System;

using UIFramework.Core.Interfaces;
using UIFramework.Navigation;
using UIFramework.Navigation.Interfaces;
using UIFramework.Registry;

using UnityEngine.Extension;

namespace UIFramework.Coordinators
{
    public class ActivateIndexCoordinator<TWidget> : IActivateIndexRequestFactory<TWidget>, IActivateRequestProcessor<TWidget> where TWidget : class, IWidget
    {
        private readonly IActivateRequestProcessor<TWidget> _processor;
        private readonly WidgetRegistry<TWidget> _registry;
        private readonly WidgetActivator<TWidget> _widgetActivator;
        
        public ActivateIndexCoordinator(IActivateRequestProcessor<TWidget> processor, WidgetRegistry<TWidget> registry, WidgetActivator<TWidget> widgetActivator)
        {
            _processor = processor;
            _registry = registry;
            _widgetActivator = widgetActivator;
        }
        
        public ActivateRequest<TWidget> CreateActivateRequest(int index)
        {
            if (!_registry.Widgets.IsValidIndex(index))
                throw new IndexOutOfRangeException();
            
            return new ActivateRequest<TWidget>(_widgetActivator, this, _widgetActivator.Active, _registry.Widgets[index]);
        }
        
        public ActivateResponse<TWidget> ProcessActivateRequest(in ActivateRequest<TWidget> request)
        {
            return _processor.ProcessActivateRequest(request);
        }
    }
}
