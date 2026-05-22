using UIFramework.Core.Interfaces;

namespace UIFramework.Navigation
{
    public readonly struct NavigateToResult<TWindow> where TWindow : class, IWindow
    {
        public readonly bool Success;
        public readonly TWindow Previous;
        public readonly TWindow Active;

        public NavigateToResult(bool success, TWindow previous, TWindow active)
        {
            Success = success;
            Previous = previous;
            Active = active;
        }
    }
}