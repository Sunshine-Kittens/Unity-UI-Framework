using UIFramework.Core.Interfaces;
using UIFramework.Navigation.Context;
using UIFramework.Navigation.Interfaces;
using UIFramework.Navigation.Metadata;

namespace UIFramework.Navigation.Base
{
    public abstract class NavigateToResultBase<TWindow, TContext> : INavigateToResult<TWindow, TContext>
        where TWindow : class, IWindow
        where TContext : WindowContext<TWindow>
    {
        private protected NavigateToResultBase() { }
        
        public bool Success { get; init; }
        
        public TContext Previous { get; init; }
        public TContext Active { get; init; }
        
        public NavigationMetadata Metadata { get; init; }

        WindowContext<TWindow> INavigateToResult<TWindow>.Previous => Previous;
        WindowContext<TWindow> INavigateToResult<TWindow>.Active => Active;
    }
}
