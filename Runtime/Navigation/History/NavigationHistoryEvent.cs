using System;

using UIFramework.Core;

namespace UIFramework.Navigation.History
{
    public class NavigationHistoryEvent : PooledHistoryEvent<NavigationHistoryEvent>
    {
        public Type WindowType { get; private set; }

        public static NavigationHistoryEvent Get(Type windowType)
        {
            NavigationHistoryEvent e = Get();
            e.WindowType = windowType;
            return e;
        }

        protected override void OnRelease() => WindowType = null;
    }
}