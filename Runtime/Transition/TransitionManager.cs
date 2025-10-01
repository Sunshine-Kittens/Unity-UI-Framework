using System;
using System.Collections.Generic;
using System.Threading;

using UnityEngine;
using UnityEngine.Extension;
using UnityEngine.Extension.Awaitable;

namespace UIFramework
{
    public class TransitionManager
    {
        private readonly struct Params : IEquatable<Params>
        {
            public readonly VisibilityTransitionParams VisibilityTransition;
            public readonly IWidget Source;
            public readonly IWidget Target;
            private readonly TimeMode _timeMode;

            public Params(in VisibilityTransitionParams visibilityTransition, IWidget source, IWidget target, TimeMode timeMode)
            {
                VisibilityTransition = visibilityTransition;
                Source = source;
                Target = target;
                _timeMode = timeMode;
            }

            public override bool Equals(object obj)
            {
                return obj is Params other && Equals(other);
            }

            public bool Equals(Params other)
            {
                return VisibilityTransition == other.VisibilityTransition && Source == other.Source && Target == other.Target;
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(VisibilityTransition, Source, Target);
            }

            public static bool operator ==(Params lhs, Params rhs)
            {
                return lhs.Equals(rhs);
            }

            public static bool operator !=(Params lhs, Params rhs)
            {
                return !(lhs == rhs);
            }

            public AnimationPlayable CreateTargetPlayable()
            {
                return CreatePlayable(Target, WidgetVisibility.Visible, VisibilityTransition.EntryAnimationRef, VisibilityTransition.Length,
                    VisibilityTransition.EasingMode);
            }

            public AnimationPlayable CreateSourcePlayable()
            {
                return CreatePlayable(Target, WidgetVisibility.Hidden, VisibilityTransition.ExitAnimationRef, VisibilityTransition.Length,
                    VisibilityTransition.EasingMode);
            }

            private AnimationPlayable CreatePlayable(IWidget widget, WidgetVisibility visibility, WidgetAnimationRef widgetAnimationRef,
                float length, EasingMode easingMode)
            {
                if (!widgetAnimationRef.IsValid) throw new InvalidOperationException("Invalid implicit animation.");
                IAnimation widgetAnimation = widgetAnimationRef.Resolve(widget, visibility);
                return widgetAnimation.Playable(length, 0.0F, PlaybackMode.Forward, easingMode, _timeMode);
            }

            public Params CreateInverted()
            {
                TransitionSortPriority sortPriority;
                switch (VisibilityTransition.SortPriority)
                {
                    default:
                        sortPriority = TransitionSortPriority.Auto;
                        break;
                    case TransitionSortPriority.Source:
                        sortPriority = TransitionSortPriority.Target;
                        break;
                    case TransitionSortPriority.Target:
                        sortPriority = TransitionSortPriority.Source;
                        break;
                }
                VisibilityTransitionParams transitionParams = new VisibilityTransitionParams(
                    VisibilityTransition.Length,
                    VisibilityTransition.EasingMode.GetInverseEasingMode(),
                    VisibilityTransition.EntryAnimationRef,
                    VisibilityTransition.ExitAnimationRef,
                    sortPriority
                );
                return new Params(in transitionParams, Target, Source, _timeMode);
            }
        }

        public bool IsTransitionActive => _transitions.Count > 0;
        public IWidget ActiveTarget => _transitions.Count > 0 ? _transitions[0].Key.Target : null;
        public IWidget ActiveSource => _transitions.Count > 0 ? _transitions[0].Key.Target : null;

        private Params? _activeTransition => _transitions.Count > 0 ? _transitions[0].Key : null;
        
        private readonly List<KeyValuePair<Params, CancellationTokenSource>> _transitions = new();
        private readonly TimeMode _timeMode = TimeMode.Scaled;

        private TransitionManager() { }

        public TransitionManager(TimeMode timeMode)
        {
            _timeMode = timeMode;
        }

        public async Awaitable Transition(VisibilityTransitionParams visibilityTransition, IWidget sourceWidget, IWidget targetWidget,
            CancellationToken cancellationToken = default)
        {
            if (sourceWidget == null) throw new ArgumentNullException(nameof(sourceWidget));
            if (targetWidget == null) throw new ArgumentNullException(nameof(targetWidget));

            Params transitionParams = new Params(visibilityTransition, sourceWidget, targetWidget, _timeMode);
            CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _transitions.Add(new KeyValuePair<Params, CancellationTokenSource>(transitionParams, cts));
            CancellationToken token = cts.Token;

            if (IsTransitionActive)
            {
                if (_transitions[0].Key.Target != sourceWidget)
                    throw new InvalidOperationException($"Cannot transition from {sourceWidget} to {_transitions[0].Key.Target}");

                bool FindCts(KeyValuePair<Params, CancellationTokenSource> pair) => pair.Value == cts;
                while (_transitions.Count > 0 && _transitions[0].Value != cts)
                {
                    if (token.IsCancellationRequested)
                    {
                        int index = _transitions.FindIndex(FindCts);
                        if (index >= 0)
                        {
                            for (int i = _transitions.Count - 1; i > index; i--)
                            {
                                CancellationTokenSource queuedCts = _transitions[i].Value;
                                _transitions.RemoveAt(i);
                                queuedCts.Cancel();
                            }
                            _transitions.RemoveAt(index);
                        }
                        throw new OperationCanceledException(token);
                    }
                    await Awaitable.NextFrameAsync(token);
                }

                if (!_activeTransition.HasValue || !_activeTransition.Value.Equals(transitionParams))
                    throw new InvalidOperationException("A queued transition was not successfully canceled.");
            }
            await ExecuteTransition(transitionParams, token);
            _transitions.RemoveAt(0);
        }

        public async Awaitable SkipActive()
        {
            if (IsTransitionActive) { }
        }

        public async Awaitable SkipAll()
        {
            if (IsTransitionActive) { }
        }

        public async Awaitable RewindActive(CancellationToken cancellationToken)
        {
            if (IsTransitionActive)
            {
                Params rewindParams = _transitions[0].Key.CreateInverted();
                for (int i = _transitions.Count - 1; i > 0; i--)
                {
                    CancellationTokenSource queuedCts = _transitions[i].Value;
                    _transitions.RemoveAt(i);
                    queuedCts.Cancel();
                }
                _transitions[0].Value.Cancel();
                CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                _transitions[0] = new KeyValuePair<Params, CancellationTokenSource>(rewindParams, cts);
                switch (rewindParams.VisibilityTransition.Target)
                {
                    case TransitionTarget.Both:
                        await WhenAll.Await(cts.Token, rewindParams.Source.RewindAnimation(cts.Token),
                            rewindParams.Target.RewindAnimation(cts.Token));
                        break;
                    case TransitionTarget.Target:
                        await WhenAll.Await(cts.Token, rewindParams.Target.RewindAnimation(cts.Token));
                        break;
                    case TransitionTarget.Source:
                        await WhenAll.Await(cts.Token, rewindParams.Source.RewindAnimation(cts.Token));
                        break;
                }
            }
        }

        private async Awaitable ExecuteTransition(Params transitionParams, CancellationToken cancellationToken)
        {
            if (transitionParams.VisibilityTransition.SortPriority == TransitionSortPriority.Auto)
            {
                switch (transitionParams.VisibilityTransition.Target)
                {
                    default:
                    case TransitionTarget.Both:
                        transitionParams.Target.SortInlineWith(transitionParams.Source);
                        break;
                    case TransitionTarget.Target:
                        transitionParams.Target.SortAbove(transitionParams.Source);
                        break;
                    case TransitionTarget.Source:
                        transitionParams.Source.SortAbove(transitionParams.Target);
                        break;
                }
            }
            else
            {
                switch (transitionParams.VisibilityTransition.SortPriority)
                {
                    case TransitionSortPriority.Source:
                        transitionParams.Source.SortAbove(transitionParams.Target);
                        break;
                    case TransitionSortPriority.Target:
                        transitionParams.Target.SortAbove(transitionParams.Source);
                        break;
                }
            }

            if (transitionParams.VisibilityTransition.Target == TransitionTarget.None)
            {
                ExecuteNone(in transitionParams);
            }
            else
            {
                switch (transitionParams.VisibilityTransition.Target)
                {
                    case TransitionTarget.Both:
                        await ExecuteOnBoth(transitionParams, cancellationToken);
                        break;
                    case TransitionTarget.Target:
                        await ExecuteOnTarget(transitionParams, cancellationToken);
                        break;
                    case TransitionTarget.Source:
                        await ExecuteOnSource(transitionParams, cancellationToken);
                        break;
                }
            }
        }

        private void ExecuteNone(in Params transitionParams)
        {
            transitionParams.Source.SetVisibility(WidgetVisibility.Hidden);
            transitionParams.Target.SetVisibility(WidgetVisibility.Visible);
        }

        private async Awaitable ExecuteOnTarget(Params transitionParams, CancellationToken cancellationToken)
        {
            AnimationPlayable targetWindowPlayable = transitionParams.CreateTargetPlayable();
            await transitionParams.Target.AnimateVisibility(WidgetVisibility.Visible, targetWindowPlayable, InterruptBehavior.Immediate, cancellationToken);
            transitionParams.Source.SetVisibility(WidgetVisibility.Hidden);
        }

        private async Awaitable ExecuteOnSource(Params transitionParams, CancellationToken cancellationToken)
        {
            transitionParams.Target.SetVisibility(WidgetVisibility.Visible);
            AnimationPlayable sourceWindowPlayable = transitionParams.CreateSourcePlayable();
            await transitionParams.Source.AnimateVisibility(WidgetVisibility.Hidden, sourceWindowPlayable, InterruptBehavior.Immediate, cancellationToken);
        }

        private async Awaitable ExecuteOnBoth(Params transitionParams, CancellationToken cancellationToken)
        {
            AnimationPlayable sourceWindowPlayable = transitionParams.CreateSourcePlayable();
            Awaitable sourceAwaitable = transitionParams.Source.AnimateVisibility(WidgetVisibility.Hidden, sourceWindowPlayable, InterruptBehavior.Immediate, 
                cancellationToken);

            AnimationPlayable targetWindowPlayable = transitionParams.CreateTargetPlayable();
            Awaitable targetAwaitable = transitionParams.Target.AnimateVisibility(WidgetVisibility.Visible, targetWindowPlayable, InterruptBehavior.Immediate, 
                cancellationToken);
            
            await WhenAll.Await(cancellationToken, sourceAwaitable, targetAwaitable);
        }

        public void Terminate()
        {
            _ = SkipAll();
        }
    }
}