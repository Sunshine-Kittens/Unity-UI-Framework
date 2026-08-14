using NUnit.Framework;

using UIFramework.Controllers;
using UIFramework.Navigation;
using UIFramework.TestUtils;

using UnityEngine.Extension;

namespace UIFramework.Tests.EditMode
{
    // Known defect: a group pushed but never navigated into cannot collapse. Collapse is driven only by
    // ScreenGroup's Exited event, which is raised from OnScreenHidden. Such a group has no active screen, so
    // WindowNavigator.Clear() returns Success=false, no screen hides, Exited never fires, and the group
    // cannot leave the stack.
    //
    // These tests assert the defect as it stands; invert them when it is fixed.
    public sealed class EmptyGroupCollapseTests
    {
        private FakeScreenA _a;
        private ScreenController _controller;

        [SetUp]
        public void SetUp()
        {
            _a = new FakeScreenA();
            _controller = new ScreenController(new[] { new FakeCollector(_a) }, TimeMode.Scaled, new ManualClock());
            _controller.Initialize();
            _controller.CreateNavigateToRequest<FakeScreenA>().Execute();
            _controller.PushGroup();
            Assume.That(_controller.Groups, Has.Count.EqualTo(2));
        }

        [Test]
        public void ReturnDoesNotCollapseAGroupThatWasNeverNavigatedInto()
        {
            _controller.Return();

            Assert.That(_controller.Groups, Has.Count.EqualTo(2), "the empty overlay stays on the stack");
        }

        [Test]
        public void ExitDoesNotCollapseAGroupThatWasNeverNavigatedInto()
        {
            _controller.Exit(new ExitRequest());

            Assert.That(_controller.Groups, Has.Count.EqualTo(2), "Exit hits the same dead end as Return");
        }

        [Test]
        public void TheLayerBelowStaysPausedAfterAFailedCollapse()
        {
            _controller.Return();

            Assert.That(_controller.Groups[0].IsInteractable.Value, Is.False,
                "nothing restores the base group, so it is paused for good");
        }

        [Test]
        public void RepeatedReturnsNeverRecover()
        {
            _controller.Return();
            _controller.Return();
            _controller.Return();

            Assert.That(_controller.Groups, Has.Count.EqualTo(2), "there is no retry path out of this state");
        }
    }
}
