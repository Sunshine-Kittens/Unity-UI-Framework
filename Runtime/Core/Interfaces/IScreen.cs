using UIFramework.Controllers.Interfaces;

namespace UIFramework.Core.Interfaces
{
    public delegate void ScreenAction(IScreen screen);

    public interface IReadOnlyScreen : IReadOnlyWindow
    {
        public IScreenController Controller { get; }
    }

    public interface IScreen : IReadOnlyScreen, IWindow
    {
        public void SetController(IScreenController controller);
        public void ClearController();
    }
}