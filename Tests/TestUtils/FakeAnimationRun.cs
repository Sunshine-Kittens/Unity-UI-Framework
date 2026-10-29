using System;
using System.Threading;

using UIFramework.Core.Interfaces;

using UnityEngine;
using UnityEngine.Extension;

namespace UIFramework.TestUtils
{
    // One AnimateVisibility call on an animation-enabled FakeScreen. It runs until the test completes it, a
    // cancellation token cancels it, or the widget interrupts it. There is deliberately no Cancel(): a run is
    // cancelled only through the token or the widget, so the unwinding order is the real one.
    public sealed class FakeAnimationRun
    {
        public enum RunState
        {
            Running,
            Completed,
            Cancelled
        }

        public FakeScreen Widget { get; }
        public WidgetVisibility Visibility { get; }
        public AnimationPlayable Playable { get; }
        public InterruptBehavior InterruptBehavior { get; }

        // The token as the caller passed it.
        public CancellationToken Token { get; }

        public RunState State { get; private set; } = RunState.Running;

        private readonly AwaitableCompletionSource _animation = new();
        private readonly AwaitableCompletionSource _completed = new();
        private CancellationTokenRegistration _registration;

        // Registers last: an already-cancelled token cancels the run here, before the widget raises its
        // start events, as the real widget does.
        internal FakeAnimationRun(FakeScreen widget, WidgetVisibility visibility, in AnimationPlayable playable,
            InterruptBehavior interruptBehavior, CancellationToken token, CancellationToken cancelledBy)
        {
            Widget = widget;
            Visibility = visibility;
            Playable = playable;
            InterruptBehavior = interruptBehavior;
            Token = token;
            _registration = cancelledBy.Register(CancelSources);
        }

        // The animation reaches its end. Throws when the run has already ended, so a test cannot complete a
        // cancelled run and pass.
        public void Complete()
        {
            if (State != RunState.Running || !_animation.TrySetResult())
                throw new InvalidOperationException(
                    $"{Widget.GetType().Name} run toward {Visibility} has already ended ({State}).");
        }

        internal Awaitable AnimationAwaitable => _animation.Awaitable;
        internal Awaitable CompletedAwaitable => _completed.Awaitable;

        internal void CompleteAnimation() => _animation.TrySetResult();

        internal void MarkCancelled()
        {
            State = RunState.Cancelled;
            _completed.TrySetCanceled();
        }

        internal void MarkCompleted()
        {
            State = RunState.Completed;
            _completed.TrySetResult();
        }

        internal void Release()
        {
            _registration.Dispose();
            _registration = default;
        }

        private void CancelSources()
        {
            _animation.TrySetCanceled();
            _completed.TrySetCanceled();
        }
    }
}
