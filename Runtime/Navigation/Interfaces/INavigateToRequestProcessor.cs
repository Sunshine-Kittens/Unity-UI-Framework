using UIFramework.Core.Interfaces;

namespace UIFramework.Navigation.Interfaces
{
    public interface INavigateToRequestProcessor<TWindow> where TWindow : class, IWindow
    {
        public NavigateToResponse<TWindow> ProcessNavigateToRequest(in NavigateToRequest<TWindow> request);
    }
}
