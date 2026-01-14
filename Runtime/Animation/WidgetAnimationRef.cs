using System;
using UnityEngine.Extension;

namespace UIFramework
{
    public readonly struct WidgetAnimationRef : IEquatable<WidgetAnimationRef>
    {
        public bool IsValid => _type != Type.None;
        
        private enum Type { None, Generic, Explicit }
        private readonly Type _type;
        private readonly IAnimation _widgetAnimation;
        private readonly GenericAnimation _genericAnimation;

        private WidgetAnimationRef(Type type, IAnimation widgetAnimation, GenericAnimation genericAnimation)
        {
            _type = type;
            _widgetAnimation = widgetAnimation;
            _genericAnimation = genericAnimation;
        }        

        public static WidgetAnimationRef FromGeneric(GenericAnimation animationType)
        {
            return new WidgetAnimationRef(Type.Generic, null, animationType);
        }

        public static WidgetAnimationRef FromExplicit(IAnimation widgetAnimation)
        {
            if(widgetAnimation == null) throw new ArgumentNullException(nameof(widgetAnimation));
            return new WidgetAnimationRef(Type.Generic, widgetAnimation, default);
        }

        public IAnimation Resolve(IWidget widget, WidgetVisibility visibility)
        {
            switch (_type)
            {
                case Type.Explicit:
                    return _widgetAnimation;
                case Type.Generic:
                    return widget.GetGenericAnimation(_genericAnimation, visibility);  
                default:
                    throw new InvalidOperationException("Invalid animation reference.");        
            }
        }

        public bool Equals(WidgetAnimationRef other)
        {
            return Equals(_widgetAnimation, other._widgetAnimation) && _genericAnimation.Equals(other._genericAnimation);
        }

        public override bool Equals(object obj)
        {
            return obj is WidgetAnimationRef other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(_widgetAnimation, _genericAnimation);
        }
    }
}
