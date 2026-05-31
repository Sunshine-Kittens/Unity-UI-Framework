---
name: project-controller-redesign
description: Converged design for the Controller / ScreenGroup / layering rework — decisions locked 2026-05-29. Full plan in the plan file.
metadata: 
  node_type: memory
  type: project
  originSessionId: 1b320fb0-e415-4df8-9065-db75ec4a9105
---

Redesign of the controller/group/layering system. Full plan: `~/.claude/plans/lazy-discovering-liskov.md` (v3).

**Progress (2026-05-30, branch `widget-refactor`):** Phases A–D done (written blind, NO compile yet). A: `History.Clear()` releases entries, `WindowNavigator.Reset()`, `TransitionManager.Reset()`. B: new `Controller<TWindow>`/`IController`, `TabController : Controller<TWindow>`. C: `IScreenGroup` consolidated (no Initialize/Terminate/State, `IsInteractable` is `IReadOnlyScalarFlag`, no `IsEnabled`, folds in `Tick`); sealed `ScreenGroup` written (`Runtime/Groups/ScreenGroup.cs`). D: `ScreenController : Controller<IScreen>` rewritten (group stack + pool + overlay bands + bubble-return + Exited-collapse + occupancy via `Holds`); `MenuController` hooks renamed `OnEnter/OnExit`→`OnEntering/OnExited`; `IScreenController : IController, IScreenNavigator` (+`Groups`/`ActiveGroup`/`PushGroup`); deleted `ScreenGroupController`(+I), `ScreenGroupBase`(+I) and their metas. **Phase E partial:** `ScreenControllerHost.cs` written (serialized `ScreenCollector[]` + `TimeMode`, builds/Initializes controller on enable, Terminates on disable, ticks via `UpdateManager`, exposes `Controller`/`NavigateTo`/`Return`/`Exit`/`PushGroup`). **Still pending in E: NUnit tests + samples rewrite** (deferred until a Unity compile pass — both need the compiler/editor to be worth writing; tests must run, samples are `Samples~` (uncompiled) targeting the dead `Controller` MB API + scene wiring).

**Enable/disable re-init gap (found 2026-05-30):** `WidgetRegistry.Register` only inits screens with `State==Uninitialized` (WidgetRegistry.cs:110); `Terminate` leaves them `Terminated`. So a rebuilt controller on a 2nd enable skips re-initializing previously-terminated screens. Runtime-level fix needed (reset screens to Uninitialized on Terminate, or support re-init) before repeated enable/disable works. Documented in `ScreenControllerHost` header.

**Phase D decisions (2026-05-30):**
- Overlay open is an explicit `PushGroup()` (returns the new top `IScreenGroup`) — NOT a `NavigateToRequest` builder flag (the builder has no group notion). Controller's `IScreenNavigator` delegates to the top group; first `CreateNavigateToRequest` lazily pushes a base group.
- `Return`: back within active group while `PreviousScreen != null`; at root, collapse via `Exit` (unless base group); collapse pop + resume-below happens in the `Exited` handler.
- **`ScreenController.OnGroupExited` calls `group.Reset()` synchronously from within the group's `Exited` callback** (fires as last screen finishes hiding). This is the plan's flagged HIGHEST-RISK path (transition-manager reset mid-completion, stale event subs) — MUST validate in play mode. `_active` is still non-null at hide-complete so `TransitionManager.Reset` cancels it and the transition's own `finally` releases the entry (no double-release).
- Bands: `SetLayerOrder(i * 100)` per stack index. **Pinned groups deferred** — plan listed them but no sample needs them and they add band-ordering complexity; revisit with a concrete use case.
- `Controller.cs`/`IController.cs` still lack `.meta` (Unity will generate; not hand-writing GUIDs).

**Phase C decisions (2026-05-30, confirmed w/ user — supersede plan prose written under per-group registries):**
- One **shared** controller-owned registry → group tracks its own held set (`HashSet<IScreen> _held`); SetOpacity/Tick/SetLayerOrder/interactable scope to `_held`, never `_registry.Widgets`. Screen joins `_held` on first NavigateTo (`Acquire`: SetNavigator+subscribe events+apply opacity/band/interactable), leaves on `Reset`.
- **Ctor-inject** the shared registry, **dropped `Bind`** — only `Reset()` for pooled reuse (navigator/transition/history reset, release held, null external event subs).
- `SetLayerOrder` → `SetGlobalSortOrder` (render band).
- Controller-facing (NOT on `IScreenGroup`): `SetLayerOrder`, `SetInteractable`, `Reset`, `Holds(Type)`.
- **Deferred to Phase D:** delete `ScreenGroupBase.cs` + `IScreenGroupBase.cs` (still used by old `ScreenController`/`MenuController`, kept compiling until D rewrites them). `ScreenGroupController` stub still compiles against new `IScreenGroup`.

**Why:** the controller layer never re-cohered after the pure-C# migration (`ScreenGroupController` was all
`NotImplementedException`); nothing layered groups; drop-in DX was lost. Goal: coherent, layered, low-boilerplate.

**Locked decisions (do not relitigate or rename):**
- **Vocabulary fixed:** `WidgetRegistry` stays "registry" (never "catalog"); `ScreenGroup` stays that name (never
  "ScreenLayer"/"context"). "Layer" is NOT a type — it's the controller's arrangement of groups (stack + render band).
- **No shared "context" abstraction.** Tab and screen controllers share only an abstract `Controller<TWindow>`
  (owns `WidgetRegistry<TWindow>` + collectors + lifecycle + Tick) and reuse existing primitives
  (`WindowNavigator`, `TransitionManager`, `History`, coordinators). `History` lives ONLY on `ScreenGroup`.
- **Collectors are controller-level + editor-time** (define the registry of available widgets), NOT a group input.
- **`ScreenGroup` is sealed + runtime + pooled by the controller**; owns navigator/history/transitions over the
  controller's INJECTED shared registry; `Bind(registry)`/`Reset()`; `Exited` event; implements `IScreenNavigator`
  (a screen's `Navigator` back-ref IS its `ScreenGroup`).
- **`ScreenController : Controller<IScreen>`** owns the group pool + layering (stack + pinned), overlay (lower group
  stays visible, input-paused), bubble-return (back within group, then pop group when its history empties),
  screen-driven collapse via the group's `Exited`, and **occupancy = disallow** (a window lives in one group at a
  time; registry is Type-keyed). `MenuController : ScreenController` (backdrop). `TabController<TWindow>` has no history.
- **Roles:** Widget = element; Window = controller-managed unit (member-light marker, meaning = the `Controller<TWindow>`
  constraint); Screen = a window that self-drives (holds its `ScreenGroup` as `Navigator`).
- **Builder stays the sole nav parameterization** (`CreateNavigateToRequest(...).Execute()`); no per-permutation overloads.

**How to apply:** implement against the plan file; this is a breaking change (version bump + changelog). All C# is
written without a local Unity compiler — needs a compile pass in-editor. See also [[project-review-2026-05]] for
the REVIEW.md items (e.g. #23 History.Clear pooling, folded into Phase A).
