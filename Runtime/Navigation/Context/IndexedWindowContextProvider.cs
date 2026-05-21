using System;

using UIFramework.Core.Interfaces;
using UIFramework.Registry;

namespace UIFramework.Navigation.Context
{
    public class IndexedWindowContextProvider<TWindow> : IContextProvider<TWindow, WindowIndexContext<TWindow>> where TWindow : class, IWindow
    {
        private readonly WidgetRegistry<TWindow> _registry;
        
        public IndexedWindowContextProvider(WidgetRegistry<TWindow> registry)
        {
            _registry = registry ?? throw new ArgumentException(nameof(registry));    
        }
        
        public WindowIndexContext<TWindow> GetContext(TWindow window)
        {
            if (window != null)
                return new WindowIndexContext<TWindow> { Window = window, Index = _registry.IndexOf(window) };
            return null;
        }
    }
}
