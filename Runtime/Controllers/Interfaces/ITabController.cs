using System.Collections.Generic;

using UIFramework.Core.Interfaces;
using UIFramework.Navigation.Interfaces;

namespace UIFramework.Controllers.Interfaces
{
    public interface ITabController<TWindow> : INavigateToRequestFactory<TWindow>, INavigateToIndexRequestFactory<TWindow>
        where TWindow : class, IWindow
    {
        public IReadOnlyList<TWindow> Tabs { get; }
        
        public TWindow ActiveWindow { get; }
        public int ActiveIndex { get; }
        public bool IsInitialized { get; }

        public event WindowIndexAction TabAdded;
        public event WindowIndexAction TabIndexChanged;
        public event WindowIndexAction TabRemoved;
        public event WindowAction WindowShown;
        public event WindowAction WindowHidden;

        public void Initialize();
        public void Terminate();

        public void AddTabWindow(TWindow widget);
        public void SetTabIndex(TWindow widget, int index);

        public void RemoveTabWindow(TWindow widget);
        public void RemoveTabWindowAt(int index);

        public void ClearTabs();
    }
}
