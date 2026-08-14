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

        // Known defect: the group reports itself paused, but its screens stay interactable. ScalarFlag.Value
        // is a counter, and Acquire's `screen.IsInteractable.Value = _isInteractable.Value` raises the
        // screen's count to 2, so the pause's single decrement lands on 1 — still truthy.
        // When that is fixed, the second assertion below should become Is.False.
        [Test]
        public void PushingAGroupDoesNotActuallyPauseTheLayerBelow()
        {
            _controller.Initialize();
            _controller.CreateNavigateToRequest<FakeScreenA>().Execute();

            _controller.PushGroup();

            Assert.That(_controller.Groups[0].IsInteractable.Value, Is.False, "the group considers itself paused");
            Assert.That(_a.IsInteractable.Value, Is.True, "but the held screen is still interactable");
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

            Assert.That(() => _controller.CreateNavigateToRequest<FakeScreenA>().Execute(),
                Throws.InvalidOperationException);
        }
    }
}
