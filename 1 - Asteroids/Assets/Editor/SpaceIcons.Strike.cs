using System;
using UnityEngine;

namespace Portfolio.Asteroids.EditorTools
{
    /// <summary>
    /// The strike icons, in the style of the other icons (signed distance shapes with an ink outline, a glow and a
    /// vertical gradient): one per <see cref="StrikeItem"/> (the Supply Room's cards, the HUD's special weapon, the pickups),
    /// the money pickups, the Supply Room itself, the strike tab and the three difficulties.
    /// Weapon colours follow what they hit: gold for the always-on guns, cyan for air-only, orange for ground-only and
    /// green for weapons that hit both.
    /// </summary>
    internal static partial class SpaceIcons
    {
        /// <summary>The strike icons besides the items, written to Art/Strike/Icons/{name}.png.</summary>
        public static readonly string[] StrikeIconNames =
        {
            "SupplyRoom", "Money", "Energy", "CreditOrb", "SmallArms", "Isotopes", "Thaelite", "FusionCore", "FreyliumOre",
            "Strike", "Rookie", "Veteran", "Elite", "Target", "Scanner"
        };

        private static readonly Color GoldTop = new Color(1f, 0.93f, 0.55f);
        private static readonly Color GoldBottom = new Color(0.95f, 0.6f, 0.12f);
        private static readonly Color GoldGlow = new Color(1f, 0.75f, 0.2f, 0.55f);
        private static readonly Color AirTop = new Color(0.7f, 0.96f, 1f);
        private static readonly Color AirBottom = new Color(0.2f, 0.62f, 1f);
        private static readonly Color AirGlow = new Color(0.3f, 0.8f, 1f, 0.55f);
        private static readonly Color GroundTop = new Color(1f, 0.78f, 0.45f);
        private static readonly Color GroundBottom = new Color(0.95f, 0.38f, 0.1f);
        private static readonly Color GroundGlow = new Color(1f, 0.5f, 0.15f, 0.55f);
        private static readonly Color BothTop = new Color(0.75f, 1f, 0.7f);
        private static readonly Color BothBottom = new Color(0.25f, 0.8f, 0.35f);
        private static readonly Color BothGlow = new Color(0.35f, 1f, 0.45f, 0.5f);


        /// <summary>The icon of a strike item (every value of <see cref="StrikeItem"/>).</summary>
        public static Texture2D StrikeItemIcon(StrikeItem item)
        {
            const int size = 128;
            Color white = Color.white;
            switch (item)
            {
                case StrikeItem.MachineGun:
                    return Draw(size, L(p => Mathf.Min(Bullet(p, new Vector2(-0.3f, -0.05f), 0.8f), Bullet(p, new Vector2(0.3f, -0.05f), 0.8f)),
                        GoldTop, GoldBottom, 0.06f, GoldGlow));
                case StrikeItem.PlasmaCannon:
                    return Draw(size,
                        L(p => Mathf.Min(Circle(p, new Vector2(0f, 0.3f), 0.42f), Polygon(p, new[] { new Vector2(-0.3f, 0.2f), new Vector2(0.3f, 0.2f), new Vector2(0f, -0.9f) })),
                            new Color(0.95f, 0.75f, 1f), new Color(0.6f, 0.3f, 1f), 0.06f, new Color(0.7f, 0.4f, 1f, 0.6f), 0.18f),
                        L(p => Circle(p, new Vector2(0f, 0.3f), 0.2f), white, new Color(0.9f, 0.85f, 1f), 0f));
                case StrikeItem.MicroMissiles:
                    return Draw(size, L(p => Mathf.Min(MissileShape(Rotate(p, 22f), new Vector2(-0.25f, -0.05f), 0.55f),
                        Mathf.Min(MissileShape(p, new Vector2(0f, 0.05f), 0.6f), MissileShape(Rotate(p, -22f), new Vector2(0.25f, -0.05f), 0.55f))),
                        BothTop, BothBottom, 0.06f, BothGlow));
                case StrikeItem.Dumbfire:
                    return Draw(size,
                        L(p => Mathf.Min(Circle(p, new Vector2(-0.3f, -0.75f), 0.18f), Circle(p, new Vector2(0.3f, -0.75f), 0.18f)),
                            new Color(1f, 0.9f, 0.4f), new Color(1f, 0.4f, 0.1f), 0f, new Color(1f, 0.5f, 0.1f, 0.7f), 0.2f),
                        L(p => Mathf.Min(MissileShape(p, new Vector2(-0.3f, 0.1f), 0.72f), MissileShape(p, new Vector2(0.3f, 0.1f), 0.72f)), BothTop, BothBottom, 0.06f, BothGlow));
                case StrikeItem.MiniGun:
                    return Draw(size,
                        L(p => Mathf.Min(Mathf.Abs(Circle(p, Vector2.zero, 0.62f)) - 0.07f, Cross(p, 0.9f, 0.06f)), BothTop, BothBottom, 0.05f, BothGlow),
                        L(p =>
                        {
                            float d = float.MaxValue;
                            for (int i = 0; i < 6; i++)
                            {
                                d = Mathf.Min(d, Circle(p, Rotate(new Vector2(0f, 0.3f), i * 60f), 0.1f));
                            }
                            return d;
                        }, white, new Color(0.85f, 1f, 0.85f), 0.04f));
                case StrikeItem.LaserTurret:
                    return Draw(size,
                        L(p => Mathf.Min(Mathf.Max(Circle(p, new Vector2(0f, -0.55f), 0.55f), -(p.y + 0.55f)), Box(p, new Vector2(0f, -0.62f), new Vector2(0.7f, 0.08f))),
                            AirTop, AirBottom, 0.06f, AirGlow),
                        L(p => Zigzag(p, new Vector2(0f, -0.2f), new Vector2(0.55f, 0.85f), 0.07f), white, new Color(0.8f, 1f, 1f), 0.04f, new Color(0.5f, 0.95f, 1f, 0.8f), 0.2f));
                case StrikeItem.MissilePods:
                    return Draw(size,
                        L(p => Box(p, new Vector2(0f, -0.2f), new Vector2(0.6f, 0.5f), 0.14f), new Color(0.55f, 0.75f, 0.95f), new Color(0.25f, 0.4f, 0.7f), 0.06f, AirGlow),
                        L(p =>
                        {
                            float d = float.MaxValue;
                            for (int i = 0; i < 4; i++)
                            {
                                d = Mathf.Min(d, MissileShape(p, new Vector2(-0.39f + i * 0.26f, 0.35f), 0.5f));
                            }
                            return d;
                        }, AirTop, AirBottom, 0.05f));
                case StrikeItem.AirMissiles:
                    return Draw(size, L(p => Mathf.Min(MissileShape(p, new Vector2(-0.3f, 0f), 0.95f), MissileShape(p, new Vector2(0.3f, 0f), 0.95f)), AirTop, AirBottom, 0.06f, AirGlow));
                case StrikeItem.GroundMissiles:
                    return Draw(size, L(p => MissileShape(new Vector2(p.x, -p.y) / 1.35f, new Vector2(0f, 0f), 0.95f) * 1.35f, GroundTop, GroundBottom, 0.06f, GroundGlow));
                case StrikeItem.Bombs:
                    return Draw(size,
                        L(p => Mathf.Min(Ellipse(p, new Vector2(0f, -0.15f), new Vector2(0.38f, 0.58f)),
                            Polygon(p, new[] { new Vector2(-0.42f, 0.78f), new Vector2(0.42f, 0.78f), new Vector2(0.18f, 0.3f), new Vector2(-0.18f, 0.3f) })),
                            GroundTop, GroundBottom, 0.06f, GroundGlow),
                        L(p => Box(p, new Vector2(0f, -0.1f), new Vector2(0.38f, 0.06f)), new Color(0.2f, 0.15f, 0.1f), new Color(0.2f, 0.15f, 0.1f), 0f));
                case StrikeItem.PowerDisrupter:
                    return Draw(size,
                        L(p => Circle(p, Vector2.zero, 0.7f), new Color(0.85f, 0.6f, 1f), new Color(0.45f, 0.2f, 0.85f), 0.06f, new Color(0.7f, 0.4f, 1f, 0.6f)),
                        L(p => Polygon(p, new[]
                        {
                            new Vector2(0.1f, 0.6f), new Vector2(-0.35f, -0.05f), new Vector2(-0.02f, -0.05f),
                            new Vector2(-0.12f, -0.6f), new Vector2(0.35f, 0.08f), new Vector2(0.03f, 0.08f)
                        }), white, new Color(0.95f, 0.9f, 1f), 0f));
                case StrikeItem.PulseCannon:
                    return Draw(size, L(p =>
                    {
                        float d = float.MaxValue;
                        for (int i = 0; i < 3; i++)
                        {
                            float radius = 0.35f + i * 0.28f;
                            float arc = Mathf.Abs(Circle(p, new Vector2(0f, -0.75f), radius)) - 0.07f;
                            arc = Mathf.Max(arc, -(p.y + 0.75f - Mathf.Abs(p.x) * 0.6f));
                            d = Mathf.Min(d, arc);
                        }
                        return d;
                    }, new Color(0.7f, 1f, 0.95f), new Color(0.15f, 0.75f, 0.7f), 0.05f, new Color(0.3f, 1f, 0.9f, 0.55f)));
                case StrikeItem.Deathray:
                    return Draw(size,
                        L(p => Box(p, new Vector2(0f, 0.1f), new Vector2(0.22f, 0.85f), 0.1f), new Color(1f, 0.6f, 0.6f), new Color(1f, 0.15f, 0.2f), 0.05f, new Color(1f, 0.25f, 0.3f, 0.75f), 0.25f),
                        L(p => Box(p, new Vector2(0f, 0.1f), new Vector2(0.07f, 0.8f), 0.05f), white, white, 0f),
                        L(p => Circle(p, new Vector2(0f, -0.72f), 0.22f), white, new Color(1f, 0.8f, 0.8f), 0.05f, new Color(1f, 0.4f, 0.4f, 0.7f)));
                case StrikeItem.TwinLaser:
                    return Draw(size,
                        L(p => Mathf.Min(Box(p, new Vector2(-0.38f, 0.05f), new Vector2(0.14f, 0.85f), 0.07f), Box(p, new Vector2(0.38f, 0.05f), new Vector2(0.14f, 0.85f), 0.07f)),
                            new Color(1f, 0.65f, 1f), new Color(0.85f, 0.2f, 0.9f), 0.05f, new Color(1f, 0.35f, 1f, 0.7f), 0.22f),
                        L(p => Mathf.Min(Box(p, new Vector2(-0.38f, 0.05f), new Vector2(0.04f, 0.8f)), Box(p, new Vector2(0.38f, 0.05f), new Vector2(0.04f, 0.8f))), white, white, 0f));
                case StrikeItem.MegaBomb:
                    return Draw(size,
                        L(p =>
                        {
                            float angle = Mathf.Atan2(p.y, p.x);
                            float spikes = Mathf.Pow(Mathf.Abs(Mathf.Cos(angle * 4f)), 8f) * 0.3f;
                            return p.magnitude - (0.62f + spikes);
                        }, new Color(1f, 0.95f, 0.8f), new Color(1f, 0.55f, 0.2f), 0.05f, new Color(1f, 0.7f, 0.3f, 0.6f)),
                        L(p => Mathf.Min(Circle(p, new Vector2(0f, -0.05f), 0.42f), Box(p, new Vector2(0f, 0.42f), new Vector2(0.14f, 0.12f), 0.03f)),
                            new Color(0.4f, 0.42f, 0.5f), new Color(0.15f, 0.16f, 0.2f), 0.06f),
                        L(p => Circle(p, new Vector2(-0.14f, 0.08f), 0.09f), new Color(1f, 1f, 1f, 0.8f), new Color(1f, 1f, 1f, 0.5f), 0f));
                case StrikeItem.EnergyModule:
                    return Draw(size,
                        L(p => Mathf.Min(Box(p, new Vector2(0f, -0.08f), new Vector2(0.45f, 0.72f), 0.1f), Box(p, new Vector2(0f, 0.72f), new Vector2(0.2f, 0.1f), 0.03f)),
                            new Color(0.3f, 0.36f, 0.45f), new Color(0.12f, 0.15f, 0.2f), 0.06f, new Color(0.4f, 1f, 0.5f, 0.45f)),
                        L(p => Polygon(p, new[]
                        {
                            new Vector2(0.1f, 0.5f), new Vector2(-0.25f, -0.05f), new Vector2(-0.02f, -0.05f),
                            new Vector2(-0.1f, -0.6f), new Vector2(0.27f, 0.05f), new Vector2(0.04f, 0.05f)
                        }), new Color(0.75f, 1f, 0.7f), new Color(0.3f, 0.9f, 0.35f), 0.04f, new Color(0.4f, 1f, 0.5f, 0.6f)));
                case StrikeItem.PhaseShield:
                    return Draw(size,
                        L(p => Polygon(p, RegularPolygon(6, 0.92f, Mathf.PI / 6f, Vector2.zero)), new Color(0.8f, 0.7f, 1f), new Color(0.45f, 0.3f, 0.95f), 0.07f, new Color(0.6f, 0.45f, 1f, 0.6f)),
                        L(p => Mathf.Abs(Polygon(p, RegularPolygon(6, 0.62f, Mathf.PI / 6f, Vector2.zero))) - 0.045f, white, new Color(0.9f, 0.9f, 1f), 0f),
                        L(p => Mathf.Abs(Polygon(p, RegularPolygon(6, 0.34f, Mathf.PI / 6f, Vector2.zero))) - 0.045f, white, new Color(0.9f, 0.9f, 1f), 0f));
                case StrikeItem.IonScanner:
                    return ScannerIcon(size);
                default:
                    throw new ArgumentException($"No strike icon for {item}.", nameof(item));
            }
        }


        /// <summary>The strike icon <paramref name="name"/> (one of <see cref="StrikeIconNames"/>).</summary>
        public static Texture2D StrikeIcon(string name)
        {
            const int size = 128;
            Color white = Color.white;
            switch (name)
            {
                case "SupplyRoom":
                    return Draw(size,
                        L(p => Box(p, new Vector2(0f, -0.12f), new Vector2(0.78f, 0.62f), 0.06f), new Color(0.85f, 0.72f, 0.45f), new Color(0.55f, 0.42f, 0.22f), 0.07f, GoldGlow),
                        L(p => Mathf.Min(Mathf.Abs(Box(p, new Vector2(0f, -0.12f), new Vector2(0.62f, 0.46f))) - 0.05f,
                            Mathf.Min(Segment(p, new Vector2(-0.6f, -0.56f), new Vector2(0.6f, 0.32f), 0.06f), Segment(p, new Vector2(-0.6f, 0.32f), new Vector2(0.6f, -0.56f), 0.06f))),
                            new Color(0.4f, 0.3f, 0.16f), new Color(0.3f, 0.22f, 0.1f), 0f),
                        L(p => Chamfer(p, new Vector2(0f, 0.62f), new Vector2(0.34f, 0.14f), 0.08f), GoldTop, GoldBottom, 0.05f));
                case "Money":
                    return CoinIcon(size, GoldTop, GoldBottom, GoldGlow);
                case "Energy":
                    return Draw(size, L(p => Polygon(p, new[]
                    {
                        new Vector2(0.15f, 0.9f), new Vector2(-0.5f, -0.05f), new Vector2(-0.05f, -0.05f),
                        new Vector2(-0.2f, -0.9f), new Vector2(0.5f, 0.12f), new Vector2(0.06f, 0.12f)
                    }), new Color(0.8f, 1f, 0.7f), new Color(0.3f, 0.9f, 0.35f), 0.06f, new Color(0.4f, 1f, 0.5f, 0.6f)));
                case "CreditOrb":
                    return Draw(size,
                        L(p => Circle(p, Vector2.zero, 0.62f), GoldTop, GoldBottom, 0.06f, new Color(1f, 0.8f, 0.3f, 0.7f), 0.25f),
                        L(p => Circle(p, new Vector2(-0.18f, 0.2f), 0.18f), new Color(1f, 1f, 1f, 0.85f), new Color(1f, 1f, 1f, 0.6f), 0f));
                case "SmallArms":
                    return Draw(size,
                        L(p => Box(p, new Vector2(0f, -0.3f), new Vector2(0.8f, 0.42f), 0.06f), new Color(0.55f, 0.62f, 0.35f), new Color(0.3f, 0.36f, 0.18f), 0.07f),
                        L(p =>
                        {
                            float d = float.MaxValue;
                            for (int i = 0; i < 4; i++)
                            {
                                d = Mathf.Min(d, Bullet(p, new Vector2(-0.45f + i * 0.3f, 0.35f), 0.55f));
                            }
                            return d;
                        }, GoldTop, GoldBottom, 0.05f, GoldGlow),
                        L(p => Box(p, new Vector2(0f, -0.3f), new Vector2(0.5f, 0.08f)), new Color(1f, 0.85f, 0.3f), new Color(1f, 0.85f, 0.3f), 0f));
                case "Isotopes":
                    return Draw(size,
                        L(p => Box(p, Vector2.zero, new Vector2(0.48f, 0.85f), 0.2f), new Color(0.65f, 1f, 0.55f), new Color(0.2f, 0.7f, 0.25f), 0.07f, new Color(0.4f, 1f, 0.35f, 0.7f), 0.2f),
                        L(p => Trefoil(p, 0.36f), new Color(0.1f, 0.15f, 0.08f), new Color(0.1f, 0.15f, 0.08f), 0f));
                case "Thaelite":
                    return Draw(size, L(p =>
                    {
                        float d = Polygon(p, new[] { new Vector2(0f, 0.92f), new Vector2(0.28f, 0.1f), new Vector2(0f, -0.8f), new Vector2(-0.28f, 0.1f) });
                        d = Mathf.Min(d, Polygon(Rotate(p - new Vector2(-0.42f, -0.2f), 30f), new[] { new Vector2(0f, 0.55f), new Vector2(0.18f, 0f), new Vector2(0f, -0.5f), new Vector2(-0.18f, 0f) }));
                        return Mathf.Min(d, Polygon(Rotate(p - new Vector2(0.42f, -0.25f), -28f), new[] { new Vector2(0f, 0.5f), new Vector2(0.17f, 0f), new Vector2(0f, -0.45f), new Vector2(-0.17f, 0f) }));
                    }, new Color(0.7f, 0.9f, 1f), new Color(0.2f, 0.45f, 1f), 0.06f, new Color(0.35f, 0.6f, 1f, 0.65f)));
                case "FusionCore":
                    return Draw(size,
                        L(p => Mathf.Min(Mathf.Abs(Ellipse(p, Vector2.zero, new Vector2(0.9f, 0.32f))) - 0.05f, Mathf.Abs(Ellipse(Rotate(p, 60f), Vector2.zero, new Vector2(0.9f, 0.32f))) - 0.05f),
                            new Color(1f, 0.85f, 0.5f), new Color(1f, 0.55f, 0.2f), 0.03f),
                        L(p => Circle(p, Vector2.zero, 0.42f), new Color(1f, 0.95f, 0.7f), new Color(1f, 0.45f, 0.1f), 0.06f, new Color(1f, 0.55f, 0.15f, 0.8f), 0.25f),
                        L(p => Circle(p, new Vector2(-0.12f, 0.12f), 0.14f), white, new Color(1f, 1f, 0.9f), 0f));
                case "FreyliumOre":
                    return Draw(size,
                        L(p =>
                        {
                            float angle = Mathf.Atan2(p.y, p.x);
                            return p.magnitude - (0.72f + Mathf.Sin(angle * 5f + 0.4f) * 0.08f + Mathf.Sin(angle * 3f) * 0.05f);
                        }, new Color(0.5f, 0.4f, 0.6f), new Color(0.22f, 0.16f, 0.3f), 0.07f, new Color(0.85f, 0.4f, 1f, 0.55f)),
                        L(p => Mathf.Min(Zigzag(p, new Vector2(-0.45f, -0.35f), new Vector2(0.4f, 0.4f), 0.05f), Zigzag(p, new Vector2(-0.2f, 0.45f), new Vector2(0.15f, -0.5f), 0.04f)),
                            new Color(1f, 0.7f, 1f), new Color(0.9f, 0.35f, 1f), 0f));
                case "Strike":
                    return Draw(size,
                        L(p => Mathf.Min(Box(p, new Vector2(0f, -0.62f), new Vector2(0.95f, 0.06f)), Box(p, new Vector2(0f, -0.84f), new Vector2(0.95f, 0.04f))),
                            new Color(0.95f, 0.75f, 0.45f), new Color(0.75f, 0.5f, 0.25f), 0f),
                        L(p => Ship((p - new Vector2(0f, 0.12f)) / 0.78f) * 0.78f, new Color(1f, 0.85f, 0.6f), new Color(1f, 0.5f, 0.2f), 0.06f, new Color(1f, 0.55f, 0.2f, 0.5f)));
                case "Rookie":
                    return RankIcon(size, 1, false);
                case "Veteran":
                    return RankIcon(size, 2, false);
                case "Elite":
                    return RankIcon(size, 3, true);
                case "Target":
                    return Draw(size,
                        L(p => Mathf.Min(Mathf.Abs(Circle(p, Vector2.zero, 0.6f)) - 0.07f, Cross(p, 0.92f, 0.07f)), new Color(1f, 0.6f, 0.55f), new Color(1f, 0.25f, 0.25f), 0.05f, new Color(1f, 0.3f, 0.3f, 0.5f)),
                        L(p => Circle(p, Vector2.zero, 0.12f), white, white, 0f));
                case "Scanner":
                    return ScannerIcon(size);
                default:
                    throw new ArgumentException($"No strike icon named {name}.", nameof(name));
            }
        }


        private static Texture2D ScannerIcon(int size)
        {
            return Draw(size,
                L(p =>
                {
                    float d = Circle(p, new Vector2(0f, -0.6f), 0.14f);
                    for (int i = 0; i < 3; i++)
                    {
                        float arc = Mathf.Abs(Circle(p, new Vector2(0f, -0.6f), 0.4f + i * 0.28f)) - 0.06f;
                        arc = Mathf.Max(arc, -(p.y + 0.6f - Mathf.Abs(p.x) * 0.9f));
                        d = Mathf.Min(d, arc);
                    }
                    return d;
                }, AirTop, AirBottom, 0.05f, AirGlow));
        }


        private static Texture2D CoinIcon(int size, Color top, Color bottom, Color glow)
        {
            return Draw(size,
                L(p => Circle(p, Vector2.zero, 0.85f), top, bottom, 0.07f, glow),
                L(p => Mathf.Abs(Circle(p, Vector2.zero, 0.66f)) - 0.035f, new Color(0.75f, 0.45f, 0.08f), new Color(0.75f, 0.45f, 0.08f), 0f),
                L(p => Mathf.Min(Segment(p, new Vector2(0f, -0.52f), new Vector2(0f, 0.52f), 0.05f), DollarS(p)), new Color(0.6f, 0.33f, 0.05f), new Color(0.6f, 0.33f, 0.05f), 0f));
        }


        private static Texture2D RankIcon(int size, int chevrons, bool star)
        {
            return Draw(size,
                L(p =>
                {
                    float d = float.MaxValue;
                    for (int i = 0; i < chevrons; i++)
                    {
                        float y = -0.55f + i * 0.4f + (star ? -0.1f : 0f) + (3 - chevrons) * 0.2f;
                        d = Mathf.Min(d, Mathf.Min(Segment(p, new Vector2(-0.62f, y + 0.3f), new Vector2(0f, y), 0.1f), Segment(p, new Vector2(0f, y), new Vector2(0.62f, y + 0.3f), 0.1f)));
                    }
                    if (star)
                    {
                        d = Mathf.Min(d, Polygon(p, StarPoints(0.3f, 0.13f, new Vector2(0f, 0.72f))));
                    }
                    return d;
                }, GoldTop, GoldBottom, 0.06f, GoldGlow));
        }


        // ------------------------------------------------------------------ shapes

        /// <summary>A cartridge standing up: a rounded case and a pointed bullet.</summary>
        private static float Bullet(Vector2 p, Vector2 center, float height)
        {
            float half = height * 0.5f;
            float body = Box(p, center + new Vector2(0f, -half * 0.25f), new Vector2(0.12f, half * 0.75f), 0.03f);
            float tip = Polygon(p, new[]
            {
                center + new Vector2(-0.12f, half * 0.5f), center + new Vector2(0.12f, half * 0.5f), center + new Vector2(0.06f, half * 0.95f),
                center + new Vector2(0f, half * 1.05f), center + new Vector2(-0.06f, half * 0.95f)
            });
            return Mathf.Min(body, tip);
        }


        /// <summary>A missile pointing up, <paramref name="length"/> long, centred on <paramref name="center"/>.</summary>
        private static float MissileShape(Vector2 p, Vector2 center, float length)
        {
            float half = length * 0.5f;
            float width = length * 0.15f;
            Vector2 q = p - center;
            float body = Box(q, new Vector2(0f, -half * 0.1f), new Vector2(width, half * 0.8f), width * 0.6f);
            float nose = Polygon(q, new[] { new Vector2(-width, half * 0.65f), new Vector2(width, half * 0.65f), new Vector2(0f, half) });
            float fins = Polygon(q, new[] { new Vector2(-width * 2.3f, -half), new Vector2(width * 2.3f, -half), new Vector2(width, -half * 0.5f), new Vector2(-width, -half * 0.5f) });
            return Mathf.Min(body, Mathf.Min(nose, fins));
        }


        private static float Ellipse(Vector2 p, Vector2 center, Vector2 radii)
        {
            Vector2 q = new Vector2((p.x - center.x) / radii.x, (p.y - center.y) / radii.y);
            return (q.magnitude - 1f) * Mathf.Min(radii.x, radii.y);
        }


        private static float Cross(Vector2 p, float reach, float width)
        {
            float horizontal = Mathf.Max(Box(p, Vector2.zero, new Vector2(reach, width)), -Box(p, Vector2.zero, new Vector2(0.28f, reach)));
            float vertical = Mathf.Max(Box(p, Vector2.zero, new Vector2(width, reach)), -Box(p, Vector2.zero, new Vector2(reach, 0.28f)));
            return Mathf.Min(horizontal, vertical);
        }


        /// <summary>A jagged line (a lightning zap or an ore vein) from <paramref name="a"/> to <paramref name="b"/>.</summary>
        private static float Zigzag(Vector2 p, Vector2 a, Vector2 b, float width)
        {
            Vector2 along = b - a;
            Vector2 side = new Vector2(-along.y, along.x).normalized * 0.12f;
            Vector2 previous = a;
            float d = float.MaxValue;
            for (int i = 1; i <= 4; i++)
            {
                Vector2 next = i == 4 ? b : a + along * (i / 4f) + side * (i % 2 == 0 ? -1f : 1f);
                d = Mathf.Min(d, Segment(p, previous, next, width));
                previous = next;
            }
            return d;
        }


        /// <summary>The three blades of a radiation sign around a hub.</summary>
        private static float Trefoil(Vector2 p, float radius)
        {
            float angle = Mathf.Atan2(p.y, p.x) * Mathf.Rad2Deg;
            float sector = Mathf.Repeat(angle - 90f + 30f, 120f);
            float blade = Mathf.Max(Mathf.Max(p.magnitude - radius, radius * 0.35f - p.magnitude), (Mathf.Abs(sector - 30f) - 30f) * Mathf.Deg2Rad * p.magnitude);
            return Mathf.Min(blade, Circle(p, Vector2.zero, radius * 0.22f));
        }


        /// <summary>The S of a dollar sign: two half rings.</summary>
        private static float DollarS(Vector2 p)
        {
            const float r = 0.22f;
            // The upper ring without its lower right quarter, the lower ring without its upper left quarter.
            float topArc = Mathf.Max(Mathf.Abs(Circle(p, new Vector2(0f, 0.2f), r)) - 0.055f, -Mathf.Max(-p.x, 0.2f - p.y));
            float bottomArc = Mathf.Max(Mathf.Abs(Circle(p, new Vector2(0f, -0.2f), r)) - 0.055f, -Mathf.Max(p.x, p.y + 0.2f));
            return Mathf.Min(topArc, bottomArc);
        }
    }
}
