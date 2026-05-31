using System;
using System.Collections.Generic;
using System.Threading;

using UIFramework.Collectors;
using UIFramework.Controllers.Interfaces;
using UIFramework.Core.Interfaces;
using UIFramework.Groups;
using UIFramework.Navigation;

using UnityEngine.Extension;

namespace UIFramework.Controllers
{
    // Manages screens as a stack of layered ScreenGroups over the controller's one shared registry.
    // Navigation delegates to the active (top) group; PushGroup overlays a new group above the current one
    // (which stays visible, input-paused). Groups are pooled: taken on push, reset and returned on collapse.
    // A group collapses either by bubble-return (Return at its root) or a screen-driven Exit; either way the
    // group's Exited event drives the pop + resume-below here.
    public class ScreenController : Controller<IScreen>, IScreenController
    {
        // Render band assigned per stacked group (via GlobalSortOrder). The step leaves headroom between
        // layers so distinct canvases / UITK panels separate cleanly.
        private const int _LayerBandStep = 100;

        // Covariant reference conversion: List<ScreenGroup> -> IReadOnlyList<IScreenGroup>.
        public IReadOnlyList<IScreenGroup> Groups => _groups;
        public IScreenGroup ActiveGroup => _groups.Count > 0 ? _groups[^1] : null;

        private readonly IEnumerable<WidgetCollector<IScreen>> _collectors;
        private readonly List<ScreenGroup> _groups = new();
        private readonly Stack<ScreenGroup> _pool = new();

        public ScreenController(IEnumerable<WidgetCollector<IScreen>> collectors, TimeMode timeMode)
            : base(timeMode)
        {
            _collectors = collectors ?? throw new ArgumentNullException(nameof(collectors));
        }

        protected override void OnInitialize()
        {
            // The base has already initialized the registry; collecting now registers + initializes each
            // screen (post-init Register initializes it immediately and fires OnWidgetInitialize).
            Registry.Collect(_collectors);
        }

        protected override void OnWidgetInitialize(IScreen window)
        {
            // Screens start hidden; a group shows them when navigated to. All per-screen navigation wiring
            // (navigator back-ref, event subscriptions, opacity/band) is the group's, applied on join.
            window.SetVisibility(WidgetVisibility.Hidden);
        }

        protected override void OnTerminate()
        {
            for (int i = _groups.Count - 1; i >= 0; i--)
                _groups[i].Reset();
            _groups.Clear();
            _pool.Clear();
        }

        public override void Tick()
        {
            for (int i = 0; i < _groups.Count; i++)
                _groups[i].Tick();
        }

        public IScreenGroup PushGroup()
        {
            if (!IsInitialized)
                throw new InvalidOperationException("ScreenController is not initialized.");
            return PushGroupInternal();
        }

        public NavigateToRequest<IScreen> CreateNavigateToRequest(IScreen screen)
        {
            if (screen == null) throw new ArgumentNullException(nameof(screen));
            ScreenGroup group = ActiveOrBase();
            GuardOccupancy(screen.GetType(), group);
            return group.CreateNavigateToRequest(screen);
        }

        public NavigateToRequest<IScreen> CreateNavigateToRequest<TTarget>() where TTarget : class, IScreen
        {
            ScreenGroup group = ActiveOrBase();
            GuardOccupancy(typeof(TTarget), group);
            return group.CreateNavigateToRequest<TTarget>();
        }

        public NavigateToRequest<IScreen> CreateNavigateToRequest(string identifier)
        {
            ScreenGroup group = ActiveOrBase();
            IScreen target = Registry.Get(identifier);
            GuardOccupancy(target.GetType(), group);
            return group.CreateNavigateToRequest(target);
        }

        public NavigateToResponse<IScreen> Return(CancellationToken cancellationToken = default)
        {
            ScreenGroup active = _groups.Count > 0 ? _groups[^1] : null;
            if (active == null)
                return FailureResponse();

            // Back within the active group while it still has history...
            if (active.PreviousScreen != null)
                return active.Return(cancellationToken);

            // ...otherwise the group is at its root: collapse it (Exited -> pop + resume below), unless it is
            // the base group, which has nowhere to return to.
            if (_groups.Count > 1)
                return active.Exit(new ExitRequest { CancellationToken = cancellationToken });

            return FailureResponse();
        }

        public NavigateToResponse<IScreen> Exit(in ExitRequest request)
        {
            ScreenGroup active = _groups.Count > 0 ? _groups[^1] : null;
            if (active == null)
                return FailureResponse();
            // The group's Exited event drives the pop + resume-below in OnGroupExited.
            return active.Exit(in request);
        }

        // Activation hooks for subclasses (e.g. MenuController's backdrop): fired as the controller gains its
        // first active group and loses its last.
        protected virtual void OnEntering() { }
        protected virtual void OnExited() { }

        private ScreenGroup ActiveOrBase()
        {
            if (!IsInitialized)
                throw new InvalidOperationException("ScreenController is not initialized.");
            return _groups.Count > 0 ? _groups[^1] : PushGroupInternal();
        }

        private ScreenGroup PushGroupInternal()
        {
            ScreenGroup group = _pool.Count > 0 ? _pool.Pop() : new ScreenGroup(Registry, TimeMode);
            // Re-subscribe each push: a pooled group cleared these in Reset() when it was collapsed.
            group.Entering += () => OnGroupEntering(group);
            group.Exited += () => OnGroupExited(group);

            if (_groups.Count > 0)
                _groups[^1].SetInteractable(false);   // pause the layer below

            _groups.Add(group);
            RecomputeBands();
            return group;
        }

        private void OnGroupEntering(ScreenGroup group)
        {
            // The first group to enter activates the controller (e.g. shows a menu backdrop).
            if (_groups.Count == 1 && ReferenceEquals(_groups[0], group))
                OnEntering();
        }

        private void OnGroupExited(ScreenGroup group)
        {
            int index = _groups.IndexOf(group);
            if (index < 0)
                return;   // guard: already popped / re-entrant exit

            _groups.RemoveAt(index);
            // Reset runs from within the group's Exited callback (fired as its last screen finished hiding).
            // By then the hide transition has settled, so this releases held screens, history and the pooled
            // primitives cleanly. This synchronous reset-on-collapse is the framework's highest-risk path —
            // validate in play mode (transition cleanup, stale event subs) before relying on it.
            group.Reset();
            _pool.Push(group);
            RecomputeBands();

            if (_groups.Count > 0)
                _groups[^1].SetInteractable(true);   // resume the layer below
            else
                OnExited();   // last group gone -> controller inactive
        }

        private void RecomputeBands()
        {
            for (int i = 0; i < _groups.Count; i++)
                _groups[i].SetLayerOrder(i * _LayerBandStep);
        }

        private void GuardOccupancy(Type screenType, ScreenGroup target)
        {
            // A screen lives in at most one group at a time; opening one already held elsewhere is disallowed.
            for (int i = 0; i < _groups.Count; i++)
            {
                ScreenGroup group = _groups[i];
                if (!ReferenceEquals(group, target) && group.Holds(screenType))
                    throw new InvalidOperationException(
                        $"Screen '{screenType.Name}' is already held by another group.");
            }
        }

        // A completed, no-effect response (a null completion task resolves to an already-completed awaitable).
        private static NavigateToResponse<IScreen> FailureResponse() =>
            new(new NavigateToResult<IScreen>(false, null, null), null);
    }
}
