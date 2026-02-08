using UIFramework.Controllers;

namespace UIFramework.Interfaces
{
    public interface IReadOnlyScreen : IReadOnlyWindow
    {
        
    }

    public interface IScreen : IReadOnlyScreen, IWindow 
    {
        public ScreenController Controller { get; }

        public TControllerType GetController<TControllerType>() where TControllerType : ScreenController;
        public void SetController(ScreenController controller);
        public bool SetBackButtonActive(bool active);
    }
}