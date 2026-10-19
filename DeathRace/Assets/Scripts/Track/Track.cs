using System.Collections.Generic;
using Gamebox.Racing;
using UnityEngine;

namespace Portfolio.DeathRace
{
    /// <summary>
    /// A built track in the scene: its layout and centre line, the width of the road and where the barriers stand, the
    /// grid, the pickup spots, the racing line and safe speeds the computer's drivers follow, and the outline of its map.
    /// The numbers come from <see cref="TrackGeometry"/>; <see cref="TrackBuilder"/> makes the object and its scenery.
    ///
    /// The road surface is the plane y = 0 (cars stand on it); distances are metres along <see cref="Path"/>, lateral
    /// offsets positive to the left of the direction of travel.
    /// </summary>
    public class Track : MonoBehaviour
    {
        /// <summary>The height of the road surface.</summary>
        public const float SurfaceHeight = 0f;

        protected readonly List<Vector3> pickupSpots = new List<Vector3>();

        private readonly List<int> pickupClusters = new List<int>();

        public TrackLayout Layout { get; protected set; }
        public RacePath Path { get; protected set; }

        /// <summary>The measurements of the layout (widths, racing line, speeds, grid, rules).</summary>
        public TrackGeometry Geometry { get; protected set; }

        /// <summary>Where the start and finish line is along <see cref="Path"/>.</summary>
        public float LineDistance { get; protected set; }

        /// <summary>Half the layout's width (the road is a little wider in hairpins: <see cref="HalfWidthAt"/>).</summary>
        public float HalfWidth => Layout != null ? Layout.Width * 0.5f : 7f;

        public IReadOnlyList<Vector3> PickupSpots => pickupSpots;

        /// <summary>The cluster (an index into the layout's pickup distances) of each of <see cref="PickupSpots"/>.</summary>
        public IReadOnlyList<int> PickupClusters => pickupClusters;

        /// <summary>The outline the minimap draws (the centre line's samples).</summary>
        public IReadOnlyList<Vector2> MinimapOutline => Path != null ? Path.Samples : new Vector2[0];

        /// <summary>The road and its barriers (not the ground round them), from y = 0 to the top of the barriers.</summary>
        public Bounds Bounds { get; protected set; }

        /// <summary>The look the scenery wears now.</summary>
        public DeathRaceTheme Theme { get; private set; }

        /// <summary>The effects and models the scenery borrows (fire for the fire pits); may be null.</summary>
        public DeathRaceContent Content { get; private set; }

        public virtual void Setup(TrackLayout layout, RacePath path)
        {
            Layout = layout;
            Path = path;
            Geometry = new TrackGeometry(layout, path);
            LineDistance = Geometry.LineDistance;
            pickupSpots.Clear();
            pickupSpots.AddRange(Geometry.PickupSpots);
            pickupClusters.Clear();
            pickupClusters.AddRange(Geometry.PickupClusters);
            Bounds = MeasureBounds();
        }

        /// <summary>
        /// Grid slot <paramref name="index"/> (0 = pole): staggered pairs behind the line, the pole on the inside of the
        /// first turn, on the road and facing the way of travel. Any number of slots works (rows go back 9 m apiece).
        /// </summary>
        public virtual Pose GridSlot(int index)
        {
            return Geometry.GridSlot(index);
        }

        /// <summary>Half the width of the road at <paramref name="distance"/>.</summary>
        public float HalfWidthAt(float distance)
        {
            return Geometry.HalfWidthAt(distance);
        }

        /// <summary>How far the face of either barrier is from the centre line at <paramref name="distance"/>.</summary>
        public float BarrierOffset(float distance)
        {
            return Geometry.BarrierOffset(distance);
        }

        /// <summary>
        /// How far left (positive) or right of the centre line the racing line runs at <paramref name="distance"/>: wide into
        /// corners, tight at the apex; always at least 2.5 m inside the edge of the road.
        /// </summary>
        public float RacingLineOffset(float distance)
        {
            return Geometry.RacingLineOffset(distance);
        }

        /// <summary>The racing line's point at <paramref name="distance"/> in the ground plane (x, z).</summary>
        public Vector2 RacingLinePoint(float distance)
        {
            return Geometry.RacingLinePoint(distance);
        }

        /// <summary>
        /// How fast to go at <paramref name="distance"/> to make the corners ahead, from 0 to 1 (1 = flat out; a share of
        /// <see cref="TrackGeometry.ReferenceSpeed"/>, for baseline grip). Look a little ahead of the car with it.
        /// </summary>
        public float SpeedHint(float distance)
        {
            return Geometry.SpeedHint(distance);
        }

        /// <summary>The same as <see cref="SpeedHint"/> in metres per second.</summary>
        public float SafeSpeed(float distance)
        {
            return Geometry.SafeSpeed(distance);
        }

        /// <summary>
        /// The distance along the track nearest a world point. Pass the last answer as <paramref name="hint"/> to search
        /// only near it (faster, and it keeps a car on its own part of the track where two parts pass close).
        /// </summary>
        public float Locate(Vector3 world, float hint = -1f)
        {
            return Path.Project(RacePath.ToPlane(world), hint);
        }

        /// <summary>Whether a world point is on the road (the verge before the barriers is not).</summary>
        public bool IsOnRoad(Vector3 world, float hint = -1f)
        {
            return Geometry.IsOnRoad(RacePath.ToPlane(world), hint);
        }

        /// <summary>A pose on the middle of the road at <paramref name="distance"/>, facing the way of travel.</summary>
        public Pose Respawn(float distance)
        {
            return Geometry.Respawn(distance);
        }

        /// <summary>
        /// Dresses the track in another look: the road, the barriers and the scenery are built again from the theme (the
        /// same scatter, the same seed), while the centre line, the grid and the barrier colliders stay as they are.
        /// </summary>
        public void Restyle(DeathRaceTheme theme)
        {
            TrackBuilder.Dress(this, theme, Content);
        }

        internal void Remember(DeathRaceTheme theme, DeathRaceContent content)
        {
            Theme = theme;
            Content = content;
        }

        private Bounds MeasureBounds()
        {
            var bounds = new Bounds(RacePath.ToWorld(Path.PointAt(0f)), Vector3.zero);
            float step = Path.Spacing;
            for (int i = 0; i < Path.Count; i++)
            {
                float distance = i * step;
                Vector2 centre = Path.Samples[i];
                Vector2 normal = Path.NormalAt(distance);
                float reach = Geometry.BarrierOffset(distance) + TrackGeometry.BarrierThickness;
                bounds.Encapsulate(RacePath.ToWorld(centre + normal * reach));
                bounds.Encapsulate(RacePath.ToWorld(centre - normal * reach));
            }
            bounds.Encapsulate(new Vector3(bounds.center.x, TrackBuilder.BarrierHeight, bounds.center.z));
            return bounds;
        }
    }
}
