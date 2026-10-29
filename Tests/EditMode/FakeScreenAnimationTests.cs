using System;
using System.Threading;

using NUnit.Framework;

using UIFramework.Core.Interfaces;
using UIFramework.TestUtils;

using UnityEngine;
using UnityEngine.Extension;

namespace UIFramework.Tests.EditMode
{
    // The edit-mode harness rests on two things: a cancelled frame wait resumes inline, with no frame passing,
    // and FakeScreen's opt-in animation behaves at its boundary the way the real widget's does, except where
    // FakeScreen.Animation.cs lists a difference; the tests of those differences pin the fake, not WidgetBase.
    public sealed class FakeScreenAnimationTests
    {
        private FakeScreenA _a;
        private AnimationPlayable _playable;

        [SetUp]
        public void SetUp()
        {
            _a = new FakeScreenA();
            _playable = new AnimationPlayable(new FakeAnimation(1f));
        }

        [Test]
        public void ACancelledFrameWaitResumesInline()
        {
            using CancellationTokenSource source = new();
            AwaitableProbe wait = AwaitableProbe.Watch(Awaitable.NextFrameAsync(source.Token));
            Assert.That(wait.IsComplete, Is.False, "arrange: no frame passes inside a test");

            source.Cancel();

            Assert.That(wait.IsComplete, Is.True);
            Assert.That(wait.Exception, Is.InstanceOf<OperationCanceledException>());
        }

        [Test]
        public void WithoutAnimationEnabledTheAnimationMembersThrowSynchronously()
        {
            Assert.Throws<NotSupportedException>(() => _a.AnimateVisibility(WidgetVisibility.Visible, _playable));
            Assert.Throws<NotSupportedException>(() => _a.SkipAnimation());
            Assert.Throws<NotSupportedException>(() => _a.RewindAnimation());
            Assert.That(_a.IsAnimating, Is.False);
        }

        [Test]
        public void ARunSetsVisibilityAtTheStartAndRaisesShownOnlyWhenCompleted()
        {
            _a.EnableAnimation();
            bool shown = false, animatingWhenShown = false;
            _a.Shown += _ =>
            {
                shown = true;
                animatingWhenShown = _a.IsAnimating;
            };

            AwaitableProbe animation = AwaitableProbe.Watch(_a.AnimateVisibility(WidgetVisibility.Visible, _playable));

            Assert.That(_a.Visibility, Is.EqualTo(WidgetVisibility.Visible));
            Assert.That(_a.IsAnimating, Is.True);
            Assert.That(shown, Is.False);
            Assert.That(animation.IsComplete, Is.False);

            _a.LastRun.Complete();

            Assert.That(shown, Is.True);
            Assert.That(animatingWhenShown, Is.True, "Shown fires before the run settles");
            Assert.That(animation.IsComplete, Is.True);
            Assert.That(animation.Exception, Is.Null);
            Assert.That(_a.IsAnimating, Is.False);
        }

        [Test]
        public void CancellingTheTokenCancelsTheRunInlineAndKeepsTheNewVisibility()
        {
            _a.EnableAnimation();
            using CancellationTokenSource source = new();
            AwaitableProbe animation = AwaitableProbe.Watch(
                _a.AnimateVisibility(WidgetVisibility.Visible, _playable, InterruptBehavior.Immediate, source.Token));

            source.Cancel();

            Assert.That(animation.Exception, Is.InstanceOf<OperationCanceledException>());
            Assert.That(_a.LastRun.State, Is.EqualTo(FakeAnimationRun.RunState.Cancelled));
            Assert.That(_a.IsAnimating, Is.False);
            Assert.That(_a.Visibility, Is.EqualTo(WidgetVisibility.Visible));
            Assert.Throws<InvalidOperationException>(() => _a.LastRun.Complete(), "an ended run cannot be completed");
        }

        [Test]
        public void SkipAnimationCompletesTheRunningRunInline()
        {
            _a.EnableAnimation();
            AwaitableProbe animation = AwaitableProbe.Watch(_a.AnimateVisibility(WidgetVisibility.Visible, _playable));

            AwaitableProbe skip = AwaitableProbe.Watch(_a.SkipAnimation());

            Assert.That(animation.IsComplete, Is.True);
            Assert.That(animation.Exception, Is.Null);
            Assert.That(skip.IsComplete, Is.True);
            Assert.That(skip.Exception, Is.Null);
            Assert.That(_a.LastRun.State, Is.EqualTo(FakeAnimationRun.RunState.Completed));
        }

        [Test]
        public void SkipAnimationDoesNothingUnlessAnimating()
        {
            _a.EnableAnimation();

            AwaitableProbe skip = AwaitableProbe.Watch(_a.SkipAnimation());

            Assert.That(skip.IsComplete, Is.True);
            Assert.That(skip.Exception, Is.Null);
            Assert.That(_a.Runs, Is.Empty);
        }

        [Test]
        public void RewindAnimationDoesNothingUnlessAnimating()
        {
            _a.EnableAnimation();
            using CancellationTokenSource source = new();

            AwaitableProbe rewind = AwaitableProbe.Watch(_a.RewindAnimation(source.Token));

            Assert.That(rewind.IsComplete, Is.True);
            Assert.That(rewind.Exception, Is.Null);
            Assert.That(_a.Runs, Is.Empty);
            Assert.That(_a.RewindTokens, Is.EqualTo(new[] { source.Token }), "the token is recorded all the same");
        }

        [Test]
        public void InterruptingARunningRunOtherThanImmediatelyIsNotModelled()
        {
            _a.EnableAnimation();
            AwaitableProbe.Watch(_a.AnimateVisibility(WidgetVisibility.Visible, _playable));

            AwaitableProbe interrupt = AwaitableProbe.Watch(
                _a.AnimateVisibility(WidgetVisibility.Hidden, _playable, InterruptBehavior.Ignore));

            Assert.That(interrupt.Exception, Is.InstanceOf<NotSupportedException>());
            Assert.Throws<NotSupportedException>(() => _a.RewindAnimation());
            Assert.That(_a.LastRun.State, Is.EqualTo(FakeAnimationRun.RunState.Running),
                "the running run is untouched");
        }

        [Test]
        public void SettingADifferentVisibilityCancelsTheRunningRun()
        {
            _a.EnableAnimation();
            AwaitableProbe animation = AwaitableProbe.Watch(_a.AnimateVisibility(WidgetVisibility.Visible, _playable));

            _a.SetVisibility(WidgetVisibility.Hidden);

            Assert.That(animation.Exception, Is.InstanceOf<OperationCanceledException>());
            Assert.That(_a.Visibility, Is.EqualTo(WidgetVisibility.Hidden));
            Assert.That(_a.IsAnimating, Is.False);
        }

        [Test]
        public void SettingTheSameVisibilitySkipsTheRunningRun()
        {
            _a.EnableAnimation();
            AwaitableProbe animation = AwaitableProbe.Watch(_a.AnimateVisibility(WidgetVisibility.Visible, _playable));

            _a.SetVisibility(WidgetVisibility.Visible);

            Assert.That(animation.IsComplete, Is.True);
            Assert.That(animation.Exception, Is.Null);
            Assert.That(_a.LastRun.State, Is.EqualTo(FakeAnimationRun.RunState.Completed));
        }

        [Test]
        public void TerminatingCancelsTheRunningRun()
        {
            _a.EnableAnimation();
            _a.Initialize();
            AwaitableProbe animation = AwaitableProbe.Watch(_a.AnimateVisibility(WidgetVisibility.Visible, _playable));

            _a.Terminate();

            Assert.That(animation.Exception, Is.InstanceOf<OperationCanceledException>());
            Assert.That(_a.LastRun.State, Is.EqualTo(FakeAnimationRun.RunState.Cancelled));
            Assert.That(_a.IsAnimating, Is.False);
        }

        [Test]
        public void ARunThatIgnoresCancellationKeepsRunningUntilCompleted()
        {
            _a.EnableAnimation();
            _a.IgnoreCancellation = true;
            using CancellationTokenSource source = new();
            AwaitableProbe animation = AwaitableProbe.Watch(
                _a.AnimateVisibility(WidgetVisibility.Visible, _playable, InterruptBehavior.Immediate, source.Token));

            source.Cancel();
            Assert.That(animation.IsComplete, Is.False);

            _a.LastRun.Complete();
            Assert.That(animation.IsComplete, Is.True);
            Assert.That(animation.Exception, Is.Null);
        }
    }
}
