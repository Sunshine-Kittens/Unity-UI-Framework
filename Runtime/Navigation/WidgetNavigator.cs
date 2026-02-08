using System;

using UIFramework.Registry;

using UnityEngine;
using UnityEngine.Extension;

namespace UIFramework.Navigation
{
    public readonly struct NavigationHistoryEvent : IHistoryEvent
    {
        public readonly Type WidgetType;

        public NavigationHistoryEvent(Type widgetType)
        {
            WidgetType = widgetType;
        }
    }

    public interface INavigatorVersion
    {
        public int Version { get; }
    }
    
    public sealed class WidgetNavigator<TWidget> : INavigatorVersion where TWidget : class, IWidget
    {
        public int Version { get; private set; }

        public TWidget Active => ActiveType != null ? _registry.Get(ActiveType) : null;
        public Type ActiveType { get; private set; } = null;
        
        public event Action<NavigationResult<TWidget>> OnNavigationUpdate;

        public IScalarFlag IsLocked => _isLocked;
        private readonly ScalarFlag _isLocked = new ScalarFlag(false);
        
        private readonly WidgetRegistry<TWidget> _registry;
        private readonly History _history = null;

        private WidgetNavigator() { }

        public WidgetNavigator(WidgetRegistry<TWidget> widgetRegistry, History history)
        {
            _registry = widgetRegistry ?? throw new ArgumentNullException(nameof(widgetRegistry));
            _history = history;
        }

        public NavigationResult<TWidget> Navigate(TWidget target, bool addToHistory = true)
        {
            Type targetType = target.GetType();
            if (ValidateTravelTarget(targetType, target))
            {
                return Navigate(targetType, target, addToHistory);
            }
            return InvokeNavigationUpdate(new NavigationResult<TWidget>(false, null, Active, IsLocked.Value, _history.Count, null));
        }

        public NavigationResult<TWidget> Return()
        {
            if (Active != null)
            {
                if (IsLocked.Value)
                {
                    return InvokeNavigationUpdate(new NavigationResult<TWidget>(false, null, Active, IsLocked.Value,  _history.Count, null));
                }

                if (_history.Count > 0)
                {
                    IHistoryEntry historyEntry = _history.Pop();
                    if (!historyEntry.TryGetEvent(out NavigationHistoryEvent historyEvent))
                        throw new InvalidOperationException($"Unable to find navigation event for entry {historyEntry.ID}.");
                    TWidget target = _registry.Get(historyEvent.WidgetType);
                    TWidget previous = Active;
                    ActiveType = historyEvent.WidgetType;
                    Version++;
                    return InvokeNavigationUpdate(new NavigationResult<TWidget>(true, previous, target, IsLocked.Value, _history.Count, historyEntry));
                }
            }
            return InvokeNavigationUpdate(new NavigationResult<TWidget>(false, null, Active, IsLocked.Value, _history.Count, null));
        }

        public NavigationResult<TWidget> Lock()
        {
            IsLocked.Value = true;
            return InvokeNavigationUpdate(new NavigationResult<TWidget>(IsLocked.Value, null, Active, IsLocked.Value, _history.Count, null));
        }

        public NavigationResult<TWidget> Unlock()
        {
            IsLocked.Value = false;
            return InvokeNavigationUpdate(new NavigationResult<TWidget>(!IsLocked.Value, null, Active, IsLocked.Value, _history.Count, null));
        }

        public NavigationResult<TWidget> Clear()
        {
            if (ActiveType != null)
            {
                IsLocked.Reset();
                TWidget previous = Active;
                _history.Clear();
                ActiveType = null;
                Version++;
                return InvokeNavigationUpdate(new NavigationResult<TWidget>(true, previous, null, IsLocked.Value, _history.Count, null));
            }
            return InvokeNavigationUpdate(new NavigationResult<TWidget>(false, null, Active, IsLocked.Value, _history.Count, null));
        }
        
        private NavigationResult<TWidget> Navigate(Type type, TWidget target, bool addToHistory)
        {
            if (IsLocked.Value)
            {
                return InvokeNavigationUpdate(new NavigationResult<TWidget>(false, null, Active, IsLocked.Value, _history.Count, null));
            }

            IHistoryEntry historyEntry = null;
            if (ActiveType != null && addToHistory)
            {
                historyEntry = _history.PushNewEntry();
                NavigationHistoryEvent historyEvent = new  NavigationHistoryEvent(type);
                historyEntry.Append(historyEvent);
            }

            TWidget previous = Active;
            ActiveType = type;
            Version++;
            return InvokeNavigationUpdate(new NavigationResult<TWidget>(true, previous, target, IsLocked.Value, _history.Count, historyEntry));
        }
        
        private bool ValidateTravelTarget(Type type, TWidget target)
        {
            if (!ValidateTravelTarget(type, out TWidget cached))
            {
                return false;
            }

            if (!cached.Equals(target))
            {
                throw new InvalidOperationException($"Unable to travel to: {type}, the target type exists in navigation but the instance of the target provided does not.");
            }
            return true;
        }

        private bool ValidateTravelTarget(Type type, out TWidget target)
        {
            target = null;
            if (ActiveType == type)
            {
                Debug.LogWarning($"Unable to travel to: {type} as it is already active.");
                return false;
            }

            if (!_registry.TryGet(type, out target))
            {
                throw new InvalidOperationException($"Unable to travel to: {type}, not found in navigation.");
            }
            return target.IsInitialized;
        }
        
        private NavigationResult<TWidget> InvokeNavigationUpdate(NavigationResult<TWidget> navigationNavigationResult)
        {
            OnNavigationUpdate?.Invoke(navigationNavigationResult);
            return navigationNavigationResult;
        }
    }
}
