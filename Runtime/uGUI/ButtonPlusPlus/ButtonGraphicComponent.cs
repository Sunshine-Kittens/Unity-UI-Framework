using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

namespace UIFramework.UGUI
{
    public class ButtonGraphicComponent : MonoBehaviour, IButtonComponent
    {
        [SerializeField] private Graphic _targetGraphic;

        [SerializeField] private Selectable.Transition _transition = Selectable.Transition.ColorTint;

        [SerializeField] private ColorBlock _colors = ColorBlock.defaultColorBlock;

        [SerializeField] private Sprite _normalSprite = null;
        [SerializeField] private SpriteState _spriteState = default;

        public AnimationTriggers AnimationTriggers { get { return _animationTriggers; } }
        [SerializeField] private AnimationTriggers _animationTriggers = new AnimationTriggers();

        [SerializeField] private ButtonState[] _states = null;

        public string State { get; private set; } = ButtonState.DefaultStateName;

        private Dictionary<string, int> _stateMap = null;
        private int _stateIndex = -1;
        private SelectionState _selectionState = SelectionState.Normal;

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
                    return _colors;
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
                    return _spriteState;
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
                    return _animationTriggers;
                }
            }
        }

        public void DoStateTransition(SelectionState state, bool instant)
        {
            _selectionState = state;

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

            switch (_transition)
            {
                case Selectable.Transition.ColorTint:
                    StartColorTween(tintColor * _activeColors.colorMultiplier, instant);
                    break;
                case Selectable.Transition.SpriteSwap:
                    DoSpriteSwap(transitionSprite);
                    break;
                case Selectable.Transition.Animation:
                    TriggerAnimation(triggerName);
                    break;
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
                }
            }
            else
            {
                _stateIndex = -1;
                State = state;
            }
        }

        private void StartColorTween(Color targetColor, bool instant)
        {
            if (_targetGraphic != null)
            {
                _targetGraphic.CrossFadeColor(targetColor, instant ? 0f : _activeColors.fadeDuration, true, true);
            }            
        }

        private void DoSpriteSwap(Sprite newSprite)
        {
            Image image = _targetGraphic as Image;
            if (image != null)
            {
                image.overrideSprite = newSprite;
            }            
        }

        private void TriggerAnimation(string triggername)
        {
            if(_transition == Selectable.Transition.Animation)
            {
                Animator animator = GetComponent<Animator>();
                if (animator != null && animator.isActiveAndEnabled && animator.hasBoundPlayables && !string.IsNullOrEmpty(triggername))
                {
                    animator.ResetTrigger(_animationTriggers.normalTrigger);
                    animator.ResetTrigger(_animationTriggers.highlightedTrigger);
                    animator.ResetTrigger(_animationTriggers.pressedTrigger);
                    animator.ResetTrigger(_animationTriggers.selectedTrigger);
                    animator.ResetTrigger(_animationTriggers.disabledTrigger);

                    for(int i = 0; i < _states.Length; i++)
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
    }
}
