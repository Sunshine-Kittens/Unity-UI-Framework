using System;

using UIFramework.Controllers.Interfaces;
using UIFramework.Core.Interfaces;

namespace UIFramework.UGUI
{
    public class Screen : Window, IScreen
    {
        public IScreenController Controller { get; private set; } = null;
        
        // IScreen
        public void SetController(IScreenController controller)
        {
            if (Controller != null)
                throw new InvalidOperationException("Cannot set the controller while it is already set");
            Controller = controller ?? throw new ArgumentNullException(nameof(controller));
        }

        public void ClearController()
        {
            if (Controller == null)
                throw new InvalidOperationException("Cannot clear the controller while it is not set");
            Controller = null;
        }
    }
}