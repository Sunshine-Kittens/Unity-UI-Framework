using System;
using System.Collections.Generic;
using System.Threading;

using UIFramework.Animation;
using UIFramework.Core.Interfaces;

using UnityEngine;
using UnityEngine.Extension;
using UnityEngine.Extension.Awaitable;

namespace UIFramework.Transitioning
{
    public class TransitionManager
    {
        private readonly struct Params : IEquatable<Params>
        {
            public readonly VisibilityTransitionParams Transition;
            public readonly IWidget Source;
            public readonly IWidget Target;
            private readonly TimeMode _timeMode;

            public Params(in VisibilityTransitionParams transition, IWidget source, IWidget target, TimeMode timeMode)
            {
                Transition = transition;
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
                return Transition == other.Transition && Source == other.Source && Target == other.Target;
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(Transition, Source, Target);
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
                return CreatePlayable(Target, WidgetVisibility.Visible, Transition.EntryAnimationRef, Transition.Length,
                    Transition.EasingMode);
            }

            public AnimationPlayable CreateSourcePlayable()
            {
                return CreatePlayable(Target, WidgetVisibility.Hidden, Transition.ExitAnimationRef, Transition.Length,
                    Transition.EasingMode);
            }

            private AnimationPlayable CreatePlayable(IWidget widget, WidgetVisibility visibility, WidgetAnimationRef widgetAnimationRef,
                float length, EasingMode easingMode)
            {
                if (!widgetAnimationRef.IsValid) throw new InvalidOperationException("Invalid implicit animation.");
                IAnimation widgetAnimation = widgetAnimationRef.Resolve(widget, visibility);
                return widgetAnimation.ToPlayable()
                    .WithLength(length)
                    .WithEasingMode(easingMode)
                    .WithTimeMode(_timeMode)
                    .Create();
            }

            public Params CreateInverted()
            {
                TransitionSortPriority sortPriority;
                switch (Transition.SortPriority)
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
                    Transition.Length,
                    Transition.EasingMode.GetInverseEasingMode(),
                    Transition.EntryAnimationRef,
                    Transition.ExitAnimationRef,
                    sortPriority
                );
                return new Params(in transitionParams, Target, Source, _timeMode);
            }
        }

        private sealed class Entry
        {
            public readonly Params Params;
            public readonly CancellationTokenSource Cts;

            public Entry(Params @params, CancellationTokenSource cts)
            {
                Params = @params;
                Cts = cts;
            }
        }
        
        public bool IsTransitionActive => _active != null;
        public IWidget ActiveTarget => _active != null ? _active.Params.Target : null;
        public IWidget ActiveSource => _active != null ? _active.Params.Source : null;

        private Entry _active = null;
        private readonly List<Entry> _pending = new();
        
        private readonly TimeMode _timeMode = TimeMode.Scaled;

        private TransitionManager() { }

        public TransitionManager(TimeMode timeMode)
        {
            _timeMode = timeMode;
        }

        public async Awaitable Transition(VisibilityTransitionParams transition, IWidget sourceWidget, IWidget targetWidget,
            CancellationToken cancellationToken = default)
        {
            if (sourceWidget == null) throw new ArgumentNullException(nameof(sourceWidget));
            if (targetWidget == null) throw new ArgumentNullException(nameof(targetWidget));

            IWidget expectedSource = GetExpectedSource();
            if (expectedSource != null && !ReferenceEquals(expectedSource, sourceWidget))
            {
                throw new InvalidOperationException(
                    $"Cannot transition from {sourceWidget}; expected source is {expectedSource}.");
            }

            Params @params = new Params(transition, sourceWidget, targetWidget, _timeMode);
            CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            Entry entry = new Entry(@params, linkedCts);
            _pending.Add(entry);
            
            CancellationToken token = linkedCts.Token;
            try
            {
                while (!IsActive(entry))
                {
                    token.ThrowIfCancellationRequested();
                
                    if (!IsPending(entry))
                        throw new OperationCanceledException(token);
                
                    if (_active == null && _pending.Count > 0 && ReferenceEquals(_pending[0], entry))
                    {
                        _pending.RemoveAt(0);
                        _active = entry;
                        break;
                    }
                    await Awaitable.NextFrameAsync(token);
                }
                await ExecuteTransition(@params, token);
            }
            catch (OperationCanceledException)
            {
                if (IsActive(entry))
                {
                    CancelAllPending();
                }
                else
                {
                    CancelPendingAfter(entry);
                }
                throw;
            }
            finally
            {
                if (IsActive(entry))
                {
                    _active = null;
                    PromoteNextIfAny();
                }
                else
                {
                    RemovePending(entry);
                }
                linkedCts.Dispose();
            }
        }
        
        public async Awaitable SkipActive()
        {
            if (_active == null)
                return;

            Params @params = _active.Params;
            CancellationToken token = CancellationToken.None;
            switch (@params.Transition.Target)
            {
                case TransitionTarget.Both:
                    await WhenAll.Await(token, @params.Source.SkipAnimation(), @params.Target.SkipAnimation());
                    break;
                case TransitionTarget.Target:
                    await WhenAll.Await(token, @params.Target.SkipAnimation());
                    break;
                case TransitionTarget.Source:
                    await WhenAll.Await(token, @params.Source.SkipAnimation());
                    break;
            }
        }

        public async Awaitable SkipAll()
        {
            if (_active == null)
                return;

            Params @params = _active.Params;
            CancellationToken token = CancellationToken.None;
            switch (@params.Transition.Target)
            {
                case TransitionTarget.Both:
                    await WhenAll.Await(token, @params.Source.SkipAnimation(), @params.Target.SkipAnimation());
                    break;
                case TransitionTarget.Target:
                    await WhenAll.Await(token, @params.Target.SkipAnimation());
                    break;
                case TransitionTarget.Source:
                    await WhenAll.Await(token, @params.Source.SkipAnimation());
                    break;
            }
            
            //TODO: This needs to somehow wait for when the active entry is assigned and then skip it.
        }
        
        public async Awaitable RewindActive(CancellationToken cancellationToken = default)
        {
            if (_active == null)
                return;
            
            Params rewindParams = _active.Params.CreateInverted();
            CancelAllPending();
            _active.Cts.Cancel();
            
            CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _active = new Entry(rewindParams, linkedCts);

            CancellationToken token = linkedCts.Token;
            switch (rewindParams.Transition.Target)
            {
                case TransitionTarget.Both:
                    await WhenAll.Await(token, rewindParams.Source.RewindAnimation(token), 
                        rewindParams.Target.RewindAnimation(token));
                    break;
                case TransitionTarget.Target:
                    await WhenAll.Await(token, rewindParams.Target.RewindAnimation(token));
                    break;
                case TransitionTarget.Source:
                    await WhenAll.Await(token, rewindParams.Source.RewindAnimation(token));
                    break;
            }

            _active = null;
            PromoteNextIfAny();
        }

        private async Awaitable ExecuteTransition(Params transitionParams, CancellationToken cancellationToken)
        {
            if (transitionParams.Transition.SortPriority == TransitionSortPriority.Auto)
            {
                switch (transitionParams.Transition.Target)
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
                switch (transitionParams.Transition.SortPriority)
                {
                    case TransitionSortPriority.Source:
                        transitionParams.Source.SortAbove(transitionParams.Target);
                        break;
                    case TransitionSortPriority.Target:
                        transitionParams.Target.SortAbove(transitionParams.Source);
                        break;
                }
            }

            if (transitionParams.Transition.Target == TransitionTarget.None)
            {
                ExecuteNone(in transitionParams);
            }
            else
            {
                switch (transitionParams.Transition.Target)
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

        private bool IsActive(Entry entry)
        {
            return ReferenceEquals(_active, entry);   
        }

        private bool IsPending(Entry entry)
        {
            return _pending.Contains(entry);
        }

        private void RemovePending(Entry entry)
        {
            int index = _pending.IndexOf(entry);
            if (index >= 0)
                _pending.RemoveAt(index);
        }

        private void CancelAllPending()
        {
            foreach (Entry t in _pending)
                t.Cts.Cancel();
            _pending.Clear();
        }

        private void CancelPendingAfter(Entry entry)
        {
            int index = _pending.IndexOf(entry);
            if (index < 0)
                return;

            for (int i = _pending.Count - 1; i > index; i--)
            {
                Entry pendingEntry = _pending[i];
                _pending.RemoveAt(i);
                pendingEntry.Cts.Cancel();
            }
        }
        
        private void PromoteNextIfAny()
        {
            if (_pending.Count == 0)
                return;

            Entry next = _pending[0];
            _pending.RemoveAt(0);
            _active = next;
        }
        
        private IWidget GetExpectedSource()
        {
            if (_pending.Count > 0)
                return _pending[^1].Params.Target;

            if (_active != null)
                return _active.Params.Target;

            return null;
        }
        
        public void Terminate()
        {
            _ = SkipAll();
        }
    }
}