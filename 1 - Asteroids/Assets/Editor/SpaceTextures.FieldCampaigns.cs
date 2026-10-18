using UnityEngine;

namespace Portfolio.Asteroids.EditorTools
{
    /// <summary>
    /// The textures of the field campaigns (SpaceTextures.FieldCampaigns.cs): the surfaces of their stars and worlds (a
    /// dying red giant, a white-hot corona, an iron moon, a shattered world with glowing fissures, a black machine world
    /// laced with gold), the scratches that make iron shine, and the sprites of their hazards: the front of a solar
    /// flare, the glow of its warning, a bolt of lightning, an ion cloud, and the reticle of a marked enemy.
    /// </summary>
    internal static partial class SpaceTextures
    {
        // ------------------------------------------------------------------ stars and worlds

        /// <summary>
        /// A star's surface (equirectangular): a granulation of hot cells with darker lanes, a few dark spots and bright
        /// faculae, from <paramref name="dark"/> through <paramref name="mid"/> to <paramref name="hot"/>. Used as both its
        /// colour and its emission, so it burns.
        /// </summary>
        public static Texture2D StarSurface(Color dark, Color mid, Color hot, int seed, float spots)
        {
            const int width = 1024;
            const int height = 512;
            return Make(width, height, (px, py) =>
            {
                float x = px / (float)width;
                float y = py / (float)height;
                // A fine granulation (small cells with soft lanes) over large slow swirls of heat.
                float cells = Smooth(0f, 0.3f, CellEdge(x, y * 0.5f, 150, seed));
                float large = Fbm(x, y * 0.5f, 5, 5, seed + 1);
                float fine = Fbm(x, y * 0.5f, 40, 2, seed + 2);
                float heat = Mathf.Clamp01(0.42f + cells * 0.14f + (large - 0.5f) * 0.9f + (fine - 0.5f) * 0.2f);
                float spot = Smooth(0.72f, 0.8f, Fbm(x, y * 0.5f, 9, 3, seed + 3)) * spots;
                float facula = Smooth(0.66f, 0.74f, Fbm(x, y * 0.5f, 14, 3, seed + 4)) * (1f - spot);
                heat = Mathf.Clamp01(heat * (1f - spot * 0.85f) + facula * 0.25f);
                Color color = heat < 0.5f ? Color.Lerp(dark, mid, heat * 2f) : Color.Lerp(mid, hot, (heat - 0.5f) * 2f);
                color.a = 1f;
                return color;
            });
        }


        /// <summary>An airless iron moon: grey plains with a metallic sheen, craters with bright rims, dark maria.</summary>
        public static Texture2D IronMoon(int seed)
        {
            const int width = 1024;
            const int height = 512;
            var craters = new Vector3[60];
            var random = new System.Random(seed);
            for (int i = 0; i < craters.Length; i++)
            {
                craters[i] = new Vector3((float)random.NextDouble(), 0.12f + (float)random.NextDouble() * 0.76f, 0.008f + (float)random.NextDouble() * (i < 6 ? 0.07f : 0.03f));
            }
            return Make(width, height, (px, py) =>
            {
                float x = px / (float)width;
                float y = py / (float)height;
                float n = Fbm(x, y * 0.5f, 6, 5, seed + 1);
                float detail = Fbm(x, y * 0.5f, 48, 2, seed + 2);
                Color plains = Color.Lerp(new Color(0.34f, 0.36f, 0.4f), new Color(0.62f, 0.64f, 0.68f), Smooth(0.35f, 0.7f, n));
                plains = Color.Lerp(plains, new Color(0.2f, 0.22f, 0.26f), Smooth(0.55f, 0.7f, Fbm(x, y * 0.5f, 3, 3, seed + 3)) * 0.6f);
                plains *= 0.9f + detail * 0.2f;
                foreach (Vector3 crater in craters)
                {
                    float dx = Mathf.DeltaAngle(x * 360f, crater.x * 360f) / 360f;
                    float dy = (y - crater.y) * 0.5f;
                    float d = Mathf.Sqrt(dx * dx + dy * dy) / crater.z;
                    if (d > 1.3f)
                    {
                        continue;
                    }
                    float bowl = 1f - Smooth(0.6f, 1f, d);
                    float rim = Mathf.Clamp01(1f - Mathf.Abs(d - 1f) * 6f);
                    plains = Color.Lerp(plains, plains * 0.62f, bowl * 0.8f);
                    plains = Color.Lerp(plains, new Color(0.85f, 0.87f, 0.9f), rim * 0.55f);
                }
                plains.a = 1f;
                return plains;
            });
        }


        /// <summary>A world cracked open: dark ash crust in plates, lighter dust, and the fissures between the plates.</summary>
        public static Texture2D ShatteredWorld(int seed, bool glow)
        {
            const int width = 1024;
            const int height = 512;
            return Make(width, height, (px, py) =>
            {
                float x = px / (float)width;
                float y = py / (float)height;
                float edge = CellEdge(x, y * 0.5f, 9, seed);
                float wobble = Fbm(x, y * 0.5f, 16, 3, seed + 1);
                float crack = 1f - Smooth(0.008f, 0.05f + wobble * 0.04f, edge);
                float rift = 1f - Smooth(0.02f, 0.06f, Mathf.Abs(y - 0.5f - (Fbm(x, 0.3f, 5, 3, seed + 2) - 0.5f) * 0.25f));
                float fissure = Mathf.Clamp01(crack + rift);
                if (glow)
                {
                    Color hot = Color.Lerp(new Color(1f, 0.3f, 0.05f), new Color(1f, 0.85f, 0.45f), Smooth(0.6f, 1f, fissure));
                    return new Color(hot.r * fissure, hot.g * fissure, hot.b * fissure, 1f);
                }
                float n = Fbm(x, y * 0.5f, 8, 4, seed + 3);
                Color crust = Color.Lerp(new Color(0.16f, 0.13f, 0.12f), new Color(0.42f, 0.36f, 0.3f), Smooth(0.35f, 0.75f, n));
                crust = Color.Lerp(crust, new Color(0.08f, 0.05f, 0.05f), fissure);
                crust.a = 1f;
                return crust;
            });
        }


        /// <summary>A black machine world: dark plates, and (as the glow) the golden traces that run between them.</summary>
        public static Texture2D MachineWorld(int seed, bool glow)
        {
            const int width = 1024;
            const int height = 512;
            const int cells = 64;
            return Make(width, height, (px, py) =>
            {
                float x = px / (float)width * cells;
                float y = py / (float)height * cells * 0.5f;
                int cx = Mathf.FloorToInt(x);
                int cy = Mathf.FloorToInt(y);
                float fx = x - cx;
                float fy = y - cy;
                // Every cell holds a trace along one of its axes, or a node, or nothing: circuitry.
                float roll = Hash(Mod(cx, cells), cy, seed);
                float line = 0f;
                if (roll < 0.35f)
                {
                    line = Mathf.Clamp01(1f - Mathf.Abs(fy - 0.5f) * 14f);
                }
                else if (roll < 0.6f)
                {
                    line = Mathf.Clamp01(1f - Mathf.Abs(fx - 0.5f) * 14f);
                }
                else if (roll < 0.66f)
                {
                    float dx = fx - 0.5f;
                    float dy = fy - 0.5f;
                    line = Mathf.Clamp01(1f - Mathf.Abs(Mathf.Sqrt(dx * dx + dy * dy) - 0.22f) * 18f);
                }
                float region = Smooth(0.45f, 0.6f, Fbm(px / (float)width, py / (float)height * 0.5f, 5, 3, seed + 1));
                line *= region;
                float latitude = Mathf.Abs(py / (float)height - 0.5f);
                if (glow)
                {
                    float equator = Mathf.Clamp01(1f - latitude * 30f) * 0.8f;
                    float v = Mathf.Clamp01(line + equator);
                    return new Color(v, v * 0.72f, v * 0.25f, 1f);
                }
                float plate = 0.08f + Hash(Mod(cx / 4, cells), cy / 4, seed + 2) * 0.06f;
                Color color = new Color(plate, plate * 0.95f, plate * 1.1f);
                color = Color.Lerp(color, new Color(0.55f, 0.42f, 0.18f), line * 0.6f);
                color.a = 1f;
                return color;
            });
        }


        // ------------------------------------------------------------------ rocks

        /// <summary>Fine bright scratches and pits on dark (the emission of iron rocks, which makes them glint), tileable.</summary>
        public static Texture2D MetalScratches()
        {
            const int size = 512;
            return Make(size, (px, py) =>
            {
                float x = px / (float)size;
                float y = py / (float)size;
                float ridge = 1f - Mathf.Abs(Fbm(x, y, 9, 3, 811) * 2f - 1f);
                float scratches = Smooth(0.94f, 0.99f, ridge);
                float seams = 1f - Smooth(0.01f, 0.03f, CellEdge(x, y, 5, 812));
                float pits = Hash(px, py, 813) > 0.996f ? 1f : 0f;
                float v = Mathf.Clamp01(scratches * 0.6f + seams * 0.5f + pits);
                return new Color(v * 0.8f, v * 0.9f, v, 1f);
            });
        }


        // ------------------------------------------------------------------ hazards

        /// <summary>
        /// The front of a solar flare: a band across u, burning white at its leading edge (top) and trailing ragged
        /// tongues of fire down to nothing; tiles along u.
        /// </summary>
        public static Texture2D FlareFront()
        {
            const int width = 512;
            const int height = 128;
            return Make(width, height, (px, py) =>
            {
                float u = px / (float)width;
                float v = (py + 0.5f) / height;
                float tongues = Fbm(u, v * 0.25f, 16, 4, 821);
                float reach = 0.25f + tongues * 0.75f;
                float body = Mathf.Clamp01((v - (1f - reach)) / Mathf.Max(0.05f, reach));
                float lead = Smooth(0.8f, 0.97f, v) * (1f - Smooth(0.97f, 1f, v));
                float a = Mathf.Clamp01(body * body * 0.9f + lead);
                return new Color(1f, 1f, 1f, a);
            });
        }


        /// <summary>A soft band (strongest at the bottom edge, fading up), for the glow that warns of a flare.</summary>
        public static Texture2D EdgeGlow()
        {
            const int width = 16;
            const int height = 128;
            return Make(width, height, (px, py) =>
            {
                float v = (py + 0.5f) / height;
                float a = Mathf.Pow(1f - v, 1.4f);
                return new Color(1f, 1f, 1f, a);
            });
        }


        /// <summary>A forked bolt of lightning from the top edge to the bottom one, on transparent.</summary>
        public static Texture2D Lightning(int seed)
        {
            const int width = 64;
            const int height = 256;
            var random = new System.Random(seed);
            var paths = new System.Collections.Generic.List<Vector2[]>();
            Vector2[] Path(Vector2 from, Vector2 to, int steps, float jag)
            {
                var points = new Vector2[steps + 1];
                for (int i = 0; i <= steps; i++)
                {
                    float t = i / (float)steps;
                    Vector2 p = Vector2.Lerp(from, to, t);
                    if (i > 0 && i < steps)
                    {
                        p.x += ((float)random.NextDouble() * 2f - 1f) * jag;
                    }
                    points[i] = p;
                }
                return points;
            }
            Vector2[] main = Path(new Vector2(0.5f, 1f), new Vector2(0.5f, 0f), 14, 0.16f);
            paths.Add(main);
            for (int b = 0; b < 3; b++)
            {
                Vector2 start = main[3 + b * 3];
                paths.Add(Path(start, start + new Vector2(((float)random.NextDouble() - 0.5f) * 0.8f, -0.25f), 5, 0.08f));
            }
            float Distance(Vector2 p, Vector2 a, Vector2 b)
            {
                Vector2 ab = b - a;
                float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(0.0001f, ab.sqrMagnitude));
                Vector2 d = p - (a + ab * t);
                return Mathf.Sqrt(d.x * d.x * 16f + d.y * d.y);
            }
            return Make(width, height, (px, py) =>
            {
                var p = new Vector2((px + 0.5f) / width, (py + 0.5f) / height);
                float best = 10f;
                float branchBest = 10f;
                for (int k = 0; k < paths.Count; k++)
                {
                    Vector2[] path = paths[k];
                    for (int i = 0; i < path.Length - 1; i++)
                    {
                        float d = Distance(p, path[i], path[i + 1]);
                        if (k == 0)
                        {
                            best = Mathf.Min(best, d);
                        }
                        else
                        {
                            branchBest = Mathf.Min(branchBest, d);
                        }
                    }
                }
                float core = Mathf.Clamp01(1f - best / 0.035f);
                float glow = Mathf.Exp(-best * best * 900f) * 0.6f;
                float branch = Mathf.Clamp01(1f - branchBest / 0.02f) * 0.8f + Mathf.Exp(-branchBest * branchBest * 1600f) * 0.3f;
                return new Color(1f, 1f, 1f, Mathf.Clamp01(core + glow + branch));
            });
        }


        /// <summary>A billowing round cloud, soft at its rim, with denser knots (tinted by the material).</summary>
        public static Texture2D IonCloud(int seed)
        {
            const int size = 256;
            return Make(size, (px, py) =>
            {
                float d = Radial(px, py, size);
                float n = Fbm(px / (float)size, py / (float)size, 4, 5, seed);
                float knots = Fbm(px / (float)size, py / (float)size, 9, 3, seed + 1);
                float a = Mathf.Clamp01(1f - d * (1.15f + (0.5f - n) * 0.9f));
                a = a * a * (0.55f + knots * 0.7f);
                return new Color(1f, 1f, 1f, Mathf.Clamp01(a));
            });
        }


        /// <summary>The reticle of a marked enemy: four corner brackets around a thin broken ring.</summary>
        public static Texture2D Reticle()
        {
            const int size = 128;
            return Make(size, (px, py) =>
            {
                float x = (px + 0.5f) / size * 2f - 1f;
                float y = (py + 0.5f) / size * 2f - 1f;
                float ax = Mathf.Abs(x);
                float ay = Mathf.Abs(y);
                bool corner = ax > 0.55f && ay > 0.55f;
                float bracket = corner ? Mathf.Max(Mathf.Clamp01(1f - Mathf.Abs(ax - 0.9f) * 22f), Mathf.Clamp01(1f - Mathf.Abs(ay - 0.9f) * 22f)) : 0f;
                float d = Mathf.Sqrt(x * x + y * y);
                float angle = Mathf.Atan2(y, x) / (Mathf.PI * 2f) + 0.5f;
                float dash = Mathf.Repeat(angle * 8f, 1f) < 0.5f ? 1f : 0f;
                float ring = Mathf.Clamp01(1f - Mathf.Abs(d - 0.62f) * 30f) * dash;
                float dot = Mathf.Clamp01(1f - d * 12f);
                return new Color(1f, 1f, 1f, Mathf.Clamp01(bracket + ring * 0.8f + dot));
            });
        }
    }
}
