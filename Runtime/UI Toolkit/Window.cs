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

        public IAnimation GetGenericAnimation(GenericAnimation genericAnimation)
        {
            throw new NotImplementedException();
        }

        public virtual void SetWaiting(bool waiting) { }
    }
}