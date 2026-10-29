namespace UIFramework.TestUtils
{
    // Real uGUI screens for play-mode tests. Abstract because WidgetRegistry is Type-keyed: two instances of one
    // concrete type cannot share a registry, so tests use the distinct subclasses below.
    public abstract class RecordingScreen : UIFramework.UGUI.Screen
    {
        public int InitializeCount { get; private set; }
        public int TerminateCount { get; private set; }

        protected override void OnInitialize() => InitializeCount++;
        protected override void OnTerminate() => TerminateCount++;
    }

    public sealed class UguiScreenA : RecordingScreen { }
    public sealed class UguiScreenB : RecordingScreen { }
    public sealed class UguiScreenC : RecordingScreen { }

    // A plain nested widget: not a screen, so a screen collector never registers it and only its parent's
    // cascade drives its lifecycle.
    public sealed class RecordingWidget : UIFramework.UGUI.Widget
    {
        public int InitializeCount { get; private set; }
        public int TerminateCount { get; private set; }

        protected override void OnInitialize() => InitializeCount++;
        protected override void OnTerminate() => TerminateCount++;
    }
}
