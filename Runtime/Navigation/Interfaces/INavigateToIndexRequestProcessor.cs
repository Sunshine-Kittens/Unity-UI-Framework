using UIFramework.Core.Interfaces;

namespace UIFramework.Navigation.Interfaces
{
    public interface INavigateToIndexRequestProcessor<TWindow> where TWindow : class, IWindow
    {
        public NavigateToIndexResponse<TWindow> ProcessNavigateToRequest(in NavigateToIndexRequest<TWindow> request);
    }
}
