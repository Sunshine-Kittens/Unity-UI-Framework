using System;

using UnityEngine;
using UnityEngine.Extension;

namespace UIFramework.UIToolkit
{
    public abstract class Window : Widget, IWindow
    {
        // IWindow
        [field: SerializeField] public string Identifier { get; } = string.Empty;
        [field: SerializeField] public bool SupportsHistory { get; } = true;

        public override IAnimation GetGenericAnimation(GenericAnimation genericAnimation, WidgetVisibility visibility)
        {
            switch (visibility)
            {
                case WidgetVisibility.Visible:
                    return new ShowWindowAnimation(VisualElement, genericAnimation);
                case WidgetVisibility.Hidden:
                    return new HideWindowAnimation(VisualElement, genericAnimation);
            }
            throw new InvalidOperationException("Widget visibility is unsupported.");
        }

        public virtual void SetWaiting(bool waiting) { }
    }
}