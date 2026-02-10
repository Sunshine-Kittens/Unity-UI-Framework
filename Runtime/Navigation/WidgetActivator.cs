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
    
    public class WidgetActivator<TWidget> : IActivatorVersion where TWidget : class, IWidget
    {
        public int Version { get; private set; }

        public TWidget Active { get; private set; }
        public Type ActiveType { get; private set; } = null;

        public event Action<ActivateResult<TWidget>> OnActivateUpdate;

        private int _cachedActiveIndex = -1;
        private readonly WidgetRegistry<TWidget> _registry;

        private WidgetActivator() { }

        public WidgetActivator(WidgetRegistry<TWidget> widgetRegistry)
        {
            _registry = widgetRegistry ?? throw new ArgumentNullException(nameof(widgetRegistry));
            _registry.WidgetUnregistered += OnWidgetUnregistered;
        }

        public int GetActiveIndex()
        {
            if (_registry.Widgets.IsValidIndex(_cachedActiveIndex) && _registry.Widgets[_cachedActiveIndex].GetType() == ActiveType)
            {
                return _cachedActiveIndex;   
            }
            return UpdateActiveIndex();
        }
        
        public ActivateResult<TWidget> Activate(TWidget target)
        {
            Type targetType = target.GetType();
            if (ValidateTarget(targetType, target, out int index))
            {
                return Activate(targetType, target, index);
            }
            return InvokeActiveWidgetUpdate(new ActivateResult<TWidget>(false, Active, null, _cachedActiveIndex));
        }
        
        private ActivateResult<TWidget> Activate(Type type, TWidget target, int index)
        {
            TWidget previous = Active;
            ActiveType = type;
            Active = target;
            _cachedActiveIndex = index;
            Version++;
            return InvokeActiveWidgetUpdate(new ActivateResult<TWidget>(true, target, previous, index));
        }
        
        private bool ValidateTarget(Type type, TWidget target, out int index)
        {
            if (!ValidateTarget(type, out TWidget cached, out index))
            {
                return false;
            }

            if (!cached.Equals(target))
            {
                throw new InvalidOperationException($"Unable to set active to: {type}, the target type exists in registry but the instance of the target provided does not.");
            }
            return true;
        }

        private bool ValidateTarget(Type type, out TWidget target, out int index)
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
        
        private ActivateResult<TWidget> InvokeActiveWidgetUpdate(ActivateResult<TWidget> setActiveWidgetResult)
        {
            OnActivateUpdate?.Invoke(setActiveWidgetResult);
            return setActiveWidgetResult;
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
                if (_registry.TryGet(ActiveType, out TWidget widget, out activeIndex) && widget == Active)
                {
                    return activeIndex != _cachedActiveIndex;
                }
                return true;
            }
            activeIndex = -1;
            return false;
        }

        private void OnWidgetUnregistered(TWidget widget, int index)
        {
            if (HasActiveIndexChanged(out int activeIndex))
            {
                TWidget previous = Active;
                if (activeIndex == -1)
                {
                    Active = null;
                    ActiveType = null;   
                }
                UpdateActiveIndex();
                InvokeActiveWidgetUpdate(new ActivateResult<TWidget>(true, Active, previous, activeIndex));
            }
        }
    }
}
