using System;
using System.Threading;

using UIFramework.Collectors;
using UIFramework.Controllers.Interfaces;
using UIFramework.Core.Interfaces;
using UIFramework.Groups;
using UIFramework.Navigation;

using UnityEngine;
using UnityEngine.Extension;

namespace UIFramework.Controllers
{
    // Drop-in MonoBehaviour host for a ScreenController. The serialized collectors define the registry of
    // available screens (controller-level, editor-time); groups are created and layered at runtime by the
    // controller. The host builds the pure-C# controller on enable, terminates it on disable, ticks it via
    // the UpdateManager, and exposes the controller plus navigation entry points for scene code.
    //
    // NOTE: clean enable -> disable -> enable cycles are not yet guaranteed. WidgetRegistry.Register only
    // initializes screens whose State is Uninitialized, and Terminate leaves them Terminated — so a rebuilt
    // controller on a second enable would skip re-initializing previously-terminated screens. Resolve in the
    // Runtime lifecycle (reset screens to Uninitialized on Terminate, or support re-init) before relying on
    // repeated enable/disable. Validate in play mode.
    public class ScreenControllerHost : MonoBehaviour, IUpdatable
    {
        [Tooltip("Collectors defining which screens this controller manages. Controller-level and editor-time.")]
        [SerializeField] private ScreenCollector[] _collectors = Array.Empty<ScreenCollector>();
        [SerializeField] private TimeMode _timeMode = TimeMode.Scaled;

        public bool Active => isActiveAndEnabled;
        public IScreenController Controller => _controller;
        public IScreenGroup ActiveGroup => _controller?.ActiveGroup;

        private ScreenController _controller;

        protected virtual void OnEnable()
        {
            _controller = CreateController();
            _controller.Initialize();
            UpdateManager.AddUpdatable(this);
        }

        protected virtual void OnDisable()
        {
            UpdateManager.RemoveUpdatable(this);
            if (_controller != null)
            {
                _controller.Terminate();
                _controller = null;
            }
        }

        public void ManagedUpdate() => _controller?.Tick();

        // Override to host a MenuController or a custom ScreenController subclass.
        protected virtual ScreenController CreateController() => new ScreenController(_collectors, _timeMode);

        // Navigation entry points — delegate to the active (top) group via the controller. The builder
        // configures the rest, e.g. host.NavigateTo<MyScreen>().WithTransition(...).Execute().
        public NavigateToRequest<IScreen> NavigateTo<TScreen>() where TScreen : class, IScreen =>
            _controller.CreateNavigateToRequest<TScreen>();

        public NavigateToRequest<IScreen> NavigateTo(string identifier) =>
            _controller.CreateNavigateToRequest(identifier);

        public NavigateToResponse<IScreen> Return(CancellationToken cancellationToken = default) =>
            _controller.Return(cancellationToken);

        public NavigateToResponse<IScreen> Exit(in ExitRequest request) =>
            _controller.Exit(in request);

        // Opens a new overlay group above the current one and returns it; the layer below stays visible but
        // input-paused. Navigate into it via the controller (which now targets the top group) or directly.
        public IScreenGroup PushGroup() => _controller.PushGroup();
    }
}
