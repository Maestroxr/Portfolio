using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Portfolio.Asteroids.EditorTools
{
    /// <summary>
    /// Generates the Asteroids module's art: textures, interface sprites and icons, materials (over the procedural
    /// textures and the model packs in Art/StarSparrow and Art/AsteroidsPack), meshes, the sounds and music, and the
    /// hangar pictures of the ships. Everything is written to fixed paths, so running it again updates in place.
    /// </summary>
    internal static class AsteroidsArtBuilder
    {
        public const string StarSparrow = "Art/StarSparrow";
        public const string RockPack = "Art/AsteroidsPack/Assets";

        public static readonly string[] SectorNames = { "Kepler", "Crimson", "Frost", "Void" };

        /// <summary>The hangar: name, StarSparrow model, material colour, engine colour.</summary>
        public static readonly (string name, string model, string material, Color engine)[] Hulls =
        {
            ("Sparrow", "StarSparrow2", "StarSparrow Blue", new Color(0.4f, 0.8f, 1f)),
            ("Hornet", "StarSparrow11", "StarSparrow Yellow", new Color(1f, 0.75f, 0.3f)),
            ("Bulwark", "StarSparrow8", "Unified/StarSparrow Grey", new Color(0.55f, 1f, 0.7f)),
            ("Phantom", "StarSparrow12", "StarSparrow Purple", new Color(0.85f, 0.5f, 1f))
        };

        private static Shader Lit => Shader.Find("Universal Render Pipeline/Lit");
        private static Shader Glow => AssetDatabase.LoadAssetAtPath<Shader>(AsteroidsAssets.Path("Art/Shaders/Glow.shader"));
        private static Shader ShieldShader => AssetDatabase.LoadAssetAtPath<Shader>(AsteroidsAssets.Path("Art/Shaders/Shield.shader"));
        private static Shader BackgroundShader => AssetDatabase.LoadAssetAtPath<Shader>(AsteroidsAssets.Path("Art/Shaders/SpaceBackground.shader"));

        /// <summary>Generates every art asset.</summary>
        public static void BuildAll()
        {
            Progress("Textures", 0.05f);
            BuildTextures();
            Progress("Interface", 0.2f);
            BuildInterface();
            Progress("Materials", 0.35f);
            BuildMaterials();
            Progress("Models", 0.5f);
            BuildMeshes();
            Progress("Sounds", 0.65f);
            BuildSounds();
            Progress("Hangar pictures", 0.85f);
            BuildShipPreviews();
            AssetDatabase.SaveAssets();
            EditorUtility.ClearProgressBar();
        }

        private static void Progress(string step, float value)
        {
            EditorUtility.DisplayProgressBar("Asteroids", $"Building {step}...", value);
        }

        // ------------------------------------------------------------------ textures

        private static void BuildTextures()
        {
            AsteroidsAssets.SaveTexture(SpaceTextures.Palette(false), "Art/Textures/Palette.png", AsteroidsAssets.PaletteTexture);
            AsteroidsAssets.SaveTexture(SpaceTextures.Palette(true), "Art/Textures/PaletteEmission.png", AsteroidsAssets.PaletteTexture);
            AsteroidsAssets.SaveTexture(SpaceTextures.Nebula(), "Art/Textures/Nebula.png", AsteroidsAssets.DataTexture);
            AsteroidsAssets.SaveTexture(SpaceTextures.OreVeins(), "Art/Textures/OreVeins.png", AsteroidsAssets.TileableTexture);
            AsteroidsAssets.SaveTexture(SpaceTextures.LavaCracks(), "Art/Textures/LavaCracks.png", AsteroidsAssets.TileableTexture);
            AsteroidsAssets.SaveTexture(SpaceTextures.GasGiant(new Color(0.1f, 0.25f, 0.45f), new Color(0.25f, 0.6f, 0.75f), new Color(0.6f, 0.9f, 0.95f),
                new Color(0.95f, 0.98f, 1f), 811, false), "Art/Textures/PlanetKepler.png", AsteroidsAssets.ClampedTexture);
            AsteroidsAssets.SaveTexture(SpaceTextures.IcePlanet(), "Art/Textures/PlanetFrost.png", AsteroidsAssets.ClampedTexture);
            AsteroidsAssets.SaveTexture(SpaceTextures.GasGiant(new Color(0.12f, 0.05f, 0.25f), new Color(0.35f, 0.15f, 0.55f), new Color(0.75f, 0.35f, 0.8f),
                new Color(1f, 0.75f, 0.95f), 911, true), "Art/Textures/PlanetVoid.png", AsteroidsAssets.ClampedTexture);
            AsteroidsAssets.SaveTexture(SpaceTextures.RingBands(), "Art/Textures/RingBands.png", importer =>
            {
                AsteroidsAssets.ParticleTexture(importer);
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.mipmapEnabled = true;
            });
            AsteroidsAssets.SaveTexture(SpaceTextures.AccretionDisk(), "Art/Textures/AccretionDisk.png", AsteroidsAssets.ParticleTexture);
            AsteroidsAssets.SaveTexture(SpaceTextures.SoftDot(), "Art/Textures/SoftDot.png", AsteroidsAssets.ParticleTexture);
            AsteroidsAssets.SaveTexture(SpaceTextures.AtmosphereHalo(), "Art/Textures/AtmosphereHalo.png", AsteroidsAssets.ParticleTexture);
            AsteroidsAssets.SaveTexture(SpaceTextures.Glow(), "Art/Textures/Glow.png", AsteroidsAssets.ParticleTexture);
            AsteroidsAssets.SaveTexture(SpaceTextures.Flare(), "Art/Textures/Flare.png", AsteroidsAssets.ParticleTexture);
            AsteroidsAssets.SaveTexture(SpaceTextures.Streak(), "Art/Textures/Streak.png", AsteroidsAssets.ParticleTexture);
            AsteroidsAssets.SaveTexture(SpaceTextures.Smoke(), "Art/Textures/Smoke.png", AsteroidsAssets.ParticleTexture);
            AsteroidsAssets.SaveTexture(SpaceTextures.Ring(), "Art/Textures/Ring.png", AsteroidsAssets.ParticleTexture);
            AsteroidsAssets.SaveTexture(SpaceTextures.DangerRing(), "Art/Textures/DangerRing.png", AsteroidsAssets.ParticleTexture);
            AsteroidsAssets.SaveTexture(SpaceTextures.Bolt(), "Art/Textures/Bolt.png", AsteroidsAssets.ParticleTexture);
            AsteroidsAssets.SaveTexture(SpaceTextures.Plasma(), "Art/Textures/Plasma.png", AsteroidsAssets.ParticleTexture);
            AsteroidsAssets.SaveTexture(SpaceTextures.Flame(), "Art/Textures/Flame.png", AsteroidsAssets.ParticleTexture);
            AsteroidsAssets.SaveTexture(SpaceTextures.Shard(), "Art/Textures/Shard.png", AsteroidsAssets.ParticleTexture);
            AsteroidsAssets.SaveTexture(SpaceTextures.Square(), "Art/Textures/Square.png", AsteroidsAssets.ParticleTexture);
            AsteroidsAssets.SaveTexture(SpaceTextures.ReflectionStudio(), "Art/Textures/ReflectionStudio.png", AsteroidsAssets.CubemapTexture);
        }

        /// <summary>The scene's reflection environment (<see cref="SpaceTextures.ReflectionStudio"/> as a cube map).</summary>
        public static Cubemap ReflectionCube => AsteroidsAssets.Load<Cubemap>("Art/Textures/ReflectionStudio.png");

        private static void BuildInterface()
        {
            AsteroidsAssets.SaveTexture(SpaceIcons.Panel(false), "Art/Interface/Panel.png", AsteroidsAssets.SpriteTexture(new Vector4(18f, 18f, 18f, 18f)));
            AsteroidsAssets.SaveTexture(SpaceIcons.Panel(true), "Art/Interface/Button.png", AsteroidsAssets.SpriteTexture(new Vector4(18f, 18f, 18f, 18f)));
            AsteroidsAssets.SaveTexture(SpaceIcons.Frame(), "Art/Interface/Frame.png", AsteroidsAssets.SpriteTexture(new Vector4(20f, 20f, 20f, 20f)));
            AsteroidsAssets.SaveTexture(SpaceIcons.Hexagon(false), "Art/Interface/Hexagon.png", AsteroidsAssets.SpriteTexture(Vector4.zero));
            AsteroidsAssets.SaveTexture(SpaceIcons.Hexagon(true), "Art/Interface/HexagonFrame.png", AsteroidsAssets.SpriteTexture(Vector4.zero));
            AsteroidsAssets.SaveTexture(SpaceIcons.Bar(), "Art/Interface/Bar.png", AsteroidsAssets.SpriteTexture(new Vector4(6f, 6f, 6f, 6f)));
            AsteroidsAssets.SaveTexture(SpaceIcons.TimerRing(), "Art/Interface/TimerRing.png", AsteroidsAssets.SpriteTexture(Vector4.zero));
            AsteroidsAssets.SaveTexture(SpaceIcons.Vignette(), "Art/Interface/Vignette.png", AsteroidsAssets.SpriteTexture(Vector4.zero));
            AsteroidsAssets.SaveTexture(SpaceIcons.Fade(), "Art/Interface/Fade.png", AsteroidsAssets.SpriteTexture(Vector4.zero));
            AsteroidsAssets.SaveTexture(SpaceIcons.Lane(), "Art/Interface/Lane.png", AsteroidsAssets.SpriteTexture(Vector4.zero));
            AsteroidsAssets.SaveTexture(SpaceTextures.Glow(), "Art/Interface/Glow.png", AsteroidsAssets.SpriteTexture(Vector4.zero));
            foreach (string name in SpaceIcons.IconNames)
            {
                AsteroidsAssets.SaveTexture(SpaceIcons.Icon(name), $"Art/Icons/{name}.png", AsteroidsAssets.SpriteTexture(Vector4.zero));
            }
            AsteroidsAssets.SaveTexture(SpaceIcons.GameIcon(), "Art/Icons/GameIcon.png", AsteroidsAssets.SpriteTexture(Vector4.zero));
        }

        public static Sprite Sprite(string relative)
        {
            return AsteroidsAssets.Load<Sprite>(relative);
        }

        public static Sprite Icon(string name)
        {
            return AsteroidsAssets.Load<Sprite>($"Art/Icons/{name}.png");
        }

        public static Sprite Interface(string name)
        {
            return AsteroidsAssets.Load<Sprite>($"Art/Interface/{name}.png");
        }

        public static Texture2D Texture(string name)
        {
            return AsteroidsAssets.Load<Texture2D>($"Art/Textures/{name}.png");
        }

        // ------------------------------------------------------------------ materials

        private static void BuildMaterials()
        {
            Texture2D palette = Texture("Palette");
            Texture2D paletteEmission = Texture("PaletteEmission");
            var rock = AsteroidsAssets.Load<Texture2D>($"{RockPack}/Textures/seamless_rock_texture.jpg");
            var rockDetail = AsteroidsAssets.Load<Texture2D>($"{RockPack}/Textures/seamless_rock_texture_detail.jpg");

            AsteroidsAssets.SaveMaterial("Art/Materials/Palette.mat", Lit, m => SetupLit(m, Color.white, palette, null, 0.45f, 0.25f, paletteEmission, new Color(2.2f, 2.2f, 2.2f)));
            AsteroidsAssets.SaveMaterial("Art/Materials/IcePalette.mat", Lit, m => SetupLit(m, new Color(0.86f, 0.9f, 0.95f), palette, null, 0.8f, 0.05f, paletteEmission, new Color(0.9f, 0.9f, 0.9f)));
            AsteroidsAssets.SaveMaterial("Art/Materials/CrystalPalette.mat", Lit, m => SetupLit(m, Color.white, palette, null, 0.75f, 0.2f, paletteEmission, new Color(3f, 3f, 3f)));
            AsteroidsAssets.SaveMaterial("Art/Materials/Rock.mat", Lit, m => SetupLit(m, new Color(0.82f, 0.78f, 0.74f), rock, rockDetail, 0.12f, 0f, null, Color.black));
            AsteroidsAssets.SaveMaterial("Art/Materials/Ore.mat", Lit, m => SetupLit(m, new Color(0.62f, 0.5f, 0.4f), rock, rockDetail, 0.35f, 0.45f, Texture("OreVeins"), new Color(3.2f, 2.3f, 0.8f)));
            AsteroidsAssets.SaveMaterial("Art/Materials/Magma.mat", Lit, m => SetupLit(m, new Color(0.3f, 0.26f, 0.26f), rock, rockDetail, 0.2f, 0f, Texture("LavaCracks"), new Color(3.4f, 1.3f, 0.4f)));
            AsteroidsAssets.SaveMaterial("Art/Materials/TitanRock.mat", Lit, m => SetupLit(m, new Color(0.64f, 0.56f, 0.52f), rock, rockDetail, 0.25f, 0.1f, Texture("LavaCracks"), new Color(3.2f, 0.8f, 2.1f), 0.6f));
            AsteroidsAssets.SaveMaterial("Art/Materials/Debris.mat", Lit, m => SetupLit(m, Color.white, rock, null, 0.1f, 0f, null, Color.black));

            var insectAlbedo = AsteroidsAssets.Load<Texture2D>($"{StarSparrow}/Textures/BonusContent/FlyingInsect/FlyingInsect_Purple.png");
            var insectNormal = AsteroidsAssets.Load<Texture2D>($"{StarSparrow}/Textures/BonusContent/FlyingInsect/FlyingInsect_Normal.png");
            AsteroidsAssets.SaveMaterial("Art/Materials/HiveQueen.mat", Lit, m => SetupLit(m, new Color(1f, 0.85f, 1f), insectAlbedo, insectNormal, 0.6f, 0.2f, insectAlbedo, new Color(1.4f, 0.4f, 1.8f)));
            AsteroidsAssets.SaveMaterial("Art/Materials/Wasp.mat", Lit, m => SetupLit(m, Color.white, insectAlbedo, insectNormal, 0.55f, 0.15f, insectAlbedo, new Color(0.5f, 0.15f, 0.7f)));

            AsteroidsAssets.SaveMaterial("Art/Materials/Space.mat", BackgroundShader, m =>
            {
                m.SetTexture("_NebulaTex", Texture("Nebula"));
                m.SetColor("_SpaceColor", new Color(0.01f, 0.012f, 0.03f));
                m.SetColor("_NebulaColor", new Color(0.1f, 0.3f, 0.6f));
                m.SetColor("_HighlightColor", new Color(0.4f, 0.9f, 1f));
                m.SetColor("_DustColor", new Color(0.02f, 0.01f, 0.04f));
                m.SetFloat("_StarDensity", 1f);
            });

            AsteroidsAssets.SaveMaterial("Art/Materials/PlanetKepler.mat", Lit, m => SetupLit(m, Color.white, Texture("PlanetKepler"), null, 0.3f, 0f, null, Color.black));
            AsteroidsAssets.SaveMaterial("Art/Materials/PlanetFrost.mat", Lit, m => SetupLit(m, Color.white, Texture("PlanetFrost"), null, 0.35f, 0f, null, Color.black));
            AsteroidsAssets.SaveMaterial("Art/Materials/PlanetVoid.mat", Lit, m => SetupLit(m, Color.white, Texture("PlanetVoid"), null, 0.3f, 0f, null, Color.black));
            AsteroidsAssets.SaveMaterial("Art/Materials/Atmosphere.mat", ShieldShader, m =>
            {
                m.SetColor("_BaseColor", new Color(0.3f, 0.6f, 1f, 1f));
                m.SetFloat("_FresnelPower", 3.2f);
                m.SetFloat("_Hex", 0f);
                m.SetFloat("_Core", 0f);
                m.SetVector("_Hit", new Vector4(0f, 1f, 0f, 0f));
            });
            AsteroidsAssets.SaveMaterial("Art/Materials/ShipShield.mat", ShieldShader, m =>
            {
                m.SetColor("_BaseColor", new Color(0.35f, 0.8f, 1.6f, 1f));
                m.SetFloat("_FresnelPower", 2.4f);
                m.SetFloat("_Hex", 1f);
                m.SetFloat("_HexScale", 3.2f);
                m.SetFloat("_Core", 0.05f);
            });
            AsteroidsAssets.SaveMaterial("Art/Materials/BossShield.mat", ShieldShader, m =>
            {
                m.SetColor("_BaseColor", new Color(1.4f, 0.45f, 1.6f, 0.9f));
                m.SetFloat("_FresnelPower", 2f);
                m.SetFloat("_Hex", 1f);
                m.SetFloat("_HexScale", 2.2f);
                m.SetFloat("_Core", 0.08f);
            });
            AsteroidsAssets.SaveMaterial("Art/Materials/BlackHole.mat", Shader.Find("Universal Render Pipeline/Unlit"), m =>
            {
                m.SetColor("_BaseColor", Color.black);
            });

            GlowMaterial("PlanetRing", Texture("RingBands"), new Color(1f, 1f, 1f, 1f), false, true);
            GlowMaterial("Accretion", Texture("AccretionDisk"), new Color(1.6f, 1.2f, 2f, 1f), true);
            GlowMaterial("AtmosphereHalo", Texture("AtmosphereHalo"), new Color(0.3f, 0.6f, 1f, 1f), true);
            GlowMaterial("Glow", Texture("Glow"), Color.white, true);
            GlowMaterial("Beacon", Texture("Glow"), Color.white, true);
            GlowMaterial("Solid", Texture("Square"), Color.white, true);
            GlowMaterial("Flare", Texture("Flare"), Color.white, true);
            GlowMaterial("Ring", Texture("Ring"), Color.white, true);
            GlowMaterial("DangerRing", Texture("DangerRing"), new Color(2f, 0.35f, 0.25f, 0.9f), true);
            GlowMaterial("Bolt", Texture("Bolt"), new Color(0.6f, 1.8f, 3f, 1f), true);
            GlowMaterial("DroneBolt", Texture("Bolt"), new Color(0.6f, 3f, 1.6f, 1f), true);
            GlowMaterial("Laser", Texture("Bolt"), new Color(3.5f, 0.6f, 1f, 1f), true);
            GlowMaterial("Pellet", Texture("Plasma"), new Color(3f, 1.6f, 0.5f, 1f), true);
            GlowMaterial("EnemyPlasma", Texture("Plasma"), new Color(3f, 0.4f, 0.9f, 1f), true);
            GlowMaterial("Shrapnel", Texture("Bolt"), new Color(3f, 1.3f, 0.3f, 1f), true);
            GlowMaterial("VoidShard", Texture("Shard"), new Color(2.2f, 0.6f, 3f, 1f), true);
            GlowMaterial("Acid", Texture("Plasma"), new Color(1.2f, 3f, 0.6f, 1f), true);
            GlowMaterial("EngineFlame", Texture("Flame"), new Color(0.8f, 1.6f, 3f, 1f), true);
            GlowMaterial("BossFlame", Texture("Flame"), new Color(3f, 0.8f, 0.4f, 1f), true);
            GlowMaterial("Muzzle", Texture("Glow"), Color.white, true);
            GlowMaterial("ParticleAdd", Texture("SoftDot"), Color.white, true);
            GlowMaterial("ParticleSpark", Texture("Streak"), Color.white, true);
            GlowMaterial("ParticleFlare", Texture("Flare"), Color.white, true);
            GlowMaterial("ParticleShard", Texture("Shard"), Color.white, true);
            GlowMaterial("ParticleSmoke", Texture("Smoke"), Color.white, false);
            GlowMaterial("ParticleRing", Texture("Ring"), Color.white, true);

            foreach (string icon in new[] { "Repair", "Shield", "Blaster", "Laser", "Scatter", "Missile", "Overdrive", "Magnet", "Chrono", "Drones", "Ship", "Nova" })
            {
                Texture2D texture = AsteroidsAssets.Load<Texture2D>($"Art/Icons/{icon}.png");
                GlowMaterial($"Icon{icon}", texture, Color.white, false);
            }
        }

        /// <summary>A material of the Glow shader: additive, or alpha blended when <paramref name="additive"/> is false.</summary>
        public static Material GlowMaterial(string name, Texture texture, Color color, bool additive, bool twoSided = true)
        {
            return AsteroidsAssets.SaveMaterial($"Art/Materials/{name}.mat", Glow, m =>
            {
                m.SetTexture("_BaseMap", texture);
                m.SetColor("_BaseColor", color);
                m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                m.SetFloat("_DstBlend", additive ? (float)BlendMode.One : (float)BlendMode.OneMinusSrcAlpha);
                m.SetFloat("_Cull", twoSided ? (float)CullMode.Off : (float)CullMode.Back);
                m.renderQueue = (int)RenderQueue.Transparent;
            });
        }

        /// <summary>URP Lit, opaque, with an optional normal map and emission (black emission keeps the hit flash working).</summary>
        private static void SetupLit(Material material, Color color, Texture baseMap, Texture normalMap, float smoothness, float metallic,
            Texture emissionMap, Color emission, float tiling = 1f)
        {
            material.SetFloat("_WorkflowMode", 1f);
            material.SetFloat("_Surface", 0f);
            material.SetFloat("_Blend", 0f);
            material.SetFloat("_AlphaClip", 0f);
            material.SetColor("_BaseColor", color);
            material.SetTexture("_BaseMap", baseMap);
            material.SetTextureScale("_BaseMap", Vector2.one * tiling);
            material.SetFloat("_Smoothness", smoothness);
            material.SetFloat("_Metallic", metallic);
            material.SetFloat("_EnvironmentReflections", 0f);
            material.SetFloat("_SpecularHighlights", 1f);
            material.SetFloat("_ReceiveShadows", 1f);
            material.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF");
            material.DisableKeyword("_SPECULARHIGHLIGHTS_OFF");
            material.DisableKeyword("_RECEIVE_SHADOWS_OFF");
            material.SetTexture("_BumpMap", normalMap);
            material.SetFloat("_BumpScale", 1f);
            if (normalMap != null)
            {
                material.EnableKeyword("_NORMALMAP");
            }
            else
            {
                material.DisableKeyword("_NORMALMAP");
            }
            material.SetColor("_EmissionColor", emission);
            material.SetTexture("_EmissionMap", emissionMap);
            material.EnableKeyword("_EMISSION");
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            UnityEditor.BaseShaderGUI.SetupMaterialBlendMode(material);
        }

        public static Material Material(string name)
        {
            return AsteroidsAssets.Load<Material>($"Art/Materials/{name}.mat");
        }

        public static Material PackMaterial(string relative)
        {
            return AsteroidsAssets.Load<Material>($"{StarSparrow}/Materials/{relative}.mat");
        }

        public static Mesh PackMesh(string file, string name)
        {
            return AsteroidsAssets.LoadSub<Mesh>($"{StarSparrow}/Meshes/{file}", name);
        }

        // ------------------------------------------------------------------ meshes

        private static void BuildMeshes()
        {
            for (int i = 1; i <= 3; i++)
            {
                Mesh source = AsteroidsAssets.LoadSub<Mesh>($"{RockPack}/Meshes/asteroid{i}_LOD1.fbx", "Cube");
                SaveNormalized(source, $"Art/Models/Rock{i}.asset", 1f);
                Mesh detailed = AsteroidsAssets.LoadSub<Mesh>($"{RockPack}/Meshes/asteroid{i}_LOD0.fbx", "Cube");
                if (i == 3)
                {
                    SaveNormalized(detailed, "Art/Models/TitanRock.asset", 1f);
                }
            }
            SaveNormalized(PackMesh("BonusContent/AsteroidsSample.FBX", "Emissive/LavaAsteroid2"), "Art/Models/Comet.asset", 1f);
            for (int i = 0; i < 3; i++)
            {
                AsteroidsAssets.SaveMesh(SpaceModels.IceChunk(1100 + i * 17), $"Art/Models/Ice{i + 1}.asset");
                AsteroidsAssets.SaveMesh(SpaceModels.CrystalRock(1300 + i * 23), $"Art/Models/Crystal{i + 1}.asset");
            }
            AsteroidsAssets.SaveMesh(SpaceModels.Saucer(false), "Art/Models/Saucer.asset");
            AsteroidsAssets.SaveMesh(SpaceModels.SaucerLights(false), "Art/Models/SaucerLights.asset");
            AsteroidsAssets.SaveMesh(SpaceModels.Saucer(true), "Art/Models/Scout.asset");
            AsteroidsAssets.SaveMesh(SpaceModels.SaucerLights(true, 10), "Art/Models/ScoutLights.asset");
            AsteroidsAssets.SaveMesh(SpaceModels.SupplyPod(), "Art/Models/SupplyPod.asset");
            AsteroidsAssets.SaveMesh(SpaceModels.Drone(), "Art/Models/Drone.asset");
            AsteroidsAssets.SaveMesh(SpaceModels.Gem(Swatch.GlowCyan, Swatch.GlowWhite), "Art/Models/Crystal.asset");
            AsteroidsAssets.SaveMesh(SpaceModels.PickupFrame(), "Art/Models/PickupFrame.asset");
            SaveMesh(SpaceModels.FlameQuad(), "Art/Models/FlameQuad.asset");
            SaveMesh(SpaceModels.Annulus(1.35f, 2.35f, 96), "Art/Models/PlanetRing.asset");
        }

        private static void SaveNormalized(Mesh source, string relative, float radius)
        {
            if (source == null)
            {
                Debug.LogError($"Asteroids: the source mesh for {relative} is missing (is the model pack in {StarSparrow} and {RockPack}?).");
                return;
            }
            string name = System.IO.Path.GetFileNameWithoutExtension(relative);
            SaveMesh(SpaceModels.Normalized(source, name, radius), relative);
        }

        /// <summary>Saves a finished mesh, replacing the data of an existing asset at the path.</summary>
        private static void SaveMesh(Mesh mesh, string relative)
        {
            string path = AsteroidsAssets.Path(relative);
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing == null)
            {
                int slash = relative.LastIndexOf('/');
                AsteroidsAssets.EnsureFolder(relative.Substring(0, slash));
                AssetDatabase.CreateAsset(mesh, path);
                return;
            }
            bool same = existing.vertexCount == mesh.vertexCount && existing.vertices.SequenceEqual(mesh.vertices) && existing.triangles.SequenceEqual(mesh.triangles);
            if (!same)
            {
                EditorUtility.CopySerialized(mesh, existing);
                existing.name = System.IO.Path.GetFileNameWithoutExtension(relative);
                EditorUtility.SetDirty(existing);
            }
            Object.DestroyImmediate(mesh);
        }

        public static Mesh Model(string name)
        {
            return AsteroidsAssets.Load<Mesh>($"Art/Models/{name}.asset");
        }

        // ------------------------------------------------------------------ sounds

        private static void BuildSounds()
        {
            foreach (string name in SpaceSounds.EffectNames)
            {
                AsteroidsAssets.SaveAudio(SpaceSounds.Effect(name), $"Art/Audio/{name}.wav", SpaceSounds.IsLoop(name));
            }
            AsteroidsAssets.SaveAudio(SpaceSounds.MenuMusic(), "Art/Audio/MenuMusic.wav", true);
            AsteroidsAssets.SaveAudio(SpaceSounds.BattleMusic(), "Art/Audio/BattleMusic.wav", true);
            AsteroidsAssets.SaveAudio(SpaceSounds.BossMusic(), "Art/Audio/BossMusic.wav", true);
        }

        public static AudioClip Sound(string name)
        {
            return AsteroidsAssets.Load<AudioClip>($"Art/Audio/{name}.wav");
        }

        // ------------------------------------------------------------------ hangar pictures

        /// <summary>
        /// Renders every hangar ship from above into a transparent picture (two renders, over black and white). The
        /// temporary studio lives on a layer of its own far away, so whatever scene is open does not show up.
        /// </summary>
        private static void BuildShipPreviews()
        {
            const int studioLayer = 31;
            var studio = new GameObject("Hangar Studio") { hideFlags = HideFlags.HideAndDontSave };
            studio.transform.position = new Vector3(0f, -5000f, 0f);
            try
            {
                Light Lamp(string name, Quaternion rotation, float intensity, Color color)
                {
                    var lampObject = new GameObject(name) { hideFlags = HideFlags.HideAndDontSave };
                    lampObject.transform.SetParent(studio.transform, false);
                    lampObject.transform.rotation = rotation;
                    var lamp = lampObject.AddComponent<Light>();
                    lamp.type = LightType.Directional;
                    lamp.intensity = intensity;
                    lamp.color = color;
                    lamp.cullingMask = 1 << studioLayer;
                    return lamp;
                }

                Lamp("Key", Quaternion.Euler(35f, -30f, 0f), 1.6f, Color.white);
                Lamp("Fill", Quaternion.Euler(-30f, 150f, 0f), 0.6f, new Color(0.6f, 0.75f, 1f));

                var cameraObject = new GameObject("Camera") { hideFlags = HideFlags.HideAndDontSave };
                cameraObject.transform.SetParent(studio.transform, false);
                cameraObject.transform.localPosition = new Vector3(0f, 0f, -10f);
                var camera = cameraObject.AddComponent<Camera>();
                camera.orthographic = true;
                camera.orthographicSize = 0.62f;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.cullingMask = 1 << studioLayer;
                camera.allowHDR = false;
                camera.enabled = false;
                camera.GetUniversalAdditionalCameraData().renderPostProcessing = false;

                foreach ((string name, string model, string material, Color engine) in Hulls)
                {
                    Mesh mesh = ShipMesh(model);
                    if (mesh == null)
                    {
                        continue;
                    }
                    var ship = new GameObject(name) { hideFlags = HideFlags.HideAndDontSave, layer = studioLayer };
                    ship.transform.SetParent(studio.transform, false);
                    ship.AddComponent<MeshFilter>().sharedMesh = mesh;
                    ship.AddComponent<MeshRenderer>().sharedMaterial = PackMaterial(material);
                    float length = Mathf.Max(mesh.bounds.size.x, mesh.bounds.size.z);
                    ship.transform.localScale = Vector3.one / length;
                    ship.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
                    ship.transform.localPosition = -(ship.transform.localRotation * mesh.bounds.center) / length;
                    Texture2D picture = Matte(camera, 256);
                    AsteroidsAssets.SaveTexture(picture, $"Art/Previews/{name}.png", AsteroidsAssets.SpriteTexture(Vector4.zero));
                    Object.DestroyImmediate(ship);
                }
            }
            finally
            {
                Object.DestroyImmediate(studio);
            }
        }

        /// <summary>Renders the camera over black and over white and recovers colour and transparency from the difference.</summary>
        private static Texture2D Matte(Camera camera, int size)
        {
            Color[] black = Render(camera, size, Color.black);
            Color[] white = Render(camera, size, Color.white);
            var pixels = new Color[black.Length];
            for (int i = 0; i < pixels.Length; i++)
            {
                Color b = black[i];
                Color w = white[i];
                float alpha = Mathf.Clamp01(1f - ((w.r - b.r) + (w.g - b.g) + (w.b - b.b)) / 3f);
                pixels[i] = alpha > 0.001f ? new Color(b.r / alpha, b.g / alpha, b.b / alpha, alpha) : new Color(0f, 0f, 0f, 0f);
            }
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        private static Color[] Render(Camera camera, int size, Color background)
        {
            camera.backgroundColor = background;
            var target = RenderTexture.GetTemporary(size, size, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            camera.targetTexture = target;
            camera.Render();
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = target;
            var read = new Texture2D(size, size, TextureFormat.RGBA32, false);
            read.ReadPixels(new Rect(0, 0, size, size), 0, 0);
            read.Apply();
            RenderTexture.active = previous;
            camera.targetTexture = null;
            RenderTexture.ReleaseTemporary(target);
            Color[] pixels = read.GetPixels();
            Object.DestroyImmediate(read);
            return pixels;
        }

        public static Mesh ShipMesh(string model)
        {
            int number = int.Parse(model.Substring("StarSparrow".Length));
            string file = number <= 7 ? "StarSparrowExamples1.FBX" : "StarSparrowExamples2.FBX";
            return PackMesh(file, model);
        }

        public static Sprite Preview(string name)
        {
            return AsteroidsAssets.Load<Sprite>($"Art/Previews/{name}.png");
        }
    }
}
