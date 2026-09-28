using System;
using System.Collections.Generic;
using System.IO;
using Gamebox.Editor;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using Object = UnityEngine.Object;

namespace Portfolio.EndlessRunner.EditorTools
{
    /// <summary>
    /// Generates the Endless Runner's art: textures, materials, meshes, sounds, fonts and the prefabs of the runner,
    /// the track pieces and the scenery of every world, once per theme (<see cref="RunnerThemes"/>). Everything is
    /// written to fixed paths, so running it again updates the assets in place. What both themes share (the palette,
    /// the road tiles, the particle sprites, the sounds) is built once with the classic theme.
    /// </summary>
    internal static class RunnerArtBuilder
    {
        private const float WagonShort = 10f;
        private const float WagonLong = 16f;
        private const float RampWidth = 2.3f;
        private const string FontCharacters = " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~";

        private static Shader Lit => Shader.Find("Universal Render Pipeline/Lit");
        private static Shader ParticlesUnlit => Shader.Find("Universal Render Pipeline/Particles/Unlit");

        /// <summary>Generates every art asset of every theme.</summary>
        public static void BuildAll()
        {
            Progress("Textures", 0.05f);
            BuildSharedTextures();
            foreach (ThemeArt theme in RunnerThemes.All)
            {
                Progress($"{theme.DisplayName} textures", 0.1f);
                BuildTextures(theme);
                BuildIcons(theme);
                Progress($"{theme.DisplayName} materials", 0.25f);
                BuildMaterials(theme);
                BuildFonts(theme);
            }
            Progress("Sounds", 0.35f);
            BuildSounds();
            BuildTiles();
            foreach (ThemeArt theme in RunnerThemes.All)
            {
                Progress($"{theme.DisplayName} pieces", 0.5f);
                BuildPieces(theme);
                Progress($"{theme.DisplayName} scenery", 0.7f);
                BuildScenery(theme);
                Progress($"{theme.DisplayName} runner", 0.85f);
                BuildRunner(theme);
            }
            AssetDatabase.SaveAssets();
            EditorUtility.ClearProgressBar();
        }

        private static void Progress(string step, float value)
        {
            EditorUtility.DisplayProgressBar("Endless Runner", $"Building {step}...", value);
        }

        // ------------------------------------------------------------------ textures

        /// <summary>The palette, the ground and particle textures every theme draws with.</summary>
        private static void BuildSharedTextures()
        {
            RunnerAssets.SaveTexture(TextureFactory.Palette(false), "Art/Textures/Palette.png", RunnerAssets.PaletteTexture);
            RunnerAssets.SaveTexture(TextureFactory.Palette(true), "Art/Textures/PaletteEmission.png", RunnerAssets.PaletteTexture);
            RunnerAssets.SaveTexture(TextureFactory.Asphalt(), "Art/Textures/Asphalt.png", RunnerAssets.TileableTexture);
            RunnerAssets.SaveTexture(TextureFactory.Sand(), "Art/Textures/Sand.png", RunnerAssets.TileableTexture);
            RunnerAssets.SaveTexture(TextureFactory.Snow(), "Art/Textures/Snow.png", RunnerAssets.TileableTexture);
            RunnerAssets.SaveTexture(TextureFactory.Ash(false), "Art/Textures/Ash.png", RunnerAssets.TileableTexture);
            RunnerAssets.SaveTexture(TextureFactory.Ash(true), "Art/Textures/AshEmission.png", RunnerAssets.TileableTexture);
            RunnerAssets.SaveTexture(TextureFactory.Water(), "Art/Textures/Water.png", RunnerAssets.TileableTexture);
            RunnerAssets.SaveTexture(TextureFactory.Lava(), "Art/Textures/Lava.png", RunnerAssets.TileableTexture);
            RunnerAssets.SaveTexture(TextureFactory.SoftDot(), "Art/Textures/SoftDot.png", RunnerAssets.ParticleTexture);
            RunnerAssets.SaveTexture(TextureFactory.Sparkle(), "Art/Textures/Sparkle.png", RunnerAssets.ParticleTexture);
            RunnerAssets.SaveTexture(TextureFactory.Smoke(), "Art/Textures/Smoke.png", RunnerAssets.ParticleTexture);
            RunnerAssets.SaveTexture(TextureFactory.Ring(), "Art/Textures/Ring.png", RunnerAssets.ParticleTexture);
            RunnerAssets.SaveTexture(TextureFactory.Square(), "Art/Textures/Square.png", RunnerAssets.ParticleTexture);
            RunnerAssets.SaveTexture(TextureFactory.RainStreak(), "Art/Textures/RainStreak.png", RunnerAssets.ParticleTexture);
            RunnerAssets.SaveTexture(TextureFactory.Fade(), "Art/Icons/Fade.png", RunnerAssets.SpriteTexture(Vector4.zero));
            RunnerAssets.SaveTexture(TextureFactory.Vignette(), "Art/Icons/Vignette.png", RunnerAssets.SpriteTexture(Vector4.zero));
            RunnerAssets.SaveTexture(TextureFactory.SoftDot(), "Art/Icons/Glow.png", RunnerAssets.SpriteTexture(Vector4.zero));
            RunnerAssets.SaveTexture(TextureFactory.Ring(), "Art/Icons/Ring.png", RunnerAssets.SpriteTexture(Vector4.zero));
            RunnerAssets.SaveTexture(TextureFactory.Grain(), "Art/Icons/Grain.png", importer =>
            {
                RunnerAssets.SpriteTexture(Vector4.zero)(importer);
                importer.wrapMode = TextureWrapMode.Repeat;
                importer.filterMode = FilterMode.Point;
            });
        }

        /// <summary>The curb stripes of the theme's worlds, and the surfaces only its worlds use.</summary>
        private static void BuildTextures(ThemeArt theme)
        {
            if (theme.IsClassic)
            {
                SaveStripes(theme, "Meadow", new Color(0.9f, 0.22f, 0.2f), new Color(0.96f, 0.96f, 0.94f));
                SaveStripes(theme, "Desert", new Color(0.8f, 0.4f, 0.24f), new Color(0.98f, 0.9f, 0.72f));
                SaveStripes(theme, "Snow", new Color(0.25f, 0.5f, 0.9f), new Color(0.96f, 0.98f, 1f));
                SaveStripes(theme, "Night", new Color(0.15f, 0.9f, 1f), new Color(0.12f, 0.14f, 0.3f));
                SaveStripes(theme, "Volcano", new Color(1f, 0.45f, 0.1f), new Color(0.12f, 0.1f, 0.1f));
                return;
            }
            RunnerAssets.SaveTexture(TextureFactory.WetAsphalt(), $"{theme.Textures}/WetAsphalt.png", RunnerAssets.TileableTexture);
            RunnerAssets.SaveTexture(TextureFactory.Concrete(), $"{theme.Textures}/Concrete.png", RunnerAssets.TileableTexture);
            RunnerAssets.SaveTexture(TextureFactory.Gravel(), $"{theme.Textures}/Gravel.png", RunnerAssets.TileableTexture);
            RunnerAssets.SaveTexture(TextureFactory.Tiles(), $"{theme.Textures}/Tiles.png", RunnerAssets.TileableTexture);
            RunnerAssets.SaveTexture(TextureFactory.CityLights(), $"{theme.Textures}/CityLights.png", RunnerAssets.TileableTexture);
            SaveStripes(theme, "Downtown", new Color(0.55f, 0.55f, 0.52f), new Color(0.2f, 0.2f, 0.22f));
            SaveStripes(theme, "Subway", new Color(1f, 0.7f, 0.15f), new Color(0.15f, 0.15f, 0.15f));
            SaveStripes(theme, "Docks", new Color(0.95f, 0.9f, 0.8f), new Color(0.35f, 0.15f, 0.12f));
            SaveStripes(theme, "Highway", new Color(1f, 0.55f, 0.1f), new Color(0.9f, 0.9f, 0.88f));
            SaveStripes(theme, "Rooftops", new Color(0.4f, 0.22f, 0.18f), new Color(0.6f, 0.58f, 0.55f));
        }

        private static void SaveStripes(ThemeArt theme, string biome, Color first, Color second)
        {
            RunnerAssets.SaveTexture(TextureFactory.Stripes(first, second), $"{theme.Textures}/Curb{biome}.png", importer =>
            {
                RunnerAssets.TileableTexture(importer);
                importer.filterMode = FilterMode.Bilinear;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.mipmapEnabled = false;
            });
        }

        private static void BuildIcons(ThemeArt theme)
        {
            foreach (string name in TextureFactory.IconNames)
            {
                RunnerAssets.SaveTexture(TextureFactory.Icon(name, theme.Icons), $"{theme.IconFolder}/{name}.png", RunnerAssets.SpriteTexture(Vector4.zero));
            }
            var border = new Vector4(24f, 24f, 24f, 24f);
            RunnerAssets.SaveTexture(TextureFactory.RoundedPanel(false, theme.Panels), $"{theme.IconFolder}/Panel.png", RunnerAssets.SpriteTexture(border));
            RunnerAssets.SaveTexture(TextureFactory.RoundedPanel(true, theme.Panels), $"{theme.IconFolder}/Button.png", RunnerAssets.SpriteTexture(border));
            RunnerAssets.SaveTexture(TextureFactory.RoundedPanel(true, theme.Panels, 1.15f), $"{theme.IconFolder}/ButtonHover.png", RunnerAssets.SpriteTexture(border));
            RunnerAssets.SaveTexture(TextureFactory.RoundedPanel(true, theme.Panels, 0.8f), $"{theme.IconFolder}/ButtonPressed.png", RunnerAssets.SpriteTexture(border));
            RunnerAssets.SaveTexture(TextureFactory.RoundedPanel(true, theme.Panels, 0.55f), $"{theme.IconFolder}/ButtonDisabled.png", RunnerAssets.SpriteTexture(border));
            RunnerAssets.SaveTexture(TextureFactory.Backdrop(), $"{theme.IconFolder}/Backdrop.png", RunnerAssets.SpriteTexture(Vector4.zero));
        }

        /// <summary>An icon of the classic theme.</summary>
        public static Sprite Icon(string name)
        {
            return Icon(RunnerThemes.Classic, name);
        }

        /// <summary>An icon (or shape) of a theme; the shapes every theme shares (Fade, Vignette, Glow, Ring, Grain) live with the classic one.</summary>
        public static Sprite Icon(ThemeArt theme, string name)
        {
            Sprite sprite = RunnerAssets.Load<Sprite>($"{theme.IconFolder}/{name}.png");
            return sprite != null ? sprite : RunnerAssets.Load<Sprite>($"Art/Icons/{name}.png");
        }

        // ------------------------------------------------------------------ materials

        private static void BuildMaterials(ThemeArt theme)
        {
            var palette = RunnerAssets.Load<Texture2D>("Art/Textures/Palette.png");
            var paletteGlow = RunnerAssets.Load<Texture2D>("Art/Textures/PaletteEmission.png");
            RunnerAssets.SaveMaterial($"{theme.Materials}/Palette.mat", Lit, m => SetupLit(m, Color.white, palette, theme.PaletteSmoothness, 0f, paletteGlow, Color.white * 1.8f));
            if (theme.CoinColor.HasValue)
            {
                RunnerAssets.SaveMaterial($"{theme.Materials}/Coin.mat", Lit, m => SetupLit(m, theme.CoinColor.Value, null, 0.78f, 0.35f, null, new Color(0.55f, 0.34f, 0.02f)));
            }

            var asphalt = RunnerAssets.Load<Texture2D>("Art/Textures/Asphalt.png");
            var water = RunnerAssets.Load<Texture2D>("Art/Textures/Water.png");
            if (theme.IsClassic)
            {
                var grass = AssetDatabase.LoadAssetAtPath<Texture2D>(RunnerAssets.Path("Stylized Grass Texture/Textures/Vol_42_1_Base_Color.png"));
                var sand = RunnerAssets.Load<Texture2D>("Art/Textures/Sand.png");
                var snow = RunnerAssets.Load<Texture2D>("Art/Textures/Snow.png");
                var ash = RunnerAssets.Load<Texture2D>("Art/Textures/Ash.png");
                var ashGlow = RunnerAssets.Load<Texture2D>("Art/Textures/AshEmission.png");
                var lava = RunnerAssets.Load<Texture2D>("Art/Textures/Lava.png");

                Road(theme, "Meadow", asphalt, new Color(0.44f, 0.45f, 0.5f), 0.12f);
                Road(theme, "Desert", asphalt, new Color(0.82f, 0.64f, 0.48f), 0.08f);
                Road(theme, "Snow", asphalt, new Color(0.62f, 0.7f, 0.8f), 0.35f);
                Road(theme, "Night", asphalt, new Color(0.25f, 0.25f, 0.36f), 0.3f);
                Road(theme, "Volcano", asphalt, new Color(0.24f, 0.21f, 0.23f), 0.12f);
                Stripes(theme, "Meadow", new Color(0.97f, 0.97f, 0.95f), false);
                Stripes(theme, "Desert", new Color(1f, 0.95f, 0.85f), false);
                Stripes(theme, "Snow", new Color(1f, 0.82f, 0.25f), false);
                Stripes(theme, "Night", new Color(0.3f, 0.95f, 1f), true);
                Stripes(theme, "Volcano", new Color(1f, 0.55f, 0.15f), true);
                Curb(theme, "Meadow", false);
                Curb(theme, "Desert", false);
                Curb(theme, "Snow", false);
                Curb(theme, "Night", true);
                Curb(theme, "Volcano", true);
                Ground(theme, "Meadow", grass, new Color(0.92f, 1f, 0.82f), 0.05f, null, null, 20f);
                Ground(theme, "Desert", sand, new Color(1f, 0.86f, 0.64f), 0.05f, null, null, 2f);
                Ground(theme, "Snow", snow, new Color(0.96f, 0.98f, 1f), 0.35f, null, null, 2f);
                Ground(theme, "Night", grass, new Color(0.3f, 0.45f, 0.5f), 0.05f, null, null, 20f);
                Ground(theme, "Volcano", ash, new Color(0.5f, 0.45f, 0.45f), 0.1f, ashGlow, new Color(1.6f, 1.6f, 1.6f), 1.5f);
                RunnerAssets.SaveMaterial($"{theme.Materials}/Cliff.mat", Lit, m => SetupLit(m, new Color(0.3f, 0.24f, 0.21f), asphalt, 0.05f, 0f, null, null));
                RunnerAssets.SaveMaterial($"{theme.Materials}/Water.mat", Lit, m => SetupLit(m, new Color(0.35f, 0.62f, 0.95f), water, 0.9f, 0f, null, new Color(0.02f, 0.08f, 0.14f)));
                RunnerAssets.SaveMaterial($"{theme.Materials}/IceWater.mat", Lit, m => SetupLit(m, new Color(0.62f, 0.85f, 1f), water, 0.95f, 0f, null, new Color(0.05f, 0.12f, 0.18f)));
                RunnerAssets.SaveMaterial($"{theme.Materials}/GlowWater.mat", Lit, m => SetupLit(m, new Color(0.2f, 0.55f, 0.75f), water, 0.9f, 0f, water, new Color(0.15f, 0.6f, 0.8f)));
                RunnerAssets.SaveMaterial($"{theme.Materials}/Lava.mat", Lit, m => SetupLit(m, new Color(1f, 0.5f, 0.2f), lava, 0.3f, 0f, lava, new Color(2.2f, 1.2f, 0.6f)));
            }
            else
            {
                var wet = RunnerAssets.Load<Texture2D>($"{theme.Textures}/WetAsphalt.png");
                var concrete = RunnerAssets.Load<Texture2D>($"{theme.Textures}/Concrete.png");
                var gravel = RunnerAssets.Load<Texture2D>($"{theme.Textures}/Gravel.png");
                var tiles = RunnerAssets.Load<Texture2D>($"{theme.Textures}/Tiles.png");
                var lights = RunnerAssets.Load<Texture2D>($"{theme.Textures}/CityLights.png");

                Road(theme, "Downtown", wet, new Color(0.42f, 0.44f, 0.5f), 0.75f);
                Road(theme, "Subway", asphalt, new Color(0.3f, 0.29f, 0.3f), 0.2f);
                Road(theme, "Docks", concrete, new Color(0.5f, 0.52f, 0.55f), 0.45f);
                Road(theme, "Highway", wet, new Color(0.38f, 0.39f, 0.43f), 0.7f);
                Road(theme, "Rooftops", concrete, new Color(0.55f, 0.52f, 0.5f), 0.25f);
                Stripes(theme, "Downtown", new Color(0.85f, 0.85f, 0.8f), false);
                Stripes(theme, "Subway", new Color(1f, 0.75f, 0.2f), true);
                Stripes(theme, "Docks", new Color(0.95f, 0.9f, 0.7f), false);
                Stripes(theme, "Highway", new Color(1f, 0.95f, 0.8f), false);
                Stripes(theme, "Rooftops", new Color(0.8f, 0.78f, 0.72f), false);
                Curb(theme, "Downtown", false);
                Curb(theme, "Subway", false);
                Curb(theme, "Docks", false);
                Curb(theme, "Highway", false);
                Curb(theme, "Rooftops", false);
                Ground(theme, "Downtown", wet, new Color(0.3f, 0.32f, 0.36f), 0.7f, null, null, 3f);
                Ground(theme, "Subway", gravel, new Color(0.3f, 0.29f, 0.28f), 0.05f, null, null, 3f);
                Ground(theme, "Docks", concrete, new Color(0.45f, 0.47f, 0.5f), 0.4f, null, null, 3f);
                Ground(theme, "Highway", gravel, new Color(0.38f, 0.37f, 0.36f), 0.1f, null, null, 3f);
                Ground(theme, "Rooftops", gravel, new Color(0.5f, 0.47f, 0.45f), 0.08f, null, null, 3f);
                RunnerAssets.SaveMaterial($"{theme.Materials}/Cliff.mat", Lit, m => SetupLit(m, new Color(0.28f, 0.28f, 0.3f), concrete, 0.1f, 0f, null, null, 0.5f));
                RunnerAssets.SaveMaterial($"{theme.Materials}/Tiles.mat", Lit, m => SetupLit(m, new Color(0.8f, 0.8f, 0.76f), tiles, 0.5f, 0f, null, null, 0.5f));
                RunnerAssets.SaveMaterial($"{theme.Materials}/HarbourWater.mat", Lit, m => SetupLit(m, new Color(0.12f, 0.18f, 0.24f), water, 0.95f, 0f, water, new Color(0.03f, 0.08f, 0.12f)));
                RunnerAssets.SaveMaterial($"{theme.Materials}/Canal.mat", Lit, m => SetupLit(m, new Color(0.08f, 0.1f, 0.14f), water, 0.95f, 0f, water, new Color(0.1f, 0.14f, 0.2f)));
                RunnerAssets.SaveMaterial($"{theme.Materials}/StreetBelow.mat", Lit, m => SetupLit(m, new Color(0.05f, 0.05f, 0.06f), lights, 0.2f, 0f, lights, new Color(2.5f, 2.2f, 1.8f), 0.35f));
                RunnerAssets.SaveMaterial($"{theme.Materials}/TrackBed.mat", Lit, m => SetupLit(m, new Color(0.12f, 0.11f, 0.11f), gravel, 0.1f, 0f, null, null, 2f));
                RunnerAssets.SaveMaterial($"{theme.Materials}/Traffic.mat", Lit, m => SetupLit(m, new Color(0.06f, 0.06f, 0.07f), lights, 0.3f, 0f, lights, new Color(2.8f, 2f, 1.2f), 0.5f));
            }

            var softDot = RunnerAssets.Load<Texture2D>("Art/Textures/SoftDot.png");
            var sparkle = RunnerAssets.Load<Texture2D>("Art/Textures/Sparkle.png");
            var smoke = RunnerAssets.Load<Texture2D>("Art/Textures/Smoke.png");
            var ring = RunnerAssets.Load<Texture2D>("Art/Textures/Ring.png");
            var square = RunnerAssets.Load<Texture2D>("Art/Textures/Square.png");
            Particle(theme, "ParticleSparkle", sparkle, true, theme.SparkleTint);
            Particle(theme, "ParticleGlow", softDot, true, theme.SparkleTint);
            Particle(theme, "ParticleSoft", softDot, false, Color.white);
            Particle(theme, "ParticleSmoke", smoke, false, Color.white);
            Particle(theme, "ParticleConfetti", square, false, Color.white, true);
            Particle(theme, "ParticleDebris", square, false, Color.white, true, true);
            Particle(theme, "ShieldRing", ring, true, theme.ShieldRing);
            Particle(theme, "HaloGold", softDot, true, theme.HaloGold);
            Particle(theme, "HaloMagnet", softDot, true, theme.HaloMagnet);
            Particle(theme, "HaloShield", softDot, true, theme.HaloShield);
            Particle(theme, "HaloMultiplier", softDot, true, theme.HaloMultiplier);
            Particle(theme, "HaloSuperJump", softDot, true, theme.HaloSuperJump);
            Particle(theme, "HaloGem", softDot, true, theme.HaloGem);
            if (!theme.IsClassic)
            {
                var streak = RunnerAssets.Load<Texture2D>("Art/Textures/RainStreak.png");
                Particle(theme, "ParticleRain", streak, false, new Color(0.75f, 0.85f, 1f, 0.5f));
            }

            var skyShader = RunnerAssets.Load<Shader>("Art/Shaders/RunnerSky.shader");
            RunnerAssets.SaveMaterial($"{theme.Materials}/Sky.mat", skyShader, m => m.SetFloat("_Skyline", theme.IsClassic ? 0f : 1f));
        }

        private static void Road(ThemeArt theme, string biome, Texture2D texture, Color color, float smoothness)
        {
            RunnerAssets.SaveMaterial($"{theme.Materials}/Road{biome}.mat", Lit, m => SetupLit(m, color, texture, smoothness, 0f, null, null));
        }

        private static void Stripes(ThemeArt theme, string biome, Color color, bool glow)
        {
            RunnerAssets.SaveMaterial($"{theme.Materials}/Stripes{biome}.mat", Lit, m => SetupLit(m, color, null, 0.3f, 0f, null, glow ? color * 1.6f : (Color?)null));
        }

        private static void Curb(ThemeArt theme, string biome, bool glow)
        {
            var texture = RunnerAssets.Load<Texture2D>($"{theme.Textures}/Curb{biome}.png");
            RunnerAssets.SaveMaterial($"{theme.Materials}/Curb{biome}.mat", Lit, m => SetupLit(m, Color.white, texture, 0.25f, 0f, glow ? texture : null, glow ? new Color(1.3f, 1.3f, 1.3f) : (Color?)null));
        }

        private static void Ground(ThemeArt theme, string biome, Texture2D texture, Color color, float smoothness, Texture emissionMap, Color? emission, float tiling)
        {
            RunnerAssets.SaveMaterial($"{theme.Materials}/Ground{biome}.mat", Lit, m => SetupLit(m, color, texture, smoothness, 0f, emissionMap, emission, tiling));
        }

        /// <summary>URP Lit, opaque, no environment reflections (the scene has no reflection probe).</summary>
        private static void SetupLit(Material material, Color color, Texture texture, float smoothness, float metallic,
            Texture emissionMap, Color? emission, float tiling = 1f)
        {
            material.SetFloat("_WorkflowMode", 1f);
            material.SetFloat("_Surface", 0f);
            material.SetFloat("_Blend", 0f);
            material.SetFloat("_AlphaClip", 0f);
            material.SetColor("_BaseColor", color);
            material.SetTexture("_BaseMap", texture);
            material.SetTextureScale("_BaseMap", Vector2.one * tiling);
            material.SetFloat("_Smoothness", smoothness);
            material.SetFloat("_Metallic", metallic);
            material.SetFloat("_EnvironmentReflections", 0f);
            material.SetFloat("_SpecularHighlights", 1f);
            material.SetFloat("_ReceiveShadows", 1f);
            material.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF");
            material.DisableKeyword("_SPECULARHIGHLIGHTS_OFF");
            material.DisableKeyword("_RECEIVE_SHADOWS_OFF");
            if (emission.HasValue)
            {
                material.SetColor("_EmissionColor", emission.Value);
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
            BaseShaderGUI.SetupMaterialBlendMode(material);
        }

        private static void Particle(ThemeArt theme, string name, Texture texture, bool additive, Color color, bool unlitColor = false, bool opaque = false)
        {
            RunnerAssets.SaveMaterial($"{theme.Materials}/{name}.mat", ParticlesUnlit, m =>
            {
                m.SetTexture("_BaseMap", texture);
                m.SetColor("_BaseColor", color);
                m.SetFloat("_Surface", opaque ? 0f : 1f);
                m.SetFloat("_Blend", additive ? 2f : 0f);
                m.SetFloat("_ColorMode", 0f);
                m.SetFloat("_Cull", opaque ? 2f : 0f);
                BaseShaderGUI.SetupMaterialBlendMode(m);
            });
        }

        /// <summary>A material of the classic theme.</summary>
        public static Material Material(string name)
        {
            return Material(RunnerThemes.Classic, name);
        }

        public static Material Material(ThemeArt theme, string name)
        {
            return RunnerAssets.Load<Material>($"{theme.Materials}/{name}.mat");
        }

        // ------------------------------------------------------------------ fonts

        /// <summary>
        /// The theme's fonts: the project's default font for the classic theme, or the theme's own font file baked into
        /// a static TextMesh Pro asset (once; rebuilding keeps its GUID), each with an outlined title material and a
        /// lighter HUD material.
        /// </summary>
        private static void BuildFonts(ThemeArt theme)
        {
            TMP_FontAsset font = FontAsset(theme);
            if (font == null)
            {
                return;
            }
            RunnerAssets.SaveMaterial($"{theme.Materials}/FontTitle.mat", font.material.shader, m =>
            {
                m.CopyPropertiesFromMaterial(font.material);
                m.EnableKeyword("OUTLINE_ON");
                m.EnableKeyword("UNDERLAY_ON");
                m.SetFloat("_FaceDilate", theme.IsClassic ? 0.15f : 0.05f);
                m.SetFloat("_OutlineWidth", theme.IsClassic ? 0.26f : 0.16f);
                m.SetColor("_OutlineColor", theme.IsClassic ? new Color(0.1f, 0.07f, 0.2f, 1f) : new Color(0.03f, 0.03f, 0.04f, 1f));
                m.SetColor("_UnderlayColor", theme.IsClassic ? new Color(0f, 0f, 0.05f, 0.65f) : new Color(0f, 0f, 0f, 0.8f));
                m.SetFloat("_UnderlayOffsetX", 0.5f);
                m.SetFloat("_UnderlayOffsetY", -0.7f);
                m.SetFloat("_UnderlayDilate", 0.3f);
                m.SetFloat("_UnderlaySoftness", theme.IsClassic ? 0.25f : 0.4f);
            });
            RunnerAssets.SaveMaterial($"{theme.Materials}/FontHud.mat", font.material.shader, m =>
            {
                m.CopyPropertiesFromMaterial(font.material);
                m.EnableKeyword("OUTLINE_ON");
                m.DisableKeyword("UNDERLAY_ON");
                m.SetFloat("_FaceDilate", theme.IsClassic ? 0.08f : 0.12f);
                m.SetFloat("_OutlineWidth", theme.IsClassic ? 0.18f : 0.08f);
                m.SetColor("_OutlineColor", theme.IsClassic ? new Color(0.05f, 0.05f, 0.12f, 1f) : new Color(0.02f, 0.02f, 0.03f, 1f));
            });
        }

        /// <summary>The font of a theme: the default one, or the theme's own baked from its font file.</summary>
        public static TMP_FontAsset FontAsset(ThemeArt theme)
        {
            if (string.IsNullOrEmpty(theme.FontFile))
            {
                return TMP_Settings.defaultFontAsset;
            }
            string path = $"{theme.Fonts}/{theme.FontName} SDF.asset";
            var fontAsset = RunnerAssets.Load<TMP_FontAsset>(path);
            if (fontAsset != null)
            {
                return fontAsset;
            }
            string sourcePath = RunnerAssets.Path($"{theme.Fonts}/{theme.FontFile}");
            if (!File.Exists(RunnerAssets.FullPath(sourcePath)))
            {
                Debug.LogWarning($"Endless Runner: the font file {sourcePath} is missing; {theme.DisplayName} uses the default font.");
                return TMP_Settings.defaultFontAsset;
            }
            AssetDatabase.ImportAsset(sourcePath, ImportAssetOptions.ForceSynchronousImport);
            var source = AssetDatabase.LoadAssetAtPath<Font>(sourcePath);
            fontAsset = TMP_FontAsset.CreateFontAsset(source, 72, 9, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, false);
            fontAsset.name = $"{theme.FontName} SDF";
            fontAsset.TryAddCharacters(FontCharacters, out string missing);
            if (!string.IsNullOrEmpty(missing))
            {
                Debug.Log($"Endless Runner: {theme.FontName} has no glyphs for \"{missing}\".");
            }
            fontAsset.atlasPopulationMode = AtlasPopulationMode.Static;
            TMP_FontAsset fallback = TMP_Settings.defaultFontAsset;
            if (fallback != null)
            {
                fontAsset.fallbackFontAssetTable = new List<TMP_FontAsset> { fallback };
            }
            RunnerAssets.EnsureFolder(theme.Fonts);
            AssetDatabase.CreateAsset(fontAsset, RunnerAssets.Path(path));
            Texture2D atlas = fontAsset.atlasTexture;
            atlas.name = $"{theme.FontName} SDF Atlas";
            AssetDatabase.AddObjectToAsset(atlas, fontAsset);
            Material material = fontAsset.material;
            material.name = $"{theme.FontName} SDF Material";
            AssetDatabase.AddObjectToAsset(material, fontAsset);
            EditorUtility.SetDirty(fontAsset);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(RunnerAssets.Path(path));
            return RunnerAssets.Load<TMP_FontAsset>(path);
        }

        // ------------------------------------------------------------------ sounds

        private static void BuildSounds()
        {
            foreach (string name in SoundFactory.EffectNames)
            {
                RunnerAssets.SaveAudio(SoundFactory.Effect(name), $"Audio/{name}.wav", false);
            }
            RunnerAssets.SaveAudio(SoundFactory.RunMusic(), "Audio/RunMusic.wav", true);
            RunnerAssets.SaveAudio(SoundFactory.MenuMusic(), "Audio/MenuMusic.wav", true);
        }

        public static AudioClip Sound(string name)
        {
            return RunnerAssets.Load<AudioClip>($"Audio/{name}.wav");
        }

        // ------------------------------------------------------------------ tiles

        /// <summary>The road tiles every theme shares; their materials come from the world at run time.</summary>
        private static void BuildTiles()
        {
            RoadTileBuilder(false);
            RoadTileBuilder(true);
        }

        private static void RoadTileBuilder(bool chasm)
        {
            string name = chasm ? "ChasmTile" : "RoadTile";
            Mesh surface = RunnerAssets.SaveMesh(RunnerModels.RoadTile(chasm, LayoutBuilder.GapStart, LayoutBuilder.GapEnd), $"Art/Models/{name}.asset");
            var root = new GameObject(name);
            var tile = root.AddComponent<TerrainBehaviour>();
            tile.length = RunnerModels.TileLength;
            GameObject visual = Visual(root.transform, "Surface", surface, null);
            var renderer = visual.GetComponent<MeshRenderer>();
            var materials = new List<Material> { Material("RoadMeadow"), Material("StripesMeadow"), Material("CurbMeadow"), Material("GroundMeadow") };
            if (chasm)
            {
                materials.Add(Material("Cliff"));
            }
            renderer.sharedMaterials = materials.ToArray();
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            tile.surface = renderer;
            float width = (RunnerModels.RoadHalfWidth + RunnerModels.CurbWidth) * 2f;
            if (chasm)
            {
                AddBox(root, new Vector3(0f, -0.3f, LayoutBuilder.GapStart * 0.5f), new Vector3(width, 0.6f, LayoutBuilder.GapStart));
                float farLength = RunnerModels.TileLength - LayoutBuilder.GapEnd;
                AddBox(root, new Vector3(0f, -0.3f, LayoutBuilder.GapEnd + farLength * 0.5f), new Vector3(width, 0.6f, farLength));
                Mesh fillMesh = RunnerAssets.SaveMesh(RunnerModels.ChasmFill(LayoutBuilder.GapStart, LayoutBuilder.GapEnd), "Art/Models/ChasmFill.asset");
                GameObject fill = Visual(root.transform, "Fill", fillMesh, Material("Water"));
                fill.AddComponent<TextureScroller>().speed = new Vector2(0.06f, 0.015f);
                fill.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                tile.chasmFill = fill.GetComponent<MeshRenderer>();
                tile.gap = new Vector2(LayoutBuilder.GapStart, LayoutBuilder.GapEnd);
            }
            else
            {
                AddBox(root, new Vector3(0f, -0.3f, RunnerModels.TileLength * 0.5f), new Vector3(width, 0.6f, RunnerModels.TileLength));
            }
            RunnerAssets.SavePrefab(root, $"Prefabs/Tiles/{name}.prefab");
        }

        public static TerrainBehaviour Tile(bool chasm)
        {
            return RunnerAssets.Load<GameObject>($"Prefabs/Tiles/{(chasm ? "ChasmTile" : "RoadTile")}.prefab").GetComponent<TerrainBehaviour>();
        }

        // ------------------------------------------------------------------ track pieces

        /// <summary>
        /// The pieces of a theme. Every piece has the name, the length, the kind and the colliders of its classic
        /// counterpart: only the models and materials differ, so a race lays out the same track on every theme.
        /// </summary>
        private static void BuildPieces(ThemeArt theme)
        {
            ModelSet models = theme.Models;
            Material palette = Material(theme, "Palette");

            SaveObstacle(theme, "Hurdle", models.Hurdle(), ObstacleKind.Hurdle, 0.4f, root =>
                AddBox(root, new Vector3(0f, 0.5f, 0f), new Vector3(2.2f, 1f, 0.25f)));
            SaveObstacle(theme, "Barrier", models.Barrier(), ObstacleKind.Barrier, 0.4f, root =>
            {
                AddBox(root, new Vector3(0f, 1.65f, 0f), new Vector3(2.46f, 0.9f, 0.16f));
                AddBox(root, new Vector3(-1.18f, 1.1f, 0f), new Vector3(0.16f, 2.2f, 0.16f));
                AddBox(root, new Vector3(1.18f, 1.1f, 0f), new Vector3(0.16f, 2.2f, 0.16f));
            });
            SaveObstacle(theme, "CrateStack", models.CrateStack(), ObstacleKind.Block, 1f, root =>
                AddBox(root, new Vector3(0f, 1.1f, 0f), new Vector3(1.95f, 2.2f, 1.7f)));
            SaveObstacle(theme, "BarrelStack", models.BarrelStack(), ObstacleKind.Block, 0.6f, root =>
                AddBox(root, new Vector3(0f, 1.05f, 0f), new Vector3(1.85f, 2.1f, 0.9f)));
            for (int livery = 0; livery < WagonLiveries.Length; livery++)
            {
                foreach (float length in new[] { WagonShort, WagonLong })
                {
                    string size = length > WagonShort ? "Long" : "Short";
                    float wagonLength = length;
                    SaveObstacle(theme, $"Wagon{size}{WagonLiveries[livery]}", models.Wagon(length, livery), ObstacleKind.Platform, length, root =>
                        AddBox(root, new Vector3(0f, 1.2f, wagonLength * 0.5f), new Vector3(2.3f, 2.4f, wagonLength)));
                }
            }
            Mesh rampCollider = RunnerAssets.SaveMesh(RunnerModels.RampCollider(RampWidth, LayoutBuilder.WagonHeight, LayoutBuilder.RampLength, 0.3f), "Art/Models/RampCollider.asset");
            SaveObstacle(theme, "Ramp", models.Ramp(RampWidth, LayoutBuilder.WagonHeight, LayoutBuilder.RampLength), ObstacleKind.Ramp, LayoutBuilder.RampLength, root =>
            {
                var collider = root.AddComponent<MeshCollider>();
                collider.sharedMesh = rampCollider;
                collider.convex = true;
            });
            SaveObstacle(theme, "Bridge", models.Bridge(4.5f), ObstacleKind.Bridge, 4.5f, root =>
                AddBox(root, new Vector3(0f, -0.15f, 2.25f), new Vector3(2.3f, 0.3f, 4.5f)));

            Mesh wheel = RunnerAssets.SaveMesh(models.Wheel(), $"{theme.ModelFolder}/CartWheel.asset");
            SaveObstacle(theme, "Cart", models.CartBody(), ObstacleKind.Cart, 1.1f, root =>
            {
                AddBox(root, new Vector3(0f, 0.78f, 0f), new Vector3(1.7f, 1.45f, 2.1f));
                var body = root.AddComponent<Rigidbody>();
                body.isKinematic = true;
                body.useGravity = false;
                var obstacle = root.GetComponent<Obstacle>();
                obstacle.moveSpeed = 6f;
                obstacle.wakeDistance = 55f;
                obstacle.wheelRadius = 0.3f;
                var wheels = new List<Transform>();
                foreach (Vector3 position in new[] { new Vector3(-0.72f, 0.3f, -0.65f), new Vector3(0.72f, 0.3f, -0.65f), new Vector3(-0.72f, 0.3f, 0.65f), new Vector3(0.72f, 0.3f, 0.65f) })
                {
                    GameObject child = Visual(root.transform, "Wheel", wheel, palette);
                    child.transform.localPosition = position;
                    wheels.Add(child.transform);
                }
                obstacle.wheels = wheels.ToArray();
            });

            // Pickups
            Mesh coinMesh = RunnerAssets.SaveMesh(models.Coin(), $"{theme.ModelFolder}/Coin.asset");
            Material coinMaterial = theme.CoinColor.HasValue ? Material(theme, "Coin") : palette;
            SavePiece<Coin>(theme, "Coin", 0.5f, root =>
            {
                var coin = root.GetComponent<Coin>();
                coin.value = 1;
                coin.radius = 0.55f;
                coin.magnetic = true;
                GameObject visual = Visual(root.transform, "Visual", coinMesh, coinMaterial);
                Spin(visual, 170f, 0.08f, 3f);
            });
            Mesh gemMesh = RunnerAssets.SaveMesh(models.Gem(), $"{theme.ModelFolder}/Gem.asset");
            SavePiece<Coin>(theme, "Gem", 0.5f, root =>
            {
                var coin = root.GetComponent<Coin>();
                coin.value = 5;
                coin.gem = true;
                coin.radius = 0.6f;
                coin.magnetic = true;
                GameObject visual = Visual(root.transform, "Visual", gemMesh, palette);
                Spin(visual, 120f, 0.12f, 2.5f);
                Halo(theme, root.transform, "HaloGem", 1.6f);
            });
            PowerUp(theme, "Magnet", models.Magnet(), PowerUpType.Magnet, "HaloMagnet");
            PowerUp(theme, "Shield", models.Shield(), PowerUpType.Shield, "HaloShield");
            PowerUp(theme, "Multiplier", models.Multiplier(), PowerUpType.Multiplier, "HaloMultiplier");
            PowerUp(theme, "SuperJump", models.SuperJump(), PowerUpType.SuperJump, "HaloSuperJump");

            Mesh padBase = RunnerAssets.SaveMesh(models.JumpPadBase(), $"{theme.ModelFolder}/JumpPadBase.asset");
            Mesh padTop = RunnerAssets.SaveMesh(models.JumpPadMembrane(), $"{theme.ModelFolder}/JumpPadMembrane.asset");
            SavePiece<JumpPad>(theme, "JumpPad", 1f, root =>
            {
                var pad = root.GetComponent<JumpPad>();
                pad.radius = 0.95f;
                pad.launchHeight = 5.5f;
                Visual(root.transform, "Base", padBase, palette);
                GameObject membrane = Visual(root.transform, "Membrane", padTop, palette);
                membrane.transform.localPosition = new Vector3(0f, 0.2f, 0f);
                pad.membrane = membrane.transform;
                Halo(theme, root.transform, "HaloSuperJump", 2.4f).transform.localPosition = new Vector3(0f, 0.4f, 0f);
            });

            Mesh finish = RunnerAssets.SaveMesh(models.FinishArch(), $"{theme.ModelFolder}/FinishArch.asset");
            SavePiece<TrackPiece>(theme, "FinishLine", 1f, root => Visual(root.transform, "Visual", finish, palette));
        }

        private static readonly string[] WagonLiveries = { "Red", "Blue", "Green", "Yellow" };

        private static void SaveObstacle(ThemeArt theme, string name, MeshBuilder model, ObstacleKind kind, float length, Action<GameObject> configure)
        {
            Mesh mesh = RunnerAssets.SaveMesh(model, $"{theme.ModelFolder}/{name}.asset");
            SavePiece<Obstacle>(theme, name, length, root =>
            {
                root.GetComponent<Obstacle>().kind = kind;
                Visual(root.transform, "Visual", mesh, Material(theme, "Palette"));
                configure(root);
            });
        }

        private static void PowerUp(ThemeArt theme, string name, MeshBuilder model, PowerUpType type, string halo)
        {
            Mesh mesh = RunnerAssets.SaveMesh(model, $"{theme.ModelFolder}/{name}.asset");
            SavePiece<PowerUpPickup>(theme, name, 0.5f, root =>
            {
                var pickup = root.GetComponent<PowerUpPickup>();
                pickup.type = type;
                pickup.radius = 0.85f;
                GameObject visual = Visual(root.transform, "Visual", mesh, Material(theme, "Palette"));
                visual.transform.localScale = Vector3.one * 1.3f;
                Spin(visual, 110f, 0.15f, 2.5f);
                Halo(theme, root.transform, halo, 2.2f);
            });
        }

        private static T SavePiece<T>(ThemeArt theme, string name, float length, Action<GameObject> configure) where T : TrackPiece
        {
            var root = new GameObject(name);
            var piece = root.AddComponent<T>();
            piece.length = length;
            configure(root);
            return RunnerAssets.SavePrefab(root, $"{theme.Pieces}/{name}.prefab").GetComponent<T>();
        }

        private static GameObject Visual(Transform parent, string name, Mesh mesh, Material material)
        {
            var visual = new GameObject(name);
            visual.transform.SetParent(parent, false);
            visual.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = visual.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            return visual;
        }

        private static void AddBox(GameObject root, Vector3 center, Vector3 size)
        {
            var box = root.AddComponent<BoxCollider>();
            box.center = center;
            box.size = size;
        }

        private static void Spin(GameObject visual, float degrees, float bob, float bobSpeed)
        {
            var spinner = visual.AddComponent<Spinner>();
            spinner.degreesPerSecond = degrees;
            spinner.bobHeight = bob;
            spinner.bobSpeed = bobSpeed;
        }

        private static GameObject Halo(ThemeArt theme, Transform parent, string material, float size)
        {
            var halo = new GameObject("Halo");
            halo.transform.SetParent(parent, false);
            halo.transform.localScale = Vector3.one * size;
            halo.AddComponent<MeshFilter>().sharedMesh = Resources.GetBuiltinResource<Mesh>("Quad.fbx");
            var renderer = halo.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = Material(theme, material);
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            halo.AddComponent<Billboard>().pulse = 0.12f;
            return halo;
        }

        /// <summary>A piece of the classic theme.</summary>
        public static T Piece<T>(string name) where T : Object
        {
            return Piece<T>(RunnerThemes.Classic, name);
        }

        public static T Piece<T>(ThemeArt theme, string name) where T : Object
        {
            var prefab = RunnerAssets.Load<GameObject>($"{theme.Pieces}/{name}.prefab");
            if (prefab == null)
            {
                throw new InvalidOperationException($"Missing piece prefab {name} of {theme.DisplayName}; build the art first.");
            }
            return typeof(T) == typeof(GameObject) ? prefab as T : prefab.GetComponent(typeof(T)) as T;
        }

        public static IEnumerable<string> WagonNames(bool longWagons)
        {
            foreach (string livery in WagonLiveries)
            {
                yield return $"Wagon{(longWagons ? "Long" : "Short")}{livery}";
            }
        }

        // ------------------------------------------------------------------ scenery

        private static void BuildScenery(ThemeArt theme)
        {
            Material palette = Material(theme, "Palette");
            foreach (SceneryRecipe recipe in theme.Scenery)
            {
                Mesh mesh = RunnerAssets.SaveMesh(recipe.Model(), $"{theme.ModelFolder}/Scenery/{recipe.Name}.asset");
                var root = new GameObject(recipe.Name);
                var piece = root.AddComponent<TrackPiece>();
                piece.length = recipe.Length;
                GameObject visual = Visual(root.transform, "Visual", mesh, palette);
                if (recipe.NoShadow)
                {
                    visual.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                }
                RunnerAssets.SavePrefab(root, $"{theme.SceneryFolder}/{recipe.Biome}/{recipe.Name}.prefab");
            }
        }

        /// <summary>A piece of scenery of the classic theme.</summary>
        public static TrackPiece SceneryPiece(string biome, string name)
        {
            return SceneryPiece(RunnerThemes.Classic, biome, name);
        }

        public static TrackPiece SceneryPiece(ThemeArt theme, string biome, string name)
        {
            var prefab = RunnerAssets.Load<GameObject>($"{theme.SceneryFolder}/{biome}/{name}.prefab");
            return prefab != null ? prefab.GetComponent<TrackPiece>() : null;
        }

        /// <summary>Every piece of scenery of every theme, for the pools of the scene.</summary>
        public static IEnumerable<TrackPiece> AllScenery()
        {
            foreach (ThemeArt theme in RunnerThemes.All)
            {
                foreach (SceneryRecipe recipe in theme.Scenery)
                {
                    TrackPiece piece = SceneryPiece(theme, recipe.Biome, recipe.Name);
                    if (piece != null)
                    {
                        yield return piece;
                    }
                }
            }
        }

        // ------------------------------------------------------------------ runner

        /// <summary>
        /// The runner's body parts of a theme. The classic theme also writes the player prefab (the skeleton every
        /// theme's parts hang on, with the auras and the dust); the other themes only bring their meshes, which
        /// <see cref="RunnerSkin"/> puts on the skeleton at run time.
        /// </summary>
        private static void BuildRunner(ThemeArt theme)
        {
            ModelSet models = theme.Models;
            Material palette = Material(theme, "Palette");
            string folder = $"{theme.ModelFolder}/Runner";
            Mesh torso = RunnerAssets.SaveMesh(models.Torso(), $"{folder}/Torso.asset");
            Mesh head = RunnerAssets.SaveMesh(models.Head(), $"{folder}/Head.asset");
            Mesh upperArm = RunnerAssets.SaveMesh(models.UpperArm(), $"{folder}/UpperArm.asset");
            Mesh forearm = RunnerAssets.SaveMesh(models.Forearm(), $"{folder}/Forearm.asset");
            Mesh thigh = RunnerAssets.SaveMesh(models.Thigh(), $"{folder}/Thigh.asset");
            Mesh shin = RunnerAssets.SaveMesh(models.Shin(), $"{folder}/Shin.asset");
            if (!theme.IsClassic)
            {
                return;
            }
            Mesh magnetRing = RunnerAssets.SaveMesh(RunnerModels.Ring(0.62f, 0.05f, Swatch.GlowRed), $"{folder}/MagnetRing.asset");
            Mesh springRing = RunnerAssets.SaveMesh(RunnerModels.Ring(0.34f, 0.045f, Swatch.GlowGreen), $"{folder}/SpringRing.asset");

            var root = new GameObject("Player");
            var controller = root.AddComponent<CharacterController>();
            controller.height = 1.7f;
            controller.radius = 0.32f;
            controller.center = new Vector3(0f, 0.85f, 0f);
            controller.slopeLimit = 50f;
            controller.stepOffset = 0.3f;
            controller.skinWidth = 0.04f;
            controller.minMoveDistance = 0f;
            var player = root.AddComponent<RunnerPlayer>();

            var body = new GameObject("Body");
            body.transform.SetParent(root.transform, false);
            var animator = body.AddComponent<RunnerAnimator>();

            Transform hips = Joint(body.transform, "Hips", new Vector3(0f, theme.HipsHeight, 0f), null, null);
            Transform spine = Joint(hips, "Spine", Vector3.zero, torso, palette);
            Transform headJoint = Joint(spine, "Head", theme.Neck, head, palette);
            Transform armLeft = Joint(spine, "ArmLeft", new Vector3(-theme.Shoulder.x, theme.Shoulder.y, theme.Shoulder.z), upperArm, palette);
            Transform forearmLeft = Joint(armLeft, "ForearmLeft", new Vector3(0f, theme.Elbow, 0f), forearm, palette);
            Transform armRight = Joint(spine, "ArmRight", theme.Shoulder, upperArm, palette);
            Transform forearmRight = Joint(armRight, "ForearmRight", new Vector3(0f, theme.Elbow, 0f), forearm, palette);
            Transform legLeft = Joint(hips, "LegLeft", new Vector3(-theme.Hip, 0f, 0f), thigh, palette);
            Transform shinLeft = Joint(legLeft, "ShinLeft", new Vector3(0f, theme.Knee, 0f), shin, palette);
            Transform legRight = Joint(hips, "LegRight", new Vector3(theme.Hip, 0f, 0f), thigh, palette);
            Transform shinRight = Joint(legRight, "ShinRight", new Vector3(0f, theme.Knee, 0f), shin, palette);

            animator.player = player;
            animator.body = body.transform;
            animator.hips = hips;
            animator.spine = spine;
            animator.head = headJoint;
            animator.armLeft = armLeft;
            animator.armRight = armRight;
            animator.forearmLeft = forearmLeft;
            animator.forearmRight = forearmRight;
            animator.legLeft = legLeft;
            animator.legRight = legRight;
            animator.shinLeft = shinLeft;
            animator.shinRight = shinRight;

            var shield = new GameObject("ShieldBubble");
            shield.transform.SetParent(root.transform, false);
            shield.transform.localPosition = new Vector3(0f, 1f, 0f);
            GameObject shieldRing = Halo(theme, shield.transform, "ShieldRing", 2.1f);
            shieldRing.GetComponent<Billboard>().pulse = 0.04f;
            Halo(theme, shield.transform, "HaloShield", 2.3f).GetComponent<Billboard>().pulse = 0.06f;
            shield.SetActive(false);

            GameObject magnet = Visual(root.transform, "MagnetAura", magnetRing, palette);
            magnet.transform.localPosition = new Vector3(0f, 0.9f, 0f);
            magnet.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
            var magnetSpinner = magnet.AddComponent<Spinner>();
            magnetSpinner.degreesPerSecond = 220f;
            magnetSpinner.bobHeight = 0.25f;
            magnetSpinner.bobSpeed = 4f;
            magnet.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            magnet.SetActive(false);

            var boots = new GameObject("SuperJumpAura");
            boots.transform.SetParent(root.transform, false);
            GameObject bootRing = Visual(boots.transform, "Ring", springRing, palette);
            bootRing.transform.localPosition = new Vector3(0f, 0.12f, 0f);
            var bootSpinner = bootRing.AddComponent<Spinner>();
            bootSpinner.degreesPerSecond = -260f;
            bootSpinner.bobHeight = 0.05f;
            bootSpinner.bobSpeed = 8f;
            bootRing.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            Halo(theme, boots.transform, "HaloSuperJump", 1.4f).transform.localPosition = new Vector3(0f, 0.2f, 0f);
            boots.SetActive(false);

            ParticleSystem dust = ParticleFactory.Create("RunDust", root.transform, Material(theme, "ParticleSmoke"));
            dust.transform.localPosition = new Vector3(0f, 0.05f, -0.15f);
            ParticleFactory.Lifetime(dust, 0.35f, 0.6f);
            ParticleFactory.Speed(dust, 0.4f, 1.2f);
            ParticleFactory.Size(dust, 0.25f, 0.5f);
            ParticleFactory.Colors(dust, new Color(0.85f, 0.8f, 0.7f, 0.55f), new Color(0.95f, 0.92f, 0.85f, 0.4f));
            ParticleFactory.Circle(dust, 0.2f, false);
            ParticleFactory.SizeOverLife(dust, 0.6f, 1.6f);
            ParticleFactory.FadeOut(dust);
            ParticleFactory.Gravity(dust, -0.05f);
            ParticleFactory.MaxParticles(dust, 120);

            player.animator = animator;
            player.shieldBubble = shield;
            player.magnetAura = magnet;
            player.superJumpAura = boots;
            player.runDust = dust;

            // The skin follows the active theme: other themes' parts, joints and aura materials go on this skeleton.
            var skin = root.AddComponent<RunnerSkin>();
            skin.animator = animator;
            skin.shieldBubble = shield;
            skin.magnetAura = magnet;
            skin.superJumpAura = boots;
            skin.runDust = dust;

            SetLayer(root, RunnerPlayer.Layer);
            RunnerAssets.SavePrefab(root, "Prefabs/Player.prefab");
        }

        /// <summary>The runner's parts of a theme, as the theme asset lists them.</summary>
        public static Mesh RunnerPart(ThemeArt theme, string part)
        {
            return RunnerAssets.Load<Mesh>($"{theme.ModelFolder}/Runner/{part}.asset");
        }

        private static Transform Joint(Transform parent, string name, Vector3 position, Mesh mesh, Material material)
        {
            var joint = new GameObject(name);
            joint.transform.SetParent(parent, false);
            joint.transform.localPosition = position;
            if (mesh != null)
            {
                joint.AddComponent<MeshFilter>().sharedMesh = mesh;
                joint.AddComponent<MeshRenderer>().sharedMaterial = material;
            }
            return joint.transform;
        }

        private static void SetLayer(GameObject root, int layer)
        {
            root.layer = layer;
            foreach (Transform child in root.transform)
            {
                SetLayer(child.gameObject, layer);
            }
        }
    }
}
