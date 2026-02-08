using System;
using System.Threading;

using UIFramework.Animation;
using UIFramework.Interfaces;

using UnityEngine;
using UnityEngine.Extension;

namespace UIFramework.Navigation
{
    public readonly ref struct ExitRequest<TWidget> where TWidget : class, IWidget
    {
        private readonly IExitRequestFactory<TWidget> _factory;
        private readonly IExitRequestProcessor<TWidget> _processor;
        private readonly IWidget _activeWidget;
        
        public readonly CancellationToken CancellationToken;
        
        private readonly WidgetAnimationRef _animationRef;
        private readonly float _length;
        private readonly EasingMode _easingMode;
        private readonly BuilderFlags _flags;

        public readonly int NavigationVersion;

        [Flags]
        private enum BuilderFlags : byte
        {
            None = 0,
            Animation = 1 << 0,
            Length = 1 << 1,
            EasingMode = 1 << 2,
            CancellationToken = 1 << 4
        }

        internal ExitRequest(IExitRequestFactory<TWidget> factory, int navigationVersion, IExitRequestProcessor<TWidget> processor, IWidget activeWidget)
        {
            NavigationVersion = navigationVersion;
            _factory = factory;
            _processor = processor;
            
            _activeWidget = activeWidget;
            
            _animationRef = WidgetAnimationRef.None;
            _length = 0.0F;
            _easingMode = EasingMode.Linear;
            CancellationToken = CancellationToken.None;
            _flags = BuilderFlags.None;
        }
        
        private ExitRequest(IExitRequestFactory<TWidget> factory, int navigationVersion, IExitRequestProcessor<TWidget> processor, IWidget activeWidget, WidgetAnimationRef animation, 
            float length, EasingMode easingMode, CancellationToken cancellationToken, BuilderFlags flags)
        {
            NavigationVersion = navigationVersion;
            _factory = factory;
            _processor = processor;
            _activeWidget = activeWidget;
            _animationRef = animation;
            _length = length;
            _easingMode = easingMode;
            CancellationToken = cancellationToken;
            _flags = flags;
        }

        public bool IsValid()
        {
            return _factory.IsRequestValid(in this);
        }
        
        public ExitRequest<TWidget> WithAnimation(WidgetAnimationRef animation)
        {
            return new ExitRequest<TWidget>(
                _factory,
                NavigationVersion,
                _processor,
                _activeWidget, 
                animation,
                _length,
                _easingMode,
                CancellationToken,
                _flags | BuilderFlags.Animation 
            );
        }
        
        public ExitRequest<TWidget> WithLength(float length)
        {
            return new ExitRequest<TWidget>(
                _factory,
                NavigationVersion,
                _processor,
                _activeWidget,
                _animationRef,
                length,
                _easingMode,
                CancellationToken,
                _flags | BuilderFlags.Length 
            );
        }
        
        public ExitRequest<TWidget> WithEasingMode(EasingMode easingMode)
        {
            return new ExitRequest<TWidget>(
                _factory,
                NavigationVersion,
                _processor,
                _activeWidget,
                _animationRef,
                _length,
                easingMode,
                CancellationToken,
                _flags | BuilderFlags.EasingMode 
            );
        }
        
        public ExitRequest<TWidget> WithCancellation(CancellationToken cancellationToken)
        {
            return new ExitRequest<TWidget>(
                _factory,
                NavigationVersion,
                _processor,
                _activeWidget,
                _animationRef,
                _length,
                _easingMode,
                cancellationToken,
                _flags | BuilderFlags.CancellationToken 
            );
        }
        
        public ExitResponse Execute()
        {
            if (!IsValid())
                throw new InvalidOperationException("Unable to execute request, the request is no longer valid.");
            return _processor.ProcessExitRequest(in this);
        }
        
        private bool IsInstant()
        {
            return (_flags.HasFlag(BuilderFlags.Length) && Mathf.Approximately(_length, 0.0F)) || !_flags.HasFlag(BuilderFlags.Animation);
        }
        
        public AnimationPlayable GetAnimation(TimeMode timeMode)
        {
            if (IsInstant()) return default;
            
            IAnimation animation = _flags.HasFlag(BuilderFlags.Animation) ?
                _animationRef.Resolve(_activeWidget, WidgetVisibility.Hidden) : 
                _activeWidget.GetDefaultAnimation(WidgetVisibility.Hidden);

            if (animation == null) return default;
            
            float length = _flags.HasFlag(BuilderFlags.Length) ? _length : animation.Length;
            EasingMode easingMode = _flags.HasFlag(BuilderFlags.EasingMode) ? _easingMode : EasingMode.Linear;
            
            return animation.ToPlayable()
                .WithLength(length)
                .WithEasingMode(easingMode)
                .WithTimeMode(timeMode)
                .Create();
        }
    }
}
