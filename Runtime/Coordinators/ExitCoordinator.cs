using UIFramework.Core.Interfaces;
using UIFramework.Navigation;
using UIFramework.Navigation.Interfaces;
using UIFramework.Transitioning;

using UnityEngine;
using UnityEngine.Extension;

namespace UIFramework.Coordinators
{
    public class ExitCoordinator<TWindow> : IExitNavigator<TWindow> where TWindow : class, IWindow
    {
        private readonly TimeMode _timeMode;
        private readonly WindowNavigator<TWindow> _windowNavigator;
        private readonly TransitionManager _transitionManager;
        
        public ExitCoordinator(TimeMode timeMode, WindowNavigator<TWindow> windowNavigator, TransitionManager transitionManager)
        {
            _timeMode = timeMode;
            _transitionManager = transitionManager;
            _windowNavigator = windowNavigator;
        }

        public NavigationResponse<TWindow> Exit(in ExitRequest request)
        {
            NavigationResult<TWindow> result = _windowNavigator.Clear();
            Awaitable awaitable = null;
            if (result.Success)
            {
                _transitionManager.Terminate();
                IWindow window = result.Previous;
                AnimationPlayable playable = GetAnimationPlayable(in request, window, _timeMode);
                if (playable.IsValid())
                {
                    awaitable = window.AnimateVisibility(WidgetVisibility.Visible, playable, InterruptBehavior.Immediate, request.CancellationToken);
                }
                else
                {
                    window.SetVisibility(WidgetVisibility.Hidden);
                }
                 
            }
            return new NavigationResponse<TWindow>(result, awaitable);
        }
        
        private bool IsRequestInstant(in ExitRequest request)
        {
            return (request.Length.HasValue && Mathf.Approximately(request.Length.Value, 0.0F)) || (!request.Length.HasValue && !request.Animation.IsValid);
        }
        
        private AnimationPlayable GetAnimationPlayable(in ExitRequest request, IWindow window, TimeMode timeMode)
        {
            if (IsRequestInstant(request)) return default;
            
            IAnimation animation = request.Animation.IsValid ?
                request.Animation.Resolve(window, WidgetVisibility.Hidden) : 
                window.GetDefaultAnimation(WidgetVisibility.Hidden);

            if (animation == null) return default;
            
            float length = request.Length.GetValueOrDefault(animation.Length);
            EasingMode easingMode = request.EasingMode.GetValueOrDefault(UnityEngine.Extension.EasingMode.Linear);
            
            return animation.ToPlayable()
                .WithLength(length)
                .WithEasingMode(easingMode)
                .WithTimeMode(timeMode)
                .Create();
        }
    }
}
