using System.Collections.Generic;
using Gamebox.Editor;
using UnityEngine;

namespace Portfolio.DeathRace.EditorTools
{
    /// <summary>
    /// The icons of the game, each a few bold shapes (signed distance functions on a 128 unit square, y up) drawn in the
    /// style of a look: Scorched fills them in warm, shaded colours with a dark cartoon outline and a hard shadow;
    /// Nightfall draws them as dark glass outlined in neon with a glow. The same shapes make the 128 px icons, the 256 px
    /// pictures of the five upgrades and the launcher's emblem (<see cref="GameIcon"/>).
    /// </summary>
    internal static class DeathRaceArtIcons
    {
        /// <summary>
        /// The icons every look has, by file name (and by key: theme.SpriteOf). After the first twenty come those of the
        /// leagues: the nitro system and the gun mount, the fittings, the guns, the special weapons, the workshop, a league
        /// and the statuses a car can suffer (the names in Arsenal's Icon fields).
        /// </summary>
        public static readonly string[] Names =
        {
            "Ammo", "Mine", "Rocket", "Nitro", "Cash", "Repair", "Health", "Flag", "Skull", "Engine", "Tyres", "Armour",
            "Spikes", "Piercing", "Sabotage", "Trophy", "Star", "Lock", "Car", "Timer",
            "Boost", "GunMount", "Cage", "Shield", "Jammer", "Welder", "Magazine", "MachineGun", "Minigun", "Flamethrower", "Plasma",
            "Oil", "Emp", "Mortar", "Workshop", "League", "Burning", "Jammed", "Slicked"
        };

        private sealed class Part
        {
            public Sdf Shape;
            public Color Fill;
            public Color Accent;
            /// <summary>Drawn without an outline or a shadow: a detail on top of the part below it.</summary>
            public bool Detail;
        }

        private sealed class Icon
        {
            public readonly List<Part> Parts = new List<Part>();
            public Color Accent;

            public Icon(Color accent)
            {
                Accent = accent;
            }

            public Icon Add(Sdf shape, Color fill, Color? accent = null)
            {
                Parts.Add(new Part { Shape = shape, Fill = fill, Accent = accent ?? Accent });
                return this;
            }

            public Icon Detail(Sdf shape, Color fill, Color? accent = null)
            {
                Parts.Add(new Part { Shape = shape, Fill = fill, Accent = accent ?? Accent, Detail = true });
                return this;
            }
        }

        private static Color Hex(uint rgb, float alpha = 1f)
        {
            return DeathRaceThemeSpec.Hex(rgb, alpha);
        }

        private static Vector2 V(float x, float y)
        {
            return new Vector2(x, y);
        }

        /// <summary>Draws icon <paramref name="name"/> at <paramref name="size"/> pixels in the style of <paramref name="spec"/>.</summary>
        public static Texture2D Draw(string name, int size, DeathRaceThemeSpec spec)
        {
            Icon icon = Build(name);
            var raster = new Raster(size, size, Color.clear);
            Render(raster, icon, spec.Night, size / 128f, spec);
            return raster.ToTexture();
        }

        private static void Render(Raster r, Icon icon, bool neon, float k, DeathRaceThemeSpec spec)
        {
            var bounds = new Rect(0f, 0f, r.Width, r.Height);
            Sdf Scaled(Sdf shape)
            {
                return p => shape(p / k) * k;
            }

            Sdf union = p =>
            {
                float d = float.MaxValue;
                foreach (Part part in icon.Parts)
                {
                    if (!part.Detail)
                    {
                        d = Mathf.Min(d, part.Shape(p / k) * k);
                    }
                }
                return d;
            };
            if (neon)
            {
                r.Shadow(union, DeathRaceArtInterface.WithAlpha(icon.Accent, 0.55f), 9f * k, Vector2.zero, bounds);
                foreach (Part part in icon.Parts)
                {
                    Sdf shape = Scaled(part.Shape);
                    Color glass = Color.Lerp(spec.PlateDark, part.Accent, part.Detail ? 0.5f : 0.2f);
                    r.Fill(shape, p => Color.Lerp(DeathRaceArtInterface.Shade(glass, 0.7f), glass, Mathf.Clamp01(p.y / r.Height)), bounds);
                    float width = (part.Detail ? 1.8f : 3f) * k;
                    r.Fill(p => Sd.Outline(shape(p) + width * 0.5f, width), part.Accent, bounds);
                    r.Fill(p => Sd.Outline(shape(p) + width * 0.5f, width * 0.35f), DeathRaceArtInterface.Lighten(part.Accent, 0.6f), bounds);
                }
            }
            else
            {
                r.Shadow(union, new Color(0f, 0f, 0f, 0.55f), 3.5f * k, new Vector2(2f * k, -3f * k), bounds);
                var outline = Hex(0x1A0E06);
                foreach (Part part in icon.Parts)
                {
                    Sdf shape = Scaled(part.Shape);
                    if (!part.Detail)
                    {
                        r.Fill(p => shape(p) - 3.2f * k, outline, bounds);
                    }
                    Color fill = part.Fill;
                    r.Fill(shape, p =>
                    {
                        float t = Mathf.Clamp01(p.y / r.Height);
                        return Color.Lerp(DeathRaceArtInterface.Shade(fill, 0.72f), DeathRaceArtInterface.Lighten(fill, 0.18f), t);
                    }, bounds);
                }
            }
        }

        private static Icon Build(string name)
        {
            switch (name)
            {
                case "Ammo": return Ammo();
                case "Mine": return Mine();
                case "Rocket": return Rocket();
                case "Nitro": return Nitro();
                case "Cash": return Cash();
                case "Repair": return Repair();
                case "Health": return Health();
                case "Flag": return Flag();
                case "Skull": return Skull(V(64f, 64f), 1f, Hex(0xFF3FC8));
                case "Engine": return Engine();
                case "Tyres": return Tyres();
                case "Armour": return Armour();
                case "Spikes": return Spikes();
                case "Piercing": return Piercing();
                case "Sabotage": return Sabotage();
                case "Trophy": return Trophy();
                case "Star": return new Icon(Hex(0xFFD24A)).Add(p => Sd.Star(p, V(64f, 62f), 52f, 5, 0.45f), Hex(0xE8B42A));
                case "Lock": return Lock();
                case "Car": return Car();
                case "Boost": return Boost();
                case "GunMount": return GunMount();
                case "Cage": return Cage();
                case "Shield": return Shield();
                case "Jammer": return Jammer();
                case "Welder": return Welder();
                case "Magazine": return Magazine();
                case "MachineGun": return MachineGun();
                case "Minigun": return Minigun();
                case "Flamethrower": return Flamethrower();
                case "Plasma": return Plasma();
                case "Oil": return Oil();
                case "Emp": return Emp();
                case "Mortar": return Mortar();
                case "Workshop": return Workshop();
                case "League": return League();
                case "Burning": return Burning();
                case "Jammed": return Jammed();
                case "Slicked": return Slicked();
                default: return Timer();
            }
        }

        // ------------------------------------------------------------------------------------------------
        // The icons (128 units, y up)
        // ------------------------------------------------------------------------------------------------

        private static Icon Ammo()
        {
            var icon = new Icon(Hex(0xFFD24A));
            for (int i = -1; i <= 1; i++)
            {
                float x = 64f + i * 30f;
                float lift = i == 0 ? 8f : 0f;
                float tipY = 74f + lift;
                Vector2 tip = V(x, tipY);
                icon.Add(p => Sd.Union(Sd.Box(p, V(x, 47f + lift * 0.5f), V(11f, 27f + lift * 0.5f), 2f), Sd.Box(p, V(x, 21f), V(12.5f, 3.5f), 1f)), Hex(0xD9A441));
                icon.Add(p => Sd.Intersect(Sd.Ellipse(p, tip, V(11f, 26f)), tipY - p.y), Hex(0xC8673A));
                icon.Detail(p => Sd.Box(p, V(x, 30f), V(11.2f, 1.6f)), Hex(0x8A6420));
            }
            return icon;
        }

        private static Icon Mine()
        {
            var icon = new Icon(Hex(0xFF3A3A));
            var c = V(64f, 62f);
            icon.Add(p => Sd.Circle(p, c, 42f), Hex(0x4A5056));
            icon.Detail(p =>
            {
                float d = float.MaxValue;
                for (int i = 0; i < 3; i++)
                {
                    float a = i * Mathf.PI * 2f / 3f + Mathf.PI / 2f;
                    d = Mathf.Min(d, Sd.Circle(p, c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 36.5f, 2.8f));
                }
                return d;
            }, Hex(0xB8BEC4));
            icon.Add(p => Sd.Circle(p, c, 31f), Hex(0xE8B21C), Hex(0xFFD21F));
            icon.Detail(p =>
            {
                float angle = Mathf.Atan2(p.y - c.y, p.x - c.x) + Mathf.PI;
                float stripe = Mathf.Repeat(angle / (Mathf.PI * 2f) * 10f, 1f) < 0.5f ? -1f : 1f;
                return Mathf.Max(Sd.Ring(p, c, 25f, 12f), stripe);
            }, Hex(0x1C1B1A));
            icon.Add(p => Sd.Circle(p, c, 15f), Hex(0x7A838C));
            icon.Add(p => Sd.Circle(p, c, 8f), Hex(0xE8402A), Hex(0xFF5A3A));
            icon.Detail(p => Sd.Circle(p, c + V(-2.5f, 2.5f), 2.6f), Hex(0xFFD0C0));
            return icon;
        }

        private static Icon Rocket()
        {
            var icon = new Icon(Hex(0xFF8A2A));
            var c = V(64f, 64f);
            const float turn = -45f;
            Vector2 R(Vector2 p)
            {
                return Sd.Rotate(p, c, turn) - c;
            }

            icon.Add(p =>
            {
                Vector2 q = R(p);
                return Sd.Union(Sd.Ellipse(q, V(0f, -46f), V(9f, 16f)), Sd.Ellipse(q, V(0f, -40f), V(6f, 10f)));
            }, Hex(0xFF9A2A), Hex(0xFFD24A));
            icon.Add(p =>
            {
                Vector2 q = R(p);
                Vector2[] left = { V(-11f, -10f), V(-25f, -30f), V(-25f, -36f), V(-11f, -26f) };
                Vector2[] right = { V(11f, -10f), V(25f, -30f), V(25f, -36f), V(11f, -26f) };
                return Sd.Union(Sd.Polygon(q, left), Sd.Polygon(q, right));
            }, Hex(0xC62E24), Hex(0xFF3A3A));
            icon.Add(p =>
            {
                Vector2 q = R(p);
                return Sd.Union(Sd.Box(q, V(0f, 2f), V(11f, 30f), 4f), Sd.Intersect(Sd.Ellipse(q, V(0f, 28f), V(11f, 24f)), 28f - q.y));
            }, Hex(0xD8D4CC), Hex(0xFF8A2A));
            icon.Detail(p =>
            {
                Vector2 q = R(p);
                return Sd.Intersect(Sd.Intersect(Sd.Ellipse(q, V(0f, 28f), V(11f, 24f)), 28f - q.y), -(q.y - 38f));
            }, Hex(0xC62E24), Hex(0xFF3A3A));
            icon.Detail(p => Sd.Circle(R(p), V(0f, 10f), 5f), Hex(0x3A6EA8), Hex(0x3FE6FF));
            icon.Add(p => Sd.Box(R(p), V(0f, -31f), V(7f, 4f), 1f), Hex(0x4A4F55));
            return icon;
        }

        private static Icon Nitro()
        {
            var icon = new Icon(Hex(0x3FE6FF));
            icon.Add(p => Sd.Union(Sd.Box(p, V(64f, 92f), V(8f, 10f), 2f), Sd.Box(p, V(64f, 106f), V(16f, 4f), 2f)), Hex(0xB8BEC4));
            icon.Add(p => Sd.Box(p, V(64f, 50f), V(22f, 38f), 20f), Hex(0x2F6BFF));
            icon.Detail(p => Sd.Box(p, V(64f, 52f), V(22.2f, 11f)), Hex(0xEDEAE2), Hex(0xEAF8FF));
            icon.Detail(p => Sd.Union(Sd.Union(Sd.Segment(p, V(55f, 45f), V(55f, 59f), 2.6f), Sd.Segment(p, V(55f, 59f), V(73f, 45f), 2.6f)), Sd.Segment(p, V(73f, 45f), V(73f, 59f), 2.6f)),
                Hex(0x1E3E70), Hex(0x2F6BFF));
            icon.Detail(p => Sd.Box(p, V(52f, 72f), V(3f, 12f), 3f), Hex(0x8FB6FF, 0.8f));
            return icon;
        }

        private static Icon Cash()
        {
            var icon = new Icon(Hex(0x5CFFA0));
            var note = V(54f, 66f);
            const float turn = 14f;
            icon.Add(p => Sd.Box(Sd.Rotate(p, note, turn), note, V(40f, 24f), 4f), Hex(0x3E9A4E));
            icon.Detail(p => Sd.Outline(Sd.Box(Sd.Rotate(p, note, turn), note, V(33f, 17f), 3f), 2f), Hex(0x86D88E));
            icon.Detail(p => Sd.Circle(Sd.Rotate(p, note, turn), note, 9f), Hex(0x86D88E));
            var coin = V(80f, 46f);
            icon.Add(p => Sd.Circle(p, coin, 27f), Hex(0xE0B030), Hex(0xFFD24A));
            icon.Detail(p => Sd.Ring(p, coin, 21f, 2.2f), Hex(0xA8801A), Hex(0xFFD24A));
            Vector2[] s =
            {
                V(88f, 55f), V(84f, 58.5f), V(76f, 58.5f), V(72f, 55f), V(72f, 50.5f), V(76f, 47.5f), V(84f, 45f), V(88f, 42f), V(88f, 37f), V(84f, 33.5f),
                V(76f, 33.5f), V(72f, 37f)
            };
            icon.Detail(p =>
            {
                float d = Sd.Segment(p, coin + V(0f, -17f), coin + V(0f, 17f), 2.2f);
                for (int i = 0; i < s.Length - 1; i++)
                {
                    d = Mathf.Min(d, Sd.Segment(p, s[i], s[i + 1], 2.8f));
                }
                return d;
            }, Hex(0x7A5A10), Hex(0xFFF2B0));
            return icon;
        }

        private static Icon Repair()
        {
            var icon = new Icon(Hex(0xFFE08A));
            var head = V(88f, 88f);
            var tail = V(36f, 36f);
            icon.Add(p =>
            {
                float d = Sd.Union(Sd.Segment(p, tail, head, 9f), Sd.Circle(p, head, 21f));
                d = Sd.Union(d, Sd.Circle(p, tail, 14f));
                d = Sd.Subtract(d, Sd.Box(Sd.Rotate(p, head, 45f), head + V(0f, 12f), V(8f, 16f), 1f));
                d = Sd.Subtract(d, Sd.Circle(p, tail, 6f));
                return d;
            }, Hex(0xB8BEC4));
            icon.Detail(p => Sd.Segment(p, V(46f, 50f), V(72f, 76f), 2.2f), Hex(0xE6EAEE));
            return icon;
        }

        private static Icon Health()
        {
            var icon = new Icon(Hex(0xFF3F6A));
            icon.Add(p => Sd.Box(p, V(64f, 64f), V(46f, 46f), 14f), Hex(0xE8E2D6), Hex(0xFF8FB0));
            icon.Add(p => Sd.Union(Sd.Box(p, V(64f, 64f), V(13f, 34f), 3f), Sd.Box(p, V(64f, 64f), V(34f, 13f), 3f)), Hex(0xD8302A));
            return icon;
        }

        private static Icon Flag()
        {
            var icon = new Icon(Hex(0xEAF2FF));
            float Wave(float x)
            {
                return Mathf.Sin((x - 32f) * 0.075f) * 6f;
            }

            Sdf cloth = p => Sd.Box(new Vector2(p.x, p.y - Wave(p.x)), V(70f, 84f), V(36f, 22f), 2f);
            icon.Add(cloth, Color.white);
            icon.Detail(p =>
            {
                float d = cloth(p);
                if (d > 0f)
                {
                    return d;
                }
                float fx = (p.x - 34f) / 12f;
                float fy = (p.y - Wave(p.x) - 62f) / 11f;
                int column = Mathf.FloorToInt(fx);
                int row = Mathf.FloorToInt(fy);
                float edge = Mathf.Min(Mathf.Min(fx - column, column + 1 - fx) * 12f, Mathf.Min(fy - row, row + 1 - fy) * 11f);
                return Mathf.Max(d, (column + row) % 2 == 0 ? -edge : edge);
            }, Hex(0x1A1A1C), Hex(0x2FE4FF));
            icon.Add(p => Sd.Union(Sd.Segment(p, V(32f, 14f), V(32f, 110f), 4.5f), Sd.Circle(p, V(32f, 112f), 6f)), Hex(0x7A7F86));
            return icon;
        }

        private static Icon Skull(Vector2 c, float s, Color accent)
        {
            var icon = new Icon(accent);
            Vector2 At(float x, float y)
            {
                return c + new Vector2(x, y) * s;
            }

            icon.Add(p => Sd.SmoothUnion(Sd.Circle(p, At(0f, 8f), 38f * s), Sd.Box(p, At(0f, -24f), new Vector2(22f, 16f) * s, 8f * s), 10f * s), Hex(0xE8DEC8), Hex(0xF2E6FF));
            icon.Detail(p => Sd.Union(Sd.Ellipse(p, At(-15f, 4f), new Vector2(11f, 12f) * s), Sd.Ellipse(p, At(15f, 4f), new Vector2(11f, 12f) * s)), Hex(0x1A1214), accent);
            icon.Detail(p => Sd.Polygon(p, new[] { At(0f, -6f), At(-6f, -16f), At(6f, -16f) }), Hex(0x1A1214), accent);
            icon.Detail(p =>
            {
                float d = Sd.Segment(p, At(-16f, -27f), At(16f, -27f), 1.6f * s);
                foreach (float x in new[] { -10f, 0f, 10f })
                {
                    d = Mathf.Min(d, Sd.Segment(p, At(x, -34f), At(x, -20f), 1.4f * s));
                }
                return d;
            }, Hex(0x3A2E28), accent);
            return icon;
        }

        private static Icon Engine()
        {
            var icon = new Icon(Hex(0xFF8A2A));
            icon.Add(p => Sd.Ring(p, V(22f, 52f), 12f, 7f), Hex(0x4A4F55));
            foreach (float x in new[] { 40f, 64f, 88f })
            {
                float cx = x;
                icon.Add(p => Sd.Box(p, V(cx, 84f), V(10f, 11f), 2f), Hex(0xB8BEC4));
            }
            icon.Add(p => Sd.Box(p, V(64f, 52f), V(38f, 24f), 4f), Hex(0xC62E24));
            icon.Add(p => Sd.Box(p, V(64f, 101f), V(34f, 6f), 3f), Hex(0xD8DCE0));
            icon.Add(p => Sd.Box(p, V(64f, 23f), V(30f, 6f), 3f), Hex(0x3A3F45));
            icon.Detail(p => Sd.Union(Sd.Box(p, V(50f, 52f), V(2f, 16f)), Sd.Box(p, V(78f, 52f), V(2f, 16f))), Hex(0x8E1E18));
            return icon;
        }

        private static Icon Tyres()
        {
            var icon = new Icon(Hex(0x3FE6FF));
            var c = V(64f, 64f);
            icon.Add(p =>
            {
                float d = Sd.Ring(p, c, 41f, 26f);
                float angle = Mathf.Atan2(p.y - c.y, p.x - c.x);
                float notch = Mathf.Abs(Mathf.Repeat(angle / (Mathf.PI * 2f) * 18f, 1f) - 0.5f);
                return Sd.Subtract(d, Sd.Intersect((notch - 0.2f) * 18f, 50f - (p - c).magnitude));
            }, Hex(0x26262A));
            icon.Add(p => Sd.Circle(p, c, 27f), Hex(0xC0C4C8));
            icon.Detail(p =>
            {
                float d = Sd.Circle(p, c, 9f);
                for (int i = 0; i < 5; i++)
                {
                    float a = i * Mathf.PI * 2f / 5f + Mathf.PI * 0.5f;
                    d = Mathf.Min(d, Sd.Circle(p, c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 17f, 4f));
                }
                return d;
            }, Hex(0x3A3F45));
            return icon;
        }

        private static readonly Vector2[] ShieldOutline =
        {
            V(22f, 108f), V(106f, 108f), V(106f, 62f), V(99f, 42f), V(86f, 27f), V(64f, 14f), V(42f, 27f), V(29f, 42f), V(22f, 62f)
        };

        private static Icon Armour()
        {
            var icon = new Icon(Hex(0x4A8CFF));
            icon.Add(p => Sd.Polygon(p, ShieldOutline), Hex(0xA8AEB4));
            icon.Detail(p => Sd.Intersect(Sd.Box(p, V(64f, 60f), V(11f, 50f)), Sd.Polygon(p, ShieldOutline) + 6f), Hex(0xC8662A), Hex(0xFF8A2A));
            icon.Detail(p => Sd.Outline(Sd.Polygon(p, ShieldOutline) + 7f, 2f), Hex(0x6E747A));
            icon.Detail(p =>
            {
                float d = float.MaxValue;
                foreach (Vector2 at in new[] { V(32f, 98f), V(96f, 98f), V(32f, 66f), V(96f, 66f), V(64f, 26f) })
                {
                    d = Mathf.Min(d, Sd.Circle(p, at, 3.2f));
                }
                return d;
            }, Hex(0xE6EAEE));
            return icon;
        }

        private static Icon Spikes()
        {
            var icon = new Icon(Hex(0xFF3A3A));
            for (int i = 0; i < 5; i++)
            {
                float x = 28f + i * 18f;
                icon.Add(p => Sd.Polygon(p, new[] { V(x - 8f, 52f), V(x + 8f, 52f), V(x, 96f) }), Hex(0xD8DCE0));
            }
            icon.Add(p => Sd.Box(p, V(64f, 42f), V(46f, 13f), 6f), Hex(0x4A4F55));
            icon.Detail(p => Sd.Box(p, V(64f, 46f), V(40f, 2.5f)), Hex(0xC62E24), Hex(0xFF3A3A));
            return icon;
        }

        private static Icon Piercing()
        {
            var icon = new Icon(Hex(0xFFD24A));
            var c = V(64f, 64f);
            icon.Add(p => Sd.Star(p, V(92f, 92f), 30f, 8, 0.45f, 0f), Hex(0xFFB02A), Hex(0xFF8A2A));
            Vector2 R(Vector2 p)
            {
                return Sd.Rotate(p, c, -45f) - c;
            }

            icon.Add(p => Sd.Union(Sd.Box(R(p), V(0f, -14f), V(12f, 30f), 2f), Sd.Box(R(p), V(0f, -44f), V(13.5f, 3.5f), 1f)), Hex(0xD9A441));
            icon.Add(p => Sd.Intersect(Sd.Ellipse(R(p), V(0f, 16f), V(12f, 30f)), 16f - R(p).y), Hex(0x6E747A), Hex(0xEAF2FF));
            icon.Detail(p => Sd.Box(R(p), V(0f, 10f), V(12.2f, 3f)), Hex(0xC62E24), Hex(0xFF3A3A));
            return icon;
        }

        private static Icon Sabotage()
        {
            var icon = new Icon(Hex(0xFF8A2A));
            var c = V(56f, 52f);
            icon.Add(p => Sd.Box(Sd.Rotate(p, V(82f, 78f), 45f), V(82f, 78f), V(9f, 7f), 2f), Hex(0x6E747A));
            icon.Add(p => Sd.Circle(p, c, 36f), Hex(0x2E2E34), Hex(0xFF8A2A));
            icon.Detail(p => Sd.Circle(p, c + V(-13f, 13f), 8f), Hex(0x6A6A74));
            icon.Add(p => Sd.Union(Sd.Segment(p, V(86f, 84f), V(94f, 96f), 2.4f), Sd.Segment(p, V(94f, 96f), V(102f, 100f), 2.4f)), Hex(0xB08A5E));
            icon.Add(p => Sd.Star(p, V(104f, 104f), 14f, 6, 0.4f), Hex(0xFFD24A), Hex(0xFFE08A));
            return icon;
        }

        private static Icon Trophy()
        {
            var icon = new Icon(Hex(0xFFD24A));
            icon.Add(p => Sd.Union(Sd.Ring(p, V(30f, 86f), 13f, 6f), Sd.Ring(p, V(98f, 86f), 13f, 6f)), Hex(0xC9961E));
            icon.Add(p => Sd.Polygon(p, new[] { V(30f, 108f), V(98f, 108f), V(92f, 80f), V(80f, 62f), V(48f, 62f), V(36f, 80f) }), Hex(0xE8B42A));
            icon.Add(p => Sd.Union(Sd.Box(p, V(64f, 50f), V(6f, 13f)), Sd.Box(p, V(64f, 26f), V(24f, 7f), 3f)), Hex(0xC9961E));
            icon.Add(p => Sd.Box(p, V(64f, 37f), V(14f, 5f), 2f), Hex(0xE8B42A));
            icon.Detail(p => Sd.Star(p, V(64f, 86f), 11f, 5, 0.45f), Hex(0xFFF0B0));
            return icon;
        }

        private static Icon Lock()
        {
            var icon = new Icon(Hex(0x3FE6FF));
            icon.Add(p => Sd.Union(Sd.Intersect(Sd.Ring(p, V(64f, 76f), 20f, 9f), 76f - p.y), Sd.Union(Sd.Box(p, V(44f, 72f), V(4.5f, 8f)), Sd.Box(p, V(84f, 72f), V(4.5f, 8f)))), Hex(0xB8BEC4));
            icon.Add(p => Sd.Box(p, V(64f, 46f), V(33f, 28f), 6f), Hex(0xE0B030));
            icon.Detail(p => Sd.Union(Sd.Circle(p, V(64f, 52f), 7f), Sd.Box(p, V(64f, 41f), V(3f, 9f), 1f)), Hex(0x3A2A0A));
            return icon;
        }

        private static Icon Car()
        {
            var icon = new Icon(Hex(0xFF3A3A));
            foreach (Vector2 wheel in new[] { V(36f, 92f), V(92f, 92f), V(36f, 36f), V(92f, 36f) })
            {
                Vector2 w = wheel;
                icon.Add(p => Sd.Box(p, w, V(7f, 12f), 3f), Hex(0x1A1A1C), Hex(0xEAF2FF));
            }
            icon.Add(p => Sd.Box(p, V(64f, 64f), V(26f, 50f), 14f), Hex(0xC62E24));
            icon.Detail(p => Sd.Box(p, V(64f, 76f), V(19f, 11f), 5f), Hex(0x2C3A48), Hex(0x3FE6FF));
            icon.Detail(p => Sd.Box(p, V(64f, 40f), V(18f, 8f), 4f), Hex(0x2C3A48), Hex(0x3FE6FF));
            icon.Detail(p => Sd.Box(p, V(64f, 58f), V(4f, 40f)), Hex(0xEDEAE2));
            return icon;
        }

        private static Icon Timer()
        {
            var icon = new Icon(Hex(0x3FE6FF));
            icon.Add(p => Sd.Union(Sd.Box(p, V(64f, 106f), V(10f, 6f), 2f), Sd.Box(p, V(64f, 98f), V(4f, 4f))), Hex(0x7A7F86));
            icon.Add(p => Sd.Circle(p, V(64f, 56f), 42f), Hex(0xB8BEC4));
            icon.Add(p => Sd.Circle(p, V(64f, 56f), 33f), Hex(0xEDEAE2), Hex(0xEAF8FF));
            icon.Detail(p => Sd.Union(Sd.Segment(p, V(64f, 56f), V(64f, 80f), 3f), Sd.Segment(p, V(64f, 56f), V(81f, 46f), 3f)), Hex(0xC62E24), Hex(0xFF3FC8));
            icon.Detail(p =>
            {
                float d = float.MaxValue;
                for (int i = 0; i < 12; i++)
                {
                    float a = i * Mathf.PI / 6f;
                    var dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                    d = Mathf.Min(d, Sd.Segment(p, V(64f, 56f) + dir * 26f, V(64f, 56f) + dir * 30f, 1.4f));
                }
                return d;
            }, Hex(0x3A3F45));
            return icon;
        }

        // ------------------------------------------------------------------------------------------------
        // The icons of the leagues' upgrades, fittings, guns, special weapons and statuses (128 units, y up)
        // ------------------------------------------------------------------------------------------------

        /// <summary>The nitro system: two bottles on a manifold, a blue flame out of its nozzle.</summary>
        private static Icon Boost()
        {
            var icon = new Icon(Hex(0x3FE6FF));
            Vector2[] flame = Tongue(V(64f, 90f), 32f, 13f, 3f, 2f);
            icon.Add(p => Sd.Polygon(p, flame), Hex(0x3FA8FF), Hex(0x3FE6FF));
            icon.Detail(p => Sd.Polygon(p, flame) + 5f, Hex(0xD8F4FF), Hex(0xEAF8FF));
            icon.Add(p => Sd.Union(Sd.Segment(p, V(40f, 82f), V(88f, 82f), 5f), Sd.Box(p, V(64f, 86f), V(7f, 8f), 2f)), Hex(0xB8BEC4));
            foreach (float x in new[] { 40f, 88f })
            {
                float bx = x;
                icon.Add(p => Sd.Box(p, V(bx, 78f), V(6f, 5f), 1.5f), Hex(0xB8BEC4));
                icon.Add(p => Sd.Box(p, V(bx, 44f), V(17f, 30f), 15f), Hex(0x2F6BFF));
                icon.Detail(p => Sd.Box(p, V(bx, 46f), V(17.2f, 7f)), Hex(0xEDEAE2), Hex(0xEAF8FF));
                icon.Detail(p => Sd.Box(p, V(bx - 8f, 60f), V(2.5f, 8f), 2.5f), Hex(0x8FB6FF, 0.8f));
            }
            return icon;
        }

        /// <summary>The gun mount: a gun on a pedestal bolted to a plate, a plus for the upgrade.</summary>
        private static Icon GunMount()
        {
            var icon = new Icon(Hex(0xFF8A2A));
            var pivot = V(62f, 58f);
            Vector2 R(Vector2 p)
            {
                return Sd.Rotate(p, pivot, 18f);
            }

            icon.Add(p => Sd.Box(p, V(64f, 20f), V(46f, 8f), 4f), Hex(0x4A4F55));
            icon.Detail(p =>
            {
                float d = float.MaxValue;
                foreach (float x in new[] { 26f, 48f, 80f, 102f })
                {
                    d = Mathf.Min(d, Sd.Circle(p, V(x, 20f), 2.8f));
                }
                return d;
            }, Hex(0xB8BEC4));
            icon.Add(p => Sd.Polygon(p, new[] { V(50f, 27f), V(78f, 27f), V(70f, 52f), V(58f, 52f) }), Hex(0x6E747A));
            icon.Add(p => Sd.Box(R(p), V(38f, 46f), V(9f, 10f), 2f), Hex(0x5B6236));
            icon.Add(p => Sd.Union(Sd.Box(R(p), V(94f, 61f), V(20f, 4.5f), 2f), Sd.Box(R(p), V(115f, 61f), V(4f, 7f), 1f)), Hex(0x2A2D33));
            icon.Add(p => Sd.Box(R(p), V(80f, 61f), V(8f, 8f), 2f), Hex(0x8A9096));
            icon.Add(p => Sd.Box(R(p), V(54f, 60f), V(22f, 11f), 3f), Hex(0x3A3F45));
            icon.Detail(p => Sd.Box(R(p), V(54f, 67f), V(18f, 2f)), Hex(0x8A9096));
            icon.Add(p => Sd.Union(Sd.Box(p, V(24f, 102f), V(4.5f, 14f), 1.5f), Sd.Box(p, V(24f, 102f), V(14f, 4.5f), 1.5f)), Hex(0x8BD450), Hex(0x5CFFA0));
            return icon;
        }

        /// <summary>The roll cage: a frame of tubes over a car's body.</summary>
        private static Icon Cage()
        {
            var icon = new Icon(Hex(0x3FE6FF));
            Vector2[] joints = { V(32f, 52f), V(46f, 98f), V(84f, 98f), V(100f, 52f), V(68f, 52f), V(66f, 98f) };
            icon.Add(p =>
            {
                float d = Sd.Segment(p, joints[0], joints[1], 4.5f);
                d = Mathf.Min(d, Sd.Segment(p, joints[1], joints[2], 4.5f));
                d = Mathf.Min(d, Sd.Segment(p, joints[2], joints[3], 4.5f));
                d = Mathf.Min(d, Sd.Segment(p, joints[5], joints[4], 4f));
                d = Mathf.Min(d, Sd.Segment(p, joints[1], joints[4], 3.5f));
                d = Mathf.Min(d, Sd.Segment(p, joints[5], joints[3], 3.5f));
                return d;
            }, Hex(0xB8BEC4));
            icon.Detail(p =>
            {
                float d = float.MaxValue;
                foreach (Vector2 joint in new[] { joints[1], joints[2], joints[5] })
                {
                    d = Mathf.Min(d, Sd.Circle(p, joint, 6f));
                }
                return d;
            }, Hex(0x6E747A));
            icon.Add(p => Sd.Box(p, V(64f, 42f), V(52f, 12f), 6f), Hex(0xC62E24), Hex(0xFF3A3A));
            icon.Detail(p => Sd.Box(p, V(64f, 46f), V(48f, 2f)), Hex(0xE8664A), Hex(0xFF8A8A));
            foreach (float x in new[] { 34f, 94f })
            {
                float wx = x;
                icon.Add(p => Sd.Circle(p, V(wx, 28f), 12f), Hex(0x1A1A1C), Hex(0xEAF2FF));
                icon.Detail(p => Sd.Circle(p, V(wx, 28f), 5f), Hex(0xB8BEC4));
            }
            return icon;
        }

        /// <summary>Blast shielding: a riveted plate over a burst.</summary>
        private static Icon Shield()
        {
            var icon = new Icon(Hex(0xFF8A2A));
            icon.Add(p => Sd.Star(p, V(64f, 46f), 42f, 9, 0.55f), Hex(0xFF8A1E), Hex(0xFFB02A));
            icon.Detail(p => Sd.Star(p, V(64f, 46f), 24f, 9, 0.55f, 110f), Hex(0xFFE08A), Hex(0xFFF2B0));
            icon.Add(p => Sd.Intersect(Sd.Ring(p, V(64f, 20f), 78f, 18f), Mathf.Abs(p.x - 64f) - 54f), Hex(0xA8AEB4), Hex(0x4A8CFF));
            icon.Detail(p =>
            {
                float d = float.MaxValue;
                for (int i = -2; i <= 2; i++)
                {
                    float a = (90f + i * 17f) * Mathf.Deg2Rad;
                    d = Mathf.Min(d, Sd.Circle(p, V(64f, 20f) + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 78f, 3f));
                }
                return d;
            }, Hex(0xE6EAEE));
            icon.Detail(p => Sd.Intersect(Sd.Ring(p, V(64f, 20f), 71f, 2.2f), Mathf.Abs(p.x - 64f) - 50f), Hex(0x6E747A), Hex(0x4A8CFF));
            return icon;
        }

        /// <summary>The radar jammer: a mast sending waves, struck through.</summary>
        private static Icon Jammer()
        {
            var icon = new Icon(Hex(0xFF3F6A));
            var top = V(64f, 80f);
            icon.Add(p =>
            {
                float d = float.MaxValue;
                foreach (float radius in new[] { 20f, 32f, 44f })
                {
                    d = Mathf.Min(d, Sd.Intersect(Sd.Ring(p, top, radius, 5f), Mathf.Abs(p.y - top.y) - Mathf.Abs(p.x - top.x) * 0.75f));
                }
                return d;
            }, Hex(0x3FA8FF), Hex(0x3FE6FF));
            icon.Add(p => Sd.Union(Sd.Segment(p, V(64f, 22f), top, 4.5f), Sd.Polygon(p, new[] { V(46f, 16f), V(82f, 16f), V(64f, 40f) })), Hex(0x8A9096));
            icon.Add(p => Sd.Circle(p, top, 8f), Hex(0xE8402A), Hex(0xFF5A3A));
            icon.Add(p => Sd.Segment(p, V(24f, 22f), V(104f, 106f), 6f), Hex(0xD8302A), Hex(0xFF3F6A));
            return icon;
        }

        /// <summary>The field welder: a torch spitting sparks.</summary>
        private static Icon Welder()
        {
            var icon = new Icon(Hex(0xFFD24A));
            var tip = V(88f, 40f);
            icon.Add(p => Sd.Star(p, tip, 28f, 8, 0.3f, 20f), Hex(0xFFC93C), Hex(0xFFE08A));
            icon.Detail(p => Sd.Circle(p, tip, 8f), Hex(0xFFFDF0), Hex(0xFFFFFF));
            icon.Detail(p =>
            {
                float d = float.MaxValue;
                foreach (float degrees in new[] { -70f, -30f, 10f, 50f, 200f, 250f })
                {
                    float a = degrees * Mathf.Deg2Rad;
                    var dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                    d = Mathf.Min(d, Sd.Segment(p, tip + dir * 33f, tip + dir * 38f, 2f));
                }
                return d;
            }, Hex(0xFFB02A), Hex(0xFFE08A));
            icon.Add(p => Sd.Union(Sd.Segment(p, V(20f, 112f), V(30f, 98f), 4f), Sd.Segment(p, V(30f, 98f), V(36f, 92f), 4f)), Hex(0x2A2D33));
            icon.Add(p => Sd.Segment(p, V(36f, 92f), V(60f, 68f), 9f), Hex(0xC62E24), Hex(0xFF3A3A));
            icon.Detail(p =>
            {
                float d = float.MaxValue;
                for (int i = 0; i < 4; i++)
                {
                    Vector2 at = Vector2.Lerp(V(40f, 88f), V(56f, 72f), i / 3f);
                    d = Mathf.Min(d, Sd.Segment(p, at + V(-6f, -6f), at + V(6f, 6f), 1.4f));
                }
                return d;
            }, Hex(0x8E1E18));
            icon.Add(p => Sd.Segment(p, V(60f, 68f), V(76f, 52f), 6f), Hex(0xB8BEC4));
            icon.Add(p => Sd.Segment(p, V(76f, 52f), V(84f, 44f), 3f), Hex(0xD9A441));
            return icon;
        }

        /// <summary>The extended magazine: a long box magazine with rounds showing, a plus for the extra half.</summary>
        private static Icon Magazine()
        {
            var icon = new Icon(Hex(0xFFD24A));
            var c = V(54f, 58f);
            Vector2 R(Vector2 p)
            {
                return Sd.Rotate(p, c, -12f);
            }

            icon.Add(p => Sd.Box(R(p), V(54f, 90f), V(9f, 8f), 1f), Hex(0xD9A441));
            icon.Add(p => Sd.Intersect(Sd.Ellipse(R(p), V(54f, 98f), V(8f, 16f)), 98f - R(p).y), Hex(0xC8673A));
            icon.Add(p => Sd.Box(R(p), V(54f, 54f), V(20f, 40f), 4f), Hex(0x3A3F45));
            icon.Add(p => Sd.Box(R(p), V(54f, 16f), V(23f, 5f), 2f), Hex(0x2A2D33));
            icon.Detail(p =>
            {
                float d = float.MaxValue;
                for (int i = 0; i < 4; i++)
                {
                    d = Mathf.Min(d, Sd.Box(R(p), V(54f, 30f + i * 16f), V(9f, 4f), 1.5f));
                }
                return d;
            }, Hex(0xD9A441));
            icon.Detail(p => Sd.Box(R(p), V(40f, 54f), V(2f, 34f)), Hex(0x6E747A));
            icon.Add(p => Sd.Union(Sd.Box(p, V(100f, 86f), V(5f, 16f), 1.5f), Sd.Box(p, V(100f, 86f), V(16f, 5f), 1.5f)), Hex(0x8BD450), Hex(0x5CFFA0));
            return icon;
        }

        /// <summary>The machine gun from the side: stock, receiver, the cooling jacket, the belt of rounds.</summary>
        private static Icon MachineGun()
        {
            var icon = new Icon(Hex(0xFFD24A));
            icon.Add(p =>
            {
                float d = float.MaxValue;
                for (int i = 0; i < 6; i++)
                {
                    float t = i / 5f;
                    Vector2 at = V(54f - t * 14f, 46f - t * 30f + t * t * 8f);
                    d = Mathf.Min(d, Sd.Box(Sd.Rotate(p, at, 30f * t), at, V(6f, 3.4f), 1f));
                }
                return d;
            }, Hex(0xD9A441));
            icon.Add(p => Sd.Polygon(p, new[] { V(8f, 52f), V(30f, 60f), V(30f, 70f), V(10f, 66f) }), Hex(0x8A6236), Hex(0xFFB02A));
            icon.Add(p => Sd.Polygon(p, new[] { V(36f, 56f), V(46f, 56f), V(42f, 36f), V(32f, 38f) }), Hex(0x2A2D33));
            icon.Add(p => Sd.Box(p, V(84f, 64f), V(22f, 7f), 3f), Hex(0x6E747A));
            icon.Detail(p =>
            {
                float d = float.MaxValue;
                foreach (float x in new[] { 70f, 80f, 90f, 100f })
                {
                    d = Mathf.Min(d, Sd.Box(p, V(x, 64f), V(2.5f, 3f), 1f));
                }
                return d;
            }, Hex(0x2A2D33));
            icon.Add(p => Sd.Union(Sd.Box(p, V(112f, 64f), V(7f, 2.8f)), Sd.Box(p, V(119f, 64f), V(2.5f, 5f), 1f)), Hex(0x2A2D33));
            icon.Add(p => Sd.Box(p, V(46f, 64f), V(18f, 9f), 2f), Hex(0x3A3F45));
            icon.Add(p => Sd.Box(p, V(54f, 50f), V(8f, 6f), 1.5f), Hex(0x5B6236));
            icon.Detail(p => Sd.Box(p, V(46f, 71f), V(15f, 2f)), Hex(0x8A9096));
            return icon;
        }

        /// <summary>The minigun: a bundle of barrels on a motor, clamped twice, its belt feeding in.</summary>
        private static Icon Minigun()
        {
            var icon = new Icon(Hex(0xFF8A2A));
            icon.Add(p =>
            {
                float d = float.MaxValue;
                for (int i = 0; i < 5; i++)
                {
                    float t = i / 4f;
                    Vector2 at = V(28f - t * 6f, 44f - t * 26f);
                    d = Mathf.Min(d, Sd.Box(Sd.Rotate(p, at, 70f), at, V(6f, 3.4f), 1f));
                }
                return d;
            }, Hex(0xD9A441));
            icon.Add(p => Sd.Union(Sd.Segment(p, V(22f, 90f), V(46f, 90f), 3.5f), Sd.Union(Sd.Segment(p, V(24f, 80f), V(24f, 90f), 3f), Sd.Segment(p, V(44f, 80f), V(44f, 90f), 3f))),
                Hex(0x2A2D33));
            icon.Add(p => Sd.Box(p, V(34f, 64f), V(20f, 17f), 5f), Hex(0x3A3F45));
            icon.Add(p =>
            {
                float d = float.MaxValue;
                foreach (float y in new[] { 54f, 64f, 74f })
                {
                    d = Mathf.Min(d, Sd.Box(p, V(84f, y), V(32f, 3.8f), 1.5f));
                }
                return d;
            }, Hex(0x8A9096));
            icon.Add(p => Sd.Union(Sd.Box(p, V(66f, 64f), V(4f, 15f), 2f), Sd.Box(p, V(108f, 64f), V(4f, 15f), 2f)), Hex(0xB8BEC4));
            icon.Detail(p =>
            {
                float d = float.MaxValue;
                foreach (float y in new[] { 54f, 64f, 74f })
                {
                    d = Mathf.Min(d, Sd.Circle(p, V(116f, y), 2.2f));
                }
                return d;
            }, Hex(0x1A1A1C));
            icon.Detail(p => Sd.Box(p, V(34f, 70f), V(16f, 2f)), Hex(0x6E747A));
            return icon;
        }

        /// <summary>The flamethrower: a tank and a lance, fire roaring out of its nozzle.</summary>
        private static Icon Flamethrower()
        {
            var icon = new Icon(Hex(0xFF8A2A));
            var root = V(74f, 58f);
            var tongues = new[] { Tongue(root, 50f, 17f, 0f, 3f), Tongue(root + V(0f, 2f), 38f, 12f, 12f, 3f), Tongue(root + V(0f, -2f), 36f, 12f, -12f, -3f) };
            Sdf flames = p =>
            {
                Vector2 q = Sd.Rotate(p, root, -90f);
                float d = float.MaxValue;
                foreach (Vector2[] outline in tongues)
                {
                    d = Mathf.Min(d, Sd.Polygon(q, outline));
                }
                return d;
            };
            icon.Add(flames, Hex(0xE8501A), Hex(0xFF8A2A));
            icon.Detail(p => flames(p) + 7f, Hex(0xFFA02A), Hex(0xFFB02A));
            icon.Detail(p => flames(p) + 14f, Hex(0xFFE04A), Hex(0xFFF2B0));
            icon.Add(p => Sd.Box(p, V(22f, 54f), V(11f, 26f), 10f), Hex(0xC62E24), Hex(0xFF3A3A));
            icon.Detail(p => Sd.Box(p, V(22f, 58f), V(11.2f, 3f)), Hex(0x1C1B1A));
            icon.Add(p => Sd.Box(p, V(22f, 84f), V(5f, 4f), 1f), Hex(0xB8BEC4));
            icon.Add(p => Sd.Segment(p, V(32f, 56f), V(72f, 58f), 5f), Hex(0x8A9096));
            icon.Add(p => Sd.Polygon(p, new[] { V(42f, 52f), V(52f, 52f), V(48f, 34f), V(38f, 36f) }), Hex(0x2A2D33));
            icon.Add(p => Sd.Box(p, V(74f, 58f), V(4f, 7f), 1f), Hex(0x3A3F45));
            return icon;
        }

        /// <summary>The plasma cannon's bolt: a glowing ball in two rings, its trail behind it.</summary>
        private static Icon Plasma()
        {
            var icon = new Icon(Hex(0xC45AFF));
            var c = V(74f, 72f);
            icon.Add(p => Sd.Polygon(p, new[] { V(12f, 24f), V(88f, 48f), V(58f, 94f) }), Hex(0x8A4ADF), Hex(0xC45AFF));
            icon.Detail(p => Sd.Polygon(p, new[] { V(22f, 32f), V(80f, 56f), V(64f, 82f) }), Hex(0xB08AFF), Hex(0xE0C8FF));
            icon.Add(p => Sd.Circle(p, c, 28f), Hex(0x6E3AD8), Hex(0xC45AFF));
            icon.Detail(p => Sd.Circle(p, c, 18f), Hex(0xB08AFF), Hex(0xE0C8FF));
            icon.Detail(p => Sd.Circle(p, c + V(-5f, 5f), 8f), Hex(0xF4EEFF), Hex(0xFFFFFF));
            icon.Detail(p => Sd.Outline(Sd.Ellipse(Sd.Rotate(p, c, 30f), c, V(42f, 13f)), 3f), Hex(0x3FE6FF));
            icon.Detail(p => Sd.Outline(Sd.Ellipse(Sd.Rotate(p, c, -40f), c, V(42f, 13f)), 3f), Hex(0x3FE6FF));
            return icon;
        }

        /// <summary>The oil slick: a black drop over a glossy puddle with a sheen.</summary>
        private static Icon Oil()
        {
            var icon = new Icon(Hex(0xB45AFF));
            icon.Add(p => Sd.Union(Sd.Ellipse(p, V(66f, 26f), V(50f, 14f)), Sd.Ellipse(p, V(34f, 32f), V(18f, 9f))), Hex(0x2A2630));
            icon.Detail(p => Sd.Outline(Sd.Ellipse(p, V(70f, 27f), V(34f, 7f)), 2.4f), Hex(0x6A5A8A), Hex(0x3FE6FF));
            icon.Add(p => Sd.SmoothUnion(Sd.Circle(p, V(64f, 62f), 26f), Sd.Polygon(p, new[] { V(64f, 114f), V(44f, 72f), V(84f, 72f) }), 6f),
                Hex(0x1E1C24), Hex(0xB45AFF));
            icon.Detail(p => Sd.Ellipse(Sd.Rotate(p, V(53f, 70f), 30f), V(53f, 70f), V(5f, 11f)), Hex(0x8A8496), Hex(0xE0C8FF));
            return icon;
        }

        /// <summary>The EMP blast: a lightning bolt in a ring that throws off sparks.</summary>
        private static Icon Emp()
        {
            var icon = new Icon(Hex(0x3FE6FF));
            var c = V(64f, 64f);
            icon.Add(p => Sd.Ring(p, c, 46f, 6f), Hex(0x2F6BFF), Hex(0x3FE6FF));
            icon.Detail(p =>
            {
                float d = float.MaxValue;
                for (int i = 0; i < 4; i++)
                {
                    float a = (45f + i * 90f) * Mathf.Deg2Rad;
                    var dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                    d = Mathf.Min(d, Sd.Segment(p, c + dir * 53f, c + dir * 60f, 2.4f));
                }
                return d;
            }, Hex(0x8FD8FF), Hex(0xEAF8FF));
            icon.Add(p => Sd.Circle(p, c, 36f), Hex(0x1C2A4A), Hex(0x2F6BFF));
            icon.Add(p => Sd.Polygon(p, new[] { V(72f, 102f), V(44f, 60f), V(62f, 60f), V(54f, 26f), V(86f, 70f), V(68f, 70f) }), Hex(0xFFE04A), Hex(0xEAF8FF));
            return icon;
        }

        /// <summary>The mortar: a tube on its base plate and bipod, the shell at the top of its arc.</summary>
        private static Icon Mortar()
        {
            var icon = new Icon(Hex(0xFF8A2A));
            Vector2 mouth = V(56f, 78f);
            Vector2 shell = V(94f, 96f);
            icon.Detail(p =>
            {
                float d = float.MaxValue;
                for (int i = 1; i <= 4; i++)
                {
                    float t = i / 5f;
                    Vector2 at = (1f - t) * (1f - t) * mouth + 2f * (1f - t) * t * V(70f, 112f) + t * t * (shell - V(18f, 0f));
                    d = Mathf.Min(d, Sd.Circle(p, at, 3.4f));
                }
                return d;
            }, Hex(0xEDEAE2));
            icon.Add(p => Sd.Box(p, V(36f, 18f), V(24f, 5f), 2f), Hex(0x4A4F55));
            icon.Add(p => Sd.Union(Sd.Segment(p, V(48f, 52f), V(68f, 16f), 3f), Sd.Segment(p, V(62f, 16f), V(76f, 16f), 3f)), Hex(0x6E747A));
            icon.Add(p => Sd.Segment(p, V(32f, 24f), mouth + V(-2f, -4f), 9f), Hex(0x5B6236));
            icon.Add(p => Sd.Segment(p, mouth + V(-2f, -4f), mouth, 11f), Hex(0x3D4224));
            icon.Add(p =>
            {
                Vector2 q = Sd.Rotate(p, shell, -90f);
                return Sd.Union(Sd.Ellipse(q, shell, V(10f, 18f)), Sd.Box(q, shell - V(0f, 18f), V(6.5f, 7f), 1f));
            }, Hex(0x6E747A));
            icon.Detail(p => Sd.Box(Sd.Rotate(p, shell, -90f), shell + V(0f, 3f), V(10.2f, 2.4f)), Hex(0xC62E24), Hex(0xFF3A3A));
            return icon;
        }

        /// <summary>The workshop: a gear with a spanner across it.</summary>
        private static Icon Workshop()
        {
            var icon = new Icon(Hex(0xFFD24A));
            var c = V(54f, 56f);
            icon.Add(p =>
            {
                float angle = Mathf.Atan2(p.y - c.y, p.x - c.x);
                float tooth = Mathf.Abs(Mathf.Repeat(angle / (Mathf.PI * 2f) * 8f, 1f) - 0.5f);
                float teeth = Sd.Intersect(Sd.Circle(p, c, 42f), (tooth - 0.22f) * 28f);
                return Sd.Subtract(Sd.Union(Sd.Circle(p, c, 32f), teeth), Sd.Circle(p, c, 12f));
            }, Hex(0x8A9096));
            icon.Detail(p => Sd.Ring(p, c, 22f, 2f), Hex(0x6E747A));
            var head = V(98f, 98f);
            icon.Add(p =>
            {
                float d = Sd.Union(Sd.Segment(p, V(58f, 58f), head, 7f), Sd.Circle(p, head, 17f));
                return Sd.Subtract(d, Sd.Box(Sd.Rotate(p, head, 45f), head + V(0f, 11f), V(7f, 13f), 1f));
            }, Hex(0xD8DCE0));
            icon.Detail(p => Sd.Segment(p, V(66f, 70f), V(86f, 90f), 2f), Hex(0xF2F4F6));
            return icon;
        }

        /// <summary>A league: a pennant hanging from its rod, a star over two chevrons.</summary>
        private static Icon League()
        {
            var icon = new Icon(Hex(0xFF3FC8));
            Vector2[] banner = { V(36f, 104f), V(92f, 104f), V(92f, 20f), V(64f, 38f), V(36f, 20f) };
            icon.Add(p => Sd.Polygon(p, banner), Hex(0xC62E24), Hex(0xFF3FC8));
            icon.Detail(p => Sd.Outline(Sd.Polygon(p, banner) + 5f, 2.4f), Hex(0xE8B42A), Hex(0xFFD24A));
            icon.Detail(p => Sd.Star(p, V(64f, 80f), 14f, 5, 0.45f), Hex(0xFFD24A), Hex(0xFFE08A));
            icon.Detail(p =>
            {
                float d = float.MaxValue;
                foreach (float y in new[] { 62f, 50f })
                {
                    d = Mathf.Min(d, Sd.Union(Sd.Segment(p, V(48f, y), V(64f, y - 9f), 2.8f), Sd.Segment(p, V(64f, y - 9f), V(80f, y), 2.8f)));
                }
                return d;
            }, Hex(0xFFD24A), Hex(0xFFE08A));
            icon.Add(p => Sd.Union(Sd.Segment(p, V(26f, 108f), V(102f, 108f), 4f), Sd.Union(Sd.Circle(p, V(24f, 108f), 6f), Sd.Circle(p, V(104f, 108f), 6f))),
                Hex(0xC9961E));
            return icon;
        }

        /// <summary>Burning: the car is on fire.</summary>
        private static Icon Burning()
        {
            var icon = new Icon(Hex(0xFF8A2A));
            var tongues = new[] { Tongue(V(64f, 18f), 92f, 30f, 4f, 5f), Tongue(V(38f, 22f), 60f, 20f, -12f, -4f), Tongue(V(90f, 22f), 64f, 20f, 12f, 4f) };
            Sdf flames = p =>
            {
                float d = float.MaxValue;
                foreach (Vector2[] outline in tongues)
                {
                    d = Mathf.Min(d, Sd.Polygon(p, outline));
                }
                return d;
            };
            icon.Add(flames, Hex(0xE8401A), Hex(0xFF8A2A));
            icon.Detail(p => flames(p) + 9f, Hex(0xFF9A2A), Hex(0xFFB02A));
            icon.Detail(p => flames(p) + 18f, Hex(0xFFE04A), Hex(0xFFF2B0));
            return icon;
        }

        /// <summary>Jammed: the sights fizzling out with an electric spark across them.</summary>
        private static Icon Jammed()
        {
            var icon = new Icon(Hex(0x3FE6FF));
            var c = V(64f, 64f);
            icon.Add(p =>
            {
                float d = Sd.Ring(p, c, 34f, 7f);
                d = Mathf.Min(d, Sd.Box(p, c + V(0f, 42f), V(3.5f, 12f)));
                d = Mathf.Min(d, Sd.Box(p, c - V(0f, 42f), V(3.5f, 12f)));
                d = Mathf.Min(d, Sd.Box(p, c + V(42f, 0f), V(12f, 3.5f)));
                return Mathf.Min(d, Sd.Box(p, c - V(42f, 0f), V(12f, 3.5f)));
            }, Hex(0x8A9096), Hex(0x8FA3C4));
            Vector2[] zig = { V(16f, 78f), V(42f, 56f), V(56f, 76f), V(74f, 48f), V(86f, 68f), V(112f, 46f) };
            Sdf spark = p =>
            {
                float d = float.MaxValue;
                for (int i = 0; i + 1 < zig.Length; i++)
                {
                    d = Mathf.Min(d, Sd.Segment(p, zig[i], zig[i + 1], 4.5f));
                }
                return d;
            };
            icon.Add(spark, Hex(0x3FA8FF), Hex(0x3FE6FF));
            icon.Detail(p => spark(p) + 3f, Hex(0xEAF8FF), Hex(0xFFFFFF));
            return icon;
        }

        /// <summary>Slicked: a car spinning on a puddle of oil.</summary>
        private static Icon Slicked()
        {
            var icon = new Icon(Hex(0xB45AFF));
            var car = V(64f, 62f);
            icon.Add(p => Sd.Union(Sd.Ellipse(p, V(64f, 50f), V(54f, 30f)), Sd.Circle(p, V(104f, 30f), 10f)), Hex(0x2A2630));
            icon.Detail(p => Sd.Outline(Sd.Ellipse(p, V(60f, 44f), V(40f, 18f)), 2.2f), Hex(0x6A5A8A), Hex(0x3FE6FF));
            Sdf arcs = p =>
            {
                Vector2 d = p - car;
                float angle = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
                float off = Mathf.Min(Mathf.Abs(Mathf.DeltaAngle(angle, 135f)), Mathf.Abs(Mathf.DeltaAngle(angle, 315f)));
                return Sd.Intersect(Sd.Ring(p, car, 46f, 5f), (off - 38f) * 0.8f);
            };
            icon.Add(p =>
            {
                float d = arcs(p);
                foreach (float degrees in new[] { 173f, 353f })
                {
                    float a = degrees * Mathf.Deg2Rad;
                    var radial = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                    var tangent = new Vector2(-radial.y, radial.x);
                    Vector2 at = car + radial * 46f;
                    d = Mathf.Min(d, Sd.Polygon(p, new[] { at + tangent * 10f, at + radial * 8f, at - radial * 8f }));
                }
                return d;
            }, Hex(0xEDEAE2), Hex(0xEAF2FF));
            Vector2 R(Vector2 p)
            {
                return Sd.Rotate(p, car, 35f);
            }

            foreach (Vector2 wheel in new[] { V(48f, 84f), V(80f, 84f), V(48f, 40f), V(80f, 40f) })
            {
                Vector2 w = wheel;
                icon.Add(p => Sd.Box(R(p), w, V(5f, 9f), 2.5f), Hex(0x1A1A1C), Hex(0xEAF2FF));
            }
            icon.Add(p => Sd.Box(R(p), car, V(19f, 36f), 10f), Hex(0xC62E24), Hex(0xFF3A3A));
            icon.Detail(p => Sd.Box(R(p), car + V(0f, 9f), V(13f, 8f), 4f), Hex(0x2C3A48), Hex(0x3FE6FF));
            icon.Detail(p => Sd.Box(R(p), car - V(0f, 16f), V(12f, 5f), 3f), Hex(0x2C3A48), Hex(0x3FE6FF));
            return icon;
        }

        /// <summary>
        /// The outline of a tongue of fire rising from <paramref name="root"/>: <paramref name="height"/> tall,
        /// <paramref name="radius"/> wide at its belly, its tip moved sideways by <paramref name="lean"/> and its middle
        /// by <paramref name="wave"/>.
        /// </summary>
        private static Vector2[] Tongue(Vector2 root, float height, float radius, float lean, float wave)
        {
            const int steps = 14;
            var left = new List<Vector2>();
            var right = new List<Vector2>();
            for (int i = 0; i <= steps; i++)
            {
                float t = i / (float)steps;
                float width = radius * Mathf.Sqrt(Mathf.Max(0f, Mathf.Sin(Mathf.PI * (t * 0.85f + 0.15f)))) * Mathf.Pow(1f - t, 0.7f);
                float x = root.x + lean * t * t + Mathf.Sin(t * Mathf.PI * 1.6f) * wave * t;
                float y = root.y + height * t;
                left.Add(new Vector2(x - width, y));
                right.Add(new Vector2(x + width, y));
            }
            // The round bottom.
            var outline = new List<Vector2>();
            for (int i = 0; i <= 8; i++)
            {
                float a = Mathf.PI + i * Mathf.PI / 8f;
                outline.Add(new Vector2(root.x + Mathf.Cos(a) * radius * 0.67f, root.y + Mathf.Sin(a) * radius * 0.5f));
            }
            for (int i = 1; i <= steps; i++)
            {
                outline.Add(right[i]);
            }
            for (int i = steps - 1; i >= 1; i--)
            {
                outline.Add(left[i]);
            }
            return outline.ToArray();
        }

        // ------------------------------------------------------------------------------------------------
        // The launcher's emblem
        // ------------------------------------------------------------------------------------------------

        /// <summary>
        /// The game's emblem for the launcher's card: a skull in a racing tyre on a rusted badge, flames roaring up
        /// behind it. Drawn on 256 units at <paramref name="size"/> pixels; <paramref name="spec"/>'s look tints the badge
        /// and the flames (the night one burns in neon).
        /// </summary>
        public static Texture2D GameIcon(int size, DeathRaceThemeSpec spec)
        {
            var r = new Raster(size, size, Color.clear);
            float k = size / 256f;
            var bounds = new Rect(0f, 0f, size, size);
            Vector2 S(float x, float y)
            {
                return new Vector2(x, y) * k;
            }

            Vector2 center = S(128f, 104f);
            bool night = spec.Night;
            Color badge = night ? spec.PlateDark : Hex(0x2A1C14);
            Color rim = night ? spec.Accent : Hex(0xB4520E);
            Color flameOuter = night ? spec.Second : Hex(0xD8361A);
            Color flameMiddle = night ? Hex(0xFF7AE0) : Hex(0xFF8A1E);
            Color flameInner = night ? Hex(0xFFD6F6) : Hex(0xFFD84A);

            // Flames behind everything: tongues of fire licking up and out from behind the wheel.
            var tongues = new[]
            {
                (x: -60f, height: 104f, lean: -30f, radius: 32f), (x: -30f, height: 124f, lean: -12f, radius: 36f), (x: 2f, height: 134f, lean: 6f, radius: 40f),
                (x: 32f, height: 122f, lean: 16f, radius: 36f), (x: 62f, height: 100f, lean: 30f, radius: 32f), (x: -86f, height: 70f, lean: -34f, radius: 26f),
                (x: 88f, height: 66f, lean: 34f, radius: 26f)
            };
            // Each tongue is a teardrop polygon: round at its root, tapering to a point, leaning out and waving a little.
            var outlines = new List<Vector2[]>();
            foreach (var t in tongues)
            {
                outlines.Add(Tongue(center + S(t.x, 14f), t.height * k, t.radius * k, t.lean * k, Mathf.Sign(t.lean + 0.01f) * 6f * k));
            }
            Sdf flames = p =>
            {
                float d = float.MaxValue;
                foreach (Vector2[] outline in outlines)
                {
                    d = Mathf.Min(d, Sd.Polygon(p, outline));
                }
                return d;
            };
            r.Shadow(flames, new Color(flameOuter.r, flameOuter.g, flameOuter.b, night ? 0.65f : 0.45f), 12f * k, Vector2.zero, bounds);
            r.Fill(flames, flameOuter, bounds);
            r.Fill(p => flames(p) + 9f * k, flameMiddle, bounds);
            r.Fill(p => flames(p) + 20f * k, p => Color.Lerp(flameInner, Color.white, Mathf.Clamp01((p.y - center.y - 60f * k) / (90f * k)) * 0.5f), bounds);

            // The badge.
            Sdf disc = p => Sd.Circle(p, center, 86f * k);
            r.Shadow(disc, new Color(0f, 0f, 0f, 0.6f), 8f * k, S(0f, -6f), bounds);
            r.Fill(disc, p => Color.Lerp(DeathRaceArtInterface.Shade(badge, 0.7f), DeathRaceArtInterface.Lighten(badge, 0.12f), Mathf.Clamp01((p.y - center.y) / (172f * k) + 0.5f)), bounds);
            if (night)
            {
                r.Shadow(p => Sd.Ring(p, center, 80f * k, 8f * k), DeathRaceArtInterface.WithAlpha(rim, 0.6f), 10f * k, Vector2.zero, bounds);
            }
            r.Fill(p => Sd.Ring(p, center, 80f * k, 9f * k), rim, bounds);
            r.Fill(p => Sd.Ring(p, center, 80f * k, 3f * k), DeathRaceArtInterface.Lighten(rim, 0.4f), bounds);
            for (int i = 0; i < 8; i++)
            {
                float a = i * Mathf.PI / 4f + Mathf.PI / 8f;
                Vector2 at = center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 80f * k;
                r.Fill(p => Sd.Circle(p, at, 3.6f * k), night ? DeathRaceArtInterface.Lighten(rim, 0.7f) : Hex(0xE6D8C0), Sd.Around(at, 6f * k));
            }

            // The tyre.
            Sdf tyre = p =>
            {
                float d = Sd.Ring(p, center, 57f * k, 24f * k);
                float angle = Mathf.Atan2(p.y - center.y, p.x - center.x);
                float notch = Mathf.Abs(Mathf.Repeat(angle / (Mathf.PI * 2f) * 22f, 1f) - 0.5f);
                return Sd.Subtract(d, Sd.Intersect((notch - 0.18f) * 16f * k, 63f * k - (p - center).magnitude));
            };
            r.Fill(p => tyre(p) - 3f * k, Hex(0x0A0808), bounds);
            r.Fill(tyre, p => Color.Lerp(Hex(0x1A1A1C), Hex(0x3A3A3E), Mathf.Clamp01((p.y - center.y) / (130f * k) + 0.5f)), bounds);
            if (night)
            {
                r.Fill(p => Sd.Ring(p, center, 57f * k + 12f * k, 1.6f * k), DeathRaceArtInterface.WithAlpha(spec.Accent, 0.8f), bounds);
            }
            r.Fill(p => Sd.Circle(p, center, 45f * k), night ? Hex(0x10162A) : Hex(0x5A524A), bounds);

            // The skull in the middle of the wheel.
            Icon skull = Skull(new Vector2(128f, 100f) / 2f, 0.56f, spec.Accent);
            var scaled = new Raster(size, size, Color.clear);
            Render(scaled, skull, night, k * 2f, spec);
            Color[] pixels = scaled.Pixels;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    r.Blend(x, y, pixels[y * size + x]);
                }
            }
            return r.ToTexture();
        }
    }
}
