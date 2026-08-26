using UIFramework.Core.Interfaces;

namespace UIFramework.Animation
{
    public abstract class WidgetVisibilityAnimation : WidgetAnimation
    {
        private readonly WidgetVisibility _visibility;

        protected WidgetVisibilityAnimation(WidgetVisibility visibility)
        {
            _visibility = visibility;
        }
        
        // Orients playback time to the target visibility: a show runs 0 -> 1 (hidden pose to resting
        // pose), a hide runs the same journey backwards. Subclasses treat the resolved value as "how
        // present is the widget", whatever property they animate.
        protected float ResolveNormalisedTime(float normalisedTime)
        {
            return _visibility == WidgetVisibility.Visible ? normalisedTime : 1.0F - normalisedTime;
        }
    }
}