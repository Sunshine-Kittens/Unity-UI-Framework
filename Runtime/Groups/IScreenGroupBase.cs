using System;

using UIFramework.Core;
using UIFramework.Core.Interfaces;
using UIFramework.Navigation.Interfaces;

using UnityEngine.Extension;

namespace UIFramework.Groups
{
    public interface IScreenGroupBase : IScreenNavigator
    {
        public bool IsInitialized { get; }
        public InitializationState State { get; }

        public bool IsVisible { get; }
        public float Opacity { get; }

        public IScreen ActiveScreen { get; }
        public IScreen PreviousScreen { get; }

        public IScalarFlag IsEnabled { get; }
        public IScalarFlag IsInteractable { get; }

        public TimeMode TimeMode { get; }
        
        public event Action Entering;
        public event Action Entered;
        public event Action Exiting;
        public event Action Exited;

        public event ScreenAction ScreenShowing;
        public event ScreenAction ScreenShown;
        public event ScreenAction ScreenHiding;
        public event ScreenAction ScreenHidden;

        public void Initialize();
        public void Terminate();

        public void SetOpacity(float opacity);
    }
}
