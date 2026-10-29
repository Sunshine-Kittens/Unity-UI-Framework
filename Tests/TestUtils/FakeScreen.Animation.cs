using System;
using System.Collections.Generic;
using System.Threading;

using UIFramework.Animation;
using UIFramework.Core.Interfaces;

using UnityEngine;
using UnityEngine.Extension;

namespace UIFramework.TestUtils
{
    // Animation for edit mode, where the player loop that drives real animation does not run. Off by default:
    // the animation members then throw synchronously, so reaching an end state without an exception proves the
    // instant path was taken.
    //
    // Enabled, each AnimateVisibility call starts a FakeAnimationRun that the test ends by hand. The members
    // follow WidgetBase step for step on the same primitives (a linked token source, a token registration and
    // completion sources), so the inline ordering of start events, cancellation and completion is the real
    // one. Where it differs from WidgetBase:
    // - nothing is evaluated and no time passes; a run ends only when completed, skipped or cancelled;
    // - only an immediate interrupt is modelled: interrupting a running animation any other way, or rewinding
    //   one, throws NotSupportedException;
    // - IgnoreCancellation models a widget whose animation ignores its token, which WidgetBase never does;
    //   interrupting or hiding such a run orphans it still running;
    // - SkipNeverSettles models a skip that ends the animation but whose awaitable never settles;
    // - there is no handle pooling, and there are no SetActive or OnShow/OnHide hooks.
    public abstract partial class FakeScreen
    {
        private const string _AnimationUnsupported =
            "FakeScreen cannot animate unless EnableAnimation() is called: real animation is player-loop driven.";

        private readonly List<FakeAnimationRun> _runs = new();
        private FakeAnimationRun _currentRun;
        private CancellationTokenSource _animationCts;

        public bool AnimationEnabled { get; private set; }

        // Runs never observe cancellation, modelling a widget whose animation ignores its token. Only
        // Complete() or SkipAnimation() ends them.
        public bool IgnoreCancellation;

        // SkipAnimation ends the running run as usual, but the awaitable it returns never settles.
        public bool SkipNeverSettles;

        // Every run started, in order.
        public IReadOnlyList<FakeAnimationRun> Runs => _runs;
        public FakeAnimationRun LastRun => _runs.Count > 0 ? _runs[^1] : null;

        // Every token RewindAnimation received, in order.
        public readonly List<CancellationToken> RewindTokens = new();

        public FakeScreen EnableAnimation()
        {
            AnimationEnabled = true;
            return this;
        }

        public VisibilityAnimationBuilder AnimateVisibility(WidgetVisibility visibility)
        {
            if (!AnimationEnabled)
                throw new NotSupportedException(_AnimationUnsupported);
            return new VisibilityAnimationBuilder(this, visibility);
        }

        public Awaitable AnimateVisibility(WidgetVisibility visibility, AnimationPlayable playable,
            InterruptBehavior interruptBehavior = InterruptBehavior.Immediate, CancellationToken cancellationToken = default)
        {
            if (!AnimationEnabled)
                throw new NotSupportedException(_AnimationUnsupported);
            return AnimateVisibilityCore(visibility, playable, interruptBehavior, cancellationToken);
        }

        public Awaitable SkipAnimation()
        {
            if (!AnimationEnabled)
                throw new NotSupportedException(_AnimationUnsupported);
            return SkipAnimationCore();
        }

        // Does nothing unless animating, as WidgetBase does; rewinding a running animation is not modelled.
        public Awaitable RewindAnimation(CancellationToken cancellationToken = default)
        {
            if (!AnimationEnabled)
                throw new NotSupportedException(_AnimationUnsupported);

            Calls.Add(nameof(RewindAnimation));
            RewindTokens.Add(cancellationToken);
            if (IsAnimating)
                throw new NotSupportedException("FakeScreen does not model rewinding a running animation.");

            AwaitableCompletionSource done = new();
            done.SetResult();
            return done.Awaitable;
        }

        private async Awaitable AnimateVisibilityCore(WidgetVisibility visibility, AnimationPlayable playable,
            InterruptBehavior interruptBehavior, CancellationToken cancellationToken)
        {
            Calls.Add($"{nameof(AnimateVisibility)}:{visibility}");
            if (visibility == Visibility)
                return;

            if (IsAnimating && interruptBehavior != InterruptBehavior.Immediate)
                throw new NotSupportedException($"FakeScreen does not model a {interruptBehavior} interrupt.");

            CancellationTokenSource cts = cancellationToken.CanBeCanceled
                ? CancellationTokenSource.CreateLinkedTokenSource(cancellationToken)
                : new CancellationTokenSource();

            // Unwinds the interrupted run inline, including its restore of the interactable override.
            if (IsAnimating)
                _animationCts?.Cancel();
            _isInteractable.SetOverrideValue(false);

            FakeAnimationRun run = new(this, visibility, playable, interruptBehavior, cancellationToken,
                IgnoreCancellation ? CancellationToken.None : cts.Token);
            _runs.Add(run);
            _animationCts = cts;
            _currentRun = run;

            Visibility = visibility;
            if (visibility == WidgetVisibility.Visible)
                Showing?.Invoke(this);
            else
                Hiding?.Invoke(this);

            try
            {
                await run.AnimationAwaitable;
            }
            catch (OperationCanceledException)
            {
                run.MarkCancelled();
                ReleaseRun(run);
                throw;
            }
            finally
            {
                Calls.Add(nameof(ResetAnimatedProperties));
                _isInteractable.SetOverrideValue(true);
            }

            // The end events fire while the run still counts as animating; only then does the run settle.
            if (Visibility == WidgetVisibility.Visible)
                Shown?.Invoke(this);
            else
                Hidden?.Invoke(this);
            run.MarkCompleted();
            ReleaseRun(run);
        }

        private async Awaitable SkipAnimationCore()
        {
            Calls.Add(nameof(SkipAnimation));
            if (IsAnimating)
            {
                FakeAnimationRun run = _currentRun;
                run.CompleteAnimation();
                if (SkipNeverSettles)
                    await new AwaitableCompletionSource().Awaitable;
                await run.CompletedAwaitable;
            }
        }

        private void ReleaseRun(FakeAnimationRun run)
        {
            if (ReferenceEquals(_currentRun, run))
                _currentRun = null;
            run.Release();
        }
    }
}
