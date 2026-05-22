using UIFramework.Core.Interfaces;
using UIFramework.Navigation.Interfaces;
using UIFramework.Registry;

namespace UIFramework.Controllers.Interfaces
{
    public interface ITabController<TWindow> : INavigateToRequestFactory<TWindow>, INavigateToIndexRequestFactory<TWindow>
        where TWindow : class, IWindow
    {
        public IWidgetRegistry<TWindow> Registry { get; }
        public TWindow ActiveWindow { get; }
        public int ActiveIndex { get; }
        public bool IsInitialized { get; }

        public event WindowIndexAction TabAdded;
        public event WindowIndexAction TabRemoved;
        public event WindowAction WindowShown;
        public event WindowAction WindowHidden;

        public void Initialize();
        public void Terminate();
    }
}
