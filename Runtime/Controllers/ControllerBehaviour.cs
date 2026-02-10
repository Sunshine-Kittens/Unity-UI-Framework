using System;
using System.Threading;

using UIFramework.Controllers.Interfaces;
using UIFramework.Core;
using UIFramework.Core.Interfaces;
using UIFramework.Navigation;

using UnityEngine;
using UnityEngine.Extension;

namespace UIFramework.Controllers
{
    public abstract class ControllerBehaviour<TWidget> : MonoBehaviour, IController<TWidget>where TWidget : class, IWidget
    {
        public bool Active { get; }
        public void ManagedUpdate()
        {
            throw new NotImplementedException();
        }
        public NavigationRequest<TWidget> CreateNavigationRequest(TWidget widget)
        {
            throw new NotImplementedException();
        }
        public NavigationRequest<TWidget> CreateNavigationRequest<TTarget>() where TTarget : class, TWidget
        {
            throw new NotImplementedException();
        }
        public NavigationResponse<TWidget> Return(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
        public NavigationResponse<TWidget> Exit(in ExitRequest request)
        {
            throw new NotImplementedException();
        }
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
        public void Initialize()
        {
            throw new NotImplementedException();
        }
        public void Terminate()
        {
            throw new NotImplementedException();
        }
        public void SetOpacity(float opacity)
        {
            throw new NotImplementedException();
        }
    }
}
