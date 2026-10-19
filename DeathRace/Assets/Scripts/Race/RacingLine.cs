using System;
using Gamebox.Racing;
using UnityEngine;

namespace Portfolio.DeathRace
{
    /// <summary>
    /// The line a good driver takes round a track: wide into a corner, clipping the inside at the apex, wide again on
    /// the way out, so the bends are as gentle as the road allows. It is an offset from the centre line for every sample
    /// of the track's <see cref="RacePath"/> (+ = left, like <see cref="RacePath.NormalAt"/>).
    ///
    /// It is found the way Rémi Coulom's K1999 driver found its line: every point is moved across the road until the
    /// bend of the line there is the average of the bends either side, so the bending is spread as evenly as the road's
    /// edges let it be; first on a coarse spacing for the long shapes, then finer and finer for the detail. A computer
    /// driver with less skill drives a <see cref="Blend"/> of it closer to the middle of the road (and slower).
    /// </summary>
    public sealed class RacingLine
    {
        /// <summary>How far the line keeps from the road's edge, metres (half a car and some room).</summary>
        public const float DefaultMargin = 2.4f;

        private const int Passes = 60;

        private readonly RacePath path;
        private readonly float[] offsets;
        private readonly Vector2[] points;
        private readonly float[] curvature;
        private readonly float[] segments;

        private RacingLine(RacePath path, float[] offsets)
        {
            this.path = path;
            this.offsets = offsets;
            int count = offsets.Length;
            points = new Vector2[count];
            for (int i = 0; i < count; i++)
            {
                float distance = i * path.Spacing;
                points[i] = path.PointAt(distance) + path.NormalAt(distance) * offsets[i];
            }
            segments = new float[count];
            for (int i = 0; i < count; i++)
            {
                segments[i] = Vector2.Distance(points[i], points[(i + 1) % count]);
            }
            curvature = new float[count];
            var raw = new float[count];
            const int reach = 3;
            for (int i = 0; i < count; i++)
            {
                raw[i] = Menger(points[Index(i - reach)], points[i], points[Index(i + reach)]);
            }
            for (int i = 0; i < count; i++)
            {
                float sum = 0f;
                for (int k = -2; k <= 2; k++)
                {
                    sum += raw[Index(i + k)];
                }
                curvature[i] = sum / 5f;
            }
        }

        public RacePath Path => path;

        /// <summary>The samples of the line (one per sample of the path).</summary>
        public int Count => offsets.Length;

        /// <summary>The length of the line once round, metres (a little shorter than the centre line's, usually).</summary>
        public float Length
        {
            get
            {
                float length = 0f;
                foreach (float segment in segments)
                {
                    length += segment;
                }
                return length;
            }
        }

        /// <summary>The line of <paramref name="path"/> within <paramref name="halfWidthAt"/> (the road's half width at a distance).</summary>
        public static RacingLine Build(RacePath path, Func<float, float> halfWidthAt, float margin = DefaultMargin)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }
            int count = path.Count;
            var centres = new Vector2[count];
            var normals = new Vector2[count];
            var room = new float[count];
            for (int i = 0; i < count; i++)
            {
                float distance = i * path.Spacing;
                centres[i] = path.PointAt(distance);
                normals[i] = path.NormalAt(distance);
                float half = halfWidthAt != null ? halfWidthAt(distance) : 7f;
                room[i] = Mathf.Max(0f, half - margin);
            }
            var offsets = new float[count];
            if (path.Closed && count >= 16)
            {
                int step = 1;
                while (step * 2 <= count / 8 && step < 32)
                {
                    step *= 2;
                }
                for (; step >= 1; step /= 2)
                {
                    Relax(centres, normals, room, offsets, step);
                }
            }
            return new RacingLine(path, offsets);
        }

        /// <summary>The middle of the road all the way round.</summary>
        public static RacingLine Centre(RacePath path)
        {
            return new RacingLine(path, new float[path.Count]);
        }

        /// <summary>This line moved towards the middle of the road: <paramref name="share"/> 1 is the line, 0 the middle.</summary>
        public RacingLine Blend(float share)
        {
            share = Mathf.Clamp01(share);
            var blended = new float[offsets.Length];
            for (int i = 0; i < offsets.Length; i++)
            {
                blended[i] = offsets[i] * share;
            }
            return new RacingLine(path, blended);
        }

        /// <summary>The line's offset from the centre at <paramref name="distance"/> along the path (+ = left).</summary>
        public float OffsetAt(float distance)
        {
            return Sample(offsets, distance);
        }

        public Vector2 PointAt(float distance)
        {
            return path.PointAt(distance) + path.NormalAt(distance) * OffsetAt(distance);
        }

        /// <summary>How sharply the line bends at <paramref name="distance"/> (1/m, + = left).</summary>
        public float CurvatureAt(float distance)
        {
            return Sample(curvature, distance);
        }

        /// <summary>The bend of the line at sample <paramref name="index"/>.</summary>
        public float CurvatureOf(int index)
        {
            return curvature[Index(index)];
        }

        /// <summary>The length of the line from sample <paramref name="index"/> to the next.</summary>
        public float SegmentOf(int index)
        {
            return segments[Index(index)];
        }

        public float OffsetOf(int index)
        {
            return offsets[Index(index)];
        }

        /// <summary>The signed curvature of the circle through three points (1/m, + when they turn left).</summary>
        public static float Menger(Vector2 a, Vector2 b, Vector2 c)
        {
            Vector2 ab = b - a;
            Vector2 bc = c - b;
            Vector2 ac = c - a;
            float cross = ab.x * bc.y - ab.y * bc.x;
            float product = ab.magnitude * bc.magnitude * ac.magnitude;
            return product > 1e-6f ? 2f * cross / product : 0f;
        }

        private int Index(int index)
        {
            int count = offsets.Length;
            index %= count;
            return index < 0 ? index + count : index;
        }

        private float Sample(float[] values, float distance)
        {
            float position = path.Wrap(distance) / path.Spacing;
            int index = Mathf.FloorToInt(position);
            float t = position - index;
            return Mathf.Lerp(values[Index(index)], values[Index(index + 1)], t);
        }

        /// <summary>
        /// One level of the search: the points every <paramref name="step"/> samples are moved across the road (within
        /// <paramref name="room"/>) until each bends as much as the average of its neighbours, then the samples between
        /// them are filled in along straight lines for the next, finer level.
        /// </summary>
        private static void Relax(Vector2[] centres, Vector2[] normals, float[] room, float[] offsets, int step)
        {
            int count = centres.Length;
            int nodes = (count + step - 1) / step;
            if (nodes < 5)
            {
                return;
            }
            var grid = new int[nodes];
            for (int j = 0; j < nodes; j++)
            {
                grid[j] = j * step;
            }
            Vector2 Point(int j)
            {
                int i = grid[((j % nodes) + nodes) % nodes];
                return centres[i] + normals[i] * offsets[i];
            }
            const float probe = 0.05f;
            for (int pass = 0; pass < Passes; pass++)
            {
                for (int j = 0; j < nodes; j++)
                {
                    int i = grid[j];
                    Vector2 before2 = Point(j - 2);
                    Vector2 before = Point(j - 1);
                    Vector2 here = Point(j);
                    Vector2 after = Point(j + 1);
                    Vector2 after2 = Point(j + 2);
                    float target = 0.5f * (Menger(before2, before, here) + Menger(here, after, after2));
                    float now = Menger(before, here, after);
                    float moved = Menger(before, here + normals[i] * probe, after);
                    float slope = (moved - now) / probe;
                    if (Mathf.Abs(slope) < 1e-7f)
                    {
                        continue;
                    }
                    float change = Mathf.Clamp((target - now) / slope, -step, step);
                    offsets[i] = Mathf.Clamp(offsets[i] + change * 0.7f, -room[i], room[i]);
                }
            }
            if (step == 1)
            {
                return;
            }
            for (int j = 0; j < nodes; j++)
            {
                int from = grid[j];
                int to = j + 1 < nodes ? grid[j + 1] : count;
                float start = offsets[from];
                float end = offsets[to % count];
                for (int i = from + 1; i < to; i++)
                {
                    float t = (i - from) / (float)(to - from);
                    offsets[i] = Mathf.Clamp(Mathf.Lerp(start, end, t), -room[i], room[i]);
                }
            }
        }
    }

    /// <summary>
    /// How fast a car can go at every point of a <see cref="RacingLine"/>: as fast as its grip allows through each bend,
    /// and from there backwards as fast as its brakes can still bring it down to the next bend's speed. Not capped by the
    /// top speed (the driver caps it with the top speed it has, nitro or not), so it also says how far a straight runs.
    /// </summary>
    public sealed class SpeedProfile
    {
        /// <summary>A speed no straight goes beyond (m/s).</summary>
        public const float Unlimited = 120f;

        private readonly RacingLine line;
        private readonly float[] speeds;

        /// <param name="line">The line driven.</param>
        /// <param name="handling">The car.</param>
        /// <param name="gripShare">The share of the tyres' grip used in bends (a careful driver uses less).</param>
        /// <param name="brakeShare">The share of the brakes planned with (a careful driver brakes earlier).</param>
        public SpeedProfile(RacingLine line, CarHandling handling, float gripShare, float brakeShare)
        {
            this.line = line ?? throw new ArgumentNullException(nameof(line));
            int count = line.Count;
            speeds = new float[count];
            for (int i = 0; i < count; i++)
            {
                speeds[i] = CornerSpeed(handling, line.CurvatureOf(i), gripShare);
            }
            float brake = handling.Brake * Mathf.Clamp(brakeShare, 0.1f, 1f);
            for (int round = 0; round < 2; round++)
            {
                for (int i = count - 1; i >= 0; i--)
                {
                    int next = (i + 1) % count;
                    float reachable = Mathf.Sqrt(speeds[next] * speeds[next] + 2f * brake * line.SegmentOf(i));
                    speeds[i] = Mathf.Min(speeds[i], reachable);
                }
            }
        }

        public RacingLine Line => line;

        /// <summary>The fastest the car may be going at <paramref name="distance"/> along the path (m/s).</summary>
        public float SpeedAt(float distance)
        {
            RacePath path = line.Path;
            float position = path.Wrap(distance) / path.Spacing;
            int index = Mathf.FloorToInt(position);
            float t = position - index;
            int count = speeds.Length;
            return Mathf.Lerp(speeds[((index % count) + count) % count], speeds[(((index + 1) % count) + count) % count], t);
        }

        /// <summary>The lowest speed allowed from <paramref name="distance"/> over the next <paramref name="span"/> metres.</summary>
        public float MinAhead(float distance, float span)
        {
            float spacing = line.Path.Spacing;
            float lowest = SpeedAt(distance);
            for (float x = spacing; x <= span; x += spacing)
            {
                lowest = Mathf.Min(lowest, SpeedAt(distance + x));
            }
            return lowest;
        }

        /// <summary>
        /// The time of a flying lap on this profile in a car that handles like <paramref name="handling"/> with
        /// <paramref name="topShare"/> of its top speed: as fast as the profile allows wherever the engine can get it there.
        /// </summary>
        public float LapTime(CarHandling handling, float topShare = 1f)
        {
            int count = speeds.Length;
            var reached = new float[count];
            float top = handling.TopSpeed * Mathf.Clamp(topShare, 0.1f, 1f);
            for (int i = 0; i < count; i++)
            {
                reached[i] = Mathf.Min(top, speeds[i]);
            }
            for (int round = 0; round < 2; round++)
            {
                for (int i = 0; i < count; i++)
                {
                    int previous = (i - 1 + count) % count;
                    float pull = Mathf.Max(0.3f, handling.DriveAcceleration(reached[previous], false));
                    reached[i] = Mathf.Min(reached[i], Mathf.Sqrt(reached[previous] * reached[previous] + 2f * pull * line.SegmentOf(previous)));
                }
            }
            float time = 0f;
            for (int i = 0; i < count; i++)
            {
                time += line.SegmentOf(i) / Mathf.Max(1f, reached[i]);
            }
            return time;
        }

        /// <summary>The fastest a bend of <paramref name="curvature"/> can be taken with a share of the grip (not capped by the top speed).</summary>
        public static float CornerSpeed(CarHandling handling, float curvature, float gripShare)
        {
            float k = Mathf.Abs(curvature);
            if (k < 1e-4f)
            {
                return Unlimited;
            }
            float speed = Mathf.Sqrt(handling.LateralGrip * gripShare / k);
            for (int i = 0; i < 2; i++)
            {
                speed = Mathf.Sqrt(handling.GripAt(speed, 0f) * gripShare / k);
            }
            return Mathf.Min(Unlimited, speed);
        }
    }
}
