using System.Threading;

using UIFramework.Core;
using UIFramework.Core.Interfaces;
using UIFramework.Navigation;
using UIFramework.Navigation.History;
using UIFramework.Navigation.Interfaces;
using UIFramework.Registry;
using UIFramework.Transitioning;

using UnityEngine;

namespace UIFramework.Coordinators
{
    public class ReturnCoordinator<TWindow> : IReturnNavigator<TWindow>
        where TWindow : class, IWindow
    {
        private readonly WindowNavigator<TWindow> _navigator;
        private readonly WidgetRegistry<TWindow> _registry;
        private readonly History _history;
        private readonly TransitionManager _transitionManager;

        public ReturnCoordinator(WindowNavigator<TWindow> navigator, WidgetRegistry<TWindow> registry,
            History history, TransitionManager transitionManager)
        {
            _navigator = navigator;
            _registry = registry;
            _history = history;
            _transitionManager = transitionManager;
        }

        public NavigateToResponse<TWindow> Return(CancellationToken cancellationToken = default)
        {
            if (_history.Count == 0)
            {
                return new NavigateToResponse<TWindow>(
                    new NavigateToResult<TWindow>(false, _navigator.ActiveInstance, null), null
                );
            }

            using HistoryEventCollection events = _history.Pop();

            if (!events.TryGetEvent(out NavigationHistoryEvent navEvent))
                throw new System.InvalidOperationException("History entry is missing a NavigationHistoryEvent.");

            if (!events.TryGetEvent(out TransitionHistoryEvent transitionEvent))
                throw new System.InvalidOperationException("History entry is missing a TransitionHistoryEvent.");

            if (!_registry.TryGet(navEvent.WindowType, out TWindow target))
            {
                return new NavigateToResponse<TWindow>(
                    new NavigateToResult<TWindow>(false, _navigator.ActiveInstance, null), null
                );
            }

            NavigateToResult<TWindow> result = _navigator.NavigateTo(target);
            if (!result.Success)
                return new NavigateToResponse<TWindow>(result, null);

            VisibilityTransitionParams transition = transitionEvent.Transition.Invert();
            Awaitable awaitable = _transitionManager.Transition(transition, result.Active, result.Previous, cancellationToken);
            return new NavigateToResponse<TWindow>(result, awaitable);
        }
    }
}
