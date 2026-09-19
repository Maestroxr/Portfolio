using System;
using UnityEngine;

namespace Portfolio.Asteroids.EditorTools
{
    /// <summary>
    /// Procedural textures of the Asteroids module: the tileable nebula noise behind the playfield, glowing ore veins
    /// and lava cracks for the rocks, planet surfaces, a ring system, the accretion disk of a black hole and the
    /// particle sprites. Written as PNG files by the art builder.
    /// </summary>
    internal static class SpaceTextures
    {
        // ------------------------------------------------------------------ noise

        public static float Hash(int x, int y, int seed)
        {
            unchecked
            {
                int h = x * 374761393 + y * 668265263 + seed * 144269504;
                h = (h ^ (h >> 13)) * 1274126177;
                h ^= h >> 16;
                return (h & 0xFFFFFF) / (float)0xFFFFFF;
            }
        }

        public static int Mod(int value, int period)
        {
            int result = value % period;
            return result < 0 ? result + period : result;
        }

        /// <summary>Value noise that tiles every <paramref name="period"/> cells.</summary>
        public static float TileNoise(float x, float y, int period, int seed)
        {
            int x0 = Mathf.FloorToInt(x);
            int y0 = Mathf.FloorToInt(y);
            float fx = x - x0;
            float fy = y - y0;
            float u = fx * fx * (3f - 2f * fx);
            float v = fy * fy * (3f - 2f * fy);
            float a = Hash(Mod(x0, period), Mod(y0, period), seed);
            float b = Hash(Mod(x0 + 1, period), Mod(y0, period), seed);
            float c = Hash(Mod(x0, period), Mod(y0 + 1, period), seed);
            float d = Hash(Mod(x0 + 1, period), Mod(y0 + 1, period), seed);
            return Mathf.Lerp(Mathf.Lerp(a, b, u), Mathf.Lerp(c, d, u), v);
        }

        /// <summary>Tileable fractal noise at (x, y) in tile units (0..1 covers the tile once), in 0..1.</summary>
        public static float Fbm(float x, float y, int baseCells, int octaves, int seed, float persistence = 0.5f)
        {
            float value = 0f;
            float amplitude = 0.5f;
            float total = 0f;
            int cells = baseCells;
            for (int o = 0; o < octaves; o++)
            {
                value += amplitude * TileNoise(x * cells, y * cells, cells, seed + o * 31);
                total += amplitude;
                amplitude *= persistence;
                cells *= 2;
            }
            return value / total;
        }

        /// <summary>Distance to the nearest Voronoi cell border (tileable), in cell units.</summary>
        public static float CellEdge(float x, float y, int cells, int seed)
        {
            x *= cells;
            y *= cells;
            int cx = Mathf.FloorToInt(x);
            int cy = Mathf.FloorToInt(y);
            float first = float.MaxValue;
            float second = float.MaxValue;
            for (int oy = -1; oy <= 1; oy++)
            {
                for (int ox = -1; ox <= 1; ox++)
                {
                    int gx = cx + ox;
                    int gy = cy + oy;
                    float fx = gx + Hash(Mod(gx, cells), Mod(gy, cells), seed);
                    float fy = gy + Hash(Mod(gx, cells), Mod(gy, cells), seed + 1);
                    float distance = Mathf.Sqrt((fx - x) * (fx - x) + (fy - y) * (fy - y));
                    if (distance < first)
                    {
                        second = first;
                        first = distance;
                    }
                    else if (distance < second)
                    {
                        second = distance;
                    }
                }
            }
            return (second - first) * 0.5f;
        }

        public static Texture2D Make(int width, int height, Func<int, int, Color> pixel, bool linear = false)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false, linear);
            var pixels = new Color[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    pixels[y * width + x] = pixel(x, y);
                }
            }
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        public static Texture2D Make(int size, Func<int, int, Color> pixel)
        {
            return Make(size, size, pixel);
        }

        private static float Smooth(float edge0, float edge1, float x)
        {
            float t = Mathf.Clamp01((x - edge0) / (edge1 - edge0));
            return t * t * (3f - 2f * t);
        }

        // ------------------------------------------------------------------ surfaces

        public static Texture2D Palette(bool emission)
        {
            var texture = new Texture2D(EditorTools.Palette.Size, EditorTools.Palette.Size, TextureFormat.RGBA32, false);
            texture.SetPixels(EditorTools.Palette.Pixels(emission));
            texture.Apply();
            return texture;
        }

        /// <summary>
        /// The nebula data every sector colours differently: R soft clouds (domain-warped), G bright filaments, B dark
        /// dust lanes, A fine detail. Tiles in both directions.
        /// </summary>
        public static Texture2D Nebula()
        {
            const int size = 1024;
            return Make(size, size, (px, py) =>
            {
                float x = px / (float)size;
                float y = py / (float)size;
                float qx = Fbm(x, y, 3, 4, 101);
                float qy = Fbm(x + 0.37f, y + 0.71f, 3, 4, 102);
                float warp = Fbm(x + qx * 0.6f, y + qy * 0.6f, 4, 5, 103);
                float clouds = Smooth(0.32f, 0.78f, warp);
                float ridge = 1f - Mathf.Abs(Fbm(x + qy * 0.3f, y + qx * 0.3f, 6, 5, 104) * 2f - 1f);
                float filaments = Mathf.Pow(ridge, 5f) * Smooth(0.35f, 0.7f, warp);
                float dust = Smooth(0.52f, 0.72f, Fbm(x + qx * 0.4f, y, 5, 5, 105));
                float detail = Fbm(x, y, 16, 3, 106);
                return new Color(clouds, filaments, dust, detail);
            }, true);
        }

        /// <summary>
        /// A latitude-longitude environment for the reflections of metal hulls: dark blue space with a large soft key light
        /// behind the camera, a cool horizon band and a warm fill, so shiny surfaces pick up highlights from every angle.
        /// </summary>
        public static Texture2D ReflectionStudio()
        {
            const int width = 512;
            const int height = 256;
            Vector3 key = new Vector3(-0.45f, 0.55f, -0.7f).normalized;
            Vector3 fill = new Vector3(0.75f, -0.35f, -0.55f).normalized;
            Vector3 back = new Vector3(0.1f, 0.35f, 0.93f).normalized;
            return Make(width, height, (px, py) =>
            {
                float longitude = ((px + 0.5f) / width - 0.5f) * Mathf.PI * 2f;
                float latitude = ((py + 0.5f) / height - 0.5f) * Mathf.PI;
                var direction = new Vector3(Mathf.Cos(latitude) * Mathf.Sin(longitude), Mathf.Sin(latitude), Mathf.Cos(latitude) * Mathf.Cos(longitude));
                float up = direction.y * 0.5f + 0.5f;
                Color color = Color.Lerp(new Color(0.02f, 0.03f, 0.06f), new Color(0.09f, 0.12f, 0.2f), up);
                float band = Mathf.Exp(-direction.y * direction.y * 60f);
                color += new Color(0.12f, 0.3f, 0.45f) * band;
                color += new Color(1f, 0.97f, 0.92f) * Smooth(0.72f, 0.93f, Vector3.Dot(direction, key));
                color += new Color(0.55f, 0.32f, 0.16f) * Smooth(0.6f, 0.9f, Vector3.Dot(direction, fill));
                color += new Color(0.25f, 0.45f, 0.8f) * Smooth(0.55f, 0.95f, Vector3.Dot(direction, back));
                float nebula = Fbm(px / (float)width, py / (float)height, 4, 4, 707);
                color += new Color(0.05f, 0.08f, 0.16f) * Smooth(0.45f, 0.8f, nebula);
                color.a = 1f;
                return color;
            });
        }

        /// <summary>Thin glowing veins of ore (white, tinted by the material), tileable.</summary>
        public static Texture2D OreVeins()
        {
            const int size = 512;
            return Make(size, (px, py) =>
            {
                float x = px / (float)size;
                float y = py / (float)size;
                float ridge = 1f - Mathf.Abs(Fbm(x, y, 5, 4, 201) * 2f - 1f);
                float veins = Smooth(0.9f, 0.98f, ridge);
                float speckle = Hash(px, py, 202) > 0.994f ? 1f : 0f;
                float patches = Smooth(0.45f, 0.7f, Fbm(x, y, 3, 3, 203));
                float v = Mathf.Clamp01(veins * (0.4f + patches) + speckle * patches);
                return new Color(v, v * 0.85f, v * 0.6f, 1f);
            });
        }

        /// <summary>Glowing cracks between cooled plates of lava rock (white-hot to orange, tinted by the material), tileable.</summary>
        public static Texture2D LavaCracks()
        {
            const int size = 512;
            return Make(size, (px, py) =>
            {
                float x = px / (float)size;
                float y = py / (float)size;
                float edge = CellEdge(x, y, 7, 301);
                float wobble = Fbm(x, y, 8, 3, 302);
                float crack = 1f - Smooth(0.015f, 0.07f + wobble * 0.05f, edge);
                float pools = Smooth(0.62f, 0.8f, Fbm(x, y, 4, 3, 303)) * 0.6f;
                float heat = Mathf.Clamp01(crack + pools);
                Color hot = Color.Lerp(new Color(1f, 0.35f, 0.05f), new Color(1f, 0.9f, 0.55f), Smooth(0.7f, 1f, heat));
                return hot * heat;
            });
        }

        /// <summary>Banded gas giant (equirectangular, wraps around) through four colours, with an optional great storm.</summary>
        public static Texture2D GasGiant(Color a, Color b, Color c, Color d, int seed, bool storm)
        {
            const int width = 1024;
            const int height = 512;
            return Make(width, height, (px, py) =>
            {
                float x = px / (float)width;
                float y = py / (float)height;
                float turbulence = Fbm(x, y * 0.5f, 6, 5, seed) - 0.5f;
                float latitude = y + turbulence * 0.08f + (Fbm(x * 2f, y, 12, 3, seed + 5) - 0.5f) * 0.015f;
                float bands = Mathf.Sin(latitude * Mathf.PI * 13f) * 0.5f + 0.5f;
                float wide = Mathf.Sin(latitude * Mathf.PI * 4.2f + 1.3f) * 0.5f + 0.5f;
                float t = Mathf.Clamp01(bands * 0.55f + wide * 0.45f);
                Color color = t < 0.33f ? Color.Lerp(a, b, t / 0.33f) : t < 0.66f ? Color.Lerp(b, c, (t - 0.33f) / 0.33f) : Color.Lerp(c, d, (t - 0.66f) / 0.34f);
                if (storm)
                {
                    float dx = Mathf.DeltaAngle(x * 360f, 0.3f * 360f) / 360f;
                    float dy = (y - 0.38f) * 2.2f;
                    float spot = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx * 36f + dy * dy * 36f));
                    color = Color.Lerp(color, Color.Lerp(d, Color.white, 0.25f), Smooth(0f, 0.6f, spot));
                }
                float polar = Smooth(0.36f, 0.5f, Mathf.Abs(y - 0.5f));
                color = Color.Lerp(color, color * 0.55f, polar);
                color.a = 1f;
                return color;
            });
        }

        /// <summary>A frozen world: pale ice, darker cracked plains, bright polar caps (equirectangular).</summary>
        public static Texture2D IcePlanet()
        {
            const int width = 1024;
            const int height = 512;
            return Make(width, height, (px, py) =>
            {
                float x = px / (float)width;
                float y = py / (float)height;
                float n = Fbm(x, y * 0.5f, 8, 5, 401);
                float detail = Fbm(x, y * 0.5f, 32, 3, 403);
                // A fine network of thin fractures over a few faint large plates, on banded, mottled ice.
                float fine = 1f - Smooth(0.004f, 0.025f, CellEdge(x, y * 0.5f, 28, 402));
                float plates = 1f - Smooth(0.006f, 0.03f, CellEdge(x, y * 0.5f, 7, 404));
                float bands = 0.5f + 0.5f * Mathf.Sin((y + (n - 0.5f) * 0.1f) * Mathf.PI * 14f);
                Color plains = Color.Lerp(new Color(0.46f, 0.62f, 0.76f), new Color(0.8f, 0.88f, 0.95f), Smooth(0.3f, 0.75f, n * 0.7f + bands * 0.3f));
                plains *= 0.9f + detail * 0.2f;
                Color color = Color.Lerp(plains, new Color(0.3f, 0.47f, 0.66f), Mathf.Clamp01(fine * 0.4f + plates * 0.25f));
                float cap = Smooth(0.32f, 0.44f, Mathf.Abs(y - 0.5f) + (n - 0.5f) * 0.05f);
                color = Color.Lerp(color, new Color(0.96f, 0.98f, 1f), cap);
                color.a = 1f;
                return color;
            });
        }

        /// <summary>Radial bands of a ring system along u (inner to outer edge), with transparency in the gaps.</summary>
        public static Texture2D RingBands()
        {
            const int width = 512;
            const int height = 8;
            return Make(width, height, (px, py) =>
            {
                float u = px / (float)(width - 1);
                float fine = Fbm(u, 0.5f, 64, 3, 501);
                float coarse = Fbm(u, 0.2f, 12, 2, 502);
                float gap = Smooth(0.47f, 0.5f, u) * (1f - Smooth(0.52f, 0.55f, u));
                float edges = Smooth(0f, 0.08f, u) * (1f - Smooth(0.9f, 1f, u));
                float density = Mathf.Clamp01((0.35f + fine * 0.5f + coarse * 0.4f) * edges * (1f - gap * 0.9f));
                float shade = 0.75f + fine * 0.35f;
                return new Color(shade, shade * 0.97f, shade * 0.92f, density);
            });
        }

        /// <summary>The swirling disk of a black hole seen from above: bright spiral arms around a dark centre.</summary>
        public static Texture2D AccretionDisk()
        {
            const int size = 256;
            return Make(size, (px, py) =>
            {
                float x = (px + 0.5f) / size * 2f - 1f;
                float y = (py + 0.5f) / size * 2f - 1f;
                float r = Mathf.Sqrt(x * x + y * y);
                if (r > 1f)
                {
                    return new Color(0f, 0f, 0f, 0f);
                }
                float angle = Mathf.Atan2(y, x) / (Mathf.PI * 2f) + 0.5f;
                float spiral = angle * 3f + Mathf.Log(Mathf.Max(0.05f, r)) * 1.6f;
                float arms = Mathf.Pow(0.5f + 0.5f * Mathf.Sin(spiral * Mathf.PI * 2f), 3f);
                float grain = Fbm(angle, r, 8, 3, 601);
                float inner = Smooth(0.22f, 0.32f, r);
                float outer = 1f - Smooth(0.55f, 1f, r);
                float intensity = inner * outer * (0.35f + arms * 0.9f) * (0.7f + grain * 0.6f);
                float hot = 1f - Smooth(0.3f, 0.7f, r);
                Color color = Color.Lerp(new Color(0.55f, 0.35f, 1f), new Color(1f, 0.9f, 1f), hot);
                return new Color(color.r, color.g, color.b, Mathf.Clamp01(intensity));
            });
        }

        // ------------------------------------------------------------------ particles and sprites

        private static float Radial(int x, int y, int size)
        {
            return Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(size / 2f, size / 2f)) / (size / 2f);
        }

        /// <summary>
        /// The glow around a planet, for a quad 1.7 times the planet's size behind it: full inside the planet's disc (which
        /// hides it) and fading out from the edge of the disc.
        /// </summary>
        public static Texture2D AtmosphereHalo()
        {
            const int size = 256;
            const float edge = 1f / 1.7f;
            return Make(size, (x, y) =>
            {
                float r = Radial(x, y, size);
                float t = Mathf.Max(0f, (r - edge) / (1f - edge));
                float a = Mathf.Exp(-t * 4.5f) * (1f - Smooth(0.8f, 1f, r));
                return new Color(1f, 1f, 1f, a);
            });
        }

        public static Texture2D SoftDot()
        {
            const int size = 64;
            return Make(size, (x, y) =>
            {
                float a = Mathf.Clamp01(1f - Radial(x, y, size));
                return new Color(1f, 1f, 1f, a * a * (3f - 2f * a));
            });
        }

        /// <summary>A soft glow with a hot core, for halos, muzzle flashes and beacons.</summary>
        public static Texture2D Glow()
        {
            const int size = 128;
            return Make(size, (x, y) =>
            {
                float d = Radial(x, y, size);
                float core = Mathf.Clamp01(1f - d * 3f);
                float halo = Mathf.Exp(-d * d * 7f) * (1f - Smooth(0.85f, 1f, d));
                return new Color(1f, 1f, 1f, Mathf.Clamp01(halo * 0.8f + core * core));
            });
        }

        /// <summary>A four-pointed star flare.</summary>
        public static Texture2D Flare()
        {
            const int size = 128;
            return Make(size, (x, y) =>
            {
                float px = (x + 0.5f) / size * 2f - 1f;
                float py = (y + 0.5f) / size * 2f - 1f;
                float rays = Mathf.Max(Mathf.Clamp01(1f - Mathf.Abs(px) * 12f - Mathf.Abs(py) * 1.05f), Mathf.Clamp01(1f - Mathf.Abs(py) * 12f - Mathf.Abs(px) * 1.05f));
                float d = Mathf.Sqrt(px * px + py * py);
                float glow = Mathf.Exp(-d * d * 14f);
                return new Color(1f, 1f, 1f, Mathf.Clamp01(rays * rays + glow));
            });
        }

        /// <summary>A streak for sparks, stretched along its velocity by the particle renderer.</summary>
        public static Texture2D Streak()
        {
            const int size = 64;
            return Make(size, (x, y) =>
            {
                float px = (x + 0.5f) / size * 2f - 1f;
                float py = (y + 0.5f) / size * 2f - 1f;
                float a = Mathf.Clamp01(1f - Mathf.Abs(px) * 2.5f) * Mathf.Clamp01(1f - Mathf.Abs(py));
                return new Color(1f, 1f, 1f, a * a);
            });
        }

        public static Texture2D Smoke()
        {
            const int size = 64;
            return Make(size, (x, y) =>
            {
                float d = Radial(x, y, size);
                float n = Fbm(x / (float)size, y / (float)size, 4, 3, 701);
                float a = Mathf.Clamp01(1f - d * (1.1f + (0.5f - n) * 0.9f));
                return new Color(1f, 1f, 1f, a * a);
            });
        }

        /// <summary>A thin ring for shockwaves and warp-ins.</summary>
        public static Texture2D Ring()
        {
            const int size = 256;
            return Make(size, (x, y) =>
            {
                float d = Radial(x, y, size);
                float rim = Mathf.Clamp01(1f - Mathf.Abs(d - 0.9f) / 0.07f);
                float inner = Mathf.Clamp01(1f - Mathf.Abs(d - 0.9f) / 0.3f) * 0.25f;
                float a = d > 1f ? 0f : Mathf.Clamp01(rim * rim + inner);
                return new Color(1f, 1f, 1f, a);
            });
        }

        /// <summary>A dashed ring showing how far a blast reaches.</summary>
        public static Texture2D DangerRing()
        {
            const int size = 256;
            return Make(size, (x, y) =>
            {
                float px = (x + 0.5f) / size * 2f - 1f;
                float py = (y + 0.5f) / size * 2f - 1f;
                float d = Mathf.Sqrt(px * px + py * py);
                float angle = Mathf.Atan2(py, px) / (Mathf.PI * 2f) + 0.5f;
                float dash = Mathf.Repeat(angle * 24f, 1f) < 0.62f ? 1f : 0f;
                float rim = Mathf.Clamp01(1f - Mathf.Abs(d - 0.94f) / 0.035f) * dash;
                float fill = d < 0.94f ? 0.12f * Smooth(0.2f, 0.94f, d) : 0f;
                return new Color(1f, 1f, 1f, Mathf.Clamp01(rim + fill));
            });
        }

        /// <summary>An energy bolt: a bright core in an elongated glow (pointing along +v).</summary>
        public static Texture2D Bolt()
        {
            const int width = 32;
            const int height = 128;
            return Make(width, height, (x, y) =>
            {
                float px = ((x + 0.5f) / width * 2f - 1f) * 1.2f;
                float py = (y + 0.5f) / height * 2f - 1f;
                float body = Mathf.Clamp01(1f - px * px) * Mathf.Clamp01(1f - py * py);
                float core = Mathf.Clamp01(1f - Mathf.Abs(px) * 4f) * Mathf.Clamp01(1f - Mathf.Abs(py) * 1.3f);
                return new Color(1f, 1f, 1f, Mathf.Clamp01(body * body * 0.8f + core));
            }, false);
        }

        /// <summary>A round plasma ball with a white-hot centre.</summary>
        public static Texture2D Plasma()
        {
            const int size = 64;
            return Make(size, (x, y) =>
            {
                float d = Radial(x, y, size);
                float core = Mathf.Clamp01(1f - d * 2.4f);
                float body = Mathf.Clamp01(1f - d);
                return new Color(1f, 1f, 1f, Mathf.Clamp01(body * body * 0.9f + core));
            });
        }

        /// <summary>An engine flame: a teardrop from the nozzle (top) fading down.</summary>
        public static Texture2D Flame()
        {
            const int width = 64;
            const int height = 128;
            return Make(width, height, (x, y) =>
            {
                float px = (x + 0.5f) / width * 2f - 1f;
                float t = 1f - (y + 0.5f) / height;
                float widthAt = Mathf.Lerp(1f, 0.05f, Mathf.Pow(t, 0.7f));
                float across = Mathf.Clamp01(1f - Mathf.Abs(px) / Mathf.Max(0.01f, widthAt));
                float along = 1f - t;
                float core = Mathf.Clamp01(1f - Mathf.Abs(px) * 3.5f) * Mathf.Pow(along, 2f);
                return new Color(1f, 1f, 1f, Mathf.Clamp01(across * across * along + core));
            });
        }

        /// <summary>A sharp triangular shard.</summary>
        public static Texture2D Shard()
        {
            const int size = 64;
            return Make(size, (x, y) =>
            {
                float px = (x + 0.5f) / size * 2f - 1f;
                float py = (y + 0.5f) / size * 2f - 1f;
                float inside = py > -0.8f && Mathf.Abs(px) < (0.95f - py) * 0.35f ? 1f : 0f;
                float edge = Mathf.Clamp01(1f - Mathf.Abs(Mathf.Abs(px) - (0.95f - py) * 0.35f) * 20f);
                return new Color(1f, 1f, 1f, Mathf.Clamp01(inside * 0.8f + edge * 0.6f));
            });
        }

        public static Texture2D Square()
        {
            return Make(8, (x, y) => Color.white);
        }
    }
}
