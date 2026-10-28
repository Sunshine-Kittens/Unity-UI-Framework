using NUnit.Framework;

using UIFramework.Animation;
using UIFramework.Core.Interfaces;
using UIFramework.TestUtils;

namespace UIFramework.Tests.PlayMode
{
    // The uGUI Widget's RectTransform and CanvasGroup are components on its own GameObject, so they are
    // fetched lazily through properties rather than acquired at initialization — they outlive every lifecycle
    // transition, unlike the parent Canvas.
    //
    // This used to fail: Opacity, SetOpacity and GetGenericAnimation read the backing fields instead, and
    // nothing in initialization touched the CanvasGroup property, so that field stayed null and all three threw
    // on a freshly initialized widget. The only members that did read that property were the enabled/interactable
    // handlers, which run only when a ScalarFlag actually changes value — so whether a widget worked depended
    // on whether something had happened to prime it. Reachable in ordinary use: a group calls SetOpacity on
    // every screen it acquires.
    //
    // Initialize now reads both properties itself — RectTransform while capturing the canvas root and resting
    // pose, CanvasGroup while pushing the enabled/interactable state — priming both fields as a side effect, so
    // only an uninitialized widget that no other accessor has touched yet can still tell the lazy path from the
    // field.
    public sealed class UguiWidgetAccessorTests
    {
        [Test]
        public void AFreshWidgetReportsItsOpacity()
        {
            using UguiWidgetFixture fixture = new();

            Assert.That(fixture.Widget.Opacity, Is.EqualTo(1f));
        }

        [Test]
        public void AnUninitializedWidgetAcceptsAnOpacityChange()
        {
            using UguiWidgetFixture fixture = new(initialize: false);

            fixture.Widget.SetOpacity(0.5f);

            Assert.That(fixture.Widget.Opacity, Is.EqualTo(0.5f));
        }

        [Test]
        public void AnUninitializedWidgetResolvesAGenericShowAnimation()
        {
            using UguiWidgetFixture fixture = new(initialize: false);

            Assert.That(fixture.Widget.GetGenericAnimation(GenericAnimation.Fade, WidgetVisibility.Visible),
                Is.Not.Null);
        }

        // A widget of its own: resolving the show animation first would prime both fields through the properties
        // and hide a hide branch that reads the fields.
        [Test]
        public void AnUninitializedWidgetResolvesAGenericHideAnimation()
        {
            using UguiWidgetFixture fixture = new(initialize: false);

            Assert.That(fixture.Widget.GetGenericAnimation(GenericAnimation.Fade, WidgetVisibility.Hidden),
                Is.Not.Null);
        }

        // Nothing has touched a property on this widget at all, so it exercises the lazy path from cold.
        [Test]
        public void AnUninitializedWidgetReportsItsOpacity()
        {
            using UguiWidgetFixture fixture = new(initialize: false);

            Assert.That(fixture.Widget.Opacity, Is.EqualTo(1f));
        }
    }
}
