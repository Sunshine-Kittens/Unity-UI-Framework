using UIFramework.Core.Interfaces;
using UIFramework.Navigation.Base;
using UIFramework.Navigation.Context;
using UIFramework.Registry;

using UnityEngine.Extension;

namespace UIFramework.Navigation
{
    public sealed class WindowIndexNavigator<TWindow> : WindowNavigatorBase<TWindow, WindowIndexContext<TWindow>, NavigateToIndexResult<TWindow>>
        where TWindow : class, IWindow
    {
        public int ActiveIndex => ActiveType != null ? Registry.IndexOf(ActiveType) : -1;
        
        public WindowIndexNavigator(WidgetRegistry<TWindow> registry, Core.History history) : 
            base(new IndexedWindowContextProvider<TWindow>(registry), registry, history) { }

        public NavigateToIndexResult<TWindow> NavigateTo(int index)
        {
            if (Registry.Widgets.IsValidIndex(index))
            {
                return NavigateTo(Registry.Widgets[index]);
            }
            return InvokeNavigationUpdate(BuildResult(false, null, GetWindowContext(Active), GetHistoryMetadata(null)));
        }
    }
}
