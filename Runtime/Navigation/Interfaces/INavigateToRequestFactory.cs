using UIFramework.Core.Interfaces;

namespace UIFramework.Navigation.Interfaces
{
    public interface INavigateToRequestFactory<TWindow> where TWindow : class, IWindow
    {
        public NavigateToRequest<TWindow> CreateNavigateToRequest(TWindow window);
        public NavigateToRequest<TWindow> CreateNavigateToRequest<TTarget>() where TTarget : class, TWindow;
        public NavigateToRequest<TWindow> CreateNavigateToRequest(string identifier);
    }
}
