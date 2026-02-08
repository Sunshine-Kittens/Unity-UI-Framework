using UIFramework.Navigation;

namespace UIFramework.Interfaces
{
    public interface IExitNavigator<TWidget> where TWidget : class, IWidget
    {
        public NavigationResponse<TWidget> Exit(in ExitRequest request);
    }
}
