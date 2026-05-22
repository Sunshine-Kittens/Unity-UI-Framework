using UIFramework.Core.Interfaces;

namespace UIFramework.Navigation.Interfaces
{
    public interface IScreenNavigator : INavigateToRequestFactory<IScreen>, IReturnNavigator<IScreen>, IExitNavigator<IScreen>
    {
    }
}
