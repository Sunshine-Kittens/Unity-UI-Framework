using System;

using UIFramework.Core.Interfaces;
using UIFramework.Navigation.Interfaces;

namespace UIFramework.UGUI
{
    public class Screen : Window, IScreen
    {
        public IScreenNavigator Navigator { get; private set; } = null;

        public void SetNavigator(IScreenNavigator navigator)
        {
            if (Navigator != null)
                throw new InvalidOperationException("Cannot set the navigator while it is already set.");
            Navigator = navigator ?? throw new ArgumentNullException(nameof(navigator));
        }

        public void ClearNavigator()
        {
            if (Navigator == null)
                throw new InvalidOperationException("Cannot clear the navigator while it is not set.");
            Navigator = null;
        }
    }
}
