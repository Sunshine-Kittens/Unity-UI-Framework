using System;

using UIFramework.Interfaces;

using UnityEngine;
using UnityEngine.UI;

namespace UIFramework.UGUI
{
    public class Screen : Window, IScreen
    {
        public Controller Controller { get; private set; } = null;

        protected virtual Button BackButton => null;

        // IScreen
        public TControllerType GetController<TControllerType>() where TControllerType : Controller
        {
            return Controller as TControllerType;
        }

        public void SetController(Controller controller)
        {
            if (Controller != null)
            {
                throw new InvalidOperationException("Cannot set the controller while it is already set");
            }
            Controller = controller ?? throw new ArgumentNullException(nameof(controller));
        }

        protected override void OnInitialize()
        {
            base.OnInitialize();
            BackButton?.onClick.AddListener(BackButtonClicked);
        }

        protected override void OnTerminate()
        {
            Controller = null;
            BackButton?.onClick.RemoveListener(BackButtonClicked);
            base.OnTerminate();
        }

        public bool SetBackButtonActive(bool active)
        {
            if (BackButton != null)
            {
                BackButton.gameObject.SetActive(active);
                return true;
            }
            return false;
        }

        private void BackButtonClicked()
        {
            if (Visibility == WidgetVisibility.Visible)
            {
                Controller.HideActiveScreen();
            }
        }
    }
}