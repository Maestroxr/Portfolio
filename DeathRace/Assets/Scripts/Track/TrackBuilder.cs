using System.Collections.Generic;
using Gamebox.Racing;
using UnityEngine;

namespace Portfolio.DeathRace
{
    /// <summary>
    /// Builds a track in the scene from its layout in the look of a theme: the road with its verges and curbs, the
    /// barriers and their colliders, the ground, the start line and the grid, the scenery scattered round it (the theme's
    /// props and landmarks, lamps, the set pieces of the layout) - all of it static, and all of it seeded by the layout,
    /// so a track looks the same every time. Missing art never stops a build: plain coloured stand-ins take its place.
    ///
    /// The track is built at its parent's origin (keep the parent at the identity: the race reads world positions off
    /// the track). The colliders are built once; <see cref="Dress"/> (and <see cref="Track.Restyle"/>) rebuilds only the
    /// look. The files TrackBuilder.Surface.cs and TrackBuilder.Scenery.cs hold the two halves of the look.
    /// </summary>
    public static partial class TrackBuilder
    {
        /// <summary>How tall the barriers are.</summary>
        public const float BarrierHeight = 1.05f;

        /// <summary>The barrier colliders reach from <see cref="ColliderBottom"/> to this height (far above any car).</summary>
        public const float ColliderTop = 3f;
        public const float ColliderBottom = -1f;

        /// <summary>How thick a barrier collider is (thicker than the barrier, behind its face, so nothing tunnels through).</summary>
        public const float ColliderThickness = 1.2f;

        /// <summary>The height of the ground plane (just below the road).</summary>
        public const float GroundHeight = -0.04f;

        private const string SceneryName = "Scenery";
        private const string CollidersName = "Barrier Colliders";

        // A collider segment follows the barrier until the barrier strays this far from its chord, or this long.
        private const float ColliderTolerance = 0.06f;
        private const float ColliderMaxLength = 30f;
        private const float ColliderOverlap = 0.4f;

        private static PhysicsMaterial barrierPhysics;

        /// <summary>
        /// Builds the track of <paramref name="layout"/> under <paramref name="parent"/> in the look of
        /// <paramref name="theme"/> (either may be missing: the track then wears plain stand-ins).
        /// </summary>
        public static Track Build(TrackLayout layout, DeathRaceTheme theme, DeathRaceContent content, Transform parent)
        {
            var root = new GameObject($"Track {layout.Name}");
            root.transform.SetParent(parent, false);
            root.isStatic = true;
            Track track = root.AddComponent<Track>();
            track.Setup(layout, TrackGeometry.CreatePath(layout));
            BuildColliders(track);
            Dress(track, theme, content);
            return track;
        }

        /// <summary>(Re)builds everything the eye sees of a track in a look: the old scenery goes, the colliders stay.</summary>
        public static void Dress(Track track, DeathRaceTheme theme, DeathRaceContent content)
        {
            track.Remember(theme, content);
            Transform old = track.transform.Find(SceneryName);
            if (old != null)
            {
                old.name = SceneryName + " (old)";
                old.gameObject.SetActive(false);
                Discard(old.gameObject);
            }
            var scenery = new GameObject(SceneryName);
            scenery.transform.SetParent(track.transform, false);
            scenery.isStatic = true;
            var dressing = new Dressing(track, theme, content, scenery.AddComponent<TrackScenery>());
            BuildSurface(dressing);
            BuildScenery(dressing);
        }

        /// <summary>The low-friction material of the barriers: a car that touches one slides along it instead of sticking.</summary>
        public static PhysicsMaterial BarrierPhysics
        {
            get
            {
                if (barrierPhysics == null)
                {
                    barrierPhysics = new PhysicsMaterial("Death Race Barrier")
                    {
                        dynamicFriction = 0.05f,
                        staticFriction = 0.05f,
                        bounciness = 0.1f,
                        frictionCombine = PhysicsMaterialCombine.Minimum,
                        bounceCombine = PhysicsMaterialCombine.Average
                    };
                }
                return barrierPhysics;
            }
        }

        /// <summary>
        /// Box colliders along both barriers on the wall layer: each follows the barrier's face for as long as the face
        /// stays within a few centimetres of a straight line, and overlaps the next, so the wall is closed all round.
        /// </summary>
        private static void BuildColliders(Track track)
        {
            var holder = new GameObject(CollidersName);
            holder.transform.SetParent(track.transform, false);
            holder.layer = DeathRaceLayers.Wall;
            holder.isStatic = true;
            RacePath path = track.Path;
            int count = path.Count;
            var face = new Vector2[count];
            int index = 0;
            for (int side = 1; side >= -1; side -= 2)
            {
                for (int i = 0; i < count; i++)
                {
                    float distance = i * path.Spacing;
                    face[i] = path.Samples[i] + path.NormalAt(distance) * (side * track.BarrierOffset(distance));
                }
                foreach (Vector2Int segment in Segments(face))
                {
                    Vector2 a = face[segment.x % count];
                    Vector2 b = face[segment.y % count];
                    Vector2 along = b - a;
                    float length = along.magnitude;
                    if (length < 1e-3f)
                    {
                        continue;
                    }
                    Vector2 direction = along / length;
                    Vector2 outward = new Vector2(-direction.y, direction.x) * side;
                    Vector2 centre = (a + b) * 0.5f + outward * (ColliderThickness * 0.5f);
                    var wall = new GameObject($"Wall {index++}");
                    wall.transform.SetParent(holder.transform, false);
                    wall.transform.localPosition = RacePath.ToWorld(centre, (ColliderTop + ColliderBottom) * 0.5f);
                    wall.transform.localRotation = TrackGeometry.Facing(direction);
                    wall.layer = DeathRaceLayers.Wall;
                    wall.isStatic = true;
                    var box = wall.AddComponent<BoxCollider>();
                    box.size = new Vector3(ColliderThickness, ColliderTop - ColliderBottom, length + ColliderOverlap * 2f);
                    box.sharedMaterial = BarrierPhysics;
                }
            }
        }

        /// <summary>
        /// Splits a closed line into chords (pairs of indices, the second may pass the end: take it modulo the count),
        /// each as long as the line stays within <see cref="ColliderTolerance"/> of it.
        /// </summary>
        private static IEnumerable<Vector2Int> Segments(Vector2[] line)
        {
            int count = line.Length;
            int start = 0;
            while (start < count)
            {
                int end = start + 1;
                while (end + 1 <= count && Fits(line, start, end + 1))
                {
                    end++;
                }
                yield return new Vector2Int(start, end);
                start = end;
            }
        }

        private static bool Fits(Vector2[] line, int start, int end)
        {
            int count = line.Length;
            Vector2 a = line[start % count];
            Vector2 b = line[end % count];
            Vector2 chord = b - a;
            float length = chord.magnitude;
            if (length > ColliderMaxLength)
            {
                return false;
            }
            if (length < 1e-4f)
            {
                return true;
            }
            Vector2 direction = chord / length;
            for (int i = start + 1; i < end; i++)
            {
                Vector2 offset = line[i % count] - a;
                if (Mathf.Abs(offset.x * direction.y - offset.y * direction.x) > ColliderTolerance)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>Destroys an object now in the editor (tests, previews) or at the end of the frame in play.</summary>
        internal static void Discard(Object target)
        {
            if (target == null)
            {
                return;
            }
            if (Application.isPlaying)
            {
                Object.Destroy(target);
            }
            else
            {
                Object.DestroyImmediate(target);
            }
        }

        /// <summary>A texture length that repeats a whole number of times over <paramref name="length"/> (no seam at the line).</summary>
        private static float Tile(float length, float nominal)
        {
            return length / Mathf.Max(1f, Mathf.Round(length / nominal));
        }

        /// <summary>Everything one dressing of a track works with.</summary>
        private sealed class Dressing
        {
            public Dressing(Track track, DeathRaceTheme theme, DeathRaceContent content, TrackScenery owner)
            {
                Track = track;
                Geometry = track.Geometry;
                Path = track.Path;
                Theme = theme;
                Content = content;
                Owner = owner;
                Biome = track.Layout.Biome;
                Look = theme != null ? theme.Biome(Biome) : null;
                Night = theme != null && theme.night;
                Count = Path.Count;
                Step = Path.Spacing;
                Centre = new Vector2[Count + 1];
                Normal = new Vector2[Count + 1];
                Half = new float[Count + 1];
                for (int i = 0; i <= Count; i++)
                {
                    float distance = i * Step;
                    Centre[i] = Path.Samples[i % Count];
                    Normal[i] = Path.NormalAt(distance);
                    Half[i] = Geometry.HalfWidthAt(distance);
                }
            }

            public Track Track { get; }
            public TrackGeometry Geometry { get; }
            public RacePath Path { get; }
            public DeathRaceTheme Theme { get; }
            public DeathRaceContent Content { get; }
            public TrackScenery Owner { get; }
            public Biome Biome { get; }
            public BiomeLook Look { get; }
            public bool Night { get; }

            /// <summary>Samples round the loop; the frames below have one more (the first again) to close strips.</summary>
            public int Count { get; }
            public float Step { get; }
            public Vector2[] Centre { get; }
            public Vector2[] Normal { get; }
            public float[] Half { get; }

            public Transform Root => Owner.transform;

            public Material Material(TrackSurface surface)
            {
                return TrackMaterials.For(surface, Look, Biome, Night);
            }

            /// <summary>A point of the ground plane <paramref name="offset"/> left of the centre line at sample i, at height y.</summary>
            public Vector3 At(int i, float offset, float y)
            {
                return RacePath.ToWorld(Centre[i] + Normal[i] * offset, y);
            }

            /// <summary>Adds a static object drawing <paramref name="mesh"/> in <paramref name="material"/> under the scenery.</summary>
            public GameObject Draw(string name, TrackMesh mesh, Material material, bool shadows, Transform parent = null)
            {
                var item = new GameObject(name);
                item.transform.SetParent(parent != null ? parent : Root, false);
                item.isStatic = true;
                item.AddComponent<MeshFilter>().sharedMesh = Owner.Own(mesh.ToMesh(name));
                var renderer = item.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = shadows ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off;
                return item;
            }
        }
    }
}
