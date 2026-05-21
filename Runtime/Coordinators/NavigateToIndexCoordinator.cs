using UIFramework.Core.Interfaces;
using UIFramework.Navigation;
using UIFramework.Navigation.Context;
using UIFramework.Navigation.Interfaces;
using UIFramework.Registry;

namespace UIFramework.Coordinators
{
    public class NavigateToIndexCoordinator<TWindow> : INavigateToIndexRequestFactory<TWindow, WindowIndexContext<TWindow>> where TWindow : class, IWindow
    {
        private readonly INavigateToRequestProcessor<TWindow, WindowIndexContext<TWindow>> _processor;
        private readonly WidgetRegistry<TWindow> _registry;
        private readonly WindowNavigator<TWindow, WindowIndexContext<TWindow>> _windowNavigator;
        
        public NavigateToIndexCoordinator(INavigateToRequestProcessor<TWindow, WindowIndexContext<TWindow>> processor, WidgetRegistry<TWindow> registry, 
            WindowNavigator<TWindow, WindowIndexContext<TWindow>> windowNavigator)
        {
            _processor = processor;
            _registry = registry;
            _windowNavigator = windowNavigator;
        }
        
        public NavigateToRequest<TWindow, WindowIndexContext<TWindow>> CreateNavigateToIndexRequest(int index)
        {
            return new NavigateToRequest<TWindow, WindowIndexContext<TWindow>>(_windowNavigator, _processor, _windowNavigator.Active, _registry.Widgets[index]);
        }
    }
}
