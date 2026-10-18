using UnityEngine;

namespace Portfolio.Asteroids.EditorTools
{
    /// <summary>
    /// The armory's icons, in the style of the others: the four new weapons and two power-ups of the asteroid field (the
    /// HUD, the touch FIRE button and the pickups, written to Art/Icons with the other icons) and the twelve new strike
    /// items (the Supply Room, the HUD's special weapon, the pickups). The strike specials keep the colours of what they hit
    /// (cyan air, orange ground, green both); the gear has colours of its own.
    /// </summary>
    internal static partial class SpaceIcons
    {
        /// <summary>The field icon of an armory weapon or power-up, or null for any other name.</summary>
        private static Texture2D ArmoryIcon(string name)
        {
            const int size = 128;
            Color white = Color.white;
            switch (name)
            {
                case "Arc":
                    return Draw(size,
                        L(p => Mathf.Min(Circle(p, new Vector2(-0.55f, -0.55f), 0.2f), Circle(p, new Vector2(0.55f, 0.55f), 0.2f)),
                            new Color(0.85f, 0.75f, 1f), new Color(0.5f, 0.3f, 1f), 0.06f, new Color(0.6f, 0.4f, 1f, 0.6f)),
                        L(p => Zigzag(p, new Vector2(-0.48f, -0.48f), new Vector2(0.48f, 0.48f), 0.09f), new Color(0.95f, 0.9f, 1f), new Color(0.7f, 0.55f, 1f),
                            0.05f, new Color(0.7f, 0.5f, 1f, 0.8f), 0.22f));
                case "Rail":
                    return Draw(size,
                        L(p => Box(p, new Vector2(0f, 0f), new Vector2(0.11f, 0.88f), 0.08f), new Color(0.95f, 0.98f, 1f), new Color(0.55f, 0.75f, 1f), 0.05f,
                            new Color(0.6f, 0.85f, 1f, 0.75f), 0.24f),
                        L(p =>
                        {
                            float d = float.MaxValue;
                            for (int i = 0; i < 3; i++)
                            {
                                d = Mathf.Min(d, Box(p, new Vector2(0f, -0.45f + i * 0.3f), new Vector2(0.4f, 0.06f), 0.04f));
                            }
                            return d;
                        }, new Color(0.7f, 0.82f, 0.95f), new Color(0.35f, 0.45f, 0.65f), 0.05f));
                case "Flak":
                    return Draw(size,
                        L(p =>
                        {
                            float d = Circle(p, Vector2.zero, 0.3f);
                            for (int i = 0; i < 8; i++)
                            {
                                Vector2 direction = Rotate(Vector2.up, i * 45f + 22.5f);
                                d = Mathf.Min(d, Segment(p, direction * 0.45f, direction * 0.82f, 0.07f));
                            }
                            return d;
                        }, new Color(1f, 0.92f, 0.55f), new Color(1f, 0.55f, 0.15f), 0.06f, new Color(1f, 0.7f, 0.2f, 0.6f)),
                        L(p => Circle(p, new Vector2(-0.08f, 0.08f), 0.11f), white, new Color(1f, 0.95f, 0.8f), 0f));
                case "Wave":
                    // Three crescents, each wider than the one before, rising from a small emitter.
                    return Draw(size,
                        L(p =>
                        {
                            float d = float.MaxValue;
                            for (int i = 0; i < 3; i++)
                            {
                                float radius = 0.42f + i * 0.3f;
                                float arc = Mathf.Abs(Circle(p, new Vector2(0f, -0.8f), radius)) - (0.07f + i * 0.012f);
                                // Only the top of each ring.
                                arc = Mathf.Max(arc, -(p.y + 0.8f - radius * 0.45f));
                                d = Mathf.Min(d, arc);
                            }
                            return d;
                        }, new Color(0.7f, 1f, 0.95f), new Color(0.15f, 0.8f, 0.65f), 0.05f, new Color(0.3f, 1f, 0.85f, 0.6f)),
                        L(p => Circle(p, new Vector2(0f, -0.72f), 0.13f), white, new Color(0.8f, 1f, 0.95f), 0.04f));
                case "Piercing":
                    return Draw(size,
                        L(p => Mathf.Min(Mathf.Abs(Circle(p, new Vector2(-0.3f, -0.3f), 0.28f)) - 0.07f, Mathf.Abs(Circle(p, new Vector2(0.3f, 0.3f), 0.28f)) - 0.07f),
                            new Color(0.95f, 0.75f, 1f), new Color(0.7f, 0.3f, 0.95f), 0.05f, new Color(0.8f, 0.4f, 1f, 0.55f)),
                        L(p => Mathf.Min(Segment(p, new Vector2(-0.82f, -0.82f), new Vector2(0.55f, 0.55f), 0.06f),
                            Polygon(p, new[] { new Vector2(0.88f, 0.88f), new Vector2(0.38f, 0.66f), new Vector2(0.66f, 0.38f) })), white, new Color(0.9f, 0.85f, 1f), 0.05f));
                case "Aegis":
                    return Draw(size,
                        L(p => Mathf.Max(Polygon(p, new[]
                        {
                            new Vector2(0f, 0.9f), new Vector2(0.75f, 0.62f), new Vector2(0.68f, -0.1f), new Vector2(0f, -0.92f), new Vector2(-0.68f, -0.1f),
                            new Vector2(-0.75f, 0.62f)
                        }), -1f), new Color(1f, 0.85f, 0.55f), new Color(1f, 0.5f, 0.15f), 0.07f, new Color(1f, 0.6f, 0.25f, 0.6f)),
                        L(p => Polygon(p, StarPoints(0.42f, 0.19f, new Vector2(0f, 0.05f))), white, new Color(1f, 0.95f, 0.85f), 0.04f));
                default:
                    return null;
            }
        }


        /// <summary>The icon of one of the armory's strike items (18 and up), or null for an older item.</summary>
        private static Texture2D ArmoryItemIcon(StrikeItem item)
        {
            const int size = 128;
            Color white = Color.white;
            switch (item)
            {
                case StrikeItem.FlakCannon:
                    return Draw(size,
                        L(p =>
                        {
                            float angle = Mathf.Atan2(p.y, p.x);
                            float spikes = Mathf.Pow(Mathf.Abs(Mathf.Cos(angle * 3.5f)), 5f) * 0.32f;
                            return Circle(p - new Vector2(0f, 0.22f), Vector2.zero, 0.36f + spikes);
                        }, AirTop, AirBottom, 0.06f, AirGlow),
                        L(p => Mathf.Min(Box(p, new Vector2(-0.22f, -0.62f), new Vector2(0.09f, 0.3f), 0.04f), Box(p, new Vector2(0.22f, -0.62f), new Vector2(0.09f, 0.3f), 0.04f)),
                            new Color(0.55f, 0.65f, 0.8f), new Color(0.3f, 0.36f, 0.5f), 0.05f),
                        L(p => Circle(p, new Vector2(0f, 0.22f), 0.13f), white, new Color(0.85f, 0.95f, 1f), 0f));
                case StrikeItem.ClusterBombs:
                    return Draw(size,
                        L(p => Mathf.Min(Ellipse(p, new Vector2(0f, 0.25f), new Vector2(0.3f, 0.45f)),
                            Polygon(p, new[] { new Vector2(-0.32f, 0.88f), new Vector2(0.32f, 0.88f), new Vector2(0.14f, 0.55f), new Vector2(-0.14f, 0.55f) })),
                            GroundTop, GroundBottom, 0.06f, GroundGlow),
                        L(p => Mathf.Min(Circle(p, new Vector2(-0.5f, -0.55f), 0.16f), Mathf.Min(Circle(p, new Vector2(0f, -0.7f), 0.16f), Circle(p, new Vector2(0.5f, -0.55f), 0.16f))),
                            GroundTop, GroundBottom, 0.05f, GroundGlow));
                case StrikeItem.Napalm:
                    return Draw(size,
                        L(p => Flame(p, new Vector2(0f, -0.1f), 0.85f), new Color(1f, 0.85f, 0.4f), new Color(1f, 0.32f, 0.08f), 0.06f, GroundGlow, 0.2f),
                        L(p => Flame(p, new Vector2(0f, -0.3f), 0.45f), new Color(1f, 1f, 0.85f), new Color(1f, 0.8f, 0.3f), 0f),
                        L(p => Box(p, new Vector2(0f, -0.82f), new Vector2(0.62f, 0.06f), 0.03f), GroundTop, GroundBottom, 0.04f));
                case StrikeItem.SwarmRockets:
                    // Five small rockets fanned out from one launcher, each trailing a thin line of smoke.
                    return Draw(size,
                        L(p =>
                        {
                            float d = float.MaxValue;
                            for (int i = 0; i < 5; i++)
                            {
                                Vector2 direction = Rotate(Vector2.up, (i - 2) * 26f);
                                d = Mathf.Min(d, Segment(p, new Vector2(0f, -0.85f), new Vector2(0f, -0.85f) + direction * (SwarmReach(i) - 0.45f), 0.03f));
                            }
                            return d;
                        }, new Color(0.85f, 0.88f, 0.85f, 0.85f), new Color(0.6f, 0.62f, 0.6f, 0.7f), 0f),
                        L(p =>
                        {
                            float d = float.MaxValue;
                            for (int i = 0; i < 5; i++)
                            {
                                float angle = (i - 2) * 26f;
                                Vector2 head = new Vector2(0f, -0.85f) + Rotate(Vector2.up, angle) * SwarmReach(i);
                                d = Mathf.Min(d, MissileShape(Rotate(p - head, -angle), new Vector2(0f, -0.22f), 0.48f));
                            }
                            return d;
                        }, BothTop, BothBottom, 0.05f, BothGlow));
                case StrikeItem.ArcLightning:
                    return Draw(size,
                        L(p => Mathf.Min(Circle(p, new Vector2(-0.6f, -0.62f), 0.16f), Mathf.Min(Circle(p, new Vector2(0.05f, 0.1f), 0.14f), Circle(p, new Vector2(0.62f, 0.62f), 0.16f))),
                            BothTop, BothBottom, 0.05f, BothGlow),
                        L(p => Mathf.Min(Zigzag(p, new Vector2(-0.52f, -0.52f), new Vector2(0f, 0.05f), 0.06f), Zigzag(p, new Vector2(0.1f, 0.15f), new Vector2(0.55f, 0.55f), 0.06f)),
                            white, new Color(0.85f, 1f, 0.85f), 0.04f, new Color(0.5f, 1f, 0.6f, 0.8f), 0.2f));
                case StrikeItem.RailGun:
                    return Draw(size,
                        L(p => Mathf.Min(Box(p, new Vector2(-0.22f, -0.05f), new Vector2(0.07f, 0.82f), 0.03f), Box(p, new Vector2(0.22f, -0.05f), new Vector2(0.07f, 0.82f), 0.03f)),
                            new Color(0.75f, 0.85f, 0.75f), new Color(0.35f, 0.45f, 0.38f), 0.05f),
                        L(p => Box(p, new Vector2(0f, 0.2f), new Vector2(0.08f, 0.62f), 0.06f), BothTop, BothBottom, 0.04f, BothGlow, 0.25f),
                        L(p => Box(p, new Vector2(0f, -0.7f), new Vector2(0.36f, 0.12f), 0.04f), new Color(0.6f, 0.68f, 0.62f), new Color(0.3f, 0.36f, 0.32f), 0.05f));
                case StrikeItem.ArmorPlating:
                    return Draw(size,
                        L(p => Chamfer(p, new Vector2(0f, 0.02f), new Vector2(0.72f, 0.8f), 0.3f), new Color(0.78f, 0.84f, 0.92f), new Color(0.38f, 0.44f, 0.55f), 0.07f,
                            new Color(0.6f, 0.75f, 1f, 0.45f)),
                        L(p => Mathf.Abs(Chamfer(p, new Vector2(0f, 0.02f), new Vector2(0.52f, 0.6f), 0.22f)) - 0.035f, new Color(0.25f, 0.3f, 0.38f), new Color(0.25f, 0.3f, 0.38f), 0f),
                        L(p =>
                        {
                            float d = float.MaxValue;
                            foreach (Vector2 rivet in new[] { new Vector2(-0.48f, 0.56f), new Vector2(0.48f, 0.56f), new Vector2(-0.48f, -0.52f), new Vector2(0.48f, -0.52f) })
                            {
                                d = Mathf.Min(d, Circle(p, rivet, 0.07f));
                            }
                            return d;
                        }, white, new Color(0.8f, 0.85f, 0.9f), 0f));
                case StrikeItem.RepairDrone:
                    return Draw(size,
                        L(p => Mathf.Min(Mathf.Min(Segment(p, new Vector2(-0.62f, 0.62f), new Vector2(0.62f, -0.62f), 0.07f),
                            Segment(p, new Vector2(-0.62f, -0.62f), new Vector2(0.62f, 0.62f), 0.07f)),
                            Mathf.Min(Mathf.Min(Circle(p, new Vector2(-0.62f, 0.62f), 0.2f), Circle(p, new Vector2(0.62f, -0.62f), 0.2f)),
                                Mathf.Min(Circle(p, new Vector2(-0.62f, -0.62f), 0.2f), Circle(p, new Vector2(0.62f, 0.62f), 0.2f)))),
                            new Color(0.75f, 0.82f, 0.9f), new Color(0.35f, 0.42f, 0.52f), 0.05f),
                        L(p => Circle(p, Vector2.zero, 0.38f), new Color(0.6f, 1f, 0.7f), new Color(0.2f, 0.8f, 0.35f), 0.06f, new Color(0.3f, 1f, 0.5f, 0.55f)),
                        L(p => Mathf.Min(Box(p, Vector2.zero, new Vector2(0.07f, 0.22f), 0.02f), Box(p, Vector2.zero, new Vector2(0.22f, 0.07f), 0.02f)), white, white, 0f));
                case StrikeItem.Afterburner:
                    // A nozzle with a long flame roaring down out of it, white-hot inside.
                    return Draw(size,
                        L(p => Flame(new Vector2(p.x, -p.y), new Vector2(0f, -0.05f), 0.98f), new Color(1f, 0.85f, 0.35f), new Color(1f, 0.35f, 0.1f), 0.05f,
                            new Color(1f, 0.55f, 0.2f, 0.6f), 0.2f),
                        L(p => Flame(new Vector2(p.x, -p.y), new Vector2(0f, -0.12f), 0.55f), white, new Color(0.75f, 0.88f, 1f), 0f),
                        L(p => Polygon(p, new[] { new Vector2(-0.45f, 0.85f), new Vector2(0.45f, 0.85f), new Vector2(0.3f, 0.2f), new Vector2(-0.3f, 0.2f) }),
                            new Color(0.75f, 0.78f, 0.85f), new Color(0.38f, 0.4f, 0.48f), 0.06f),
                        L(p => Box(p, new Vector2(0f, 0.2f), new Vector2(0.34f, 0.06f), 0.02f), GoldTop, GoldBottom, 0.04f));
                case StrikeItem.SalvageMagnet:
                    return Draw(size,
                        L(p => Mathf.Min(Mathf.Max(Mathf.Abs(Circle(p, new Vector2(0f, 0.15f), 0.42f)) - 0.16f, -(p.y - 0.15f)),
                            Mathf.Min(Box(p, new Vector2(-0.42f, 0.42f), new Vector2(0.16f, 0.28f)), Box(p, new Vector2(0.42f, 0.42f), new Vector2(0.16f, 0.28f)))),
                            new Color(1f, 0.45f, 0.5f), new Color(0.85f, 0.15f, 0.2f), 0.07f, new Color(1f, 0.3f, 0.35f, 0.5f)),
                        L(p => Mathf.Min(Box(p, new Vector2(-0.42f, 0.62f), new Vector2(0.16f, 0.1f)), Box(p, new Vector2(0.42f, 0.62f), new Vector2(0.16f, 0.1f))), white, white, 0.06f),
                        L(p => Circle(p, new Vector2(0f, -0.62f), 0.26f), GoldTop, GoldBottom, 0.05f, GoldGlow));
                case StrikeItem.WingmanDrones:
                    return Draw(size,
                        L(p => Ship((p - new Vector2(0f, 0.08f)) / 0.62f) * 0.62f, new Color(0.9f, 0.97f, 1f), new Color(0.45f, 0.75f, 1f), 0.06f),
                        L(p => Mathf.Min(Circle(p, new Vector2(-0.68f, -0.35f), 0.17f), Circle(p, new Vector2(0.68f, -0.35f), 0.17f)), AirTop, AirBottom, 0.05f, AirGlow),
                        L(p => Mathf.Min(Segment(p, new Vector2(-0.68f, -0.1f), new Vector2(-0.68f, 0.7f), 0.03f), Segment(p, new Vector2(0.68f, -0.1f), new Vector2(0.68f, 0.7f), 0.03f)),
                            GoldTop, GoldBottom, 0f, GoldGlow));
                case StrikeItem.RepairKit:
                    return Draw(size,
                        L(p => Mathf.Min(Box(p, new Vector2(0f, -0.12f), new Vector2(0.78f, 0.56f), 0.1f), Mathf.Abs(Box(p, new Vector2(0f, 0.55f), new Vector2(0.26f, 0.16f), 0.05f)) - 0.05f),
                            new Color(0.92f, 0.95f, 0.98f), new Color(0.6f, 0.66f, 0.72f), 0.07f, new Color(0.4f, 1f, 0.5f, 0.4f)),
                        L(p => Mathf.Min(Box(p, new Vector2(0f, -0.12f), new Vector2(0.11f, 0.36f), 0.03f), Box(p, new Vector2(0f, -0.12f), new Vector2(0.36f, 0.11f), 0.03f)),
                            new Color(0.5f, 1f, 0.6f), new Color(0.15f, 0.75f, 0.3f), 0.04f));
                default:
                    return null;
            }
        }


        /// <summary>How far from the launcher rocket <paramref name="index"/> (0 to 4) of the swarm rockets' icon is: the middle one farthest.</summary>
        private static float SwarmReach(int index)
        {
            int side = Mathf.Abs(index - 2);
            return side == 0 ? 1.55f : side == 1 ? 1.35f : 1.12f;
        }


        /// <summary>A flame pointing up: a teardrop with its tip at <paramref name="center"/> + <paramref name="height"/> up.</summary>
        private static float Flame(Vector2 p, Vector2 center, float height)
        {
            float bowl = Circle(p, center, height * 0.42f);
            float tip = Polygon(p, new[]
            {
                center + new Vector2(-height * 0.4f, -height * 0.05f), center + new Vector2(height * 0.4f, -height * 0.05f),
                center + new Vector2(height * 0.08f, height * 0.6f), center + new Vector2(0f, height * 0.85f), center + new Vector2(-height * 0.1f, height * 0.55f)
            });
            return Mathf.Min(bowl, tip);
        }
    }
}
