using System;
using System.Collections.Generic;

using UIFramework.Collectors;

namespace UIFramework.Registry
{
    //TODO: Add clear method
    public interface IWidgetRegistry<TWidget> where TWidget : class, IWidget
    {
        public void Register(TWidget widget);
        public void Unregister(TWidget widget);

        public TWidget Get<TWidgetType>() where TWidgetType : class, TWidget;
        public TWidget Get(Type widgetType);

        public bool TryGet<TWidgetType>(out TWidgetType widget) where TWidgetType : class, TWidget;
        public bool TryGet<TWidgetType>(out TWidgetType widget, out int index) where TWidgetType : class, TWidget;
        public bool TryGet(Type widgetType, out TWidget widget);
        public bool TryGet(Type widgetType, out TWidget widget, out int index);

        public int IndexOf<TWidgetType>() where TWidgetType : class, TWidget;
        public int IndexOf(Type widgetType);
    }
    
    public class WidgetRegistry<TWidget> : IWidgetRegistry<TWidget> where TWidget : class, IWidget
    {
        public IReadOnlyList<TWidget> Widgets => _widgets;

        public event Action<TWidget> WidgetRegistered;
        public event Action<TWidget> WidgetUnregistered;
        
        private bool _isInitialized;
        private readonly List<TWidget> _widgets;
        private readonly Dictionary<Type, TWidget> _widgetMap;
        private readonly Action<TWidget> _onInitialize;

        public WidgetRegistry(Action<TWidget> onInitialize)
        {
            _onInitialize = onInitialize;
        }
        
        public void Collect(IEnumerable<IWidgetCollector<TWidget>> collectors)
        {
            foreach (IWidgetCollector<TWidget> collector in collectors)
            {
                if (collector == null)
                {
                    continue;
                }

                IEnumerable<TWidget> widgets = collector.Collect();
                foreach (TWidget widget in widgets)
                {
                    Register(widget);
                }
            }
        }
        
        public void Collect(params IWidgetCollector<TWidget>[] collectors)
        {
            foreach (IWidgetCollector<TWidget> collector in collectors)
            {
                if (collector == null)
                {
                    continue;
                }

                IEnumerable<TWidget> widgets = collector.Collect();
                foreach (TWidget widget in widgets)
                {
                    Register(widget);
                }
            }
        }
        
        public void Register(TWidget widget)
        {
            if (widget == null) throw new ArgumentNullException(nameof(widget));

            Type widgetType = widget.GetType();
            if (!_widgetMap.TryAdd(widgetType, widget))
            {
                throw new InvalidOperationException($"Widget of type {widgetType.Name} is already registered");
            }
            if (_isInitialized && widget.State == WidgetState.Uninitialized)
            {
                widget.Initialize();
                _onInitialize?.Invoke(widget);
            }
            _widgets.Add(widget);
            WidgetRegistered?.Invoke(widget);
        }

        public void Unregister(TWidget widget)
        {
            if (widget == null) throw new ArgumentNullException(nameof(widget));

            Type widgetType = widget.GetType();
            if (!_widgetMap.Remove(widgetType, out TWidget foundWidget))
            {
                throw new InvalidOperationException($"Widget of type {widgetType.Name} is not registered");
            }
            if (_isInitialized && widget.State == WidgetState.Initialized)
            {
                widget.Terminate();
            }
            _widgets.Remove(widget);
            WidgetUnregistered?.Invoke(widget);
        }
        
        public TWidget Get<TWidgetType>() where TWidgetType : class, TWidget
        {
            return Get(typeof(TWidgetType)) as TWidgetType;
        }
        
        public TWidget Get(Type widgetType)
        {
            if (!_isInitialized) throw new InvalidOperationException("Registry not initialized");
            
            if (!_widgetMap.TryGetValue(widgetType, out TWidget widget))
            {
                throw new KeyNotFoundException($"No widget of type {widgetType.Name} registered");
            }
            return widget;
        }
        
        public bool TryGet<TWidgetType>(out TWidgetType widget) where TWidgetType : class, TWidget
        {
            if (TryGet(typeof(TWidget), out TWidget w))
            {
                widget = w as TWidgetType;
                return true;    
            }
            widget = null;
            return false;
        }

        public bool TryGet<TWidgetType>(out TWidgetType widget, out int index) where TWidgetType : class, TWidget
        {
            if (TryGet(typeof(TWidget), out TWidget w))
            {
                widget = w as TWidgetType;
                index = _widgets.IndexOf(widget);
                return true;    
            }
            widget = null;
            index = -1;
            return false;
        }
        
        public bool TryGet(Type widgetType, out TWidget widget)
        {
            widget = null;
            if (!_isInitialized) return false;

            if (_widgetMap.TryGetValue(widgetType, out TWidget foundWidget))
            {
                widget = foundWidget;
                return true;
            }
            return false;
        }

        public bool TryGet(Type widgetType, out TWidget widget, out int index)
        {
            widget = null;
            index = -1;
            if (!_isInitialized) return false;

            if (_widgetMap.TryGetValue(widgetType, out TWidget foundWidget))
            {
                widget = foundWidget;
                index = _widgets.IndexOf(foundWidget);
                return true;
            }
            return false;
        }
        
        public int IndexOf<TWidgetType>() where TWidgetType : class, TWidget
        {
            if (_widgetMap.TryGetValue(typeof(TWidgetType), out TWidget widget))
            {
                return _widgets.IndexOf(widget);
            }
            return -1;
        }
        
        public int IndexOf(Type widgetType)
        {
            if (_widgetMap.TryGetValue(widgetType, out TWidget widget))
            {
                return _widgets.IndexOf(widget);
            }
            return -1;
        }
        
        public void Initialize()
        {
            if (_isInitialized) throw new InvalidOperationException("Registry already initialized");

            foreach (TWidget widget in _widgets)
            {
                if (widget.State == WidgetState.Uninitialized)
                {
                    widget.Initialize();
                    _onInitialize?.Invoke(widget);
                }
            }
            _isInitialized = true;
        }

        public void Terminate()
        {
            foreach (TWidget widget in _widgets)
            {
                if (widget.State == WidgetState.Initialized)
                {
                    widget.Terminate();
                }
            }

            _widgetMap.Clear();
            _widgets.Clear();
            _isInitialized = false;
        }
    }
}
