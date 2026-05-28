using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

namespace UIFramework
{
    internal static class UIExtensions
    {
        [ThreadStatic]
        private static Stack<VisualElement> _hierarchyStack;

        // Generic
        public static Color GetContrastingColor(in Color color)
        {
            float luminance = color.r * 0.299F + color.g * 0.587F + color.b * 0.114F;
            return luminance > 0.729F ? Color.black : Color.white;
        }

        // UI Toolkit
        public static EasingMode ToUnityEasingMode(this UnityEngine.Extension.EasingMode mode)
        {
            return (EasingMode)mode;
        }

        public static void IterateHierarchy(this VisualElement visualElement, Action<VisualElement> action)
        {
            if (_hierarchyStack == null)
                _hierarchyStack = new Stack<VisualElement>();

            _hierarchyStack.Push(visualElement);
            try
            {
                while (_hierarchyStack.Count > 0)
                {
                    VisualElement currentElement = _hierarchyStack.Pop();
                    for (int i = 0; i < currentElement.hierarchy.childCount; i++)
                    {
                        VisualElement child = currentElement.hierarchy.ElementAt(i);
                        _hierarchyStack.Push(child);
                        action.Invoke(child);
                    }
                }
            }
            finally
            {
                _hierarchyStack.Clear();
            }
        }

        public static void IterateHierarchy<TState>(this VisualElement visualElement, Action<VisualElement, TState> action, TState state)
        {
            if (_hierarchyStack == null)
                _hierarchyStack = new Stack<VisualElement>();

            _hierarchyStack.Push(visualElement);
            try
            {
                while (_hierarchyStack.Count > 0)
                {
                    VisualElement currentElement = _hierarchyStack.Pop();
                    for (int i = 0; i < currentElement.hierarchy.childCount; i++)
                    {
                        VisualElement child = currentElement.hierarchy.ElementAt(i);
                        _hierarchyStack.Push(child);
                        action.Invoke(child, state);
                    }
                }
            }
            finally
            {
                _hierarchyStack.Clear();
            }
        }

        // UGUI
        public static void FocusVertically(this ScrollRect instance, RectTransform target)
        {
            Vector2 viewportLocalPosition = instance.viewport.localPosition;
            Vector2 childLocalPosition = target.localPosition;
            instance.content.localPosition = new Vector2(
                instance.content.localPosition.x,
                0 - (viewportLocalPosition.y + childLocalPosition.y)
            );
        }

        public static T CreateOrGetFromCache<T>(in List<T> cache, in T template, in RectTransform parent) where T : UnityEngine.Object
        {
            T unityObject = null;
            if (cache.Count > 0)
            {
                unityObject = cache[cache.Count - 1];
                cache.RemoveAt(cache.Count - 1);
            }
            else
            {
                unityObject = UnityEngine.Object.Instantiate(template, parent);
            }
            return unityObject;
        }

        public static Vector3 GetWorldBottomLeftCorner(this RectTransform rectTransform)
        {
            Rect tmpRect = rectTransform.rect;
            Vector3 bottomLeft = new Vector3(tmpRect.x, tmpRect.y, 0.0F);

            Matrix4x4 mat = rectTransform.localToWorldMatrix;
            bottomLeft = mat.MultiplyPoint(bottomLeft);
            return bottomLeft;
        }

        public static Vector3 GetWorldTopLeftCorner(this RectTransform rectTransform)
        {
            Rect tmpRect = rectTransform.rect;
            Vector3 topLeft = new Vector3(tmpRect.x, tmpRect.yMax, 0.0F);

            Matrix4x4 mat = rectTransform.localToWorldMatrix;
            topLeft = mat.MultiplyPoint(topLeft);
            return topLeft;
        }

        public static Vector3 GetWorldTopRightCorner(this RectTransform rectTransform)
        {
            Rect tmpRect = rectTransform.rect;
            Vector3 topRight = new Vector3(tmpRect.xMax, tmpRect.yMax, 0.0F);

            Matrix4x4 mat = rectTransform.localToWorldMatrix;
            topRight = mat.MultiplyPoint(topRight);
            return topRight;
        }

        public static Vector3 GetWorldBottomRightCorner(this RectTransform rectTransform)
        {
            Rect tmpRect = rectTransform.rect;
            Vector3 bottomRight = new Vector3(tmpRect.xMax, tmpRect.y, 0.0F);

            Matrix4x4 mat = rectTransform.localToWorldMatrix;
            bottomRight = mat.MultiplyPoint(bottomRight);
            return bottomRight;
        }

        public static Vector3 GetLocalBottomLeftCorner(this RectTransform rectTransform)
        {
            Rect tmpRect = rectTransform.rect;
            return new Vector3(tmpRect.x, tmpRect.y, 0.0F);
        }

        public static Vector3 GetLocalTopLeftCorner(this RectTransform rectTransform)
        {
            Rect tmpRect = rectTransform.rect;
            return new Vector3(tmpRect.x, tmpRect.yMax, 0.0F);
        }

        public static Vector3 GetLocalTopRightCorner(this RectTransform rectTransform)
        {
            Rect tmpRect = rectTransform.rect;
            return new Vector3(tmpRect.xMax, tmpRect.yMax, 0.0F);
        }

        public static Vector3 GetLocalBottomRightCorner(this RectTransform rectTransform)
        {
            Rect tmpRect = rectTransform.rect;
            return new Vector3(tmpRect.xMax, tmpRect.y, 0.0F);
        }
    }
}