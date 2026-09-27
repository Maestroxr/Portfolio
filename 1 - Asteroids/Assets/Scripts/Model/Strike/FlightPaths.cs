using System.Collections.Generic;
using UnityEngine;

namespace Portfolio.Asteroids
{
    /// <summary>
    /// The screen-space paths of the air units: waypoint lists in normalised field coordinates (x -1 at the left edge to
    /// 1 at the right, y -1 at the bottom to 1 at the top; beyond 1 is off screen), entering from the top unless the path
    /// says otherwise, with the flight type each path defaults to and the waypoint a repeating path loops back to. Pure
    /// data: deterministic, the same on every client.
    /// </summary>
    public static class FlightPaths
    {
        /// <summary>How far outside the field (normalised) a path starts, so even a transport is fully off screen.</summary>
        public const float Outside = 1.4f;

        /// <summary>How far outside the field (normalised) a path that enters from a side starts.</summary>
        public const float OutsideSide = 1.25f;

        private static readonly Dictionary<int, FlightCurve> curves = new Dictionary<int, FlightCurve>();

        /// <summary>Every path in enum order.</summary>
        public static FlightPath[] All =>
            new[]
            {
                FlightPath.StraightDown, FlightPath.DiveLeft, FlightPath.Swoop, FlightPath.Zigzag, FlightPath.CrossLeft, FlightPath.SweepDown,
                FlightPath.HoverTop, FlightPath.HoverMid, FlightPath.RiseUp, FlightPath.Arc, FlightPath.Spiral, FlightPath.Strafe
            };


        /// <summary>The waypoints of <paramref name="path"/> in normalised field coordinates (unmirrored, reference point at x = 0).</summary>
        public static Vector2[] Normalised(FlightPath path)
        {
            switch (path)
            {
                case FlightPath.DiveLeft:
                    // Enters a little right of the reference point and dives out at the lower left.
                    return Points(0.35f, Outside, 0.25f, 0.7f, 0f, 0.1f, -0.4f, -0.6f, -0.8f, -Outside);
                case FlightPath.Swoop:
                    // Dives down the middle, turns low and climbs out of the right side.
                    return Points(0f, Outside, 0f, 0.5f, 0.08f, -0.2f, 0.3f, -0.45f, 0.55f, -0.25f, 0.8f, 0.3f, OutsideSide, 0.8f);
                case FlightPath.Zigzag:
                    return Points(0f, Outside, 0.3f, 0.9f, -0.3f, 0.45f, 0.3f, 0f, -0.3f, -0.45f, 0.3f, -0.9f, 0f, -Outside);
                case FlightPath.CrossLeft:
                    // Enters from the right side high up and crosses to the left, sinking a little.
                    return Points(OutsideSide, 0.75f, 0.6f, 0.6f, 0f, 0.5f, -0.6f, 0.4f, -OutsideSide, 0.25f);
                case FlightPath.SweepDown:
                    // A wide S from the left half down through the middle and out at the bottom.
                    return Points(-0.6f, Outside, -0.6f, 0.5f, -0.2f, 0.1f, 0.4f, 0f, 0.6f, -0.5f, 0.3f, -1f, 0.1f, -Outside);
                case FlightPath.HoverTop:
                    // Comes down the left of the reference point into a loop of about 8 x 5 m in the upper screen, which it
                    // flies anticlockwise (its left side downwards, so it joins the loop without a turn; loops from the second waypoint).
                    return Points(-0.22f, Outside, -0.22f, 0.62f, 0f, 0.37f, 0.22f, 0.62f, 0f, 0.87f, -0.22f, 0.62f);
                case FlightPath.HoverMid:
                    // Comes down to the middle and circles there like HoverTop, lower (loops from the third waypoint).
                    return Points(-0.22f, Outside, -0.22f, 0.6f, -0.22f, 0.1f, 0f, -0.15f, 0.22f, 0.1f, 0f, 0.35f, -0.22f, 0.1f);
                case FlightPath.RiseUp:
                    // Comes from behind: rises from the bottom edge and leaves at the top.
                    return Points(0.15f, -Outside, 0.1f, -0.5f, 0f, 0.4f, 0f, Outside);
                case FlightPath.Arc:
                    // Down the left, across the middle and back up out of the top at the right.
                    return Points(-0.7f, Outside, -0.55f, 0.35f, 0f, 0f, 0.55f, 0.35f, 0.7f, Outside);
                case FlightPath.Spiral:
                    // Comes down the right of the reference point into a circle of about 4.5 m radius in the upper middle,
                    // which it flies clockwise (its right side downwards; loops from the second waypoint).
                    return Points(0.25f, Outside, 0.25f, 0.25f, 0f, -0.2f, -0.25f, 0.25f, 0f, 0.7f, 0.25f, 0.25f);
                case FlightPath.Strafe:
                    // Dives on the left, levels out low and strafes across to the right side.
                    return Points(-0.5f, Outside, -0.5f, 0.3f, -0.35f, -0.3f, 0f, -0.45f, 0.5f, -0.45f, OutsideSide, -0.4f);
                default:
                    return Points(0f, Outside, 0f, 0.4f, 0f, -0.6f, 0f, -Outside);
            }
        }


        /// <summary>
        /// The waypoints of <paramref name="path"/> in field meters (the playfield of <see cref="StrikeRules.HalfSize"/>
        /// centred on the origin), mirrored left to right when <paramref name="mirror"/>.
        /// </summary>
        public static Vector2[] Waypoints(FlightPath path, bool mirror)
        {
            Vector2[] points = Normalised(path);
            var result = new Vector2[points.Length];
            Vector2 half = StrikeRules.HalfSize;
            for (int i = 0; i < points.Length; i++)
            {
                result[i] = new Vector2((mirror ? -points[i].x : points[i].x) * half.x, points[i].y * half.y);
            }
            return result;
        }


        /// <summary>What an air unit on <paramref name="path"/> does at its end unless its event says otherwise.</summary>
        public static FlightType DefaultType(FlightPath path)
        {
            switch (path)
            {
                case FlightPath.HoverTop:
                case FlightPath.HoverMid:
                case FlightPath.Spiral:
                    return FlightType.Repeat;
                default:
                    return FlightType.Linear;
            }
        }


        /// <summary>
        /// The waypoint a repeating path loops back to (0 = the start). The hovering paths end on that waypoint, so their
        /// loop is closed; any other path flown with <see cref="FlightType.Repeat"/> flies back to it from its end.
        /// </summary>
        public static int RepeatFrom(FlightPath path)
        {
            switch (path)
            {
                case FlightPath.HoverTop:
                case FlightPath.Spiral:
                    return 1;
                case FlightPath.HoverMid:
                    return 2;
                default:
                    return 0;
            }
        }


        /// <summary>
        /// The smoothed curve of <paramref name="path"/> in field meters (mirrored when <paramref name="mirror"/>), looping
        /// from <see cref="RepeatFrom"/> when <paramref name="repeat"/>. Curves are immutable and shared.
        /// </summary>
        public static FlightCurve Curve(FlightPath path, bool mirror, bool repeat)
        {
            int key = ((int)path << 2) | (mirror ? 2 : 0) | (repeat ? 1 : 0);
            if (!curves.TryGetValue(key, out FlightCurve curve))
            {
                curve = new FlightCurve(Waypoints(path, mirror), repeat ? RepeatFrom(path) : -1);
                curves[key] = curve;
            }
            return curve;
        }


        /// <summary>
        /// The point of a uniform Catmull-Rom segment from <paramref name="p1"/> (t = 0) to <paramref name="p2"/> (t = 1),
        /// shaped by the neighbours <paramref name="p0"/> and <paramref name="p3"/>.
        /// </summary>
        public static Vector2 CatmullRom(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
        {
            float t2 = t * t;
            float t3 = t2 * t;
            return 0.5f * (2f * p1 + (p2 - p0) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 + (3f * p1 - p0 - 3f * p2 + p3) * t3);
        }


        private static Vector2[] Points(params float[] xy)
        {
            var points = new Vector2[xy.Length / 2];
            for (int i = 0; i < points.Length; i++)
            {
                points[i] = new Vector2(xy[2 * i], xy[2 * i + 1]);
            }
            return points;
        }
    }


    /// <summary>
    /// A flight path smoothed with Catmull-Rom splines through its waypoints and measured, so a body can fly it at a constant
    /// speed: the position after a distance flown. A linear curve goes on straight along its last direction past the end;
    /// a looping curve wraps past its end back to the loop's start. Immutable, deterministic.
    /// </summary>
    public sealed class FlightCurve
    {
        /// <summary>Samples per segment between two waypoints.</summary>
        private const int Samples = 16;

        private readonly Vector2[] points;
        private readonly float[] lengths;

        /// <summary>The length of the curve from the first waypoint to the last (m).</summary>
        public float Length { get; }

        /// <summary>Whether the curve loops past its end.</summary>
        public bool Loops { get; }

        /// <summary>The distance along the curve at which its loop starts (<see cref="Length"/> when it does not loop).</summary>
        public float LoopStart { get; }

        /// <summary>The first waypoint.</summary>
        public Vector2 Start => points[0];

        /// <summary>The last waypoint.</summary>
        public Vector2 End => points[points.Length - 1];

        /// <summary>The direction at the last waypoint, which a linear flight keeps after it.</summary>
        public Vector2 EndDirection { get; }

        /// <summary>How many waypoints the curve runs through (with the one that closes a loop).</summary>
        public int WaypointCount { get; }


        /// <summary>
        /// The curve through <paramref name="waypoints"/>; looping from waypoint <paramref name="loopFrom"/> when it is 0 or
        /// more (a loop whose last waypoint is not the loop's first flies back to it).
        /// </summary>
        public FlightCurve(Vector2[] waypoints, int loopFrom = -1)
        {
            var controls = new List<Vector2>(waypoints ?? new Vector2[0]);
            if (controls.Count == 0)
            {
                controls.Add(Vector2.zero);
            }
            if (controls.Count == 1)
            {
                controls.Add(controls[0]);
            }
            bool loops = loopFrom >= 0 && loopFrom < controls.Count;
            if (loops && (controls[controls.Count - 1] - controls[loopFrom]).sqrMagnitude > 1e-6f)
            {
                controls.Add(controls[loopFrom]);
            }
            int count = controls.Count;
            WaypointCount = count;
            points = new Vector2[(count - 1) * Samples + 1];
            lengths = new float[points.Length];
            for (int k = 0; k < count - 1; k++)
            {
                Vector2 p1 = controls[k];
                Vector2 p2 = controls[k + 1];
                Vector2 p0 = k > 0 ? controls[k - 1] : 2f * p1 - p2;
                if (loops && k == loopFrom && count > 2)
                {
                    // The loop's first segment leaves its start the way the loop arrives there, so the loop is smooth.
                    p0 = controls[count - 2];
                }
                Vector2 p3 = k + 2 < count ? controls[k + 2] : loops ? controls[Mathf.Min(loopFrom + 1, count - 1)] : 2f * p2 - p1;
                for (int s = 0; s < Samples; s++)
                {
                    points[k * Samples + s] = FlightPaths.CatmullRom(p0, p1, p2, p3, s / (float)Samples);
                }
            }
            points[points.Length - 1] = controls[count - 1];
            for (int i = 1; i < points.Length; i++)
            {
                lengths[i] = lengths[i - 1] + Vector2.Distance(points[i - 1], points[i]);
            }
            Length = lengths[lengths.Length - 1];
            float loopStart = loops ? lengths[loopFrom * Samples] : Length;
            Loops = loops && Length - loopStart > 0.01f;
            LoopStart = Loops ? loopStart : Length;
            Vector2 last = points[points.Length - 1] - points[points.Length - 2];
            EndDirection = last.sqrMagnitude > 1e-8f ? last.normalized : Vector2.down;
        }


        /// <summary>The distance along the curve at waypoint <paramref name="index"/> (clamped to the first and the last).</summary>
        public float DistanceAt(int index)
        {
            return lengths[Mathf.Clamp(index, 0, WaypointCount - 1) * Samples];
        }


        /// <summary>
        /// Where a kamikaze on this curve dives: at the last waypoint but one, since every path's last waypoint lies off
        /// screen (the leg to it only takes a unit out); at the end of a curve of two waypoints.
        /// </summary>
        public float DiveAt => WaypointCount > 2 ? DistanceAt(WaypointCount - 2) : Length;


        /// <summary>Whether a linear flight has passed the last waypoint after <paramref name="distance"/> m.</summary>
        public bool IsPastEnd(float distance)
        {
            return !Loops && distance >= Length;
        }


        /// <summary>The distance on the curve a flight of <paramref name="distance"/> m is at (a loop wraps back into itself).</summary>
        public float Wrap(float distance)
        {
            if (!Loops || distance <= Length)
            {
                return distance;
            }
            return LoopStart + Mathf.Repeat(distance - LoopStart, Length - LoopStart);
        }


        /// <summary>The position after <paramref name="distance"/> m of flight from the start.</summary>
        public Vector2 PointAt(float distance)
        {
            if (distance <= 0f)
            {
                return points[0];
            }
            distance = Wrap(distance);
            if (distance >= Length)
            {
                return End + EndDirection * (distance - Length);
            }
            int i = Segment(distance);
            float span = lengths[i + 1] - lengths[i];
            float t = span > 1e-6f ? (distance - lengths[i]) / span : 0f;
            return Vector2.LerpUnclamped(points[i], points[i + 1], t);
        }


        /// <summary>The unit direction of flight after <paramref name="distance"/> m.</summary>
        public Vector2 DirectionAt(float distance)
        {
            distance = Wrap(Mathf.Max(0f, distance));
            if (distance >= Length)
            {
                return EndDirection;
            }
            int i = Segment(distance);
            Vector2 step = points[i + 1] - points[i];
            return step.sqrMagnitude > 1e-8f ? step.normalized : EndDirection;
        }


        /// <summary>The index of the sample segment that holds <paramref name="distance"/> (0 &lt;= distance &lt; Length).</summary>
        private int Segment(float distance)
        {
            int low = 0;
            int high = lengths.Length - 2;
            while (low < high)
            {
                int middle = (low + high + 1) / 2;
                if (lengths[middle] <= distance)
                {
                    low = middle;
                }
                else
                {
                    high = middle - 1;
                }
            }
            return low;
        }
    }
}
