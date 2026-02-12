using UIFramework.Core;
using UIFramework.Core.Interfaces;

namespace UIFramework.Navigation
{
    public readonly struct NavigationResult<TWindow> where TWindow : class, IWindow 
    {
        public readonly bool Success;
        public readonly TWindow Previous;
        public readonly TWindow Active;
        public readonly bool IsLocked;

        public readonly int HistoryCount;
        public readonly IHistoryEntry HistoryEntry;
        
        public NavigationResult(bool success, TWindow previous, TWindow active, bool isLocked, int historyCount, IHistoryEntry historyEntry)
        {
            Success = success;
            Previous = previous;
            Active = active;
            IsLocked = isLocked;
            HistoryCount = historyCount;
            HistoryEntry = historyEntry;
        }
    }
}
