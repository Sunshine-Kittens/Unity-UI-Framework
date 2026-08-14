using System;
using System.Collections.Generic;
using System.Threading;

using UIFramework.Animation;
using UIFramework.Core.Interfaces;
using UIFramework.Navigation.Interfaces;

using UnityEngine;
using UnityEngine.Extension;

namespace UIFramework.TestUtils
{
    // Pure-C# IScreen double. No MonoBehaviour, no GameObject, no player loop, so the group stack, registry
    // and navigator can all be driven from edit mode.
    //
    // Abstract because WidgetRegistry is Type-keyed: two instances of one concrete type cannot co-exist in a
    // registry, so tests use the distinct subclasses below.
    public abstract class FakeScreen : IScreen
    {
        public readonly List<string> Calls = new();
        public readonly List<float> Ticks = new();

        // Mirrors WidgetBase.SetVisibility, which raises the pair synchronously. Disable to raise them by hand
        // and simulate a transition that has not settled yet.
        public bool AutoRaiseVisibilityEvents = true;

        // NavigateToCoordinator rejects data the target does not accept; flip this to exercise that path.
        public bool AcceptsData;
        public object LastData;

        public string Identifier { get; set; } = string.Empty;

        public WidgetState State { get; private set; } = WidgetState.Uninitialized;
        public bool IsInitialized => State == WidgetState.Initialized;

        public IWidget Parent => null;
        public int ChildCount => 0;

        public WidgetVisibility Visibility { get; private set; } = WidgetVisibility.Hidden;
        public bool IsVisible => Visibility == WidgetVisibility.Visible && Opacity > 0f;
        public bool IsAnimating => false;

        public int LocalSortOrder { get; private set; }
        public int GlobalSortOrder { get; private set; }
        public int RenderSortOrder { get; private set; }
        public float Opacity { get; private set; } = 1f;

        IReadOnlyScalarFlag IReadOnlyWidget.IsEnabled => _isEnabled;
        IReadOnlyScalarFlag IReadOnlyWidget.IsInteractable => _isInteractable;
        public IScalarFlag IsEnabled => _isEnabled;
        public IScalarFlag IsInteractable => _isInteractable;
        private readonly ScalarFlag _isEnabled = new(true);
        private readonly ScalarFlag _isInteractable = new(true);

        public IScreenNavigator Navigator { get; private set; }

        public event WidgetAction Initialized;
        public event WidgetAction Terminated;
        public event WidgetAction Showing;
        public event WidgetAction Shown;
        public event WidgetAction Hiding;
        public event WidgetAction Hidden;

        // Arrangement helper — lets a test start from Terminated to reproduce the enable/disable re-init gap.
        public void ForceState(WidgetState state) => State = state;

        public void RaiseShowing() { Calls.Add(nameof(RaiseShowing)); Showing?.Invoke(this); }
        public void RaiseShown() { Calls.Add(nameof(RaiseShown)); Shown?.Invoke(this); }
        public void RaiseHiding() { Calls.Add(nameof(RaiseHiding)); Hiding?.Invoke(this); }
        public void RaiseHidden() { Calls.Add(nameof(RaiseHidden)); Hidden?.Invoke(this); }

        public void Initialize()
        {
            Calls.Add(nameof(Initialize));
            State = WidgetState.Initialized;
            Initialized?.Invoke(this);
        }

        public void Terminate()
        {
            Calls.Add(nameof(Terminate));
            State = WidgetState.Terminated;
            Terminated?.Invoke(this);
        }

        public void SetVisibility(WidgetVisibility visibility)
        {
            Calls.Add($"{nameof(SetVisibility)}:{visibility}");
            if (visibility == Visibility)
                return;

            Visibility = visibility;
            if (!AutoRaiseVisibilityEvents)
                return;

            if (visibility == WidgetVisibility.Visible)
            {
                Showing?.Invoke(this);
                Shown?.Invoke(this);
            }
            else
            {
                Hiding?.Invoke(this);
                Hidden?.Invoke(this);
            }
        }

        public bool IsVisibilityState(WidgetVisibility visibility, bool? isAnimating = null)
            => Visibility == visibility && (!isAnimating.HasValue || isAnimating.Value == IsAnimating);

        public void Tick(float deltaTime) => Ticks.Add(deltaTime);

        IReadOnlyWidget IReadOnlyWidget.GetChildAt(int index) => null;
        public IWidget GetChildAt(int index) => null;

        public void SetLocalSortOrder(int sortOrder) { LocalSortOrder = sortOrder; Calls.Add($"{nameof(SetLocalSortOrder)}:{sortOrder}"); }
        public void SetGlobalSortOrder(int sortOrder) { GlobalSortOrder = sortOrder; Calls.Add($"{nameof(SetGlobalSortOrder)}:{sortOrder}"); }
        public void SetRenderSortOrder(int sortOrder) { RenderSortOrder = sortOrder; Calls.Add($"{nameof(SetRenderSortOrder)}:{sortOrder}"); }
        public void SetOpacity(float opacity) { Opacity = opacity; Calls.Add($"{nameof(SetOpacity)}:{opacity}"); }

        public void SortAbove(IWidget target) => Calls.Add(nameof(SortAbove));
        public void SortBelow(IWidget target) => Calls.Add(nameof(SortBelow));
        public void SortInlineWith(IWidget target) => Calls.Add(nameof(SortInlineWith));

        public bool IsValidData(object data) => AcceptsData;
        public void SetData(object data) { LastData = data; Calls.Add(nameof(SetData)); }

        public void SetNavigator(IScreenNavigator navigator) { Navigator = navigator; Calls.Add(nameof(SetNavigator)); }
        public void ClearNavigator() { Navigator = null; Calls.Add(nameof(ClearNavigator)); }

        // Null by default — consumers treat "no default animation" as "show instantly". Assign to check
        // whether the framework ever resolves a default on the entry side, which it currently does not.
        public IAnimation DefaultVisibleAnimation;
        public IAnimation DefaultHiddenAnimation;

        public IAnimation GetDefaultAnimation(WidgetVisibility visibility)
            => visibility == WidgetVisibility.Visible ? DefaultVisibleAnimation : DefaultHiddenAnimation;

        public IAnimation GetGenericAnimation(GenericAnimation genericAnimation, WidgetVisibility visibility) => null;

        public void ResetAnimatedProperties() => Calls.Add(nameof(ResetAnimatedProperties));

        // Animation is driven by the player loop, which does not run in edit mode. Throwing names the problem
        // rather than leaving a test to fail on a null Awaitable.
        private const string _AnimationUnsupported =
            "FakeScreen cannot animate: animation is player-loop driven. Use a play-mode test with real widgets.";

        public VisibilityAnimationBuilder AnimateVisibility(WidgetVisibility visibility)
            => throw new NotSupportedException(_AnimationUnsupported);

        public Awaitable AnimateVisibility(WidgetVisibility visibility, AnimationPlayable playable,
            InterruptBehavior interruptBehavior = InterruptBehavior.Immediate, CancellationToken cancellationToken = default)
            => throw new NotSupportedException(_AnimationUnsupported);

        public Awaitable SkipAnimation() => throw new NotSupportedException(_AnimationUnsupported);
        public Awaitable RewindAnimation(CancellationToken cancellationToken = default)
            => throw new NotSupportedException(_AnimationUnsupported);
    }

    public sealed class FakeScreenA : FakeScreen { }
    public sealed class FakeScreenB : FakeScreen { }
    public sealed class FakeScreenC : FakeScreen { }
    public sealed class FakeScreenD : FakeScreen { }
    public sealed class FakeScreenE : FakeScreen { }
}
