using UIFramework.Core.Interfaces;

namespace UIFramework.Navigation
{
    public readonly struct ActivateResult<TWindow> where TWindow : class, IWidget
    {
        public readonly bool Success;
        public readonly TWindow Active;
        public readonly TWindow Previous;
        public readonly int Index;

        public ActivateResult(bool success, TWindow active, TWindow previous, int index)
        {
            Success = success;
            Active = active;
            Previous = previous;
            Index = index;
        }
    }
}
