using System.Collections;

using NUnit.Framework;

using UIFramework.Core;
using UIFramework.Core.Interfaces;
using UIFramework.Registry;

using UnityEngine;
using UnityEngine.TestTools;

namespace UIFramework.Tests.PlayMode
{
    // Verifies this assembly is discovered and wired. Asserts nothing about framework behaviour.
    public sealed class DiscoverySmokeTests
    {
        [Test]
        public void RuntimeAssemblyIsReferenced()
        {
            WidgetRegistry<IScreen> registry = new();
            Assert.That(registry.IsInitialized, Is.False);
        }

        // The reason play-mode tests exist: the player loop runs here and does not in edit mode, so anything
        // driven by Time or an awaited Awaitable can only be exercised from this assembly.
        [UnityTest]
        public IEnumerator PlayerLoopAdvancesFrames()
        {
            int startFrame = Time.frameCount;
            yield return null;
            Assert.That(Time.frameCount, Is.GreaterThan(startFrame));
        }

        [UnityTest]
        public IEnumerator UnityTimeSourceReportsPlayerLoopDelta()
        {
            yield return null;
            Assert.That(UnityTimeSource.Default.GetDeltaTime(UnityEngine.Extension.TimeMode.Unscaled),
                Is.GreaterThan(0f));
        }
    }
}
