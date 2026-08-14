using UIFramework.Core.Interfaces;

namespace UIFramework.Registry
{
    // Lifecycle policy for registry-held widgets: which state transitions are legal, and how to perform them.
    // Separated from WidgetRegistry so the registry keeps its collection and lockstep-initialization
    // responsibilities without hard-coding the transition rules.
    //
    // CanInitialize must return false for WidgetState.Initialized. WidgetBase.Initialize cascades to child
    // widgets unguarded and both backends throw on an already-initialized widget, so a policy that permits
    // re-entry on Initialized would break nested widgets.
    public interface IWidgetLifecycle<TWidget> where TWidget : class, IWidget
    {
        public bool CanInitialize(TWidget widget);
        public void Initialize(TWidget widget);

        public bool CanTerminate(TWidget widget);
        public void Terminate(TWidget widget);
    }

    public sealed class WidgetLifecycle<TWidget> : IWidgetLifecycle<TWidget> where TWidget : class, IWidget
    {
        public bool CanInitialize(TWidget widget) => widget.State == WidgetState.Uninitialized;
        public void Initialize(TWidget widget) => widget.Initialize();

        public bool CanTerminate(TWidget widget) => widget.State == WidgetState.Initialized;
        public void Terminate(TWidget widget) => widget.Terminate();
    }
}
