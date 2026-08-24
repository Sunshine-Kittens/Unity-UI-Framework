using System;
using System.Collections.Generic;

using UIFramework.Collectors;
using UIFramework.Core.Interfaces;

namespace UIFramework.Registry
{
    public interface IWidgetRegistry<TWidget> where TWidget : class, IWidget
    {
        public IReadOnlyList<TWidget> Widgets { get; }
        public bool IsInitialized { get; }

        public event Action<TWidget, int> WidgetRegistered;
        public event Action<TWidget, int> WidgetIndexChanged;
        public event Action<TWidget, int> WidgetUnregistered;

        // Raised whenever the registry comes to hold an initialized widget, and before it stops holding one,
        // whatever drove the transition: the registry itself, or a widget that initializes or terminates on its
        // own. The two strictly alternate per widget for as long as it is registered.
        public event Action<TWidget> WidgetInitialized;
        public event Action<TWidget> WidgetTerminated;

        public void Initialize();
        public void Terminate();

        public void Collect(IEnumerable<IWidgetCollector<TWidget>> collectors);
        public void Collect(params IWidgetCollector<TWidget>[] collectors);

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
        public int IndexOf(TWidget widget);
    }
    
    public class WidgetRegistry<TWidget> : IWidgetRegistry<TWidget> where TWidget : class, IWidget
    {
        public IReadOnlyList<TWidget> Widgets => _widgets;

        public event Action<TWidget, int> WidgetRegistered;
        public event Action<TWidget, int> WidgetIndexChanged;
        public event Action<TWidget, int> WidgetUnregistered;

        public event Action<TWidget> WidgetInitialized;
        public event Action<TWidget> WidgetTerminated;

        public bool IsInitialized => _isInitialized;
        
        private bool _isInitialized;
        private readonly List<TWidget> _widgets;
        private readonly HashSet<TWidget> _announced;
        private readonly Dictionary<Type, TWidget> _widgetTypeMap;
        private readonly Dictionary<string, TWidget> _widgetIdentifierMap;
        private readonly IWidgetLifecycle<TWidget> _lifecycle;

        public WidgetRegistry() : this(null) { }

        public WidgetRegistry(IWidgetLifecycle<TWidget> lifecycle)
        {
            _lifecycle = lifecycle ?? new WidgetLifecycle<TWidget>();
            _widgets = new List<TWidget>();
            _announced = new HashSet<TWidget>();
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
                throw new InvalidOperationException($"Widget of type {widgetType.Name} is already registered");
            
            string identifier = widget.Identifier;
            if (!string.IsNullOrEmpty(identifier) && !_widgetIdentifierMap.TryAdd(identifier, widget))
            {
                _widgetTypeMap.Remove(widgetType);
                throw new InvalidOperationException($"Widget with identifier '{identifier}' is already registered");
            }
            
            SubscribeToLifecycle(widget);
            if (_isInitialized)
            {
                if (_lifecycle.CanInitialize(widget))
                    _lifecycle.Initialize(widget);
                AnnounceInitialized(widget);
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
            if (_isInitialized && _lifecycle.CanTerminate(widget))
            {
                _lifecycle.Terminate(widget);
            }
            UnsubscribeFromLifecycle(widget);
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

            // Set ahead of the loop: the relay below speaks only for an initialized registry, and a consumer
            // handling an announcement may look other widgets up.
            _isInitialized = true;
            foreach (TWidget widget in _widgets)
            {
                if (_lifecycle.CanInitialize(widget))
                    _lifecycle.Initialize(widget);
                AnnounceInitialized(widget);
            }
        }

        public void Terminate()
        {
            if (!_isInitialized) throw new InvalidOperationException("Registry is not initialized");
            
            foreach (TWidget widget in _widgets)
            {
                if (_lifecycle.CanTerminate(widget))
                {
                    _lifecycle.Terminate(widget);
                }
                UnsubscribeFromLifecycle(widget);
            }
            _widgetTypeMap.Clear();
            _widgetIdentifierMap.Clear();
            _widgets.Clear();
            _isInitialized = false;
        }

        // WidgetInitialized/WidgetTerminated are raised only by relaying the widget's own events, so there is
        // exactly one raise site per transition no matter who drove it, and the ordering is always the widget's:
        // after it is live, before it is torn down.
        private void SubscribeToLifecycle(TWidget widget)
        {
            widget.Initialized += OnWidgetInitialized;
            widget.Terminating += OnWidgetTerminating;
        }

        private void UnsubscribeFromLifecycle(TWidget widget)
        {
            widget.Initialized -= OnWidgetInitialized;
            widget.Terminating -= OnWidgetTerminating;
            _announced.Remove(widget);
        }

        private void OnWidgetInitialized(IWidget widget) => AnnounceInitialized((TWidget)widget);

        private void OnWidgetTerminating(IWidget widget) => AnnounceTerminated((TWidget)widget);

        // Announcing is idempotent, tracked per widget in _announced, because one transition can reach here
        // twice: a registered child initialized by its parent's cascade is heard through the relay and then
        // offered again when the loop reaches the child in its own right.
        //
        // Both are gated on _isInitialized the way the registry's own lifecycle hooks are, since subscriptions
        // outlive initialization: an uninitialized registry holds widgets but does not speak for them.
        private void AnnounceInitialized(TWidget widget)
        {
            if (_isInitialized && widget.State == WidgetState.Initialized && _announced.Add(widget))
                WidgetInitialized?.Invoke(widget);
        }

        private void AnnounceTerminated(TWidget widget)
        {
            if (_isInitialized && _announced.Remove(widget))
                WidgetTerminated?.Invoke(widget);
        }
    }
}
