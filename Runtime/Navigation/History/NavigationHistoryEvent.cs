using System;

using UIFramework.Core;

namespace UIFramework.Navigation.History
{
    public readonly struct NavigationHistoryEvent : IHistoryEvent
    {
        public readonly Type WindowType;

        public NavigationHistoryEvent(Type windowType)
        {
            WindowType = windowType;
        }
    }
}
