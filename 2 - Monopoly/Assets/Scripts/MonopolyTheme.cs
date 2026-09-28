using System;
using System.Collections.Generic;
using System.Globalization;
using Gamebox;
using TMPro;
using UnityEngine;

namespace Portfolio.Monopoly
{
    /// <summary>The named colours the board, the tokens and the interface are drawn with (<see cref="MonopolyStyle"/>).</summary>
    [Serializable]
    public class MonopolyPalette
    {
        public Color red = MonopolyStyle.Defaults.Red;
        public Color darkRed = MonopolyStyle.Defaults.DarkRed;
        [Tooltip("The words on paper.")]
        public Color ink = MonopolyStyle.Defaults.Ink;
        [Tooltip("The dark plate behind white words: the band of a station's deed, the computer tag, the jail banner.")]
        public Color plate = MonopolyStyle.Defaults.Ink;
        [Tooltip("The faces of the cards and popups; a translucent colour makes them glassy.")]
        public Color paper = MonopolyStyle.Defaults.Paper;
        [Tooltip("Boxes and rows on the paper.")]
        public Color panel = MonopolyStyle.Defaults.Panel;
        [Tooltip("The quiet buttons (Back, Cancel, the round icon buttons).")]
        public Color soft = MonopolyStyle.Defaults.Soft;
        [Tooltip("The field colour of the board.")]
        public Color mint = MonopolyStyle.Defaults.Mint;
        public Color green = MonopolyStyle.Defaults.Green;
        public Color blue = MonopolyStyle.Defaults.Blue;
        public Color gold = MonopolyStyle.Defaults.Gold;
        [Tooltip("Secondary words and lines.")]
        public Color muted = MonopolyStyle.Defaults.Muted;
        public Color chanceOrange = MonopolyStyle.Defaults.ChanceOrange;
        public Color chestBlue = MonopolyStyle.Defaults.ChestBlue;
        [Tooltip("The drop shadow (or halo) under the cards.")]
        public Color shadow = MonopolyStyle.Defaults.Shadow;
    }


    /// <summary>The colours of what is printed on the board surface (names, prices, icons, the logo).</summary>
    [Serializable]
    public class PrintLook
    {
        public Color ink = MonopolyStyle.Defaults.Ink;
        [Tooltip("The city under a street's name.")]
        public Color city = MonopolyStyle.Defaults.PrintCity;
        public Color utilityIcon = MonopolyStyle.Defaults.PrintUtility;
        public Color taxIcon = MonopolyStyle.Defaults.PrintTax;
        [Tooltip("GO, its arrow and the Free Parking car.")]
        public Color go = MonopolyStyle.Defaults.Red;
        [Tooltip("The officer of Go To Jail.")]
        public Color officer = MonopolyStyle.Defaults.Blue;
        public Color chanceMark = MonopolyStyle.Defaults.ChanceOrange;
        public Color chestIcon = MonopolyStyle.Defaults.ChestBlue;
        [Tooltip("The MORTGAGED stamp on a space.")]
        public Color mortgaged = MonopolyStyle.Defaults.PrintMortgaged;
        [Tooltip("The words on the two decks.")]
        public Color deckWords = Color.white;
        [Tooltip("The name of the game across the middle of the board.")]
        public Color logoWord = Color.white;
        [Tooltip("The edition line under the logo.")]
        public Color edition = MonopolyStyle.Defaults.Ink;
    }


    /// <summary>The look of one colour set of properties.</summary>
    [Serializable]
    public class GroupLook
    {
        public string name;
        public Color color = Color.white;
        [Tooltip("Words that read on top of the set's colour.")]
        public Color textColor = Color.white;
    }


    /// <summary>The look of one seat: its colour, the base its token stands on and the ring around it on its turn.</summary>
    [Serializable]
    public class SeatLook
    {
        public string name;
        public Color color = Color.white;
        [Tooltip("The token base and the owner tags of the seat (a lit material in the seat's colour).")]
        public Material baseMaterial;
        [Tooltip("The glowing ring around the token while it is the seat's turn.")]
        public Material ringMaterial;
    }


    /// <summary>One of the eight tokens: its name, its figure, its badge in the interface and the colour of the figure.</summary>
    [Serializable]
    public class TokenLook
    {
        public string name;
        public Mesh mesh;
        [Tooltip("A white silhouette of the figure for the interface badges.")]
        public Sprite badge;
        [Tooltip("The base colour of the figure (the token material is tinted with it).")]
        public Color tint = Color.white;
    }


    /// <summary>The generic shapes the interface is built from.</summary>
    [Serializable]
    public class UIKitSprites
    {
        [Tooltip("A nine-sliced rounded rectangle: cards, buttons, bars and chips.")]
        public Sprite rounded;
        public Sprite circle;
        public Sprite ring;
        [Tooltip("A radial glow.")]
        public Sprite glow;
        [Tooltip("The nine-sliced drop shadow under the cards.")]
        public Sprite shadow;
        [Tooltip("The gloss on top of the buttons.")]
        public Sprite sheen;
        [Tooltip("The banknote that flies between the panels.")]
        public Sprite bill;
        [Tooltip("The banner of the logo on the title screen.")]
        public Sprite logo;
    }


    /// <summary>What the scene around the board looks like: the sky, the lights and the confetti.</summary>
    [Serializable]
    public class SceneLook
    {
        [Tooltip("The camera background and the fog in the distance.")]
        public Color background = MonopolyStyle.Defaults.Background;
        public Color ambientSky = MonopolyStyle.Defaults.AmbientSky;
        public Color ambientEquator = MonopolyStyle.Defaults.AmbientEquator;
        public Color ambientGround = MonopolyStyle.Defaults.AmbientGround;
        public Color sunColor = new Color(1f, 0.96f, 0.9f);
        public float sunIntensity = 0.9f;
        public Color fillColor = new Color(0.8f, 0.87f, 1f);
        public float fillIntensity = 0.22f;
        [Tooltip("What the metal of the tokens reflects.")]
        public Cubemap reflection;
        public float reflectionIntensity = 0.85f;
        [Tooltip("The colours of the confetti of a win.")]
        public List<Color> confetti = new List<Color>();
    }


    /// <summary>
    /// The whole look of the Monopoly game in one asset: the board and the table, the cards, the tokens, the houses and
    /// hotels, the dice, the shapes and fonts of the interface, the palette and the lights. The scene reads it through
    /// <see cref="MonopolyStyle"/> (the colours and names) and through the themed parts the builders put on the
    /// objects (<see cref="ThemedLook"/> and the base themed components by key), so pointing the game's definition at
    /// another theme changes everything the player sees. "Classic" is the original look; the content builder makes both
    /// themes from the art the builders generate.
    /// </summary>
    [CreateAssetMenu(fileName = "MonopolyTheme", menuName = "Monopoly/Theme", order = 3)]
    public class MonopolyTheme : GameTheme
    {
        [Header("Words")]
        [Tooltip("The line under the logo (WORLD TOUR EDITION).")]
        public string editionLabel = "WORLD TOUR EDITION";

        [Header("Colours")]
        public MonopolyPalette palette = new MonopolyPalette();
        public PrintLook print = new PrintLook();
        [Tooltip("The colour sets, in board order (brown to dark blue, then the stations and the utilities).")]
        public List<GroupLook> groups = new List<GroupLook>();
        [Tooltip("The four seats, in seat order.")]
        public List<SeatLook> seats = new List<SeatLook>();

        [Header("Board and table")]
        public Material boardMaterial;
        public Material boardSlabMaterial;
        public Material tableMaterial;
        public Texture2D boardTexture;
        public Texture2D tableTexture;
        public Mesh boardTopMesh;
        public Mesh boardSlabMesh;
        public Mesh tableMesh;
        [Tooltip("The glowing frame around a highlighted space.")]
        public Material highlightMaterial;
        [Tooltip("The shade over a mortgaged space.")]
        public Material mortgagedMaterial;
        [Tooltip("The soft shadow under tokens and dice.")]
        public Material contactShadowMaterial;
        [Tooltip("The plate along the outer edge of an owned space.")]
        public Mesh ownerTagMesh;

        [Header("Cards")]
        [Tooltip("The printed top of the Chance deck.")]
        public Material chanceBackMaterial;
        public Material chestBackMaterial;
        [Tooltip("The white block of a deck.")]
        public Material cardEdgeMaterial;
        public Texture2D chanceBackTexture;
        public Texture2D chestBackTexture;
        public Mesh cardStackMesh;

        [Header("Logo")]
        public Material logoMaterial;
        public Texture2D logoTexture;

        [Header("Tokens")]
        [Tooltip("The eight tokens, in token index order.")]
        public List<TokenLook> tokens = new List<TokenLook>();
        [Tooltip("The metal of the figures.")]
        public Material tokenMaterial;
        [Tooltip("How much the figures light up in their own tint (0 for plain metal figures).")]
        [Range(0f, 1f)] public float tokenGlow;
        public Mesh tokenBaseMesh;
        public Mesh turnRingMesh;

        [Header("Buildings")]
        public Mesh houseMesh;
        public Mesh hotelMesh;
        public Material houseMaterial;
        public Material hotelMaterial;

        [Header("Dice")]
        public Mesh dieMesh;
        public Material dieMaterial;
        public Material speedDieMaterial;
        public Texture2D diceTexture;
        public Texture2D speedDieTexture;

        [Header("Interface")]
        public UIKitSprites ui = new UIKitSprites();
        [Tooltip("Body text.")]
        public TMP_FontAsset bodyFont;
        [Tooltip("Names and values.")]
        public TMP_FontAsset boldFont;
        [Tooltip("Prices, buttons and headings.")]
        public TMP_FontAsset heavyFont;
        [Tooltip("The big words: the logo, banners, flying money.")]
        public TMP_FontAsset titleFont;
        [Tooltip("The icons (Font Awesome).")]
        public TMP_FontAsset iconFont;
        [Tooltip("A material of the bold font with a soft shadow, for words over the board.")]
        public Material textShadowMaterial;
        [Tooltip("A material of the title font with an outline and a shadow.")]
        public Material titleMaterial;

        [Header("Scene")]
        public SceneLook scene = new SceneLook();

        public override GameType Game => GameType.Monopoly;

        /// <summary>The look of a colour set, or null when the theme does not list it.</summary>
        public GroupLook Group(ColorGroup group)
        {
            int index = Array.IndexOf(MonopolyStyle.Groups, group);
            return index >= 0 && index < groups.Count ? groups[index] : null;
        }

        public SeatLook Seat(int seat)
        {
            return seats.Count > 0 ? seats[((seat % seats.Count) + seats.Count) % seats.Count] : null;
        }

        public TokenLook Token(int index)
        {
            return index >= 0 && index < tokens.Count ? tokens[index] : null;
        }

        /// <summary>A text of the theme by key ("edition"), or null.</summary>
        public string WordOf(string key)
        {
            switch (key)
            {
                case "edition": return editionLabel;
                default: return null;
            }
        }

        /// <summary>
        /// A colour by expression: a key of a colour of the theme ("palette.ink", "groups.2.color"), optionally
        /// followed by <c>*f</c> (multiplied by <c>f</c>: a shade), <c>~t</c> (blended <c>t</c> of the way to the paper
        /// colour: a tint) and <c>@a</c> (an alpha), e.g. "palette.muted~0.6@0.8". <c>on:</c> in front asks for the
        /// colour of words on a face of that colour (<see cref="TextOn"/>), e.g. "on:palette.green". False when the key
        /// is unknown.
        /// </summary>
        public bool TryResolveColor(string expression, out Color color)
        {
            color = default;
            if (string.IsNullOrEmpty(expression))
            {
                return false;
            }
            if (expression.StartsWith(OnPrefix, StringComparison.Ordinal))
            {
                if (!TryResolveColor(expression.Substring(OnPrefix.Length), out Color face))
                {
                    return false;
                }
                color = TextOn(face);
                return true;
            }
            int end = expression.IndexOfAny(Operators);
            string key = end < 0 ? expression : expression.Substring(0, end);
            if (!TryColor(key.Trim(), out color))
            {
                return false;
            }
            while (end >= 0 && end < expression.Length)
            {
                char op = expression[end];
                int next = expression.IndexOfAny(Operators, end + 1);
                string number = next < 0 ? expression.Substring(end + 1) : expression.Substring(end + 1, next - end - 1);
                if (!float.TryParse(number, NumberStyles.Float, CultureInfo.InvariantCulture, out float amount))
                {
                    return false;
                }
                switch (op)
                {
                    case '*': color = MonopolyStyle.Shade(color, amount); break;
                    case '~': color = new Color(Mathf.Lerp(color.r, palette.paper.r, amount), Mathf.Lerp(color.g, palette.paper.g, amount), Mathf.Lerp(color.b, palette.paper.b, amount), color.a); break;
                    case '@': color.a = amount; break;
                }
                end = next;
            }
            return true;
        }

        public Color ResolveColor(string expression, Color fallback)
        {
            return TryResolveColor(expression, out Color color) ? color : fallback;
        }

        /// <summary>
        /// The colour of words on a face of <paramref name="face"/>: the ink on the paper and the boxes on it, the
        /// darker of the ink and the paper on light faces, white on the others.
        /// </summary>
        public Color TextOn(Color face)
        {
            return TextOn(face, palette.ink, palette.paper, palette.panel, palette.soft);
        }

        /// <summary><see cref="TextOn(Color)"/> for a palette given by its colours (the classic one without a theme).</summary>
        public static Color TextOn(Color face, Color ink, Color paper, Color panel, Color soft)
        {
            if (SameHue(face, paper) || SameHue(face, panel) || SameHue(face, soft))
            {
                return ink;
            }
            if (face.grayscale > 0.72f)
            {
                Color dark = ink.grayscale < paper.grayscale ? ink : paper;
                dark.a = 1f;
                return dark;
            }
            return Color.white;
        }

        private static bool SameHue(Color a, Color b)
        {
            return Mathf.Abs(a.r - b.r) < 0.01f && Mathf.Abs(a.g - b.g) < 0.01f && Mathf.Abs(a.b - b.b) < 0.01f;
        }

        public const string OnPrefix = "on:";

        private static readonly char[] Operators = { '*', '~', '@' };

        public override bool Validate(List<string> problems)
        {
            bool complete = base.Validate(problems);
            if (groups.Count != MonopolyStyle.Groups.Length)
            {
                problems.Add($"{name}.groups lists {groups.Count} colour sets, the board has {MonopolyStyle.Groups.Length}");
                complete = false;
            }
            if (seats.Count != MonopolyStyle.Defaults.PlayerColors.Length)
            {
                problems.Add($"{name}.seats lists {seats.Count} seats, the game has {MonopolyStyle.Defaults.PlayerColors.Length}");
                complete = false;
            }
            if (tokens.Count != MonopolyStyle.Defaults.TokenNames.Length)
            {
                problems.Add($"{name}.tokens lists {tokens.Count} tokens, the game has {MonopolyStyle.Defaults.TokenNames.Length}");
                complete = false;
            }
            for (int i = 0; i < tokens.Count; i++)
            {
                if (tokens[i] != null && string.IsNullOrEmpty(tokens[i].name))
                {
                    problems.Add($"{name}.tokens[{i}] has no name");
                    complete = false;
                }
            }
            for (int i = 0; i < seats.Count; i++)
            {
                if (seats[i] != null && string.IsNullOrEmpty(seats[i].name))
                {
                    problems.Add($"{name}.seats[{i}] has no name");
                    complete = false;
                }
            }
            if (string.IsNullOrEmpty(editionLabel))
            {
                problems.Add($"{name}.editionLabel is empty");
                complete = false;
            }
            return complete;
        }
    }
}
