using System;

using UIFramework.Core.Interfaces;
using UIFramework.Navigation.Interfaces;
using UIFramework.Registry;

using UnityEngine;

namespace UIFramework.Navigation
{
    public sealed class WindowNavigator<TWindow> : INavigationVersion
        where TWindow : class, IWindow
    {
        public int Version { get; private set; }

        public TWindow ActiveInstance => _activeType != null ? _registry.Get(_activeType) : null;
        public int ActiveIndex => _activeType != null ? _registry.IndexOf(_activeType) : -1;
        public Type ActiveType => _activeType;

        private Type _activeType;
        private readonly WidgetRegistry<TWindow> _registry;

        public event Action<NavigateToResult<TWindow>> OnNavigationUpdate;

        public WindowNavigator(WidgetRegistry<TWindow> registry)
        {
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            _registry.WidgetUnregistered += OnWidgetUnregistered;
        }

        public NavigateToResult<TWindow> NavigateTo(TWindow target)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));

            Type targetType = target.GetType();

            if (_activeType == targetType)
            {
                Debug.LogWarning($"Unable to navigate to {targetType.Name}: already active.");
                return InvokeNavigationUpdate(new NavigateToResult<TWindow>(false, ActiveInstance, target));
            }

            if (!_registry.TryGet(targetType, out TWindow cached))
                throw new InvalidOperationException(
                    $"Unable to navigate to {targetType.Name}: not found in registry.");

            if (!cached.Equals(target))
                throw new InvalidOperationException(
                    $"Unable to navigate to {targetType.Name}: instance does not match the registered instance.");

            if (!target.IsInitialized)
                return InvokeNavigationUpdate(new NavigateToResult<TWindow>(false, ActiveInstance, target));

            TWindow previous = ActiveInstance;
            _activeType = targetType;
            Version++;
            return InvokeNavigationUpdate(new NavigateToResult<TWindow>(true, previous, target));
        }

        public NavigateToResult<TWindow> Clear()
        {
            if (_activeType == null)
                return InvokeNavigationUpdate(new NavigateToResult<TWindow>(false, null, null));

            TWindow previous = ActiveInstance;
            _activeType = null;
            Version++;
            return InvokeNavigationUpdate(new NavigateToResult<TWindow>(true, previous, null));
        }

        // Silent reset for pooled reuse — clears active state and invalidates outstanding requests
        // (via Version) without firing a navigation update.
        public void Reset()
        {
            _activeType = null;
            Version++;
        }

        private NavigateToResult<TWindow> InvokeNavigationUpdate(NavigateToResult<TWindow> result)
        {
            OnNavigationUpdate?.Invoke(result);
            return result;
        }

        private void OnWidgetUnregistered(TWindow window, int index)
        {
            if (_activeType != null && window.GetType() == _activeType)
            {
                TWindow previous = window;
                _activeType = null;
                Version++;
                OnNavigationUpdate?.Invoke(new NavigateToResult<TWindow>(true, previous, null));
            }
        }
    }
}
