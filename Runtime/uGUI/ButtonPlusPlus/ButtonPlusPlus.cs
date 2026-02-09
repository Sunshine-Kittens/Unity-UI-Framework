using System.Collections.Generic;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UIFramework.UGUI
{
    public class ButtonPlusPlus : Button, IButtonState
    {
        [SerializeField] private Sprite _normalSprite = null;
        [SerializeField] private ButtonState[] _states = null;

        public string State { get; private set; } = ButtonState.DefaultStateName;

        private IButtonComponent[] _buttonComponents = null;
        private Dictionary<string, int> _stateMap = null;
        private int _stateIndex = -1;

        private ColorBlock _activeColors
        {
            get
            {
                if (_stateIndex >= 0)
                {
                    return _states[_stateIndex].Colors;
                }
                else
                {
                    return colors;
                }
            }
        }

        private Sprite _activeNormalSprite
        {
            get
            {
                if (_stateIndex >= 0)
                {
                    return _states[_stateIndex].NormalSprite;
                }
                else
                {
                    return _normalSprite;
                }
            }
        }

        private SpriteState _activeSpriteState
        {
            get
            {
                if (_stateIndex >= 0)
                {
                    return _states[_stateIndex].SpriteState;
                }
                else
                {
                    return spriteState;
                }
            }
        }

        private AnimationTriggers _activeAnimationTriggers
        {
            get
            {
                if (_stateIndex >= 0)
                {
                    return _states[_stateIndex].AnimationTriggers;
                }
                else
                {
                    return animationTriggers;
                }
            }
        }

        public void ResetButtonState()
        {
            SetButtonState(ButtonState.DefaultStateName);
        }

        public void SetButtonState(string state)
        {
            if (state != ButtonState.DefaultStateName)
            {
                Dictionary<string, int> stateMap = GetStateMap();
                int index;
                if (stateMap.TryGetValue(state, out index))
                {
                    _stateIndex = index;
                    State = state;
                    IButtonComponent[] buttonComponents = GetButtonComponents();
                    for (int i = 0; i < buttonComponents.Length; i++)
                    {
                        buttonComponents[i].SetButtonState(state);
                    }
                }
            }
            else
            {
                _stateIndex = -1;
                State = state;
                IButtonComponent[] buttonComponents = GetButtonComponents();
                for (int i = 0; i < buttonComponents.Length; i++)
                {
                    buttonComponents[i].SetButtonState(state);
                }
            }
            DoStateTransition(currentSelectionState, false);
        }

        private Dictionary<string, int> GetStateMap()
        {
            if (_stateMap == null)
            {
                _stateMap = new Dictionary<string, int>();
                for (int i = 0; i < _states.Length; i++)
                {
                    _stateMap.Add(_states[i].Name, i);
                }
            }
            return _stateMap;
        }

        private IButtonComponent[] GetButtonComponents()
        {
            if(_buttonComponents == null)
            {
                _buttonComponents = GetComponentsInChildren<IButtonComponent>();
            }
            return _buttonComponents;
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            // OnValidate can be called before OnEnable, this makes it unsafe to access other components
            // since they might not have been initialized yet.
            // OnSetProperty potentially access Animator or Graphics. (case 618186)
            if (isActiveAndEnabled)
            {
                if (!interactable && EventSystem.current != null && EventSystem.current.currentSelectedGameObject == gameObject)
                    EventSystem.current.SetSelectedGameObject(null);
                // Need to clear out the override image on the target...
                DoSpriteSwap(_activeNormalSprite);

                // If the transition mode got changed, we need to clear all the transitions, since we don't know what the old transition mode was.
                StartColorTween(Color.white, true);
                TriggerAnimation(_activeAnimationTriggers.normalTrigger);

                // And now go to the right state.
                DoStateTransition(currentSelectionState, true);
            }
        }

#endif // if UNITY_EDITOR

        protected override void DoStateTransition(SelectionState state, bool instant)
        {
            if (!gameObject.activeInHierarchy)
                return;

            Color tintColor;
            Sprite transitionSprite;
            string triggerName;

            switch (state)
            {
                case SelectionState.Normal:
                    tintColor = _activeColors.normalColor;
                    transitionSprite = _activeNormalSprite;
                    triggerName = _activeAnimationTriggers.normalTrigger;
                    break;
                case SelectionState.Highlighted:
                    tintColor = _activeColors.highlightedColor;
                    transitionSprite = _activeSpriteState.highlightedSprite;
                    triggerName = _activeAnimationTriggers.highlightedTrigger;
                    break;
                case SelectionState.Pressed:
                    tintColor = _activeColors.pressedColor;
                    transitionSprite = _activeSpriteState.pressedSprite;
                    triggerName = _activeAnimationTriggers.pressedTrigger;
                    break;
                case SelectionState.Selected:
                    tintColor = _activeColors.selectedColor;
                    transitionSprite = _activeSpriteState.selectedSprite;
                    triggerName = _activeAnimationTriggers.selectedTrigger;
                    break;
                case SelectionState.Disabled:
                    tintColor = _activeColors.disabledColor;
                    transitionSprite = _activeSpriteState.disabledSprite;
                    triggerName = _activeAnimationTriggers.disabledTrigger;
                    break;
                default:
                    tintColor = Color.black;
                    transitionSprite = null;
                    triggerName = string.Empty;
                    break;
            }

            switch (transition)
            {
                case Transition.ColorTint:
                    StartColorTween(tintColor * _activeColors.colorMultiplier, instant);
                    break;
                case Transition.SpriteSwap:
                    DoSpriteSwap(transitionSprite);
                    break;
                case Transition.Animation:
                    TriggerAnimation(triggerName);
                    break;
            }

            IButtonComponent[] buttonComponents = GetButtonComponents();
            for(int i = 0; i < buttonComponents.Length; i++)
            {
                buttonComponents[i].DoStateTransition((UGUI.SelectionState)state, instant);
            }
        }

        private void StartColorTween(Color targetColor, bool instant)
        {
            if (targetGraphic == null)
                return;

            targetGraphic.CrossFadeColor(targetColor, instant ? 0f : _activeColors.fadeDuration, true, true);
        }

        private void DoSpriteSwap(Sprite newSprite)
        {
            if (image == null)
                return;

            image.overrideSprite = newSprite;
        }

        private void TriggerAnimation(string triggername)
        {
            if (transition != Transition.Animation || animator == null || !animator.isActiveAndEnabled || !animator.hasBoundPlayables || string.IsNullOrEmpty(triggername))
                return;

            animator.ResetTrigger(animationTriggers.normalTrigger);
            animator.ResetTrigger(animationTriggers.highlightedTrigger);
            animator.ResetTrigger(animationTriggers.pressedTrigger);
            animator.ResetTrigger(animationTriggers.selectedTrigger);
            animator.ResetTrigger(animationTriggers.disabledTrigger);

            for (int i = 0; i < _states.Length; i++)
            {
                animator.ResetTrigger(_states[i].AnimationTriggers.normalTrigger);
                animator.ResetTrigger(_states[i].AnimationTriggers.highlightedTrigger);
                animator.ResetTrigger(_states[i].AnimationTriggers.pressedTrigger);
                animator.ResetTrigger(_states[i].AnimationTriggers.selectedTrigger);
                animator.ResetTrigger(_states[i].AnimationTriggers.disabledTrigger);
            }

            animator.SetTrigger(triggername);
        }
    }
}
