using System;
using System.Collections;
using UnityEngine;

namespace Portfolio.Monopoly
{
    /// <summary>Easing curves and coroutine tweens for the board and the interface.</summary>
    public static class Tween
    {
        public static float OutCubic(float t)
        {
            t = Mathf.Clamp01(t) - 1f;
            return t * t * t + 1f;
        }

        public static float InOutCubic(float t)
        {
            t = Mathf.Clamp01(t);
            return t < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f;
        }

        public static float OutBack(float t, float overshoot = 1.70158f)
        {
            t = Mathf.Clamp01(t) - 1f;
            return t * t * ((overshoot + 1f) * t + overshoot) + 1f;
        }

        public static float OutQuad(float t)
        {
            t = Mathf.Clamp01(t);
            return 1f - (1f - t) * (1f - t);
        }

        public static float InQuad(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t;
        }

        public static float InOutSine(float t)
        {
            return -(Mathf.Cos(Mathf.PI * Mathf.Clamp01(t)) - 1f) / 2f;
        }

        /// <summary>A bouncy settle: overshoots and wobbles back to 1.</summary>
        public static float OutElastic(float t)
        {
            t = Mathf.Clamp01(t);
            if (t <= 0f || t >= 1f)
            {
                return t;
            }
            return Mathf.Pow(2f, -10f * t) * Mathf.Sin((t * 10f - 0.75f) * (2f * Mathf.PI / 3f)) + 1f;
        }

        /// <summary>Runs <paramref name="step"/> from 0 to 1 over <paramref name="duration"/> seconds.</summary>
        public static IEnumerator Run(float duration, Action<float> step, bool unscaled = false)
        {
            if (duration <= 0f)
            {
                step(1f);
                yield break;
            }
            float time = 0f;
            while (time < duration)
            {
                step(time / duration);
                yield return null;
                time += unscaled ? Time.unscaledDeltaTime : Time.deltaTime;
            }
            step(1f);
        }

        /// <summary>Waits game time, or unscaled time for interface animations that play while paused.</summary>
        public static IEnumerator Wait(float seconds, bool unscaled = false)
        {
            float time = 0f;
            while (time < seconds)
            {
                yield return null;
                time += unscaled ? Time.unscaledDeltaTime : Time.deltaTime;
            }
        }

        /// <summary>A point on a jump from <paramref name="from"/> to <paramref name="to"/> that rises <paramref name="height"/>.</summary>
        public static Vector3 Arc(Vector3 from, Vector3 to, float height, float t)
        {
            Vector3 point = Vector3.Lerp(from, to, t);
            point.y += 4f * height * t * (1f - t);
            return point;
        }
    }
}
