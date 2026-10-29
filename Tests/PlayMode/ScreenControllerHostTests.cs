using System.Collections;

using NUnit.Framework;

using UIFramework.Collectors;
using UIFramework.Controllers;
using UIFramework.Core.Interfaces;
using UIFramework.Navigation;
using UIFramework.TestUtils;
using UIFramework.Transitioning;

using UnityEngine.Extension;
using UnityEngine.TestTools;

namespace UIFramework.Tests.PlayMode
{
    // The shipping host over real screens found by a real collector. Enabling builds a controller and brings the
    // screens up; disabling tears it down and terminates them; enabling again adopts the same screens afresh,
    // with no navigation state carried over.
    public sealed class ScreenControllerHostTests
    {
        private UguiScreenFixture _fixture;
        private UguiScreenA _a;
        private UguiScreenB _b;
        private UguiScreenC _c;

        [SetUp]
        public void SetUp()
        {
            _fixture = new UguiScreenFixture();
            _a = _fixture.AddScreen<UguiScreenA>();
            _b = _fixture.AddScreen<UguiScreenB>();
            _c = _fixture.AddScreen<UguiScreenC>();
        }

        [UnityTearDown]
        public IEnumerator TearDown() => _fixture.Close();

        private ScreenControllerHost CreateHost() => _fixture.CreateHost(_fixture.AddCollector());

        private static void Cycle(ScreenControllerHost host)
        {
            host.gameObject.SetActive(false);
            host.gameObject.SetActive(true);
        }

        private void Show<TScreen>(ScreenControllerHost host) where TScreen : class, IScreen
            => _fixture.Instant(host.NavigateTo<TScreen>().Execute());

        private AwaitableProbe FadeTo<TScreen>(ScreenControllerHost host, float length) where TScreen : class, IScreen
            => _fixture.Watch(host.NavigateTo<TScreen>()
                .WithTransition(Transition.Fade(length, EasingMode.Linear))
                .Execute());

        // A collector added with no custom root searches its own hierarchy.
        [Test]
        public void ACollectorWithNoCustomRootFindsTheScreensBelowIt()
        {
            ScreenCollector collector = _fixture.Root.AddComponent<ScreenCollector>();

            Assert.That(collector.Collect(), Is.EquivalentTo(new IScreen[] { _a, _b, _c }));
        }

        [Test]
        public void EnablingTheHostInitializesTheCollectedScreens()
        {
            ScreenControllerHost host = CreateHost();

            Assert.That(host.Controller, Is.Not.Null);
            Assert.That(host.Controller.Groups, Is.Empty);
            foreach (RecordingScreen screen in new RecordingScreen[] { _a, _b, _c })
            {
                Assert.That(screen.State, Is.EqualTo(WidgetState.Initialized), screen.name);
                Assert.That(screen.Visibility, Is.EqualTo(WidgetVisibility.Hidden), screen.name);
                Assert.That(screen.gameObject.activeSelf, Is.False, screen.name);
            }
        }

        [Test]
        public void TheHostNavigatesReturnsAndExits()
        {
            ScreenControllerHost host = CreateHost();

            Show<UguiScreenA>(host);
            Show<UguiScreenB>(host);
            Assert.That(_b.Visibility, Is.EqualTo(WidgetVisibility.Visible));
            Assert.That(_a.gameObject.activeSelf, Is.False);

            _fixture.Instant(host.Return());
            Assert.That(_a.Visibility, Is.EqualTo(WidgetVisibility.Visible));
            Assert.That(_b.gameObject.activeSelf, Is.False);

            host.PushGroup();
            Show<UguiScreenC>(host);
            Assert.That(_c.Visibility, Is.EqualTo(WidgetVisibility.Visible));
            Assert.That(_a.CanvasGroup.blocksRaycasts, Is.False, "the overlay pauses the layer below");

            _fixture.Instant(host.Return());
            Assert.That(host.Controller.Groups, Has.Count.EqualTo(1), "returning at the overlay's root collapses it");
            Assert.That(_c.gameObject.activeSelf, Is.False);
            Assert.That(_a.CanvasGroup.blocksRaycasts, Is.True);

            host.PushGroup();
            Show<UguiScreenC>(host);
            _fixture.Instant(host.Exit(new ExitRequest()));
            Assert.That(host.Controller.Groups, Has.Count.EqualTo(1), "exiting the overlay collapses it");
            Assert.That(_c.gameObject.activeSelf, Is.False);
            Assert.That(_a.Visibility, Is.EqualTo(WidgetVisibility.Visible));
            Assert.That(_a.CanvasGroup.blocksRaycasts, Is.True);
        }

        [Test]
        public void DisablingTheHostTerminatesItsScreens()
        {
            ScreenControllerHost host = CreateHost();
            Show<UguiScreenA>(host);
            Show<UguiScreenB>(host);
            host.PushGroup();
            Show<UguiScreenC>(host);

            host.gameObject.SetActive(false);

            Assert.That(host.Controller, Is.Null);
            foreach (RecordingScreen screen in new RecordingScreen[] { _a, _b, _c })
            {
                Assert.That(screen.State, Is.EqualTo(WidgetState.Terminated), screen.name);
                Assert.That(screen.gameObject.activeSelf, Is.False, screen.name);
                Assert.That(screen.Navigator, Is.Null, $"{screen.name} is detached from the discarded group");
            }
        }

        [Test]
        public void AReEnabledHostNavigatesTheSameScreensAgain()
        {
            ScreenControllerHost host = CreateHost();
            Show<UguiScreenA>(host);
            Show<UguiScreenB>(host);

            Cycle(host);

            Assert.That(host.Controller.Groups, Is.Empty, "no group survives the cycle");
            foreach (RecordingScreen screen in new RecordingScreen[] { _a, _b, _c })
                Assert.That(screen.State, Is.EqualTo(WidgetState.Initialized), screen.name);

            Show<UguiScreenA>(host);

            Assert.That(_a.Visibility, Is.EqualTo(WidgetVisibility.Visible));
            Assert.That(_a.Navigator, Is.SameAs(host.ActiveGroup));
            Assert.That(_fixture.Instant(host.Return()).Result.Success, Is.False, "and no history survives it either");
        }

        [Test]
        public void AHostCycleCarriesTheChildrenOfItsScreens()
        {
            RecordingWidget child = _fixture.AddWidget<RecordingWidget>(_a.transform);
            ScreenControllerHost host = CreateHost();
            Assert.That(child.State, Is.EqualTo(WidgetState.Initialized), "a screen's child comes up with it");

            host.gameObject.SetActive(false);
            Assert.That(child.State, Is.EqualTo(WidgetState.Terminated), "and goes down with it");

            host.gameObject.SetActive(true);
            Assert.That(child.State, Is.EqualTo(WidgetState.Initialized), "and comes back up when it is adopted again");
            Assert.That(child.InitializeCount, Is.EqualTo(2));
        }

        [UnityTest]
        public IEnumerator DisablingTheHostMidNavigationSettlesEverything()
        {
            ScreenControllerHost host = CreateHost();
            Show<UguiScreenA>(host);
            AwaitableProbe navigation = FadeTo<UguiScreenB>(host, 2f);
            yield return null;
            Assert.That(_b.IsAnimating, Is.True, "arrange: the navigation is in flight");

            host.gameObject.SetActive(false);

            Assert.That(_b.State, Is.EqualTo(WidgetState.Terminated));
            Assert.That(_b.IsAnimating, Is.False);
            Assert.That(_b.gameObject.activeSelf, Is.False);
            Assert.That(_b.CanvasGroup.alpha, Is.EqualTo(1f), "the interrupted fade is undone");
            Assert.That(_fixture.PooledHandles, Is.EqualTo(1),
                "the interrupted show's handle is returned exactly once");

            // The navigation is wound up with its host.
            yield return navigation.WaitForCompletion(1f);

            host.gameObject.SetActive(true);
            Show<UguiScreenA>(host);

            Assert.That(_a.Visibility, Is.EqualTo(WidgetVisibility.Visible), "the re-enabled host navigates as normal");
        }
    }
}
