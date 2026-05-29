using System;
using System.Collections.Generic;
using System.Threading;

using UIFramework.Collectors;
using UIFramework.Controllers.Interfaces;
using UIFramework.Core.Interfaces;
using UIFramework.Groups;
using UIFramework.Navigation;

namespace UIFramework.Controllers
{
    public class ScreenGroupController : IScreenGroupController
    {
        public IReadOnlyList<IScreenGroup> Groups => throw new NotImplementedException();
        
        public NavigateToRequest<IScreen> CreateNavigateToRequest(IScreen window)
        {
            throw new NotImplementedException();
        }
        
        public NavigateToRequest<IScreen> CreateNavigateToRequest<TTarget>() where TTarget : class, IScreen
        {
            throw new NotImplementedException();
        }

        public NavigateToRequest<IScreen> CreateNavigateToRequest(string identifier)
        {
            throw new NotImplementedException();
        }
        
        public NavigateToResponse<IScreen> Return(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
        
        public NavigateToResponse<IScreen> Exit(in ExitRequest request)
        {
            throw new NotImplementedException();
        }
        
        public IScreenGroup AddGroup(IEnumerable<WidgetCollector<IScreen>> collectors)
        {
            throw new NotImplementedException();
        }
    }
}
