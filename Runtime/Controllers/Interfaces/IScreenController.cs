using System.Collections.Generic;

using UIFramework.Groups;
using UIFramework.Navigation.Interfaces;

namespace UIFramework.Controllers.Interfaces
{
    // A controller that manages screens as a stack of layered groups over one shared registry.
    // Navigation (IScreenNavigator) delegates to the active (top) group; PushGroup opens a new overlay
    // layer above the current one, which stays visible but input-paused. Groups are pooled and collapse
    // via bubble-return or a screen-driven Exit.
    public interface IScreenController : IController, IScreenNavigator
    {
        // The active group stack, bottom (index 0) to top (the active group).
        public IReadOnlyList<IScreenGroup> Groups { get; }
        public IScreenGroup ActiveGroup { get; }

        // Opens a new group as the top overlay layer and returns it; the layer below stays visible but is
        // input-paused. Navigate into the returned group (or via the controller, which targets the top).
        public IScreenGroup PushGroup();
    }
}
