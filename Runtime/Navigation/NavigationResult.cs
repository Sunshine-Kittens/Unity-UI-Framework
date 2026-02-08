namespace UIFramework.Navigation
{
    public readonly struct NavigationResult<TWidget> where TWidget : class, IWidget
    {
        public readonly bool Success;
        public readonly TWidget Previous;
        public readonly TWidget Active;
        public readonly bool IsLocked;

        public readonly int HistoryCount;
        public readonly IHistoryEntry HistoryEntry;
        
        public NavigationResult(bool success, TWidget previous, TWidget active, bool isLocked, int historyCount, IHistoryEntry historyEntry)
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
