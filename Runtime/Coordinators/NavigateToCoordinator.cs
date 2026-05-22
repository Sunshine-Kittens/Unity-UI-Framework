using System.Threading;

using UIFramework.Core;
using UIFramework.Core.Interfaces;
using UIFramework.Navigation;
using UIFramework.Navigation.History;
using UIFramework.Navigation.Interfaces;
using UIFramework.Transitioning;

using UnityEngine;
using UnityEngine.Extension;

namespace UIFramework.Coordinators
{
    public class NavigateToCoordinator<TWindow> : INavigateToCoordinator<TWindow>
        where TWindow : class, IWindow
    {
        private readonly TimeMode _timeMode;
        private readonly WindowNavigator<TWindow> _navigator;
        private readonly Core.History _history;
        private readonly TransitionManager _transitionManager;

        public NavigateToCoordinator(TimeMode timeMode, WindowNavigator<TWindow> navigator,
            Core.History history, TransitionManager transitionManager)
        {
            _timeMode = timeMode;
            _navigator = navigator;
            _history = history;
            _transitionManager = transitionManager;
        }

        public NavigateToResponse<TWindow> Navigate(in NavigateToRequest<TWindow> request)
        {
            NavigateToResult<TWindow> result = _navigator.NavigateTo(request.Window);
            if (!result.Success)
                return new NavigateToResponse<TWindow>(result, null);

            TWindow previous = result.Previous;
            TWindow active = result.Active;
            VisibilityTransitionParams transition = request.Transition;

            IHistoryEntry historyEntry = null;
            if (previous != null && _history != null)
            {
                historyEntry = _history.PushNewEntry();
                // Record where we came FROM so Return() can navigate back there.
                historyEntry.Append(new NavigationHistoryEvent(previous.GetType()));
            }

            Awaitable awaitable = Execute(transition, active, previous, request.Data, request.CancellationToken);

            if (historyEntry != null)
            {
                historyEntry.Append(new TransitionHistoryEvent(transition));
                _history.CommitEntry(historyEntry.ID);
            }

            return new NavigateToResponse<TWindow>(result, awaitable);
        }

        private Awaitable Execute(VisibilityTransitionParams transition, TWindow target, TWindow source,
            object data, CancellationToken cancellationToken)
        {
            if (data != null) target.SetData(data);

            if (source != null)
            {
                bool hasAnimation = transition.Length > 0.0F &&
                    (transition.EntryAnimationRef.IsValid || transition.ExitAnimationRef.IsValid);
                return _transitionManager.Transition(
                    hasAnimation ? transition : Transitioning.Transition.None(),
                    source, target, cancellationToken);
            }

            if (transition.Length > 0.0F && transition.EntryAnimationRef.IsValid)
            {
                AnimationPlayable playable = transition.EntryAnimationRef
                    .Resolve(target, WidgetVisibility.Visible)
                    .ToPlayable()
                    .WithLength(transition.Length)
                    .WithEasingMode(transition.EasingMode)
                    .WithTimeMode(_timeMode)
                    .Create();
                return target.AnimateVisibility(WidgetVisibility.Visible, playable,
                    InterruptBehavior.Immediate, cancellationToken);
            }

            target.SetVisibility(WidgetVisibility.Visible);
            return null;
        }
    }
}
