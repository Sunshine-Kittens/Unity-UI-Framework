using System.Collections;

using NUnit.Framework;

using UIFramework.Animation;
using UIFramework.Core.Interfaces;
using UIFramework.TestUtils;

using UnityEngine;
using UnityEngine.Extension;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

using UitkScreen = UIFramework.UIToolkit.Screen;
using UitkWidget = UIFramework.UIToolkit.Widget;
using UitkWindow = UIFramework.UIToolkit.Window;

namespace UIFramework.Tests.PlayMode
{
    // A UI Toolkit widget drives one element of its document, found by name at initialize: display follows
    // visibility, and the enabled and interactable flags reach the element and everything under it.
    public sealed class UitkWidgetSmokeTests
    {
        private UitkDocumentFixture _fixture;

        [SetUp]
        public void SetUp() => _fixture = new UitkDocumentFixture();

        [UnityTearDown]
        public IEnumerator TearDown() => _fixture.Close();

        private sealed class BoxWidget : UIFramework.UIToolkit.WidgetT<Box>
        {
            public Box Box => VisualElement;
        }

        [Test]
        public void TheFixtureDocumentIsOnAPanelInTheSameFrame()
        {
            Assert.That(_fixture.RootElement, Is.Not.Null);
            Assert.That(_fixture.RootElement.panel, Is.Not.Null);
            Assert.That(_fixture.Document.panelSettings, Is.SameAs(_fixture.PanelSettings));
        }

        [Test]
        public void InitializeBindsTheNamedElementAndHidesIt()
        {
            VisualElement outer = _fixture.AddElement("outer");
            VisualElement inner = _fixture.AddElement("inner", outer);
            VisualElement sibling = _fixture.AddElement("sibling");
            UitkWidget widget = _fixture.AddWidget<UitkWidget>("inner");

            widget.Initialize();

            Assert.That(inner.resolvedStyle.display, Is.EqualTo(DisplayStyle.None), "a nested element is found by name");
            Assert.That(widget.gameObject.activeSelf, Is.False);

            widget.SetVisibility(WidgetVisibility.Visible);

            Assert.That(inner.resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex));
            Assert.That(widget.gameObject.activeSelf, Is.True);
            Assert.That(outer.style.display.keyword, Is.EqualTo(StyleKeyword.Null), "only the named element is driven");
            Assert.That(sibling.style.display.keyword, Is.EqualTo(StyleKeyword.Null));

            widget.SetVisibility(WidgetVisibility.Hidden);

            Assert.That(inner.resolvedStyle.display, Is.EqualTo(DisplayStyle.None));
            Assert.That(widget.gameObject.activeSelf, Is.False);
            Assert.That(widget.State, Is.EqualTo(WidgetState.Initialized),
                "hiding deactivates the widget's own GameObject, not the document's");
        }

        [Test]
        public void AMissingElementFailsInitializeWithoutTouchingTheWidget()
        {
            UitkWidget widget = _fixture.AddWidget<UitkWidget>("absent");
            int initialized = 0;
            widget.Initialized += _ => initialized++;

            Assert.That(() => widget.Initialize(), Throws.InvalidOperationException);

            Assert.That(widget.State, Is.EqualTo(WidgetState.Uninitialized));
            Assert.That(initialized, Is.Zero);

            VisualElement element = _fixture.AddElement("absent");
            widget.Initialize();

            Assert.That(widget.State, Is.EqualTo(WidgetState.Initialized), "the widget can try again once the element exists");
            Assert.That(initialized, Is.EqualTo(1));
            Assert.That(element.resolvedStyle.display, Is.EqualTo(DisplayStyle.None));
        }

        [Test]
        public void DisablingAUitkWidgetDisablesItsElementTree()
        {
            VisualElement element = _fixture.AddElement("widget");
            VisualElement child = _fixture.AddElement("child", element);
            VisualElement grandchild = _fixture.AddElement("grandchild", child);
            VisualElement authored = _fixture.AddElement("authored", element);
            authored.pickingMode = PickingMode.Ignore;
            UitkWidget widget = _fixture.AddWidget<UitkWidget>("widget");
            widget.Initialize();
            Assert.That(element.enabledSelf, Is.True, "arrange: an initialized widget's element is enabled");
            Assert.That(grandchild.pickingMode, Is.EqualTo(PickingMode.Position), "arrange: and takes the pointer");

            widget.IsEnabled.Value = false;

            Assert.That(element.enabledSelf, Is.False);
            Assert.That(element.pickingMode, Is.EqualTo(PickingMode.Ignore));
            Assert.That(child.pickingMode, Is.EqualTo(PickingMode.Ignore), "the whole tree under the element stops taking the pointer");
            Assert.That(grandchild.pickingMode, Is.EqualTo(PickingMode.Ignore));

            widget.IsEnabled.Value = true;

            Assert.That(element.enabledSelf, Is.True);
            Assert.That(element.pickingMode, Is.EqualTo(PickingMode.Position));
            Assert.That(child.pickingMode, Is.EqualTo(PickingMode.Position));
            Assert.That(grandchild.pickingMode, Is.EqualTo(PickingMode.Position));
            Assert.That(authored.pickingMode, Is.EqualTo(PickingMode.Ignore),
                "an element authored to ignore the pointer keeps ignoring it");
        }

        [UnityTest]
        public IEnumerator AnAnimatingUitkWidgetIgnoresThePointer()
        {
            UitkWidget widget = _fixture.AddBoundWidget<UitkWidget>("widget");
            VisualElement element = _fixture.Element("widget");
            IAnimation fade = widget.GetGenericAnimation(GenericAnimation.Fade, WidgetVisibility.Visible);
            AwaitableProbe probe = _fixture.Watch(widget.AnimateVisibility(WidgetVisibility.Visible)
                .WithAnimation(fade)
                .WithLength(0.1f)
                .Animate());
            Assert.That(widget.IsAnimating, Is.True, "arrange: the fade is running");

            Assert.That(element.pickingMode, Is.EqualTo(PickingMode.Ignore));

            yield return probe.WaitForCompletion();

            Assert.That(probe.Exception, Is.Null);
            Assert.That(widget.IsAnimating, Is.False);
            Assert.That(element.pickingMode, Is.EqualTo(PickingMode.Position), "the pointer is given back when it ends");
            Assert.That(_fixture.PooledHandles, Is.EqualTo(1), "and the handle goes back to the UI Toolkit pool");
        }

        [Test]
        public void AUitkWindowAndScreenShowAndHideTheirElements()
        {
            AssertShowsAndHides(_fixture.AddBoundWidget<UitkWindow>("window"), _fixture.Element("window"));
            AssertShowsAndHides(_fixture.AddBoundWidget<UitkScreen>("screen"), _fixture.Element("screen"));
        }

        private static void AssertShowsAndHides(UitkWidget widget, VisualElement element)
        {
            string type = widget.GetType().Name;
            Assert.That(element.resolvedStyle.display, Is.EqualTo(DisplayStyle.None), type);

            widget.SetVisibility(WidgetVisibility.Visible);
            Assert.That(element.resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex), type);

            widget.SetVisibility(WidgetVisibility.Hidden);
            Assert.That(element.resolvedStyle.display, Is.EqualTo(DisplayStyle.None), type);

            widget.SetVisibility(WidgetVisibility.Visible);
            widget.Terminate();
            Assert.That(widget.State, Is.EqualTo(WidgetState.Terminated), type);
            Assert.That(element.resolvedStyle.display, Is.EqualTo(DisplayStyle.None), $"{type} is hidden by teardown");
        }

        [Test]
        public void ResettingAnUnboundUitkWidgetDoesNothing()
        {
            UitkWidget fresh = _fixture.AddWidget<UitkWidget>("absent");
            UitkWidget terminated = _fixture.AddBoundWidget<UitkWidget>("widget");
            VisualElement element = _fixture.Element("widget");
            terminated.Terminate();
            element.style.translate = new Translate(40f, -25f);
            StyleTranslate displaced = element.style.translate;

            Assert.That(() => fresh.ResetAnimatedProperties(), Throws.Nothing, "before its element is found");
            Assert.That(() => terminated.ResetAnimatedProperties(), Throws.Nothing, "after its element is dropped");
            Assert.That(element.style.translate, Is.EqualTo(displaced), "a terminated widget leaves its old element alone");
        }

        [Test]
        public void ATypedUitkWidgetSeesItsElementAsThatType()
        {
            Box box = _fixture.AddElement<Box>("box");
            BoxWidget widget = _fixture.AddWidget<BoxWidget>("box");

            widget.Initialize();

            Assert.That(widget.Box, Is.SameAs(box));
        }

        [UnityTest]
        public IEnumerator ClosingTheUitkFixtureTerminatesItsWidgetsThenDestroysItsObjects()
        {
            // Subscribed ahead of the widget, so the document's own teardown is seen before the widget hears of it.
            bool documentDisabled = false;
            _fixture.DocumentSource.Disabled += () => documentDisabled = true;
            UitkWidget widget = _fixture.AddBoundWidget<UitkWidget>("widget");
            widget.SetVisibility(WidgetVisibility.Visible);
            VisualElement element = _fixture.Element("widget");
            bool terminatedWithTheTreeUp = false;
            widget.Terminated += _ => terminatedWithTheTreeUp = !documentDisabled && element.panel != null;
            GameObject root = _fixture.Root;
            PanelSettings settings = _fixture.PanelSettings;

            yield return _fixture.Close();

            Assert.That(widget.State, Is.EqualTo(WidgetState.Terminated));
            Assert.That(terminatedWithTheTreeUp, Is.True, "a live widget is terminated before the document goes");
            Assert.That(root == null, Is.True, "the document and widgets are destroyed");
            Assert.That(settings == null, Is.True, "and so are the panel settings");
            Assert.That(element.panel, Is.Null, "the tree is off the panel");
        }
    }
}
