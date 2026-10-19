using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Portfolio.DeathRace.EditorTools
{
    /// <summary>How a transparent material of the cars, items and effects blends over what is behind it.</summary>
    internal enum Blending
    {
        /// <summary>Smoke, dust, shadows, scorch marks, skid marks.</summary>
        Alpha,
        /// <summary>Fire, flashes, sparks, glows, beams: light added to the scene.</summary>
        Additive
    }

    internal static partial class CarArt
    {
        public static Shader Lit => Shader.Find("Universal Render Pipeline/Lit");
        public static Shader ParticlesUnlit => Shader.Find("Universal Render Pipeline/Particles/Unlit");

        /// <summary>
        /// An opaque URP Lit surface: base colour and map, smoothness, metallic, and an emission map lit at
        /// <paramref name="emission"/> (the keyword stays on whenever an emission map is given, so a renderer's property
        /// block can light it brighter, as the brake lights do).
        /// </summary>
        public static void Surface(Material material, Color color, Texture map, float smoothness, float metallic,
            Texture emissionMap = null, Color? emission = null, bool reflections = false)
        {
            material.SetFloat("_WorkflowMode", 1f);
            material.SetFloat("_Surface", 0f);
            material.SetFloat("_Blend", 0f);
            material.SetFloat("_AlphaClip", 0f);
            material.SetFloat("_Cull", 2f);
            material.SetColor("_BaseColor", color);
            material.SetTexture("_BaseMap", map);
            material.SetFloat("_Smoothness", smoothness);
            material.SetFloat("_Metallic", metallic);
            material.SetFloat("_EnvironmentReflections", reflections ? 1f : 0f);
            material.SetFloat("_SpecularHighlights", 1f);
            material.SetFloat("_ReceiveShadows", 1f);
            SetKeyword(material, "_ENVIRONMENTREFLECTIONS_OFF", !reflections);
            material.DisableKeyword("_SPECULARHIGHLIGHTS_OFF");
            material.DisableKeyword("_RECEIVE_SHADOWS_OFF");
            material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.DisableKeyword("_ALPHATEST_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.SetFloat("_SrcBlend", (float)BlendMode.One);
            material.SetFloat("_DstBlend", (float)BlendMode.Zero);
            material.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
            material.SetFloat("_DstBlendAlpha", (float)BlendMode.Zero);
            material.SetFloat("_ZWrite", 1f);
            material.SetOverrideTag("RenderType", "Opaque");
            material.renderQueue = (int)RenderQueue.Geometry;
            material.SetShaderPassEnabled("DepthOnly", true);
            if (emissionMap != null || emission.HasValue)
            {
                material.SetColor("_EmissionColor", emission ?? Color.white);
                material.SetTexture("_EmissionMap", emissionMap);
                material.EnableKeyword("_EMISSION");
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            }
            else
            {
                material.SetColor("_EmissionColor", Color.black);
                material.SetTexture("_EmissionMap", null);
                material.DisableKeyword("_EMISSION");
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.EmissiveIsBlack;
            }
        }

        /// <summary>
        /// A transparent URP Particles/Unlit material (particles, glows, beams, decals and trails): the blend factors,
        /// keywords and queue that the material inspector would set, written here so no editor GUI code is needed.
        /// </summary>
        public static void Transparent(Material material, Texture map, Color color, Blending blend, bool twoSided = true, int queueOffset = 0)
        {
            material.SetTexture("_BaseMap", map);
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", blend == Blending.Additive ? 2f : 0f);
            material.SetFloat("_AlphaClip", 0f);
            material.SetFloat("_Cull", twoSided ? 0f : 2f);
            material.SetFloat("_ColorMode", 0f);
            material.SetFloat("_SoftParticlesEnabled", 0f);
            material.SetFloat("_CameraFadingEnabled", 0f);
            material.SetFloat("_DistortionEnabled", 0f);
            material.SetFloat("_FlipbookBlending", 0f);
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)(blend == Blending.Additive ? BlendMode.One : BlendMode.OneMinusSrcAlpha));
            material.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
            material.SetFloat("_DstBlendAlpha", (float)(blend == Blending.Additive ? BlendMode.One : BlendMode.OneMinusSrcAlpha));
            material.SetFloat("_ZWrite", 0f);
            material.SetFloat("_QueueOffset", queueOffset);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.DisableKeyword("_ALPHATEST_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.DisableKeyword("_ALPHAMODULATE_ON");
            material.DisableKeyword("_SOFTPARTICLES_ON");
            material.DisableKeyword("_FADING_ON");
            material.DisableKeyword("_DISTORTION_ON");
            material.DisableKeyword("_FLIPBOOKBLENDING_ON");
            material.DisableKeyword("_COLOROVERLAY_ON");
            material.DisableKeyword("_COLORCOLOR_ON");
            material.DisableKeyword("_COLORADDSUBDIFF_ON");
            material.SetOverrideTag("RenderType", "Transparent");
            material.renderQueue = (int)RenderQueue.Transparent + queueOffset;
            material.SetShaderPassEnabled("DepthOnly", false);
            material.enableInstancing = true;
        }

        private static void SetKeyword(Material material, string keyword, bool on)
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

        // ------------------------------------------------------------------ texture import

        /// <summary>A palette: one texel per colour, so no filtering, mipmaps or compression may blend them.</summary>
        public static void PaletteImport(TextureImporter importer)
        {
            importer.textureShape = TextureImporterShape.Texture2D;
            importer.textureType = TextureImporterType.Default;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.sRGBTexture = true;
            importer.alphaSource = TextureImporterAlphaSource.None;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.npotScale = TextureImporterNPOTScale.None;
        }

        /// <summary>A soft transparent texture of a particle, glow, beam or decal.</summary>
        public static void SoftImport(TextureImporter importer)
        {
            importer.textureShape = TextureImporterShape.Texture2D;
            importer.textureType = TextureImporterType.Default;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.mipmapEnabled = true;
            importer.sRGBTexture = true;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.npotScale = TextureImporterNPOTScale.None;
        }

        /// <summary>A texture repeated along a trail (skid marks): repeats along u, clamps across.</summary>
        public static void TrailImport(TextureImporter importer)
        {
            SoftImport(importer);
            importer.wrapModeU = TextureWrapMode.Repeat;
            importer.wrapModeV = TextureWrapMode.Clamp;
        }

        /// <summary>
        /// 0 below <paramref name="edge0"/>, 1 above <paramref name="edge1"/>, eased between (shader smoothstep; Unity's
        /// Mathf.SmoothStep interpolates between its first two arguments instead).
        /// </summary>
        public static float Smooth(float edge0, float edge1, float x)
        {
            float t = Mathf.Clamp01((x - edge0) / (edge1 - edge0));
            return t * t * (3f - 2f * t);
        }

        /// <summary>Pixels to a texture, rows from the bottom.</summary>
        public static Texture2D ToTexture(int width, int height, Func<float, float, Color> pixel)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var pixels = new Color[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    pixels[y * width + x] = pixel((x + 0.5f) / width, (y + 0.5f) / height);
                }
            }
            texture.SetPixels(pixels);
            texture.Apply(false, false);
            return texture;
        }
    }
}
