using Gamebox.Editor;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TextCore.LowLevel;

namespace Portfolio.Monopoly.EditorTools
{
    /// <summary>
    /// Builds the art and sound of the game into the package: for every look of <see cref="MonopolyThemeSpec.All"/> the
    /// generated textures, meshes and URP materials and the TextMesh Pro fonts (baked from the files in the look's Fonts
    /// folder; the Font Awesome icons come from the classic one), and once the Kenney sounds (configured) and the
    /// synthesised music and effects. Assets are updated in place and only rewritten when they changed. The accessors
    /// hand the scene and interface builders the classic assets unless a spec is named.
    /// </summary>
    internal static class MonopolyArtBuilder
    {
        public const string KenneyFolder = "Audio/Kenney";
        public const string GeneratedAudioFolder = "Audio/Generated";

        public const float Corner = 1.6f;
        public const float Width = 1f;
        public const int BoardPixels = 4096;

        public static BoardGeometry Geometry => new BoardGeometry(Corner, Width);

        public static MonopolyThemeSpec Classic => MonopolyThemeSpec.Classic;

        public static void BuildAll()
        {
            foreach (MonopolyThemeSpec spec in MonopolyThemeSpec.All)
            {
                BuildLook(spec);
            }
            Step("Sound", 0.9f);
            BuildAudio();
            AssetDatabase.SaveAssets();
        }

        /// <summary>The textures, meshes, materials and fonts of one look.</summary>
        public static void BuildLook(MonopolyThemeSpec spec)
        {
            Step($"{spec.Name}: textures", 0.1f);
            BuildTextures(spec);
            Step($"{spec.Name}: meshes", 0.4f);
            BuildMeshes(spec);
            Step($"{spec.Name}: materials", 0.6f);
            BuildMaterials(spec);
            Step($"{spec.Name}: fonts", 0.75f);
            BuildFonts(spec);
        }

        private static void Step(string what, float progress)
        {
            if (!Application.isBatchMode)
            {
                EditorUtility.DisplayProgressBar("Monopoly: art and sound", what, progress);
            }
            Debug.Log($"Monopoly art: {what}");
        }

        // ------------------------------------------------------------------ accessors for the other builders

        public static Sprite Sprite(string relative)
        {
            Sprite sprite = MonopolyAssets.LoadSprite($"{relative}.png");
            if (sprite == null)
            {
                throw new System.InvalidOperationException($"Monopoly: sprite {relative} is missing; rebuild the art.");
            }
            return sprite;
        }

        public static Sprite UI(string name, MonopolyThemeSpec spec = null)
        {
            return Sprite($"{(spec ?? Classic).UIFolder}/{name}");
        }

        public static Sprite Badge(int token, MonopolyThemeSpec spec = null)
        {
            return Sprite($"{(spec ?? Classic).TokensFolder}/Badge_{token}");
        }

        public static Mesh Mesh(string name, MonopolyThemeSpec spec = null)
        {
            return MonopolyAssets.Require<Mesh>($"{(spec ?? Classic).MeshesFolder}/{name}.asset");
        }

        public static Material Material(string name, MonopolyThemeSpec spec = null)
        {
            return MonopolyAssets.Require<Material>($"{(spec ?? Classic).MaterialsFolder}/{name}.mat");
        }

        public static Texture2D Texture(string name, MonopolyThemeSpec spec = null)
        {
            return MonopolyAssets.Require<Texture2D>($"{(spec ?? Classic).TexturesFolder}/{name}.png");
        }

        /// <summary>A texture of a look that only some looks have (the glow of the board), or null.</summary>
        public static Texture2D OptionalTexture(string name, MonopolyThemeSpec spec)
        {
            return MonopolyAssets.Load<Texture2D>($"{spec.TexturesFolder}/{name}.png");
        }

        public static TMP_FontAsset Font(string name, MonopolyThemeSpec spec = null)
        {
            return MonopolyAssets.Require<TMP_FontAsset>($"{(spec ?? Classic).FontsFolder}/{name} SDF.asset");
        }

        public static TMP_FontAsset Body => Font(Classic.Body.name);
        public static TMP_FontAsset Bold => Font(Classic.Bold.name);
        public static TMP_FontAsset Heavy => Font(Classic.Heavy.name);
        public static TMP_FontAsset IconFont => Font("Icons");

        public static TMP_FontAsset BodyOf(MonopolyThemeSpec spec) => Font(spec.Body.name, spec);
        public static TMP_FontAsset BoldOf(MonopolyThemeSpec spec) => Font(spec.Bold.name, spec);
        public static TMP_FontAsset HeavyOf(MonopolyThemeSpec spec) => Font(spec.Heavy.name, spec);
        public static TMP_FontAsset TitleOf(MonopolyThemeSpec spec) => Font(spec.Title.name, spec);

        /// <summary>A soft shadow material of the bold font, for text over the 3D board.</summary>
        public static Material TextShadowOf(MonopolyThemeSpec spec)
        {
            return MonopolyAssets.Require<Material>($"{spec.FontsFolder}/{spec.Bold.name} Shadow.mat");
        }

        /// <summary>The outlined material of the title font, for the logo and the banners.</summary>
        public static Material TitleMaterialOf(MonopolyThemeSpec spec)
        {
            return MonopolyAssets.Require<Material>($"{spec.FontsFolder}/{spec.Title.name} Title.mat");
        }

        public static Material TextShadow => TextShadowOf(Classic);
        public static Material TitleMaterial => TitleMaterialOf(Classic);

        public static AudioClip Clip(string name)
        {
            AudioClip clip = MonopolyAssets.Load<AudioClip>($"{KenneyFolder}/{name}.ogg") ?? MonopolyAssets.Load<AudioClip>($"{GeneratedAudioFolder}/{name}.wav");
            if (clip == null)
            {
                throw new System.InvalidOperationException($"Monopoly: sound {name} is missing.");
            }
            return clip;
        }

        public static Cubemap ReflectionOf(MonopolyThemeSpec spec)
        {
            return MonopolyAssets.Load<Cubemap>($"{spec.TexturesFolder}/Reflection.png");
        }

        public static Cubemap Reflection => ReflectionOf(Classic);

        // ------------------------------------------------------------------ textures

        private static void BuildTextures(MonopolyThemeSpec spec)
        {
            var board = WorldTourBoard.Create();
            string textures = spec.TexturesFolder;
            Texture2D surface = MonopolyArt.Board(Geometry, board.spaces, BoardPixels, spec, out Texture2D glow);
            MonopolyAssets.SaveTexture(surface, $"{textures}/Board.png", MonopolyAssets.SurfaceTexture(false, BoardPixels));
            if (glow != null)
            {
                MonopolyAssets.SaveTexture(glow, $"{textures}/BoardGlow.png", MonopolyAssets.SurfaceTexture(false, BoardPixels));
            }
            int tableSize = spec.Style == ArtStyle.Galactic ? 2048 : 1024;
            MonopolyAssets.SaveTexture(MonopolyArt.Table(tableSize, spec), $"{textures}/Table.png", MonopolyAssets.SurfaceTexture(true, tableSize));
            MonopolyAssets.SaveTexture(MonopolyArt.DiceAtlas(false, 256, spec), $"{textures}/Dice.png", MonopolyAssets.SurfaceTexture(false, 1024, false));
            MonopolyAssets.SaveTexture(MonopolyArt.DiceAtlas(true, 256, spec), $"{textures}/SpeedDie.png", MonopolyAssets.SurfaceTexture(false, 1024, false));
            MonopolyAssets.SaveTexture(MonopolyArt.CardBack(spec.Palette.chanceOrange, 320, 480, spec), $"{textures}/CardChance.png", MonopolyAssets.SurfaceTexture(false, 512, false));
            MonopolyAssets.SaveTexture(MonopolyArt.CardBack(spec.Palette.chestBlue, 320, 480, spec), $"{textures}/CardChest.png", MonopolyAssets.SurfaceTexture(false, 512, false));
            MonopolyAssets.SaveTexture(MonopolyArt.HighlightFrame(256), $"{textures}/Highlight.png", MonopolyAssets.DecalTexture(256));
            MonopolyAssets.SaveTexture(MonopolyArt.LogoBanner(1024, 300, spec), $"{textures}/Logo.png", MonopolyAssets.DecalTexture(1024));
            MonopolyAssets.SaveTexture(MonopolyArt.ContactShadow(128), $"{textures}/ContactShadow.png", MonopolyAssets.DecalTexture(128));
            MonopolyAssets.SaveTexture(ReflectionStrip(128, spec), $"{textures}/Reflection.png", importer =>
            {
                importer.textureShape = TextureImporterShape.TextureCube;
                importer.generateCubemap = TextureImporterGenerateCubemap.FullCubemap;
                importer.mipmapEnabled = true;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.sRGBTexture = true;
            });
            if (spec.Style == ArtStyle.Galactic)
            {
                MonopolyAssets.SaveTexture(MonopolyArt.BuildingSkin(false, 256, spec), $"{textures}/HouseSkin.png", MonopolyAssets.SurfaceTexture(true, 256, false));
                MonopolyAssets.SaveTexture(MonopolyArt.BuildingSkin(true, 256, spec), $"{textures}/HotelSkin.png", MonopolyAssets.SurfaceTexture(true, 256, false));
            }

            string ui = spec.UIFolder;
            MonopolyAssets.SaveTexture(MonopolyArt.Rounded(128, 40f), $"{ui}/Rounded.png", MonopolyAssets.SpriteTexture(new Vector4(48, 48, 48, 48)));
            Texture2D shadow = spec.Style == ArtStyle.Galactic ? MonopolyArt.Halo(128, 36f, 22f) : MonopolyArt.Shadow(128, 36f, 22f);
            MonopolyAssets.SaveTexture(shadow, $"{ui}/Shadow.png", MonopolyAssets.SpriteTexture(new Vector4(60, 60, 60, 60)));
            MonopolyAssets.SaveTexture(MonopolyArt.Circle(256), $"{ui}/Circle.png", MonopolyAssets.SpriteTexture());
            MonopolyAssets.SaveTexture(MonopolyArt.Ring(256, 22f), $"{ui}/Ring.png", MonopolyAssets.SpriteTexture());
            MonopolyAssets.SaveTexture(MonopolyArt.Glow(256), $"{ui}/Glow.png", MonopolyAssets.SpriteTexture());
            MonopolyAssets.SaveTexture(MonopolyArt.Sheen(16, 128, spec), $"{ui}/Sheen.png", MonopolyAssets.SpriteTexture());
            MonopolyAssets.SaveTexture(MonopolyArt.Bill(192, 96, spec), $"{ui}/Bill.png", MonopolyAssets.SpriteTexture());
            MonopolyAssets.SaveTexture(MonopolyArt.LogoBanner(1024, 300, spec), $"{ui}/Logo.png", MonopolyAssets.SpriteTexture(new Vector4(60, 60, 60, 60)));
            for (int token = 0; token < spec.Tokens.Length; token++)
            {
                MonopolyAssets.SaveTexture(MonopolyArt.TokenBadge(token, 256, spec), $"{spec.TokensFolder}/Badge_{token}.png", MonopolyAssets.SpriteTexture());
            }
        }

        /// <summary>
        /// A soft studio for the metal to reflect, as a horizontal strip of six cube faces (+X, -X, +Y, -Y, +Z, -Z):
        /// a ceiling with two soft boxes, a horizon and a floor in the colours of the look.
        /// </summary>
        private static Texture2D ReflectionStrip(int face, MonopolyThemeSpec spec)
        {
            var raster = new Raster(face * 6, face, Color.black);
            for (int f = 0; f < 6; f++)
            {
                for (int y = 0; y < face; y++)
                {
                    for (int x = 0; x < face; x++)
                    {
                        float sc = (x + 0.5f) / face * 2f - 1f;
                        float tc = (face - y - 0.5f) / face * 2f - 1f;
                        Vector3 d;
                        switch (f)
                        {
                            case 0: d = new Vector3(1f, -tc, -sc); break;
                            case 1: d = new Vector3(-1f, -tc, sc); break;
                            case 2: d = new Vector3(sc, 1f, tc); break;
                            case 3: d = new Vector3(sc, -1f, -tc); break;
                            case 4: d = new Vector3(sc, -tc, 1f); break;
                            default: d = new Vector3(-sc, -tc, -1f); break;
                        }
                        raster.Set(f * face + x, y, Studio(d.normalized, spec));
                    }
                }
            }
            return raster.ToTexture(false);
        }

        private static Color Studio(Vector3 d, MonopolyThemeSpec spec)
        {
            Color c = d.y >= 0f ? Color.Lerp(spec.StudioHorizon, spec.StudioCeiling, Mathf.Pow(d.y, 0.6f)) : Color.Lerp(spec.StudioHorizon, spec.StudioFloor, Mathf.Pow(-d.y, 0.5f));
            // Two soft boxes overhead and a rim light, which give the metal its highlights.
            float light = Box(d, new Vector3(-0.4f, 0.85f, -0.3f).normalized, 0.28f) * 1.4f
                + Box(d, new Vector3(0.6f, 0.6f, 0.4f).normalized, 0.22f) * 0.9f
                + Box(d, new Vector3(0f, 0.2f, -1f).normalized, 0.3f) * 0.5f;
            c += spec.StudioLight * light;
            return new Color(Mathf.Min(c.r, 1f), Mathf.Min(c.g, 1f), Mathf.Min(c.b, 1f), 1f);
        }

        private static float Box(Vector3 d, Vector3 center, float size)
        {
            float angle = Mathf.Acos(Mathf.Clamp(Vector3.Dot(d, center), -1f, 1f));
            return Mathf.Clamp01(1f - angle / size);
        }

        // ------------------------------------------------------------------ meshes

        private static void BuildMeshes(MonopolyThemeSpec spec)
        {
            string meshes = spec.MeshesFolder;
            for (int token = 0; token < spec.Tokens.Length; token++)
            {
                MonopolyAssets.SaveMesh(MonopolyModels.Token(spec, token), $"{meshes}/Token_{token}.asset");
            }
            MonopolyAssets.SaveMesh(MonopolyModels.TokenBase(spec), $"{meshes}/TokenBase.asset");
            MonopolyAssets.SaveMesh(MonopolyModels.TurnRing(), $"{meshes}/TurnRing.asset");
            MonopolyAssets.SaveMesh(MonopolyModels.House(spec), $"{meshes}/House.asset");
            MonopolyAssets.SaveMesh(MonopolyModels.Hotel(spec), $"{meshes}/Hotel.asset");
            MonopolyAssets.SaveMesh(MonopolyModels.Die(), $"{meshes}/Die.asset");
            MonopolyAssets.SaveMesh(MonopolyModels.BoardSlab(Geometry.Side, 0.3f, 0.2f), $"{meshes}/BoardSlab.asset");
            MonopolyAssets.SaveMesh(MonopolyModels.BoardTop(Geometry.Side), $"{meshes}/BoardTop.asset");
            MonopolyAssets.SaveMesh(MonopolyModels.Table(90f, spec.TableTiles), $"{meshes}/Table.asset");
            MonopolyAssets.SaveMesh(MonopolyModels.Quad(), $"{meshes}/FlatQuad.asset");
            MonopolyAssets.SaveMesh(MonopolyModels.CardStack(1.2f, 1.8f, 0.14f), $"{meshes}/CardStack.asset");
            MonopolyAssets.SaveMesh(MonopolyModels.OwnerTag(Width - 0.14f, 0.13f), $"{meshes}/OwnerTag.asset");
        }

        // ------------------------------------------------------------------ materials

        private static Shader Lit => Shader.Find("Universal Render Pipeline/Lit");
        private static Shader Unlit => Shader.Find("Universal Render Pipeline/Unlit");

        private static void BuildMaterials(MonopolyThemeSpec spec)
        {
            bool glow = spec.Emissive;
            Texture2D board = Texture("Board", spec);
            Texture2D boardGlow = OptionalTexture("BoardGlow", spec);
            Texture2D table = Texture("Table", spec);
            Texture2D dice = Texture("Dice", spec);
            Texture2D speed = Texture("SpeedDie", spec);
            Texture2D chance = Texture("CardChance", spec);
            Texture2D chest = Texture("CardChest", spec);
            LitMaterial(spec, "Board", Color.white, 0f, glow ? 0.35f : 0.12f, board, glow ? boardGlow : null, Glow(spec, 1.3f));
            LitMaterial(spec, "BoardSlab", spec.SlabColor, spec.SlabMetallic, spec.SlabSmoothness, null);
            LitMaterial(spec, "Table", Color.white, 0f, glow ? 0.05f : 0.42f, table, glow ? table : null, Glow(spec, 0.75f));
            LitMaterial(spec, "Pewter", spec.TokenMetal, spec.TokenMetallic, spec.TokenSmoothness, null, null, Glow(spec, spec.TokenGlow));
            if (glow)
            {
                Texture2D houseSkin = Texture("HouseSkin", spec);
                Texture2D hotelSkin = Texture("HotelSkin", spec);
                LitMaterial(spec, "House", spec.HouseColor, 0.2f, 0.55f, houseSkin, houseSkin, new Color(1.6f, 1.6f, 1.6f));
                LitMaterial(spec, "Hotel", spec.HotelColor, 0.2f, 0.55f, hotelSkin, hotelSkin, new Color(1.6f, 1.6f, 1.6f));
            }
            else
            {
                LitMaterial(spec, "House", spec.HouseColor, 0f, 0.62f, null);
                LitMaterial(spec, "Hotel", spec.HotelColor, 0f, 0.62f, null);
            }
            LitMaterial(spec, "Die", Color.white, 0f, 0.78f, dice, glow ? dice : null, Glow(spec, 1.2f));
            LitMaterial(spec, "SpeedDie", Color.white, 0f, 0.78f, speed, glow ? speed : null, Glow(spec, 1.2f));
            LitMaterial(spec, "CardChance", Color.white, 0f, 0.4f, chance, glow ? chance : null, Glow(spec, 0.9f));
            LitMaterial(spec, "CardChest", Color.white, 0f, 0.4f, chest, glow ? chest : null, Glow(spec, 0.9f));
            LitMaterial(spec, "CardEdge", spec.CardEdge, 0f, 0.3f, null);
            for (int seat = 0; seat < spec.Seats.Length; seat++)
            {
                Color color = spec.Seats[seat].color;
                LitMaterial(spec, $"Seat_{seat}", color, 0.15f, 0.7f, null, null, glow ? color * 0.55f : Color.black);
                TransparentMaterial(spec, $"TurnRing_{seat}", Color.Lerp(color, Color.white, 0.25f), null, false);
            }
            TransparentMaterial(spec, "Highlight", Color.white, Texture("Highlight", spec), true);
            TransparentMaterial(spec, "Mortgaged", spec.MortgagedShade, null, false);
            TransparentMaterial(spec, "ContactShadow", Color.white, Texture("ContactShadow", spec), false);
            TransparentMaterial(spec, "Logo", Color.white, Texture("Logo", spec), false);
        }

        /// <summary>How strongly a material of an emissive look glows through its emission map; black (no glow) for the others.</summary>
        private static Color Glow(MonopolyThemeSpec spec, float amount)
        {
            return spec.Emissive ? new Color(amount, amount, amount) : Color.black;
        }

        /// <summary>A lit material; with an <paramref name="emission"/> map or a colour above black it glows.</summary>
        private static Material LitMaterial(MonopolyThemeSpec spec, string name, Color color, float metallic, float smoothness, Texture2D texture,
            Texture2D emission = null, Color emissionColor = default)
        {
            bool glows = emission != null || emissionColor.maxColorComponent > 0f;
            return MonopolyAssets.SaveMaterial($"{spec.MaterialsFolder}/{name}.mat", Lit, m =>
            {
                m.SetColor("_BaseColor", color);
                m.SetTexture("_BaseMap", texture);
                m.SetFloat("_Metallic", metallic);
                m.SetFloat("_Smoothness", smoothness);
                m.SetFloat("_Surface", 0f);
                m.SetTexture("_EmissionMap", glows ? emission : null);
                m.SetColor("_EmissionColor", glows ? emissionColor : Color.black);
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

        /// <summary>An unlit transparent material: alpha blended, or additive for glows.</summary>
        private static Material TransparentMaterial(MonopolyThemeSpec spec, string name, Color color, Texture2D texture, bool additive)
        {
            return MonopolyAssets.SaveMaterial($"{spec.MaterialsFolder}/{name}.mat", Unlit, m =>
            {
                m.SetColor("_BaseColor", color);
                m.SetTexture("_BaseMap", texture);
                m.SetFloat("_Surface", 1f);
                m.SetFloat("_Blend", additive ? 2f : 0f);
                m.SetFloat("_SrcBlend", additive ? (float)BlendMode.SrcAlpha : (float)BlendMode.SrcAlpha);
                m.SetFloat("_DstBlend", additive ? (float)BlendMode.One : (float)BlendMode.OneMinusSrcAlpha);
                if (m.HasProperty("_SrcBlendAlpha"))
                {
                    m.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
                    m.SetFloat("_DstBlendAlpha", additive ? (float)BlendMode.One : (float)BlendMode.OneMinusSrcAlpha);
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

        // ------------------------------------------------------------------ fonts

        private const string Latin =
            " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~" +
            "¡£©«®°±·»¿ÀÁÂÃÄÅÆÇÈÉÊË" +
            "ÌÍÎÏÑÒÓÔÕÖ×ØÙÚÛÜßàáâãä" +
            "åæçèéêëìíîïñòóôõö÷øùúû" +
            "üÿ–—‘’“”•…€";

        private static void BuildFonts(MonopolyThemeSpec spec)
        {
            // The icons are baked once, from the classic look's Font Awesome file, and fall back from every font.
            TMP_FontAsset icons = BakeFont(Classic, "fa-solid-900.ttf", "Icons", Icons.All, 64, 8, 1024, 1024);
            TMP_FontAsset fallback = TMP_Settings.defaultFontAsset;
            var specs = new List<FontSpec> { spec.Body, spec.Bold, spec.Heavy };
            if (specs.All(f => f.name != spec.Title.name))
            {
                specs.Add(spec.Title);
            }
            foreach (FontSpec fontSpec in specs)
            {
                TMP_FontAsset font = BakeFont(spec, fontSpec.file, fontSpec.name, Latin, 56, 7, 1024, 1024);
                MonopolyAssets.ApplyIfChanged(font, () =>
                {
                    var table = new List<TMP_FontAsset> { icons };
                    if (fallback != null)
                    {
                        table.Add(fallback);
                    }
                    if (font.fallbackFontAssetTable == null || !font.fallbackFontAssetTable.SequenceEqual(table))
                    {
                        font.fallbackFontAssetTable = table;
                    }
                });
            }
            TMP_FontAsset bold = BoldOf(spec);
            Material baseMaterial = bold.material;
            // A soft shadow for text over the 3D board and for titles.
            MonopolyAssets.SaveMaterial($"{spec.FontsFolder}/{spec.Bold.name} Shadow.mat", baseMaterial.shader, m =>
            {
                m.CopyPropertiesFromMaterial(baseMaterial);
                m.EnableKeyword("UNDERLAY_ON");
                m.SetColor("_UnderlayColor", new Color(0f, 0f, 0.05f, 0.45f));
                m.SetFloat("_UnderlayOffsetX", 0.3f);
                m.SetFloat("_UnderlayOffsetY", -0.6f);
                m.SetFloat("_UnderlaySoftness", 0.35f);
            });
            TMP_FontAsset title = TitleOf(spec);
            Material titleBase = title.material;
            MonopolyAssets.SaveMaterial($"{spec.FontsFolder}/{spec.Title.name} Title.mat", titleBase.shader, m =>
            {
                m.CopyPropertiesFromMaterial(titleBase);
                m.EnableKeyword("OUTLINE_ON");
                m.EnableKeyword("UNDERLAY_ON");
                m.SetFloat("_OutlineWidth", 0.12f);
                m.SetColor("_OutlineColor", spec.TitleOutline);
                m.SetColor("_UnderlayColor", spec.TitleUnderlay);
                m.SetFloat("_UnderlayOffsetX", spec.Emissive ? 0f : 0.4f);
                m.SetFloat("_UnderlayOffsetY", spec.Emissive ? 0f : -0.8f);
                m.SetFloat("_UnderlaySoftness", spec.Emissive ? 0.6f : 0.15f);
                m.SetFloat("_FaceDilate", 0.1f);
            });
        }

        /// <summary>
        /// Bakes a font file of a look into a static TextMesh Pro font asset (atlas and material as sub-assets). The
        /// asset is only created when missing, so rebuilding keeps its GUID and bytes; missing characters are added to it.
        /// </summary>
        private static TMP_FontAsset BakeFont(MonopolyThemeSpec spec, string file, string name, string characters, int pointSize, int padding, int width, int height)
        {
            string path = $"{spec.FontsFolder}/{name} SDF.asset";
            var fontAsset = MonopolyAssets.Load<TMP_FontAsset>(path);
            if (fontAsset == null)
            {
                string sourcePath = MonopolyAssets.Path($"{spec.FontsFolder}/{file}");
                AssetDatabase.ImportAsset(sourcePath, ImportAssetOptions.ForceSynchronousImport);
                var source = AssetDatabase.LoadAssetAtPath<Font>(sourcePath);
                if (source == null)
                {
                    throw new System.InvalidOperationException($"Monopoly: the font file {sourcePath} is missing.");
                }
                fontAsset = TMP_FontAsset.CreateFontAsset(source, pointSize, padding, GlyphRenderMode.SDFAA, width, height, AtlasPopulationMode.Dynamic, false);
                fontAsset.name = $"{name} SDF";
                fontAsset.TryAddCharacters(characters, out string missing);
                if (!string.IsNullOrEmpty(missing))
                {
                    Debug.Log($"Monopoly: {name} has no glyphs for \"{missing}\".");
                }
                fontAsset.atlasPopulationMode = AtlasPopulationMode.Static;
                MonopolyAssets.EnsureFolder(spec.FontsFolder);
                AssetDatabase.CreateAsset(fontAsset, MonopolyAssets.Path(path));
                Texture2D atlas = fontAsset.atlasTexture;
                atlas.name = $"{name} SDF Atlas";
                AssetDatabase.AddObjectToAsset(atlas, fontAsset);
                Material material = fontAsset.material;
                material.name = $"{name} SDF Material";
                AssetDatabase.AddObjectToAsset(material, fontAsset);
                EditorUtility.SetDirty(fontAsset);
                AssetDatabase.SaveAssets();
                AssetDatabase.ImportAsset(MonopolyAssets.Path(path));
                fontAsset = MonopolyAssets.Load<TMP_FontAsset>(path);
            }
            else if (!fontAsset.HasCharacters(characters, out uint[] absent, false, false) && absent != null && absent.Length > 0)
            {
                // Characters the game started to use since the font was baked: add them to the existing atlas.
                var added = new System.Text.StringBuilder();
                foreach (uint unicode in absent)
                {
                    added.Append(char.ConvertFromUtf32((int)unicode));
                }
                fontAsset.atlasPopulationMode = AtlasPopulationMode.Dynamic;
                fontAsset.TryAddCharacters(added.ToString(), out string missing);
                fontAsset.atlasPopulationMode = AtlasPopulationMode.Static;
                if (!string.IsNullOrEmpty(missing))
                {
                    Debug.Log($"Monopoly: {name} has no glyphs for \"{missing}\".");
                }
                EditorUtility.SetDirty(fontAsset);
                EditorUtility.SetDirty(fontAsset.atlasTexture);
                AssetDatabase.SaveAssets();
            }
            return fontAsset;
        }

        // ------------------------------------------------------------------ sound

        private static void BuildAudio()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:AudioClip", new[] { MonopolyAssets.Path(KenneyFolder) }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                MonopolyAssets.ConfigureAudio(path.Substring(MonopolyAssets.Root.Length + 1), false);
            }
            MonopolyAssets.SaveAudio(SoundFactory.Wav(SoundFactory.GameLoop()), $"{GeneratedAudioFolder}/GameLoop.wav", true);
            MonopolyAssets.SaveAudio(SoundFactory.Wav(SoundFactory.MenuLoop()), $"{GeneratedAudioFolder}/MenuLoop.wav", true);
            MonopolyAssets.SaveAudio(SoundFactory.Wav(SoundFactory.Fanfare()), $"{GeneratedAudioFolder}/Fanfare.wav", false);
            MonopolyAssets.SaveAudio(SoundFactory.Wav(SoundFactory.JailDoor()), $"{GeneratedAudioFolder}/JailDoor.wav", false);
            MonopolyAssets.SaveAudio(SoundFactory.Wav(SoundFactory.Gavel()), $"{GeneratedAudioFolder}/Gavel.wav", false);
            MonopolyAssets.SaveAudio(SoundFactory.Wav(SoundFactory.Whoosh()), $"{GeneratedAudioFolder}/Whoosh.wav", false);
            MonopolyAssets.SaveAudio(SoundFactory.Wav(SoundFactory.SadTrombone()), $"{GeneratedAudioFolder}/SadTrombone.wav", false);
        }
    }
}
