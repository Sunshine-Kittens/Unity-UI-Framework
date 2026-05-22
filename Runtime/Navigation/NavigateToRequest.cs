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
    public readonly ref struct NavigateToRequest<TWindow> 
        where TWindow : class, IWindow
    {
        private readonly INavigationVersion _navigationVersion;
        private readonly INavigateToCoordinator<TWindow> _coordinator;
        private readonly TWindow _sourceWindow;
        private readonly TWindow _targetWindow;

        public TWindow Window => _targetWindow;
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

        internal NavigateToRequest(INavigationVersion navigationVersion, INavigateToCoordinator<TWindow> coordinator, TWindow sourceWindow, 
            TWindow targetWindow)
        {
            _navigationVersion = navigationVersion;
            _version = navigationVersion.Version;
            _coordinator = coordinator;
            
            _sourceWindow = sourceWindow;
            _targetWindow = targetWindow;
            
            Data = null;
            _animationRef = WidgetAnimationRef.None;
            _length = 0.0F;
            _easingMode = EasingMode.Linear;
            _transitionParams = null;
            CancellationToken = CancellationToken.None;
            _flags = BuilderFlags.None;
        }
        
        private NavigateToRequest(INavigationVersion navigationVersion, int version, INavigateToCoordinator<TWindow> coordinator, TWindow sourceWindow, 
            TWindow targetWindow, object data, WidgetAnimationRef animation, float length, EasingMode easingMode, VisibilityTransitionParams? transitionParams, 
            CancellationToken cancellationToken, BuilderFlags flags)
        {
            _navigationVersion = navigationVersion;
            _version = version;
            _coordinator = coordinator;
            
            _sourceWindow = sourceWindow;
            _targetWindow = targetWindow;
            
            Data = data;
            _animationRef = animation;
            _length = length;
            _easingMode = easingMode;
            _transitionParams = transitionParams;
            CancellationToken = cancellationToken;
            _flags = flags;
        }
        
        public bool IsValid()
        {
            return _navigationVersion.Version ==  _version;
        }
        
        public NavigateToRequest<TWindow> WithData(object data)
        {
            return new NavigateToRequest<TWindow>(
                _navigationVersion,
                _version,
                _coordinator,
                _sourceWindow,
                _targetWindow,
                data,
                _animationRef,
                _length,
                _easingMode,
                _transitionParams,
                CancellationToken,
                _flags | BuilderFlags.Data 
            );
        }
        
        public NavigateToRequest<TWindow> WithAnimation(WidgetAnimationRef animation)
        {
            if(_flags.HasFlag(BuilderFlags.Transition)) 
                throw new InvalidOperationException("Cannot define both an explicit animation and transition");
            return new NavigateToRequest<TWindow>(
                _navigationVersion,
                _version,
                _coordinator,
                _sourceWindow,
                _targetWindow,
                Data,
                animation,
                _length,
                _easingMode,
                _transitionParams,
                CancellationToken,
                _flags | BuilderFlags.Animation 
            );
        }
        
        public NavigateToRequest<TWindow> WithLength(float length)
        {
            if(_flags.HasFlag(BuilderFlags.Transition)) 
                throw new InvalidOperationException("Cannot define both an explicit length and transition");
            return new NavigateToRequest<TWindow>(
                _navigationVersion,
                _version,
                _coordinator,
                _sourceWindow,
                _targetWindow,
                Data,
                _animationRef,
                length,
                _easingMode,
                _transitionParams,
                CancellationToken,
                _flags | BuilderFlags.Length 
            );
        }
        
        public NavigateToRequest<TWindow> WithEasingMode(EasingMode easingMode)
        {
            if(_flags.HasFlag(BuilderFlags.Transition)) 
                throw new InvalidOperationException("Cannot define both an explicit easing mode and transition");
            return new NavigateToRequest<TWindow>(
                _navigationVersion,
                _version,
                _coordinator,
                _sourceWindow,
                _targetWindow,
                Data,
                _animationRef,
                _length,
                easingMode,
                _transitionParams,
                CancellationToken,
                _flags | BuilderFlags.EasingMode 
            );
        }
        
        public NavigateToRequest<TWindow> WithTransition(VisibilityTransitionParams transitionParams)
        {
            if(_flags.HasFlag(BuilderFlags.Animation)) 
                throw new InvalidOperationException("Cannot define both an explicit transition and animation");
            return new NavigateToRequest<TWindow>(
                _navigationVersion,
                _version,
                _coordinator,
                _sourceWindow,
                _targetWindow,
                Data,
                _animationRef,
                _length,
                _easingMode,
                transitionParams,
                CancellationToken,
                _flags | BuilderFlags.Transition 
            );
        }
        
        public NavigateToRequest<TWindow> WithCancellation(CancellationToken cancellationToken)
        {
            return new NavigateToRequest<TWindow>(
                _navigationVersion,
                _version,
                _coordinator,
                _sourceWindow,
                _targetWindow,
                Data,
                _animationRef,
                _length,
                _easingMode,
                _transitionParams,
                cancellationToken,
                _flags | BuilderFlags.CancellationToken 
            );
        }
        
        public NavigateToResponse<TWindow> Execute()
        {
            if (!IsValid())
                throw new InvalidOperationException("Unable to execute request, the request is no longer valid.");
            return _coordinator.NavigateTo(in this);
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
            
            float length = _flags.HasFlag(BuilderFlags.Length) ? _length :
                (entryAnimation?.Length ?? exitAnimation?.Length ?? 0.0f);
            EasingMode easingMode = _flags.HasFlag(BuilderFlags.EasingMode) ? _easingMode : EasingMode.Linear;
            
            return Transitioning.Transition.Custom(length, easingMode, exitAnimation, entryAnimation, TransitionSortPriority.Target);
        }
    }
}
