namespace UIFramework
{
    public enum EasingMode
    {
        Ease,
        EaseIn,
        EaseOut,
        EaseInOut,
        Linear,
        EaseInSine,
        EaseOutSine,
        EaseInOutSine,
        EaseInCubic,
        EaseOutCubic,
        EaseInOutCubic,
        EaseInCirc,
        EaseOutCirc,
        EaseInOutCirc,
        EaseInElastic,
        EaseOutElastic,
        EaseInOutElastic,
        EaseInBack,
        EaseOutBack,
        EaseInOutBack,
        EaseInBounce,
        EaseOutBounce,
        EaseInOutBounce
    }

    public static class Easing
    {
        public static EasingMode GetInverseEasingMode(EasingMode mode)
        {
            switch (mode)
            {
                default:
                    return mode;
                case EasingMode.EaseIn:
                    return EasingMode.EaseOut;
                case EasingMode.EaseOut:
                    return EasingMode.EaseIn;
                case EasingMode.EaseInSine:
                    return EasingMode.EaseOutSine;
                case EasingMode.EaseOutSine:
                    return EasingMode.EaseInSine;
                case EasingMode.EaseInCubic:
                    return EasingMode.EaseOutCubic;
                case EasingMode.EaseOutCubic:
                    return EasingMode.EaseInCubic;
                case EasingMode.EaseInCirc:
                    return EasingMode.EaseOutCirc;
                case EasingMode.EaseOutCirc:
                    return EasingMode.EaseInCirc;
                case EasingMode.EaseInElastic:
                    return EasingMode.EaseOutElastic;
                case EasingMode.EaseOutElastic:
                    return EasingMode.EaseInElastic;
                case EasingMode.EaseInBack:
                    return EasingMode.EaseOutBack;
                case EasingMode.EaseOutBack:
                    return EasingMode.EaseInBack;
                case EasingMode.EaseInBounce:
                    return EasingMode.EaseOutBounce;
                case EasingMode.EaseOutBounce:
                    return EasingMode.EaseInBounce;
            }
        }

        public static float PerformEase(float t, EasingMode mode)
        {
            switch (mode)
            {
                default:
                case EasingMode.Ease:
                    // "Best-fit" cubic curve trying to match start/end points and derivatives (stays within 0.079 of the exact curve).
                    // y = a t^3 + b t^2 + c t + d, where a = -0.2, b = -0.6, c = 1.8, d = 0
                    // Should be equivalent to cubic-bezier(0.25,0.1,0.25,1)
                    return t * (1.8f + t * (-0.6f + t * -0.2f));
                case EasingMode.EaseIn:
                    // Should be equivalent to cubic-bezier(0.42,0,1,1)
                    return InQuad(t);
                case EasingMode.EaseOut:
                    // Should be equivalent to cubic-bezier(0,0,0.58,1)
                    return OutQuad(t);
                case EasingMode.EaseInOut:
                    // Should be equivalent to cubic-bezier(0.42,0,0.58,1)
                    return InOutQuad(t);
                case EasingMode.Linear:
                    // Should be equivalent to cubic-bezier(0,0,1,1)
                    return Linear(t);
                case EasingMode.EaseInSine:
                    return InSine(t);
                case EasingMode.EaseOutSine:
                    return OutSine(t);
                case EasingMode.EaseInOutSine:
                    return InOutSine(t);
                case EasingMode.EaseInCubic:
                    return InCubic(t);
                case EasingMode.EaseOutCubic:
                    return OutCubic(t);
                case EasingMode.EaseInOutCubic:
                    return InOutCubic(t);
                case EasingMode.EaseInCirc:
                    return InCirc(t);
                case EasingMode.EaseOutCirc:
                    return OutCirc(t);
                case EasingMode.EaseInOutCirc:
                    return InOutCirc(t);
                case EasingMode.EaseInElastic:
                    return InElastic(t);
                case EasingMode.EaseOutElastic:
                    return OutElastic(t);
                case EasingMode.EaseInOutElastic:
                    return InOutElastic(t);
                case EasingMode.EaseInBack:
                    return InBack(t);
                case EasingMode.EaseOutBack:
                    return OutBack(t);
                case EasingMode.EaseInOutBack:
                    return InOutBack(t);
                case EasingMode.EaseInBounce:
                    return InBounce(t);
                case EasingMode.EaseOutBounce:
                    return OutBounce(t);
                case EasingMode.EaseInOutBounce:
                    return InOutBounce(t);
            }
        }

        private static float Linear(float t)
        {
            return t;
        }

        private static float InSine(float t)
        {
            return UnityEngine.UIElements.Experimental.Easing.InSine(t);
        }

        private static float OutSine(float t)
        {
            return UnityEngine.UIElements.Experimental.Easing.OutSine(t);
        }

        private static float InOutSine(float t)
        {
            return UnityEngine.UIElements.Experimental.Easing.InOutSine(t);
        }

        private static float InQuad(float t)
        {
            return UnityEngine.UIElements.Experimental.Easing.InQuad(t);
        }

        private static float OutQuad(float t)
        {
            return UnityEngine.UIElements.Experimental.Easing.OutQuad(t);
        }

        private static float InOutQuad(float t)
        {
            return UnityEngine.UIElements.Experimental.Easing.InOutQuad(t);
        }

        private static float InCubic(float t)
        {
            return UnityEngine.UIElements.Experimental.Easing.InCubic(t);
        }

        private static float OutCubic(float t)
        {
            return UnityEngine.UIElements.Experimental.Easing.OutCubic(t);
        }

        private static float InOutCubic(float t)
        {
            return UnityEngine.UIElements.Experimental.Easing.InOutCubic(t);
        }

        private static float InBounce(float t)
        {
            return UnityEngine.UIElements.Experimental.Easing.InBounce(t);
        }

        private static float OutBounce(float t)
        {
            return UnityEngine.UIElements.Experimental.Easing.OutBounce(t);
        }

        private static float InOutBounce(float t)
        {
            return UnityEngine.UIElements.Experimental.Easing.InOutBounce(t);
        }

        private static float InElastic(float t)
        {
            return UnityEngine.UIElements.Experimental.Easing.InElastic(t);
        }

        private static float OutElastic(float t)
        {
            return UnityEngine.UIElements.Experimental.Easing.OutElastic(t);
        }

        private static float InOutElastic(float t)
        {
            return UnityEngine.UIElements.Experimental.Easing.InOutElastic(t);
        }

        private static float InBack(float t)
        {
            return UnityEngine.UIElements.Experimental.Easing.InBack(t);
        }

        private static float OutBack(float t)
        {
            return UnityEngine.UIElements.Experimental.Easing.OutBack(t);
        }

        private static float InOutBack(float t)
        {
            return UnityEngine.UIElements.Experimental.Easing.InOutBack(t);
        }

        private static float InCirc(float t)
        {
            return UnityEngine.UIElements.Experimental.Easing.InCirc(t);
        }

        private static float OutCirc(float t)
        {
            return UnityEngine.UIElements.Experimental.Easing.OutCirc(t);
        }

        private static float InOutCirc(float t)
        {
            return UnityEngine.UIElements.Experimental.Easing.InOutCirc(t);
        }
    }
}
