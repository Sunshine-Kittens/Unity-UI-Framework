using System;
using System.Collections;

using NUnit.Framework;

using UIFramework.Animation;
using UIFramework.Core.Interfaces;
using UIFramework.TestUtils;

using UnityEngine;
using UnityEngine.Extension;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

using UitkWidget = UIFramework.UIToolkit.Widget;

namespace UIFramework.Tests.PlayMode
{
    // The UI Toolkit half of the reset guards. Animations write inline translate, rotate, scale and opacity on
    // the element; every way an animation ends (completion, interruption, terminate) hands the transforms back
    // to the stylesheet and the opacity back to the last value set from outside. Opacity is read as the element
    // resolves it, which is what is displayed, and is set to a value other than 1 so a restore is told apart
    // from a reset to the default.
    public sealed class UitkWidgetReinitializationTests
    {
        private const float _RestingOpacity = 0.5f;

        private UitkDocumentFixture _fixture;

        [SetUp]
        public void SetUp() => _fixture = new UitkDocumentFixture();

        [UnityTearDown]
        public IEnumerator TearDown() => _fixture.Close();

        private static Awaitable StartAnimation(UitkWidget widget, GenericAnimation type, WidgetVisibility to,
            float length = 2f)
        {
            IAnimation animation = widget.GetGenericAnimation(type, to);
            return widget.AnimateVisibility(to)
                .WithAnimation(animation)
                .WithLength(length)
                .Animate();
        }

        [UnityTest]
        public IEnumerator AnInterruptedUitkSlideRestoresTheRestingState()
        {
            UitkWidget widget = _fixture.AddBoundWidget<UitkWidget>("widget");
            VisualElement element = _fixture.Element("widget");
            widget.SetOpacity(_RestingOpacity);
            StyleTranslate home = element.style.translate;
            AwaitableProbe probe = _fixture.Watch(StartAnimation(widget, GenericAnimation.SlideFromLeft, WidgetVisibility.Visible));
            yield return null;
            Assert.That(widget.IsAnimating, Is.True, "arrange: the slide is mid-flight");
            Assert.That(element.style.translate, Is.Not.EqualTo(home), "arrange: the slide has displaced the element");
            Assert.That(element.resolvedStyle.opacity, Is.LessThan(_RestingOpacity), "arrange: and its fade the set opacity");

            widget.SetVisibility(WidgetVisibility.Hidden);

            Assert.That(element.style.translate, Is.EqualTo(home), "the pose is restored, not left mid-slide");
            Assert.That(element.resolvedStyle.opacity, Is.EqualTo(_RestingOpacity).Within(0.001f),
                "and the set opacity, not the fade's");
            Assert.That(element.resolvedStyle.display, Is.EqualTo(DisplayStyle.None));
            Assert.That(widget.gameObject.activeSelf, Is.False);

            yield return probe.WaitForCompletion();
            Assert.That(probe.Exception, Is.TypeOf<OperationCanceledException>());
        }

        [UnityTest]
        public IEnumerator ACompletedUitkHideEndsHidden()
        {
            UitkWidget widget = _fixture.AddBoundWidget<UitkWidget>("widget");
            VisualElement element = _fixture.Element("widget");
            widget.SetOpacity(_RestingOpacity);
            widget.SetVisibility(WidgetVisibility.Visible);
            Assert.That(element.resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex), "arrange: the widget is shown");
            AwaitableProbe probe = _fixture.Watch(StartAnimation(widget, GenericAnimation.Fade, WidgetVisibility.Hidden, 0.1f));

            yield return probe.WaitForCompletion();

            Assert.That(probe.Exception, Is.Null);
            Assert.That(widget.gameObject.activeSelf, Is.False, "an invisible widget must not stay active, eating input");
            Assert.That(element.resolvedStyle.display, Is.EqualTo(DisplayStyle.None), "and its element is not displayed");
            Assert.That(element.resolvedStyle.opacity, Is.EqualTo(_RestingOpacity).Within(0.001f),
                "and the set opacity is back for the next show");
        }

        [UnityTest]
        public IEnumerator AUitkAnimationEndingRestoresTheSetOpacity()
        {
            UitkWidget widget = _fixture.AddBoundWidget<UitkWidget>("widget");
            VisualElement element = _fixture.Element("widget");
            widget.SetOpacity(_RestingOpacity);
            Assert.That(element.resolvedStyle.opacity, Is.EqualTo(_RestingOpacity).Within(0.001f),
                "arrange: the element shows the set opacity");
            AwaitableProbe probe = _fixture.Watch(StartAnimation(widget, GenericAnimation.Fade, WidgetVisibility.Visible, 0.1f));
            Assert.That(element.resolvedStyle.opacity, Is.EqualTo(0f).Within(0.01f),
                "arrange: the fade has displaced the set opacity, so the end value is a restore rather than a leftover");

            yield return probe.WaitForCompletion();

            Assert.That(probe.Exception, Is.Null);
            Assert.That(element.resolvedStyle.opacity, Is.EqualTo(_RestingOpacity).Within(0.001f));
        }

        [Test]
        public void TerminatingAUitkWidgetRestoresTheAnimatedPose()
        {
            UitkWidget widget = _fixture.AddBoundWidget<UitkWidget>("widget");
            VisualElement element = _fixture.Element("widget");
            widget.SetVisibility(WidgetVisibility.Visible);
            widget.SetOpacity(_RestingOpacity);
            StyleTranslate homeTranslate = element.style.translate;
            StyleRotate homeRotate = element.style.rotate;
            StyleScale homeScale = element.style.scale;
            element.style.translate = new Translate(40f, -25f);
            element.style.rotate = new Rotate(60f);
            element.style.scale = new Scale(new Vector2(0.4f, 0.4f));
            element.style.opacity = 0.2f;
            Assert.That(element.style.translate, Is.Not.EqualTo(homeTranslate), "arrange: translate is displaced");
            Assert.That(element.style.rotate, Is.Not.EqualTo(homeRotate), "arrange: rotate is displaced");
            Assert.That(element.style.scale, Is.Not.EqualTo(homeScale), "arrange: scale is displaced");
            Assert.That(element.resolvedStyle.opacity, Is.EqualTo(0.2f).Within(0.001f), "arrange: opacity is displaced");

            widget.Terminate();

            Assert.That(element.style.translate, Is.EqualTo(homeTranslate), "translate is handed back on teardown");
            Assert.That(element.style.rotate, Is.EqualTo(homeRotate), "rotate is handed back on teardown");
            Assert.That(element.style.scale, Is.EqualTo(homeScale), "scale is handed back on teardown");
            Assert.That(element.resolvedStyle.opacity, Is.EqualTo(_RestingOpacity).Within(0.001f),
                "and opacity returns to the set value");
        }

        [UnityTest]
        public IEnumerator TerminatingAUitkWidgetMidAnimationStopsItsWrites()
        {
            UitkWidget widget = _fixture.AddBoundWidget<UitkWidget>("widget");
            VisualElement element = _fixture.Element("widget");
            widget.SetOpacity(_RestingOpacity);
            StyleTranslate home = element.style.translate;
            AwaitableProbe probe = _fixture.Watch(StartAnimation(widget, GenericAnimation.SlideFromLeft, WidgetVisibility.Visible));
            yield return null;
            Assert.That(widget.IsAnimating, Is.True, "arrange: the slide is mid-flight");
            Assert.That(element.style.translate, Is.Not.EqualTo(home), "arrange: the slide has displaced the element");
            Assert.That(element.resolvedStyle.opacity, Is.LessThan(_RestingOpacity), "arrange: and its fade the set opacity");

            widget.Terminate();
            yield return probe.WaitForCompletion();

            Assert.That(probe.Exception, Is.TypeOf<OperationCanceledException>(), "terminating cancels the animation");
            Assert.That(element.style.translate, Is.EqualTo(home));
            Assert.That(element.resolvedStyle.opacity, Is.EqualTo(_RestingOpacity).Within(0.001f));

            float until = Time.realtimeSinceStartup + 0.1f;
            while (Time.realtimeSinceStartup < until)
                yield return null;

            Assert.That(element.style.translate, Is.EqualTo(home), "nothing writes to the element after teardown");
            Assert.That(element.resolvedStyle.opacity, Is.EqualTo(_RestingOpacity).Within(0.001f));
            Assert.That(_fixture.PooledHandles, Is.EqualTo(1), "the cancelled animation's handle is back in the pool");
        }

        [Test]
        public void ReinitializingAUitkWidgetPushesInteractivityToItsElement()
        {
            UitkWidget widget = _fixture.AddBoundWidget<UitkWidget>("widget");
            VisualElement element = _fixture.Element("widget");
            VisualElement child = _fixture.AddElement("child", element);
            widget.IsEnabled.Value = false;
            Assert.That(element.enabledSelf, Is.False, "arrange: the element is disabled");
            Assert.That(child.pickingMode, Is.EqualTo(PickingMode.Ignore), "arrange: and ignores the pointer");

            widget.Terminate();
            widget.Initialize();

            Assert.That(widget.IsInteractable.Value, Is.True, "the flags reset with the lifecycle");
            Assert.That(element.enabledSelf, Is.True,
                "and the element is brought in step, not left holding the previous lifecycle's values");
            Assert.That(element.pickingMode, Is.EqualTo(PickingMode.Position));
            Assert.That(child.pickingMode, Is.EqualTo(PickingMode.Position));

            widget.IsEnabled.Value = false;
            Assert.That(widget.IsInteractable.Value, Is.False,
                "and the override count is balanced, so disabling still gates input after the cycle");
        }

        private sealed class TerminateObservingWidget : UitkWidget
        {
            public VisualElement Element => VisualElement;
            public VisualElement ElementInOnTerminate;
            public int? SortOrderInOnTerminate;

            protected override void OnTerminate()
            {
                ElementInOnTerminate = VisualElement;
                SortOrderInOnTerminate = LocalSortOrder;
            }
        }

        [Test]
        public void UitkTeardownHooksStillSeeTheElement()
        {
            // A sibling before the element, so a live read is 1, distinct from the 0 an unbound read could give.
            _fixture.AddElement("sibling");
            VisualElement element = _fixture.AddElement("widget");
            TerminateObservingWidget widget = _fixture.AddWidget<TerminateObservingWidget>("widget");
            widget.Initialize();
            Assert.That(widget.Element, Is.SameAs(element), "arrange: the widget is bound to the element");
            Assert.That(widget.LocalSortOrder, Is.EqualTo(1), "arrange: the element comes after its sibling");
            VisualElement elementInTerminated = null;
            int? sortOrderInTerminated = null;
            widget.Terminated += _ =>
            {
                elementInTerminated = widget.Element;
                sortOrderInTerminated = widget.LocalSortOrder;
            };

            widget.Terminate();

            Assert.That(widget.ElementInOnTerminate, Is.SameAs(element),
                "the subclass teardown hook runs before the element is released, mirroring OnInitialize");
            Assert.That(widget.SortOrderInOnTerminate, Is.EqualTo(1));
            Assert.That(elementInTerminated, Is.SameAs(element), "and so do Terminated subscribers");
            Assert.That(sortOrderInTerminated, Is.EqualTo(1));
        }
    }
}
