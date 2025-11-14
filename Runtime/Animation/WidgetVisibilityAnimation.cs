namespace UIFramework.Animation
{
    public abstract class WidgetVisibilityAnimation : WidgetAnimation
    {
        private readonly WidgetVisibility _visibility;

        protected WidgetVisibilityAnimation(WidgetVisibility visibility)
        {
            _visibility = visibility;
        }
        
        protected float ResolveNormalisedTime(float normalisedTime)
        {
            return _visibility == WidgetVisibility.Hidden ? normalisedTime : 1.0F - normalisedTime;
        }
    }
}