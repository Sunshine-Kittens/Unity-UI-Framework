using System.Collections;

using NUnit.Framework;

using UIFramework.Animation;
using UIFramework.Controllers;
using UIFramework.Core.Interfaces;
using UIFramework.TestUtils;
using UIFramework.Transitioning;

using UnityEngine.Extension;
using UnityEngine.TestTools;

namespace UIFramework.Tests.PlayMode
{
    // Animated navigation on real screens, driven by the player loop. An animation starts inside the call that
    // requests it, so its opening state is checked synchronously; later states are checked after it ends, or
    // part-way through a run long enough that the frames waited cannot finish it.
    public sealed class ScreenNavigationAnimationTests
    {
        private UguiScreenFixture _fixture;
        private UguiScreenA _a;
        private UguiScreenB _b;
        private ScreenController _controller;

        [SetUp]
        public void SetUp()
        {
            _fixture = new UguiScreenFixture();
            _a = _fixture.AddScreen<UguiScreenA>();
            _b = _fixture.AddScreen<UguiScreenB>();
            _controller = _fixture.CreateController(_a, _b);
        }

        [UnityTearDown]
        public IEnumerator TearDown() => _fixture.Close();

        private void ShowA() => _fixture.Instant(_controller.CreateNavigateToRequest<UguiScreenA>().Execute());

        private AwaitableProbe FadeTo<TScreen>(float length) where TScreen : class, IScreen
            => _fixture.Watch(_controller.CreateNavigateToRequest<TScreen>()
                .WithTransition(Transition.Fade(length, EasingMode.Linear))
                .Execute());

        private AwaitableProbe ExpandTo<TScreen>(float length) where TScreen : class, IScreen
            => _fixture.Watch(_controller.CreateNavigateToRequest<TScreen>()
                .WithAnimation(WidgetAnimationRef.FromGeneric(GenericAnimation.Expand))
                .WithLength(length)
                .Execute());

        private AwaitableProbe Return() => _fixture.Watch(_controller.Return());

        private static void AssertShownAndSettled(RecordingScreen screen)
        {
            Assert.That(screen.Visibility, Is.EqualTo(WidgetVisibility.Visible), screen.name);
            Assert.That(screen.gameObject.activeSelf, Is.True, screen.name);
            Assert.That(screen.IsAnimating, Is.False, screen.name);
            Assert.That(screen.CanvasGroup.alpha, Is.EqualTo(1f), screen.name);
            Assert.That(screen.CanvasGroup.blocksRaycasts, Is.True, $"{screen.name} takes input once settled");
        }

        private static void AssertHidden(RecordingScreen screen)
        {
            Assert.That(screen.Visibility, Is.EqualTo(WidgetVisibility.Hidden), screen.name);
            Assert.That(screen.gameObject.activeSelf, Is.False, screen.name);
            Assert.That(screen.IsAnimating, Is.False, screen.name);
        }

        [UnityTest]
        public IEnumerator AnAnimatedNavigationEndsWithOnlyTheTargetShown()
        {
            ShowA();
            AwaitableProbe navigation = FadeTo<UguiScreenB>(0.1f);

            yield return navigation.WaitForCompletion();

            Assert.That(navigation.Exception, Is.Null);
            AssertShownAndSettled(_b);
            AssertHidden(_a);
            Assert.That(_controller.ActiveGroup.ActiveScreen, Is.SameAs(_b));
            Assert.That(_controller.ActiveGroup.PreviousScreen, Is.SameAs(_a));
            Assert.That(_fixture.PooledHandles, Is.EqualTo(1), "the show's handle is returned exactly once");
        }

        // A target-only fade brings the target in over the source, which is hidden only once the fade ends.
        [UnityTest]
        public IEnumerator TheTargetFadesInOverTheStillShownSource()
        {
            ShowA();
            FadeTo<UguiScreenB>(2f);

            Assert.That(_b.IsAnimating, Is.True);
            Assert.That(_b.gameObject.activeSelf, Is.True);
            Assert.That(_b.CanvasGroup.blocksRaycasts, Is.False, "an animating screen takes no input");
            Assert.That(_a.Visibility, Is.EqualTo(WidgetVisibility.Visible));
            Assert.That(_a.gameObject.activeSelf, Is.True);

            yield return UguiScreenFixture.WaitUntil(() => _b.CanvasGroup.alpha > 0.05f, "the target fades in");

            Assert.That(_b.IsAnimating, Is.True, "the fade is part-way through");
            Assert.That(_b.CanvasGroup.alpha, Is.LessThan(1f));
            Assert.That(_a.gameObject.activeSelf, Is.True, "the source is still shown under it");
        }

        // The named animation shows the target; the source hides at the same time with its own default animation.
        [UnityTest]
        public IEnumerator ANamedAnimationShowsTheTargetWhileTheSourceHides()
        {
            ShowA();
            ExpandTo<UguiScreenB>(2f);

            Assert.That(_a.CanvasGroup.alpha, Is.EqualTo(1f).Within(0.01f), "the source's fade out starts opaque");
            Assert.That(_a.IsAnimating, Is.True, "the source animates out");
            Assert.That(_b.IsAnimating, Is.True, "the target animates in");
            Assert.That(_a.Visibility, Is.EqualTo(WidgetVisibility.Hidden));
            Assert.That(_a.gameObject.activeSelf, Is.True, "a hiding screen stays on screen until its hide ends");

            yield return UguiScreenFixture.WaitUntil(() => _a.CanvasGroup.alpha < 0.95f, "the source fades out");

            Assert.That(_a.IsAnimating, Is.True, "the hide is part-way through");
            Assert.That(_b.RectTransform.localScale.x, Is.LessThan(1f), "the target plays the named animation");
        }

        // The recorded transition is replayed inverted: the screen returned to is shown at once and the one left
        // fades out over it, starting opaque.
        [UnityTest]
        public IEnumerator AnAnimatedReturnShowsThePreviousScreenAndFadesTheLeavingOneOut()
        {
            ShowA();
            AwaitableProbe forward = FadeTo<UguiScreenB>(0.1f);
            yield return forward.WaitForCompletion();
            Assert.That(forward.Exception, Is.Null, "arrange: the forward navigation completed");

            AwaitableProbe back = Return();

            Assert.That(_a.Visibility, Is.EqualTo(WidgetVisibility.Visible));
            Assert.That(_a.gameObject.activeSelf, Is.True);
            Assert.That(_a.IsAnimating, Is.False, "the screen returned to is shown at once");
            Assert.That(_b.IsAnimating, Is.True, "the screen left animates out");
            Assert.That(_b.Visibility, Is.EqualTo(WidgetVisibility.Hidden));
            Assert.That(_b.CanvasGroup.alpha, Is.EqualTo(1f).Within(0.01f), "a fade out starts opaque");

            yield return back.WaitForCompletion();

            Assert.That(back.Exception, Is.Null);
            AssertShownAndSettled(_a);
            AssertHidden(_b);
            Assert.That(_controller.ActiveGroup.ActiveScreen, Is.SameAs(_a));
            Assert.That(_controller.ActiveGroup.PreviousScreen, Is.Null);
        }

        // With nothing shown yet there is no source to transition from, so the target's show animates on its own.
        [UnityTest]
        public IEnumerator AnAnimatedFirstNavigationShowsTheScreen()
        {
            AwaitableProbe navigation = FadeTo<UguiScreenA>(0.1f);
            int entered = 0;
            _controller.ActiveGroup.Entered += () => entered++;

            Assert.That(_a.IsAnimating, Is.True);

            yield return navigation.WaitForCompletion();

            Assert.That(navigation.Exception, Is.Null);
            AssertShownAndSettled(_a);
            Assert.That(entered, Is.EqualTo(1), "the group enters once its first screen is shown");
        }
    }
}
