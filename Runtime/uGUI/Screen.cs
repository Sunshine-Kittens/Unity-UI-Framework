using System;

using UnityEngine;
using UnityEngine.UI;

namespace UIFramework.UGUI
{    
    public abstract class Screen : Window, IScreen
    {
        public Controller Controller { get; private set; } = null;   

        public virtual bool SupportsHistory { get; } = true;

        protected virtual Button BackButton { get; } = null;
        private Canvas _canvas = null;

        public override int SortOrder
        {
            get
            {
                return base.SortOrder;
            }
            set
            {
                base.SortOrder = value;
                _canvas.sortingOrder = value;
            }
        }

        // IScreen
        public ControllerType GetController<ControllerType>() where ControllerType : Controller
        {
            return Controller as ControllerType;
        }

        public virtual void Initialize(Controller controller)
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
            _canvas = GetComponentInParent<Canvas>(true);            
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

        protected override void OnInitialize()
        {
            base.OnInitialize();
            if (BackButton != null)
            {
                BackButton.onClick.AddListener(BackButtonClicked);
            }
        }

        protected override void OnTerminate()
        {
            base.OnTerminate();
            if (BackButton != null)
            {
                BackButton.onClick.RemoveListener(BackButtonClicked);
            }
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

        public bool Equals(INavigableWindow other)
        {
            return other as Screen == this;
        }
        
        private void BackButtonClicked()
        {
            if (AccessState == AccessState.Open || AccessState == AccessState.Opening)
            {
                OnBackButtonClicked();
                Controller.TryCloseScreen();
            }
        }

        protected virtual void OnBackButtonClicked() { }
    }
}