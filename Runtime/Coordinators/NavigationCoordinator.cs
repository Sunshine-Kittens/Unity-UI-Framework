using System.Threading;

using UIFramework.Core;
using UIFramework.Core.Interfaces;
using UIFramework.Navigation;
using UIFramework.Navigation.Interfaces;
using UIFramework.Registry;
using UIFramework.Transitioning;

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
    
    public class NavigationCoordinator<TWindow> : INavigationRequestFactory<TWindow>, INavigationRequestProcessor<TWindow> where TWindow : class, IWindow
    {
        private readonly TimeMode _timeMode;
        private readonly WidgetRegistry<TWindow> _registry;
        private readonly WindowNavigator<TWindow> _windowNavigator;
        private readonly History _history;
        private readonly TransitionManager _transitionManager;
        
        public NavigationCoordinator(TimeMode timeMode, WidgetRegistry<TWindow> registry, WindowNavigator<TWindow> windowNavigator,
            History history, TransitionManager transitionManager)
        {
            _timeMode = timeMode;
            _registry = registry;
            _windowNavigator = windowNavigator;
            _history = history;
            _transitionManager = transitionManager;
        }
        
        public NavigationRequest<TWindow> CreateNavigationRequest(TWindow window)
        {
            return new NavigationRequest<TWindow>(_windowNavigator, this, _windowNavigator.Active, window);
        }
        
        public NavigationRequest<TWindow> CreateNavigationRequest<TTarget>() where TTarget : class, TWindow
        {
            return new NavigationRequest<TWindow>(_windowNavigator, this, _windowNavigator.Active, _registry.Get<TTarget>());
        }

        public NavigationResponse<TWindow> ProcessNavigationRequest(in NavigationRequest<TWindow> request)
        {
            Awaitable awaitable = null;
            NavigationResult<TWindow> result = _windowNavigator.Navigate(request.Window, request.AddToHistory);
            if (result.Success)
            {
                awaitable = Navigate(request.Transition, result.Active, result.Previous, request.Data, 
                    result.HistoryEntry, request.CancellationToken);
            }
            return new NavigationResponse<TWindow>(result, awaitable);
        }
        
        private Awaitable Navigate(VisibilityTransitionParams transition, TWindow target, TWindow source, object data, IHistoryEntry historyEntry, 
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
