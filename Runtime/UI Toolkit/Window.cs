using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.Extension;
using UnityEngine.UIElements;

namespace UIFramework.UIToolkit
{
    public abstract class Window : UIBehaviour, IWindow
    {
        private const string _NonInteractiveClassName = ".non-interactive";

        // UI Toolkit Window
        protected UIDocument UIDocument { get { return _uiBehaviourDocument.Document; } }
        private UIBehaviourDocument _uiBehaviourDocument = null;

        protected VisualElement VisualElement { get { return _visualElement; } }
        private VisualElement _visualElement = null;

        // IAccessible
        public AccessState AccessState { get; private set; } = AccessState.None;

        public event IAccessibleAction Opening = default;
        public event IAccessibleAction Opened = default;
        public event IAccessibleAction Closed = default;
        public event IAccessibleAction Closing = default;

        public AnimationPlayer.PlaybackData AccessAnimationPlaybackData { get; private set; } = default;
        public AccessAnimationPlayable AccessAnimationPlayable { get; private set; } = default;

        // IWindow
        public string Identifier { get; } = string.Empty;
        public bool IsVisible { get { return AccessState != AccessState.Closed; } }

        IReadOnlyScalarFlag IReadOnlyWindow.IsEnabled => IsEnabled;
        public IScalarFlag IsEnabled => _isEnabled;
        private readonly ScalarFlag _isEnabled = new ScalarFlag(true);
        
        IReadOnlyScalarFlag IReadOnlyWindow.IsInteractable => IsInteractable;
        public IScalarFlag IsInteractable => _isInteractable;
        private readonly ScalarFlag _isInteractable = new ScalarFlag(true);

        IReadOnlyScalarFlag IReadOnlyWindow.IsHidden => IsHidden;
        public IScalarFlag IsHidden => _isHidden;
        private readonly ScalarFlag _isHidden = new ScalarFlag(false);

        public virtual int SortOrder
        {
            get
            {
                return _sortOrder;
            }
            set
            {
                _sortOrder = value;
                SetSortOrder(_sortOrder);
            }
        }
        private int _sortOrder = 0;

        // UI Toolkit Window        
        private bool _isActive = true;
        private bool _isWaiting = false;
        private AnimationPlayer _animationPlayer = null;
        private IAccessibleAction _onAccessAnimationComplete = null;

        private Dictionary<GenericWindowAnimationType, GenericWindowAnimation> _genericAnimations = new Dictionary<GenericWindowAnimationType, GenericWindowAnimation>();

        protected Window() { }

        public Window(UIBehaviourDocument uiBehaviourDocument, string identifier)
        {
            if (uiBehaviourDocument == null) throw new ArgumentNullException(nameof(uiBehaviourDocument));
            if (uiBehaviourDocument.Document == null) throw new ArgumentNullException(nameof(uiBehaviourDocument.Document));
            if (string.IsNullOrEmpty(identifier)) throw new ArgumentNullException(nameof(identifier));

            _uiBehaviourDocument = uiBehaviourDocument;
            Identifier = identifier;                             
        }

        // IUIBehaviour
        public override bool IsValid()
        {
            return _uiBehaviourDocument.Document.rootVisualElement != null && _uiBehaviourDocument.Document.rootVisualElement.Contains(_visualElement);
        }

        public override void Initialize()
        {
            if (State == BehaviourState.Initialized)
            {
                throw new InvalidOperationException("Window already initialized.");
            }

            if(!_uiBehaviourDocument.gameObject.activeSelf)
            {
                Debug.Log("UI Document and all parents enabled by Window Initialization.", _uiBehaviourDocument);
                Transform target = _uiBehaviourDocument.gameObject.transform;
                while (target != null)
                {
                    target.gameObject.SetActive(true);
                    target = target.parent;
                }
            }

            if(!_uiBehaviourDocument.Document.gameObject.activeInHierarchy)
            {
                throw new InvalidOperationException("Unabled to initialize UI Toolkit window for disabled UI document.");
            }

            VisualElement visualElement = _uiBehaviourDocument.Document.rootVisualElement.Q(Identifier);
            if (visualElement == null) throw new InvalidOperationException($"Failed to find visual element with name: {Identifier}");
            _visualElement = visualElement;

            _uiBehaviourDocument.Enabled += OnDocumentEnabled;
            _uiBehaviourDocument.Disabled += OnDocumentDisabled;
            _uiBehaviourDocument.Destroyed += OnDocumentDestroyed;

            base.Initialize();
            _isEnabled.OnUpdate += OnIsEnabledUpdated;
            _isInteractable.OnUpdate += OnIsInteractableUpdated;
            _isHidden.OnUpdate += OnIsHiddenUpdated;
            AccessState = AccessState.Closed;

            SetActive(false);
            OnInitialize(_visualElement);
        }

        public override void Terminate()
        {
            if (State != BehaviourState.Initialized)
            {
                throw new InvalidOperationException("Window cannot be terminated.");
            }
            base.Terminate();

            _uiBehaviourDocument.Enabled -= OnDocumentEnabled;
            _uiBehaviourDocument.Disabled -= OnDocumentDisabled;
            _uiBehaviourDocument.Destroyed -= OnDocumentDestroyed;

            Close();            
            AccessState = AccessState.None;
            ResetAnimatedProperties();
            ClearAnimationReferences();            
            _visualElement = null;
            _isEnabled.Reset(true);
            _isEnabled.OnUpdate -= OnIsEnabledUpdated;
            _isInteractable.Reset(true);
            _isInteractable.OnUpdate -= OnIsInteractableUpdated;
            _isHidden.Reset(false);
            _isHidden.OnUpdate -= OnIsHiddenUpdated;
            _sortOrder = 0;
            _genericAnimations.Clear();
            OnTerminate();
        }

        // IWindow
        public GenericWindowAnimation GetAnimation(GenericWindowAnimationType type)
        {
            GenericWindowAnimation animation;
            if (!_genericAnimations.TryGetValue(type, out animation))
            {
                animation = new UITKGenericWindowAnimation(_visualElement, type);
                _genericAnimations.Add(type, animation);
            }
            return animation;
        }

        public bool SetWaiting(bool waiting)
        {
            if (ShowWaitingIndicator(waiting))
            {
                if (waiting != _isWaiting)
                {
                    _isWaiting = waiting;
                    _isEnabled.SetOverrideValue(!waiting);
                }
                return true;
            }
            return false;
        }

        // IAccessible
        public virtual AccessAnimation GetDefaultAccessAnimation()
        {
            return GetAnimation(GenericWindowAnimationType.Fade);
        }

        public virtual void ResetAnimatedProperties()
        {
            if(_visualElement != null)
            {
                _visualElement.style.opacity = 1;
                _visualElement.style.translate = new Translate(Length.Percent(0.0F), Length.Percent(0.0F));
                _visualElement.style.scale = new Scale(Vector2.one);
                _visualElement.style.rotate = new Rotate(0.0F);
            }            
        }

        public bool Open(in AccessAnimationPlayable playable, IAccessibleAction onComplete = null)
        {
            if (AccessState == AccessState.Closing || AccessState == AccessState.Closed)
            {
                if (_animationPlayer != null)
                {
                    Debug.Log("Window is already playing a close animation, the provided open animation is ignored and the current close animation is rewound.");
                    _animationPlayer.Rewind();
                }
                else
                {
                    _isInteractable.SetOverrideValue(false);
                    AccessAnimationPlayable = playable;
                    _animationPlayer = AnimationPlayer.PlayAnimation(playable.Animation, playable.StartTime, playable.PlaybackMode, playable.EasingMode, playable.TimeMode, playable.PlaybackSpeed);
                    _animationPlayer.OnComplete += OnAnimationComplete;
                    AccessAnimationPlaybackData = _animationPlayer.Data;
                }
                _onAccessAnimationComplete = onComplete;
                AccessState = AccessState.Opening;
                SetActive(true);
                Opening?.Invoke(this);
                OnOpen();
                return true;
            }
            return false;
        }

        public bool Open()
        {
            if (AccessState == AccessState.Closing || AccessState == AccessState.Closed || AccessState == AccessState.Opening)
            {
                if (AccessState == AccessState.Closing || AccessState == AccessState.Opening)
                {
                    Debug.Log("Open was called on a Window without an animation while already playing a state animation, " +
                        "this may cause unexpected behviour of the UI.");
                    _isInteractable.SetOverrideValue(true);

                    float time = 0.0F;
                    if (AccessState == AccessState.Closing) time = 0.0F;
                    else if (AccessState == AccessState.Opening) time = 1.0F;

                    _animationPlayer.SetCurrentTime(time);
                    _animationPlayer.Stop();
                    ClearAnimationReferences();
                    _onAccessAnimationComplete = null;
                }
                AccessAnimationPlayable = default;
                AccessState previousState = AccessState;
                AccessState = AccessState.Open;
                if (previousState != AccessState.Opening)
                {
                    SetActive(true);
                    Opening?.Invoke(this);
                    OnOpen();
                }                
                Opened?.Invoke(this);
                OnOpened();
                return true;
            }
            return false;
        }

        public bool Close(in AccessAnimationPlayable playable, IAccessibleAction onComplete = null)
        {
            if (AccessState == AccessState.Opening || AccessState == AccessState.Open)
            {
                if (_animationPlayer != null)
                {
                    Debug.Log("Window is already playing a open animation, the provided open animation is ignored and the current close animation is rewound.");
                    _animationPlayer.Rewind();
                }
                else
                {
                    _isInteractable.SetOverrideValue(false);
                    AccessAnimationPlayable = playable;
                    _animationPlayer = AnimationPlayer.PlayAnimation(playable.Animation, playable.StartTime, playable.PlaybackMode, playable.EasingMode, playable.TimeMode, playable.PlaybackSpeed);
                    _animationPlayer.OnComplete += OnAnimationComplete;
                    AccessAnimationPlaybackData = _animationPlayer.Data;
                }
                _onAccessAnimationComplete = onComplete;
                AccessState = AccessState.Closing;
                Closing?.Invoke(this);
                OnClose();
                return true;
            }
            return false;
        }

        public bool Close()
        {
            if (AccessState == AccessState.Opening || AccessState == AccessState.Open || AccessState == AccessState.Closing)
            {
                if (AccessState == AccessState.Opening || AccessState == AccessState.Closing)
                {
                    Debug.Log("Close was called on a Window without an animation while already playing a state animation, " +
                        "this may cause unexpected behviour of the UI.");
                    _isInteractable.SetOverrideValue(true);

                    float time = 0.0F;
                    if (AccessState == AccessState.Opening) time = 0.0F;
                    else if (AccessState == AccessState.Closing) time = 1.0F;

                    _animationPlayer.SetCurrentTime(time);
                    _animationPlayer.Stop();
                    ClearAnimationReferences();
                    _onAccessAnimationComplete = null;
                }
                AccessAnimationPlayable = default;
                AccessState previousState = AccessState;
                AccessState = AccessState.Closed;                
                if(previousState != AccessState.Closing)
                {
                    Closing?.Invoke(this);
                    OnClose();
                }                
                SetActive(false);
                Closed?.Invoke(this);
                OnClosed();
                return true;
            }
            return false;
        }

        public bool SkipAccessAnimation()
        {
            if (_animationPlayer != null)
            {
                _animationPlayer.Complete();
                return true;
            }
            return false;
        }

        // IDataRecipient
        public virtual void SetData(object data) { }

        public virtual bool IsValidData(object data) { return false; }

        // UIToolkitWindow
        public virtual bool ShowWaitingIndicator(bool show) { return false; }

        protected virtual void OnInitialize(VisualElement visualElement) { }

        protected virtual void OnOpen() { }
        protected virtual void OnOpened() { }
        protected virtual void OnClose() { }
        protected virtual void OnClosed() { }

        protected virtual void OnTerminate() { }

        private void OnAnimationComplete(IAnimation animation)
        {
            ResetAnimatedProperties();
            ClearAnimationReferences();
            _isInteractable.SetOverrideValue(true);
            if (AccessState == AccessState.Opening)
            {
                AccessState = AccessState.Open;
                Opened?.Invoke(this);
                OnOpened();
                IAccessibleAction action = _onAccessAnimationComplete;
                _onAccessAnimationComplete = null;
                action?.Invoke(this);
            }
            else if (AccessState == AccessState.Closing)
            {
                AccessState = AccessState.Closed;
                SetActive(false);
                Closed?.Invoke(this);
                OnClosed();
                IAccessibleAction action = _onAccessAnimationComplete;
                _onAccessAnimationComplete = null;
                action?.Invoke(this);
            }
            else
            {
                throw new Exception(string.Format("Window animation complete while in unexpected state: {0}", AccessState.ToString()));
            }
        }

        protected void SetActive(bool active)
        {
            _isActive = active;
            if(_visualElement != null)
            {
                _visualElement.style.display = active ? DisplayStyle.Flex : DisplayStyle.None;
            }            
        }

        protected void SetInteractable(VisualElement visualElement, bool interactable)
        {
            PickingMode pickingMode = interactable ? PickingMode.Position : PickingMode.Ignore;
            SetInteractablePickingMode(visualElement, pickingMode);
            visualElement.IterateHierarchy(delegate (VisualElement child)
            {
                SetInteractablePickingMode(child, pickingMode);
            });
        }

        private void SetSortOrder(int sortOrder)
        {
            VisualElement.Hierarchy parentHierarchy = _visualElement.parent.hierarchy;
            int newIndexInHierarchy = Mathf.Clamp(_sortOrder, 0, parentHierarchy.childCount - 1);
            int existingIndexInHierarchy = parentHierarchy.IndexOf(_visualElement);
            if (newIndexInHierarchy != existingIndexInHierarchy)
            {
                _visualElement.PlaceInFront(parentHierarchy.ElementAt(newIndexInHierarchy));
            }
            UIDocument.sortingOrder = _sortOrder;
            //UIDocument.panelSettings.sortingOrder = _sortOrder;
        }

        private void SetInteractablePickingMode(VisualElement visualElement, PickingMode pickingMode)
        {
            if (pickingMode == PickingMode.Position)
            {
                bool isNonInteractive = false;
                List<string> classList = visualElement.GetClasses() as List<string>;
                if (classList != null)
                {
                    isNonInteractive = classList.Contains(_NonInteractiveClassName);
                }
                else
                {
                    // Linq as fallback incase class list type changes in the future
                    isNonInteractive = visualElement.GetClasses().Contains(_NonInteractiveClassName);
                    Debug.LogWarning("VisualElement.GetClasses() no longer returns a List<string>.");
                }

                if (isNonInteractive)
                {
                    visualElement.pickingMode = PickingMode.Position;
                    visualElement.RemoveFromClassList(_NonInteractiveClassName);
                }
            }
            else if (visualElement.pickingMode == PickingMode.Position)
            {
                visualElement.pickingMode = PickingMode.Ignore;
                visualElement.AddToClassList(_NonInteractiveClassName);
            }
        }

        private void ClearAnimationReferences()
        {
            if(_animationPlayer != null)
            {
                _animationPlayer.OnComplete = null;
                _animationPlayer = null;
            }            
            AccessAnimationPlaybackData.ReleaseReferences();
        }

        protected virtual void OnDocumentEnabled() 
        {
            if(!IsValid())
            {
                throw new Exception("UI Toolkit Window is not valid during UIDocument OnEnable message broadcast.");
            }
        }

        protected virtual void OnDocumentDisabled() 
        {
            Terminate();
        }

        private void OnDocumentDestroyed()
        {
            Terminate();
        }
        
        private void OnIsEnabledUpdated(bool value)
        {
            if (value)
            {
                _isInteractable.SetOverrideValue(true);
                _visualElement.SetEnabled(true);
            }
            else
            {
                _isInteractable.SetOverrideValue(false);
                _visualElement.SetEnabled(false);
            }
        }
        
        private void OnIsInteractableUpdated(bool value)
        {
            if (value)
            {
                SetInteractable(_visualElement, true);
            }
            else
            {
                SetInteractable(_visualElement, false);
            }
        }
        
        private void OnIsHiddenUpdated(bool value)
        {
            _visualElement.style.display = value ? DisplayStyle.None : DisplayStyle.Flex;
        }
    }
}