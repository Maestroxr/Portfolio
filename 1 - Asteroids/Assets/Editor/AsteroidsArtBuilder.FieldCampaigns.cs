using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Portfolio.Asteroids.EditorTools
{
    /// <summary>
    /// The art of the second and third asteroid field campaigns (AsteroidsArtBuilder.FieldCampaigns.cs), under Art/Field:
    /// the surfaces of the eight sectors' stars and worlds and their materials, the iron, hydra and wreck rocks, the
    /// paints of the new enemies, allies and bosses (palette models tinted, so the Arcade 80s theme draws each in a neon
    /// of its own), the glows of the hazards and beams, the models of <see cref="SpaceModels"/> (FieldCampaigns), the
    /// sound effects and the two battle loops. Runs before the theme art, which derives the neon counterparts.
    /// </summary>
    internal static partial class AsteroidsArtBuilder
    {
        /// <summary>The folder of the field campaigns' art.</summary>
        public const string FieldArt = "Art/Field";

        /// <summary>The palette paints of the field campaigns' models: name, tint of the palette, strength of the glowing swatches.</summary>
        private static readonly (string name, Color tint, float glow)[] FieldPaints =
        {
            ("Hydra", new Color(0.74f, 1f, 0.64f), 2.4f), ("Wreck", new Color(1f, 0.8f, 0.68f), 2.6f),
            ("SentryPaint", new Color(1f, 0.86f, 0.74f), 2.2f), ("MinelayerPaint", new Color(1f, 0.96f, 0.72f), 2.2f),
            ("StalkerPaint", new Color(0.9f, 0.78f, 1f), 2.8f), ("AllyPaint", new Color(0.88f, 0.96f, 1f), 2.2f),
            ("HeraldPaint", new Color(0.78f, 0.88f, 1f), 3f), ("BastionPaint", new Color(1f, 0.84f, 0.72f), 2.4f),
            ("LeviathanPaint", new Color(1f, 0.78f, 0.7f), 2.8f), ("MatriarchPaint", new Color(0.94f, 0.78f, 1f), 3f),
            ("HydraHide", new Color(0.72f, 1f, 0.62f), 2.6f), ("GatePaint", new Color(1f, 0.93f, 0.72f), 3f)
        };

        /// <summary>The pack paints of the field campaigns' pack hulls: name, the pack material, a tint, the glow of the canopies.</summary>
        private static readonly (string name, string source, Color tint, Color glow)[] FieldHullPaints =
        {
            ("CorsairPaint", "StarSparrow Red", new Color(1f, 0.8f, 0.76f), new Color(2.4f, 0.45f, 0.25f)),
            ("WarlordPaint", "StarSparrow Red", new Color(0.6f, 0.52f, 0.5f), new Color(3f, 1.4f, 0.3f))
        };


        static partial void BuildFieldCampaignArt()
        {
            BuildFieldTextures();
            BuildFieldMaterials();
            BuildFieldMeshes();
            BuildFieldSounds();
        }


        // ------------------------------------------------------------------ accessors

        public static Material FieldMaterial(string name)
        {
            return AsteroidsAssets.Load<Material>($"{FieldArt}/Materials/{name}.mat");
        }


        public static Mesh FieldModel(string name)
        {
            return AsteroidsAssets.Load<Mesh>($"{FieldArt}/Models/{name}.asset");
        }


        public static AudioClip FieldSound(string name)
        {
            return AsteroidsAssets.Load<AudioClip>($"{FieldArt}/Audio/{name}.wav");
        }


        public static Texture2D FieldTexture(string name)
        {
            return AsteroidsAssets.Load<Texture2D>($"{FieldArt}/Textures/{name}.png");
        }


        // ------------------------------------------------------------------ textures

        private static void BuildFieldTextures()
        {
            string Path(string name) => $"{FieldArt}/Textures/{name}.png";
            Action<TextureImporter> planet = Flat2D(AsteroidsAssets.ClampedTexture);
            AsteroidsAssets.SaveTexture(SpaceTextures.StarSurface(new Color(0.35f, 0.04f, 0.02f), new Color(0.85f, 0.22f, 0.05f), new Color(1f, 0.7f, 0.3f), 6101, 1f),
                Path("PlanetEmber"), planet);
            AsteroidsAssets.SaveTexture(SpaceTextures.GasGiant(new Color(0.06f, 0.07f, 0.22f), new Color(0.2f, 0.25f, 0.6f), new Color(0.45f, 0.55f, 0.95f),
                new Color(0.85f, 0.9f, 1f), 6201, true), Path("PlanetIon"), planet);
            AsteroidsAssets.SaveTexture(SpaceTextures.IronMoon(6301), Path("PlanetIron"), planet);
            AsteroidsAssets.SaveTexture(SpaceTextures.StarSurface(new Color(0.9f, 0.55f, 0.2f), new Color(1f, 0.85f, 0.5f), new Color(1f, 0.98f, 0.9f), 6401, 0.35f),
                Path("PlanetCorona"), planet);
            AsteroidsAssets.SaveTexture(SpaceTextures.ShatteredWorld(6501, false), Path("PlanetShattered"), planet);
            AsteroidsAssets.SaveTexture(SpaceTextures.ShatteredWorld(6501, true), Path("PlanetShatteredGlow"), planet);
            AsteroidsAssets.SaveTexture(SpaceTextures.GasGiant(new Color(0.12f, 0.2f, 0.18f), new Color(0.3f, 0.45f, 0.42f), new Color(0.55f, 0.5f, 0.75f),
                new Color(0.85f, 0.95f, 0.9f), 6601, false), Path("PlanetPhantom"), planet);
            AsteroidsAssets.SaveTexture(SpaceTextures.GasGiant(new Color(0.1f, 0.18f, 0.04f), new Color(0.3f, 0.5f, 0.08f), new Color(0.7f, 0.8f, 0.2f),
                new Color(0.95f, 1f, 0.6f), 6701, true), Path("PlanetHydra"), planet);
            AsteroidsAssets.SaveTexture(SpaceTextures.MachineWorld(6801, false), Path("PlanetGate"), planet);
            AsteroidsAssets.SaveTexture(SpaceTextures.MachineWorld(6801, true), Path("PlanetGateGlow"), planet);
            AsteroidsAssets.SaveTexture(SpaceTextures.MetalScratches(), Path("MetalScratches"), Flat2D(AsteroidsAssets.TileableTexture));
            AsteroidsAssets.SaveTexture(SpaceTextures.FlareFront(), Path("FlareFront"), Flat2D(importer =>
            {
                AsteroidsAssets.ParticleTexture(importer);
                importer.wrapModeU = TextureWrapMode.Repeat;
                importer.wrapModeV = TextureWrapMode.Clamp;
            }));
            AsteroidsAssets.SaveTexture(SpaceTextures.EdgeGlow(), Path("EdgeGlow"), Flat2D(AsteroidsAssets.ParticleTexture));
            AsteroidsAssets.SaveTexture(SpaceTextures.Lightning(6901), Path("Lightning"), Flat2D(AsteroidsAssets.ParticleTexture));
            AsteroidsAssets.SaveTexture(SpaceTextures.IonCloud(7001), Path("IonCloud"), Flat2D(AsteroidsAssets.ParticleTexture));
            AsteroidsAssets.SaveTexture(SpaceTextures.Reticle(), Path("Reticle"), Flat2D(AsteroidsAssets.ParticleTexture));
        }


        // ------------------------------------------------------------------ materials

        private static void BuildFieldMaterials()
        {
            string Path(string name) => $"{FieldArt}/Materials/{name}.mat";
            Texture2D palette = Texture("Palette");
            Texture2D paletteEmission = Texture("PaletteEmission");
            var rock = AsteroidsAssets.Load<Texture2D>($"{RockPack}/Textures/seamless_rock_texture.jpg");
            var rockDetail = AsteroidsAssets.Load<Texture2D>($"{RockPack}/Textures/seamless_rock_texture_detail.jpg");

            // Iron: the rock pack's surface, steel blue and metallic, its scratches glinting.
            AsteroidsAssets.SaveMaterial(Path("Metal"), Lit, m => SetupLit(m, new Color(0.8f, 0.85f, 0.95f), rock, rockDetail, 0.5f, 0.45f,
                FieldTexture("MetalScratches"), new Color(0.6f, 0.72f, 0.95f), 1.4f));
            // The Slag Titan: iron with molten seams.
            AsteroidsAssets.SaveMaterial(Path("TitanIron"), Lit, m => SetupLit(m, new Color(0.5f, 0.52f, 0.58f), rock, rockDetail, 0.5f, 0.7f,
                Texture("LavaCracks"), new Color(3.2f, 1.1f, 0.3f), 0.6f));
            foreach ((string name, Color tint, float glow) in FieldPaints)
            {
                AsteroidsAssets.SaveMaterial(Path(name), Lit, m => SetupLit(m, tint, palette, null, 0.5f, 0.3f, paletteEmission, new Color(glow, glow, glow)));
            }
            foreach ((string name, string source, Color tint, Color glow) in FieldHullPaints)
            {
                Material pack = PackMaterial(source);
                AsteroidsAssets.SaveMaterial(Path(name), Lit, m =>
                {
                    if (pack != null)
                    {
                        m.CopyPropertiesFromMaterial(pack);
                        m.shaderKeywords = pack.shaderKeywords;
                    }
                    m.SetColor("_BaseColor", tint);
                    m.SetColor("_Color", tint);
                    m.SetColor("_EmissionColor", glow);
                    m.EnableKeyword("_EMISSION");
                });
            }

            // The planets and stars of the eight sectors (a star burns: its surface is its emission too).
            void Planet(string name, Color emission, string glowMap = null)
            {
                Texture2D surface = FieldTexture(name);
                Texture2D glow = glowMap != null ? FieldTexture(glowMap) : emission.maxColorComponent > 0f ? surface : null;
                AsteroidsAssets.SaveMaterial(Path(name), Lit, m => SetupLit(m, Color.white, surface, null, 0.3f, 0f, glow, emission));
            }
            Planet("PlanetEmber", new Color(1.25f, 0.75f, 0.55f));
            Planet("PlanetIon", Color.black);
            Planet("PlanetIron", Color.black);
            Planet("PlanetCorona", new Color(0.8f, 0.5f, 0.24f));
            Planet("PlanetShattered", new Color(3f, 1.4f, 0.5f), "PlanetShatteredGlow");
            Planet("PlanetPhantom", Color.black);
            Planet("PlanetHydra", Color.black);
            Planet("PlanetGate", new Color(3f, 2.4f, 1.2f), "PlanetGateGlow");

            // The glows of the hazards, beams and markers.
            FieldGlow("FlareFront", FieldTexture("FlareFront"), new Color(3.6f, 1.5f, 0.35f, 1f), true, new Vector2(5f, 1f));
            FieldGlow("FlareEdge", FieldTexture("EdgeGlow"), new Color(4f, 1.7f, 0.35f, 1f), true);
            FieldGlow("FlareEmber", Texture("SoftDot"), new Color(3f, 1.4f, 0.4f, 1f), true);
            FieldGlow("StormCloud", FieldTexture("IonCloud"), new Color(0.5f, 0.75f, 2.6f, 0.7f), true);
            FieldGlow("StormShade", FieldTexture("IonCloud"), new Color(0.03f, 0.04f, 0.14f, 0.9f), false);
            FieldGlow("StormArc", FieldTexture("Lightning"), new Color(1.3f, 1.7f, 3.2f, 1f), true);
            FieldGlow("LightningBolt", FieldTexture("Lightning"), new Color(1.8f, 2.2f, 3.4f, 1f), true);
            FieldGlow("ZoneRing", Texture("DangerRing"), new Color(0.7f, 1.3f, 3f, 0.95f), true);
            FieldGlow("LanceBeam", Texture("Bolt"), new Color(3.2f, 0.55f, 0.45f, 1f), true);
            FieldGlow("SweepBeam", Texture("Bolt"), new Color(3.2f, 1.7f, 0.35f, 1f), true);
            FieldGlow("BeamSource", Texture("Glow"), new Color(3f, 1.2f, 0.7f, 1f), true);
            FieldGlow("NovaOrb", Texture("Plasma"), new Color(3f, 1.6f, 0.5f, 1f), true);
            FieldGlow("NovaOrbHalo", Texture("Glow"), new Color(2.4f, 0.8f, 0.3f, 0.6f), true);
            FieldGlow("Reticle", FieldTexture("Reticle"), new Color(3f, 0.9f, 0.3f, 1f), true);
            FieldGlow("HullBarBack", Texture("Square"), new Color(0.04f, 0.06f, 0.08f, 0.75f), false);
            FieldGlow("HullBarFill", Texture("Square"), new Color(0.6f, 2f, 0.8f, 1f), true);
            FieldGlow("SentryCharge", Texture("Glow"), new Color(3f, 1.1f, 0.3f, 1f), true);
            FieldGlow("StalkerGlow", Texture("Glow"), new Color(1.4f, 0.5f, 2.6f, 0.45f), true);
            FieldGlow("CorsairGlow", Texture("Glow"), new Color(2.4f, 0.5f, 0.35f, 0.35f), true);
            FieldGlow("AllyGlow", Texture("Glow"), new Color(0.4f, 1.3f, 2.2f, 0.35f), true);
            FieldGlow("HeraldGlow", Texture("Glow"), new Color(0.4f, 1f, 2.6f, 0.4f), true);
            FieldGlow("BastionGlow", Texture("Glow"), new Color(2.4f, 1f, 0.35f, 0.35f), true);
            FieldGlow("SunGlow", Texture("Glow"), new Color(3f, 2f, 0.6f, 0.5f), true);
            FieldGlow("LeviathanGlow", Texture("Glow"), new Color(2.6f, 0.45f, 0.25f, 0.4f), true);
            FieldGlow("MatriarchGlow", Texture("Glow"), new Color(1.6f, 0.45f, 2.6f, 0.45f), true);
            FieldGlow("HydraGlow", Texture("Glow"), new Color(0.6f, 2.4f, 0.5f, 0.35f), true);
            FieldGlow("GateGlow", Texture("Glow"), new Color(2.8f, 2.1f, 0.8f, 0.45f), true);
        }


        /// <summary>A material of the Glow shader under Art/Field/Materials, as <see cref="GlowMaterial"/> makes them.</summary>
        public static Material FieldGlow(string name, Texture texture, Color color, bool additive, Vector2? tiling = null)
        {
            return AsteroidsAssets.SaveMaterial($"{FieldArt}/Materials/{name}.mat", Glow, m =>
            {
                m.SetTexture("_BaseMap", texture);
                m.SetTextureScale("_BaseMap", tiling ?? Vector2.one);
                m.SetColor("_BaseColor", color);
                m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                m.SetFloat("_DstBlend", additive ? (float)BlendMode.One : (float)BlendMode.OneMinusSrcAlpha);
                m.SetFloat("_Cull", (float)CullMode.Off);
                m.renderQueue = (int)RenderQueue.Transparent;
            });
        }


        // ------------------------------------------------------------------ models

        private static void BuildFieldMeshes()
        {
            string Path(string name) => $"{FieldArt}/Models/{name}.asset";
            for (int i = 0; i < 3; i++)
            {
                AsteroidsAssets.SaveMesh(SpaceModels.Wreck(i), Path($"Wreck{i + 1}"));
                AsteroidsAssets.SaveMesh(SpaceModels.HydraRock(4300 + i * 29), Path($"HydraRock{i + 1}"));
            }
            AsteroidsAssets.SaveMesh(SpaceModels.SentryBase(), Path("SentryBase"));
            AsteroidsAssets.SaveMesh(SpaceModels.SentryTurret(), Path("SentryTurret"));
            AsteroidsAssets.SaveMesh(SpaceModels.Minelayer(), Path("Minelayer"));
            AsteroidsAssets.SaveMesh(SpaceModels.MineDrum(), Path("MineDrum"));
            AsteroidsAssets.SaveMesh(SpaceModels.Stalker(), Path("Stalker"));
            AsteroidsAssets.SaveMesh(SpaceModels.Freighter(), Path("Freighter"));
            AsteroidsAssets.SaveMesh(SpaceModels.StationHub(), Path("StationHub"));
            AsteroidsAssets.SaveMesh(SpaceModels.StationRing(), Path("StationRing"));
            AsteroidsAssets.SaveMesh(SpaceModels.StormHerald(), Path("StormHerald"));
            AsteroidsAssets.SaveMesh(SpaceModels.HeraldRings(), Path("HeraldRings"));
            AsteroidsAssets.SaveMesh(SpaceModels.BastionFortress(), Path("BastionFortress"));
            AsteroidsAssets.SaveMesh(SpaceModels.BastionRing(), Path("BastionRing"));
            AsteroidsAssets.SaveMesh(SpaceModels.SunCannon(), Path("SunCannon"));
            AsteroidsAssets.SaveMesh(SpaceModels.ScrapMaw(), Path("ScrapMaw"));
            AsteroidsAssets.SaveMesh(SpaceModels.WraithMatriarch(), Path("WraithMatriarch"));
            AsteroidsAssets.SaveMesh(SpaceModels.Hydra(), Path("Hydra"));
            AsteroidsAssets.SaveMesh(SpaceModels.GateCore(), Path("GateCore"));
            AsteroidsAssets.SaveMesh(SpaceModels.GateRing(3.3f, 2, Swatch.GlowYellow), Path("GateRing"));
            AsteroidsAssets.SaveMesh(SpaceModels.GateRing(2.25f, 1, Swatch.GlowCyan), Path("GateRingInner"));
        }


        // ------------------------------------------------------------------ sounds

        private static void BuildFieldSounds()
        {
            foreach (string name in SpaceSounds.FieldEffectNames)
            {
                AsteroidsAssets.SaveAudio(SpaceSounds.FieldEffect(name), $"{FieldArt}/Audio/{name}.wav", SpaceSounds.IsFieldLoop(name));
            }
            AsteroidsAssets.SaveAudio(SpaceSounds.FrontierMusic(), $"{FieldArt}/Audio/FrontierMusic.wav", true);
            AsteroidsAssets.SaveAudio(SpaceSounds.GateMusic(), $"{FieldArt}/Audio/GateMusic.wav", true);
        }
    }
}
