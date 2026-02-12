using System;

using UIFramework.Core;
using UIFramework.Core.Interfaces;
using UIFramework.Registry;

using UnityEngine;
using UnityEngine.Extension;

namespace UIFramework.Navigation
{
    public readonly struct NavigationHistoryEvent : IHistoryEvent
    {
        public readonly Type WindowType;

        public NavigationHistoryEvent(Type windowType)
        {
            WindowType = windowType;
        }
    }

    public interface INavigatorVersion
    {
        public int Version { get; }
    }
    
    public sealed class WindowNavigator<TWindow> : INavigatorVersion where TWindow : class, IWindow
    {
        public int Version { get; private set; }

        public TWindow Active => ActiveType != null ? _registry.Get(ActiveType) : null;
        public Type ActiveType { get; private set; } = null;
        
        public event Action<NavigationResult<TWindow>> OnNavigationUpdate;

        public IScalarFlag IsLocked => _isLocked;
        private readonly ScalarFlag _isLocked = new ScalarFlag(false);
        
        private readonly WidgetRegistry<TWindow> _registry;
        private readonly History _history = null;

        private WindowNavigator() { }

        public WindowNavigator(WidgetRegistry<TWindow> registry, History history)
        {
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            _history = history;
            _registry.WidgetUnregistered += OnWindowUnregistered;
        }

        public NavigationResult<TWindow> Navigate(TWindow target, bool addToHistory = true)
        {
            Type targetType = target.GetType();
            if (ValidateTravelTarget(targetType, target))
            {
                return Navigate(targetType, target, addToHistory);
            }
            return InvokeNavigationUpdate(new NavigationResult<TWindow>(false, null, Active, IsLocked.Value, _history.Count, null));
        }

        public NavigationResult<TWindow> Return()
        {
            if (Active != null)
            {
                if (IsLocked.Value)
                {
                    return InvokeNavigationUpdate(new NavigationResult<TWindow>(false, null, Active, IsLocked.Value,  _history.Count, null));
                }

                if (_history.Count > 0)
                {
                    IHistoryEntry historyEntry = _history.Pop();
                    if (!historyEntry.TryGetEvent(out NavigationHistoryEvent historyEvent))
                        throw new InvalidOperationException($"Unable to find navigation event for entry {historyEntry.ID}.");
                    TWindow target = _registry.Get(historyEvent.WindowType);
                    TWindow previous = Active;
                    ActiveType = historyEvent.WindowType;
                    Version++;
                    return InvokeNavigationUpdate(new NavigationResult<TWindow>(true, previous, target, IsLocked.Value, _history.Count, historyEntry));
                }
            }
            return InvokeNavigationUpdate(new NavigationResult<TWindow>(false, null, Active, IsLocked.Value, _history.Count, null));
        }

        public NavigationResult<TWindow> Lock()
        {
            IsLocked.Value = true;
            return InvokeNavigationUpdate(new NavigationResult<TWindow>(IsLocked.Value, null, Active, IsLocked.Value, _history.Count, null));
        }

        public NavigationResult<TWindow> Unlock()
        {
            IsLocked.Value = false;
            return InvokeNavigationUpdate(new NavigationResult<TWindow>(!IsLocked.Value, null, Active, IsLocked.Value, _history.Count, null));
        }

        public NavigationResult<TWindow> Clear()
        {
            if (ActiveType != null)
            {
                IsLocked.Reset();
                TWindow previous = Active;
                _history.Clear();
                ActiveType = null;
                Version++;
                return InvokeNavigationUpdate(new NavigationResult<TWindow>(true, previous, null, IsLocked.Value, _history.Count, null));
            }
            return InvokeNavigationUpdate(new NavigationResult<TWindow>(false, null, Active, IsLocked.Value, _history.Count, null));
        }
        
        private NavigationResult<TWindow> Navigate(Type type, TWindow target, bool addToHistory)
        {
            if (IsLocked.Value)
            {
                return InvokeNavigationUpdate(new NavigationResult<TWindow>(false, null, Active, IsLocked.Value, _history.Count, null));
            }

            IHistoryEntry historyEntry = null;
            if (ActiveType != null && addToHistory)
            {
                historyEntry = _history.PushNewEntry();
                NavigationHistoryEvent historyEvent = new  NavigationHistoryEvent(type);
                historyEntry.Append(historyEvent);
            }

            TWindow previous = Active;
            ActiveType = type;
            Version++;
            return InvokeNavigationUpdate(new NavigationResult<TWindow>(true, previous, target, IsLocked.Value, _history.Count, historyEntry));
        }
        
        private bool ValidateTravelTarget(Type type, TWindow target)
        {
            if (!ValidateTravelTarget(type, out TWindow cached))
            {
                return false;
            }

            if (!cached.Equals(target))
            {
                throw new InvalidOperationException($"Unable to travel to: {type}, the target type exists in navigation but the instance of the target provided does not.");
            }
            return true;
        }

        private bool ValidateTravelTarget(Type type, out TWindow target)
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
        
        private NavigationResult<TWindow> InvokeNavigationUpdate(NavigationResult<TWindow> navigationNavigationResult)
        {
            OnNavigationUpdate?.Invoke(navigationNavigationResult);
            return navigationNavigationResult;
        }
        
        private void OnWindowUnregistered(TWindow window, int index)
        {
            if (window == Active)
            {
                InvokeNavigationUpdate(new NavigationResult<TWindow>(true, Active, null, IsLocked.Value, _history.Count, null));
            }
        }
    }
}
