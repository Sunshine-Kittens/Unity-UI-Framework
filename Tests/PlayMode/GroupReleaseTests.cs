using System.Collections;

using NUnit.Framework;

using UIFramework.Animation;
using UIFramework.Core.Interfaces;
using UIFramework.Groups;
using UIFramework.Navigation;
using UIFramework.TestUtils;
using UIFramework.Transitioning;

using UnityEngine.Extension;
using UnityEngine.TestTools;

namespace UIFramework.Tests.PlayMode
{
    // Resetting a group hands its screens back for another group to take, so none may stay on screen or keep
    // animating: a show in flight is cancelled and a hide in flight is finished.
    //
    // Reset is controller-facing and not on IScreenGroup, so the group is built directly here.
    public sealed class GroupReleaseTests
    {
        private UguiScreenFixture _fixture;
        private UguiScreenA _a;
        private UguiScreenB _b;
        private ScreenGroup _group;

        [SetUp]
        public void SetUp()
        {
            _fixture = new UguiScreenFixture();
            _a = _fixture.AddScreen<UguiScreenA>();
            _b = _fixture.AddScreen<UguiScreenB>();
            _group = _fixture.CreateGroup(_fixture.CreateRegistry(_a, _b));
            _fixture.Instant(_group.CreateNavigateToRequest(_a).Execute());
            Assert.That(_a.Visibility, Is.EqualTo(WidgetVisibility.Visible),
                "arrange: the group shows its first screen");
        }

        [UnityTearDown]
        public IEnumerator TearDown() => _fixture.Close();

        private NavigateToResponse<IScreen> FadeToB(float length)
            => _group.CreateNavigateToRequest(_b).WithTransition(Transition.Fade(length, EasingMode.Linear)).Execute();

        private NavigateToResponse<IScreen> AnimateBothToB(float length)
            => _group.CreateNavigateToRequest(_b)
                .WithAnimation(WidgetAnimationRef.FromGeneric(GenericAnimation.Fade))
                .WithLength(length)
                .Execute();

        private static void AssertReleased(RecordingScreen screen)
        {
            Assert.That(screen.Visibility, Is.EqualTo(WidgetVisibility.Hidden), screen.name);
            Assert.That(screen.gameObject.activeSelf, Is.False, screen.name);
            Assert.That(screen.IsAnimating, Is.False, screen.name);
            Assert.That(screen.CanvasGroup.alpha, Is.EqualTo(1f), $"{screen.name} is back at its resting opacity");
            Assert.That(screen.IsInteractable.Value, Is.True, screen.name);
            Assert.That(screen.Navigator, Is.Null, screen.name);
        }

        [UnityTest]
        public IEnumerator ResettingDuringAShowLeavesTheScreenHiddenAndSettled()
        {
            AwaitableProbe navigation = _fixture.Watch(FadeToB(2f));
            yield return null;
            Assert.That(_b.IsAnimating, Is.True, "arrange: the show is in flight");

            _group.Reset();

            AssertReleased(_a);
            AssertReleased(_b);
            Assert.That(_fixture.PooledHandles, Is.EqualTo(1), "the cancelled show's handle is returned exactly once");

            yield return navigation.WaitForCompletion(1f);
            Assert.That(navigation.IsComplete, Is.True, "the navigation is wound up with its show");
        }

        [UnityTest]
        public IEnumerator ResettingDuringAHideFinishesIt()
        {
            _fixture.Watch(AnimateBothToB(2f));
            yield return null;
            Assert.That(_a.IsAnimating, Is.True, "arrange: the hide is in flight");
            Assert.That(_b.IsAnimating, Is.True, "arrange: the show is in flight");

            _group.Reset();

            AssertReleased(_a);
            AssertReleased(_b);
            Assert.That(_fixture.PooledHandles, Is.EqualTo(2), "each screen's handle is returned exactly once");
        }
    }
}
