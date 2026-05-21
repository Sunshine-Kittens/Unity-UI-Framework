using System.Threading;

using UIFramework.Core.Interfaces;
using UIFramework.Navigation.Context;

namespace UIFramework.Navigation.Interfaces
{
    public interface IReturnNavigator<TWindow> where TWindow : class, IWindow
    {
        public NavigateToResponse<TWindow> Return(CancellationToken cancellationToken = default);
    }
}
