using System;

using NUnit.Framework;

using UIFramework.Animation;
using UIFramework.Core.Interfaces;
using UIFramework.TestUtils;

namespace UIFramework.Tests.PlayMode
{
    // Known defect: the uGUI Widget exposes a lazy CanvasGroup property but three members read the backing
    // field directly — Opacity, SetOpacity and GetGenericAnimation. Nothing in Initialize() touches the
    // property, and the only members that do are the enabled/interactable handlers, which run only when a
    // ScalarFlag actually changes value. So a freshly initialized widget throws on all three.
    //
    // This is reachable in normal use: ScreenGroup.Acquire calls SetOpacity on every screen it takes.
    //
    // These tests assert the defect as it stands; invert them when it is fixed.
    public sealed class UguiWidgetAccessorTests
    {
        [Test]
        public void ReadingOpacityOnAFreshWidgetThrows()
        {
            using UguiWidgetFixture fixture = new(primeCanvasGroup: false);

            Assert.That(() => _ = fixture.Widget.Opacity, Throws.TypeOf<NullReferenceException>());
        }

        [Test]
        public void SettingOpacityOnAFreshWidgetThrows()
        {
            using UguiWidgetFixture fixture = new(primeCanvasGroup: false);

            Assert.That(() => fixture.Widget.SetOpacity(0.5f), Throws.TypeOf<NullReferenceException>());
        }

        [Test]
        public void ResolvingAGenericAnimationOnAFreshWidgetThrows()
        {
            using UguiWidgetFixture fixture = new(primeCanvasGroup: false);

            Assert.That(() => fixture.Widget.GetGenericAnimation(GenericAnimation.Fade, WidgetVisibility.Visible),
                Throws.TypeOf<ArgumentNullException>());
        }

        [Test]
        public void TouchingTheCanvasGroupPropertyPrimesTheBackingField()
        {
            using UguiWidgetFixture fixture = new(primeCanvasGroup: true);

            Assert.That(fixture.Widget.Opacity, Is.EqualTo(1f),
                "the accessors work once something has read the property");
        }
    }
}
