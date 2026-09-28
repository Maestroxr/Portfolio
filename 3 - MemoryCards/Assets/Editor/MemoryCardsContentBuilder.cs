using System.Collections.Generic;
using System.Linq;
using Gamebox;
using Gamebox.Launcher;
using UnityEditor;
using UnityEngine;

namespace Portfolio.MemoryCards.EditorTools
{
    /// <summary>
    /// Builds the content of Memory Cards: the worlds of every theme, the rules of every level, the level assets, the
    /// campaign, the free play settings, the theme assets (<see cref="MemoryCardsTheme"/>, one per
    /// <see cref="ThemeSpecs"/> entry, gathering the art the art builder drew) and the launcher entry with its themes.
    /// Values that belong to the content (board sizes, twists, goals, texts) live in <see cref="Levels"/>, so hand
    /// edits of those assets are overwritten by a rebuild.
    /// </summary>
    internal static class MemoryCardsContentBuilder
    {
        public const string CampaignPath = "Settings/Campaign/MemoryCardsCampaign.asset";
        public const string DefaultSettingsPath = "Settings/DefaultGameSettings.asset";
        public const string CustomSettingsPath = "Settings/CustomSettings.asset";
        public const string CurrentSettingsPath = "Settings/CurrentGameSettings.asset";
        public const string GameDefinitionPath = "Resources/Games/MemoryCards.asset";

        /// <summary>One level of the campaign as the generator sees it.</summary>
        private sealed class LevelSpec
        {
            public string Title;
            public int World;
            public string Description;
            public string Introduces;
            public string Tip;
            public int Cards;
            public int Columns;
            public int Match = 2;
            public float Time;
            public float Unflip = 0.9f;
            public float Preview;
            public int Hearts;
            public int Moves;
            public float MatchBonus;
            public int ShuffleEvery;
            public bool Parade;
            public int Bombs;
            public int Wilds;
            public int Clocks;
            public int Peeks;
            public int Frozen;
            public int MaxMistakes = 4;
            public StarGoal Third = StarGoal.Time;
            public float ThirdTarget = 60f;
        }

        private static readonly LevelSpec[] Levels =
        {
            // Sunny Farm: the basics, one idea per level.
            new LevelSpec
            {
                Title = "Hello, Farm!", World = 0, Cards = 12, Columns = 4, Preview = 2f, Unflip = 1f,
                Description = "Flip two cards at a time and find the matching pairs of farm animals.",
                Tip = "Click a card to flip it, then find its twin!", MaxMistakes = 4, Third = StarGoal.Time, ThirdTarget = 45f
            },
            new LevelSpec
            {
                Title = "Barnyard Buddies", World = 0, Cards = 16, Columns = 4, Introduces = "Combos",
                Description = "Find pairs back to back to build a combo worth up to x5 points!",
                Tip = "Matches in a row multiply your points.", MaxMistakes = 6, Third = StarGoal.Combo, ThirdTarget = 3f
            },
            new LevelSpec
            {
                Title = "Peek-a-Boo", World = 0, Cards = 20, Columns = 5, Preview = 4f, Introduces = "Memorize",
                Description = "Every card shows its face before the round begins. Remember as many as you can!",
                Tip = "Watch closely while the cards are face up.", MaxMistakes = 5, Third = StarGoal.Time, ThirdTarget = 70f
            },
            new LevelSpec
            {
                Title = "Race the Rooster", World = 0, Cards = 16, Columns = 4, Time = 50f, MatchBonus = 2f, Introduces = "Time attack",
                Description = "Beat the clock before the rooster crows! Every pair you find adds two seconds.",
                Tip = "The clock is ticking - every match buys you time.", MaxMistakes = 6, Third = StarGoal.Time, ThirdTarget = 20f
            },
            new LevelSpec
            {
                Title = "Tick-Tock Barn", World = 0, Cards = 20, Columns = 5, Time = 45f, MatchBonus = 1.5f, Clocks = 2, Introduces = "Clock cards",
                Description = "Clock cards hide among the animals. Flip one for six extra seconds!",
                Tip = "Clock cards give you extra time.", MaxMistakes = 6, Third = StarGoal.Time, ThirdTarget = 15f
            },
            new LevelSpec
            {
                Title = "Hen Party", World = 0, Cards = 18, Columns = 6, Hearts = 5, Preview = 3f, Introduces = "Survival",
                Description = "Five hearts, and every mistake costs one. Can you keep them all?",
                Tip = "Every mistake costs a heart - think before you flip!", MaxMistakes = 2, Third = StarGoal.Moves, ThirdTarget = 13f
            },

            // Jungle Jam: special cards and new ways to match.
            new LevelSpec
            {
                Title = "Welcome to the Jungle", World = 1, Cards = 25, Columns = 5, Wilds = 1, Preview = 2f, Introduces = "Wild card",
                Description = "The rainbow wild card matches any animal - and finds its twin for you!",
                Tip = "Flip the wild card together with any animal to match all of its kind.", MaxMistakes = 8, Third = StarGoal.Time, ThirdTarget = 100f
            },
            new LevelSpec
            {
                Title = "Monkey Business", World = 1, Cards = 20, Columns = 5, ShuffleEvery = 3, Preview = 2f, Introduces = "Shuffle",
                Description = "Cheeky monkeys! After every third mistake the hidden cards swap places.",
                Tip = "Three mistakes and the monkeys shuffle the hidden cards!", MaxMistakes = 6, Third = StarGoal.Combo, ThirdTarget = 3f
            },
            new LevelSpec
            {
                Title = "Snake Pit", World = 1, Cards = 24, Columns = 6, Bombs = 2, Hearts = 4, Introduces = "Bombs",
                Description = "Two bombs hide in the pit. Each one costs a heart - remember where they are!",
                Tip = "Bombs cost a heart. Once you have seen one, stay away from it!", MaxMistakes = 3, Third = StarGoal.Time, ThirdTarget = 110f
            },
            new LevelSpec
            {
                Title = "Parrot Parade", World = 1, Cards = 20, Columns = 5, Parade = true, Preview = 3f, Introduces = "Parade",
                Description = "The parrots want a parade! Match the animals in the order shown at the top.",
                Tip = "Only the animal the parade asks for counts. Remember the others for later!", MaxMistakes = 8, Third = StarGoal.Moves, ThirdTarget = 22f
            },
            new LevelSpec
            {
                Title = "Triple Trouble", World = 1, Cards = 18, Columns = 6, Match = 3, Preview = 2f, Introduces = "Triplets",
                Description = "Everything comes in threes here. Find three of a kind to make a match.",
                Tip = "Flip three matching cards to make a set.", MaxMistakes = 6, Third = StarGoal.Time, ThirdTarget = 80f
            },
            new LevelSpec
            {
                Title = "Jungle Rumble", World = 1, Cards = 24, Columns = 6, Time = 75f, MatchBonus = 2f, Bombs = 2, Clocks = 1, Wilds = 1, ShuffleEvery = 4,
                Description = "Bombs, a wild card, a clock and shuffles, all against the clock. Rumble in the jungle!",
                Tip = "Use the wild card on the animal you cannot find.", MaxMistakes = 6, Third = StarGoal.Time, ThirdTarget = 20f
            },

            // Frosty Shores: ice, limits and everything combined.
            new LevelSpec
            {
                Title = "Thin Ice", World = 2, Cards = 20, Columns = 5, Frozen = 6, Preview = 2f, Introduces = "Ice",
                Description = "Frozen cards need a tap to crack the ice before they will flip.",
                Tip = "Tap a frozen card once to crack the ice, then again to flip it.", MaxMistakes = 6, Third = StarGoal.Time, ThirdTarget = 80f
            },
            new LevelSpec
            {
                Title = "Penguin Waddle", World = 2, Cards = 24, Columns = 6, Moves = 26, Peeks = 2, Introduces = "Moves and peeks",
                Description = "Waddle wisely: you only have 26 moves. Peek cards show the whole board for a moment.",
                Tip = "Peek cards reveal every hidden card for a moment.", MaxMistakes = 8, Third = StarGoal.Moves, ThirdTarget = 18f
            },
            new LevelSpec
            {
                Title = "Whale Songs", World = 2, Cards = 24, Columns = 6, Match = 3, Time = 90f, MatchBonus = 3f, Preview = 3f,
                Description = "Triplets against the clock. Sing along and find three of a kind!",
                Tip = "Every set of three adds three seconds.", MaxMistakes = 6, Third = StarGoal.Time, ThirdTarget = 25f
            },
            new LevelSpec
            {
                Title = "Blizzard", World = 2, Cards = 20, Columns = 5, ShuffleEvery = 2, Frozen = 6, Hearts = 6,
                Description = "A blizzard blows the cards around after every second mistake - and some are frozen solid.",
                Tip = "Two mistakes and the blizzard shuffles the hidden cards.", MaxMistakes = 4, Third = StarGoal.Time, ThirdTarget = 110f
            },
            new LevelSpec
            {
                Title = "Polar Parade", World = 2, Cards = 24, Columns = 6, Parade = true, Bombs = 2, Hearts = 5, Preview = 4f,
                Description = "A parade on thin ice: match in order and steer clear of the bombs.",
                Tip = "The parade shows the next animals at the top.", MaxMistakes = 4, Third = StarGoal.Moves, ThirdTarget = 24f
            },
            new LevelSpec
            {
                Title = "Aurora Finale", World = 2, Cards = 28, Columns = 7, Wilds = 1, Bombs = 2, Peeks = 1, Clocks = 2, Frozen = 4, ShuffleEvery = 4, Hearts = 6,
                Description = "The grand finale under the northern lights. Every twist you have learned, all at once!",
                Tip = "Save the peek card for when the board is full of question marks.", MaxMistakes = 4, Third = StarGoal.Time, ThirdTarget = 150f
            }
        };

        public static MemoryCardsCampaign Campaign => MemoryCardsAssets.Load<MemoryCardsCampaign>(CampaignPath);

        public static MemoryCardsSettings DefaultSettings => MemoryCardsAssets.Load<MemoryCardsSettings>(DefaultSettingsPath);

        public static MemoryCardsSettings CustomSettings => MemoryCardsAssets.Load<MemoryCardsSettings>(CustomSettingsPath);

        public static MemoryCardsSettings CurrentSettings => MemoryCardsAssets.Load<MemoryCardsSettings>(CurrentSettingsPath);

        /// <summary>The asset of a theme, relative to the package.</summary>
        public static string ThemePath(ThemeSpec theme)
        {
            return $"{ThemeSpecs.ThemesFolder}/{theme.Id}.asset";
        }

        public static MemoryCardsTheme Theme(ThemeSpec theme)
        {
            return MemoryCardsAssets.Load<MemoryCardsTheme>(ThemePath(theme));
        }

        public static CardWorld World(ThemeSpec theme, int index)
        {
            return MemoryCardsAssets.Load<CardWorld>(theme.WorldPath(theme.Worlds[index]));
        }

        /// <summary>A world of the original campaign (the Classic theme).</summary>
        public static CardWorld World(int index)
        {
            return World(ThemeSpecs.Classic, index);
        }

        public static void BuildAll()
        {
            List<Sprite> animals = Faces(ThemeSpecs.Classic);
            List<CardWorld> worlds = BuildWorlds(ThemeSpecs.Classic);
            BuildFreePlaySettings(animals);

            var levels = new List<GameLevel>();
            for (int i = 0; i < Levels.Length; i++)
            {
                levels.Add(BuildLevel(i, Levels[i]));
            }
            int carnival = WorldSpecs.All.Length - 1;
            levels.Add(MemoryCardsAssets.SaveScriptable<MemoryCardsLevel>("Settings/Campaign/MemoryCardsEndless.asset", level =>
            {
                SetLevelBase(level, levels.Count, "Endless Carnival");
                level.kind = LevelKind.Endless;
                level.world = carnival;
                level.description = "Clear board after board before the clock runs out. Every cleared board adds time, and new twists join the fun.";
                level.introduces = string.Empty;
                level.tip = string.Empty;
                MemoryCardsAssets.SetObject(level, "<Settings>k__BackingField", null);
            }));
            levels.Add(MemoryCardsAssets.SaveScriptable<MemoryCardsLevel>("Settings/Campaign/MemoryCardsFreePlay.asset", level =>
            {
                SetLevelBase(level, levels.Count, "Free Play");
                level.kind = LevelKind.FreePlay;
                level.world = carnival;
                level.description = "Deal a board with your own rules: open Settings to choose the size, the timer, hearts, moves and special cards.";
                level.introduces = string.Empty;
                level.tip = string.Empty;
                MemoryCardsAssets.SetObject(level, "<Settings>k__BackingField", null);
            }));

            MemoryCardsCampaign campaign = MemoryCardsAssets.SaveScriptable<MemoryCardsCampaign>(CampaignPath, asset =>
            {
                asset.worlds = WorldSpecs.All.Select((spec, i) => new MemoryCardsCampaign.World
                {
                    title = spec.Name,
                    theme = worlds[i],
                    starsRequired = spec.StarsRequired
                }).ToArray();
                asset.animals = new List<Sprite>(animals);
                asset.endlessAfter = 6;
                MemoryCardsAssets.Set(asset, "<LevelList>k__BackingField", p =>
                {
                    p.arraySize = levels.Count;
                    for (int i = 0; i < levels.Count; i++)
                    {
                        p.GetArrayElementAtIndex(i).objectReferenceValue = levels[i];
                    }
                });
            });
            List<string> problems = campaign.Validate();
            if (problems.Count > 0)
            {
                throw new System.InvalidOperationException("Memory Cards campaign is invalid:\n" + string.Join("\n", problems));
            }
            var themes = new List<MemoryCardsTheme>();
            foreach (ThemeSpec spec in ThemeSpecs.All)
            {
                themes.Add(BuildTheme(spec, campaign));
            }
            BuildGameDefinition(themes);
            AssetDatabase.SaveAssets();
        }

        /// <summary>The faces of a theme's deck, in the order save games refer to them; fails when the art is missing.</summary>
        private static List<Sprite> Faces(ThemeSpec theme)
        {
            List<Sprite> faces = theme.Faces.Select(name => MemoryCardsArtBuilder.Face(theme, name)).ToList();
            if (faces.Any(sprite => sprite == null))
            {
                throw new System.InvalidOperationException($"Memory Cards: faces of the {theme.Name} theme are missing; run Rebuild Art first.");
            }
            return faces;
        }

        private static List<CardWorld> BuildWorlds(ThemeSpec theme)
        {
            return theme.Worlds.Select(spec => BuildWorld(theme, spec)).ToList();
        }

        private static CardWorld BuildWorld(ThemeSpec theme, WorldSpec spec)
        {
            return MemoryCardsAssets.SaveScriptable<CardWorld>(theme.WorldPath(spec), world =>
            {
                world.displayName = spec.Name;
                world.tagline = spec.Tagline;
                world.skyTop = WorldSpecs.Color(spec.SkyTop);
                world.skyBottom = WorldSpecs.Color(spec.SkyBottom);
                world.accent = WorldSpecs.Color(spec.Accent);
                world.accentDark = WorldSpecs.Color(spec.AccentDark);
                world.cardColor = WorldSpecs.Color(spec.Card);
                world.cardBack = MemoryCardsArtBuilder.Card(theme, theme.BackName(spec));
                world.levelCardBack = MemoryCardsArtBuilder.Card(theme, $"{theme.BackName(spec)}Plain");
                world.mascot = MemoryCardsArtBuilder.Face(theme, spec.Mascot);
                world.ambient = MemoryCardsArtBuilder.Backdrop(theme, spec.Ambient);
                world.ambientColor = spec.AmbientColor;
                world.motion = spec.Motion;
                IEnumerable<string> names = spec.Animals.Length > 0 ? spec.Animals : theme.Faces;
                world.animals = names.Select(name => MemoryCardsArtBuilder.Face(theme, name)).ToList();
            });
        }

        /// <summary>
        /// The theme asset of <paramref name="spec"/>: its worlds, its deck and every sprite, font, material and colour
        /// of its art, checked against the campaign (every level can be dealt from its world) and by the theme itself.
        /// </summary>
        private static MemoryCardsTheme BuildTheme(ThemeSpec spec, MemoryCardsCampaign campaign)
        {
            List<Sprite> faces = Faces(spec);
            List<CardWorld> worlds = BuildWorlds(spec);
            MemoryCardsTheme theme = MemoryCardsAssets.SaveScriptable<MemoryCardsTheme>(ThemePath(spec), asset => Fill(asset, spec, worlds, faces));
            var problems = new List<string>(campaign.Validate(theme));
            theme.Validate(problems);
            if (problems.Count > 0)
            {
                throw new System.InvalidOperationException($"Memory Cards theme {spec.Name} is invalid:\n" + string.Join("\n", problems));
            }
            return theme;
        }

        private static void Fill(MemoryCardsTheme theme, ThemeSpec spec, List<CardWorld> worlds, List<Sprite> faces)
        {
            MemoryCardsAssets.Set(theme, "displayName", p => p.stringValue = spec.Name);
            MemoryCardsAssets.Set(theme, "description", p => p.stringValue = spec.Description);
            MemoryCardsAssets.SetObject(theme, "preview", worlds[0].cardBack);
            theme.worlds = new List<CardWorld>(worlds);
            theme.faces = new List<Sprite>(faces);

            theme.special.wild = MemoryCardsArtBuilder.Card(spec, "Wild");
            theme.special.bomb = MemoryCardsArtBuilder.Card(spec, "Bomb");
            theme.special.clock = MemoryCardsArtBuilder.Card(spec, "Clock");
            theme.special.peek = MemoryCardsArtBuilder.Card(spec, "Peek");

            theme.card.front = MemoryCardsArtBuilder.Card(spec, "CardFront");
            theme.card.shadow = MemoryCardsArtBuilder.Card(spec, "CardShadow");
            theme.card.glow = MemoryCardsArtBuilder.Card(spec, "CardGlow");
            theme.card.badge = MemoryCardsArtBuilder.Card(spec, "Badge");
            theme.card.ice = MemoryCardsArtBuilder.Card(spec, "Ice");
            theme.card.crackedIce = MemoryCardsArtBuilder.Card(spec, "IceCracked");
            theme.card.defaultBack = worlds[0].cardBack;

            theme.kit.primary = MemoryCardsArtBuilder.Kit(spec, "Primary");
            theme.kit.secondary = MemoryCardsArtBuilder.Kit(spec, "Secondary");
            theme.kit.accent = MemoryCardsArtBuilder.Kit(spec, "Accent");
            theme.kit.danger = MemoryCardsArtBuilder.Kit(spec, "Danger");
            theme.kit.neutral = MemoryCardsArtBuilder.Kit(spec, "Neutral");
            theme.kit.roundPrimary = MemoryCardsArtBuilder.Kit(spec, "RoundPrimary");
            theme.kit.roundSecondary = MemoryCardsArtBuilder.Kit(spec, "RoundSecondary");
            theme.kit.roundAccent = MemoryCardsArtBuilder.Kit(spec, "RoundAccent");
            theme.kit.roundDanger = MemoryCardsArtBuilder.Kit(spec, "RoundDanger");
            theme.kit.roundNeutral = MemoryCardsArtBuilder.Kit(spec, "RoundNeutral");
            theme.kit.inputField = MemoryCardsArtBuilder.Kit(spec, "InputField");
            theme.kit.panel = MemoryCardsArtBuilder.Ui(spec, "Panel");
            theme.kit.panelDepth = MemoryCardsArtBuilder.Ui(spec, "PanelDepth");
            theme.kit.pill = MemoryCardsArtBuilder.Ui(spec, "Pill");
            theme.kit.disc = MemoryCardsArtBuilder.Ui(spec, "Disc");
            theme.kit.soft = MemoryCardsArtBuilder.Ui(spec, "Soft");
            theme.kit.tintPanel = MemoryCardsArtBuilder.Tint(spec, "Panel");
            theme.kit.tintPill = MemoryCardsArtBuilder.Tint(spec, "Pill");
            theme.kit.tintDisc = MemoryCardsArtBuilder.Tint(spec, "Disc");

            theme.icons.pause = MemoryCardsArtBuilder.Icon(spec, "Pause");
            theme.icons.settings = MemoryCardsArtBuilder.Icon(spec, "Settings");
            theme.icons.power = MemoryCardsArtBuilder.Icon(spec, "Power");
            theme.icons.home = MemoryCardsArtBuilder.Icon(spec, "Home");
            theme.icons.levels = MemoryCardsArtBuilder.Icon(spec, "Levels");
            theme.icons.padlock = MemoryCardsArtBuilder.Icon(spec, "Lock");
            theme.icons.heart = MemoryCardsArtBuilder.Icon(spec, "Heart");
            theme.icons.clock = MemoryCardsArtBuilder.Icon(spec, "Clock");
            theme.icons.stopwatch = MemoryCardsArtBuilder.Icon(spec, "Stopwatch");
            theme.icons.moves = MemoryCardsArtBuilder.Icon(spec, "Moves");
            theme.icons.check = MemoryCardsArtBuilder.Icon(spec, "Check");
            theme.icons.cross = MemoryCardsArtBuilder.Icon(spec, "Cross");
            theme.icons.play = MemoryCardsArtBuilder.Icon(spec, "Play");
            theme.icons.retry = MemoryCardsArtBuilder.Icon(spec, "Retry");
            theme.icons.next = MemoryCardsArtBuilder.Icon(spec, "Next");
            theme.icons.back = MemoryCardsArtBuilder.Icon(spec, "Back");
            theme.icons.infinity = MemoryCardsArtBuilder.Icon(spec, "Infinity");
            theme.icons.sliders = MemoryCardsArtBuilder.Icon(spec, "Sliders");
            theme.icons.flag = MemoryCardsArtBuilder.Icon(spec, "Flag");
            theme.icons.cards = MemoryCardsArtBuilder.Icon(spec, "Cards");
            theme.icons.paw = MemoryCardsArtBuilder.Icon(spec, "Paw");
            theme.icons.star = MemoryCardsArtBuilder.Icon(spec, "Star");
            theme.icons.heartFull = MemoryCardsArtBuilder.Icon(spec, "HeartFull");
            theme.icons.heartEmpty = MemoryCardsArtBuilder.Icon(spec, "HeartEmpty");

            theme.hud.starFull = MemoryCardsArtBuilder.Kit(spec, "StarFull");
            theme.hud.starEmpty = MemoryCardsArtBuilder.Kit(spec, "StarEmpty");
            theme.hud.goalDone = MemoryCardsArtBuilder.Kit(spec, "GoalDone");
            theme.hud.goalMissed = MemoryCardsArtBuilder.Kit(spec, "GoalMissed");
            // In LevelMode order: classic, time attack, survival, move limit, parade, endless, free play.
            theme.hud.modeIcons = new List<Sprite>
            {
                theme.icons.cards, theme.icons.stopwatch, theme.icons.heart, theme.icons.moves, theme.icons.flag, theme.icons.infinity, theme.icons.sliders
            };

            theme.title.words = spec.TitleWords;
            theme.title.subtitle = spec.Subtitle;
            theme.title.gradientTop = spec.GradientTop;
            theme.title.gradientBottom = spec.GradientBottom;
            theme.title.ornament = MemoryCardsArtBuilder.Ui(spec, spec.Ornament);
            theme.title.ornamentColor = spec.OrnamentColor;
            theme.title.mascots = spec.Mascots.Select(name => MemoryCardsArtBuilder.Face(spec, name)).ToList();

            TMP_FontAssetOf(theme, spec);
            spec.Palette(theme.palette);

            theme.backdrop.pattern = MemoryCardsArtBuilder.Pattern(spec);
            theme.backdrop.patternColor = spec.PatternColor;
            theme.backdrop.patternTileSize = 300f;
            theme.backdrop.vignette = MemoryCardsArtBuilder.Ui(spec, "Soft");
            theme.backdrop.vignetteColor = spec.VignetteColor;

            theme.particles.spark = MemoryCardsArtBuilder.Particle(spec, "Spark");
            theme.particles.star = MemoryCardsArtBuilder.Particle(spec, "Star");
            theme.particles.circle = MemoryCardsArtBuilder.Particle(spec, "Circle");
            theme.particles.confetti = MemoryCardsArtBuilder.Particle(spec, "Confetti");
            theme.particles.shard = MemoryCardsArtBuilder.Particle(spec, "Shard");
            theme.particles.wildColors = new List<Color>(spec.WildColors);
            spec.Words(theme.words);

            // The shared menu is not used by this game; its skin still carries the look, should a scene install it.
            MenuSkin menu = theme.Menu;
            menu.backdrop = theme.kit.panel;
            menu.backdropColor = theme.palette.dim;
            menu.window = theme.kit.panelDepth;
            menu.windowColor = Color.white;
            menu.settingsWindow = theme.kit.panelDepth;
            menu.headerBanner = theme.kit.pill;
            menu.rowsBackground = theme.kit.panel;
            menu.rowsColor = Color.white;
            menu.rowBackground = theme.kit.pill;
            menu.rowColor = Color.white;
            menu.button = theme.kit.primary;
            menu.buttonHover = theme.kit.accent;
            menu.buttonPressed = theme.kit.secondary;
            menu.buttonDisabled = theme.kit.neutral;
            menu.arrowButton = theme.kit.roundSecondary;
            menu.track = theme.kit.pill;
            menu.fill = theme.kit.pill;
            menu.fillColor = theme.palette.gold;
            menu.knob = theme.kit.disc;
            menu.checkBox = theme.kit.disc;
            menu.checkMark = theme.icons.check;
            menu.headerColor = theme.palette.light;
            menu.textColor = theme.palette.ink;
            menu.valueColor = theme.palette.softInk;
            menu.buttonTextColor = theme.palette.buttonText;
            menu.errorColor = theme.palette.bad;
        }

        /// <summary>The fonts of a theme and the materials its labels are drawn with.</summary>
        private static void TMP_FontAssetOf(MemoryCardsTheme theme, ThemeSpec spec)
        {
            TMPro.TMP_FontAsset body = MemoryCardsArtBuilder.Font(spec.BodyFont);
            TMPro.TMP_FontAsset title = MemoryCardsArtBuilder.Font(spec.TitleFont);
            if (body == null || title == null)
            {
                throw new System.InvalidOperationException($"Memory Cards: the fonts of the {spec.Name} theme are missing; run Rebuild Art first.");
            }
            theme.Fonts.body = body;
            theme.Fonts.bodyMaterial = body.material;
            theme.Fonts.title = title;
            theme.Fonts.titleMaterial = MemoryCardsArtBuilder.FontMaterial(spec.TitleFont, "Title");
            theme.text.outline = MemoryCardsArtBuilder.FontMaterial(spec.BodyFont, "Outline");
            theme.text.shadow = MemoryCardsArtBuilder.FontMaterial(spec.BodyFont, "Shadow");
            theme.text.title = theme.Fonts.titleMaterial;
        }

        private static MemoryCardsLevel BuildLevel(int index, LevelSpec spec)
        {
            MemoryCardsSettings settings = MemoryCardsAssets.SaveScriptable<MemoryCardsSettings>($"Settings/Levels/Level{index + 1:00}.asset", rules =>
            {
                rules.CardTypes = new List<Sprite>();
                rules.CardsAmount = spec.Cards;
                rules.CardsPerRow = spec.Columns;
                rules.FlippedCardsPerMatch = spec.Match;
                rules.TimePerGame = spec.Time;
                rules.TimeUntilUnflip = spec.Unflip;
                rules.PreviewTime = spec.Preview;
                rules.Hearts = spec.Hearts;
                rules.MoveLimit = spec.Moves;
                rules.MatchTimeBonus = spec.MatchBonus;
                rules.ShuffleEvery = spec.ShuffleEvery;
                rules.Parade = spec.Parade;
                rules.Bombs = spec.Bombs;
                rules.Wilds = spec.Wilds;
                rules.Clocks = spec.Clocks;
                rules.Peeks = spec.Peeks;
                rules.Frozen = spec.Frozen;
            });
            // The first level keeps the asset of the original single level (and with it its GUID).
            string file = $"Settings/Campaign/MemoryCardsLevel{index + 1}.asset";
            return MemoryCardsAssets.SaveScriptable<MemoryCardsLevel>(file, level =>
            {
                SetLevelBase(level, index, spec.Title);
                MemoryCardsAssets.SetObject(level, "<Settings>k__BackingField", settings);
                level.kind = LevelKind.Campaign;
                level.world = spec.World;
                level.description = spec.Description;
                level.introduces = spec.Introduces ?? string.Empty;
                level.tip = spec.Tip ?? string.Empty;
                level.goals = new StarGoals { maxMistakes = spec.MaxMistakes, third = spec.Third, thirdTarget = spec.ThirdTarget };
            });
        }

        private static void SetLevelBase(GameLevel level, int index, string title)
        {
            MemoryCardsAssets.Set(level, "<Index>k__BackingField", p => p.intValue = index);
            MemoryCardsAssets.Set(level, "<Title>k__BackingField", p => p.stringValue = title);
        }

        /// <summary>Free play: the default rules (a classic 4 by 4 board) and the custom rules the settings panel edits.</summary>
        private static void BuildFreePlaySettings(List<Sprite> animals)
        {
            MemoryCardsSettings defaults = MemoryCardsAssets.SaveScriptable<MemoryCardsSettings>(DefaultSettingsPath, rules =>
            {
                rules.CardTypes = new List<Sprite>(animals);
                rules.CardsAmount = 20;
                rules.CardsPerRow = 5;
                rules.FlippedCardsPerMatch = 2;
                rules.TimePerGame = 60f;
                rules.TimeUntilUnflip = 0.9f;
                rules.PreviewTime = 2f;
                rules.Hearts = 0;
                rules.MoveLimit = 0;
                rules.MatchTimeBonus = 1f;
                rules.ShuffleEvery = 0;
                rules.Parade = false;
                rules.Bombs = 0;
                rules.Wilds = 0;
                rules.Clocks = 0;
                rules.Peeks = 0;
                rules.Frozen = 0;
            });
            foreach (string path in new[] { CustomSettingsPath, CurrentSettingsPath })
            {
                MemoryCardsAssets.SaveScriptable<MemoryCardsSettings>(path, rules =>
                {
                    rules.CopySettings(defaults);
                    rules.CardTypes = new List<Sprite>(animals);
                });
            }
        }

        /// <summary>The launcher entry: its words and icon, and the themes it can show (the first is the one it starts with).</summary>
        private static void BuildGameDefinition(List<MemoryCardsTheme> themes)
        {
            var definition = MemoryCardsAssets.Load<GameDefinition>(GameDefinitionPath);
            if (definition == null)
            {
                return;
            }
            MemoryCardsAssets.ApplyIfChanged(definition, () =>
            {
                MemoryCardsAssets.Set(definition, "displayName", p => p.stringValue = "Memory Cards");
                MemoryCardsAssets.Set(definition, "description", p => p.stringValue =
                    "Match cute critters across three worlds and 18 levels: memorize, beat the clock, dodge bombs, play wild cards, crack ice and follow the parade.");
                MemoryCardsAssets.SetObject(definition, "icon", MemoryCardsAssets.LoadSprite("Art/LauncherIcon.png"));
                if (themes.Count > 0)
                {
                    definition.Theme = themes[0];
                    foreach (MemoryCardsTheme theme in themes)
                    {
                        definition.AddTheme(theme);
                    }
                }
            });
            GameThemes.ForgetDefinitions();
        }
    }
}
