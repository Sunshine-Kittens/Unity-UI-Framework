using UIFramework.Core;

namespace UIFramework.Navigation.Metadata
{
    public sealed record NavigationHistoryMetadata :  NavigationMetadata
    {
        public int HistoryCount { get; init; }
        public IHistoryEntry HistoryEntry { get; init; }
    }
}
