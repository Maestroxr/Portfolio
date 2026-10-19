using System.Collections.Generic;
using Gamebox.Racing;
using UnityEngine;

namespace Portfolio.DeathRace
{
    /// <summary>
    /// Everything about a track that needs no scene, measured once from its layout: how wide the road is at every point
    /// (hairpins open up a little, narrow stretches close in), where the barriers stand, the racing line the computer's
    /// drivers follow and how fast each part of it can be taken, the grid, the pickup spots, and the rules every layout
    /// keeps (<see cref="Problems"/>). <see cref="Track"/> wraps it in the scene; the tests and the previews use it as it is.
    ///
    /// Distances are metres along the centre line (<see cref="RacePath"/>), lateral offsets are positive to the left of
    /// the direction of travel (like <see cref="RacePath.NormalAt"/>), and the road surface is the plane y = 0.
    /// </summary>
    public sealed class TrackGeometry
    {
        /// <summary>The distance between two samples of the centre line.</summary>
        public const float Spacing = 2f;

        /// <summary>The verge between the edge of the road and the face of the barrier (still drivable).</summary>
        public const float Shoulder = 2.5f;

        /// <summary>How thick a barrier is at its foot.</summary>
        public const float BarrierThickness = 0.7f;

        /// <summary>How far the racing line keeps from the edge of the road (half a car and a little air).</summary>
        public const float LineMargin = 2.5f;

        /// <summary>How long the road takes to blend into the width of a <see cref="TrackLayout.Stretch"/>.</summary>
        public const float StretchBlend = 20f;

        /// <summary>The speed a <see cref="SpeedHint"/> of 1 stands for (a little above the fastest car's top speed).</summary>
        public const float ReferenceSpeed = 50f;

        /// <summary>The sideways grip <see cref="SafeSpeed"/> assumes (m/s^2): an arcade car on a baseline set of tyres.</summary>
        public const float LateralGrip = 14f;

        /// <summary>The braking <see cref="SafeSpeed"/> assumes (m/s^2).</summary>
        public const float Braking = 16f;

        /// <summary>
        /// The lengths of an ordinary lap: the first nine tracks run 700-1,100 m; the faster cars of the harder leagues
        /// get up to 1,500 m (Gridlock Boulevard, the longest, is 1,385 m).
        /// </summary>
        public const float MinLength = 600f;
        public const float MaxLength = 1500f;
        public const float ArenaMinLength = 400f;
        public const float ArenaMaxLength = 520f;
        public const float MinWidth = 12f;
        public const float MaxWidth = 16f;

        /// <summary>How many road widths apart two parts of the track that are not next to each other stay.</summary>
        public const float ClearanceWidths = 2.5f;

        /// <summary>Two points closer than this many widths along the line are the same part of the track.</summary>
        public const float AdjacentWidths = 7f;

        /// <summary>The tightest corner a road can take, in road widths of radius.</summary>
        public const float MinRadiusWidths = 1.2f;

        public const int MinPickups = 4;
        public const int MaxPickups = 7;

        /// <summary>A start straight bends less than this (1/m) from <see cref="StraightBehind"/> before the line to <see cref="StraightAhead"/> after it.</summary>
        public const float StraightCurvature = 1f / 150f;
        public const float StraightBehind = 45f;
        public const float StraightAhead = 20f;

        // The grid: the pole sits GridLead metres behind the line, rows GridRow apart, the second car of a row GridStagger
        // further back, each GridLateral of the half width off the middle (the pole on the inside of the first turn).
        private const float GridLead = 6f;
        private const float GridRow = 9f;
        private const float GridStagger = 4.5f;
        private const float GridLateral = 0.42f;

        /// <summary>How far behind the line the grid of eight reaches (no pickups there).</summary>
        private const float GridLength = 50f;

        // Hairpins open up: by up to HairpinWiden of the width where the radius is WidenFullRadius widths or less (none from
        // WidenNoneRadius), but only where the road turns more than HairpinFrom degrees within HairpinSpan metres either
        // side (a plain 90 degree corner keeps its width), smoothed over WidenSmoothing metres either side.
        private const float HairpinWiden = 0.2f;
        private const float WidenFullRadius = 1.4f;
        private const float WidenNoneRadius = 2.6f;
        private const float WidenSmoothing = 12f;
        private const float HairpinSpan = 30f;
        private const float HairpinFrom = 100f;
        private const float HairpinFull = 150f;

        private readonly float[] halfWidths;
        private readonly float[] racingOffsets;
        private readonly float[] safeSpeeds;
        private readonly List<Vector3> pickupSpots = new List<Vector3>();
        private readonly List<int> pickupClusters = new List<int>();
        private readonly int poleSide;

        public TrackGeometry(TrackLayout layout) : this(layout, CreatePath(layout))
        {
        }

        public TrackGeometry(TrackLayout layout, RacePath path)
        {
            Layout = layout;
            Path = path;
            LineDistance = path.Wrap(layout.StartDistance);
            halfWidths = MeasureHalfWidths();
            racingOffsets = MeasureRacingLine();
            safeSpeeds = MeasureSafeSpeeds();
            poleSide = MeasurePoleSide();
            PlacePickups();
        }

        public TrackLayout Layout { get; }
        public RacePath Path { get; }

        /// <summary>Where the start and finish line is along <see cref="Path"/>.</summary>
        public float LineDistance { get; }

        /// <summary>The spots pickups lie on (y = 0), cluster after cluster in the order of the layout's distances.</summary>
        public IReadOnlyList<Vector3> PickupSpots => pickupSpots;

        /// <summary>The cluster (an index into the layout's pickup distances) of each of <see cref="PickupSpots"/>.</summary>
        public IReadOnlyList<int> PickupClusters => pickupClusters;

        /// <summary>The centre line of a layout, sampled the way every track is.</summary>
        public static RacePath CreatePath(TrackLayout layout)
        {
            return new RacePath(layout.Points, true, Spacing);
        }

        /// <summary>Half the width of the road at <paramref name="distance"/>.</summary>
        public float HalfWidthAt(float distance)
        {
            return Sampled(halfWidths, distance);
        }

        /// <summary>How far the face of either barrier is from the centre line at <paramref name="distance"/>.</summary>
        public float BarrierOffset(float distance)
        {
            return HalfWidthAt(distance) + Shoulder;
        }

        /// <summary>
        /// The racing line: how far left (positive) or right of the centre line a quick car runs at
        /// <paramref name="distance"/>, wide into a corner, tight at its apex, wide out of it. It always stays
        /// <see cref="LineMargin"/> inside the edge of the road.
        /// </summary>
        public float RacingLineOffset(float distance)
        {
            return Sampled(racingOffsets, distance);
        }

        public Vector2 RacingLinePoint(float distance)
        {
            return Path.PointAt(distance) + Path.NormalAt(distance) * RacingLineOffset(distance);
        }

        /// <summary>
        /// The fastest a car can be going at <paramref name="distance"/> on the racing line and still make the corners
        /// ahead, in m/s, for <see cref="LateralGrip"/> and <see cref="Braking"/>; at most <see cref="ReferenceSpeed"/>.
        /// </summary>
        public float SafeSpeed(float distance)
        {
            return Sampled(safeSpeeds, distance);
        }

        /// <summary><see cref="SafeSpeed"/> as a share of <see cref="ReferenceSpeed"/>: 1 = flat out, 0.3 = a hairpin is coming.</summary>
        public float SpeedHint(float distance)
        {
            return Mathf.Clamp01(SafeSpeed(distance) / ReferenceSpeed);
        }

        /// <summary>Whether a point of the ground plane is on the road (not the verge) of its nearest part of the track.</summary>
        public bool IsOnRoad(Vector2 point, float hint = -1f)
        {
            float distance = Path.Project(point, hint);
            return Mathf.Abs(Path.SignedOffset(point, distance)) <= HalfWidthAt(distance);
        }

        /// <summary>
        /// Grid slot <paramref name="index"/> (0 = pole): staggered pairs behind the line, the pole on the inside of the first
        /// turn, every slot on the road and facing the way of travel.
        /// </summary>
        public Pose GridSlot(int index)
        {
            index = Mathf.Max(0, index);
            float distance = LineDistance - GridLead - index / 2 * GridRow - index % 2 * GridStagger;
            int side = index % 2 == 0 ? poleSide : -poleSide;
            Vector2 point = Path.PointAt(distance) + Path.NormalAt(distance) * (side * HalfWidthAt(distance) * GridLateral);
            return new Pose(RacePath.ToWorld(point), Facing(Path.TangentAt(distance)));
        }

        /// <summary>A pose on the middle of the road at <paramref name="distance"/>, facing the way of travel.</summary>
        public Pose Respawn(float distance)
        {
            return new Pose(RacePath.ToWorld(Path.PointAt(distance)), Facing(Path.TangentAt(distance)));
        }

        /// <summary>
        /// The rotation that turns a car's forward (+Z) to <paramref name="direction"/> of the ground plane, worked out
        /// without the engine (so it also runs in plain tests).
        /// </summary>
        public static Quaternion Facing(Vector2 direction)
        {
            float half = Mathf.Atan2(direction.x, direction.y) * 0.5f;
            return new Quaternion(0f, Mathf.Sin(half), 0f, Mathf.Cos(half));
        }

        /// <summary>
        /// What is wrong with the layout, in words (empty when nothing is): its length and width, its tightest corner,
        /// whether two parts of it come too close or cross, whether the line is on a straight, whether the grid fits on the
        /// road and whether its pickups are spread round the lap.
        /// </summary>
        public List<string> Problems()
        {
            var problems = new List<string>();
            TrackLayout layout = Layout;
            string name = layout.Name;
            if (string.IsNullOrEmpty(name))
            {
                problems.Add("a track needs a name");
                name = "?";
            }
            if (string.IsNullOrEmpty(layout.Description))
            {
                problems.Add($"{name}: needs a description");
            }
            if (layout.Laps < DeathRaceSettings.MinLaps || layout.Laps > DeathRaceSettings.MaxLaps)
            {
                problems.Add($"{name}: {layout.Laps} laps");
            }
            if (layout.Width < MinWidth || layout.Width > MaxWidth)
            {
                problems.Add($"{name}: width {layout.Width} is not {MinWidth}-{MaxWidth} m");
            }
            foreach (TrackLayout.Stretch stretch in layout.Stretches)
            {
                if (stretch.Width < MinWidth || stretch.Width > MaxWidth)
                {
                    problems.Add($"{name}: a stretch {stretch.Width} m wide is not {MinWidth}-{MaxWidth} m");
                }
            }
            float minLength = layout.Showdown ? ArenaMinLength : MinLength;
            float maxLength = layout.Showdown ? ArenaMaxLength : MaxLength;
            if (Path.Length < minLength || Path.Length > maxLength)
            {
                problems.Add($"{name}: {Path.Length:0} m long, not {minLength:0}-{maxLength:0}");
            }
            TrackMeasures measures = Measure();
            float radiusLimit = MinRadiusWidths * layout.Width;
            if (measures.MinRadius < radiusLimit)
            {
                problems.Add($"{name}: a corner of radius {measures.MinRadius:0.0} m at {measures.MinRadiusAt:0} m (the road takes {radiusLimit:0.0})");
            }
            if (measures.Clearance < 1f)
            {
                problems.Add($"{name}: the parts at {measures.ClosestA:0} m and {measures.ClosestB:0} m are {measures.Clearance * ClearanceWidths:0.00} widths apart");
            }
            if (measures.Crossings > 0)
            {
                problems.Add($"{name}: the centre line crosses itself {measures.Crossings} times");
            }
            if (measures.StartCurvature > StraightCurvature)
            {
                problems.Add($"{name}: the line is not on a straight (it bends at {measures.StartCurvature * 1000f:0.0}/km)");
            }
            for (int i = 0; i < 8; i++)
            {
                Pose slot = GridSlot(i);
                Vector2 point = RacePath.ToPlane(slot.position);
                float distance = Path.Project(point, LineDistance - 25f, 40f);
                if (Mathf.Abs(Path.SignedOffset(point, distance)) + 1.2f > HalfWidthAt(distance) || Path.Delta(LineDistance, distance) >= -1f)
                {
                    problems.Add($"{name}: grid slot {i} is not on the road behind the line");
                }
            }
            CheckPickups(problems, name);
            return problems;
        }

        /// <summary>The measurements behind <see cref="Problems"/> (also printed by the previews).</summary>
        public TrackMeasures Measure()
        {
            var measures = new TrackMeasures { Length = Path.Length, MinRadius = float.MaxValue, Clearance = float.MaxValue };
            int count = Path.Count;
            float step = Path.Spacing;
            for (int i = 0; i < count; i++)
            {
                float curvature = Mathf.Abs(Path.CurvatureAt(i * step));
                float radius = curvature > 1e-6f ? 1f / curvature : float.MaxValue;
                if (radius < measures.MinRadius)
                {
                    measures.MinRadius = radius;
                    measures.MinRadiusAt = i * step;
                }
            }
            measures.MinWidth = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                measures.MinWidth = Mathf.Min(measures.MinWidth, halfWidths[i] * 2f);
                measures.MaxWidth = Mathf.Max(measures.MaxWidth, halfWidths[i] * 2f);
            }
            IReadOnlyList<Vector2> samples = Path.Samples;
            float adjacent = AdjacentWidths * Layout.Width;
            for (int i = 0; i < count; i++)
            {
                for (int j = i + 1; j < count; j++)
                {
                    int apart = Mathf.Min(j - i, count - (j - i));
                    if (apart * step <= adjacent)
                    {
                        continue;
                    }
                    float need = ClearanceWidths * 2f * Mathf.Max(halfWidths[i], halfWidths[j]);
                    float ratio = Vector2.Distance(samples[i], samples[j]) / need;
                    if (ratio < measures.Clearance)
                    {
                        measures.Clearance = ratio;
                        measures.ClosestA = i * step;
                        measures.ClosestB = j * step;
                    }
                }
            }
            measures.Crossings = CountCrossings(samples);
            for (float x = -StraightBehind; x <= StraightAhead; x += step)
            {
                measures.StartCurvature = Mathf.Max(measures.StartCurvature, Mathf.Abs(Path.CurvatureAt(LineDistance + x)));
            }
            return measures;
        }

        private void CheckPickups(List<string> problems, string name)
        {
            float[] distances = Layout.PickupDistances;
            if (distances == null || distances.Length < MinPickups || distances.Length > MaxPickups)
            {
                problems.Add($"{name}: needs {MinPickups}-{MaxPickups} pickup clusters");
                return;
            }
            var sorted = new List<float>();
            foreach (float distance in distances)
            {
                if (distance < 0f || distance >= Path.Length)
                {
                    problems.Add($"{name}: a pickup cluster at {distance:0} m is off the lap");
                }
                float fromLine = Path.Delta(LineDistance, distance);
                if (fromLine > -GridLength && fromLine < 15f)
                {
                    problems.Add($"{name}: the pickup cluster at {distance:0} m lies on the grid");
                }
                sorted.Add(Path.Wrap(distance));
            }
            sorted.Sort();
            for (int i = 0; i < sorted.Count; i++)
            {
                float gap = Path.Wrap(sorted[(i + 1) % sorted.Count] - sorted[i]);
                if (gap < 50f || gap > Path.Length * 0.4f)
                {
                    problems.Add($"{name}: pickup clusters at {sorted[i]:0} m and the next are {gap:0} m apart");
                }
            }
            for (int i = 0; i < pickupSpots.Count; i++)
            {
                Vector2 point = RacePath.ToPlane(pickupSpots[i]);
                float distance = Path.Project(point, distances[pickupClusters[i]], 30f);
                if (Mathf.Abs(Path.SignedOffset(point, distance)) + 1f > HalfWidthAt(distance))
                {
                    problems.Add($"{name}: pickup spot {i} is off the road");
                }
            }
        }

        private static int CountCrossings(IReadOnlyList<Vector2> samples)
        {
            int count = samples.Count;
            int crossings = 0;
            for (int i = 0; i < count; i++)
            {
                Vector2 a = samples[i];
                Vector2 b = samples[(i + 1) % count];
                for (int j = i + 2; j < count; j++)
                {
                    if ((j + 1) % count == i)
                    {
                        continue;
                    }
                    if (SegmentsCross(a, b, samples[j], samples[(j + 1) % count]))
                    {
                        crossings++;
                    }
                }
            }
            return crossings;
        }

        private static bool SegmentsCross(Vector2 a, Vector2 b, Vector2 c, Vector2 d)
        {
            float d1 = Cross(c, d, a);
            float d2 = Cross(c, d, b);
            float d3 = Cross(a, b, c);
            float d4 = Cross(a, b, d);
            return d1 > 0f != d2 > 0f && d3 > 0f != d4 > 0f;
        }

        private static float Cross(Vector2 origin, Vector2 a, Vector2 b)
        {
            return (a.x - origin.x) * (b.y - origin.y) - (a.y - origin.y) * (b.x - origin.x);
        }

        /// <summary>A value per sample, read between samples at a distance.</summary>
        private float Sampled(float[] values, float distance)
        {
            float position = Path.Wrap(distance) / Path.Spacing;
            int index = Mathf.FloorToInt(position);
            float t = position - index;
            int count = values.Length;
            index = (index % count + count) % count;
            return Mathf.LerpUnclamped(values[index], values[(index + 1) % count], t);
        }

        /// <summary>The width the layout asks for at a distance: its own, or a stretch's, blended at the stretch's ends.</summary>
        private float LayoutWidth(float distance)
        {
            float width = Layout.Width;
            float length = Path.Length;
            foreach (TrackLayout.Stretch stretch in Layout.Stretches)
            {
                float span = Path.Wrap(stretch.To - stretch.From);
                float into = Path.Wrap(distance - stretch.From);
                float weight;
                if (into <= span)
                {
                    weight = 1f;
                }
                else
                {
                    float gap = Mathf.Min(ArcDistance(distance, stretch.From, length), ArcDistance(distance, stretch.To, length));
                    weight = 1f - Smooth(gap / StretchBlend);
                }
                width += (stretch.Width - width) * weight;
            }
            return width;
        }

        private float[] MeasureHalfWidths()
        {
            int count = Path.Count;
            float step = Path.Spacing;
            var curvatures = new float[count];
            var widths = new float[count];
            for (int i = 0; i < count; i++)
            {
                curvatures[i] = Path.CurvatureAt(i * step, 8f);
                widths[i] = LayoutWidth(i * step);
            }
            int turnReach = Mathf.Max(1, Mathf.RoundToInt(HairpinSpan / step));
            var widen = new float[count];
            for (int i = 0; i < count; i++)
            {
                float curvature = Mathf.Abs(curvatures[i]);
                float radius = curvature > 1e-6f ? 1f / curvature : float.MaxValue;
                float tight = Smooth((WidenNoneRadius * widths[i] - radius) / ((WidenNoneRadius - WidenFullRadius) * widths[i]));
                float turn = 0f;
                for (int j = -turnReach; j <= turnReach; j++)
                {
                    turn += curvatures[Wrap(i + j, count)];
                }
                float degrees = Mathf.Abs(turn * step) * Mathf.Rad2Deg;
                float hairpin = Smooth((degrees - HairpinFrom) / (HairpinFull - HairpinFrom));
                widen[i] = HairpinWiden * tight * hairpin;
            }
            int reach = Mathf.Max(1, Mathf.RoundToInt(WidenSmoothing / step));
            var halves = new float[count];
            for (int i = 0; i < count; i++)
            {
                float sum = 0f;
                for (int j = -reach; j <= reach; j++)
                {
                    sum += widen[Wrap(i + j, count)];
                }
                halves[i] = widths[i] * (1f + sum / (2 * reach + 1)) * 0.5f;
            }
            return halves;
        }

        /// <summary>
        /// A minimum-curvature line inside the road: every point is pulled towards the middle of its neighbours (which
        /// straightens the line) and held inside the margins, first over long reaches, then shorter ones, until it settles.
        /// </summary>
        private float[] MeasureRacingLine()
        {
            int count = Path.Count;
            float step = Path.Spacing;
            IReadOnlyList<Vector2> centre = Path.Samples;
            var normals = new Vector2[count];
            var limits = new float[count];
            for (int i = 0; i < count; i++)
            {
                normals[i] = Path.NormalAt(i * step);
                limits[i] = Mathf.Max(0f, halfWidths[i] - LineMargin);
            }
            var offsets = new float[count];
            int[,] passes = { { 16, 40 }, { 10, 50 }, { 6, 60 }, { 3, 80 }, { 2, 80 } };
            for (int pass = 0; pass < passes.GetLength(0); pass++)
            {
                int reach = passes[pass, 0];
                for (int iteration = 0; iteration < passes[pass, 1]; iteration++)
                {
                    for (int i = 0; i < count; i++)
                    {
                        int before = Wrap(i - reach, count);
                        int after = Wrap(i + reach, count);
                        Vector2 a = centre[before] + normals[before] * offsets[before];
                        Vector2 b = centre[after] + normals[after] * offsets[after];
                        float target = Vector2.Dot((a + b) * 0.5f - centre[i], normals[i]);
                        float offset = offsets[i] + 0.6f * (target - offsets[i]);
                        offsets[i] = Mathf.Clamp(offset, -limits[i], limits[i]);
                    }
                }
            }
            return offsets;
        }

        /// <summary>The corner speed of every point of the racing line, then lowered where braking for a slower one ahead.</summary>
        private float[] MeasureSafeSpeeds()
        {
            int count = Path.Count;
            float step = Path.Spacing;
            var line = new Vector2[count];
            for (int i = 0; i < count; i++)
            {
                line[i] = Path.Samples[i] + Path.NormalAt(i * step) * racingOffsets[i];
            }
            const int span = 4;
            var speeds = new float[count];
            for (int i = 0; i < count; i++)
            {
                Vector2 before = line[Wrap(i - span, count)] - line[Wrap(i - span - 1, count)];
                Vector2 after = line[Wrap(i + span + 1, count)] - line[Wrap(i + span, count)];
                float angle = Mathf.Abs(Vector2.SignedAngle(before, after)) * Mathf.Deg2Rad;
                float length = 0f;
                for (int j = i - span - 1; j < i + span + 1; j++)
                {
                    length += Vector2.Distance(line[Wrap(j, count)], line[Wrap(j + 1, count)]);
                }
                float curvature = angle / Mathf.Max(length, 1e-3f);
                speeds[i] = curvature > 1e-6f ? Mathf.Min(ReferenceSpeed, Mathf.Sqrt(LateralGrip / curvature)) : ReferenceSpeed;
            }
            for (int round = 0; round < 2; round++)
            {
                for (int i = count - 1; i >= 0; i--)
                {
                    float next = speeds[(i + 1) % count];
                    speeds[i] = Mathf.Min(speeds[i], Mathf.Sqrt(next * next + 2f * Braking * step));
                }
            }
            return speeds;
        }

        /// <summary>The pole goes on the inside of the first turn after the line.</summary>
        private int MeasurePoleSide()
        {
            float turn = 0f;
            for (float x = 0f; x < 160f; x += 4f)
            {
                turn += Path.CurvatureAt(LineDistance + x);
            }
            return turn >= 0f ? 1 : -1;
        }

        /// <summary>Three spots per cluster across the road: a row, or a diagonal one way or the other.</summary>
        private void PlacePickups()
        {
            float[] distances = Layout.PickupDistances ?? new float[0];
            float[] across = { -0.55f, 0f, 0.55f };
            for (int cluster = 0; cluster < distances.Length; cluster++)
            {
                float centre = distances[cluster];
                int pattern = cluster % 3;
                foreach (float lateral in across)
                {
                    float along = pattern == 1 ? lateral * 6f : pattern == 2 ? -lateral * 6f : 0f;
                    float distance = centre + along;
                    Vector2 point = Path.PointAt(distance) + Path.NormalAt(distance) * (lateral * HalfWidthAt(distance));
                    pickupSpots.Add(RacePath.ToWorld(point));
                    pickupClusters.Add(cluster);
                }
            }
        }

        private static float ArcDistance(float a, float b, float length)
        {
            float distance = Mathf.Abs(a - b) % length;
            return Mathf.Min(distance, length - distance);
        }

        private static int Wrap(int index, int count)
        {
            return (index % count + count) % count;
        }

        private static float Smooth(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * (3f - 2f * t);
        }
    }

    /// <summary>What <see cref="TrackGeometry.Measure"/> finds (distances in metres).</summary>
    public struct TrackMeasures
    {
        public float Length;
        public float MinRadius;
        public float MinRadiusAt;

        /// <summary>The closest two parts of the track come, as a share of the clearance they need (1 = just enough).</summary>
        public float Clearance;
        public float ClosestA;
        public float ClosestB;
        public int Crossings;

        /// <summary>The most the centre line bends about the start line (1/m).</summary>
        public float StartCurvature;
        public float MinWidth;
        public float MaxWidth;
    }
}
