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
    /// sprites, card and interface sounds, jingles, the Kenney Future font), then draws the art of every theme of
    /// <see cref="ThemeSpecs"/> with <see cref="MemoryCardsArt"/> (cards, special faces, icons, panels, particles,
    /// backdrop shapes; for After Dark also the interface kit and the deck of <see cref="AfterDarkFaces"/>), bakes the
    /// fonts of every theme into TextMesh Pro font assets, and synthesises the music and the remaining effects with
    /// <see cref="SoundFactory"/>. The original art keeps its folders under Art/; a theme's own art goes under its
    /// <see cref="ThemeSpec.ArtRoot"/>.
    /// </summary>
    internal static class MemoryCardsArtBuilder
    {
        public const string AnimalsFolder = "Art/Animals";
        public const string KenneyUiFolder = "Art/Kenney/UI";
        public const string KenneyFontsFolder = "Art/Kenney/Fonts";
        public const string KenneyAudioFolder = "Audio/Kenney";
        public const string GeneratedAudioFolder = "Audio/Generated";
        /// <summary>The folders of the original art (the Classic theme).</summary>
        public const string CardsFolder = "Art/Cards";
        public const string IconsFolder = "Art/Icons";
        public const string UiFolder = "Art/UI";
        public const string ParticlesFolder = "Art/Particles";
        public const string BackdropFolder = "Art/Backdrop";
        public const string FontsFolder = "Art/Fonts";
        public const string FontAssetPath = FontsFolder + "/KenneyFuture SDF.asset";

        public static readonly string[] IconNames =
        {
            "Pause", "Settings", "Power", "Home", "Levels", "Lock", "Heart", "Clock", "Stopwatch", "Moves", "Check", "Cross",
            "Play", "Retry", "Next", "Back", "Infinity", "Sliders", "Flag", "Cards", "Paw", "Star"
        };

        /// <summary>The roles of the buttons of a kit; a round button is "Round" + role.</summary>
        public static readonly string[] KitRoles = { "Primary", "Secondary", "Accent", "Danger", "Neutral" };

        public static readonly string[] ParticleNames = { "Spark", "Star", "Confetti", "Shard", "Circle" };

        /// <summary>The Kenney sprite that plays each part of the kit in the Classic theme.</summary>
        private static readonly Dictionary<string, string> KenneyKit = new Dictionary<string, string>
        {
            { "Primary", "ButtonGreen" }, { "Secondary", "ButtonBlue" }, { "Accent", "ButtonYellow" }, { "Danger", "ButtonRed" }, { "Neutral", "ButtonGrey" },
            { "RoundPrimary", "RoundGreen" }, { "RoundSecondary", "RoundBlue" }, { "RoundAccent", "RoundYellow" }, { "RoundDanger", "RoundRed" }, { "RoundNeutral", "RoundGrey" },
            { "InputField", "InputField" }, { "StarFull", "StarFull" }, { "StarEmpty", "StarEmpty" }, { "GoalDone", "GoalDone" }, { "GoalMissed", "GoalMissed" }
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

        /// <summary>Every character the font assets bake (the interface is English only).</summary>
        private const string FontCharacters =
            " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~";

        public static void BuildAll()
        {
            Step("Animals", 0.02f);
            ConfigureAnimals();
            Step("Interface sprites", 0.05f);
            ConfigureKenneyUi();
            float share = 0.7f / ThemeSpecs.All.Length;
            for (int i = 0; i < ThemeSpecs.All.Length; i++)
            {
                BuildTheme(ThemeSpecs.All[i], 0.1f + i * share, share);
            }
            Step("Launcher icon", 0.82f);
            BuildLauncherIcon();
            Step("Sounds", 0.85f);
            BuildAudio();
            AssetDatabase.SaveAssets();
        }

        /// <summary>Draws the art of one theme and bakes its fonts.</summary>
        public static void BuildTheme(ThemeSpec theme, float from = 0f, float share = 1f)
        {
            Step($"{theme.Name}: cards", from);
            BuildCards(theme);
            BuildSpecialFaces(theme);
            Step($"{theme.Name}: icons and panels", from + share * 0.2f);
            BuildIcons(theme);
            BuildUi(theme);
            BuildParticles(theme);
            BuildBackdrop(theme);
            if (theme.DrawFaces)
            {
                Step($"{theme.Name}: deck", from + share * 0.4f);
                BuildFaces(theme);
            }
            Step($"{theme.Name}: fonts", from + share * 0.9f);
            BuildFonts(theme);
        }

        private static void Step(string what, float progress)
        {
            if (!Application.isBatchMode)
            {
                EditorUtility.DisplayProgressBar("Memory Cards art", what, progress);
            }
        }

        #region Accessors

        /// <summary>A face of the deck of <paramref name="theme"/>.</summary>
        public static Sprite Face(ThemeSpec theme, string name)
        {
            return MemoryCardsAssets.LoadSprite($"{theme.FacesFolder}/{name}.png");
        }

        public static Sprite Card(ThemeSpec theme, string name)
        {
            return MemoryCardsAssets.LoadSprite($"{theme.CardsFolder}/{name}.png");
        }

        public static Sprite Icon(ThemeSpec theme, string name)
        {
            return MemoryCardsAssets.LoadSprite($"{theme.IconsFolder}/{name}.png");
        }

        public static Sprite Ui(ThemeSpec theme, string name)
        {
            return MemoryCardsAssets.LoadSprite($"{theme.UiFolder}/{name}.png");
        }

        /// <summary>
        /// A part of the interface kit of <paramref name="theme"/> by role ("Primary", "RoundAccent", "InputField",
        /// "StarFull", "GoalDone"...): a Kenney sprite for the Classic theme, a drawn one under the theme's UI folder otherwise.
        /// </summary>
        public static Sprite Kit(ThemeSpec theme, string role)
        {
            if (theme.KenneyKit)
            {
                return Kenney(KenneyKit[role]);
            }
            bool button = System.Array.IndexOf(KitRoles, role) >= 0;
            return Ui(theme, button ? $"Button{role}" : role);
        }

        /// <summary>
        /// A white sprite of the kit that the game tints ("Panel", "Pill", "Disc"): Kenney's kit has white panels already,
        /// a drawn kit has tint variants of its own.
        /// </summary>
        public static Sprite Tint(ThemeSpec theme, string part)
        {
            if (theme.KenneyKit)
            {
                return Ui(theme, part == "Panel" ? "PanelDepth" : part);
            }
            return Ui(theme, $"{part}Tint");
        }

        public static Sprite Particle(ThemeSpec theme, string name)
        {
            return MemoryCardsAssets.LoadSprite($"{theme.ParticlesFolder}/{name}.png");
        }

        public static Sprite Backdrop(ThemeSpec theme, string name)
        {
            return MemoryCardsAssets.LoadSprite($"{theme.BackdropFolder}/{name}.png");
        }

        public static Texture2D Pattern(ThemeSpec theme)
        {
            return MemoryCardsAssets.Load<Texture2D>($"{theme.BackdropFolder}/Pattern.png");
        }

        public static TMP_FontAsset Font(FontSpec font)
        {
            return MemoryCardsAssets.Load<TMP_FontAsset>(font.AssetPath);
        }

        /// <summary>A material preset of a font: "Outline", "Shadow" or "Title".</summary>
        public static Material FontMaterial(FontSpec font, string preset)
        {
            return MemoryCardsAssets.Load<Material>(font.MaterialPath(preset));
        }

        // The original art, for the campaign's own worlds and the launcher icon.

        public static Sprite Animal(string name)
        {
            return Face(ThemeSpecs.Classic, name);
        }

        public static Sprite Card(string name)
        {
            return Card(ThemeSpecs.Classic, name);
        }

        public static Sprite Icon(string name)
        {
            return Icon(ThemeSpecs.Classic, name);
        }

        public static Sprite Ui(string name)
        {
            return Ui(ThemeSpecs.Classic, name);
        }

        public static Sprite Kenney(string name)
        {
            return MemoryCardsAssets.LoadSprite($"{KenneyUiFolder}/{name}.png");
        }

        public static Sprite Particle(string name)
        {
            return Particle(ThemeSpecs.Classic, name);
        }

        public static Sprite Backdrop(string name)
        {
            return Backdrop(ThemeSpecs.Classic, name);
        }

        public static Texture2D ClassicPattern => Pattern(ThemeSpecs.Classic);

        public static AudioClip KenneySound(string name)
        {
            return MemoryCardsAssets.Load<AudioClip>($"{KenneyAudioFolder}/{name}.ogg");
        }

        public static AudioClip GeneratedSound(string name)
        {
            return MemoryCardsAssets.Load<AudioClip>($"{GeneratedAudioFolder}/{name}.wav");
        }

        public static TMP_FontAsset KenneyFont => Font(ThemeSpecs.KenneyFuture);

        public static Material FontMaterial(string preset)
        {
            return FontMaterial(ThemeSpecs.KenneyFuture, preset);
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

        private static void BuildCards(ThemeSpec theme)
        {
            ArtStyle style = theme.Style;
            var noBorder = MemoryCardsAssets.SpriteTexture(Vector4.zero, true);
            Save(MemoryCardsArt.CardFront(style), $"{theme.CardsFolder}/CardFront.png", noBorder);
            Save(MemoryCardsArt.CardShadow(), $"{theme.CardsFolder}/CardShadow.png", noBorder);
            Save(MemoryCardsArt.CardGlow(), $"{theme.CardsFolder}/CardGlow.png", noBorder);
            Save(MemoryCardsArt.Ice(false, style), $"{theme.CardsFolder}/Ice.png", noBorder);
            Save(MemoryCardsArt.Ice(true, style), $"{theme.CardsFolder}/IceCracked.png", noBorder);
            Save(MemoryCardsArt.Badge(style), $"{theme.CardsFolder}/Badge.png", noBorder);
            foreach (WorldSpec world in theme.Worlds)
            {
                Color color = WorldSpecs.Color(world.Card);
                Color dark = WorldSpecs.Color(world.CardDark);
                Save(MemoryCardsArt.CardBack(color, dark, world.Pattern, world.Emblem, style, true), $"{theme.CardsFolder}/{theme.BackName(world)}.png", noBorder);
                Save(MemoryCardsArt.CardBack(color, dark, world.Pattern, world.Emblem, style, false), $"{theme.CardsFolder}/{theme.BackName(world)}Plain.png", noBorder);
            }
        }

        private static void BuildSpecialFaces(ThemeSpec theme)
        {
            var sprite = MemoryCardsAssets.SpriteTexture(Vector4.zero, true);
            Save(MemoryCardsArt.WildFace(theme.Style), $"{theme.CardsFolder}/Wild.png", sprite);
            Save(MemoryCardsArt.BombFace(theme.Style), $"{theme.CardsFolder}/Bomb.png", sprite);
            Save(MemoryCardsArt.ClockFace(theme.Style), $"{theme.CardsFolder}/Clock.png", sprite);
            Save(MemoryCardsArt.PeekFace(theme.Style), $"{theme.CardsFolder}/Peek.png", sprite);
        }

        private static void BuildIcons(ThemeSpec theme)
        {
            var sprite = MemoryCardsAssets.SpriteTexture(Vector4.zero, true);
            foreach (string icon in IconNames)
            {
                Save(MemoryCardsArt.Icon(icon, theme.Style), $"{theme.IconsFolder}/{icon}.png", sprite);
            }
            Save(MemoryCardsArt.HeartIcon(true, theme.Style), $"{theme.IconsFolder}/HeartFull.png", sprite);
            Save(MemoryCardsArt.HeartIcon(false, theme.Style), $"{theme.IconsFolder}/HeartEmpty.png", sprite);
        }

        /// <summary>The panels and pills of a theme, and the whole button kit of a theme that does not use Kenney's.</summary>
        private static void BuildUi(ThemeSpec theme)
        {
            ArtStyle style = theme.Style;
            Save(MemoryCardsArt.Panel(128, 30f, style), $"{theme.UiFolder}/Panel.png", MemoryCardsAssets.SpriteTexture(new Vector4(40f, 40f, 40f, 40f)));
            Save(MemoryCardsArt.PanelDepth(128, 30f, 10f, style), $"{theme.UiFolder}/PanelDepth.png", MemoryCardsAssets.SpriteTexture(new Vector4(40f, 50f, 40f, 40f)));
            Save(MemoryCardsArt.Pill(128, 64, style), $"{theme.UiFolder}/Pill.png", MemoryCardsAssets.SpriteTexture(new Vector4(32f, 32f, 32f, 32f)));
            Save(MemoryCardsArt.Disc(128, style), $"{theme.UiFolder}/Disc.png", MemoryCardsAssets.SpriteTexture());
            Save(MemoryCardsArt.Soft(128), $"{theme.UiFolder}/Soft.png", MemoryCardsAssets.SpriteTexture());
            if (theme.KenneyKit)
            {
                return;
            }
            var noBorder = MemoryCardsAssets.SpriteTexture(Vector4.zero, true);
            var buttonBorder = MemoryCardsAssets.SpriteTexture(new Vector4(40f, 50f, 40f, 40f));
            foreach (string role in KitRoles)
            {
                (Color face, Color lip) = KitColors(style, role);
                Save(MemoryCardsArt.KitButton(face, lip, style), $"{theme.UiFolder}/Button{role}.png", buttonBorder);
                Save(MemoryCardsArt.KitRound(face, lip, style), $"{theme.UiFolder}/Round{role}.png", noBorder);
            }
            Save(MemoryCardsArt.InputField(style), $"{theme.UiFolder}/InputField.png", MemoryCardsAssets.SpriteTexture(new Vector4(22f, 22f, 22f, 22f)));
            Save(MemoryCardsArt.StarSprite(true, style), $"{theme.UiFolder}/StarFull.png", noBorder);
            Save(MemoryCardsArt.StarSprite(false, style), $"{theme.UiFolder}/StarEmpty.png", noBorder);
            Save(MemoryCardsArt.GoalMark(true, style), $"{theme.UiFolder}/GoalDone.png", noBorder);
            Save(MemoryCardsArt.GoalMark(false, style), $"{theme.UiFolder}/GoalMissed.png", noBorder);
            Save(MemoryCardsArt.Ornament(), $"{theme.UiFolder}/Ornament.png", noBorder);
            Save(MemoryCardsArt.TintPanel(style), $"{theme.UiFolder}/PanelTint.png", MemoryCardsAssets.SpriteTexture(new Vector4(40f, 50f, 40f, 40f)));
            Save(MemoryCardsArt.TintPill(style), $"{theme.UiFolder}/PillTint.png", MemoryCardsAssets.SpriteTexture(new Vector4(32f, 32f, 32f, 32f)));
            Save(MemoryCardsArt.TintDisc(style), $"{theme.UiFolder}/DiscTint.png", MemoryCardsAssets.SpriteTexture());
        }

        private static (Color, Color) KitColors(ArtStyle style, string role)
        {
            switch (role)
            {
                case "Primary": return (style.Primary, style.PrimaryLip);
                case "Secondary": return (style.Secondary, style.SecondaryLip);
                case "Accent": return (style.Accent, style.AccentLip);
                case "Danger": return (style.Danger, style.DangerLip);
                default: return (style.Neutral, style.NeutralLip);
            }
        }

        private static void BuildParticles(ThemeSpec theme)
        {
            foreach (string name in ParticleNames)
            {
                Save(MemoryCardsArt.Particle(name), $"{theme.ParticlesFolder}/{name}.png", MemoryCardsAssets.SpriteTexture());
            }
        }

        private static void BuildBackdrop(ThemeSpec theme)
        {
            var sprite = MemoryCardsAssets.SpriteTexture(Vector4.zero, true);
            foreach (string name in theme.Ambients)
            {
                Save(MemoryCardsArt.Ambient(name), $"{theme.BackdropFolder}/{name}.png", sprite);
            }
            Save(MemoryCardsArt.Pattern(theme.Style.Deco), $"{theme.BackdropFolder}/Pattern.png", MemoryCardsAssets.TileTexture);
        }

        /// <summary>The drawn deck of a theme (After Dark's keepsakes).</summary>
        private static void BuildFaces(ThemeSpec theme)
        {
            var sprite = MemoryCardsAssets.SpriteTexture(Vector4.zero, true);
            foreach (string name in theme.Faces)
            {
                Save(AfterDarkFaces.Draw(name), $"{theme.FacesFolder}/{name}.png", sprite);
            }
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

        #region Fonts

        /// <summary>Bakes the body font of a theme and, when it differs, its title font.</summary>
        private static void BuildFonts(ThemeSpec theme)
        {
            BuildFont(theme.BodyFont);
            if (theme.TitleFont != theme.BodyFont)
            {
                BuildFont(theme.TitleFont);
            }
        }

        /// <summary>
        /// Bakes a TrueType file into a static TextMesh Pro font asset (atlas and material as sub-assets) and creates
        /// the material presets: an outlined one for labels and the HUD, one with a soft shadow for body text, and the
        /// heavy title outline. The font asset is only created when it is missing, so rebuilding keeps its GUID and bytes.
        /// </summary>
        private static void BuildFont(FontSpec spec)
        {
            TMP_FontAsset fontAsset = Font(spec);
            if (fontAsset == null)
            {
                var source = MemoryCardsAssets.Require<UnityEngine.Font>(spec.Source);
                fontAsset = TMP_FontAsset.CreateFontAsset(source, 72, 9, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, false);
                fontAsset.name = spec.Name;
                fontAsset.TryAddCharacters(FontCharacters, out string missing);
                if (!string.IsNullOrEmpty(missing))
                {
                    Debug.Log($"Memory Cards: {spec.Name} has no glyphs for \"{missing}\"; the default font fills in.");
                }
                fontAsset.atlasPopulationMode = AtlasPopulationMode.Static;
                MemoryCardsAssets.EnsureFolder(spec.Folder);
                AssetDatabase.CreateAsset(fontAsset, MemoryCardsAssets.Path(spec.AssetPath));
                Texture2D atlas = fontAsset.atlasTexture;
                atlas.name = $"{spec.Name} Atlas";
                AssetDatabase.AddObjectToAsset(atlas, fontAsset);
                Material material = fontAsset.material;
                material.name = $"{spec.Name} Material";
                AssetDatabase.AddObjectToAsset(material, fontAsset);
                EditorUtility.SetDirty(fontAsset);
                AssetDatabase.SaveAssets();
                AssetDatabase.ImportAsset(MemoryCardsAssets.Path(spec.AssetPath));
                fontAsset = Font(spec);
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
            MemoryCardsAssets.SaveMaterial(spec.MaterialPath("Outline"), baseMaterial.shader, m =>
            {
                m.CopyPropertiesFromMaterial(baseMaterial);
                m.EnableKeyword("OUTLINE_ON");
                m.EnableKeyword("UNDERLAY_ON");
                m.SetFloat("_OutlineWidth", 0.22f);
                m.SetColor("_OutlineColor", spec.OutlineColor);
                m.SetFloat("_FaceDilate", 0.18f);
                m.SetColor("_UnderlayColor", spec.OutlineShadow);
                m.SetFloat("_UnderlayOffsetX", 0.35f);
                m.SetFloat("_UnderlayOffsetY", -0.6f);
                m.SetFloat("_UnderlayDilate", 0.2f);
                m.SetFloat("_UnderlaySoftness", 0.1f);
            });
            MemoryCardsAssets.SaveMaterial(spec.MaterialPath("Shadow"), baseMaterial.shader, m =>
            {
                m.CopyPropertiesFromMaterial(baseMaterial);
                m.EnableKeyword("UNDERLAY_ON");
                m.SetColor("_UnderlayColor", spec.SoftShadow);
                m.SetFloat("_UnderlayOffsetX", 0.2f);
                m.SetFloat("_UnderlayOffsetY", -0.45f);
                m.SetFloat("_UnderlaySoftness", 0.25f);
                m.SetFloat("_FaceDilate", 0.05f);
            });
            MemoryCardsAssets.SaveMaterial(spec.MaterialPath("Title"), baseMaterial.shader, m =>
            {
                m.CopyPropertiesFromMaterial(baseMaterial);
                m.EnableKeyword("OUTLINE_ON");
                m.EnableKeyword("UNDERLAY_ON");
                m.SetFloat("_OutlineWidth", 0.3f);
                m.SetColor("_OutlineColor", spec.TitleOutline);
                m.SetFloat("_FaceDilate", 0.28f);
                m.SetColor("_UnderlayColor", spec.TitleShadow);
                m.SetFloat("_UnderlayOffsetX", 0.5f);
                m.SetFloat("_UnderlayOffsetY", -0.9f);
                m.SetFloat("_UnderlayDilate", 0.3f);
                m.SetFloat("_UnderlaySoftness", 0.05f);
            });
        }

        #endregion
    }
}
