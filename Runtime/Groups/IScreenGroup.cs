using System;

using UIFramework.Core.Interfaces;
using UIFramework.Navigation.Interfaces;

using UnityEngine.Extension;

namespace UIFramework.Groups
{
    // A group of screens with its own navigation context (navigator + history + transitions), composed over
    // the controller's shared screen registry. A group is just that collection plus its navigation state; it
    // has no notion of where it sits relative to other groups, and no notion of render order. The controller
    // is what stacks groups and renders each group's screens in the right order — that arrangement is the
    // layering, and it belongs entirely to the controller, never here.
    //
    // Consolidates the former IScreenGroup + IScreenGroupBase. There is no separate Initialize/Terminate:
    // a group is constructed live over an already-initialized registry and reset for pooled reuse by its
    // owning controller.
    public interface IScreenGroup : IScreenNavigator
    {
        public bool IsVisible { get; }
        public float Opacity { get; }

        public IScreen ActiveScreen { get; }
        public IScreen PreviousScreen { get; }

        // The controller pauses lower layers by driving this flag; it is read-only to consumers.
        public IReadOnlyScalarFlag IsInteractable { get; }

        public TimeMode TimeMode { get; }

        public event Action Entering;
        public event Action Entered;
        public event Action Exiting;
        public event Action Exited;

        public event ScreenAction ScreenShowing;
        public event ScreenAction ScreenShown;
        public event ScreenAction ScreenHiding;
        public event ScreenAction ScreenHidden;

        public void SetOpacity(float opacity);
        public void Tick();
    }
}
