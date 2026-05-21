using UIFramework.Core.Interfaces;

namespace UIFramework.Navigation.Context
{
    public record WindowIndexContext<TWindow> : WindowContext<TWindow> where TWindow : class, IWindow
    {
        public int Index { get; init; }
    }
}
