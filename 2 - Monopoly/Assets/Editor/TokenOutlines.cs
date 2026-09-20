using System.Collections.Generic;
using UnityEngine;

namespace Portfolio.Monopoly.EditorTools
{
    /// <summary>
    /// Side views of the tokens that are cut out and extruded (the Scottie dog, the cat, the T-Rex and the body of the
    /// race car): anchors in design units (x forward, y up) that a Catmull-Rom spline runs through. Sharp anchors make
    /// corners (ear tips, paws). <see cref="Outline"/> turns them into a dense counter-clockwise polygon; the same
    /// outlines draw the token badges of the interface.
    /// </summary>
    internal static class TokenOutlines
    {
        private const bool S = true;
        private const bool N = false;

        public static readonly (float x, float y, bool sharp)[] Dog =
        {
            (20, 0, S), (19, 16, N), (14, 34, N), (15, 50, N), (12, 60, N), (11, 72, N), (14, 86, S), (20, 70, N), (24, 58, N),
            (40, 59, N), (62, 59, N), (72, 63, N), (75, 74, N), (78, 90, N), (81, 99, S), (86, 86, N), (90, 78, N), (96, 76, N),
            (110, 71, N), (119, 68, S), (121, 60, N), (118, 50, N), (113, 42, N), (106, 34, S), (99, 40, N), (93, 34, N),
            (93, 20, N), (95, 8, N), (100, 0, S), (84, 0, S), (83, 13, N), (78, 20, N), (62, 19, N), (44, 19, N), (36, 17, N),
            (33, 10, N), (34, 0, S)
        };

        public static readonly (float x, float y, bool sharp)[] Cat =
        {
            (66, 0, S), (26, 0, N), (6, 1, N), (-8, 6, N), (-17, 16, N), (-18, 30, S), (-11, 21, N), (-2, 13, N), (4, 20, N),
            (6, 38, N), (12, 54, N), (24, 64, N), (35, 72, N), (37, 84, N), (39, 96, N), (42, 108, S), (50, 99, N), (58, 108, S),
            (63, 97, N), (67, 86, N), (66, 76, N), (61, 69, N), (58, 60, N), (60, 44, N), (62, 24, N), (63, 10, N)
        };

        public static readonly (float x, float y, bool sharp)[] Dino =
        {
            (0, 42, S), (18, 50, N), (40, 62, N), (56, 72, N), (70, 80, N), (80, 90, N), (92, 97, N), (110, 98, N), (126, 92, S),
            (127, 83, N), (122, 80, N), (126, 76, S), (118, 71, N), (104, 70, N), (95, 65, N), (91, 56, N), (95, 52, N),
            (103, 49, N), (106, 44, S), (98, 44, N), (90, 46, N), (84, 40, N), (82, 30, N), (86, 12, N), (98, 3, N), (100, 0, S),
            (74, 0, S), (72, 12, N), (68, 22, N), (60, 18, N), (58, 8, N), (62, 0, S), (40, 0, S), (42, 10, N), (44, 26, N),
            (34, 36, N), (16, 38, N)
        };

        public static readonly (float x, float y, bool sharp)[] CarBody =
        {
            (0, 22, S), (8, 29, N), (24, 33, N), (36, 35, N), (42, 38, S), (46, 36, N), (50, 44, S), (55, 44, S), (57, 37, N),
            (70, 35, N), (88, 32, N), (100, 29, N), (106, 25, S), (108, 15, S), (92, 11, S), (20, 11, S), (5, 15, N)
        };

        /// <summary>The closed outline through the anchors, counter-clockwise, <paramref name="samples"/> points per span.</summary>
        public static List<Vector2> Outline((float x, float y, bool sharp)[] anchors, int samples = 8)
        {
            var points = new List<Vector2>();
            int n = anchors.Length;
            for (int i = 0; i < n; i++)
            {
                var p0 = anchors[(i - 1 + n) % n];
                var p1 = anchors[i];
                var p2 = anchors[(i + 1) % n];
                var p3 = anchors[(i + 2) % n];
                Vector2 t1 = p1.sharp ? Vector2.zero : new Vector2(p2.x - p0.x, p2.y - p0.y) * 0.5f;
                Vector2 t2 = p2.sharp ? Vector2.zero : new Vector2(p3.x - p1.x, p3.y - p1.y) * 0.5f;
                int steps = p1.sharp && p2.sharp ? 1 : samples;
                for (int k = 0; k < steps; k++)
                {
                    float t = k / (float)steps;
                    float h00 = 2 * t * t * t - 3 * t * t + 1;
                    float h10 = t * t * t - 2 * t * t + t;
                    float h01 = -2 * t * t * t + 3 * t * t;
                    float h11 = t * t * t - t * t;
                    var point = new Vector2(
                        h00 * p1.x + h10 * t1.x + h01 * p2.x + h11 * t2.x,
                        h00 * p1.y + h10 * t1.y + h01 * p2.y + h11 * t2.y);
                    if (points.Count == 0 || (points[points.Count - 1] - point).sqrMagnitude > 0.16f)
                    {
                        points.Add(point);
                    }
                }
            }
            if (SignedArea(points) < 0f)
            {
                points.Reverse();
            }
            return points;
        }

        public static float SignedArea(IList<Vector2> polygon)
        {
            float area = 0f;
            for (int i = 0; i < polygon.Count; i++)
            {
                Vector2 a = polygon[i];
                Vector2 b = polygon[(i + 1) % polygon.Count];
                area += a.x * b.y - b.x * a.y;
            }
            return area * 0.5f;
        }
    }
}
