using UIFramework.Interfaces;
using UIFramework.Navigation;
using UIFramework.WidgetTransition;

using UnityEngine;
using UnityEngine.Extension;

namespace UIFramework.Coordinators
{
    public class ExitCoordinator<TWidget> : IExitNavigator<TWidget> where TWidget : class, IWidget
    {
        private readonly TimeMode _timeMode;
        private readonly WidgetNavigator<TWidget> _widgetNavigator;
        private readonly TransitionManager _transitionManager;
        
        public ExitCoordinator(TimeMode timeMode, WidgetNavigator<TWidget> widgetNavigator, TransitionManager transitionManager)
        {
            _timeMode = timeMode;
            _transitionManager = transitionManager;
            _widgetNavigator = widgetNavigator;
        }

        public NavigationResponse<TWidget> Exit(in ExitRequest request)
        {
            NavigationResult<TWidget> result = _widgetNavigator.Clear();
            Awaitable awaitable = null;
            if (result.Success)
            {
                _transitionManager.Terminate();
                IWidget widget = result.Previous;
                AnimationPlayable playable = GetAnimationPlayable(in request, widget, _timeMode);
                if (playable.IsValid())
                {
                    awaitable = widget.AnimateVisibility(WidgetVisibility.Visible, playable, InterruptBehavior.Immediate, request.CancellationToken);
                }
                else
                {
                    widget.SetVisibility(WidgetVisibility.Hidden);
                }
                 
            }
            return new NavigationResponse<TWidget>(result, awaitable);
        }
        
        private bool IsRequestInstant(in ExitRequest request)
        {
            return (request.Length.HasValue && Mathf.Approximately(request.Length.Value, 0.0F)) || (!request.Length.HasValue && !request.Animation.IsValid);
        }
        
        private AnimationPlayable GetAnimationPlayable(in ExitRequest request, IWidget widget, TimeMode timeMode)
        {
            if (IsRequestInstant(request)) return default;
            
            IAnimation animation = request.Animation.IsValid ?
                request.Animation.Resolve(widget, WidgetVisibility.Hidden) : 
                widget.GetDefaultAnimation(WidgetVisibility.Hidden);

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
