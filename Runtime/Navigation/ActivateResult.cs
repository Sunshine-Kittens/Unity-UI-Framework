using UIFramework.Core.Interfaces;

namespace UIFramework.Navigation
{
    public readonly struct ActivateResult<TWidget> where TWidget : class, IWidget
    {
        public readonly bool Success;
        public readonly TWidget Active;
        public readonly TWidget Previous;
        public readonly int Index;

        public ActivateResult(bool success, TWidget active, TWidget previous, int index)
        {
            Success = success;
            Active = active;
            Previous = previous;
            Index = index;
        }
    }
}
