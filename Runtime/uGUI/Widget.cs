using System;

using UIFramework.UIToolkit;

using UnityEngine;
using UnityEngine.Extension;
using UnityEngine.UI;

namespace UIFramework.UGUI
{
    [RequireComponent(typeof(CanvasGroup), typeof(RectTransform))]
    public abstract class Widget : WidgetBase<Widget>
    {
        // WidgetBase
        public override int LocalSortOrder => RectTransform.GetSiblingIndex();

        public override int GlobalSortOrder => _rootUnderCanvas.GetSiblingIndex();
        
        public override int RenderSortOrder => _canvas != null ? _canvas.sortingOrder : 0;

        public override float Opacity => _canvasGroup.alpha;

        // uGUI Widget
        public RectTransform RectTransform
        {
            get
            {
                if (_rectTransform == null)
                {
                    _rectTransform = GetComponent<RectTransform>();
                }
                return _rectTransform;
            }
        }
        private RectTransform _rectTransform = null;

        public CanvasGroup CanvasGroup
        {
            get
            {
                if (_canvasGroup == null)
                {
                    _canvasGroup = GetComponent<CanvasGroup>();
                }
                return _canvasGroup;
            }
        }
        private CanvasGroup _canvasGroup = null;
        
        private Canvas _canvas = null;
        private Transform _rootUnderCanvas = null;
        protected Vector3 _activeAnchoredPosition { get; private set; } = Vector3.zero;

        // IWidget
        public sealed override void Initialize()
        {
            if (State == WidgetState.Initialized)
            {
                throw new InvalidOperationException("Widget already initialized.");
            }

            _canvas = GetComponentInParent<Canvas>();
            if (_canvas == null)
            {
                throw new InvalidOperationException("Unable to initialize uGUI Widget, no parent Canvas found.");
            }
            Transform rootUnderCanvas = RectTransform;
            while (rootUnderCanvas.parent != null && rootUnderCanvas.parent != _canvas.transform)
            {
                rootUnderCanvas =  rootUnderCanvas.parent;        
            }
            _rootUnderCanvas = rootUnderCanvas;
            _activeAnchoredPosition = RectTransform.anchoredPosition;
            base.Initialize();
            OnInitialize();
        }

        public override void Terminate()
        {
            if (State != WidgetState.Initialized)
            {
                throw new InvalidOperationException("Widget cannot be terminated.");
            }
            _canvas = null;
            _rootUnderCanvas = null;
            _activeAnchoredPosition = Vector3.zero;
            base.Terminate();
            OnTerminate();
        }

        public override IAnimation GetDefaultAnimation(WidgetVisibility visibility)
        {
            return GetGenericAnimation(GenericAnimation.Fade, visibility);
        }

        public override IAnimation GetGenericAnimation(GenericAnimation genericAnimation, WidgetVisibility visibility)
        {
            switch (visibility)
            {
                case WidgetVisibility.Visible:
                    return new HideWidgetAnimation(Visual)
            }
            throw new InvalidOperationException("Widget visibility is unsupported.");
        }

        public override void ResetAnimatedProperties() { }

        public sealed override void SetLocalSortOrder(int sortOrder)
        {
            RectTransform.SetSiblingIndex(sortOrder);
        }

        public sealed override void SetGlobalSortOrder(int sortOrder)
        {
            _rootUnderCanvas.SetSiblingIndex(sortOrder);
        }

        public sealed override void SetRenderSortOrder(int sortOrder)
        {
            _canvas.sortingOrder = sortOrder;
        }

        public sealed override void SetOpacity(float opacity)
        {
            _canvasGroup.alpha = opacity;
        }    

        // WidgetBase
        protected sealed override void SortAgainst(IWidget target, int direction)
        {
            if (target is Widget uguiWidget)
            {
                if (uguiWidget.RectTransform.parent == RectTransform.parent)
                {
                    SetLocalSortOrder(uguiWidget.LocalSortOrder + direction);
                }
                else if (uguiWidget._canvas == _canvas)
                {
                    SetGlobalSortOrder(uguiWidget.GlobalSortOrder + direction);
                }
                else
                {
                    SetRenderSortOrder(uguiWidget.RenderSortOrder + direction);
                }
            }
            else
            {
                SetRenderSortOrder(target.RenderSortOrder + direction);
            }
        }
        
        protected sealed override void OnIsEnabledUpdated(bool value)
        {
            if (value)
            {
                IsInteractableInternal.SetOverrideValue(true);
                CanvasGroup.interactable = true;
            }
            else
            {
                IsInteractableInternal.SetOverrideValue(false);
                CanvasGroup.interactable = false;
            }
        }

        protected sealed override void OnIsInteractableUpdated(bool value)
        {
            if (value)
            {
                CanvasGroup.blocksRaycasts = true;
            }
            else
            {
                CanvasGroup.blocksRaycasts = false;
            }
        }
        
        protected sealed override void SetActive(bool active)
        {
            gameObject.SetActive(active);
        }

        // uGUI Widget
        protected void RebuildLayout(RectTransform target = null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(target != null ? target : RectTransform);
        }
    }
}