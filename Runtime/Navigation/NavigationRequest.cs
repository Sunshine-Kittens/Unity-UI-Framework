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
    public readonly ref struct NavigationRequest<TWindow> where TWindow : class, IWindow
    {
        private readonly INavigatorVersion _navigatorVersion;
        private readonly INavigationRequestProcessor<TWindow> _processor;
        private readonly TWindow _sourceWindow;
        private readonly TWindow _targetWindow;

        public TWindow Window => _targetWindow;
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

        internal NavigationRequest(INavigatorVersion navigatorVersion, INavigationRequestProcessor<TWindow> processor, TWindow sourceWindow, 
            TWindow targetWindow)
        {
            _navigatorVersion = navigatorVersion;
            _version = navigatorVersion.Version;
            _processor = processor;
            
            _sourceWindow = sourceWindow;
            _targetWindow = targetWindow;
            
            Data = null;
            _animationRef = WidgetAnimationRef.None;
            _length = 0.0F;
            _easingMode = EasingMode.Linear;
            AddToHistory = true;
            _transitionParams = null;
            CancellationToken = CancellationToken.None;
            _flags = BuilderFlags.None;
        }
        
        private NavigationRequest(INavigatorVersion navigatorVersion, int version, INavigationRequestProcessor<TWindow> processor, TWindow sourceWindow, 
            TWindow targetWindow, object data, WidgetAnimationRef animation, float length, EasingMode easingMode, bool addToHistory, 
            VisibilityTransitionParams? transitionParams, CancellationToken cancellationToken, BuilderFlags flags)
        {
            _navigatorVersion = navigatorVersion;
            _version = version;
            _processor = processor;
            
            _sourceWindow = sourceWindow;
            _targetWindow = targetWindow;
            
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
        
        public NavigationRequest<TWindow> WithData(object data)
        {
            return new NavigationRequest<TWindow>(
                _navigatorVersion,
                _version,
                _processor,
                _sourceWindow,
                _targetWindow,
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
        
        public NavigationRequest<TWindow> WithAnimation(WidgetAnimationRef animation)
        {
            if(_flags.HasFlag(BuilderFlags.Transition)) 
                throw new InvalidOperationException("Cannot define both an explicit animation and transition");
            return new NavigationRequest<TWindow>(
                _navigatorVersion,
                _version,
                _processor,
                _sourceWindow,
                _targetWindow,
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
        
        public NavigationRequest<TWindow> WithLength(float length)
        {
            if(_flags.HasFlag(BuilderFlags.Transition)) 
                throw new InvalidOperationException("Cannot define both an explicit length and transition");
            return new NavigationRequest<TWindow>(
                _navigatorVersion,
                _version,
                _processor,
                _sourceWindow,
                _targetWindow,
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
        
        public NavigationRequest<TWindow> WithEasingMode(EasingMode easingMode)
        {
            if(_flags.HasFlag(BuilderFlags.Transition)) 
                throw new InvalidOperationException("Cannot define both an explicit easing mode and transition");
            return new NavigationRequest<TWindow>(
                _navigatorVersion,
                _version,
                _processor,
                _sourceWindow,
                _targetWindow,
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

        public NavigationRequest<TWindow> WithAddToHistory(bool addToHistory)
        {
            return new NavigationRequest<TWindow>(
                _navigatorVersion,
                _version,
                _processor,
                _sourceWindow,
                _targetWindow,
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
        
        public NavigationRequest<TWindow> WithTransition(VisibilityTransitionParams transitionParams)
        {
            if(_flags.HasFlag(BuilderFlags.Animation)) 
                throw new InvalidOperationException("Cannot define both an explicit transition and animation");
            return new NavigationRequest<TWindow>(
                _navigatorVersion,
                _version,
                _processor,
                _sourceWindow,
                _targetWindow,
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
        
        public NavigationRequest<TWindow> WithCancellation(CancellationToken cancellationToken)
        {
            return new NavigationRequest<TWindow>(
                _navigatorVersion,
                _version,
                _processor,
                _sourceWindow,
                _targetWindow,
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
        
        public NavigationResponse<TWindow> Execute()
        {
            if (!IsValid())
                throw new InvalidOperationException("Unable to execute request, the request is no longer valid.");
            return _processor.ProcessNavigationRequest(in this);
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

            IAnimation exitAnimation = _sourceWindow.GetDefaultAnimation(WidgetVisibility.Hidden);
            IAnimation entryAnimation = _flags.HasFlag(BuilderFlags.Animation) ?
                _animationRef.Resolve(_targetWindow, WidgetVisibility.Visible) : 
                _targetWindow.GetDefaultAnimation(WidgetVisibility.Visible);

            if (exitAnimation == null && entryAnimation == null)
                return Transitioning.Transition.None();
            
            float length = _flags.HasFlag(BuilderFlags.Length) ? _length : entryAnimation.Length;
            EasingMode easingMode = _flags.HasFlag(BuilderFlags.EasingMode) ? _easingMode : EasingMode.Linear;
            
            return Transitioning.Transition.Custom(length, easingMode, exitAnimation, entryAnimation, TransitionSortPriority.Target);
        }
    }
}
