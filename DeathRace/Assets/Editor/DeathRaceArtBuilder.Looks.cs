using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Portfolio.DeathRace.EditorTools
{
    /// <summary>
    /// What each look puts over the whole picture: the sky that shiny things reflect (an equirectangular picture imported
    /// as a convolved cube map; the scene has no sky box, so without it paint, chrome and wet roads reflect black), the
    /// post processing profile (Nightfall's bloom is the strong one, so neon, lamps and lights glow) and the material of
    /// the title font (an outline and a shadow by day, a neon glow at night).
    /// </summary>
    internal static partial class DeathRaceArtBuilder
    {
        public static Cubemap Sky(DeathRaceThemeSpec spec)
        {
            return DeathRaceAssets.Require<Cubemap>(SkyPath(spec));
        }

        public static VolumeProfile Volume(DeathRaceThemeSpec spec)
        {
            return DeathRaceAssets.Require<VolumeProfile>(spec.VolumePath);
        }

        public static Material TitleMaterial(DeathRaceThemeSpec spec)
        {
            return DeathRaceAssets.Require<Material>($"{spec.InterfaceFolder}/TitleFont.mat");
        }

        private static string SkyPath(DeathRaceThemeSpec spec)
        {
            return $"Art/Sky/{spec.AssetName}Sky.png";
        }

        private static void BuildLooks()
        {
            foreach (DeathRaceThemeSpec spec in DeathRaceThemeSpec.All)
            {
                DeathRaceAssets.SaveTexture(SkyPicture(spec, 512, 256), SkyPath(spec), SkyImport);
                BuildVolume(spec);
                BuildTitleMaterial(spec);
            }
        }

        private static void SkyImport(TextureImporter importer)
        {
            importer.textureType = TextureImporterType.Default;
            importer.textureShape = TextureImporterShape.TextureCube;
            importer.sRGBTexture = true;
            importer.mipmapEnabled = true;
            importer.filterMode = FilterMode.Trilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.maxTextureSize = 512;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.textureShape = TextureImporterShape.TextureCube;
            settings.generateCubemap = TextureImporterGenerateCubemap.Cylindrical;
            settings.cubemapConvolution = TextureImporterCubemapConvolution.Specular;
            settings.seamlessCubemap = true;
            importer.SetTextureSettings(settings);
        }

        /// <summary>
        /// The sky of a look as a latitude-longitude picture: the ground below the horizon, the horizon's haze into the
        /// zenith above, the sun's glare by day; at night a band of city glow in neon colours along the horizon and the moon.
        /// </summary>
        private static Texture2D SkyPicture(DeathRaceThemeSpec spec, int width, int height)
        {
            var pixels = new Color[width * height];
            Vector3 sun = -(Quaternion.Euler(spec.SunEuler) * Vector3.forward);
            Color ground = spec.Night ? new Color(0.02f, 0.02f, 0.035f) : new Color(0.36f, 0.28f, 0.2f);
            Color[] neon = { DeathRaceThemeSpec.Hex(0xFF3FC8), DeathRaceThemeSpec.Hex(0x2FE4FF), DeathRaceThemeSpec.Hex(0xFF8A2A), DeathRaceThemeSpec.Hex(0x9B5CFF) };
            for (int y = 0; y < height; y++)
            {
                float latitude = ((y + 0.5f) / height - 0.5f) * Mathf.PI;
                for (int x = 0; x < width; x++)
                {
                    float longitude = (x + 0.5f) / width * Mathf.PI * 2f;
                    var direction = new Vector3(Mathf.Cos(latitude) * Mathf.Sin(longitude), Mathf.Sin(latitude), Mathf.Cos(latitude) * Mathf.Cos(longitude));
                    float up = direction.y;
                    Color c;
                    if (up < 0f)
                    {
                        c = Color.Lerp(spec.SkyHorizon * 0.6f, ground, Mathf.Clamp01(-up * 4f));
                    }
                    else
                    {
                        c = Color.Lerp(spec.SkyHorizon, spec.SkyZenith, Mathf.Pow(up, 0.55f));
                    }
                    float toSun = Mathf.Max(0f, Vector3.Dot(direction, sun));
                    if (spec.Night)
                    {
                        // City glow: a band just over the horizon whose colour drifts round the compass.
                        // A tight band at the horizon and a broad haze above it, which the wet roads seen from above reflect.
                        float band = Mathf.Exp(-Mathf.Abs(up - 0.03f) * 18f) + Mathf.Exp(-Mathf.Abs(up - 0.35f) * 4f) * 0.45f;
                        float t = Mathf.Repeat(longitude / (Mathf.PI * 2f) * neon.Length + DeathRaceArtNoise.Fbm(longitude / (Mathf.PI * 2f), 0.5f, 6, 1, 3, 951) * 1.5f, neon.Length);
                        int index = Mathf.FloorToInt(t);
                        Color glow = Color.Lerp(neon[index % neon.Length], neon[(index + 1) % neon.Length], t - index);
                        float flicker = 0.55f + 0.45f * DeathRaceArtNoise.Fbm(longitude / (Mathf.PI * 2f), 0.5f, 24, 1, 2, 953);
                        c += glow * band * flicker * 0.5f;
                        c += new Color(0.75f, 0.8f, 1f) * Mathf.Pow(toSun, 900f) * 1.2f + new Color(0.2f, 0.25f, 0.45f) * Mathf.Pow(toSun, 24f) * 0.3f;
                    }
                    else
                    {
                        c += spec.SkyGlow * (Mathf.Pow(toSun, 600f) * 1.5f + Mathf.Pow(toSun, 12f) * 0.35f);
                    }
                    c.a = 1f;
                    pixels[y * width + x] = new Color(Mathf.Clamp01(c.r), Mathf.Clamp01(c.g), Mathf.Clamp01(c.b), 1f);
                }
            }
            return DeathRaceArtNoise.ToTexture(pixels, width, height);
        }

        private static void BuildVolume(DeathRaceThemeSpec spec)
        {
            string path = DeathRaceAssets.Path(spec.VolumePath);
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                DeathRaceAssets.EnsureFolder("Config/Themes");
                AssetDatabase.CreateAsset(profile, path);
            }
            DeathRaceAssets.ApplyIfChanged(profile, () =>
            {
                var bloom = Component<Bloom>(profile);
                bloom.threshold.Override(spec.BloomThreshold);
                bloom.intensity.Override(spec.BloomIntensity);
                bloom.scatter.Override(spec.BloomScatter);
                bloom.tint.Override(spec.BloomTint);
                bloom.highQualityFiltering.Override(true);
                var tone = Component<Tonemapping>(profile);
                tone.mode.Override(spec.Tonemapping);
                var colors = Component<ColorAdjustments>(profile);
                colors.postExposure.Override(spec.Exposure);
                colors.contrast.Override(spec.Contrast);
                colors.saturation.Override(spec.Saturation);
                colors.colorFilter.Override(spec.ColorFilter);
                var balance = Component<WhiteBalance>(profile);
                balance.temperature.Override(spec.Temperature);
                balance.tint.Override(spec.Tint);
                var vignette = Component<Vignette>(profile);
                vignette.intensity.Override(spec.Vignette);
                vignette.smoothness.Override(0.42f);
                vignette.color.Override(spec.VignetteColor);
                var grain = Component<FilmGrain>(profile);
                grain.type.Override(FilmGrainLookup.Medium1);
                grain.intensity.Override(spec.Grain);
                grain.response.Override(0.8f);
                var aberration = Component<ChromaticAberration>(profile);
                aberration.intensity.Override(spec.Aberration);
                aberration.active = spec.Aberration > 0f;
                var tones = Component<ShadowsMidtonesHighlights>(profile);
                tones.shadows.Override(spec.Shadows);
                tones.highlights.Override(spec.Highlights);
            });
            EditorUtility.SetDirty(profile);
        }

        private static T Component<T>(VolumeProfile profile) where T : VolumeComponent
        {
            if (!profile.TryGet(out T component))
            {
                component = profile.Add<T>(true);
                component.name = typeof(T).Name;
                AssetDatabase.AddObjectToAsset(component, profile);
            }
            return component;
        }

        /// <summary>The title font of a look: Scorched writes with a dark outline and a hard shadow, Nightfall glows.</summary>
        private static void BuildTitleMaterial(DeathRaceThemeSpec spec)
        {
            TMP_FontAsset font = TitleFont;
            Material source = font.material;
            DeathRaceAssets.SaveMaterial($"{spec.InterfaceFolder}/TitleFont.mat", source.shader, m =>
            {
                m.SetTexture("_MainTex", source.GetTexture("_MainTex"));
                m.SetFloat("_TextureWidth", source.GetFloat("_TextureWidth"));
                m.SetFloat("_TextureHeight", source.GetFloat("_TextureHeight"));
                m.SetFloat("_GradientScale", source.GetFloat("_GradientScale"));
                m.SetFloat("_WeightNormal", source.GetFloat("_WeightNormal"));
                m.SetFloat("_WeightBold", source.GetFloat("_WeightBold"));
                m.SetFloat("_FaceDilate", 0.05f);
                m.SetColor("_FaceColor", Color.white);
                m.EnableKeyword("UNDERLAY_ON");
                if (spec.Night)
                {
                    m.SetColor("_OutlineColor", new Color(spec.Accent.r * 0.35f, spec.Accent.g * 0.35f, spec.Accent.b * 0.35f, 1f));
                    m.SetFloat("_OutlineWidth", 0.08f);
                    m.EnableKeyword("GLOW_ON");
                    m.SetColor("_GlowColor", new Color(spec.Accent.r, spec.Accent.g, spec.Accent.b, 0.75f));
                    m.SetFloat("_GlowOffset", 0f);
                    m.SetFloat("_GlowInner", 0.05f);
                    m.SetFloat("_GlowOuter", 0.45f);
                    m.SetFloat("_GlowPower", 0.6f);
                    m.SetColor("_UnderlayColor", new Color(spec.Second.r, spec.Second.g, spec.Second.b, 0.45f));
                    m.SetFloat("_UnderlayOffsetX", 0.6f);
                    m.SetFloat("_UnderlayOffsetY", -0.6f);
                    m.SetFloat("_UnderlayDilate", 0.1f);
                    m.SetFloat("_UnderlaySoftness", 0.35f);
                }
                else
                {
                    m.SetColor("_OutlineColor", new Color(0.12f, 0.05f, 0.02f, 1f));
                    m.SetFloat("_OutlineWidth", 0.14f);
                    m.DisableKeyword("GLOW_ON");
                    m.SetColor("_UnderlayColor", new Color(0f, 0f, 0f, 0.7f));
                    m.SetFloat("_UnderlayOffsetX", 0.7f);
                    m.SetFloat("_UnderlayOffsetY", -0.8f);
                    m.SetFloat("_UnderlayDilate", 0.2f);
                    m.SetFloat("_UnderlaySoftness", 0.1f);
                }
                m.EnableKeyword("OUTLINE_ON");
            });
        }
    }
}
