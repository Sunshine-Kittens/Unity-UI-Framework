using NUnit.Framework;

using UIFramework.Core;
using UIFramework.Core.Interfaces;
using UIFramework.Registry;
using UIFramework.TestUtils;

namespace UIFramework.Tests.EditMode
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

        [Test]
        public void TestUtilsAssemblyIsReferenced()
        {
            using PoolScope pools = new();
            Assert.That(pools.TotalPooled, Is.Zero);
        }

        [Test]
        public void InternalsAreVisible()
        {
            // Compiles only because AssemblyInfo.cs grants this assembly name access to UIFramework internals.
            Assert.That(PoolRegistry.CountOf("NoSuchPool"), Is.EqualTo(-1));
        }
    }
}
