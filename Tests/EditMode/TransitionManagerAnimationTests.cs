using System;
using System.Threading;

using NUnit.Framework;

using UIFramework.Animation;
using UIFramework.Core.Interfaces;
using UIFramework.TestUtils;
using UIFramework.Transitioning;

using UnityEngine.Extension;

using static UIFramework.TestUtils.TransitionHarness;

namespace UIFramework.Tests.EditMode
{
    // Animated transitions over animation-enabled FakeScreens, whose animations run until the test completes
    // them. Covers what the manager hands each widget, when a transition counts as finished, how skipping and
    // rewinding pick their widgets, and the disposal of each transition's token.
    public sealed class TransitionManagerAnimationTests
    {
        private TransitionHarness _harness;
        private FakeScreenA _a;
        private FakeScreenB _b;
        private FakeScreenC _c;
        private FakeAnimation _exit;
        private FakeAnimation _entry;

        [SetUp]
        public void SetUp()
        {
            _harness = new TransitionHarness(TimeMode.Unscaled);
            _a = new FakeScreenA();
            _b = new FakeScreenB();
            _c = new FakeScreenC();
            _a.EnableAnimation();
            _b.EnableAnimation();
            _c.EnableAnimation();
            _exit = new FakeAnimation(1f);
            _entry = new FakeAnimation(1f);
            _a.SetVisibility(WidgetVisibility.Visible);
        }

        [TearDown]
        public void TearDown()
            => Assert.That(_harness.Close(), Is.Zero, "every awaitable the test started has settled");

        private static void AssertSucceeded(AwaitableProbe probe)
        {
            Assert.That(probe.IsComplete, Is.True);
            Assert.That(probe.Exception, Is.Null);
        }

        // A disposed token source refuses to create a wait handle.
        private static void AssertDisposed(CancellationToken token)
            => Assert.Throws<ObjectDisposedException>(() => _ = token.WaitHandle);

        // Leaves a transition from B to C active and waiting for a frame, so its widgets have not started
        // animating.
        private void ActivateWaitingTransition(VisibilityTransitionParams transition)
        {
            _harness.RunInstantThenQueue(_a, _b, () => _harness.Run(transition, _b, _c));
            Assert.That(_harness.Manager.ActiveTarget, Is.SameAs(_c), "arrange: the queued transition is active");
        }

        [Test]
        public void AnEntryOnlyTransitionLastsUntilTheTargetFinishesAnimatingIn()
        {
            AwaitableProbe transition = _harness.Run(EntryOnly(_entry), _a, _b);
            FakeAnimationRun run = _b.LastRun;

            Assert.That(run?.Visibility, Is.EqualTo(WidgetVisibility.Visible));
            Assert.That(run.InterruptBehavior, Is.EqualTo(InterruptBehavior.Immediate));
            Assert.That(transition.IsComplete, Is.False);
            Assert.That(_harness.Manager.ActiveTarget, Is.SameAs(_b));

            run.Complete();

            AssertSucceeded(transition);
            Assert.That(_a.Visibility, Is.EqualTo(WidgetVisibility.Hidden));
            Assert.That(_b.Visibility, Is.EqualTo(WidgetVisibility.Visible));
            Assert.That(_harness.Manager.IsTransitionActive, Is.False);
            Assert.That(_harness.PooledEntries, Is.EqualTo(1));
        }

        [Test]
        public void ATwoSidedTransitionLastsUntilBothWidgetsFinishAnimating()
        {
            AwaitableProbe transition = _harness.Run(TwoSided(_exit, _entry), _a, _b);
            Assert.That(_a.LastRun?.Visibility, Is.EqualTo(WidgetVisibility.Hidden),
                "arrange: the source animates out");
            Assert.That(_b.LastRun?.Visibility, Is.EqualTo(WidgetVisibility.Visible),
                "arrange: the target animates in");

            _a.LastRun.Complete();
            Assert.That(transition.IsComplete, Is.False, "the target is still animating");

            _b.LastRun.Complete();
            AssertSucceeded(transition);
            Assert.That(_harness.Manager.IsTransitionActive, Is.False);
        }

        [Test]
        public void ThePlayableCarriesTheTransitionsAnimationEasingAndLengthAndTheManagersTimeMode()
        {
            FakeAnimation entry = new(2f);

            _harness.Run(EntryOnly(entry, length: 0.5f, easingMode: EasingMode.EaseInOut), _a, _b);
            AnimationPlayable playable = _b.LastRun.Playable;

            Assert.That(playable.Animation, Is.SameAs(entry));
            Assert.That(playable.EasingMode, Is.EqualTo(EasingMode.EaseInOut));
            Assert.That(playable.PlaybackSpeed, Is.EqualTo(4f), "the animation's length over the transition's");
            Assert.That(playable.TimeMode, Is.EqualTo(TimeMode.Unscaled));
        }

        [Test]
        public void GenericAnimationsResolveAgainstTheWidgetEachSideAnimates()
        {
            FakeAnimation sourceOut = new(), sourceIn = new(), targetOut = new(), targetIn = new();
            _a.SetGenericAnimation(GenericAnimation.Fade, WidgetVisibility.Hidden, sourceOut);
            _a.SetGenericAnimation(GenericAnimation.Fade, WidgetVisibility.Visible, sourceIn);
            _b.SetGenericAnimation(GenericAnimation.Fade, WidgetVisibility.Hidden, targetOut);
            _b.SetGenericAnimation(GenericAnimation.Fade, WidgetVisibility.Visible, targetIn);
            WidgetAnimationRef fade = WidgetAnimationRef.FromGeneric(GenericAnimation.Fade);

            _harness.Run(Refs(fade, fade), _a, _b);

            Assert.That(_a.LastRun?.Playable.Animation, Is.SameAs(sourceOut));
            Assert.That(_b.LastRun?.Playable.Animation, Is.SameAs(targetIn));
        }

        [Test]
        public void CancellingAnAnimatingTransitionCancelsItsAnimation()
        {
            CancellationTokenSource source = _harness.CreateSource();
            AwaitableProbe transition = _harness.Run(EntryOnly(_entry), _a, _b, source.Token);
            FakeAnimationRun run = _b.LastRun;

            source.Cancel();

            Assert.That(run.State, Is.EqualTo(FakeAnimationRun.RunState.Cancelled));
            Assert.That(transition.Exception, Is.InstanceOf<OperationCanceledException>());
            Assert.That(_harness.Manager.IsTransitionActive, Is.False);
            Assert.That(_harness.PooledEntries, Is.EqualTo(1));
        }

        [Test]
        public void CancellingAnExitOnlyTransitionCancelsItsAnimation()
        {
            CancellationTokenSource source = _harness.CreateSource();
            AwaitableProbe transition = _harness.Run(ExitOnly(_exit), _a, _b, source.Token);
            FakeAnimationRun run = _a.LastRun;

            source.Cancel();

            Assert.That(run.State, Is.EqualTo(FakeAnimationRun.RunState.Cancelled));
            Assert.That(transition.Exception, Is.InstanceOf<OperationCanceledException>());
            Assert.That(_harness.Manager.IsTransitionActive, Is.False);
        }

        [Test]
        public void CancellingATwoSidedTransitionCancelsBothAnimations()
        {
            CancellationTokenSource source = _harness.CreateSource();
            AwaitableProbe transition = _harness.Run(TwoSided(_exit, _entry), _a, _b, source.Token);
            FakeAnimationRun sourceRun = _a.LastRun, targetRun = _b.LastRun;

            source.Cancel();

            Assert.That(sourceRun?.State, Is.EqualTo(FakeAnimationRun.RunState.Cancelled));
            Assert.That(targetRun?.State, Is.EqualTo(FakeAnimationRun.RunState.Cancelled));
            Assert.That(transition.Exception, Is.InstanceOf<OperationCanceledException>());
            Assert.That(_harness.Manager.IsTransitionActive, Is.False);
        }

        [Test]
        public void ATransitionDisposesTheTokenItHandedTheWidgetOnceItFinishes()
        {
            AwaitableProbe transition = _harness.Run(EntryOnly(_entry), _a, _b);
            CancellationToken token = _b.LastRun.Token;

            _b.LastRun.Complete();
            Assert.That(transition.IsComplete, Is.True, "arrange: the transition has finished");

            AssertDisposed(token);
        }

        [Test]
        public void SkipActiveFinishesTheTargetAnimationOfAnEntryOnlyTransition()
        {
            AwaitableProbe transition = _harness.Run(EntryOnly(_entry), _a, _b);

            AwaitableProbe skip = _harness.Watch(_harness.Manager.SkipActive());

            AssertSucceeded(skip);
            AssertSucceeded(transition);
            Assert.That(_b.LastRun.State, Is.EqualTo(FakeAnimationRun.RunState.Completed));
            Assert.That(_a.Calls, Does.Not.Contain(nameof(FakeScreen.SkipAnimation)));
        }

        [Test]
        public void SkipActiveFinishesTheSourceAnimationOfAnExitOnlyTransition()
        {
            AwaitableProbe transition = _harness.Run(ExitOnly(_exit), _a, _b);

            AwaitableProbe skip = _harness.Watch(_harness.Manager.SkipActive());

            AssertSucceeded(skip);
            AssertSucceeded(transition);
            Assert.That(_a.LastRun.State, Is.EqualTo(FakeAnimationRun.RunState.Completed));
            Assert.That(_b.Calls, Does.Not.Contain(nameof(FakeScreen.SkipAnimation)));
        }

        [Test]
        public void SkipActiveFinishesBothAnimationsOfATwoSidedTransition()
        {
            AwaitableProbe transition = _harness.Run(TwoSided(_exit, _entry), _a, _b);

            AwaitableProbe skip = _harness.Watch(_harness.Manager.SkipActive());

            AssertSucceeded(skip);
            AssertSucceeded(transition);
            Assert.That(_a.LastRun.State, Is.EqualTo(FakeAnimationRun.RunState.Completed));
            Assert.That(_b.LastRun.State, Is.EqualTo(FakeAnimationRun.RunState.Completed));
        }

        [Test]
        public void SkipAllFinishesTheActiveTransitionAndLaterTransitionsStillAnimate()
        {
            AwaitableProbe transition = _harness.Run(EntryOnly(_entry), _a, _b);

            AwaitableProbe skipAll = _harness.Watch(_harness.Manager.SkipAll(_harness.CreateSource().Token));

            AssertSucceeded(skipAll);
            AssertSucceeded(transition);
            Assert.That(_harness.Manager.IsTransitionActive, Is.False);

            _harness.Run(EntryOnly(_entry), _b, _c);
            Assert.That(_c.LastRun?.State, Is.EqualTo(FakeAnimationRun.RunState.Running),
                "a later transition animates");
        }

        [Test]
        public void SkipAllFinishesTheSourceAnimationOfAnExitOnlyTransition()
        {
            AwaitableProbe transition = _harness.Run(ExitOnly(_exit), _a, _b);

            AwaitableProbe skipAll = _harness.Watch(_harness.Manager.SkipAll(_harness.CreateSource().Token));

            AssertSucceeded(skipAll);
            AssertSucceeded(transition);
            Assert.That(_a.LastRun.State, Is.EqualTo(FakeAnimationRun.RunState.Completed));
            Assert.That(_b.Calls, Does.Not.Contain(nameof(FakeScreen.SkipAnimation)));
        }

        [Test]
        public void SkipAllFinishesBothAnimationsOfATwoSidedTransition()
        {
            AwaitableProbe transition = _harness.Run(TwoSided(_exit, _entry), _a, _b);

            AwaitableProbe skipAll = _harness.Watch(_harness.Manager.SkipAll(_harness.CreateSource().Token));

            AssertSucceeded(skipAll);
            AssertSucceeded(transition);
            Assert.That(_a.LastRun.State, Is.EqualTo(FakeAnimationRun.RunState.Completed));
            Assert.That(_b.LastRun.State, Is.EqualTo(FakeAnimationRun.RunState.Completed));
        }

        [Test]
        public void RewindActiveRewindsTheTargetOfAnEntryOnlyTransition()
        {
            ActivateWaitingTransition(EntryOnly(_entry));

            AssertSucceeded(_harness.Watch(_harness.Manager.RewindActive()));

            Assert.That(_c.Calls, Does.Contain(nameof(FakeScreen.RewindAnimation)));
            Assert.That(_b.Calls, Does.Not.Contain(nameof(FakeScreen.RewindAnimation)));
        }

        [Test]
        public void RewindActiveRewindsTheSourceOfAnExitOnlyTransition()
        {
            ActivateWaitingTransition(ExitOnly(_exit));

            AssertSucceeded(_harness.Watch(_harness.Manager.RewindActive()));

            Assert.That(_b.Calls, Does.Contain(nameof(FakeScreen.RewindAnimation)));
            Assert.That(_c.Calls, Does.Not.Contain(nameof(FakeScreen.RewindAnimation)));
        }

        [Test]
        public void RewindActiveRewindsBothWidgetsOfATwoSidedTransition()
        {
            ActivateWaitingTransition(TwoSided(_exit, _entry));

            AssertSucceeded(_harness.Watch(_harness.Manager.RewindActive()));

            Assert.That(_b.Calls, Does.Contain(nameof(FakeScreen.RewindAnimation)));
            Assert.That(_c.Calls, Does.Contain(nameof(FakeScreen.RewindAnimation)));
        }

        [Test]
        public void RewindActiveDisposesItsTokenReleasesItsEntryAndLeavesTheManagerIdle()
        {
            ActivateWaitingTransition(EntryOnly(_entry));

            AwaitableProbe rewind = _harness.Watch(_harness.Manager.RewindActive());

            AssertSucceeded(rewind);
            Assert.That(_harness.Manager.IsTransitionActive, Is.False);
            Assert.That(_c.RewindTokens, Has.Count.EqualTo(1), "arrange: the target was rewound once");
            AssertDisposed(_c.RewindTokens[0]);
            Assert.That(_harness.PooledEntries, Is.EqualTo(2), "the rewound transition's entry and the rewind's own");
        }
    }
}
