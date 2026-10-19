using UnityEngine;

namespace Portfolio.DeathRace.EditorTools
{
    /// <summary>
    /// Seeded, tileable noise for the generated surfaces: value noise and its fractal sum, cellular (Worley) noise for
    /// cracks, stones and slabs, and a height field to normal map step. Every function tiles over the unit square in
    /// both directions (the period is a whole number of cells), so the textures repeat without seams on the track.
    /// </summary>
    internal static class DeathRaceArtNoise
    {
        /// <summary>A hash of a lattice point to 0..1.</summary>
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

        /// <summary>
        /// Smooth value noise at (<paramref name="u"/>, <paramref name="v"/>) in the unit square with
        /// <paramref name="cellsX"/> by <paramref name="cellsY"/> cells; 0..1, tiling at the square's edges.
        /// </summary>
        public static float Value(float u, float v, int cellsX, int cellsY, int seed)
        {
            float x = u * cellsX;
            float y = v * cellsY;
            int x0 = Mathf.FloorToInt(x);
            int y0 = Mathf.FloorToInt(y);
            float fx = x - x0;
            float fy = y - y0;
            float sx = fx * fx * fx * (fx * (fx * 6f - 15f) + 10f);
            float sy = fy * fy * fy * (fy * (fy * 6f - 15f) + 10f);
            float a = Hash(Mod(x0, cellsX), Mod(y0, cellsY), seed);
            float b = Hash(Mod(x0 + 1, cellsX), Mod(y0, cellsY), seed);
            float c = Hash(Mod(x0, cellsX), Mod(y0 + 1, cellsY), seed);
            float d = Hash(Mod(x0 + 1, cellsX), Mod(y0 + 1, cellsY), seed);
            return Mathf.Lerp(Mathf.Lerp(a, b, sx), Mathf.Lerp(c, d, sx), sy);
        }

        /// <summary>Fractal value noise (octaves double the cells and halve the weight), 0..1, tiling.</summary>
        public static float Fbm(float u, float v, int cellsX, int cellsY, int octaves, int seed, float gain = 0.5f)
        {
            float sum = 0f;
            float amplitude = 1f;
            float total = 0f;
            for (int octave = 0; octave < octaves; octave++)
            {
                sum += Value(u, v, cellsX, cellsY, seed + octave * 101) * amplitude;
                total += amplitude;
                amplitude *= gain;
                cellsX *= 2;
                cellsY *= 2;
            }
            return sum / total;
        }

        public static float Fbm(float u, float v, int cells, int octaves, int seed, float gain = 0.5f)
        {
            return Fbm(u, v, cells, cells, octaves, seed, gain);
        }

        /// <summary>Fractal value noise at (<paramref name="x"/>, <paramref name="y"/>) in cell units, not tiling (for sprites).</summary>
        public static float Plain(float x, float y, int octaves, int seed)
        {
            const int period = 1 << 14;
            return Fbm(x / period, y / period, period, period, octaves, seed);
        }

        /// <summary>Ridged noise at (<paramref name="x"/>, <paramref name="y"/>) in cell units, not tiling.</summary>
        public static float PlainRidge(float x, float y, int octaves, int seed)
        {
            const int period = 1 << 14;
            return Ridge(x / period, y / period, period, period, octaves, seed);
        }

        /// <summary>
        /// Ridged noise: sharp creases where the value noise crosses its middle; 0..1 with the creases at 1. Good for
        /// branching cracks and rock veins.
        /// </summary>
        public static float Ridge(float u, float v, int cellsX, int cellsY, int octaves, int seed)
        {
            float sum = 0f;
            float amplitude = 1f;
            float total = 0f;
            for (int octave = 0; octave < octaves; octave++)
            {
                float n = 1f - Mathf.Abs(Value(u, v, cellsX, cellsY, seed + octave * 131) * 2f - 1f);
                sum += n * n * amplitude;
                total += amplitude;
                amplitude *= 0.5f;
                cellsX *= 2;
                cellsY *= 2;
            }
            return sum / total;
        }

        /// <summary>The result of <see cref="Cells"/>: distances to the nearest and second nearest feature point and the id of the nearest.</summary>
        public struct Cell
        {
            public float F1;
            public float F2;
            public float Id;
            public Vector2 Center;

            /// <summary>How near the point is to the border between two cells (0 on the border), in cell units.</summary>
            public float Edge => F2 - F1;
        }

        /// <summary>
        /// Cellular noise with one jittered feature point per cell (<paramref name="jitter"/> 0 = a regular grid), tiling;
        /// distances in cell units.
        /// </summary>
        public static Cell Cells(float u, float v, int cellsX, int cellsY, int seed, float jitter = 0.85f)
        {
            float x = u * cellsX;
            float y = v * cellsY;
            int cx = Mathf.FloorToInt(x);
            int cy = Mathf.FloorToInt(y);
            float f1 = 9f;
            float f2 = 9f;
            float id = 0f;
            Vector2 center = Vector2.zero;
            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    int gx = cx + dx;
                    int gy = cy + dy;
                    int wx = Mod(gx, cellsX);
                    int wy = Mod(gy, cellsY);
                    float px = gx + 0.5f + (Hash(wx, wy, seed) - 0.5f) * jitter;
                    float py = gy + 0.5f + (Hash(wx, wy, seed + 7) - 0.5f) * jitter;
                    float ddx = px - x;
                    float ddy = py - y;
                    float d = Mathf.Sqrt(ddx * ddx + ddy * ddy);
                    if (d < f1)
                    {
                        f2 = f1;
                        f1 = d;
                        id = Hash(wx, wy, seed + 13);
                        center = new Vector2(px / cellsX, py / cellsY);
                    }
                    else if (d < f2)
                    {
                        f2 = d;
                    }
                }
            }
            return new Cell { F1 = f1, F2 = f2, Id = id, Center = center };
        }

        public static float Smooth(float edge0, float edge1, float x)
        {
            float t = Mathf.Clamp01((x - edge0) / (edge1 - edge0));
            return t * t * (3f - 2f * t);
        }

        /// <summary>
        /// A tangent space normal map (OpenGL style, green up) from a tiling height field of <paramref name="width"/> by
        /// <paramref name="height"/> samples; <paramref name="strength"/> scales the slopes (height units per pixel).
        /// </summary>
        public static Color[] NormalMap(float[] heights, int width, int height, float strength)
        {
            var normals = new Color[heights.Length];
            for (int y = 0; y < height; y++)
            {
                int up = Mod(y + 1, height) * width;
                int down = Mod(y - 1, height) * width;
                int row = y * width;
                for (int x = 0; x < width; x++)
                {
                    int right = Mod(x + 1, width);
                    int left = Mod(x - 1, width);
                    float dx = (heights[row + right] - heights[row + left]) * 0.5f * strength;
                    float dy = (heights[up + x] - heights[down + x]) * 0.5f * strength;
                    var normal = new Vector3(-dx, -dy, 1f).normalized;
                    normals[row + x] = new Color(normal.x * 0.5f + 0.5f, normal.y * 0.5f + 0.5f, normal.z * 0.5f + 0.5f, 1f);
                }
            }
            return normals;
        }

        public static Texture2D ToTexture(Color[] pixels, int width, int height, bool linear = false)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false, linear);
            texture.SetPixels(pixels);
            texture.Apply(false, false);
            return texture;
        }
    }
}
