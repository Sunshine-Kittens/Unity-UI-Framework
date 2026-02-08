using UIFramework.Core.Interfaces;

namespace UIFramework.Navigation.Interfaces
{
    public interface IExitNavigator<TWidget> where TWidget : class, IWidget
    {
        public NavigationResponse<TWidget> Exit(in ExitRequest request);
    }
}
