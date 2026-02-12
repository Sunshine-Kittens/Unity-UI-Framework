using UIFramework.Core.Interfaces;

namespace UIFramework.Navigation.Interfaces
{
    public interface INavigationRequestProcessor<TWindow> where TWindow : class, IWindow
    {
        public NavigationResponse<TWindow> ProcessNavigationRequest(in NavigationRequest<TWindow> request);
    }
}
