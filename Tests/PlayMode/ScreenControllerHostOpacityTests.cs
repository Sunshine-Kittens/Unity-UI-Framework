using NUnit.Framework;

using UIFramework.Controllers;
using UIFramework.Core.Interfaces;
using UIFramework.TestUtils;

using UnityEngine;
using UnityEngine.Extension;

namespace UIFramework.Tests.PlayMode
{
    // The host rebuilds its controller every time it is enabled, so an opacity set on the host has to outlive
    // the controller it was first applied to.
    public sealed class ScreenControllerHostOpacityTests
    {
        [Test]
        public void HostOpacitySurvivesADisableEnableCycle()
        {
            using PoolScope pools = new();
            FakeScreenA screen = new();
            GameObject root = new("TestHost");
            root.SetActive(false);
            try
            {
                FakeHost host = root.AddComponent<FakeHost>();
                host.Screens = new IScreen[] { screen };
                root.SetActive(true);

                host.SetOpacity(0.5f);
                Assert.That(host.Controller.Opacity, Is.EqualTo(0.5f), "a live host forwards to its controller");
                host.NavigateTo<FakeScreenA>().Execute();
                Assert.That(screen.Opacity, Is.EqualTo(0.5f));

                root.SetActive(false);
                root.SetActive(true);
                host.NavigateTo<FakeScreenA>().Execute();

                Assert.That(host.Opacity, Is.EqualTo(0.5f));
                Assert.That(host.Controller.Opacity, Is.EqualTo(0.5f), "the rebuilt controller starts at the host's opacity");
                Assert.That(screen.Opacity, Is.EqualTo(0.5f));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void HostOpacitySetBeforeTheFirstEnableIsApplied()
        {
            using PoolScope pools = new();
            FakeScreenA screen = new();
            GameObject root = new("TestHost");
            root.SetActive(false);
            try
            {
                FakeHost host = root.AddComponent<FakeHost>();
                host.Screens = new IScreen[] { screen };
                host.SetOpacity(0.5f);

                root.SetActive(true);
                host.NavigateTo<FakeScreenA>().Execute();

                Assert.That(host.Controller.Opacity, Is.EqualTo(0.5f));
                Assert.That(screen.Opacity, Is.EqualTo(0.5f));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void OpacitySetThroughTheControllerSurvivesADisableEnableCycle()
        {
            using PoolScope pools = new();
            GameObject root = new("TestHost");
            root.SetActive(false);
            try
            {
                FakeHost host = root.AddComponent<FakeHost>();
                host.Screens = new IScreen[] { new FakeScreenA() };
                root.SetActive(true);

                host.Controller.SetOpacity(0.3f);
                Assert.That(host.Opacity, Is.EqualTo(0.3f), "the host reports its live controller's opacity");

                root.SetActive(false);
                Assert.That(host.Opacity, Is.EqualTo(0.3f), "a disabled host keeps the value it captured");
                root.SetActive(true);

                Assert.That(host.Controller.Opacity, Is.EqualTo(0.3f));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private sealed class FakeHost : ScreenControllerHost
        {
            public IScreen[] Screens;

            protected override ScreenController CreateController() =>
                new(new[] { new FakeCollector(Screens) }, TimeMode.Scaled, new ManualClock());
        }
    }
}
