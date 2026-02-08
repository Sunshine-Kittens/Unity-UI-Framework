using System.Collections.Generic;

using UIFramework.Collectors;
using UIFramework.Core.Interfaces;
using UIFramework.Navigation;

namespace UIFramework.Controllers
{
    public class ScreenController : Controller<IScreen>
    {
        protected override IEnumerable<WidgetCollector<IScreen>> Collectors { get; }
        
        protected override void OnNavigationUpdate(NavigationResult<IScreen> navigationResult)
        {
            if (navigationResult.Success)
            {
                bool backButtonActive = navigationResult.HistoryCount > 0;
                SetBackButtonActive(backButtonActive);
            }
        }

        protected virtual void SetBackButtonActive(bool active)
        {
            
        }
    }
}
