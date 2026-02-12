using System.Collections.Generic;

using UIFramework.Collectors;
using UIFramework.Core.Interfaces;

using UnityEngine.Extension;

namespace UIFramework.Controllers
{
    public class MenuController : ScreenController
    {
        public MenuController(IEnumerable<WidgetCollector<IScreen>> collectors, TimeMode timeMode) : base(collectors, timeMode) { }
    }
}
