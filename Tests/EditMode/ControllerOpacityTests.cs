using NUnit.Framework;

using UIFramework.Controllers;
using UIFramework.Groups;
using UIFramework.TestUtils;

using UnityEngine.Extension;

namespace UIFramework.Tests.EditMode
{
    // A controller's opacity fades everything it manages: each screen ends up at its group's opacity scaled
    // by the controller's, whichever is set first and however the group stack changes afterwards.
    public sealed class ControllerOpacityTests
    {
        private PoolScope _pools;
        private FakeScreenA _a;
        private FakeScreenB _b;
        private FakeScreenC _c;
        private ScreenController _controller;

        [SetUp]
        public void SetUp()
        {
            _pools = new PoolScope();
            _a = new FakeScreenA();
            _b = new FakeScreenB();
            _c = new FakeScreenC();
            _controller = new ScreenController(new[] { new FakeCollector(_a, _b, _c) }, TimeMode.Scaled, new ManualClock());
            _controller.Initialize();
        }

        [TearDown]
        public void TearDown() => _pools?.Dispose();

        [Test]
        public void ControllerOpacityScalesEveryGroupsScreens()
        {
            _controller.CreateNavigateToRequest<FakeScreenA>().Execute();
            IScreenGroup overlay = _controller.PushGroup();
            _controller.CreateNavigateToRequest<FakeScreenB>().Execute();
            overlay.SetOpacity(0.5f);

            _controller.SetOpacity(0.5f);

            Assert.That(_controller.Opacity, Is.EqualTo(0.5f));
            Assert.That(_a.Opacity, Is.EqualTo(0.5f), "base group at full opacity");
            Assert.That(_b.Opacity, Is.EqualTo(0.25f), "overlay at half opacity");
        }

        [Test]
        public void GroupOpacityKeepsTheControllerFactor()
        {
            _controller.CreateNavigateToRequest<FakeScreenA>().Execute();
            _controller.SetOpacity(0.5f);

            _controller.ActiveGroup.SetOpacity(0.5f);

            Assert.That(_a.Opacity, Is.EqualTo(0.25f));
            Assert.That(_controller.ActiveGroup.Opacity, Is.EqualTo(0.5f), "a group reports its own opacity");
        }

        [Test]
        public void AScreenJoiningLaterGetsTheCombinedOpacity()
        {
            _controller.SetOpacity(0.4f);

            _controller.CreateNavigateToRequest<FakeScreenA>().Execute();

            Assert.That(_a.Opacity, Is.EqualTo(0.4f).Within(1e-6f));
        }

        [Test]
        public void AReusedGroupTakesTheCurrentControllerOpacity()
        {
            _controller.CreateNavigateToRequest<FakeScreenA>().Execute();
            IScreenGroup first = _controller.PushGroup();
            _controller.CreateNavigateToRequest<FakeScreenB>().Execute();
            _controller.SetOpacity(0.5f);
            _controller.Return();
            Assume.That(_controller.Groups, Has.Count.EqualTo(1), "returning from the overlay's root collapses it");

            _controller.SetOpacity(0.8f);
            IScreenGroup second = _controller.PushGroup();
            _controller.CreateNavigateToRequest<FakeScreenC>().Execute();

            Assert.That(second, Is.SameAs(first), "the collapsed group is reused");
            Assert.That(_c.Opacity, Is.EqualTo(0.8f).Within(1e-6f));
        }

        [Test]
        public void AGroupUnderAFullyTransparentControllerIsNotVisible()
        {
            _controller.CreateNavigateToRequest<FakeScreenA>().Execute();
            Assume.That(_controller.ActiveGroup.IsVisible, Is.True);

            _controller.SetOpacity(0f);

            Assert.That(_controller.ActiveGroup.IsVisible, Is.False);
        }
    }
}
