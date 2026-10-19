using Gamebox.Editor;
using System.Collections.Generic;
using UnityEngine;

namespace Portfolio.Asteroids.EditorTools
{
    /// <summary>
    /// The procedural models of the field campaigns (SpaceModels.FieldCampaigns.cs), coloured through the palette like
    /// the others and built with +Y toward the camera and +Z as their nose: wreckage and hydra rocks, the sentry and its
    /// turret, the minelayer and its mine drum, the phase stalker, the freighter and the station, and the bodies of the
    /// new bosses (the Storm Herald, the Bastion, the Sunbreaker's sun cannon, the Scrap Leviathan, the Wraith
    /// Matriarch, the Hydra and the Gatekeeper).
    /// </summary>
    internal static partial class SpaceModels
    {
        // ------------------------------------------------------------------ rocks

        /// <summary>
        /// A piece of a dead ship, about one unit in radius: a buckled hull plate with its ribs (variant 0), a broken girder
        /// truss (1), or a hull section with a fuel tank that still glows (2, the one that explodes).
        /// </summary>
        public static LowPolyBuilder Wreck(int variant)
        {
            var b = new LowPolyBuilder();
            var random = new System.Random(4100 + variant * 37);
            float Jitter(float amount) => ((float)random.NextDouble() * 2f - 1f) * amount;
            switch (variant)
            {
                case 0:
                {
                    // Plates along an arc, each a little askew, ribs inside, a torn edge.
                    for (int i = -2; i <= 2; i++)
                    {
                        float angle = i * 16f;
                        b.Push(Quaternion.Euler(0f, 0f, angle) * new Vector3(0f, -1.1f, 0f) + new Vector3(0f, 1.05f, 0f), Quaternion.Euler(Jitter(6f), Jitter(4f), angle));
                        b.Color(i % 2 == 0 ? Swatch.Gunmetal : Swatch.HullDark);
                        b.BeveledBox(new Vector3(0f, 0f, Jitter(0.08f)), new Vector3(0.62f, 0.12f, 1.25f + Jitter(0.25f)), 0.03f);
                        b.Color(Swatch.Trim);
                        b.BeveledBox(new Vector3(0f, 0.12f, 0f), new Vector3(0.08f, 0.14f, 1.1f), 0.02f);
                        b.Pop();
                    }
                    b.Color(Swatch.Hazard);
                    b.BeveledBox(new Vector3(-0.35f, 0.02f, 0.55f), new Vector3(0.5f, 0.14f, 0.12f), 0.02f);
                    b.Color(Swatch.Black);
                    b.BeveledBox(new Vector3(0.45f, 0.08f, -0.35f), new Vector3(0.4f, 0.16f, 0.45f), 0.03f);
                    b.Color(Swatch.Panel);
                    for (int i = 0; i < 4; i++)
                    {
                        b.Push(new Vector3(Jitter(0.8f), 0.1f, 0.75f + Jitter(0.15f)), Quaternion.Euler(Jitter(30f), Jitter(40f), Jitter(30f)));
                        b.BeveledBox(Vector3.zero, new Vector3(0.18f, 0.06f, 0.3f), 0.01f);
                        b.Pop();
                    }
                    break;
                }
                case 1:
                {
                    // A triangular truss with cross braces and a node block, snapped off at one end.
                    var corners = new[] { new Vector3(-0.45f, -0.3f, 0f), new Vector3(0.45f, -0.3f, 0f), new Vector3(0f, 0.45f, 0f) };
                    b.Color(Swatch.Trim);
                    for (int c = 0; c < 3; c++)
                    {
                        b.Rod(corners[c] + new Vector3(0f, 0f, -1f), corners[c] + new Vector3(Jitter(0.1f), Jitter(0.1f), 0.9f + Jitter(0.2f)), 0.07f, 6, false);
                    }
                    b.Color(Swatch.Gunmetal);
                    for (int s = 0; s < 4; s++)
                    {
                        float z = -0.9f + s * 0.55f;
                        for (int c = 0; c < 3; c++)
                        {
                            b.Rod(corners[c] + new Vector3(0f, 0f, z), corners[(c + 1) % 3] + new Vector3(0f, 0f, z + 0.27f), 0.04f, 5, false);
                        }
                    }
                    b.Color(Swatch.HullDark);
                    b.BeveledBox(new Vector3(0f, 0f, -0.95f), new Vector3(1.1f, 0.95f, 0.35f), 0.06f);
                    b.Color(Swatch.Hazard);
                    b.BeveledBox(new Vector3(0f, 0.5f, -0.95f), new Vector3(0.7f, 0.08f, 0.36f), 0.02f);
                    b.Color(Swatch.GlowRed);
                    b.Sphere(new Vector3(0.4f, 0.4f, -0.95f), 0.07f, 4, 6, false);
                    break;
                }
                default:
                {
                    // A hull section around a fuel tank: the tank glows through the gaps.
                    b.Push(Vector3.zero, Quaternion.Euler(90f, 0f, 0f));
                    b.Color(Swatch.DarkRed);
                    b.Cylinder(new Vector3(0f, -0.75f, 0f), 0.5f, 0.5f, 1.5f, 12, true, false, false);
                    b.Color(Swatch.HullDark);
                    b.Sphere(new Vector3(0f, 0.75f, 0f), 0.5f, 5, 12, true);
                    b.Sphere(new Vector3(0f, -0.75f, 0f), 0.5f, 5, 12, true);
                    b.Color(Swatch.GlowOrange);
                    b.Cylinder(new Vector3(0f, 0.25f, 0f), 0.52f, 0.52f, 0.1f, 12, true, false, false);
                    b.Cylinder(new Vector3(0f, -0.35f, 0f), 0.52f, 0.52f, 0.1f, 12, true, false, false);
                    b.Pop();
                    b.Color(Swatch.Gunmetal);
                    for (int i = 0; i < 3; i++)
                    {
                        float angle = i * 120f + 20f;
                        b.Push(Vector3.zero, Quaternion.Euler(0f, 0f, angle));
                        b.BeveledBox(new Vector3(0f, 0.62f, Jitter(0.2f)), new Vector3(0.5f, 0.1f, 1.3f + Jitter(0.3f)), 0.03f);
                        b.Pop();
                    }
                    b.Color(Swatch.Hazard);
                    b.BeveledBox(new Vector3(0f, 0.55f, 0.65f), new Vector3(0.6f, 0.06f, 0.14f), 0.02f);
                    b.Color(Swatch.GlowRed);
                    b.Sphere(new Vector3(0f, 0.55f, -0.6f), 0.1f, 4, 8, false);
                    break;
                }
            }
            return b;
        }


        /// <summary>A hydra rock: a lumpy green body bristling with glowing nodules, about one unit in radius.</summary>
        public static LowPolyBuilder HydraRock(int seed)
        {
            var b = new LowPolyBuilder();
            var random = new System.Random(seed);
            FacetedBlob(b, new Vector3(0.8f, 0.72f, 0.76f), 1, 0.28f, seed, random, Swatch.Green, Swatch.DarkGreen, Swatch.Teal);
            int nodules = 6 + random.Next(4);
            for (int i = 0; i < nodules; i++)
            {
                Vector3 direction = RandomDirection(random);
                float size = 0.13f + (float)random.NextDouble() * 0.14f;
                b.Color(i % 3 == 0 ? Swatch.GlowYellow : Swatch.GlowGreen);
                b.Sphere(direction * (0.66f + size * 0.3f), Vector3.one * size, 4, 8, true);
                if (i % 2 == 0)
                {
                    Spike(b, direction * 0.6f, direction, 0.07f, 0.25f + (float)random.NextDouble() * 0.2f, Swatch.DarkGreen, Swatch.GlowGreen);
                }
            }
            return b;
        }

        // ------------------------------------------------------------------ enemies and allies

        /// <summary>The sentry's platform: a hexagonal armoured base with fins and running lights (the turret is apart).</summary>
        public static LowPolyBuilder SentryBase()
        {
            var b = new LowPolyBuilder();
            b.Color(Swatch.Gunmetal);
            b.Cylinder(new Vector3(0f, -0.2f, 0f), 0.95f, 0.85f, 0.25f, 6, false, true, true, Mathf.PI / 6f);
            b.Color(Swatch.HullDark);
            b.Cylinder(new Vector3(0f, 0.05f, 0f), 0.78f, 0.62f, 0.12f, 6, false, false, true, Mathf.PI / 6f);
            for (int i = 0; i < 3; i++)
            {
                float angle = i * 120f + 60f;
                b.Push(Quaternion.Euler(0f, angle, 0f) * new Vector3(0f, -0.05f, 0.95f), Quaternion.Euler(0f, angle, 0f));
                b.Color(Swatch.Panel);
                b.BeveledBox(Vector3.zero, new Vector3(0.45f, 0.22f, 0.4f), 0.04f);
                b.Color(Swatch.Hazard);
                b.BeveledBox(new Vector3(0f, 0.12f, 0.08f), new Vector3(0.35f, 0.04f, 0.1f), 0.01f);
                b.Color(Swatch.GlowOrange);
                b.Sphere(new Vector3(0f, 0.1f, 0.22f), 0.06f, 4, 6, false);
                b.Pop();
            }
            b.Color(Swatch.GlowOrange);
            b.Torus(new Vector3(0f, 0.1f, 0f), 0.55f, 0.035f, 24, 4);
            return b;
        }


        /// <summary>The sentry's turret: a squat dome with twin barrels along +Z.</summary>
        public static LowPolyBuilder SentryTurret()
        {
            var b = new LowPolyBuilder();
            b.Color(Swatch.Panel);
            b.Sphere(Vector3.zero, new Vector3(0.42f, 0.32f, 0.42f), 6, 12, true, 0.5f);
            b.Color(Swatch.Gunmetal);
            for (int side = -1; side <= 1; side += 2)
            {
                b.Rod(new Vector3(side * 0.14f, 0.15f, 0.1f), new Vector3(side * 0.14f, 0.15f, 0.95f), 0.07f, 8, true);
                b.Color(Swatch.GlowRed);
                b.Cylinder(new Vector3(side * 0.14f, 0.15f, 0.95f), 0.075f, 0.075f, 0.05f, 8, true);
                b.Color(Swatch.Gunmetal);
            }
            b.Color(Swatch.HullDark);
            b.BeveledBox(new Vector3(0f, 0.25f, -0.15f), new Vector3(0.3f, 0.12f, 0.3f), 0.03f);
            return b;
        }


        /// <summary>The minelayer: a broad, blunt hull in yellow and black hazard paint with a glowing cockpit at the nose.</summary>
        public static LowPolyBuilder Minelayer()
        {
            var b = new LowPolyBuilder();
            b.Color(Swatch.Hull);
            b.BeveledBox(new Vector3(0f, 0f, 0.1f), new Vector3(1.15f, 0.55f, 2.4f), 0.14f);
            b.Color(Swatch.Gunmetal);
            for (int side = -1; side <= 1; side += 2)
            {
                b.BeveledBox(new Vector3(side * 0.8f, -0.05f, -0.25f), new Vector3(0.5f, 0.45f, 1.6f), 0.1f);
                b.Color(Swatch.Hazard);
                for (int s = 0; s < 3; s++)
                {
                    b.Push(new Vector3(side * 0.8f, 0.19f, -0.75f + s * 0.45f), Quaternion.Euler(0f, 35f * side, 0f));
                    b.BeveledBox(Vector3.zero, new Vector3(0.12f, 0.03f, 0.5f), 0.01f);
                    b.Pop();
                }
                b.Color(Swatch.GlowOrange);
                b.Cylinder(new Vector3(side * 0.8f, -0.05f, -1.05f), 0.16f, 0.2f, 0.06f, 10, true);
                b.Color(Swatch.Gunmetal);
            }
            b.Color(Swatch.GlowCyan);
            b.Sphere(new Vector3(0f, 0.25f, 0.95f), new Vector3(0.3f, 0.16f, 0.3f), 5, 10, true, 0.5f);
            b.Color(Swatch.Black);
            b.BeveledBox(new Vector3(0f, 0.28f, -0.55f), new Vector3(0.95f, 0.06f, 0.95f), 0.03f);
            b.Color(Swatch.GlowRed);
            b.Sphere(new Vector3(0.55f, 0.28f, 1.2f), 0.06f, 4, 6, false);
            b.Sphere(new Vector3(-0.55f, 0.28f, 1.2f), 0.06f, 4, 6, false);
            return b;
        }


        /// <summary>The minelayer's mine drum, seen from above: a disc ringed with mines that turns as it sows them.</summary>
        public static LowPolyBuilder MineDrum()
        {
            var b = new LowPolyBuilder();
            b.Color(Swatch.HullDark);
            b.Cylinder(Vector3.zero, 0.42f, 0.38f, 0.12f, 12, false);
            for (int i = 0; i < 6; i++)
            {
                float angle = i * Mathf.PI * 2f / 6f;
                var at = new Vector3(Mathf.Cos(angle) * 0.3f, 0.14f, Mathf.Sin(angle) * 0.3f);
                b.Color(Swatch.Gunmetal);
                b.Sphere(at, 0.09f, 4, 8, true);
                b.Color(Swatch.GlowRed);
                b.Sphere(at + Vector3.up * 0.07f, 0.035f, 3, 6, false);
            }
            return b;
        }


        /// <summary>The phase stalker: a dark blade of a ship with glowing edges and a single eye.</summary>
        public static LowPolyBuilder Stalker()
        {
            var b = new LowPolyBuilder();
            var outline = new[]
            {
                new Vector2(0f, 1.05f), new Vector2(0.22f, 0.35f), new Vector2(0.75f, -0.15f), new Vector2(0.5f, -0.55f), new Vector2(0.18f, -0.3f),
                new Vector2(0f, -0.65f), new Vector2(-0.18f, -0.3f), new Vector2(-0.5f, -0.55f), new Vector2(-0.75f, -0.15f), new Vector2(-0.22f, 0.35f)
            };
            b.Push(Vector3.zero, Quaternion.Euler(90f, 0f, 0f));
            b.Color(Swatch.Purple);
            b.Prism(outline, 0.18f);
            b.Color(Swatch.Black);
            b.Prism(new[] { new Vector2(0f, 0.75f), new Vector2(0.14f, 0.2f), new Vector2(0f, -0.4f), new Vector2(-0.14f, 0.2f) }, 0.3f);
            b.Pop();
            b.Color(Swatch.GlowViolet);
            for (int i = 0; i < outline.Length; i++)
            {
                Vector2 a = outline[i];
                Vector2 c = outline[(i + 1) % outline.Length];
                b.Rod(new Vector3(a.x, 0.1f, a.y), new Vector3(c.x, 0.1f, c.y), 0.025f, 4, false);
            }
            b.Color(Swatch.GlowMagenta);
            b.Sphere(new Vector3(0f, 0.17f, 0.3f), new Vector3(0.1f, 0.08f, 0.16f), 4, 8, true);
            return b;
        }


        /// <summary>A freighter, about 3.6 long: a spine with a cockpit, two rows of cargo containers and a twin engine block.</summary>
        public static LowPolyBuilder Freighter()
        {
            var b = new LowPolyBuilder();
            b.Color(Swatch.Gunmetal);
            b.BeveledBox(new Vector3(0f, 0f, 0f), new Vector3(0.36f, 0.3f, 3.1f), 0.06f);
            b.Color(Swatch.Hull);
            b.BeveledBox(new Vector3(0f, 0.05f, 1.55f), new Vector3(0.85f, 0.5f, 0.7f), 0.12f);
            b.Color(Swatch.GlowCyan);
            b.BeveledBox(new Vector3(0f, 0.31f, 1.7f), new Vector3(0.55f, 0.04f, 0.25f), 0.02f);
            Swatch[] cargo = { Swatch.Orange, Swatch.Teal, Swatch.Yellow, Swatch.Red, Swatch.Blue, Swatch.Orange };
            for (int i = 0; i < 3; i++)
            {
                for (int side = -1; side <= 1; side += 2)
                {
                    b.Color(cargo[i * 2 + (side > 0 ? 1 : 0)]);
                    b.BeveledBox(new Vector3(side * 0.42f, 0f, 0.7f - i * 0.82f), new Vector3(0.44f, 0.44f, 0.74f), 0.04f);
                    b.Color(Swatch.HullDark);
                    b.BeveledBox(new Vector3(side * 0.42f, 0.23f, 0.7f - i * 0.82f), new Vector3(0.3f, 0.02f, 0.6f), 0.01f);
                }
            }
            b.Color(Swatch.HullDark);
            b.BeveledBox(new Vector3(0f, 0f, -1.65f), new Vector3(1.05f, 0.5f, 0.55f), 0.1f);
            for (int side = -1; side <= 1; side += 2)
            {
                b.Push(new Vector3(side * 0.3f, 0f, -1.9f), Quaternion.Euler(-90f, 0f, 0f));
                b.Color(Swatch.Gunmetal);
                b.Cylinder(Vector3.zero, 0.2f, 0.26f, 0.3f, 10, true);
                b.Color(Swatch.GlowCyan);
                b.Cylinder(new Vector3(0f, 0.3f, 0f), 0.2f, 0.2f, 0.03f, 10, true);
                b.Pop();
                b.Color(side < 0 ? Swatch.GlowRed : Swatch.GlowGreen);
                b.Sphere(new Vector3(side * 0.62f, 0.1f, -1.5f), 0.06f, 4, 6, false);
            }
            return b;
        }


        /// <summary>A station's hub: a drum with a dome, solar wings and spokes out to its ring (the ring is apart).</summary>
        public static LowPolyBuilder StationHub()
        {
            var b = new LowPolyBuilder();
            b.Color(Swatch.Hull);
            b.Cylinder(new Vector3(0f, -0.3f, 0f), 0.6f, 0.6f, 0.55f, 16, true);
            b.Color(Swatch.Panel);
            b.Sphere(new Vector3(0f, 0.25f, 0f), new Vector3(0.48f, 0.3f, 0.48f), 6, 16, true, 0.5f);
            b.Color(Swatch.GlowCyan);
            b.Torus(new Vector3(0f, 0.25f, 0f), 0.5f, 0.03f, 24, 4);
            for (int i = 0; i < 4; i++)
            {
                float angle = i * 90f + 45f;
                b.Push(Vector3.zero, Quaternion.Euler(0f, angle, 0f));
                b.Color(Swatch.Trim);
                b.Rod(new Vector3(0f, 0f, 0.55f), new Vector3(0f, 0f, 1.5f), 0.05f, 6, false);
                b.Color(Swatch.Navy);
                b.BeveledBox(new Vector3(0f, 0.02f, 1.05f), new Vector3(0.5f, 0.03f, 0.55f), 0.01f);
                b.Color(Swatch.Blue);
                b.BeveledBox(new Vector3(0f, 0.04f, 1.05f), new Vector3(0.42f, 0.01f, 0.47f), 0.005f);
                b.Pop();
            }
            return b;
        }


        /// <summary>A station's ring with its modules and lights (turns slowly around the hub).</summary>
        public static LowPolyBuilder StationRing()
        {
            var b = new LowPolyBuilder();
            b.Color(Swatch.Hull);
            b.Torus(Vector3.zero, 1.6f, 0.13f, 48, 8);
            for (int i = 0; i < 8; i++)
            {
                float angle = i * 45f;
                b.Push(Quaternion.Euler(0f, angle, 0f) * new Vector3(0f, 0f, 1.6f), Quaternion.Euler(0f, angle, 0f));
                b.Color(i % 2 == 0 ? Swatch.Panel : Swatch.Teal);
                b.BeveledBox(Vector3.zero, new Vector3(0.42f, 0.3f, 0.36f), 0.05f);
                b.Color(i % 2 == 0 ? Swatch.GlowYellow : Swatch.GlowCyan);
                b.Sphere(new Vector3(0f, 0.17f, 0f), 0.05f, 4, 6, false);
                b.Pop();
            }
            return b;
        }

        // ------------------------------------------------------------------ bosses

        /// <summary>The Storm Herald: a great ion manta with swept wings veined in light, head fins and a whip of a tail.</summary>
        public static LowPolyBuilder StormHerald()
        {
            var b = new LowPolyBuilder();
            b.Color(Swatch.Blue);
            b.Sphere(new Vector3(0f, 0f, 0.1f), new Vector3(0.75f, 0.32f, 1.25f), 8, 16, true);
            var wing = new[]
            {
                new Vector2(0.4f, 0.75f), new Vector2(1.3f, 0.35f), new Vector2(2.25f, -0.35f), new Vector2(2.1f, -0.6f), new Vector2(1.2f, -0.45f), new Vector2(0.45f, -0.75f)
            };
            for (int side = -1; side <= 1; side += 2)
            {
                var outline = new Vector2[wing.Length];
                for (int i = 0; i < wing.Length; i++)
                {
                    outline[side < 0 ? i : wing.Length - 1 - i] = new Vector2(wing[i].x * side, wing[i].y);
                }
                b.Push(Vector3.zero, Quaternion.Euler(90f, 0f, 0f));
                b.Color(Swatch.Cyan);
                b.Prism(outline, 0.12f);
                b.Pop();
                b.Color(Swatch.GlowCyan);
                b.Rod(new Vector3(side * 0.5f, 0.07f, 0.4f), new Vector3(side * 2.1f, 0.07f, -0.4f), 0.035f, 4, false);
                b.Rod(new Vector3(side * 0.5f, 0.07f, -0.2f), new Vector3(side * 1.3f, 0.07f, -0.4f), 0.03f, 4, false);
                b.Color(Swatch.Blue);
                b.Rod(new Vector3(side * 0.35f, 0.05f, 1.1f), new Vector3(side * 0.55f, 0.05f, 1.75f), 0.12f, 6, true, 0.03f);
                b.Color(Swatch.GlowCyan);
                b.Sphere(new Vector3(side * 0.3f, 0.22f, 0.95f), 0.09f, 4, 8, true);
            }
            b.Color(Swatch.Blue);
            b.Rod(new Vector3(0f, 0f, -1.05f), new Vector3(0f, 0f, -2.6f), 0.14f, 6, true, 0.02f);
            b.Color(Swatch.GlowBlue);
            b.Rod(new Vector3(0f, 0.3f, 0.9f), new Vector3(0f, 0.3f, -0.9f), 0.05f, 4, false);
            b.Color(Swatch.GlowWhite);
            b.Sphere(new Vector3(0f, 0.2f, 0.2f), 0.16f, 5, 10, true);
            return b;
        }


        /// <summary>The ion rings that circle the Storm Herald (they spin on their own).</summary>
        public static LowPolyBuilder HeraldRings()
        {
            var b = new LowPolyBuilder();
            b.Color(Swatch.GlowCyan);
            b.Torus(Vector3.zero, 2.45f, 0.045f, 48, 4, 300f, true, 10f);
            b.Color(Swatch.GlowBlue);
            b.Torus(Vector3.zero, 2.75f, 0.035f, 48, 4, 250f, true, 200f);
            b.Color(Swatch.GlowWhite);
            for (int i = 0; i < 6; i++)
            {
                float angle = (10f + i * 50f) * Mathf.Deg2Rad;
                b.Sphere(new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * 2.45f, 0.08f, 4, 6, false);
            }
            return b;
        }


        /// <summary>The Bastion: an octagonal fortress of armour plates around a reactor, a heavy gun on each flank.</summary>
        public static LowPolyBuilder BastionFortress()
        {
            var b = new LowPolyBuilder();
            var octagon = new Vector2[8];
            for (int i = 0; i < 8; i++)
            {
                float angle = (i * 45f + 22.5f) * Mathf.Deg2Rad;
                octagon[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 1.65f;
            }
            b.Push(Vector3.zero, Quaternion.Euler(90f, 0f, 0f));
            b.Color(Swatch.Gunmetal);
            b.Prism(octagon, 0.55f);
            b.Pop();
            for (int i = 0; i < 8; i++)
            {
                float angle = i * 45f + 22.5f;
                b.Push(Quaternion.Euler(0f, angle, 0f) * new Vector3(0f, 0.32f, 1.15f), Quaternion.Euler(-12f, angle, 0f));
                b.Color(i % 2 == 0 ? Swatch.HullDark : Swatch.Panel);
                b.BeveledBox(Vector3.zero, new Vector3(0.95f, 0.16f, 0.62f), 0.05f);
                b.Color(Swatch.Hazard);
                b.BeveledBox(new Vector3(0f, 0.09f, 0.22f), new Vector3(0.6f, 0.03f, 0.08f), 0.01f);
                b.Pop();
            }
            for (int i = 0; i < 4; i++)
            {
                float angle = i * 90f;
                b.Push(Quaternion.Euler(0f, angle, 0f) * new Vector3(0f, 0.35f, 1.55f), Quaternion.Euler(0f, angle, 0f));
                b.Color(Swatch.Trim);
                b.BeveledBox(Vector3.zero, new Vector3(0.6f, 0.4f, 0.55f), 0.07f);
                b.Color(Swatch.Gunmetal);
                b.Rod(new Vector3(-0.15f, 0.12f, 0.2f), new Vector3(-0.15f, 0.12f, 0.95f), 0.08f, 8, true);
                b.Rod(new Vector3(0.15f, 0.12f, 0.2f), new Vector3(0.15f, 0.12f, 0.95f), 0.08f, 8, true);
                b.Color(Swatch.GlowRed);
                b.Sphere(new Vector3(0f, 0.22f, -0.05f), 0.07f, 4, 6, false);
                b.Pop();
            }
            b.Color(Swatch.HullDark);
            b.Cylinder(new Vector3(0f, 0.2f, 0f), 0.75f, 0.65f, 0.3f, 16, true);
            b.Color(Swatch.GlowOrange);
            b.Sphere(new Vector3(0f, 0.5f, 0f), new Vector3(0.52f, 0.35f, 0.52f), 6, 16, true, 0.5f);
            b.Color(Swatch.GlowYellow);
            b.Torus(new Vector3(0f, 0.52f, 0f), 0.6f, 0.04f, 24, 4);
            return b;
        }


        /// <summary>The Bastion's outer ring of gun pods (it turns around the fortress).</summary>
        public static LowPolyBuilder BastionRing()
        {
            var b = new LowPolyBuilder();
            b.Color(Swatch.Trim);
            b.Torus(Vector3.zero, 2.3f, 0.09f, 48, 6);
            for (int i = 0; i < 6; i++)
            {
                float angle = i * 60f;
                b.Push(Quaternion.Euler(0f, angle, 0f) * new Vector3(0f, 0f, 2.3f), Quaternion.Euler(0f, angle, 0f));
                b.Color(Swatch.Gunmetal);
                b.BeveledBox(Vector3.zero, new Vector3(0.42f, 0.3f, 0.42f), 0.06f);
                b.Color(Swatch.Hazard);
                b.BeveledBox(new Vector3(0f, 0.16f, 0f), new Vector3(0.3f, 0.02f, 0.3f), 0.01f);
                b.Color(Swatch.GlowRed);
                b.Sphere(new Vector3(0f, 0.2f, 0.15f), 0.06f, 4, 6, false);
                b.Pop();
            }
            return b;
        }


        /// <summary>The Sunbreaker's sun cannon: a ring of gold with a white-hot heart, held by three prongs, at the nose of its hull.</summary>
        public static LowPolyBuilder SunCannon()
        {
            var b = new LowPolyBuilder();
            b.Push(Vector3.zero, Quaternion.Euler(90f, 0f, 0f));
            b.Color(Swatch.Gold);
            b.Torus(Vector3.zero, 0.85f, 0.13f, 32, 8);
            b.Color(Swatch.GlowYellow);
            b.Torus(Vector3.zero, 0.85f, 0.05f, 32, 4, 360f, true);
            b.Pop();
            b.Color(Swatch.GlowWhite);
            b.Sphere(Vector3.zero, 0.38f, 6, 12, true);
            b.Color(Swatch.GlowOrange);
            b.Sphere(Vector3.zero, 0.55f, 6, 12, false);
            b.Color(Swatch.Gunmetal);
            for (int i = 0; i < 3; i++)
            {
                float angle = (i * 120f + 90f) * Mathf.Deg2Rad;
                var at = new Vector3(Mathf.Cos(angle) * 0.85f, Mathf.Sin(angle) * 0.85f * 0.3f, 0f);
                b.Rod(at, new Vector3(at.x * 0.5f, at.y * 0.5f, -1.3f), 0.08f, 6, true, 0.12f);
            }
            return b;
        }


        /// <summary>The Scrap Leviathan's head: a dark maw ringed in red heat and four jaws of salvaged plating.</summary>
        public static LowPolyBuilder ScrapMaw()
        {
            var b = new LowPolyBuilder();
            b.Color(Swatch.Basalt);
            b.Sphere(Vector3.zero, new Vector3(1f, 0.7f, 1f), 8, 16, false);
            b.Color(Swatch.Black);
            b.Sphere(new Vector3(0f, 0.35f, 0f), new Vector3(0.62f, 0.4f, 0.62f), 6, 14, true, 0.5f);
            b.Color(Swatch.GlowRed);
            b.Torus(new Vector3(0f, 0.55f, 0f), 0.62f, 0.08f, 32, 6);
            b.Color(Swatch.GlowOrange);
            b.Sphere(new Vector3(0f, 0.6f, 0f), 0.2f, 5, 10, true);
            // Four mandibles of salvaged plating, swept around the maw like the blades of a pinwheel, teeth inward.
            for (int i = 0; i < 4; i++)
            {
                float angle = i * 90f + 45f;
                b.Push(Quaternion.Euler(0f, angle, 0f) * new Vector3(0f, 0.3f, 0.85f), Quaternion.Euler(-18f, angle + 38f, 0f));
                b.Color(Swatch.Panel);
                b.BeveledBox(new Vector3(0f, 0f, 0.45f), new Vector3(0.5f, 0.22f, 1.3f), 0.05f);
                b.Color(Swatch.Trim);
                b.BeveledBox(new Vector3(0.18f, 0.12f, 0.55f), new Vector3(0.12f, 0.1f, 1.1f), 0.02f);
                b.Color(Swatch.White);
                for (int t = 0; t < 3; t++)
                {
                    b.Push(new Vector3(-0.27f, 0.02f, 0.15f + t * 0.35f), Quaternion.Euler(0f, 0f, 90f));
                    b.Cylinder(Vector3.zero, 0.07f, 0f, 0.28f, 4, false);
                    b.Pop();
                }
                b.Color(Swatch.Hazard);
                b.BeveledBox(new Vector3(0f, 0.12f, -0.05f), new Vector3(0.4f, 0.02f, 0.1f), 0.01f);
                b.Pop();
            }
            return b;
        }


        /// <summary>The Wraith Matriarch: a crescent of dark crystal with a crown of spikes, one burning eye and trailing tendrils.</summary>
        public static LowPolyBuilder WraithMatriarch()
        {
            var b = new LowPolyBuilder();
            // The crescent in convex pieces (a prism is filled from its middle, so a crescent goes quad by quad).
            const int pieces = 14;
            b.Push(Vector3.zero, Quaternion.Euler(90f, 0f, 0f));
            for (int i = 0; i < pieces; i++)
            {
                float a0 = Mathf.Lerp(-150f, -30f, i / (float)pieces) * Mathf.Deg2Rad;
                float a1 = Mathf.Lerp(-150f, -30f, (i + 1) / (float)pieces) * Mathf.Deg2Rad;
                float b0 = Mathf.Lerp(-140f, -40f, i / (float)pieces) * Mathf.Deg2Rad;
                float b1 = Mathf.Lerp(-140f, -40f, (i + 1) / (float)pieces) * Mathf.Deg2Rad;
                var quad = new[]
                {
                    new Vector2(Mathf.Cos(a0), -Mathf.Sin(a0)) * 1.9f, new Vector2(Mathf.Cos(a1), -Mathf.Sin(a1)) * 1.9f,
                    new Vector2(Mathf.Cos(b1) * 1.25f, -Mathf.Sin(b1) * 1.25f - 0.55f), new Vector2(Mathf.Cos(b0) * 1.25f, -Mathf.Sin(b0) * 1.25f - 0.55f)
                };
                b.Color(i % 2 == 0 ? Swatch.Purple : Swatch.Violet);
                b.Prism(quad, 0.3f, new Vector3(0f, -0.4f, 0f));
            }
            b.Pop();
            b.Color(Swatch.Violet);
            for (int i = 1; i < 16; i += 2)
            {
                float angle = Mathf.Lerp(-150f, -30f, i / 16f) * Mathf.Deg2Rad;
                var root = new Vector3(Mathf.Cos(angle) * 1.8f, 0.05f, -Mathf.Sin(angle) * 1.8f - 0.4f);
                Vector3 outward = new Vector3(root.x, 0f, root.z + 0.4f).normalized;
                Spike(b, root, (outward + Vector3.up * 0.3f).normalized, 0.09f, 0.35f + Mathf.Abs(i - 8) * -0.02f + 0.15f, Swatch.Violet, Swatch.GlowViolet);
            }
            b.Color(Swatch.Black);
            b.Sphere(new Vector3(0f, 0.05f, -0.2f), new Vector3(0.7f, 0.35f, 0.6f), 6, 14, true);
            b.Color(Swatch.GlowMagenta);
            b.Sphere(new Vector3(0f, 0.3f, -0.15f), new Vector3(0.26f, 0.16f, 0.3f), 5, 12, true);
            b.Color(Swatch.GlowViolet);
            b.Torus(new Vector3(0f, 0.25f, -0.15f), 0.42f, 0.04f, 24, 4);
            b.Color(Swatch.Violet);
            for (int t = -2; t <= 2; t++)
            {
                var path = new List<Vector3>();
                for (int s = 0; s <= 8; s++)
                {
                    float k = s / 8f;
                    path.Add(new Vector3(t * 0.28f + Mathf.Sin(k * 5f + t) * 0.18f * k, -0.05f, -0.6f - k * (1.4f + Mathf.Abs(t) * 0.25f)));
                }
                b.Tube(path, 0.06f * (1f - Mathf.Abs(t) * 0.12f), 6);
                b.Color(Swatch.GlowViolet);
                b.Sphere(path[path.Count - 1], 0.07f, 4, 6, false);
                b.Color(Swatch.Violet);
            }
            return b;
        }


        /// <summary>The Hydra: a lumpy body bristling with nodules and three necks reaching forward to fanged, glowing-eyed heads.</summary>
        public static LowPolyBuilder Hydra()
        {
            var b = new LowPolyBuilder();
            var random = new System.Random(5511);
            FacetedBlob(b, new Vector3(1.15f, 0.7f, 1.05f), 1, 0.2f, 5511, random, Swatch.Green, Swatch.DarkGreen, Swatch.Teal);
            for (int i = 0; i < 9; i++)
            {
                Vector3 direction = RandomDirection(random);
                direction.y = Mathf.Abs(direction.y);
                b.Color(i % 3 == 0 ? Swatch.GlowYellow : Swatch.GlowGreen);
                b.Sphere(Vector3.Scale(direction, new Vector3(1f, 0.62f, 0.95f)), 0.14f + (float)random.NextDouble() * 0.1f, 4, 8, true);
            }
            var heads = new[] { new Vector3(-1.55f, 0.15f, 1.35f), new Vector3(0f, 0.25f, 2.05f), new Vector3(1.55f, 0.15f, 1.35f) };
            foreach (Vector3 head in heads)
            {
                var path = new List<Vector3>();
                Vector3 root = new Vector3(head.x * 0.4f, 0.2f, 0.55f);
                for (int s = 0; s <= 10; s++)
                {
                    float k = s / 10f;
                    Vector3 p = Vector3.Lerp(root, head, k);
                    p.y += Mathf.Sin(k * Mathf.PI) * 0.35f;
                    p.x += Mathf.Sin(k * Mathf.PI * 2f) * 0.15f * Mathf.Sign(head.x + 0.01f);
                    path.Add(p);
                }
                b.Color(Swatch.DarkGreen);
                b.Tube(path, 0.2f, 8);
                Vector3 forward = (head - path[path.Count - 3]).normalized;
                Quaternion turn = Quaternion.LookRotation(forward, Vector3.up);
                b.Push(head, turn);
                b.Color(Swatch.Green);
                b.Sphere(Vector3.zero, new Vector3(0.32f, 0.24f, 0.45f), 6, 12, true);
                b.Color(Swatch.Teal);
                b.Cylinder(new Vector3(0f, -0.05f, 0.3f), 0.18f, 0f, 0.4f, 6, false);
                b.Color(Swatch.GlowYellow);
                b.Sphere(new Vector3(0.15f, 0.15f, 0.2f), 0.06f, 4, 6, false);
                b.Sphere(new Vector3(-0.15f, 0.15f, 0.2f), 0.06f, 4, 6, false);
                b.Color(Swatch.White);
                b.Cylinder(new Vector3(0.1f, -0.12f, 0.4f), 0.03f, 0f, 0.14f, 4, false);
                b.Cylinder(new Vector3(-0.1f, -0.12f, 0.4f), 0.03f, 0f, 0.14f, 4, false);
                b.Pop();
            }
            return b;
        }


        /// <summary>The Gatekeeper's core: a black eye of a sphere with a golden iris and a white-hot pupil, three spokes out to the gate.</summary>
        public static LowPolyBuilder GateCore()
        {
            var b = new LowPolyBuilder();
            b.Color(Swatch.Black);
            b.Sphere(Vector3.zero, new Vector3(0.95f, 0.7f, 0.95f), 10, 20, true);
            b.Color(Swatch.Gold);
            b.Torus(new Vector3(0f, 0.55f, 0f), 0.5f, 0.09f, 32, 6);
            b.Color(Swatch.GlowYellow);
            b.Sphere(new Vector3(0f, 0.62f, 0f), new Vector3(0.36f, 0.12f, 0.36f), 5, 14, true);
            b.Color(Swatch.GlowWhite);
            b.Sphere(new Vector3(0f, 0.72f, 0f), 0.14f, 4, 10, true);
            b.Color(Swatch.Trim);
            for (int i = 0; i < 3; i++)
            {
                float angle = i * 120f + 90f;
                b.Push(Vector3.zero, Quaternion.Euler(0f, angle, 0f));
                b.Rod(new Vector3(0f, 0f, 0.85f), new Vector3(0f, 0f, 2.6f), 0.07f, 6, false);
                b.Color(Swatch.GlowYellow);
                b.Sphere(new Vector3(0f, 0.05f, 1.75f), 0.08f, 4, 6, false);
                b.Color(Swatch.Trim);
                b.Pop();
            }
            return b;
        }


        /// <summary>The gate itself: eight arcs of a golden ring with glowing glyph plates (it turns around the core).</summary>
        public static LowPolyBuilder GateRing(float radius, int glyphsPerArc, Swatch glow)
        {
            var b = new LowPolyBuilder();
            for (int i = 0; i < 8; i++)
            {
                float start = i * 45f + 4f;
                b.Color(Swatch.Gold);
                b.Torus(Vector3.zero, radius, 0.16f, 10, 6, 37f, false, start);
                for (int g = 0; g < glyphsPerArc; g++)
                {
                    float angle = (start + (g + 0.5f) * 37f / glyphsPerArc) * Mathf.Deg2Rad;
                    var at = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;
                    b.Push(at + Vector3.up * 0.13f, Quaternion.Euler(0f, -angle * Mathf.Rad2Deg, 0f));
                    b.Color(glow);
                    b.BeveledBox(Vector3.zero, new Vector3(0.1f, 0.05f, 0.22f), 0.015f);
                    b.Pop();
                }
            }
            return b;
        }
    }
}
