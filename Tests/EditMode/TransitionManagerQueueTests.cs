using System;
using System.Threading;

using NUnit.Framework;

using UIFramework.Core.Interfaces;
using UIFramework.TestUtils;

using static UIFramework.TestUtils.TransitionHarness;

namespace UIFramework.Tests.EditMode
{
    // Transitions run one at a time, in the order they were started. Each one must start from where the one
    // before it ends, so its source must be the target of the last queued transition, or of the active one
    // when nothing is queued. A cancelled transition takes every transition queued after it with it, since
    // those start from where it would have ended.
    public sealed class TransitionManagerQueueTests
    {
        private TransitionHarness _harness;
        private FakeScreenA _a;
        private FakeScreenB _b;
        private FakeScreenC _c;
        private FakeScreenD _d;
        private FakeScreenE _e;

        [SetUp]
        public void SetUp()
        {
            _harness = new TransitionHarness();
            _a = new FakeScreenA();
            _b = new FakeScreenB();
            _c = new FakeScreenC();
            _d = new FakeScreenD();
            _e = new FakeScreenE();
        }

        [TearDown]
        public void TearDown()
            => Assert.That(_harness.Close(), Is.Zero, "every awaitable the test started has settled");

        private Exception RejectionFrom(IWidget source)
            => _harness.RejectionOf(() => _harness.Manager.Transition(Instant, source, _a));

        [Test]
        public void AnInstantTransitionStartedWhileIdleCompletesAtOnce()
        {
            _a.SetVisibility(WidgetVisibility.Visible);

            AwaitableProbe transition = _harness.Run(Instant, _a, _b);

            Assert.That(transition.IsComplete, Is.True);
            Assert.That(transition.Exception, Is.Null);
            Assert.That(_a.Visibility, Is.EqualTo(WidgetVisibility.Hidden));
            Assert.That(_b.Visibility, Is.EqualTo(WidgetVisibility.Visible));
            Assert.That(_harness.Manager.IsTransitionActive, Is.False);
            Assert.That(_harness.PooledEntries, Is.EqualTo(1), "its entry is released exactly once");
        }

        [Test]
        public void TheRunningTransitionIsReportedAsActive()
        {
            bool active = false;
            IWidget activeSource = null, activeTarget = null;
            _b.Shown += _ =>
            {
                active = _harness.Manager.IsTransitionActive;
                activeSource = _harness.Manager.ActiveSource;
                activeTarget = _harness.Manager.ActiveTarget;
            };

            _harness.Run(Instant, _a, _b);

            Assert.That(active, Is.True);
            Assert.That(activeSource, Is.SameAs(_a));
            Assert.That(activeTarget, Is.SameAs(_b));
            Assert.That(_harness.Manager.ActiveSource, Is.Null, "nothing is active once it has finished");
            Assert.That(_harness.Manager.ActiveTarget, Is.Null);
        }

        [Test]
        public void ATransitionStartedWhileAnotherRunsWaitsForItsTurn()
        {
            AwaitableProbe second = null;
            bool completeWhenStarted = true;
            WidgetVisibility targetWhenStarted = WidgetVisibility.Visible;

            AwaitableProbe first = _harness.RunInstantThenQueue(_a, _b, () =>
            {
                second = _harness.Run(Instant, _b, _c);
                completeWhenStarted = second.IsComplete;
                targetWhenStarted = _c.Visibility;
            });

            Assert.That(first.IsComplete, Is.True);
            Assert.That(first.Exception, Is.Null, "the running transition is undisturbed");
            Assert.That(completeWhenStarted, Is.False, "the second transition does not run inside the first");
            Assert.That(targetWhenStarted, Is.EqualTo(WidgetVisibility.Hidden));
        }

        [Test]
        public void WhenTheActiveTransitionEndsTheFirstQueuedTransitionBecomesActive()
        {
            _harness.RunInstantThenQueue(_a, _b, () =>
            {
                _harness.Run(Instant, _b, _c);
                _harness.Run(Instant, _c, _d);
                _harness.Run(Instant, _d, _e);
            });

            Assert.That(_harness.Manager.IsTransitionActive, Is.True);
            Assert.That(_harness.Manager.ActiveSource, Is.SameAs(_b));
            Assert.That(_harness.Manager.ActiveTarget, Is.SameAs(_c));
        }

        [Test]
        public void ANullSourceIsRejectedWithoutTakingAnEntry()
        {
            _harness.Run(Instant, _c, _d);
            Assert.That(_harness.PooledEntries, Is.EqualTo(1), "arrange: the pool holds an entry to take");

            Exception rejection = _harness.RejectionOf(() => _harness.Manager.Transition(Instant, null, _b));

            Assert.That(rejection, Is.InstanceOf<ArgumentNullException>());
            Assert.That(_harness.Manager.IsTransitionActive, Is.False);
            Assert.That(_harness.PooledEntries, Is.EqualTo(1));
        }

        [Test]
        public void ANullTargetIsRejectedWithoutTakingAnEntry()
        {
            _harness.Run(Instant, _c, _d);
            Assert.That(_harness.PooledEntries, Is.EqualTo(1), "arrange: the pool holds an entry to take");

            Exception rejection = _harness.RejectionOf(() => _harness.Manager.Transition(Instant, _a, null));

            Assert.That(rejection, Is.InstanceOf<ArgumentNullException>());
            Assert.That(_harness.Manager.IsTransitionActive, Is.False);
            Assert.That(_harness.PooledEntries, Is.EqualTo(1));
        }

        [Test]
        public void WithNothingQueuedOnlyTheActiveTargetIsAcceptedAsTheNextSource()
        {
            _harness.RunInstantThenQueue(_a, _b, () => _harness.Run(Instant, _b, _c));
            int pooled = _harness.PooledEntries;

            Assert.That(RejectionFrom(_b), Is.InstanceOf<InvalidOperationException>(), "the active source");
            Assert.That(_harness.PooledEntries, Is.EqualTo(pooled), "a rejected transition takes no entry");
            Assert.That(RejectionFrom(_c), Is.Null, "the active target");
        }

        [Test]
        public void WithTransitionsQueuedOnlyTheLastQueuedTargetIsAcceptedAsTheNextSource()
        {
            _harness.RunInstantThenQueue(_a, _b, () =>
            {
                _harness.Run(Instant, _b, _c);
                _harness.Run(Instant, _c, _d);
                _harness.Run(Instant, _d, _e);
            });

            Assert.That(RejectionFrom(_c), Is.InstanceOf<InvalidOperationException>(), "the active target");
            Assert.That(RejectionFrom(_d), Is.InstanceOf<InvalidOperationException>(), "the first queued target");
            Assert.That(RejectionFrom(_e), Is.Null, "the last queued target");
        }

        [Test]
        public void ATransitionWhoseTokenIsAlreadyCancelledEndsCancelledWithoutRunning()
        {
            _a.SetVisibility(WidgetVisibility.Visible);

            AwaitableProbe transition = _harness.Run(Instant, _a, _b, _harness.CancelledToken());

            Assert.That(transition.Exception, Is.InstanceOf<OperationCanceledException>());
            Assert.That(_a.Visibility, Is.EqualTo(WidgetVisibility.Visible));
            Assert.That(_b.Visibility, Is.EqualTo(WidgetVisibility.Hidden));
            Assert.That(_harness.Manager.IsTransitionActive, Is.False);
            Assert.That(_harness.PooledEntries, Is.EqualTo(1), "its entry is released");
        }

        [Test]
        public void CancellingTheActiveTransitionWithNothingQueuedLeavesTheManagerIdle()
        {
            CancellationTokenSource secondSource = _harness.CreateSource();
            AwaitableProbe second = null;
            _harness.RunInstantThenQueue(_a, _b, () => second = _harness.Run(Instant, _b, _c, secondSource.Token));
            Assert.That(_harness.Manager.ActiveTarget, Is.SameAs(_c), "arrange: the queued transition is active");

            secondSource.Cancel();

            Assert.That(second.Exception, Is.InstanceOf<OperationCanceledException>());
            Assert.That(_harness.Manager.IsTransitionActive, Is.False);
            Assert.That(_harness.PooledEntries, Is.EqualTo(2));
        }

        [Test]
        public void CancellingAQueuedTransitionCancelsEveryTransitionQueuedAfterIt()
        {
            CancellationTokenSource thirdSource = _harness.CreateSource();
            AwaitableProbe second = null, third = null, fourth = null;
            _harness.RunInstantThenQueue(_a, _b, () =>
            {
                second = _harness.Run(Instant, _b, _c);
                third = _harness.Run(Instant, _c, _d, thirdSource.Token);
                fourth = _harness.Run(Instant, _d, _e);
            });
            Assert.That(_harness.PooledEntries, Is.EqualTo(1),
                "arrange: only the finished transition has released its entry");

            thirdSource.Cancel();

            Assert.That(third.Exception, Is.InstanceOf<OperationCanceledException>());
            Assert.That(fourth.Exception, Is.InstanceOf<OperationCanceledException>());
            Assert.That(second.IsComplete, Is.False, "the transition before it is untouched");
            Assert.That(_harness.Manager.ActiveTarget, Is.SameAs(_c));
            Assert.That(_harness.PooledEntries, Is.EqualTo(3));
        }

        [Test]
        public void AfterTheQueueIsCancelledTheActiveTargetIsTheNextSourceAgain()
        {
            CancellationTokenSource thirdSource = _harness.CreateSource();
            _harness.RunInstantThenQueue(_a, _b, () =>
            {
                _harness.Run(Instant, _b, _c);
                _harness.Run(Instant, _c, _d, thirdSource.Token);
                _harness.Run(Instant, _d, _e);
            });
            Assert.That(RejectionFrom(_c), Is.InstanceOf<InvalidOperationException>(),
                "arrange: the last queued target is the expected source");

            thirdSource.Cancel();

            Assert.That(RejectionFrom(_e), Is.InstanceOf<InvalidOperationException>(), "the cancelled last target");
            Assert.That(RejectionFrom(_c), Is.Null, "the active target");
        }

        [Test]
        public void CancellingTheLastQueuedTransitionLeavesTheOthersInLine()
        {
            CancellationTokenSource fourthSource = _harness.CreateSource();
            AwaitableProbe second = null, third = null, fourth = null;
            _harness.RunInstantThenQueue(_a, _b, () =>
            {
                second = _harness.Run(Instant, _b, _c);
                third = _harness.Run(Instant, _c, _d);
                fourth = _harness.Run(Instant, _d, _e, fourthSource.Token);
            });

            fourthSource.Cancel();

            Assert.That(fourth.Exception, Is.InstanceOf<OperationCanceledException>());
            Assert.That(second.IsComplete, Is.False);
            Assert.That(third.IsComplete, Is.False);
            Assert.That(_harness.Manager.ActiveTarget, Is.SameAs(_c));
            Assert.That(_harness.PooledEntries, Is.EqualTo(2));
            Assert.That(RejectionFrom(_e), Is.InstanceOf<InvalidOperationException>(), "the cancelled last target");
            Assert.That(RejectionFrom(_d), Is.Null, "the target of the transition now last in line");
        }
    }
}
