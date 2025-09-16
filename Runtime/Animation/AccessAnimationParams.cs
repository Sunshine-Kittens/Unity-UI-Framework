using UnityEngine.Extension;

namespace UIFramework
{
    public struct AccessAnimationParams
    {        
        public float Length { get; private set; }
        public EasingMode EasingMode { get; private set; }
        public ImplicitAnimation ImplicitAnimation { get; private set; }

        public AccessAnimationParams(float length)
            : this(null, length, EasingMode.Linear) { }

        public AccessAnimationParams(float length, EasingMode easingMode)
            : this(null, length, easingMode) { }

        public AccessAnimationParams(ImplicitAnimation animation, float length)
            : this(animation, length, EasingMode.Linear) { }        

        public AccessAnimationParams(ImplicitAnimation animation, float length, EasingMode easingMode)
        {
            Length = length;
            ImplicitAnimation = animation;
            EasingMode = easingMode;            
        }

        private WidgetAnimation GetWindowAnimation(IWindow window)
        {
            if(ImplicitAnimation == null)
            {
                return window.GetDefaultAccessAnimation();
            }
            else
            {
                return ImplicitAnimation.GetWindowAnimation(window);
            }
        }

        public AccessAnimationPlayable CreatePlayable(IWindow window, AccessOperation accessOperation, float startOffset = 0.0F, TimeMode timeMode = TimeMode.Scaled)
        {
            return GetWindowAnimation(window).CreatePlayable(accessOperation, Length, startOffset, EasingMode, timeMode);
        }
    }
}
