using System;
using UnityEngine;
using static Portfolio.MemoryCards.EditorTools.Shapes;

namespace Portfolio.MemoryCards.EditorTools
{
    /// <summary>The emblem in the medallion of a card back.</summary>
    internal enum CardEmblem
    {
        Paw,
        Diamond,
        Lips,
        Mask,
        Star
    }


    /// <summary>The colours and flags the shared drawings of <see cref="MemoryCardsArt"/> take from a theme.</summary>
    internal sealed class ArtStyle
    {
        /// <summary>Art deco frames: gold corner ornaments on the cards and gold rims on the panels.</summary>
        public bool Deco;
        public Color Gold = Hex("#D9B14C");
        public Color GoldDark = Hex("#8F6F1F");
        public Color GoldLight = Hex("#F3DB8C");

        // Card fronts and backs.
        public Color FrontTop = Hex("#FFFEFA");
        public Color FrontBottom = Hex("#F4EEE2");
        public Color FrontInner = Hex("#E9E0CF");
        public Color FrontOuter = Hex("#D8CDB8");
        /// <summary>The printed pattern on a card back.</summary>
        public Color BackInk = new Color(1f, 1f, 1f, 0.2f);
        public Color BackInner = new Color(1f, 1f, 1f, 0.55f);
        /// <summary>The outer border of a back; the world's dark colour when clear.</summary>
        public Color BackOuter = Color.clear;
        public Color MedallionFill = Color.white;
        /// <summary>The ring of the medallion; blended from the world's colours when clear.</summary>
        public Color MedallionRing = Color.clear;
        /// <summary>The emblem in the medallion; the world's colour when clear.</summary>
        public Color Emblem = Color.clear;

        // Ice and badge.
        public Color IceTop = new Color(0.86f, 0.97f, 1f);
        public Color IceBottom = new Color(0.62f, 0.86f, 1f);
        public Color IceCrack = new Color(0.2f, 0.45f, 0.65f, 0.85f);
        public Color BadgeRing = Color.white;
        public Color BadgeTop = Hex("#4CD06C");
        public Color BadgeBottom = Hex("#2FA94E");
        public Color BadgeMark = Color.white;

        // Special faces: the disc behind each, the shade of its lower part, and the ring around it (no ring when clear).
        public Color WildDisc = Hex("#EFE6FF");
        public Color WildDiscLow = Hex("#E2D3FF");
        public Color BombDisc = Hex("#FFE2DC");
        public Color BombDiscLow = Hex("#FFD2C8");
        public Color ClockDisc = Hex("#DDF7E3");
        public Color ClockDiscLow = Hex("#CBF0D4");
        public Color PeekDisc = Hex("#EDE3FF");
        public Color PeekDiscLow = Hex("#E2D5FF");
        public Color DiscRing = Color.clear;
        /// <summary>The wild star is a rainbow when clear, else a gradient of this and the light gold.</summary>
        public Color WildStar = Color.clear;
        public Color ClockBody = Hex("#34B25A");
        public Color ClockHands = Hex("#2A2C3A");
        public Color PeekLid = Hex("#8B5CF6");
        public Color PeekIris = Hex("#3F7BF7");
        public Color PeekIrisLight = Hex("#62C3FF");

        // Hearts.
        public Color HeartRim = Hex("#C81E45");
        public Color HeartTop = Hex("#FF6F8C");
        public Color HeartBottom = Hex("#E8335A");
        public Color HeartEmpty = new Color(0.2f, 0.22f, 0.32f, 0.55f);

        // Panels and pills (the drawn kit; Kenney's buttons are imported).
        public Color PanelFill = Color.white;
        public Color PanelLip = Hex("#C9CEDB");
        /// <summary>The rim of the panels and pills; none when clear.</summary>
        public Color PanelRim = Color.clear;

        // The buttons of the drawn kit, by role: the face and the lip below it.
        public Color Primary = Hex("#5CB85C");
        public Color PrimaryLip = Hex("#3E8E41");
        public Color Secondary = Hex("#3D8BFF");
        public Color SecondaryLip = Hex("#2A5FB0");
        public Color Accent = Hex("#FFC933");
        public Color AccentLip = Hex("#C9961A");
        public Color Danger = Hex("#E0453F");
        public Color DangerLip = Hex("#A52A26");
        public Color Neutral = Hex("#B8BCC9");
        public Color NeutralLip = Hex("#8A8F9E");
        public Color InputFill = Hex("#F4F1EA");
        public Color StarFill = Hex("#FFC933");
        public Color StarRim = Hex("#B07A12");
        public Color StarEmpty = new Color(0.55f, 0.55f, 0.62f, 0.45f);

        /// <summary>The look of the original game: Kenney's rounded pastel style.</summary>
        public static readonly ArtStyle Classic = new ArtStyle();

        /// <summary>Black, gold and deep red with art deco frames.</summary>
        public static readonly ArtStyle AfterDark = new ArtStyle
        {
            Deco = true,
            FrontTop = Hex("#241826"),
            FrontBottom = Hex("#150E17"),
            FrontInner = Hex("#D9B14C"),
            FrontOuter = Hex("#8F6F1F"),
            BackInk = new Color(0.85f, 0.7f, 0.3f, 0.22f),
            BackInner = new Color(0.85f, 0.7f, 0.3f, 0.6f),
            BackOuter = Hex("#D9B14C"),
            MedallionFill = Hex("#160E18"),
            MedallionRing = Hex("#D9B14C"),
            Emblem = Hex("#F3DB8C"),
            IceTop = new Color(0.8f, 0.78f, 0.98f),
            IceBottom = new Color(0.55f, 0.5f, 0.85f),
            IceCrack = new Color(0.95f, 0.85f, 0.55f, 0.9f),
            BadgeRing = Hex("#F3DB8C"),
            BadgeTop = Hex("#D9B14C"),
            BadgeBottom = Hex("#9C7A22"),
            BadgeMark = Hex("#160E18"),
            WildDisc = Hex("#2A1626"),
            WildDiscLow = Hex("#22111F"),
            BombDisc = Hex("#2A1220"),
            BombDiscLow = Hex("#220E1A"),
            ClockDisc = Hex("#221826"),
            ClockDiscLow = Hex("#1B131E"),
            PeekDisc = Hex("#1E1630"),
            PeekDiscLow = Hex("#181127"),
            DiscRing = Hex("#D9B14C"),
            WildStar = Hex("#D9B14C"),
            ClockBody = Hex("#D9B14C"),
            ClockHands = Hex("#2A1626"),
            PeekLid = Hex("#D9B14C"),
            PeekIris = Hex("#7A4BD6"),
            PeekIrisLight = Hex("#C49BFF"),
            HeartRim = Hex("#D9B14C"),
            HeartTop = Hex("#E23A5C"),
            HeartBottom = Hex("#8E1631"),
            HeartEmpty = new Color(0.55f, 0.42f, 0.2f, 0.4f),
            PanelFill = Hex("#221722"),
            PanelLip = Hex("#0E080E"),
            PanelRim = Hex("#D9B14C"),
            Primary = Hex("#8E1631"),
            PrimaryLip = Hex("#4A0A14"),
            Secondary = Hex("#3A1A3A"),
            SecondaryLip = Hex("#1A0A1A"),
            Accent = Hex("#D9B14C"),
            AccentLip = Hex("#8F6F1F"),
            Danger = Hex("#B8213D"),
            DangerLip = Hex("#5C1230"),
            Neutral = Hex("#2A202A"),
            NeutralLip = Hex("#120C12"),
            InputFill = Hex("#160E18"),
            StarFill = Hex("#F3DB8C"),
            StarRim = Hex("#8F6F1F"),
            StarEmpty = new Color(0.55f, 0.42f, 0.2f, 0.45f)
        };
    }


    /// <summary>A font of a theme: the file it is baked from and the asset it becomes.</summary>
    internal sealed class FontSpec
    {
        /// <summary>The TrueType file, relative to the package.</summary>
        public string Source;
        /// <summary>The folder of the font asset and its materials.</summary>
        public string Folder;
        /// <summary>The name of the font asset ("KenneyFuture SDF") and the prefix of its materials.</summary>
        public string Name;
        public Color OutlineColor = new Color(0.12f, 0.12f, 0.24f, 1f);
        public Color OutlineShadow = new Color(0.05f, 0.05f, 0.15f, 0.55f);
        public Color SoftShadow = new Color(0f, 0f, 0.1f, 0.35f);
        public Color TitleOutline = new Color(0.2f, 0.1f, 0.35f, 1f);
        public Color TitleShadow = new Color(0.1f, 0.02f, 0.2f, 0.7f);

        public string AssetPath => $"{Folder}/{Name}.asset";

        /// <summary>The name without its " SDF" suffix, which the materials start with ("KenneyFuture Outline").</summary>
        public string Prefix => Name.EndsWith(" SDF") ? Name.Substring(0, Name.Length - 4) : Name;

        /// <summary>The material preset called <paramref name="preset"/> ("Outline", "Shadow", "Title"), relative to the package.</summary>
        public string MaterialPath(string preset)
        {
            return $"{Folder}/{Prefix} {preset}.mat";
        }
    }


    /// <summary>
    /// What the generators need to know about one theme: where its art goes, its worlds and deck, the colours of the
    /// shared drawings, its fonts, its title screen and the palette the game reads at run time.
    /// </summary>
    internal sealed class ThemeSpec
    {
        public string Id;
        public string Name;
        public string Description;
        /// <summary>The folder of the generated art, relative to the package ("Art", "Art/Themes/AfterDark").</summary>
        public string ArtRoot;
        /// <summary>The folder of the faces of the deck.</summary>
        public string FacesFolder;
        /// <summary>Whether the faces are drawn (else they are imported renders).</summary>
        public bool DrawFaces;
        /// <summary>Whether the interface kit is Kenney's (else it is drawn under ArtRoot/UI).</summary>
        public bool KenneyKit;
        /// <summary>Put before a world's id in the name of its asset and its card back.</summary>
        public string WorldPrefix = string.Empty;
        public WorldSpec[] Worlds;
        /// <summary>The faces of the deck, in the order save games refer to them.</summary>
        public string[] Faces;
        /// <summary>The backdrop shapes to draw.</summary>
        public string[] Ambients;
        public ArtStyle Style;
        public FontSpec TitleFont;
        public FontSpec BodyFont;
        public string TitleWords;
        public string Subtitle;
        public Color GradientTop;
        public Color GradientBottom;
        /// <summary>Faces standing around the title: two big ones, then two small ones.</summary>
        public string[] Mascots;
        /// <summary>The interface sprite behind the title and its tint.</summary>
        public string Ornament;
        public Color OrnamentColor;
        public Color PatternColor;
        public Color VignetteColor;
        /// <summary>Fills the palette the game reads at run time.</summary>
        public Action<MemoryCardsTheme.Palette> Palette;
        /// <summary>The colours of the burst of a wild card.</summary>
        public Color[] WildColors;
        /// <summary>Fills the words of the theme: what the faces are called and the texts of the levels.</summary>
        public Action<MemoryCardsTheme.Words> Words;

        public string CardsFolder => $"{ArtRoot}/Cards";
        public string IconsFolder => $"{ArtRoot}/Icons";
        public string UiFolder => $"{ArtRoot}/UI";
        public string ParticlesFolder => $"{ArtRoot}/Particles";
        public string BackdropFolder => $"{ArtRoot}/Backdrop";
        public string FontsFolder => $"{ArtRoot}/Fonts";

        /// <summary>The file name of a world's card back ("BackFarm", "BackAfterDarkLounge").</summary>
        public string BackName(WorldSpec world)
        {
            return $"Back{WorldPrefix}{world.Id}";
        }

        /// <summary>The asset of a world, relative to the package.</summary>
        public string WorldPath(WorldSpec world)
        {
            return $"Settings/Worlds/{WorldPrefix}{world.Id}.asset";
        }
    }


    /// <summary>The themes of the game: the original look and After Dark.</summary>
    internal static class ThemeSpecs
    {
        public const string ThemesFolder = "Settings/Themes";
        public const string AfterDarkRoot = "Art/Themes/AfterDark";

        /// <summary>The faces of the After Dark deck, in the order save games refer to them.</summary>
        public static readonly string[] AfterDarkFaces =
        {
            "lipstick", "stiletto", "corset", "garter", "champagne", "rose", "mask", "fan", "perfume", "pearls",
            "dancer", "cocktail", "glove", "lips", "cherry", "handcuffs", "candle", "ring", "boot", "holder",
            "bow", "letter", "key", "curtain", "chandelier", "tophat", "cane", "bowtie", "dice", "playingcard"
        };

        /// <summary>The worlds of After Dark: one for every world of the campaign, dealing as many faces as the original ones.</summary>
        public static readonly WorldSpec[] AfterDarkWorlds =
        {
            new WorldSpec
            {
                Id = "Lounge", Name = "Velvet Lounge", Tagline = "Dim the lights, take a seat.",
                SkyTop = "#1C0B16", SkyBottom = "#4A1630", Accent = "#D9B14C", AccentDark = "#F3DB8C",
                Card = "#4A1030", CardDark = "#260818", Pattern = CardPattern.Damask, Emblem = CardEmblem.Diamond,
                Ambient = "Smoke", AmbientColor = new Color(1f, 0.9f, 0.8f, 0.1f), Motion = AmbientMotion.Drift,
                Mascot = "champagne", StarsRequired = 0,
                Animals = new[] { "champagne", "cocktail", "cherry", "candle", "chandelier", "tophat", "cane", "bowtie", "dice", "playingcard" }
            },
            new WorldSpec
            {
                Id = "Boudoir", Name = "Boudoir", Tagline = "Silk, lace and a locked door.",
                SkyTop = "#150912", SkyBottom = "#3A1128", Accent = "#E2557B", AccentDark = "#F4A3BA",
                Card = "#1E1220", CardDark = "#0C060C", Pattern = CardPattern.Lace, Emblem = CardEmblem.Lips,
                Ambient = "Petal", AmbientColor = new Color(0.89f, 0.33f, 0.48f, 0.75f), Motion = AmbientMotion.Fall,
                Mascot = "lipstick", StarsRequired = 10,
                Animals = new[] { "lipstick", "perfume", "pearls", "corset", "garter", "stiletto", "glove", "rose", "lips", "letter", "bow", "ring" }
            },
            new WorldSpec
            {
                Id = "Masquerade", Name = "Masquerade", Tagline = "Nobody knows who you are tonight.",
                SkyTop = "#0B0A1C", SkyBottom = "#2A1C46", Accent = "#9B6DFF", AccentDark = "#CDB6FF",
                Card = "#1C1636", CardDark = "#0A0818", Pattern = CardPattern.Sunburst, Emblem = CardEmblem.Mask,
                Ambient = "Sparkle", AmbientColor = new Color(1f, 0.92f, 0.7f, 0.8f), Motion = AmbientMotion.Rise,
                Mascot = "mask", StarsRequired = 26,
                Animals = new[] { "mask", "fan", "dancer", "holder", "boot", "handcuffs", "curtain", "key", "candle", "chandelier", "cane" }
            },
            new WorldSpec
            {
                Id = "Bar", Name = "Champagne Bar", Tagline = "Every glass, every twist!",
                SkyTop = "#3A0C1C", SkyBottom = "#8E1631", Accent = "#F3DB8C", AccentDark = "#F8E8B0",
                Card = "#8E1631", CardDark = "#4A0A14", Pattern = CardPattern.Chevron, Emblem = CardEmblem.Star,
                Ambient = "Bubble", AmbientColor = new Color(1f, 0.95f, 0.85f, 0.55f), Motion = AmbientMotion.Rise,
                Mascot = "cocktail", StarsRequired = 0,
                Animals = new string[0]
            }
        };

        public static readonly FontSpec KenneyFuture = new FontSpec
        {
            Source = "Art/Kenney/Fonts/Kenney Future.ttf",
            Folder = "Art/Fonts",
            Name = "KenneyFuture SDF"
        };

        public static readonly FontSpec Cinzel = new FontSpec
        {
            Source = AfterDarkRoot + "/Fonts/Cinzel.ttf",
            Folder = AfterDarkRoot + "/Fonts",
            Name = "Cinzel SDF",
            TitleOutline = new Color(0.3f, 0.2f, 0.05f, 1f),
            TitleShadow = new Color(0f, 0f, 0f, 0.8f)
        };

        public static readonly FontSpec Playfair = new FontSpec
        {
            Source = AfterDarkRoot + "/Fonts/PlayfairDisplay-Regular.ttf",
            Folder = AfterDarkRoot + "/Fonts",
            Name = "PlayfairDisplay SDF",
            OutlineColor = new Color(0.08f, 0.04f, 0.08f, 1f),
            OutlineShadow = new Color(0f, 0f, 0f, 0.6f),
            SoftShadow = new Color(0f, 0f, 0f, 0.5f)
        };

        public static readonly ThemeSpec Classic = new ThemeSpec
        {
            Id = "Classic",
            Name = "Classic",
            Description = "The original look: thirty cute critters, sunny worlds and the rounded Kenney interface.",
            ArtRoot = "Art",
            FacesFolder = MemoryCardsArtBuilder.AnimalsFolder,
            DrawFaces = false,
            KenneyKit = true,
            WorldPrefix = string.Empty,
            Worlds = WorldSpecs.All,
            Faces = WorldSpecs.Animals,
            Ambients = new[] { "Cloud", "Leaf", "Snowflake", "Balloon" },
            Style = ArtStyle.Classic,
            TitleFont = KenneyFuture,
            BodyFont = KenneyFuture,
            TitleWords = "MEMORY CARDS",
            Subtitle = "A critter matching adventure",
            GradientTop = Color.white,
            GradientBottom = Hex("#FFD36B"),
            Mascots = new[] { "giraffe", "panda", "chick", "penguin" },
            Ornament = "Soft",
            OrnamentColor = new Color(1f, 1f, 1f, 0f),
            PatternColor = new Color(1f, 1f, 1f, 0.07f),
            VignetteColor = new Color(1f, 1f, 1f, 0.12f),
            Palette = palette =>
            {
                // The values the scene builder and the views used before the themes; the fields default to them too.
                palette.ink = Hex("#2B2D42");
                palette.softInk = Hex("#6B6F86");
                palette.light = Color.white;
                palette.gold = Hex("#FFC933");
                palette.buttonText = Color.white;
                palette.accentText = Hex("#2B2D42");
                palette.neutralText = Hex("#2B2D42");
                palette.wild = new Color(0.85f, 0.55f, 1f);
                palette.peek = new Color(0.8f, 0.65f, 1f);
                palette.combo = new Color(1f, 0.55f, 0.85f);
                palette.placeholder = new Color(0.4f, 0.42f, 0.5f, 0.5f);
                palette.panel = Color.white;
                palette.pill = new Color(0.1f, 0.1f, 0.22f, 0.5f);
                palette.dim = new Color(0.08f, 0.06f, 0.16f, 0.58f);
                palette.sky = Hex("#62C4FF");
                palette.detailsHeader = Hex("#FF9A1F");
                palette.resultsRibbon = Hex("#FF9A1F");
                palette.pauseRibbon = Hex("#8E7CFF");
                palette.settingsRibbon = Hex("#1FC2FF");
                palette.good = new Color(0.35f, 0.85f, 0.4f);
                palette.bad = new Color(1f, 0.38f, 0.32f);
                palette.matchGlow = new Color(1f, 0.85f, 0.25f);
                palette.mistakeTint = new Color(1f, 0.55f, 0.55f);
                palette.wiltTint = new Color(0.62f, 0.62f, 0.68f);
                palette.celebration = new System.Collections.Generic.List<Color>
                {
                    Hex("#FFC933"), Color.white, new Color(0.4f, 0.8f, 1f), new Color(1f, 0.45f, 0.6f), new Color(0.5f, 0.9f, 0.45f)
                };
                palette.seats = new System.Collections.Generic.List<Color>
                {
                    new Color(0.26f, 0.6f, 1f), new Color(1f, 0.42f, 0.42f), new Color(0.35f, 0.8f, 0.45f), new Color(1f, 0.7f, 0.25f)
                };
                palette.hurry = new Color(1f, 0.35f, 0.3f);
                palette.tabText = new Color(0.2f, 0.22f, 0.3f);
                palette.tabTextSelected = Color.white;
                palette.tabIdle = Color.white;
                palette.tabLocked = new Color(0.7f, 0.72f, 0.78f);
                palette.cardLocked = new Color(0.62f, 0.62f, 0.68f);
                palette.lockShade = new Color(0.12f, 0.12f, 0.22f, 0.5f);
                palette.lockDisc = new Color(0.15f, 0.16f, 0.26f, 0.85f);
                palette.defeat = new Color(0.55f, 0.5f, 0.62f);
                palette.best = Hex("#C96A00");
                palette.goalReached = new Color(0.13f, 0.5f, 0.24f);
                palette.goalMissed = new Color(0.35f, 0.35f, 0.42f, 0.8f);
                palette.lobbyAccent = Hex("#3D8BFF");
                palette.lobbyWindow = Hex("#2B2D42");
                palette.lobbyRow = Hex("#3B3E5C");
            },
            WildColors = new[]
            {
                new Color(1f, 0.35f, 0.35f), new Color(1f, 0.7f, 0.2f), new Color(1f, 0.95f, 0.3f),
                new Color(0.4f, 0.9f, 0.4f), new Color(0.35f, 0.7f, 1f), new Color(0.75f, 0.45f, 1f)
            },
            // The levels keep their own words.
            Words = words =>
            {
                words.face = "animal";
                words.faces = "animals";
                words.shuffle = "The critters are on the move";
                words.levels = new System.Collections.Generic.List<MemoryCardsTheme.LevelWords>();
            }
        };

        public static readonly ThemeSpec AfterDark = new ThemeSpec
        {
            Id = "AfterDark",
            Name = "After Dark",
            Description = "A grown-up night out: black, gold and deep red, art deco frames, velvet and lace, and a deck of thirty boudoir and burlesque keepsakes.",
            ArtRoot = AfterDarkRoot,
            FacesFolder = AfterDarkRoot + "/Faces",
            DrawFaces = true,
            KenneyKit = false,
            WorldPrefix = "AfterDark",
            Worlds = AfterDarkWorlds,
            Faces = AfterDarkFaces,
            Ambients = new[] { "Smoke", "Petal", "Sparkle", "Bubble" },
            Style = ArtStyle.AfterDark,
            TitleFont = Cinzel,
            BodyFont = Playfair,
            TitleWords = "MEMORY CARDS",
            Subtitle = "An after-hours matching game",
            GradientTop = Hex("#F8E8B0"),
            GradientBottom = Hex("#C9992E"),
            Mascots = new[] { "mask", "champagne", "rose", "fan" },
            Ornament = "Ornament",
            OrnamentColor = new Color(0.85f, 0.7f, 0.3f, 0.9f),
            PatternColor = new Color(0.85f, 0.7f, 0.3f, 0.06f),
            VignetteColor = new Color(1f, 0.8f, 0.5f, 0.08f),
            Palette = palette =>
            {
                palette.ink = Hex("#F4E7CF");
                palette.softInk = Hex("#B9A07A");
                palette.light = Hex("#F4E7CF");
                palette.gold = Hex("#F3DB8C");
                palette.buttonText = Hex("#F4E7CF");
                palette.accentText = Hex("#160E18");
                palette.neutralText = Hex("#F4E7CF");
                palette.wild = Hex("#F3DB8C");
                palette.peek = Hex("#CDB6FF");
                palette.combo = Hex("#F4A3BA");
                palette.placeholder = new Color(0.73f, 0.63f, 0.48f, 0.5f);
                palette.panel = Color.white;
                palette.pill = new Color(0f, 0f, 0f, 0.55f);
                palette.dim = new Color(0f, 0f, 0f, 0.72f);
                palette.sky = Hex("#120C12");
                palette.detailsHeader = Hex("#8E1631");
                palette.resultsRibbon = Hex("#8E1631");
                palette.pauseRibbon = Hex("#5C1230");
                palette.settingsRibbon = Hex("#3A1A3A");
                palette.good = Hex("#F3DB8C");
                palette.bad = Hex("#FF5C6E");
                palette.matchGlow = Hex("#D9B14C");
                palette.mistakeTint = new Color(1f, 0.6f, 0.65f);
                palette.wiltTint = new Color(0.45f, 0.4f, 0.45f);
                palette.celebration = new System.Collections.Generic.List<Color>
                {
                    Hex("#D9B14C"), Hex("#F3DB8C"), Hex("#F4E7CF"), Hex("#8E1631"), Hex("#E2557B"), Hex("#5C1230")
                };
                palette.seats = new System.Collections.Generic.List<Color>
                {
                    Hex("#D9B14C"), Hex("#E2557B"), Hex("#9B6DFF"), Hex("#F4E7CF")
                };
                palette.hurry = Hex("#FF5C6E");
                palette.tabText = Hex("#F4E7CF");
                palette.tabTextSelected = Hex("#120C12");
                palette.tabIdle = Hex("#120C12");
                palette.tabLocked = Hex("#2A222A");
                palette.cardLocked = new Color(0.45f, 0.4f, 0.45f);
                palette.lockShade = new Color(0f, 0f, 0f, 0.55f);
                palette.lockDisc = new Color(0.1f, 0.06f, 0.1f, 0.9f);
                palette.defeat = Hex("#3A2A3A");
                palette.best = Hex("#F3DB8C");
                palette.goalReached = Hex("#F3DB8C");
                palette.goalMissed = new Color(0.73f, 0.63f, 0.48f, 0.7f);
                palette.lobbyAccent = Hex("#D9B14C");
                palette.lobbyWindow = Hex("#1A121A");
                palette.lobbyRow = Hex("#2C1F2C");
            },
            WildColors = new[] { Hex("#F3DB8C"), Hex("#D9B14C"), Hex("#F4E7CF"), Hex("#E2557B"), Hex("#C41E3A"), Hex("#CDB6FF") },
            Words = words =>
            {
                words.face = "keepsake";
                words.faces = "keepsakes";
                words.shuffle = "Sleight of hand - the cards have moved";
                words.levels = AfterDarkLevels();
            }
        };

        /// <summary>
        /// The levels in the words of After Dark, in campaign order (18 campaign levels, the endless run, free play); an
        /// empty text keeps the level's own.
        /// </summary>
        private static System.Collections.Generic.List<MemoryCardsTheme.LevelWords> AfterDarkLevels()
        {
            MemoryCardsTheme.LevelWords Say(string title, string description = null, string tip = null)
            {
                return new MemoryCardsTheme.LevelWords { title = title, description = description ?? string.Empty, tip = tip ?? string.Empty };
            }

            return new System.Collections.Generic.List<MemoryCardsTheme.LevelWords>
            {
                // Velvet Lounge.
                Say("Doors Open", "Flip two cards at a time and find the matching pairs. The night is young."),
                Say("Bubbly Company"),
                Say("A Second Look"),
                Say("Beat the Bell", "Beat the clock before the bell for last orders! Every pair you find adds two seconds."),
                Say("After Midnight", "Clock cards hide among the keepsakes. Flip one for six extra seconds!"),
                Say("Heartbreakers"),
                // Boudoir.
                Say("Wild at Heart", "The golden wild card matches any keepsake - and finds its twin for you!",
                    "Flip the wild card together with any keepsake to match all of its kind."),
                Say("Sleight of Hand", "Nimble fingers! After every third mistake the hidden cards swap places.",
                    "Three mistakes and the dealer shuffles the hidden cards!"),
                Say("Silk and Danger", "Two bombs hide behind the silk. Each one costs a heart - remember where they are!"),
                Say("Running Order", "Tonight's parade has a running order! Match the keepsakes in the order shown at the top.",
                    "Only the keepsake the parade asks for counts. Remember the others for later!"),
                Say("Three's Company"),
                Say("Midnight Rumble", "Bombs, a wild card, a clock and shuffles, all against the clock. The boudoir after midnight!",
                    "Use the wild card on the keepsake you cannot find."),
                // Masquerade.
                Say("Cold Shoulder"),
                Say("Measured Steps", "Mind your steps: you only have 26 moves. Peek cards show the whole board for a moment."),
                Say("The Band Plays On", "Triplets against the clock. Keep time with the band and find three of a kind!"),
                Say("Change Partners", "The dancers change partners after every second mistake - and some cards are frozen solid.",
                    "Two mistakes and the dancers swap the hidden cards."),
                Say("Grand Procession", "A procession on thin ice: match in order and steer clear of the bombs.",
                    "The parade shows the next keepsakes at the top."),
                Say("Unmasked", "The grand finale under the chandeliers. Every twist you have learned, all at once!"),
                // The Champagne Bar.
                Say("Last Call"),
                Say(string.Empty)
            };
        }

        /// <summary>Every theme, the one the game starts with first.</summary>
        public static readonly ThemeSpec[] All = { Classic, AfterDark };

        public static ThemeSpec Find(string id)
        {
            foreach (ThemeSpec spec in All)
            {
                if (spec.Id == id)
                {
                    return spec;
                }
            }
            return null;
        }
    }
}
