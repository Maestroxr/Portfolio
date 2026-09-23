using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore;
using UnityEngine.TextCore.LowLevel;

namespace Portfolio.Heroes.EditorTools
{
    /// <summary>
    /// The words and signs of the interface: the fonts as signed distance field assets (Cinzel for titles, Alegreya
    /// for text with its real bold and italic faces, Cinzel Decorative for the name of the game, and two Noto symbol
    /// fonts behind them for arrows and marks), the looks of each (gold titles, text shadowed over the map), the icons
    /// by name, the resource and stat icons in color, and a sprite asset of them so a line of text can say
    /// &lt;sprite name="gold"&gt; 500.
    /// </summary>
    internal static partial class HeroesUIArt
    {
        private const string FontFolder = "Art/Generated/Fonts";

        /// <summary>The signs the symbol fonts are asked for; the stars come from the icon sprites instead.</summary>
        private const string Arrows = "←↑→↓↔↕⇐⇒⚔⚒⚑";
        private const string Marks = "✓✔✗✘♥♦♣♠●○■□▲▼◆◇❖❧";

        // ------------------------------------------------------------------ fonts

        private static void Fonts(HeroesArt art)
        {
            TMP_FontAsset arrows = Font("Art/Fonts/NotoSansSymbols-Regular.ttf", $"{FontFolder}/Noto Symbols SDF.asset", 64, Arrows);
            TMP_FontAsset marks = Font("Art/Fonts/NotoSansSymbols2-Regular.ttf", $"{FontFolder}/Noto Symbols2 SDF.asset", 64, Marks);
            var fallbacks = new List<TMP_FontAsset>();
            if (arrows != null)
            {
                fallbacks.Add(arrows);
            }
            if (marks != null)
            {
                fallbacks.Add(marks);
            }

            art.titleFont = Font("Art/Fonts/Cinzel-Bold.ttf", "Art/Generated/Cinzel-Bold SDF.asset", 90);
            art.bodyFont = Font("Art/Fonts/Alegreya-Regular.ttf", "Art/Generated/Alegreya SDF.asset", 90);
            art.logoFont = Font("Art/Fonts/CinzelDecorative-Black.ttf", $"{FontFolder}/CinzelDecorative-Black SDF.asset", 90);
            TMP_FontAsset bold = Font("Art/Fonts/Alegreya-Bold.ttf", $"{FontFolder}/Alegreya-Bold SDF.asset", 90);
            TMP_FontAsset italic = Font("Art/Fonts/Alegreya-Italic.ttf", $"{FontFolder}/Alegreya-Italic SDF.asset", 90);

            // <b> and <i> draw with the real faces rather than a thickened or slanted regular.
            if (art.bodyFont != null && (bold != null || italic != null))
            {
                TMP_FontWeightPair[] weights = art.bodyFont.fontWeightTable;
                if (weights != null && weights.Length >= 8 && (weights[7].regularTypeface != bold || weights[4].italicTypeface != italic))
                {
                    weights[7].regularTypeface = bold;
                    weights[4].italicTypeface = italic;
                    EditorUtility.SetDirty(art.bodyFont);
                }
            }
            foreach (TMP_FontAsset font in new[] { art.titleFont, art.bodyFont, art.logoFont, bold, italic })
            {
                Fallbacks(font, fallbacks);
            }

            art.titleLooks = Looks(art.titleFont, "Cinzel-Bold");
            art.bodyLooks = Looks(art.bodyFont, "Alegreya");
            art.logoLooks = Looks(art.logoFont, "CinzelDecorative-Black");
        }

        private static void Fallbacks(TMP_FontAsset font, List<TMP_FontAsset> fallbacks)
        {
            if (font == null)
            {
                return;
            }
            List<TMP_FontAsset> current = font.fallbackFontAssetTable ?? new List<TMP_FontAsset>();
            if (!current.SequenceEqual(fallbacks))
            {
                font.fallbackFontAssetTable = new List<TMP_FontAsset>(fallbacks);
                EditorUtility.SetDirty(font);
            }
        }

        /// <summary>
        /// A font asset for <paramref name="source"/>, made once: dynamic (glyphs added as text asks for them), or, given
        /// <paramref name="characters"/>, a small fixed atlas of just those (the symbol fonts, whose files need not ship).
        /// </summary>
        private static TMP_FontAsset Font(string source, string relative, int samplingSize, string characters = null)
        {
            var font = HeroesAssets.Load<UnityEngine.Font>(source);
            if (font == null)
            {
                Debug.LogWarning($"Heroes: the font {source} is missing.");
                return null;
            }
            TMP_FontAsset existing = HeroesAssets.Load<TMP_FontAsset>(relative);
            if (existing != null)
            {
                return existing;
            }
            bool fixedSet = !string.IsNullOrEmpty(characters);
            TMP_FontAsset asset = fixedSet
                ? TMP_FontAsset.CreateFontAsset(font, samplingSize, 6, GlyphRenderMode.SDFAA, 512, 512, AtlasPopulationMode.Dynamic, false)
                : TMP_FontAsset.CreateFontAsset(font, samplingSize, 9, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
            if (asset == null)
            {
                Debug.LogWarning($"Heroes: no font asset could be made of {source}.");
                return null;
            }
            asset.name = Path.GetFileNameWithoutExtension(relative);
            if (fixedSet)
            {
                if (!asset.TryAddCharacters(characters, out string missing) && !string.IsNullOrEmpty(missing))
                {
                    Debug.Log($"Heroes: {Path.GetFileName(source)} has no glyph for {missing.Length} of the signs asked of it.");
                }
                asset.atlasPopulationMode = AtlasPopulationMode.Static;
            }
            HeroesAssets.EnsureFolderOf(relative);
            AssetDatabase.CreateAsset(asset, HeroesAssets.Path(relative));
            asset.material.name = asset.name + " Material";
            asset.atlasTexture.name = asset.name + " Atlas";
            AssetDatabase.AddObjectToAsset(asset.material, asset);
            AssetDatabase.AddObjectToAsset(asset.atlasTexture, asset);
            AssetDatabase.SaveAssets();
            return asset;
        }

        /// <summary>The materials of a font for every <see cref="TextLook"/>, the first its own.</summary>
        private static Material[] Looks(TMP_FontAsset font, string name)
        {
            var looks = new Material[Enum.GetValues(typeof(TextLook)).Length];
            if (font == null || font.material == null)
            {
                return looks;
            }
            looks[(int)TextLook.Plain] = font.material;
            looks[(int)TextLook.Gold] = Look(font, name, "Gold", m =>
            {
                Outline(m, 0.16f, new Color(0.14f, 0.07f, 0.02f, 1f));
                Underlay(m, new Color(0f, 0f, 0f, 0.8f), new Vector2(0.7f, -0.9f), 0.1f, 0.35f);
                m.SetFloat(ShaderUtilities.ID_FaceDilate, 0.1f);
            });
            looks[(int)TextLook.Shadow] = Look(font, name, "Shadow", m =>
            {
                Underlay(m, new Color(0f, 0f, 0f, 0.9f), new Vector2(0.45f, -0.6f), 0.35f, 0.55f);
            });
            looks[(int)TextLook.Outline] = Look(font, name, "Outline", m =>
            {
                Outline(m, 0.24f, new Color(0.06f, 0.035f, 0.02f, 1f));
                m.SetFloat(ShaderUtilities.ID_FaceDilate, 0.14f);
            });
            looks[(int)TextLook.Logo] = Look(font, name, "Logo", m =>
            {
                Outline(m, 0.2f, new Color(0.18f, 0.08f, 0.02f, 1f));
                Underlay(m, new Color(1f, 0.64f, 0.2f, 0.6f), Vector2.zero, 0.6f, 0.9f);
                m.SetFloat(ShaderUtilities.ID_FaceDilate, 0.12f);
            });
            return looks;
        }

        private static Material Look(TMP_FontAsset font, string fontName, string look, Action<Material> configure)
        {
            return HeroesAssets.Material($"{FontFolder}/{fontName} {look}.mat", font.material.shader, m =>
            {
                m.CopyPropertiesFromMaterial(font.material);
                m.shaderKeywords = font.material.shaderKeywords;
                configure(m);
                // The ratios TextMesh Pro works out itself the first time the material draws, set now, so drawing text
                // with it does not change the file and a rebuild does not change it back.
                ShaderUtilities.UpdateShaderRatios(m);
            });
        }

        private static void Outline(Material material, float width, Color color)
        {
            material.EnableKeyword("OUTLINE_ON");
            material.SetFloat(ShaderUtilities.ID_OutlineWidth, width);
            material.SetColor(ShaderUtilities.ID_OutlineColor, color);
        }

        private static void Underlay(Material material, Color color, Vector2 offset, float dilate, float softness)
        {
            material.EnableKeyword("UNDERLAY_ON");
            material.SetColor(ShaderUtilities.ID_UnderlayColor, color);
            material.SetFloat(ShaderUtilities.ID_UnderlayOffsetX, offset.x);
            material.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, offset.y);
            material.SetFloat(ShaderUtilities.ID_UnderlayDilate, dilate);
            material.SetFloat(ShaderUtilities.ID_UnderlaySoftness, softness);
        }

        // ------------------------------------------------------------------ icons

        /// <summary>The icons drawn in color (the downloaded ones are white shapes), with the tones they are painted in.</summary>
        private static readonly (string name, Color light, Color dark)[] Colored =
        {
            ("gold", new Color(1f, 0.9f, 0.45f), new Color(0.74f, 0.44f, 0.06f)),
            ("wood", new Color(0.8f, 0.56f, 0.32f), new Color(0.4f, 0.22f, 0.09f)),
            ("ore", new Color(0.8f, 0.8f, 0.82f), new Color(0.34f, 0.34f, 0.38f)),
            ("mercury", new Color(0.93f, 0.9f, 1f), new Color(0.48f, 0.42f, 0.7f)),
            ("sulfur", new Color(0.96f, 1f, 0.45f), new Color(0.6f, 0.6f, 0.04f)),
            ("crystal", new Color(1f, 0.62f, 0.6f), new Color(0.7f, 0.07f, 0.12f)),
            ("gems", new Color(0.6f, 1f, 0.82f), new Color(0.04f, 0.55f, 0.46f)),
            ("stat_attack", new Color(1f, 0.58f, 0.46f), new Color(0.66f, 0.12f, 0.08f)),
            ("stat_defense", new Color(0.72f, 0.84f, 1f), new Color(0.2f, 0.34f, 0.7f)),
            ("stat_power", new Color(0.96f, 0.68f, 1f), new Color(0.46f, 0.14f, 0.66f)),
            ("stat_knowledge", new Color(0.62f, 0.92f, 1f), new Color(0.08f, 0.44f, 0.7f)),
            ("damage", new Color(1f, 0.72f, 0.5f), new Color(0.7f, 0.24f, 0.08f)),
            ("health", new Color(1f, 0.52f, 0.5f), new Color(0.65f, 0.07f, 0.1f)),
            ("speed", new Color(0.72f, 1f, 0.7f), new Color(0.14f, 0.54f, 0.2f)),
            ("movement", new Color(0.96f, 0.86f, 0.62f), new Color(0.55f, 0.38f, 0.14f)),
            ("mana", new Color(0.62f, 0.82f, 1f), new Color(0.1f, 0.3f, 0.8f)),
            ("morale", new Color(1f, 0.86f, 0.42f), new Color(0.75f, 0.34f, 0.05f)),
            ("luck", new Color(0.62f, 1f, 0.6f), new Color(0.08f, 0.55f, 0.15f)),
            ("experience", new Color(1f, 0.92f, 0.52f), new Color(0.6f, 0.4f, 0.1f))
        };

        /// <summary>The names the sprite asset gives the colored icons (the file names less their "stat_").</summary>
        private static string SpriteName(string icon)
        {
            return icon.StartsWith("stat_", StringComparison.Ordinal) ? icon.Substring(5) : icon;
        }

        private static void Icons(HeroesArt art)
        {
            art.icons.Clear();
            art.iconNames.Clear();
            string folder = HeroesAssets.Path("Art/UI/Icons");
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { folder }).OrderBy(AssetDatabase.GUIDToAssetPath))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string relative = path.Substring(HeroesAssets.Root.Length + 1);
                HeroesAssets.ConfigureTexture(relative, importer => HeroesAssets.SpriteImport(importer, Vector4.zero, 256));
                Sprite sprite = HeroesAssets.Sprite(relative);
                if (sprite == null)
                {
                    continue;
                }
                string name = Path.GetFileNameWithoutExtension(path);
                art.icons.Add(sprite);
                art.iconNames.Add(name);
            }
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { HeroesAssets.Path("Art/UI/Items") }))
            {
                string relative = AssetDatabase.GUIDToAssetPath(guid).Substring(HeroesAssets.Root.Length + 1);
                HeroesAssets.ConfigureTexture(relative, importer => HeroesAssets.SpriteImport(importer, Vector4.zero, 128));
            }

            // The colored icons, and the sheet of them the text draws its inline icons from.
            var colored = new Dictionary<string, Sprite>();
            var cells = new List<(string name, Color[] pixels, uint unicode)>();
            foreach ((string name, Color light, Color dark) in Colored)
            {
                Texture2D white = Read($"Art/UI/Icons/{name}.png");
                if (white == null)
                {
                    continue;
                }
                Color[] big = Paint(white, 128, light, dark);
                UnityEngine.Object.DestroyImmediate(white);
                var texture = new Texture2D(128, 128, TextureFormat.RGBA32, false);
                texture.SetPixels(big);
                texture.Apply();
                string relative = $"{Out}/Icons/{name}.png";
                HeroesAssets.SaveTexture(texture, relative, importer => HeroesAssets.SpriteImport(importer, Vector4.zero, 128));
                colored[name] = HeroesAssets.Sprite(relative);
                cells.Add((SpriteName(name), Shrink(big, 128, 64), 0xFFFE));
            }
            foreach ((string name, Sprite star, uint unicode) in new[] { ("star", art.star, 0x2605u), ("star_empty", art.starEmpty, 0x2606u) })
            {
                Texture2D source = star != null ? Read(AssetDatabase.GetAssetPath(star).Substring(HeroesAssets.Root.Length + 1)) : null;
                if (source != null)
                {
                    cells.Add((name, Shrink(source.GetPixels(), source.width, 64), unicode));
                    UnityEngine.Object.DestroyImmediate(source);
                }
            }

            // The icons the game looks up by name: the resources and the primary skills in color.
            string[] resources = { "gold", "wood", "ore", "mercury", "sulfur", "crystal", "gems" };
            art.resourceIcons = resources.Select(name => colored.TryGetValue(name, out Sprite sprite) ? sprite : art.Icon(name)).ToArray();
            string[] stats = { "stat_attack", "stat_defense", "stat_power", "stat_knowledge" };
            art.statIcons = stats.Select(name => colored.TryGetValue(name, out Sprite sprite) ? sprite : art.Icon(name)).ToArray();
            art.skillIcons = new Sprite[HeroData.SkillCount];
            for (int i = 0; i < art.skillIcons.Length; i++)
            {
                string name = "skill_" + ToSnake(((SkillId)i).ToString());
                art.skillIcons[i] = art.Icon(name);
            }
            art.spellIcons = new Sprite[Spells.Count];
            for (int i = 0; i < art.spellIcons.Length; i++)
            {
                art.spellIcons[i] = art.Icon("spell_" + ToSnake(((SpellId)i).ToString()));
            }
            art.artifactIcons = new Sprite[Artifacts.Count];
            for (int i = 0; i < art.artifactIcons.Length; i++)
            {
                ArtifactDef def = Artifacts.Get((ArtifactId)i);
                art.artifactIcons[i] = def != null ? HeroesAssets.Sprite($"Art/UI/Items/{def.Item}.png") : null;
            }
            art.iconSprites = SpriteSheet(cells);
        }

        /// <summary>
        /// A white icon painted in two tones, light at the top and dark at the bottom, its edges raised toward the light,
        /// on a dark outline and a soft shadow, <paramref name="size"/> pixels square.
        /// </summary>
        private static Color[] Paint(Texture2D white, int size, Color light, Color dark)
        {
            const int margin = 8;
            int inner = size - margin * 2;
            float[] alpha = new float[size * size];
            Color[] source = white.GetPixels();
            float step = white.width / (float)inner;
            for (int y = 0; y < inner; y++)
            {
                for (int x = 0; x < inner; x++)
                {
                    float sum = 0f;
                    int count = 0;
                    for (int j = Mathf.FloorToInt(y * step); j < Mathf.FloorToInt((y + 1) * step); j++)
                    {
                        for (int i = Mathf.FloorToInt(x * step); i < Mathf.FloorToInt((x + 1) * step); i++)
                        {
                            Color c = source[Mathf.Min(j, white.height - 1) * white.width + Mathf.Min(i, white.width - 1)];
                            sum += c.a * c.r;
                            count++;
                        }
                    }
                    alpha[(y + margin) * size + x + margin] = count > 0 ? sum / count : 0f;
                }
            }
            float A(int x, int y) => x < 0 || y < 0 || x >= size || y >= size ? 0f : alpha[y * size + x];
            // A blurred copy is the height of the raised shape.
            float[] height = Blur(alpha, size, 2);
            float H(int x, int y) => x < 0 || y < 0 || x >= size || y >= size ? 0f : height[y * size + x];
            var raster = new Gamebox.Editor.Raster(size, size, Color.clear);
            // The shadow and the outline: the shape grown by a few pixels.
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float grown = 0f;
                    float shadow = 0f;
                    for (int dy = -4; dy <= 4; dy++)
                    {
                        for (int dx = -4; dx <= 4; dx++)
                        {
                            float d = Mathf.Sqrt(dx * dx + dy * dy);
                            if (d <= 3.6f)
                            {
                                grown = Mathf.Max(grown, A(x + dx, y + dy) * Mathf.Clamp01(3.6f - d + 0.5f));
                            }
                            if (d <= 4.2f)
                            {
                                shadow = Mathf.Max(shadow, A(x + dx - 2, y + dy + 3) * Mathf.Clamp01(1f - d / 4.2f));
                            }
                        }
                    }
                    raster.Blend(x, y, new Color(0f, 0f, 0f, shadow * 0.45f));
                    raster.Blend(x, y, new Color(0.08f, 0.05f, 0.03f, grown));
                }
            }
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float a = A(x, y);
                    if (a <= 0f)
                    {
                        continue;
                    }
                    float t = (y - margin) / (float)inner;
                    Color color = Color.Lerp(dark, light, Mathf.SmoothStep(0f, 1f, t));
                    float gx = H(x + 1, y) - H(x - 1, y);
                    float gy = H(x, y + 1) - H(x, y - 1);
                    float lit = -(gx * Light.x + gy * Light.y);
                    color *= 1f + lit * 1.2f;
                    color.a = a;
                    raster.Blend(x, y, color);
                }
            }
            return raster.Pixels;
        }

        private static float[] Blur(float[] values, int size, int radius)
        {
            var result = new float[values.Length];
            var temp = new float[values.Length];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float sum = 0f;
                    for (int d = -radius; d <= radius; d++)
                    {
                        sum += values[y * size + Mathf.Clamp(x + d, 0, size - 1)];
                    }
                    temp[y * size + x] = sum / (radius * 2 + 1);
                }
            }
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float sum = 0f;
                    for (int d = -radius; d <= radius; d++)
                    {
                        sum += temp[Mathf.Clamp(y + d, 0, size - 1) * size + x];
                    }
                    result[y * size + x] = sum / (radius * 2 + 1);
                }
            }
            return result;
        }

        /// <summary>A square picture scaled down by averaging (in premultiplied color, so edges do not darken).</summary>
        private static Color[] Shrink(Color[] pixels, int size, int to)
        {
            var result = new Color[to * to];
            int step = size / to;
            for (int y = 0; y < to; y++)
            {
                for (int x = 0; x < to; x++)
                {
                    float r = 0f, g = 0f, b = 0f, a = 0f;
                    for (int j = 0; j < step; j++)
                    {
                        for (int i = 0; i < step; i++)
                        {
                            Color c = pixels[(y * step + j) * size + x * step + i];
                            r += c.r * c.a;
                            g += c.g * c.a;
                            b += c.b * c.a;
                            a += c.a;
                        }
                    }
                    result[y * to + x] = a > 0.0001f ? new Color(r / a, g / a, b / a, a / (step * step)) : Color.clear;
                }
            }
            return result;
        }

        /// <summary>
        /// The sheet of inline icons: 64 pixel cells, eight to a row, and the TextMesh Pro sprite asset over it, every
        /// sprite named and the stars also found by their characters (★ ☆).
        /// </summary>
        private static TMP_SpriteAsset SpriteSheet(List<(string name, Color[] pixels, uint unicode)> cells)
        {
            const int cell = 64;
            const int columns = 8;
            int rows = Mathf.Max(1, Mathf.CeilToInt(cells.Count / (float)columns));
            int width = cell * columns;
            int height = Mathf.NextPowerOfTwo(cell * rows);
            var sheet = new Texture2D(width, height, TextureFormat.RGBA32, false);
            sheet.SetPixels(new Color[width * height]);
            for (int i = 0; i < cells.Count; i++)
            {
                int x = i % columns * cell;
                int y = height - (i / columns + 1) * cell;
                sheet.SetPixels(x, y, cell, cell, cells[i].pixels);
            }
            sheet.Apply();
            const string sheetPath = Out + "/IconSheet.png";
            Texture2D atlas = HeroesAssets.SaveTexture(sheet, sheetPath, importer =>
            {
                importer.textureType = TextureImporterType.Default;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = true;
                importer.maxTextureSize = 512;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.filterMode = FilterMode.Trilinear;
                importer.npotScale = TextureImporterNPOTScale.None;
            });

            string relative = $"{FontFolder}/Icons.asset";
            TMP_SpriteAsset asset = HeroesAssets.Load<TMP_SpriteAsset>(relative);
            bool created = asset == null;
            string before = created ? null : EditorJsonUtility.ToJson(asset);
            if (created)
            {
                asset = ScriptableObject.CreateInstance<TMP_SpriteAsset>();
                asset.name = "Icons";
                // Marked as of the current layout before a material is given it, or it would be "upgraded" to nothing.
                var serialized = new SerializedObject(asset);
                serialized.FindProperty("m_Version").stringValue = "1.1.0";
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
            asset.spriteSheet = atlas;
            asset.spriteGlyphTable.Clear();
            asset.spriteCharacterTable.Clear();
            for (int i = 0; i < cells.Count; i++)
            {
                int x = i % columns * cell;
                int y = height - (i / columns + 1) * cell;
                var glyph = new TMP_SpriteGlyph
                {
                    index = (uint)i,
                    metrics = new GlyphMetrics(cell, cell, 0f, cell * 0.84f, cell * 1.06f),
                    glyphRect = new GlyphRect(x, y, cell, cell),
                    scale = 1f,
                    atlasIndex = 0
                };
                asset.spriteGlyphTable.Add(glyph);
                asset.spriteCharacterTable.Add(new TMP_SpriteCharacter(cells[i].unicode, glyph) { name = cells[i].name, scale = 1f });
            }
            if (created)
            {
                HeroesAssets.EnsureFolderOf(relative);
                AssetDatabase.CreateAsset(asset, HeroesAssets.Path(relative));
                var material = new Material(Shader.Find("TextMeshPro/Sprite")) { name = "Icons Material" };
                material.SetTexture(ShaderUtilities.ID_MainTex, atlas);
                asset.material = material;
                AssetDatabase.AddObjectToAsset(material, asset);
            }
            else if (asset.material != null && asset.material.GetTexture(ShaderUtilities.ID_MainTex) != atlas)
            {
                asset.material.SetTexture(ShaderUtilities.ID_MainTex, atlas);
                EditorUtility.SetDirty(asset.material);
            }
            asset.UpdateLookupTables();
            if (created || EditorJsonUtility.ToJson(asset) != before)
            {
                EditorUtility.SetDirty(asset);
            }
            return asset;
        }

        public static string ToSnake(string name)
        {
            var text = new System.Text.StringBuilder();
            for (int i = 0; i < name.Length; i++)
            {
                if (char.IsUpper(name[i]))
                {
                    if (i > 0)
                    {
                        text.Append('_');
                    }
                    text.Append(char.ToLowerInvariant(name[i]));
                }
                else
                {
                    text.Append(name[i]);
                }
            }
            return text.ToString();
        }
    }
}
