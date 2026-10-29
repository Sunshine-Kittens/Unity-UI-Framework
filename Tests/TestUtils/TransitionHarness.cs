using System;
using System.Collections.Generic;
using System.Threading;

using UIFramework.Animation;
using UIFramework.Core.Interfaces;
using UIFramework.Transitioning;

using UnityEngine;
using UnityEngine.Extension;

namespace UIFramework.TestUtils
{
    // Drives a TransitionManager directly from a synchronous edit-mode test.
    //
    // No frame passes inside a [Test], so a transition that waits for its turn in the queue waits until it is
    // cancelled, and a cancelled wait resumes inline. Every awaitable and token source a test starts goes
    // through here, so Close() can cancel whatever is left waiting and report anything that never settled
    // before the pools are cleared: a wait left behind would otherwise resume in a later frame and release
    // into another test's pool.
    public sealed class TransitionHarness
    {
        public const string EntryPool = "TransitionEntry";

        private readonly PoolScope _pools = new();
        private readonly List<AwaitableProbe> _probes = new();
        private readonly List<CancellationTokenSource> _sources = new();

        public TransitionHarness(TimeMode timeMode = TimeMode.Scaled) => Manager = new TransitionManager(timeMode);

        public TransitionManager Manager { get; }

        // -1 until the first transition of the domain registers the pool.
        public int PooledEntries => _pools.CountOf(EntryPool);

        public static VisibilityTransitionParams Instant => new(0f, EasingMode.Linear, WidgetAnimationRef.None,
            WidgetAnimationRef.None, TransitionSortPriority.Auto);

        public static VisibilityTransitionParams InstantWithPriority(TransitionSortPriority sortPriority)
            => new(0f, EasingMode.Linear, WidgetAnimationRef.None, WidgetAnimationRef.None, sortPriority);

        public static VisibilityTransitionParams EntryOnly(IAnimation entry, float length = 1f,
            EasingMode easingMode = EasingMode.Linear)
            => new(length, easingMode, WidgetAnimationRef.None, WidgetAnimationRef.FromExplicit(entry),
                TransitionSortPriority.Auto);

        public static VisibilityTransitionParams ExitOnly(IAnimation exit, float length = 1f)
            => new(length, EasingMode.Linear, WidgetAnimationRef.FromExplicit(exit), WidgetAnimationRef.None,
                TransitionSortPriority.Auto);

        public static VisibilityTransitionParams TwoSided(IAnimation exit, IAnimation entry, float length = 1f)
            => new(length, EasingMode.Linear, WidgetAnimationRef.FromExplicit(exit),
                WidgetAnimationRef.FromExplicit(entry), TransitionSortPriority.Auto);

        public static VisibilityTransitionParams Refs(in WidgetAnimationRef exit, in WidgetAnimationRef entry,
            float length = 1f)
            => new(length, EasingMode.Linear, exit, entry, TransitionSortPriority.Auto);

        public AwaitableProbe Watch(Awaitable awaitable)
        {
            AwaitableProbe probe = AwaitableProbe.Watch(awaitable);
            _probes.Add(probe);
            return probe;
        }

        // Cancelled by Close() if the test has not cancelled it.
        public CancellationTokenSource CreateSource()
        {
            CancellationTokenSource source = new();
            _sources.Add(source);
            return source;
        }

        public CancellationToken CancelledToken()
        {
            CancellationTokenSource source = CreateSource();
            source.Cancel();
            return source.Token;
        }

        public AwaitableProbe Run(in VisibilityTransitionParams transition, IWidget source, IWidget target,
            CancellationToken cancellationToken = default)
            => Watch(Manager.Transition(transition, source, target, cancellationToken));

        // The exception a rejected call produces, whether thrown by the call or carried by its awaitable.
        // Null when the call is accepted. An accepted call is left running like any other transition, so it
        // changes the next expected source.
        public Exception RejectionOf(Func<Awaitable> start)
        {
            AwaitableProbe probe;
            try
            {
                probe = Watch(start());
            }
            catch (Exception exception)
            {
                return exception;
            }
            return probe.IsComplete ? probe.Exception : null;
        }

        // Runs an instant transition whose source raises Hidden part-way through it; transitions that `queue`
        // starts from that handler line up behind it. When the instant transition finishes, the first of them
        // becomes the active transition and waits for a frame, which never passes inside a [Test], so it stays
        // active until cancelled. The rest stay queued behind it.
        public AwaitableProbe RunInstantThenQueue(FakeScreen source, IWidget target, Action queue)
        {
            source.SetVisibility(WidgetVisibility.Visible);

            void OnHidden(IWidget _)
            {
                source.Hidden -= OnHidden;
                queue();
            }

            source.Hidden += OnHidden;
            return Run(Instant, source, target);
        }

        // Cancels every source the test created, then resets the manager, then clears the pools. Returns how
        // many awaitables the test started are still unsettled after that. A source whose cancellation throws
        // does not stop the others or the reset; what it threw is rethrown at the end.
        public int Close()
        {
            try
            {
                List<Exception> faults = null;
                foreach (CancellationTokenSource source in _sources)
                {
                    try
                    {
                        source.Cancel();
                    }
                    catch (Exception exception)
                    {
                        (faults ??= new List<Exception>()).Add(exception);
                    }
                }
                try
                {
                    Manager.Reset();
                }
                catch (Exception exception)
                {
                    (faults ??= new List<Exception>()).Add(exception);
                }

                int unsettled = 0;
                foreach (AwaitableProbe probe in _probes)
                {
                    if (!probe.IsComplete)
                        unsettled++;
                }

                if (faults != null)
                {
                    throw new AggregateException(
                        $"Cancelling the test's token sources threw; {unsettled} awaitable(s) left unsettled.", faults);
                }
                return unsettled;
            }
            finally
            {
                foreach (CancellationTokenSource source in _sources)
                    source.Dispose();
                _pools.Dispose();
            }
        }
    }
}
