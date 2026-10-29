using System.Collections;

using NUnit.Framework;

using UIFramework.Animation;
using UIFramework.Controllers;
using UIFramework.Core.Interfaces;
using UIFramework.Groups;
using UIFramework.Navigation;
using UIFramework.TestUtils;
using UIFramework.Transitioning;

using UnityEngine.Extension;
using UnityEngine.TestTools;

namespace UIFramework.Tests.PlayMode
{
    // An overlay that holds a shown screen collapses through its own presentation events: hiding the exited
    // screen raises Exited, and the controller pops the group, resets it for reuse and resumes the layer below.
    // Every collapse leaves the released screens hidden, detached and settled, and the layer below taking input
    // again.
    public sealed class GroupCollapseTests
    {
        private UguiScreenFixture _fixture;
        private UguiScreenA _a;
        private UguiScreenB _b;
        private UguiScreenC _c;
        private ScreenController _controller;
        private IScreenGroup _overlay;
        private int _exited;

        [SetUp]
        public void SetUp()
        {
            _fixture = new UguiScreenFixture();
            _a = _fixture.AddScreen<UguiScreenA>();
            _b = _fixture.AddScreen<UguiScreenB>();
            _c = _fixture.AddScreen<UguiScreenC>();
            _controller = _fixture.CreateController(_a, _b, _c);
            Show<UguiScreenA>();
            _overlay = _controller.PushGroup();
            Show<UguiScreenB>();
            _exited = 0;
            _overlay.Exited += () => _exited++;

            Assert.That(_b.Visibility, Is.EqualTo(WidgetVisibility.Visible), "arrange: the overlay shows its screen");
            Assert.That(_a.CanvasGroup.blocksRaycasts, Is.False, "arrange: the layer below is paused");
        }

        [UnityTearDown]
        public IEnumerator TearDown() => _fixture.Close();

        private void Show<TScreen>() where TScreen : class, IScreen
            => _fixture.Instant(_controller.CreateNavigateToRequest<TScreen>().Execute());

        private NavigateToResponse<IScreen> FadeTo<TScreen>(float length) where TScreen : class, IScreen
            => _controller.CreateNavigateToRequest<TScreen>()
                .WithTransition(Transition.Fade(length, EasingMode.Linear))
                .Execute();

        private static ExitRequest FadeOut(float length)
            => new() { Animation = WidgetAnimationRef.FromGeneric(GenericAnimation.Fade), Length = length };

        private NavigateToResponse<IScreen> ExitAnimated(float length) => _controller.Exit(FadeOut(length));

        private NavigateToResponse<IScreen> ExitInstantly() => _fixture.Instant(_controller.Exit(new ExitRequest()));

        private void AssertCollapsed(params RecordingScreen[] released)
        {
            Assert.That(_controller.Groups, Has.Count.EqualTo(1), "the overlay is popped");
            Assert.That(_controller.ActiveGroup.ActiveScreen, Is.SameAs(_a));
            foreach (RecordingScreen screen in released)
            {
                Assert.That(screen.Visibility, Is.EqualTo(WidgetVisibility.Hidden), screen.name);
                Assert.That(screen.gameObject.activeSelf, Is.False, screen.name);
                Assert.That(screen.IsAnimating, Is.False, screen.name);
                Assert.That(screen.Navigator, Is.Null, $"{screen.name} is detached for the next group");
                Assert.That(screen.IsInteractable.Value, Is.True, screen.name);
            }
            Assert.That(_a.Visibility, Is.EqualTo(WidgetVisibility.Visible), "the layer below stays shown");
            Assert.That(_a.CanvasGroup.blocksRaycasts, Is.True, "and takes input again");
        }

        [Test]
        public void ExitCollapsesAGroupHoldingAShownScreen()
        {
            NavigateToResponse<IScreen> response = ExitInstantly();

            Assert.That(response.Result.Success, Is.True);
            AssertCollapsed(_b);
            Assert.That(_exited, Is.EqualTo(1), "the collapse is driven by the overlay's Exited");
        }

        [Test]
        public void ReturnAtTheRootOfAShownOverlayCollapsesIt()
        {
            NavigateToResponse<IScreen> response = _fixture.Instant(_controller.Return());

            Assert.That(response.Result.Success, Is.True);
            AssertCollapsed(_b);
            Assert.That(_exited, Is.EqualTo(1));
        }

        [Test]
        public void ReturnWithHistoryStaysInTheOverlay()
        {
            Show<UguiScreenC>();

            _fixture.Instant(_controller.Return());

            Assert.That(_controller.Groups, Has.Count.EqualTo(2));
            Assert.That(_overlay.ActiveScreen, Is.SameAs(_b));
            Assert.That(_b.Visibility, Is.EqualTo(WidgetVisibility.Visible));
            Assert.That(_c.gameObject.activeSelf, Is.False);
            Assert.That(_exited, Is.Zero);
        }

        [Test]
        public void AScreenCanExitItsOwnGroup()
        {
            _fixture.Instant(_b.Navigator.Exit(new ExitRequest()));

            AssertCollapsed(_b);
            Assert.That(_exited, Is.EqualTo(1));
        }

        // One exit releases every screen the overlay ever showed and discards its history, so a reused group
        // starts with nothing to return to.
        [Test]
        public void ExitingAMultiStepOverlayReleasesEveryScreenItHeld()
        {
            Show<UguiScreenC>();
            Assert.That(_overlay.PreviousScreen, Is.SameAs(_b), "arrange: the overlay has history");

            _fixture.Instant(_c.Navigator.Exit(new ExitRequest()));

            AssertCollapsed(_b, _c);
            Assert.That(_exited, Is.EqualTo(1));
            IScreenGroup reused = _controller.PushGroup();
            Assert.That(reused, Is.SameAs(_overlay), "arrange: the pooled group is reused");
            Assert.That(reused.PreviousScreen, Is.Null);
        }

        [Test]
        public void ACollapsedOverlayCanBeNavigatedIntoAgain()
        {
            ExitInstantly();

            IScreenGroup reused = _controller.PushGroup();
            Show<UguiScreenB>();

            Assert.That(reused, Is.SameAs(_overlay), "the pooled group is reused");
            Assert.That(reused.ActiveScreen, Is.SameAs(_b));
            Assert.That(_b.Navigator, Is.SameAs(reused));
            Assert.That(_b.Visibility, Is.EqualTo(WidgetVisibility.Visible));
            Assert.That(_a.CanvasGroup.blocksRaycasts, Is.False, "and pauses the layer below again");
        }

        [UnityTest]
        public IEnumerator AnAnimatedExitKeepsTheOverlayUntilTheHideEnds()
        {
            _fixture.Watch(ExitAnimated(2f));
            yield return null;

            Assert.That(_controller.Groups, Has.Count.EqualTo(2));
            Assert.That(_b.IsAnimating, Is.True);
            Assert.That(_b.gameObject.activeSelf, Is.True, "the exiting screen is still on screen");
            Assert.That(_a.CanvasGroup.blocksRaycasts, Is.False, "the layer below stays paused");
            Assert.That(_exited, Is.Zero);
        }

        // The pop and the reset run inside the hide's completion, on the animation runner, while the exiting
        // screen is still finishing its own animation.
        [UnityTest]
        public IEnumerator AnAnimatedExitCollapsesTheOverlayWhenTheHideEnds()
        {
            AwaitableProbe exit = _fixture.Watch(ExitAnimated(0.1f));

            yield return exit.WaitForCompletion();

            Assert.That(exit.Exception, Is.Null);
            AssertCollapsed(_b);
            Assert.That(_b.CanvasGroup.alpha, Is.EqualTo(1f), "the resting opacity is restored");
            Assert.That(_exited, Is.EqualTo(1));
            Assert.That(_fixture.PooledHandles, Is.EqualTo(1), "the hide's handle is returned exactly once");
        }

        // An overlay already exiting has no active screen, so a second exit pops it at once. The release finishes
        // the hide still in flight without raising Exited for it.
        [UnityTest]
        public IEnumerator ExitingAnExitingOverlayPopsItWithoutExited()
        {
            AwaitableProbe first = _fixture.Watch(ExitAnimated(2f));
            yield return null;
            Assert.That(_b.IsAnimating, Is.True, "arrange: the first exit is still hiding");

            NavigateToResponse<IScreen> second = ExitInstantly();

            Assert.That(second.Result.Success, Is.True);
            AssertCollapsed(_b);
            Assert.That(_exited, Is.Zero);
            Assert.That(first.IsComplete, Is.True, "the first exit's hide is finished, not abandoned");
            Assert.That(first.Exception, Is.Null);
        }

        // Exiting while the overlay is part-way through an animated navigation still collapses it cleanly. Only
        // the end state is pinned; how the in-flight navigation is wound up is not.
        [UnityTest]
        public IEnumerator ExitingDuringAnAnimatedNavigationCollapsesCleanly()
        {
            AwaitableProbe navigation = _fixture.Watch(FadeTo<UguiScreenC>(2f));
            yield return null;
            Assert.That(_c.IsAnimating, Is.True, "arrange: the navigation is in flight");

            ExitInstantly();
            yield return null;

            Assert.That(navigation.IsComplete, Is.True);
            AssertCollapsed(_b, _c);
        }
    }
}
