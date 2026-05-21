using System;

using UIFramework.Core;
using UIFramework.Core.Interfaces;
using UIFramework.Navigation.Context;
using UIFramework.Navigation.History;
using UIFramework.Navigation.Interfaces;
using UIFramework.Navigation.Metadata;
using UIFramework.Registry;

using UnityEngine;

namespace UIFramework.Navigation.Base
{
    public abstract class WindowNavigatorBase<TWindow, TContext, TResult>
        where TWindow : class, IWindow 
        where TContext : WindowContext<TWindow>
        where TResult : NavigateToResultBase<TWindow, TContext>, new()
    {
        public int Version { get; private set; }

        public TWindow Active => ActiveType != null ? Registry.Get(ActiveType) : null;
        public Type ActiveType { get; private set; }
        
        public event Action<INavigateToResult<TWindow>> OnNavigationUpdate;
        
        public bool SupportsHistory => _history != null;
        
        protected readonly WidgetRegistry<TWindow> Registry;
        private readonly IContextProvider<TWindow, TContext> _contextProvider;
        private readonly Core.History _history;
        
        private protected WindowNavigatorBase(IContextProvider<TWindow, TContext> contextProvider, WidgetRegistry<TWindow> registry, Core.History history)
        {
            _contextProvider = contextProvider ?? throw new ArgumentNullException(nameof(contextProvider));
            Registry = registry ?? throw new ArgumentNullException(nameof(registry));
            _history = history;
            Registry.WidgetUnregistered += OnWindowUnregistered;
        }

        public TResult NavigateTo(TWindow target)
        {
            Type targetType = target.GetType();
            if (ValidateTravelTarget(targetType, target))
            {
                return NavigateTo(targetType, target);
            }
            return InvokeNavigationUpdate(BuildResult(false, null, GetWindowContext(Active), GetHistoryMetadata(null)));
        }

        public TResult Return()
        {
            if (SupportsHistory)
            {
                while (GetHistoryCount() > 0)
                {
                    IHistoryEntry historyEntry = _history.Pop();
                    if (!historyEntry.TryGetEvent(out NavigationHistoryEvent historyEvent))
                        throw new InvalidOperationException($"Unable to find navigation event for entry {historyEntry.ID}.");
                    if (Registry.TryGet(historyEvent.WindowType, out TWindow target))
                    {
                        TWindow previous = Active;
                        ActiveType = historyEvent.WindowType;
                        Version++;
                        return InvokeNavigationUpdate(
                            BuildResult(true, GetWindowContext(previous), GetWindowContext(target), GetHistoryMetadata(null))
                        );       
                    }
                }
            }
            return InvokeNavigationUpdate(BuildResult(false, null, GetWindowContext(Active), GetHistoryMetadata(null)));
        }

        public TResult Clear()
        {
            if (ActiveType != null)
            {
                TWindow previous = Active;
                ClearHistory();
                ActiveType = null;
                Version++;
                return InvokeNavigationUpdate(BuildResult(true, GetWindowContext(previous), null, GetHistoryMetadata(null)));
            }
            return InvokeNavigationUpdate(BuildResult(false, null, GetWindowContext(Active), GetHistoryMetadata(null)));
        }
        
        private TResult NavigateTo(Type type, TWindow target)
        {
            IHistoryEntry historyEntry = null;
            if (ActiveType != null && SupportsHistory)
            {
                historyEntry = _history.PushNewEntry();
                NavigationHistoryEvent historyEvent = new  NavigationHistoryEvent(type);
                historyEntry.Append(historyEvent);
            }

            TWindow previous = Active;
            ActiveType = type;
            Version++;
            return InvokeNavigationUpdate(
                BuildResult(true, GetWindowContext(previous), GetWindowContext(target), GetHistoryMetadata(historyEntry))
            );
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

            if (!Registry.TryGet(type, out target))
            {
                throw new InvalidOperationException($"Unable to travel to: {type}, not found in navigation.");
            }
            return target.IsInitialized;
        }
        
        protected TResult InvokeNavigationUpdate(TResult navigateToNavigateToResult)
        {
            OnNavigationUpdate?.Invoke(navigateToNavigateToResult);
            return navigateToNavigateToResult;
        }

        private int GetHistoryCount()
        {
            return _history?.Count ?? 0;
        }

        private void ClearHistory()
        {
            _history?.Clear();
        }

        protected NavigationHistoryMetadata GetHistoryMetadata(IHistoryEntry historyEntry)
        {
            if (SupportsHistory)
            {
                return new NavigationHistoryMetadata
                {
                    HistoryCount = GetHistoryCount(),
                    HistoryEntry = historyEntry
                };
            }
            return null;
        }

        protected TContext GetWindowContext(TWindow window)
        {
            return _contextProvider.GetContext(window);
        }
        
        protected TResult BuildResult(bool success, TContext previous, TContext active, NavigationMetadata metadata)
        {
            return new TResult
            {
                Success = success,
                Previous = previous,
                Active = active,
                Metadata = metadata,
            };
        }
        
        private void OnWindowUnregistered(TWindow window, int index)
        {
            if (window == Active)
            {
                TWindow previous = Active;
                ActiveType = null;
                InvokeNavigationUpdate(BuildResult(true, GetWindowContext(previous), null, GetHistoryMetadata(null)));
            }
        }
    }
}
