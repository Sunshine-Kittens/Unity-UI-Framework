using System.Collections;

using NUnit.Framework;

using UIFramework.Animation;
using UIFramework.Core.Interfaces;
using UIFramework.TestUtils;

using UnityEngine.Extension;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

using UitkWidget = UIFramework.UIToolkit.Widget;
using UitkWindow = UIFramework.UIToolkit.Window;

namespace UIFramework.Tests.PlayMode
{
    // A generic animation runs between an away pose and the resting pose: a show starts away and a hide ends
    // away. Slides move the element a whole element's width or height off its place, a flip turns it, an expand
    // shrinks it to nothing and a fade makes it transparent. On a plain widget every one of them also fades; on
    // a window only the fade and the flip do.
    //
    // Evaluated directly, so each pose is read the moment the animation writes it.
    public sealed class UitkGenericAnimationTests
    {
        private UitkDocumentFixture _fixture;

        [SetUp]
        public void SetUp() => _fixture = new UitkDocumentFixture();

        [UnityTearDown]
        public IEnumerator TearDown() => _fixture.Close();

        private VisualElement EvaluateAway<T>(GenericAnimation type, WidgetVisibility visibility) where T : UitkWidget
        {
            T widget = _fixture.AddBoundWidget<T>("widget");
            IAnimation animation = widget.GetGenericAnimation(type, visibility);
            Assert.That(animation, Is.Not.Null, "arrange: the widget resolves the animation");

            animation.Evaluate(AwayTime(visibility));
            return _fixture.Element("widget");
        }

        private static float AwayTime(WidgetVisibility visibility) => visibility == WidgetVisibility.Visible ? 0f : 1f;

        [Test]
        public void AUitkWidgetShowStartsAway([Values] GenericAnimation type)
        {
            VisualElement element = EvaluateAway<UitkWidget>(type, WidgetVisibility.Visible);

            AssertAway(element, type, fades: true);
        }

        [Test]
        public void AUitkWidgetHideEndsAway([Values] GenericAnimation type)
        {
            VisualElement element = EvaluateAway<UitkWidget>(type, WidgetVisibility.Hidden);

            AssertAway(element, type, fades: true);
        }

        [Test]
        public void AUitkWindowShowStartsAway([Values] GenericAnimation type)
        {
            VisualElement element = EvaluateAway<UitkWindow>(type, WidgetVisibility.Visible);

            AssertAway(element, type, fades: type == GenericAnimation.Fade || type == GenericAnimation.Flip);
        }

        [Test]
        public void AUitkWindowHideEndsAway([Values] GenericAnimation type)
        {
            VisualElement element = EvaluateAway<UitkWindow>(type, WidgetVisibility.Hidden);

            AssertAway(element, type, fades: type == GenericAnimation.Fade || type == GenericAnimation.Flip);
        }

        [Test]
        public void TheDefaultUitkAnimationIsAFade([Values] bool window,
            [Values(WidgetVisibility.Visible, WidgetVisibility.Hidden)] WidgetVisibility visibility)
        {
            UitkWidget widget = window
                ? _fixture.AddBoundWidget<UitkWindow>("widget")
                : _fixture.AddBoundWidget<UitkWidget>("widget");

            widget.GetDefaultAnimation(visibility).Evaluate(AwayTime(visibility));

            AssertAway(_fixture.Element("widget"), GenericAnimation.Fade, fades: true);
        }

        // Transforms are read as written. A percentage translate resolves against the element's size, which an
        // element laid out by nothing does not have. Opacity is read as resolved, which is what is displayed.
        private static void AssertAway(VisualElement element, GenericAnimation type, bool fades)
        {
            switch (type)
            {
                case GenericAnimation.SlideFromLeft:
                    AssertTranslatedBy(element, -100f, 0f);
                    break;
                case GenericAnimation.SlideFromRight:
                    AssertTranslatedBy(element, 100f, 0f);
                    break;
                case GenericAnimation.SlideFromBottom:
                    AssertTranslatedBy(element, 0f, 100f);
                    break;
                case GenericAnimation.SlideFromTop:
                    AssertTranslatedBy(element, 0f, -100f);
                    break;
                default:
                    Assert.That(element.style.translate.keyword, Is.EqualTo(StyleKeyword.Null), $"{type} does not move the element");
                    break;
            }

            if (type == GenericAnimation.Flip)
            {
                Assert.That(element.style.rotate.keyword, Is.Not.EqualTo(StyleKeyword.Null), "a flip turns the element away");
                Assert.That(element.style.rotate.value.angle.ToDegrees(), Is.Not.EqualTo(0f).Within(0.01f),
                    "a flip turns the element away");
            }
            else
            {
                Assert.That(element.style.rotate.keyword, Is.EqualTo(StyleKeyword.Null), $"{type} does not turn the element");
            }

            if (type == GenericAnimation.Expand)
            {
                Assert.That(element.style.scale.keyword, Is.Not.EqualTo(StyleKeyword.Null), "an expand shrinks the element away");
                Assert.That(element.style.scale.value.value.x, Is.EqualTo(0f).Within(0.001f), "an expand shrinks the element away");
                Assert.That(element.style.scale.value.value.y, Is.EqualTo(0f).Within(0.001f), "an expand shrinks the element away");
            }
            else
            {
                Assert.That(element.style.scale.keyword, Is.EqualTo(StyleKeyword.Null), $"{type} does not scale the element");
            }

            Assert.That(element.resolvedStyle.opacity, Is.EqualTo(fades ? 0f : 1f).Within(0.001f),
                fades ? $"{type} fades the element out" : $"{type} leaves opacity alone");
        }

        private static void AssertTranslatedBy(VisualElement element, float xPercent, float yPercent)
        {
            Assert.That(element.style.translate.keyword, Is.Not.EqualTo(StyleKeyword.Null), "a slide moves the element away");
            Translate translate = element.style.translate.value;
            Assert.That(translate.x.unit, Is.EqualTo(LengthUnit.Percent));
            Assert.That(translate.x.value, Is.EqualTo(xPercent).Within(0.01f), "horizontally, as a share of its width");
            Assert.That(translate.y.unit, Is.EqualTo(LengthUnit.Percent));
            Assert.That(translate.y.value, Is.EqualTo(yPercent).Within(0.01f), "vertically, as a share of its height");
        }
    }
}
