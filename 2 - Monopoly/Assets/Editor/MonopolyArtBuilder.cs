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
    /// Builds the art and sound of the game into the package: generated textures, meshes and URP materials, the
    /// TextMesh Pro fonts (Poppins and the Font Awesome icons, baked from the files in Art/Fonts), the Kenney sounds
    /// (configured) and the synthesised music and effects. Assets are updated in place and only rewritten when they
    /// changed.
    /// </summary>
    internal static class MonopolyArtBuilder
    {
        public const string TexturesFolder = "Art/Textures";
        public const string UIFolder = "Art/UI";
        public const string TokensFolder = "Art/Tokens";
        public const string MeshesFolder = "Art/Meshes";
        public const string MaterialsFolder = "Art/Materials";
        public const string FontsFolder = "Art/Fonts";
        public const string KenneyFolder = "Audio/Kenney";
        public const string GeneratedAudioFolder = "Audio/Generated";

        public const float Corner = 1.6f;
        public const float Width = 1f;
        public const int BoardPixels = 4096;

        public static BoardGeometry Geometry => new BoardGeometry(Corner, Width);

        public static void BuildAll()
        {
            Step("Textures", 0.1f);
            BuildTextures();
            Step("Meshes", 0.45f);
            BuildMeshes();
            Step("Materials", 0.6f);
            BuildMaterials();
            Step("Fonts", 0.75f);
            BuildFonts();
            Step("Sound", 0.9f);
            BuildAudio();
            AssetDatabase.SaveAssets();
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

        public static Sprite UI(string name)
        {
            return Sprite($"{UIFolder}/{name}");
        }

        public static Sprite Badge(int token)
        {
            return Sprite($"{TokensFolder}/Badge_{token}");
        }

        public static Mesh Mesh(string name)
        {
            return MonopolyAssets.Require<Mesh>($"{MeshesFolder}/{name}.asset");
        }

        public static Material Material(string name)
        {
            return MonopolyAssets.Require<Material>($"{MaterialsFolder}/{name}.mat");
        }

        public static TMP_FontAsset Font(string name)
        {
            return MonopolyAssets.Require<TMP_FontAsset>($"{FontsFolder}/{name} SDF.asset");
        }

        public static TMP_FontAsset Body => Font("Poppins SemiBold");
        public static TMP_FontAsset Bold => Font("Poppins Bold");
        public static TMP_FontAsset Heavy => Font("Poppins Black");
        public static TMP_FontAsset IconFont => Font("Icons");

        public static AudioClip Clip(string name)
        {
            AudioClip clip = MonopolyAssets.Load<AudioClip>($"{KenneyFolder}/{name}.ogg") ?? MonopolyAssets.Load<AudioClip>($"{GeneratedAudioFolder}/{name}.wav");
            if (clip == null)
            {
                throw new System.InvalidOperationException($"Monopoly: sound {name} is missing.");
            }
            return clip;
        }

        public static Cubemap Reflection => MonopolyAssets.Load<Cubemap>($"{TexturesFolder}/Reflection.png");

        // ------------------------------------------------------------------ textures

        private static void BuildTextures()
        {
            var board = WorldTourBoard.Create();
            MonopolyAssets.SaveTexture(MonopolyArt.Board(Geometry, board.spaces, BoardPixels), $"{TexturesFolder}/Board.png", MonopolyAssets.SurfaceTexture(false, BoardPixels));
            MonopolyAssets.SaveTexture(MonopolyArt.Table(1024), $"{TexturesFolder}/Table.png", MonopolyAssets.SurfaceTexture(true, 1024));
            MonopolyAssets.SaveTexture(MonopolyArt.DiceAtlas(false, 256), $"{TexturesFolder}/Dice.png", MonopolyAssets.SurfaceTexture(false, 1024, false));
            MonopolyAssets.SaveTexture(MonopolyArt.DiceAtlas(true, 256), $"{TexturesFolder}/SpeedDie.png", MonopolyAssets.SurfaceTexture(false, 1024, false));
            MonopolyAssets.SaveTexture(MonopolyArt.CardBack(MonopolyStyle.ChanceOrange, 320, 480), $"{TexturesFolder}/CardChance.png", MonopolyAssets.SurfaceTexture(false, 512, false));
            MonopolyAssets.SaveTexture(MonopolyArt.CardBack(MonopolyStyle.ChestBlue, 320, 480), $"{TexturesFolder}/CardChest.png", MonopolyAssets.SurfaceTexture(false, 512, false));
            MonopolyAssets.SaveTexture(MonopolyArt.HighlightFrame(256), $"{TexturesFolder}/Highlight.png", MonopolyAssets.DecalTexture(256));
            MonopolyAssets.SaveTexture(MonopolyArt.LogoBanner(1024, 300), $"{TexturesFolder}/Logo.png", MonopolyAssets.DecalTexture(1024));
            MonopolyAssets.SaveTexture(MonopolyArt.ContactShadow(128), $"{TexturesFolder}/ContactShadow.png", MonopolyAssets.DecalTexture(128));
            MonopolyAssets.SaveTexture(ReflectionStrip(128), $"{TexturesFolder}/Reflection.png", importer =>
            {
                importer.textureShape = TextureImporterShape.TextureCube;
                importer.generateCubemap = TextureImporterGenerateCubemap.FullCubemap;
                importer.mipmapEnabled = true;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.sRGBTexture = true;
            });

            MonopolyAssets.SaveTexture(MonopolyArt.Rounded(128, 40f), $"{UIFolder}/Rounded.png", MonopolyAssets.SpriteTexture(new Vector4(48, 48, 48, 48)));
            MonopolyAssets.SaveTexture(MonopolyArt.Shadow(128, 36f, 22f), $"{UIFolder}/Shadow.png", MonopolyAssets.SpriteTexture(new Vector4(60, 60, 60, 60)));
            MonopolyAssets.SaveTexture(MonopolyArt.Circle(256), $"{UIFolder}/Circle.png", MonopolyAssets.SpriteTexture());
            MonopolyAssets.SaveTexture(MonopolyArt.Ring(256, 22f), $"{UIFolder}/Ring.png", MonopolyAssets.SpriteTexture());
            MonopolyAssets.SaveTexture(MonopolyArt.Glow(256), $"{UIFolder}/Glow.png", MonopolyAssets.SpriteTexture());
            MonopolyAssets.SaveTexture(MonopolyArt.Sheen(16, 128), $"{UIFolder}/Sheen.png", MonopolyAssets.SpriteTexture());
            MonopolyAssets.SaveTexture(MonopolyArt.Bill(192, 96), $"{UIFolder}/Bill.png", MonopolyAssets.SpriteTexture());
            MonopolyAssets.SaveTexture(MonopolyArt.LogoBanner(1024, 300), $"{UIFolder}/Logo.png", MonopolyAssets.SpriteTexture(new Vector4(60, 60, 60, 60)));
            for (int token = 0; token < MonopolyStyle.TokenCount; token++)
            {
                MonopolyAssets.SaveTexture(MonopolyArt.TokenBadge(token, 256), $"{TokensFolder}/Badge_{token}.png", MonopolyAssets.SpriteTexture());
            }
        }

        /// <summary>
        /// A soft studio for the metal to reflect, as a horizontal strip of six cube faces (+X, -X, +Y, -Y, +Z, -Z):
        /// a warm ceiling with two soft boxes, a neutral horizon and a dark floor.
        /// </summary>
        private static Texture2D ReflectionStrip(int face)
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
                        raster.Set(f * face + x, y, Studio(d.normalized));
                    }
                }
            }
            return raster.ToTexture(false);
        }

        private static Color Studio(Vector3 d)
        {
            Color floor = new Color(0.16f, 0.12f, 0.1f);
            Color horizon = new Color(0.62f, 0.6f, 0.58f);
            Color ceiling = new Color(0.95f, 0.92f, 0.86f);
            Color c = d.y >= 0f ? Color.Lerp(horizon, ceiling, Mathf.Pow(d.y, 0.6f)) : Color.Lerp(horizon, floor, Mathf.Pow(-d.y, 0.5f));
            // Two soft boxes overhead and a rim light, which give the pewter its highlights.
            float light = Box(d, new Vector3(-0.4f, 0.85f, -0.3f).normalized, 0.28f) * 1.4f
                + Box(d, new Vector3(0.6f, 0.6f, 0.4f).normalized, 0.22f) * 0.9f
                + Box(d, new Vector3(0f, 0.2f, -1f).normalized, 0.3f) * 0.5f;
            c += new Color(light, light, light, 0f);
            c.a = 1f;
            return new Color(Mathf.Min(c.r, 1f), Mathf.Min(c.g, 1f), Mathf.Min(c.b, 1f), 1f);
        }

        private static float Box(Vector3 d, Vector3 center, float size)
        {
            float angle = Mathf.Acos(Mathf.Clamp(Vector3.Dot(d, center), -1f, 1f));
            return Mathf.Clamp01(1f - angle / size);
        }

        // ------------------------------------------------------------------ meshes

        private static void BuildMeshes()
        {
            for (int token = 0; token < MonopolyStyle.TokenCount; token++)
            {
                MonopolyAssets.SaveMesh(MonopolyModels.Token(token), $"{MeshesFolder}/Token_{token}.asset");
            }
            MonopolyAssets.SaveMesh(MonopolyModels.TokenBase(), $"{MeshesFolder}/TokenBase.asset");
            MonopolyAssets.SaveMesh(MonopolyModels.TurnRing(), $"{MeshesFolder}/TurnRing.asset");
            MonopolyAssets.SaveMesh(MonopolyModels.House(), $"{MeshesFolder}/House.asset");
            MonopolyAssets.SaveMesh(MonopolyModels.Hotel(), $"{MeshesFolder}/Hotel.asset");
            MonopolyAssets.SaveMesh(MonopolyModels.Die(), $"{MeshesFolder}/Die.asset");
            MonopolyAssets.SaveMesh(MonopolyModels.BoardSlab(Geometry.Side, 0.3f, 0.2f), $"{MeshesFolder}/BoardSlab.asset");
            MonopolyAssets.SaveMesh(MonopolyModels.BoardTop(Geometry.Side), $"{MeshesFolder}/BoardTop.asset");
            MonopolyAssets.SaveMesh(MonopolyModels.Table(90f, 9f), $"{MeshesFolder}/Table.asset");
            MonopolyAssets.SaveMesh(MonopolyModels.Quad(), $"{MeshesFolder}/FlatQuad.asset");
            MonopolyAssets.SaveMesh(MonopolyModels.CardStack(1.2f, 1.8f, 0.14f), $"{MeshesFolder}/CardStack.asset");
            MonopolyAssets.SaveMesh(MonopolyModels.OwnerTag(Width - 0.14f, 0.13f), $"{MeshesFolder}/OwnerTag.asset");
        }

        // ------------------------------------------------------------------ materials

        private static Shader Lit => Shader.Find("Universal Render Pipeline/Lit");
        private static Shader Unlit => Shader.Find("Universal Render Pipeline/Unlit");

        private static void BuildMaterials()
        {
            Texture2D board = MonopolyAssets.Load<Texture2D>($"{TexturesFolder}/Board.png");
            Texture2D table = MonopolyAssets.Load<Texture2D>($"{TexturesFolder}/Table.png");
            LitMaterial("Board", Color.white, 0f, 0.12f, board);
            LitMaterial("BoardSlab", MonopolyStyle.Hex(0x18202B), 0.1f, 0.55f, null);
            LitMaterial("Table", Color.white, 0f, 0.42f, table);
            LitMaterial("Pewter", MonopolyStyle.Hex(0xC7CBD1), 0.92f, 0.74f, null);
            LitMaterial("House", MonopolyStyle.Hex(0x1FA355), 0f, 0.62f, null);
            LitMaterial("Hotel", MonopolyStyle.Hex(0xD7263D), 0f, 0.62f, null);
            LitMaterial("Die", Color.white, 0f, 0.78f, MonopolyAssets.Load<Texture2D>($"{TexturesFolder}/Dice.png"));
            LitMaterial("SpeedDie", Color.white, 0f, 0.78f, MonopolyAssets.Load<Texture2D>($"{TexturesFolder}/SpeedDie.png"));
            LitMaterial("CardChance", Color.white, 0f, 0.4f, MonopolyAssets.Load<Texture2D>($"{TexturesFolder}/CardChance.png"));
            LitMaterial("CardChest", Color.white, 0f, 0.4f, MonopolyAssets.Load<Texture2D>($"{TexturesFolder}/CardChest.png"));
            LitMaterial("CardEdge", MonopolyStyle.Hex(0xF3EFE4), 0f, 0.3f, null);
            for (int seat = 0; seat < 4; seat++)
            {
                LitMaterial($"Seat_{seat}", MonopolyStyle.PlayerColor(seat), 0.15f, 0.7f, null);
                TransparentMaterial($"TurnRing_{seat}", MonopolyStyle.Tint(MonopolyStyle.PlayerColor(seat), 0.25f), null, false);
            }
            TransparentMaterial("Highlight", Color.white, MonopolyAssets.Load<Texture2D>($"{TexturesFolder}/Highlight.png"), true);
            TransparentMaterial("Mortgaged", new Color(0.05f, 0.06f, 0.1f, 0.62f), null, false);
            TransparentMaterial("ContactShadow", Color.white, MonopolyAssets.Load<Texture2D>($"{TexturesFolder}/ContactShadow.png"), false);
            TransparentMaterial("Logo", Color.white, MonopolyAssets.Load<Texture2D>($"{TexturesFolder}/Logo.png"), false);
        }

        private static Material LitMaterial(string name, Color color, float metallic, float smoothness, Texture2D texture)
        {
            return MonopolyAssets.SaveMaterial($"{MaterialsFolder}/{name}.mat", Lit, m =>
            {
                m.SetColor("_BaseColor", color);
                m.SetTexture("_BaseMap", texture);
                m.SetFloat("_Metallic", metallic);
                m.SetFloat("_Smoothness", smoothness);
                m.SetFloat("_Surface", 0f);
                m.enableInstancing = true;
            });
        }

        /// <summary>An unlit transparent material: alpha blended, or additive for glows.</summary>
        private static Material TransparentMaterial(string name, Color color, Texture2D texture, bool additive)
        {
            return MonopolyAssets.SaveMaterial($"{MaterialsFolder}/{name}.mat", Unlit, m =>
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

        private static void BuildFonts()
        {
            TMP_FontAsset icons = BakeFont("fa-solid-900.ttf", "Icons", Icons.All, 64, 8, 1024, 1024);
            TMP_FontAsset fallback = TMP_Settings.defaultFontAsset;
            foreach ((string file, string name) in new[] { ("Poppins-SemiBold.ttf", "Poppins SemiBold"), ("Poppins-Bold.ttf", "Poppins Bold"), ("Poppins-Black.ttf", "Poppins Black") })
            {
                TMP_FontAsset font = BakeFont(file, name, Latin, 56, 7, 1024, 1024);
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
            TMP_FontAsset bold = Bold;
            Material baseMaterial = bold.material;
            // A soft shadow for text over the 3D board and for titles.
            MonopolyAssets.SaveMaterial($"{FontsFolder}/Poppins Bold Shadow.mat", baseMaterial.shader, m =>
            {
                m.CopyPropertiesFromMaterial(baseMaterial);
                m.EnableKeyword("UNDERLAY_ON");
                m.SetColor("_UnderlayColor", new Color(0f, 0f, 0.05f, 0.45f));
                m.SetFloat("_UnderlayOffsetX", 0.3f);
                m.SetFloat("_UnderlayOffsetY", -0.6f);
                m.SetFloat("_UnderlaySoftness", 0.35f);
            });
            TMP_FontAsset heavy = Heavy;
            Material heavyMaterial = heavy.material;
            MonopolyAssets.SaveMaterial($"{FontsFolder}/Poppins Black Title.mat", heavyMaterial.shader, m =>
            {
                m.CopyPropertiesFromMaterial(heavyMaterial);
                m.EnableKeyword("OUTLINE_ON");
                m.EnableKeyword("UNDERLAY_ON");
                m.SetFloat("_OutlineWidth", 0.12f);
                m.SetColor("_OutlineColor", new Color(0.35f, 0f, 0.05f, 1f));
                m.SetColor("_UnderlayColor", new Color(0.1f, 0f, 0.02f, 0.6f));
                m.SetFloat("_UnderlayOffsetX", 0.4f);
                m.SetFloat("_UnderlayOffsetY", -0.8f);
                m.SetFloat("_UnderlaySoftness", 0.15f);
                m.SetFloat("_FaceDilate", 0.1f);
            });
        }

        public static Material TextShadow => MonopolyAssets.Require<Material>($"{FontsFolder}/Poppins Bold Shadow.mat");
        public static Material TitleMaterial => MonopolyAssets.Require<Material>($"{FontsFolder}/Poppins Black Title.mat");

        /// <summary>
        /// Bakes a font file into a static TextMesh Pro font asset (atlas and material as sub-assets). The asset is only
        /// created when missing, so rebuilding keeps its GUID and bytes; missing characters are added to it.
        /// </summary>
        private static TMP_FontAsset BakeFont(string file, string name, string characters, int pointSize, int padding, int width, int height)
        {
            string path = $"{FontsFolder}/{name} SDF.asset";
            var fontAsset = MonopolyAssets.Load<TMP_FontAsset>(path);
            if (fontAsset == null)
            {
                string sourcePath = MonopolyAssets.Path($"{FontsFolder}/{file}");
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
                MonopolyAssets.EnsureFolder(FontsFolder);
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
