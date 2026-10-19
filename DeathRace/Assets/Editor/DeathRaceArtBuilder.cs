using Gamebox.Editor;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Portfolio.DeathRace.EditorTools
{
    /// <summary>
    /// Generates the art of the game: first the car side (<see cref="DeathRacePrefabBuilder.BuildArt"/>: car and item
    /// meshes, materials and sounds), then the world: the surfaces of the four biomes (DeathRaceArtBuilder.Surfaces.cs),
    /// the scenery and lamps in the palette of each look (DeathRaceArtBuilder.Props.cs), the interface sprites, icons and
    /// the launcher icon (DeathRaceArtBuilder.Interface.cs), and per look the reflected sky, the post processing and the
    /// title font's material (DeathRaceArtBuilder.Looks.cs). Everything is seeded and written in place through
    /// <see cref="DeathRaceAssets"/>, so a second run writes nothing and every GUID stays.
    ///
    /// The UV convention agreed with the track builder (TRACK writes the UVs, the materials keep a tiling of 1): road U 0
    /// at the left edge to 1 at the right, V = metres along / 16; shoulder U 0 at the road to 1 at the barrier, V = metres
    /// / 8; curb U 0 at the road to 1 outside, V = metres / 4 (one stripe of each colour); barrier U = metres along / 4,
    /// V 0 at the foot to 1 at the top, the top cap at V 0.9-1; ground UV = world xz / 32; line U = metres across / 4,
    /// V = metres along / 4.
    /// </summary>
    internal static partial class DeathRaceArtBuilder
    {
        public const string IconsFolder = "Art/Icons";
        public const string InterfaceFolder = "Art/UI";
        public const string AudioFolder = "Audio/Generated";

        private const string FontsFolder = "Prefabs/UI/MainMenu/Fonts";

        private static Shader Lit => Shader.Find("Universal Render Pipeline/Lit");
        private static Shader Unlit => Shader.Find("Universal Render Pipeline/Unlit");

        /// <summary>BaseGame's Poppins Black: titles and headers.</summary>
        public static TMP_FontAsset TitleFont => BaseFont("Poppins Black SDF");

        /// <summary>BaseGame's Poppins Bold: buttons and labels.</summary>
        public static TMP_FontAsset BoldFont => BaseFont("Poppins Bold SDF");

        /// <summary>BaseGame's Poppins SemiBold: body text, names and rows.</summary>
        public static TMP_FontAsset BodyFont => BaseFont("Poppins SemiBold SDF");

        public static void BuildAll()
        {
            Progress("Cars, items and sounds", 0.02f);
            DeathRacePrefabBuilder.BuildArt();
            BuildWorld();
        }

        /// <summary>The world's art alone (no car side): the surfaces, the scenery, the interface and the looks.</summary>
        public static void BuildWorld()
        {
            try
            {
                Progress("Surfaces", 0.25f);
                BuildSurfaces();
                Progress("Scenery", 0.55f);
                BuildProps();
                Progress("Interface", 0.75f);
                BuildInterface();
                Progress("Skies and post processing", 0.92f);
                BuildLooks();
                AssetDatabase.SaveAssets();
            }
            finally
            {
                if (!Application.isBatchMode)
                {
                    EditorUtility.ClearProgressBar();
                }
            }
        }

        /// <summary>A sprite of the interface folder (e.g. "Rounded", "Scorched/Window"), or null when it was not built.</summary>
        public static Sprite UI(string name)
        {
            return DeathRaceAssets.LoadSprite($"{InterfaceFolder}/{name}.png");
        }

        /// <summary>An icon (e.g. "GameIcon", "Nightfall/Rocket"), or null when it was not built.</summary>
        public static Sprite Icon(string name)
        {
            return DeathRaceAssets.LoadSprite($"{IconsFolder}/{name}.png");
        }

        public static AudioClip Clip(string name)
        {
            return DeathRaceAssets.Load<AudioClip>($"{AudioFolder}/{name}.wav");
        }

        private static TMP_FontAsset BaseFont(string name)
        {
            string path = GameMenuInstaller.PackagePath($"{FontsFolder}/{name}.asset");
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (font == null)
            {
                throw new System.InvalidOperationException($"BaseGame font {path} is missing");
            }
            return font;
        }

        private static void Progress(string step, float fraction)
        {
            if (!Application.isBatchMode)
            {
                EditorUtility.DisplayProgressBar("Death Race art", step, fraction);
            }
        }

        // ------------------------------------------------------------------------------------------------
        // Materials
        // ------------------------------------------------------------------------------------------------

        /// <summary>What a URP Lit material of the world is made of; unset maps leave their feature off.</summary>
        private struct LitSetup
        {
            public Color Color;
            public Texture BaseMap;
            public Texture NormalMap;
            public float NormalScale;
            public float Smoothness;
            /// <summary>Smoothness = the base map's alpha times <see cref="Smoothness"/> (the wet spots of a surface).</summary>
            public bool SmoothnessFromAlpha;
            public float Metallic;
            /// <summary>URP's metallic map (R metallic, A smoothness), for the palettes.</summary>
            public Texture MetallicMap;
            public Color Emission;
            public Texture EmissionMap;
            /// <summary>A detail albedo multiplied in at twice its value (the macro variation of the ground).</summary>
            public Texture Detail;
            public float DetailTiling;
        }

        /// <summary>
        /// Writes a URP Lit material, opaque, with its keywords set by hand (the shader's inspector is not there to do it in
        /// a batch run): normal map, metallic map, smoothness from the albedo's alpha, emission and detail.
        /// </summary>
        private static Material SaveLit(string relative, LitSetup setup)
        {
            return DeathRaceAssets.SaveMaterial(relative, Lit, m =>
            {
                m.SetFloat("_WorkflowMode", 1f);
                m.SetFloat("_Surface", 0f);
                m.SetFloat("_Blend", 0f);
                m.SetFloat("_AlphaClip", 0f);
                m.SetFloat("_Cull", (float)CullMode.Back);
                m.SetFloat("_SrcBlend", (float)BlendMode.One);
                m.SetFloat("_DstBlend", (float)BlendMode.Zero);
                m.SetFloat("_ZWrite", 1f);
                m.SetOverrideTag("RenderType", "Opaque");
                m.renderQueue = (int)RenderQueue.Geometry;
                m.SetColor("_BaseColor", setup.Color);
                m.SetTexture("_BaseMap", setup.BaseMap);
                m.SetTextureScale("_BaseMap", Vector2.one);
                m.SetFloat("_Smoothness", setup.Smoothness);
                m.SetFloat("_Metallic", setup.Metallic);
                m.SetFloat("_EnvironmentReflections", 1f);
                m.SetFloat("_SpecularHighlights", 1f);
                m.SetFloat("_ReceiveShadows", 1f);
                m.DisableKeyword("_ENVIRONMENTREFLECTIONS_OFF");
                m.DisableKeyword("_SPECULARHIGHLIGHTS_OFF");
                m.DisableKeyword("_RECEIVE_SHADOWS_OFF");
                m.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
                m.DisableKeyword("_ALPHATEST_ON");

                m.SetTexture("_BumpMap", setup.NormalMap);
                m.SetFloat("_BumpScale", setup.NormalMap != null ? setup.NormalScale : 1f);
                Toggle(m, "_NORMALMAP", setup.NormalMap != null);

                m.SetTexture("_MetallicGlossMap", setup.MetallicMap);
                Toggle(m, "_METALLICSPECGLOSSMAP", setup.MetallicMap != null);
                m.SetFloat("_SmoothnessTextureChannel", setup.SmoothnessFromAlpha ? 1f : 0f);
                Toggle(m, "_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A", setup.SmoothnessFromAlpha);

                bool glows = setup.Emission.maxColorComponent > 0f;
                m.SetColor("_EmissionColor", glows ? setup.Emission : Color.black);
                m.SetTexture("_EmissionMap", glows ? setup.EmissionMap : null);
                Toggle(m, "_EMISSION", glows);
                m.globalIlluminationFlags = glows ? MaterialGlobalIlluminationFlags.RealtimeEmissive : MaterialGlobalIlluminationFlags.EmissiveIsBlack;

                m.SetTexture("_DetailAlbedoMap", setup.Detail);
                m.SetTextureScale("_DetailAlbedoMap", Vector2.one * (setup.Detail != null ? setup.DetailTiling : 1f));
                m.SetFloat("_DetailAlbedoMapScale", 1f);
                Toggle(m, "_DETAIL_MULX2", setup.Detail != null);
                m.DisableKeyword("_DETAIL_SCALED");
                m.enableInstancing = true;
            });
        }

        /// <summary>An unlit additive material (the light pools of the lamps).</summary>
        private static Material SaveAdditive(string relative, Texture texture, Color color)
        {
            return DeathRaceAssets.SaveMaterial(relative, Unlit, m =>
            {
                m.SetColor("_BaseColor", color);
                m.SetTexture("_BaseMap", texture);
                m.SetFloat("_Surface", 1f);
                m.SetFloat("_Blend", 2f);
                m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                m.SetFloat("_DstBlend", (float)BlendMode.One);
                if (m.HasProperty("_SrcBlendAlpha"))
                {
                    m.SetFloat("_SrcBlendAlpha", (float)BlendMode.Zero);
                    m.SetFloat("_DstBlendAlpha", (float)BlendMode.One);
                }
                m.SetFloat("_ZWrite", 0f);
                m.SetFloat("_Cull", (float)CullMode.Back);
                m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                m.SetOverrideTag("RenderType", "Transparent");
                m.renderQueue = (int)RenderQueue.Transparent - 50;
                m.enableInstancing = true;
            });
        }

        private static void Toggle(Material material, string keyword, bool on)
        {
            if (on)
            {
                material.EnableKeyword(keyword);
            }
            else
            {
                material.DisableKeyword(keyword);
            }
        }

        // ------------------------------------------------------------------------------------------------
        // Texture importers
        // ------------------------------------------------------------------------------------------------

        /// <summary>A repeating colour picture of a surface; <paramref name="clampV"/> stops it wrapping up and down (the barriers).</summary>
        private static System.Action<TextureImporter> SurfaceImport(int maxSize, bool clampV = false)
        {
            return importer =>
            {
                GameAssets.SurfaceTexture(true, maxSize)(importer);
                importer.alphaSource = TextureImporterAlphaSource.FromInput;
                importer.wrapModeU = TextureWrapMode.Repeat;
                importer.wrapModeV = clampV ? TextureWrapMode.Clamp : TextureWrapMode.Repeat;
            };
        }

        private static System.Action<TextureImporter> NormalImport(int maxSize, bool clampV = false)
        {
            return importer =>
            {
                importer.textureShape = TextureImporterShape.Texture2D;
                importer.textureType = TextureImporterType.NormalMap;
                importer.convertToNormalmap = false;
                importer.wrapModeU = TextureWrapMode.Repeat;
                importer.wrapModeV = clampV ? TextureWrapMode.Clamp : TextureWrapMode.Repeat;
                importer.filterMode = FilterMode.Trilinear;
                importer.anisoLevel = 8;
                importer.mipmapEnabled = true;
                importer.maxTextureSize = maxSize;
                importer.textureCompression = TextureImporterCompression.CompressedHQ;
                importer.npotScale = TextureImporterNPOTScale.None;
            };
        }

        /// <summary>A linear (not colour) repeating texture: the macro variation.</summary>
        private static System.Action<TextureImporter> LinearImport(int maxSize)
        {
            return importer =>
            {
                GameAssets.SurfaceTexture(true, maxSize)(importer);
                importer.sRGBTexture = false;
            };
        }

        /// <summary>A palette: one texel per colour, never filtered or compressed.</summary>
        private static System.Action<TextureImporter> PaletteImport(bool linear)
        {
            return importer =>
            {
                importer.textureShape = TextureImporterShape.Texture2D;
                importer.textureType = TextureImporterType.Default;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.filterMode = FilterMode.Point;
                importer.mipmapEnabled = false;
                importer.sRGBTexture = !linear;
                importer.alphaSource = TextureImporterAlphaSource.FromInput;
                importer.alphaIsTransparency = false;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.npotScale = TextureImporterNPOTScale.None;
            };
        }
    }
}
