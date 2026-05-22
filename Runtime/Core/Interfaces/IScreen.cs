using UIFramework.Navigation.Interfaces;

namespace UIFramework.Core.Interfaces
{
    public delegate void ScreenAction(IScreen screen);

    public interface IReadOnlyScreen : IReadOnlyWindow
    {
        public IScreenNavigator Navigator { get; }
    }

    public interface IScreen : IReadOnlyScreen, IWindow
    {
        public void SetNavigator(IScreenNavigator navigator);
        public void ClearNavigator();
    }
}
