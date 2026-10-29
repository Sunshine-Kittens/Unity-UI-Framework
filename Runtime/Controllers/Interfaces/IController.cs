namespace UIFramework.Controllers.Interfaces
{
    // Lifecycle contract shared by all controllers (TabController, ScreenController, …).
    // Deliberately non-generic: every member here is window-type-agnostic, so there is nothing for a
    // TWindow parameter to bind to — templating it would add an unused type argument. Anything that is
    // window-typed (the managed-window collection) or navigational lives on the specific controller
    // interfaces, since that is where tab and screen controllers diverge.
    public interface IController
    {
        public bool IsInitialized { get; }

        public void Initialize();
        public void Terminate();
        public void Tick();
    }
}
