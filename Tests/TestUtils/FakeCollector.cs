using System.Collections.Generic;

using UIFramework.Collectors;
using UIFramework.Core.Interfaces;

namespace UIFramework.TestUtils
{
    // Screen source for a controller without a scene. Viable because the controller takes the collector
    // interface rather than the shipped WidgetCollector, which is a MonoBehaviour.
    public sealed class FakeCollector : IWidgetCollector<IScreen>
    {
        private readonly IScreen[] _screens;

        public FakeCollector(params IScreen[] screens) => _screens = screens;

        public int CollectCount { get; private set; }

        public System.Type WidgetType => typeof(IScreen);

        public IEnumerable<IScreen> Collect()
        {
            CollectCount++;
            return _screens;
        }
    }
}
