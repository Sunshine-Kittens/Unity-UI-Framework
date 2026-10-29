using System;

using UIFramework.Animation;
using UIFramework.Core.Interfaces;

using UnityEngine;

namespace UIFramework.UGUI
{
    public class UguiGenericAnimation : GenericWidgetAnimation
    {
        private readonly RectTransform _displayRectTransform = null;
        private readonly RectTransform _rectTransform = null;
        private readonly CanvasGroup _canvasGroup = null;
        private readonly WidgetVisibility _visibility;

        private readonly Vector2 _activeAnchoredPosition = Vector2.zero;

        private Vector3 _offDisplayLeft = Vector3.zero;
        private Vector3 _offDisplayRight = Vector3.zero;
        private Vector3 _offDisplayBottom = Vector3.zero;
        private Vector3 _offDisplayTop = Vector3.zero;

        public UguiGenericAnimation(RectTransform displayRectTransform, RectTransform rectTransform, Vector3 activeAnchoredPosition, CanvasGroup canvasGroup, 
            GenericAnimation genericAnimation, WidgetVisibility visibility) : base(genericAnimation, visibility)
        {
            if (displayRectTransform == null)
            {
                throw new ArgumentNullException(nameof(displayRectTransform));
            }

            if (rectTransform == null)
            {
                throw new ArgumentNullException(nameof(rectTransform));
            }

            if (canvasGroup == null)
            {
                throw new ArgumentNullException(nameof(canvasGroup));
            }

            _displayRectTransform = displayRectTransform;
            _rectTransform = rectTransform;
            _canvasGroup = canvasGroup;

            _activeAnchoredPosition = activeAnchoredPosition;
        }

        public void Prepare()
        {
            switch (GenericAnimation)
            {
                case GenericAnimation.SlideFromLeft:
                    CalculateOffDisplayLeft();
                    break;
                case GenericAnimation.SlideFromRight:
                    CalculateOffDisplayRight();
                    break;
                case GenericAnimation.SlideFromBottom:
                    CalculateOffDisplayBottom();
                    break;
                case GenericAnimation.SlideFromTop:
                    CalculateOffDisplayTop();
                    break;
            }
        }
        
        protected override void Fade(float normalisedTime)
        {
            if (_canvasGroup != null) _canvasGroup.alpha = ResolveNormalisedTime(normalisedTime);
        }

        protected override void SlideFromLeft(float normalisedTime)
        {
            if (_rectTransform != null) _rectTransform.anchoredPosition = 
                Vector2.LerpUnclamped(_offDisplayLeft, _activeAnchoredPosition, ResolveNormalisedTime(normalisedTime));
        }

        protected override void SlideFromRight(float normalisedTime)
        {
            if (_rectTransform != null) _rectTransform.anchoredPosition = 
                Vector2.LerpUnclamped(_offDisplayRight, _activeAnchoredPosition, ResolveNormalisedTime(normalisedTime));
        }

        protected override void SlideFromBottom(float normalisedTime)
        {
            if (_rectTransform != null) _rectTransform.anchoredPosition = 
                Vector2.LerpUnclamped(_offDisplayBottom, _activeAnchoredPosition, ResolveNormalisedTime(normalisedTime));
        }

        protected override void SlideFromTop(float normalisedTime)
        {
            if (_rectTransform != null) _rectTransform.anchoredPosition = 
                Vector2.LerpUnclamped(_offDisplayTop, _activeAnchoredPosition, ResolveNormalisedTime(normalisedTime));
        }

        // Flip is dependant on the pivot point of the screens rect transform
        protected override void Flip(float normalisedTime)
        {
            if (_rectTransform != null)
            {
                Vector3 euler = _rectTransform.eulerAngles;
                euler.y = (1.0F - ResolveNormalisedTime(normalisedTime)) * 90.0F;
                _rectTransform.localRotation = Quaternion.Euler(euler);
            }
        }

        protected override void Expand(float normalisedTime)
        {
            if (_rectTransform != null)
            {
                Vector3 scale = Vector3.one * ResolveNormalisedTime(normalisedTime);
                _rectTransform.localScale = scale;
            }
        }

        protected override bool IsSupportedType(GenericAnimation type)
        {
            switch (type)
            {
                default:
                    return false;
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

        private void CalculateOffDisplayLeft()
        {
            if (_displayRectTransform != null && _rectTransform != null)
            {
                Vector3 displayWorldBottomLeftCorner = _displayRectTransform.GetWorldBottomLeftCorner();
                Vector3 displayLocalBottomLeftCorner = _rectTransform.InverseTransformPoint(displayWorldBottomLeftCorner);

                Vector3 localBottomRightCorner = _rectTransform.GetLocalBottomRightCorner();
                float distance = displayLocalBottomLeftCorner.x - localBottomRightCorner.x;
                _offDisplayLeft = new Vector2(_activeAnchoredPosition.x + distance, _activeAnchoredPosition.y);
            }
        }

        private void CalculateOffDisplayRight()
        {
            if (_displayRectTransform != null && _rectTransform != null)
            {
                Vector3 displayWorldBottomRightCorner = _displayRectTransform.GetWorldBottomRightCorner();
                Vector3 displayLocalBottomRightCorner = _rectTransform.InverseTransformPoint(displayWorldBottomRightCorner);

                Vector3 localBottomLeftCorner = _rectTransform.GetLocalBottomLeftCorner();
                float distance = displayLocalBottomRightCorner.x - localBottomLeftCorner.x;
                _offDisplayRight = new Vector2(_activeAnchoredPosition.x + distance, _activeAnchoredPosition.y);
            }
        }

        private void CalculateOffDisplayBottom()
        {
            if (_displayRectTransform != null && _rectTransform != null)
            {
                Vector3 displayWorldBottomLeftCorner = _displayRectTransform.GetWorldBottomLeftCorner();
                Vector3 displayLocalBottomLeftCorner = _rectTransform.InverseTransformPoint(displayWorldBottomLeftCorner);

                Vector3 localTopLeftCorner = _rectTransform.GetLocalTopLeftCorner();
                float distance = displayLocalBottomLeftCorner.y - localTopLeftCorner.y;
                _offDisplayBottom = new Vector2(_activeAnchoredPosition.x, _activeAnchoredPosition.y + distance);
            }
        }

        private void CalculateOffDisplayTop()
        {
            if (_displayRectTransform != null && _rectTransform != null)
            {
                Vector3 displayWorldTopLeftCorner = _displayRectTransform.GetWorldTopLeftCorner();
                Vector3 displayLocalTopLeftCorner = _rectTransform.InverseTransformPoint(displayWorldTopLeftCorner);

                Vector3 localBottomLeftCorner = _rectTransform.GetLocalBottomLeftCorner();
                float distance = displayLocalTopLeftCorner.y - localBottomLeftCorner.y;
                _offDisplayTop = new Vector2(_activeAnchoredPosition.x, _activeAnchoredPosition.y + distance);
            }
        }
    }

    internal abstract class WidgetAnimation : UguiGenericAnimation
    {
        protected WidgetAnimation(RectTransform displayRectTransform, RectTransform rectTransform, Vector3 activeAnchoredPosition, CanvasGroup canvasGroup,
            GenericAnimation genericAnimation, WidgetVisibility visibility) :
            base(displayRectTransform, rectTransform, activeAnchoredPosition, canvasGroup, genericAnimation, visibility)
        {
            
        }

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
        public ShowWidgetAnimation(RectTransform displayRectTransform, RectTransform rectTransform, Vector3 activeAnchoredPosition, CanvasGroup canvasGroup, 
            GenericAnimation genericAnimation) 
            : base(displayRectTransform, rectTransform, activeAnchoredPosition, canvasGroup, genericAnimation, WidgetVisibility.Visible) { }
    }

    internal sealed class HideWidgetAnimation : WidgetAnimation
    {
        public HideWidgetAnimation(RectTransform displayRectTransform, RectTransform rectTransform, Vector3 activeAnchoredPosition, CanvasGroup canvasGroup,
            GenericAnimation genericAnimation)
            : base(displayRectTransform, rectTransform, activeAnchoredPosition, canvasGroup, genericAnimation, WidgetVisibility.Hidden) { }
    }

    internal sealed class ShowWindowAnimation : UguiGenericAnimation
    {
        public ShowWindowAnimation(RectTransform displayRectTransform, RectTransform rectTransform, Vector3 activeAnchoredPosition, CanvasGroup canvasGroup, 
            GenericAnimation genericAnimation) 
            : base(displayRectTransform, rectTransform, activeAnchoredPosition, canvasGroup, genericAnimation, WidgetVisibility.Visible) { }
    }

    internal sealed class HideWindowAnimation : UguiGenericAnimation
    {
        public HideWindowAnimation(RectTransform displayRectTransform, RectTransform rectTransform, Vector3 activeAnchoredPosition, CanvasGroup canvasGroup, 
            GenericAnimation genericAnimation) 
            : base(displayRectTransform, rectTransform, activeAnchoredPosition, canvasGroup, genericAnimation, WidgetVisibility.Hidden) { }
    }
}