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
    // Opacity, SetOpacity and GetGenericAnimation must read through those properties: a backing field stays
    // null until an accessor primes it. Initialize primes both — RectTransform while capturing the canvas root
    // and resting pose, CanvasGroup while pushing the enabled/interactable state — so only an uninitialized,
    // untouched widget can tell a field read from a property read.
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
