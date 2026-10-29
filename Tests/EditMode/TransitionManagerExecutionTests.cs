using NUnit.Framework;

using UIFramework.Core.Interfaces;
using UIFramework.TestUtils;
using UIFramework.Transitioning;

using static UIFramework.TestUtils.TransitionHarness;

namespace UIFramework.Tests.EditMode
{
    // How a transition orders and shows its two widgets. Sorting happens before either widget is animated, so
    // it is visible even where FakeScreen's animation members then throw.
    public sealed class TransitionManagerExecutionTests
    {
        private TransitionHarness _harness;
        private FakeScreenA _a;
        private FakeScreenB _b;
        private FakeAnimation _exit;
        private FakeAnimation _entry;

        [SetUp]
        public void SetUp()
        {
            _harness = new TransitionHarness();
            _a = new FakeScreenA();
            _b = new FakeScreenB();
            _exit = new FakeAnimation(1f);
            _entry = new FakeAnimation(1f);
            _a.SetVisibility(WidgetVisibility.Visible);
        }

        [TearDown]
        public void TearDown()
            => Assert.That(_harness.Close(), Is.Zero, "every awaitable the test started has settled");

        private static bool IsSort(string call) => call == nameof(FakeScreen.SortAbove)
            || call == nameof(FakeScreen.SortBelow) || call == nameof(FakeScreen.SortInlineWith);

        private static void AssertSortedOnlyBy(FakeScreen sorted, string sort, FakeScreen other)
        {
            Assert.That(sorted.Calls.FindAll(IsSort), Is.EqualTo(new[] { sort }), "one sort, by the sorted widget");
            Assert.That(other.Calls.FindAll(IsSort), Is.Empty);
        }

        [Test]
        public void AnInstantTransitionSortsTheTargetInlineWithTheSource()
        {
            _harness.Run(Instant, _a, _b);

            AssertSortedOnlyBy(_b, nameof(FakeScreen.SortInlineWith), _a);
        }

        [Test]
        public void AnEntryOnlyTransitionSortsTheTargetAboveTheSource()
        {
            _harness.Run(EntryOnly(_entry), _a, _b);

            AssertSortedOnlyBy(_b, nameof(FakeScreen.SortAbove), _a);
        }

        [Test]
        public void AnExitOnlyTransitionSortsTheSourceAboveTheTarget()
        {
            _harness.Run(ExitOnly(_exit), _a, _b);

            AssertSortedOnlyBy(_a, nameof(FakeScreen.SortAbove), _b);
        }

        [Test]
        public void ATwoSidedTransitionSortsTheTargetInlineWithTheSource()
        {
            _harness.Run(TwoSided(_exit, _entry), _a, _b);

            AssertSortedOnlyBy(_b, nameof(FakeScreen.SortInlineWith), _a);
        }

        [Test]
        public void SourcePrioritySortsTheSourceAboveTheTarget()
        {
            _harness.Run(InstantWithPriority(TransitionSortPriority.Source), _a, _b);

            AssertSortedOnlyBy(_a, nameof(FakeScreen.SortAbove), _b);
        }

        [Test]
        public void TargetPrioritySortsTheTargetAboveTheSource()
        {
            _harness.Run(InstantWithPriority(TransitionSortPriority.Target), _a, _b);

            AssertSortedOnlyBy(_b, nameof(FakeScreen.SortAbove), _a);
        }

        [Test]
        public void AnExitOnlyTransitionShowsTheTargetBeforeTheSourceAnimatesOut()
        {
            _a.EnableAnimation();

            AwaitableProbe transition = _harness.Run(ExitOnly(_exit), _a, _b);
            Assert.That(_a.LastRun?.State, Is.EqualTo(FakeAnimationRun.RunState.Running),
                "arrange: the source is animating out");

            Assert.That(_b.Visibility, Is.EqualTo(WidgetVisibility.Visible));
            Assert.That(_a.LastRun.Visibility, Is.EqualTo(WidgetVisibility.Hidden));

            _a.LastRun.Complete();
            Assert.That(transition.IsComplete, Is.True);
            Assert.That(transition.Exception, Is.Null);
            Assert.That(_harness.Manager.IsTransitionActive, Is.False);
        }

        [Test]
        public void SkipActiveWhileIdleCompletesAtOnce()
        {
            AwaitableProbe skip = _harness.Watch(_harness.Manager.SkipActive());

            Assert.That(skip.IsComplete, Is.True);
            Assert.That(skip.Exception, Is.Null);
        }

        [Test]
        public void RewindActiveWhileIdleCompletesAtOnceWithoutTakingAnEntry()
        {
            int pooled = _harness.PooledEntries;

            AwaitableProbe rewind = _harness.Watch(_harness.Manager.RewindActive());

            Assert.That(rewind.IsComplete, Is.True);
            Assert.That(rewind.Exception, Is.Null);
            Assert.That(_harness.PooledEntries, Is.EqualTo(pooled));
        }
    }
}
