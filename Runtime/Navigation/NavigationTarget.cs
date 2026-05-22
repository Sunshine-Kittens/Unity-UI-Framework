using System;
using System.Collections.Generic;

using UIFramework.Core.Interfaces;
using UIFramework.Registry;

namespace UIFramework.Navigation
{
    // Lightweight discriminated union representing how a navigation target is identified.
    // Controllers create one of these and pass it to CreateNavigateToRequest — resolution
    // against the registry happens inside the navigator, not at the call site.
    public readonly struct NavigationTarget<TWindow> where TWindow : class, IWindow
    {
        private enum Kind : byte { Instance, Type, Index }

        private readonly Kind _kind;
        private readonly TWindow _instance;
        private readonly Type _type;
        private readonly int _index;

        private NavigationTarget(Kind kind, TWindow instance, Type type, int index)
        {
            _kind = kind;
            _instance = instance;
            _type = type;
            _index = index;
        }

        public static NavigationTarget<TWindow> FromInstance(TWindow instance)
        {
            if (instance == null) throw new ArgumentNullException(nameof(instance));
            return new NavigationTarget<TWindow>(Kind.Instance, instance, null, -1);
        }

        public static NavigationTarget<TWindow> FromType<T>() where T : class, TWindow
            => new NavigationTarget<TWindow>(Kind.Type, null, typeof(T), -1);

        public static NavigationTarget<TWindow> FromType(Type type)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            return new NavigationTarget<TWindow>(Kind.Type, null, type, -1);
        }

        public static NavigationTarget<TWindow> FromIndex(int index)
        {
            if (index < 0) throw new ArgumentOutOfRangeException(nameof(index));
            return new NavigationTarget<TWindow>(Kind.Index, null, null, index);
        }

        public TWindow Resolve(WidgetRegistry<TWindow> registry)
        {
            switch (_kind)
            {
                case Kind.Instance:
                    return _instance;
                case Kind.Type:
                    return registry.Get(_type);
                case Kind.Index:
                    IReadOnlyList<TWindow> widgets = registry.Widgets;
                    if ((uint)_index >= (uint)widgets.Count)
                        throw new ArgumentOutOfRangeException(nameof(_index),
                            $"Index {_index} is out of range for registry of size {widgets.Count}.");
                    return widgets[_index];
                default:
                    throw new InvalidOperationException($"Unknown navigation target kind: {_kind}");
            }
        }
    }
}
