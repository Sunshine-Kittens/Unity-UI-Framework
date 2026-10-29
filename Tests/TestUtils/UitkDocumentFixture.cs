using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.ExceptionServices;

using UIFramework.UIToolkit;

using UnityEngine;
using UnityEngine.UIElements;

using Object = UnityEngine.Object;
using UitkWidget = UIFramework.UIToolkit.Widget;

namespace UIFramework.TestUtils
{
    // One UIDocument on a panel built in code, with widgets bound by name to elements of a tree built in code.
    // Play mode only: a widget links to its parent as it awakes, and animation runs on the player loop.
    //
    // Placement rules the widgets depend on:
    // - the document has a GameObject of its own, never a widget's or one under it: a widget deactivates its own
    //   GameObject as it hides and terminates, and disabling the document terminates its live widgets;
    // - the document stays enabled while a widget is bound: disabling it discards the tree built here, and a
    //   test that cycles it rebuilds whatever it needs from DocumentSource.Enabled;
    // - an element exists before the widget that resolves it initializes, and element names are unique;
    // - a parent is added before its children and is active while they are added, since they link to it as
    //   they awake.
    //
    // Close() terminates every live widget while the tree still exists, waits one frame for anything resuming
    // on the next frame, then destroys the GameObjects, the panel settings and theme, and clears the pools. It
    // fails if any step threw, or if an awaitable the test watched is still pending after that frame.
    public sealed class UitkDocumentFixture
    {
        // Every UI Toolkit widget shares this pool, windows and screens included.
        public static string HandlePoolName => $"VisibilityAnimationHandle<{typeof(UitkWidget).FullName}>";

        public GameObject Root { get; }
        public PanelSettings PanelSettings { get; }
        public UIDocument Document { get; }
        public WidgetDocumentSource DocumentSource { get; }

        // Read through the document each time: the document builds a new root whenever it is enabled.
        public VisualElement RootElement => Document.rootVisualElement;

        public int PooledHandles => _pools.CountOf(HandlePoolName);

        private readonly PoolScope _pools;
        private readonly ThemeStyleSheet _theme;
        private readonly List<UitkWidget> _widgets = new();
        private readonly List<AwaitableProbe> _probes = new();
        private bool _closed;

        public UitkDocumentFixture()
        {
            _pools = new PoolScope();
            try
            {
                // An empty theme rather than none, which the panel warns about.
                _theme = ScriptableObject.CreateInstance<ThemeStyleSheet>();
                PanelSettings = ScriptableObject.CreateInstance<PanelSettings>();
                PanelSettings.name = "UitkFixturePanel";
                PanelSettings.themeStyleSheet = _theme;

                Root = new GameObject("UitkFixture");
                GameObject documentObject = new("Document");
                documentObject.transform.SetParent(Root.transform, false);
                Document = documentObject.AddComponent<UIDocument>();
                Document.panelSettings = PanelSettings;
                DocumentSource = documentObject.AddComponent<WidgetDocumentSource>();
            }
            catch
            {
                Release();
                throw;
            }

            if (RootElement == null || RootElement.panel == null)
            {
                Release();
                throw new InvalidOperationException(
                    "The fixture document has no root element on a panel; update UitkDocumentFixture.");
            }
        }

        public VisualElement AddElement(string name, VisualElement parent = null) => AddElement<VisualElement>(name, parent);

        public T AddElement<T>(string name, VisualElement parent = null) where T : VisualElement, new()
        {
            // An empty name matches whichever unnamed element comes first, so it cannot single this one out.
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("A widget resolves its element by name, so the element needs one.", nameof(name));
            if (RootElement.Q(name) != null)
                throw new ArgumentException($"An element named {name} already exists.", nameof(name));

            T element = new() { name = name };
            (parent ?? RootElement).Add(element);
            return element;
        }

        public VisualElement Element(string name) => RootElement.Q(name);

        // Added to an inactive GameObject and configured before it activates, since a widget subscribes to its
        // document source as it awakes.
        public T AddWidget<T>(string elementName, Transform parent = null) where T : UitkWidget
        {
            GameObject widgetObject = new(typeof(T).Name);
            widgetObject.SetActive(false);
            widgetObject.transform.SetParent(parent != null ? parent : Root.transform, false);
            T widget = widgetObject.AddComponent<T>();
            _widgets.Add(widget);
            SetSerializedField(widget, "_documentSource", DocumentSource);
            SetSerializedField(widget, "_visualElementName", elementName);
            widgetObject.SetActive(true);

            if (!widgetObject.activeInHierarchy)
                throw new InvalidOperationException(
                    $"{typeof(T).Name} did not awake under an inactive parent: add children while their parent is active.");
            return widget;
        }

        // A widget on an element of its own, directly under the document root.
        public T AddBoundWidget<T>(string elementName, bool initialize = true) where T : UitkWidget
        {
            AddElement(elementName);
            T widget = AddWidget<T>(elementName);
            if (initialize)
                widget.Initialize();
            return widget;
        }

        public AwaitableProbe Watch(Awaitable awaitable)
        {
            AwaitableProbe probe = AwaitableProbe.Watch(awaitable);
            _probes.Add(probe);
            return probe;
        }

        // For [UnityTearDown]. A second call does nothing.
        public IEnumerator Close()
        {
            if (_closed)
                yield break;

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

        // Creation order: a parent comes first and its cascade takes its children down with it. Every widget is
        // tried even when an earlier one throws; the first exception is rethrown at the end.
        private void Shutdown()
        {
            Exception first = null;
            foreach (UitkWidget widget in _widgets)
            {
                try
                {
                    if (widget != null && widget.CanTerminate)
                        widget.Terminate();
                }
                catch (Exception exception)
                {
                    first ??= exception;
                }
            }
            _widgets.Clear();

            if (first != null)
                ExceptionDispatchInfo.Capture(first).Throw();
        }

        // The document goes before its panel settings, so it detaches from a live panel; destroying the panel
        // settings then disposes the panel.
        private void Release()
        {
            _closed = true;
            try
            {
                Shutdown();
            }
            finally
            {
                if (Root != null)
                    Object.DestroyImmediate(Root);
                if (PanelSettings != null)
                    Object.DestroyImmediate(PanelSettings);
                if (_theme != null)
                    Object.DestroyImmediate(_theme);
                _pools.Dispose();
            }
        }

        // A widget takes its document source and element name only from serialized data.
        private static void SetSerializedField(object target, string name, object value)
        {
            FieldInfo field = typeof(UitkWidget).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)
                ?? throw new MissingFieldException(
                    $"{nameof(UitkWidget)}.{name} was not found. UitkDocumentFixture sets it by reflection; update the fixture.");
            field.SetValue(target, value);
        }
    }
}
