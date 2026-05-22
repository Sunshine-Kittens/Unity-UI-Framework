using UIFramework.Core.Interfaces;

namespace UIFramework.Navigation.Interfaces
{
    public interface INavigateToCoordinator<TWindow> where TWindow : class, IWindow
    {
        public NavigateToResponse<TWindow> NavigateTo(in NavigateToRequest<TWindow> request);
    }
}
