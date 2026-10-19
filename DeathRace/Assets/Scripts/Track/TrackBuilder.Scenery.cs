using System.Collections.Generic;
using Gamebox.Racing;
using UnityEngine;

namespace Portfolio.DeathRace
{
    /// <summary>
    /// The scenery half of a track's look: the set pieces of the layout (fire pits, container stacks, tyre walls), a few
    /// landmarks in the big open spaces, lamps along the road (lit at night, a handful of them with real lights), and the
    /// theme's props scattered round it all. The scatter keeps off the road and the barriers, keeps the gaps between two
    /// close parts of the track low and open, lets things grow taller only as they stand further off (landmarks keep
    /// well away south of the road, where they would stand between the camera, which looks north, and the cars), and
    /// grows things bigger further out. At the end the static meshes are merged per
    /// material into a few chunks, so hundreds of props cost a handful of draw calls.
    /// </summary>
    public static partial class TrackBuilder
    {
        /// <summary>At most this many real lights per track (fire pits first, then lamps at night).</summary>
        public const int MaxLights = 8;

        private const float ScatterReach = 60f;
        private const int MaxProps = 600;
        private const float LampSpacing = 42f;
        private const float LampBack = 1.4f;
        private const float ChunkSize = 64f;

        // Props of the theme that belong to the fire pit set pieces (ART-WORLD names them so).
        private const string FirePitProp = "FirePit";
        private const string BurningBarrelProp = "BurningBarrel";

        private static void BuildScenery(Dressing d)
        {
            var field = new Field(d);
            var taken = new List<Vector3>();
            Transform holder = new GameObject("Static").transform;
            holder.SetParent(d.Root, false);
            holder.gameObject.isStatic = true;
            Templates templates = new Templates(d);
            int lights = BuildSetPieces(d, taken, holder);
            BuildLandmarks(d, field, taken, holder, templates);
            BuildLamps(d, field, taken, holder, templates, MaxLights - lights);
            ScatterProps(d, field, taken, holder, templates);
            templates.Dispose();
            Combine(d, holder);
        }

        // ---------------------------------------------------------------- set pieces

        private static int BuildSetPieces(Dressing d, List<Vector3> taken, Transform holder)
        {
            TrackLayout.SetPiece[] pieces = d.Track.Layout.SetPieces;
            if (pieces == null || pieces.Length == 0)
            {
                return 0;
            }
            var stone = new TrackMesh();
            var coals = new TrackMesh();
            var rubber = new TrackMesh();
            var rims = new TrackMesh();
            var containers = new TrackMesh[ContainerColors.Length];
            for (int i = 0; i < containers.Length; i++)
            {
                containers[i] = new TrackMesh();
            }
            var random = new System.Random(d.Track.Layout.Seed * 31 + 5);
            GameObject pit = Named(d, FirePitProp);
            GameObject barrel = Named(d, BurningBarrelProp);
            int lights = 0;
            foreach (TrackLayout.SetPiece piece in pieces)
            {
                float distance = d.Path.Wrap(piece.Distance);
                float side = piece.Side >= 0f ? 1f : -1f;
                Vector2 centre = d.Path.PointAt(distance);
                Vector2 normal = d.Path.NormalAt(distance) * side;
                Vector2 tangent = d.Path.TangentAt(distance);
                float back = d.Geometry.BarrierOffset(distance) + TrackGeometry.BarrierThickness;
                switch (piece.Kind)
                {
                    case SetPieceKind.FirePit:
                    {
                        Vector2 spot = centre + normal * (back + (piece.Offset > 0f ? piece.Offset : 4.5f));
                        Vector3 world = RacePath.ToWorld(spot, GroundHeight);
                        if (pit != null)
                        {
                            Place(pit, holder, spot, Range(random, 0f, 360f), 1f, "Set Piece");
                        }
                        else
                        {
                            FirePit(stone, coals, world, random);
                        }
                        Fire(d, holder, world, 1.6f, lights < MaxLights, random);
                        lights++;
                        if (barrel != null)
                        {
                            for (int flank = -1; flank <= 1; flank += 2)
                            {
                                Vector2 beside = spot + tangent * (flank * 5f) + normal * Range(random, -0.5f, 1.5f);
                                Place(barrel, holder, beside, Range(random, 0f, 360f), 1f, "Set Piece");
                                Fire(d, holder, RacePath.ToWorld(beside, GroundHeight + PropInfo.Of(barrel).Height), 0.5f, false, random);
                                taken.Add(new Vector3(beside.x, beside.y, 1f));
                            }
                        }
                        taken.Add(new Vector3(spot.x, spot.y, 3.2f));
                        break;
                    }
                    case SetPieceKind.Containers:
                    {
                        Vector2 spot = centre + normal * (back + (piece.Offset > 0f ? piece.Offset : 4.5f));
                        ContainerStack(containers, RacePath.ToWorld(spot, GroundHeight), TrackGeometry.Facing(tangent), random);
                        taken.Add(new Vector3(spot.x, spot.y, 7f));
                        break;
                    }
                    default:
                    {
                        TyreWall(d, rubber, rims, distance, side, piece.Offset > 0f ? piece.Offset : 0.9f, taken);
                        break;
                    }
                }
            }
            DrawIfAny(d, "Fire Pit Stones", stone, TrackMaterials.Plain("stone", new Color(0.36f, 0.33f, 0.3f, 1f), 0.1f), true, holder);
            DrawIfAny(d, "Fire Pit Coals", coals, TrackMaterials.Plain("coals", new Color(0.12f, 0.07f, 0.05f, 1f), 0.1f, new Color(1.6f, 0.45f, 0.08f, 1f)), false, holder);
            DrawIfAny(d, "Tyres", rubber, TrackMaterials.Plain("tyre", new Color(0.07f, 0.07f, 0.075f, 1f), 0.35f), true, holder);
            DrawIfAny(d, "Tyre Paint", rims, TrackMaterials.Plain(d.Night ? "tyre paint night" : "tyre paint", new Color(0.92f, 0.9f, 0.85f, 1f), 0.3f, d.Night ? TrackMaterials.NeonColor(d.Biome) * 0.6f : default), true, holder);
            for (int i = 0; i < containers.Length; i++)
            {
                DrawIfAny(d, $"Containers {i}", containers[i], TrackMaterials.Plain($"container {i}", ContainerColors[i], 0.25f), true, holder);
            }
            return Mathf.Min(lights, MaxLights);
        }

        private static readonly Color[] ContainerColors =
        {
            new Color(0.55f, 0.16f, 0.1f, 1f), new Color(0.12f, 0.27f, 0.45f, 1f), new Color(0.14f, 0.4f, 0.33f, 1f),
            new Color(0.8f, 0.42f, 0.1f, 1f), new Color(0.5f, 0.5f, 0.48f, 1f)
        };

        private static void FirePit(TrackMesh stone, TrackMesh coals, Vector3 centre, System.Random random)
        {
            const int stones = 10;
            for (int i = 0; i < stones; i++)
            {
                float angle = (i + (float)random.NextDouble() * 0.3f) * Mathf.PI * 2f / stones;
                var offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * 2.2f;
                var size = new Vector3(Range(random, 0.9f, 1.3f), Range(random, 0.6f, 0.9f), Range(random, 0.7f, 1f));
                stone.Box(centre + offset, size, Quaternion.Euler(0f, -angle * Mathf.Rad2Deg + Range(random, -12f, 12f), 0f));
            }
            coals.Cylinder(centre, 1.8f, 1.6f, 0.35f, 12, Quaternion.identity);
        }

        /// <summary>
        /// The theme's prop of that name for the land (the fire pit and burning barrel props are kept for the set pieces
        /// and never scattered), or null.
        /// </summary>
        private static GameObject Named(Dressing d, string name)
        {
            foreach (GameObject prop in Usable(d.Look != null ? d.Look.props : null))
            {
                if (prop.name == name)
                {
                    return prop;
                }
            }
            return null;
        }

        /// <summary>The flames of a fire (the content's fire effect) and, while the budget lasts, a flickering light.</summary>
        private static void Fire(Dressing d, Transform holder, Vector3 centre, float size, bool light, System.Random random)
        {
            GameObject fire = d.Content != null ? d.Content.fire : null;
            if (fire != null)
            {
                GameObject flames = Object.Instantiate(fire, holder);
                flames.name = "Fire";
                flames.transform.localPosition = centre + Vector3.up * 0.35f;
                flames.transform.localScale = Vector3.one * size;
            }
            else
            {
                TrackMaterials.Warn("no fire", "Death Race track: the content has no fire effect yet; the fire pits only glow.");
            }
            if (light)
            {
                var glow = new GameObject("Fire Light");
                glow.transform.SetParent(holder, false);
                glow.transform.localPosition = centre + Vector3.up * 2.5f;
                var lamp = glow.AddComponent<Light>();
                lamp.type = LightType.Point;
                lamp.color = new Color(1f, 0.55f, 0.2f, 1f);
                lamp.range = 16f;
                lamp.intensity = d.Night ? 4f : 2.5f;
                lamp.shadows = LightShadows.None;
                glow.AddComponent<TrackFlicker>().Setup(lamp.intensity, (float)random.NextDouble() * 100f);
            }
        }

        private static void ContainerStack(TrackMesh[] containers, Vector3 centre, Quaternion facing, System.Random random)
        {
            var size = new Vector3(2.45f, 2.6f, 6.1f);
            int columns = random.Next(1, 3);
            for (int column = 0; column < columns; column++)
            {
                int height = random.Next(1, 4);
                Vector3 foot = centre + facing * new Vector3((column - (columns - 1) * 0.5f) * 2.6f, 0f, Range(random, -0.6f, 0.6f));
                for (int level = 0; level < height; level++)
                {
                    Quaternion turn = facing * Quaternion.Euler(0f, Range(random, -4f, 4f), 0f);
                    containers[random.Next(containers.Length)].Box(foot + Vector3.up * (level * size.y), size, turn);
                }
            }
        }

        /// <summary>Stacks of tyres following the back of the barrier for a dozen metres either side of a corner.</summary>
        private static void TyreWall(Dressing d, TrackMesh rubber, TrackMesh rims, float distance, float side, float back, List<Vector3> taken)
        {
            for (float along = -12f; along <= 12f; along += 1.45f)
            {
                float at = distance + along;
                Vector2 spot = d.Path.PointAt(at) + d.Path.NormalAt(at) * (side * (d.Geometry.BarrierOffset(at) + TrackGeometry.BarrierThickness + back));
                Vector3 foot = RacePath.ToWorld(spot, GroundHeight);
                int height = 3 + Mathf.RoundToInt(Mathf.PingPong(along * 0.37f, 1f));
                for (int level = 0; level < height; level++)
                {
                    Vector3 at3 = foot + Vector3.up * (level * 0.3f);
                    rubber.Cylinder(at3, 0.66f, 0.66f, 0.28f, 10, Quaternion.identity, false);
                    TrackMesh paint = level % 2 == 0 ? rims : rubber;
                    paint.Cylinder(at3 + Vector3.up * 0.28f, 0.66f, 0.3f, 0.02f, 10, Quaternion.identity, false);
                }
                taken.Add(new Vector3(spot.x, spot.y, 0.8f));
            }
        }

        private static void DrawIfAny(Dressing d, string name, TrackMesh mesh, Material material, bool shadows, Transform holder)
        {
            if (!mesh.IsEmpty)
            {
                d.Draw(name, mesh, material, shadows, holder);
            }
        }

        // ---------------------------------------------------------------- landmarks

        private static void BuildLandmarks(Dressing d, Field field, List<Vector3> taken, Transform holder, Templates templates)
        {
            GameObject[] landmarks = Usable(d.Look != null ? d.Look.landmarks : null);
            if (landmarks.Length == 0)
            {
                landmarks = templates.Landmarks;
            }
            if (landmarks.Length == 0)
            {
                return;
            }
            var random = new System.Random(d.Track.Layout.Seed * 31 + 11);
            int wanted = Mathf.Clamp(Mathf.RoundToInt(d.Path.Length / 320f), 2, 4);
            Bounds area = d.Track.Bounds;
            var placed = new List<Vector2>();
            int first = random.Next(landmarks.Length);
            for (int n = 0; n < wanted; n++)
            {
                GameObject prefab = landmarks[(first + n) % landmarks.Length];
                PropInfo info = PropInfo.Of(prefab);
                float best = float.MinValue;
                Vector2 bestSpot = Vector2.zero;
                int bestSample = 0;
                for (float x = area.min.x - ScatterReach; x <= area.max.x + ScatterReach; x += 12f)
                {
                    for (float z = area.min.z - ScatterReach; z <= area.max.z + ScatterReach; z += 12f)
                    {
                        var spot = new Vector2(x + Range(random, -4f, 4f), z + Range(random, -4f, 4f));
                        float clearance = field.Clearance(spot, out int sample);
                        float need = 14f + info.Radius;
                        if (spot.y < d.Path.Samples[sample].y)
                        {
                            // South of the road it would stand between the camera and the cars: it hides ~0.6 of its height.
                            need = Mathf.Max(need, info.Radius + info.Height * 0.6f + 4f);
                        }
                        if (clearance < need || Overlaps(taken, spot, info.Radius + 4f) || Near(placed, spot, 110f))
                        {
                            continue;
                        }
                        float score = clearance < need + 35f ? 1f : 0.35f;
                        if (field.Between(spot, sample))
                        {
                            score -= 0.8f;
                        }
                        score += (float)random.NextDouble() * 0.6f;
                        if (score > best)
                        {
                            best = score;
                            bestSpot = spot;
                            bestSample = sample;
                        }
                    }
                }
                if (best == float.MinValue)
                {
                    continue;
                }
                Vector2 toRoad = d.Path.Samples[bestSample] - bestSpot;
                float yaw = Mathf.Atan2(toRoad.x, toRoad.y) * Mathf.Rad2Deg + Range(random, -25f, 25f);
                Place(prefab, holder, bestSpot, yaw, 1f, "Landmark");
                placed.Add(bestSpot);
                taken.Add(new Vector3(bestSpot.x, bestSpot.y, info.Radius));
            }
        }

        // ---------------------------------------------------------------- lamps

        private static void BuildLamps(Dressing d, Field field, List<Vector3> taken, Transform holder, Templates templates, int lightBudget)
        {
            GameObject lamp = d.Look != null ? d.Look.lamp : null;
            if (lamp == null)
            {
                if (!d.Night)
                {
                    return;
                }
                lamp = templates.Lamp;
            }
            var lamps = new List<(GameObject lamp, float distance)>();
            int index = 0;
            for (float distance = LampSpacing * 0.5f; distance < d.Path.Length - LampSpacing * 0.25f; distance += LampSpacing, index++)
            {
                float side = index % 2 == 0 ? 1f : -1f;
                Vector2 normal = d.Path.NormalAt(distance) * side;
                Vector2 spot = d.Path.PointAt(distance) + normal * (d.Geometry.BarrierOffset(distance) + TrackGeometry.BarrierThickness + LampBack);
                if (Overlaps(taken, spot, 1.5f) || field.Clearance(spot, out _) < LampBack - 0.5f)
                {
                    continue;
                }
                float yaw = Mathf.Atan2(-normal.x, -normal.y) * Mathf.Rad2Deg;
                GameObject placed = Place(lamp, holder, spot, yaw, 1f, "Lamp");
                taken.Add(new Vector3(spot.x, spot.y, 0.8f));
                lamps.Add((placed, distance));
            }
            if (!d.Night || lightBudget <= 0)
            {
                return;
            }
            // Real lights where they show most: at the line first, then at the sharpest corners, spread round the lap.
            var ranked = new List<(GameObject lamp, float distance, float score)>();
            foreach ((GameObject placed, float distance) in lamps)
            {
                float score = Mathf.Abs(d.Path.CurvatureAt(distance, 10f)) * 100f;
                if (Mathf.Abs(d.Path.Delta(d.Track.LineDistance, distance)) < LampSpacing)
                {
                    score += 100f;
                }
                ranked.Add((placed, distance, score));
            }
            ranked.Sort((a, b) => b.score.CompareTo(a.score));
            var lit = new List<float>();
            foreach ((GameObject placed, float distance, float score) in ranked)
            {
                if (lit.Count >= lightBudget)
                {
                    break;
                }
                bool crowded = false;
                foreach (float other in lit)
                {
                    crowded |= Mathf.Abs(d.Path.Delta(other, distance)) < 80f;
                }
                if (crowded)
                {
                    continue;
                }
                lit.Add(distance);
                AddLampLight(d, placed);
            }
        }

        private static void AddLampLight(Dressing d, GameObject lamp)
        {
            Transform head = lamp.transform.Find("Head");
            Vector3 position;
            if (head != null)
            {
                position = head.position;
            }
            else
            {
                Bounds bounds = PropInfo.WorldBounds(lamp);
                position = new Vector3(bounds.center.x, bounds.max.y, bounds.center.z) + lamp.transform.forward * 0.8f;
            }
            var glow = new GameObject("Lamp Light");
            glow.transform.SetParent(lamp.transform.parent, true);
            glow.transform.position = position - Vector3.up * 0.3f;
            var light = glow.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = Color.Lerp(Color.white, TrackMaterials.NeonColor(d.Biome), 0.45f);
            light.range = 26f;
            light.intensity = 3.2f;
            light.shadows = LightShadows.None;
        }

        // ---------------------------------------------------------------- props

        private static void ScatterProps(Dressing d, Field field, List<Vector3> taken, Transform holder, Templates templates)
        {
            var scattered = new List<GameObject>();
            foreach (GameObject prop in Usable(d.Look != null ? d.Look.props : null))
            {
                if (prop.name != FirePitProp && prop.name != BurningBarrelProp)
                {
                    scattered.Add(prop);
                }
            }
            GameObject[] props = scattered.Count > 0 ? scattered.ToArray() : templates.Props;
            float density = d.Look != null ? d.Look.propDensity : 1f;
            if (props.Length == 0 || density <= 0f)
            {
                return;
            }
            var infos = new PropInfo[props.Length];
            for (int i = 0; i < props.Length; i++)
            {
                infos[i] = PropInfo.Of(props[i]);
            }
            var random = new System.Random(d.Track.Layout.Seed * 31 + 23);
            float cell = 8f / Mathf.Sqrt(density);
            Bounds area = d.Track.Bounds;
            var placedProps = new SpatialCircles(8f);
            foreach (Vector3 circle in taken)
            {
                placedProps.Add(new Vector2(circle.x, circle.y), circle.z);
            }
            int placed = 0;
            for (float x = area.min.x - ScatterReach; x <= area.max.x + ScatterReach && placed < MaxProps; x += cell)
            {
                for (float z = area.min.z - ScatterReach; z <= area.max.z + ScatterReach && placed < MaxProps; z += cell)
                {
                    var spot = new Vector2(x + (float)random.NextDouble() * cell, z + (float)random.NextDouble() * cell);
                    float roll = (float)random.NextDouble();
                    int pick = random.Next(props.Length);
                    float jitter = Range(random, 0.85f, 1.15f);
                    float yaw = (float)random.NextDouble() * 360f;
                    float clearance = field.Clearance(spot, out int sample);
                    if (clearance < 1.5f || clearance > ScatterReach)
                    {
                        continue;
                    }
                    float chance = clearance < 14f ? 0.6f : clearance < 35f ? 0.32f : 0.18f;
                    bool between = field.Between(spot, sample);
                    if (between)
                    {
                        chance *= 0.5f;
                    }
                    if (roll > chance)
                    {
                        continue;
                    }
                    PropInfo info = infos[pick];
                    float scale = Mathf.Lerp(0.85f, 1.4f, Mathf.SmoothStep(0f, 1f, (clearance - 5f) / 40f)) * jitter;
                    float allowed = 1.5f + 0.9f * clearance;
                    if (between)
                    {
                        allowed = Mathf.Min(allowed, 2.2f);
                    }
                    if (info.Height * scale > allowed)
                    {
                        float fitted = allowed / Mathf.Max(0.01f, info.Height);
                        if (fitted < scale * 0.45f)
                        {
                            continue;
                        }
                        scale = fitted;
                    }
                    float radius = info.Radius * scale;
                    if (clearance < radius + 1f || placedProps.Overlaps(spot, radius * 0.9f))
                    {
                        continue;
                    }
                    Place(props[pick], holder, spot, yaw, scale, "Prop");
                    placedProps.Add(spot, radius);
                    placed++;
                }
            }
        }

        // ---------------------------------------------------------------- helpers

        private static GameObject Place(GameObject prefab, Transform holder, Vector2 spot, float yaw, float scale, string name)
        {
            GameObject item = Object.Instantiate(prefab, holder);
            item.name = $"{name} {prefab.name}";
            item.SetActive(true);
            item.transform.localPosition = RacePath.ToWorld(spot, GroundHeight);
            item.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            item.transform.localScale = prefab.transform.localScale * scale;
            foreach (Transform part in item.GetComponentsInChildren<Transform>(true))
            {
                part.gameObject.isStatic = true;
            }
            return item;
        }

        private static GameObject[] Usable(GameObject[] prefabs)
        {
            var usable = new List<GameObject>();
            if (prefabs != null)
            {
                foreach (GameObject prefab in prefabs)
                {
                    if (prefab != null)
                    {
                        usable.Add(prefab);
                    }
                }
            }
            return usable.ToArray();
        }

        private static bool Overlaps(List<Vector3> taken, Vector2 spot, float radius)
        {
            foreach (Vector3 circle in taken)
            {
                float reach = circle.z + radius;
                if ((new Vector2(circle.x, circle.y) - spot).sqrMagnitude < reach * reach)
                {
                    return true;
                }
            }
            return false;
        }

        private static bool Near(List<Vector2> spots, Vector2 spot, float distance)
        {
            foreach (Vector2 other in spots)
            {
                if ((other - spot).sqrMagnitude < distance * distance)
                {
                    return true;
                }
            }
            return false;
        }

        private static float Range(System.Random random, float min, float max)
        {
            return min + (float)random.NextDouble() * (max - min);
        }

        /// <summary>
        /// Merges every readable static mesh under <paramref name="holder"/> into one mesh per material and 64 m square,
        /// then drops the merged renderers (lights, effects and the objects themselves stay).
        /// </summary>
        private static void Combine(Dressing d, Transform holder)
        {
            var groups = new Dictionary<(Material, int, int), List<CombineInstance>>();
            var merged = new List<MeshRenderer>();
            Matrix4x4 toHolder = holder.worldToLocalMatrix;
            foreach (MeshRenderer renderer in holder.GetComponentsInChildren<MeshRenderer>())
            {
                var filter = renderer.GetComponent<MeshFilter>();
                Mesh mesh = filter != null ? filter.sharedMesh : null;
                Material[] materials = renderer.sharedMaterials;
                if (mesh == null || !mesh.isReadable || materials.Length != mesh.subMeshCount || !renderer.enabled)
                {
                    continue;
                }
                Vector3 centre = renderer.bounds.center;
                int cellX = Mathf.FloorToInt(centre.x / ChunkSize);
                int cellZ = Mathf.FloorToInt(centre.z / ChunkSize);
                bool usable = true;
                foreach (Material material in materials)
                {
                    usable &= material != null;
                }
                if (!usable)
                {
                    continue;
                }
                for (int s = 0; s < materials.Length; s++)
                {
                    var key = (materials[s], cellX, cellZ);
                    if (!groups.TryGetValue(key, out List<CombineInstance> list))
                    {
                        list = new List<CombineInstance>();
                        groups[key] = list;
                    }
                    list.Add(new CombineInstance { mesh = mesh, subMeshIndex = s, transform = toHolder * filter.transform.localToWorldMatrix });
                }
                merged.Add(renderer);
            }
            if (merged.Count < 2)
            {
                return;
            }
            var chunks = new GameObject("Merged");
            chunks.transform.SetParent(holder, false);
            chunks.isStatic = true;
            foreach (KeyValuePair<(Material, int, int), List<CombineInstance>> group in groups)
            {
                var mesh = new Mesh { name = $"Track chunk {group.Key.Item1.name}", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
                mesh.CombineMeshes(group.Value.ToArray(), true, true, false);
                var chunk = new GameObject(mesh.name);
                chunk.transform.SetParent(chunks.transform, false);
                chunk.isStatic = true;
                chunk.AddComponent<MeshFilter>().sharedMesh = d.Owner.Own(mesh);
                chunk.AddComponent<MeshRenderer>().sharedMaterial = group.Key.Item1;
            }
            foreach (MeshRenderer renderer in merged)
            {
                renderer.enabled = false;
                var filter = renderer.GetComponent<MeshFilter>();
                Discard(renderer);
                Discard(filter);
            }
        }

        /// <summary>The size of a prop or landmark (from its meshes, without placing it), for the scatter's spacing rules.</summary>
        private struct PropInfo
        {
            public float Radius;
            public float Height;

            private static readonly Dictionary<GameObject, PropInfo> cache = new Dictionary<GameObject, PropInfo>();

            public static PropInfo Of(GameObject prefab)
            {
                if (cache.TryGetValue(prefab, out PropInfo info))
                {
                    return info;
                }
                Bounds bounds = LocalBounds(prefab);
                info = new PropInfo
                {
                    Radius = Mathf.Max(0.3f, Mathf.Max(Mathf.Max(-bounds.min.x, bounds.max.x), Mathf.Max(-bounds.min.z, bounds.max.z))),
                    Height = Mathf.Max(0.2f, bounds.max.y)
                };
                if (prefab.scene.IsValid())
                {
                    return info;
                }
                cache[prefab] = info;
                return info;
            }

            /// <summary>The meshes' box in the prefab's own space (scale included).</summary>
            public static Bounds LocalBounds(GameObject prefab)
            {
                Matrix4x4 toRoot = Matrix4x4.Scale(prefab.transform.localScale) * prefab.transform.worldToLocalMatrix;
                return MeshBounds(prefab, toRoot);
            }

            public static Bounds WorldBounds(GameObject item)
            {
                return MeshBounds(item, Matrix4x4.identity);
            }

            private static Bounds MeshBounds(GameObject root, Matrix4x4 transform)
            {
                bool any = false;
                var bounds = new Bounds();
                foreach (MeshFilter filter in root.GetComponentsInChildren<MeshFilter>(true))
                {
                    Mesh mesh = filter.sharedMesh;
                    if (mesh == null)
                    {
                        continue;
                    }
                    Matrix4x4 matrix = transform * filter.transform.localToWorldMatrix;
                    Bounds local = mesh.bounds;
                    for (int corner = 0; corner < 8; corner++)
                    {
                        var point = new Vector3(
                            (corner & 1) == 0 ? local.min.x : local.max.x,
                            (corner & 2) == 0 ? local.min.y : local.max.y,
                            (corner & 4) == 0 ? local.min.z : local.max.z);
                        Vector3 world = matrix.MultiplyPoint3x4(point);
                        if (!any)
                        {
                            bounds = new Bounds(world, Vector3.zero);
                            any = true;
                        }
                        else
                        {
                            bounds.Encapsulate(world);
                        }
                    }
                }
                return any ? bounds : new Bounds(Vector3.up * 0.5f, Vector3.one);
            }
        }

        /// <summary>Circles on the ground plane in a coarse grid, for quick overlap tests among hundreds of props.</summary>
        private sealed class SpatialCircles
        {
            private readonly float cell;
            private readonly Dictionary<long, List<Vector3>> cells = new Dictionary<long, List<Vector3>>();
            private float largest;

            public SpatialCircles(float cell)
            {
                this.cell = cell;
            }

            public void Add(Vector2 centre, float radius)
            {
                long key = Key(Mathf.FloorToInt(centre.x / cell), Mathf.FloorToInt(centre.y / cell));
                if (!cells.TryGetValue(key, out List<Vector3> list))
                {
                    list = new List<Vector3>();
                    cells[key] = list;
                }
                list.Add(new Vector3(centre.x, centre.y, radius));
                largest = Mathf.Max(largest, radius);
            }

            public bool Overlaps(Vector2 centre, float radius)
            {
                int reach = Mathf.CeilToInt((radius + largest) / cell);
                int cx = Mathf.FloorToInt(centre.x / cell);
                int cz = Mathf.FloorToInt(centre.y / cell);
                for (int x = cx - reach; x <= cx + reach; x++)
                {
                    for (int z = cz - reach; z <= cz + reach; z++)
                    {
                        if (!cells.TryGetValue(Key(x, z), out List<Vector3> list))
                        {
                            continue;
                        }
                        foreach (Vector3 circle in list)
                        {
                            float both = circle.z + radius;
                            if ((new Vector2(circle.x, circle.y) - centre).sqrMagnitude < both * both)
                            {
                                return true;
                            }
                        }
                    }
                }
                return false;
            }

            private static long Key(int x, int z)
            {
                return ((long)x << 32) ^ (uint)z;
            }
        }

        /// <summary>
        /// Where the road is, for the scatter: the centre line's samples in a grid, to find how far a point is from the
        /// back of the nearest barrier and whether it lies in a gap between two parts of the track.
        /// </summary>
        private sealed class Field
        {
            private const float Cell = 16f;
            private const int MaxRings = 7;

            private readonly Dressing d;
            private readonly Dictionary<long, List<int>> cells = new Dictionary<long, List<int>>();

            public Field(Dressing dressing)
            {
                d = dressing;
                for (int i = 0; i < d.Count; i++)
                {
                    Vector2 point = d.Centre[i];
                    long key = Key(Mathf.FloorToInt(point.x / Cell), Mathf.FloorToInt(point.y / Cell));
                    if (!cells.TryGetValue(key, out List<int> list))
                    {
                        list = new List<int>();
                        cells[key] = list;
                    }
                    list.Add(i);
                }
            }

            /// <summary>How far <paramref name="point"/> is behind the back of the nearest barrier (negative: on the track).</summary>
            public float Clearance(Vector2 point, out int sample)
            {
                float distance = Nearest(point, -1, out sample);
                return distance - (d.Half[sample] + TrackGeometry.Shoulder + TrackGeometry.BarrierThickness);
            }

            /// <summary>Whether another part of the track (not the one near <paramref name="sample"/>) is also close by.</summary>
            public bool Between(Vector2 point, int sample)
            {
                float other = Nearest(point, sample, out int otherSample);
                return other - d.Half[otherSample] < 40f;
            }

            /// <summary>
            /// The distance to the nearest sample; with <paramref name="awayFrom"/> >= 0, the nearest one at least 80 m
            /// along the track from that sample (another part of the track).
            /// </summary>
            private float Nearest(Vector2 point, int awayFrom, out int sample)
            {
                int cx = Mathf.FloorToInt(point.x / Cell);
                int cz = Mathf.FloorToInt(point.y / Cell);
                float best = float.MaxValue;
                sample = 0;
                int apart = Mathf.CeilToInt(80f / d.Step);
                for (int ring = 0; ring <= MaxRings; ring++)
                {
                    for (int x = cx - ring; x <= cx + ring; x++)
                    {
                        for (int z = cz - ring; z <= cz + ring; z++)
                        {
                            if (Mathf.Max(Mathf.Abs(x - cx), Mathf.Abs(z - cz)) != ring || !cells.TryGetValue(Key(x, z), out List<int> list))
                            {
                                continue;
                            }
                            foreach (int i in list)
                            {
                                if (awayFrom >= 0)
                                {
                                    int gap = Mathf.Abs(i - awayFrom);
                                    if (Mathf.Min(gap, d.Count - gap) < apart)
                                    {
                                        continue;
                                    }
                                }
                                float squared = (d.Centre[i] - point).sqrMagnitude;
                                if (squared < best)
                                {
                                    best = squared;
                                    sample = i;
                                }
                            }
                        }
                    }
                    if (best < float.MaxValue && Mathf.Sqrt(best) < ring * Cell)
                    {
                        break;
                    }
                }
                return best < float.MaxValue ? Mathf.Sqrt(best) : 1e6f;
            }

            private static long Key(int x, int z)
            {
                return ((long)x << 32) ^ (uint)z;
            }
        }
    }
}
