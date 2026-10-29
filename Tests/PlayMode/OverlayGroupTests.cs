using System.Collections;

using NUnit.Framework;

using UIFramework.Controllers;
using UIFramework.Core.Interfaces;
using UIFramework.TestUtils;

using UnityEngine.TestTools;

namespace UIFramework.Tests.PlayMode
{
    // An overlay is additive: the layer below stays on screen and only stops taking input, which on uGUI means
    // its screens stop blocking raycasts. Collapsing the overlay hands input back.
    public sealed class OverlayGroupTests
    {
        private UguiScreenFixture _fixture;
        private UguiScreenA _a;
        private UguiScreenB _b;
        private UguiScreenC _c;
        private ScreenController _controller;

        [SetUp]
        public void SetUp()
        {
            _fixture = new UguiScreenFixture();
            _a = _fixture.AddScreen<UguiScreenA>();
            _b = _fixture.AddScreen<UguiScreenB>();
            _c = _fixture.AddScreen<UguiScreenC>();
            _controller = _fixture.CreateController(_a, _b, _c);
            _fixture.Instant(_controller.CreateNavigateToRequest<UguiScreenA>().Execute());
            _controller.PushGroup();
            _fixture.Instant(_controller.CreateNavigateToRequest<UguiScreenB>().Execute());
        }

        [UnityTearDown]
        public IEnumerator TearDown() => _fixture.Close();

        [Test]
        public void TheLayerBelowStaysRendered()
        {
            Assert.That(_a.gameObject.activeInHierarchy, Is.True);
            Assert.That(_a.Visibility, Is.EqualTo(WidgetVisibility.Visible));
            Assert.That(_a.CanvasGroup.alpha, Is.EqualTo(1f));
            Assert.That(_controller.Groups[0].IsVisible, Is.True);
        }

        [Test]
        public void TheLayerBelowStopsTakingInput()
        {
            Assert.That(_a.CanvasGroup.blocksRaycasts, Is.False);
            Assert.That(_b.CanvasGroup.blocksRaycasts, Is.True, "the overlay itself takes input");
        }

        [Test]
        public void CollapsingTheOverlayRestoresInputBelow()
        {
            _fixture.Instant(_controller.Return());

            Assert.That(_controller.Groups, Has.Count.EqualTo(1), "arrange: the overlay collapsed");
            Assert.That(_a.CanvasGroup.blocksRaycasts, Is.True);
        }

        // A screen shown in the paused layer after the overlay opened is paused with it, and resumes with it.
        [Test]
        public void AScreenJoiningThePausedLayerIsPausedToo()
        {
            _fixture.Instant(_controller.Groups[0].CreateNavigateToRequest<UguiScreenC>().Execute());

            Assert.That(_c.Visibility, Is.EqualTo(WidgetVisibility.Visible), "arrange: the paused layer shows it");
            Assert.That(_c.CanvasGroup.blocksRaycasts, Is.False);

            _fixture.Instant(_controller.Return());

            Assert.That(_c.CanvasGroup.blocksRaycasts, Is.True);
        }
    }
}
