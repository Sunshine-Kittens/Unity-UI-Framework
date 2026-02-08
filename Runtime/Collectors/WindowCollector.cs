using System.Collections.Generic;

using UIFramework.Interfaces;

namespace UIFramework.Collectors
{
    public sealed class WindowCollector : HierarchyWidgetCollector<IWindow>
    {
        public override IEnumerable<IWindow> Collect()
        {
            foreach (IWindow window in base.Collect())
            {
                if (window is IScreen) continue;
                yield return window;
            }
        }
    }
}
