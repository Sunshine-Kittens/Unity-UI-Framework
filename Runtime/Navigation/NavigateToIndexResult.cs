using UIFramework.Core.Interfaces;
using UIFramework.Navigation.Base;
using UIFramework.Navigation.Context;

namespace UIFramework.Navigation
{
    public sealed class NavigateToIndexResult<TWindow> : NavigateToResultBase<TWindow, WindowIndexContext<TWindow>>
        where TWindow : class, IWindow { }
}
