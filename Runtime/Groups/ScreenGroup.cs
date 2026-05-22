using System.Collections.Generic;

using UIFramework.Collectors;
using UIFramework.Core.Interfaces;

using UnityEngine.Extension;

namespace UIFramework.Groups
{
    public class ScreenGroup : ScreenGroupBase, IScreenGroup
    {
        public ScreenGroup(IEnumerable<WidgetCollector<IScreen>> collectors, TimeMode timeMode)
            : base(collectors, timeMode) { }
        
        public virtual void Tick()
        {
            if (IsInitialized && IsVisible)
            {
                foreach (IScreen screen in Screens)
                {
                    if (screen.IsVisible)
                        screen.Tick(DeltaTime);
                }
            }
        }
    }
}