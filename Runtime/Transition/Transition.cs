using UnityEngine.Extension;

namespace UIFramework
{
    public static class Transition
    {
        public static VisibilityTransitionParams None()
        {
            return new VisibilityTransitionParams(0.0F, EasingMode.Linear, null, null, TransitionSortPriority.Auto);
        }

        public static VisibilityTransitionParams Fade(float length, EasingMode easingMode)
        {
            return new VisibilityTransitionParams(length, easingMode, null, GenericAnimation.Fade, TransitionSortPriority.Auto);
        }

        public static VisibilityTransitionParams SlideFromLeft(float length, EasingMode easingMode)
        {
            return new VisibilityTransitionParams(length, easingMode, GenericAnimation.SlideFromRight, GenericAnimation.SlideFromLeft, TransitionSortPriority.Auto);
        }

        public static VisibilityTransitionParams SlideFromRight(float length, EasingMode easingMode)
        {
            return new VisibilityTransitionParams(length, easingMode, GenericAnimation.SlideFromLeft, GenericAnimation.SlideFromRight, TransitionSortPriority.Auto);
        }

        public static VisibilityTransitionParams SlideFromBottom(float length, EasingMode easingMode)
        {
            return new VisibilityTransitionParams(length, easingMode, GenericAnimation.SlideFromTop, GenericAnimation.SlideFromBottom, TransitionSortPriority.Auto);
        }

        public static VisibilityTransitionParams SlideFromTop(float length, EasingMode easingMode)
        {
            return new VisibilityTransitionParams(length, easingMode, GenericAnimation.SlideFromBottom, GenericAnimation.SlideFromTop, TransitionSortPriority.Auto);
        }

        public static VisibilityTransitionParams SlideOverFromLeft(float length, EasingMode easingMode)
        {
            return new VisibilityTransitionParams(length, easingMode, null, GenericAnimation.SlideFromLeft, TransitionSortPriority.Auto);
        }

        public static VisibilityTransitionParams SlideOverFromRight(float length, EasingMode easingMode)
        {
            return new VisibilityTransitionParams(length, easingMode, null, GenericAnimation.SlideFromRight, TransitionSortPriority.Auto);
        }

        public static VisibilityTransitionParams SlideOverFromBottom(float length, EasingMode easingMode)
        {
            return new VisibilityTransitionParams(length, easingMode, null, GenericAnimation.SlideFromBottom, TransitionSortPriority.Auto);
        }

        public static VisibilityTransitionParams SlideOverFromTop(float length, EasingMode easingMode)
        {
            return new VisibilityTransitionParams(length, easingMode, null, GenericAnimation.SlideFromTop, TransitionSortPriority.Auto);
        }

        public static VisibilityTransitionParams Flip(float length, EasingMode easingMode)
        {
            return new VisibilityTransitionParams(length, easingMode, GenericAnimation.Flip, null, TransitionSortPriority.Auto);
        }

        public static VisibilityTransitionParams Expand(float length, EasingMode easingMode)
        {
            return new VisibilityTransitionParams(length, easingMode, null, GenericAnimation.Expand, TransitionSortPriority.Auto);
        }

        public static VisibilityTransitionParams Custom(float length, EasingMode easingMode, WidgetAnimation exitAnimation, WidgetAnimation entryAnimation, TransitionSortPriority transitionSortPriority = TransitionSortPriority.Auto)
        {
            return new VisibilityTransitionParams(length, easingMode, exitAnimation, entryAnimation, transitionSortPriority);
        }
    }
}