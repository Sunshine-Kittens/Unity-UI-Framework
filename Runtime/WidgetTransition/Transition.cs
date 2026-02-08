using UIFramework.Animation;

using UnityEngine.Extension;

namespace UIFramework.WidgetTransition
{
    public static class Transition
    {
        public static VisibilityTransitionParams None()
        {
            return new VisibilityTransitionParams(0.0F, EasingMode.Linear, default, default, TransitionSortPriority.Auto);
        }

        public static VisibilityTransitionParams Fade(float length, EasingMode easingMode)
        {
            return new VisibilityTransitionParams(length, easingMode, default, WidgetAnimationRef.FromGeneric(GenericAnimation.Fade), 
                TransitionSortPriority.Auto);
        }

        public static VisibilityTransitionParams SlideFromLeft(float length, EasingMode easingMode)
        {
            return new VisibilityTransitionParams(length, easingMode, WidgetAnimationRef.FromGeneric(GenericAnimation.SlideFromRight), 
                WidgetAnimationRef.FromGeneric(GenericAnimation.SlideFromLeft), TransitionSortPriority.Auto);
        }

        public static VisibilityTransitionParams SlideFromRight(float length, EasingMode easingMode)
        {
            return new VisibilityTransitionParams(length, easingMode, WidgetAnimationRef.FromGeneric(GenericAnimation.SlideFromLeft), 
                WidgetAnimationRef.FromGeneric(GenericAnimation.SlideFromRight), TransitionSortPriority.Auto);
        }

        public static VisibilityTransitionParams SlideFromBottom(float length, EasingMode easingMode)
        {
            return new VisibilityTransitionParams(length, easingMode, WidgetAnimationRef.FromGeneric(GenericAnimation.SlideFromTop), 
                WidgetAnimationRef.FromGeneric(GenericAnimation.SlideFromBottom), TransitionSortPriority.Auto);
        }

        public static VisibilityTransitionParams SlideFromTop(float length, EasingMode easingMode)
        {
            return new VisibilityTransitionParams(length, easingMode, WidgetAnimationRef.FromGeneric(GenericAnimation.SlideFromBottom), 
                WidgetAnimationRef.FromGeneric(GenericAnimation.SlideFromTop), TransitionSortPriority.Auto);
        }

        public static VisibilityTransitionParams SlideOverFromLeft(float length, EasingMode easingMode)
        {
            return new VisibilityTransitionParams(length, easingMode, default, WidgetAnimationRef.FromGeneric(GenericAnimation.SlideFromLeft), 
                TransitionSortPriority.Auto);
        }

        public static VisibilityTransitionParams SlideOverFromRight(float length, EasingMode easingMode)
        {
            return new VisibilityTransitionParams(length, easingMode, default, WidgetAnimationRef.FromGeneric(GenericAnimation.SlideFromRight), 
                TransitionSortPriority.Auto);
        }

        public static VisibilityTransitionParams SlideOverFromBottom(float length, EasingMode easingMode)
        {
            return new VisibilityTransitionParams(length, easingMode, default, WidgetAnimationRef.FromGeneric(GenericAnimation.SlideFromBottom), 
                TransitionSortPriority.Auto);
        }

        public static VisibilityTransitionParams SlideOverFromTop(float length, EasingMode easingMode)
        {
            return new VisibilityTransitionParams(length, easingMode, default, WidgetAnimationRef.FromGeneric(GenericAnimation.SlideFromTop), 
                TransitionSortPriority.Auto);
        }

        public static VisibilityTransitionParams Flip(float length, EasingMode easingMode)
        {
            return new VisibilityTransitionParams(length, easingMode, WidgetAnimationRef.FromGeneric(GenericAnimation.Flip), 
                default, TransitionSortPriority.Auto);
        }

        public static VisibilityTransitionParams Expand(float length, EasingMode easingMode)
        {
            return new VisibilityTransitionParams(length, easingMode, default, WidgetAnimationRef.FromGeneric(GenericAnimation.Expand), 
                TransitionSortPriority.Auto);
        }

        public static VisibilityTransitionParams Custom(float length, EasingMode easingMode, IAnimation exitAnimation, IAnimation entryAnimation, 
            TransitionSortPriority transitionSortPriority = TransitionSortPriority.Auto)
        {
            WidgetAnimationRef exitAnimationRef = exitAnimation != null ? WidgetAnimationRef.FromExplicit(exitAnimation) : default;
            WidgetAnimationRef entryAnimationRef = entryAnimation != null ? WidgetAnimationRef.FromExplicit(entryAnimation) : default;
            return new VisibilityTransitionParams(length, easingMode, in exitAnimationRef, in entryAnimationRef, transitionSortPriority);
        }

        public static VisibilityTransitionParams Custom(float length, EasingMode easingMode, in WidgetAnimationRef exitAnimationRef, IAnimation entryAnimation, 
            TransitionSortPriority transitionSortPriority = TransitionSortPriority.Auto)
        {
            WidgetAnimationRef entryAnimationRef = entryAnimation != null ? WidgetAnimationRef.FromExplicit(entryAnimation) : default;
            return new VisibilityTransitionParams(length, easingMode, in exitAnimationRef, in entryAnimationRef, transitionSortPriority);
        }
        
        public static VisibilityTransitionParams Custom(float length, EasingMode easingMode, IAnimation exitAnimation, in WidgetAnimationRef entryAnimationRef, 
            TransitionSortPriority transitionSortPriority = TransitionSortPriority.Auto)
        {
            WidgetAnimationRef exitAnimationRef = exitAnimation != null ? WidgetAnimationRef.FromExplicit(exitAnimation) : default;
            return new VisibilityTransitionParams(length, easingMode, in exitAnimationRef, in entryAnimationRef, transitionSortPriority);
        }
        
        public static VisibilityTransitionParams Custom(float length, EasingMode easingMode, in WidgetAnimationRef exitAnimationRef, 
            in WidgetAnimationRef entryAnimationRef, TransitionSortPriority transitionSortPriority = TransitionSortPriority.Auto)
        {
            return new VisibilityTransitionParams(length, easingMode, exitAnimationRef, entryAnimationRef, transitionSortPriority);
        }
        
        public static VisibilityTransitionParams Invert(this in VisibilityTransitionParams @params)
        {
            return @params.Invert();
        }
    }
}