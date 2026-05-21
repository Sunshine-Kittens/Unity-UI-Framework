using UIFramework.Core.Interfaces;

namespace UIFramework.Navigation.Context
{
    public class WindowContextProvider<TWindow> : IContextProvider<TWindow, WindowContext<TWindow>> where TWindow : class, IWindow
    {
        public WindowContext<TWindow> GetContext(TWindow window)
        {
            if (window != null)
                return new WindowContext<TWindow>{Window = window};
            return null;
        }
    }
}
