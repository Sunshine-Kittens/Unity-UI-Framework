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
    // Known defect: cancelling a running visibility animation corrupts the widget.
    //
    // Cancellation sets both of the handle's completion sources to Canceled. The awaited animation then throws
    // OperationCanceledException, and the catch routes through Complete(), which calls the non-Try SetResult on
    // an already-cancelled source. That throws InvalidOperationException, so the handle is never released, the
    // active handle reference is never cleared, and the interactable override set on entry is never restored.
    //
    // These tests assert the defect as it stands; invert them when it is fixed.
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
        {
            IAnimation fade = _fixture.Widget.GetGenericAnimation(GenericAnimation.Fade, WidgetVisibility.Visible);
            Assume.That(fade, Is.Not.Null, "the uGUI backend supplies a generic fade");

            return _fixture.Widget.AnimateVisibility(WidgetVisibility.Visible)
                .WithAnimation(fade)
                .WithLength(length)
                .WithCancellation(cancellationToken)
                .Animate();
        }

        // Control: an animation left to finish behaves correctly, which is what makes the cancel path's
        // divergence below a defect rather than a misunderstanding of the API.
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
        public IEnumerator CancellingSurfacesInvalidOperationRatherThanCancellation()
        {
            using CancellationTokenSource cts = new();
            AwaitableProbe probe = AwaitableProbe.Watch(StartFadeIn(cts.Token));

            yield return null;
            cts.Cancel();
            yield return probe.WaitForCompletion();

            Assert.That(probe.Exception, Is.TypeOf<InvalidOperationException>(),
                "the caller sees the completion source complaining, not the cancellation it asked for");
        }

        [UnityTest]
        public IEnumerator CancellingLeavesTheWidgetPermanentlyAnimating()
        {
            using CancellationTokenSource cts = new();
            AwaitableProbe probe = AwaitableProbe.Watch(StartFadeIn(cts.Token));

            yield return null;
            cts.Cancel();
            yield return probe.WaitForCompletion();

            Assert.That(_fixture.Widget.IsAnimating, Is.True,
                "the active handle is never cleared, so the widget reports animating for good");
        }

        [UnityTest]
        public IEnumerator CancellingLeavesTheWidgetNonInteractable()
        {
            using CancellationTokenSource cts = new();
            AwaitableProbe probe = AwaitableProbe.Watch(StartFadeIn(cts.Token));

            yield return null;
            cts.Cancel();
            yield return probe.WaitForCompletion();

            Assert.That(_fixture.Widget.IsInteractable.Value, Is.False,
                "the override set on entry is only restored on the success path");
        }

        [UnityTest]
        public IEnumerator CancellingLeaksThePooledHandle()
        {
            using CancellationTokenSource cts = new();
            AwaitableProbe probe = AwaitableProbe.Watch(StartFadeIn(cts.Token));

            yield return null;
            cts.Cancel();
            yield return probe.WaitForCompletion();

            Assert.That(_pools.CountOf(UguiWidgetFixture.HandlePoolName), Is.EqualTo(0),
                "Release() is unreachable once Complete() throws, so the handle never returns to the pool");
        }
    }
}
