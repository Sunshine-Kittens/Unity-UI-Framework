using System;
using System.Collections.Generic;

using UIFramework.Collectors;
using UIFramework.Core.Interfaces;

namespace UIFramework.Registry
{
    public interface IWidgetRegistry<TWidget> where TWidget : class, IWidget
    {
        public event Action<TWidget, int> WidgetRegistered;
        public event Action<TWidget, int> WidgetIndexChanged;
        public event Action<TWidget, int> WidgetUnregistered;

        public void Register(TWidget widget);
        public void SetIndex(TWidget widget, int index);
        public void Unregister(TWidget widget);

        public void Clear();

        public TWidget Get<TWidgetType>() where TWidgetType : class, TWidget;
        public TWidget Get(Type widgetType);
        public TWidget Get(string identifier);

        public bool TryGet<TWidgetType>(out TWidgetType widget) where TWidgetType : class, TWidget;
        public bool TryGet<TWidgetType>(out TWidgetType widget, out int index) where TWidgetType : class, TWidget;
        public bool TryGet(Type widgetType, out TWidget widget);
        public bool TryGet(Type widgetType, out TWidget widget, out int index);
        public bool TryGet(string identifier, out TWidget widget);

        public int IndexOf<TWidgetType>() where TWidgetType : class, TWidget;
        public int IndexOf(Type widgetType);
    }
    
    public class WidgetRegistry<TWidget> : IWidgetRegistry<TWidget> where TWidget : class, IWidget
    {
        public IReadOnlyList<TWidget> Widgets => _widgets;

        public event Action<TWidget, int> WidgetRegistered;
        public event Action<TWidget, int> WidgetIndexChanged;
        public event Action<TWidget, int> WidgetUnregistered;
        
        public bool IsInitialized => _isInitialized;
        
        private bool _isInitialized;
        private readonly List<TWidget> _widgets;
        private readonly Dictionary<Type, TWidget> _widgetTypeMap;
        private readonly Dictionary<string, TWidget> _widgetIdentifierMap;
        private readonly Action<TWidget> _onInitialize;
        private readonly Action<TWidget> _onTerminate;

        public WidgetRegistry(Action<TWidget> onInitialize, Action<TWidget> onTerminate)
        {
            _onInitialize = onInitialize;
            _onTerminate = onTerminate;
            _widgets = new List<TWidget>();
            _widgetTypeMap = new Dictionary<Type, TWidget>();
            _widgetIdentifierMap = new Dictionary<string, TWidget>();
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
            if (!_widgetTypeMap.TryAdd(widgetType, widget))
            {
                throw new InvalidOperationException($"Widget of type {widgetType.Name} is already registered");
            }
            string identifier = widget.Identifier;
            if (!string.IsNullOrEmpty(identifier) && !_widgetIdentifierMap.TryAdd(identifier, widget))
            {
                _widgetTypeMap.Remove(widgetType);
                throw new InvalidOperationException($"Widget with identifier '{identifier}' is already registered");
            }
            if (_isInitialized && widget.State == WidgetState.Uninitialized)
            {
                widget.Initialize();
                _onInitialize?.Invoke(widget);
            }
            _widgets.Add(widget);
            WidgetRegistered?.Invoke(widget, _widgets.Count - 1);
        }

        public void SetIndex(TWidget widget, int index)
        {
            if (widget == null) throw new ArgumentNullException(nameof(widget));

            int current = _widgets.IndexOf(widget);
            if (current == -1)
                throw new InvalidOperationException($"Widget of type {widget.GetType().Name} is not registered.");

            if (index < 0 || index >= _widgets.Count)
                throw new ArgumentOutOfRangeException(nameof(index), $"Index must be within [0, {_widgets.Count - 1}].");

            if (current == index) return;

            _widgets.RemoveAt(current);
            _widgets.Insert(index, widget);
            WidgetIndexChanged?.Invoke(widget, index);
        }

        public void Unregister(TWidget widget)
        {
            if (widget == null) throw new ArgumentNullException(nameof(widget));

            Type widgetType = widget.GetType();
            if (!_widgetTypeMap.Remove(widgetType, out TWidget foundWidget) || foundWidget != widget)
            {
                throw new InvalidOperationException($"Widget instance of type {widgetType.Name} is not registered");
            }
            string identifier = widget.Identifier;
            if (!string.IsNullOrEmpty(identifier))
                _widgetIdentifierMap.Remove(identifier);
            if (_isInitialized && widget.State == WidgetState.Initialized)
            {
                _onTerminate?.Invoke(widget);
                widget.Terminate();
            }
            int index = _widgets.IndexOf(widget);
            _widgets.Remove(widget);
            WidgetUnregistered?.Invoke(widget, index);
        }

        public void Clear()
        {
            for (int i = _widgets.Count - 1; i >= 0; i--)
            {
                Unregister(_widgets[i]);
            }
        }
        
        public TWidget Get<TWidgetType>() where TWidgetType : class, TWidget
        {
            return Get(typeof(TWidgetType)) as TWidgetType;
        }
        
        public TWidget Get(Type widgetType)
        {
            if (!_isInitialized) throw new InvalidOperationException("Registry not initialized");

            if (!_widgetTypeMap.TryGetValue(widgetType, out TWidget widget))
            {
                throw new KeyNotFoundException($"No widget of type {widgetType.Name} registered");
            }
            return widget;
        }

        public TWidget Get(string identifier)
        {
            if (!_isInitialized) throw new InvalidOperationException("Registry not initialized");

            if (!_widgetIdentifierMap.TryGetValue(identifier, out TWidget widget))
            {
                throw new KeyNotFoundException($"No widget with identifier '{identifier}' registered");
            }
            return widget;
        }
        
        public bool TryGet<TWidgetType>(out TWidgetType widget) where TWidgetType : class, TWidget
        {
            if (TryGet(typeof(TWidgetType), out TWidget w))
            {
                widget = w as TWidgetType;
                return true;    
            }
            widget = null;
            return false;
        }

        public bool TryGet<TWidgetType>(out TWidgetType widget, out int index) where TWidgetType : class, TWidget
        {
            if (TryGet(typeof(TWidgetType), out TWidget w))
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

            if (_widgetTypeMap.TryGetValue(widgetType, out TWidget foundWidget))
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

            if (_widgetTypeMap.TryGetValue(widgetType, out TWidget foundWidget))
            {
                widget = foundWidget;
                index = _widgets.IndexOf(foundWidget);
                return true;
            }
            return false;
        }

        public bool TryGet(string identifier, out TWidget widget)
        {
            widget = null;
            if (!_isInitialized) return false;

            return _widgetIdentifierMap.TryGetValue(identifier, out widget);
        }

        public int IndexOf<TWidgetType>() where TWidgetType : class, TWidget
        {
            if (_widgetTypeMap.TryGetValue(typeof(TWidgetType), out TWidget widget))
            {
                return _widgets.IndexOf(widget);
            }
            return -1;
        }
        
        public int IndexOf(Type widgetType)
        {
            if (_widgetTypeMap.TryGetValue(widgetType, out TWidget widget))
            {
                return _widgets.IndexOf(widget);
            }
            return -1;
        }
        
        public int IndexOf(TWidget widget)
        {
            return _widgets.IndexOf(widget);
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
            if (!_isInitialized) throw new InvalidOperationException("Registry is not initialized");
            
            foreach (TWidget widget in _widgets)
            {
                if (widget.State == WidgetState.Initialized)
                {
                    _onTerminate?.Invoke(widget);
                    widget.Terminate();
                }
            }
            _widgetTypeMap.Clear();
            _widgetIdentifierMap.Clear();
            _widgets.Clear();
            _isInitialized = false;
        }
    }
}
