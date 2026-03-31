using System;

using UnityEngine.Extension;

namespace UIFramework
{
    public interface IReadOnlyWindow : IReadOnlyUIBehaviour, IReadOnlyAccessible
    {
        string Identifier { get; }
        
        bool IsVisible { get; }
        
        public float Opacity { get; }
        
        IReadOnlyScalarFlag IsHidden { get; }
        IReadOnlyScalarFlag IsEnabled { get; }
        IReadOnlyScalarFlag IsInteractable { get; }
        
        int SortOrder { get; }
    }

    /// <summary>
    /// Interface <c>IWindow</c> defines expected contract for all <c>UIFramework</c> windows.
    /// </summary>
    public interface IWindow : IReadOnlyWindow, IAccessible, IDataRecipient, IUIBehaviour
    {
        new IScalarFlag IsHidden { get; }
        new IScalarFlag IsEnabled { get; }
        new IScalarFlag IsInteractable { get; }
        
        new int SortOrder { get; set; }
        
        GenericWindowAnimation GetAnimation(GenericWindowAnimationType type);        

        bool SetWaiting(bool waiting);
        
        public void SetOpacity(float opacity);
    }
}