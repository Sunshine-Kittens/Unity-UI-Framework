using System;
using System.Threading;

using UIFramework.Animation;
using UIFramework.Core.Interfaces;
using UIFramework.Navigation.Interfaces;
using UIFramework.Transitioning;

using UnityEngine;
using UnityEngine.Extension;

namespace UIFramework.Navigation
{
    public readonly ref struct ActivateRequest<TWidget> where TWidget : class, IWidget
    {
        private readonly IActivatorVersion _activatorVersion;
        private readonly IActivateRequestProcessor<TWidget> _processor;
        private readonly TWidget _sourceWidget;
        private readonly TWidget _targetWidget;

        public TWidget Widget => _targetWidget;
        public readonly object Data;
        public readonly CancellationToken CancellationToken;
        
        public VisibilityTransitionParams Transition => ResolveTransition();
        
        private readonly WidgetAnimationRef _animationRef;
        private readonly float _length;
        private readonly EasingMode _easingMode;
        private readonly VisibilityTransitionParams? _transitionParams;
        private readonly BuilderFlags _flags;
        private readonly int _version;
        
        [Flags]
        private enum BuilderFlags : byte
        {
            None = 0,
            Data = 1 << 0,
            Animation = 1 << 1,
            Length = 1 << 2,
            EasingMode = 1 << 3,
            Transition = 1 << 4,
            CancellationToken = 1 << 5
        }

        internal ActivateRequest(IActivatorVersion activatorVersion, IActivateRequestProcessor<TWidget> processor, TWidget sourceWidget, TWidget targetWidget)
        {
            _activatorVersion = activatorVersion;
            _version = activatorVersion.Version;
            _processor = processor;
            
            _sourceWidget = sourceWidget;
            _targetWidget = targetWidget;
            
            Data = null;
            _animationRef = WidgetAnimationRef.None;
            _length = 0.0F;
            _easingMode = EasingMode.Linear;
            _transitionParams = null;
            CancellationToken = CancellationToken.None;
            _flags = BuilderFlags.None;
        }
        
        private ActivateRequest(IActivatorVersion activatorVersion, int version, IActivateRequestProcessor<TWidget> processor, TWidget sourceWidget, TWidget targetWidget, object data, WidgetAnimationRef animation, float length, 
            EasingMode easingMode, VisibilityTransitionParams? transitionParams, CancellationToken cancellationToken, BuilderFlags flags)
        {
            _activatorVersion = activatorVersion;
            _version = version;
            _processor = processor;
            
            _sourceWidget = sourceWidget;
            _targetWidget = targetWidget;
            
            Data = data;
            _animationRef = animation;
            _length = length;
            _easingMode = easingMode;
            _transitionParams = transitionParams;
            CancellationToken = cancellationToken;
            _flags = flags;
        }
        
        public ActivateRequest<TWidget> WithData(object data)
        {
            return new ActivateRequest<TWidget>(
                _activatorVersion,
                _version,
                _processor,
                _sourceWidget,
                _targetWidget,
                data,
                _animationRef,
                _length,
                _easingMode,
                _transitionParams,
                CancellationToken,
                _flags | BuilderFlags.Data 
            );
        }
        
        public ActivateRequest<TWidget> WithAnimation(WidgetAnimationRef animation)
        {
            if(_flags.HasFlag(BuilderFlags.Transition)) 
                throw new InvalidOperationException("Cannot define both an explicit animation and transition");
            return new ActivateRequest<TWidget>(
                _activatorVersion,
                _version,
                _processor,
                _sourceWidget,
                _targetWidget,
                Data,
                animation,
                _length,
                _easingMode,
                _transitionParams,
                CancellationToken,
                _flags | BuilderFlags.Animation 
            );
        }
        
        public ActivateRequest<TWidget> WithLength(float length)
        {
            if(_flags.HasFlag(BuilderFlags.Transition)) 
                throw new InvalidOperationException("Cannot define both an explicit length and transition");
            return new ActivateRequest<TWidget>(
                _activatorVersion,
                _version,
                _processor,
                _sourceWidget,
                _targetWidget,
                Data,
                _animationRef,
                length,
                _easingMode,
                _transitionParams,
                CancellationToken,
                _flags | BuilderFlags.Length 
            );
        }
        
        public ActivateRequest<TWidget> WithEasingMode(EasingMode easingMode)
        {
            if(_flags.HasFlag(BuilderFlags.Transition)) 
                throw new InvalidOperationException("Cannot define both an explicit easing mode and transition");
            return new ActivateRequest<TWidget>(
                _activatorVersion,
                _version,
                _processor,
                _sourceWidget,
                _targetWidget,
                Data,
                _animationRef,
                _length,
                easingMode,
                _transitionParams,
                CancellationToken,
                _flags | BuilderFlags.EasingMode 
            );
        }
        
        public ActivateRequest<TWidget> WithTransition(VisibilityTransitionParams transitionParams)
        {
            if(_flags.HasFlag(BuilderFlags.Animation)) 
                throw new InvalidOperationException("Cannot define both an explicit transition and animation");
            return new ActivateRequest<TWidget>(
                _activatorVersion,
                _version,
                _processor,
                _sourceWidget,
                _targetWidget,
                Data,
                _animationRef,
                _length,
                _easingMode,
                transitionParams,
                CancellationToken,
                _flags | BuilderFlags.Transition 
            );
        }
        
        public ActivateRequest<TWidget> WithCancellation(CancellationToken cancellationToken)
        {
            return new ActivateRequest<TWidget>(
                _activatorVersion,
                _version,
                _processor,
                _sourceWidget,
                _targetWidget,
                Data,
                _animationRef,
                _length,
                _easingMode,
                _transitionParams,
                cancellationToken,
                _flags | BuilderFlags.CancellationToken 
            );
        }
        
        public ActivateResponse<TWidget> Execute()
        {
            return _processor.ProcessActivateRequest(in this);
        }
        
        private bool IsInstant()
        {
            return (_flags.HasFlag(BuilderFlags.Length) && Mathf.Approximately(_length, 0.0F)) ||
                (!_flags.HasFlag(BuilderFlags.Animation) && !_flags.HasFlag(BuilderFlags.Transition)) ||
                (_flags.HasFlag(BuilderFlags.Transition) && _transitionParams.GetValueOrDefault() == Transitioning.Transition.None());
        }
        
        private VisibilityTransitionParams ResolveTransition()
        {
            if (IsInstant()) return Transitioning.Transition.None();
            
            if (_flags.HasFlag(BuilderFlags.Transition) && _transitionParams.HasValue)
                return _transitionParams.Value;

            IAnimation exitAnimation = _sourceWidget.GetDefaultAnimation(WidgetVisibility.Hidden);
            IAnimation entryAnimation = _flags.HasFlag(BuilderFlags.Animation) ?
                _animationRef.Resolve(_targetWidget, WidgetVisibility.Visible) : 
                _targetWidget.GetDefaultAnimation(WidgetVisibility.Visible);

            if (exitAnimation == null && entryAnimation == null)
                return Transitioning.Transition.None();
            
            float length = _flags.HasFlag(BuilderFlags.Length) ? _length : entryAnimation.Length;
            EasingMode easingMode = _flags.HasFlag(BuilderFlags.EasingMode) ? _easingMode : EasingMode.Linear;
            
            return Transitioning.Transition.Custom(length, easingMode, exitAnimation, entryAnimation, TransitionSortPriority.Target);
        }
    }
}
