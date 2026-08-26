using NUnit.Framework;

using UIFramework.Controllers;
using UIFramework.Core.Interfaces;
using UIFramework.Groups;
using UIFramework.TestUtils;

using UnityEngine.Extension;

namespace UIFramework.Tests.EditMode
{
    // Proves the fakes drive the real controller, registry, group and navigator from edit mode. Asserts
    // current behaviour only — characterization of the known defects lands separately.
    public sealed class HarnessSmokeTests
    {
        private ManualClock _clock;
        private FakeScreenA _a;
        private FakeScreenB _b;
        private ScreenController _controller;

        [SetUp]
        public void SetUp()
        {
            PoolRegistryReset();
            _clock = new ManualClock(0.25f);
            _a = new FakeScreenA();
            _b = new FakeScreenB();
            _controller = new ScreenController(new[] { new FakeCollector(_a, _b) }, TimeMode.Scaled, _clock);
        }

        [TearDown]
        public void TearDown() => PoolRegistryReset();

        private static void PoolRegistryReset()
        {
            using PoolScope _ = new();
        }

        [Test]
        public void InitializeCollectsAndInitializesScreens()
        {
            // Visible beforehand, so the Hidden assert below discriminates the controller's hook rather than
            // restating the fake's initial state.
            _a.SetVisibility(WidgetVisibility.Visible);

            _controller.Initialize();

            Assert.That(_controller.IsInitialized, Is.True);
            Assert.That(_a.State, Is.EqualTo(WidgetState.Initialized));
            Assert.That(_b.State, Is.EqualTo(WidgetState.Initialized));
            Assert.That(_a.Visibility, Is.EqualTo(WidgetVisibility.Hidden), "OnWidgetInitialize hides screens");
        }

        [Test]
        public void NavigateToMakesTheScreenActiveAndVisible()
        {
            _controller.Initialize();

            _controller.CreateNavigateToRequest<FakeScreenA>().Execute();

            Assert.That(_controller.ActiveGroup, Is.Not.Null);
            Assert.That(_controller.ActiveGroup.ActiveScreen, Is.SameAs(_a));
            Assert.That(_a.Visibility, Is.EqualTo(WidgetVisibility.Visible));
            Assert.That(_a.Navigator, Is.SameAs(_controller.ActiveGroup));
        }

        [Test]
        public void PushedGroupGetsTheNextLayerBand()
        {
            _controller.Initialize();
            _controller.CreateNavigateToRequest<FakeScreenA>().Execute();

            IScreenGroup overlay = _controller.PushGroup();
            _controller.CreateNavigateToRequest<FakeScreenB>().Execute();

            Assert.That(_controller.Groups, Has.Count.EqualTo(2));
            Assert.That(_controller.ActiveGroup, Is.SameAs(overlay));
            Assert.That(_a.GlobalSortOrder, Is.EqualTo(0));
            Assert.That(_b.GlobalSortOrder, Is.EqualTo(100), "band step is 100 per stacked group");
        }

        // IsInteractable.Value counts requests rather than assigning, so the group holds exactly one
        // outstanding decrement per held screen. This used to fail: joining a group also incremented the
        // count, so the pause's decrement never reached zero and the layer below stayed live.
        [Test]
        public void PushingAGroupPausesTheLayerBelow()
        {
            _controller.Initialize();
            _controller.CreateNavigateToRequest<FakeScreenA>().Execute();

            _controller.PushGroup();

            Assert.That(_controller.Groups[0].IsInteractable.Value, Is.False);
            Assert.That(_a.IsInteractable.Value, Is.False, "and the pause reaches the held screens");
        }

        [Test]
        public void AScreenJoiningAPausedGroupIsPausedToo()
        {
            _controller.Initialize();
            _controller.CreateNavigateToRequest<FakeScreenA>().Execute();
            _controller.PushGroup();
            Assume.That(_controller.Groups[0].IsInteractable.Value, Is.False, "the base group is paused");

            // Navigate the paused base group directly: _b joins while the group is paused, so it must take
            // the group's one outstanding decrement on entry rather than arriving live under an overlay.
            _controller.Groups[0].CreateNavigateToRequest<FakeScreenB>().Execute();

            Assert.That(_b.IsInteractable.Value, Is.False, "a screen joining a paused group joins paused");

            _controller.Return();   // collapse the overlay

            Assert.That(_b.IsInteractable.Value, Is.True, "and is given back the decrement on resume");
        }

        [Test]
        public void AScreenJoiningTheUnpausedTopGroupStaysInteractable()
        {
            _controller.Initialize();
            _controller.CreateNavigateToRequest<FakeScreenA>().Execute();
            _controller.PushGroup();

            // _b joins the overlay, which is itself live; _a stays paused under it.
            _controller.CreateNavigateToRequest<FakeScreenB>().Execute();

            Assert.That(_b.IsInteractable.Value, Is.True, "the top group is not paused");
            Assert.That(_a.IsInteractable.Value, Is.False);
        }

        [Test]
        public void TickDrivesHeldScreensWithTheInjectedDelta()
        {
            _controller.Initialize();
            _controller.CreateNavigateToRequest<FakeScreenA>().Execute();

            _controller.Tick();
            _clock.Delta = 0.5f;
            _controller.Tick();

            Assert.That(_a.Ticks, Is.EqualTo(new[] { 0.25f, 0.5f }));
            Assert.That(_clock.Queries, Has.Count.EqualTo(2));
        }

        [Test]
        public void OccupancyGuardRejectsAScreenHeldByAnotherGroup()
        {
            _controller.Initialize();
            _controller.CreateNavigateToRequest<FakeScreenA>().Execute();
            _controller.PushGroup();

            // Matched on the message: several navigation paths throw InvalidOperationException, and the
            // fake's navigator methods are unguarded, so the type alone does not pin the occupancy guard.
            Assert.That(() => _controller.CreateNavigateToRequest<FakeScreenA>().Execute(),
                Throws.InvalidOperationException.With.Message.Contains("already held by another group"));
        }
    }
}
