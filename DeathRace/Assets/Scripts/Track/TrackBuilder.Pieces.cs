using System.Collections.Generic;
using Gamebox.Racing;
using UnityEngine;

namespace Portfolio.DeathRace
{
    /// <summary>
    /// The set pieces of the harder leagues' tracks, built from plain boxes and drums like the container stacks and the
    /// tyre walls: the storage tanks and pipe racks of Refinery Row, the snow-capped boulders an avalanche left beside
    /// Avalanche Ridge, and the pile-ups of burnt-out cars along Gridlock Boulevard (one of them still burning). Every
    /// piece stands behind the barrier, follows the bend of the road where it is long, and marks the ground it takes so
    /// the scatter of props keeps off it. The meshes are gathered per paint and drawn once all the pieces are placed.
    /// </summary>
    public static partial class TrackBuilder
    {
        private const float PipeRackReach = 14f;
        private const float PipeRackFrames = 7f;
        private const float PipeRackDepth = 3.2f;
        private const float PipeRackHeight = 4.6f;

        private static readonly Color[] TankBandColors =
        {
            new Color(0.75f, 0.2f, 0.12f, 1f), new Color(0.1f, 0.42f, 0.5f, 1f), new Color(0.85f, 0.55f, 0.1f, 1f)
        };

        private static readonly Color[] WreckPaints =
        {
            new Color(0.3f, 0.12f, 0.1f, 1f), new Color(0.14f, 0.19f, 0.27f, 1f), new Color(0.36f, 0.31f, 0.16f, 1f),
            new Color(0.18f, 0.22f, 0.18f, 1f)
        };

        /// <summary>The meshes of these set pieces, one per paint.</summary>
        private sealed class PieceMeshes
        {
            public readonly TrackMesh Concrete = new TrackMesh();
            public readonly TrackMesh TankShell = new TrackMesh();
            public readonly TrackMesh TankRoof = new TrackMesh();
            public readonly TrackMesh[] TankBands = { new TrackMesh(), new TrackMesh(), new TrackMesh() };
            public readonly TrackMesh Steel = new TrackMesh();
            public readonly TrackMesh Warning = new TrackMesh();
            public readonly TrackMesh Pipes = new TrackMesh();
            public readonly TrackMesh GasPipes = new TrackMesh();
            public readonly TrackMesh WaterPipes = new TrackMesh();
            public readonly TrackMesh Burnt = new TrackMesh();
            public readonly TrackMesh Rust = new TrackMesh();
            public readonly TrackMesh[] Paints = { new TrackMesh(), new TrackMesh(), new TrackMesh(), new TrackMesh() };
            public readonly TrackMesh Glass = new TrackMesh();
            public readonly TrackMesh Rubber = new TrackMesh();
            public readonly TrackMesh Rock = new TrackMesh();
            public readonly TrackMesh Snow = new TrackMesh();

            public void Draw(Dressing d, Transform holder)
            {
                string time = d.Night ? "night" : "day";
                DrawIfAny(d, "Tank Pads", Concrete, TrackMaterials.Plain("piece concrete", new Color(0.55f, 0.54f, 0.5f, 1f), 0.1f), true, holder);
                DrawIfAny(d, "Tanks", TankShell, TrackMaterials.Plain("tank shell", new Color(0.86f, 0.86f, 0.82f, 1f), 0.35f), true, holder);
                DrawIfAny(d, "Tank Roofs", TankRoof, TrackMaterials.Plain("tank roof", new Color(0.62f, 0.63f, 0.62f, 1f), 0.3f), true, holder);
                for (int i = 0; i < TankBands.Length; i++)
                {
                    DrawIfAny(d, $"Tank Bands {i}", TankBands[i], TrackMaterials.Plain($"tank band {i}", TankBandColors[i], 0.3f), true, holder);
                }
                DrawIfAny(d, "Steelwork", Steel, TrackMaterials.Plain("piece steel", new Color(0.24f, 0.25f, 0.27f, 1f), 0.4f), true, holder);
                DrawIfAny(d, "Warning Lights", Warning, TrackMaterials.Plain($"warning light {time}", new Color(0.9f, 0.1f, 0.05f, 1f), 0.5f,
                    new Color(1f, 0.08f, 0.04f, 1f) * (d.Night ? 3f : 0.8f)), false, holder);
                DrawIfAny(d, "Pipes", Pipes, TrackMaterials.Plain("pipe", new Color(0.62f, 0.64f, 0.66f, 1f), 0.55f), true, holder);
                DrawIfAny(d, "Gas Pipes", GasPipes, TrackMaterials.Plain("gas pipe", new Color(0.88f, 0.68f, 0.12f, 1f), 0.45f), true, holder);
                DrawIfAny(d, "Water Pipes", WaterPipes, TrackMaterials.Plain("water pipe", new Color(0.7f, 0.14f, 0.1f, 1f), 0.45f), true, holder);
                DrawIfAny(d, "Burnt Wrecks", Burnt, TrackMaterials.Plain("wreck burnt", new Color(0.1f, 0.085f, 0.08f, 1f), 0.1f), true, holder);
                DrawIfAny(d, "Rusty Wrecks", Rust, TrackMaterials.Plain("wreck rust", new Color(0.4f, 0.2f, 0.1f, 1f), 0.1f), true, holder);
                for (int i = 0; i < Paints.Length; i++)
                {
                    DrawIfAny(d, $"Wreck Paint {i}", Paints[i], TrackMaterials.Plain($"wreck paint {i}", WreckPaints[i], 0.25f), true, holder);
                }
                DrawIfAny(d, "Wreck Glass", Glass, TrackMaterials.Plain("wreck glass", new Color(0.05f, 0.06f, 0.07f, 1f), 0.8f), true, holder);
                DrawIfAny(d, "Wreck Tyres", Rubber, TrackMaterials.Plain("tyre", new Color(0.07f, 0.07f, 0.075f, 1f), 0.35f), true, holder);
                DrawIfAny(d, "Boulders", Rock, TrackMaterials.Plain("boulder", new Color(0.36f, 0.38f, 0.42f, 1f), 0.12f), true, holder);
                DrawIfAny(d, "Boulder Snow", Snow, TrackMaterials.Plain("boulder snow", new Color(0.9f, 0.93f, 0.98f, 1f), 0.3f), true, holder);
            }
        }

        /// <summary>
        /// A storage tank behind the barrier (<paramref name="back"/> metres from it to the tank's near edge): a squat drum
        /// on a concrete pad with a band of paint, a low cone of a roof with red warning lights, and a stair winding up its
        /// south face (the face the camera sees).
        /// </summary>
        private static void StorageTank(Dressing d, PieceMeshes m, float distance, float side, float back, System.Random random, List<Vector3> taken)
        {
            float radius = Range(random, 7f, 9f);
            float height = Range(random, 7.5f, 10f);
            Vector2 centre = d.Path.PointAt(distance) + d.Path.NormalAt(distance) * (side * (BehindBarrier(d, distance) + back + radius));
            Vector3 foot = RacePath.ToWorld(centre, GroundHeight);
            const float pad = 0.35f;
            m.Concrete.Cylinder(foot, radius + 0.9f, radius + 0.9f, pad, 28, Quaternion.identity);
            Vector3 shellFoot = foot + Vector3.up * pad;
            m.TankShell.Cylinder(shellFoot, radius, radius, height, 28, Quaternion.identity, false);
            m.TankBands[random.Next(m.TankBands.Length)].Cylinder(shellFoot + Vector3.up * (height * 0.6f), radius + 0.05f, radius + 0.05f, 1.1f, 28,
                Quaternion.identity, false);
            Vector3 top = shellFoot + Vector3.up * height;
            m.TankRoof.Cylinder(top, radius + 0.15f, 1.1f, radius * 0.2f, 28, Quaternion.identity);
            m.Steel.Cylinder(top + Vector3.up * (radius * 0.2f), 1.1f, 0.8f, 0.6f, 10, Quaternion.identity);
            for (int i = 0; i < 2; i++)
            {
                float angle = (i == 0 ? 200f : 340f) * Mathf.Deg2Rad;
                Vector3 at = top + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * (radius - 0.4f);
                m.Steel.Box(at, new Vector3(0.12f, 0.9f, 0.12f), Quaternion.identity);
                m.Warning.Box(at + Vector3.up * 0.9f, new Vector3(0.35f, 0.3f, 0.35f), Quaternion.identity);
            }
            // The stair: steps winding up a third of the way round the south face, a rail outside them.
            const int steps = 18;
            float first = Range(random, 215f, 235f);
            for (int i = 0; i < steps; i++)
            {
                float angle = (first + i * 110f / steps) * Mathf.Deg2Rad;
                var outward = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                Quaternion along = Quaternion.LookRotation(new Vector3(-outward.z, 0f, outward.x));
                float y = (i + 1) * height / steps;
                m.Steel.Box(shellFoot + outward * (radius + 0.55f) + Vector3.up * y, new Vector3(1f, 0.12f, 1.3f), along);
                m.Steel.Box(shellFoot + outward * (radius + 1.05f) + Vector3.up * y, new Vector3(0.06f, 1f, 1.3f), along);
            }
            taken.Add(new Vector3(centre.x, centre.y, radius + 2f));
        }

        /// <summary>
        /// A pipe rack along the back of the barrier for <see cref="PipeRackReach"/> metres either side: steel frames of two
        /// posts and two beams, and pipes on both beams (grey, one yellow gas line, one red fire main) bending with the road.
        /// </summary>
        private static void PipeRack(Dressing d, PieceMeshes m, float distance, float side, float back, System.Random random, List<Vector3> taken)
        {
            int frames = Mathf.RoundToInt(PipeRackReach * 2f / PipeRackFrames) + 1;
            var near = new Vector3[frames];
            var outward = new Vector3[frames];
            for (int f = 0; f < frames; f++)
            {
                float at = distance - PipeRackReach + f * PipeRackFrames;
                Vector2 normal = d.Path.NormalAt(at) * side;
                Vector2 spot = d.Path.PointAt(at) + normal * (BehindBarrier(d, at) + back);
                near[f] = RacePath.ToWorld(spot, GroundHeight);
                outward[f] = RacePath.ToWorld(normal);
                Quaternion facing = TrackGeometry.Facing(d.Path.TangentAt(at));
                Vector3 far = near[f] + outward[f] * PipeRackDepth;
                m.Steel.Box(near[f], new Vector3(0.3f, PipeRackHeight, 0.3f), facing);
                m.Steel.Box(far, new Vector3(0.3f, PipeRackHeight, 0.3f), facing);
                foreach (float y in new[] { 2.5f, PipeRackHeight - 0.2f })
                {
                    Vector3 middle = (near[f] + far) * 0.5f + Vector3.up * (y - 0.12f);
                    m.Steel.Box(middle, new Vector3(PipeRackDepth + 0.6f, 0.24f, 0.3f), facing);
                }
                Vector2 middleSpot = spot + normal * (PipeRackDepth * 0.5f);
                taken.Add(new Vector3(middleSpot.x, middleSpot.y, PipeRackDepth * 0.5f + 1.2f));
            }
            // depth across the rack, height of the beam it lies on, radius, paint (0 grey, 1 gas, 2 water)
            float[,] pipes =
            {
                { 0.5f, PipeRackHeight, 0.32f, 0 }, { 1.25f, PipeRackHeight, 0.22f, 1 }, { 2f, PipeRackHeight, 0.4f, 0 }, { 2.75f, PipeRackHeight, 0.22f, 0 },
                { 0.7f, 2.62f, 0.36f, 2 }, { 1.6f, 2.62f, 0.26f, 0 }, { 2.5f, 2.62f, 0.3f, 0 }
            };
            for (int p = 0; p < pipes.GetLength(0); p++)
            {
                TrackMesh mesh = pipes[p, 3] > 1.5f ? m.WaterPipes : pipes[p, 3] > 0.5f ? m.GasPipes : m.Pipes;
                float radius = pipes[p, 2];
                for (int f = 0; f + 1 < frames; f++)
                {
                    Vector3 from = near[f] + outward[f] * pipes[p, 0] + Vector3.up * (pipes[p, 1] + radius);
                    Vector3 to = near[f + 1] + outward[f + 1] * pipes[p, 0] + Vector3.up * (pipes[p, 1] + radius);
                    Vector3 run = to - from;
                    float length = run.magnitude;
                    Vector3 direction = run / Mathf.Max(1e-3f, length);
                    // A little longer than the gap, so the pipe has no seams where it bends with the road.
                    m.Steel.Cylinder(from, radius * 1.25f, radius * 1.25f, 0.2f, 8, Quaternion.FromToRotation(Vector3.up, direction), false);
                    mesh.Cylinder(from - direction * (radius * 0.5f), radius, radius, length + radius, 8, Quaternion.FromToRotation(Vector3.up, direction), false);
                }
            }
        }

        /// <summary>
        /// A pile-up of four or five burnt-out, rusted or faded cars shoved against the back of the barrier, a few of them
        /// on their sides or roofs; one of them still burns.
        /// </summary>
        private static void Wrecks(Dressing d, Transform holder, PieceMeshes m, float distance, float side, float back, System.Random random, List<Vector3> taken)
        {
            int cars = random.Next(4, 6);
            int burning = random.Next(cars);
            for (int i = 0; i < cars; i++)
            {
                float at = distance + (i - (cars - 1) * 0.5f) * 5.2f + Range(random, -0.8f, 0.8f);
                Vector2 normal = d.Path.NormalAt(at) * side;
                float depth = BehindBarrier(d, at) + back + 1.2f + Range(random, 0f, 2.6f);
                Vector2 spot = d.Path.PointAt(at) + normal * depth;
                Vector2 tangent = d.Path.TangentAt(at);
                float yaw = Mathf.Atan2(tangent.x, tangent.y) * Mathf.Rad2Deg + Range(random, -50f, 50f) + (random.Next(2) == 0 ? 180f : 0f);
                int pose = random.Next(5);
                float roll = pose == 0 ? 90f : pose == 1 ? 180f : 0f;
                float lift = pose == 0 ? 0.95f : 0.75f;
                Quaternion rotation = Quaternion.Euler(0f, yaw, 0f) * Quaternion.Euler(0f, 0f, roll);
                Vector3 centre = RacePath.ToWorld(spot, GroundHeight + lift);
                int paint = random.Next(4);
                TrackMesh body = paint == 0 ? m.Burnt : paint == 1 ? m.Rust : m.Paints[random.Next(m.Paints.Length)];
                Hulk(m, body, centre, rotation, random);
                if (i == burning)
                {
                    Fire(d, holder, centre + Vector3.up * 0.4f, 0.75f, false, random);
                }
                taken.Add(new Vector3(spot.x, spot.y, 2.4f));
            }
        }

        /// <summary>One wreck: a chassis, a cabin with blacked-out windows, and the wheels it still has.</summary>
        private static void Hulk(PieceMeshes m, TrackMesh body, Vector3 centre, Quaternion rotation, System.Random random)
        {
            BoxAt(body, centre + rotation * new Vector3(0f, -0.15f, 0f), new Vector3(1.9f, 0.62f, 4.3f), rotation);
            BoxAt(random.Next(3) == 0 ? m.Burnt : body, centre + rotation * new Vector3(0f, 0.42f, -0.25f), new Vector3(1.6f, 0.55f, 2.1f), rotation);
            BoxAt(m.Glass, centre + rotation * new Vector3(0f, 0.45f, -0.25f), new Vector3(1.64f, 0.32f, 1.7f), rotation);
            Quaternion wheel = rotation * Quaternion.Euler(0f, 0f, 90f);
            for (int w = 0; w < 4; w++)
            {
                if (random.Next(5) == 0)
                {
                    continue;
                }
                float x = w % 2 == 0 ? -0.98f : 0.98f;
                float z = w < 2 ? 1.35f : -1.35f;
                Vector3 hub = centre + rotation * new Vector3(x, -0.37f, z);
                m.Rubber.Cylinder(hub - wheel * Vector3.up * 0.13f, 0.38f, 0.38f, 0.26f, 10, wheel);
            }
        }

        /// <summary>Six to nine boulders strewn behind the barrier for a dozen metres either side, each in its drift of snow.</summary>
        private static void Boulders(Dressing d, PieceMeshes m, float distance, float side, float back, System.Random random, List<Vector3> taken)
        {
            int count = random.Next(6, 10);
            for (int i = 0; i < count; i++)
            {
                float at = distance + Range(random, -12f, 12f);
                Vector2 normal = d.Path.NormalAt(at) * side;
                float size = Range(random, 1.1f, 3f);
                Vector2 spot = d.Path.PointAt(at) + normal * (BehindBarrier(d, at) + back + size + Range(random, 0f, 5f));
                Vector3 foot = RacePath.ToWorld(spot, GroundHeight - 0.2f);
                int sides = random.Next(5, 8);
                float top = size * Range(random, 0.4f, 0.7f);
                float height = size * Range(random, 0.7f, 1.2f);
                Quaternion tilt = Quaternion.Euler(Range(random, -12f, 12f), Range(random, 0f, 360f), Range(random, -12f, 12f));
                m.Snow.Cylinder(RacePath.ToWorld(spot, GroundHeight), size * 1.45f, size * 0.85f, 0.4f, 8, Quaternion.Euler(0f, Range(random, 0f, 360f), 0f));
                m.Rock.Cylinder(foot, size, top, height, sides, tilt);
                m.Snow.Cylinder(foot + tilt * Vector3.up * (height - 0.05f), top * 1.08f, top * 0.55f, Mathf.Max(0.2f, size * 0.18f), sides, tilt);
                taken.Add(new Vector3(spot.x, spot.y, size * 1.45f));
            }
        }

        /// <summary>How far the back of the barrier is from the centre line at <paramref name="distance"/>.</summary>
        private static float BehindBarrier(Dressing d, float distance)
        {
            return d.Geometry.BarrierOffset(distance) + TrackGeometry.BarrierThickness;
        }

        /// <summary>A box by its centre (the mesh's boxes stand on their foot).</summary>
        private static void BoxAt(TrackMesh mesh, Vector3 centre, Vector3 size, Quaternion rotation)
        {
            mesh.Box(centre - rotation * new Vector3(0f, size.y * 0.5f, 0f), size, rotation);
        }
    }
}
