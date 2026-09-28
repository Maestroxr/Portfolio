using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TextCore;
using UnityEngine.TextCore.LowLevel;
using Object = UnityEngine.Object;

namespace Portfolio.Asteroids.EditorTools
{
    /// <summary>
    /// A generated look of the game, described once: its name and folders, its palette, its fonts and the colour every
    /// kind of material takes. The builders derive the look's art from the Classic art with it (AsteroidsArtBuilder.Theme.cs),
    /// build its ground (the Grid world) and its interface sprites, and AsteroidsThemeBuilder gathers everything into the
    /// theme asset.
    /// </summary>
    internal sealed class ThemeSpec
    {
        public string Name;
        public string DisplayName;
        public string Description;
        /// <summary>The font files under Fonts/ (without .ttf) of the titles and of everything else.</summary>
        public string TitleFont;
        public string BodyFont;
        /// <summary>
        /// How big the fonts draw at a given size (TextMesh Pro's face scale): a wide font drawn smaller fills the boxes the
        /// interface was laid out for with the default font.
        /// </summary>
        public float TitleScale = 1f;
        public float BodyScale = 1f;
        public Color Night = SpaceTextures.Hex("07061A");
        public Color Magenta = SpaceTextures.Hex("FF2BD6");
        public Color Cyan = SpaceTextures.Hex("19E6FF");
        public Color Violet = SpaceTextures.Hex("7B2FF7");
        public Color Orange = SpaceTextures.Hex("FF7A1A");
        public Color Yellow = SpaceTextures.Hex("FFE94A");
        public Color Green = SpaceTextures.Hex("39FF88");
        public Color Pink = SpaceTextures.Hex("FF3B6B");
        public Color White = SpaceTextures.Hex("E8F6FF");
        /// <summary>How bright the neon edges are (over 1 they bloom).</summary>
        public float EdgeIntensity = 2.2f;
        /// <summary>The wire copies draw the edges where the faces turn by more than this many degrees (and open borders) bright.</summary>
        public float WireAngle = 25f;
        /// <summary>
        /// How bright the other edges of the wire copies are drawn (0 hides them), at most: a mesh of more triangles than
        /// <see cref="InnerDensity"/> draws them dimmer in proportion, so dense models do not fill in with lines.
        /// </summary>
        public float InnerEdges = 0.3f;
        public int InnerDensity = 2000;
        /// <summary>Words of the keys of lit materials that draw no wire (the tumbling debris particles, whose colours are no coordinates).</summary>
        public string[] NoWire = { "Debris" };
        /// <summary>The edge colour of a lit material by a word of its key, the first match winning; the base colour's hue otherwise.</summary>
        public (string word, string color)[] Edges = new (string, string)[0];
        /// <summary>The line colour of an icon by a word of its name, the first match winning; a pale cyan otherwise.</summary>
        public (string word, string color)[] Lines = new (string, string)[0];

        public string ArtFolder => $"Art/Themes/{Name}";
        public string PrefabFolder => $"Prefabs/Themes/{Name}";
        public string ConfigFolder => $"Config/Themes/{Name}";

        /// <summary>A colour of the palette by name (Magenta, Cyan, Violet, Orange, Yellow, Green, Pink, White, Night; Dim is a dark violet).</summary>
        public Color Named(string color)
        {
            switch (color)
            {
                case "Magenta": return Magenta;
                case "Cyan": return Cyan;
                case "Violet": return Violet;
                case "Orange": return Orange;
                case "Yellow": return Yellow;
                case "Green": return Green;
                case "Pink": return Pink;
                case "White": return White;
                case "Night": return Night;
                case "Dim": return Color.Lerp(Violet, Night, 0.35f);
                default: return Cyan;
            }
        }


        /// <summary>The neon colour nearest in hue to <paramref name="color"/>, as bright (HDR included) and as transparent as it.</summary>
        public Color Neon(Color color)
        {
            float intensity = Mathf.Max(color.r, Mathf.Max(color.g, color.b));
            if (intensity <= 0.001f)
            {
                return color;
            }
            Color.RGBToHSV(new Color(color.r / intensity, color.g / intensity, color.b / intensity), out float hue, out float saturation, out _);
            Color nearest;
            if (saturation < 0.2f)
            {
                nearest = Color.Lerp(White, Cyan, 0.25f);
            }
            else
            {
                nearest = Magenta;
                float best = float.MaxValue;
                foreach (Color candidate in new[] { Magenta, Violet, Cyan, Green, Yellow, Orange, Pink })
                {
                    Color.RGBToHSV(candidate, out float candidateHue, out _, out _);
                    float distance = Mathf.Abs(Mathf.DeltaAngle(hue * 360f, candidateHue * 360f));
                    if (distance < best)
                    {
                        best = distance;
                        nearest = candidate;
                    }
                }
            }
            return new Color(nearest.r * intensity, nearest.g * intensity, nearest.b * intensity, color.a);
        }


        /// <summary>The first colour of <paramref name="table"/> whose word occurs in <paramref name="text"/>; null when none does.</summary>
        public Color? Match((string word, string color)[] table, string text)
        {
            foreach ((string word, string color) in table)
            {
                if (text.IndexOf(word, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return Named(color);
                }
            }
            return null;
        }
    }


    /// <summary>
    /// The art of a generated theme, derived from the Classic art: the interface sprites and icons traced as neon lines,
    /// scan lines and a CRT vignette for the interface, the fonts baked for TextMesh Pro, a neon material for every
    /// material of the Classic art (by the kind of thing it is), wire copies of the models for the edge shader, the
    /// hangar pictures rendered again, and the neon grid ground of the strike mode.
    /// </summary>
    internal static partial class AsteroidsArtBuilder
    {
        /// <summary>The Arcade 80s theme: a vector arcade in synthwave colours.</summary>
        public static readonly ThemeSpec Arcade = new ThemeSpec
        {
            Name = "Arcade80s",
            DisplayName = "Arcade 80s",
            Description = "A vector arcade cabinet of 1985: neon wireframe ships and rocks, shots like bright lines, a synthwave grid under a striped sun, scan lines over the screen.",
            TitleFont = "PressStart2P",
            TitleScale = 0.72f,
            BodyScale = 0.86f,
            BodyFont = "Orbitron",
            Edges = new[]
            {
                ("PlanetKepler", "Cyan"), ("PlanetFrost", "White"), ("PlanetVoid", "Violet"), ("Planet", "Magenta"), ("Cloud", "Dim"),
                ("Titan", "Magenta"), ("Rock", "Violet"), ("Ore", "Yellow"), ("Magma", "Orange"), ("Debris", "Violet"),
                ("Ice", "Cyan"), ("Crystal", "Magenta"), ("HiveQueen", "Magenta"), ("Wasp", "Magenta"), ("Insect", "Magenta"),
                ("Mine", "Pink"), ("Missile", "Orange"), ("Materials/Palette", "Green"),
                ("Blue", "Cyan"), ("Yellow", "Yellow"), ("Grey", "White"), ("Gray", "White"), ("Steel", "White"), ("Purple", "Violet"), ("Violet", "Violet"),
                ("Crimson", "Magenta"), ("Red", "Magenta"), ("Olive", "Green"), ("Green", "Green"), ("Sand", "Yellow"), ("Tan", "Yellow"),
                ("Rust", "Orange"), ("Orange", "Orange"), ("Black", "Dim"), ("Night", "Dim"), ("Basalt", "Dim"), ("Ground", "Green"), ("Strike", "Cyan")
            },
            Lines = new[]
            {
                ("StarEmpty", "Dim"), ("Star", "Yellow"), ("Skull", "Magenta"), ("Warning", "Magenta"), ("Elite", "Magenta"), ("Lock", "Violet"),
                ("Money", "Yellow"), ("CreditOrb", "Yellow"), ("Rookie", "Green"), ("Veteran", "Orange"), ("Energy", "Green"), ("Check", "Green")
            }
        };

        /// <summary>Every generated theme the builders make art for.</summary>
        public static ThemeSpec[] GeneratedThemes => new[] { Arcade };

        /// <summary>The interface sprites that are traced as neon lines (the others, soft glows and bars, serve every theme as they are).</summary>
        private static readonly (string name, Func<Texture2D> draw, float border)[] NeonInterface =
        {
            ("Panel", () => SpaceIcons.Panel(false), 18f), ("Button", () => SpaceIcons.Panel(true), 18f), ("Frame", SpaceIcons.Frame, 20f),
            ("Hexagon", () => SpaceIcons.Hexagon(false), 0f), ("HexagonFrame", () => SpaceIcons.Hexagon(true), 0f)
        };

        private const string FontCharacters = " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~" +
            "\u00A0\u00B0\u00D7\u00B7\u2013\u2014\u2026\u2018\u2019\u201C\u201D\u00A9";

        public static Shader NeonSurfaceShader(ThemeSpec spec)
        {
            return AssetDatabase.LoadAssetAtPath<Shader>(AsteroidsAssets.Path($"{spec.ArtFolder}/Shaders/NeonSurface.shader"));
        }

        public static Shader SynthwaveShader(ThemeSpec spec)
        {
            return AssetDatabase.LoadAssetAtPath<Shader>(AsteroidsAssets.Path($"{spec.ArtFolder}/Shaders/SynthwaveBackground.shader"));
        }

        public static Shader NeonGroundShader(ThemeSpec spec)
        {
            return AssetDatabase.LoadAssetAtPath<Shader>(AsteroidsAssets.Path($"{spec.ArtFolder}/Shaders/NeonGround.shader"));
        }


        /// <summary>The art step's part of the generated themes: fonts, neon sprites, the overlays and the clear sprite of the Classic look.</summary>
        private static void BuildThemeArt()
        {
            AsteroidsAssets.SaveTexture(SpaceTextures.Make(64, (x, y) => new Color(0f, 0f, 0f, 0f)), "Art/Interface/Clear.png", Flat2D(AsteroidsAssets.SpriteTexture(Vector4.zero)));
            foreach (ThemeSpec spec in GeneratedThemes)
            {
                if (NeonSurfaceShader(spec) == null || SynthwaveShader(spec) == null || NeonGroundShader(spec) == null)
                {
                    AsteroidsAssets.Problem($"The shaders of the {spec.DisplayName} theme are missing under {spec.ArtFolder}/Shaders.");
                    continue;
                }
                BuildThemeFonts(spec);
                BuildNeonSprites(spec);
                BuildOverlays(spec);
            }
        }

        // ------------------------------------------------------------------ fonts

        public static string FontAssetPath(ThemeSpec spec, string file)
        {
            return $"{spec.ArtFolder}/Fonts/{file} SDF.asset";
        }

        public static TMP_FontAsset ThemeFont(ThemeSpec spec, string file)
        {
            return AsteroidsAssets.Load<TMP_FontAsset>(FontAssetPath(spec, file));
        }

        public static Material ThemeFontMaterial(ThemeSpec spec, TextRoleName role)
        {
            return AsteroidsAssets.Load<Material>($"{spec.ArtFolder}/Fonts/{spec.Name} {role}.mat");
        }

        /// <summary>The two font materials of a generated theme.</summary>
        public enum TextRoleName
        {
            Title,
            Hud
        }


        /// <summary>
        /// Bakes the theme's fonts into static TextMesh Pro font assets (atlas and material as sub-assets; created only when
        /// missing, so a rebuild keeps their GUIDs) and makes their materials: a glowing outlined one for titles and an
        /// outlined one for the HUD.
        /// </summary>
        private static void BuildThemeFonts(ThemeSpec spec)
        {
            TMP_FontAsset title = BakeFont(spec, spec.TitleFont, spec.TitleScale);
            TMP_FontAsset body = BakeFont(spec, spec.BodyFont, spec.BodyScale);
            if (title == null || body == null)
            {
                return;
            }
            Color glow = spec.Magenta;
            AsteroidsAssets.SaveMaterial($"{spec.ArtFolder}/Fonts/{spec.Name} {TextRoleName.Title}.mat", title.material.shader, m =>
            {
                m.CopyPropertiesFromMaterial(title.material);
                m.EnableKeyword("OUTLINE_ON");
                m.EnableKeyword("UNDERLAY_ON");
                m.SetFloat("_FaceDilate", 0.06f);
                m.SetFloat("_OutlineWidth", 0.16f);
                m.SetColor("_OutlineColor", new Color(spec.Night.r, spec.Night.g, spec.Night.b, 1f));
                m.SetColor("_UnderlayColor", new Color(glow.r, glow.g, glow.b, 0.7f));
                m.SetFloat("_UnderlayOffsetX", 0f);
                m.SetFloat("_UnderlayOffsetY", 0f);
                m.SetFloat("_UnderlayDilate", 0.75f);
                m.SetFloat("_UnderlaySoftness", 0.7f);
            });
            AsteroidsAssets.SaveMaterial($"{spec.ArtFolder}/Fonts/{spec.Name} {TextRoleName.Hud}.mat", body.material.shader, m =>
            {
                m.CopyPropertiesFromMaterial(body.material);
                m.EnableKeyword("OUTLINE_ON");
                m.DisableKeyword("UNDERLAY_ON");
                m.SetFloat("_FaceDilate", 0.12f);
                m.SetFloat("_OutlineWidth", 0.18f);
                m.SetColor("_OutlineColor", new Color(spec.Night.r, spec.Night.g, spec.Night.b, 1f));
            });
        }


        private static TMP_FontAsset BakeFont(ThemeSpec spec, string file, float scale)
        {
            string path = FontAssetPath(spec, file);
            TMP_FontAsset asset = AsteroidsAssets.Load<TMP_FontAsset>(path);
            if (asset == null)
            {
                var source = AsteroidsAssets.Load<Font>($"{spec.ArtFolder}/Fonts/{file}.ttf");
                if (source == null)
                {
                    AsteroidsAssets.Problem($"{file}.ttf is missing under {spec.ArtFolder}/Fonts.");
                    return null;
                }
                asset = TMP_FontAsset.CreateFontAsset(source, 72, 9, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, false);
                asset.name = $"{file} SDF";
                asset.TryAddCharacters(FontCharacters, out string missing);
                if (!string.IsNullOrEmpty(missing))
                {
                    Debug.Log($"Asteroids: {file} has no glyphs for \"{missing}\"; the default font fills in.");
                }
                asset.atlasPopulationMode = AtlasPopulationMode.Static;
                AsteroidsAssets.EnsureFolder($"{spec.ArtFolder}/Fonts");
                AssetDatabase.CreateAsset(asset, AsteroidsAssets.Path(path));
                Texture2D atlas = asset.atlasTexture;
                atlas.name = $"{file} SDF Atlas";
                AssetDatabase.AddObjectToAsset(atlas, asset);
                Material material = asset.material;
                material.name = $"{file} SDF Material";
                AssetDatabase.AddObjectToAsset(material, asset);
                EditorUtility.SetDirty(asset);
                AssetDatabase.SaveAssets();
                AssetDatabase.ImportAsset(AsteroidsAssets.Path(path));
                asset = AsteroidsAssets.Load<TMP_FontAsset>(path);
            }
            if (asset == null)
            {
                return null;
            }
            TMP_FontAsset fallback = TMP_Settings.defaultFontAsset;
            if (fallback != null && (asset.fallbackFontAssetTable == null || !asset.fallbackFontAssetTable.Contains(fallback)))
            {
                asset.fallbackFontAssetTable = new List<TMP_FontAsset> { fallback };
                EditorUtility.SetDirty(asset);
            }
            FaceInfo face = asset.faceInfo;
            if (!Mathf.Approximately(face.scale, scale))
            {
                face.scale = scale;
                asset.faceInfo = face;
                EditorUtility.SetDirty(asset);
            }
            return asset;
        }

        // ------------------------------------------------------------------ sprites

        /// <summary>A sprite of a generated theme by the key of the Classic sprite it stands in for ("Icons/Star"), or null when the theme keeps the Classic one.</summary>
        public static Sprite ThemeSprite(ThemeSpec spec, string key)
        {
            return AsteroidsAssets.Load<Sprite>($"{spec.ArtFolder}/{key}.png");
        }


        /// <summary>
        /// The panels, frames, hexagons and every icon of the game traced as neon lines: the Classic drawing is made again
        /// and its edges (the outline and the changes of tone inside) become bright lines with a soft halo over a dark,
        /// half transparent fill. The panels' lines are white, so the interface tints them; the icons take a colour of the
        /// theme.
        /// </summary>
        private static void BuildNeonSprites(ThemeSpec spec)
        {
            foreach ((string name, Func<Texture2D> draw, float border) in NeonInterface)
            {
                SaveNeon(spec, draw(), $"Interface/{name}", Vector4.one * border, Color.white, 0.9f);
            }
            foreach (string name in SpaceIcons.IconNames)
            {
                SaveNeon(spec, SpaceIcons.Icon(name), $"Icons/{name}", Vector4.zero, LineColor(spec, name), 0.5f);
            }
            SaveNeon(spec, SpaceIcons.GameIcon(), "Icons/GameIcon", Vector4.zero, spec.Cyan, 0.9f);
            foreach (StrikeItem item in (StrikeItem[])Enum.GetValues(typeof(StrikeItem)))
            {
                SaveNeon(spec, SpaceIcons.StrikeItemIcon(item), $"Strike/Icons/{item}", Vector4.zero, LineColor(spec, item.ToString()), 0.5f);
            }
            foreach (string name in SpaceIcons.StrikeIconNames)
            {
                SaveNeon(spec, SpaceIcons.StrikeIcon(name), $"Strike/Icons/{name}", Vector4.zero, LineColor(spec, name), 0.5f);
            }
        }


        private static Color LineColor(ThemeSpec spec, string name)
        {
            return spec.Match(spec.Lines, name) ?? Color.Lerp(spec.Cyan, spec.White, 0.35f);
        }


        private static void SaveNeon(ThemeSpec spec, Texture2D drawing, string relative, Vector4 border, Color line, float fillAlpha)
        {
            Texture2D neon = Neonize(drawing, line, spec.Night, fillAlpha);
            Object.DestroyImmediate(drawing);
            AsteroidsAssets.SaveTexture(neon, $"{spec.ArtFolder}/{relative}.png", Flat2D(AsteroidsAssets.SpriteTexture(border)));
        }


        /// <summary>
        /// Traces <paramref name="source"/> as neon: the edges of its coverage and of its tones become lines of
        /// <paramref name="line"/> two pixels wide with a soft halo, and what is inside becomes <paramref name="fill"/> at
        /// <paramref name="fillAlpha"/>.
        /// </summary>
        public static Texture2D Neonize(Texture2D source, Color line, Color fill, float fillAlpha)
        {
            int width = source.width;
            int height = source.height;
            Color[] pixels = source.GetPixels();
            var value = new float[pixels.Length];
            var inside = new float[pixels.Length];
            for (int i = 0; i < pixels.Length; i++)
            {
                Color pixel = pixels[i];
                float luma = 0.3f * pixel.r + 0.59f * pixel.g + 0.11f * pixel.b;
                inside[i] = pixel.a;
                value[i] = pixel.a * (0.35f + 0.65f * luma);
            }
            float At(float[] field, int x, int y)
            {
                return field[Mathf.Clamp(y, 0, height - 1) * width + Mathf.Clamp(x, 0, width - 1)];
            }
            var edge = new float[pixels.Length];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float gx = At(value, x + 1, y) - At(value, x - 1, y);
                    float gy = At(value, x, y + 1) - At(value, x, y - 1);
                    float gradient = Mathf.Sqrt(gx * gx + gy * gy) * 0.5f;
                    edge[y * width + x] = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.05f, 0.3f, gradient));
                }
            }
            int thick = Mathf.Max(1, width / 128);
            var lines = new float[pixels.Length];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float best = 0f;
                    for (int dy = -thick; dy <= thick; dy++)
                    {
                        for (int dx = -thick; dx <= thick; dx++)
                        {
                            best = Mathf.Max(best, At(edge, x + dx, y + dy));
                        }
                    }
                    lines[y * width + x] = best;
                }
            }
            int reach = Mathf.Max(3, width / 32);
            var halo = new float[pixels.Length];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float sum = 0f;
                    float weight = 0f;
                    for (int dy = -reach; dy <= reach; dy++)
                    {
                        for (int dx = -reach; dx <= reach; dx++)
                        {
                            float w = 1f - Mathf.Sqrt(dx * dx + dy * dy) / (reach + 1f);
                            if (w <= 0f)
                            {
                                continue;
                            }
                            sum += At(lines, x + dx, y + dy) * w;
                            weight += w;
                        }
                    }
                    halo[y * width + x] = weight > 0f ? sum / weight : 0f;
                }
            }
            var result = new Color[pixels.Length];
            Color haloColor = Color.Lerp(line, Color.white, 0.15f);
            for (int i = 0; i < pixels.Length; i++)
            {
                float lineAlpha = lines[i];
                float haloAlpha = Mathf.Clamp01(halo[i] * 0.9f) * (1f - lineAlpha);
                float fillPart = inside[i] * fillAlpha * (1f - lineAlpha - haloAlpha * 0.5f);
                float alpha = Mathf.Clamp01(lineAlpha + haloAlpha + fillPart);
                if (alpha <= 0.002f)
                {
                    result[i] = new Color(0f, 0f, 0f, 0f);
                    continue;
                }
                Color color = (line * lineAlpha + haloColor * haloAlpha + fill * fillPart) / alpha;
                color.a = alpha;
                result[i] = color;
            }
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.SetPixels(result);
            texture.Apply();
            return texture;
        }


        /// <summary>The overlays of a generated theme's interface: tiled scan lines and the dark corners of a CRT screen.</summary>
        private static void BuildOverlays(ThemeSpec spec)
        {
            AsteroidsAssets.SaveTexture(SpaceTextures.Make(64, (x, y) =>
            {
                int row = y % 3;
                return new Color(0f, 0f, 0f, row == 0 ? 0.3f : row == 1 ? 0.08f : 0f);
            }), $"{spec.ArtFolder}/Interface/Scanlines.png", Flat2D(importer =>
            {
                AsteroidsAssets.SpriteTexture(Vector4.zero)(importer);
                importer.wrapMode = TextureWrapMode.Repeat;
                importer.filterMode = FilterMode.Point;
            }));
            AsteroidsAssets.SaveTexture(SpaceTextures.Make(256, (x, y) =>
            {
                float u = (x + 0.5f) / 256f * 2f - 1f;
                float v = (y + 0.5f) / 256f * 2f - 1f;
                // The rounded rectangle of a tube: clear inside, darkening only towards the corners.
                float round = Mathf.Pow(Mathf.Pow(Mathf.Abs(u), 6f) + Mathf.Pow(Mathf.Abs(v), 6f), 1f / 6f);
                float dark = SpaceTextures.Step(0.88f, 1.2f, round) * 0.7f;
                float bleed = SpaceTextures.Step(0.96f, 1.12f, round) * 0.15f;
                return new Color(spec.Violet.r * bleed, spec.Violet.g * bleed, spec.Violet.b * bleed, Mathf.Clamp01(dark + bleed));
            }), $"{spec.ArtFolder}/Interface/Crt.png", Flat2D(AsteroidsAssets.SpriteTexture(Vector4.zero)));
        }

        // ------------------------------------------------------------------ materials

        /// <summary>
        /// The theme's material for <paramref name="classic"/>, filed under <paramref name="key"/>: a glow keeps its texture
        /// (an icon becomes the theme's icon) in the nearest neon colour, a shield its hex pattern in neon, the space quad
        /// takes the synthwave sky, a lit model becomes a dark hull with edges in the colour the spec gives its kind, and
        /// what a theme cannot re-draw (shadows, the black hole, the Classic ground) stays as it is.
        /// </summary>
        public static Material ThemeMaterial(ThemeSpec spec, Material classic, string key)
        {
            if (classic == null || classic.shader == null)
            {
                return classic;
            }
            string file = $"{spec.ArtFolder}/Materials/{FileName(key, "Materials")}.mat";
            switch (classic.shader.name)
            {
                case "Portfolio/Asteroids/Glow":
                    return AsteroidsAssets.SaveMaterial(file, classic.shader, m =>
                    {
                        m.CopyPropertiesFromMaterial(classic);
                        m.renderQueue = classic.renderQueue;
                        string textureKey = ThemeKeys.TextureKey(classic.GetTexture("_BaseMap"));
                        if (textureKey != null && (textureKey.StartsWith("Icons/", StringComparison.Ordinal) || textureKey.StartsWith("Strike/Icons/", StringComparison.Ordinal)))
                        {
                            Texture2D icon = AsteroidsAssets.Load<Texture2D>($"{spec.ArtFolder}/{textureKey}.png");
                            if (icon != null)
                            {
                                m.SetTexture("_BaseMap", icon);
                            }
                        }
                        m.SetColor("_BaseColor", spec.Neon(classic.GetColor("_BaseColor")));
                    });
                case "Portfolio/Asteroids/Shield":
                    return AsteroidsAssets.SaveMaterial(file, classic.shader, m =>
                    {
                        m.CopyPropertiesFromMaterial(classic);
                        m.renderQueue = classic.renderQueue;
                        m.SetColor("_BaseColor", spec.Neon(classic.GetColor("_BaseColor")));
                    });
                case "Portfolio/Asteroids/SpaceBackground":
                    return AsteroidsAssets.SaveMaterial(file, SynthwaveShader(spec), m =>
                    {
                        m.SetColor("_SpaceColor", spec.Night);
                        m.SetColor("_NebulaColor", spec.Cyan * 1.8f);
                        m.SetColor("_HighlightColor", spec.Yellow * 2.2f);
                        m.SetColor("_DustColor", spec.Magenta * 0.9f);
                        m.SetFloat("_StarDensity", 1f);
                    });
                case "Portfolio/Asteroids/Ground":
                case "Portfolio/Asteroids/NeonGround":
                case "Portfolio/Asteroids/StrikeShadow":
                case "Universal Render Pipeline/Unlit":
                    return classic;
                default:
                    Color edge = EdgeColor(spec, key, classic);
                    bool globe = key.IndexOf("Planet", StringComparison.OrdinalIgnoreCase) >= 0 && key.IndexOf("Ring", StringComparison.OrdinalIgnoreCase) < 0;
                    bool wire = !globe && Array.TrueForAll(spec.NoWire, word => key.IndexOf(word, StringComparison.OrdinalIgnoreCase) < 0);
                    return AsteroidsAssets.SaveMaterial(file, NeonSurfaceShader(spec), m =>
                    {
                        m.SetColor("_EdgeColor", new Color(edge.r * spec.EdgeIntensity, edge.g * spec.EdgeIntensity, edge.b * spec.EdgeIntensity, 1f));
                        m.SetColor("_FillColor", new Color(spec.Night.r + edge.r * 0.05f, spec.Night.g + edge.g * 0.05f, spec.Night.b + edge.b * 0.05f, 1f));
                        m.SetFloat("_EdgeWidth", globe ? 1.2f : 1.4f);
                        m.SetFloat("_Wire", wire ? 1f : 0f);
                        m.SetFloat("_Grid", globe ? 1f : 0f);
                        m.SetVector("_GridScale", new Vector4(24f, 12f, 0f, 0f));
                        m.SetFloat("_Rim", globe ? 0.9f : 0.35f);
                        m.SetFloat("_RimPower", globe ? 2.5f : 3f);
                        m.SetFloat("_Pulse", globe ? 0.05f : 0.2f);
                        m.SetColor("_EmissionColor", Color.black);
                    });
            }
        }


        /// <summary>The edge colour of a lit material: the spec's word for its key, else the neon nearest its base colour.</summary>
        private static Color EdgeColor(ThemeSpec spec, string key, Material classic)
        {
            Color? named = spec.Match(spec.Edges, key);
            if (named.HasValue)
            {
                return named.Value;
            }
            Color baseColor = classic.HasProperty("_BaseColor") ? classic.GetColor("_BaseColor") : classic.HasProperty("_Color") ? classic.GetColor("_Color") : Color.white;
            Color neon = spec.Neon(baseColor);
            float intensity = Mathf.Max(neon.r, Mathf.Max(neon.g, neon.b));
            return intensity > 0.001f ? new Color(neon.r / intensity, neon.g / intensity, neon.b / intensity, 1f) : spec.Cyan;
        }


        /// <summary>A file name for a keyed asset: the key's segments other than <paramref name="drop"/>, joined by spaces.</summary>
        public static string FileName(string key, string drop)
        {
            var name = new StringBuilder();
            foreach (string segment in key.Replace('#', '/').Split('/'))
            {
                if (segment.Length == 0 || string.Equals(segment, drop, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                if (name.Length > 0)
                {
                    name.Append(' ');
                }
                name.Append(segment);
            }
            return name.ToString();
        }

        // ------------------------------------------------------------------ meshes

        /// <summary>
        /// The theme's copy of <paramref name="source"/> for the edge shader, saved under Models/: every triangle with
        /// vertices of its own, its corners coloured with barycentric coordinates and its edges given a strength (see
        /// <see cref="MakeWire"/>). An existing copy is updated in place when its geometry, colours or strengths changed.
        /// </summary>
        public static Mesh WireMesh(ThemeSpec spec, Mesh source, string key)
        {
            if (source == null)
            {
                return null;
            }
            string relative = $"{spec.ArtFolder}/Models/{FileName(key, "Meshes")}.asset";
            int triangles = 0;
            for (int sub = 0; sub < source.subMeshCount; sub++)
            {
                triangles += (int)source.GetIndexCount(sub) / 3;
            }
            float inner = spec.InnerEdges * Mathf.Clamp01(spec.InnerDensity / (float)Mathf.Max(1, triangles));
            Mesh wire = MakeWire(source, $"{source.name} Wire", spec.WireAngle, inner);
            string path = AsteroidsAssets.Path(relative);
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing == null)
            {
                AsteroidsAssets.EnsureFolder(relative.Substring(0, relative.LastIndexOf('/')));
                AssetDatabase.CreateAsset(wire, path);
                return wire;
            }
            bool same = existing.vertexCount == wire.vertexCount && existing.vertices.SequenceEqual(wire.vertices)
                && existing.triangles.SequenceEqual(wire.triangles) && existing.colors.SequenceEqual(wire.colors) && Strengths(existing).SequenceEqual(Strengths(wire));
            if (!same)
            {
                EditorUtility.CopySerialized(wire, existing);
                existing.name = System.IO.Path.GetFileNameWithoutExtension(relative);
                EditorUtility.SetDirty(existing);
            }
            Object.DestroyImmediate(wire);
            return existing;
        }


        /// <summary>
        /// A copy of <paramref name="source"/> whose triangles have vertices of their own, so the edge shader can draw its
        /// edges: the colours of a triangle's corners are the barycentric coordinates (red, green, blue at the three corners;
        /// alpha 0 tells the shader they are coordinates, a mesh without colours being white), and the second UV set holds,
        /// the same at the three corners, how bright the edge facing each corner is drawn: 1 for a feature edge (the border
        /// of an open surface, a crease where the faces on either side turn by more than <paramref name="angle"/> degrees),
        /// <paramref name="inner"/> for the others.
        /// </summary>
        private static Mesh MakeWire(Mesh source, string name, float angle, float inner)
        {
            Vector3[] positions = source.vertices;
            Vector3[] normals = source.normals;
            Vector2[] uvs = source.uv;
            // Corners at the same place are one corner, whatever their normals and UVs.
            var welded = new int[positions.Length];
            var places = new Dictionary<Vector3Int, int>();
            for (int i = 0; i < positions.Length; i++)
            {
                Vector3 p = positions[i];
                var cell = new Vector3Int(Mathf.RoundToInt(p.x * 10000f), Mathf.RoundToInt(p.y * 10000f), Mathf.RoundToInt(p.z * 10000f));
                if (!places.TryGetValue(cell, out int id))
                {
                    id = places.Count;
                    places[cell] = id;
                }
                welded[i] = id;
            }
            var all = new List<int>();
            for (int sub = 0; sub < source.subMeshCount; sub++)
            {
                all.AddRange(source.GetTriangles(sub));
            }
            int count = all.Count / 3;
            var faceNormals = new Vector3[count];
            var faces = new Dictionary<long, List<int>>();
            long EdgeOf(int a, int b)
            {
                return a < b ? ((long)a << 32) | (uint)b : ((long)b << 32) | (uint)a;
            }
            for (int t = 0; t < count; t++)
            {
                Vector3 a = positions[all[t * 3]];
                Vector3 b = positions[all[t * 3 + 1]];
                Vector3 c = positions[all[t * 3 + 2]];
                faceNormals[t] = Vector3.Cross(b - a, c - a).normalized;
                for (int corner = 0; corner < 3; corner++)
                {
                    long edge = EdgeOf(welded[all[t * 3 + (corner + 1) % 3]], welded[all[t * 3 + (corner + 2) % 3]]);
                    if (!faces.TryGetValue(edge, out List<int> list))
                    {
                        list = new List<int>(2);
                        faces[edge] = list;
                    }
                    list.Add(t);
                }
            }
            float crease = Mathf.Cos(angle * Mathf.Deg2Rad);
            bool Drawn(int t, int corner)
            {
                long edge = EdgeOf(welded[all[t * 3 + (corner + 1) % 3]], welded[all[t * 3 + (corner + 2) % 3]]);
                List<int> sharing = faces[edge];
                if (sharing.Count < 2)
                {
                    return true;
                }
                foreach (int other in sharing)
                {
                    if (other != t && Vector3.Dot(faceNormals[t], faceNormals[other]) < crease)
                    {
                        return true;
                    }
                }
                return false;
            }
            var vertices = new List<Vector3>();
            var outNormals = new List<Vector3>();
            var outUvs = new List<Vector2>();
            var strengths = new List<Vector3>();
            var colors = new List<Color>();
            var submeshes = new List<int[]>();
            int first = 0;
            for (int sub = 0; sub < source.subMeshCount; sub++)
            {
                int[] triangles = source.GetTriangles(sub);
                var indices = new int[triangles.Length];
                for (int i = 0; i < triangles.Length; i += 3)
                {
                    int t = first + i / 3;
                    var strength = new Vector3(Drawn(t, 0) ? 1f : inner, Drawn(t, 1) ? 1f : inner, Drawn(t, 2) ? 1f : inner);
                    for (int corner = 0; corner < 3; corner++)
                    {
                        int index = triangles[i + corner];
                        indices[i + corner] = vertices.Count;
                        vertices.Add(positions[index]);
                        if (normals.Length == positions.Length)
                        {
                            outNormals.Add(normals[index]);
                        }
                        outUvs.Add(uvs.Length == positions.Length ? uvs[index] : Vector2.zero);
                        colors.Add(new Color(corner == 0 ? 1f : 0f, corner == 1 ? 1f : 0f, corner == 2 ? 1f : 0f, 0f));
                        strengths.Add(strength);
                    }
                }
                first += triangles.Length / 3;
                submeshes.Add(indices);
            }
            var mesh = new Mesh { name = name, indexFormat = vertices.Count > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
            mesh.SetVertices(vertices);
            if (outNormals.Count == vertices.Count)
            {
                mesh.SetNormals(outNormals);
            }
            mesh.SetUVs(0, outUvs);
            mesh.SetUVs(1, strengths);
            mesh.SetColors(colors);
            mesh.subMeshCount = submeshes.Count;
            for (int sub = 0; sub < submeshes.Count; sub++)
            {
                mesh.SetTriangles(submeshes[sub], sub);
            }
            if (outNormals.Count != vertices.Count)
            {
                mesh.RecalculateNormals();
            }
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>
        /// The meshes a generated theme draws in place of Classic ones, by key, before they are wired: the scanned rocks of
        /// the model pack (thousands of small faces) become low-poly faceted rocks, as an old vector game drew them.
        /// </summary>
        public static Dictionary<string, Mesh> ThemeShapes(ThemeSpec spec)
        {
            var shapes = new Dictionary<string, Mesh>();
            for (int i = 1; i <= 3; i++)
            {
                shapes[$"Models/Rock{i}"] = AsteroidsAssets.SaveMesh(SpaceModels.VectorRock(2100 + i * 31, 1), $"{spec.ArtFolder}/Shapes/Rock{i}.asset");
            }
            shapes["Models/TitanRock"] = AsteroidsAssets.SaveMesh(SpaceModels.VectorRock(2231, 2), $"{spec.ArtFolder}/Shapes/TitanRock.asset");
            return shapes;
        }


        /// <summary>The edge strengths of a wire copy (its second UV set).</summary>
        private static List<Vector3> Strengths(Mesh mesh)
        {
            var list = new List<Vector3>();
            mesh.GetUVs(1, list);
            return list;
        }

        // ------------------------------------------------------------------ ground

        private static GroundTheme grid;

        /// <summary>The neon grid world of the Arcade theme: every kind of ground, two variants each, no props.</summary>
        public static GroundTheme Grid => grid ??= MakeGrid(Arcade);

        /// <summary>The worlds of the generated themes.</summary>
        public static GroundTheme[] GeneratedGroundThemes => new[] { Grid };


        private static GroundTheme MakeGrid(ThemeSpec spec)
        {
            var kinds = new List<(TerrainKind kind, int variants)>();
            foreach (TerrainKind kind in Enum.GetValues(typeof(TerrainKind)))
            {
                kinds.Add((kind, 2));
            }
            return new GroundTheme
            {
                Name = "Grid", Seed = 77, Accent = spec.Cyan, Neon = spec, Kinds = kinds.ToArray(),
                Shallow = spec.Cyan, Deep = spec.Night, Foam = spec.Cyan,
                Sun = Color.white, SunIntensity = 1f, Ambient = new Color(0.4f, 0.4f, 0.5f),
                Shadow = new Color(0.02f, 0f, 0.1f, 0.55f), CraterTint = spec.Magenta,
                AltShare = 0.3f, RockShare = 0.12f, Variation = 0f, Blocky = true, Dunes = 0f, PavedRoughness = 0.02f
            };
        }


        /// <summary>The ground art of a neon world: the mask of every tile and its NeonGround material; a ring for the craters.</summary>
        private static void BuildNeonTheme(GroundTheme theme)
        {
            ThemeSpec spec = theme.Neon;
            Color crater = spec.Magenta;
            AsteroidsAssets.SaveMaterial($"{theme.Folder}/Crater.mat", Glow, m => GlowSetup(m, Texture("Ring"), new Color(crater.r * 1.6f, crater.g * 1.6f, crater.b * 1.6f, 0.8f), true));
            AsteroidsAssets.SaveMaterial($"{theme.Folder}/Rubble.mat", Glow, m => GlowSetup(m, Texture("DangerRing"), new Color(spec.Cyan.r, spec.Cyan.g, spec.Cyan.b, 0.6f), true));
            foreach ((TerrainKind kind, int variants) in theme.Kinds)
            {
                for (int v = 0; v < variants; v++)
                {
                    BuildNeonTile(Plan(theme, kind, v));
                }
            }
        }


        private static void BuildNeonTile(TilePlan plan)
        {
            ThemeSpec spec = plan.Theme.Neon;
            Func<Vector2, float> rock = plan.Rock;
            Func<Vector2, float> alt = plan.Alt;
            Texture2D masks = AsteroidsAssets.SaveTexture(SpaceTextures.TileMasks(p => new Vector4(plan.Paved(p), plan.Liquid(p), rock(p), alt(p)), MaskRanges),
                $"{plan.Folder}/{plan.Name}Masks.png", DataImporter);
            AsteroidsAssets.SaveMaterial($"{plan.Folder}/{plan.Name}.mat", NeonGroundShader(spec), m =>
            {
                m.SetTexture("_Masks", masks);
                m.SetVector("_Ranges", MaskRanges);
                m.SetVector("_TileSize", new Vector4(StrikeRules.TileWidth, StrikeRules.TileLength, 0f, 0f));
                m.SetFloat("_Cell", 2.5f);
                m.SetColor("_FillColor", new Color(0.015f, 0.008f, 0.05f, 1f));
                m.SetColor("_LineColor", spec.Cyan * 0.8f);
                m.SetColor("_MajorColor", spec.Cyan * 1.6f);
                m.SetColor("_PavedColor", new Color(spec.Violet.r * 0.25f, spec.Violet.g * 0.25f, spec.Violet.b * 0.25f, 1f));
                m.SetColor("_PavedLine", spec.Magenta * 1.8f);
                m.SetColor("_LiquidColor", new Color(0f, 0.05f, 0.16f, 1f));
                m.SetColor("_ShoreColor", spec.Cyan * 2f);
                m.SetColor("_RockColor", new Color(spec.Violet.r * 0.18f, spec.Violet.g * 0.18f, spec.Violet.b * 0.18f, 1f));
                m.SetColor("_AltColor", new Color(spec.Violet.r * 0.1f, spec.Violet.g * 0.1f, spec.Violet.b * 0.1f, 1f));
                m.SetFloat("_LineWidth", 1.3f);
                m.enableInstancing = false;
            });
        }


        private static void GlowSetup(Material material, Texture texture, Color color, bool additive)
        {
            material.SetTexture("_BaseMap", texture);
            material.SetColor("_BaseColor", color);
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", additive ? (float)BlendMode.One : (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_Cull", (float)CullMode.Off);
            material.renderQueue = (int)RenderQueue.Transparent;
        }
    }
}
