using System;

using UIFramework.Animation;
using UIFramework.Core.Interfaces;

using UnityEngine;
using UnityEngine.Extension;

namespace UIFramework.UGUI
{
    public class Window : Widget, IWindow
    {
        // IWindow
        [field: SerializeField] public bool SupportsHistory { get; private set; } = true;

        public override IAnimation GetGenericAnimation(GenericAnimation genericAnimation, WidgetVisibility visibility)
        {
            Canvas canvas = GetComponentInParent<Canvas>(true);
            RectTransform canvasRectTransform = canvas.transform as RectTransform;
            switch (visibility)
            {
                case WidgetVisibility.Visible:
                    return new ShowWindowAnimation(canvasRectTransform, RectTransform, _activeAnchoredPosition, CanvasGroup, genericAnimation);
                case WidgetVisibility.Hidden:
                    return new HideWindowAnimation(canvasRectTransform, RectTransform, _activeAnchoredPosition, CanvasGroup, genericAnimation);
            }
            throw new InvalidOperationException("Widget visibility is unsupported.");
        }

        public virtual void SetWaiting(bool waiting) { }
    }
}