using System;

using UnityEngine.Extension;

namespace UIFramework
{
    public readonly struct ImplicitAnimation : IEquatable<ImplicitAnimation>
    {
        public bool IsValid => _animation != null || _animationType != null;
        
        private readonly WidgetAnimation _animation;
        private readonly GenericAnimation? _animationType;

        private ImplicitAnimation(GenericAnimation animationType)
        {
            _animation = null;
            _animationType = animationType;
        }        

        private ImplicitAnimation(WidgetAnimation widgetAnimation)
        {
            _animation = widgetAnimation;
            _animationType = null;
        }        

        public static implicit operator ImplicitAnimation(GenericAnimation animationType)
        {
            return new ImplicitAnimation(animationType);
        }

        public static implicit operator GenericAnimation(ImplicitAnimation implicitAnimation)
        {
            return implicitAnimation._animationType.GetValueOrDefault(GenericAnimation.Fade);
        }

        public static implicit operator ImplicitAnimation(WidgetAnimation widgetAnimation)
        {
            return new ImplicitAnimation(widgetAnimation);
        }

        public static implicit operator WidgetAnimation(ImplicitAnimation implicitAnimation)
        {
            return implicitAnimation._animation;
        }

        public IAnimation GetAnimation(IWidget widget, WidgetVisibility visibility)
        {
            if (_animationType != null)
            {
                return widget.GetGenericAnimation(_animationType.Value, visibility);
            }
            if (_animation != null)
            {
                return _animation;   
            }
            throw new InvalidOperationException("Animation is not valid");
        }

        public bool Equals(ImplicitAnimation other)
        {
            return Equals(_animation, other._animation) && _animationType == other._animationType;
        }

        public override bool Equals(object obj)
        {
            return obj is ImplicitAnimation other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(_animation, _animationType);
        }
    }
}
