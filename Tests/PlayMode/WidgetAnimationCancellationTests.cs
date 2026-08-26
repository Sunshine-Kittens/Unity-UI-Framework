using System;
using System.Collections;
using System.Threading;

using NUnit.Framework;

using UIFramework.Animation;
using UIFramework.Core.Interfaces;
using UIFramework.TestUtils;

using UnityEngine;
using UnityEngine.Extension;
using UnityEngine.TestTools;

namespace UIFramework.Tests.PlayMode
{
    // Cancelling a running visibility animation must leave the widget usable: the caller sees the
    // cancellation it asked for, the widget stops reporting itself as animating, the interactable override
    // taken on entry is given back, and the pooled handle returns to the pool.
    //
    // All four used to fail. Cancellation settles the handle's completion sources, and the teardown then
    // called the non-Try setters on them, so it threw partway through and skipped the rest of the cleanup.
    public sealed class WidgetAnimationCancellationTests
    {
        private PoolScope _pools;
        private UguiWidgetFixture _fixture;

        [SetUp]
        public void SetUp()
        {
            _pools = new PoolScope();
            _fixture = new UguiWidgetFixture();
        }

        [TearDown]
        public void TearDown()
        {
            _fixture?.Dispose();
            _pools?.Dispose();
        }

        private Awaitable StartFadeIn(CancellationToken cancellationToken, float length = 2f)
            => StartFade(WidgetVisibility.Visible, cancellationToken, length);

        private Awaitable StartFade(WidgetVisibility visibility, CancellationToken cancellationToken, float length = 2f)
        {
            IAnimation fade = _fixture.Widget.GetGenericAnimation(GenericAnimation.Fade, visibility);
            Assume.That(fade, Is.Not.Null, "the uGUI backend supplies a generic fade");

            return _fixture.Widget.AnimateVisibility(visibility)
                .WithAnimation(fade)
                .WithLength(length)
                .WithCancellation(cancellationToken)
                .Animate();
        }

        private IEnumerator CancelMidAnimation(AwaitableProbe probe, CancellationTokenSource cts)
        {
            yield return null;
            cts.Cancel();
            yield return probe.WaitForCompletion();
        }

        [UnityTest]
        public IEnumerator AnimationRunToCompletionReleasesItsHandle()
        {
            AwaitableProbe probe = AwaitableProbe.Watch(StartFadeIn(CancellationToken.None, 0.1f));

            yield return probe.WaitForCompletion();

            Assert.That(probe.Exception, Is.Null);
            Assert.That(_fixture.Widget.IsAnimating, Is.False);
            Assert.That(_fixture.Widget.IsInteractable.Value, Is.True);
            Assert.That(_pools.CountOf(UguiWidgetFixture.HandlePoolName), Is.EqualTo(1),
                "the handle is returned to the pool");
        }

        [UnityTest]
        public IEnumerator CancellingSurfacesOperationCanceled()
        {
            using CancellationTokenSource cts = new();
            AwaitableProbe probe = AwaitableProbe.Watch(StartFadeIn(cts.Token));

            yield return CancelMidAnimation(probe, cts);

            Assert.That(probe.Exception, Is.TypeOf<OperationCanceledException>(),
                "the caller is told the animation was cancelled, not that a completion source was misused");
        }

        [UnityTest]
        public IEnumerator CancellingClearsTheAnimatingState()
        {
            using CancellationTokenSource cts = new();
            AwaitableProbe probe = AwaitableProbe.Watch(StartFadeIn(cts.Token));

            yield return CancelMidAnimation(probe, cts);

            Assert.That(_fixture.Widget.IsAnimating, Is.False,
                "the active handle is cleared, so a later animation can start");
        }

        [UnityTest]
        public IEnumerator CancellingRestoresInteractivity()
        {
            using CancellationTokenSource cts = new();
            AwaitableProbe probe = AwaitableProbe.Watch(StartFadeIn(cts.Token));

            yield return CancelMidAnimation(probe, cts);

            Assert.That(_fixture.Widget.IsInteractable.Value, Is.True,
                "the override taken when the animation started is given back on every exit path");
        }

        [UnityTest]
        public IEnumerator CancellingReturnsTheHandleToThePool()
        {
            using CancellationTokenSource cts = new();
            AwaitableProbe probe = AwaitableProbe.Watch(StartFadeIn(cts.Token));

            yield return CancelMidAnimation(probe, cts);

            Assert.That(_pools.CountOf(UguiWidgetFixture.HandlePoolName), Is.EqualTo(1),
                "the handle is pooled rather than leaked, along with its cancellation registration");
        }

        [UnityTest]
        public IEnumerator AWidgetCanAnimateAgainAfterBeingCancelled()
        {
            using (CancellationTokenSource cts = new())
            {
                AwaitableProbe cancelled = AwaitableProbe.Watch(StartFadeIn(cts.Token));
                yield return CancelMidAnimation(cancelled, cts);
            }

            // The cancelled show already flipped Visibility to Visible, so the second animation must go the
            // other way — animating to Visible again would early-return without running anything.
            AwaitableProbe second = AwaitableProbe.Watch(StartFade(WidgetVisibility.Hidden, CancellationToken.None, 0.1f));
            yield return second.WaitForCompletion();

            Assert.That(second.Exception, Is.Null, "a cancelled animation leaves no state behind");
            Assert.That(_fixture.Widget.Visibility, Is.EqualTo(WidgetVisibility.Hidden));
            Assert.That(_fixture.Widget.IsAnimating, Is.False);
        }
    }
}
