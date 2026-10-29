using System;

using UIFramework.Animation;

using UnityEngine;
using UnityEngine.Extension;

namespace UIFramework.Transitioning
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
        public readonly EasingMode EasingMode;
        public readonly WidgetAnimationRef ExitAnimationRef;
        public readonly WidgetAnimationRef EntryAnimationRef;
        public readonly TransitionSortPriority SortPriority;
        private readonly int _lengthMs;

        public TransitionTarget Target
        {
            get
            {
                if(ExitAnimationRef.IsValid && EntryAnimationRef.IsValid)
                {
                    return TransitionTarget.Both;
                }
                if(ExitAnimationRef.IsValid)
                {
                    return TransitionTarget.Source;
                }
                if (EntryAnimationRef.IsValid)
                {
                    return TransitionTarget.Target;
                }
                return TransitionTarget.None;
            }
        }

        internal VisibilityTransitionParams(float length, EasingMode easingMode, in WidgetAnimationRef exitAnimationRef, in WidgetAnimationRef entryAnimationRef, 
            TransitionSortPriority transitionSortPriority)
        {
            Length = length;
            _lengthMs = SecondsToMilliseconds(length);
            EasingMode = easingMode;
            ExitAnimationRef = exitAnimationRef;
            EntryAnimationRef = entryAnimationRef;
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
            return _lengthMs == other._lengthMs && EasingMode == other.EasingMode && ExitAnimationRef.Equals(other.ExitAnimationRef) && 
                EntryAnimationRef.Equals(other.EntryAnimationRef) && SortPriority == other.SortPriority;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(_lengthMs, EasingMode, ExitAnimationRef, EntryAnimationRef);
        }

        public static bool operator ==(VisibilityTransitionParams lhs, VisibilityTransitionParams rhs)
        {
            return lhs.Equals(rhs);
        }

        public static bool operator !=(VisibilityTransitionParams lhs, VisibilityTransitionParams rhs)
        {
            return !(lhs == rhs);
        }
        
        public VisibilityTransitionParams Invert()
        {
            return new VisibilityTransitionParams(Length, EasingMode, EntryAnimationRef, ExitAnimationRef, GetInvertedSortPriority(SortPriority));
        }
        
        public VisibilityTransitionParams Invert(float length)
        {
            return new VisibilityTransitionParams(length, EasingMode, EntryAnimationRef, ExitAnimationRef, GetInvertedSortPriority(SortPriority));
        }
        
        public VisibilityTransitionParams Invert(float length, EasingMode easingMode)
        {
            return new VisibilityTransitionParams(length, easingMode, EntryAnimationRef, ExitAnimationRef, GetInvertedSortPriority(SortPriority));
        }

        private TransitionSortPriority GetInvertedSortPriority(TransitionSortPriority sortPriority)
        {
            switch (sortPriority)
            {
                case TransitionSortPriority.Source:
                    return TransitionSortPriority.Target;
                case TransitionSortPriority.Target:
                    return TransitionSortPriority.Source;
            }
            return sortPriority;
        }
    }
}
