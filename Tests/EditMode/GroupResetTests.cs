using NUnit.Framework;

using UIFramework.Core.Interfaces;
using UIFramework.Groups;
using UIFramework.Registry;
using UIFramework.TestUtils;

using UnityEngine.Extension;

namespace UIFramework.Tests.EditMode
{
    // Known defect: ScreenGroup.Reset() unsubscribes and clears navigation state but never forces held
    // screens hidden, so a collapsed group returns still-visible screens to the shared registry for the next
    // group to acquire.
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
        public void ResetLeavesReleasedScreensVisible()
        {
            _group.Reset();

            Assert.That(_a.Visibility, Is.EqualTo(WidgetVisibility.Visible),
                "the screen is released while still rendered");
        }

        [Test]
        public void ResetDetachesTheNavigator()
        {
            _group.Reset();

            Assert.That(_a.Navigator, Is.Null);
        }

        [Test]
        public void AReleasedScreenCarriesItsStateIntoTheNextGroup()
        {
            _group.Reset();
            ScreenGroup reused = new(_registry, TimeMode.Scaled, new ManualClock());

            Assert.That(_a.Visibility, Is.EqualTo(WidgetVisibility.Visible),
                "the next group acquires a screen that is already visible");
            Assert.That(reused.ActiveScreen, Is.Null, "even though the new group has navigated nowhere");
        }
    }
}
