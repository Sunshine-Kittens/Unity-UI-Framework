using UIFramework.Core.Interfaces;
using UIFramework.Navigation.Context;
using UIFramework.Navigation.Metadata;

namespace UIFramework.Navigation.Interfaces
{
    public interface INavigateToResult<TWindow> where TWindow : class, IWindow
    {
        public bool Success { get; }
        
        public WindowContext<TWindow> Previous { get; }
        public WindowContext<TWindow> Active { get; }
        
        public NavigationMetadata Metadata { get; }
    }
    
    public interface INavigateToResult<TWindow, out TContext> : INavigateToResult<TWindow>
        where TWindow : class, IWindow
        where TContext : WindowContext<TWindow> 
    {
        new TContext Previous { get; }
        new TContext Active { get; }
    }
}
