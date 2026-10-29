using System.Collections;

using NUnit.Framework;

using UIFramework.Core.Interfaces;
using UIFramework.TestUtils;

using UnityEngine.TestTools;
using UnityEngine.UIElements;

using UitkWidget = UIFramework.UIToolkit.Widget;

namespace UIFramework.Tests.PlayMode
{
    // The lifecycle rule lives in WidgetBase, so the UI Toolkit backend fails a bad transition exactly as uGUI
    // does, before any of its own work: a rejected call leaves the widget and its element as it found them.
    public sealed class UitkWidgetLifecycleGuardTests
    {
        private UitkDocumentFixture _fixture;

        [SetUp]
        public void SetUp() => _fixture = new UitkDocumentFixture();

        [UnityTearDown]
        public IEnumerator TearDown() => _fixture.Close();

        [Test]
        public void InitializingAnInitializedUitkWidgetThrows()
        {
            UitkWidget widget = _fixture.AddBoundWidget<UitkWidget>("widget");

            Assert.That(widget.CanInitialize, Is.False);
            Assert.That(() => widget.Initialize(), Throws.InvalidOperationException);
        }

        [Test]
        public void TerminatingAUitkWidgetThatWasNeverInitializedThrows()
        {
            UitkWidget widget = _fixture.AddBoundWidget<UitkWidget>("widget", initialize: false);

            Assert.That(widget.CanTerminate, Is.False);
            Assert.That(() => widget.Terminate(), Throws.InvalidOperationException);
        }

        [Test]
        public void TerminatingAUitkWidgetTwiceThrows()
        {
            UitkWidget widget = _fixture.AddBoundWidget<UitkWidget>("widget");
            widget.Terminate();

            Assert.That(() => widget.Terminate(), Throws.InvalidOperationException);
        }

        // A guard placed after any of the work still throws, so the throw alone does not show that it runs first.
        [Test]
        public void ARejectedUitkInitializeLeavesTheWidgetUntouched()
        {
            UitkWidget widget = _fixture.AddBoundWidget<UitkWidget>("widget");
            VisualElement element = _fixture.Element("widget");
            widget.SetVisibility(WidgetVisibility.Visible);
            Assert.That(element.resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex), "arrange: the shown element displays");
            int initialized = 0;
            widget.Initialized += _ => initialized++;

            Assert.That(() => widget.Initialize(), Throws.InvalidOperationException);

            Assert.That(initialized, Is.Zero);
            Assert.That(widget.Visibility, Is.EqualTo(WidgetVisibility.Visible));
            Assert.That(widget.gameObject.activeSelf, Is.True, "a shown widget stays shown");
            Assert.That(element.resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex), "and so does its element");
        }

        [Test]
        public void ARejectedUitkTerminateRaisesNoTerminating()
        {
            UitkWidget widget = _fixture.AddBoundWidget<UitkWidget>("widget", initialize: false);
            int terminating = 0;
            widget.Terminating += _ => terminating++;

            Assert.That(() => widget.Terminate(), Throws.InvalidOperationException);
            Assert.That(terminating, Is.Zero, "a widget that was never initialized");

            widget.Initialize();
            widget.Terminate();
            Assert.That(() => widget.Terminate(), Throws.InvalidOperationException);

            Assert.That(terminating, Is.EqualTo(1), "only the accepted terminate is announced");
        }

        [Test]
        public void ATerminatedUitkWidgetCanInitializeAgain()
        {
            UitkWidget widget = _fixture.AddBoundWidget<UitkWidget>("widget");
            widget.Terminate();

            Assert.That(widget.CanInitialize, Is.True);
            Assert.That(widget.State, Is.EqualTo(WidgetState.Terminated));

            widget.Initialize();

            Assert.That(widget.State, Is.EqualTo(WidgetState.Initialized));
        }

        // Terminate drops the element and Initialize finds it again, so a later show reaches it.
        [Test]
        public void ReinitializingAUitkWidgetReacquiresItsElement()
        {
            UitkWidget widget = _fixture.AddBoundWidget<UitkWidget>("widget");
            VisualElement element = _fixture.Element("widget");
            widget.Terminate();

            widget.Initialize();
            widget.SetVisibility(WidgetVisibility.Visible);

            Assert.That(element.resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex));
        }
    }
}
