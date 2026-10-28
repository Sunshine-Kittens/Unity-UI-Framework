using System.Collections.Generic;
using System.Text.RegularExpressions;

using NUnit.Framework;

using UIFramework.Core.Interfaces;
using UIFramework.Registry;
using UIFramework.TestUtils;

using UnityEngine;
using UnityEngine.TestTools;

namespace UIFramework.Tests.EditMode
{
    // A widget's initialization and teardown run arbitrary backend code that can throw — a screen with no
    // parent canvas, a destroyed object mid-teardown. The registry's contract under failure: registration is
    // all-or-nothing, initialization fails fast but stays unwindable through Terminate, and teardown always
    // completes its sweep. Handlers run only against a fully consistent registry, and may register or
    // unregister without breaking an in-flight sweep.
    public sealed class RegistryExceptionSafetyTests
    {
        private readonly List<string> _announced = new();
        private WidgetRegistry<IScreen> _registry;

        [SetUp]
        public void SetUp()
        {
            _announced.Clear();
            _registry = new WidgetRegistry<IScreen>();
            _registry.WidgetRegistered += (widget, _) => _announced.Add($"registered:{widget.GetType().Name}");
            _registry.WidgetInitialized += widget => _announced.Add($"init:{widget.GetType().Name}");
            _registry.WidgetTerminated += widget => _announced.Add($"term:{widget.GetType().Name}");
        }

        [Test]
        public void AFailedRegistrationLeavesTheRegistryUntouched()
        {
            _registry.Initialize();
            FakeScreenA screen = new() { FailInitialize = true };

            Assert.That(() => _registry.Register(screen), Throws.InvalidOperationException);

            Assert.That(_registry.Widgets, Is.Empty);
            Assert.That(_registry.TryGet<FakeScreenA>(out _), Is.False, "the lookup maps hold nothing either");
            Assert.That(_announced, Is.Empty);

            // No leaked lifecycle subscription: a self-driven transition on the rejected widget stays silent.
            screen.FailInitialize = false;
            screen.Initialize();
            Assert.That(_announced, Is.Empty);

            // And the failed attempt did not poison the type as a duplicate.
            screen.Terminate();
            _registry.Register(screen);
            Assert.That(_registry.Get<FakeScreenA>(), Is.SameAs(screen));
        }

        [Test]
        public void HandlersSeeTheWidgetFullyRegistered()
        {
            _registry.Initialize();
            FakeScreenA screen = new();
            int indexSeenByInitialized = -1;
            _registry.WidgetInitialized += widget => indexSeenByInitialized = _registry.IndexOf(widget);

            _registry.Register(screen);

            Assert.That(_announced, Is.EqualTo(new[] { "registered:FakeScreenA", "init:FakeScreenA" }),
                "Registered fires before Initialized, matching the Initialize() path");
            Assert.That(indexSeenByInitialized, Is.EqualTo(0), "the announced widget is already listed");
        }

        [Test]
        public void AFailedInitializeFailsFastAndUnwindsThroughTerminate()
        {
            FakeScreenA failing = new() { FailInitialize = true };
            FakeScreenB other = new();
            _registry.Register(failing);
            _registry.Register(other);

            Assert.That(() => _registry.Initialize(), Throws.InvalidOperationException);
            Assert.That(_registry.IsInitialized, Is.True, "the registry does not pretend the attempt never happened");
            Assert.That(other.State, Is.EqualTo(WidgetState.Uninitialized), "the sweep aborted fail-fast");

            _registry.Terminate();

            Assert.That(_registry.IsInitialized, Is.False);
            Assert.That(_registry.Widgets, Is.Empty, "Terminate unwinds the partial state cleanly");
        }

        [Test]
        public void TerminateCompletesTheSweepWhenAWidgetThrows()
        {
            FakeScreenA failing = new() { FailTerminate = true };
            FakeScreenB other = new();
            _registry.Register(failing);
            _registry.Register(other);
            _registry.Initialize();
            _announced.Clear();

            LogAssert.Expect(LogType.Exception, new Regex("FakeScreenA failed to terminate"));
            _registry.Terminate();

            Assert.That(other.State, Is.EqualTo(WidgetState.Terminated),
                "widgets after the failing one still come down");
            Assert.That(_registry.IsInitialized, Is.False);
            Assert.That(_registry.Widgets, Is.Empty);

            // The accepted cost of completing the sweep: the failing widget is forgotten while still live, and was
            // already announced as terminated, since Terminating fires before its teardown failed.
            Assert.That(failing.State, Is.EqualTo(WidgetState.Initialized));
            Assert.That(_announced, Does.Contain("term:FakeScreenA"));
        }

        // Unregister mirrors Register: teardown runs first, while the registry still answers lookups, so a widget
        // whose teardown throws is left registered rather than half-removed.
        [Test]
        public void AFailedUnregisterLeavesTheWidgetRegistered()
        {
            FakeScreenA failing = new() { FailTerminate = true };
            _registry.Register(failing);
            _registry.Initialize();

            Assert.That(() => _registry.Unregister(failing), Throws.InvalidOperationException);

            Assert.That(_registry.Widgets, Has.Member(failing));
            Assert.That(_registry.TryGet(out FakeScreenA found) && ReferenceEquals(found, failing), Is.True,
                "lookups still resolve the widget");
        }

        [Test]
        public void AHandlerUnregisteringTheSameWidgetDuringItsTeardownIsTolerated()
        {
            FakeScreenA a = new();
            _registry.Register(a);
            _registry.Initialize();
            int unregistered = 0;
            _registry.WidgetUnregistered += (_, _) => unregistered++;
            _registry.WidgetTerminated += widget =>
            {
                if (_registry.IndexOf(widget) >= 0)
                    _registry.Unregister(widget);
            };

            Assert.That(() => _registry.Unregister(a), Throws.Nothing);

            Assert.That(_registry.Widgets, Is.Empty);
            Assert.That(unregistered, Is.EqualTo(1), "removed exactly once");
        }

        [Test]
        public void AHandlerUnregisteringAnotherWidgetDuringClearDoesNotBreakIt()
        {
            FakeScreenA a = new();
            FakeScreenB b = new();
            FakeScreenC c = new();
            _registry.Register(a);
            _registry.Register(b);
            _registry.Register(c);
            _registry.Initialize();
            _registry.WidgetUnregistered += (widget, _) =>
            {
                if (widget == c && _registry.IndexOf(a) >= 0)
                    _registry.Unregister(a);
            };

            Assert.That(() => _registry.Clear(), Throws.Nothing);

            Assert.That(_registry.Widgets, Is.Empty);
            Assert.That(a.State, Is.EqualTo(WidgetState.Terminated));
            Assert.That(b.State, Is.EqualTo(WidgetState.Terminated));
            Assert.That(c.State, Is.EqualTo(WidgetState.Terminated));
        }

        [Test]
        public void AHandlerRegisteringDuringInitializeDoesNotBreakTheSweep()
        {
            FakeScreenA a = new();
            FakeScreenB b = new();
            FakeScreenC late = new();
            _registry.Register(a);
            _registry.Register(b);
            _registry.WidgetInitialized += widget =>
            {
                if (widget == a)
                    _registry.Register(late);
            };

            _registry.Initialize();

            Assert.That(a.State, Is.EqualTo(WidgetState.Initialized));
            Assert.That(b.State, Is.EqualTo(WidgetState.Initialized));
            Assert.That(late.State, Is.EqualTo(WidgetState.Initialized),
                "a widget registered mid-sweep is initialized by Register itself");
            Assert.That(_announced.FindAll(entry => entry == "init:FakeScreenC"), Has.Count.EqualTo(1));
        }

        [Test]
        public void AHandlerUnregisteringDuringTerminateDoesNotBreakTheSweep()
        {
            FakeScreenA a = new();
            FakeScreenB b = new();
            _registry.Register(a);
            _registry.Register(b);
            _registry.Initialize();
            _registry.WidgetTerminated += widget =>
            {
                if (widget == a && _registry.IndexOf(b) >= 0)
                    _registry.Unregister(b);
            };

            _registry.Terminate();

            Assert.That(a.State, Is.EqualTo(WidgetState.Terminated));
            Assert.That(b.State, Is.EqualTo(WidgetState.Terminated));
            Assert.That(_registry.Widgets, Is.Empty);
        }
    }
}
