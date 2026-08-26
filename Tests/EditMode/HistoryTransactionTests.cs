using NUnit.Framework;

using UIFramework.Controllers;
using UIFramework.Core.Interfaces;
using UIFramework.Groups;
using UIFramework.TestUtils;

using UnityEngine.Extension;

namespace UIFramework.Tests.EditMode
{
    // Known defect: history desynchronises from navigation when a navigation fails. NavigateToCoordinator
    // advances the navigator, pushes a history entry, and only then runs Execute — which throws when the
    // target rejects the request data. The entry is left uncommitted in the stack, and History.Pop/TryPeek
    // never check commit state, so it reads as a real back entry.
    [Category("Characterization")]
    public sealed class HistoryTransactionTests
    {
        private FakeScreenA _a;
        private FakeScreenB _b;
        private ScreenController _controller;

        [SetUp]
        public void SetUp()
        {
            _a = new FakeScreenA();
            _b = new FakeScreenB();   // AcceptsData is false, so navigating to it with data throws
            _controller = new ScreenController(new[] { new FakeCollector(_a, _b) }, TimeMode.Scaled, new ManualClock());
            _controller.Initialize();
            _controller.CreateNavigateToRequest<FakeScreenA>().Execute();
        }

        private void NavigateToBWithRejectedData()
            => Assert.That(() => _controller.CreateNavigateToRequest<FakeScreenB>().WithData("rejected").Execute(),
                Throws.ArgumentException);

        [Test]
        public void TheNavigatorAdvancesEvenThoughTheNavigationFailed()
        {
            NavigateToBWithRejectedData();

            Assert.That(_controller.ActiveGroup.ActiveScreen, Is.SameAs(_b),
                "the navigator moved before Execute could throw");
        }

        [Test]
        public void TheTargetIsNeverShown()
        {
            NavigateToBWithRejectedData();

            Assert.That(_b.Visibility, Is.EqualTo(WidgetVisibility.Hidden));
            Assert.That(_a.Visibility, Is.EqualTo(WidgetVisibility.Visible),
                "the previous screen is left visible under an 'active' screen that never appeared");
        }

        [Test]
        public void TheUncommittedEntryStillReadsAsABackEntry()
        {
            NavigateToBWithRejectedData();
            IScreenGroup group = _controller.ActiveGroup;

            Assert.That(group.PreviousScreen, Is.SameAs(_a),
                "the stranded entry is indistinguishable from a committed one");
        }
    }
}
