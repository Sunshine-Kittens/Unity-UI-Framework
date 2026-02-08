using System;

using UIFramework.Interfaces;
using UIFramework.Navigation;
using UIFramework.Registry;
using UIFramework.WidgetTransition;

using UnityEngine;

namespace UIFramework.Coordinators
{
    // This needs to actually pop the transition...
    // Consider whether return actions should only rewind and for explicit overrides we navigate to the desired screen?
    // This may mean dropping the builder pattern
    // We could consider using the complier shim to enable init properties for object initializers on structs
    public class ReturnCoordinator<TWidget> : IReturnRequestFactory<TWidget>, IReturnRequestProcessor<TWidget> where TWidget : class, IWidget
    {
        private readonly WidgetRegistry<TWidget> _registry;
        private readonly NavigationManager<TWidget> _navigationManager;
        private readonly History _history;
        private readonly TransitionManager _transitionManager;
        
        public ReturnCoordinator(WidgetRegistry<TWidget> registry, NavigationManager<TWidget> navigationManager,
            History history, TransitionManager transitionManager)
        {
            _registry = registry;
            _transitionManager = transitionManager;
            _navigationManager = navigationManager;
            _history = history;
        }

        public ReturnRequest<TWidget> CreateReturnRequest()
        {
            IHistoryEntry historyEntry = _history.Peek();
            if (!historyEntry.TryGetEvent(out NavigationHistoryEvent historyEvent))
                throw new InvalidOperationException($"Unable to find navigation event for entry {historyEntry.ID}.");
            if (!_registry.TryGet(historyEvent.WidgetType, out TWidget widget))
                throw new InvalidOperationException($"Unable to find registered widget for type {historyEvent.WidgetType}.");
            return new ReturnRequest<TWidget>(this, _navigationManager.Version, this, _navigationManager.Active, widget);
        }
        
        public bool IsRequestValid(in ReturnRequest<TWidget> request)
        {
            return request.NavigationVersion == _navigationManager.Version;
        }
        
        //TODO: 
        public NavigationResponse<TWidget> ProcessReturnRequest(in ReturnRequest<TWidget> request)
        {
            NavigationResult<TWidget> result = _navigationManager.Return();
            Awaitable awaitable = null;
            if (result.Success)
            {
                IHistoryEntry historyEntry = _history.Pop();
                if (!historyEntry.TryGetEvent(out NavigationHistoryEvent historyEvent))
                    throw new InvalidOperationException($"Unable to find navigation event for entry {historyEntry.ID}.");
                if (!_registry.TryGet(historyEvent.WidgetType, out TWidget widget))
                    throw new InvalidOperationException($"Unable to find registered widget for type {historyEvent.WidgetType}.");
                awaitable = _transitionManager.Transition(request.Transition, result.Active, result.Previous, 
                    request.CancellationToken);
            }
            return new NavigationResponse<TWidget>(result, awaitable);
        }
    }
}
