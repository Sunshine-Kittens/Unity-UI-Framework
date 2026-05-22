using UIFramework.Core;
using UIFramework.Transitioning;

namespace UIFramework.Navigation.History
{
    public readonly struct TransitionHistoryEvent : IHistoryEvent
    {
        public readonly VisibilityTransitionParams Transition;

        public TransitionHistoryEvent(VisibilityTransitionParams transition)
        {
            Transition = transition;
        }
    }
}
