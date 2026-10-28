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

        [UnityTest]
        public IEnumerator AnInterruptedFadeRestoresTheRestingState()
        {
            _ = StartFade(WidgetVisibility.Visible);
            yield return null;
            Assume.That(_fixture.Widget.IsAnimating, Is.True);
            Assume.That(_fixture.Widget.CanvasGroup.alpha, Is.LessThan(1f), "the fade is mid-flight");

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

        [Test]
        public void TerminationRestoresTheAnimatedPose()
        {
            RectTransform rect = _fixture.Widget.RectTransform;
            Vector2 home = rect.anchoredPosition;
            rect.anchoredPosition = home + new Vector2(370f, -120f);
            rect.localScale = new Vector3(0.4f, 0.4f, 1f);

            _fixture.Widget.Terminate();

            Assert.That(rect.anchoredPosition, Is.EqualTo(home), "position restored on teardown");
            Assert.That(rect.localScale, Is.EqualTo(Vector3.one), "scale restored on teardown");
        }

        [Test]
        public void ReinitializationDoesNotAdoptADisplacedPoseAsHome()
        {
            RectTransform rect = _fixture.Widget.RectTransform;
            Vector2 home = rect.anchoredPosition;
            rect.anchoredPosition = home + new Vector2(370f, -120f);

            _fixture.Widget.Terminate();
            _fixture.Widget.Initialize();

            // Displace again and terminate: if re-initialize had captured the displaced pose as home, this
            // teardown would restore to the displaced position rather than the original.
            rect.anchoredPosition = home + new Vector2(-55f, 90f);
            _fixture.Widget.Terminate();

            Assert.That(rect.anchoredPosition, Is.EqualTo(home));
        }

        [Test]
        public void ReinitializationPushesInteractivityToTheBackend()
        {
            _fixture.Widget.IsEnabled.Value = false;
            Assume.That(_fixture.Widget.CanvasGroup.interactable, Is.False);
            Assume.That(_fixture.Widget.CanvasGroup.blocksRaycasts, Is.False);

            _fixture.Widget.Terminate();
            _fixture.Widget.Initialize();

            Assert.That(_fixture.Widget.IsInteractable.Value, Is.True, "the flags reset with the lifecycle");
            Assert.That(_fixture.Widget.CanvasGroup.interactable, Is.True,
                "and the backend is brought in step, not left holding the previous lifecycle's values");
            Assert.That(_fixture.Widget.CanvasGroup.blocksRaycasts, Is.True);
        }

        private sealed class TerminateObservingWidget : Widget
        {
            public int? SortOrderInOnTerminate;
            protected override void OnTerminate() => SortOrderInOnTerminate = GlobalSortOrder;
        }

        [Test]
        public void OnTerminateStillSeesTheRenderHandles()
        {
            GameObject root = new("Canvas", typeof(RectTransform), typeof(Canvas));
            try
            {
                GameObject widgetObject = new("Widget", typeof(RectTransform), typeof(CanvasGroup));
                widgetObject.transform.SetParent(root.transform, false);
                TerminateObservingWidget widget = widgetObject.AddComponent<TerminateObservingWidget>();
                widget.Initialize();

                widget.Terminate();

                Assert.That(widget.SortOrderInOnTerminate, Is.Not.Null,
                    "the subclass teardown hook runs before the render handles are released, mirroring OnInitialize");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }
    }
}
