using System.Collections.Generic;

using UIFramework.Collectors;
using UIFramework.Controllers.Interfaces;
using UIFramework.Core.Interfaces;
using UIFramework.Groups;

using UnityEngine.Extension;

namespace UIFramework.Controllers
{
    public class ScreenController : ScreenGroupBase, IScreenController
    {
        public bool Active => IsInitialized && IsVisible;
        
        public ScreenController(IEnumerable<WidgetCollector<IScreen>> collectors, TimeMode timeMode)
            : base(collectors, timeMode) { }

        public void ManagedUpdate()
        {
            foreach (IScreen screen in Screens)
            {
                if (screen.IsVisible)
                    screen.Tick(DeltaTime);
            }
        }

        protected override void OnInitialize()
        {
            base.OnInitialize();
            UpdateManager.AddUpdatable(this);
        }

        protected override void OnTerminate()
        {
            UpdateManager.RemoveUpdatable(this);
            base.OnTerminate();
        }
    }
}
