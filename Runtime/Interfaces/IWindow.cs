using UnityEngine.Extension;

namespace UIFramework
{
    public interface IReadOnlyWindow : IReadOnlyWidget
    {
        public bool SupportsHistory { get; }
    }

    /// <summary>
    /// Interface <c>IWindow</c> defines expected contract for all <c>UIFramework</c> windows.
    /// </summary>
    public interface IWindow : IReadOnlyWindow, IWidget
    {
        public void SetWaiting(bool waiting);
    }
}