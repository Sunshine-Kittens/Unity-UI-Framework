using UIFramework.Core;
using UIFramework.Core.Interfaces;
using UIFramework.Navigation;
using UIFramework.Navigation.Interfaces;
using UIFramework.Registry;
using UIFramework.Transitioning;

namespace UIFramework.Coordinators
{
    public readonly struct TransitionHistoryEvent : IHistoryEvent
    {
        public readonly VisibilityTransitionParams Transition;

        public TransitionHistoryEvent(VisibilityTransitionParams transition)
        {
            Transition = transition;
        }
    }
    
    public class NavigateToCoordinator<TWindow> : INavigateToRequestFactory<TWindow> where TWindow : class, IWindow
    {
        private readonly INavigateToRequestProcessor<TWindow> _processor;
        private readonly WidgetRegistry<TWindow> _registry;
        private readonly WindowNavigator<TWindow> _windowNavigator;
        
        public NavigateToCoordinator(INavigateToRequestProcessor<TWindow> processor, WidgetRegistry<TWindow> registry, 
            WindowNavigator<TWindow> windowNavigator)
        {
            _processor = processor;
            _registry = registry;
            _windowNavigator = windowNavigator;
        }
        
        public NavigateToRequest<TWindow> CreateNavigateToRequest(TWindow window)
        {
            return new NavigateToRequest<TWindow>(_windowNavigator, _processor, _windowNavigator.Active, window);
        }
        
        public NavigateToRequest<TWindow> CreateNavigateToRequest<TTarget>() where TTarget : class, TWindow
        {
            return new NavigateToRequest<TWindow>(_windowNavigator, _processor, _windowNavigator.Active, _registry.Get<TTarget>());
        }
    }
}
