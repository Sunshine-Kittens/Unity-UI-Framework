using UIFramework.Core.Interfaces;

namespace UIFramework.Navigation.Interfaces
{
    public interface INavigationRequestProcessor<TWidget> where TWidget : class, IWidget
    {
        public NavigationResponse<TWidget> ProcessNavigationRequest(in NavigationRequest<TWidget> request);
    }
}
