using UIFramework.Core.Interfaces;

namespace UIFramework.Navigation.Context
{
    public interface IContextProvider<in TWindow, out TContext> where TWindow : class, IWindow where TContext : WindowContext<TWindow>
    {
        public TContext GetContext(TWindow window);
    }
}
