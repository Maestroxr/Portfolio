using System.Collections.Generic;
using UnityEngine;

namespace Portfolio.Asteroids
{
    /// <summary>Distances on a playfield that wraps around: the short way may lead across an edge.</summary>
    public static class FieldMath
    {
        /// <summary>
        /// Shortest offset from <paramref name="from"/> to <paramref name="to"/> on a field that repeats every
        /// <paramref name="period"/> meters per axis; an axis with a period of zero does not wrap.
        /// </summary>
        public static Vector2 Delta(Vector2 from, Vector2 to, Vector2 period)
        {
            Vector2 delta = to - from;
            delta.x = Shortest(delta.x, period.x);
            delta.y = Shortest(delta.y, period.y);
            return delta;
        }

        /// <summary>Index of the point closest to <paramref name="from"/> across the wrapping edges, or -1 when there is none.</summary>
        public static int Nearest(IReadOnlyList<Vector2> points, Vector2 from, Vector2 period)
        {
            int best = -1;
            float bestDistance = float.MaxValue;
            for (int i = 0; points != null && i < points.Count; i++)
            {
                float distance = Delta(from, points[i], period).sqrMagnitude;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = i;
                }
            }
            return best;
        }

        /// <summary>
        /// Where pilot <paramref name="slot"/> of <paramref name="pilots"/> starts a shared mission: alone in the middle,
        /// otherwise evenly on a circle around it, the first one on top.
        /// </summary>
        public static Vector2 StartPoint(int slot, int pilots, float radius)
        {
            if (pilots <= 1)
            {
                return Vector2.zero;
            }
            float angle = (90f - 360f * slot / pilots) * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
        }

        private static float Shortest(float delta, float period)
        {
            if (period <= 0f)
            {
                return delta;
            }
            delta = Mathf.Repeat(delta + period * 0.5f, period) - period * 0.5f;
            return delta;
        }
    }
}
