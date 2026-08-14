using NUnit.Framework;

using UIFramework.Controllers;
using UIFramework.Navigation;
using UIFramework.TestUtils;

using UnityEngine.Extension;

namespace UIFramework.Tests.EditMode
{
    // A group pushed but never navigated into must still collapse. It has no active screen, so the Exited
    // event that normally drives collapse can never fire — the controller pops such a group directly.
    //
    // This used to fail: Exit ran through the navigator, which reported failure on an empty navigator, so
    // nothing hid, nothing collapsed, and the layer below stayed paused permanently.
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
        public void ReturnCollapsesAGroupThatWasNeverNavigatedInto()
        {
            _controller.Return();

            Assert.That(_controller.Groups, Has.Count.EqualTo(1));
        }

        [Test]
        public void ExitCollapsesAGroupThatWasNeverNavigatedInto()
        {
            _controller.Exit(new ExitRequest());

            Assert.That(_controller.Groups, Has.Count.EqualTo(1));
        }

        [Test]
        public void CollapsingReportsTheScreenResumedUnderneath()
        {
            NavigateToResponse<Core.Interfaces.IScreen> response = _controller.Return();

            Assert.That(response.Result.Success, Is.True);
            Assert.That(response.Result.Active, Is.SameAs(_a), "the caller is told what is active now");
        }

        [Test]
        public void TheLayerBelowResumesAfterCollapse()
        {
            _controller.Return();

            Assert.That(_controller.Groups[0].IsInteractable.Value, Is.True);
            Assert.That(_a.IsInteractable.Value, Is.True, "and the resume reaches the held screens");
        }

        [Test]
        public void ReturnAtTheBaseGroupLeavesItStanding()
        {
            _controller.Return();   // collapses the overlay
            _controller.Return();   // base group has no history and nothing below it

            Assert.That(_controller.Groups, Has.Count.EqualTo(1));
            Assert.That(_controller.ActiveGroup.ActiveScreen, Is.SameAs(_a));
        }

        [Test]
        public void ACollapsedGroupCanBePushedAgain()
        {
            _controller.Return();

            _controller.PushGroup();

            Assert.That(_controller.Groups, Has.Count.EqualTo(2), "the pooled group is reusable");
            Assert.That(_a.IsInteractable.Value, Is.False, "and pausing still works on the second push");
        }
    }
}
