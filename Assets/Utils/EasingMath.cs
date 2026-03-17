//==============================================================================
// @Author:   Gary Yang
// @File:     Interpolation_easing.cs
// @brief:    interpolation functions for various easing math translating
//              directly from LERP
// @copyright MIT 
//==============================================================================



namespace Utils
{
//  just some functions I can shove into the SRT actions instead of lerp-ing
//  Utility class for easing functions

using UnityEngine;

public static class Easing
{
    
    public static float Linear(float t) => t;
    
    public static float EaseInQuad(float t) => t * t;
    
    public static float EaseOutQuad(float t)
    {
        return -(t * (t - 2));
    }
    
    public static float EaseInOutQuad(float t)
    {
        t *= 2;
        if (t < 1) return 0.5f * t * t;
        t -= 1;
        return -0.5f * (t * (t - 2) - 1);
    }
    
    public static float EaseInCubic(float t) => t * t * t;
    
    public static float EaseOutCubic(float t)
    {
        t -= 1;
        return t * t * t + 1;
    }
    
    public static float EaseInOutCubic(float t)
    {
        t *= 2;
        if (t < 1) return 0.5f * t * t * t;
        t -= 2;
        return 0.5f * (t * t * t + 2);
    }
    
    public static float EaseOutElastic(float t)
    {
        if (t == 0f) return 0f;
        if (t == 1f) return 1f;

        // The "period" determines how fast the oscillation is; 0.3 is common.
        const float p = 0.3f;

        // This shift (s) helps align the sine wave so it ends cleanly at t = 1.
        float s = p / 4f;

        // This is the classic formula:
        return   Mathf.Pow(2f, -10f * t) 
               * Mathf.Sin((t - s) * (2f * Mathf.PI) / p) 
               + 1f;
    }
    
    // Configurable version with defaults tuned for smoothness
    public static float EaseOutElastic(float x, float decayRate = 6f, float oscillationSpeed = 6f, float frequency = 4f)
    {
        // Handle edge cases
        if (x == 0f) return 0f;
        if (x == 1f) return 1f;
        
        float c4 = (2f * Mathf.PI) / frequency;
        
        // Customizable decay
        float decay = Mathf.Pow(2f, -decayRate * x);
        
        // Customizable oscillation
        float wave = Mathf.Sin((x * oscillationSpeed - 0.75f) * c4);
        
        float result = decay * wave + 1f;
        
        return result;
    }
    
    // ADDITIONAL EASING FUNCTIONS
    
    // Elastic easing (inward)
    public static float EaseInElastic(float t)
    {
        const float c4 = (2 * Mathf.PI) / 3;
        
        if (t == 0) return 0;
        if (t == 1) return 1;
        return -Mathf.Pow(2, 10 * t - 10) * Mathf.Sin((t * 10 - 10.75f) * c4);
    }
    
    public static float EaseInOutElastic(float t)
    {
        const float c5 = (2 * Mathf.PI) / 4.5f;
        
        if (t == 0) return 0;
        if (t == 1) return 1;
        if (t < 0.5f)
            return -(Mathf.Pow(2, 20 * t - 10) * Mathf.Sin((20 * t - 11.125f) * c5)) / 2;
        return (Mathf.Pow(2, -20 * t + 10) * Mathf.Sin((20 * t - 11.125f) * c5)) / 2 + 1;
    }
    
    // Bounce easing
    public static float EaseOutBounce(float t)
    {
        const float n1 = 7.5625f;
        const float d1 = 2.75f;
        
        if (t < 1 / d1)
        {
            return n1 * t * t;
        }
        else if (t < 2 / d1)
        {
            return n1 * (t -= 1.5f / d1) * t + 0.75f;
        }
        else if (t < 2.5 / d1)
        {
            return n1 * (t -= 2.25f / d1) * t + 0.9375f;
        }
        else
        {
            return n1 * (t -= 2.625f / d1) * t + 0.984375f;
        }
    }
    
    public static float EaseInBounce(float t) => 1 - EaseOutBounce(1 - t);
    
    public static float EaseInOutBounce(float t) => 
        t < 0.5f
            ? (1 - EaseOutBounce(1 - 2 * t)) / 2
            : (1 + EaseOutBounce(2 * t - 1)) / 2;
    
    // Back easing (overshooting)
    public static float EaseInBack(float t)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1;
        
        return c3 * t * t * t - c1 * t * t;
    }
    
    public static float EaseOutBack(float t)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1;
        
        return 1 + c3 * Mathf.Pow(t - 1, 3) + c1 * Mathf.Pow(t - 1, 2);
    }
    
    public static float EaseInOutBack(float t)
    {
        const float c1 = 1.70158f;
        const float c2 = c1 * 1.525f;
        
        return t < 0.5f
            ? (Mathf.Pow(2 * t, 2) * ((c2 + 1) * 2 * t - c2)) / 2
            : (Mathf.Pow(2 * t - 2, 2) * ((c2 + 1) * (t * 2 - 2) + c2) + 2) / 2;
    }
    
    // Sine easing
    public static float EaseInSine(float t) => 1 - Mathf.Cos(t * Mathf.PI / 2);
    public static float EaseOutSine(float t) => Mathf.Sin(t * Mathf.PI / 2);
    public static float EaseInOutSine(float t) => -(Mathf.Cos(Mathf.PI * t) - 1) / 2;
    
    // Exponential easing
    public static float EaseInExpo(float t) => t == 0 ? 0 : Mathf.Pow(2, 10 * t - 10);
    public static float EaseOutExpo(float t) => t == 1 ? 1 : 1 - Mathf.Pow(2, -10 * t);
    public static float EaseInOutExpo(float t)
    {
        if (t == 0) return 0;
        if (t == 1) return 1;
        return t < 0.5
            ? Mathf.Pow(2, 20 * t - 10) / 2
            : (2 - Mathf.Pow(2, -20 * t + 10)) / 2;
    }
    
    // Circular easing
    public static float EaseInCirc(float t) => 1 - Mathf.Sqrt(1 - Mathf.Pow(t, 2));
    public static float EaseOutCirc(float t) => Mathf.Sqrt(1 - Mathf.Pow(t - 1, 2));
    public static float EaseInOutCirc(float t)
    {
        return t < 0.5
            ? (1 - Mathf.Sqrt(1 - Mathf.Pow(2 * t, 2))) / 2
            : (Mathf.Sqrt(1 - Mathf.Pow(-2 * t + 2, 2)) + 1) / 2;
    }
}
}