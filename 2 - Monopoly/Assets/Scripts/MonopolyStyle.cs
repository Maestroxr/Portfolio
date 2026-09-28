using System.Globalization;
using Gamebox;
using UnityEngine;

namespace Portfolio.Monopoly
{
    /// <summary>
    /// The colours, names and number formats the board, the tokens and the interface share. They come from the active
    /// <see cref="MonopolyTheme"/> (<see cref="Theme"/>); without one (a project without the theme assets, a test) the
    /// classic values of <see cref="Defaults"/> apply, so every call site works either way.
    /// </summary>
    public static class MonopolyStyle
    {
        /// <summary>The classic look, as constants: what the game shows without a theme and what "Classic" is built from.</summary>
        public static class Defaults
        {
            public static readonly Color Red = Hex(0xE4002B);
            public static readonly Color DarkRed = Hex(0xA8001F);
            public static readonly Color Ink = Hex(0x1B2330);
            public static readonly Color Paper = Hex(0xFFFFFF);
            public static readonly Color Panel = Hex(0xF7F8FA);
            public static readonly Color Soft = Hex(0xEEF1F5);
            public static readonly Color Mint = Hex(0xD4EAD5);
            public static readonly Color Green = Hex(0x17A34A);
            public static readonly Color Blue = Hex(0x1D6FD8);
            public static readonly Color Gold = Hex(0xF5B301);
            public static readonly Color Muted = Hex(0x8C96A5);
            public static readonly Color ChanceOrange = Hex(0xF58220);
            public static readonly Color ChestBlue = Hex(0x1F8ACB);
            public static readonly Color Shadow = new Color(0f, 0.02f, 0.08f, 0.35f);
            public static readonly Color PrintCity = Shade(Mint, 0.45f);
            public static readonly Color PrintUtility = Hex(0x1F6FB2);
            public static readonly Color PrintTax = Shade(Gold, 0.85f);
            public static readonly Color PrintMortgaged = Hex(0xFF4D5E);
            public static readonly Color Background = Hex(0x10151D);
            public static readonly Color AmbientSky = Hex(0x98A2AE);
            public static readonly Color AmbientEquator = Hex(0x767B82);
            public static readonly Color AmbientGround = Hex(0x3C332C);

            /// <summary>The seat colours, in seat order.</summary>
            public static readonly Color[] PlayerColors = { Hex(0xE53935), Hex(0x1E88E5), Hex(0x2EAA4F), Hex(0xF9A825) };
            public static readonly string[] PlayerColorNames = { "Red", "Blue", "Green", "Yellow" };

            /// <summary>The token lineup, in token index order (the meshes and badges follow it).</summary>
            public static readonly string[] TokenNames = { "Race Car", "Top Hat", "Scottie Dog", "Battleship", "Cat", "Rubber Duck", "Penguin", "T-Rex" };

            public static Color GroupColor(ColorGroup group)
            {
                switch (group)
                {
                    case ColorGroup.Brown: return Hex(0x8E5A3C);
                    case ColorGroup.LightBlue: return Hex(0x9FD8F5);
                    case ColorGroup.Pink: return Hex(0xD6358E);
                    case ColorGroup.Orange: return Hex(0xF7941D);
                    case ColorGroup.Red: return Hex(0xE4202E);
                    case ColorGroup.Yellow: return Hex(0xFEDB00);
                    case ColorGroup.Green: return Hex(0x1FA355);
                    case ColorGroup.DarkBlue: return Hex(0x0B5CAD);
                    case ColorGroup.Railroad: return Hex(0x3A3F47);
                    case ColorGroup.Utility: return Hex(0x7A8699);
                    default: return Hex(0xBFC7CF);
                }
            }

            /// <summary>Text that reads on top of a set's colour.</summary>
            public static Color GroupTextColor(ColorGroup group)
            {
                return group == ColorGroup.LightBlue || group == ColorGroup.Yellow ? Ink : Color.white;
            }
        }

        private static MonopolyTheme cached;
        private static bool watching;

        /// <summary>A theme to use instead of the active one: the builders pin the classic look while they build, tests pin what they check.</summary>
        public static MonopolyTheme Override { get; set; }

        /// <summary>The active theme of the game, or null when the project has none.</summary>
        public static MonopolyTheme Theme
        {
            get
            {
                if (Override != null)
                {
                    return Override;
                }
                if (!watching)
                {
                    watching = true;
                    GameThemes.Changed += (game, theme) =>
                    {
                        if (game == GameType.Monopoly)
                        {
                            cached = theme as MonopolyTheme;
                        }
                    };
                }
                if (cached == null)
                {
                    cached = GameThemes.Active<MonopolyTheme>(GameType.Monopoly);
                }
                return cached;
            }
        }

        /// <summary>Takes <paramref name="theme"/> as the active one now (the manager tells the style before it repaints).</summary>
        public static void Follow(MonopolyTheme theme)
        {
            cached = theme;
        }

        private static MonopolyPalette Palette => Theme != null ? Theme.palette : null;

        public static Color Red => Palette?.red ?? Defaults.Red;
        public static Color DarkRed => Palette?.darkRed ?? Defaults.DarkRed;
        public static Color Ink => Palette?.ink ?? Defaults.Ink;
        /// <summary>The dark plate behind white words (the band of a station's deed, the jail banner).</summary>
        public static Color Plate => Palette?.plate ?? Defaults.Ink;
        public static Color Paper => Palette?.paper ?? Defaults.Paper;
        public static Color Panel => Palette?.panel ?? Defaults.Panel;
        public static Color Soft => Palette?.soft ?? Defaults.Soft;
        public static Color Mint => Palette?.mint ?? Defaults.Mint;
        public static Color Green => Palette?.green ?? Defaults.Green;
        public static Color Blue => Palette?.blue ?? Defaults.Blue;
        public static Color Gold => Palette?.gold ?? Defaults.Gold;
        public static Color Muted => Palette?.muted ?? Defaults.Muted;
        public static Color ChanceOrange => Palette?.chanceOrange ?? Defaults.ChanceOrange;
        public static Color ChestBlue => Palette?.chestBlue ?? Defaults.ChestBlue;
        public static Color Shadow => Palette?.shadow ?? Defaults.Shadow;

        /// <summary>The seat colours, in seat order.</summary>
        public static Color[] PlayerColors
        {
            get
            {
                MonopolyTheme theme = Theme;
                if (theme == null || theme.seats.Count == 0)
                {
                    return Defaults.PlayerColors;
                }
                var colors = new Color[theme.seats.Count];
                for (int i = 0; i < colors.Length; i++)
                {
                    colors[i] = theme.seats[i].color;
                }
                return colors;
            }
        }

        public static string[] PlayerColorNames
        {
            get
            {
                MonopolyTheme theme = Theme;
                if (theme == null || theme.seats.Count == 0)
                {
                    return Defaults.PlayerColorNames;
                }
                var names = new string[theme.seats.Count];
                for (int i = 0; i < names.Length; i++)
                {
                    names[i] = string.IsNullOrEmpty(theme.seats[i].name) ? Defaults.PlayerColorNames[i % Defaults.PlayerColorNames.Length] : theme.seats[i].name;
                }
                return names;
            }
        }

        /// <summary>The token lineup, in token index order (the meshes and badges follow it).</summary>
        public static string[] TokenNames
        {
            get
            {
                MonopolyTheme theme = Theme;
                if (theme == null || theme.tokens.Count == 0)
                {
                    return Defaults.TokenNames;
                }
                var names = new string[theme.tokens.Count];
                for (int i = 0; i < names.Length; i++)
                {
                    names[i] = string.IsNullOrEmpty(theme.tokens[i].name) ? Defaults.TokenNames[i % Defaults.TokenNames.Length] : theme.tokens[i].name;
                }
                return names;
            }
        }

        public static int TokenCount => TokenNames.Length;

        public static Color PlayerColor(int seat)
        {
            Color[] colors = PlayerColors;
            return colors[((seat % colors.Length) + colors.Length) % colors.Length];
        }

        public static Color GroupColor(ColorGroup group)
        {
            GroupLook look = Theme != null ? Theme.Group(group) : null;
            return look != null ? look.color : Defaults.GroupColor(group);
        }

        /// <summary>Text that reads on top of a set's colour.</summary>
        public static Color GroupTextColor(ColorGroup group)
        {
            GroupLook look = Theme != null ? Theme.Group(group) : null;
            return look != null ? look.textColor : Defaults.GroupTextColor(group);
        }

        public static string GroupName(ColorGroup group)
        {
            switch (group)
            {
                case ColorGroup.LightBlue: return "Light Blue";
                case ColorGroup.DarkBlue: return "Dark Blue";
                case ColorGroup.Railroad: return "Stations";
                case ColorGroup.Utility: return "Utilities";
                default: return group.ToString();
            }
        }

        /// <summary>The sets in board order, for rows of set chips (and the order of a theme's group looks).</summary>
        public static readonly ColorGroup[] Groups =
        {
            ColorGroup.Brown, ColorGroup.LightBlue, ColorGroup.Pink, ColorGroup.Orange, ColorGroup.Red, ColorGroup.Yellow,
            ColorGroup.Green, ColorGroup.DarkBlue, ColorGroup.Railroad, ColorGroup.Utility
        };

        public static string Money(int amount)
        {
            string digits = Mathf.Abs(amount).ToString("N0", CultureInfo.InvariantCulture);
            return amount < 0 ? $"-${digits}" : $"${digits}";
        }

        public static string SignedMoney(int amount)
        {
            return amount >= 0 ? "+" + Money(amount) : Money(amount);
        }

        public static Color Hex(int rgb)
        {
            return new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f, 1f);
        }

        /// <summary>
        /// The colour of words on a face of <paramref name="face"/>: the ink on the paper, a dark colour on light faces,
        /// white on the others (see <see cref="MonopolyTheme.TextOn(Color)"/>).
        /// </summary>
        public static Color TextOn(Color face)
        {
            MonopolyTheme theme = Theme;
            return theme != null ? theme.TextOn(face) : MonopolyTheme.TextOn(face, Defaults.Ink, Defaults.Paper, Defaults.Panel, Defaults.Soft);
        }

        public static Color WithAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }

        public static Color Shade(Color color, float factor)
        {
            return new Color(color.r * factor, color.g * factor, color.b * factor, color.a);
        }

        /// <summary>A colour washed towards the paper colour (white in the classic look, dark in a dark one).</summary>
        public static Color Tint(Color color, float amount)
        {
            Color paper = Paper;
            return new Color(Mathf.Lerp(color.r, paper.r, amount), Mathf.Lerp(color.g, paper.g, amount), Mathf.Lerp(color.b, paper.b, amount), color.a);
        }

        public static string ColorTag(Color color)
        {
            return "#" + ColorUtility.ToHtmlStringRGB(color);
        }

        /// <summary>The red of a warning inside rich text ("$40 short", "mortgaged").</summary>
        public static string RedTag => ColorTag(Red);

        /// <summary>A player's name in their colour, for rich text.</summary>
        public static string Named(PlayerState player)
        {
            return player == null ? "" : Colored(player, player.name);
        }

        /// <summary>
        /// Whether the player is the seat called "You" (the default name of a lone human player), which changes the
        /// grammar of the texts about them: "You build", "Your turn", "Ada pays you".
        /// </summary>
        public static bool IsYou(PlayerState player)
        {
            return player != null && MatchSetup.IsYou(player.name);
        }

        /// <summary>A present tense verb agreeing with the player: "Ada builds", "You build".</summary>
        public static string Verb(PlayerState player, string thirdPerson)
        {
            if (!IsYou(player) || string.IsNullOrEmpty(thirdPerson))
            {
                return thirdPerson;
            }
            switch (thirdPerson)
            {
                case "is": return "are";
                case "has": return "have";
                case "was": return "were";
                case "does": return "do";
                case "goes": return "go";
                default: return thirdPerson.EndsWith("s") ? thirdPerson.Substring(0, thirdPerson.Length - 1) : thirdPerson;
            }
        }

        /// <summary>The player's name in their colour as the object of a sentence ("Ada pays you").</summary>
        public static string NamedObject(PlayerState player)
        {
            return IsYou(player) ? Colored(player, "you") : Named(player);
        }

        /// <summary>"Ada's" or "Your", in the player's colour.</summary>
        public static string NamedPossessive(PlayerState player, bool capital = true)
        {
            return IsYou(player) ? Colored(player, capital ? "Your" : "your") : Named(player) + "'s";
        }

        /// <summary>"Ada's" or "Your".</summary>
        public static string Possessive(PlayerState player, bool capital = true)
        {
            return IsYou(player) ? capital ? "Your" : "your" : player.name + "'s";
        }

        private static string Colored(PlayerState player, string text)
        {
            return $"<color={ColorTag(Shade(PlayerColor(player.color), 0.85f))}>{text}</color>";
        }
    }

    /// <summary>Codepoints of the Font Awesome Free solid icons the game uses (the icon font asset holds only these).</summary>
    public static class Icons
    {
        public const string Train = "\uF238";
        public const string Bulb = "\uF0EB";
        public const string Faucet = "\uE006";
        public const string Car = "\uF1B9";
        public const string Question = "?";
        public const string Gem = "\uF3A5";
        public const string MoneyBag = "\uF81D";
        public const string Coins = "\uF51E";
        public const string Gavel = "\uF0E3";
        public const string House = "\uF015";
        public const string Hotel = "\uF594";
        public const string Dice = "\uF522";
        public const string Handshake = "\uF2B5";
        public const string Crown = "\uF521";
        public const string Trophy = "\uF091";
        public const string Star = "\uF005";
        public const string Pause = "\uF04C";
        public const string Play = "\uF04B";
        public const string Gear = "\uF013";
        public const string SoundOn = "\uF028";
        public const string SoundOff = "\uF6A9";
        public const string Robot = "\uF544";
        public const string User = "\uF007";
        public const string Bus = "\uF207";
        public const string Plane = "\uF072";
        public const string Globe = "\uF0AC";
        public const string Lock = "\uF023";
        public const string Handcuffs = "\uE4F8";
        public const string Ticket = "\uF145";
        public const string Close = "\uF00D";
        public const string Check = "\uF00C";
        public const string Plus = "+";
        public const string Minus = "\uF068";
        public const string Left = "\uF053";
        public const string Right = "\uF054";
        public const string ArrowLeft = "\uF177";
        public const string Info = "\uF05A";
        public const string Officer = "\uE54A";
        public const string Building = "\uF1AD";
        public const string Chest = "\uF49E";
        public const string Bolt = "\uF0E7";
        public const string Hat = "\uF6E8";
        public const string Forward = "\uF04E";
        public const string Flag = "\uF11E";
        public const string Clock = "\uF017";
        public const string Gift = "\uF06B";
        public const string Undo = "\uF2EA";
        public const string Home = "\uF015";
        public const string Exit = "\uF2F5";
        public const string Save = "\uF0C7";
        public const string Folder = "\uF07C";
        public const string Bank = "\uF19C";
        public const string Percent = "%";
        public const string Mortgage = "\uF53D";
        public const string Hourglass = "\uF252";
        public const string Fire = "\uF06D";
        public const string Party = "\uF79F";
        public const string Music = "\uF001";
        public const string Square = "\uF0C8";
        public const string Circle = "\uF111";
        public const string Palette = "\uF53F";

        /// <summary>Every icon above, for baking the icon font asset.</summary>
        public const string All = Train + Bulb + Faucet + Car + Question + Gem + MoneyBag + Coins + Gavel + House + Hotel + Dice +
            Handshake + Crown + Trophy + Star + Pause + Play + Gear + SoundOn + SoundOff + Robot + User + Bus + Plane + Globe + Lock +
            Handcuffs + Ticket + Close + Check + Plus + Minus + Left + Right + ArrowLeft + Info + Officer + Building + Chest + Bolt +
            Hat + Forward + Flag + Clock + Gift + Undo + Exit + Save + Folder + Bank + Percent + Mortgage + Hourglass + Fire + Party +
            Square + Circle + Music + Palette +
            "$0123456789";

        public static string ForSpace(SpaceData space)
        {
            switch (space.kind)
            {
                case SpaceKind.Railroad: return Train;
                case SpaceKind.Utility: return space.name.Contains("Water") ? Faucet : Bulb;
                case SpaceKind.Chance: return Question;
                case SpaceKind.CommunityChest: return Chest;
                case SpaceKind.Tax: return space.tax >= 200 ? MoneyBag : Gem;
                case SpaceKind.FreeParking: return Car;
                case SpaceKind.GoToJail: return Officer;
                case SpaceKind.Jail: return Lock;
                case SpaceKind.Go: return ArrowLeft;
                default: return House;
            }
        }
    }
}
