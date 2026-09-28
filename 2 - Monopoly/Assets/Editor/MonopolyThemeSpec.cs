using System.Collections.Generic;
using UnityEngine;

namespace Portfolio.Monopoly.EditorTools
{
    /// <summary>Which family of drawings and models a theme is made of.</summary>
    internal enum ArtStyle
    {
        /// <summary>The original board game look: mint board, wooden table, pewter figures, white cards.</summary>
        Classic,
        /// <summary>A space station trading floor: dark panels, neon rims, holograms, spacecraft tokens.</summary>
        Galactic
    }


    /// <summary>A font file of a theme (under its Fonts folder) and the name of the font asset baked from it.</summary>
    internal sealed class FontSpec
    {
        public string file;
        public string name;

        public FontSpec(string file, string name)
        {
            this.file = file;
            this.name = name;
        }
    }


    /// <summary>
    /// Everything the builders need to make one look of the game: where its art goes, which style of drawings and
    /// models, the palette and the print colours, the colour sets, the seats, the tokens, the fonts, the material
    /// colours and the lights. The same builders run for every spec; <see cref="All"/> lists the looks the game ships.
    /// </summary>
    internal sealed class MonopolyThemeSpec
    {
        /// <summary>The display name of the theme.</summary>
        public string Name;
        /// <summary>The theme asset: Config/Themes/&lt;AssetName&gt;.asset.</summary>
        public string AssetName;
        public string Description;
        /// <summary>The folder of the theme's art, relative to the package (Textures, Meshes, Materials, Fonts, Tokens and UI go under it).</summary>
        public string Folder;
        public ArtStyle Style;
        public string EditionLabel;

        public MonopolyPalette Palette = new MonopolyPalette();
        public PrintLook Print = new PrintLook();
        /// <summary>The colour sets in the order of <see cref="MonopolyStyle.Groups"/>: name, colour, text colour.</summary>
        public (string name, Color color, Color text)[] Groups;
        public (string name, Color color)[] Seats;
        public (string name, Color tint)[] Tokens;
        public SceneLook Scene = new SceneLook();

        public FontSpec Body;
        public FontSpec Bold;
        public FontSpec Heavy;
        /// <summary>The font of the big words (the logo, the banners); the heavy font in the classic look.</summary>
        public FontSpec Title;
        public Color TitleOutline;
        public Color TitleUnderlay;

        /// <summary>The field of the board and the lines on it.</summary>
        public Color BoardField;
        public Color BoardLine;
        public Color SlabColor;
        public float SlabMetallic;
        public float SlabSmoothness;
        public Color TokenMetal;
        public float TokenMetallic;
        public float TokenSmoothness;
        /// <summary>How much the figures light up in their own tint.</summary>
        public float TokenGlow;
        public Color HouseColor;
        public Color HotelColor;
        public Color CardEdge;
        public Color DieBody;
        public Color DiePip;
        public Color SpeedDieBody;
        public Color SpeedDiePip;
        public Color MortgagedShade;
        /// <summary>Whether the lanes of the board, the windows, the pips and the seat colours glow.</summary>
        public bool Emissive;
        /// <summary>How often the table texture repeats across the table.</summary>
        public float TableTiles;
        /// <summary>The studio the metal reflects: floor, horizon, ceiling and the colour of its lights.</summary>
        public Color StudioFloor;
        public Color StudioHorizon;
        public Color StudioCeiling;
        public Color StudioLight;

        public string TexturesFolder => $"{Folder}/Textures";
        public string MeshesFolder => $"{Folder}/Meshes";
        public string MaterialsFolder => $"{Folder}/Materials";
        public string FontsFolder => $"{Folder}/Fonts";
        public string TokensFolder => $"{Folder}/Tokens";
        public string UIFolder => $"{Folder}/UI";
        public string AssetPath => $"Config/Themes/{AssetName}.asset";

        public Color GroupColor(ColorGroup group)
        {
            int index = System.Array.IndexOf(MonopolyStyle.Groups, group);
            return index >= 0 ? Groups[index].color : Color.gray;
        }

        private static Color Hex(int rgb)
        {
            return MonopolyStyle.Hex(rgb);
        }

        private static Color Hex(int rgb, float alpha)
        {
            return MonopolyStyle.WithAlpha(MonopolyStyle.Hex(rgb), alpha);
        }

        /// <summary>The original look, from the classic values of <see cref="MonopolyStyle.Defaults"/>.</summary>
        public static readonly MonopolyThemeSpec Classic = new MonopolyThemeSpec
        {
            Name = "Classic",
            AssetName = "Classic",
            Description = "The World Tour edition: a mint board on a wooden table, pewter figures and white title deeds.",
            Folder = "Art",
            Style = ArtStyle.Classic,
            EditionLabel = "WORLD TOUR EDITION",
            Groups = new[]
            {
                ("Brown", MonopolyStyle.Defaults.GroupColor(ColorGroup.Brown), MonopolyStyle.Defaults.GroupTextColor(ColorGroup.Brown)),
                ("Light Blue", MonopolyStyle.Defaults.GroupColor(ColorGroup.LightBlue), MonopolyStyle.Defaults.GroupTextColor(ColorGroup.LightBlue)),
                ("Pink", MonopolyStyle.Defaults.GroupColor(ColorGroup.Pink), MonopolyStyle.Defaults.GroupTextColor(ColorGroup.Pink)),
                ("Orange", MonopolyStyle.Defaults.GroupColor(ColorGroup.Orange), MonopolyStyle.Defaults.GroupTextColor(ColorGroup.Orange)),
                ("Red", MonopolyStyle.Defaults.GroupColor(ColorGroup.Red), MonopolyStyle.Defaults.GroupTextColor(ColorGroup.Red)),
                ("Yellow", MonopolyStyle.Defaults.GroupColor(ColorGroup.Yellow), MonopolyStyle.Defaults.GroupTextColor(ColorGroup.Yellow)),
                ("Green", MonopolyStyle.Defaults.GroupColor(ColorGroup.Green), MonopolyStyle.Defaults.GroupTextColor(ColorGroup.Green)),
                ("Dark Blue", MonopolyStyle.Defaults.GroupColor(ColorGroup.DarkBlue), MonopolyStyle.Defaults.GroupTextColor(ColorGroup.DarkBlue)),
                ("Stations", MonopolyStyle.Defaults.GroupColor(ColorGroup.Railroad), MonopolyStyle.Defaults.GroupTextColor(ColorGroup.Railroad)),
                ("Utilities", MonopolyStyle.Defaults.GroupColor(ColorGroup.Utility), MonopolyStyle.Defaults.GroupTextColor(ColorGroup.Utility))
            },
            Seats = new[]
            {
                ("Red", MonopolyStyle.Defaults.PlayerColors[0]), ("Blue", MonopolyStyle.Defaults.PlayerColors[1]),
                ("Green", MonopolyStyle.Defaults.PlayerColors[2]), ("Yellow", MonopolyStyle.Defaults.PlayerColors[3])
            },
            Tokens = new[]
            {
                ("Race Car", Hex(0xC7CBD1)), ("Top Hat", Hex(0xC7CBD1)), ("Scottie Dog", Hex(0xC7CBD1)), ("Battleship", Hex(0xC7CBD1)),
                ("Cat", Hex(0xC7CBD1)), ("Rubber Duck", Hex(0xC7CBD1)), ("Penguin", Hex(0xC7CBD1)), ("T-Rex", Hex(0xC7CBD1))
            },
            Scene = new SceneLook
            {
                confetti = new List<Color>
                {
                    MonopolyStyle.Defaults.Red, MonopolyStyle.Defaults.Gold, MonopolyStyle.Defaults.Green, MonopolyStyle.Defaults.Blue,
                    MonopolyStyle.Defaults.PlayerColors[0]
                }
            },
            Body = new FontSpec("Poppins-SemiBold.ttf", "Poppins SemiBold"),
            Bold = new FontSpec("Poppins-Bold.ttf", "Poppins Bold"),
            Heavy = new FontSpec("Poppins-Black.ttf", "Poppins Black"),
            Title = new FontSpec("Poppins-Black.ttf", "Poppins Black"),
            TitleOutline = new Color(0.35f, 0f, 0.05f, 1f),
            TitleUnderlay = new Color(0.1f, 0f, 0.02f, 0.6f),
            BoardField = Hex(0xC4E3C6),
            BoardLine = Hex(0x1F2A24),
            SlabColor = Hex(0x18202B),
            SlabMetallic = 0.1f,
            SlabSmoothness = 0.55f,
            TokenMetal = Hex(0xC7CBD1),
            TokenMetallic = 0.92f,
            TokenSmoothness = 0.74f,
            HouseColor = Hex(0x1FA355),
            HotelColor = Hex(0xD7263D),
            CardEdge = Hex(0xF3EFE4),
            DieBody = Hex(0xFBFAF6),
            DiePip = Hex(0x1A1A1D),
            SpeedDieBody = Hex(0xD7263D),
            SpeedDiePip = Color.white,
            MortgagedShade = new Color(0.05f, 0.06f, 0.1f, 0.62f),
            Emissive = false,
            TableTiles = 9f,
            StudioFloor = new Color(0.16f, 0.12f, 0.1f),
            StudioHorizon = new Color(0.62f, 0.6f, 0.58f),
            StudioCeiling = new Color(0.95f, 0.92f, 0.86f),
            StudioLight = Color.white
        };

        /// <summary>A space station trading floor: a near-black board with neon lanes, holographic cards, spacecraft tokens.</summary>
        public static readonly MonopolyThemeSpec GalacticTrade = new MonopolyThemeSpec
        {
            Name = "Galactic Trade",
            AssetName = "GalacticTrade",
            Description = "A space station trading floor: a dark board with glowing lanes, holographic cards, spacecraft tokens and domes and towers with lit windows.",
            Folder = "Art/Themes/GalacticTrade",
            Style = ArtStyle.Galactic,
            EditionLabel = "GALACTIC TRADE EDITION",
            Palette = new MonopolyPalette
            {
                red = Hex(0xFF3D6E),
                darkRed = Hex(0xB01E4C),
                ink = Hex(0xE6F4FF),
                plate = Hex(0x2A2F6B),
                paper = Hex(0x0B1220, 0.94f),
                panel = Hex(0x162238),
                soft = Hex(0x223250),
                mint = Hex(0x0A1020),
                green = Hex(0x45FFA8),
                blue = Hex(0x2F7BFF),
                gold = Hex(0xFFC94D),
                muted = Hex(0x8BA3C7),
                chanceOrange = Hex(0xFFB44F),
                chestBlue = Hex(0x55EEFF),
                shadow = new Color(0.2f, 0.9f, 1f, 0.32f)
            },
            Print = new PrintLook
            {
                ink = Hex(0xDDF6FF),
                city = Hex(0x6FA8C8),
                utilityIcon = Hex(0x35E0FF),
                taxIcon = Hex(0xFFC94D),
                go = Hex(0x2EF2A0),
                officer = Hex(0xB48CFF),
                chanceMark = Hex(0xFF9A3C),
                chestIcon = Hex(0x35E0FF),
                mortgaged = Hex(0xFF5C7A),
                deckWords = Hex(0xEAF8FF),
                logoWord = Hex(0xEAF8FF),
                edition = Hex(0x8FE9FF)
            },
            Groups = new[]
            {
                ("Copper", Hex(0xD2691E), Color.white),
                ("Ice", Hex(0x4DE8FF), Hex(0x061020)),
                ("Magenta", Hex(0xFF4FD8), Color.white),
                ("Amber", Hex(0xFF8A2A), Hex(0x061020)),
                ("Crimson", Hex(0xFF2E5B), Color.white),
                ("Solar", Hex(0xFFE13A), Hex(0x061020)),
                ("Emerald", Hex(0x3DFF8F), Hex(0x061020)),
                ("Sapphire", Hex(0x5B6BFF), Color.white),
                ("Docks", Hex(0xA07BFF), Color.white),
                ("Reactors", Hex(0x7FE0D0), Hex(0x061020))
            },
            Seats = new[]
            {
                ("Red", Hex(0xFF4D6D)), ("Blue", Hex(0x2F9BFF)), ("Green", Hex(0x45FF96)), ("Yellow", Hex(0xFFC94D))
            },
            Tokens = new[]
            {
                ("Rocket", Hex(0xF2F6FF)), ("Satellite", Hex(0xC8D3E0)), ("Robot", Hex(0x9FD4FF)), ("UFO", Hex(0xB9FFB0)),
                ("Comet", Hex(0xCFF6FF)), ("Ringed Planet", Hex(0xFFB570)), ("Astronaut Helmet", Hex(0xFFFFFF)), ("Space Station", Hex(0xB8C4D6))
            },
            Scene = new SceneLook
            {
                background = Hex(0x04060C),
                ambientSky = Hex(0x2A3D66),
                ambientEquator = Hex(0x18243F),
                ambientGround = Hex(0x05070C),
                sunColor = new Color(0.85f, 0.92f, 1f),
                sunIntensity = 1f,
                fillColor = new Color(0.6f, 0.45f, 1f),
                fillIntensity = 0.35f,
                reflectionIntensity = 0.9f,
                confetti = new List<Color> { Hex(0x35E0FF), Hex(0xA07BFF), Hex(0xFF4FD8), Hex(0x2EF2A0), Hex(0xFFC94D) }
            },
            Body = new FontSpec("Exo2-SemiBold.ttf", "Exo 2 SemiBold"),
            Bold = new FontSpec("Exo2-Bold.ttf", "Exo 2 Bold"),
            Heavy = new FontSpec("Exo2-Black.ttf", "Exo 2 Black"),
            Title = new FontSpec("Orbitron-Black.ttf", "Orbitron Black"),
            TitleOutline = new Color(0.05f, 0.35f, 0.5f, 1f),
            TitleUnderlay = new Color(0.1f, 0.6f, 0.9f, 0.55f),
            BoardField = Hex(0x070B14),
            BoardLine = Hex(0x35E0FF),
            SlabColor = Hex(0x0A0F1A),
            SlabMetallic = 0.7f,
            SlabSmoothness = 0.55f,
            TokenMetal = Hex(0xE8EEF6),
            TokenMetallic = 0.3f,
            TokenSmoothness = 0.68f,
            TokenGlow = 0.3f,
            HouseColor = Hex(0xDCE8F5),
            HotelColor = Hex(0xDCE8F5),
            CardEdge = Hex(0x162034),
            DieBody = Hex(0x121A2B),
            DiePip = Hex(0x5FF0FF),
            SpeedDieBody = Hex(0x3A2A7A),
            SpeedDiePip = Hex(0xFFC94D),
            MortgagedShade = new Color(0.02f, 0.03f, 0.06f, 0.72f),
            Emissive = true,
            TableTiles = 3f,
            StudioFloor = Hex(0x05070C),
            StudioHorizon = Hex(0x16233F),
            StudioCeiling = Hex(0x3A4E80),
            StudioLight = new Color(0.7f, 0.9f, 1f)
        };

        /// <summary>The looks the game ships, the classic one first.</summary>
        public static readonly MonopolyThemeSpec[] All = { Classic, GalacticTrade };
    }
}
