using System.Collections.Generic;

using NUnit.Framework;

using UIFramework.Controllers;
using UIFramework.Core.Interfaces;
using UIFramework.Registry;
using UIFramework.TestUtils;

using UnityEngine.Extension;

namespace UIFramework.Tests.EditMode
{
    // A registered widget is not always the registry's to drive. The UI Toolkit backend initializes and
    // terminates itself when its UIDocument cycles, because that is the only path that re-resolves its visual
    // tree. So the registry raises WidgetInitialized/WidgetTerminated by relaying the widget's own events
    // rather than by announcing what it did, and a widget that arrives already initialized is announced on
    // attachment — Unity's component enable order decides which of the two happens first.
    //
    // The invariant these pin: for as long as a widget is registered, its announcements strictly alternate,
    // so a consumer that wires on one and unwires on the other stays balanced.
    public sealed class RegistryLifecycleTests
    {
        private readonly List<string> _announced = new();
        private WidgetRegistry<IScreen> _registry;
        private FakeScreenA _a;

        [SetUp]
        public void SetUp()
        {
            _announced.Clear();
            _a = new FakeScreenA();
            _registry = new WidgetRegistry<IScreen>();
            _registry.WidgetInitialized += widget => _announced.Add($"init:{widget.GetType().Name}");
            _registry.WidgetTerminated += widget => _announced.Add($"term:{widget.GetType().Name}");
        }

        // This used to fail: the registry only initialized widgets in the Uninitialized state, so a widget
        // whose host had been disabled and re-enabled was registered and then left dead.
        [Test]
        public void ATerminatedWidgetIsInitializedWhenRegistered()
        {
            _registry.Initialize();
            _a.ForceState(WidgetState.Terminated);

            _registry.Register(_a);

            Assert.That(_a.State, Is.EqualTo(WidgetState.Initialized));
            Assert.That(_announced, Is.EqualTo(new[] { "init:FakeScreenA" }));
        }

        [Test]
        public void ATerminatedWidgetIsInitializedByRegistryInitialize()
        {
            _registry.Register(_a);
            _a.ForceState(WidgetState.Terminated);

            _registry.Initialize();

            Assert.That(_a.State, Is.EqualTo(WidgetState.Initialized));
            Assert.That(_announced, Is.EqualTo(new[] { "init:FakeScreenA" }));
        }

        [Test]
        public void EachRegistryDrivenTransitionIsAnnouncedOnce()
        {
            _registry.Register(_a);
            _registry.Initialize();
            _registry.Terminate();

            Assert.That(_announced, Is.EqualTo(new[] { "init:FakeScreenA", "term:FakeScreenA" }));
        }

        [Test]
        public void TerminationIsAnnouncedBeforeTeardown()
        {
            _registry.Register(_a);
            _registry.Initialize();
            WidgetState observed = WidgetState.Uninitialized;
            _registry.WidgetTerminated += widget => observed = widget.State;

            _registry.Unregister(_a);

            Assert.That(observed, Is.EqualTo(WidgetState.Initialized), "consumers unwire while the widget is live");
            Assert.That(_a.State, Is.EqualTo(WidgetState.Terminated));
        }

        [Test]
        public void ASelfDrivenTransitionIsAnnounced()
        {
            _registry.Register(_a);
            _registry.Initialize();
            _announced.Clear();

            _a.Terminate();
            _a.Initialize();

            Assert.That(_announced, Is.EqualTo(new[] { "term:FakeScreenA", "init:FakeScreenA" }));
        }

        [Test]
        public void RegisteringAnAlreadyInitializedWidgetAnnouncesIt()
        {
            _registry.Initialize();
            _a.Initialize();
            _announced.Clear();

            _registry.Register(_a);

            Assert.That(_announced, Is.EqualTo(new[] { "init:FakeScreenA" }));
        }

        [Test]
        public void InitializeAnnouncesAnAlreadyInitializedWidget()
        {
            _registry.Register(_a);
            _a.Initialize();

            _registry.Initialize();

            Assert.That(_announced, Is.EqualTo(new[] { "init:FakeScreenA" }),
                "and the transition itself was silent, since an uninitialized registry does not speak");
        }

        [Test]
        public void AnUnregisteredWidgetIsNoLongerAnnounced()
        {
            _registry.Register(_a);
            _registry.Initialize();
            _registry.Unregister(_a);
            _announced.Clear();

            _a.Initialize();
            _a.Terminate();

            Assert.That(_announced, Is.Empty);
        }

        [Test]
        public void TerminatingTheRegistryDetachesItsWidgets()
        {
            _registry.Register(_a);
            _registry.Initialize();
            _registry.Terminate();
            _announced.Clear();

            _a.Initialize();
            _a.Terminate();

            Assert.That(_announced, Is.Empty);
        }

        [Test]
        public void AnnouncementsAlternateAcrossAHostCycle()
        {
            _registry.Register(_a);
            _registry.Initialize();
            _a.Terminate();
            _a.Initialize();
            _registry.Unregister(_a);

            Assert.That(_announced, Is.EqualTo(new[]
            {
                "init:FakeScreenA", "term:FakeScreenA", "init:FakeScreenA", "term:FakeScreenA"
            }));
        }

        // A hierarchy collector registers a parent and its nested children, so the parent's cascade drives a
        // child the registry is also holding. The relay hears that cascade, and the registry then reaches the
        // child in its own right — this used to announce it twice and break the alternation above.
        [Test]
        public void ACascadeInitializedChildIsAnnouncedOnce()
        {
            FakeScreenB child = new();
            _a.AddChild(child);
            _registry.Register(_a);
            _registry.Register(child);

            _registry.Initialize();

            Assert.That(_announced, Is.EqualTo(new[] { "init:FakeScreenB", "init:FakeScreenA" }),
                "the child comes up inside the parent's cascade, so its event lands first");
        }

        [Test]
        public void ACascadeTerminatedChildIsAnnouncedOnce()
        {
            FakeScreenB child = new();
            _a.AddChild(child);
            _registry.Register(_a);
            _registry.Register(child);
            _registry.Initialize();
            _announced.Clear();

            _registry.Terminate();

            Assert.That(_announced, Is.EqualTo(new[] { "term:FakeScreenA", "term:FakeScreenB" }),
                "Terminating is raised before the cascade, so the parent's lands first");
        }

        [Test]
        public void AParentCascadeSkipsAChildThatIsAlreadyInitialized()
        {
            FakeScreenB child = new();
            _a.AddChild(child);
            child.Initialize();

            Assert.That(() => _a.Initialize(), Throws.Nothing);
            Assert.That(child.State, Is.EqualTo(WidgetState.Initialized));
        }

        // The whole point, end to end: the host rebuilds its controller on every enable over screens that
        // survive in the scene, so the second enable adopts screens the first one terminated.
        [Test]
        public void ARebuiltControllerReinitializesScreensLeftTerminated()
        {
            ScreenController first = new(new[] { new FakeCollector(_a) }, TimeMode.Scaled, new ManualClock());
            first.Initialize();
            first.Terminate();
            Assume.That(_a.State, Is.EqualTo(WidgetState.Terminated));

            ScreenController second = new(new[] { new FakeCollector(_a) }, TimeMode.Scaled, new ManualClock());
            second.Initialize();

            Assert.That(_a.State, Is.EqualTo(WidgetState.Initialized));

            second.CreateNavigateToRequest<FakeScreenA>().Execute();

            Assert.That(second.ActiveGroup.ActiveScreen, Is.SameAs(_a), "and it can be navigated to again");
            Assert.That(_a.Visibility, Is.EqualTo(WidgetVisibility.Visible));
        }
    }
}
