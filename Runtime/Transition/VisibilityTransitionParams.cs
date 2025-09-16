using System;

using UnityEngine;
using UnityEngine.Extension;

namespace UIFramework
{
    public enum TransitionSortPriority
    {
        Auto,
        Source,
        Target
    }
    
    public enum TransitionTarget    
    {
        None,
        Source,
        Target,
        Both
    }
    
    public readonly struct VisibilityTransitionParams : IEquatable<VisibilityTransitionParams>
    {
        public readonly float Length;
        public readonly int LengthMs;
        public readonly EasingMode EasingMode;
        public readonly ImplicitAnimation ExitAnimation;
        public readonly ImplicitAnimation EntryAnimation;
        public readonly TransitionSortPriority SortPriority;

        public TransitionTarget Target
        {
            get
            {
                if(ExitAnimation.IsValid && EntryAnimation.IsValid)
                {
                    return TransitionTarget.Both;
                }
                if(ExitAnimation.IsValid)
                {
                    return TransitionTarget.Source;
                }
                if (EntryAnimation.IsValid)
                {
                    return TransitionTarget.Target;
                }
                return TransitionTarget.None;
            }
        }

        internal VisibilityTransitionParams(float length, EasingMode easingMode, ImplicitAnimation exitAnimation, ImplicitAnimation entryAnimation, 
            TransitionSortPriority transitionSortPriority)
        {
            Length = length;
            LengthMs = SecondsToMilliseconds(length);
            EasingMode = easingMode;
            ExitAnimation = exitAnimation;
            EntryAnimation = entryAnimation;
            SortPriority = transitionSortPriority;
        }

        private static int SecondsToMilliseconds(float seconds)
        {
            return Mathf.RoundToInt(seconds * 1000);
        }

        public override bool Equals(object obj)
        {
            return obj is VisibilityTransitionParams other && Equals(other);
        }

        public bool Equals(VisibilityTransitionParams other)
        {
            return LengthMs == other.LengthMs && EasingMode == other.EasingMode && ExitAnimation.Equals(other.ExitAnimation) && 
                EntryAnimation.Equals(other.EntryAnimation);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(LengthMs, EasingMode, ExitAnimation, EntryAnimation);
        }

        public static bool operator ==(VisibilityTransitionParams lhs, VisibilityTransitionParams rhs)
        {
            return lhs.Equals(rhs);
        }

        public static bool operator !=(VisibilityTransitionParams lhs, VisibilityTransitionParams rhs)
        {
            return !(lhs == rhs);
        }
    }
}
