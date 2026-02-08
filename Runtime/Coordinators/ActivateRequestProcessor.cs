using System.Threading;

using UIFramework.Interfaces;
using UIFramework.Navigation;
using UIFramework.WidgetTransition;

using UnityEngine;
using UnityEngine.Extension;

namespace UIFramework.Coordinators
{
    public class ActivateRequestProcessor<TWidget> : IActivateRequestProcessor<TWidget> where TWidget : class, IWidget
    {
        private readonly TimeMode _timeMode;
        private readonly WidgetActivator<TWidget> _widgetActivator;
        private readonly TransitionManager _transitionManager;
        
        public ActivateRequestProcessor(TimeMode timeMode, WidgetActivator<TWidget> widgetActivator, TransitionManager transitionManager)
        {
            _timeMode = timeMode;
            _widgetActivator = widgetActivator;
            _transitionManager = transitionManager;
        }
        
        public ActivateResponse<TWidget> ProcessActivateRequest(in ActivateRequest<TWidget> request)
        {
            Awaitable awaitable = null;
            ActivateResult<TWidget> result = _widgetActivator.Activate(request.Widget);
            if (result.Success)
            {
                awaitable = Activate(request.Transition, result.Active, result.Previous, request.Data, request.CancellationToken);
            }
            return new ActivateResponse<TWidget>(result, awaitable);
        }
        
        private Awaitable Activate(VisibilityTransitionParams transition, TWidget target, TWidget source, object data, CancellationToken cancellationToken)
        {
            Awaitable awaitable = null;
            if (data != null) target.SetData(data);
            if (source != null)
            {
                if (transition.Length > 0.0F && (transition.EntryAnimationRef.IsValid || transition.ExitAnimationRef.IsValid))
                {
                    awaitable = _transitionManager.Transition(transition, source, target, cancellationToken);
                }
                else
                {
                    awaitable = _transitionManager.Transition(Transition.None(), source, target, cancellationToken);
                }
            }
            else
            {
                if (transition.Length > 0.0F && transition.EntryAnimationRef.IsValid)
                {
                    AnimationPlayable playable = transition.EntryAnimationRef.Resolve(target, WidgetVisibility.Visible)
                        .ToPlayable()
                        .WithLength(transition.Length)
                        .WithEasingMode(transition.EasingMode)
                        .WithTimeMode(_timeMode)
                        .Create();
                    awaitable = target.AnimateVisibility(WidgetVisibility.Visible, playable, InterruptBehavior.Immediate, cancellationToken);
                }
                else
                {
                    target.SetVisibility(WidgetVisibility.Visible);
                }
            }
            return awaitable;
        }
    }
}
