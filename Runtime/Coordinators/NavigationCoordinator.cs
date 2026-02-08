using System.Threading;

using UIFramework.Interfaces;
using UIFramework.Navigation;
using UIFramework.Registry;
using UIFramework.WidgetTransition;

using UnityEngine;
using UnityEngine.Extension;

namespace UIFramework.Coordinators
{
    public readonly struct TransitionHistoryEvent : IHistoryEvent
    {
        public readonly VisibilityTransitionParams Transition;

        public TransitionHistoryEvent(VisibilityTransitionParams transition)
        {
            Transition = transition;
        }
    }
    
    public class NavigationCoordinator<TWidget> : INavigationRequestFactory<TWidget>, INavigationRequestProcessor<TWidget> where TWidget : class, IWidget
    {
        private readonly TimeMode _timeMode;
        private readonly WidgetRegistry<TWidget> _registry;
        private readonly NavigationManager<TWidget> _navigationManager;
        private readonly History _history;
        private readonly TransitionManager _transitionManager;
        
        public NavigationCoordinator(TimeMode timeMode, WidgetRegistry<TWidget> registry, NavigationManager<TWidget> navigationManager,
            History history, TransitionManager transitionManager)
        {
            _timeMode = timeMode;
            _registry = registry;
            _navigationManager = navigationManager;
            _history = history;
            _transitionManager = transitionManager;
        }

        public bool IsRequestValid(in NavigationRequest<TWidget> request)
        {
            return request.NavigationVersion == _navigationManager.Version;
        }
        
        public NavigationRequest<TWidget> CreateNavigationRequest(TWidget widget)
        {
            return new NavigationRequest<TWidget>(this, _navigationManager.Version, this, _navigationManager.Active, widget);
        }
        
        public NavigationRequest<TWidget> CreateNavigationRequest<TTarget>() where TTarget : class, TWidget
        {
            return new NavigationRequest<TWidget>(this, _navigationManager.Version, this, _navigationManager.Active, _registry.Get<TTarget>());
        }

        public NavigationResponse<TWidget> ProcessNavigationRequest(in NavigationRequest<TWidget> request)
        {
            Awaitable awaitable = null;
            NavigationResult<TWidget> result = _navigationManager.Navigate(request.Widget, request.AddToHistory);
            if (result.Success)
            {
                awaitable = Navigate(request.Transition, result.Active, result.Previous, request.Data, 
                    result.HistoryEntry, request.CancellationToken);
            }
            return new NavigationResponse<TWidget>(result, awaitable);
        }
        
        private Awaitable Navigate(VisibilityTransitionParams transition, TWidget target, TWidget source, object data, IHistoryEntry historyEntry, 
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
