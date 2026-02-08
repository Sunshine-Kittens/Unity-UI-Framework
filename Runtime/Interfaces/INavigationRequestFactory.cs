using UIFramework.Navigation;

namespace UIFramework.Interfaces
{
    public interface INavigationRequestFactory<TWidget> where TWidget : class, IWidget
    {
        public NavigationRequest<TWidget> CreateNavigationRequest(TWidget widget);
        public NavigationRequest<TWidget> CreateNavigationRequest<TTarget>() where TTarget : class, TWidget;
        public bool IsRequestValid(in NavigationRequest<TWidget> request);
    }
}
