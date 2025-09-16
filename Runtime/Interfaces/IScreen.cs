namespace UIFramework
{
    public interface IReadOnlyScreen : IReadOnlyWindow
    {
        
    }

    public interface IScreen : IReadOnlyScreen, IWindow
    {
        public Controller Controller { get; }

        public TControllerType GetController<TControllerType>() where TControllerType : Controller;
        public void SetController(Controller controller);
        public bool SetBackButtonActive(bool active);
    }
}