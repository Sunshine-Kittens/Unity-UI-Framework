using System;

using UIFramework.Core;
using UIFramework.Core.Interfaces;
using UIFramework.Navigation.Interfaces;

using UnityEngine.Extension;

namespace UIFramework.Controllers.Interfaces
{
    public enum ScreenControllerState
    {
        Uninitialized,
        Initialized,
        Terminated
    }
    
    public interface IScreenController : IUpdatable, INavigationRequestFactory<IScreen>, IReturnNavigator<IScreen>, IExitNavigator<IScreen>
    {
        public bool IsInitialized { get; }
        public ScreenControllerState State { get; }
        
        public bool IsVisible { get; }
        public float Opacity { get; }

        public IScreen ActiveScreen { get; }
        public IScreen PreviousScreen { get; }
        
        public IScalarFlag IsEnabled { get; }
        public IScalarFlag IsInteractable { get; }

        public TimeMode TimeMode { get; }
        
        public IHistoryGroups HistoryGroups { get; }

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
