using System.Threading;

using UIFramework.Core.Interfaces;

namespace UIFramework.Navigation.Interfaces
{
    public interface IReturnNavigator<TWindow> where TWindow : class, IWindow
    {
        public NavigationResponse<TWindow> Return(CancellationToken cancellationToken = default);
    }
}
