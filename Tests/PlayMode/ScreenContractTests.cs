using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;

using NUnit.Framework;

using UIFramework.Core.Interfaces;
using UIFramework.Groups;
using UIFramework.Navigation.Interfaces;
using UIFramework.Registry;
using UIFramework.TestUtils;

using UnityEngine.Extension;
using UnityEngine.TestTools;

using UitkScreen = UIFramework.UIToolkit.Screen;

namespace UIFramework.Tests.PlayMode
{
    // The edit-mode suite stands FakeScreen in for real screens. These cases pin where they agree: who owns
    // the navigator, the state Terminate leaves behind, and the parent link. Each case runs against every backend.
    public sealed class ScreenContractTests
    {
        public enum Backend
        {
            Fake,
            Ugui,
            Uitk
        }

        private UguiScreenFixture _fixture;
        private UitkDocumentFixture _uitk;

        // Built only by the cases that need a document.
        private UitkDocumentFixture Uitk => _uitk ??= new UitkDocumentFixture();

        [SetUp]
        public void SetUp()
        {
            _fixture = new UguiScreenFixture();
            _uitk = null;
        }

        // Each fixture closes even when the other fails; the first failure is rethrown afterwards.
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            List<Exception> faults = new();
            yield return CloseCollectingFaults(_fixture.Close(), faults);
            if (_uitk != null)
                yield return CloseCollectingFaults(_uitk.Close(), faults);

            if (faults.Count > 0)
                ExceptionDispatchInfo.Capture(faults[0]).Throw();
        }

        private static IEnumerator CloseCollectingFaults(IEnumerator close, List<Exception> faults)
        {
            while (true)
            {
                bool more;
                try
                {
                    more = close.MoveNext();
                }
                catch (Exception exception)
                {
                    faults.Add(exception);
                    more = false;
                }

                if (!more)
                    yield break;
                yield return close.Current;
            }
        }

        private IScreen CreateInitialized(Backend backend)
        {
            IScreen screen = backend switch
            {
                Backend.Fake => new FakeScreenA(),
                Backend.Ugui => _fixture.AddScreen<UguiScreenA>(),
                Backend.Uitk => Uitk.AddBoundWidget<UitkScreen>("screen", initialize: false),
                _ => throw new ArgumentOutOfRangeException(nameof(backend))
            };
            screen.Initialize();
            return screen;
        }

        private static IScreenNavigator CreateNavigator()
            => new ScreenGroup(new WidgetRegistry<IScreen>(), TimeMode.Scaled);

        [TestCase(Backend.Fake)]
        [TestCase(Backend.Ugui)]
        [TestCase(Backend.Uitk)]
        public void SettingANavigatorWhileOneIsSetThrows(Backend backend)
        {
            IScreen screen = CreateInitialized(backend);
            IScreenNavigator first = CreateNavigator();
            screen.SetNavigator(first);

            Assert.That(() => screen.SetNavigator(CreateNavigator()), Throws.InvalidOperationException
                .With.Message.EqualTo("Cannot set the navigator while it is already set."));
            Assert.That(screen.Navigator, Is.SameAs(first));
        }

        [TestCase(Backend.Fake)]
        [TestCase(Backend.Ugui)]
        [TestCase(Backend.Uitk)]
        public void ClearingAnUnsetNavigatorThrows(Backend backend)
        {
            IScreen screen = CreateInitialized(backend);

            Assert.That(() => screen.ClearNavigator(), Throws.InvalidOperationException
                .With.Message.EqualTo("Cannot clear the navigator while it is not set."));
        }

        [TestCase(Backend.Fake)]
        [TestCase(Backend.Ugui)]
        [TestCase(Backend.Uitk)]
        public void SettingANullNavigatorThrows(Backend backend)
        {
            IScreen screen = CreateInitialized(backend);

            Assert.That(() => screen.SetNavigator(null), Throws.TypeOf<ArgumentNullException>()
                .With.Property(nameof(ArgumentNullException.ParamName)).EqualTo("navigator"));
            Assert.That(screen.Navigator, Is.Null);
        }

        [TestCase(Backend.Fake)]
        [TestCase(Backend.Ugui)]
        [TestCase(Backend.Uitk)]
        public void AClearedNavigatorCanBeSetAgain(Backend backend)
        {
            IScreen screen = CreateInitialized(backend);
            screen.SetNavigator(CreateNavigator());
            screen.ClearNavigator();
            IScreenNavigator next = CreateNavigator();

            screen.SetNavigator(next);

            Assert.That(screen.Navigator, Is.SameAs(next));
        }

        [TestCase(Backend.Fake)]
        [TestCase(Backend.Ugui)]
        [TestCase(Backend.Uitk)]
        public void TerminatingResetsTheFlags(Backend backend)
        {
            IScreen screen = CreateInitialized(backend);
            screen.IsEnabled.Value = false;
            screen.IsInteractable.Value = false;

            screen.Terminate();

            Assert.That(screen.IsEnabled.Value, Is.True);
            Assert.That(screen.IsInteractable.Value, Is.True);
        }

        [TestCase(Backend.Fake)]
        [TestCase(Backend.Ugui)]
        [TestCase(Backend.Uitk)]
        public void TerminatingLeavesTheScreenHidden(Backend backend)
        {
            IScreen screen = CreateInitialized(backend);
            screen.SetVisibility(WidgetVisibility.Visible);

            screen.Terminate();

            Assert.That(screen.Visibility, Is.EqualTo(WidgetVisibility.Hidden));
        }

        [TestCase(Backend.Fake)]
        [TestCase(Backend.Ugui)]
        [TestCase(Backend.Uitk)]
        public void AChildReportsItsParent(Backend backend)
        {
            IWidget parent;
            IWidget child;
            if (backend == Backend.Fake)
            {
                FakeScreenA fakeParent = new();
                FakeScreenB fakeChild = new();
                fakeParent.AddChild(fakeChild);
                parent = fakeParent;
                child = fakeChild;
            }
            else if (backend == Backend.Ugui)
            {
                UguiScreenA uguiParent = _fixture.AddScreen<UguiScreenA>();
                child = _fixture.AddScreen<UguiScreenB>(uguiParent.transform);
                parent = uguiParent;
            }
            else
            {
                UitkScreen uitkParent = Uitk.AddWidget<UitkScreen>("parent");
                child = Uitk.AddWidget<UitkScreen>("child", uitkParent.transform);
                parent = uitkParent;
            }

            Assert.That(child.Parent, Is.SameAs(parent));
            Assert.That(parent.ChildCount, Is.EqualTo(1));
            Assert.That(parent.GetChildAt(0), Is.SameAs(child));
        }
    }
}
