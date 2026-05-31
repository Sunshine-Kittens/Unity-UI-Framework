# Controller / ScreenGroup Redesign — Handoff Context

> **Purpose:** self-contained context to resume this work in a fresh chat / different repo folder.
> The original plan file and Claude memories live under `~/.claude/` and do **not** travel with the
> package, so everything needed is reproduced here. Branch: **`widget-refactor`**.
> Status as of 2026-05-31: **Phases A–D + the Phase-E host are written but have NEVER been compiled.**
> The package is a git submodule of a parent repo that currently has unrelated dependency/compile
> errors, so none of this has been through a Unity compile pass yet.

---

## 1. Why this work exists

`com.lukeandrews.ui-framework` is a Unity 6.3 UI framework package (UITK + uGUI) providing widgets,
screens, windows, controllers, navigation, transitions. Performance constraint: minimise GC/heap
allocations (mobile/game hardware) — prefer value types, pools, avoid closures in hot paths.

`ScreenController` was migrated from a drop-in MonoBehaviour to a pure-C# stack (`ScreenGroupBase` +
coordinators + registry + history + `NavigateToRequest` builder). The controller/manager layer never
re-cohered: `ScreenGroupController` was all `NotImplementedException`, nothing layered groups, and the
drop-in DX was lost. This redesign re-coheres it into a coherent, layered, low-boilerplate system.

---

## 2. Converged architecture (locked — do NOT relitigate/rename)

| Concept | Owner / shape | Notes |
|---|---|---|
| `WidgetRegistry<TWindow>` | owned by `Controller<TWindow>` | **one shared** registry; built from controller-level collectors; widgets init/terminate **once** |
| `Controller<TWindow>` (abstract) | base | registry + collectors + lifecycle + `Tick`. The ONLY shared base between tab/screen. Nothing navigational beyond owning the registry. |
| `TabController<TWindow>` | sealed `: Controller<TWindow>` | composes `WindowNavigator` + `TransitionManager`; index-addressed; **no history, no groups** |
| `ScreenGroup` (sealed) | pooled by `ScreenController` | `WindowNavigator` + `History` + `TransitionManager` + coordinators over the **injected shared registry**; implements `IScreenGroup`/`IScreenNavigator`; `Exited` event; `Reset()` for pooling |
| `ScreenController` | `: Controller<IScreen>` | owns the **group pool** + **layering**: a stack of groups, overlay bands, bubble-return, occupancy, `Exited`-driven collapse |
| `MenuController` | `: ScreenController` | backdrop animated on the controller's first-group-enter / last-group-exit |
| `ScreenControllerHost` | `MonoBehaviour, IUpdatable` | drop-in: serialized collectors + TimeMode; builds/initializes the controller, ticks it |

**Fixed vocabulary:** `WidgetRegistry` = "registry" (never "catalog"). `ScreenGroup` = that name (never
"ScreenLayer"/"context"). **"Layer" is NOT a type** — it is the controller's arrangement of groups
(the stack + each group's render band). A group never names itself a layer.

**Key principles:**
- **No shared "context" abstraction.** Tabs and screens share only `Controller<TWindow>` (registry +
  lifecycle) and reuse existing primitives (`WindowNavigator`, `TransitionManager`, `History`,
  coordinators), composed differently. `History` exists **only** on `ScreenGroup`.
- **Collectors are controller-level + editor-time** — they define the registry of available widgets.
  NOT a group input. Groups draw their windows from the controller's shared registry.
- **Groups are runtime, pooled** by the controller (taken on push, Reset + returned on collapse).
- **Layering = overlay**: a pushed group renders above (higher GlobalSortOrder band); the one below
  stays visible, input-paused. Per-group history preserved.
- **Return bubbles**: back within the active group; when its history empties, collapse/pop the group.
- **Collapse-in-one-call**: any group closes from any phase via `Exit` (a screen calls
  `Navigator.Exit(...)`); the controller observes the group's `Exited` event → pops + resumes below.
- **Occupancy: disallow** — registry is `Type`-keyed; a window lives in at most one group at a time;
  opening one already held by another group throws.

**Roles:** Widget = composable element. Window = a widget a `Controller<TWindow>` manages as a unit
(member-light marker; meaning = the `Controller<TWindow>` constraint). Screen = a window that
self-drives navigation (its `Navigator` back-reference is its owning `ScreenGroup`).

**Builder is the sole nav parameterization:** `CreateNavigateToRequest(...).Execute()`. No
per-permutation overloads. **The builder has no group/overlay notion** — opening an overlay is an
explicit `ScreenController.PushGroup()` call, not a builder flag.

---

## 3. Status by phase

All C# was written **blind** (no local Unity compiler). Nothing is staged/committed.

### ✅ Phase A — Reset hooks on existing primitives (staged in git)
- `Runtime/Core/History.cs` — `Clear()` now `Release()`s each entry to the pool before clearing
  (also fixes REVIEW.md #23).
- `Runtime/Navigation/WindowNavigator.cs` — added `Reset()`: silent active-state clear + `Version++`
  (invalidates outstanding requests; no navigation-update fired). Registry stays bound (shared, never
  re-bound), so `Reset()` does not touch the `WidgetUnregistered` subscription.
- `Runtime/Transitioning/TransitionManager.cs` — added `Reset()`: clears `_skipAll`, cancels
  `_active` + `_pending`, clears `_pending`/`_pendingSet`. In-flight `Transition` awaitables are
  cancelled and released by their **own finally blocks** (Reset does not release them → no
  double-release).

### ✅ Phase B — `Controller<TWindow>` base + `TabController` refactor
- New `Runtime/Controllers/Controller.cs` — abstract `Controller<TWindow>` (registry + collectors +
  lifecycle + `Tick`; `OnWidgetInitialize/Terminate`, `OnInitialize/OnTerminate` virtuals).
- New `Runtime/Controllers/Interfaces/IController.cs` — `IsInitialized`, `Initialize`, `Terminate`,
  `Tick` (deliberately non-generic).
- `Runtime/Controllers/TabController.cs` — now `: Controller<TWindow>`, composing
  `WindowNavigator` + `TransitionManager` (history = null).
- `Runtime/Controllers/Interfaces/ITabController.cs` — now `: IController, ...` (dropped its own
  `IsInitialized`/`Initialize`/`Terminate`).

### ✅ Phase C — sealed `ScreenGroup` over injected shared registry
- `Runtime/Groups/IScreenGroup.cs` — **consolidated** the former `IScreenGroup` + `IScreenGroupBase`
  into one interface `: IScreenNavigator`. No `Initialize/Terminate`, no `State`; `IsInteractable` is
  now `IReadOnlyScalarFlag` (read-only to consumers); dropped `IsEnabled`; folds in `Tick`.
- `Runtime/Groups/ScreenGroup.cs` — **rewritten** as `sealed class ScreenGroup : IScreenGroup`.
  - **Ctor-injects the shared registry** (`new ScreenGroup(registry, timeMode)`); navigator /
    transitions / history / coordinators are `readonly`, wired once in the ctor. **No `Bind`** — only
    `Reset()` for pooled reuse (registry is constant for the controller's life).
  - **Tracks its own held set** `HashSet<IScreen> _held`. A screen joins on its first NavigateTo
    (`Acquire`: `SetNavigator(this)` + subscribe Showing/Shown/Hiding/Hidden + apply current
    opacity/band/interactable) and leaves on `Reset`. `SetOpacity`/`Tick`/`SetLayerOrder` and
    interactable-propagation iterate **only `_held`**, never `_registry.Widgets` — so groups sharing
    the registry never stomp each other's screens.
  - Controller-facing surface (NOT on `IScreenGroup`): `SetLayerOrder(int)` →
    `SetGlobalSortOrder` (render band), `SetInteractable(bool)`, `Holds(Type)` (occupancy query),
    `Reset()` (navigator/transition/history reset, release held screens, **null all external event
    subscribers** so a reused instance carries no stale controller wiring).
- Deleted `Runtime/Groups/ScreenGroupBase.cs` + `IScreenGroupBase.cs` (in Phase D).

### ✅ Phase D — `ScreenController` owns registry + group pool + layering
- `Runtime/Controllers/ScreenController.cs` — `: Controller<IScreen>, IScreenController`.
  - `OnInitialize` → `Registry.Collect(_collectors)` (base already did `Registry.Initialize()`).
  - `OnWidgetInitialize` → `SetVisibility(Hidden)` (screens start hidden; group does the rest on join).
  - Group **stack** `List<ScreenGroup> _groups` (top = active) + **pool** `Stack<ScreenGroup> _pool`.
  - `IScreenNavigator` delegates to the active/top group; first `CreateNavigateToRequest` lazily
    pushes a **base group** (`ActiveOrBase`).
  - `PushGroup()` → take from pool or `new`, re-subscribe `Entering`/`Exited`, pause the layer below
    (`SetInteractable(false)`), push, `RecomputeBands()`.
  - `Return`: back within active group while `PreviousScreen != null`; else at root → collapse via
    `Exit` (unless it is the base group). `Exit` delegates to the active group.
  - `OnGroupExited` (the collapse handler): pop the group, **`group.Reset()` synchronously**, push to
    pool, recompute bands, resume the layer below (`SetInteractable(true)`) or fire `OnExited` when the
    stack empties. Guarded by `IndexOf < 0`.
  - `GuardOccupancy(Type, target)` throws if any group other than `target` `Holds` the type.
  - Bands: `SetLayerOrder(i * _LayerBandStep)`, `_LayerBandStep = 100`.
  - `FailureResponse()` returns a completed no-op `NavigateToResponse` (a `default` would NPE in
    `GetAwaiter` because its completion task would be null).
  - Subclass hooks: `protected virtual OnEntering()` (first group enters) / `OnExited()` (last exits).
  - *(User-applied tweaks on this file: `_LayerBandStep` naming + `FailureResponse()` name — keep.)*
- `Runtime/Controllers/MenuController.cs` — hooks renamed `OnEnter/OnExit` → `OnEntering/OnExited`
  (now controller-activation hooks). Backdrop animate-on-enter / animate-on-exit logic unchanged.
- `Runtime/Controllers/Interfaces/IScreenController.cs` — now `: IController, IScreenNavigator`
  with `Groups`, `ActiveGroup`, `PushGroup()`.
- **Deleted:** `ScreenGroupController.cs`(+meta), `Interfaces/IScreenGroupController.cs`(+meta),
  `Groups/ScreenGroupBase.cs`(+meta), `Groups/IScreenGroupBase.cs`(+meta). No dangling refs remain.

### 🔄 Phase E — host done; tests + samples pending
- ✅ `Runtime/Controllers/ScreenControllerHost.cs` — `MonoBehaviour, IUpdatable`. Serialized
  `ScreenCollector[]` + `TimeMode`; `OnEnable` builds + `Initialize`s the controller +
  `UpdateManager.AddUpdatable`; `OnDisable` removes + `Terminate`s; `ManagedUpdate` → `Tick`.
  Exposes `Controller`, `ActiveGroup`, `NavigateTo<T>()`/`NavigateTo(id)`/`Return`/`Exit`/`PushGroup`.
  `virtual CreateController()` lets a subclass host a `MenuController`.
- ⬜ **NUnit tests** — deferred. The `Tests/` edit-mode asmdef doesn't exist yet. Their value is in
  *running* (catch the pooling/reset bugs); `IScreen` is a large surface to fake. Write **after** a
  compile pass so they can be executed/iterated. Target cases: push/pop + band order, occupancy
  disallow, return bubbling, screen-driven collapse via `Exited`, pool reuse resets state.
- ⬜ **Samples rewrite** — deferred. `Samples~/Example/Scripts/*` target the long-dead `Controller`
  MonoBehaviour API (`OpenScreen`, `CloseAll`, `AccessAnimationParams`, `CreateAccessPlayable`, …) and
  `Samples~/` is **not compiled** by Unity. Needs the example scene/prefab wiring; do after compile.
  Target scenarios: single group; additive overlay group; bubble-return; modal global-close.

---

## 4. Open risks & known gaps (READ before relying on this)

1. **Highest risk — synchronous `Reset()` inside `Exited`.** `ScreenController.OnGroupExited` calls
   `group.Reset()` from within the group's `Exited` event, which fires from `OnScreenHidden` during
   transition completion. At hide-complete `TransitionManager._active` is still non-null, so
   `TransitionManager.Reset()` cancels it and the transition's own `finally` releases the entry (no
   double-release — by design). **This re-entrancy MUST be validated in play mode** (transition
   cleanup, stale event subs, no corruption of in-progress iteration). If it proves unsafe, defer the
   reset (e.g. queue collapsed groups and Reset them on the next `Tick`), but note that deferral risks
   leaving stale per-screen event subscriptions on screens reopened in another group before reset.

2. **Enable/disable re-init gap.** `WidgetRegistry.Register` only initializes a screen when
   `widget.State == Uninitialized` (`WidgetRegistry.cs` ~line 110); `Terminate` leaves screens
   `Terminated`. So a controller rebuilt on a **second** enable would skip re-initializing
   previously-terminated screens. Clean enable→disable→enable cycles need a Runtime fix (reset screens
   to `Uninitialized` on `Terminate`, or support re-init). Documented in the `ScreenControllerHost`
   header. Decide and fix during/after the compile pass.

3. **Missing `.meta` files.** New files `Controller.cs`, `IController.cs`, `ScreenControllerHost.cs`
   have no `.meta` yet. Unity generates them on import — do NOT hand-write GUIDs (collision risk).
   Let Unity create them, then commit.

4. **Pinned groups deferred.** The plan listed "pinned groups at fixed bands" but no sample needs them
   and they add band-ordering complexity. `PushGroup()` currently has no `pinned` flag. Revisit with a
   concrete use case.

5. **Breaking API + version bump.** `ScreenController` reshaped, `Controller<TWindow>` introduced,
   `ScreenGroupController` removed, `IScreenGroup` consolidated. Needs a package version bump +
   changelog entry before release.

---

## 5. Verification checklist (once the package compiles)

- [ ] Package compiles in Unity 6.3 (resolve the parent-repo dependency errors first).
- [ ] Fix any A–D compile errors surfaced (all written blind).
- [ ] Edit-mode NUnit tests for the pure-C# controller/group logic (fakes for screens/registry):
      push/pop + band order, occupancy disallow, return bubbling, `Exited` collapse, pool-reuse reset.
- [ ] Play-mode via rewritten sample: drop-in host; open; additive overlay (lower stays visible,
      input-paused); bubble-return; modal global-close collapses a multi-phase group in one call;
      clean enable/disable cycles (see gap #2).
- [ ] Profile a push/return cycle: confirm pooling holds (no per-open churn of
      `History`/`TransitionManager`); watch for closure churn in `PushGroupInternal` re-subscription.
- [ ] Validate cross-group sort banding across canvases / UITK panels visually.

---

## 6. Quick reference — touched files

**Modified:** `Runtime/Core/History.cs`, `Runtime/Navigation/WindowNavigator.cs`,
`Runtime/Transitioning/TransitionManager.cs`, `Runtime/Controllers/TabController.cs`,
`Runtime/Controllers/Interfaces/ITabController.cs`, `Runtime/Controllers/ScreenController.cs`,
`Runtime/Controllers/MenuController.cs`, `Runtime/Controllers/Interfaces/IScreenController.cs`,
`Runtime/Groups/IScreenGroup.cs`, `Runtime/Groups/ScreenGroup.cs`.

**Added:** `Runtime/Controllers/Controller.cs`, `Runtime/Controllers/Interfaces/IController.cs`,
`Runtime/Controllers/ScreenControllerHost.cs`.

**Deleted:** `Runtime/Controllers/ScreenGroupController.cs`(+meta),
`Runtime/Controllers/Interfaces/IScreenGroupController.cs`(+meta),
`Runtime/Groups/ScreenGroupBase.cs`(+meta), `Runtime/Groups/IScreenGroupBase.cs`(+meta).

**Related:** `REVIEW.md` (package root) — earlier full code review; item #23 (History.Clear pooling)
folded into Phase A.

**Archived source material** (copied into the repo so they travel with the package; `.handoff/` is a
hidden dir Unity ignores):
- `.handoff/PLAN-lazy-discovering-liskov.md` — the original verbatim v3 implementation plan.
- `.handoff/MEMORY-controller-redesign.md` — the Claude memory note with the running progress log and
  the locked Phase C/D decisions.
