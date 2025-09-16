using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Extension;

namespace UIFramework
{
    public class Navigation<TNavigable> where TNavigable : class, IWindow
    {
        public readonly struct Event
        {
            public readonly bool Success;
            public readonly TNavigable Previous;
            public readonly TNavigable Active;
            public readonly int HistoryCount;
            public readonly bool IsLocked;

            public Event(bool success, TNavigable previous, TNavigable active, int historyCount, bool isLocked)
            {
                Success = success;
                Previous = previous;
                Active = active;
                HistoryCount = historyCount;
                IsLocked = isLocked;
            }
        }
        
        public IReadOnlyDictionary<Type, TNavigable> Navigables { get { return _navigables; } }
        private Dictionary<Type, TNavigable> _navigables = null;

        public TNavigable Active => ActiveType != null ? _navigables[ActiveType] : default;

        public Type ActiveType { get; private set; } = null;

        public int HistoryCount => _history.Count;

        public event Action<Event> OnNavigationUpdate;

        public IScalarFlag IsLocked => _isLocked;
        private readonly ScalarFlag _isLocked = new ScalarFlag(false);

        private readonly History<Type> _history = null;

        private Navigation() { }

        public Navigation(Dictionary<Type, TNavigable> navigables)
        {
            _navigables = navigables ?? throw new ArgumentNullException(nameof(navigables));
            _history = new History<Type>(_navigables.Count);
        }

        public Event Travel<TTarget>(bool excludeCurrentFromHistory = false)
            where TTarget : TNavigable
        {
            Type targetType = typeof(TTarget);
            if (ValidateTravelTarget(targetType, out TNavigable target))
            {
                return InvokeNavigationUpdate(Travel(targetType, target, excludeCurrentFromHistory));
            }
            return InvokeNavigationUpdate(new Event(false, null, Active, HistoryCount, IsLocked.Value));
        }

        public Event Travel(TNavigable target, bool excludeCurrentFromHistory = false)
        {
            Type targetType = target.GetType();
            if (ValidateTravelTarget(targetType, target))
            {
                return Travel(targetType, target, excludeCurrentFromHistory);
            }
            return InvokeNavigationUpdate(new Event(false, null, Active, HistoryCount, IsLocked.Value));
        }

        private Event Travel(Type type, TNavigable target, bool excludeCurrentFromHistory)
        {
            if (IsLocked.Value)
            {
                return InvokeNavigationUpdate(new Event(false, null, Active, HistoryCount, IsLocked.Value));
            }

            if (ActiveType != null && Active.SupportsHistory && !excludeCurrentFromHistory)
            {
                _history.Push(ActiveType);
            }

            TNavigable previous = Active;
            ActiveType = type;
            return InvokeNavigationUpdate(new Event(true, previous, target, HistoryCount, IsLocked.Value));
        }

        private bool ValidateTravelTarget(Type type, TNavigable target)
        {
            if (!ValidateTravelTarget(type, out TNavigable cached))
            {
                return false;
            }

            if (!cached.Equals(target))
            {
                throw new InvalidOperationException($"Unable to travel to: {type}, the target type exists in navigation but the instance of the target provided does not.");
            }
            return true;
        }

        private bool ValidateTravelTarget(Type type, out TNavigable target)
        {
            target = null;
            if (ActiveType == type)
            {
                Debug.LogWarning($"Unable to travel to: {type} as it is already active.");
                return false;
            }

            if (!_navigables.TryGetValue(type, out target))
            {
                throw new InvalidOperationException($"Unable to travel to: {type}, not found in navigation.");
            }
            return target.IsInitialized;
        }

        public Event Back()
        {
            if (Active != null)
            {
                if (IsLocked.Value)
                {
                    return InvokeNavigationUpdate(new Event(false, null, Active, HistoryCount, IsLocked.Value));
                }

                if (HistoryCount > 0)
                {
                    Type targetType = _history.Pop();
                    TNavigable target = _navigables[targetType];
                    TNavigable previous = Active;
                    ActiveType = targetType;
                    return InvokeNavigationUpdate(new Event(true, previous, target, HistoryCount, IsLocked.Value));
                }
            }
            return InvokeNavigationUpdate(new Event(false, null, Active, HistoryCount, IsLocked.Value));
        }

        public Event Lock()
        {
            IsLocked.Value = true;
            return InvokeNavigationUpdate(new Event(IsLocked.Value, null, Active, HistoryCount, IsLocked.Value));
        }

        public Event Unlock()
        {
            IsLocked.Value = false;
            return InvokeNavigationUpdate(new Event(!IsLocked.Value, null, Active, HistoryCount, IsLocked.Value));
        }

        public Event Clear()
        {
            if (ActiveType != null)
            {
                IsLocked.Reset();
                TNavigable previous = Active;
                ClearHistory();
                ActiveType = null;
                return InvokeNavigationUpdate(new Event(true, previous, null, HistoryCount, IsLocked.Value));
            }
            return InvokeNavigationUpdate(new Event(false, null, Active, HistoryCount, IsLocked.Value));
        }

        public void StartNewHistoryGroup()
        {
            _history.StartNewGroup();
        }

        public Event ClearLatestHistoryGroup()
        {
            if (_history.ClearLatestGroup())
            {
                return InvokeNavigationUpdate(new Event(true, null, Active, HistoryCount, IsLocked.Value));
            }
            return InvokeNavigationUpdate(new Event(false, null, Active, HistoryCount, IsLocked.Value));
        }

        public Event InsertHistory<T>() where T : TNavigable
        {
            _history.Push(typeof(T));
            return InvokeNavigationUpdate(new Event(true, null, Active, HistoryCount, IsLocked.Value));
        }

        public Event ClearHistory()
        {
            if (_history.Clear())
            {
                return InvokeNavigationUpdate(new Event(true, null, Active, HistoryCount, IsLocked.Value));
            }
            return InvokeNavigationUpdate(new Event(false, null, Active, HistoryCount, IsLocked.Value));
        }

        private Event InvokeNavigationUpdate(Event navigationEvent)
        {
            OnNavigationUpdate?.Invoke(navigationEvent);
            return navigationEvent;
        }
    }
}
