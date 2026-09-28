using System;
using System.Collections.Generic;
using Gamebox;
using UnityEngine;

namespace Portfolio.MemoryCards
{
    /// <summary>
    /// The look of Memory Cards in one asset: the worlds of the campaign (their colours, card backs, mascots, backdrop
    /// shapes and the faces dealt there), the whole deck, the special cards, the card sprites, the interface kit, the
    /// icons, the fonts with their text materials, the palette, the backdrop, the particles and the title screen. The
    /// game reads every visual through the active theme (<see cref="GameThemes"/>, <see cref="MemoryCardsGameManager"/>)
    /// and the themed parts of the scene ask by field path ("kit.primary", "icons.play", "palette.ink"). A theme brings
    /// its own worlds and deck: a saved game stores the index of every face in <see cref="Faces"/>, so a game saved
    /// under one theme shows the other theme's face of the same index when it is loaded there.
    /// </summary>
    [CreateAssetMenu(fileName = "MemoryCardsTheme", menuName = "Memory Cards/Theme", order = 5)]
    public class MemoryCardsTheme : GameTheme
    {
        /// <summary>Faces a deck needs for the largest campaign board (28 cards in pairs).</summary>
        public const int MinimumFaces = 14;

        /// <summary>The faces of the special cards.</summary>
        [Serializable]
        public class SpecialFaces
        {
            public Sprite wild;
            public Sprite bomb;
            public Sprite clock;
            public Sprite peek;

            /// <summary>The face of a special card; null for an ordinary card.</summary>
            public Sprite Of(CardKind kind)
            {
                switch (kind)
                {
                    case CardKind.Wild: return wild;
                    case CardKind.Bomb: return bomb;
                    case CardKind.Clock: return clock;
                    case CardKind.Peek: return peek;
                    default: return null;
                }
            }
        }


        /// <summary>The parts of a card that every world shares.</summary>
        [Serializable]
        public class CardLook
        {
            public Sprite front;
            public Sprite shadow;
            [Tooltip("The glow hugging a card, tinted at run time.")]
            public Sprite glow;
            [Tooltip("The badge of a matched card.")]
            public Sprite badge;
            public Sprite ice;
            public Sprite crackedIce;
            [Tooltip("The back of the cards of a level without a world.")]
            public Sprite defaultBack;
        }


        /// <summary>The sprites of the interface: buttons in five roles, panels, pills and the input field.</summary>
        [Serializable]
        public class InterfaceKit
        {
            [Tooltip("The button of the main action of a screen (play, next, save).")]
            public Sprite primary;
            [Tooltip("The button of the other actions (players, load, settings).")]
            public Sprite secondary;
            [Tooltip("The highlighted button (continue, restart, retry).")]
            public Sprite accent;
            [Tooltip("The button that leaves (quit, exit).")]
            public Sprite danger;
            [Tooltip("The quiet button (levels, back).")]
            public Sprite neutral;
            public Sprite roundPrimary;
            public Sprite roundSecondary;
            public Sprite roundAccent;
            public Sprite roundDanger;
            public Sprite roundNeutral;
            public Sprite inputField;
            [Tooltip("A flat rounded panel, nine-sliced.")]
            public Sprite panel;
            [Tooltip("A rounded panel with a lip at the bottom, nine-sliced.")]
            public Sprite panelDepth;
            public Sprite pill;
            public Sprite disc;
            [Tooltip("A soft round glow.")]
            public Sprite soft;
            [Tooltip("A white panel with a lip that the game tints (world tabs, the frame of the seat whose turn it is).")]
            public Sprite tintPanel;
            [Tooltip("A white pill that the game tints (headers, ribbons, badges, bars).")]
            public Sprite tintPill;
            [Tooltip("A white disc that the game tints (the next face of the parade).")]
            public Sprite tintDisc;
        }


        /// <summary>The interface icons, drawn white and tinted where they are used.</summary>
        [Serializable]
        public class IconSet
        {
            public Sprite pause;
            public Sprite settings;
            public Sprite power;
            public Sprite home;
            public Sprite levels;
            public Sprite padlock;
            public Sprite heart;
            public Sprite clock;
            public Sprite stopwatch;
            public Sprite moves;
            public Sprite check;
            public Sprite cross;
            public Sprite play;
            public Sprite retry;
            public Sprite next;
            public Sprite back;
            public Sprite infinity;
            public Sprite sliders;
            public Sprite flag;
            public Sprite cards;
            [Tooltip("The mark beside a tip.")]
            public Sprite paw;
            public Sprite star;
            public Sprite heartFull;
            public Sprite heartEmpty;
        }


        /// <summary>The stars, goal marks and mode icons of the level select, the HUD and the results.</summary>
        [Serializable]
        public class HudLook
        {
            public Sprite starFull;
            public Sprite starEmpty;
            public Sprite goalDone;
            public Sprite goalMissed;
            [Tooltip("An icon per level mode, in LevelMode order: classic, time attack, survival, move limit, parade, endless, free play.")]
            public List<Sprite> modeIcons = new List<Sprite>();

            public Sprite ModeIcon(LevelMode mode)
            {
                int index = (int)mode;
                return modeIcons != null && index >= 0 && index < modeIcons.Count ? modeIcons[index] : null;
            }
        }


        /// <summary>The title of the level select: its words, their gradient, an ornament and the mascots around it.</summary>
        [Serializable]
        public class TitleLook
        {
            public string words = "MEMORY CARDS";
            public string subtitle = "A critter matching adventure";
            public Color gradientTop = Color.white;
            public Color gradientBottom = new Color(1f, 0.83f, 0.42f);
            [Tooltip("A shape behind the title.")]
            public Sprite ornament;
            public Color ornamentColor = new Color(1f, 1f, 1f, 0.2f);
            [Tooltip("The figures standing around the title: two big ones, then two small ones.")]
            public List<Sprite> mascots = new List<Sprite>();
        }


        /// <summary>The materials the texts are drawn with: an outline, a soft shadow and the heavy title outline.</summary>
        [Serializable]
        public class TextStyles
        {
            [Tooltip("The body font with an outline (labels on coloured buttons, the HUD).")]
            public Material outline;
            [Tooltip("The body font with a soft shadow.")]
            public Material shadow;
            [Tooltip("The title font with its heavy outline.")]
            public Material title;
        }


        /// <summary>The colours of the interface that are not a world's.</summary>
        [Serializable]
        public class Palette
        {
            [Header("Words")]
            [Tooltip("Words on panels.")]
            public Color ink = new Color(0.169f, 0.176f, 0.259f);
            [Tooltip("Quieter words on panels.")]
            public Color softInk = new Color(0.42f, 0.435f, 0.525f);
            [Tooltip("Words on the backdrop and on coloured buttons.")]
            public Color light = Color.white;
            public Color gold = new Color(1f, 0.788f, 0.2f);
            [Tooltip("Words on the primary, secondary and danger buttons.")]
            public Color buttonText = Color.white;
            [Tooltip("Words on the accent buttons.")]
            public Color accentText = new Color(0.169f, 0.176f, 0.259f);
            [Tooltip("Words and icons on the neutral buttons.")]
            public Color neutralText = new Color(0.169f, 0.176f, 0.259f);
            [Tooltip("The words of an empty input field.")]
            public Color placeholder = new Color(0.4f, 0.42f, 0.5f, 0.5f);

            [Header("Panels")]
            [Tooltip("The tint of the panels.")]
            public Color panel = Color.white;
            [Tooltip("The translucent pills of the HUD.")]
            public Color pill = new Color(0.1f, 0.1f, 0.22f, 0.5f);
            [Tooltip("The darkening behind the pause menu and the results.")]
            public Color dim = new Color(0.08f, 0.06f, 0.16f, 0.58f);
            [Tooltip("What the camera clears to, behind the backdrop.")]
            public Color sky = new Color(0.384f, 0.769f, 1f);

            [Header("Headers")]
            public Color detailsHeader = new Color(1f, 0.604f, 0.122f);
            public Color resultsRibbon = new Color(1f, 0.604f, 0.122f);
            public Color pauseRibbon = new Color(0.557f, 0.486f, 1f);
            public Color settingsRibbon = new Color(0.122f, 0.761f, 1f);

            [Header("Play")]
            public Color good = new Color(0.35f, 0.85f, 0.4f);
            public Color bad = new Color(1f, 0.38f, 0.32f);
            public Color matchGlow = new Color(1f, 0.85f, 0.25f);
            public Color mistakeTint = new Color(1f, 0.55f, 0.55f);
            public Color wiltTint = new Color(0.62f, 0.62f, 0.68f);
            [Tooltip("The confetti of a cleared board (a world's accent comes first).")]
            public List<Color> celebration = new List<Color>();
            [Tooltip("The colour of each seat of a versus game.")]
            public List<Color> seats = new List<Color>();
            public Color hurry = new Color(1f, 0.35f, 0.3f);
            [Tooltip("The banner of a wild card.")]
            public Color wild = new Color(0.85f, 0.55f, 1f);
            [Tooltip("The banner of a peek card.")]
            public Color peek = new Color(0.8f, 0.65f, 1f);
            [Tooltip("The combo written under a set.")]
            public Color combo = new Color(1f, 0.55f, 0.85f);

            [Header("Level select")]
            public Color tabText = new Color(0.2f, 0.22f, 0.3f);
            public Color tabTextSelected = Color.white;
            [Tooltip("What the accent of a world blends towards on a tab that is not selected.")]
            public Color tabIdle = Color.white;
            public Color tabLocked = new Color(0.7f, 0.72f, 0.78f);
            public Color cardLocked = new Color(0.62f, 0.62f, 0.68f);
            public Color lockShade = new Color(0.12f, 0.12f, 0.22f, 0.5f);
            public Color lockDisc = new Color(0.15f, 0.16f, 0.26f, 0.85f);

            [Header("Results")]
            [Tooltip("The ribbon of a lost game.")]
            public Color defeat = new Color(0.55f, 0.5f, 0.62f);
            public Color best = new Color(0.788f, 0.416f, 0f);
            public Color goalReached = new Color(0.13f, 0.5f, 0.24f);
            public Color goalMissed = new Color(0.35f, 0.35f, 0.42f, 0.8f);

            [Header("Online")]
            public Color lobbyAccent = new Color(0.239f, 0.545f, 1f);
            public Color lobbyWindow = new Color(0.169f, 0.176f, 0.259f);
            public Color lobbyRow = new Color(0.231f, 0.243f, 0.361f);

            /// <summary>The colour of a seat of a versus game.</summary>
            public Color Seat(int seat, Color fallback)
            {
                return seats != null && seats.Count > 0 ? seats[Mathf.Abs(seat) % seats.Count] : fallback;
            }
        }


        /// <summary>The backdrop behind the board: the scrolling pattern and the glow over it.</summary>
        [Serializable]
        public class BackdropLook
        {
            public Texture2D pattern;
            public Color patternColor = new Color(1f, 1f, 1f, 0.07f);
            public float patternTileSize = 300f;
            public Sprite vignette;
            public Color vignetteColor = new Color(1f, 1f, 1f, 0.12f);
        }


        /// <summary>The shapes of the interface particles.</summary>
        [Serializable]
        public class ParticleLook
        {
            public Sprite spark;
            public Sprite star;
            public Sprite circle;
            public Sprite confetti;
            public Sprite shard;
            [Tooltip("The colours of the burst of a wild card.")]
            public List<Color> wildColors = new List<Color>();
        }


        /// <summary>The words of a level that a theme tells in its own way; an empty text keeps the level's own.</summary>
        [Serializable]
        public class LevelWords
        {
            public string title;
            [TextArea] public string description;
            [TextArea] public string tip;
        }


        /// <summary>What the game calls the faces of the deck, and the words of the levels in the theme's voice.</summary>
        [Serializable]
        public class Words
        {
            [Tooltip("One face of the deck in hints (\"every card of the animal you pair it with\").")]
            public string face = "animal";
            [Tooltip("The faces in hints (\"remember where the animals are\").")]
            public string faces = "animals";
            [Tooltip("What the shuffle banner says.")]
            public string shuffle = "The critters are on the move";
            [Tooltip("The texts of the levels in campaign order; an empty entry keeps the level's own.")]
            public List<LevelWords> levels = new List<LevelWords>();

            /// <summary>The title of the level at <paramref name="index"/> in this theme, or <paramref name="own"/>.</summary>
            public string Title(int index, string own)
            {
                return Pick(Level(index)?.title, own);
            }

            public string Description(int index, string own)
            {
                return Pick(Level(index)?.description, own);
            }

            public string Tip(int index, string own)
            {
                return Pick(Level(index)?.tip, own);
            }

            private LevelWords Level(int index)
            {
                return levels != null && index >= 0 && index < levels.Count ? levels[index] : null;
            }

            private static string Pick(string themed, string own)
            {
                return string.IsNullOrEmpty(themed) ? own : themed;
            }
        }


        [Header("Worlds and deck")]
        [Tooltip("The worlds of the campaign in campaign order; a theme has one for every world of the campaign.")]
        [SerializeField] internal List<CardWorld> worlds = new List<CardWorld>();
        [Tooltip("Every face of the deck; save games refer to faces by their index here.")]
        [SerializeField] internal List<Sprite> faces = new List<Sprite>();
        [SerializeField] internal SpecialFaces special = new SpecialFaces();
        [SerializeField] internal CardLook card = new CardLook();

        [Header("Interface")]
        [SerializeField] internal InterfaceKit kit = new InterfaceKit();
        [SerializeField] internal IconSet icons = new IconSet();
        [SerializeField] internal HudLook hud = new HudLook();
        [SerializeField] internal TitleLook title = new TitleLook();
        [SerializeField] internal TextStyles text = new TextStyles();
        [SerializeField] internal Palette palette = new Palette();
        [SerializeField] internal BackdropLook backdrop = new BackdropLook();
        [SerializeField] internal ParticleLook particles = new ParticleLook();
        [SerializeField] internal Words words = new Words();

        public override GameType Game => GameType.MemoryCards;

        public IReadOnlyList<CardWorld> Worlds => worlds;

        public IReadOnlyList<Sprite> Faces => faces;

        public SpecialFaces Special => special;

        public CardLook Card => card;

        public InterfaceKit Kit => kit;

        public IconSet Icons => icons;

        public HudLook Hud => hud;

        public TitleLook Title => title;

        public TextStyles Text => text;

        public Palette Colors => palette;

        public BackdropLook Backdrop => backdrop;

        public ParticleLook Particles => particles;

        public Words Say => words;


        /// <summary>The world at <paramref name="index"/> of the campaign, or null when the theme has none there.</summary>
        public CardWorld World(int index)
        {
            return worlds != null && index >= 0 && index < worlds.Count ? worlds[index] : null;
        }


        /// <summary>The faces dealt in the world at <paramref name="index"/>: the world's own, else the whole deck.</summary>
        public IReadOnlyList<Sprite> FacesOf(int index)
        {
            CardWorld world = World(index);
            return world != null && world.animals != null && world.animals.Count > 0 ? world.animals : faces;
        }


        /// <summary>Index of <paramref name="face"/> in the deck, or -1.</summary>
        public int IndexOf(Sprite face)
        {
            return face != null && faces != null ? faces.IndexOf(face) : -1;
        }


        /// <summary>
        /// On top of the empty references the base check finds: the worlds, the deck and the mode icons the game needs.
        /// </summary>
        public override bool Validate(List<string> problems)
        {
            int before = problems.Count;
            base.Validate(problems);
            if (worlds == null || worlds.Count == 0)
            {
                problems.Add($"{name}.worlds has no world");
            }
            else
            {
                for (int i = 0; i < worlds.Count; i++)
                {
                    if (worlds[i] != null && (worlds[i].animals == null || worlds[i].animals.Count == 0))
                    {
                        problems.Add($"{name}.worlds[{i}] ({worlds[i].displayName}) deals no faces");
                    }
                }
            }
            if (faces == null || faces.Count < MinimumFaces)
            {
                problems.Add($"{name}.faces holds {(faces != null ? faces.Count : 0)} faces; the campaign needs {MinimumFaces}");
            }
            int modes = Enum.GetValues(typeof(LevelMode)).Length;
            if (hud == null || hud.modeIcons == null || hud.modeIcons.Count != modes)
            {
                problems.Add($"{name}.hud.modeIcons needs one icon per level mode ({modes})");
            }
            if (title == null || title.mascots == null || title.mascots.Count == 0)
            {
                problems.Add($"{name}.title.mascots has no figure");
            }
            return problems.Count == before;
        }
    }
}
