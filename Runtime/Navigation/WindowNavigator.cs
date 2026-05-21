using UIFramework.Core.Interfaces;
using UIFramework.Navigation.Base;
using UIFramework.Navigation.Context;
using UIFramework.Navigation.Interfaces;
using UIFramework.Registry;

namespace UIFramework.Navigation
{
    public sealed class WindowNavigator<TWindow> : WindowNavigatorBase<TWindow, WindowContext<TWindow>, NavigateToResult<TWindow>>
        where TWindow : class, IWindow
    {
        public WindowNavigator(IContextProvider<TWindow, WindowContext<TWindow>> contextProvider, WidgetRegistry<TWindow> registry, Core.History history) 
            : base(contextProvider, registry, history) { }
    }
}
