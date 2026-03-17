//==============================================================================
// @Author:   Gary Yang
// @File:     Easing Data
// @brief:    interpolation functions for various easing math translating
//              directly from LERP 
// @copyright MIT 
//==============================================================================


using System;
using System.Collections.Generic;
using UnityEngine;

namespace Utils
{
    public enum EasingType
    {
        Linear, 
        EaseInQuad,
        EaseOutQuad,
        EaseInOutQuad,
        EaseInCubic,
        EaseOutCubic,
        EaseInOutCubic,
        EaseOutElastic,
        EaseInOutElastic,
        EaseOutBounce,
        EaseInBounce,
        EaseInOutBounce,
        EaseInBack,
        EaseInSine,
        EaseOutSine,
        EaseInOutSine,
        EaseInExpo,
        EaseOutExpo,
        EaseInOutExpo,
        EaseInCirc,
        EaseOutCirc,
        EaseInOutCirc
    }
    
    [CreateAssetMenu(menuName = "GunShip/Utils/EasingData")]
    public class EasingData :  ScriptableObject
    {
        // Map each easing enum value to the corresponding easing math function.
        private static readonly IReadOnlyDictionary<EasingType, Func<float, float>> _evaluators =
            new Dictionary<EasingType, Func<float, float>>
            {
                { EasingType.Linear, Easing.Linear },
                { EasingType.EaseInQuad, Easing.EaseInQuad },
                { EasingType.EaseOutQuad, Easing.EaseOutQuad },
                { EasingType.EaseInOutQuad, Easing.EaseInOutQuad },
                { EasingType.EaseInCubic, Easing.EaseInCubic },
                { EasingType.EaseOutCubic, Easing.EaseOutCubic },
                { EasingType.EaseInOutCubic, Easing.EaseInOutCubic },
                { EasingType.EaseOutElastic, Easing.EaseOutElastic },
                { EasingType.EaseInOutElastic, Easing.EaseInOutElastic },
                { EasingType.EaseOutBounce, Easing.EaseOutBounce },
                { EasingType.EaseInBounce, Easing.EaseInBounce },
                { EasingType.EaseInOutBounce, Easing.EaseInOutBounce },
                { EasingType.EaseInBack, Easing.EaseInBack },
                { EasingType.EaseInSine, Easing.EaseInSine },
                { EasingType.EaseOutSine, Easing.EaseOutSine },
                { EasingType.EaseInOutSine, Easing.EaseInOutSine },
                { EasingType.EaseInExpo, Easing.EaseInExpo },
                { EasingType.EaseOutExpo, Easing.EaseOutExpo },
                { EasingType.EaseInOutExpo, Easing.EaseInOutExpo },
                { EasingType.EaseInCirc, Easing.EaseInCirc },
                { EasingType.EaseOutCirc, Easing.EaseOutCirc },
                { EasingType.EaseInOutCirc, Easing.EaseInOutCirc }
            };

        [SerializeField]
        private EasingType easingType = EasingType.Linear;

        public EasingType Type => easingType;

        public float Evaluate(float t) => Evaluate(easingType, t);

        public static float Evaluate(EasingType type, float t)
        {
            if (_evaluators.TryGetValue(type, out var evaluator))
            {
                return evaluator(t);
            }

            Debug.LogWarning($"Easing type {type} is not mapped. Falling back to Linear.");
            return _evaluators[EasingType.Linear](t);
        }

        public static bool TryGetEvaluator(EasingType type, out Func<float, float> evaluator) =>
            _evaluators.TryGetValue(type, out evaluator);

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!_evaluators.ContainsKey(easingType))
            {
                Debug.LogWarning($"Easing type {easingType} is missing evaluator. Resetting to Linear.");
                easingType = EasingType.Linear;
            }
        }
#endif
    }
}