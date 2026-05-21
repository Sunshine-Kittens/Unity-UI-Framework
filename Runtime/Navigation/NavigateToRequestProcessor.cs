using UIFramework.Core;
using UIFramework.Core.Interfaces;
using UIFramework.Navigation.Base;
using UIFramework.Navigation.Context;
using UIFramework.Navigation.Interfaces;
using UIFramework.Transitioning;

using UnityEngine;
using UnityEngine.Extension;

using NavigationHistoryMetadata = UIFramework.Navigation.Metadata.NavigationHistoryMetadata;

namespace UIFramework.Navigation
{
    public class NavigateToRequestProcessor<TWindow> : 
        NavigateToRequestProcessorBase<TWindow, WindowContext<TWindow>, NavigateToResult<TWindow>>, 
        INavigateToRequestProcessor<TWindow> 
        where TWindow : class, IWindow 
    {
        public NavigateToRequestProcessor(TimeMode timeMode, WindowNavigator<TWindow> navigator, Core.History history, TransitionManager transitionManager) :
            base (timeMode, navigator, history, transitionManager) { }

        public NavigateToResponse<TWindow> ProcessNavigateToRequest(in NavigateToRequest<TWindow> request)
        {
            Awaitable awaitable = null;
            NavigateToResult<TWindow> result = Navigator.NavigateTo(request.Window);
            if (result.Success)
            {
                TWindow previous = result.Previous.Window;
                TWindow active = result.Active.Window;
                IHistoryEntry historyEntry = null;
                if (result.Metadata is NavigationHistoryMetadata historyMetadata)
                {
                    historyEntry = historyMetadata.HistoryEntry;
                }
                awaitable = Navigate(request.Transition, active, previous, request.Data, historyEntry, request.CancellationToken);
            }
            return new NavigateToResponse<TWindow>(result, awaitable);
        }
    }
}
