using UIFramework.Core.Interfaces;

namespace UIFramework.Navigation.Interfaces
{
    public interface IExitNavigator<TWindow> where TWindow : class, IWindow
    {
        public NavigationResponse<TWindow> Exit(in ExitRequest request);
    }
}
