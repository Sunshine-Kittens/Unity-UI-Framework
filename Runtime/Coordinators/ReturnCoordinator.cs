using System;
using System.Threading;

using UIFramework.Core;
using UIFramework.Core.Interfaces;
using UIFramework.Navigation;
using UIFramework.Navigation.History;
using UIFramework.Navigation.Interfaces;
using UIFramework.Transitioning;

using UnityEngine;

namespace UIFramework.Coordinators
{
    public class ReturnCoordinator<TWindow> : IReturnNavigator<TWindow>
        where TWindow : class, IWindow
    {
        private readonly WindowNavigator<TWindow> _navigator;
        private readonly History _history;
        private readonly TransitionManager _transitionManager;

        public ReturnCoordinator(WindowNavigator<TWindow> navigator, History history, TransitionManager transitionManager)
        {
            _navigator = navigator;
            _history = history;
            _transitionManager = transitionManager;
        }

        public NavigateToResponse<TWindow> Return(CancellationToken cancellationToken = default)
        {
            if (_history.Count == 0)
                return new NavigateToResponse<TWindow>(new NavigateToResult<TWindow>(false, _navigator.Active, null), null);

            IHistoryEntry historyEntry = _history.Pop();

            if (!historyEntry.TryGetEvent(out NavigationHistoryEvent navEvent))
                throw new InvalidOperationException($"History entry {historyEntry.ID} is missing a NavigationHistoryEvent.");

            if (!historyEntry.TryGetEvent(out TransitionHistoryEvent transitionEvent))
                throw new InvalidOperationException($"History entry {historyEntry.ID} is missing a TransitionHistoryEvent.");

            NavigateToResult<TWindow> result = _navigator.SetActive(navEvent.WindowType);
            if (!result.Success)
                return new NavigateToResponse<TWindow>(result, null);

            VisibilityTransitionParams transition = transitionEvent.Transition.Invert();
            Awaitable awaitable = _transitionManager.Transition(transition, result.Active, result.Previous, cancellationToken);
            return new NavigateToResponse<TWindow>(result, awaitable);
        }
    }
}
