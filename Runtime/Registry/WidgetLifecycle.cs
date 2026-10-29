using UIFramework.Core.Interfaces;

namespace UIFramework.Registry
{
    // Lifecycle policy for registry-held widgets: which state transitions are legal, and how to perform them.
    // Separated from WidgetRegistry so the registry keeps its collection and lockstep-initialization
    // responsibilities without hard-coding the transition rules.
    //
    // A policy may be stricter than the widget's own rule but never looser: WidgetBase enforces the rule on
    // entry and throws, so permitting a transition the widget rejects turns a skipped widget into an exception.
    public interface IWidgetLifecycle<TWidget> where TWidget : class, IWidget
    {
        public bool CanInitialize(TWidget widget);
        public void Initialize(TWidget widget);

        public bool CanTerminate(TWidget widget);
        public void Terminate(TWidget widget);
    }

    public sealed class WidgetLifecycle<TWidget> : IWidgetLifecycle<TWidget> where TWidget : class, IWidget
    {
        // Defers to the widget rather than restating the rule: two copies can drift, and a policy that rejects
        // a Terminated widget would leave one whose host has cycled registered but dead.
        public bool CanInitialize(TWidget widget) => widget.CanInitialize;
        public void Initialize(TWidget widget) => widget.Initialize();

        public bool CanTerminate(TWidget widget) => widget.CanTerminate;
        public void Terminate(TWidget widget) => widget.Terminate();
    }
}
