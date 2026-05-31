using System;
using System.Collections.Generic;
using System.Threading;

using UIFramework.Core;
using UIFramework.Core.Interfaces;
using UIFramework.Coordinators;
using UIFramework.Navigation;
using UIFramework.Navigation.History;
using UIFramework.Registry;
using UIFramework.Transitioning;

using UnityEngine;
using UnityEngine.Extension;

namespace UIFramework.Groups
{
    // A group of screens with its own navigation context (navigator + history + transitions), composed over
    // the controller's shared screen registry. Sealed and pooled: the owning ScreenController constructs it
    // once over the shared registry, reuses it across push/pop, and Resets it between uses.
    //
    // The registry is shared by every group the controller owns, so the group never iterates the whole
    // registry. It tracks the subset it currently holds (_held) — the screens it has navigated to this
    // session — and scopes all per-screen state (visibility ticking, opacity, layer band, interactability,
    // navigator back-reference, event subscriptions) to that subset. A screen joins _held the first time the
    // group navigates to it and leaves on Reset; while held, it belongs to this group alone (occupancy is the
    // controller's to enforce via Holds).
    public sealed class ScreenGroup : IScreenGroup
    {
        public bool IsVisible => _presentationState > 0 && _presentationState < GroupPresentationState.Exited && _opacity > 0f;

        public float Opacity => _opacity;
        private float _opacity = 1f;

        public IScreen ActiveScreen => _navigator.ActiveInstance;

        public IScreen PreviousScreen
        {
            get
            {
                if (_history.TryPeek(out HistoryEventView events) && events.TryGetEvent(out NavigationHistoryEvent navEvent))
                {
                    _registry.TryGet(navEvent.WindowType, out IScreen screen);
                    return screen;
                }
                return null;
            }
        }

        // Read-only to consumers; the owning controller drives it via SetInteractable to pause lower layers.
        public IReadOnlyScalarFlag IsInteractable => _isInteractable;
        private readonly ScalarFlag _isInteractable = new(true);

        public TimeMode TimeMode { get; }

        public event Action Entering;
        public event Action Entered;
        public event Action Exiting;
        public event Action Exited;

        public event ScreenAction ScreenShowing;
        public event ScreenAction ScreenShown;
        public event ScreenAction ScreenHiding;
        public event ScreenAction ScreenHidden;

        private readonly WidgetRegistry<IScreen> _registry;
        private readonly WindowNavigator<IScreen> _navigator;
        private readonly TransitionManager _transitionManager;
        private readonly History _history;
        private readonly NavigateToCoordinator<IScreen> _navigateToCoordinator;
        private readonly ReturnCoordinator<IScreen> _returnCoordinator;
        private readonly ExitCoordinator<IScreen> _exitCoordinator;

        private readonly HashSet<IScreen> _held = new();

        private int _layerOrder;
        private GroupPresentationState _presentationState;

        private float DeltaTime => TimeMode == TimeMode.Unscaled ? Time.unscaledDeltaTime : Time.deltaTime;

        // The shared registry is already initialized by the controller; the group only composes navigation
        // over it. Per-screen wiring (navigator back-ref, event subscriptions, group state) happens on join,
        // not here, since registry init is controller-owned and spans every group.
        public ScreenGroup(WidgetRegistry<IScreen> registry, TimeMode timeMode)
        {
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            TimeMode = timeMode;

            _navigator = new WindowNavigator<IScreen>(_registry);
            _transitionManager = new TransitionManager(timeMode);
            _history = new History(_registry.Widgets.Count);
            _navigateToCoordinator = new NavigateToCoordinator<IScreen>(timeMode, _navigator, _history, _transitionManager);
            _returnCoordinator = new ReturnCoordinator<IScreen>(_navigator, _registry, _history, _transitionManager);
            _exitCoordinator = new ExitCoordinator<IScreen>(timeMode, _navigator, _transitionManager);

            _navigator.OnNavigationUpdate += NavigationUpdate;
            _isInteractable.OnUpdate += OnIsInteractableUpdated;
        }

        public NavigateToRequest<IScreen> CreateNavigateToRequest(IScreen screen)
        {
            return new NavigateToRequest<IScreen>(_navigator, _navigateToCoordinator, _navigator.ActiveInstance, screen);
        }

        public NavigateToRequest<IScreen> CreateNavigateToRequest<TTarget>() where TTarget : class, IScreen
        {
            return new NavigateToRequest<IScreen>(_navigator, _navigateToCoordinator, _navigator.ActiveInstance, _registry.Get<TTarget>());
        }

        public NavigateToRequest<IScreen> CreateNavigateToRequest(string identifier)
        {
            return new NavigateToRequest<IScreen>(_navigator, _navigateToCoordinator, _navigator.ActiveInstance, _registry.Get(identifier));
        }

        public NavigateToResponse<IScreen> Return(CancellationToken cancellationToken = default)
        {
            return _returnCoordinator.Return(cancellationToken);
        }

        public NavigateToResponse<IScreen> Exit(in ExitRequest request)
        {
            return _exitCoordinator.Exit(in request);
        }

        public void SetOpacity(float opacity)
        {
            _opacity = opacity;
            foreach (IScreen screen in _held)
                screen.SetOpacity(opacity);
        }

        public void Tick()
        {
            if (!IsVisible)
                return;

            float deltaTime = DeltaTime;
            foreach (IScreen screen in _held)
            {
                if (screen.IsVisible)
                    screen.Tick(deltaTime);
            }
        }

        // --- Controller-facing surface (not on IScreenGroup; only the owning controller calls these) ---

        // Positions the whole group as a render band. GlobalSortOrder is the canvas/document band (uGUI
        // sibling index, UITK document sortingOrder), so every held screen shares the group's layer.
        public void SetLayerOrder(int layerOrder)
        {
            _layerOrder = layerOrder;
            foreach (IScreen screen in _held)
                screen.SetGlobalSortOrder(layerOrder);
        }

        public void SetInteractable(bool interactable)
        {
            _isInteractable.Value = interactable;
        }

        // True while the given screen type belongs to this group, so the controller can enforce occupancy
        // (a screen lives in one group at a time).
        public bool Holds(Type screenType)
        {
            foreach (IScreen screen in _held)
            {
                if (screen.GetType() == screenType)
                    return true;
            }
            return false;
        }

        // Returns the group to a clean state for pooled reuse. Assumes its transitions have already settled
        // (the group exited) — see TransitionManager.Reset. Releases every held screen and drops external
        // event subscribers so a reused instance carries no stale controller wiring.
        public void Reset()
        {
            foreach (IScreen screen in _held)
                ReleaseScreen(screen);
            _held.Clear();

            _navigator.Reset();
            _transitionManager.Reset();
            _history.Clear();

            _isInteractable.Reset(true);
            _opacity = 1f;
            _layerOrder = 0;
            _presentationState = default;

            Entering = null;
            Entered = null;
            Exiting = null;
            Exited = null;
            ScreenShowing = null;
            ScreenShown = null;
            ScreenHiding = null;
            ScreenHidden = null;
        }

        // A screen joins the group the first time it is navigated to: claim it as a member, apply the group's
        // current layer/opacity/interactability, and subscribe to its visibility events.
        private void Acquire(IScreen screen)
        {
            if (!_held.Add(screen))
                return;

            screen.SetNavigator(this);
            screen.Showing += OnScreenShowing;
            screen.Shown += OnScreenShown;
            screen.Hiding += OnScreenHiding;
            screen.Hidden += OnScreenHidden;

            screen.SetGlobalSortOrder(_layerOrder);
            screen.SetOpacity(_opacity);
            screen.IsInteractable.Value = _isInteractable.Value;
        }

        private void ReleaseScreen(IScreen screen)
        {
            screen.Showing -= OnScreenShowing;
            screen.Shown -= OnScreenShown;
            screen.Hiding -= OnScreenHiding;
            screen.Hidden -= OnScreenHidden;
            screen.ClearNavigator();
        }

        private void NavigationUpdate(NavigateToResult<IScreen> result)
        {
            if (result.Success)
            {
                // Claim the incoming screen before its show completes so the presentation state machine
                // (driven by the screen's Shown/Hidden below) sees a subscribed, group-owned screen.
                if (result.Active != null)
                    Acquire(result.Active);

                if (result.Previous == null && result.Active != null)
                {
                    _presentationState = GroupPresentationState.Entering;
                    Entering?.Invoke();
                }
                else if (result.Previous != null && result.Active == null)
                {
                    _presentationState = GroupPresentationState.Exiting;
                    Exiting?.Invoke();
                }
            }
        }

        private void OnScreenShowing(IWidget widget) => ScreenShowing?.Invoke(widget as IScreen);

        private void OnScreenShown(IWidget widget)
        {
            if (_presentationState == GroupPresentationState.Entering)
            {
                _presentationState = GroupPresentationState.Entered;
                Entered?.Invoke();
            }
            ScreenShown?.Invoke(widget as IScreen);
        }

        private void OnScreenHiding(IWidget widget) => ScreenHiding?.Invoke(widget as IScreen);

        private void OnScreenHidden(IWidget widget)
        {
            if (_presentationState == GroupPresentationState.Exiting)
            {
                _presentationState = GroupPresentationState.Exited;
                Exited?.Invoke();
            }
            ScreenHidden?.Invoke(widget as IScreen);
        }

        private void OnIsInteractableUpdated(bool value)
        {
            foreach (IScreen screen in _held)
                screen.IsInteractable.Value = value;
        }
    }
}
