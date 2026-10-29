using UIFramework.Core.Interfaces;

namespace UIFramework.Navigation.Interfaces
{
    public interface INavigateToIndexRequestFactory<TWindow> where TWindow : class, IWindow
    {
        public NavigateToRequest<TWindow> CreateNavigateToRequest(int index);
    }
}