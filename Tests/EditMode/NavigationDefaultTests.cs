using NUnit.Framework;

using UIFramework.Controllers;
using UIFramework.Core.Interfaces;
using UIFramework.TestUtils;

using UnityEngine.Extension;

namespace UIFramework.Tests.EditMode
{
    // Known defect: navigation never animates by default. NavigateToRequest.IsInstant() is true whenever
    // neither the Animation nor the Transition flag is set, so the ergonomic path never animates and a
    // target's default entry animation is never resolved. WithLength alone sets only the Length flag, so it
    // changes nothing.
    //
    // FakeScreen.AnimateVisibility throws, so reaching Visible without an exception is itself proof that the
    // instant path was taken.
    public sealed class NavigationDefaultTests
    {
        private FakeScreenA _a;
        private FakeAnimation _entryAnimation;
        private ScreenController _controller;

        [SetUp]
        public void SetUp()
        {
            _entryAnimation = new FakeAnimation(0.5f);
            _a = new FakeScreenA { DefaultVisibleAnimation = _entryAnimation };
            _controller = new ScreenController(new[] { new FakeCollector(_a) }, TimeMode.Scaled, new ManualClock());
            _controller.Initialize();
        }

        [Test]
        public void ExecuteIsInstantEvenWhenTheTargetHasADefaultAnimation()
        {
            _controller.CreateNavigateToRequest<FakeScreenA>().Execute();

            Assert.That(_a.Visibility, Is.EqualTo(WidgetVisibility.Visible));
            Assert.That(_a.Calls, Does.Contain("SetVisibility:Visible"), "shown instantly, not animated");
            Assert.That(_entryAnimation.Evaluations, Is.Empty, "the default animation is never played");
        }

        [Test]
        public void WithLengthAloneDoesNotProduceAnAnimation()
        {
            _controller.CreateNavigateToRequest<FakeScreenA>().WithLength(0.5f).Execute();

            Assert.That(_a.Calls, Does.Contain("SetVisibility:Visible"));
            Assert.That(_entryAnimation.Evaluations, Is.Empty,
                "WithLength sets only the Length flag, so IsInstant() is still true");
        }

        [Test]
        public void WithEasingModeAloneDoesNotProduceAnAnimation()
        {
            _controller.CreateNavigateToRequest<FakeScreenA>().WithEasingMode(EasingMode.EaseInOut).Execute();

            Assert.That(_entryAnimation.Evaluations, Is.Empty);
        }
    }
}
