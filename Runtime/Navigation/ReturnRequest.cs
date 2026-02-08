using System;
using System.Threading;

using UIFramework.Animation;
using UIFramework.Interfaces;
using UIFramework.WidgetTransition;

using UnityEngine;
using UnityEngine.Extension;

namespace UIFramework.Navigation
{
    public readonly ref struct ReturnRequest<TWidget> where TWidget : class, IWidget
    {
        private readonly IReturnRequestFactory<TWidget> _factory;
        private readonly IReturnRequestProcessor<TWidget> _processor;
        private readonly IWidget _sourceWidget;
        private readonly IWidget _targetWidget;
        
        public readonly CancellationToken CancellationToken;
        
        public VisibilityTransitionParams Transition => ResolveTransition();
        
        private readonly WidgetAnimationRef _animationRef;
        private readonly float _length;
        private readonly EasingMode _easingMode;
        private readonly VisibilityTransitionParams? _transitionParams;
        private readonly BuilderFlags _flags;
        
        public readonly int NavigationVersion;
        
        [Flags]
        private enum BuilderFlags : byte
        {
            None = 0,
            Animation = 1 << 0,
            Length = 1 << 1,
            EasingMode = 1 << 2,
            Transition = 1 << 3,
            CancellationToken = 1 << 4
        }

        internal ReturnRequest(IReturnRequestFactory<TWidget> factory, int navigationVersion, IReturnRequestProcessor<TWidget> processor, IWidget sourceWidget, IWidget targetWidget)
        {
            _factory = factory;
            NavigationVersion = navigationVersion;
            _processor = processor;
            
            _sourceWidget = sourceWidget;
            _targetWidget = targetWidget;
            
            _animationRef = WidgetAnimationRef.None;
            _length = 0.0F;
            _easingMode = EasingMode.Linear;
            _transitionParams = null;
            CancellationToken = CancellationToken.None;
            _flags = BuilderFlags.None;
        }
        
        private ReturnRequest(IReturnRequestFactory<TWidget> factory, int navigationVersion, IReturnRequestProcessor<TWidget> processor, IWidget sourceWidget, 
            IWidget targetWidget, WidgetAnimationRef animation, float length, EasingMode easingMode, VisibilityTransitionParams? transitionParams, 
            CancellationToken cancellationToken, BuilderFlags flags)
        {
            _factory = factory;
            NavigationVersion = navigationVersion;
            _processor = processor;
            
            _sourceWidget = sourceWidget;
            _targetWidget = targetWidget;
            
            _animationRef = animation;
            _length = length;
            _easingMode = easingMode;
            _transitionParams = transitionParams;
            CancellationToken = cancellationToken;
            _flags = flags;
        }
        
        public bool IsValid()
        {
            return _factory.IsRequestValid(in this);
        }
        
        public ReturnRequest<TWidget> WithAnimation(WidgetAnimationRef animation)
        {
            if(_flags.HasFlag(BuilderFlags.Transition)) 
                throw new InvalidOperationException("Cannot define both an explicit animation and transition");
            return new ReturnRequest<TWidget>(
                _factory,
                NavigationVersion,
                _processor,
                _sourceWidget,
                _targetWidget,
                animation,
                _length,
                _easingMode,
                _transitionParams,
                CancellationToken,
                _flags | BuilderFlags.Animation 
            );
        }
        
        public ReturnRequest<TWidget> WithLength(float length)
        {
            if(_flags.HasFlag(BuilderFlags.Transition)) 
                throw new InvalidOperationException("Cannot define both an explicit length and transition");
            return new ReturnRequest<TWidget>(
                _factory,
                NavigationVersion,
                _processor,
                _sourceWidget,
                _targetWidget,
                _animationRef,
                length,
                _easingMode,
                _transitionParams,
                CancellationToken,
                _flags | BuilderFlags.Length 
            );
        }
        
        public ReturnRequest<TWidget> WithEasingMode(EasingMode easingMode)
        {
            if(_flags.HasFlag(BuilderFlags.Transition)) 
                throw new InvalidOperationException("Cannot define both an explicit easing mode and transition");
            return new ReturnRequest<TWidget>(
                _factory,
                NavigationVersion,
                _processor,
                _sourceWidget,
                _targetWidget,
                _animationRef,
                _length,
                easingMode,
                _transitionParams,
                CancellationToken,
                _flags | BuilderFlags.EasingMode 
            );
        }
        
        public ReturnRequest<TWidget> WithTransition(VisibilityTransitionParams transitionParams)
        {
            if(_flags.HasFlag(BuilderFlags.Animation)) 
                throw new InvalidOperationException("Cannot define both an explicit transition and animation");
            return new ReturnRequest<TWidget>(
                _factory,
                NavigationVersion,
                _processor,
                _sourceWidget,
                _targetWidget,
                _animationRef,
                _length,
                _easingMode,
                transitionParams,
                CancellationToken,
                _flags | BuilderFlags.Transition 
            );
        }
        
        public ReturnRequest<TWidget> WithCancellation(CancellationToken cancellationToken)
        {
            return new ReturnRequest<TWidget>(
                _factory,
                NavigationVersion,
                _processor,
                _sourceWidget,
                _targetWidget,
                _animationRef,
                _length,
                _easingMode,
                _transitionParams,
                cancellationToken,
                _flags | BuilderFlags.CancellationToken 
            );
        }
        
        public NavigationResponse<TWidget> Execute()
        {
            if (!IsValid())
                throw new InvalidOperationException("Unable to execute request, the request is no longer valid.");
            return _processor.ProcessReturnRequest(in this);
        }
        
        private bool IsInstant()
        {
            return (_flags.HasFlag(BuilderFlags.Length) && Mathf.Approximately(_length, 0.0F)) ||
                (!_flags.HasFlag(BuilderFlags.Animation) && !_flags.HasFlag(BuilderFlags.Transition)) ||
                (_flags.HasFlag(BuilderFlags.Transition) && _transitionParams.GetValueOrDefault() == WidgetTransition.Transition.None());
        }
        
        private VisibilityTransitionParams ResolveTransition()
        {
            if (IsInstant()) return WidgetTransition.Transition.None();
            
            if (_flags.HasFlag(BuilderFlags.Transition) && _transitionParams.HasValue)
                return _transitionParams.Value;

            IAnimation exitAnimation = _sourceWidget.GetDefaultAnimation(WidgetVisibility.Hidden);
            IAnimation entryAnimation = _flags.HasFlag(BuilderFlags.Animation) ?
                _animationRef.Resolve(_targetWidget, WidgetVisibility.Visible) : 
                _targetWidget.GetDefaultAnimation(WidgetVisibility.Visible);

            if (exitAnimation == null && entryAnimation == null)
                return WidgetTransition.Transition.None();
            
            float length = _flags.HasFlag(BuilderFlags.Length) ? _length : entryAnimation.Length;
            EasingMode easingMode = _flags.HasFlag(BuilderFlags.EasingMode) ? _easingMode : EasingMode.Linear;
            
            return WidgetTransition.Transition.Custom(length, easingMode, exitAnimation, entryAnimation, TransitionSortPriority.Target);
        }
    }
}
