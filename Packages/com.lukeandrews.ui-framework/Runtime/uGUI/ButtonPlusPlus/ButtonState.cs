using UnityEngine;
using UnityEngine.UI;

namespace UIFramework.UGUI
{
    [System.Serializable]
    public struct ButtonState
    {
        public string Name { get { return _name; } }
        [SerializeField] private string _name;

        public ColorBlock Colors { get { return _colors; } }
        [SerializeField] private ColorBlock _colors;

        public Sprite NormalSprite { get { return _normalSprite; } }
        [SerializeField] private Sprite _normalSprite;

        public SpriteState SpriteState { get { return _spriteState; } }
        [SerializeField] private SpriteState _spriteState;

        public AnimationTriggers AnimationTriggers { get { return _animationTriggers; } }
        [SerializeField] private AnimationTriggers _animationTriggers;

        public ButtonState(string name)
        {
            _name = name;
        }

        public ButtonState(string name, ColorBlock colors, Sprite normalSprite, SpriteState spriteState, AnimationTriggers animationTriggers)
        {
            _name = name;
            _colors = colors;
            _normalSprite = normalSprite;
            _spriteState = spriteState;
            _animationTriggers = animationTriggers;
        }
    }
}
