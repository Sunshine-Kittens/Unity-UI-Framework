using System;

using UnityEngine.UIElements;

namespace UIFramework.UIToolkit
{
    public abstract class Screen : Window, IScreen
    {
        public Controller Controller { get; private set; } = null;    

        public virtual bool SupportsHistory { get; } = true;

        protected virtual string BackButtonName { get; } = null;
        private Button BackButton = null;        

        public override int SortOrder
        {
            get
            {
                return base.SortOrder;
            }
            set
            {
                base.SortOrder = value;
            }
        }

        public Screen(UIBehaviourDocument uIBehaviourDocument, string identifier) : base(uIBehaviourDocument, identifier) { }

        // IScreen
        public ControllerType GetController<ControllerType>() where ControllerType : Controller
        {
            return Controller as ControllerType;
        }

        public void Initialize(Controller controller)
        {
            if (controller == null)
            {
                throw new ArgumentNullException(nameof(controller));
            }

            if (State == BehaviourState.Initialized)
            {
                throw new InvalidOperationException("Screen already initialized.");
            }
            Controller = controller;            
            base.Initialize();
        }

        // TODO: Find a more elegant solution for this.
        public new void Initialize() { throw new InvalidOperationException("Screen cannot be initialized without a controller."); }

        public sealed override void Terminate()
        {
            if (State != BehaviourState.Initialized)
            {
                throw new InvalidOperationException("Screen cannot be terminated.");
            }
            Controller = null;
            base.Terminate();            
        }

        protected override void OnInitialize(VisualElement visualElement)
        {
            base.OnInitialize(visualElement);
            if (!string.IsNullOrWhiteSpace(BackButtonName))
            {
                BackButton = visualElement.Q<Button>(BackButtonName);
                if(BackButton != null)
                {
                    BackButton.RegisterCallback<ClickEvent>(BackButtonClicked);
                }                
            }
        }

        protected override void OnTerminate()
        {
            base.OnTerminate();
            if(BackButton != null)
            {
                BackButton.UnregisterCallback<ClickEvent>(BackButtonClicked);
            }              
        }

        public bool SetBackButtonActive(bool active)
        {
            if(BackButton != null)
            {
                BackButton.style.display = active ? DisplayStyle.Flex : DisplayStyle.None;
                return true;
            }            
            return false;
        }

        public bool Equals(INavigableWindow other)
        {
            return other as Screen == this;
        }

        private void BackButtonClicked(ClickEvent clickEvent)
        {
            if(AccessState == AccessState.Open || AccessState == AccessState.Opening)
            {
                OnBackButtonClicked();
                Controller.TryCloseScreen();
            }            
        }
        
        protected virtual void OnBackButtonClicked() { }
    }
}