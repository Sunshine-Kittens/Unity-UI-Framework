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

        public TWindow Active => _activeType != null ? _registry.Get(_activeType) : null;
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
            return NavigateToInternal(target.GetType(), target);
        }

        public NavigateToResult<TWindow> NavigateTo(NavigationTarget<TWindow> target)
        {
            TWindow resolved = target.Resolve(_registry);
            return NavigateToInternal(resolved.GetType(), resolved);
        }

        // Used by ReturnCoordinator to restore a previous screen without going through history again.
        public NavigateToResult<TWindow> SetActive(Type targetType)
        {
            if (targetType == null) throw new ArgumentNullException(nameof(targetType));
            if (!_registry.TryGet(targetType, out TWindow target))
                return InvokeNavigationUpdate(new NavigateToResult<TWindow>(false, Active, null));

            TWindow previous = Active;
            _activeType = targetType;
            Version++;
            return InvokeNavigationUpdate(new NavigateToResult<TWindow>(true, previous, target));
        }

        public NavigateToResult<TWindow> Clear()
        {
            if (_activeType == null)
                return InvokeNavigationUpdate(new NavigateToResult<TWindow>(false, null, null));

            TWindow previous = Active;
            _activeType = null;
            Version++;
            return InvokeNavigationUpdate(new NavigateToResult<TWindow>(true, previous, null));
        }

        private NavigateToResult<TWindow> NavigateToInternal(Type targetType, TWindow target)
        {
            if (_activeType == targetType)
            {
                Debug.LogWarning($"Unable to navigate to {targetType.Name}: already active.");
                return InvokeNavigationUpdate(new NavigateToResult<TWindow>(false, Active, target));
            }

            if (!_registry.TryGet(targetType, out TWindow cached))
                throw new InvalidOperationException(
                    $"Unable to navigate to {targetType.Name}: not found in registry.");

            if (!cached.Equals(target))
                throw new InvalidOperationException(
                    $"Unable to navigate to {targetType.Name}: provided instance does not match the registered instance."
                );

            if (!target.IsInitialized)
                return InvokeNavigationUpdate(new NavigateToResult<TWindow>(false, Active, target));

            TWindow previous = Active;
            _activeType = targetType;
            Version++;
            return InvokeNavigationUpdate(new NavigateToResult<TWindow>(true, previous, target));
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
