using NUnit.Framework;

using UIFramework.Animation;
using UIFramework.Core.Interfaces;
using UIFramework.TestUtils;

using UnityEngine;
using UnityEngine.Extension;

namespace UIFramework.Tests.PlayMode
{
    // How a visibility animation hands over when it ends. Unity resumes Awaitable continuations inline, so
    // skipping an animation runs its tail, its caller's continuation and anything that continuation starts
    // before SkipAnimation itself resumes.
    //
    // Synchronous tests: Play evaluates the start pose without a frame passing, and no frame passes inside a
    // [Test], so a 2s fade stays running until it is skipped.
    public sealed class WidgetAnimationCompletionTests
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
            Assume.That(fade, Is.Not.Null, "the uGUI backend supplies a generic fade");

            return _fixture.Widget.AnimateVisibility(to)
                .WithAnimation(fade)
                .WithLength(length)
                .Animate();
        }

        [Test]
        public void ASkipWaitsForItsOwnAnimationWhenTheNextOneReusesTheHandle()
        {
            AwaitableProbe hide = null;

            async Awaitable ShowThenHide()
            {
                await StartFade(WidgetVisibility.Visible);
                hide = AwaitableProbe.Watch(StartFade(WidgetVisibility.Hidden));
            }

            AwaitableProbe show = AwaitableProbe.Watch(ShowThenHide());
            Assert.That(_fixture.Widget.IsAnimating, Is.True, "arrange: the show is running");

            AwaitableProbe skip = AwaitableProbe.Watch(_fixture.Widget.SkipAnimation());

            Assert.That(show.IsComplete, Is.True, "arrange: the skipped show completed and its caller went on");
            Assert.That(show.Exception, Is.Null, "arrange: the skipped show succeeded");
            Assert.That(hide?.IsComplete, Is.False, "arrange: the caller's hide is running");
            Assert.That(HandlePoolCount, Is.Zero, "arrange: the hide took the show's handle back out of the pool");
            Assert.That(skip.IsComplete, Is.True,
                "the skip waits for the show it skipped, not for the hide that reuses its handle");
            Assert.That(skip.Exception, Is.Null);
            Assert.That(_fixture.Widget.IsAnimating, Is.True, "the hide is still animating");
        }
    }
}
