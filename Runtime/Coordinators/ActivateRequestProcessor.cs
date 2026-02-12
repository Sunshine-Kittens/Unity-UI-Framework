using System.Threading;

using UIFramework.Core.Interfaces;
using UIFramework.Navigation;
using UIFramework.Navigation.Interfaces;
using UIFramework.Transitioning;

using UnityEngine;
using UnityEngine.Extension;

namespace UIFramework.Coordinators
{
    public class ActivateRequestProcessor<TWindow> : IActivateRequestProcessor<TWindow> where TWindow : class, IWindow
    {
        private readonly TimeMode _timeMode;
        private readonly WindowActivator<TWindow> _windowActivator;
        private readonly TransitionManager _transitionManager;
        
        public ActivateRequestProcessor(TimeMode timeMode, WindowActivator<TWindow> windowActivator, TransitionManager transitionManager)
        {
            _timeMode = timeMode;
            _windowActivator = windowActivator;
            _transitionManager = transitionManager;
        }
        
        public ActivateResponse<TWindow> ProcessActivateRequest(in ActivateRequest<TWindow> request)
        {
            Awaitable awaitable = null;
            ActivateResult<TWindow> result = _windowActivator.Activate(request.Window);
            if (result.Success)
            {
                awaitable = Activate(request.Transition, result.Active, result.Previous, request.Data, request.CancellationToken);
            }
            return new ActivateResponse<TWindow>(result, awaitable);
        }
        
        private Awaitable Activate(VisibilityTransitionParams transition, TWindow target, TWindow source, object data, CancellationToken cancellationToken)
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
