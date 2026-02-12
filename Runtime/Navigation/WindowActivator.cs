using System;

using UIFramework.Core.Interfaces;
using UIFramework.Registry;

using UnityEngine;
using UnityEngine.Extension;

namespace UIFramework.Navigation
{
    public interface IActivatorVersion
    {
        public int Version { get; }
    }
    
    public class WindowActivator<TWindow> : IActivatorVersion where TWindow : class, IWindow
    {
        public int Version { get; private set; }

        public TWindow Active { get; private set; }
        public Type ActiveType { get; private set; } = null;

        public event Action<ActivateResult<TWindow>> OnActivateUpdate;

        private int _cachedActiveIndex = -1;
        private readonly WidgetRegistry<TWindow> _registry;

        private WindowActivator() { }

        public WindowActivator(WidgetRegistry<TWindow> registry)
        {
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            _registry.WidgetUnregistered += OnWindowUnregistered;
        }

        public int GetActiveIndex()
        {
            if (_registry.Widgets.IsValidIndex(_cachedActiveIndex) && _registry.Widgets[_cachedActiveIndex].GetType() == ActiveType)
            {
                return _cachedActiveIndex;   
            }
            return UpdateActiveIndex();
        }
        
        public ActivateResult<TWindow> Activate(TWindow target)
        {
            Type targetType = target.GetType();
            if (ValidateTarget(targetType, target, out int index))
            {
                return Activate(targetType, target, index);
            }
            return InvokeActiveWindowUpdate(new ActivateResult<TWindow>(false, Active, null, _cachedActiveIndex));
        }
        
        private ActivateResult<TWindow> Activate(Type type, TWindow target, int index)
        {
            TWindow previous = Active;
            ActiveType = type;
            Active = target;
            _cachedActiveIndex = index;
            Version++;
            return InvokeActiveWindowUpdate(new ActivateResult<TWindow>(true, target, previous, index));
        }
        
        private bool ValidateTarget(Type type, TWindow target, out int index)
        {
            if (!ValidateTarget(type, out TWindow cached, out index))
            {
                return false;
            }

            if (!cached.Equals(target))
            {
                throw new InvalidOperationException($"Unable to set active to: {type}, the target type exists in registry but the instance of the target provided does not.");
            }
            return true;
        }

        private bool ValidateTarget(Type type, out TWindow target, out int index)
        {
            target = null;
            index = -1;
            if (ActiveType == type)
            {
                Debug.LogWarning($"Unable to set active to: {type} as it is already active.");
                return false;
            }

            if (!_registry.TryGet(type, out target, out index))
            {
                throw new InvalidOperationException($"Unable to set active to: {type}, not found in registry.");
            }
            return target.IsInitialized;
        }
        
        private ActivateResult<TWindow> InvokeActiveWindowUpdate(ActivateResult<TWindow> setActiveResult)
        {
            OnActivateUpdate?.Invoke(setActiveResult);
            return setActiveResult;
        }

        private int UpdateActiveIndex()
        {
            if (ActiveType != null)
                _cachedActiveIndex = _registry.IndexOf(ActiveType);
            else
                _cachedActiveIndex = -1;   
            return _cachedActiveIndex;
        }
        
        private bool HasActiveIndexChanged(out int activeIndex)
        {
            if (_cachedActiveIndex != -1)
            {
                if (_registry.TryGet(ActiveType, out TWindow window, out activeIndex) && window == Active)
                {
                    return activeIndex != _cachedActiveIndex;
                }
                return true;
            }
            activeIndex = -1;
            return false;
        }

        private void OnWindowUnregistered(TWindow window, int index)
        {
            if (HasActiveIndexChanged(out int activeIndex))
            {
                TWindow previous = Active;
                if (activeIndex == -1)
                {
                    Active = null;
                    ActiveType = null;   
                }
                UpdateActiveIndex();
                InvokeActiveWindowUpdate(new ActivateResult<TWindow>(true, Active, previous, activeIndex));
            }
        }
    }
}
