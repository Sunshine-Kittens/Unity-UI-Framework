using UIFramework.Controllers;

namespace UIFramework.Core.Interfaces
{
    public interface IReadOnlyScreen : IReadOnlyWindow
    {
        
    }

    public interface IScreen : IReadOnlyScreen, IWindow 
    {
        public Controller<IScreen> Controller { get; }

        public TController GetController<TController>() where TController : Controller<IScreen>;
        public void SetController(Controller<IScreen> controller);
        public bool SetBackButtonActive(bool active);
    }
}