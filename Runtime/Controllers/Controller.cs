using System;

using UIFramework.Controllers.Interfaces;
using UIFramework.Core.Interfaces;
using UIFramework.Registry;

using UnityEngine.Extension;

namespace UIFramework.Controllers
{
    // Shared base for window controllers. Owns the registry of available windows and the lifecycle.
    // Nothing navigational lives here — subclasses compose navigation over the shared registry:
    // TabController holds a single flat selection; ScreenController stacks layered groups.
    public abstract class Controller<TWindow> : IController where TWindow : class, IWindow
    {
        public bool IsInitialized => Registry.IsInitialized;

        protected WidgetRegistry<TWindow> Registry { get; }
        protected TimeMode TimeMode { get; }

        protected Controller(TimeMode timeMode)
        {
            TimeMode = timeMode;
            // The method-group hooks are only invoked once a widget is initialized (during Initialize or a
            // post-init Register), never during construction — so virtual dispatch here is safe.
            Registry = new WidgetRegistry<TWindow>(OnWidgetInitialize, OnWidgetTerminate);
        }

        public virtual void Initialize()
        {
            if (IsInitialized)
                throw new InvalidOperationException($"{GetType().Name} is already initialized.");
            Registry.Initialize();
            OnInitialize();
        }

        public virtual void Terminate()
        {
            if (!IsInitialized)
                throw new InvalidOperationException($"{GetType().Name} is not initialized.");
            OnTerminate();
            Registry.Terminate();
        }

        public virtual void Tick() { }

        // Per-widget registry hooks — subclasses wire events / initial state for each managed window.
        protected virtual void OnWidgetInitialize(TWindow window) { }
        protected virtual void OnWidgetTerminate(TWindow window) { }

        // Controller-level setup/teardown around registry init/terminate.
        protected virtual void OnInitialize() { }
        protected virtual void OnTerminate() { }
    }
}
