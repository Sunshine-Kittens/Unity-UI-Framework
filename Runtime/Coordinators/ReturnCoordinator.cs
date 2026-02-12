using System;
using System.Threading;

using UIFramework.Core;
using UIFramework.Core.Interfaces;
using UIFramework.Navigation;
using UIFramework.Navigation.Interfaces;
using UIFramework.Transitioning;

using UnityEngine;

namespace UIFramework.Coordinators
{
    public class ReturnCoordinator<TWindow> : IReturnNavigator<TWindow> where TWindow : class, IWindow
    {
        private readonly WindowNavigator<TWindow> _windowNavigator;
        private readonly History _history;
        private readonly TransitionManager _transitionManager;
        
        public ReturnCoordinator(WindowNavigator<TWindow> windowNavigator, History history, TransitionManager transitionManager)
        {
            _transitionManager = transitionManager;
            _windowNavigator = windowNavigator;
            _history = history;
        }

        public NavigationResponse<TWindow> Return(CancellationToken cancellationToken = default)
        {
            NavigationResult<TWindow> result = _windowNavigator.Return();
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
            return new NavigationResponse<TWindow>(result, awaitable);
        }
    }
}
