using System.Collections;
using System.Threading;

using NUnit.Framework;

using UIFramework.Core.Interfaces;
using UIFramework.TestUtils;

using UnityEngine.Extension;
using UnityEngine.TestTools;

namespace UIFramework.Tests.PlayMode
{
    // Covers AnimationPlayer, which lives in the unity-extensions package. These belong with that package if
    // it ever gains its own test assembly; they are here because the playground is where both are integrated
    // and because a regression here stops every animation in this framework.
    //
    // The player resolves the next event index once per play/rewind and walks it as time passes. Getting the
    // "no events remain" sentinel wrong made the forward loop index -1, which threw every frame — and since
    // every animation in this framework reports zero events, that was every animation.
    public sealed class AnimationPlayerEventTests
    {
        private UguiWidgetFixture _fixture;

        [SetUp]
        public void SetUp() => _fixture = new UguiWidgetFixture();

        [TearDown]
        public void TearDown() => _fixture?.Dispose();

        private AwaitableProbe Play(FakeAnimation animation, float length, PlaybackMode playbackMode, float startTime)
            => AwaitableProbe.Watch(_fixture.Widget.AnimateVisibility(WidgetVisibility.Visible)
                .WithAnimation(animation)
                .WithLength(length)
                .WithPlaybackMode(playbackMode)
                .WithStartTime(startTime)
                .WithTimeMode(TimeMode.Unscaled)
                .WithCancellation(CancellationToken.None)
                .Animate());

        [UnityTest]
        public IEnumerator AnimationWithNoEventsCompletesWithoutThrowing()
        {
            FakeAnimation animation = new(0.1f);

            AwaitableProbe probe = Play(animation, 0.1f, PlaybackMode.Forward, 0f);
            yield return probe.WaitForCompletion();

            Assert.That(probe.Exception, Is.Null, "the empty-event case is the common case");
            Assert.That(animation.Evaluations, Is.Not.Empty, "the animation was actually driven");
        }

        [UnityTest]
        public IEnumerator ForwardPlaybackFiresEventsInOrder()
        {
            System.Collections.Generic.List<string> fired = new();
            FakeAnimation animation = new(0.2f,
                new AnimationEventVoid(0.05f, () => fired.Add("early")),
                new AnimationEventVoid(0.15f, () => fired.Add("late")));

            AwaitableProbe probe = Play(animation, 0.2f, PlaybackMode.Forward, 0f);
            yield return probe.WaitForCompletion();

            Assert.That(probe.Exception, Is.Null);
            Assert.That(fired, Is.EqualTo(new[] { "early", "late" }));
        }

        [UnityTest]
        public IEnumerator ReversePlaybackFiresEventsInReverseOrder()
        {
            System.Collections.Generic.List<string> fired = new();
            FakeAnimation animation = new(0.2f,
                new AnimationEventVoid(0.05f, () => fired.Add("early")),
                new AnimationEventVoid(0.15f, () => fired.Add("late")));

            AwaitableProbe probe = Play(animation, 0.2f, PlaybackMode.Reverse, 0.2f);
            yield return probe.WaitForCompletion();

            Assert.That(probe.Exception, Is.Null);
            Assert.That(fired, Is.EqualTo(new[] { "late", "early" }),
                "reverse walks the elapsed window backwards");
        }
    }
}
