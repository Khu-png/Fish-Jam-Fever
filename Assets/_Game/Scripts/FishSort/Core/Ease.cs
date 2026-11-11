using UnityEngine;

namespace FishSort
{
    // Small easing set used by coroutine tweens (project has no tween library).
    public static class Ease
    {
        private const float BACK_OVERSHOOT = 1.70158f;
        private const float BACK_OVERSHOOT_PLUS_ONE = BACK_OVERSHOOT + 1f;
        private const float HALF = 0.5f;

        public static float InOutCubic(float t)
        {
            return t < HALF ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) * HALF;
        }

        // Gentle start and finish with no flat spot in between.
        public static float InOutSine(float t)
        {
            return (1f - Mathf.Cos(t * Mathf.PI)) * HALF;
        }

        public static float OutCubic(float t)
        {
            float u = 1f - t;
            return 1f - u * u * u;
        }

        public static float OutQuad(float t)
        {
            return 1f - (1f - t) * (1f - t);
        }

        public static float InQuad(float t)
        {
            return t * t;
        }

        public static float OutBack(float t)
        {
            float p = t - 1f;
            return 1f + BACK_OVERSHOOT_PLUS_ONE * p * p * p + BACK_OVERSHOOT * p * p;
        }

        // 0 -> 1 -> 0 bump, used for punch scales.
        public static float Punch(float t)
        {
            return Mathf.Sin(t * Mathf.PI);
        }
    }
}
