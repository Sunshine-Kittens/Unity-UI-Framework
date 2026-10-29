using System.Threading;

using UIFramework.Animation;
using UIFramework.Core.Interfaces;

using UnityEngine;
using UnityEngine.Extension;

namespace UIFramework.Navigation
{
    public readonly ref struct ExitRequest
    {
        public WidgetAnimationRef Animation { get; init; }
        public float? Length { get; init; }
        public EasingMode? EasingMode { get; init; }
        public CancellationToken CancellationToken { get; init; }
        
        private bool IsInstant()
        {
            return (Length.HasValue && Mathf.Approximately(Length.Value, 0.0F)) || (!Length.HasValue && !Animation.IsValid);
        }
        
        public AnimationPlayable GetAnimation(IWindow window, TimeMode timeMode)
        {
            if (IsInstant()) return default;
            
            IAnimation animation = Animation.IsValid ?
                Animation.Resolve(window, WidgetVisibility.Hidden) : 
                window.GetDefaultAnimation(WidgetVisibility.Hidden);

            if (animation == null) return default;
            
            float length = Length.GetValueOrDefault(animation.Length);
            EasingMode easingMode = EasingMode.GetValueOrDefault(UnityEngine.Extension.EasingMode.Linear);
            
            return animation.ToPlayable()
                .WithLength(length)
                .WithEasingMode(easingMode)
                .WithTimeMode(timeMode)
                .Create();
        }
    }
}
