using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Portfolio.Asteroids.EditorTools
{
    /// <summary>
    /// The strike art (design 4.3), all under Art/Strike: decal, rotor and beam textures, the icons of every strike item
    /// and of the Supply Room, the enemy air force's hull paints (the StarSparrow pack's textures recoloured, with red
    /// canopy glow), the tinted palettes of the procedural models, the glows of the shots, the drop shadow material, the
    /// procedural models (gunship, boss hulls and weapon parts, money pickups), and the strike sounds and music.
    /// Everything is deterministic, so a rebuild writes nothing that did not change.
    /// </summary>
    internal static partial class AsteroidsArtBuilder
    {
        /// <summary>The folder of the strike art.</summary>
        public const string StrikeArt = "Art/Strike";

        private static Shader StrikeShadowShader => AssetDatabase.LoadAssetAtPath<Shader>(AsteroidsAssets.Path($"{StrikeArt}/Shaders/StrikeShadow.shader"));

        /// <summary>
        /// The enemy hull paints: name, the pack material they start from (under Art/StarSparrow/Materials: its maps and
        /// keywords), a tint of its colour, and the glow of the canopies (the pack's emission mask, red instead of blue).
        /// </summary>
        private static readonly (string name, string source, Color tint, Color glow)[] AirPaints =
        {
            ("AirCrimson", "StarSparrow Red", new Color(0.95f, 0.85f, 0.82f), new Color(2.2f, 0.35f, 0.25f)),
            ("AirRust", "StarSparrow Orange", new Color(0.92f, 0.82f, 0.75f), new Color(2.2f, 0.6f, 0.2f)),
            ("AirSteel", "StarSparrow Black", new Color(1f, 0.6f, 0.55f), new Color(2.4f, 0.3f, 0.25f)),
            ("AirSand", "Unified/StarSparrow Grey", new Color(0.95f, 0.84f, 0.66f), new Color(2.2f, 0.45f, 0.25f)),
            ("AirOlive", "StarSparrow Green", new Color(0.75f, 0.8f, 0.6f), new Color(2.2f, 0.6f, 0.2f)),
            ("AirViolet", "StarSparrow Purple", new Color(0.85f, 0.75f, 0.9f), new Color(2.2f, 0.3f, 1.3f)),
            ("AirBlack", "StarSparrow Black", new Color(0.8f, 0.78f, 0.85f), new Color(2.4f, 0.2f, 0.6f))
        };

        /// <summary>The tinted palettes of the procedural strike models: name, tint of the base colour.</summary>
        private static readonly (string name, Color tint)[] PaletteTints =
        {
            ("StrikePalette", Color.white), ("StrikeSand", new Color(1f, 0.88f, 0.68f)), ("StrikeOlive", new Color(0.78f, 0.86f, 0.6f)),
            ("StrikeBasalt", new Color(0.7f, 0.6f, 0.56f)), ("StrikeNight", new Color(0.7f, 0.75f, 0.95f)), ("StrikeCrimson", new Color(1f, 0.72f, 0.7f))
        };


        static partial void BuildStrikeArt()
        {
            BuildStrikeTextures();
            BuildStrikeIcons();
            BuildStrikeMaterials();
            BuildStrikeMeshes();
            BuildStrikeSounds();
        }


        // ------------------------------------------------------------------ accessors

        /// <summary>The sprite of a strike item's icon (Art/Strike/Icons/{item}.png): Supply Room cards, HUD, pickups.</summary>
        public static Sprite StrikeIcon(StrikeItem item)
        {
            return AsteroidsAssets.Load<Sprite>($"{StrikeArt}/Icons/{item}.png");
        }


        /// <summary>A strike icon by name: a <see cref="StrikeItem"/> name or one of <see cref="SpaceIcons.StrikeIconNames"/>.</summary>
        public static Sprite StrikeIcon(string name)
        {
            return AsteroidsAssets.Load<Sprite>($"{StrikeArt}/Icons/{name}.png");
        }


        public static Material StrikeMaterial(string name)
        {
            return AsteroidsAssets.Load<Material>($"{StrikeArt}/Materials/{name}.mat");
        }


        public static Mesh StrikeModel(string name)
        {
            return AsteroidsAssets.Load<Mesh>($"{StrikeArt}/Models/{name}.asset");
        }


        public static AudioClip StrikeSound(string name)
        {
            return AsteroidsAssets.Load<AudioClip>($"{StrikeArt}/Audio/{name}.wav");
        }


        public static Texture2D StrikeTexture(string name)
        {
            return AsteroidsAssets.Load<Texture2D>($"{StrikeArt}/Textures/{name}.png");
        }


        /// <summary>A texture of the StarSparrow pack (under Art/StarSparrow/Textures).</summary>
        private static Texture2D PackTexture(string relative)
        {
            return AsteroidsAssets.Load<Texture2D>($"{StarSparrow}/Textures/{relative}.png");
        }


        // ------------------------------------------------------------------ textures

        private static Action<TextureImporter> Flat2D(Action<TextureImporter> preset)
        {
            return importer =>
            {
                preset(importer);
                importer.textureShape = TextureImporterShape.Texture2D;
            };
        }


        private static void BuildStrikeTextures()
        {
            AsteroidsAssets.SaveTexture(CraterTexture(), $"{StrikeArt}/Textures/Crater.png", Flat2D(importer =>
            {
                AsteroidsAssets.ParticleTexture(importer);
                importer.mipmapEnabled = true;
            }));
            AsteroidsAssets.SaveTexture(RotorTexture(), $"{StrikeArt}/Textures/RotorBlur.png", Flat2D(AsteroidsAssets.ParticleTexture));
            AsteroidsAssets.SaveTexture(BeamTexture(), $"{StrikeArt}/Textures/Beam.png", Flat2D(importer =>
            {
                AsteroidsAssets.ParticleTexture(importer);
                importer.wrapModeU = TextureWrapMode.Clamp;
                importer.wrapModeV = TextureWrapMode.Repeat;
            }));
            AsteroidsAssets.SaveTexture(PulseTexture(), $"{StrikeArt}/Textures/Pulse.png", Flat2D(AsteroidsAssets.ParticleTexture));
            AsteroidsAssets.SaveTexture(TracerTexture(), $"{StrikeArt}/Textures/Tracer.png", Flat2D(AsteroidsAssets.ParticleTexture));
        }


        /// <summary>A scorched crater seen from above: a dark pit, a burnt ring with ragged streaks, soft ragged edges.</summary>
        private static Texture2D CraterTexture()
        {
            const int size = 256;
            return SpaceTextures.Make(size, (x, y) =>
            {
                float u = (x + 0.5f) / size * 2f - 1f;
                float v = (y + 0.5f) / size * 2f - 1f;
                float r = Mathf.Sqrt(u * u + v * v);
                float angle = Mathf.Atan2(v, u);
                float ragged = SpaceTextures.Fbm((angle / (Mathf.PI * 2f)) + 0.5f, r * 0.5f, 6, 3, 911) * 0.3f;
                float streaks = Mathf.Pow(Mathf.Abs(Mathf.Sin(angle * 7f + ragged * 9f)), 6f) * 0.25f;
                float edge = 0.62f + ragged + streaks;
                float alpha = Mathf.Clamp01((edge - r) / 0.18f);
                float pit = Mathf.Clamp01(1f - r / 0.38f);
                float rim = Mathf.Clamp01(1f - Mathf.Abs(r - 0.42f) / 0.1f);
                float grain = SpaceTextures.Hash(x, y, 77) * 0.12f;
                float dark = 0.08f + grain + rim * 0.18f - pit * 0.06f;
                var color = new Color(dark * 1.05f, dark * 0.95f, dark * 0.85f, alpha * (0.85f + pit * 0.15f));
                if (pit > 0.6f && SpaceTextures.Hash(x / 3, y / 3, 5) > 0.93f)
                {
                    color = new Color(0.9f, 0.4f, 0.1f, color.a);
                }
                return color;
            });
        }


        /// <summary>A spinning rotor as the eye sees it: a faint disc with two blurred blades and a hub.</summary>
        private static Texture2D RotorTexture()
        {
            const int size = 256;
            return SpaceTextures.Make(size, (x, y) =>
            {
                float u = (x + 0.5f) / size * 2f - 1f;
                float v = (y + 0.5f) / size * 2f - 1f;
                float r = Mathf.Sqrt(u * u + v * v);
                if (r > 0.99f)
                {
                    return new Color(0f, 0f, 0f, 0f);
                }
                float angle = Mathf.Atan2(v, u);
                float blade = Mathf.Pow(Mathf.Abs(Mathf.Cos(angle)), 60f);
                float trail = Mathf.Pow(Mathf.Clamp01(Mathf.Cos(angle * 2f + 0.5f) * 0.5f + 0.5f), 6f) * 0.25f;
                float disc = 0.1f + 0.06f * Mathf.Clamp01(1f - Mathf.Abs(r - 0.9f) / 0.05f);
                float hub = Mathf.Clamp01((0.12f - r) / 0.02f);
                float alpha = Mathf.Clamp01(disc + blade * 0.8f + trail) * Mathf.Clamp01((0.99f - r) / 0.03f);
                alpha = Mathf.Max(alpha, hub);
                return new Color(0.08f, 0.08f, 0.1f, alpha);
            });
        }


        /// <summary>The cross-section of a beam (a white-hot core, a soft coloured edge), repeating along its length.</summary>
        private static Texture2D BeamTexture()
        {
            const int width = 64;
            const int height = 128;
            return SpaceTextures.Make(width, height, (x, y) =>
            {
                float u = Mathf.Abs((x + 0.5f) / width * 2f - 1f);
                float v = (y + 0.5f) / height;
                float ripple = 0.85f + 0.15f * Mathf.Sin(v * Mathf.PI * 2f * 4f);
                float core = Mathf.Clamp01(1f - u / 0.22f);
                float glow = Mathf.Pow(Mathf.Clamp01(1f - u), 2.2f) * ripple;
                float white = core * core;
                return new Color(Mathf.Lerp(1f, 1f, white), Mathf.Lerp(1f, 1f, white), 1f, Mathf.Clamp01(glow + white));
            });
        }


        /// <summary>The pulse cannon's shockwave: a bright crescent bowed toward the top.</summary>
        private static Texture2D PulseTexture()
        {
            const int size = 128;
            return SpaceTextures.Make(size, (x, y) =>
            {
                float u = (x + 0.5f) / size * 2f - 1f;
                float v = (y + 0.5f) / size * 2f - 1f;
                float r = Mathf.Sqrt(u * u + (v + 0.9f) * (v + 0.9f));
                float band = Mathf.Clamp01(1f - Mathf.Abs(r - 1.3f) / 0.22f);
                float sides = Mathf.Clamp01(1f - Mathf.Abs(u) / 1f);
                float a = band * band * sides;
                return new Color(1f, 1f, 1f, a);
            });
        }


        /// <summary>A tracer round: a short bright streak with a hot head.</summary>
        private static Texture2D TracerTexture()
        {
            const int size = 64;
            return SpaceTextures.Make(size, (x, y) =>
            {
                float u = Mathf.Abs((x + 0.5f) / size * 2f - 1f);
                float v = (y + 0.5f) / size;
                float across = Mathf.Clamp01(1f - u / 0.35f);
                float along = Mathf.Clamp01(v * 1.2f) * Mathf.Clamp01((1f - v) / 0.15f);
                float head = Mathf.Clamp01(1f - Mathf.Abs(v - 0.8f) / 0.15f) * Mathf.Clamp01(1f - u / 0.5f);
                return new Color(1f, 1f, 1f, Mathf.Clamp01(across * across * along + head * 0.8f));
            });
        }


        // ------------------------------------------------------------------ icons

        private static void BuildStrikeIcons()
        {
            Action<TextureImporter> sprite = Flat2D(AsteroidsAssets.SpriteTexture(Vector4.zero));
            foreach (StrikeItem item in (StrikeItem[])Enum.GetValues(typeof(StrikeItem)))
            {
                AsteroidsAssets.SaveTexture(SpaceIcons.StrikeItemIcon(item), $"{StrikeArt}/Icons/{item}.png", sprite);
            }
            foreach (string name in SpaceIcons.StrikeIconNames)
            {
                AsteroidsAssets.SaveTexture(SpaceIcons.StrikeIcon(name), $"{StrikeArt}/Icons/{name}.png", sprite);
            }
        }


        // ------------------------------------------------------------------ materials

        /// <summary>A Glow material under Art/Strike/Materials (additive, or alpha blended).</summary>
        public static Material StrikeGlowMaterial(string name, Texture texture, Color color, bool additive)
        {
            return StrikeGlow(name, texture, color, additive);
        }


        private static Material StrikeGlow(string name, Texture texture, Color color, bool additive)
        {
            return AsteroidsAssets.SaveMaterial($"{StrikeArt}/Materials/{name}.mat", Glow, m =>
            {
                m.SetTexture("_BaseMap", texture);
                m.SetColor("_BaseColor", color);
                m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                m.SetFloat("_DstBlend", additive ? (float)BlendMode.One : (float)BlendMode.OneMinusSrcAlpha);
                m.SetFloat("_Cull", (float)CullMode.Off);
                m.renderQueue = (int)RenderQueue.Transparent;
            });
        }


        private static void BuildStrikeMaterials()
        {
            // Copies of the pack's own URP materials (the way the hangar hulls look in the game), recoloured.
            foreach ((string name, string source, Color tint, Color glow) in AirPaints)
            {
                Material pack = PackMaterial(source);
                AsteroidsAssets.SaveMaterial($"{StrikeArt}/Materials/{name}.mat", Lit, m =>
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
            Texture2D palette = Texture("Palette");
            Texture2D paletteEmission = Texture("PaletteEmission");
            foreach ((string name, Color tint) in PaletteTints)
            {
                AsteroidsAssets.SaveMaterial($"{StrikeArt}/Materials/{name}.mat", Lit, m => SetupLit(m, tint, palette, null, 0.4f, 0.3f, paletteEmission, new Color(2.2f, 2.2f, 2.2f)));
            }

            AsteroidsAssets.SaveMaterial($"{StrikeArt}/Materials/Shadow.mat", StrikeShadowShader, m =>
            {
                m.SetColor("_BaseColor", new Color(0f, 0f, 0f, 0.45f));
                m.SetFloat("_StencilBit", 64f);
                m.renderQueue = (int)RenderQueue.Transparent - 50;
            });

            Texture2D glowTexture = Texture("Glow");
            Texture2D plasma = Texture("Plasma");
            Texture2D bolt = Texture("Bolt");
            Texture2D square = Texture("Square");
            Texture2D beam = StrikeTexture("Beam");
            Texture2D tracer = StrikeTexture("Tracer");
            // Alpha-blended (not additive), so the machine gun's tracers stay visible over bright sand and snow.
            StrikeGlow("Bullet", tracer, new Color(2.6f, 1.45f, 0.3f, 1f), false);
            StrikeGlow("MiniGunRound", tracer, new Color(2.8f, 1.1f, 0.3f, 1f), false);
            StrikeGlow("PlasmaBolt", plasma, new Color(2.4f, 0.8f, 3.2f, 1f), true);
            StrikeGlow("Pulse", StrikeTexture("Pulse"), new Color(0.6f, 3f, 2.6f, 1f), true);
            StrikeGlow("DisrupterOrb", plasma, new Color(1.6f, 0.8f, 3f, 1f), true);
            StrikeGlow("MissileFlame", glowTexture, new Color(3f, 1.4f, 0.4f, 0.9f), true);
            StrikeGlow("EnemyFlak", plasma, new Color(3.2f, 1.3f, 0.3f, 1f), true);
            StrikeGlow("EnemyBolt", bolt, new Color(3.2f, 0.45f, 0.35f, 1f), true);
            StrikeGlow("EnemyRocketFlame", glowTexture, new Color(3f, 0.7f, 0.25f, 0.9f), true);
            StrikeGlow("EnemyMineGlow", glowTexture, new Color(3f, 0.35f, 0.3f, 0.7f), true);
            StrikeGlow("EnemyBeam", beam, new Color(3.5f, 0.45f, 0.4f, 1f), true);
            StrikeGlow("EnemyBeamTelegraph", beam, new Color(2f, 0.3f, 0.3f, 0.35f), true);
            StrikeGlow("PlayerBeam", beam, new Color(1.2f, 2.6f, 3.6f, 1f), true);
            StrikeGlow("PlayerBeamTwin", beam, new Color(3.4f, 1f, 3.4f, 1f), true);
            StrikeGlow("PlayerZap", beam, new Color(1.5f, 3.4f, 3.6f, 1f), true);
            StrikeGlow("MegabombFlash", square, new Color(2f, 2f, 2f, 1f), true);
            StrikeGlow("EngineRed", Texture("Flame"), new Color(3f, 0.9f, 0.4f, 1f), true);
            StrikeGlow("Crater", StrikeTexture("Crater"), Color.white, false);
            StrikeGlow("Rotor", StrikeTexture("RotorBlur"), Color.white, false);

            // The icons shown inside the pickups' frames (alpha blended, like the field's Icon materials).
            foreach (StrikeItem item in (StrikeItem[])Enum.GetValues(typeof(StrikeItem)))
            {
                StrikeGlow($"Icon{item}", AsteroidsAssets.Load<Texture2D>($"{StrikeArt}/Icons/{item}.png"), Color.white, false);
            }
            foreach (string name in SpaceIcons.StrikeIconNames)
            {
                StrikeGlow($"Icon{name}", AsteroidsAssets.Load<Texture2D>($"{StrikeArt}/Icons/{name}.png"), Color.white, false);
            }
        }


        // ------------------------------------------------------------------ meshes

        private static void BuildStrikeMeshes()
        {
            string Path(string name) => $"{StrikeArt}/Models/{name}.asset";
            AsteroidsAssets.SaveMesh(SpaceModels.GunshipBody(), Path("Gunship"));
            AsteroidsAssets.SaveMesh(SpaceModels.RotorBlades(4, 2.1f), Path("Rotor"));
            AsteroidsAssets.SaveMesh(SpaceModels.RotorBlades(3, 2.8f), Path("RotorLarge"));
            AsteroidsAssets.SaveMesh(SpaceModels.CrawlerHull(false), Path("CrawlerHull"));
            AsteroidsAssets.SaveMesh(SpaceModels.CrawlerHull(true), Path("FoundryHull"));
            AsteroidsAssets.SaveMesh(SpaceModels.TurretBase(0.85f), Path("TurretBase"));
            AsteroidsAssets.SaveMesh(SpaceModels.TurretBase(1.35f), Path("TurretBaseLarge"));
            AsteroidsAssets.SaveMesh(SpaceModels.TurretHead(2, 1.3f, 0.26f, Swatch.HullDark), Path("TwinTurret"));
            AsteroidsAssets.SaveMesh(SpaceModels.TurretHead(4, 1f, 0.36f, Swatch.DarkGreen), Path("FlakTurret"));
            AsteroidsAssets.SaveMesh(SpaceModels.TurretHead(1, 2.8f, 0f, Swatch.Gunmetal, 1.7f), Path("MainCannon"));
            AsteroidsAssets.SaveMesh(SpaceModels.TurretHead(2, 1.1f, 0.3f, Swatch.DarkRed), Path("SideCannon"));
            AsteroidsAssets.SaveMesh(SpaceModels.LauncherHead(), Path("Launcher"));
            AsteroidsAssets.SaveMesh(SpaceModels.LaserEmitter(1.5f, Swatch.GlowRed), Path("LaserCore"));
            AsteroidsAssets.SaveMesh(SpaceModels.LaserEmitter(0.9f, Swatch.GlowMagenta), Path("LaserEmitter"));
            AsteroidsAssets.SaveMesh(SpaceModels.PumpStation(), Path("PumpStation"));
            AsteroidsAssets.SaveMesh(SpaceModels.ReactorCore(1.7f, Swatch.GlowGreen), Path("Reactor"));
            AsteroidsAssets.SaveMesh(SpaceModels.ReactorCore(2.3f, Swatch.GlowOrange), Path("Dome"));
            AsteroidsAssets.SaveMesh(SpaceModels.RigPlatform(), Path("RigPlatform"));
            AsteroidsAssets.SaveMesh(SpaceModels.SiloPad(), Path("SiloPad"));
            AsteroidsAssets.SaveMesh(SpaceModels.Silo(1.5f), Path("Silo"));
            AsteroidsAssets.SaveMesh(SpaceModels.Bastion(), Path("Bastion"));
            AsteroidsAssets.SaveMesh(SpaceModels.FlameVent(), Path("FlameVent"));
            AsteroidsAssets.SaveMesh(SpaceModels.StationCore(2.6f), Path("StationCore"));
            AsteroidsAssets.SaveMesh(SpaceModels.StationRing(6.2f, 6, Swatch.GlowRed), Path("StationOuterRing"));
            AsteroidsAssets.SaveMesh(SpaceModels.StationRing(3.9f, 4, Swatch.GlowMagenta), Path("StationInnerRing"));
            AsteroidsAssets.SaveMesh(SpaceModels.ModulePod(Swatch.HullDark, Swatch.GlowOrange), Path("ModulePod"));
            AsteroidsAssets.SaveMesh(SpaceModels.ModulePod(Swatch.DarkRed, Swatch.GlowRed), Path("ModulePodRed"));
            AsteroidsAssets.SaveMesh(SpaceModels.AmmoCrate(), Path("AmmoCrate"));
            AsteroidsAssets.SaveMesh(SpaceModels.IsotopeCanister(), Path("IsotopeCanister"));
            AsteroidsAssets.SaveMesh(SpaceModels.ThaeliteCluster(), Path("ThaeliteCluster"));
            AsteroidsAssets.SaveMesh(SpaceModels.FusionCore(), Path("FusionCore"));
            AsteroidsAssets.SaveMesh(SpaceModels.FreyliumOre(), Path("FreyliumOre"));
            AsteroidsAssets.SaveMesh(SpaceModels.CreditCoin(), Path("CreditCoin"));
        }


        // ------------------------------------------------------------------ sounds

        private static void BuildStrikeSounds()
        {
            foreach (string name in SpaceSounds.StrikeEffectNames)
            {
                AsteroidsAssets.SaveAudio(SpaceSounds.StrikeEffect(name), $"{StrikeArt}/Audio/{name}.wav", SpaceSounds.IsStrikeLoop(name));
            }
            AsteroidsAssets.SaveAudio(SpaceSounds.StrikeMusic(), $"{StrikeArt}/Audio/StrikeMusic.wav", true);
            AsteroidsAssets.SaveAudio(SpaceSounds.StrikeBossMusic(), $"{StrikeArt}/Audio/StrikeBossMusic.wav", true);
        }
    }
}
