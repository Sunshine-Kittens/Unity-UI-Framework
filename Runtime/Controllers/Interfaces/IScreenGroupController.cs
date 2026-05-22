using System.Collections.Generic;

using UIFramework.Collectors;
using UIFramework.Core.Interfaces;
using UIFramework.Groups;
using UIFramework.Navigation.Interfaces;

namespace UIFramework.Controllers.Interfaces
{
    public interface IScreenGroupController : IScreenNavigator
    {
        public IReadOnlyList<IScreenGroup> Groups { get; }
        public IScreenGroup AddGroup(IEnumerable<WidgetCollector<IScreen>> collectors);
    }
}
