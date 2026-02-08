using System.Threading;

using UIFramework.Core.Interfaces;

namespace UIFramework.Navigation.Interfaces
{
    public interface IReturnNavigator<TWidget> where TWidget : class, IWidget
    {
        public NavigationResponse<TWidget> Return(CancellationToken cancellationToken = default);
    }
}
