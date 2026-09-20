using System.Globalization;
using UnityEngine;

namespace Portfolio.Monopoly
{
    /// <summary>The colours, names and number formats the board, the tokens and the interface share.</summary>
    public static class MonopolyStyle
    {
        public static readonly Color Red = Hex(0xE4002B);
        public static readonly Color DarkRed = Hex(0xA8001F);
        public static readonly Color Ink = Hex(0x1B2330);
        public static readonly Color Paper = Hex(0xFFFFFF);
        public static readonly Color Mint = Hex(0xD4EAD5);
        public static readonly Color Green = Hex(0x17A34A);
        public static readonly Color Blue = Hex(0x1D6FD8);
        public static readonly Color Gold = Hex(0xF5B301);
        public static readonly Color Muted = Hex(0x8C96A5);
        public static readonly Color Panel = Hex(0xF7F8FA);
        public static readonly Color ChanceOrange = Hex(0xF58220);
        public static readonly Color ChestBlue = Hex(0x1F8ACB);

        /// <summary>The seat colours, in seat order.</summary>
        public static readonly Color[] PlayerColors = { Hex(0xE53935), Hex(0x1E88E5), Hex(0x2EAA4F), Hex(0xF9A825) };

        public static readonly string[] PlayerColorNames = { "Red", "Blue", "Green", "Yellow" };

        /// <summary>The token lineup, in token index order (the meshes and badges follow it).</summary>
        public static readonly string[] TokenNames = { "Race Car", "Top Hat", "Scottie Dog", "Battleship", "Cat", "Rubber Duck", "Penguin", "T-Rex" };

        public static int TokenCount => TokenNames.Length;

        public static Color PlayerColor(int seat)
        {
            return PlayerColors[((seat % PlayerColors.Length) + PlayerColors.Length) % PlayerColors.Length];
        }

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

        /// <summary>The sets in board order, for rows of set chips.</summary>
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

        public static Color WithAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }

        public static Color Shade(Color color, float factor)
        {
            return new Color(color.r * factor, color.g * factor, color.b * factor, color.a);
        }

        public static Color Tint(Color color, float amount)
        {
            return Color.Lerp(color, Color.white, amount);
        }

        public static string ColorTag(Color color)
        {
            return "#" + ColorUtility.ToHtmlStringRGB(color);
        }

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
        public const string Train = "";
        public const string Bulb = "";
        public const string Faucet = "";
        public const string Car = "";
        public const string Question = "?";
        public const string Gem = "";
        public const string MoneyBag = "";
        public const string Coins = "";
        public const string Gavel = "";
        public const string House = "";
        public const string Hotel = "";
        public const string Dice = "";
        public const string Handshake = "";
        public const string Crown = "";
        public const string Trophy = "";
        public const string Star = "";
        public const string Pause = "";
        public const string Play = "";
        public const string Gear = "";
        public const string SoundOn = "";
        public const string SoundOff = "";
        public const string Robot = "";
        public const string User = "";
        public const string Bus = "";
        public const string Plane = "";
        public const string Globe = "";
        public const string Lock = "";
        public const string Handcuffs = "";
        public const string Ticket = "";
        public const string Close = "";
        public const string Check = "";
        public const string Plus = "+";
        public const string Minus = "";
        public const string Left = "";
        public const string Right = "";
        public const string ArrowLeft = "";
        public const string Info = "";
        public const string Officer = "";
        public const string Building = "";
        public const string Chest = "";
        public const string Bolt = "";
        public const string Hat = "";
        public const string Forward = "";
        public const string Flag = "";
        public const string Clock = "";
        public const string Gift = "";
        public const string Undo = "";
        public const string Home = "";
        public const string Exit = "";
        public const string Save = "";
        public const string Folder = "";
        public const string Bank = "";
        public const string Percent = "%";
        public const string Mortgage = "";
        public const string Hourglass = "";
        public const string Fire = "";
        public const string Party = "";
        public const string Music = "";
        public const string Square = "";
        public const string Circle = "";

        /// <summary>Every icon above, for baking the icon font asset.</summary>
        public const string All = Train + Bulb + Faucet + Car + Question + Gem + MoneyBag + Coins + Gavel + House + Hotel + Dice +
            Handshake + Crown + Trophy + Star + Pause + Play + Gear + SoundOn + SoundOff + Robot + User + Bus + Plane + Globe + Lock +
            Handcuffs + Ticket + Close + Check + Plus + Minus + Left + Right + ArrowLeft + Info + Officer + Building + Chest + Bolt +
            Hat + Forward + Flag + Clock + Gift + Undo + Exit + Save + Folder + Bank + Percent + Mortgage + Hourglass + Fire + Party +
            Square + Circle + Music +
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
