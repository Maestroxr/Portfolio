using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace Portfolio.MemoryCards.EditorTools
{
    /// <summary>
    /// Builds the art and sound of Memory Cards: configures the imported Kenney files (animal renders, interface
    /// sprites, card and interface sounds, jingles, the Kenney Future font), draws the generated textures with
    /// <see cref="MemoryCardsArt"/>, synthesises the music and the remaining effects with <see cref="SoundFactory"/> and
    /// creates the TextMesh Pro font asset.
    /// </summary>
    internal static class MemoryCardsArtBuilder
    {
        public const string AnimalsFolder = "Art/Animals";
        public const string KenneyUiFolder = "Art/Kenney/UI";
        public const string KenneyFontsFolder = "Art/Kenney/Fonts";
        public const string KenneyAudioFolder = "Audio/Kenney";
        public const string CardsFolder = "Art/Cards";
        public const string IconsFolder = "Art/Icons";
        public const string UiFolder = "Art/UI";
        public const string ParticlesFolder = "Art/Particles";
        public const string BackdropFolder = "Art/Backdrop";
        public const string GeneratedAudioFolder = "Audio/Generated";
        public const string FontsFolder = "Art/Fonts";
        public const string FontAssetPath = FontsFolder + "/KenneyFuture SDF.asset";

        public static readonly string[] IconNames =
        {
            "Pause", "Settings", "Power", "Home", "Levels", "Lock", "Heart", "Clock", "Stopwatch", "Moves", "Check", "Cross",
            "Play", "Retry", "Next", "Back", "Infinity", "Sliders", "Flag", "Cards", "Paw", "Star"
        };

        /// <summary>Kenney interface sprites and their nine-slice borders (left, bottom, right, top).</summary>
        private static readonly Dictionary<string, Vector4> KenneySprites = new Dictionary<string, Vector4>
        {
            { "ButtonBlue", new Vector4(26f, 34f, 26f, 26f) },
            { "ButtonGreen", new Vector4(26f, 34f, 26f, 26f) },
            { "ButtonRed", new Vector4(26f, 34f, 26f, 26f) },
            { "ButtonYellow", new Vector4(26f, 34f, 26f, 26f) },
            { "ButtonGrey", new Vector4(26f, 34f, 26f, 26f) },
            { "RoundBlue", Vector4.zero },
            { "RoundGreen", Vector4.zero },
            { "RoundRed", Vector4.zero },
            { "RoundYellow", Vector4.zero },
            { "RoundGrey", Vector4.zero },
            { "StarFull", Vector4.zero },
            { "StarEmpty", Vector4.zero },
            { "GoalDone", Vector4.zero },
            { "GoalMissed", Vector4.zero },
            { "InputField", new Vector4(22f, 22f, 22f, 22f) }
        };

        /// <summary>Kenney sounds used by the game; music is synthesised.</summary>
        public static readonly string[] KenneySounds =
        {
            "card-slide-1", "card-slide-2", "card-slide-3", "card-place-1", "card-place-2", "card-place-3", "card-shuffle",
            "card-fan-1", "card-fan-2", "click_002", "select_002", "back_001", "error_004", "tick_002", "maximize_005",
            "maximize_006", "glass_002", "question_002", "question_004", "drop_004", "jingles_STEEL02", "jingles_PIZZI01",
            "jingles_PIZZI02"
        };

        /// <summary>Every character the font asset bakes (the interface is English only).</summary>
        private const string FontCharacters =
            " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~";

        public static void BuildAll()
        {
            Step("Animals", 0.05f);
            ConfigureAnimals();
            Step("Interface sprites", 0.1f);
            ConfigureKenneyUi();
            Step("Cards", 0.2f);
            BuildCards();
            Step("Special cards", 0.35f);
            BuildSpecialFaces();
            Step("Icons", 0.45f);
            BuildIcons();
            Step("Panels", 0.55f);
            BuildUi();
            Step("Particles and backdrop", 0.65f);
            BuildParticles();
            BuildBackdrop();
            BuildLauncherIcon();
            Step("Sounds", 0.75f);
            BuildAudio();
            Step("Font", 0.9f);
            BuildFont();
            AssetDatabase.SaveAssets();
        }

        private static void Step(string what, float progress)
        {
            if (!Application.isBatchMode)
            {
                EditorUtility.DisplayProgressBar("Memory Cards art", what, progress);
            }
        }

        #region Accessors

        public static Sprite Animal(string name)
        {
            return MemoryCardsAssets.LoadSprite($"{AnimalsFolder}/{name}.png");
        }

        public static Sprite Card(string name)
        {
            return MemoryCardsAssets.LoadSprite($"{CardsFolder}/{name}.png");
        }

        public static Sprite Icon(string name)
        {
            return MemoryCardsAssets.LoadSprite($"{IconsFolder}/{name}.png");
        }

        public static Sprite Ui(string name)
        {
            return MemoryCardsAssets.LoadSprite($"{UiFolder}/{name}.png");
        }

        public static Sprite Kenney(string name)
        {
            return MemoryCardsAssets.LoadSprite($"{KenneyUiFolder}/{name}.png");
        }

        public static Sprite Particle(string name)
        {
            return MemoryCardsAssets.LoadSprite($"{ParticlesFolder}/{name}.png");
        }

        public static Sprite Backdrop(string name)
        {
            return MemoryCardsAssets.LoadSprite($"{BackdropFolder}/{name}.png");
        }

        public static Texture2D Pattern => MemoryCardsAssets.Load<Texture2D>($"{BackdropFolder}/Pattern.png");

        public static AudioClip KenneySound(string name)
        {
            return MemoryCardsAssets.Load<AudioClip>($"{KenneyAudioFolder}/{name}.ogg");
        }

        public static AudioClip GeneratedSound(string name)
        {
            return MemoryCardsAssets.Load<AudioClip>($"{GeneratedAudioFolder}/{name}.wav");
        }

        public static TMP_FontAsset Font => MemoryCardsAssets.Load<TMP_FontAsset>(FontAssetPath);

        public static Material FontMaterial(string preset)
        {
            return MemoryCardsAssets.Load<Material>($"{FontsFolder}/KenneyFuture {preset}.mat");
        }

        #endregion

        #region Imported files

        private static void ConfigureAnimals()
        {
            foreach (string animal in WorldSpecs.Animals)
            {
                MemoryCardsAssets.ConfigureTexture($"{AnimalsFolder}/{animal}.png", MemoryCardsAssets.SpriteTexture(Vector4.zero, true));
            }
        }

        private static void ConfigureKenneyUi()
        {
            foreach (KeyValuePair<string, Vector4> sprite in KenneySprites)
            {
                MemoryCardsAssets.ConfigureTexture($"{KenneyUiFolder}/{sprite.Key}.png", MemoryCardsAssets.SpriteTexture(sprite.Value));
            }
        }

        #endregion

        #region Generated textures

        private static void BuildCards()
        {
            var noBorder = MemoryCardsAssets.SpriteTexture(Vector4.zero, true);
            Save(MemoryCardsArt.CardFront(), $"{CardsFolder}/CardFront.png", noBorder);
            Save(MemoryCardsArt.CardShadow(), $"{CardsFolder}/CardShadow.png", noBorder);
            Save(MemoryCardsArt.CardGlow(), $"{CardsFolder}/CardGlow.png", noBorder);
            Save(MemoryCardsArt.Ice(false), $"{CardsFolder}/Ice.png", noBorder);
            Save(MemoryCardsArt.Ice(true), $"{CardsFolder}/IceCracked.png", noBorder);
            Save(MemoryCardsArt.Badge(), $"{CardsFolder}/Badge.png", noBorder);
            foreach (WorldSpec world in WorldSpecs.All)
            {
                Color color = WorldSpecs.Color(world.Card);
                Color dark = WorldSpecs.Color(world.CardDark);
                Save(MemoryCardsArt.CardBack(color, dark, world.Pattern, true), $"{CardsFolder}/Back{world.Id}.png", noBorder);
                Save(MemoryCardsArt.CardBack(color, dark, world.Pattern, false), $"{CardsFolder}/Back{world.Id}Plain.png", noBorder);
            }
        }

        private static void BuildSpecialFaces()
        {
            var sprite = MemoryCardsAssets.SpriteTexture(Vector4.zero, true);
            Save(MemoryCardsArt.WildFace(), $"{CardsFolder}/Wild.png", sprite);
            Save(MemoryCardsArt.BombFace(), $"{CardsFolder}/Bomb.png", sprite);
            Save(MemoryCardsArt.ClockFace(), $"{CardsFolder}/Clock.png", sprite);
            Save(MemoryCardsArt.PeekFace(), $"{CardsFolder}/Peek.png", sprite);
        }

        private static void BuildIcons()
        {
            foreach (string icon in IconNames)
            {
                Save(MemoryCardsArt.Icon(icon), $"{IconsFolder}/{icon}.png", MemoryCardsAssets.SpriteTexture(Vector4.zero, true));
            }
            Save(MemoryCardsArt.HeartIcon(true), $"{IconsFolder}/HeartFull.png", MemoryCardsAssets.SpriteTexture(Vector4.zero, true));
            Save(MemoryCardsArt.HeartIcon(false), $"{IconsFolder}/HeartEmpty.png", MemoryCardsAssets.SpriteTexture(Vector4.zero, true));
        }

        private static void BuildUi()
        {
            Save(MemoryCardsArt.Panel(128, 30f), $"{UiFolder}/Panel.png", MemoryCardsAssets.SpriteTexture(new Vector4(40f, 40f, 40f, 40f)));
            Save(MemoryCardsArt.PanelDepth(128, 30f, 10f), $"{UiFolder}/PanelDepth.png", MemoryCardsAssets.SpriteTexture(new Vector4(40f, 50f, 40f, 40f)));
            Save(MemoryCardsArt.Pill(128, 64), $"{UiFolder}/Pill.png", MemoryCardsAssets.SpriteTexture(new Vector4(32f, 32f, 32f, 32f)));
            Save(MemoryCardsArt.Disc(128), $"{UiFolder}/Disc.png", MemoryCardsAssets.SpriteTexture());
            Save(MemoryCardsArt.Soft(128), $"{UiFolder}/Soft.png", MemoryCardsAssets.SpriteTexture());
        }

        private static void BuildParticles()
        {
            foreach (string name in new[] { "Spark", "Star", "Confetti", "Shard", "Circle" })
            {
                Save(MemoryCardsArt.Particle(name), $"{ParticlesFolder}/{name}.png", MemoryCardsAssets.SpriteTexture());
            }
        }

        private static void BuildBackdrop()
        {
            var sprite = MemoryCardsAssets.SpriteTexture(Vector4.zero, true);
            Save(MemoryCardsArt.Cloud(), $"{BackdropFolder}/Cloud.png", sprite);
            Save(MemoryCardsArt.Leaf(), $"{BackdropFolder}/Leaf.png", sprite);
            Save(MemoryCardsArt.Snowflake(), $"{BackdropFolder}/Snowflake.png", sprite);
            Save(MemoryCardsArt.Balloon(), $"{BackdropFolder}/Balloon.png", sprite);
            Save(MemoryCardsArt.Pattern(), $"{BackdropFolder}/Pattern.png", MemoryCardsAssets.TileTexture);
        }

        private static void BuildLauncherIcon()
        {
            string file = MemoryCardsAssets.FullPath(MemoryCardsAssets.Path($"{AnimalsFolder}/giraffe.png"));
            var animal = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            animal.LoadImage(File.ReadAllBytes(file));
            Color[] pixels = animal.GetPixels();
            int size = animal.width;
            Object.DestroyImmediate(animal);
            WorldSpec farm = WorldSpecs.All[0];
            Canvas2D icon = MemoryCardsArt.LauncherIcon(pixels, size, WorldSpecs.Color(farm.Card), WorldSpecs.Color(farm.CardDark));
            Save(icon, "Art/LauncherIcon.png", MemoryCardsAssets.SpriteTexture(Vector4.zero, true));
        }

        private static void Save(Canvas2D canvas, string relative, System.Action<TextureImporter> configure)
        {
            MemoryCardsAssets.SaveTexture(canvas.ToTexture(), relative, configure);
        }

        #endregion

        #region Audio

        private static void BuildAudio()
        {
            foreach (string sound in KenneySounds)
            {
                MemoryCardsAssets.ConfigureAudio($"{KenneyAudioFolder}/{sound}.ogg", false);
            }
            MemoryCardsAssets.SaveAudio(SoundFactory.Wav(SoundFactory.MenuMusic()), $"{GeneratedAudioFolder}/MenuMusic.wav", true);
            MemoryCardsAssets.SaveAudio(SoundFactory.Wav(SoundFactory.PlayMusic()), $"{GeneratedAudioFolder}/PlayMusic.wav", true);
            MemoryCardsAssets.SaveAudio(SoundFactory.Wav(SoundFactory.Match()), $"{GeneratedAudioFolder}/Match.wav", false);
            MemoryCardsAssets.SaveAudio(SoundFactory.Wav(SoundFactory.Bomb()), $"{GeneratedAudioFolder}/Bomb.wav", false);
            MemoryCardsAssets.SaveAudio(SoundFactory.Wav(SoundFactory.Wild()), $"{GeneratedAudioFolder}/Wild.wav", false);
            MemoryCardsAssets.SaveAudio(SoundFactory.Wav(SoundFactory.Crack()), $"{GeneratedAudioFolder}/Crack.wav", false);
            MemoryCardsAssets.SaveAudio(SoundFactory.Wav(SoundFactory.Heart()), $"{GeneratedAudioFolder}/Heart.wav", false);
        }

        #endregion

        #region Font

        /// <summary>
        /// Bakes Kenney Future into a static TextMesh Pro font asset (atlas and material as sub-assets) and creates the
        /// material presets: an outlined one for titles and the HUD, and one with a soft shadow for body text. The font
        /// asset is only created when it is missing, so rebuilding keeps its GUID and bytes.
        /// </summary>
        private static void BuildFont()
        {
            TMP_FontAsset fontAsset = Font;
            if (fontAsset == null)
            {
                var source = MemoryCardsAssets.Require<UnityEngine.Font>($"{KenneyFontsFolder}/Kenney Future.ttf");
                fontAsset = TMP_FontAsset.CreateFontAsset(source, 72, 9, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, false);
                fontAsset.name = "KenneyFuture SDF";
                fontAsset.TryAddCharacters(FontCharacters, out string missing);
                if (!string.IsNullOrEmpty(missing))
                {
                    Debug.Log($"Memory Cards: Kenney Future has no glyphs for \"{missing}\"; the default font fills in.");
                }
                fontAsset.atlasPopulationMode = AtlasPopulationMode.Static;
                MemoryCardsAssets.EnsureFolder(FontsFolder);
                AssetDatabase.CreateAsset(fontAsset, MemoryCardsAssets.Path(FontAssetPath));
                Texture2D atlas = fontAsset.atlasTexture;
                atlas.name = "KenneyFuture SDF Atlas";
                AssetDatabase.AddObjectToAsset(atlas, fontAsset);
                Material material = fontAsset.material;
                material.name = "KenneyFuture SDF Material";
                AssetDatabase.AddObjectToAsset(material, fontAsset);
                EditorUtility.SetDirty(fontAsset);
                AssetDatabase.SaveAssets();
                AssetDatabase.ImportAsset(MemoryCardsAssets.Path(FontAssetPath));
                fontAsset = Font;
            }
            MemoryCardsAssets.ApplyIfChanged(fontAsset, () =>
            {
                TMP_FontAsset fallback = TMP_Settings.defaultFontAsset;
                if (fallback != null && (fontAsset.fallbackFontAssetTable == null || !fontAsset.fallbackFontAssetTable.Contains(fallback)))
                {
                    fontAsset.fallbackFontAssetTable = new List<TMP_FontAsset> { fallback };
                }
            });

            Material baseMaterial = fontAsset.material;
            MemoryCardsAssets.SaveMaterial($"{FontsFolder}/KenneyFuture Outline.mat", baseMaterial.shader, m =>
            {
                m.CopyPropertiesFromMaterial(baseMaterial);
                m.EnableKeyword("OUTLINE_ON");
                m.EnableKeyword("UNDERLAY_ON");
                m.SetFloat("_OutlineWidth", 0.22f);
                m.SetColor("_OutlineColor", new Color(0.12f, 0.12f, 0.24f, 1f));
                m.SetFloat("_FaceDilate", 0.18f);
                m.SetColor("_UnderlayColor", new Color(0.05f, 0.05f, 0.15f, 0.55f));
                m.SetFloat("_UnderlayOffsetX", 0.35f);
                m.SetFloat("_UnderlayOffsetY", -0.6f);
                m.SetFloat("_UnderlayDilate", 0.2f);
                m.SetFloat("_UnderlaySoftness", 0.1f);
            });
            MemoryCardsAssets.SaveMaterial($"{FontsFolder}/KenneyFuture Shadow.mat", baseMaterial.shader, m =>
            {
                m.CopyPropertiesFromMaterial(baseMaterial);
                m.EnableKeyword("UNDERLAY_ON");
                m.SetColor("_UnderlayColor", new Color(0f, 0f, 0.1f, 0.35f));
                m.SetFloat("_UnderlayOffsetX", 0.2f);
                m.SetFloat("_UnderlayOffsetY", -0.45f);
                m.SetFloat("_UnderlaySoftness", 0.25f);
                m.SetFloat("_FaceDilate", 0.05f);
            });
            MemoryCardsAssets.SaveMaterial($"{FontsFolder}/KenneyFuture Title.mat", baseMaterial.shader, m =>
            {
                m.CopyPropertiesFromMaterial(baseMaterial);
                m.EnableKeyword("OUTLINE_ON");
                m.EnableKeyword("UNDERLAY_ON");
                m.SetFloat("_OutlineWidth", 0.3f);
                m.SetColor("_OutlineColor", new Color(0.2f, 0.1f, 0.35f, 1f));
                m.SetFloat("_FaceDilate", 0.28f);
                m.SetColor("_UnderlayColor", new Color(0.1f, 0.02f, 0.2f, 0.7f));
                m.SetFloat("_UnderlayOffsetX", 0.5f);
                m.SetFloat("_UnderlayOffsetY", -0.9f);
                m.SetFloat("_UnderlayDilate", 0.3f);
                m.SetFloat("_UnderlaySoftness", 0.05f);
            });
        }

        #endregion
    }
}
