using System;

using UIFramework.Core;
using UIFramework.Core.Interfaces;
using UIFramework.Navigation.Interfaces;

using UnityEngine.Extension;

namespace UIFramework.Controllers.Interfaces
{
    public enum ControllerState
    {
        Uninitialized,
        Initialized,
        Terminated
    }
    
    public interface IController<TWidget> : IUpdatable, INavigationRequestFactory<TWidget>, IReturnNavigator<TWidget>, IExitNavigator<TWidget> 
        where TWidget : class, IWidget
    {
        public bool IsInitialized { get; }
        public ControllerState State { get; }
        
        public bool IsVisible { get; }
        public float Opacity { get; }

        public TWidget ActiveWidget { get; }
        public TWidget PreviousWidget { get; }
        
        public IScalarFlag IsEnabled { get; }
        public IScalarFlag IsInteractable { get; }

        public TimeMode TimeMode { get; }
        
        public IHistoryGroups HistoryGroups { get; }

        public event Action Entering;
        public event Action Entered;
        public event Action Exiting;
        public event Action Exited;
        
        public event WidgetAction WidgetShowing;
        public event WidgetAction WidgetShown;
        public event WidgetAction WidgetHiding;
        public event WidgetAction WidgetHidden;
        
        public void Initialize();
        public void Terminate();

        public void SetOpacity(float opacity);
    }
}
