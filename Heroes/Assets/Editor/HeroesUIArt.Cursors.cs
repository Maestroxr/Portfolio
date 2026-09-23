using System;
using UnityEditor;
using UnityEngine;

namespace Portfolio.Heroes.EditorTools
{
    /// <summary>
    /// The pointers of the game, from Kenney's Cursor Pack (Art/UI/Cursors: white shapes in a black outline) painted in
    /// the gold of the interface, and the credits of everything the game shows and plays.
    /// </summary>
    internal static partial class HeroesUIArt
    {
        private const int CursorSize = 32;

        /// <summary>Where a pointer's picture comes from, how it is painted, and the pixel that clicks.</summary>
        private enum Spot
        {
            /// <summary>The first drawn pixel from the top left: the tip of a finger, a blade or a wand.</summary>
            Tip,
            Center
        }

        private static readonly (CursorKind kind, string source, Spot spot, Color light, Color dark)[] Pointers =
        {
            (CursorKind.Default, "gauntlet_default", Spot.Tip, new Color(1f, 0.93f, 0.66f), new Color(0.74f, 0.52f, 0.2f)),
            (CursorKind.Hand, "hand_point", Spot.Tip, new Color(1f, 0.95f, 0.8f), new Color(0.84f, 0.66f, 0.42f)),
            (CursorKind.Move, "boot", Spot.Center, new Color(1f, 0.93f, 0.66f), new Color(0.74f, 0.52f, 0.2f)),
            (CursorKind.Fly, "icon:spell_bless", Spot.Center, new Color(1f, 1f, 1f), new Color(0.7f, 0.8f, 0.95f)),
            (CursorKind.Attack, "tool_sword_a", Spot.Tip, new Color(0.95f, 0.96f, 1f), new Color(0.55f, 0.58f, 0.66f)),
            (CursorKind.Shoot, "tool_bow", Spot.Center, new Color(1f, 0.9f, 0.62f), new Color(0.66f, 0.42f, 0.18f)),
            (CursorKind.Cast, "tool_wand", Spot.Tip, new Color(0.82f, 0.9f, 1f), new Color(0.38f, 0.5f, 0.95f)),
            (CursorKind.Blocked, "disabled", Spot.Center, new Color(1f, 0.5f, 0.42f), new Color(0.72f, 0.1f, 0.08f)),
            (CursorKind.Wait, "busy_hourglass", Spot.Center, new Color(1f, 0.93f, 0.66f), new Color(0.74f, 0.52f, 0.2f)),
            (CursorKind.Info, "look_a", Spot.Center, new Color(1f, 0.97f, 0.88f), new Color(0.82f, 0.72f, 0.5f)),
            (CursorKind.Travel, "steps", Spot.Center, new Color(1f, 0.93f, 0.66f), new Color(0.74f, 0.52f, 0.2f)),
            (CursorKind.Visit, "door_enter", Spot.Center, new Color(1f, 0.93f, 0.66f), new Color(0.74f, 0.52f, 0.2f)),
            (CursorKind.Fight, "tool_sword_b", Spot.Tip, new Color(1f, 0.62f, 0.5f), new Color(0.7f, 0.16f, 0.1f)),
            (CursorKind.Take, "hand_open", Spot.Center, new Color(1f, 0.95f, 0.8f), new Color(0.84f, 0.66f, 0.42f))
        };

        private static void Cursors(HeroesArt art)
        {
            int count = Enum.GetValues(typeof(CursorKind)).Length;
            var cursors = new Texture2D[count];
            var hotspots = new Vector2[count];
            foreach ((CursorKind kind, string source, Spot spot, Color light, Color dark) in Pointers)
            {
                Color[] picture = source.StartsWith("icon:", StringComparison.Ordinal)
                    ? Outlined(source.Substring(5))
                    : Load(source, CursorSize);
                if (picture == null)
                {
                    continue;
                }
                picture = Tint(picture, CursorSize, light, dark);
                hotspots[(int)kind] = spot == Spot.Tip ? Tip(picture, CursorSize) : new Vector2(CursorSize / 2f, CursorSize / 2f);
                cursors[(int)kind] = SaveCursor(picture, kind);
            }

            // The strikes: the big sword turned to point the way of the blow, its hot spot on the tip.
            Color[] sword = Load("tool_sword_a_double", CursorSize * 2);
            if (sword != null)
            {
                (float angle, CursorKind kind)[] strikes =
                {
                    (0f, CursorKind.StrikeEast), (60f, CursorKind.StrikeNorthEast), (120f, CursorKind.StrikeNorthWest),
                    (180f, CursorKind.StrikeWest), (240f, CursorKind.StrikeSouthWest), (300f, CursorKind.StrikeSouthEast)
                };
                foreach ((float angle, CursorKind kind) in strikes)
                {
                    // The blade of the picture points up and to the left (135 degrees).
                    Color[] turned = Turn(sword, CursorSize * 2, angle - 135f);
                    Color[] small = Tint(Shrink(turned, CursorSize * 2, CursorSize), CursorSize, new Color(0.95f, 0.96f, 1f), new Color(0.55f, 0.58f, 0.66f));
                    Vector2 direction = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
                    hotspots[(int)kind] = Farthest(small, CursorSize, direction);
                    cursors[(int)kind] = SaveCursor(small, kind);
                }
            }
            art.cursors = cursors;
            art.cursorHotspots = hotspots;
        }

        /// <summary>A pointer of Kenney's, <paramref name="size"/> pixels square, rows from the bottom as Unity keeps them.</summary>
        private static Color[] Load(string name, int size)
        {
            Texture2D texture = Read($"Art/UI/Cursors/{name}.png");
            if (texture == null)
            {
                Debug.LogWarning($"Heroes: the pointer {name} is missing.");
                return null;
            }
            Color[] pixels = texture.width == size && texture.height == size ? texture.GetPixels() : Shrink(texture.GetPixels(), texture.width, size);
            UnityEngine.Object.DestroyImmediate(texture);
            return pixels;
        }

        /// <summary>One of the white icons of the game as a pointer: shrunk, with a black outline like Kenney's.</summary>
        private static Color[] Outlined(string icon)
        {
            Texture2D texture = Read($"Art/UI/Icons/{icon}.png");
            if (texture == null)
            {
                return null;
            }
            const int inner = CursorSize - 6;
            Color[] shrunk = Shrink(texture.GetPixels(), texture.width, texture.width / Mathf.Max(1, texture.width / inner));
            int from = texture.width / Mathf.Max(1, texture.width / inner);
            UnityEngine.Object.DestroyImmediate(texture);
            var result = new Color[CursorSize * CursorSize];
            int offset = (CursorSize - from) / 2;
            float A(int x, int y) => x < 0 || y < 0 || x >= from || y >= from ? 0f : shrunk[y * from + x].a;
            for (int y = 0; y < CursorSize; y++)
            {
                for (int x = 0; x < CursorSize; x++)
                {
                    int sx = x - offset;
                    int sy = y - offset;
                    float grown = 0f;
                    for (int dy = -2; dy <= 2; dy++)
                    {
                        for (int dx = -2; dx <= 2; dx++)
                        {
                            if (dx * dx + dy * dy <= 5)
                            {
                                grown = Mathf.Max(grown, A(sx + dx, sy + dy));
                            }
                        }
                    }
                    float a = A(sx, sy);
                    Color outline = new Color(0f, 0f, 0f, grown);
                    Color fill = new Color(1f, 1f, 1f, a);
                    result[y * CursorSize + x] = Over(fill, outline);
                }
            }
            return result;
        }

        private static Color Over(Color top, Color bottom)
        {
            float a = top.a + bottom.a * (1f - top.a);
            if (a <= 0.0001f)
            {
                return Color.clear;
            }
            return new Color((top.r * top.a + bottom.r * bottom.a * (1f - top.a)) / a, (top.g * top.a + bottom.g * bottom.a * (1f - top.a)) / a,
                (top.b * top.a + bottom.b * bottom.a * (1f - top.a)) / a, a);
        }

        /// <summary>Paints the white of a pointer from <paramref name="light"/> at the top to <paramref name="dark"/> below; the black stays.</summary>
        private static Color[] Tint(Color[] pixels, int size, Color light, Color dark)
        {
            var result = new Color[pixels.Length];
            for (int i = 0; i < pixels.Length; i++)
            {
                Color c = pixels[i];
                float white = (c.r + c.g + c.b) / 3f;
                float t = i / size / (float)(size - 1);
                Color paint = Color.Lerp(dark, light, t) * Mathf.Lerp(0.85f, 1.05f, white);
                // Kenney's pictures shade their white with greys: the grey keeps some of its darkness.
                Color color = Color.Lerp(new Color(0.05f, 0.035f, 0.02f), paint, Mathf.Clamp01(white * 1.15f));
                color.a = c.a;
                result[i] = color;
            }
            return result;
        }

        /// <summary>The pixel nearest the top left corner that is drawn, from the top left as a pointer counts.</summary>
        private static Vector2 Tip(Color[] pixels, int size)
        {
            return Farthest(pixels, size, new Vector2(-1f, 1f));
        }

        /// <summary>The drawn pixel farthest along <paramref name="direction"/> (y up), from the top left as a pointer counts.</summary>
        private static Vector2 Farthest(Color[] pixels, int size, Vector2 direction)
        {
            float best = float.MinValue;
            var spot = new Vector2(size / 2f, size / 2f);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    if (pixels[y * size + x].a < 0.5f)
                    {
                        continue;
                    }
                    float along = Vector2.Dot(new Vector2(x, y), direction);
                    if (along > best + 0.001f)
                    {
                        best = along;
                        spot = new Vector2(x, size - 1 - y);
                    }
                }
            }
            // A pixel in from the outline, onto the tip itself.
            Vector2 inward = new Vector2(-direction.x, direction.y).normalized;
            return new Vector2(Mathf.Clamp(Mathf.Round(spot.x + inward.x * 1.5f), 0f, size - 1), Mathf.Clamp(Mathf.Round(spot.y + inward.y * 1.5f), 0f, size - 1));
        }

        /// <summary>A square picture turned around its middle by <paramref name="degrees"/> (counter clockwise).</summary>
        private static Color[] Turn(Color[] pixels, int size, float degrees)
        {
            var result = new Color[pixels.Length];
            float a = -degrees * Mathf.Deg2Rad;
            float cos = Mathf.Cos(a);
            float sin = Mathf.Sin(a);
            float c = size / 2f;
            Color At(int x, int y) => x < 0 || y < 0 || x >= size || y >= size ? Color.clear : pixels[y * size + x];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float px = x + 0.5f - c;
                    float py = y + 0.5f - c;
                    float sx = px * cos - py * sin + c - 0.5f;
                    float sy = px * sin + py * cos + c - 0.5f;
                    int x0 = Mathf.FloorToInt(sx);
                    int y0 = Mathf.FloorToInt(sy);
                    float tx = sx - x0;
                    float ty = sy - y0;
                    Color c00 = At(x0, y0), c10 = At(x0 + 1, y0), c01 = At(x0, y0 + 1), c11 = At(x0 + 1, y0 + 1);
                    // Blended in premultiplied color, so the transparent surroundings do not darken the edge.
                    Color Pre(Color k) => new Color(k.r * k.a, k.g * k.a, k.b * k.a, k.a);
                    Color mix = Color.Lerp(Color.Lerp(Pre(c00), Pre(c10), tx), Color.Lerp(Pre(c01), Pre(c11), tx), ty);
                    result[y * size + x] = mix.a > 0.0001f ? new Color(mix.r / mix.a, mix.g / mix.a, mix.b / mix.a, mix.a) : Color.clear;
                }
            }
            return result;
        }

        private static Texture2D SaveCursor(Color[] pixels, CursorKind kind)
        {
            var texture = new Texture2D(CursorSize, CursorSize, TextureFormat.RGBA32, false);
            texture.SetPixels(pixels);
            texture.Apply();
            return HeroesAssets.SaveTexture(texture, $"Art/Generated/Cursors/{kind}.png", importer =>
            {
                importer.textureType = TextureImporterType.Cursor;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.isReadable = true;
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.filterMode = FilterMode.Point;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.maxTextureSize = 32;
            });
        }

        // ------------------------------------------------------------------ credits

        /// <summary>The credits (Art/Licenses/CREDITS.txt, written with the licences of every source), for a credits panel.</summary>
        private static void Credits(HeroesArt art)
        {
            art.credits = HeroesAssets.Load<TextAsset>("Art/Licenses/CREDITS.txt");
            if (art.credits == null)
            {
                Debug.LogWarning("Heroes: Art/Licenses/CREDITS.txt is missing.");
            }
        }
    }
}
