# UI Framework Code Review — 2026-05-22
# Updated 2026-05-24 — session progress tracked below

Analysis of the UI framework plugin for Unity 6.3. Covers both UITK and uGUI paths.
ScreenGroupController is WIP and was excluded from this review.

Legend: ✅ Fixed | ⚠️ Deferred | 🔲 Pending

---

## Confirmed Bugs

### 1. ✅ `WidgetRegistry.TryGet<TWidgetType>` looks up the wrong type
**File:** `Runtime/Registry/WidgetRegistry.cs:172,183`

Both generic overloads passed `typeof(TWidget)` instead of `typeof(TWidgetType)`. Fixed — both now use `typeof(TWidgetType)`.

---

### 2. ✅ `TransitionManager.CreateSourcePlayable` animates the wrong widget
**File:** `Runtime/Transitioning/TransitionManager.cs:64`

`CreateSourcePlayable` called `CreatePlayable(Target, ...)` — changed to `Source`.

---

### 3. ⚠️ `ImagePlusPlus.GetAdjustedRadii` returns unclamped radii in the editor
**File:** `Runtime/uGUI/ImagePlusPlus/ImagePlusPlus.cs:333-335`

User fixing manually. `return radii` → `return temp` on line 335.

---

### 4. ✅ `WidgetBase.Terminate` double-unsubscribes
**File:** `Runtime/Core/WidgetBase.cs:164-165, 175-177`

Duplicate `OnUpdate -=` calls after `Reset()` removed. Unsubscription before `Reset()` is intentional (prevents handler firing during reset).

---

## GC / Performance

### 5. ✅ `CancellationTokenSource` allocated per animation
`WidgetBase.AnimateVisibility()` and `TransitionManager.Transition()` now guard with `cancellationToken.CanBeCanceled` — skip `CreateLinkedTokenSource` for the common `CancellationToken.None` case.

---

### 6. ✅ `UIExtensions.IterateHierarchy` allocates a `Stack<VisualElement>` per call
**File:** `Runtime/ExtentionMethods/UIExtensions.cs`

Now uses a `[ThreadStatic]` stack cleared in a `try/finally`. Zero per-call allocation.

---

### 7. ✅ `TransitionManager._pending` uses `List.Contains` for membership checks
**File:** `Runtime/Transitioning/TransitionManager.cs`

Added `HashSet<Entry> _pendingSet` kept in sync at every mutation site. `IsPending` now O(1).

---

### 8. ✅ `VisibilityAnimationHandle` allocates two `AwaitableCompletionSource` unconditionally
**File:** `Runtime/Core/WidgetBase.cs`

`_completedCompletionSource` is now lazily allocated (only created when `CompletedAwaitable` is accessed — Queue and SkipAnimation paths). All signal sites use null-conditional.

---

### 9. ✅ `VisibilityAnimationHandle` pooled
**File:** `Runtime/Core/WidgetBase.cs`

`VisibilityAnimationHandle` is now pooled via a static `Stack<VisibilityAnimationHandle>`. Uses `Get()`/`Release()` factory pattern with `Initialize()` resetting state and both completion sources. `_animationHandle` is nulled before `Release()` so `IsAnimating` is immediately false. `OnAnimationComplete` is unsubscribed before player release on both cancel and complete paths. `CancellationTokenRegistration` stored and disposed in `Release()`.

---

### 10. ✅ `TransitionManager.Entry` pooled
**File:** `Runtime/Transitioning/TransitionManager.cs`

`Entry` is now pooled via a static `Stack<Entry>`. Uses `Get()`/`Release()`. `readonly` fields replaced with `{ get; private set; }`. Released in `Transition.finally` (after CTS dispose) and in `RewindActive.finally`. Also fixed: `RewindActive` previously leaked its `CancellationTokenSource` — now disposed in the new `finally` block.

---

## Design Issues

### 11. ✅ `NavigateToRequest.ResolveTransition` can NPE on first navigation
**File:** `Runtime/Navigation/NavigateToRequest.cs:228`

Changed `_sourceWindow.GetDefaultAnimation(...)` to `_sourceWindow?.GetDefaultAnimation(...)`.

---

### 12. ✅ `WidgetBase.RewindAnimation` uses a brittle XOR toggle
**File:** `Runtime/Core/WidgetBase.cs:351`

Replaced `Visibility ^ (WidgetVisibility)1` with an explicit conditional.

---

### 13. ✅ `IsValidData` returns `false` by default — silent no-op
**File:** `Runtime/Core/WidgetBase.cs:377` and `Runtime/Coordinators/NavigateToCoordinator.cs`

`IsValidData` default stays `false` (opt-in). Added a guard in `NavigateToCoordinator.Execute` that throws `ArgumentException` if data is non-null but `IsValidData` returns false. This surfaces the contract violation immediately rather than silently discarding data.

---

### 14. ⚠️ `ModalWidget` / `IModalWidget` are empty stubs
Deferred — intended contract not yet decided. Return to this once the modal dialogue feature is scoped.

---

### 15. 🔲 `ScreenGroupBase` initialization order is non-obvious
**File:** `Runtime/Groups/ScreenGroupBase.cs:119-120`

`_registry.Initialize()` before `_registry.Collect()` is correct but surprising. Needs a comment explaining the lazy-init flow.

---

## New Findings — GC Audit (2026-05-24)

### 16. ✅ `Widget.SetInteractable` — closure allocation per call
**File:** `Runtime/UIToolkit/Widget.cs`

`SetInteractableChild` local function removed. Added `IterateHierarchy<TState>` generic overload to `UIExtensions.cs`; call site updated to a static lambda with no capture:
```csharp
visualElement.IterateHierarchy(static (child, mode) => SetInteractablePickingMode(child, mode), pickingMode);
```

---

### 17. ⚠️ `Widget.SetInteractablePickingMode` — `GetClasses()` enumerator boxing per child
**File:** `Runtime/UIToolkit/Widget.cs:245`

`classList` is not publicly accessible in Unity 6.3 UITK — `GetClasses()` is the only available API and returns `IEnumerable<string>`, so enumerator boxing on each child visit is unavoidable. Deferred until UITK exposes a non-boxing path.

---

### 18. ✅ `History.PushNewEntry` — `new HistoryEntry` + inner `new List<IHistoryEvent>` per navigation
**File:** `Runtime/Core/History.cs`

Introduced `PooledHistoryEntry<TSelf>` public abstract base class (CRTP). Static pool field is per closed type — each `TSelf` gets its own `Stack<TSelf>` automatically. `HistoryEntry` is now a trivial one-liner inheriting from it. `IHistoryStack.Pop()` narrowed to `IReadOnlyHistoryEntry` (correctness win; `ReturnCoordinator` only uses read-only members). Pool releases on `CancelEntry` (void — no external ref); `Pop` path is not pooled (caller holds ref). Inner `_events` list lives in the base class, cleared on `Release()` and reused across pool cycles.

---

### 19. 🔲 `ButtonGraphicComponent.TriggerAnimation` — `GetComponent<Animator>()` uncached
**File:** `Runtime/uGUI/ButtonPlusPlus/ButtonGraphicComponent.cs:194`

`GetComponent<Animator>()` called on every animation state transition. Should be cached in `Awake`.

---

### 20. 🔲 `ButtonPlusPlus` / `ButtonGraphicComponent` — lazy `_stateMap` and `_buttonComponents`
**Files:** `Runtime/uGUI/ButtonPlusPlus/ButtonPlusPlus.cs:117-134`, `Runtime/uGUI/ButtonPlusPlus/ButtonGraphicComponent.cs:217-228`

Both build their state dictionaries and component arrays lazily on first state change rather than in `Awake`. In play mode this causes a first-press allocation spike. Move initialisation to `Awake` (keep null-check fallbacks for the editor `OnValidate` path).

---

## New Findings — Full Review (2026-05-28)

### 21. 🔲 `AnimateVisibility` cancel path throws `InvalidOperationException` and leaks the pooled handle
**File:** `Runtime/Core/WidgetBase.cs:331-339, 470-476, 107-112, 85-90`

When the animation token cancels (external token, or `_animationCts.Cancel()` on interrupt/teardown), the registration fires `CancelCompletionSource()` which `TrySetCanceled()`s **both** completion sources. `await handle.AnimationAwaitable` then throws `OperationCanceledException`; the catch calls `CompleteAnimationHandle(handle)` → `handle.Complete()` → `_completedCompletionSource.SetResult()`. That source is already canceled, and `SetResult()` (non-`Try`) throws `InvalidOperationException`. Two consequences:
1. The OCE is replaced by an unexpected `InvalidOperationException`.
2. `handle.Release()` is never reached → the pooled handle leaks (defeats #9); its `OnComplete` subscription and `CancellationTokenRegistration` are not disposed.

`Cancel()` has the same hazard (`_completedCompletionSource.SetCanceled()` also non-`Try`). Fix: use `TrySetResult`/`TrySetCanceled` in `Complete()`/`Cancel()`, or route the catch path through a release-only path instead of `Complete()`.

---

### 22. 🔲 A cancelled animation leaves the widget permanently non-interactable
**File:** `Runtime/Core/WidgetBase.cs:306, 342`

A non-interrupting animation sets `IsInteractableInternal.SetOverrideValue(false)` on entry (`:306`) and only restores it on the success path (`:342`). The `catch (OperationCanceledException)` path never restores it, so a navigation/transition cancelled via its token (with no follow-up `SetVisibility`, which would restore at `:230`) leaves the widget stuck non-interactable. Currently masked by #21 throwing first; fix together. Restore the override in the catch (or a `finally`).

---

### 23. 🔲 `History.Clear()` orphans pooled entries and events
**File:** `Runtime/Core/History.cs:287-295`

`Clear()` only calls `_history.Clear()`; the pooled `HistoryEntry` objects (and the `PooledHistoryEvent`s + lists they own) are dropped to GC instead of returned to their pools. Not a correctness bug, but it silently undoes the pooling (#18) on every `ScreenGroup` termination (`ScreenGroupBase.Terminate` → `_history?.Clear()`). Fix: iterate and `Release()` each entry (which already re-pools its events and list).

---

### 24. 🔲 `ScreenGroupController` is entirely `NotImplementedException`
**File:** `Runtime/Controllers/ScreenGroupController.cs`

Still a stub implementing a public interface — an instantiable type that throws on every call. Guard with `internal`/`[Obsolete]` until implemented so consumers don't bind to it. (Excluded from earlier reviews as WIP; noting for tracking.)

---

### 25. 🔲 `WindowNavigator.ActiveInstance` does a dictionary lookup on every access
**File:** `Runtime/Navigation/WindowNavigator.cs:16-17`

`ActiveInstance`/`ActiveIndex` resolve through `_registry.Get(_activeType)`/`IndexOf` each call; hit on every `CreateNavigateToRequest`, `NavigateTo` result, and `PreviousScreen`. The active instance only changes in `NavigateTo`/`Clear`/`OnWidgetUnregistered` — cache the resolved reference. Low priority (no allocation).

---

### 26. ⚠️ `GetDefaultAnimation` allocates per navigation
**File:** `Runtime/UIToolkit/Widget.cs:91-93`

`GetGenericAnimation` does `new ShowWidgetAnimation(...)` / `new HideWidgetAnimation(...)` every time a transition is resolved — the last per-navigation heap allocation on the UITK path. Harder to pool (carries per-call target state). Deferred unless it shows in a profile.

---

## Priority Summary

| Priority | Item |
|---|---|
| ✅ Done | #1 WidgetRegistry.TryGet wrong type |
| ✅ Done | #2 TransitionManager.CreateSourcePlayable wrong widget |
| ⚠️ Manual | #3 ImagePlusPlus.GetAdjustedRadii unclamped |
| ✅ Done | #4 WidgetBase double-unsubscribe |
| ✅ Done | #5 CTS allocation in hot paths |
| ✅ Done | #6 IterateHierarchy Stack allocation |
| ✅ Done | #7 TransitionManager.Entry pooled |
| ✅ Done | #8 VisibilityAnimationHandle pooled + lazy completion source |
| ✅ Done | #9 NavigateToRequest null source window |
| ✅ Done | #10 XOR visibility toggle |
| ✅ Done | #11 IsValidData guard + throw |
| ⚠️ Deferred | #14 ModalWidget stubs |
| ✅ Done | #15 ScreenGroupBase init order comment |
| ✅ Done | #16 Widget.SetInteractable closure (IterateHierarchy generic overload) |
| ⚠️ Deferred | #17 Widget.GetClasses() enumerator boxing — classList not public in UITK 6.3 |
| ✅ Done | #18 History.PushNewEntry allocation (PooledHistoryEntry<TSelf> CRTP base) |
| ✅ Done | #19 ButtonGraphicComponent uncached Animator |
| ✅ Done | #20 ButtonPlusPlus/ButtonGraphicComponent lazy init → Awake |
| 🔲 Must-fix | #21 AnimateVisibility cancel path throws + leaks handle |
| 🔲 Must-fix | #22 Cancelled animation leaves widget non-interactable |
| 🔲 Pending | #23 History.Clear orphans pooled entries/events |
| 🔲 Pending | #24 ScreenGroupController NotImplementedException stub |
| 🔲 Pending | #25 WindowNavigator.ActiveInstance per-access lookup |
| ⚠️ Deferred | #26 GetDefaultAnimation per-navigation allocation |
