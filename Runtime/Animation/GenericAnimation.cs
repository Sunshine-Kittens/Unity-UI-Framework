using System;

using UIFramework.Core.Interfaces;

namespace UIFramework.Animation
{
    /// <summary>
    /// Type of animation. Some assumptions are made.
    /// Fade, Flip, Expand: These are assumed to be from a inactive state to an active state.
    /// E.g, animating from an alpha of 0 to 1 for Fade.
    /// </summary>
    public enum GenericAnimation
    {
        Fade,
        SlideFromLeft,
        SlideFromRight,
        SlideFromBottom,
        SlideFromTop,
        Flip,
        Expand
    }

    public abstract class GenericWidgetAnimation : WidgetVisibilityAnimation
    {
        protected readonly GenericAnimation GenericAnimation;
        private readonly GenericAnimation _fallback;
        
        protected GenericWidgetAnimation(GenericAnimation genericAnimation, WidgetVisibility visibility, GenericAnimation fallback = GenericAnimation.Fade)
            : base(visibility)
        {
            GenericAnimation = genericAnimation;
            _fallback = fallback;
        }

        public override void Evaluate(float normalisedTime)
        {
            GenericAnimation evaluationAnimation = IsSupportedType(GenericAnimation) ? GenericAnimation : _fallback;
            switch (evaluationAnimation)
            {
                case GenericAnimation.Fade:
                    Fade(normalisedTime);
                    break;
                case GenericAnimation.SlideFromLeft:
                    SlideFromLeft(normalisedTime);
                    break;
                case GenericAnimation.SlideFromRight:
                    SlideFromRight(normalisedTime);
                    break;
                case GenericAnimation.SlideFromBottom:
                    SlideFromBottom(normalisedTime);
                    break;
                case GenericAnimation.SlideFromTop:
                    SlideFromTop(normalisedTime);
                    break;
                case GenericAnimation.Flip:
                    Flip(normalisedTime);
                    break;
                case GenericAnimation.Expand:
                    Expand(normalisedTime);
                    break;
                default:
                    throw new NotImplementedException();
            }            
        }

        protected abstract void Fade(float normalisedTime);
        protected abstract void SlideFromLeft(float normalisedTime);
        protected abstract void SlideFromRight(float normalisedTime);
        protected abstract void SlideFromBottom(float normalisedTime);
        protected abstract void SlideFromTop(float normalisedTime);
        protected abstract void Flip(float normalisedTime);
        protected abstract void Expand(float normalisedTime);
        protected abstract bool IsSupportedType(GenericAnimation type);
    }
}
