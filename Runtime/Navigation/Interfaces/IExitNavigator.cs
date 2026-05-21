using UIFramework.Core.Interfaces;
using UIFramework.Navigation.Context;

namespace UIFramework.Navigation.Interfaces
{
    public interface IExitNavigator<TWindow> where TWindow : class, IWindow
    {
        public NavigateToResponse<TWindow> Exit(in ExitRequest request);
    }
}
