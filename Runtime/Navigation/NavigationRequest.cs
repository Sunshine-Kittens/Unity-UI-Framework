using System;
using System.Threading;

using UIFramework.Animation;
using UIFramework.Interfaces;
using UIFramework.WidgetTransition;

using UnityEngine;
using UnityEngine.Extension;

namespace UIFramework.Navigation
{
    public readonly ref struct NavigationRequest<TWidget> where TWidget : class, IWidget
    {
        private readonly INavigatorVersion _navigatorVersion;
        private readonly INavigationRequestProcessor<TWidget> _processor;
        private readonly TWidget _sourceWidget;
        private readonly TWidget _targetWidget;

        public TWidget Widget => _targetWidget;
        public readonly bool AddToHistory;
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
            AddToHistory = 1 << 4,
            Transition = 1 << 5,
            CancellationToken = 1 << 6
        }

        internal NavigationRequest(INavigatorVersion navigatorVersion, INavigationRequestProcessor<TWidget> processor, TWidget sourceWidget, 
            TWidget targetWidget)
        {
            _navigatorVersion = navigatorVersion;
            _version = navigatorVersion.Version;
            _processor = processor;
            
            _sourceWidget = sourceWidget;
            _targetWidget = targetWidget;
            
            Data = null;
            _animationRef = WidgetAnimationRef.None;
            _length = 0.0F;
            _easingMode = EasingMode.Linear;
            AddToHistory = true;
            _transitionParams = null;
            CancellationToken = CancellationToken.None;
            _flags = BuilderFlags.None;
        }
        
        private NavigationRequest(INavigatorVersion navigatorVersion, int version, INavigationRequestProcessor<TWidget> processor, TWidget sourceWidget, 
            TWidget targetWidget, object data, WidgetAnimationRef animation, float length, EasingMode easingMode, bool addToHistory, 
            VisibilityTransitionParams? transitionParams, CancellationToken cancellationToken, BuilderFlags flags)
        {
            _navigatorVersion = navigatorVersion;
            _version = version;
            _processor = processor;
            
            _sourceWidget = sourceWidget;
            _targetWidget = targetWidget;
            
            Data = data;
            _animationRef = animation;
            _length = length;
            _easingMode = easingMode;
            AddToHistory = addToHistory;
            _transitionParams = transitionParams;
            CancellationToken = cancellationToken;
            _flags = flags;
        }
        
        public bool IsValid()
        {
            return _navigatorVersion.Version ==  _version;
        }
        
        public NavigationRequest<TWidget> WithData(object data)
        {
            return new NavigationRequest<TWidget>(
                _navigatorVersion,
                _version,
                _processor,
                _sourceWidget,
                _targetWidget,
                data,
                _animationRef,
                _length,
                _easingMode,
                AddToHistory,
                _transitionParams,
                CancellationToken,
                _flags | BuilderFlags.Data 
            );
        }
        
        public NavigationRequest<TWidget> WithAnimation(WidgetAnimationRef animation)
        {
            if(_flags.HasFlag(BuilderFlags.Transition)) 
                throw new InvalidOperationException("Cannot define both an explicit animation and transition");
            return new NavigationRequest<TWidget>(
                _navigatorVersion,
                _version,
                _processor,
                _sourceWidget,
                _targetWidget,
                Data,
                animation,
                _length,
                _easingMode,
                AddToHistory,
                _transitionParams,
                CancellationToken,
                _flags | BuilderFlags.Animation 
            );
        }
        
        public NavigationRequest<TWidget> WithLength(float length)
        {
            if(_flags.HasFlag(BuilderFlags.Transition)) 
                throw new InvalidOperationException("Cannot define both an explicit length and transition");
            return new NavigationRequest<TWidget>(
                _navigatorVersion,
                _version,
                _processor,
                _sourceWidget,
                _targetWidget,
                Data,
                _animationRef,
                length,
                _easingMode,
                AddToHistory,
                _transitionParams,
                CancellationToken,
                _flags | BuilderFlags.Length 
            );
        }
        
        public NavigationRequest<TWidget> WithEasingMode(EasingMode easingMode)
        {
            if(_flags.HasFlag(BuilderFlags.Transition)) 
                throw new InvalidOperationException("Cannot define both an explicit easing mode and transition");
            return new NavigationRequest<TWidget>(
                _navigatorVersion,
                _version,
                _processor,
                _sourceWidget,
                _targetWidget,
                Data,
                _animationRef,
                _length,
                easingMode,
                AddToHistory,
                _transitionParams,
                CancellationToken,
                _flags | BuilderFlags.EasingMode 
            );
        }

        public NavigationRequest<TWidget> WithAddToHistory(bool addToHistory)
        {
            return new NavigationRequest<TWidget>(
                _navigatorVersion,
                _version,
                _processor,
                _sourceWidget,
                _targetWidget,
                Data,
                _animationRef,
                _length,
                _easingMode,
                addToHistory,
                _transitionParams,
                CancellationToken,
                _flags | BuilderFlags.AddToHistory 
            );
        }
        
        public NavigationRequest<TWidget> WithTransition(VisibilityTransitionParams transitionParams)
        {
            if(_flags.HasFlag(BuilderFlags.Animation)) 
                throw new InvalidOperationException("Cannot define both an explicit transition and animation");
            return new NavigationRequest<TWidget>(
                _navigatorVersion,
                _version,
                _processor,
                _sourceWidget,
                _targetWidget,
                Data,
                _animationRef,
                _length,
                _easingMode,
                AddToHistory,
                transitionParams,
                CancellationToken,
                _flags | BuilderFlags.Transition 
            );
        }
        
        public NavigationRequest<TWidget> WithCancellation(CancellationToken cancellationToken)
        {
            return new NavigationRequest<TWidget>(
                _navigatorVersion,
                _version,
                _processor,
                _sourceWidget,
                _targetWidget,
                Data,
                _animationRef,
                _length,
                _easingMode,
                AddToHistory,
                _transitionParams,
                cancellationToken,
                _flags | BuilderFlags.CancellationToken 
            );
        }
        
        public NavigationResponse<TWidget> Execute()
        {
            if (!IsValid())
                throw new InvalidOperationException("Unable to execute request, the request is no longer valid.");
            return _processor.ProcessNavigationRequest(in this);
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
