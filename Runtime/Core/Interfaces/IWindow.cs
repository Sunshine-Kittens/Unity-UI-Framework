namespace UIFramework.Core.Interfaces
{
    public interface IReadOnlyWindow : IReadOnlyWidget { }

    /// <summary>
    /// Interface <c>IWindow</c> defines expected contract for all <c>UIFramework</c> windows.
    /// </summary>
    public interface IWindow : IReadOnlyWindow, IWidget
    {
        public void SetWaiting(bool waiting);
    }
}