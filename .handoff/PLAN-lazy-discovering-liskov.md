# Plan: Coherent Controller / ScreenGroup system (v3 — converged design)

## Context

`ScreenController` was migrated from a drop-in MonoBehaviour to a pure-C# stack (`ScreenGroupBase` +
coordinators + registry + history + `NavigateToRequest` builder). The controller/manager layer never
re-cohered (`ScreenGroupController` was all `NotImplementedException`), nothing layered groups, and drop-in
DX was lost. Design was re-derived with the user (this supersedes v1/v2).

Vocabulary is fixed — do NOT rename existing concepts:
- **`WidgetRegistry`** = the registry (not "catalog").
- **`ScreenGroup`** = a sealed class; the collection of screens with its own navigation context.
- **"Layer"** = NOT a type. It is the controller's arrangement of groups (the stack + each group's render
  band). A group is positioned as a layer by the controller; the group never names itself a layer.

## Converged architecture

| Concept | Owner / shape | Notes |
|---|---|---|
| `WidgetRegistry<TWindow>` | owned by `Controller<TWindow>` | shared; built from controller-level collectors; widgets init/terminate **once** |
| `Controller<TWindow>` (abstract) | base | registry + collectors + lifecycle + `Tick`. The ONLY shared base between tab/screen. Nothing navigational beyond owning the registry. |
| `TabController<TWindow> : Controller<TWindow>` | sealed | composes `WindowNavigator` + `TransitionManager` directly; index-addressed; **no history, no groups** (its coordinator already takes `history: null` today) |
| `ScreenGroup` (sealed) | pooled by `ScreenController` | the group: `WindowNavigator` + `History` + `TransitionManager` + coordinators over the **injected shared registry**; implements `IScreenNavigator`; exposes `Exited`, `SetLayerOrder`, `Tick`; `Bind(registry)`/`Reset()` for pooling |
| `ScreenController : Controller<IScreen>` | — | owns the **group pool** + the **layering**: a stack of groups + pinned groups, bubble-return, occupancy, assigns each group its `LayerOrder` band |
| `MenuController : ScreenController` | — | backdrop animated on the active group's enter/exit |

Key properties:
- **No shared "context" abstraction.** Tabs and screens share only `Controller<TWindow>` (registry + lifecycle)
  and reuse the existing primitives (`WindowNavigator`, `TransitionManager`, `History`, coordinators), composed
  differently. `History` exists **only** on `ScreenGroup`; nothing tabs touch carries screen-only concepts.
- **Collectors are controller-level, editor-time** — they define the registry of available widgets. Not a group
  input. Groups draw their windows from the controller's shared registry.
- **Groups are runtime, pooled** by the controller (Get on push, Release on pop). Reset, not reallocated.
- **Layering = overlay**: a pushed group renders above (higher band); the one below stays visible, input-paused.
  Pinned groups sit at fixed bands. Per-group history preserved.
- **Return bubbles**: back within the active group; when its history is empty, collapse/pop the group.
- **Collapse-in-one-call**: any group can be closed from any phase via `Exit` (a screen inside it calls
  `Navigator.Exit(...)`); the controller observes the group's `Exited` event → pops + resumes below (guarded),
  and resets the group's history so a reused instance is clean.
- **Occupancy: disallow** — registry is `Type`-keyed and a widget is one GameObject, so a window lives in at
  most one group at a time; opening one already held by another group throws/no-ops.

## Roles (Widget / Window / Screen) — validated
- **Widget** = a composable element: lifecycle, visibility(+animation), sort orders, opacity, enabled/interactable.
- **Window** = a widget a `Controller<TWindow>` manages as a unit. Member-light marker; its meaning is the role
  (the `Controller<TWindow>` constraint), which gives the previously-empty `IWindow` real purpose.
- **Screen** = a window that self-drives navigation: its `Navigator` back-reference is its owning `ScreenGroup`.

## Builder / facade (locked)
`CreateNavigateToRequest(...).Execute()` stays the sole parameterization mechanism — no per-permutation
overloads. Optional 1:1 entry alias only; not required.

## Implementation phases

### Phase A — Reset hooks on existing primitives (additive, low risk)
- `WindowNavigator<TWindow>`: `Reset()` (active → none; re-point/clear `WidgetUnregistered` subscription; allow
  registry re-bind for pooled reuse).
- `TransitionManager`: `Reset()` (cancel/clear `_active` + `_pending`/`_pendingSet`; reset `_skipAll`).
- `History.Clear()`: release entries to the pool (also fixes REVIEW.md #23).

### Phase B — `Controller<TWindow>` base + `TabController` refactor
- Extract `Controller<TWindow>` (registry + collectors + lifecycle + `Tick`).
- `TabController<TWindow>` becomes `: Controller<TWindow>`, composing `WindowNavigator` + `TransitionManager`
  (no history). Behaviour-preserving.

### Phase C — `ScreenGroup` sealed, poolable, over injected registry
- Replace `ScreenGroupBase` (abstract) + `ScreenGroup`/`MenuController`-as-group with a **sealed `ScreenGroup`**
  that owns navigator/history/transitions/coordinators over an **injected** `WidgetRegistry<IScreen>`; no
  collector ctor; `Bind(registry)`/`Reset()`; clears event subscribers on release; `Exited` event;
  `SetLayerOrder` applies render band to its opened windows.

### Phase D — `ScreenController` owns registry + group pool + layering
- `ScreenController : Controller<IScreen>`: build registry from collectors; controller-owned `ScreenGroup` pool;
  push/pop + pinned groups; overlay band assignment; `IScreenNavigator` delegates to top group; `Return`
  bubbles; subscribe to group `Exited` for screen-driven collapse; occupancy disallow; `Tick` ticks groups.
- `MenuController : ScreenController` restored (backdrop on active group enter/exit).
- Delete the `ScreenGroupController`/`IScreenGroupController` stubs.

### Phase E — Thin host + samples + tests
- `ScreenControllerHost : MonoBehaviour, IUpdatable`: serialized controller-level collectors + TimeMode; builds
  the controller, `Initialize`/`Terminate` on enable/disable, ticks via `UpdateManager`, exposes navigation entry
  + controller reference. (No editor-time group config — groups are runtime.)
- Rewrite `Samples~/Example/Scripts/*` to the new API (single group; additive overlay group; bubble-return;
  modal global-close). Remove dead `Controller`/`OpenScreen`/`AccessAnimation` references.
- Add a `Tests/` edit-mode asmdef (none today) with NUnit tests for pure-C# `ScreenController`/`ScreenGroup`:
  push/pop + band order, occupancy disallow, return bubbling, screen-driven collapse via `Exited`, pool reuse
  resets state.

## Open detail
- `ScreenGroup` visibility: `internal` (pure impl detail) vs `public` (advanced consumers hold/inspect a group).
  Visibility call only, not structural. Lean `public` since a screen's `Navigator` exposes it.

## Risks
- Pooling reset correctness (stale event subs, navigator `WidgetUnregistered` re-bind, transition cleanup) —
  highest risk; cover with tests.
- Cross-group sort banding across canvases / UITK panels — validate visually.
- **Cannot compile here** — all C# written blind; needs a Unity compile pass.
- Breaking API — `ScreenController` reshaped, `Controller<TWindow>` introduced, `ScreenGroupController` removed.
  Version bump + changelog.

## Verification
- Edit-mode NUnit tests for the pure-C# controller/group logic (fakes for widgets/registry).
- Manual play-mode via rewritten sample: drop-in host; open; additive overlay (lower stays visible);
  bubble-return; modal global-close collapses a multi-phase group in one call; clean enable/disable cycles.
- Profile a push/return cycle to confirm pooling holds (no per-open churn of `History`/`TransitionManager`).
