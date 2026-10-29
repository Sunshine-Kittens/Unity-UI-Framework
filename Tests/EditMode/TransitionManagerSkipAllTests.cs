using System;
using System.Threading;

using NUnit.Framework;

using UIFramework.Core.Interfaces;
using UIFramework.TestUtils;
using UIFramework.Transitioning;

using static UIFramework.TestUtils.TransitionHarness;

namespace UIFramework.Tests.EditMode
{
    // While SkipAll runs, the manager is latched: every transition started before SkipAll returns runs instant.
    // The latch must clear on every exit, including a fault or a cancellation part-way through; otherwise every
    // later transition runs instant for good.
    //
    // Unless a test enables it, these FakeScreens do not animate, so their animation members throw: a later
    // transition that faults with NotSupportedException and leaves its target hidden took the animated path,
    // and one that completes with its target visible ran instant.
    public sealed class TransitionManagerSkipAllTests
    {
        private TransitionHarness _harness;
        private FakeScreenA _a;
        private FakeScreenB _b;
        private FakeScreenC _c;
        private FakeScreenD _d;
        private FakeScreenE _e;
        private FakeAnimation _entry;

        [SetUp]
        public void SetUp()
        {
            _harness = new TransitionHarness();
            _a = new FakeScreenA();
            _b = new FakeScreenB();
            _c = new FakeScreenC();
            _d = new FakeScreenD();
            _e = new FakeScreenE();
            _entry = new FakeAnimation(1f);
        }

        [TearDown]
        public void TearDown()
            => Assert.That(_harness.Close(), Is.Zero, "every awaitable the test started has settled");

        // Leaves a transition from B to C active and waiting for a frame, cancellable through the returned source.
        private CancellationTokenSource ActivateWaitingTransition(VisibilityTransitionParams transition)
        {
            CancellationTokenSource source = _harness.CreateSource();
            _harness.RunInstantThenQueue(_a, _b, () => _harness.Run(transition, _b, _c, source.Token));
            Assert.That(_harness.Manager.ActiveTarget, Is.SameAs(_c), "arrange: the queued transition is active");
            return source;
        }

        private void AssertLaterTransitionAnimates()
        {
            AwaitableProbe later = _harness.Run(EntryOnly(_entry), _d, _e);

            Assert.That(later.Exception, Is.InstanceOf<NotSupportedException>(),
                "the later transition animates rather than running instant");
            Assert.That(_e.Visibility, Is.EqualTo(WidgetVisibility.Hidden));
        }

        [Test]
        public void SkipAllThatFaultsWhileSkippingLeavesLaterTransitionsAnimated()
        {
            CancellationTokenSource active = ActivateWaitingTransition(EntryOnly(_entry));

            // Skipping a FakeScreen without animation enabled throws, so SkipAll faults.
            AwaitableProbe skipAll = _harness.Watch(_harness.Manager.SkipAll(_harness.CreateSource().Token));
            Assert.That(skipAll.Exception, Is.InstanceOf<NotSupportedException>(),
                "arrange: skipping the active target faults SkipAll");
            active.Cancel();
            Assert.That(_harness.Manager.IsTransitionActive, Is.False, "arrange: the manager is idle");

            AssertLaterTransitionAnimates();
        }

        [Test]
        public void SkipAllCancelledWhileWaitingLeavesLaterTransitionsAnimated()
        {
            CancellationTokenSource active = ActivateWaitingTransition(Instant);
            CancellationTokenSource skip = _harness.CreateSource();

            AwaitableProbe skipAll = _harness.Watch(_harness.Manager.SkipAll(skip.Token));
            Assert.That(skipAll.IsComplete, Is.False, "arrange: SkipAll waits for the active transition");
            skip.Cancel();
            Assert.That(skipAll.Exception, Is.InstanceOf<OperationCanceledException>(),
                "arrange: SkipAll ends cancelled");
            active.Cancel();
            Assert.That(_harness.Manager.IsTransitionActive, Is.False, "arrange: the manager is idle");

            AssertLaterTransitionAnimates();
        }

        [Test]
        public void SkipAllWithAnAlreadyCancelledTokenLeavesLaterTransitionsAnimated()
        {
            CancellationTokenSource active = ActivateWaitingTransition(Instant);

            AwaitableProbe skipAll = _harness.Watch(_harness.Manager.SkipAll(_harness.CancelledToken()));
            Assert.That(skipAll.Exception, Is.InstanceOf<OperationCanceledException>(),
                "arrange: SkipAll ends cancelled");
            active.Cancel();
            Assert.That(_harness.Manager.IsTransitionActive, Is.False, "arrange: the manager is idle");

            AssertLaterTransitionAnimates();
        }

        [Test]
        public void ATransitionStartedWhileSkipAllWaitsRunsInstant()
        {
            CancellationTokenSource active = ActivateWaitingTransition(Instant);
            AwaitableProbe skipAll = _harness.Watch(_harness.Manager.SkipAll(_harness.CreateSource().Token));
            active.Cancel();
            // SkipAll checks for an idle manager only once a frame passes, so it is still waiting here.
            Assert.That(_harness.Manager.IsTransitionActive, Is.False, "arrange: the manager is idle");
            Assert.That(skipAll.IsComplete, Is.False, "arrange: SkipAll has not returned");

            AwaitableProbe during = _harness.Run(EntryOnly(_entry), _d, _e);

            Assert.That(during.IsComplete, Is.True);
            Assert.That(during.Exception, Is.Null, "the target is never animated");
            Assert.That(_e.Visibility, Is.EqualTo(WidgetVisibility.Visible));
        }

        [Test]
        public void ResetClearsTheSkipAllLatch()
        {
            ActivateWaitingTransition(Instant);
            AwaitableProbe skipAll = _harness.Watch(_harness.Manager.SkipAll(_harness.CreateSource().Token));
            Assert.That(skipAll.IsComplete, Is.False, "arrange: SkipAll waits for the active transition");

            _harness.Manager.Reset();
            Assert.That(_harness.Manager.IsTransitionActive, Is.False, "arrange: the manager is idle");

            AssertLaterTransitionAnimates();
        }

        // The skips end the animations but never settle. The transition finishes regardless, so SkipAll has
        // nothing left to wait for.
        [TestCase(TransitionTarget.Target)]
        [TestCase(TransitionTarget.Source)]
        [TestCase(TransitionTarget.Both)]
        public void SkipAllReturnsWhenAWidgetsSkipNeverSettles(TransitionTarget animated)
        {
            FakeAnimation exit = new(1f);
            VisibilityTransitionParams transition = animated switch
            {
                TransitionTarget.Target => EntryOnly(_entry),
                TransitionTarget.Source => ExitOnly(exit),
                _ => TwoSided(exit, _entry)
            };
            _a.SetVisibility(WidgetVisibility.Visible);
            foreach (FakeScreen screen in new FakeScreen[] { _a, _b })
            {
                screen.EnableAnimation();
                screen.SkipNeverSettles = true;
            }
            AwaitableProbe running = _harness.Run(transition, _a, _b);
            Assert.That(running.IsComplete, Is.False, "arrange: the transition is animating");

            AwaitableProbe skipAll = _harness.Watch(_harness.Manager.SkipAll(_harness.CreateSource().Token));
            Assert.That(running.IsComplete, Is.True, "arrange: skipping finished the transition");
            Assert.That(running.Exception, Is.Null, "arrange: the transition succeeded");
            Assert.That(_harness.Manager.IsTransitionActive, Is.False, "arrange: the manager is idle");

            Assert.That(skipAll.IsComplete, Is.True, "SkipAll returns once the queue has drained");
            Assert.That(skipAll.Exception, Is.Null);
            AssertLaterTransitionAnimates();
        }

        [Test]
        public void SkipAllWhileIdleReturnsAtOnceAndLeavesLaterTransitionsAnimated()
        {
            AwaitableProbe skipAll = _harness.Watch(_harness.Manager.SkipAll(_harness.CreateSource().Token));

            Assert.That(skipAll.IsComplete, Is.True);
            Assert.That(skipAll.Exception, Is.Null);
            AssertLaterTransitionAnimates();
        }

        [Test]
        public void TerminateWhileIdleLeavesLaterTransitionsAnimated()
        {
            _harness.Manager.Terminate();

            AssertLaterTransitionAnimates();
        }
    }
}
