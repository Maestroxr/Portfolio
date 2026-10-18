using System.Collections.Generic;
using UnityEngine;

namespace Portfolio.Asteroids.EditorTools
{
    /// <summary>
    /// The models of the Iron Tide and Hive Fall campaigns: the props of their worlds (snowy pines, ice blocks, egg pods,
    /// tendrils, crystal shards) and their ground units, in the ground palette like the others (the Iron Tide in white and
    /// steel with orange marks, the Hive in flesh and chitin with glowing ichor, the crystal pylon in glowing rose).
    /// </summary>
    internal static partial class SpaceModels
    {
        // ------------------------------------------------------------------ aircraft

        /// <summary>
        /// The Iron Tide's attack drone (1.5 m across): a hexagonal pod under a dark dome, a red eye at the nose and four
        /// ducted rotors on arms. +Y up, +Z the nose, as the other models.
        /// </summary>
        public static MeshBuilder DroneBody()
        {
            var b = new MeshBuilder();
            b.Color(Swatch.Hull);
            b.Cylinder(new Vector3(0f, 0.1f, 0f), 0.42f, 0.36f, 0.28f, 6, false, true, true, Mathf.PI / 6f);
            b.Color(Swatch.Gunmetal);
            b.Sphere(new Vector3(0f, 0.38f, 0f), new Vector3(0.3f, 0.16f, 0.3f), 4, 10, true, 0.5f);
            b.Color(Swatch.Orange);
            b.Box(new Vector3(0f, 0.39f, -0.2f), new Vector3(0.3f, 0.02f, 0.08f));
            b.Color(Swatch.GlowRed);
            b.Sphere(new Vector3(0f, 0.24f, 0.36f), 0.09f, 3, 8);
            for (int i = 0; i < 4; i++)
            {
                float angle = (i * 90f + 45f) * Mathf.Deg2Rad;
                var end = new Vector3(Mathf.Cos(angle) * 0.66f, 0.24f, Mathf.Sin(angle) * 0.66f);
                b.Color(Swatch.Gunmetal);
                b.Rod(new Vector3(0f, 0.24f, 0f), end, 0.05f, 5);
                b.Color(Swatch.Orange);
                b.Torus(end, 0.27f, 0.04f, 14, 4);
                b.Color(Swatch.Black);
                b.Cylinder(end - Vector3.up * 0.03f, 0.07f, 0.07f, 0.08f, 6);
                b.Push(end, Quaternion.Euler(0f, i * 37f, 0f));
                b.Box(Vector3.zero, new Vector3(0.48f, 0.015f, 0.05f));
                b.Pop();
            }
            return b;
        }


        /// <summary>
        /// The Hive's stinger (2.2 m long): a striped abdomen ending in a glowing sting, a thorax with four veined wings and
        /// six legs, a head with green eyes and mandibles. +Y up, +Z the nose.
        /// </summary>
        public static MeshBuilder StingerBody()
        {
            var b = new MeshBuilder();
            b.Color(Swatch.Purple);
            b.Sphere(new Vector3(0f, 0.35f, -0.72f), new Vector3(0.34f, 0.28f, 0.58f), 6, 12);
            b.Color(Swatch.CrystalDark);
            for (int i = 0; i < 3; i++)
            {
                b.Torus(new Vector3(0f, 0.35f, -0.5f - i * 0.22f), 0.31f - i * 0.04f, 0.04f, 14, 4);
            }
            b.Color(Swatch.GlowMagenta);
            b.Rod(new Vector3(0f, 0.33f, -1.25f), new Vector3(0f, 0.3f, -1.5f), 0.07f, 6, true, 0.005f);
            b.Color(Swatch.CrystalDark);
            b.Sphere(new Vector3(0f, 0.4f, 0.05f), new Vector3(0.28f, 0.26f, 0.34f), 5, 10);
            b.Color(Swatch.Basalt);
            b.Sphere(new Vector3(0f, 0.37f, 0.52f), new Vector3(0.22f, 0.19f, 0.24f), 5, 10);
            b.Color(Swatch.GlowGreen);
            for (int side = -1; side <= 1; side += 2)
            {
                b.Sphere(new Vector3(side * 0.12f, 0.45f, 0.64f), 0.07f, 3, 6);
            }
            b.Color(Swatch.Basalt);
            for (int side = -1; side <= 1; side += 2)
            {
                b.Rod(new Vector3(side * 0.08f, 0.3f, 0.7f), new Vector3(side * 0.17f, 0.28f, 0.95f), 0.03f, 4, true, 0.01f);
                for (int leg = 0; leg < 3; leg++)
                {
                    var hip = new Vector3(side * 0.18f, 0.25f, 0.18f - leg * 0.18f);
                    var knee = new Vector3(side * 0.48f, 0.3f, 0.25f - leg * 0.24f);
                    var foot = new Vector3(side * 0.6f, 0.05f, 0.2f - leg * 0.3f);
                    b.Rod(hip, knee, 0.025f, 4);
                    b.Rod(knee, foot, 0.02f, 4);
                }
            }
            for (int pair = 0; pair < 2; pair++)
            {
                for (int side = -1; side <= 1; side += 2)
                {
                    b.Push(new Vector3(side * 0.12f, 0.58f, 0.12f - pair * 0.18f), Quaternion.Euler(0f, side * (pair == 0 ? -18f : 12f), side * -8f));
                    b.Color(pair == 0 ? Swatch.Violet : Swatch.Magenta);
                    b.Sphere(new Vector3(side * 0.62f, 0f, 0f), new Vector3(0.62f, 0.025f, pair == 0 ? 0.24f : 0.19f), 3, 12);
                    b.Pop();
                }
            }
            return b;
        }


        /// <summary>The Hive's mite (1.1 m long): a beetle under a split shell, green eyes, a glowing back, six short legs.</summary>
        public static MeshBuilder MiteBody()
        {
            var b = new MeshBuilder();
            b.Color(Swatch.Basalt);
            b.Sphere(new Vector3(0f, 0.25f, 0f), new Vector3(0.36f, 0.22f, 0.5f), 5, 10);
            b.Color(Swatch.Purple);
            for (int side = -1; side <= 1; side += 2)
            {
                b.Push(new Vector3(side * 0.12f, 0.36f, -0.05f), Quaternion.Euler(0f, 0f, side * -18f));
                b.Sphere(Vector3.zero, new Vector3(0.22f, 0.12f, 0.46f), 4, 10, true, 0.55f);
                b.Pop();
            }
            b.Color(Swatch.GlowGreen);
            b.Box(new Vector3(0f, 0.43f, -0.05f), new Vector3(0.05f, 0.03f, 0.6f));
            b.Sphere(new Vector3(0.1f, 0.3f, 0.45f), 0.06f, 3, 6);
            b.Sphere(new Vector3(-0.1f, 0.3f, 0.45f), 0.06f, 3, 6);
            b.Color(Swatch.CrystalDark);
            for (int side = -1; side <= 1; side += 2)
            {
                for (int leg = 0; leg < 3; leg++)
                {
                    var hip = new Vector3(side * 0.25f, 0.2f, 0.2f - leg * 0.2f);
                    b.Rod(hip, hip + new Vector3(side * 0.25f, -0.15f, 0.05f - leg * 0.06f), 0.03f, 4);
                }
                b.Rod(new Vector3(side * 0.07f, 0.28f, 0.5f), new Vector3(side * 0.16f, 0.3f, 0.72f), 0.025f, 4, true, 0.005f);
            }
            return b;
        }


        // ------------------------------------------------------------------ props

        /// <summary>The props of the campaigns' worlds (<see cref="Prop"/> hands over the kinds it does not draw itself).</summary>
        private static void CampaignProp(MeshBuilder b, PropSpot spot, Vector3 s, System.Random random)
        {
            switch (spot.Kind)
            {
                case PropKind.Pine: Pine(b, s, spot.Main, spot.Accent); break;
                case PropKind.Pod: Pod(b, s, spot.Main, spot.Accent, random); break;
                case PropKind.Tendril: Tendril(b, s, spot.Main, spot.Accent, random); break;
                case PropKind.Shard: Shard(b, s, spot.Main, spot.Accent, random); break;
                case PropKind.IceBlock: IceBlock(b, s, spot.Main, spot.Accent, random); break;
            }
        }


        /// <summary>A conifer under snow: three stacked cones of needles, each capped white.</summary>
        private static void Pine(MeshBuilder b, Vector3 s, GroundSwatch main, GroundSwatch snow)
        {
            b.Tint(GroundSwatch.Trunk).Cylinder(Vector3.zero, s.x * 0.05f, s.x * 0.04f, s.y * 0.25f, 5);
            for (int k = 0; k < 3; k++)
            {
                float y = s.y * (0.16f + k * 0.24f);
                float r = s.x * (0.48f - k * 0.13f);
                float h = s.y * 0.4f;
                b.Tint(main).Cylinder(new Vector3(0f, y, 0f), r, 0f, h, 7);
                b.Tint(snow).Cylinder(new Vector3(0f, y + h * 0.5f, 0f), r * 0.56f, 0f, h * 0.5f, 7);
            }
        }


        /// <summary>A cluster of egg pods with glowing slits.</summary>
        private static void Pod(MeshBuilder b, Vector3 s, GroundSwatch main, GroundSwatch glow, System.Random random)
        {
            int count = 3 + random.Next(3);
            for (int i = 0; i < count; i++)
            {
                float angle = i * Mathf.PI * 2f / count + Range(random, -0.3f, 0.3f);
                float d = i == 0 ? 0f : Range(random, 0.18f, 0.32f);
                var at = new Vector3(Mathf.Cos(angle) * d * s.x, 0f, Mathf.Sin(angle) * d * s.z);
                float r = (i == 0 ? 0.3f : Range(random, 0.17f, 0.24f)) * s.x;
                float h = s.y * (i == 0 ? 1f : Range(random, 0.55f, 0.8f));
                b.Tint(main).Sphere(at + Vector3.up * h * 0.45f, new Vector3(r, h * 0.55f, r), 5, 8);
                b.Tint(glow).Sphere(at + new Vector3(0f, h * 0.75f, 0f), new Vector3(r * 0.3f, h * 0.12f, r * 0.3f), 3, 6);
            }
        }


        /// <summary>A curved spike of chitin rising from the ground, thinning to its tip.</summary>
        private static void Tendril(MeshBuilder b, Vector3 s, GroundSwatch main, GroundSwatch accent, System.Random random)
        {
            float bend = Range(random, 0.2f, 0.45f) * s.x;
            float heading = Range(random, 0f, Mathf.PI * 2f);
            var side = new Vector3(Mathf.Cos(heading), 0f, Mathf.Sin(heading));
            Vector3 previous = Vector3.zero;
            const int segments = 5;
            for (int i = 1; i <= segments; i++)
            {
                float t = i / (float)segments;
                Vector3 point = side * (bend * t * t) + Vector3.up * (s.y * t);
                float radius = s.x * 0.16f * (1f - t * 0.8f);
                b.Tint(i == segments ? accent : main).Rod(previous, point, radius, 6, true, s.x * 0.16f * (1f - (t + 0.2f) * 0.8f));
                previous = point;
            }
        }


        /// <summary>A cluster of tall six-sided crystals with pointed tips, leaning apart from a rocky foot.</summary>
        private static void Shard(MeshBuilder b, Vector3 s, GroundSwatch main, GroundSwatch accent, System.Random random)
        {
            b.Tint(GroundSwatch.ChitinDark).Blob(new Vector3(0f, s.y * 0.05f, 0f), new Vector3(s.x * 0.3f, s.y * 0.1f, s.z * 0.3f), 0, 0.25f, random.Next());
            int count = 3 + random.Next(4);
            for (int i = 0; i < count; i++)
            {
                float angle = Range(random, 0f, 360f);
                float lean = i == 0 ? Range(random, 0f, 10f) : Range(random, 24f, 50f);
                float height = s.y * (i == 0 ? 1f : Range(random, 0.45f, 0.8f));
                float width = s.x * (i == 0 ? 0.13f : Range(random, 0.07f, 0.11f));
                var foot = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), 0f, Mathf.Sin(angle * Mathf.Deg2Rad)) * (i == 0 ? 0f : s.x * 0.12f);
                b.Push(foot, Quaternion.Euler(0f, angle, 0f) * Quaternion.Euler(lean, 0f, 0f));
                b.Tint(i % 3 == 2 ? accent : main).Cylinder(Vector3.zero, width, width * 0.92f, height * 0.78f, 6);
                b.Cylinder(new Vector3(0f, height * 0.78f, 0f), width * 0.92f, 0f, height * 0.22f, 6);
                b.Pop();
            }
        }


        /// <summary>A broken block of ice under a cap of snow.</summary>
        private static void IceBlock(MeshBuilder b, Vector3 s, GroundSwatch main, GroundSwatch snow, System.Random random)
        {
            b.Push(Vector3.zero, Quaternion.Euler(Range(random, -8f, 8f), 0f, Range(random, -10f, 10f)));
            b.Tint(main).BeveledBox(new Vector3(0f, s.y * 0.4f, 0f), new Vector3(s.x, s.y * 0.8f, s.z), 0.06f);
            b.Tint(snow).BeveledBox(new Vector3(0f, s.y * 0.84f, 0f), new Vector3(s.x * 0.94f, s.y * 0.14f, s.z * 0.94f), 0.04f);
            b.Pop();
            b.Tint(main).BeveledBox(new Vector3(s.x * 0.45f, s.y * 0.2f, s.z * 0.35f), new Vector3(s.x * 0.4f, s.y * 0.4f, s.z * 0.35f), 0.04f);
        }

        // ------------------------------------------------------------------ ground units

        /// <summary>The colours of the Iron Tide's units: white and steel, orange marks.</summary>
        private const GroundSwatch CorpHull = GroundSwatch.CorpWhite;
        private const GroundSwatch CorpDark = GroundSwatch.CorpSteel;
        private const GroundSwatch CorpMark = GroundSwatch.CorpOrange;


        /// <summary>The models of the campaigns' ground units (<see cref="GroundUnit"/> hands over the units it does not know).</summary>
        private static UnitModel CampaignGroundUnit(StrikeUnit unit)
        {
            switch (unit)
            {
                case StrikeUnit.SamSite: return SamSite();
                case StrikeUnit.Artillery: return Howitzer();
                case StrikeUnit.Walker: return Walker();
                case StrikeUnit.ShieldGenerator: return ShieldPylon();
                case StrikeUnit.Destroyer: return Destroyer();
                case StrikeUnit.HiveSpawner: return HiveMound();
                case StrikeUnit.Spitter: return Spitter();
                case StrikeUnit.Prism: return Prism();
                default: return null;
            }
        }


        /// <summary>A SAM site: a ring of barrier blocks, a radar mast, and a turning rack of four launch tubes tilted up.</summary>
        private static UnitModel SamSite()
        {
            var m = new UnitModel(0.82f, 1.35f) { Turret = UnitModel.Part(), TurretPivot = new Vector3(0f, 0.22f, 0f) };
            MeshBuilder b = m.Body;
            b.Tint(GroundSwatch.ConcreteDark).Cylinder(Vector3.zero, 1.3f, 1.25f, 0.08f, 8, false, true, true, Mathf.PI / 8f);
            for (int i = 0; i < 7; i++)
            {
                float angle = (i * 360f / 8f + 22.5f + 45f) * Mathf.Deg2Rad;
                b.Push(new Vector3(Mathf.Cos(angle) * 1.08f, 0.08f, Mathf.Sin(angle) * 1.08f), Quaternion.Euler(0f, -angle * Mathf.Rad2Deg + 90f, 0f));
                b.Tint(i % 2 == 0 ? CorpDark : GroundSwatch.Concrete).BeveledBox(new Vector3(0f, 0.13f, 0f), new Vector3(0.75f, 0.26f, 0.22f), 0.04f);
                b.Pop();
            }
            b.Tint(CorpDark).Rod(new Vector3(-0.85f, 0.08f, -0.55f), new Vector3(-0.85f, 0.78f, -0.55f), 0.05f, 5);
            b.Tint(CorpHull).Box(new Vector3(-0.85f, 0.8f, -0.55f), new Vector3(0.5f, 0.06f, 0.18f));
            b.Tint(GroundSwatch.GlowRed).Sphere(new Vector3(-0.85f, 0.86f, -0.55f), 0.05f, 3, 6);
            MeshBuilder t = m.Turret;
            t.Tint(CorpDark).Cylinder(Vector3.zero, 0.42f, 0.38f, 0.16f, 10);
            t.Push(new Vector3(0f, 0.38f, 0f), Quaternion.Euler(-22f, 0f, 0f));
            t.Tint(CorpHull).BeveledBox(new Vector3(0f, 0f, 0.05f), new Vector3(0.82f, 0.46f, 1.05f), 0.05f);
            for (int i = 0; i < 4; i++)
            {
                float x = i % 2 == 0 ? -0.2f : 0.2f;
                float y = i < 2 ? -0.1f : 0.12f;
                t.Tint(GroundSwatch.Black).Cylinder(new Vector3(x, y, 0.58f), 0.09f, 0.09f, 0.02f, 8);
                t.Tint(CorpMark).Cylinder(new Vector3(x, y, 0.6f), 0.05f, 0.05f, 0.02f, 6);
            }
            t.Tint(CorpMark).Box(new Vector3(0f, 0.235f, -0.1f), new Vector3(0.7f, 0.02f, 0.12f));
            t.Pop();
            // The muzzles in the turret's space: the mouths of the upper tubes (the tilt lifts them).
            Quaternion tilt = Quaternion.Euler(-22f, 0f, 0f);
            m.Muzzles.Add(new Vector3(0f, 0.38f, 0f) + tilt * new Vector3(-0.2f, 0.12f, 0.62f));
            m.Muzzles.Add(new Vector3(0f, 0.38f, 0f) + tilt * new Vector3(0.2f, 0.12f, 0.62f));
            return m;
        }


        /// <summary>A tracked howitzer: a low chassis, a boxy turret and a long barrel with a muzzle brake.</summary>
        private static UnitModel Howitzer()
        {
            var m = new UnitModel(0.86f, 1.45f) { Turret = UnitModel.Part(), TurretPivot = new Vector3(0f, 0.5f, -0.25f) };
            MeshBuilder b = m.Body;
            for (int side = -1; side <= 1; side += 2)
            {
                b.Tint(GroundSwatch.Rubber).BeveledBox(new Vector3(side * 0.66f, 0.17f, 0f), new Vector3(0.36f, 0.34f, 2.5f), 0.09f);
                b.Tint(CorpDark);
                for (int i = 0; i < 10; i++)
                {
                    b.Box(new Vector3(side * 0.66f, 0.345f, -1.12f + i * 0.25f), new Vector3(0.36f, 0.01f, 0.06f));
                }
            }
            b.Tint(CorpDark).BeveledBox(new Vector3(0f, 0.3f, -0.1f), new Vector3(1.05f, 0.3f, 2.3f), 0.07f);
            b.Tint(CorpMark).Box(new Vector3(0f, 0.455f, 1f), new Vector3(0.8f, 0.02f, 0.14f));
            b.Tint(GroundSwatch.Black).BeveledBox(new Vector3(0f, 0.32f, -1.32f), new Vector3(0.8f, 0.2f, 0.16f), 0.03f);
            MeshBuilder t = m.Turret;
            t.Tint(CorpHull).BeveledBox(new Vector3(0f, 0.02f, -0.1f), new Vector3(0.95f, 0.34f, 1.2f), 0.08f);
            t.Tint(CorpMark).Box(new Vector3(0f, 0.195f, -0.45f), new Vector3(0.75f, 0.01f, 0.18f));
            t.Tint(CorpDark).Cylinder(new Vector3(0f, 0f, 0.5f), 0.18f, 0.18f, 0.12f, 10);
            t.Tint(GroundSwatch.SteelDark).Rod(new Vector3(0f, 0.05f, 0.55f), new Vector3(0f, 0.05f, 2.35f), 0.085f, 8);
            t.Tint(GroundSwatch.Black).BeveledBox(new Vector3(0f, 0.05f, 2.28f), new Vector3(0.26f, 0.2f, 0.22f), 0.03f);
            m.Muzzles.Add(new Vector3(0f, 0.05f, 2.42f));
            return m;
        }


        /// <summary>A walker: two jointed legs with broad feet under a turning cockpit with a gun on either side.</summary>
        private static UnitModel Walker()
        {
            var m = new UnitModel(1.05f, 1.15f) { Turret = UnitModel.Part(), TurretPivot = new Vector3(0f, 0.78f, 0f) };
            MeshBuilder b = m.Body;
            for (int side = -1; side <= 1; side += 2)
            {
                var hip = new Vector3(side * 0.42f, 0.72f, 0f);
                var knee = new Vector3(side * 0.62f, 0.42f, 0.28f);
                var ankle = new Vector3(side * 0.6f, 0.12f, -0.12f);
                b.Tint(CorpDark).Rod(hip, knee, 0.11f, 6);
                b.Rod(knee, ankle, 0.09f, 6);
                b.Tint(CorpHull).Sphere(knee, 0.13f, 3, 8);
                b.Tint(GroundSwatch.SteelDark).BeveledBox(new Vector3(side * 0.6f, 0.05f, 0.02f), new Vector3(0.42f, 0.1f, 0.7f), 0.03f);
                b.Tint(CorpMark).Box(new Vector3(side * 0.6f, 0.105f, 0.32f), new Vector3(0.3f, 0.01f, 0.08f));
            }
            b.Tint(CorpDark).BeveledBox(new Vector3(0f, 0.72f, 0f), new Vector3(0.95f, 0.16f, 0.4f), 0.04f);
            MeshBuilder t = m.Turret;
            t.Tint(CorpHull).BeveledBox(new Vector3(0f, 0.12f, 0f), new Vector3(0.82f, 0.34f, 0.9f), 0.1f);
            t.Tint(GroundSwatch.Glass).BeveledBox(new Vector3(0f, 0.2f, 0.42f), new Vector3(0.48f, 0.14f, 0.08f), 0.02f);
            t.Tint(CorpMark).Box(new Vector3(0f, 0.295f, -0.1f), new Vector3(0.6f, 0.01f, 0.14f));
            for (int side = -1; side <= 1; side += 2)
            {
                t.Tint(CorpDark).BeveledBox(new Vector3(side * 0.52f, 0.08f, 0.05f), new Vector3(0.22f, 0.22f, 0.5f), 0.04f);
                t.Tint(GroundSwatch.Black).Rod(new Vector3(side * 0.52f, 0.08f, 0.3f), new Vector3(side * 0.52f, 0.08f, 0.95f), 0.05f, 6);
                m.Muzzles.Add(new Vector3(side * 0.52f, 0.08f, 0.98f));
            }
            return m;
        }


        /// <summary>A shield generator: a hexagonal plinth, a pylon wound with glowing coils and a bright emitter on top.</summary>
        private static UnitModel ShieldPylon()
        {
            var m = new UnitModel(1f, 1.4f);
            MeshBuilder b = m.Body;
            b.Tint(GroundSwatch.ConcreteDark).Cylinder(Vector3.zero, 1.3f, 1.2f, 0.14f, 6, false, true, true, Mathf.PI / 6f);
            b.Tint(CorpDark).Cylinder(new Vector3(0f, 0.14f, 0f), 0.95f, 0.85f, 0.12f, 6, false, true, true, Mathf.PI / 6f);
            for (int i = 0; i < 3; i++)
            {
                b.Push(new Vector3(0f, 0.26f, 0f), Quaternion.Euler(0f, i * 120f, 0f));
                b.Tint(CorpHull).BeveledBox(new Vector3(0f, 0.25f, 0.55f), new Vector3(0.14f, 0.5f, 0.5f), 0.03f);
                b.Tint(CorpMark).Box(new Vector3(0f, 0.51f, 0.55f), new Vector3(0.15f, 0.02f, 0.4f));
                b.Pop();
            }
            b.Tint(CorpDark).Cylinder(new Vector3(0f, 0.26f, 0f), 0.24f, 0.18f, 0.6f, 10);
            for (int i = 0; i < 3; i++)
            {
                b.Tint(GroundSwatch.GlowCyan).Torus(new Vector3(0f, 0.38f + i * 0.17f, 0f), 0.3f - i * 0.03f, 0.04f, 16, 4);
            }
            b.Tint(GroundSwatch.GlowWhite).Sphere(new Vector3(0f, 0.92f, 0f), 0.16f, 5, 10);
            b.Tint(GroundSwatch.GlowCyan).Torus(new Vector3(0f, 0.92f, 0f), 0.24f, 0.03f, 16, 4);
            return m;
        }


        /// <summary>A destroyer, 7 m long: a pointed hull, a bridge and funnel, a forward twin turret, an aft missile box.</summary>
        private static UnitModel Destroyer()
        {
            var m = new UnitModel(0.85f, 2.6f) { Turret = UnitModel.Part(), TurretPivot = new Vector3(0f, 0.52f, 1.75f) };
            MeshBuilder b = m.Body;
            var outline = new List<Vector2>
            {
                new Vector2(0f, 3.5f), new Vector2(0.55f, 2.6f), new Vector2(0.95f, 1.2f), new Vector2(1f, -1.8f), new Vector2(0.85f, -3.2f),
                new Vector2(-0.85f, -3.2f), new Vector2(-1f, -1.8f), new Vector2(-0.95f, 1.2f), new Vector2(-0.55f, 2.6f)
            };
            b.Push(new Vector3(0f, 0.2f, 0f), Quaternion.Euler(90f, 0f, 0f));
            b.Tint(GroundSwatch.BoatGrey).Prism(outline, 0.4f);
            b.Pop();
            var deck = new List<Vector2>();
            foreach (Vector2 p in outline)
            {
                deck.Add(p * 0.92f);
            }
            b.Push(new Vector3(0f, 0.41f, 0f), Quaternion.Euler(90f, 0f, 0f));
            b.Tint(CorpDark).Prism(deck, 0.02f);
            b.Pop();
            b.Tint(CorpHull).BeveledBox(new Vector3(0f, 0.6f, 0.2f), new Vector3(1.1f, 0.4f, 1.6f), 0.06f);
            b.Tint(GroundSwatch.Glass).Box(new Vector3(0f, 0.7f, 1.01f), new Vector3(0.9f, 0.12f, 0.02f));
            b.Tint(CorpHull).BeveledBox(new Vector3(0f, 0.86f, 0.05f), new Vector3(0.7f, 0.18f, 0.8f), 0.04f);
            b.Tint(CorpDark).Cylinder(new Vector3(0f, 0.8f, -0.75f), 0.24f, 0.2f, 0.42f, 10);
            b.Tint(GroundSwatch.Black).Cylinder(new Vector3(0f, 1.22f, -0.75f), 0.18f, 0.18f, 0.02f, 10);
            b.Tint(CorpMark).Cylinder(new Vector3(0f, 1.05f, -0.75f), 0.22f, 0.21f, 0.06f, 10, false, false, false);
            b.Tint(GroundSwatch.Metal).Rod(new Vector3(0f, 1.04f, 0.15f), new Vector3(0f, 1.4f, 0.15f), 0.03f, 4);
            b.Tint(CorpDark).BeveledBox(new Vector3(0f, 0.6f, -2.1f), new Vector3(0.9f, 0.36f, 1f), 0.05f);
            for (int i = 0; i < 4; i++)
            {
                b.Tint(GroundSwatch.Black).Cylinder(new Vector3(-0.27f + (i % 2) * 0.54f, 0.79f, -2.35f + (i / 2) * 0.45f), 0.12f, 0.12f, 0.02f, 8);
            }
            b.Tint(CorpMark).Box(new Vector3(0f, 0.43f, 2.5f), new Vector3(0.5f, 0.01f, 0.4f));
            b.Tint(GroundSwatch.White).Box(new Vector3(0f, 0.2f, -3.18f), new Vector3(1.5f, 0.05f, 0.08f));
            MeshBuilder t = m.Turret;
            t.Tint(CorpHull).Cylinder(new Vector3(0f, -0.1f, 0f), 0.42f, 0.36f, 0.26f, 12);
            t.Tint(CorpHull).BeveledBox(new Vector3(0f, 0.08f, 0.1f), new Vector3(0.6f, 0.2f, 0.6f), 0.06f);
            t.Tint(GroundSwatch.SteelDark);
            for (int side = -1; side <= 1; side += 2)
            {
                t.Rod(new Vector3(side * 0.14f, 0.06f, 0.3f), new Vector3(side * 0.14f, 0.06f, 1.15f), 0.05f, 6);
                m.Muzzles.Add(new Vector3(side * 0.14f, 0.06f, 1.18f));
            }
            return m;
        }


        /// <summary>A hive spawner: a ribbed mound of flesh whose mouth on top glows; tendrils claw at the ground round it.</summary>
        private static UnitModel HiveMound()
        {
            var m = new UnitModel(1.05f, 2f);
            MeshBuilder b = m.Body;
            b.Tint(GroundSwatch.ChitinDark).Blob(new Vector3(0f, 0.1f, 0f), new Vector3(1.75f, 0.25f, 1.6f), 1, 0.12f, 3301);
            b.Tint(GroundSwatch.Flesh).Blob(new Vector3(0f, 0.4f, 0f), new Vector3(1.35f, 0.55f, 1.25f), 1, 0.1f, 3303);
            for (int i = 0; i < 7; i++)
            {
                float angle = i * Mathf.PI * 2f / 7f;
                var foot = new Vector3(Mathf.Cos(angle) * 1.5f, 0.05f, Mathf.Sin(angle) * 1.4f);
                var top = new Vector3(Mathf.Cos(angle) * 0.55f, 0.92f, Mathf.Sin(angle) * 0.5f);
                b.Tint(GroundSwatch.Chitin).Rod(foot, top, 0.12f, 6, true, 0.06f);
            }
            b.Tint(GroundSwatch.ChitinDark).Torus(new Vector3(0f, 0.9f, 0f), 0.5f, 0.12f, 14, 5);
            b.Tint(GroundSwatch.Ichor).Cylinder(new Vector3(0f, 0.86f, 0f), 0.42f, 0.42f, 0.04f, 12);
            b.Tint(GroundSwatch.Resin);
            for (int i = 0; i < 5; i++)
            {
                float angle = (i * 72f + 20f) * Mathf.Deg2Rad;
                b.Sphere(new Vector3(Mathf.Cos(angle) * 1.05f, 0.45f, Mathf.Sin(angle) * 0.95f), new Vector3(0.18f, 0.12f, 0.18f), 3, 6);
            }
            m.Muzzles.Add(new Vector3(0f, 1f, 0f));
            return m;
        }


        /// <summary>A spitter: a fleshy bulb on a root ring, with a turning stalk that ends in a glowing mouth.</summary>
        private static UnitModel Spitter()
        {
            var m = new UnitModel(0.85f, 1.15f) { Turret = UnitModel.Part(), TurretPivot = new Vector3(0f, 0.42f, 0f) };
            MeshBuilder b = m.Body;
            b.Tint(GroundSwatch.ChitinDark).Torus(new Vector3(0f, 0.08f, 0f), 0.8f, 0.14f, 14, 5);
            b.Tint(GroundSwatch.Flesh).Blob(new Vector3(0f, 0.28f, 0f), new Vector3(0.75f, 0.3f, 0.75f), 1, 0.12f, 3401);
            b.Tint(GroundSwatch.Chitin);
            for (int i = 0; i < 6; i++)
            {
                float angle = (i * 60f + 15f) * Mathf.Deg2Rad;
                var at = new Vector3(Mathf.Cos(angle) * 0.62f, 0.3f, Mathf.Sin(angle) * 0.62f);
                b.Rod(at, at * 1.35f + Vector3.up * 0.25f, 0.06f, 5, true, 0.01f);
            }
            MeshBuilder t = m.Turret;
            t.Tint(GroundSwatch.Flesh).Rod(Vector3.zero, new Vector3(0f, 0.22f, 0.35f), 0.2f, 8, true, 0.16f);
            t.Tint(GroundSwatch.Chitin).Sphere(new Vector3(0f, 0.25f, 0.45f), new Vector3(0.24f, 0.2f, 0.28f), 5, 10);
            t.Tint(GroundSwatch.Ichor).Torus(new Vector3(0f, 0.25f, 0.7f), 0.12f, 0.04f, 12, 4);
            t.Cylinder(new Vector3(0f, 0.25f, 0.7f), 0.09f, 0.09f, 0.02f, 10);
            m.Muzzles.Add(new Vector3(0f, 0.25f, 0.76f));
            return m;
        }


        /// <summary>A crystal pylon: a cluster of rose crystals round one tall prism whose core glows before it fires.</summary>
        private static UnitModel Prism()
        {
            var m = new UnitModel(1.2f, 1.25f) { Glow = new Vector3(0f, 0.75f, 0f) };
            MeshBuilder b = m.Body;
            b.Tint(GroundSwatch.ChitinDark).Blob(new Vector3(0f, 0.08f, 0f), new Vector3(1.15f, 0.18f, 1.1f), 1, 0.2f, 3501);
            var random = new System.Random(3503);
            for (int i = 0; i < 5; i++)
            {
                float angle = i * 72f + 10f;
                b.Push(new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad) * 0.62f, 0.05f, Mathf.Sin(angle * Mathf.Deg2Rad) * 0.62f),
                    Quaternion.Euler(0f, -angle, 0f) * Quaternion.Euler(0f, 0f, -28f));
                float h = 0.4f + (float)random.NextDouble() * 0.25f;
                b.Tint(i % 2 == 0 ? GroundSwatch.CrystalRose : GroundSwatch.CrystalWhite).Cylinder(Vector3.zero, 0.13f, 0.12f, h, 6);
                b.Cylinder(new Vector3(0f, h, 0f), 0.12f, 0f, 0.14f, 6);
                b.Pop();
            }
            b.Tint(GroundSwatch.CrystalWhite).Cylinder(new Vector3(0f, 0.05f, 0f), 0.32f, 0.28f, 0.85f, 6);
            b.Tint(GroundSwatch.CrystalRose).Cylinder(new Vector3(0f, 0.9f, 0f), 0.28f, 0f, 0.3f, 6);
            b.Tint(GroundSwatch.CrystalCyan).Sphere(new Vector3(0f, 0.6f, 0f), new Vector3(0.2f, 0.16f, 0.2f), 4, 6);
            m.Muzzles.Add(new Vector3(0f, 0.75f, 0f));
            return m;
        }
    }
}
