using UnityEngine;
using UnityEngine.UIElements;

namespace UIFramework.UIToolkit
{
    internal abstract class UitkGenericAnimation : GenericWidgetAnimation
    {
        private readonly VisualElement _visualElement = null;

        protected UitkGenericAnimation(VisualElement visualElement, GenericAnimation genericAnimation, WidgetVisibility visibility)
            : base(genericAnimation, visibility)
        {
            _visualElement = visualElement ?? throw new System.ArgumentNullException(nameof(visualElement));
        }

        protected override void Fade(float normalisedTime)
        {
            if (_visualElement != null)
                _visualElement.style.opacity = ResolveNormalisedTime(normalisedTime);
        }

        protected override void SlideFromLeft(float normalisedTime)
        {
            if (_visualElement != null)
            {
                float percent = (-1.0F + ResolveNormalisedTime(normalisedTime)) * 100.0F;
                _visualElement.style.translate = new Translate(UnityEngine.UIElements.Length.Percent(percent), UnityEngine.UIElements.Length.Percent(0.0F));
            }
        }

        protected override void SlideFromRight(float normalisedTime)
        {
            if (_visualElement != null)
            {
                float percent = (1.0F - ResolveNormalisedTime(normalisedTime)) * 100.0F;
                _visualElement.style.translate = new Translate(UnityEngine.UIElements.Length.Percent(percent), UnityEngine.UIElements.Length.Percent(0.0F));
            }
        }

        protected override void SlideFromBottom(float normalisedTime)
        {
            if (_visualElement != null)
            {
                float percent = (1.0F - ResolveNormalisedTime(normalisedTime)) * 100.0F;
                _visualElement.style.translate = new Translate(UnityEngine.UIElements.Length.Percent(0.0F), UnityEngine.UIElements.Length.Percent(percent));
            }
        }

        protected override void SlideFromTop(float normalisedTime)
        {
            if (_visualElement != null)
            {
                float percent = (-1.0F + ResolveNormalisedTime(normalisedTime)) * 100.0F;
                _visualElement.style.translate = new Translate(UnityEngine.UIElements.Length.Percent(0.0F), UnityEngine.UIElements.Length.Percent(percent));
            }
        }

        protected override void Flip(float normalisedTime)
        {
            if (_visualElement != null)
            {
                Fade(normalisedTime);
                float degrees = (1.0F - ResolveNormalisedTime(normalisedTime)) * 180.0F;
                _visualElement.style.rotate = new Rotate(degrees);
            }
        }

        protected override void Expand(float normalisedTime)
        {
            if (_visualElement != null)
            {
                float scaleComponent = ResolveNormalisedTime(normalisedTime);
                _visualElement.style.scale = new Scale(new Vector2(scaleComponent, scaleComponent));
            }
        }

        protected override bool IsSupportedType(GenericAnimation type)
        {
            switch (type)
            {
                default: return false;
                case GenericAnimation.Fade:
                case GenericAnimation.SlideFromLeft:
                case GenericAnimation.SlideFromRight:
                case GenericAnimation.SlideFromTop:
                case GenericAnimation.SlideFromBottom:
                case GenericAnimation.Flip:
                case GenericAnimation.Expand:
                    return true;
            }
        }
    }

    internal abstract class WidgetAnimation : UitkGenericAnimation
    {
        protected WidgetAnimation(VisualElement visualElement, GenericAnimation genericAnimation, WidgetVisibility visibility)
            : base(visualElement, genericAnimation, visibility) { }

        protected override void SlideFromLeft(float normalisedTime)
        {
            base.Fade(normalisedTime);
            base.SlideFromLeft(normalisedTime);
        }

        protected override void SlideFromRight(float normalisedTime)
        {
            base.Fade(normalisedTime);
            base.SlideFromRight(normalisedTime);
        }

        protected override void SlideFromBottom(float normalisedTime)
        {
            base.Fade(normalisedTime);
            base.SlideFromBottom(normalisedTime);
        }

        protected override void SlideFromTop(float normalisedTime)
        {
            base.Fade(normalisedTime);
            base.SlideFromTop(normalisedTime);
        }

        protected override void Flip(float normalisedTime)
        {
            base.Fade(normalisedTime);
            base.Flip(normalisedTime);
        }

        protected override void Expand(float normalisedTime)
        {
            base.Fade(normalisedTime);
            base.Expand(normalisedTime);
        }
    }

    internal sealed class ShowWidgetAnimation : WidgetAnimation
    {
        public ShowWidgetAnimation(VisualElement visualElement, GenericAnimation genericAnimation)
            : base(visualElement, genericAnimation, WidgetVisibility.Visible) { }
    }

    internal sealed class HideWidgetAnimation : WidgetAnimation
    {
        public HideWidgetAnimation(VisualElement visualElement, GenericAnimation genericAnimation)
            : base(visualElement, genericAnimation, WidgetVisibility.Hidden) { }
    }

    internal sealed class ShowWindowAnimation : UitkGenericAnimation
    {
        public ShowWindowAnimation(VisualElement visualElement, GenericAnimation genericAnimation)
            : base(visualElement, genericAnimation, WidgetVisibility.Visible) { }
    }

    internal sealed class HideWindowAnimation : UitkGenericAnimation
    {
        public HideWindowAnimation(VisualElement visualElement, GenericAnimation genericAnimation)
            : base(visualElement, genericAnimation, WidgetVisibility.Hidden) { }
    }
}