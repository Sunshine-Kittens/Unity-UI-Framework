using System.Collections;

using NUnit.Framework;

using UIFramework.Animation;
using UIFramework.Core.Interfaces;
using UIFramework.TestUtils;
using UIFramework.UGUI;

using UnityEngine;
using UnityEngine.Extension;
using UnityEngine.TestTools;

namespace UIFramework.Tests.PlayMode
{
    // A widget must come out of terminate -> re-initialize exactly as it went into its first initialize:
    // animated properties restored, the backend's interactivity in step with the flags, and the resting pose
    // re-captured from clean values. These all used to fail — animations left alpha/position/rotation/scale
    // wherever they stopped, nothing restored them, and a re-initialize adopted the displaced pose as home.
    public sealed class WidgetReinitializationTests
    {
        private static readonly Vector2 _HomePosition = new(31f, -17f);
        private static readonly Vector3 _HomeScale = new(1.3f, 1.3f, 1f);
        private static readonly Quaternion _HomeRotation = Quaternion.Euler(0f, 0f, 12f);

        private PoolScope _pools;
        private UguiWidgetFixture _fixture;

        [SetUp]
        public void SetUp()
        {
            _pools = new PoolScope();
            _fixture = new UguiWidgetFixture();
        }

        [TearDown]
        public void TearDown()
        {
            _fixture?.Dispose();
            _pools?.Dispose();
        }

        private UnityEngine.Awaitable StartFade(WidgetVisibility to, float length = 2f)
        {
            IAnimation fade = _fixture.Widget.GetGenericAnimation(GenericAnimation.Fade, to);
            return _fixture.Widget.AnimateVisibility(to)
                .WithAnimation(fade)
                .WithLength(length)
                .Animate();
        }

        // The fixture initializes at the identity pose, which a restore writing identity instead of the captured
        // pose would also satisfy. Re-initializing from a non-identity pose makes that pose the captured home.
        private void EstablishHome()
        {
            RectTransform rect = _fixture.Widget.RectTransform;
            _fixture.Widget.Terminate();
            rect.anchoredPosition = _HomePosition;
            rect.localScale = _HomeScale;
            rect.localRotation = _HomeRotation;
            _fixture.Widget.Initialize();
        }

        [UnityTest]
        public IEnumerator AnInterruptedFadeRestoresTheRestingState()
        {
            _ = StartFade(WidgetVisibility.Visible);
            yield return null;
            Assert.That(_fixture.Widget.IsAnimating, Is.True);
            Assert.That(_fixture.Widget.CanvasGroup.alpha, Is.LessThan(1f), "the fade is mid-flight");

            _fixture.Widget.SetVisibility(WidgetVisibility.Hidden);

            Assert.That(_fixture.Widget.CanvasGroup.alpha, Is.EqualTo(1f), "alpha is restored, not left mid-fade");
            Assert.That(_fixture.Widget.gameObject.activeSelf, Is.False);
        }

        [UnityTest]
        public IEnumerator ACompletedAnimatedHideEndsDeactivated()
        {
            _fixture.Widget.SetVisibility(WidgetVisibility.Visible);
            AwaitableProbe probe = AwaitableProbe.Watch(StartFade(WidgetVisibility.Hidden, 0.1f));

            yield return probe.WaitForCompletion();

            Assert.That(probe.Exception, Is.Null);
            Assert.That(_fixture.Widget.gameObject.activeSelf, Is.False,
                "an invisible widget must not stay active, eating input");
            Assert.That(_fixture.Widget.CanvasGroup.alpha, Is.EqualTo(1f), "and alpha is reset for the next show");
        }

        // Alpha returns to the last externally set opacity, not to 1, so a group's opacity survives an animation
        // ending.
        [UnityTest]
        public IEnumerator AnAnimationEndingRestoresTheExternallySetOpacity()
        {
            _fixture.Widget.SetOpacity(0.5f);
            AwaitableProbe probe = AwaitableProbe.Watch(StartFade(WidgetVisibility.Visible, 0.1f));
            Assert.That(_fixture.Widget.CanvasGroup.alpha, Is.EqualTo(0f).Within(0.01f),
                "the fade has displaced the set opacity, so the end value is a restore rather than a leftover");

            yield return probe.WaitForCompletion();

            Assert.That(probe.Exception, Is.Null);
            Assert.That(_fixture.Widget.CanvasGroup.alpha, Is.EqualTo(0.5f).Within(0.001f));
        }

        [Test]
        public void TerminationRestoresTheAnimatedPose()
        {
            EstablishHome();
            RectTransform rect = _fixture.Widget.RectTransform;
            rect.anchoredPosition = _HomePosition + new Vector2(370f, -120f);
            rect.localScale = new Vector3(0.4f, 0.4f, 1f);
            rect.localRotation = Quaternion.Euler(0f, 60f, 0f);

            _fixture.Widget.Terminate();

            Assert.That(rect.anchoredPosition, Is.EqualTo(_HomePosition), "position restored on teardown");
            Assert.That(rect.localScale, Is.EqualTo(_HomeScale), "scale restored on teardown");
            Assert.That(Quaternion.Angle(rect.localRotation, _HomeRotation), Is.LessThan(0.01f),
                "rotation restored on teardown");
        }

        [Test]
        public void ReinitializationDoesNotAdoptADisplacedPoseAsHome()
        {
            EstablishHome();
            RectTransform rect = _fixture.Widget.RectTransform;
            rect.anchoredPosition = _HomePosition + new Vector2(370f, -120f);

            _fixture.Widget.Terminate();
            _fixture.Widget.Initialize();

            // Displace again and terminate: if re-initialize had captured the displaced pose as home, this
            // teardown would restore to the displaced position rather than the original.
            rect.anchoredPosition = _HomePosition + new Vector2(-55f, 90f);
            _fixture.Widget.Terminate();

            Assert.That(rect.anchoredPosition, Is.EqualTo(_HomePosition));
        }

        [Test]
        public void ReinitializationPushesInteractivityToTheBackend()
        {
            _fixture.Widget.IsEnabled.Value = false;
            Assert.That(_fixture.Widget.CanvasGroup.interactable, Is.False);
            Assert.That(_fixture.Widget.CanvasGroup.blocksRaycasts, Is.False);

            _fixture.Widget.Terminate();
            _fixture.Widget.Initialize();

            Assert.That(_fixture.Widget.IsInteractable.Value, Is.True, "the flags reset with the lifecycle");
            Assert.That(_fixture.Widget.CanvasGroup.interactable, Is.True,
                "and the backend is brought in step, not left holding the previous lifecycle's values");
            Assert.That(_fixture.Widget.CanvasGroup.blocksRaycasts, Is.True);

            _fixture.Widget.IsEnabled.Value = false;
            Assert.That(_fixture.Widget.IsInteractable.Value, Is.False,
                "and the override count is balanced, so disabling still gates input after the cycle");
        }

        private sealed class TerminateObservingWidget : Widget
        {
            public int? SortOrderInOnTerminate;
            protected override void OnTerminate() => SortOrderInOnTerminate = GlobalSortOrder;
        }

        [Test]
        public void TeardownHooksStillSeeTheRenderHandles()
        {
            GameObject root = new("Canvas", typeof(RectTransform), typeof(Canvas));
            try
            {
                // A sibling ahead of the widget, so the live read is 1. A read after release throws today, since the
                // root is dereferenced unguarded; were it null-guarded like the render sort order, it would read 0,
                // which 1 stays distinct from.
                new GameObject("Sibling", typeof(RectTransform)).transform.SetParent(root.transform, false);
                GameObject widgetObject = new("Widget", typeof(RectTransform), typeof(CanvasGroup));
                widgetObject.transform.SetParent(root.transform, false);
                TerminateObservingWidget widget = widgetObject.AddComponent<TerminateObservingWidget>();
                widget.Initialize();
                int expected = widget.GlobalSortOrder;
                Assert.That(expected, Is.EqualTo(1));
                int? sortOrderInTerminated = null;
                widget.Terminated += _ => sortOrderInTerminated = widget.GlobalSortOrder;

                widget.Terminate();

                Assert.That(widget.SortOrderInOnTerminate, Is.EqualTo(expected),
                    "the subclass teardown hook runs before the render handles are released, mirroring OnInitialize");
                Assert.That(sortOrderInTerminated, Is.EqualTo(expected), "and so do Terminated subscribers");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }
    }
}
