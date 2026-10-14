# UI Framework

`com.lukeandrews.ui-framework` · Unity 6000.0 or later · MIT

A framework for building Unity UI out of windows, screens and controllers, with navigation history and animated
transitions. It has a uGUI implementation and a UI Toolkit implementation, and screens built with either can share one
controller and open each other.

## Install

The package depends on [Unity Engine Extension](https://github.com/Sunshine-Kittens/Unity-Extensions)
(`com.lukeandrews.unity-extension`), which no package registry serves, so add both. Either list them as git
dependencies in `Packages/manifest.json`:

```json
"com.lukeandrews.unity-extension": "https://github.com/Sunshine-Kittens/Unity-Extensions.git",
"com.lukeandrews.ui-framework": "https://github.com/Sunshine-Kittens/Unity-UI-Framework.git",
```

or add each repository as a git submodule at `Packages/com.lukeandrews.unity-extension` and
`Packages/com.lukeandrews.ui-framework`, which Unity loads as embedded packages. Pin a commit for a live project.

The package is the repository root. Don't install with `?path=Packages/com.lukeandrews.ui-framework`: that folder
holds only a copy of `package.json`.

## Windows, screens and controllers

- **Window** (`IWindow`): a panel that opens and closes, optionally animated, and receives data through
  `SetData(object)`. Base classes: `UIFramework.UGUI.Window` (a MonoBehaviour) and `UIFramework.UIToolkit.Window` (a
  plain class bound to a `VisualElement` by name).
- **Screen** (`IScreen`): a window meant to fill the screen and be the only interactive one while visible. It belongs
  to one controller, which it can reach through `Controller` or `GetController<T>()`, and says whether it goes into
  history (`SupportsHistory`). Base classes: `UIFramework.UGUI.Screen` and `UIFramework.UIToolkit.Screen`.
- **Controller**: an abstract MonoBehaviour, derived once per UI. It owns its screens, keeps one active at a time,
  navigates between them, keeps the history, and updates the visible screens each frame through Unity Engine
  Extension's `UpdateManager`.

A controller implements two members and is started by its owner, not by Unity: `Awake` only registers it for updates.

```csharp
public sealed class MenuController : UIFramework.Controller
{
    protected override void SetBackButtonActive(bool active) { }
    public override void SetWaiting(bool waiting) { }
}
```

Call `Initialize()` before opening a screen and `Terminate()` when the UI goes away (the sample uses `OnEnable` and
`OnDisable`).

### Collecting screens

A controller finds its screens through the `ScreenCollectorComponent`s in its serialized `ScreenCollectors` array:

- `UIFramework.UGUI.ScreenCollector` collects the screens under its own transform and any extra transforms you list;
- `UIFramework.UIToolkit.ScreenCollector` creates the screens in its own list, each an identifier (the element name)
  and a screen type, against the `UIBehaviourDocument` on the same GameObject.

One controller can use collectors of both kinds. Each screen type may appear once per controller, or `Initialize`
throws.

### Opening and closing

```csharp
controller.OpenScreen<SettingsScreen>(0.3f);
controller.OpenScreen<SettingsScreen>(settingsData, TransitionAnimationParams.SlideFromRight(0.3f, EasingMode.EaseInOut));
controller.CloseScreen();
controller.CloseAll(0.3f);
```

- `OpenScreen<T>` picks the screen by type and takes optional data first, then one of:
  - a length, which plays both screens' default animations;
  - `AccessAnimationParams`, the entry animation only, with the screen you leave playing its default;
  - `TransitionAnimationParams`, the exit and entry animations together.
- `excludeCurrentFromHistory` keeps the screen you are leaving out of history.
- `TryOpenScreen(identifier, out screen, …)` opens a screen by its identifier instead of its type.
- `CloseScreen` (or `TryCloseScreen`) goes back one step in history, playing the transition that led there in
  reverse; with no history it does nothing.
- `CloseAll` closes the active screen, clears history and closes the controller.
- A screen's back button (`BackButton` in uGUI, `BackButtonName` in UI Toolkit) calls `OnBackButtonClicked`, a hook
  you can override, and then `TryCloseScreen`.
- Events: `Opened`, `Closed`, and `ScreenOpened` and `ScreenClosed`, which fire when a screen starts to open or close.

A controller can animate itself as well, for a background its screens share: override `CreateAccessPlayable`. It plays
when the first screen opens and on `CloseAll`.

## Animations

- Every window supports the generic animations in `GenericWindowAnimationType`: `Fade`, `SlideFromLeft`,
  `SlideFromRight`, `SlideFromBottom`, `SlideFromTop`, `Flip` and `Expand`. Its default animation is `Fade` unless it
  overrides `GetDefaultAccessAnimation`.
- `AccessAnimationParams` describes one window's animation: a length, an optional easing (`Linear` by default), and
  optionally a generic type or a bespoke `AccessAnimation`; with neither, the window's default plays.
- `TransitionAnimationParams` pairs an exit animation with an entry animation. Presets: `Fade`, `SlideFrom…`,
  `SlideOverFrom…`, `Flip`, `Expand`, `Custom` and `None`.
- `AccessAnimationPlayable` is what a window actually plays: the animation plus start time, playback mode (`Forward`
  or `Reverse`), easing, speed and time mode (`Scaled` or `Unscaled`). `EasingMode`, `PlaybackMode` and `TimeMode`
  come from Unity Engine Extension.

## Data

The controller passes data to a screen with `SetData` before it opens. The base windows ignore data, so a window
overrides `SetData` and checks the type itself: `UIData.IsValid<T>` and `UIData.ValidateObject<T>` help. Nothing in
the framework calls `IsValidData`. A window shouldn't fetch or build its own data.

## Also in the package

- `TabController`: switches between a set of windows by type or index, with an animation and optional data. Its owner
  calls its `Initialize`, `UpdateUI` and `Terminate`.
- `WindowCollectorComponent` and its uGUI and UI Toolkit collectors: collect windows, for example for a
  `TabController`.
- `ButtonPlusPlus`: a uGUI `Button` with named states (colours, sprites, animation triggers) set with `SetButtonState`.
- `ImagePlusPlus`: a uGUI `Image` with rounded corners, a border and a gradient. It loads the shader `UI/Default++` with
  `Shader.Find`, so a player build must include that shader (for example under Always Included Shaders).
- Editor: inspectors and `GameObject > UI` menu items for `ButtonPlusPlus` and `ImagePlusPlus`.
- Sample, **Examples** (import it from the Package Manager): one controller driving three uGUI screens and two UI
  Toolkit screens, with every transition preset, data, a controller fade, and Escape to go back.

## Assemblies

| Assembly | Platforms | Namespaces |
|---|---|---|
| `UIFramework` | All | `UIFramework`, `UIFramework.UGUI`, `UIFramework.UIToolkit` |
| `UIFramework.Editor` | Editor | `UIFramework.Editor`, `UIFramework.Editor.UGUI` |

`UIFramework` references Unity Engine Extension's `UnityEngine.Extension` assembly.
