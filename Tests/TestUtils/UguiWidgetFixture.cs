using System;

using UIFramework.UGUI;

using UnityEngine;

using Object = UnityEngine.Object;

namespace UIFramework.TestUtils
{
    // Builds the minimum real uGUI hierarchy a Widget needs: Initialize() walks up for a Canvas and throws
    // without one, and the Widget itself requires a RectTransform and CanvasGroup.
    //
    // Play mode only. Anything touching animation needs the player loop, which drives AnimationSystemRunner.
    public sealed class UguiWidgetFixture : IDisposable
    {
        public GameObject Root { get; }
        public Canvas Canvas { get; }
        public Widget Widget { get; }

        // primeCanvasGroup works around a defect in the uGUI backend: Opacity, SetOpacity and
        // GetGenericAnimation read the _canvasGroup backing field rather than the lazy CanvasGroup property,
        // and nothing in Initialize() populates it — so a freshly initialized widget throws on all three.
        // Reading the property once primes the field. Pass false to exercise the defect itself.
        public UguiWidgetFixture(bool initialize = true, bool primeCanvasGroup = true)
        {
            Root = new GameObject("TestCanvas", typeof(RectTransform), typeof(Canvas));
            Canvas = Root.GetComponent<Canvas>();

            GameObject widgetObject = new("TestWidget", typeof(RectTransform), typeof(CanvasGroup));
            widgetObject.transform.SetParent(Root.transform, false);
            Widget = widgetObject.AddComponent<Widget>();

            if (initialize)
                Widget.Initialize();

            if (primeCanvasGroup)
                _ = Widget.CanvasGroup;
        }

        // Name the handle pool for this backend. Both backends declare a type called Widget, so the
        // registered key is the full type name.
        public static string HandlePoolName => $"VisibilityAnimationHandle<{typeof(Widget).FullName}>";

        public void Dispose()
        {
            if (Root != null)
                Object.DestroyImmediate(Root);
        }
    }
}
