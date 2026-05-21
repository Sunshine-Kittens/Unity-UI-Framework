using UIFramework.Core.Interfaces;
using UIFramework.Navigation.Base;
using UIFramework.Navigation.Context;

namespace UIFramework.Navigation
{
    public sealed class NavigateToResult<TWindow> : NavigateToResultBase<TWindow, WindowContext<TWindow>>
        where TWindow : class, IWindow { }
}
