using System.Threading;

using UIFramework.Interfaces;
using UIFramework.Navigation;
using UIFramework.WidgetTransition;

using UnityEngine;
using UnityEngine.Extension;

namespace UIFramework.Coordinators
{
    public class ExitCoordinator<TWidget> : IExitRequestFactory<TWidget>, IExitRequestProcessor<TWidget> where TWidget : class, IWidget
    {
        private readonly TimeMode _timeMode;
        private readonly NavigationManager<TWidget> _navigationManager;
        private readonly TransitionManager _transitionManager;
        
        public ExitCoordinator(TimeMode timeMode, NavigationManager<TWidget> navigationManager, TransitionManager transitionManager)
        {
            _timeMode = timeMode;
            _transitionManager = transitionManager;
            _navigationManager = navigationManager;
        }
        
        public ExitRequest<TWidget> CreateExitRequest()
        {
            return new ExitRequest<TWidget>(this, _navigationManager.Version, this, _navigationManager.Active);
        }
        
        public bool IsRequestValid(in  ExitRequest<TWidget> request)
        {
            return request.NavigationVersion == _navigationManager.Version;
        }
        
        public ExitResponse ProcessExitRequest(in ExitRequest<TWidget> request)
        {
            NavigationResult<TWidget> result = _navigationManager.Clear();
            Awaitable awaitable = null;
            if (result.Success)
            {
                awaitable = Exit(result.Previous, request.GetAnimation(_timeMode), request.CancellationToken); 
            }
            return new ExitResponse(result.Success, awaitable);        
        }
        
        private Awaitable Exit(TWidget widget, in AnimationPlayable playable, CancellationToken cancellationToken)
        {
            Awaitable awaitable = null;
            _transitionManager.Terminate();
            if (playable.IsValid())
            {
                awaitable = widget.AnimateVisibility(WidgetVisibility.Visible, playable, InterruptBehavior.Immediate, cancellationToken);
            }
            else
            {
                widget.SetVisibility(WidgetVisibility.Hidden);
            }
            return awaitable;
        }
    }
}
