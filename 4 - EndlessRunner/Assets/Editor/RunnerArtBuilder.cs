using System;
using System.Collections.Generic;
using Gamebox.Editor;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Portfolio.EndlessRunner.EditorTools
{
    /// <summary>
    /// Generates the Endless Runner's art: textures, materials, meshes, sounds and the prefabs of the runner, the
    /// track pieces and the scenery of all five worlds. Everything is written to fixed paths, so running it again
    /// updates the assets in place.
    /// </summary>
    internal static class RunnerArtBuilder
    {
        public static readonly string[] Themes = { "Meadow", "Desert", "Snow", "Night", "Volcano" };

        private const float WagonShort = 10f;
        private const float WagonLong = 16f;
        private const float RampWidth = 2.3f;

        private static Shader Lit => Shader.Find("Universal Render Pipeline/Lit");
        private static Shader ParticlesUnlit => Shader.Find("Universal Render Pipeline/Particles/Unlit");

        private sealed class SceneryRecipe
        {
            public string Theme;
            public string Name;
            public Func<MeshBuilder> Model;
            public float Length = 1f;
        }

        private static readonly SceneryRecipe[] Scenery =
        {
            new SceneryRecipe { Theme = "Meadow", Name = "TreeRound", Model = () => SceneryModels.RoundTree(11, Swatch.Leaf, Swatch.LeafDark, Swatch.Bark) },
            new SceneryRecipe { Theme = "Meadow", Name = "AppleTree", Model = () => SceneryModels.RoundTree(23, Swatch.LeafLight, Swatch.Leaf, Swatch.Bark, Swatch.FlowerRed, true) },
            new SceneryRecipe { Theme = "Meadow", Name = "Poplar", Model = () => SceneryModels.Poplar(31) },
            new SceneryRecipe { Theme = "Meadow", Name = "Pine", Model = () => SceneryModels.Pine(41, Swatch.Pine, Swatch.PineDark, Swatch.Bark, false) },
            new SceneryRecipe { Theme = "Meadow", Name = "Bush", Model = () => SceneryModels.Bush(51, Swatch.Leaf, Swatch.LeafDark, Swatch.FlowerRed, true) },
            new SceneryRecipe { Theme = "Meadow", Name = "Flowers", Model = () => SceneryModels.Flowers(61) },
            new SceneryRecipe { Theme = "Meadow", Name = "Rock", Model = () => SceneryModels.Rock(71, Swatch.Gray, Swatch.DarkGray) },
            new SceneryRecipe { Theme = "Meadow", Name = "Mushroom", Model = () => SceneryModels.Mushroom(81, Swatch.MushroomRed, Swatch.White, Swatch.OffWhite) },
            new SceneryRecipe { Theme = "Meadow", Name = "Cottage", Model = SceneryModels.Cottage, Length = 2f },
            new SceneryRecipe { Theme = "Meadow", Name = "Fence", Model = () => SceneryModels.Fence(RunnerModels.TileLength), Length = RunnerModels.TileLength },
            new SceneryRecipe { Theme = "Desert", Name = "Saguaro", Model = () => SceneryModels.Saguaro(101) },
            new SceneryRecipe { Theme = "Desert", Name = "SaguaroTall", Model = () => SceneryModels.Saguaro(117) },
            new SceneryRecipe { Theme = "Desert", Name = "BarrelCactus", Model = () => SceneryModels.BarrelCactus(3) },
            new SceneryRecipe { Theme = "Desert", Name = "Mesa", Model = () => SceneryModels.Mesa(131), Length = 8f },
            new SceneryRecipe { Theme = "Desert", Name = "DesertRock", Model = () => SceneryModels.DesertRock(141) },
            new SceneryRecipe { Theme = "Desert", Name = "PalmTree", Model = () => SceneryModels.PalmTree(151) },
            new SceneryRecipe { Theme = "Desert", Name = "DeadBush", Model = () => SceneryModels.DeadBush(161) },
            new SceneryRecipe { Theme = "Snow", Name = "SnowPine", Model = () => SceneryModels.Pine(201, Swatch.Pine, Swatch.PineDark, Swatch.Bark, true) },
            new SceneryRecipe { Theme = "Snow", Name = "SnowPineTall", Model = () => SceneryModels.Pine(211, Swatch.PineDark, Swatch.Pine, Swatch.Bark, true, 1.5f) },
            new SceneryRecipe { Theme = "Snow", Name = "Snowman", Model = SceneryModels.Snowman },
            new SceneryRecipe { Theme = "Snow", Name = "IceRock", Model = () => SceneryModels.Rock(221, Swatch.Ice, Swatch.IceDeep) },
            new SceneryRecipe { Theme = "Snow", Name = "IceCrystals", Model = () => SceneryModels.Crystals(231, Swatch.Ice, Swatch.GlowCyan) },
            new SceneryRecipe { Theme = "Snow", Name = "SnowBush", Model = () => SceneryModels.Bush(241, Swatch.Snow, Swatch.SnowShade) },
            new SceneryRecipe { Theme = "Snow", Name = "SnowPole", Model = SceneryModels.SnowPole },
            new SceneryRecipe { Theme = "Night", Name = "NightTree", Model = () => SceneryModels.RoundTree(301, Swatch.NightLeaf, Swatch.PineDark, Swatch.NightTrunk, Swatch.GlowYellow, true) },
            new SceneryRecipe { Theme = "Night", Name = "NightPine", Model = () => SceneryModels.Pine(311, Swatch.NightLeaf, Swatch.PineDark, Swatch.NightTrunk, false) },
            new SceneryRecipe { Theme = "Night", Name = "GlowMushrooms", Model = () => SceneryModels.GlowMushrooms(321) },
            new SceneryRecipe { Theme = "Night", Name = "NightCrystals", Model = () => SceneryModels.Crystals(331, Swatch.Violet, Swatch.GlowPurple, 1.3f) },
            new SceneryRecipe { Theme = "Night", Name = "NightRock", Model = () => SceneryModels.Rock(341, Swatch.NightRock, Swatch.Charcoal) },
            new SceneryRecipe { Theme = "Night", Name = "LampPost", Model = SceneryModels.LampPost },
            new SceneryRecipe { Theme = "Volcano", Name = "BasaltColumns", Model = () => SceneryModels.BasaltColumns(401) },
            new SceneryRecipe { Theme = "Volcano", Name = "DeadTree", Model = () => SceneryModels.DeadTree(411) },
            new SceneryRecipe { Theme = "Volcano", Name = "LavaRock", Model = () => SceneryModels.LavaRock(421) },
            new SceneryRecipe { Theme = "Volcano", Name = "AshRock", Model = () => SceneryModels.Rock(431, Swatch.Ash, Swatch.Basalt) },
            new SceneryRecipe { Theme = "Volcano", Name = "Volcano", Model = SceneryModels.Volcano, Length = 16f },
            new SceneryRecipe { Theme = "Volcano", Name = "Torch", Model = SceneryModels.Torch }
        };

        /// <summary>Generates every art asset.</summary>
        public static void BuildAll()
        {
            Progress("Textures", 0.05f);
            BuildTextures();
            Progress("Icons", 0.15f);
            BuildIcons();
            Progress("Materials", 0.25f);
            BuildMaterials();
            Progress("Sounds", 0.35f);
            BuildSounds();
            Progress("Models", 0.5f);
            BuildPieces();
            Progress("Scenery", 0.7f);
            BuildScenery();
            Progress("Runner", 0.85f);
            BuildPlayer();
            AssetDatabase.SaveAssets();
            EditorUtility.ClearProgressBar();
        }

        private static void Progress(string step, float value)
        {
            EditorUtility.DisplayProgressBar("Endless Runner", $"Building {step}...", value);
        }

        // ------------------------------------------------------------------ textures

        private static void BuildTextures()
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
            SaveStripes("Meadow", new Color(0.9f, 0.22f, 0.2f), new Color(0.96f, 0.96f, 0.94f));
            SaveStripes("Desert", new Color(0.8f, 0.4f, 0.24f), new Color(0.98f, 0.9f, 0.72f));
            SaveStripes("Snow", new Color(0.25f, 0.5f, 0.9f), new Color(0.96f, 0.98f, 1f));
            SaveStripes("Night", new Color(0.15f, 0.9f, 1f), new Color(0.12f, 0.14f, 0.3f));
            SaveStripes("Volcano", new Color(1f, 0.45f, 0.1f), new Color(0.12f, 0.1f, 0.1f));
            RunnerAssets.SaveTexture(TextureFactory.SoftDot(), "Art/Textures/SoftDot.png", RunnerAssets.ParticleTexture);
            RunnerAssets.SaveTexture(TextureFactory.Sparkle(), "Art/Textures/Sparkle.png", RunnerAssets.ParticleTexture);
            RunnerAssets.SaveTexture(TextureFactory.Smoke(), "Art/Textures/Smoke.png", RunnerAssets.ParticleTexture);
            RunnerAssets.SaveTexture(TextureFactory.Ring(), "Art/Textures/Ring.png", RunnerAssets.ParticleTexture);
            RunnerAssets.SaveTexture(TextureFactory.Square(), "Art/Textures/Square.png", RunnerAssets.ParticleTexture);
        }

        private static void SaveStripes(string theme, Color first, Color second)
        {
            RunnerAssets.SaveTexture(TextureFactory.Stripes(first, second), $"Art/Textures/Curb{theme}.png", importer =>
            {
                RunnerAssets.TileableTexture(importer);
                importer.filterMode = FilterMode.Bilinear;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.mipmapEnabled = false;
            });
        }

        private static void BuildIcons()
        {
            foreach (string name in TextureFactory.IconNames)
            {
                RunnerAssets.SaveTexture(TextureFactory.Icon(name), $"Art/Icons/{name}.png", RunnerAssets.SpriteTexture(Vector4.zero));
            }
            RunnerAssets.SaveTexture(TextureFactory.RoundedPanel(false), "Art/Icons/Panel.png", RunnerAssets.SpriteTexture(new Vector4(24f, 24f, 24f, 24f)));
            RunnerAssets.SaveTexture(TextureFactory.RoundedPanel(true), "Art/Icons/Button.png", RunnerAssets.SpriteTexture(new Vector4(24f, 24f, 24f, 24f)));
            RunnerAssets.SaveTexture(TextureFactory.Fade(), "Art/Icons/Fade.png", RunnerAssets.SpriteTexture(Vector4.zero));
            RunnerAssets.SaveTexture(TextureFactory.Vignette(), "Art/Icons/Vignette.png", RunnerAssets.SpriteTexture(Vector4.zero));
            RunnerAssets.SaveTexture(TextureFactory.SoftDot(), "Art/Icons/Glow.png", RunnerAssets.SpriteTexture(Vector4.zero));
            RunnerAssets.SaveTexture(TextureFactory.Ring(), "Art/Icons/Ring.png", RunnerAssets.SpriteTexture(Vector4.zero));
        }

        public static Sprite Icon(string name)
        {
            return RunnerAssets.Load<Sprite>($"Art/Icons/{name}.png");
        }

        // ------------------------------------------------------------------ materials

        private static void BuildMaterials()
        {
            var palette = RunnerAssets.Load<Texture2D>("Art/Textures/Palette.png");
            var paletteGlow = RunnerAssets.Load<Texture2D>("Art/Textures/PaletteEmission.png");
            RunnerAssets.SaveMaterial("Art/Materials/Palette.mat", Lit, m => SetupLit(m, Color.white, palette, 0.18f, 0f, paletteGlow, Color.white * 1.8f));
            RunnerAssets.SaveMaterial("Art/Materials/Coin.mat", Lit, m => SetupLit(m, new Color(1f, 0.8f, 0.2f), null, 0.78f, 0.35f, null, new Color(0.55f, 0.34f, 0.02f)));

            var asphalt = RunnerAssets.Load<Texture2D>("Art/Textures/Asphalt.png");
            var grass = AssetDatabase.LoadAssetAtPath<Texture2D>(RunnerAssets.Path("Stylized Grass Texture/Textures/Vol_42_1_Base_Color.png"));
            var sand = RunnerAssets.Load<Texture2D>("Art/Textures/Sand.png");
            var snow = RunnerAssets.Load<Texture2D>("Art/Textures/Snow.png");
            var ash = RunnerAssets.Load<Texture2D>("Art/Textures/Ash.png");
            var ashGlow = RunnerAssets.Load<Texture2D>("Art/Textures/AshEmission.png");
            var water = RunnerAssets.Load<Texture2D>("Art/Textures/Water.png");
            var lava = RunnerAssets.Load<Texture2D>("Art/Textures/Lava.png");

            Road("Meadow", asphalt, new Color(0.44f, 0.45f, 0.5f), 0.12f);
            Road("Desert", asphalt, new Color(0.82f, 0.64f, 0.48f), 0.08f);
            Road("Snow", asphalt, new Color(0.62f, 0.7f, 0.8f), 0.35f);
            Road("Night", asphalt, new Color(0.25f, 0.25f, 0.36f), 0.3f);
            Road("Volcano", asphalt, new Color(0.24f, 0.21f, 0.23f), 0.12f);

            Stripes("Meadow", new Color(0.97f, 0.97f, 0.95f), false);
            Stripes("Desert", new Color(1f, 0.95f, 0.85f), false);
            Stripes("Snow", new Color(1f, 0.82f, 0.25f), false);
            Stripes("Night", new Color(0.3f, 0.95f, 1f), true);
            Stripes("Volcano", new Color(1f, 0.55f, 0.15f), true);

            Curb("Meadow", false);
            Curb("Desert", false);
            Curb("Snow", false);
            Curb("Night", true);
            Curb("Volcano", true);

            RunnerAssets.SaveMaterial("Art/Materials/GroundMeadow.mat", Lit, m => SetupLit(m, new Color(0.92f, 1f, 0.82f), grass, 0.05f, 0f, null, null, 20f));
            RunnerAssets.SaveMaterial("Art/Materials/GroundDesert.mat", Lit, m => SetupLit(m, new Color(1f, 0.86f, 0.64f), sand, 0.05f, 0f, null, null, 2f));
            RunnerAssets.SaveMaterial("Art/Materials/GroundSnow.mat", Lit, m => SetupLit(m, new Color(0.96f, 0.98f, 1f), snow, 0.35f, 0f, null, null, 2f));
            RunnerAssets.SaveMaterial("Art/Materials/GroundNight.mat", Lit, m => SetupLit(m, new Color(0.3f, 0.45f, 0.5f), grass, 0.05f, 0f, null, null, 20f));
            RunnerAssets.SaveMaterial("Art/Materials/GroundVolcano.mat", Lit, m => SetupLit(m, new Color(0.5f, 0.45f, 0.45f), ash, 0.1f, 0f, ashGlow, new Color(1.6f, 1.6f, 1.6f), 1.5f));

            RunnerAssets.SaveMaterial("Art/Materials/Cliff.mat", Lit, m => SetupLit(m, new Color(0.3f, 0.24f, 0.21f), asphalt, 0.05f, 0f, null, null));
            RunnerAssets.SaveMaterial("Art/Materials/Water.mat", Lit, m => SetupLit(m, new Color(0.35f, 0.62f, 0.95f), water, 0.9f, 0f, null, new Color(0.02f, 0.08f, 0.14f)));
            RunnerAssets.SaveMaterial("Art/Materials/IceWater.mat", Lit, m => SetupLit(m, new Color(0.62f, 0.85f, 1f), water, 0.95f, 0f, null, new Color(0.05f, 0.12f, 0.18f)));
            RunnerAssets.SaveMaterial("Art/Materials/GlowWater.mat", Lit, m => SetupLit(m, new Color(0.2f, 0.55f, 0.75f), water, 0.9f, 0f, water, new Color(0.15f, 0.6f, 0.8f)));
            RunnerAssets.SaveMaterial("Art/Materials/Lava.mat", Lit, m => SetupLit(m, new Color(1f, 0.5f, 0.2f), lava, 0.3f, 0f, lava, new Color(2.2f, 1.2f, 0.6f)));

            var softDot = RunnerAssets.Load<Texture2D>("Art/Textures/SoftDot.png");
            var sparkle = RunnerAssets.Load<Texture2D>("Art/Textures/Sparkle.png");
            var smoke = RunnerAssets.Load<Texture2D>("Art/Textures/Smoke.png");
            var ring = RunnerAssets.Load<Texture2D>("Art/Textures/Ring.png");
            var square = RunnerAssets.Load<Texture2D>("Art/Textures/Square.png");
            Particle("ParticleSparkle", sparkle, true, Color.white);
            Particle("ParticleGlow", softDot, true, Color.white);
            Particle("ParticleSoft", softDot, false, Color.white);
            Particle("ParticleSmoke", smoke, false, Color.white);
            Particle("ParticleConfetti", square, false, Color.white, true);
            Particle("ParticleDebris", square, false, Color.white, true, true);
            Particle("ShieldRing", ring, true, new Color(0.4f, 0.8f, 1f, 0.55f));
            Particle("HaloGold", softDot, true, new Color(1f, 0.8f, 0.3f, 0.6f));
            Particle("HaloMagnet", softDot, true, new Color(1f, 0.3f, 0.3f, 0.7f));
            Particle("HaloShield", softDot, true, new Color(0.3f, 0.7f, 1f, 0.35f));
            Particle("HaloMultiplier", softDot, true, new Color(0.75f, 0.4f, 1f, 0.7f));
            Particle("HaloSuperJump", softDot, true, new Color(0.35f, 1f, 0.45f, 0.7f));
            Particle("HaloGem", softDot, true, new Color(1f, 0.35f, 0.9f, 0.6f));

            var skyShader = RunnerAssets.Load<Shader>("Art/Shaders/RunnerSky.shader");
            RunnerAssets.SaveMaterial("Art/Materials/Sky.mat", skyShader, m => { });
        }

        private static void Road(string theme, Texture2D texture, Color color, float smoothness)
        {
            RunnerAssets.SaveMaterial($"Art/Materials/Road{theme}.mat", Lit, m => SetupLit(m, color, texture, smoothness, 0f, null, null));
        }

        private static void Stripes(string theme, Color color, bool glow)
        {
            RunnerAssets.SaveMaterial($"Art/Materials/Stripes{theme}.mat", Lit, m => SetupLit(m, color, null, 0.3f, 0f, null, glow ? color * 1.6f : (Color?)null));
        }

        private static void Curb(string theme, bool glow)
        {
            var texture = RunnerAssets.Load<Texture2D>($"Art/Textures/Curb{theme}.png");
            RunnerAssets.SaveMaterial($"Art/Materials/Curb{theme}.mat", Lit, m => SetupLit(m, Color.white, texture, 0.25f, 0f, glow ? texture : null, glow ? new Color(1.3f, 1.3f, 1.3f) : (Color?)null));
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

        private static void Particle(string name, Texture texture, bool additive, Color color, bool unlitColor = false, bool opaque = false)
        {
            RunnerAssets.SaveMaterial($"Art/Materials/{name}.mat", ParticlesUnlit, m =>
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

        public static Material Material(string name)
        {
            return RunnerAssets.Load<Material>($"Art/Materials/{name}.mat");
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

        // ------------------------------------------------------------------ track pieces

        private static void BuildPieces()
        {
            Material palette = Material("Palette");

            // Road tiles
            RoadTileBuilder(false);
            RoadTileBuilder(true);

            // Obstacles
            SaveObstacle("Hurdle", RunnerModels.Hurdle(), ObstacleKind.Hurdle, 0.4f, root =>
                AddBox(root, new Vector3(0f, 0.5f, 0f), new Vector3(2.2f, 1f, 0.25f)));
            SaveObstacle("Barrier", RunnerModels.Barrier(), ObstacleKind.Barrier, 0.4f, root =>
            {
                AddBox(root, new Vector3(0f, 1.65f, 0f), new Vector3(2.46f, 0.9f, 0.16f));
                AddBox(root, new Vector3(-1.18f, 1.1f, 0f), new Vector3(0.16f, 2.2f, 0.16f));
                AddBox(root, new Vector3(1.18f, 1.1f, 0f), new Vector3(0.16f, 2.2f, 0.16f));
            });
            SaveObstacle("CrateStack", RunnerModels.CrateStack(), ObstacleKind.Block, 1f, root =>
                AddBox(root, new Vector3(0f, 1.1f, 0f), new Vector3(1.95f, 2.2f, 1.7f)));
            SaveObstacle("BarrelStack", RunnerModels.BarrelStack(), ObstacleKind.Block, 0.6f, root =>
                AddBox(root, new Vector3(0f, 1.05f, 0f), new Vector3(1.85f, 2.1f, 0.9f)));
            foreach ((string name, Swatch body, Swatch rib) in new[]
            {
                ("Red", Swatch.WagonRed, Swatch.DarkRed), ("Blue", Swatch.WagonBlue, Swatch.Navy),
                ("Green", Swatch.WagonGreen, Swatch.DarkGreen), ("Yellow", Swatch.WagonYellow, Swatch.Orange)
            })
            {
                foreach (float length in new[] { WagonShort, WagonLong })
                {
                    string size = length > WagonShort ? "Long" : "Short";
                    SaveObstacle($"Wagon{size}{name}", RunnerModels.Wagon(length, body, rib), ObstacleKind.Platform, length, root =>
                        AddBox(root, new Vector3(0f, 1.2f, length * 0.5f), new Vector3(2.3f, 2.4f, length)));
                }
            }
            Mesh rampCollider = RunnerAssets.SaveMesh(RunnerModels.RampCollider(RampWidth, LayoutBuilder.WagonHeight, LayoutBuilder.RampLength, 0.3f), "Art/Models/RampCollider.asset");
            SaveObstacle("Ramp", RunnerModels.Ramp(RampWidth, LayoutBuilder.WagonHeight, LayoutBuilder.RampLength), ObstacleKind.Ramp, LayoutBuilder.RampLength, root =>
            {
                var collider = root.AddComponent<MeshCollider>();
                collider.sharedMesh = rampCollider;
                collider.convex = true;
            });
            SaveObstacle("Bridge", RunnerModels.Bridge(4.5f), ObstacleKind.Bridge, 4.5f, root =>
                AddBox(root, new Vector3(0f, -0.15f, 2.25f), new Vector3(2.3f, 0.3f, 4.5f)));

            Mesh wheel = RunnerAssets.SaveMesh(RunnerModels.Wheel(), "Art/Models/CartWheel.asset");
            SaveObstacle("Cart", RunnerModels.CartBody(), ObstacleKind.Cart, 1.1f, root =>
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
            Mesh coinMesh = RunnerAssets.SaveMesh(RunnerModels.Coin(), "Art/Models/Coin.asset");
            SavePiece<Coin>("Coin", 0.5f, root =>
            {
                var coin = root.GetComponent<Coin>();
                coin.value = 1;
                coin.radius = 0.55f;
                coin.magnetic = true;
                GameObject visual = Visual(root.transform, "Visual", coinMesh, Material("Coin"));
                Spin(visual, 170f, 0.08f, 3f);
            });
            Mesh gemMesh = RunnerAssets.SaveMesh(RunnerModels.Gem(), "Art/Models/Gem.asset");
            SavePiece<Coin>("Gem", 0.5f, root =>
            {
                var coin = root.GetComponent<Coin>();
                coin.value = 5;
                coin.gem = true;
                coin.radius = 0.6f;
                coin.magnetic = true;
                GameObject visual = Visual(root.transform, "Visual", gemMesh, palette);
                Spin(visual, 120f, 0.12f, 2.5f);
                Halo(root.transform, "HaloGem", 1.6f);
            });
            PowerUp("Magnet", RunnerModels.Magnet(), PowerUpType.Magnet, "HaloMagnet");
            PowerUp("Shield", RunnerModels.Shield(), PowerUpType.Shield, "HaloShield");
            PowerUp("Multiplier", RunnerModels.StarPower(), PowerUpType.Multiplier, "HaloMultiplier");
            PowerUp("SuperJump", RunnerModels.SpringShoe(), PowerUpType.SuperJump, "HaloSuperJump");

            Mesh padBase = RunnerAssets.SaveMesh(RunnerModels.JumpPadBase(), "Art/Models/JumpPadBase.asset");
            Mesh padTop = RunnerAssets.SaveMesh(RunnerModels.JumpPadMembrane(), "Art/Models/JumpPadMembrane.asset");
            SavePiece<JumpPad>("JumpPad", 1f, root =>
            {
                var pad = root.GetComponent<JumpPad>();
                pad.radius = 0.95f;
                pad.launchHeight = 5.5f;
                Visual(root.transform, "Base", padBase, palette);
                GameObject membrane = Visual(root.transform, "Membrane", padTop, palette);
                membrane.transform.localPosition = new Vector3(0f, 0.2f, 0f);
                pad.membrane = membrane.transform;
                Halo(root.transform, "HaloSuperJump", 2.4f).transform.localPosition = new Vector3(0f, 0.4f, 0f);
            });

            Mesh finish = RunnerAssets.SaveMesh(RunnerModels.FinishArch(), "Art/Models/FinishArch.asset");
            SavePiece<TrackPiece>("FinishLine", 1f, root => Visual(root.transform, "Visual", finish, palette));
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

        private static void SaveObstacle(string name, MeshBuilder model, ObstacleKind kind, float length, Action<GameObject> configure)
        {
            Mesh mesh = RunnerAssets.SaveMesh(model, $"Art/Models/{name}.asset");
            SavePiece<Obstacle>(name, length, root =>
            {
                root.GetComponent<Obstacle>().kind = kind;
                Visual(root.transform, "Visual", mesh, Material("Palette"));
                configure(root);
            });
        }

        private static void PowerUp(string name, MeshBuilder model, PowerUpType type, string halo)
        {
            Mesh mesh = RunnerAssets.SaveMesh(model, $"Art/Models/{name}.asset");
            SavePiece<PowerUpPickup>(name, 0.5f, root =>
            {
                var pickup = root.GetComponent<PowerUpPickup>();
                pickup.type = type;
                pickup.radius = 0.85f;
                GameObject visual = Visual(root.transform, "Visual", mesh, Material("Palette"));
                visual.transform.localScale = Vector3.one * 1.3f;
                Spin(visual, 110f, 0.15f, 2.5f);
                Halo(root.transform, halo, 2.2f);
            });
        }

        private static T SavePiece<T>(string name, float length, Action<GameObject> configure) where T : TrackPiece
        {
            var root = new GameObject(name);
            var piece = root.AddComponent<T>();
            piece.length = length;
            configure(root);
            return RunnerAssets.SavePrefab(root, $"Prefabs/Pieces/{name}.prefab").GetComponent<T>();
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

        private static GameObject Halo(Transform parent, string material, float size)
        {
            var halo = new GameObject("Halo");
            halo.transform.SetParent(parent, false);
            halo.transform.localScale = Vector3.one * size;
            halo.AddComponent<MeshFilter>().sharedMesh = Resources.GetBuiltinResource<Mesh>("Quad.fbx");
            var renderer = halo.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = Material(material);
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            halo.AddComponent<Billboard>().pulse = 0.12f;
            return halo;
        }

        public static T Piece<T>(string name) where T : Object
        {
            var prefab = RunnerAssets.Load<GameObject>($"Prefabs/Pieces/{name}.prefab");
            if (prefab == null)
            {
                throw new InvalidOperationException($"Missing piece prefab {name}; build the art first.");
            }
            return typeof(T) == typeof(GameObject) ? prefab as T : prefab.GetComponent(typeof(T)) as T;
        }

        public static TerrainBehaviour Tile(bool chasm)
        {
            return RunnerAssets.Load<GameObject>($"Prefabs/Tiles/{(chasm ? "ChasmTile" : "RoadTile")}.prefab").GetComponent<TerrainBehaviour>();
        }

        public static IEnumerable<string> WagonNames(bool longWagons)
        {
            foreach (string color in new[] { "Red", "Blue", "Green", "Yellow" })
            {
                yield return $"Wagon{(longWagons ? "Long" : "Short")}{color}";
            }
        }

        // ------------------------------------------------------------------ scenery

        private static void BuildScenery()
        {
            Material palette = Material("Palette");
            foreach (SceneryRecipe recipe in Scenery)
            {
                Mesh mesh = RunnerAssets.SaveMesh(recipe.Model(), $"Art/Models/Scenery/{recipe.Name}.asset");
                var root = new GameObject(recipe.Name);
                var piece = root.AddComponent<TrackPiece>();
                piece.length = recipe.Length;
                GameObject visual = Visual(root.transform, "Visual", mesh, palette);
                if (recipe.Name == "Volcano" || recipe.Name == "Mesa")
                {
                    visual.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                }
                RunnerAssets.SavePrefab(root, $"Prefabs/Scenery/{recipe.Theme}/{recipe.Name}.prefab");
            }
        }

        public static TrackPiece SceneryPiece(string theme, string name)
        {
            var prefab = RunnerAssets.Load<GameObject>($"Prefabs/Scenery/{theme}/{name}.prefab");
            return prefab != null ? prefab.GetComponent<TrackPiece>() : null;
        }

        public static IEnumerable<TrackPiece> AllScenery()
        {
            foreach (SceneryRecipe recipe in Scenery)
            {
                TrackPiece piece = SceneryPiece(recipe.Theme, recipe.Name);
                if (piece != null)
                {
                    yield return piece;
                }
            }
        }

        // ------------------------------------------------------------------ runner

        private static void BuildPlayer()
        {
            Material palette = Material("Palette");
            Mesh torso = RunnerAssets.SaveMesh(RunnerModels.Torso(), "Art/Models/Runner/Torso.asset");
            Mesh head = RunnerAssets.SaveMesh(RunnerModels.Head(), "Art/Models/Runner/Head.asset");
            Mesh upperArm = RunnerAssets.SaveMesh(RunnerModels.UpperArm(), "Art/Models/Runner/UpperArm.asset");
            Mesh forearm = RunnerAssets.SaveMesh(RunnerModels.Forearm(), "Art/Models/Runner/Forearm.asset");
            Mesh thigh = RunnerAssets.SaveMesh(RunnerModels.Thigh(), "Art/Models/Runner/Thigh.asset");
            Mesh shin = RunnerAssets.SaveMesh(RunnerModels.Shin(), "Art/Models/Runner/Shin.asset");
            Mesh magnetRing = RunnerAssets.SaveMesh(RunnerModels.Ring(0.62f, 0.05f, Swatch.GlowRed), "Art/Models/Runner/MagnetRing.asset");
            Mesh springRing = RunnerAssets.SaveMesh(RunnerModels.Ring(0.34f, 0.045f, Swatch.GlowGreen), "Art/Models/Runner/SpringRing.asset");

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

            Transform hips = Joint(body.transform, "Hips", new Vector3(0f, 0.78f, 0f), null, null);
            Transform spine = Joint(hips, "Spine", Vector3.zero, torso, palette);
            Transform headJoint = Joint(spine, "Head", new Vector3(0f, 0.54f, 0f), head, palette);
            Transform armLeft = Joint(spine, "ArmLeft", new Vector3(-0.265f, 0.46f, 0f), upperArm, palette);
            Transform forearmLeft = Joint(armLeft, "ForearmLeft", new Vector3(0f, -0.25f, 0f), forearm, palette);
            Transform armRight = Joint(spine, "ArmRight", new Vector3(0.265f, 0.46f, 0f), upperArm, palette);
            Transform forearmRight = Joint(armRight, "ForearmRight", new Vector3(0f, -0.25f, 0f), forearm, palette);
            Transform legLeft = Joint(hips, "LegLeft", new Vector3(-0.11f, 0f, 0f), thigh, palette);
            Transform shinLeft = Joint(legLeft, "ShinLeft", new Vector3(0f, -0.36f, 0f), shin, palette);
            Transform legRight = Joint(hips, "LegRight", new Vector3(0.11f, 0f, 0f), thigh, palette);
            Transform shinRight = Joint(legRight, "ShinRight", new Vector3(0f, -0.36f, 0f), shin, palette);

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
            GameObject shieldRing = Halo(shield.transform, "ShieldRing", 2.1f);
            shieldRing.GetComponent<Billboard>().pulse = 0.04f;
            Halo(shield.transform, "HaloShield", 2.3f).GetComponent<Billboard>().pulse = 0.06f;
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
            Halo(boots.transform, "HaloSuperJump", 1.4f).transform.localPosition = new Vector3(0f, 0.2f, 0f);
            boots.SetActive(false);

            ParticleSystem dust = ParticleFactory.Create("RunDust", root.transform, Material("ParticleSmoke"));
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

            SetLayer(root, RunnerPlayer.Layer);
            RunnerAssets.SavePrefab(root, "Prefabs/Player.prefab");
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
