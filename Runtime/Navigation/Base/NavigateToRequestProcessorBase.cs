using System.Threading;

using UIFramework.Coordinators;
using UIFramework.Core;
using UIFramework.Core.Interfaces;
using UIFramework.Navigation.Context;
using UIFramework.Transitioning;

using UnityEngine;
using UnityEngine.Extension;

namespace UIFramework.Navigation.Base
{
    public class NavigateToRequestProcessorBase<TWindow, TContext, TResult>
        where TWindow : class, IWindow 
        where TContext : WindowContext<TWindow>
        where TResult : NavigateToResultBase<TWindow, TContext>, new()
    {
        protected readonly WindowNavigatorBase<TWindow, TContext, TResult> Navigator;
        
        private readonly TimeMode _timeMode;
        private readonly Core.History _history;
        private readonly TransitionManager _transitionManager;
        
        private protected NavigateToRequestProcessorBase(TimeMode timeMode, WindowNavigatorBase<TWindow, TContext, TResult> navigator, Core.History history, 
            TransitionManager transitionManager)
        {
            _timeMode = timeMode;
            Navigator = navigator;
            _history = history;
            _transitionManager = transitionManager;
        }
        
        protected Awaitable Navigate(VisibilityTransitionParams transition, TWindow target, TWindow source, object data, IHistoryEntry historyEntry, 
            CancellationToken cancellationToken)
        {
            Awaitable awaitable = null;
            if (data != null) target.SetData(data);
            if (source != null)
            {
                if (transition.Length > 0.0F && (transition.EntryAnimationRef.IsValid || transition.ExitAnimationRef.IsValid))
                {
                    awaitable = _transitionManager.Transition(transition, source, target, cancellationToken);
                }
                else
                {
                    awaitable = _transitionManager.Transition(Transition.None(), source, target, cancellationToken);
                }

                if (historyEntry != null)
                {
                    TransitionHistoryEvent historyEvent = new TransitionHistoryEvent(transition);
                    historyEntry.Append(historyEvent);
                    _history.CommitEntry(historyEntry.ID);
                }
            }
            else
            {
                if (transition.Length > 0.0F && transition.EntryAnimationRef.IsValid)
                {
                    AnimationPlayable playable = transition.EntryAnimationRef.Resolve(target, WidgetVisibility.Visible)
                        .ToPlayable()
                        .WithLength(transition.Length)
                        .WithEasingMode(transition.EasingMode)
                        .WithTimeMode(_timeMode)
                        .Create();
                    awaitable = target.AnimateVisibility(WidgetVisibility.Visible, playable, InterruptBehavior.Immediate, cancellationToken);
                }
                else
                {
                    target.SetVisibility(WidgetVisibility.Visible);
                }
            }
            return awaitable;
        }
    }
}
