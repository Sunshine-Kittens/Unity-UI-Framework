using NUnit.Framework;

using UIFramework.Core.Interfaces;
using UIFramework.Groups;
using UIFramework.Registry;
using UIFramework.TestUtils;

using UnityEngine.Extension;

namespace UIFramework.Tests.EditMode
{
    // Resetting a group returns its screens to the shared registry for another group to acquire, so each one
    // must leave in a clean state: hidden, detached from the navigator, and interactable.
    //
    // This used to fail: Reset cleared navigation state but left screens rendered, so the next group acquired
    // a screen that was already on screen.
    //
    // Reset is controller-facing and not on IScreenGroup, so the group is built directly here.
    public sealed class GroupResetTests
    {
        private WidgetRegistry<IScreen> _registry;
        private FakeScreenA _a;
        private ScreenGroup _group;

        [SetUp]
        public void SetUp()
        {
            _a = new FakeScreenA();
            _registry = new WidgetRegistry<IScreen>();
            _registry.Register(_a);
            _registry.Initialize();
            _group = new ScreenGroup(_registry, TimeMode.Scaled, new ManualClock());

            _group.CreateNavigateToRequest(_a).Execute();
            Assume.That(_a.Visibility, Is.EqualTo(WidgetVisibility.Visible));
            Assume.That(_a.Navigator, Is.SameAs(_group));
        }

        [Test]
        public void ResetHidesReleasedScreens()
        {
            _group.Reset();

            Assert.That(_a.Visibility, Is.EqualTo(WidgetVisibility.Hidden));
        }

        [Test]
        public void ResetDetachesTheNavigator()
        {
            _group.Reset();

            Assert.That(_a.Navigator, Is.Null);
        }

        [Test]
        public void ResetHidesWithoutReenteringTheGroupsOwnEvents()
        {
            bool exited = false;
            _group.Exited += () => exited = true;

            _group.Reset();

            Assert.That(exited, Is.False,
                "screens are unsubscribed before being hidden, so the presentation state machine is not re-entered");
        }

        [Test]
        public void ReleasedScreensAreLeftInteractableEvenIfTheGroupWasPaused()
        {
            _group.SetInteractable(false);
            Assume.That(_a.IsInteractable.Value, Is.False);

            _group.Reset();

            Assert.That(_a.IsInteractable.Value, Is.True, "the group's outstanding request is given back");
        }

        [Test]
        public void AReleasedScreenIsCleanForTheNextGroup()
        {
            _group.Reset();
            ScreenGroup reused = new(_registry, TimeMode.Scaled, new ManualClock());

            Assert.That(_a.Visibility, Is.EqualTo(WidgetVisibility.Hidden));
            Assert.That(reused.ActiveScreen, Is.Null);

            reused.CreateNavigateToRequest(_a).Execute();

            Assert.That(reused.ActiveScreen, Is.SameAs(_a));
            Assert.That(_a.Visibility, Is.EqualTo(WidgetVisibility.Visible), "and can be shown again");
        }
    }
}
