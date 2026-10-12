using System.Collections.Generic;
using Gamebox.Editor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Portfolio.BoardGames.EditorTools
{
    /// <summary>
    /// The backgammon art. The meshes are made once (<see cref="BuildShared"/>), in board units as
    /// <see cref="BackgammonLayout"/> lays them out: the box (rim, bar, divider and the split between the trays, wood
    /// grain mapped along each part), the felt, one point (a triangle), the checker (a lathed disc with a rim and a
    /// grooved top), the die and the doubling cube (rounded cubes on a 3 by 2 atlas, see <see cref="DiceArt"/>). Each look
    /// adds its felt, point, glow, dice and cube textures and materials (<see cref="BuildLook"/>); the box wears the
    /// draughts board's slab and the checkers the draughts pieces' materials, so the three games share one table. Classic
    /// is walnut, green felt, ivory and burgundy points, white dice and an ivory cube; Midnight dark slate with neon cyan
    /// and magenta points, glowing dice and a glass cube.
    /// </summary>
    internal static class BackgammonArt
    {
        private const string Prefix = "Backgammon";
        private const int FeltPixels = 1024;
        private const int PointPixelsX = 128;
        private const int PointPixelsY = 512;
        private const int DieCell = 128;
        /// <summary>World units one repeat of the slab's wood texture covers on the box.</summary>
        private const float WoodTile = 8f;

        private static Shader Lit => Shader.Find("Universal Render Pipeline/Lit");
        private static Shader Unlit => Shader.Find("Universal Render Pipeline/Unlit");

        /// <summary>The colours of one look that the shared spec does not carry.</summary>
        private sealed class Palette
        {
            public Color Felt;
            public bool Slate;
            public Color PointA;
            public Color PointB;
            /// <summary>Neon points: the colour the edges glow in (black for none).</summary>
            public Color GlowA;
            public Color GlowB;
            public Color DieBodyLight;
            public Color DiePipLight;
            public Color DieBodyDark;
            public Color DiePipDark;
            public bool DiceGlow;
            public Color CubeBody;
            public Color CubeInk;
            public bool CubeGlow;
            public float FeltSmoothness;
            public float PointSmoothness;
        }

        private static Palette PaletteOf(BoardGamesThemeSpec spec)
        {
            if (spec.Emissive)
            {
                return new Palette
                {
                    Felt = BoardGamesThemeSpec.Hex(0x121A2B),
                    Slate = true,
                    PointA = BoardGamesThemeSpec.Hex(0x3FE6FF),
                    PointB = BoardGamesThemeSpec.Hex(0xFF4FD8),
                    GlowA = new Color(0.15f, 0.9f, 1.05f),
                    GlowB = new Color(1.05f, 0.18f, 0.85f),
                    // Glass in the side's colour with near-white pips: dark bodies vanished on the slate felt.
                    DieBodyLight = BoardGamesThemeSpec.Hex(0x1F6E85),
                    DiePipLight = BoardGamesThemeSpec.Hex(0xE8FDFF),
                    DieBodyDark = BoardGamesThemeSpec.Hex(0x7A2470),
                    DiePipDark = BoardGamesThemeSpec.Hex(0xFFE6FA),
                    DiceGlow = true,
                    CubeBody = BoardGamesThemeSpec.Hex(0x1C253A),
                    CubeInk = BoardGamesThemeSpec.Hex(0xE6F6FF),
                    CubeGlow = true,
                    FeltSmoothness = 0.35f,
                    PointSmoothness = 0.7f
                };
            }
            return new Palette
            {
                Felt = BoardGamesThemeSpec.Hex(0x15402A),
                Slate = false,
                PointA = BoardGamesThemeSpec.Hex(0xE6D6B2),
                PointB = BoardGamesThemeSpec.Hex(0x7E1E2C),
                GlowA = Color.black,
                GlowB = Color.black,
                DieBodyLight = BoardGamesThemeSpec.Hex(0xF7F3EA),
                DiePipLight = BoardGamesThemeSpec.Hex(0x161313),
                DieBodyDark = BoardGamesThemeSpec.Hex(0xF7F3EA),
                DiePipDark = BoardGamesThemeSpec.Hex(0x161313),
                DiceGlow = false,
                CubeBody = BoardGamesThemeSpec.Hex(0xEFE6D0),
                CubeInk = BoardGamesThemeSpec.Hex(0x3A1A16),
                CubeGlow = false,
                FeltSmoothness = 0.08f,
                PointSmoothness = 0.3f
            };
        }

        public static string MeshPath(string name)
        {
            return $"{BoardGamesArtBuilder.MeshesFolder}/{Prefix}{name}.asset";
        }

        public static Mesh Mesh(string name)
        {
            return BoardGamesAssets.Require<Mesh>(MeshPath(name));
        }

        public static Material Material(string name, BoardGamesThemeSpec spec)
        {
            return BoardGamesAssets.Require<Material>($"{spec.MaterialsFolder}/{Prefix}{name}.mat");
        }

        /// <summary>What every look shares (the meshes). Called once per Build Everything.</summary>
        public static void BuildShared()
        {
            BoardGamesAssets.SaveMesh(FrameMesh().ToMesh(Prefix + "Frame"), MeshPath("Frame"));
            float fieldDepth = BackgammonLayout.FieldDepth;
            float inner = BackgammonLayout.HalfWidth - BackgammonLayout.Rim;
            BoardGamesAssets.SaveMesh(MeshFactory.Plane(inner * 2f, fieldDepth * 2f, new Rect(0f, 0f, 1f, 1f)).ToMesh(Prefix + "Felt"), MeshPath("Felt"));
            BoardGamesAssets.SaveMesh(PointMesh().ToMesh(Prefix + "Point"), MeshPath("Point"));
            BoardGamesAssets.SaveMesh(CheckerMesh().ToMesh(Prefix + "Checker"), MeshPath("Checker"));
            BoardGamesAssets.SaveMesh(DiceArt.Die(BackgammonLayout.DieSize, 0.07f, 10).ToMesh(Prefix + "Die"), MeshPath("Die"));
            BoardGamesAssets.SaveMesh(DiceArt.Die(BackgammonLayout.CubeSize, 0.06f, 10).ToMesh(Prefix + "Cube"), MeshPath("Cube"));
        }

        /// <summary>The textures and materials of one look.</summary>
        public static void BuildLook(BoardGamesThemeSpec spec)
        {
            Palette palette = PaletteOf(spec);
            string textures = spec.TexturesFolder;
            Texture2D felt = BoardGamesAssets.SaveTexture(Felt(FeltPixels, palette.Felt, palette.Slate, 5), $"{textures}/{Prefix}Felt.png", GameAssets.SurfaceTexture(false, FeltPixels));
            Texture2D point = BoardGamesAssets.SaveTexture(Point(palette.Slate), $"{textures}/{Prefix}Point.png", GameAssets.SurfaceTexture(false, PointPixelsY));
            Texture2D glow = BoardGamesAssets.SaveTexture(PointGlow(), $"{textures}/{Prefix}PointGlow.png", GameAssets.DecalTexture(PointPixelsY));
            Texture2D diceLight = BoardGamesAssets.SaveTexture(DiceArt.PipAtlas(DieCell, palette.DieBodyLight, palette.DiePipLight, palette.DiceGlow),
                $"{textures}/{Prefix}DiceLight.png", GameAssets.SurfaceTexture(false, 512, false));
            Texture2D diceDark = BoardGamesAssets.SaveTexture(DiceArt.PipAtlas(DieCell, palette.DieBodyDark, palette.DiePipDark, palette.DiceGlow),
                $"{textures}/{Prefix}DiceDark.png", GameAssets.SurfaceTexture(false, 512, false));
            Texture2D cube = BoardGamesAssets.SaveTexture(CubeAtlas(DieCell, palette), $"{textures}/{Prefix}Cube.png", GameAssets.SurfaceTexture(false, 512, false));
            // A glowing look lights the pips (and the face rims) alone: a whole lit face washes the pips out.
            Texture2D diceLightGlow = palette.DiceGlow
                ? BoardGamesAssets.SaveTexture(DiceArt.PipAtlas(DieCell, Color.black, palette.DiePipLight, true), $"{textures}/{Prefix}DiceLightGlow.png", GameAssets.SurfaceTexture(false, 512, false))
                : null;
            Texture2D diceDarkGlow = palette.DiceGlow
                ? BoardGamesAssets.SaveTexture(DiceArt.PipAtlas(DieCell, Color.black, palette.DiePipDark, true), $"{textures}/{Prefix}DiceDarkGlow.png", GameAssets.SurfaceTexture(false, 512, false))
                : null;

            LitMaterial(spec, "Felt", Color.white, 0f, palette.FeltSmoothness, felt, null, default);
            bool neon = palette.GlowA.maxColorComponent > 0f;
            LitMaterial(spec, "PointA", palette.PointA, 0f, palette.PointSmoothness, point, neon ? point : null, palette.GlowA);
            LitMaterial(spec, "PointB", palette.PointB, 0f, palette.PointSmoothness, point, neon ? point : null, palette.GlowB);
            TransparentMaterial(spec, "PointGlow", glow);
            LitMaterial(spec, "DieLight", Color.white, 0f, 0.78f, diceLight, diceLightGlow, palette.DiceGlow ? Color.white * 2.2f : default);
            LitMaterial(spec, "DieDark", Color.white, 0f, 0.78f, diceDark, diceDarkGlow, palette.DiceGlow ? Color.white * 2.2f : default);
            LitMaterial(spec, "Cube", Color.white, 0f, palette.CubeGlow ? 0.88f : 0.6f, cube, palette.CubeGlow ? cube : null, palette.CubeGlow ? Color.white * 0.9f : default);
            if (spec.Emissive)
            {
                // The draughts pieces glow whole; a stack of them would melt into one blob, so the glass checkers glow at the rim.
                Texture2D rim = BoardGamesAssets.SaveTexture(CheckerRim(), $"{textures}/{Prefix}CheckerRim.png", GameAssets.SurfaceTexture(false, 256, false));
                LitMaterial(spec, "CheckerLight", Color.Lerp(spec.LightPiece, spec.LightColor, 0.35f), 0.1f, 0.9f, null, rim, spec.LightGlow * 0.6f);
                LitMaterial(spec, "CheckerDark", Color.Lerp(spec.DarkPiece, spec.DarkColor, 0.35f), 0.1f, 0.9f, null, rim, spec.DarkGlow * 0.6f);
            }
        }

        /// <summary>The checkers of a look: the draughts pieces' materials, or for a glowing look glass with a neon rim.</summary>
        private static Material[] Checkers(BoardGamesThemeSpec spec)
        {
            return spec.Emissive
                ? new[] { Material("CheckerLight", spec), Material("CheckerDark", spec) }
                : new[] { BoardGamesArtBuilder.Material("LightPiece", spec), BoardGamesArtBuilder.Material("DarkPiece", spec) };
        }

        /// <summary>The checker material of <paramref name="side"/> in <paramref name="spec"/>'s look (the scene parts dress the checkers with it).</summary>
        public static Material CheckerMaterial(BoardGamesThemeSpec spec, Side side)
        {
            return Checkers(spec)[(int)side];
        }

        /// <summary>Fills <paramref name="theme"/>.backgammon from the assets of <paramref name="spec"/>'s look.</summary>
        public static void Dress(BoardGamesTheme theme, BoardGamesThemeSpec spec)
        {
            theme.backgammon = new BackgammonLook
            {
                frameMesh = Mesh("Frame"),
                feltMesh = Mesh("Felt"),
                pointMesh = Mesh("Point"),
                checkerMesh = Mesh("Checker"),
                dieMesh = Mesh("Die"),
                cubeMesh = Mesh("Cube"),
                frame = BoardGamesArtBuilder.Material("Slab", spec),
                felt = Material("Felt", spec),
                points = new[] { Material("PointA", spec), Material("PointB", spec) },
                pointGlow = Material("PointGlow", spec),
                checkers = Checkers(spec),
                dice = new[] { Material("DieLight", spec), Material("DieDark", spec) },
                cube = Material("Cube", spec)
            };
        }

        // ------------------------------------------------------------------ meshes

        /// <summary>The box: the rim on four sides, the bar, the divider before the trays and the split between them, from the table up to just over the felt.</summary>
        private static MeshBuilder FrameMesh()
        {
            float w = BackgammonLayout.HalfWidth;
            float d = BackgammonLayout.HalfDepth;
            float rim = BackgammonLayout.Rim;
            float field = BackgammonLayout.FieldDepth;
            float bottom = -BackgammonLayout.BaseDepth;
            float top = BackgammonLayout.FrameHeight;
            float split = BackgammonLayout.TraySplit * 0.5f;
            var mesh = new MeshBuilder();
            mesh.Append(Block(new Vector3(-w, bottom, -d), new Vector3(-w + rim, top, d)));
            mesh.Append(Block(new Vector3(w - rim, bottom, -d), new Vector3(w, top, d)));
            mesh.Append(Block(new Vector3(-w + rim, bottom, -d), new Vector3(w - rim, top, -field)));
            mesh.Append(Block(new Vector3(-w + rim, bottom, field), new Vector3(w - rim, top, d)));
            mesh.Append(Block(new Vector3(BackgammonLayout.BarMin, bottom, -field), new Vector3(BackgammonLayout.BarMax, top, field)));
            mesh.Append(Block(new Vector3(BackgammonLayout.RightMax, bottom, -field), new Vector3(BackgammonLayout.TrayMin, top, field)));
            mesh.Append(Block(new Vector3(BackgammonLayout.TrayMin, bottom, -split), new Vector3(BackgammonLayout.TrayMax, top, split)));
            return mesh;
        }

        /// <summary>
        /// A box between two corners whose faces map the wood texture in world units, the grain (the texture's v) along
        /// each face's longer side, offset by where the block is so neighbouring parts do not repeat one patch.
        /// </summary>
        private static MeshBuilder Block(Vector3 min, Vector3 max)
        {
            var mesh = new MeshBuilder();
            Vector3 center = (min + max) * 0.5f;
            Vector3 size = max - min;
            var offset = new Vector2(min.x * 0.37f + min.z * 0.11f, min.z * 0.29f + min.x * 0.07f);
            Vector3[] normals = { Vector3.up, Vector3.down, Vector3.right, Vector3.left, Vector3.forward, Vector3.back };
            foreach (Vector3 normal in normals)
            {
                Vector3 up = Mathf.Abs(normal.y) > 0.5f ? Vector3.forward : Vector3.up;
                Vector3 right = Vector3.Cross(normal, up);
                float width = Mathf.Abs(Vector3.Dot(right, size));
                float height = Mathf.Abs(Vector3.Dot(up, size));
                Vector3 c = center + Vector3.Scale(normal, size * 0.5f);
                Vector3 r = right * (width * 0.5f);
                Vector3 u = up * (height * 0.5f);
                bool wide = width >= height;
                Vector2 Uv(float s, float t)
                {
                    Vector2 uv = wide ? new Vector2(t * height, s * width) : new Vector2(s * width, t * height);
                    return (uv + offset) / WoodTile;
                }
                int a = mesh.Add(c - r - u, normal, Uv(0f, 0f));
                int b = mesh.Add(c + r - u, normal, Uv(1f, 0f));
                int e = mesh.Add(c + r + u, normal, Uv(1f, 1f));
                int f = mesh.Add(c - r + u, normal, Uv(0f, 1f));
                mesh.Quad(a, f, e, b);
            }
            return mesh;
        }

        /// <summary>A flat triangle facing up: its base one unit wide on z = 0, its tip at z = 1; uv (0, 0), (1, 0) and (0.5, 1).</summary>
        private static MeshBuilder PointMesh()
        {
            var mesh = new MeshBuilder();
            int a = mesh.Add(new Vector3(-0.5f, 0f, 0f), Vector3.up, new Vector2(0f, 0f));
            int tip = mesh.Add(new Vector3(0f, 0f, 1f), Vector3.up, new Vector2(0.5f, 1f));
            int b = mesh.Add(new Vector3(0.5f, 0f, 0f), Vector3.up, new Vector2(1f, 0f));
            mesh.Triangle(a, tip, b);
            return mesh;
        }

        /// <summary>A checker standing on y = 0: flat bottom, rounded rim, and on top a flat ring round a shallow groove and a low dish.</summary>
        private static MeshBuilder CheckerMesh()
        {
            return MeshFactory.Lathe(CheckerProfile(), 48);
        }

        /// <summary>The checker's profile, (radius, height) from the axis at the bottom round the rim to the axis at the top.</summary>
        private static List<Vector2> CheckerProfile()
        {
            float r = BackgammonLayout.CheckerRadius;
            float h = BackgammonLayout.CheckerHeight;
            float bevel = 0.022f;
            return new List<Vector2>
            {
                new Vector2(0f, 0f),
                new Vector2(r - bevel, 0f),
                new Vector2(r - bevel * 0.3f, bevel * 0.3f),
                new Vector2(r, bevel),
                new Vector2(r, h - bevel),
                new Vector2(r - bevel * 0.3f, h - bevel * 0.3f),
                new Vector2(r - bevel, h),
                new Vector2(r * 0.8f, h),
                new Vector2(r * 0.77f, h - 0.014f),
                new Vector2(r * 0.71f, h - 0.014f),
                new Vector2(r * 0.68f, h - 0.005f),
                new Vector2(r * 0.3f, h - 0.009f),
                new Vector2(0f, h - 0.009f)
            };
        }

        /// <summary>
        /// The neon rim of a glass checker, as an emission map on the lathe's v (the distance along the profile): bright
        /// round the edge and the flat top ring, dim elsewhere, so the checkers of a stack stay apart.
        /// </summary>
        private static Texture2D CheckerRim()
        {
            List<Vector2> profile = CheckerProfile();
            var distance = new float[profile.Count];
            for (int i = 1; i < profile.Count; i++)
            {
                distance[i] = distance[i - 1] + Vector2.Distance(profile[i - 1], profile[i]);
            }
            float length = distance[profile.Count - 1];
            // From the start of the side (point 3) to the inner edge of the top ring (point 7).
            float from = distance[3] / length;
            float to = distance[7] / length;
            const int height = 256;
            var raster = new Raster(8, height, Color.clear);
            for (int y = 0; y < height; y++)
            {
                float v = (y + 0.5f) / height;
                float outside = Mathf.Max(from - v, v - to, 0f);
                float a = Mathf.Lerp(0.14f, 1f, Mathf.Clamp01(1f - outside / 0.025f));
                for (int x = 0; x < 8; x++)
                {
                    raster.Set(x, y, new Color(a, a, a, 1f));
                }
            }
            return raster.ToTexture(false);
        }

        // ------------------------------------------------------------------ textures

        /// <summary>The felt of the field: cloth (a soft mottle and a fine weave) or, for slate, a cloudy speckle.</summary>
        private static Texture2D Felt(int size, Color color, bool slate, int seed)
        {
            var raster = new Raster(size, size, color);
            float ox = seed * 13.17f;
            float oy = seed * 5.31f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = x / (float)size;
                    float v = y / (float)size;
                    float n = slate
                        ? Mathf.PerlinNoise(ox + u * 5f, oy + v * 5f) * 0.7f + Mathf.PerlinNoise(ox + u * 21f, oy + v * 21f) * 0.3f
                        : Mathf.PerlinNoise(ox + u * 3.5f, oy + v * 3.5f) * 0.65f + Mathf.PerlinNoise(ox + u * 60f, oy + v * 60f) * 0.35f;
                    float t = n * 2f - 1f;
                    raster.Blend(x, y, t > 0f ? new Color(1f, 1f, 1f, 0.07f * t) : new Color(0f, 0f, 0f, -0.1f * t));
                    if (!slate && ((x >> 1) + (y >> 1)) % 2 == 0)
                    {
                        raster.Blend(x, y, new Color(0f, 0f, 0f, 0.035f));
                    }
                }
            }
            raster.Grain(new Rect(0f, 0f, size, size), slate ? 0.03f : 0.045f, seed);
            return raster.ToTexture(false);
        }

        /// <summary>
        /// The point's face, drawn white to be tinted: the triangle of the point's uv filled with a light grain and inlaid
        /// with a darker line along its edges; for neon (slate looks) a dim fill and bright edges that glow.
        /// </summary>
        private static Texture2D Point(bool neon)
        {
            int w = PointPixelsX;
            int h = PointPixelsY;
            var raster = new Raster(w, h, neon ? new Color(0.1f, 0.1f, 0.1f) : new Color(0.9f, 0.9f, 0.9f));
            var triangle = new List<Vector2> { new Vector2(0f, 0f), new Vector2(w, 0f), new Vector2(w * 0.5f, h) };
            var bounds = new Rect(0f, 0f, w, h);
            if (neon)
            {
                // A dark glass fill (the glowing checkers stand out on it) with bright edges and a glow fading inwards.
                raster.Fill(p => Sd.Polygon(p, triangle), p =>
                {
                    float inside = -Sd.Polygon(p, triangle);
                    return new Color(1f, 1f, 1f, Mathf.Exp(-inside / 6f) * 0.45f);
                }, bounds);
                raster.Fill(p => Sd.Outline(Sd.Polygon(p, triangle) + 4f, 1.8f), Color.white, bounds);
            }
            else
            {
                raster.Grain(bounds, 0.05f, 11);
                raster.Fill(p => Sd.Outline(Sd.Polygon(p, triangle) + 6f, 1.6f), new Color(0f, 0f, 0f, 0.28f), bounds);
                raster.Fill(p => Sd.Outline(Sd.Polygon(p, triangle) + 1.5f, 1.2f), new Color(0f, 0f, 0f, 0.18f), bounds);
            }
            return raster.ToTexture(false);
        }

        /// <summary>The glow over a marked point: white in the triangle of the point's uv, bright along the edges, soft inside.</summary>
        private static Texture2D PointGlow()
        {
            int w = PointPixelsX;
            int h = PointPixelsY;
            var raster = new Raster(w, h, Color.clear);
            var triangle = new List<Vector2> { new Vector2(2f, 2f), new Vector2(w - 2f, 2f), new Vector2(w * 0.5f, h - 3f) };
            raster.Fill(p => Sd.Polygon(p, triangle), p =>
            {
                float inside = -Sd.Polygon(p, triangle);
                return new Color(1f, 1f, 1f, 0.45f + 0.55f * Mathf.Exp(-inside / 7f));
            }, new Rect(0f, 0f, w, h), 2f);
            return raster.ToTexture();
        }

        /// <summary>The doubling cube's atlas: 2, 4, 8, 16, 32 and 64 on faces 1 to 6, drawn with strokes.</summary>
        private static Texture2D CubeAtlas(int cell, Palette palette)
        {
            string[] numbers = { "2", "4", "8", "16", "32", "64" };
            Color ink = palette.CubeInk;
            bool glow = palette.CubeGlow;
            return DiceArt.Atlas(cell, palette.CubeBody, (raster, value, center, size) =>
            {
                string number = numbers[value - 1];
                float height = size * (number.Length > 1 ? 0.42f : 0.5f);
                Number(raster, number, center, height, height * 0.15f, ink, glow);
            }, glow ? new Color(ink.r, ink.g, ink.b, 0.4f) : default);
        }

        /// <summary>Draws the digits of <paramref name="number"/> centred on <paramref name="center"/>, <paramref name="height"/> tall, in strokes of <paramref name="thickness"/>.</summary>
        private static void Number(Raster raster, string number, Vector2 center, float height, float thickness, Color ink, bool glow)
        {
            float unit = height / 1.6f;
            float gap = 0.32f;
            float width = number.Length + (number.Length - 1) * gap;
            var origin = new Vector2(center.x - width * unit * 0.5f, center.y - height * 0.5f);
            for (int i = 0; i < number.Length; i++)
            {
                var at = origin + new Vector2(i * (1f + gap) * unit, 0f);
                foreach (Vector2[] stroke in Strokes(number[i]))
                {
                    for (int k = 0; k + 1 < stroke.Length; k++)
                    {
                        Vector2 a = at + stroke[k] * unit;
                        Vector2 b = at + stroke[k + 1] * unit;
                        Rect bounds = Sd.Bounds(new[] { a, b });
                        bounds.xMin -= thickness * 3f;
                        bounds.yMin -= thickness * 3f;
                        bounds.xMax += thickness * 3f;
                        bounds.yMax += thickness * 3f;
                        if (glow)
                        {
                            raster.Fill(p => Sd.Segment(p, a, b, thickness * 1.6f), new Color(ink.r, ink.g, ink.b, 0.12f), bounds, thickness);
                        }
                        raster.Fill(p => Sd.Segment(p, a, b, thickness * 0.5f), ink, bounds);
                    }
                }
            }
        }

        /// <summary>The strokes of a digit in a box one unit wide and 1.6 tall (only the digits of the cube's numbers).</summary>
        private static IEnumerable<Vector2[]> Strokes(char digit)
        {
            switch (digit)
            {
                case '1':
                    yield return new[] { new Vector2(0.22f, 1.28f), new Vector2(0.56f, 1.6f), new Vector2(0.56f, 0f) };
                    break;
                case '2':
                    yield return Join(Arc(new Vector2(0.5f, 1.14f), 0.42f, 155f, -35f), new Vector2(0.06f, 0f), new Vector2(0.96f, 0f));
                    break;
                case '3':
                    yield return Arc(new Vector2(0.5f, 1.22f), 0.36f, 150f, -90f);
                    yield return Arc(new Vector2(0.5f, 0.44f), 0.44f, 90f, -150f);
                    break;
                case '4':
                    yield return new[] { new Vector2(0.72f, 0f), new Vector2(0.72f, 1.6f), new Vector2(0.04f, 0.5f), new Vector2(0.98f, 0.5f) };
                    break;
                case '6':
                    yield return Arc(new Vector2(0.5f, 0.46f), 0.44f, 0f, 360f);
                    yield return new[]
                    {
                        new Vector2(0.86f, 1.46f), new Vector2(0.64f, 1.6f), new Vector2(0.38f, 1.57f), new Vector2(0.2f, 1.4f),
                        new Vector2(0.09f, 1.08f), new Vector2(0.06f, 0.46f)
                    };
                    break;
                case '8':
                    yield return Arc(new Vector2(0.5f, 1.22f), 0.34f, 0f, 360f);
                    yield return Arc(new Vector2(0.5f, 0.44f), 0.44f, 0f, 360f);
                    break;
            }
        }

        private static Vector2[] Arc(Vector2 center, float radius, float fromDegrees, float toDegrees)
        {
            int steps = Mathf.Max(4, Mathf.CeilToInt(Mathf.Abs(toDegrees - fromDegrees) / 12f));
            var points = new Vector2[steps + 1];
            for (int i = 0; i <= steps; i++)
            {
                float angle = Mathf.Lerp(fromDegrees, toDegrees, i / (float)steps) * Mathf.Deg2Rad;
                points[i] = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            }
            return points;
        }

        private static Vector2[] Join(Vector2[] first, params Vector2[] more)
        {
            var points = new Vector2[first.Length + more.Length];
            first.CopyTo(points, 0);
            more.CopyTo(points, first.Length);
            return points;
        }

        // ------------------------------------------------------------------ materials

        /// <summary>A lit material; with an emission colour above black it glows (through <paramref name="emissionMap"/> when given).</summary>
        private static Material LitMaterial(BoardGamesThemeSpec spec, string name, Color color, float metallic, float smoothness, Texture2D texture,
            Texture2D emissionMap, Color emission)
        {
            bool glows = emission.maxColorComponent > 0f;
            return BoardGamesAssets.SaveMaterial($"{spec.MaterialsFolder}/{Prefix}{name}.mat", Lit, m =>
            {
                m.SetColor("_BaseColor", color);
                m.SetTexture("_BaseMap", texture);
                m.SetFloat("_Metallic", metallic);
                m.SetFloat("_Smoothness", smoothness);
                m.SetFloat("_Surface", 0f);
                m.SetTexture("_EmissionMap", glows ? emissionMap : null);
                m.SetColor("_EmissionColor", glows ? emission : Color.black);
                if (glows)
                {
                    m.EnableKeyword("_EMISSION");
                    m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                }
                else
                {
                    m.DisableKeyword("_EMISSION");
                    m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.EmissiveIsBlack;
                }
                m.enableInstancing = true;
            });
        }

        /// <summary>An unlit alpha blended material for glows, tinted per use through <c>_BaseColor</c>.</summary>
        private static Material TransparentMaterial(BoardGamesThemeSpec spec, string name, Texture2D texture)
        {
            return BoardGamesAssets.SaveMaterial($"{spec.MaterialsFolder}/{Prefix}{name}.mat", Unlit, m =>
            {
                m.SetColor("_BaseColor", Color.white);
                m.SetTexture("_BaseMap", texture);
                m.SetFloat("_Surface", 1f);
                m.SetFloat("_Blend", 0f);
                m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                if (m.HasProperty("_SrcBlendAlpha"))
                {
                    m.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
                    m.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
                }
                m.SetFloat("_ZWrite", 0f);
                m.SetFloat("_Cull", (float)CullMode.Back);
                m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                m.SetOverrideTag("RenderType", "Transparent");
                m.renderQueue = (int)RenderQueue.Transparent;
                m.enableInstancing = true;
            });
        }
    }
}
