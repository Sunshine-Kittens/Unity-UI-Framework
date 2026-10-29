using UIFramework.Core;
using UIFramework.Transitioning;

namespace UIFramework.Navigation.History
{
    public class TransitionHistoryEvent : PooledHistoryEvent<TransitionHistoryEvent>
    {
        public VisibilityTransitionParams Transition { get; private set; }

        public static TransitionHistoryEvent Get(VisibilityTransitionParams transition)
        {
            TransitionHistoryEvent e = Get();
            e.Transition = transition;
            return e;
        }

        protected override void OnRelease() => Transition = default;
    }
}
