using System;
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
        private readonly History _history;
        private readonly TransitionManager _transitionManager;

        public NavigateToCoordinator(TimeMode timeMode, WindowNavigator<TWindow> navigator, History history, TransitionManager transitionManager)
        {
            _timeMode = timeMode;
            _navigator = navigator;
            _history = history;
            _transitionManager = transitionManager;
        }

        public NavigateToResponse<TWindow> NavigateTo(in NavigateToRequest<TWindow> request)
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
                historyEntry.Append(new NavigationHistoryEvent(previous.GetType()));
                historyEntry.Append(new TransitionHistoryEvent(transition));
            }

            Awaitable awaitable = Execute(transition, active, previous, request.Data, request.CancellationToken);

            if (historyEntry != null)
                _history.CommitEntry(historyEntry.ID);

            return new NavigateToResponse<TWindow>(result, awaitable);
        }

        private Awaitable Execute(VisibilityTransitionParams transition, TWindow target, TWindow source, object data, CancellationToken cancellationToken)
        {
            if (data != null)
            {
                if (!target.IsValidData(data))
                    throw new ArgumentException($"{target.GetType().Name} does not accept data of type {data.GetType().Name}.");
                target.SetData(data);
            }

            if (source != null)
            {
                bool hasTransition = transition.Length > 0.0F && (transition.EntryAnimationRef.IsValid || transition.ExitAnimationRef.IsValid);
                if(!hasTransition)
                    transition = Transition.None();
                return _transitionManager.Transition(transition, source, target, cancellationToken);
            }

            bool hasAnimation = transition.Length > 0.0F && transition.EntryAnimationRef.IsValid;
            if (hasAnimation)
            {
                AnimationPlayable playable = transition.EntryAnimationRef
                    .Resolve(target, WidgetVisibility.Visible)
                    .ToPlayable()
                    .WithLength(transition.Length)
                    .WithEasingMode(transition.EasingMode)
                    .WithTimeMode(_timeMode)
                    .Create();
                
                return target.AnimateVisibility(WidgetVisibility.Visible, playable, InterruptBehavior.Immediate, cancellationToken);
            }

            target.SetVisibility(WidgetVisibility.Visible);
            return null;
        }
    }
}
