using UIFramework.Core.Interfaces;

namespace UIFramework.Navigation.Interfaces
{
    public interface INavigationRequestFactory<TWindow> where TWindow : class, IWindow
    {
        public NavigationRequest<TWindow> CreateNavigationRequest(TWindow window);
        public NavigationRequest<TWindow> CreateNavigationRequest<TTarget>() where TTarget : class, TWindow;
    }
}
