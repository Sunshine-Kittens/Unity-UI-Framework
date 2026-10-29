using UIFramework.Core.Interfaces;
using UIFramework.Navigation;
using UIFramework.Navigation.Interfaces;
using UIFramework.Transitioning;

using UnityEngine;
using UnityEngine.Extension;

namespace UIFramework.Coordinators
{
    public class ExitCoordinator<TWindow> : IExitNavigator<TWindow>
        where TWindow : class, IWindow
    {
        private readonly TimeMode _timeMode;
        private readonly WindowNavigator<TWindow> _navigator;
        private readonly TransitionManager _transitionManager;

        public ExitCoordinator(TimeMode timeMode, WindowNavigator<TWindow> navigator, TransitionManager transitionManager)
        {
            _timeMode = timeMode;
            _navigator = navigator;
            _transitionManager = transitionManager;
        }

        public NavigateToResponse<TWindow> Exit(in ExitRequest request)
        {
            NavigateToResult<TWindow> result = _navigator.Clear();
            Awaitable awaitable = null;
            if (result.Success)
            {
                _transitionManager.Terminate();
                TWindow window = result.Previous;
                AnimationPlayable playable = request.GetAnimation(window, _timeMode);
                if (playable.IsValid())
                {
                    awaitable = window.AnimateVisibility(WidgetVisibility.Hidden, playable,
                        InterruptBehavior.Immediate, request.CancellationToken);
                }
                else
                {
                    window.SetVisibility(WidgetVisibility.Hidden);
                }
            }
            return new NavigateToResponse<TWindow>(result, awaitable);
        }
    }
}