using System.Collections;

using NUnit.Framework;

using UIFramework.Core.Interfaces;
using UIFramework.TestUtils;

using UnityEngine;
using UnityEngine.TestTools;

using Object = UnityEngine.Object;
using UitkWidget = UIFramework.UIToolkit.Widget;

namespace UIFramework.Tests.PlayMode
{
    // A UI Toolkit widget listens to its document source from the moment it awakes until it is destroyed. A widget
    // that was never initialized waits for its first explicit Initialize whatever the document does, and
    // destroying the document ends a live widget's lifecycle exactly once.
    public sealed class UitkDocumentSourceTests
    {
        private UitkDocumentFixture _fixture;

        [SetUp]
        public void SetUp() => _fixture = new UitkDocumentFixture();

        [UnityTearDown]
        public IEnumerator TearDown() => _fixture.Close();

        [Test]
        public void EnablingTheDocumentLeavesAFreshWidgetUninitialized()
        {
            // Subscribed ahead of the widget, so the rebuilt tree has the element before the widget hears of it.
            _fixture.DocumentSource.Enabled += () => _fixture.AddElement("widget");
            UitkWidget widget = _fixture.AddWidget<UitkWidget>("widget");
            int initialized = 0;
            widget.Initialized += _ => initialized++;
            GameObject documentObject = _fixture.Document.gameObject;

            documentObject.SetActive(false);
            documentObject.SetActive(true);

            Assert.That(_fixture.Element("widget"), Is.Not.Null, "arrange: the rebuilt tree has the widget's element");
            Assert.That(widget.State, Is.EqualTo(WidgetState.Uninitialized));
            Assert.That(initialized, Is.Zero);
        }

        [Test]
        public void DisablingTheDocumentLeavesAFreshWidgetAlone()
        {
            UitkWidget widget = _fixture.AddWidget<UitkWidget>("widget");

            _fixture.Document.gameObject.SetActive(false);

            LogAssert.NoUnexpectedReceived();
            Assert.That(widget.State, Is.EqualTo(WidgetState.Uninitialized));
        }

        [Test]
        public void DestroyingTheDocumentTerminatesItsWidgetOnce()
        {
            UitkWidget widget = _fixture.AddBoundWidget<UitkWidget>("widget");
            int terminated = 0;
            widget.Terminated += _ => terminated++;

            Object.DestroyImmediate(_fixture.Document.gameObject);

            LogAssert.NoUnexpectedReceived();
            Assert.That(widget.State, Is.EqualTo(WidgetState.Terminated));
            Assert.That(terminated, Is.EqualTo(1), "the document is disabled and then destroyed, and only one ends the widget");
        }

        [Test]
        public void ADestroyedUitkWidgetStopsListeningToItsDocument()
        {
            UitkWidget widget = _fixture.AddBoundWidget<UitkWidget>("widget");
            widget.Terminate();
            Object.DestroyImmediate(widget.gameObject);
            GameObject documentObject = _fixture.Document.gameObject;

            documentObject.SetActive(false);
            documentObject.SetActive(true);

            LogAssert.NoUnexpectedReceived();
        }
    }
}
