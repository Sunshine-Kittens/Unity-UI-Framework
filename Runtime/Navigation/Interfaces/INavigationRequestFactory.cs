using UIFramework.Core.Interfaces;

namespace UIFramework.Navigation.Interfaces
{
    public interface INavigationRequestFactory<TWidget> where TWidget : class, IWidget
    {
        public NavigationRequest<TWidget> CreateNavigationRequest(TWidget widget);
        public NavigationRequest<TWidget> CreateNavigationRequest<TTarget>() where TTarget : class, TWidget; }
}
