using System.Threading;

using UIFramework.Navigation;

namespace UIFramework.Interfaces
{
    public interface IReturnNavigator<TWidget> where TWidget : class, IWidget
    {
        public NavigationResponse<TWidget> Return(CancellationToken cancellationToken = default);
    }
}
