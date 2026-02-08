using System;
using System.Threading;

using UnityEngine;
using UnityEngine.Extension;

namespace UIFramework.Animation
{
    public readonly ref struct VisibilityAnimationBuilder
    {
        private readonly IWidget _widget;
        private readonly WidgetVisibility _visibility;
        
        private readonly IAnimation _animation;
        private readonly float _startTime;
        private readonly PlaybackMode _playbackMode;
        private readonly EasingMode _easingMode;
        private readonly TimeMode _timeMode;
        private readonly float _playbackSpeed;
        private readonly float _length;
        private readonly InterruptBehavior _interruptBehavior;
        private readonly CancellationToken _cancellationToken;
        private readonly BuilderFlags _flags;

        [Flags]
        private enum BuilderFlags : short
        {
            None = 0,
            Animation = 1 << 0,
            StartTime = 1 << 1,
            PlaybackMode = 1 << 2,
            EasingMode = 1 << 3,
            TimeMode = 1 << 4,
            PlaybackSpeed = 1 << 5,
            Length = 1 << 6,
            InterruptBehavior = 1 << 7,
            CancellationToken = 1 << 8
        }

        internal VisibilityAnimationBuilder(IWidget widget, WidgetVisibility visibility)
        {
            _widget = widget ?? throw new ArgumentNullException(nameof(widget));
            _visibility = visibility;
            
            _animation = null;
            _startTime = 0.0F;
            _playbackMode = PlaybackMode.Forward;
            _easingMode = EasingMode.Linear;
            _timeMode = TimeMode.Scaled;
            _playbackSpeed = 1.0F;
            _interruptBehavior = InterruptBehavior.Immediate;
            _cancellationToken = CancellationToken.None;
            _length = 1.0F;
            _flags = BuilderFlags.None;
        }
        
        private VisibilityAnimationBuilder(IWidget widget, WidgetVisibility visibility, IAnimation animation, float startTime, PlaybackMode playbackMode, 
            EasingMode easingMode, TimeMode timeMode, float playbackSpeed, InterruptBehavior interruptBehavior, CancellationToken cancellationToken,
            float length, BuilderFlags flags)
        {
            _widget = widget;
            _visibility = visibility;
            
            _animation = animation;
            _startTime = startTime;
            _playbackMode = playbackMode;
            _easingMode = easingMode;
            _timeMode = timeMode;
            _playbackSpeed = playbackSpeed;
            _interruptBehavior = interruptBehavior;
            _cancellationToken = cancellationToken;
            _length = length;
            _flags = flags;
        }
        
        public VisibilityAnimationBuilder WithAnimation(IAnimation animation)
        {
            return new VisibilityAnimationBuilder(
                _widget,
                _visibility,
                animation,
                _startTime,
                _playbackMode,
                _easingMode,
                _timeMode,
                _playbackSpeed,
                _interruptBehavior,
                _cancellationToken,
                _length,
                _flags | BuilderFlags.Animation 
            );
        }
        
        public VisibilityAnimationBuilder WithStartTime(float startTime)
        {
            return new VisibilityAnimationBuilder(
                _widget,
                _visibility,
                _animation,
                startTime,
                _playbackMode,
                _easingMode,
                _timeMode,
                _playbackSpeed,
                _interruptBehavior,
                _cancellationToken,
                _length,
                _flags | BuilderFlags.StartTime 
            );
        }

        public VisibilityAnimationBuilder WithPlaybackMode(PlaybackMode playbackMode)
        {
            return new VisibilityAnimationBuilder(
                _widget,
                _visibility,
                _animation,
                _startTime,
                playbackMode,
                _easingMode,
                _timeMode,
                _playbackSpeed,
                _interruptBehavior,
                _cancellationToken,
                _length,
                _flags | BuilderFlags.PlaybackMode 
            );
        }

        public VisibilityAnimationBuilder WithEasingMode(EasingMode easingMode)
        {
            return new VisibilityAnimationBuilder(
                _widget,
                _visibility,
                _animation,
                _startTime,
                _playbackMode,
                easingMode,
                _timeMode,
                _playbackSpeed,
                _interruptBehavior,
                _cancellationToken,
                _length,
                _flags | BuilderFlags.EasingMode
            );
        }

        public VisibilityAnimationBuilder WithTimeMode(TimeMode timeMode)
        {
            return new VisibilityAnimationBuilder(
                _widget,
                _visibility,
                _animation,
                _startTime,
                _playbackMode,
                _easingMode,
                timeMode,
                _playbackSpeed,
                _interruptBehavior,
                _cancellationToken,
                _length,
                _flags | BuilderFlags.TimeMode 
            );
        }

        public VisibilityAnimationBuilder WithPlaybackSpeed(float playbackSpeed)
        {
            return new VisibilityAnimationBuilder(
                _widget,
                _visibility,
                _animation,
                _startTime,
                _playbackMode,
                _easingMode,
                _timeMode,
                playbackSpeed,
                _interruptBehavior,
                _cancellationToken,
                _length,
                _flags | BuilderFlags.PlaybackSpeed
            );
        }

        public VisibilityAnimationBuilder WithLength(float length)
        {
            return new VisibilityAnimationBuilder(
                _widget,
                _visibility,
                _animation,
                _startTime,
                _playbackMode,
                _easingMode,
                _timeMode,
                _playbackSpeed,
                _interruptBehavior,
                _cancellationToken,
                length,
                _flags | BuilderFlags.Length 
            );
        }
        
        public VisibilityAnimationBuilder WithInterruptBehavior(InterruptBehavior interruptBehavior)
        {
            return new VisibilityAnimationBuilder(
                _widget,
                _visibility,
                _animation,
                _startTime,
                _playbackMode,
                _easingMode,
                _timeMode,
                _playbackSpeed,
                interruptBehavior,
                _cancellationToken,
                _length,
                _flags | BuilderFlags.InterruptBehavior 
            );
        }
        
        public VisibilityAnimationBuilder WithCancellation(CancellationToken cancellationToken)
        {
            return new VisibilityAnimationBuilder(
                _widget,
                _visibility,
                _animation,
                _startTime,
                _playbackMode,
                _easingMode,
                _timeMode,
                _playbackSpeed,
                _interruptBehavior,
                cancellationToken,
                _length,
                _flags | BuilderFlags.CancellationToken 
            );
        }
        
        public Awaitable Animate()
        {
            AnimationPlayable playable = CreatePlayable();
            InterruptBehavior interruptBehavior = _flags.HasFlag(BuilderFlags.InterruptBehavior) ? _interruptBehavior : InterruptBehavior.Immediate;
            CancellationToken cancellationToken = _flags.HasFlag(BuilderFlags.CancellationToken) ? _cancellationToken : CancellationToken.None;
            return _widget.AnimateVisibility(_visibility, playable, interruptBehavior, cancellationToken);
        }

        private AnimationPlayable CreatePlayable()
        {
            IAnimation animation = _flags.HasFlag(BuilderFlags.Animation) ? _animation : _widget.GetDefaultAnimation(_visibility);
            if (animation == null)
            {
                throw new InvalidOperationException(
                    $"Cannot animate visibility to {_visibility} without an explicit animation or default widget configured");
            }
            
            float startTime = _flags.HasFlag(BuilderFlags.StartTime) ? _startTime : 0.0F;
            PlaybackMode playbackMode = _flags.HasFlag(BuilderFlags.PlaybackMode) ? _playbackMode : PlaybackMode.Forward;
            EasingMode easingMode = _flags.HasFlag(BuilderFlags.EasingMode) ? _easingMode : EasingMode.Linear;
            TimeMode timeMode = _flags.HasFlag(BuilderFlags.TimeMode) ? _timeMode : TimeMode.Scaled;
            float playbackSpeed = _flags.HasFlag(BuilderFlags.PlaybackSpeed) ? _playbackSpeed : 1.0F;
            
            if (_flags.HasFlag(BuilderFlags.Length))
            {
                playbackSpeed = (animation.Length / _length) * playbackSpeed; 
            }
            return new AnimationPlayable(animation, startTime, playbackMode, easingMode, timeMode, playbackSpeed);
        }
    }
}