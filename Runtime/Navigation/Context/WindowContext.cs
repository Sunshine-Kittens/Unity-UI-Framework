using UIFramework.Core.Interfaces;

namespace UIFramework.Navigation.Context
{
    public record WindowContext<TWindow> where TWindow : class, IWindow
    {
        public TWindow Window { get; init; }
    }
}
