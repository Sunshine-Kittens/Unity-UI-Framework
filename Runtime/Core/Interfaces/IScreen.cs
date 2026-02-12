using UIFramework.Controllers;

namespace UIFramework.Core.Interfaces
{
    public delegate void ScreenAction(IScreen screen);

    public interface IReadOnlyScreen : IReadOnlyWindow
    {
        public ScreenController Controller { get; }
    }

    public interface IScreen : IReadOnlyScreen, IWindow
    {
        public void SetController(ScreenController controller);
        public void ClearController();
    }
}