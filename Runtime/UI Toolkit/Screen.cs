using System;

using UnityEngine.UIElements;

namespace UIFramework.UIToolkit
{
    public abstract class Screen : Window, IScreen
    {
        public Controller Controller { get; private set; } = null;

        protected virtual string BackButtonName => null;
        private Button _backButton = null;

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
            if (!string.IsNullOrWhiteSpace(BackButtonName))
            {
                _backButton = VisualElement.Q<Button>(BackButtonName);
                _backButton?.RegisterCallback<ClickEvent>(BackButtonClicked);
            }
        }

        protected override void OnTerminate()
        {
            Controller = null;
            _backButton?.UnregisterCallback<ClickEvent>(BackButtonClicked);
            base.OnTerminate();
        }

        public bool SetBackButtonActive(bool active)
        {
            if (_backButton != null)
            {
                _backButton.style.display = active ? DisplayStyle.Flex : DisplayStyle.None;
                return true;
            }
            return false;
        }

        private void BackButtonClicked(ClickEvent clickEvent)
        {
            if (Visibility == WidgetVisibility.Visible)
            {
                Controller.HideActiveScreen();
            }
        }
    }
}