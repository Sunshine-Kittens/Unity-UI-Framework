using System;

using NUnit.Framework;

using UIFramework.Core.Interfaces;
using UIFramework.TestUtils;

using static UIFramework.TestUtils.TransitionHarness;

namespace UIFramework.Tests.EditMode
{
    // Reset returns the manager to a clean state for reuse while transitions may still be in flight. It
    // cancels them rather than releasing their entries itself: each cancelled transition releases its own
    // entry as it unwinds, so every entry goes back to the pool exactly once.
    public sealed class TransitionManagerResetTests
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

        [Test]
        public void ResetCancelsTheActiveAndQueuedTransitionsAndEachReleasesItsOwnEntry()
        {
            AwaitableProbe second = null, third = null, fourth = null;
            AwaitableProbe first = _harness.RunInstantThenQueue(_a, _b, () =>
            {
                second = _harness.Run(Instant, _b, _c);
                third = _harness.Run(Instant, _c, _d);
                fourth = _harness.Run(Instant, _d, _e);
            });
            Assert.That(first.IsComplete, Is.True, "arrange: the first transition has finished");
            Assert.That(_harness.PooledEntries, Is.EqualTo(1),
                "arrange: only the finished transition has released its entry");

            _harness.Manager.Reset();

            Assert.That(second.IsComplete, Is.True, "the active transition ends");
            Assert.That(second.Exception, Is.Not.Null, "without succeeding");
            Assert.That(third.Exception, Is.InstanceOf<OperationCanceledException>());
            Assert.That(fourth.Exception, Is.InstanceOf<OperationCanceledException>());
            Assert.That(_harness.Manager.IsTransitionActive, Is.False);
            Assert.That(_harness.PooledEntries, Is.EqualTo(4),
                "each entry is released exactly once, by its own transition");
        }

        [Test]
        public void ResetCancelsAnActiveTransitionWithNothingQueued()
        {
            AwaitableProbe second = null;
            _harness.RunInstantThenQueue(_a, _b, () => second = _harness.Run(Instant, _b, _c));
            Assert.That(_harness.Manager.ActiveTarget, Is.SameAs(_c), "arrange: the queued transition is active");

            _harness.Manager.Reset();

            Assert.That(second.Exception, Is.InstanceOf<OperationCanceledException>());
            Assert.That(_harness.Manager.IsTransitionActive, Is.False);
            Assert.That(_harness.PooledEntries, Is.EqualTo(2));
        }

        [Test]
        public void AfterResetAnyWidgetCanBeTheNextSource()
        {
            _harness.RunInstantThenQueue(_a, _b, () =>
            {
                _harness.Run(Instant, _b, _c);
                _harness.Run(Instant, _c, _d);
            });
            Assert.That(_harness.RejectionOf(() => _harness.Manager.Transition(Instant, _a, _e)),
                Is.InstanceOf<InvalidOperationException>(), "arrange: only the last queued target is accepted");

            _harness.Manager.Reset();
            AwaitableProbe next = _harness.Run(Instant, _a, _e);

            Assert.That(next.IsComplete, Is.True);
            Assert.That(next.Exception, Is.Null);
            Assert.That(_e.Visibility, Is.EqualTo(WidgetVisibility.Visible));
        }

        [Test]
        public void ResetCancelsTheAnimationOfAnExecutingTransition()
        {
            _b.EnableAnimation();
            _a.SetVisibility(WidgetVisibility.Visible);
            AwaitableProbe transition = _harness.Run(EntryOnly(_entry), _a, _b);
            FakeAnimationRun run = _b.LastRun;
            Assert.That(run?.State, Is.EqualTo(FakeAnimationRun.RunState.Running), "arrange: the target is animating");

            _harness.Manager.Reset();

            Assert.That(run.State, Is.EqualTo(FakeAnimationRun.RunState.Cancelled));
            Assert.That(transition.Exception, Is.InstanceOf<OperationCanceledException>());
            Assert.That(_harness.Manager.IsTransitionActive, Is.False);
            Assert.That(_harness.PooledEntries, Is.EqualTo(1));
        }

        [Test]
        public void ResetEmptiesTheActiveSlotEvenWhenTheActiveTransitionIgnoresCancellation()
        {
            _b.EnableAnimation();
            _b.IgnoreCancellation = true;
            _a.SetVisibility(WidgetVisibility.Visible);
            AwaitableProbe active = _harness.Run(EntryOnly(_entry), _a, _b);
            Assert.That(_harness.Manager.ActiveTarget, Is.SameAs(_b), "arrange: the animated transition is active");

            _harness.Manager.Reset();

            Assert.That(_harness.Manager.IsTransitionActive, Is.False);
            Assert.That(_harness.Manager.ActiveTarget, Is.Null);
            Assert.That(active.IsComplete, Is.False, "the ignoring animation is still running");

            _b.LastRun.Complete();
            Assert.That(active.IsComplete, Is.True, "settled by its animation");
            Assert.That(_harness.PooledEntries, Is.EqualTo(1), "and its entry is released once, by its own transition");
        }

        [Test]
        public void ResetCancelsQueuedTransitionsEvenWhenTheActiveTransitionIgnoresCancellation()
        {
            _b.EnableAnimation();
            _b.IgnoreCancellation = true;
            _a.SetVisibility(WidgetVisibility.Visible);
            AwaitableProbe active = _harness.Run(EntryOnly(_entry), _a, _b);
            AwaitableProbe second = _harness.Run(Instant, _b, _c);
            AwaitableProbe third = _harness.Run(Instant, _c, _d);
            Assert.That(_harness.Manager.ActiveTarget, Is.SameAs(_b), "arrange: the animated transition is active");

            _harness.Manager.Reset();

            Assert.That(second.Exception, Is.InstanceOf<OperationCanceledException>());
            Assert.That(third.Exception, Is.InstanceOf<OperationCanceledException>());
            Assert.That(_harness.PooledEntries, Is.EqualTo(2));

            _b.LastRun.Complete();
            Assert.That(active.IsComplete, Is.True, "arrange: the active transition settles");
            Assert.That(_harness.PooledEntries, Is.EqualTo(3), "every entry is released exactly once");
        }

        [Test]
        public void ATransitionThatFinishesAfterResetLeavesTheNextActiveTransitionInPlace()
        {
            _b.EnableAnimation();
            _b.IgnoreCancellation = true;
            _d.EnableAnimation();
            _a.SetVisibility(WidgetVisibility.Visible);
            _c.SetVisibility(WidgetVisibility.Visible);
            AwaitableProbe stale = _harness.Run(EntryOnly(_entry), _a, _b);
            _harness.Manager.Reset();
            AwaitableProbe current = _harness.Run(EntryOnly(_entry), _c, _d);
            Assert.That(_harness.Manager.ActiveTarget, Is.SameAs(_d), "arrange: a new transition is active");

            _b.LastRun.Complete();
            Assert.That(stale.IsComplete, Is.True, "arrange: the transition from before the reset has finished");

            Assert.That(_harness.Manager.ActiveTarget, Is.SameAs(_d));
            Assert.That(current.IsComplete, Is.False);
            Assert.That(_d.LastRun?.State, Is.EqualTo(FakeAnimationRun.RunState.Running));
        }
    }
}
