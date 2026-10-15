using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

namespace UIFramework.UGUI
{
    public class UGUIGenericWindowAnimation : GenericWindowAnimation
    {
        // Shared by every slide's Prepare, which runs on the main thread, so measuring allocates nothing.
        private static readonly List<Graphic> _graphics = new List<Graphic>();
        private static readonly Vector3[] _corners = new Vector3[4];

        private RectTransform _displayRectTransform = null;
        private RectTransform _rectTransform = null;
        private CanvasGroup _canvasGroup = null;

        private Vector2 _activeAnchoredPosition = Vector2.zero;

        private Vector3 _offDisplayLeft = Vector3.zero;
        private Vector3 _offDisplayRight = Vector3.zero;
        private Vector3 _offDisplayBottom = Vector3.zero;
        private Vector3 _offDisplayTop = Vector3.zero;

        public UGUIGenericWindowAnimation(RectTransform displayRectTransform, RectTransform rectTransform, Vector3 activeAnchoredPosition,
            CanvasGroup canvasGroup, GenericWindowAnimationType type) : base(type)
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

        public override void Prepare()
        {
            base.Prepare();

            switch (Type)
            {
                case GenericWindowAnimationType.SlideFromLeft:
                    CalculateOffDisplayLeft();
                    break;
                case GenericWindowAnimationType.SlideFromRight:
                    CalculateOffDisplayRight();
                    break;
                case GenericWindowAnimationType.SlideFromBottom:
                    CalculateOffDisplayBottom();
                    break;
                case GenericWindowAnimationType.SlideFromTop:
                    CalculateOffDisplayTop();
                    break;
            }
        }

        protected override void Fade(float normalisedTime)
        {
            if (_canvasGroup != null)
                _canvasGroup.alpha = normalisedTime;
        }

        protected override void SlideFromLeft(float normalisedTime)
        {
            if (_rectTransform != null)
                _rectTransform.anchoredPosition = Vector2.LerpUnclamped(_offDisplayLeft, _activeAnchoredPosition, normalisedTime);
        }

        protected override void SlideFromRight(float normalisedTime)
        {
            if (_rectTransform != null)
                _rectTransform.anchoredPosition = Vector2.LerpUnclamped(_offDisplayRight, _activeAnchoredPosition, normalisedTime);
        }

        protected override void SlideFromBottom(float normalisedTime)
        {
            if (_rectTransform != null)
                _rectTransform.anchoredPosition = Vector2.LerpUnclamped(_offDisplayBottom, _activeAnchoredPosition, normalisedTime);
        }

        protected override void SlideFromTop(float normalisedTime)
        {
            if (_rectTransform != null)
                _rectTransform.anchoredPosition = Vector2.LerpUnclamped(_offDisplayTop, _activeAnchoredPosition, normalisedTime);
        }

        // Flip is dependant on the pivot point of the screens rect transform
        protected override void Flip(float normalisedTime)
        {
            if (_rectTransform != null)
            {
                Vector3 euler = _rectTransform.eulerAngles;
                euler.y = (1.0F - normalisedTime) * 90.0F;
                _rectTransform.localRotation = Quaternion.Euler(euler);
            }
        }

        protected override void Expand(float normalisedTime)
        {
            if (_rectTransform != null)
            {
                Vector3 scale = Vector3.one * normalisedTime;
                _rectTransform.localScale = scale;
            }
        }

        protected override bool IsSupportedType(GenericWindowAnimationType type)
        {
            switch (type)
            {
                default: return false;
                case GenericWindowAnimationType.Fade:
                case GenericWindowAnimationType.SlideFromLeft:
                case GenericWindowAnimationType.SlideFromRight:
                case GenericWindowAnimationType.SlideFromTop:
                case GenericWindowAnimationType.SlideFromBottom:
                case GenericWindowAnimationType.Flip:
                case GenericWindowAnimationType.Expand:
                    return true;
            }
        }

        private void CalculateOffDisplayLeft()
        {
            if (_displayRectTransform != null && _rectTransform != null)
            {
                Vector3 displayWorldBottomLeftCorner = _displayRectTransform.GetWorldBottomLeftCorner();
                Vector3 displayLocalBottomLeftCorner = _rectTransform.InverseTransformPoint(displayWorldBottomLeftCorner);

                float distance = displayLocalBottomLeftCorner.x - GetLocalDrawnRect().xMax;
                _offDisplayLeft = new Vector2(_activeAnchoredPosition.x + distance, _activeAnchoredPosition.y);
            }
        }

        private void CalculateOffDisplayRight()
        {
            if (_displayRectTransform != null && _rectTransform != null)
            {
                Vector3 displayWorldBottomRightCorner = _displayRectTransform.GetWorldBottomRightCorner();
                Vector3 displayLocalBottomRightCorner = _rectTransform.InverseTransformPoint(displayWorldBottomRightCorner);

                float distance = displayLocalBottomRightCorner.x - GetLocalDrawnRect().xMin;
                _offDisplayRight = new Vector2(_activeAnchoredPosition.x + distance, _activeAnchoredPosition.y);
            }
        }

        private void CalculateOffDisplayBottom()
        {
            if (_displayRectTransform != null && _rectTransform != null)
            {
                Vector3 displayWorldBottomLeftCorner = _displayRectTransform.GetWorldBottomLeftCorner();
                Vector3 displayLocalBottomLeftCorner = _rectTransform.InverseTransformPoint(displayWorldBottomLeftCorner);

                float distance = displayLocalBottomLeftCorner.y - GetLocalDrawnRect().yMax;
                _offDisplayBottom = new Vector2(_activeAnchoredPosition.x, _activeAnchoredPosition.y + distance);
            }
        }

        private void CalculateOffDisplayTop()
        {
            if (_displayRectTransform != null && _rectTransform != null)
            {
                Vector3 displayWorldTopLeftCorner = _displayRectTransform.GetWorldTopLeftCorner();
                Vector3 displayLocalTopLeftCorner = _rectTransform.InverseTransformPoint(displayWorldTopLeftCorner);

                float distance = displayLocalTopLeftCorner.y - GetLocalDrawnRect().yMin;
                _offDisplayTop = new Vector2(_activeAnchoredPosition.x, _activeAnchoredPosition.y + distance);
            }
        }

        /// <summary>
        /// The window's rect grown to take in every graphic it shows, in its own space. A slide moves the window until all of
        /// that is off the display, so a child drawn past the window's edge doesn't linger on screen and then vanish.
        /// </summary>
        private Rect GetLocalDrawnRect()
        {
            Rect rect = _rectTransform.rect;
            float xMin = rect.xMin;
            float xMax = rect.xMax;
            float yMin = rect.yMin;
            float yMax = rect.yMax;

            _rectTransform.GetComponentsInChildren(true, _graphics);
            for (int i = 0; i < _graphics.Count; i++)
            {
                Graphic graphic = _graphics[i];
                if (!graphic.enabled || !TryGetShownArea(graphic.rectTransform, out RectTransform area))
                    continue;

                area.GetWorldCorners(_corners);
                for (int corner = 0; corner < _corners.Length; corner++)
                {
                    Vector3 local = _rectTransform.InverseTransformPoint(_corners[corner]);
                    xMin = Mathf.Min(xMin, local.x);
                    xMax = Mathf.Max(xMax, local.x);
                    yMin = Mathf.Min(yMin, local.y);
                    yMax = Mathf.Max(yMax, local.y);
                }
            }
            _graphics.Clear();

            return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
        }

        /// <summary>
        /// Where a graphic shows once the window does: nowhere if it or a parent below the window is inactive (the window
        /// itself opens inactive), else its own rect, or the rect of the nearest mask that clips it, so a long scroll list
        /// doesn't stretch the slide.
        /// </summary>
        private bool TryGetShownArea(RectTransform graphic, out RectTransform area)
        {
            area = graphic;
            bool clipped = false;
            for (Transform current = graphic; current != null && current != _rectTransform; current = current.parent)
            {
                if (!current.gameObject.activeSelf)
                    return false;

                if (!clipped && current != graphic && IsMask(current))
                {
                    area = (RectTransform)current;
                    clipped = true;
                }
            }
            return true;
        }

        private static bool IsMask(Transform transform)
        {
            if (transform.TryGetComponent(out RectMask2D rectMask) && rectMask.enabled)
                return true;

            return transform.TryGetComponent(out Mask mask) && mask.enabled;
        }
    }
}
