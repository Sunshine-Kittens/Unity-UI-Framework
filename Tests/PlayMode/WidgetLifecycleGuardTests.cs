using NUnit.Framework;

using UIFramework.Core.Interfaces;
using UIFramework.TestUtils;

using UnityEngine;

namespace UIFramework.Tests.PlayMode
{
    // The lifecycle rule is enforced once, in WidgetBase, rather than restated by each backend. Both backends
    // therefore fail identically on a bad transition; a caller that cannot know the current state — the UI
    // Toolkit backend reacting to document callbacks, or a parent cascading to children — tests CanInitialize
    // or CanTerminate first instead of relying on the callee to be lenient.
    //
    // Driven through the uGUI backend because it is the one with a real render hierarchy in a test scene.
    public sealed class WidgetLifecycleGuardTests
    {
        [Test]
        public void InitializingAnInitializedWidgetThrows()
        {
            using UguiWidgetFixture fixture = new();

            Assert.That(fixture.Widget.CanInitialize, Is.False);
            Assert.That(() => fixture.Widget.Initialize(), Throws.InvalidOperationException);
        }

        [Test]
        public void TerminatingAWidgetThatWasNeverInitializedThrows()
        {
            using UguiWidgetFixture fixture = new(initialize: false);

            Assert.That(fixture.Widget.CanTerminate, Is.False);
            Assert.That(() => fixture.Widget.Terminate(), Throws.InvalidOperationException);
        }

        [Test]
        public void TerminatingTwiceThrows()
        {
            using UguiWidgetFixture fixture = new();
            fixture.Widget.Terminate();

            Assert.That(() => fixture.Widget.Terminate(), Throws.InvalidOperationException,
                "callers that cannot know the state guard before calling rather than the widget going quiet");
        }

        // A guard placed after any of the work still throws, so the throw alone does not show that it runs
        // first. A rejected call must leave the widget exactly as it found it.
        [Test]
        public void ARejectedInitializeLeavesTheWidgetUntouched()
        {
            using UguiWidgetFixture fixture = new();
            fixture.Widget.SetVisibility(WidgetVisibility.Visible);
            RectTransform rect = fixture.Widget.RectTransform;
            Vector2 home = rect.anchoredPosition;
            rect.anchoredPosition = home + new Vector2(40f, -25f);
            int initialized = 0;
            fixture.Widget.Initialized += _ => initialized++;

            Assert.That(() => fixture.Widget.Initialize(), Throws.InvalidOperationException);

            Assert.That(initialized, Is.Zero);
            Assert.That(fixture.Widget.Visibility, Is.EqualTo(WidgetVisibility.Visible));
            Assert.That(fixture.Widget.gameObject.activeSelf, Is.True, "a shown widget stays shown");

            fixture.Widget.Terminate();

            Assert.That(rect.anchoredPosition, Is.EqualTo(home),
                "teardown restores the pose captured by the first initialize, not one captured by the rejected call");
        }

        [Test]
        public void ARejectedTerminateRaisesNoTerminating()
        {
            using UguiWidgetFixture fixture = new(initialize: false);
            int terminating = 0;
            fixture.Widget.Terminating += _ => terminating++;

            Assert.That(() => fixture.Widget.Terminate(), Throws.InvalidOperationException);
            Assert.That(terminating, Is.Zero, "a widget that was never initialized");

            fixture.Widget.Initialize();
            fixture.Widget.Terminate();
            Assert.That(() => fixture.Widget.Terminate(), Throws.InvalidOperationException);

            Assert.That(terminating, Is.EqualTo(1), "only the accepted terminate is announced");
        }

        [Test]
        public void ATerminatedWidgetCanInitializeAgain()
        {
            using UguiWidgetFixture fixture = new();
            fixture.Widget.Terminate();

            Assert.That(fixture.Widget.CanInitialize, Is.True);
            Assert.That(fixture.Widget.State, Is.EqualTo(WidgetState.Terminated));

            fixture.Widget.Initialize();

            Assert.That(fixture.Widget.State, Is.EqualTo(WidgetState.Initialized));
        }

        // Terminate releases the backend's render handles and Initialize acquires them again, so a recycled
        // widget must not be left reading the ones it dropped.
        [Test]
        public void ReinitializingReacquiresTheRenderHandles()
        {
            using UguiWidgetFixture fixture = new();
            fixture.Widget.Terminate();
            fixture.Widget.Initialize();

            Assert.That(() => fixture.Widget.GlobalSortOrder, Throws.Nothing);
            fixture.Widget.SetGlobalSortOrder(0);
            Assert.That(fixture.Widget.GlobalSortOrder, Is.EqualTo(0));
        }
    }
}
