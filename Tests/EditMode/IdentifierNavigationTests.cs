using NUnit.Framework;

using UIFramework.Controllers;
using UIFramework.Core.Interfaces;
using UIFramework.Groups;
using UIFramework.Navigation;
using UIFramework.Navigation.Interfaces;
using UIFramework.Registry;
using UIFramework.TestUtils;

using UnityEngine.Extension;

namespace UIFramework.Tests.EditMode
{
    // Opening a screen by identifier without knowing whether this controller registers it: an unknown
    // identifier is an expected outcome, not an error. A null, empty or whitespace identifier means "no
    // identifier". Other misuse — an uninitialized controller, a screen held by another group — still throws.
    public sealed class IdentifierNavigationTests
    {
        private PoolScope _pools;
        private FakeScreenA _a;
        private FakeScreenB _b;
        private ScreenController _controller;

        [SetUp]
        public void SetUp()
        {
            _pools = new PoolScope();
            _a = new FakeScreenA { Identifier = "a" };
            _b = new FakeScreenB { Identifier = "b" };
            _controller = new ScreenController(new[] { new FakeCollector(_a, _b) }, TimeMode.Scaled, new ManualClock());
            _controller.Initialize();
        }

        [TearDown]
        public void TearDown() => _pools?.Dispose();

        [Test]
        public void AKnownIdentifierCreatesARequestThatNavigates()
        {
            Assert.That(_controller.TryCreateNavigateToRequest("b", out NavigateToRequest<IScreen> request), Is.True);
            request.Execute();

            Assert.That(_b.Visibility, Is.EqualTo(WidgetVisibility.Visible));
            Assert.That(_controller.ActiveGroup.ActiveScreen, Is.SameAs(_b));
        }

        [Test]
        public void AnUnknownIdentifierFailsWithoutSideEffects()
        {
            Assert.That(_controller.TryCreateNavigateToRequest("missing", out _), Is.False);

            Assert.That(_controller.Groups, Is.Empty, "a failed lookup does not open a group");
            Assert.That(_a.Visibility, Is.EqualTo(WidgetVisibility.Hidden));
            Assert.That(_b.Visibility, Is.EqualTo(WidgetVisibility.Hidden));
        }

        [Test]
        public void ANullOrBlankIdentifierFails()
        {
            Assert.That(_controller.TryCreateNavigateToRequest(null, out _), Is.False);
            Assert.That(_controller.TryCreateNavigateToRequest("", out _), Is.False);
            Assert.That(_controller.TryCreateNavigateToRequest("  ", out _), Is.False);
        }

        [Test]
        public void AnIdentifierHeldByAnotherGroupStillThrows()
        {
            Assume.That(_controller.TryCreateNavigateToRequest("a", out NavigateToRequest<IScreen> request), Is.True);
            request.Execute();
            _controller.PushGroup();

            Assert.That(() => _controller.TryCreateNavigateToRequest("a", out _),
                Throws.InvalidOperationException.With.Message.Contains("already held by another group"));
        }

        [Test]
        public void AScreenCanTryNavigateByIdentifierThroughItsNavigator()
        {
            Assume.That(_controller.TryCreateNavigateToRequest("a", out NavigateToRequest<IScreen> first), Is.True);
            first.Execute();
            IScreenNavigator navigator = _a.Navigator;

            Assert.That(navigator.TryCreateNavigateToRequest("missing", out _), Is.False);
            Assert.That(navigator.TryCreateNavigateToRequest("b", out NavigateToRequest<IScreen> request), Is.True);
            request.Execute();

            Assert.That(_b.Visibility, Is.EqualTo(WidgetVisibility.Visible));
            Assert.That(_controller.ActiveGroup.ActiveScreen, Is.SameAs(_b));
        }

        [Test]
        public void AGroupOverAnUninitializedRegistryThrows()
        {
            IScreenGroup group = new ScreenGroup(new WidgetRegistry<IScreen>(), TimeMode.Scaled, new ManualClock());

            Assert.That(() => group.TryCreateNavigateToRequest("a", out _), Throws.InvalidOperationException);
        }

        [Test]
        public void ATabControllerNavigatesToAKnownIdentifier()
        {
            TabController<IScreen> tabs = CreateTabs();
            tabs.Initialize();

            Assert.That(tabs.TryCreateNavigateToRequest("d", out NavigateToRequest<IScreen> request), Is.True);
            request.Execute();

            Assert.That(tabs.ActiveWindow, Is.SameAs(tabs.Tabs[1]));
            Assert.That(tabs.Tabs[1].Visibility, Is.EqualTo(WidgetVisibility.Visible));
            tabs.Terminate();
        }

        [Test]
        public void ATabControllerFailsOnAnUnknownOrNullIdentifier()
        {
            TabController<IScreen> tabs = CreateTabs();
            tabs.Initialize();

            Assert.That(tabs.TryCreateNavigateToRequest("missing", out _), Is.False);
            Assert.That(tabs.TryCreateNavigateToRequest(null, out _), Is.False);
            tabs.Terminate();
        }

        [Test]
        public void AnUninitializedTabControllerThrows()
        {
            TabController<IScreen> tabs = CreateTabs();

            Assert.That(() => tabs.TryCreateNavigateToRequest("c", out _), Throws.InvalidOperationException);
        }

        [Test]
        public void AnUninitializedControllerThrows()
        {
            ScreenController controller = new(new[] { new FakeCollector(new FakeScreenC { Identifier = "c" }) },
                TimeMode.Scaled, new ManualClock());

            Assert.That(() => controller.TryCreateNavigateToRequest("c", out _), Throws.InvalidOperationException);
        }

        [Test]
        public void WhitespaceIdentifiersAreNotRegistered()
        {
            WidgetRegistry<IScreen> registry = new();
            registry.Initialize();
            FakeScreenC first = new() { Identifier = "  " };
            FakeScreenD second = new() { Identifier = "  " };

            registry.Register(first);
            Assert.That(() => registry.Register(second), Throws.Nothing, "blank identifiers do not collide");
            Assert.That(registry.TryGet("  ", out _), Is.False);

            registry.Unregister(first);
            Assert.That(registry.Widgets, Is.EqualTo(new IScreen[] { second }));
        }

        private static TabController<IScreen> CreateTabs() =>
            new(TimeMode.Scaled, new IScreen[] { new FakeScreenC { Identifier = "c" }, new FakeScreenD { Identifier = "d" } });
    }
}
