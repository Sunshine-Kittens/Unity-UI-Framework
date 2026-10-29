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
    // Probes for animation behaviour established empirically rather than argued: fade direction, how skip
    // and rewind settle given that Unity resumes Awaitable continuations inline, and whether the interactable
    // gate survives interrupts.
    public sealed class AnimationInterruptProbeTests
    {
        private PoolScope _pools;
        private UguiWidgetFixture _fixture;

        private int HandlePoolCount => _pools.CountOf(UguiWidgetFixture.HandlePoolName);

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

        private Awaitable StartFade(WidgetVisibility to, float length = 2f)
        {
            IAnimation fade = _fixture.Widget.GetGenericAnimation(GenericAnimation.Fade, to);
            return _fixture.Widget.AnimateVisibility(to)
                .WithAnimation(fade)
                .WithLength(length)
                .Animate();
        }

        // PROBE: does a completed show-fade leave the widget opaque, or transparent?
        [UnityTest]
        public IEnumerator ShowFadeEndsOpaque()
        {
            AwaitableProbe probe = AwaitableProbe.Watch(StartFade(WidgetVisibility.Visible, 0.1f));
            yield return probe.WaitForCompletion();

            Debug.Log($"PROBE alpha_after_show_fade={_fixture.Widget.Opacity} IsVisible={_fixture.Widget.IsVisible}");
            Assert.That(probe.Exception, Is.Null, "the animation completed rather than faulting");
            Assert.That(_fixture.Widget.Opacity, Is.EqualTo(1f).Within(0.01f),
                "a show animation must end fully opaque");
        }

        // The end state cannot pin direction: ResetAnimatedProperties restores the resting alpha on every exit.
        // The start pose can — Play evaluates the first frame before AnimateVisibility first yields.
        [Test]
        public void AShowFadeStartsTransparent()
        {
            _ = StartFade(WidgetVisibility.Visible);

            Assert.That(_fixture.Widget.CanvasGroup.alpha, Is.EqualTo(0f).Within(0.01f));
        }

        [Test]
        public void AHideFadeStartsOpaque()
        {
            _fixture.Widget.SetVisibility(WidgetVisibility.Visible);

            _ = StartFade(WidgetVisibility.Hidden);

            Assert.That(_fixture.Widget.CanvasGroup.alpha, Is.EqualTo(1f).Within(0.01f));
        }

        // PROBE: SkipAnimation is async, so a fault lands in the returned awaitable, never at the call
        // site. Both awaitables are observed; the pool count discriminates a double release (2) from a
        // leaked handle (0). show.IsComplete immediately after the call discriminates whether completion
        // continuations resume inline or on a later player-loop tick.
        [UnityTest]
        public IEnumerator SkipAnimationSettlesCleanly()
        {
            AwaitableProbe show = AwaitableProbe.Watch(StartFade(WidgetVisibility.Visible));
            yield return null;
            Assert.That(_fixture.Widget.IsAnimating, Is.True, "animation is running");

            AwaitableProbe skip = AwaitableProbe.Watch(_fixture.Widget.SkipAnimation());
            Debug.Log($"PROBE show_complete_inline_after_skip={show.IsComplete}");

            yield return skip.WaitForCompletion();
            yield return show.WaitForCompletion();

            Debug.Log($"PROBE skip_exception={skip.Exception?.GetType().Name ?? "<none>"} " +
                      $"show_exception={show.Exception?.GetType().Name ?? "<none>"} " +
                      $"handle_pool={HandlePoolCount}");
            Assert.That(skip.Exception, Is.Null, "SkipAnimation settled without faulting");
            Assert.That(show.Exception, Is.Null, "the skipped animation completed rather than faulting");
            Assert.That(_fixture.Widget.IsAnimating, Is.False);
            Assert.That(HandlePoolCount, Is.EqualTo(1), "the handle went back to the pool exactly once");
        }

        // PROBE: same shape for RewindAnimation. The interrupted show is expected to surface the
        // cancellation it was handed; the rewind call itself must settle without faulting.
        [UnityTest]
        public IEnumerator RewindAnimationSettlesCleanly()
        {
            AwaitableProbe show = AwaitableProbe.Watch(StartFade(WidgetVisibility.Visible));
            yield return null;
            Assert.That(_fixture.Widget.IsAnimating, Is.True, "animation is running");

            AwaitableProbe rewind = AwaitableProbe.Watch(_fixture.Widget.RewindAnimation());

            yield return rewind.WaitForCompletion();
            yield return show.WaitForCompletion();

            Debug.Log($"PROBE rewind_exception={rewind.Exception?.GetType().Name ?? "<none>"} " +
                      $"show_exception={show.Exception?.GetType().Name ?? "<none>"} " +
                      $"visibility={_fixture.Widget.Visibility} handle_pool={HandlePoolCount}");
            Assert.That(rewind.Exception, Is.Null, "RewindAnimation settled without faulting");
            Assert.That(show.Exception, Is.TypeOf<OperationCanceledException>(),
                "the interrupted show observed its cancellation");
            Assert.That(_fixture.Widget.Visibility, Is.EqualTo(WidgetVisibility.Hidden), "the rewound show ends hidden");
            Assert.That(_fixture.Widget.IsAnimating, Is.False);
            Assert.That(HandlePoolCount, Is.EqualTo(1),
                "one pooled handle on balance: a double release on either path would read 2, a leaked rewind handle 0");
        }

        // A rewind and the interrupted animation simply playing on both end hidden and settled; only the direction
        // of travel tells them apart. The show runs far enough first that one frame of rewinding cannot finish
        // the rewind, whose completion would restore the resting alpha.
        [UnityTest]
        public IEnumerator RewindReversesTheInterruptedAnimation()
        {
            AwaitableProbe show = AwaitableProbe.Watch(StartFade(WidgetVisibility.Visible));
            float deadline = Time.realtimeSinceStartup + 10f;
            while (_fixture.Widget.CanvasGroup.alpha < 0.3f && Time.realtimeSinceStartup < deadline)
                yield return null;
            float beforeRewind = _fixture.Widget.CanvasGroup.alpha;
            Assert.That(beforeRewind, Is.GreaterThanOrEqualTo(0.3f), "the show has progressed far enough to rewind");

            AwaitableProbe rewind = AwaitableProbe.Watch(_fixture.Widget.RewindAnimation());
            yield return null;

            Assert.That(_fixture.Widget.CanvasGroup.alpha, Is.LessThan(beforeRewind),
                $"the rewound fade travels back toward hidden (sampled {beforeRewind} before rewinding)");

            yield return rewind.WaitForCompletion();
            yield return show.WaitForCompletion();
            Assert.That(rewind.Exception, Is.Null);
        }

        // PROBE: after one interrupt, is the interactable override still able to gate input?
        [UnityTest]
        public IEnumerator InteractableGateSurvivesAnInterrupt()
        {
            _ = StartFade(WidgetVisibility.Visible);
            yield return null;
            Assert.That(_fixture.Widget.IsInteractable.Value, Is.False, "animating widget is input-gated");

            _fixture.Widget.SetVisibility(WidgetVisibility.Hidden);   // interrupt
            yield return null;

            bool gatedAfter = false;
            _ = StartFade(WidgetVisibility.Visible);
            yield return null;
            gatedAfter = !_fixture.Widget.IsInteractable.Value;

            Debug.Log($"PROBE gated_on_second_animation={gatedAfter}");
            Assert.That(gatedAfter, Is.True,
                "the override counter must still gate input on the animation after an interrupt");
        }

        // Drift compounds per interrupt, so a single-interrupt check can pass while spam navigation still
        // breaks the gate: three interrupts then a fresh animation pins the compounding case, and the final
        // assert pins the balance coming back once that animation completes.
        [UnityTest]
        public IEnumerator InteractableGateSurvivesRepeatedInterrupts()
        {
            for (int i = 0; i < 3; i++)
            {
                _ = StartFade(WidgetVisibility.Visible);
                yield return null;
                Assert.That(_fixture.Widget.IsAnimating, Is.True, "animation is running");
                _fixture.Widget.SetVisibility(WidgetVisibility.Hidden);
                yield return null;
            }

            AwaitableProbe last = AwaitableProbe.Watch(StartFade(WidgetVisibility.Visible, 0.1f));
            yield return null;
            bool gatedDuring = !_fixture.Widget.IsInteractable.Value;
            yield return last.WaitForCompletion();

            Debug.Log($"PROBE gated_after_three_interrupts={gatedDuring} balanced_after={_fixture.Widget.IsInteractable.Value}");
            Assert.That(gatedDuring, Is.True, "the gate still works after repeated interrupts");
            Assert.That(last.Exception, Is.Null);
            Assert.That(_fixture.Widget.IsInteractable.Value, Is.True,
                "and the count balances once the animation completes");
        }
    }
}
