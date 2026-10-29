using System.Collections;
using System.Collections.Generic;

using NUnit.Framework;

using UIFramework.Core.Interfaces;
using UIFramework.Registry;
using UIFramework.TestUtils;

using UnityEngine;
using UnityEngine.TestTools;

namespace UIFramework.Tests.PlayMode
{
    // A widget nested under another links to it as it awakes, and the parent then carries it through every
    // lifecycle transition: the parent's own state change brackets its children's.
    public sealed class WidgetHierarchyTests
    {
        private UguiScreenFixture _fixture;
        private UguiScreenA _parent;
        private RecordingWidget _child;

        [SetUp]
        public void SetUp()
        {
            _fixture = new UguiScreenFixture();
            _parent = _fixture.AddScreen<UguiScreenA>();
            _child = _fixture.AddWidget<RecordingWidget>(_parent.transform);
        }

        [UnityTearDown]
        public IEnumerator TearDown() => _fixture.Close();

        [Test]
        public void AChildRegistersWithItsParentWhenItAwakes()
        {
            Assert.That(_parent.ChildCount, Is.EqualTo(1));
            Assert.That(_parent.GetChildAt(0), Is.SameAs(_child));
            Assert.That(_child.Parent, Is.SameAs(_parent));
        }

        [Test]
        public void InitializingTheParentInitializesTheChild()
        {
            _parent.Initialize();

            Assert.That(_child.State, Is.EqualTo(WidgetState.Initialized));
            Assert.That(_child.InitializeCount, Is.EqualTo(1));
        }

        [Test]
        public void TerminatingTheParentBracketsTheChild()
        {
            _parent.Initialize();
            List<string> log = new();
            _parent.Terminating += _ => log.Add("parent terminating");
            _parent.Terminated += _ => log.Add("parent terminated");
            _child.Terminating += _ => log.Add("child terminating");
            _child.Terminated += _ => log.Add("child terminated");

            _parent.Terminate();

            Assert.That(log, Is.EqualTo(new[]
            {
                "parent terminating", "child terminating", "child terminated", "parent terminated"
            }));
        }

        [Test]
        public void AParentSkipsAChildAlreadyInTheTargetState()
        {
            _child.Initialize();
            Assert.That(() => _parent.Initialize(), Throws.Nothing);
            Assert.That(_child.InitializeCount, Is.EqualTo(1), "the child is not initialized a second time");

            _child.Terminate();
            Assert.That(() => _parent.Terminate(), Throws.Nothing);
            Assert.That(_child.TerminateCount, Is.EqualTo(1), "nor terminated a second time");
        }

        [Test]
        public void ADestroyedChildLeavesItsParent()
        {
            Object.DestroyImmediate(_child.gameObject);

            Assert.That(_parent.ChildCount, Is.Zero);
            Assert.That(() => _parent.Initialize(), Throws.Nothing);
            Assert.That(() => _parent.Terminate(), Throws.Nothing);
        }

        // A registry holding a parent and its nested screen hears the child come up twice: through the parent's
        // cascade, then again when it reaches the child in its own right. It announces each transition once.
        [Test]
        public void ANestedScreenIsAnnouncedOnce()
        {
            UguiScreenB nested = _fixture.AddScreen<UguiScreenB>(_parent.transform);
            WidgetRegistry<IScreen> registry = _fixture.Track(new WidgetRegistry<IScreen>());
            List<string> announced = new();
            registry.WidgetInitialized += screen => announced.Add("init:" + screen.GetType().Name);
            registry.WidgetTerminated += screen => announced.Add("term:" + screen.GetType().Name);
            registry.Register(_parent);
            registry.Register(nested);

            registry.Initialize();
            registry.Terminate();

            Assert.That(announced, Is.EqualTo(new[]
            {
                "init:UguiScreenB", "init:UguiScreenA", "term:UguiScreenA", "term:UguiScreenB"
            }));
            Assert.That(nested.InitializeCount, Is.EqualTo(1));
            Assert.That(nested.TerminateCount, Is.EqualTo(1));
        }
    }
}
