using System;
using System.Threading;

using UIFramework.Interfaces;
using UIFramework.Navigation;
using UIFramework.WidgetTransition;

using UnityEngine;

namespace UIFramework.Coordinators
{
    public class ReturnCoordinator<TWidget> : IReturnNavigator<TWidget> where TWidget : class, IWidget
    {
        private readonly NavigationManager<TWidget> _navigationManager;
        private readonly History _history;
        private readonly TransitionManager _transitionManager;
        
        public ReturnCoordinator(NavigationManager<TWidget> navigationManager, History history, TransitionManager transitionManager)
        {
            _transitionManager = transitionManager;
            _navigationManager = navigationManager;
            _history = history;
        }

        public NavigationResponse<TWidget> Return(CancellationToken cancellationToken = default)
        {
            NavigationResult<TWidget> result = _navigationManager.Return();
            Awaitable awaitable = null;
            if (result.Success)
            {
                IHistoryEntry historyEntry = _history.Pop();
                
                if (!historyEntry.TryGetEvent(out TransitionHistoryEvent transitionEvent))
                    throw new InvalidOperationException($"Unable to find transition event for entry {historyEntry.ID}.");

                VisibilityTransitionParams transition = transitionEvent.Transition.Invert();
                awaitable = _transitionManager.Transition(transition, result.Active, result.Previous, 
                    cancellationToken);
            }
            return new NavigationResponse<TWidget>(result, awaitable);
        }
    }
}
