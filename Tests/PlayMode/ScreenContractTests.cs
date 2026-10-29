using System;
using System.Collections;

using NUnit.Framework;

using UIFramework.Core.Interfaces;
using UIFramework.Groups;
using UIFramework.Navigation.Interfaces;
using UIFramework.Registry;
using UIFramework.TestUtils;

using UnityEngine.Extension;
using UnityEngine.TestTools;

namespace UIFramework.Tests.PlayMode
{
    // The edit-mode suite stands FakeScreen in for real screens. These cases pin where the two agree: who owns
    // the navigator, the state Terminate leaves behind, and the parent link. Each case runs against both.
    public sealed class ScreenContractTests
    {
        public enum Backend
        {
            Fake,
            Ugui
        }

        private UguiScreenFixture _fixture;

        [SetUp]
        public void SetUp() => _fixture = new UguiScreenFixture();

        [UnityTearDown]
        public IEnumerator TearDown() => _fixture.Close();

        private IScreen CreateInitialized(Backend backend)
        {
            IScreen screen = backend == Backend.Fake ? new FakeScreenA() : _fixture.AddScreen<UguiScreenA>();
            screen.Initialize();
            return screen;
        }

        private static IScreenNavigator CreateNavigator()
            => new ScreenGroup(new WidgetRegistry<IScreen>(), TimeMode.Scaled);

        [TestCase(Backend.Fake)]
        [TestCase(Backend.Ugui)]
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
        public void ClearingAnUnsetNavigatorThrows(Backend backend)
        {
            IScreen screen = CreateInitialized(backend);

            Assert.That(() => screen.ClearNavigator(), Throws.InvalidOperationException
                .With.Message.EqualTo("Cannot clear the navigator while it is not set."));
        }

        [TestCase(Backend.Fake)]
        [TestCase(Backend.Ugui)]
        public void SettingANullNavigatorThrows(Backend backend)
        {
            IScreen screen = CreateInitialized(backend);

            Assert.That(() => screen.SetNavigator(null), Throws.TypeOf<ArgumentNullException>()
                .With.Property(nameof(ArgumentNullException.ParamName)).EqualTo("navigator"));
            Assert.That(screen.Navigator, Is.Null);
        }

        [TestCase(Backend.Fake)]
        [TestCase(Backend.Ugui)]
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
        public void TerminatingLeavesTheScreenHidden(Backend backend)
        {
            IScreen screen = CreateInitialized(backend);
            screen.SetVisibility(WidgetVisibility.Visible);

            screen.Terminate();

            Assert.That(screen.Visibility, Is.EqualTo(WidgetVisibility.Hidden));
        }

        [TestCase(Backend.Fake)]
        [TestCase(Backend.Ugui)]
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
            else
            {
                UguiScreenA uguiParent = _fixture.AddScreen<UguiScreenA>();
                child = _fixture.AddScreen<UguiScreenB>(uguiParent.transform);
                parent = uguiParent;
            }

            Assert.That(child.Parent, Is.SameAs(parent));
            Assert.That(parent.ChildCount, Is.EqualTo(1));
            Assert.That(parent.GetChildAt(0), Is.SameAs(child));
        }
    }
}
