using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Extension;
using UnityEngine.UIElements;

namespace UIFramework.UIToolkit
{
    public class Widget : WidgetBase<Widget>
    {
        // UI Toolkit Widget
        private const string _NonInteractiveClassName = ".non-interactive";

        // WidgetBase
        public override int LocalSortOrder => _visualElement.parent != null ? _visualElement.parent.hierarchy.IndexOf(_visualElement) : 0;
        public override int GlobalSortOrder => Mathf.CeilToInt(_documentSource.Document.sortingOrder);
        public override int RenderSortOrder => Mathf.CeilToInt(_documentSource.Document.panelSettings.sortingOrder);

        public override float Opacity => _visualElement.style.opacity.value;

        // UI Toolkit Widget
        [SerializeField] private WidgetDocumentSource _documentSource;
        [SerializeField] private string _visualElementName = string.Empty;
        
        protected UIDocument UIDocument => _documentSource.Document;
        protected VisualElement VisualElement => _visualElement;
        private VisualElement _visualElement;

        // IWidget
        public sealed override void Initialize()
        {
            if (State == WidgetState.Initialized)
            {
                throw new InvalidOperationException("Widget already initialized.");
            }

            if (!_documentSource.gameObject.activeSelf)
            {
                Debug.Log("UI Document and all parents enabled by Widget Initialization.", _documentSource);
                Transform target = _documentSource.gameObject.transform;
                while (target != null)
                {
                    target.gameObject.SetActive(true);
                    target = target.parent;
                }
            }

            if (!_documentSource.Document.gameObject.activeInHierarchy)
            {
                throw new InvalidOperationException("Unabled to initialize UI Toolkit Widget for disabled UI document.");
            }

            int documentSortOrder = Mathf.CeilToInt(_documentSource.Document.sortingOrder);
            if (!Mathf.Approximately(documentSortOrder, _documentSource.Document.sortingOrder))
            {
                _documentSource.Document.sortingOrder = documentSortOrder;
            }

            int panelSortOrder = Mathf.CeilToInt(_documentSource.Document.panelSettings.sortingOrder);
            if (!Mathf.Approximately(panelSortOrder, _documentSource.Document.panelSettings.sortingOrder))
            {
                _documentSource.Document.panelSettings.sortingOrder = panelSortOrder;
            }

            VisualElement visualElement = _documentSource.Document.rootVisualElement.Q(_visualElementName);
            _visualElement = visualElement ?? throw new InvalidOperationException($"Failed to find visual element with name: {_visualElementName}");

            base.Initialize();
            OnInitialize();
        }

        public sealed override void Terminate()
        {
            if (State != WidgetState.Initialized)
            {
                throw new InvalidOperationException("Widget cannot be terminated.");
            }
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
                    return new ShowWidgetAnimation(VisualElement, genericAnimation);
                case WidgetVisibility.Hidden:
                    return new HideWidgetAnimation(VisualElement, genericAnimation);
            }
            throw new InvalidOperationException("Widget visibility is unsupported.");
        }

        public override void ResetAnimatedProperties() { }

        public sealed override void SetLocalSortOrder(int sortOrder)
        {
            if (_visualElement.parent != null)
            {
                VisualElement.Hierarchy parentHierarchy = _visualElement.parent.hierarchy;
                int newIndexInHierarchy = Mathf.Clamp(sortOrder, 0, parentHierarchy.childCount - 1);
                int existingIndexInHierarchy = parentHierarchy.IndexOf(_visualElement);
                if (newIndexInHierarchy != existingIndexInHierarchy)
                {
                    _visualElement.PlaceInFront(parentHierarchy.ElementAt(newIndexInHierarchy));
                }
            }
        }

        public sealed override void SetGlobalSortOrder(int sortOrder)
        {
            _documentSource.Document.sortingOrder = sortOrder;
        }

        public sealed override void SetRenderSortOrder(int sortOrder)
        {
            _documentSource.Document.panelSettings.sortingOrder = sortOrder;
        }

        public sealed override void SetOpacity(float opacity)
        {
            _visualElement.style.opacity = opacity;
        }

        // WidgetBase
        protected sealed override void SortAgainst(IWidget target, int direction)
        {
            if (target is Widget uitkWidget)
            {
                if (uitkWidget._documentSource.Document == _documentSource.Document)
                {
                    SetLocalSortOrder(uitkWidget.LocalSortOrder + direction);
                }
                else if (uitkWidget._documentSource.Document.panelSettings == _documentSource.Document.panelSettings)
                {
                    SetGlobalSortOrder(uitkWidget.GlobalSortOrder + direction);
                }
                else
                {
                    SetRenderSortOrder(uitkWidget.RenderSortOrder + direction);
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
                _visualElement.SetEnabled(true);
            }
            else
            {
                IsInteractableInternal.SetOverrideValue(false);
                _visualElement.SetEnabled(false);
            }
        }

        protected sealed override void OnIsInteractableUpdated(bool value)
        {
            if (value)
            {
                SetInteractable(_visualElement, true);
            }
            else
            {
                SetInteractable(_visualElement, false);
            }
        }
        
        protected sealed override void SetActive(bool active)
        {
            gameObject.SetActive(active);
            if (_visualElement != null)
            {
                _visualElement.style.display = active ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }
        
        // Unity Message
        protected override void Awake()
        {
            base.Awake();
            _documentSource.Enabled += DocumentSourceEnabled;
            _documentSource.Disabled += DocumentSourceDisabled;
            _documentSource.Destroyed += DocumentSourceDestroyed;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (_documentSource != null)
            {
                _documentSource.Enabled -= DocumentSourceEnabled;
                _documentSource.Disabled -= DocumentSourceDisabled;
                _documentSource.Destroyed -= DocumentSourceDestroyed;
            }
        }

        // UI Toolkit Widget
        private void DocumentSourceEnabled()
        {
            if (State == WidgetState.Terminated)
            {
                Initialize();
            }
        }

        private void DocumentSourceDisabled()
        {
            Terminate();
        }

        private void DocumentSourceDestroyed()
        {
            Terminate();
        }
        
        protected static void SetInteractable(VisualElement visualElement, bool interactable)
        {
            PickingMode pickingMode = interactable ? PickingMode.Position : PickingMode.Ignore;
            SetInteractablePickingMode(visualElement, pickingMode);

            void SetInteractableChild(VisualElement child)
            {
                SetInteractablePickingMode(child, pickingMode);
            }

            visualElement.IterateHierarchy(SetInteractableChild);
        }
        
        private static void SetInteractablePickingMode(VisualElement visualElement, PickingMode pickingMode)
        {
            if (pickingMode == PickingMode.Position)
            {
                bool isNonInteractive = false;
                IEnumerable<string> classes = visualElement.GetClasses();
                foreach (string @class in classes)
                {
                    if (string.Equals(@class, _NonInteractiveClassName, StringComparison.OrdinalIgnoreCase))
                    {
                        isNonInteractive = true;
                        break;
                    }
                }

                if (isNonInteractive)
                {
                    visualElement.pickingMode = PickingMode.Position;
                    visualElement.RemoveFromClassList(_NonInteractiveClassName);
                }
            }
            else if (visualElement.pickingMode == PickingMode.Position)
            {
                visualElement.pickingMode = PickingMode.Ignore;
                visualElement.AddToClassList(_NonInteractiveClassName);
            }
        }
    }
    
    public abstract class WidgetT<TVisualElement> : Widget where  TVisualElement : VisualElement
    {
        // UI Toolkit WidgetT
        protected new TVisualElement VisualElement => base.VisualElement as TVisualElement;
    }
}