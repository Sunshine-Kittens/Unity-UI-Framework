using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.ExceptionServices;

using UIFramework.Collectors;
using UIFramework.Controllers;
using UIFramework.Core.Interfaces;
using UIFramework.Groups;
using UIFramework.Navigation;
using UIFramework.Registry;

using UnityEngine;
using UnityEngine.Extension;

using Object = UnityEngine.Object;

namespace UIFramework.TestUtils
{
    // Real uGUI screens under one screen-space canvas, with the controllers, groups and hosts that drive them.
    // Play mode only: a child links to its parent in Awake, and animation runs on the player loop.
    //
    // Placement rules the screens depend on:
    // - the canvas root stays active: a screen initializes only under an active canvas;
    // - screens a controller layers are flat siblings under the root, where a band is a sibling index;
    // - a parent is added, and left uninitialized, before its children, which link to it as they awake.
    //
    // Close() tears down in dependency order: hosts, controllers, groups and registries first, while the
    // screens they release still exist; then any widget still live is terminated; then one frame for anything
    // resuming on the next frame; then the canvas and the pools. It fails if any step threw, or if an awaitable
    // the test watched is still pending after that frame.
    public sealed class UguiScreenFixture
    {
        public GameObject Root { get; }

        // Animation handles back in the pool every uGUI widget shares, screens included.
        public int PooledHandles => _pools.CountOf(UguiWidgetFixture.HandlePoolName);

        private readonly PoolScope _pools;
        private readonly List<UIFramework.UGUI.Widget> _widgets = new();
        private readonly List<ScreenControllerHost> _hosts = new();
        private readonly List<ScreenController> _controllers = new();
        private readonly List<ScreenGroup> _groups = new();
        private readonly List<WidgetRegistry<IScreen>> _registries = new();
        private readonly List<AwaitableProbe> _probes = new();

        public UguiScreenFixture()
        {
            _pools = new PoolScope();
            Root = new GameObject("ScreenCanvas", typeof(RectTransform), typeof(Canvas));
        }

        public T AddScreen<T>(Transform parent = null) where T : UIFramework.UGUI.Screen => AddWidget<T>(parent);

        // Parented before the component is added, so the widget is under its parent when it awakes.
        public T AddWidget<T>(Transform parent = null) where T : UIFramework.UGUI.Widget
        {
            GameObject widgetObject = new(typeof(T).Name, typeof(RectTransform), typeof(CanvasGroup));
            widgetObject.transform.SetParent(parent != null ? parent : Root.transform, false);
            T widget = widgetObject.AddComponent<T>();
            _widgets.Add(widget);
            return widget;
        }

        public ScreenController CreateController(params IScreen[] screens)
        {
            ScreenController controller = new(new IWidgetCollector<IScreen>[] { new FakeCollector(screens) },
                TimeMode.Scaled);
            _controllers.Add(controller);
            controller.Initialize();
            return controller;
        }

        public WidgetRegistry<IScreen> Track(WidgetRegistry<IScreen> registry)
        {
            _registries.Add(registry);
            return registry;
        }

        public WidgetRegistry<IScreen> CreateRegistry(params IScreen[] screens)
        {
            WidgetRegistry<IScreen> registry = Track(new WidgetRegistry<IScreen>());
            foreach (IScreen screen in screens)
                registry.Register(screen);
            registry.Initialize();
            return registry;
        }

        public ScreenGroup CreateGroup(WidgetRegistry<IScreen> registry)
        {
            ScreenGroup group = new(registry, TimeMode.Scaled);
            _groups.Add(group);
            return group;
        }

        // On the canvas root, so its default search root holds every fixture screen.
        public ScreenCollector AddCollector() => Root.AddComponent<ScreenCollector>();

        // The shipping host on a GameObject of its own, configured while inactive and then enabled, so its
        // controller is built by the host's own factory from the given collectors.
        public ScreenControllerHost CreateHost(params ScreenCollector[] collectors)
        {
            GameObject hostObject = new("ScreenHost");
            hostObject.SetActive(false);
            ScreenControllerHost host = hostObject.AddComponent<ScreenControllerHost>();
            _hosts.Add(host);
            SetSerializedField(host, typeof(ScreenControllerHost), "_collectors", collectors);
            hostObject.SetActive(true);
            return host;
        }

        public AwaitableProbe Watch(in NavigateToResponse<IScreen> response) => Watch(response.GetCompletionTask());

        // For an operation that finishes inside the call. Watched like any other, and rethrows its fault or
        // throws if it is still running, so a fault raised after the state a test checks still fails the test.
        public NavigateToResponse<IScreen> Instant(in NavigateToResponse<IScreen> response)
        {
            AwaitableProbe probe = Watch(in response);
            if (!probe.IsComplete)
                throw new InvalidOperationException("An operation expected to end inside its call is still running.");
            if (probe.Exception != null)
                ExceptionDispatchInfo.Capture(probe.Exception).Throw();
            return response;
        }

        // Bounded by elapsed seconds, as AwaitableProbe.WaitForCompletion is.
        public static IEnumerator WaitUntil(Func<bool> condition, string what, float timeoutSeconds = 5f)
        {
            float deadline = Time.realtimeSinceStartup + timeoutSeconds;
            while (!condition() && Time.realtimeSinceStartup < deadline)
                yield return null;

            if (!condition())
                throw new TimeoutException($"Timed out after {timeoutSeconds}s waiting until {what}.");
        }

        // For [UnityTearDown].
        public IEnumerator Close()
        {
            Exception fault = null;
            try
            {
                Shutdown();
            }
            catch (Exception exception)
            {
                fault = exception;
            }

            yield return null;

            int unsettled = CountUnsettledProbes();
            Release();

            if (fault != null)
                ExceptionDispatchInfo.Capture(fault).Throw();
            if (unsettled > 0)
                throw new InvalidOperationException($"{unsettled} awaitable(s) the test started never settled.");
        }

        private AwaitableProbe Watch(Awaitable awaitable)
        {
            AwaitableProbe probe = AwaitableProbe.Watch(awaitable);
            _probes.Add(probe);
            return probe;
        }

        private int CountUnsettledProbes()
        {
            int count = 0;
            foreach (AwaitableProbe probe in _probes)
            {
                if (!probe.IsComplete)
                    count++;
            }
            return count;
        }

        // Every step runs even when an earlier one throws; the first exception is rethrown at the end.
        private void Shutdown()
        {
            Exception first = null;

            for (int i = _hosts.Count - 1; i >= 0; i--)
                Run(() => { if (_hosts[i] != null) Object.DestroyImmediate(_hosts[i].gameObject); }, ref first);
            for (int i = _controllers.Count - 1; i >= 0; i--)
                Run(() => { if (_controllers[i].IsInitialized) _controllers[i].Terminate(); }, ref first);
            for (int i = _groups.Count - 1; i >= 0; i--)
                Run(() => _groups[i].Reset(), ref first);
            for (int i = _registries.Count - 1; i >= 0; i--)
                Run(() => { if (_registries[i].IsInitialized) _registries[i].Terminate(); }, ref first);
            // Creation order: a parent comes first and its cascade takes its children down with it.
            for (int i = 0; i < _widgets.Count; i++)
                Run(() => { if (_widgets[i] != null && _widgets[i].CanTerminate) _widgets[i].Terminate(); }, ref first);

            _hosts.Clear();
            _controllers.Clear();
            _groups.Clear();
            _registries.Clear();
            _widgets.Clear();

            if (first != null)
                ExceptionDispatchInfo.Capture(first).Throw();
        }

        private void Release()
        {
            try
            {
                Shutdown();
            }
            finally
            {
                if (Root != null)
                    Object.DestroyImmediate(Root);
                _pools.Dispose();
            }
        }

        private static void Run(Action step, ref Exception first)
        {
            try
            {
                step();
            }
            catch (Exception exception)
            {
                first ??= exception;
            }
        }

        // The host takes its collectors only from serialized data, so a test sets them by reflection.
        private static void SetSerializedField(object target, Type declaringType, string name, object value)
        {
            FieldInfo field = declaringType.GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)
                ?? throw new MissingFieldException(
                    $"{declaringType.Name}.{name} was not found. UguiScreenFixture sets it by reflection; update the fixture.");
            field.SetValue(target, value);
        }
    }
}
