using NUnit.Framework;

using UIFramework.Controllers;
using UIFramework.Core.Interfaces;
using UIFramework.TestUtils;

using UnityEngine.Extension;

namespace UIFramework.Tests.EditMode
{
    // The primary user flow: navigate from one screen to another within a group, then return. Nothing else
    // in the suite performs a successful two-screen navigation, so the forward swap, the history round-trip
    // and the in-group return are pinned here on the instant path.
    public sealed class NavigationFlowTests
    {
        private FakeScreenA _a;
        private FakeScreenB _b;
        private ScreenController _controller;

        [SetUp]
        public void SetUp()
        {
            _a = new FakeScreenA();
            _b = new FakeScreenB();
            _controller = new ScreenController(new[] { new FakeCollector(_a, _b) }, TimeMode.Scaled, new ManualClock());
            _controller.Initialize();
            _controller.CreateNavigateToRequest<FakeScreenA>().Execute();
            Assume.That(_a.Visibility, Is.EqualTo(WidgetVisibility.Visible));
        }

        [Test]
        public void ForwardNavigationSwapsScreens()
        {
            _controller.CreateNavigateToRequest<FakeScreenB>().Execute();

            Assert.That(_a.Visibility, Is.EqualTo(WidgetVisibility.Hidden), "the outgoing screen is hidden");
            Assert.That(_b.Visibility, Is.EqualTo(WidgetVisibility.Visible), "the incoming screen is shown");
            Assert.That(_controller.ActiveGroup.ActiveScreen, Is.SameAs(_b));
        }

        [Test]
        public void ForwardNavigationTracksThePreviousScreen()
        {
            _controller.CreateNavigateToRequest<FakeScreenB>().Execute();

            Assert.That(_controller.ActiveGroup.PreviousScreen, Is.SameAs(_a),
                "history records where Return would go");
        }

        // Broken today: the return path hands the transition its source and target swapped relative to the
        // forward path, so it asks to hide the already-hidden screen and show the already-visible one — a
        // no-op on every shape of transition. The navigator and history still advance, so the display
        // diverges from ActiveScreen. Red until the argument order is fixed.
        [Test]
        public void ReturnShowsThePreviousScreenAgain()
        {
            _controller.CreateNavigateToRequest<FakeScreenB>().Execute();

            _controller.Return();

            Assert.That(_controller.ActiveGroup.ActiveScreen, Is.SameAs(_a), "the navigator returns to A");
            Assert.That(_a.Visibility, Is.EqualTo(WidgetVisibility.Visible), "and A is actually shown again");
            Assert.That(_b.Visibility, Is.EqualTo(WidgetVisibility.Hidden), "and B is actually hidden");
        }

        [Test]
        public void ReturnConsumesTheHistoryEntry()
        {
            _controller.CreateNavigateToRequest<FakeScreenB>().Execute();

            _controller.Return();

            Assert.That(_controller.ActiveGroup.PreviousScreen, Is.Null,
                "one forward step and one return leave nothing to return to");
        }
    }
}
