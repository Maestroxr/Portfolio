using System;
using System.Collections.Generic;
using Gamebox;
using Gamebox.Editor;
using Gamebox.Launcher;
using UnityEditor;
using UnityEngine;

namespace Portfolio.Monopoly.EditorTools
{
    /// <summary>
    /// Writes the game's data: the World Tour board, the settings of every game mode, the modes themselves (the
    /// campaign levels), the campaign, the themes (one per look of <see cref="MonopolyThemeSpec.All"/>, from the art the
    /// art builder generated) and the launcher entry, which starts with the classic theme and lists them all. The rules
    /// of the modes live here, so hand edits of those assets are overwritten by a rebuild.
    /// </summary>
    internal static class MonopolyContentBuilder
    {
        public const string BoardPath = "Config/Board.asset";
        public const string DefaultSettingsPath = "Config/MonopolySettings.asset";
        public const string CampaignPath = "Config/Campaign/MonopolyCampaign.asset";
        public const string DefinitionPath = "Resources/Games/Monopoly.asset";

        private sealed class ModeSpec
        {
            public string file;
            public string title;
            public string tagline;
            public string description;
            public string icon;
            public Color accent;
            public string accentKey;
            public Action<RuleSet> rules;
            public bool custom;
        }

        private static readonly ModeSpec[] Modes =
        {
            new ModeSpec
            {
                file = "MonopolyLevel1", title = "Classic", icon = Icons.House, accent = MonopolyStyle.Defaults.Red, accentKey = "palette.red",
                tagline = "The full game. Buy, build, bankrupt everyone.",
                description = "The official rules. Buy what you land on or it goes to auction. Collect rent, doubled on a full color set, " +
                    "and build evenly: the bank has 32 houses and 12 hotels. Mortgage to raise cash, trade to complete sets. " +
                    "Doubles roll again, three doubles go to jail. The last player standing wins.",
                rules = r => { }
            },
            new ModeSpec
            {
                file = "MonopolyLevel2", title = "Speed Die", icon = Icons.Bus, accent = MonopolyStyle.Defaults.Blue, accentKey = "palette.blue",
                tagline = "The Mega Edition's red die: bus rides and Mr. Monopoly.",
                description = "Once you have passed GO, the red speed die rolls too. 1 to 3 add to your move. Mr. Monopoly moves you on to " +
                    "the next property for sale (or the next rent you owe). The bus lets you move by either die or both. " +
                    "Triples move you anywhere you like!",
                rules = r => r.speedDie = true
            },
            new ModeSpec
            {
                file = "MonopolyLevel3", title = "Quick Deal", icon = Icons.Hourglass, accent = MonopolyStyle.Defaults.Green, accentKey = "palette.green",
                tagline = "The short game: two deeds each, hotels at three houses.",
                description = "The official short game. Everyone is dealt two title deeds (and pays for them), hotels need only three " +
                    "houses, and the game ends at the second bankruptcy. Then the richest player wins, counting cash, property " +
                    "and buildings.",
                rules = r =>
                {
                    r.dealtProperties = 2;
                    r.housesForHotel = 3;
                    r.bankruptciesToEnd = 2;
                }
            },
            new ModeSpec
            {
                file = "MonopolyLevel4", title = "House Rules", icon = Icons.Party, accent = MonopolyStyle.Defaults.ChanceOrange, accentKey = "palette.chanceOrange",
                tagline = "Free Parking jackpot, double GO, party cards.",
                description = "The table favourites. Taxes and fines go into a Free Parking jackpot, landing right on GO pays double, " +
                    "party cards (Robin Hood, free houses, a charity gala) join the decks, and there are no auctions: what nobody " +
                    "buys stays with the bank.",
                rules = r =>
                {
                    r.freeParkingJackpot = true;
                    r.jackpotSeed = 100;
                    r.doubleSalaryOnGo = true;
                    r.partyCards = true;
                    r.auctions = false;
                }
            },
            new ModeSpec
            {
                file = "MonopolyLevel5", title = "Tycoon Rush", icon = Icons.Trophy, accent = MonopolyStyle.Defaults.Gold, accentKey = "palette.gold",
                tagline = "20 rounds, the speed die, $2,000 to start.",
                description = "A race against the clock: twenty rounds with the speed die and $2,000 each to start. When time is up the " +
                    "player with the highest net worth wins, so buy early and build fast.",
                rules = r =>
                {
                    r.roundLimit = 20;
                    r.speedDie = true;
                    r.startingCash = 2000;
                }
            },
            new ModeSpec
            {
                file = "MonopolyLevel6", title = "Custom Rules", icon = Icons.Gear, accent = MonopolyStyle.Hex(0x6B4FBB), accentKey = "print.officer", custom = true,
                tagline = "Your own mix, set in House Rules.",
                description = "Play with your own rules: turn on custom rules in House Rules on the main menu and choose the starting cash, " +
                    "a round limit, dealt title deeds, auctions, the speed die, the jackpot, double GO, no rent from jail and the " +
                    "party cards."
            }
        };

        public static Board Board => MonopolyAssets.Load<Board>(BoardPath);
        public static MonopolySettings DefaultSettings => MonopolyAssets.Load<MonopolySettings>(DefaultSettingsPath);
        public static MonopolyCampaign Campaign => MonopolyAssets.Load<MonopolyCampaign>(CampaignPath);

        /// <summary>The theme of a look, once built.</summary>
        public static MonopolyTheme Theme(MonopolyThemeSpec spec)
        {
            return MonopolyAssets.Load<MonopolyTheme>(spec.AssetPath);
        }

        /// <summary>The classic theme, which the scene is built and tagged with.</summary>
        public static MonopolyTheme ClassicTheme => Theme(MonopolyThemeSpec.Classic);

        public static void BuildAll()
        {
            Board board = MonopolyAssets.SaveScriptable<Board>(BoardPath, b => b.SetLayout(WorldTourBoard.Create()));
            MonopolySettings defaults = MonopolyAssets.SaveScriptable<MonopolySettings>(DefaultSettingsPath, s =>
            {
                s.Board = board;
                s.SetRules(new RuleSet());
                s.AnimationSpeed = 1f;
                s.BotThinkTime = 0.45f;
            });

            var levels = new List<GameLevel>();
            for (int i = 0; i < Modes.Length; i++)
            {
                ModeSpec spec = Modes[i];
                MonopolySettings settings = null;
                if (!spec.custom)
                {
                    settings = MonopolyAssets.SaveScriptable<MonopolySettings>($"Config/Campaign/Settings/{spec.title.Replace(" ", "")}.asset", s =>
                    {
                        var rules = new RuleSet();
                        spec.rules(rules);
                        s.Board = board;
                        s.SetRules(rules);
                        s.AnimationSpeed = 1f;
                        s.BotThinkTime = 0.45f;
                    });
                }
                int index = i;
                MonopolyLevel level = MonopolyAssets.SaveScriptable<MonopolyLevel>($"Config/Campaign/{spec.file}.asset", l =>
                {
                    l.Configure(settings, spec.tagline, spec.description, spec.icon, spec.accent, spec.accentKey);
                    MonopolyAssets.SetInt(l, "<Index>k__BackingField", index);
                    MonopolyAssets.SetString(l, "<Title>k__BackingField", spec.title);
                });
                levels.Add(level);
            }
            MonopolyAssets.SaveScriptable<MonopolyCampaign>(CampaignPath, c => MonopolyAssets.SetObjects(c, "<LevelList>k__BackingField", levels.ToArray()));

            var themes = new List<MonopolyTheme>();
            foreach (MonopolyThemeSpec spec in MonopolyThemeSpec.All)
            {
                themes.Add(BuildTheme(spec));
            }

            var definition = MonopolyAssets.Load<GameDefinition>(DefinitionPath);
            if (definition != null)
            {
                MonopolyAssets.ApplyIfChanged(definition, () =>
                {
                    MonopolyAssets.SetString(definition, "displayName", "Monopoly");
                    MonopolyAssets.SetString(definition, "description",
                        "Roll, buy, build and trade your way around the world. Up to four players at one screen, computer opponents on three levels, and six ways to play.");
                    foreach (MonopolyTheme theme in themes)
                    {
                        definition.AddTheme(theme);
                    }
                    definition.Theme = themes[0];
                });
                GameThemes.ForgetDefinitions();
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"Monopoly content: board, {levels.Count} modes, the campaign and {themes.Count} themes built.");
        }

        // ------------------------------------------------------------------ themes

        /// <summary>The theme asset of a look, filled from the art the art builder made for it (updated in place).</summary>
        public static MonopolyTheme BuildTheme(MonopolyThemeSpec spec)
        {
            MonopolyTheme theme = MonopolyAssets.SaveScriptable<MonopolyTheme>(spec.AssetPath, t =>
            {
                MonopolyAssets.SetString(t, "displayName", spec.Name);
                MonopolyAssets.SetString(t, "description", spec.Description);
                MonopolyAssets.SetObject(t, "preview", MonopolyArtBuilder.UI("Logo", spec));
                t.editionLabel = spec.EditionLabel;
                t.palette = Copy(spec.Palette);
                t.print = Copy(spec.Print);
                t.groups = new List<GroupLook>();
                foreach ((string name, Color color, Color text) in spec.Groups)
                {
                    t.groups.Add(new GroupLook { name = name, color = color, textColor = text });
                }
                t.seats = new List<SeatLook>();
                for (int i = 0; i < spec.Seats.Length; i++)
                {
                    t.seats.Add(new SeatLook
                    {
                        name = spec.Seats[i].name,
                        color = spec.Seats[i].color,
                        baseMaterial = MonopolyArtBuilder.Material($"Seat_{i}", spec),
                        ringMaterial = MonopolyArtBuilder.Material($"TurnRing_{i}", spec)
                    });
                }
                t.tokens = new List<TokenLook>();
                for (int i = 0; i < spec.Tokens.Length; i++)
                {
                    t.tokens.Add(new TokenLook
                    {
                        name = spec.Tokens[i].name,
                        tint = spec.Tokens[i].tint,
                        mesh = MonopolyArtBuilder.Mesh($"Token_{i}", spec),
                        badge = MonopolyArtBuilder.Badge(i, spec)
                    });
                }
                t.boardMaterial = MonopolyArtBuilder.Material("Board", spec);
                t.boardSlabMaterial = MonopolyArtBuilder.Material("BoardSlab", spec);
                t.tableMaterial = MonopolyArtBuilder.Material("Table", spec);
                t.boardTexture = MonopolyArtBuilder.Texture("Board", spec);
                t.tableTexture = MonopolyArtBuilder.Texture("Table", spec);
                t.boardTopMesh = MonopolyArtBuilder.Mesh("BoardTop", spec);
                t.boardSlabMesh = MonopolyArtBuilder.Mesh("BoardSlab", spec);
                t.tableMesh = MonopolyArtBuilder.Mesh("Table", spec);
                t.highlightMaterial = MonopolyArtBuilder.Material("Highlight", spec);
                t.mortgagedMaterial = MonopolyArtBuilder.Material("Mortgaged", spec);
                t.contactShadowMaterial = MonopolyArtBuilder.Material("ContactShadow", spec);
                t.ownerTagMesh = MonopolyArtBuilder.Mesh("OwnerTag", spec);
                t.chanceBackMaterial = MonopolyArtBuilder.Material("CardChance", spec);
                t.chestBackMaterial = MonopolyArtBuilder.Material("CardChest", spec);
                t.cardEdgeMaterial = MonopolyArtBuilder.Material("CardEdge", spec);
                t.chanceBackTexture = MonopolyArtBuilder.Texture("CardChance", spec);
                t.chestBackTexture = MonopolyArtBuilder.Texture("CardChest", spec);
                t.cardStackMesh = MonopolyArtBuilder.Mesh("CardStack", spec);
                t.logoMaterial = MonopolyArtBuilder.Material("Logo", spec);
                t.logoTexture = MonopolyArtBuilder.Texture("Logo", spec);
                t.tokenMaterial = MonopolyArtBuilder.Material("Pewter", spec);
                t.tokenGlow = spec.TokenGlow;
                t.tokenBaseMesh = MonopolyArtBuilder.Mesh("TokenBase", spec);
                t.turnRingMesh = MonopolyArtBuilder.Mesh("TurnRing", spec);
                t.houseMesh = MonopolyArtBuilder.Mesh("House", spec);
                t.hotelMesh = MonopolyArtBuilder.Mesh("Hotel", spec);
                t.houseMaterial = MonopolyArtBuilder.Material("House", spec);
                t.hotelMaterial = MonopolyArtBuilder.Material("Hotel", spec);
                t.dieMesh = MonopolyArtBuilder.Mesh("Die", spec);
                t.dieMaterial = MonopolyArtBuilder.Material("Die", spec);
                t.speedDieMaterial = MonopolyArtBuilder.Material("SpeedDie", spec);
                t.diceTexture = MonopolyArtBuilder.Texture("Dice", spec);
                t.speedDieTexture = MonopolyArtBuilder.Texture("SpeedDie", spec);
                t.ui = new UIKitSprites
                {
                    rounded = MonopolyArtBuilder.UI("Rounded", spec),
                    circle = MonopolyArtBuilder.UI("Circle", spec),
                    ring = MonopolyArtBuilder.UI("Ring", spec),
                    glow = MonopolyArtBuilder.UI("Glow", spec),
                    shadow = MonopolyArtBuilder.UI("Shadow", spec),
                    sheen = MonopolyArtBuilder.UI("Sheen", spec),
                    bill = MonopolyArtBuilder.UI("Bill", spec),
                    logo = MonopolyArtBuilder.UI("Logo", spec)
                };
                t.bodyFont = MonopolyArtBuilder.BodyOf(spec);
                t.boldFont = MonopolyArtBuilder.BoldOf(spec);
                t.heavyFont = MonopolyArtBuilder.HeavyOf(spec);
                t.titleFont = MonopolyArtBuilder.TitleOf(spec);
                t.iconFont = MonopolyArtBuilder.IconFont;
                t.textShadowMaterial = MonopolyArtBuilder.TextShadowOf(spec);
                t.titleMaterial = MonopolyArtBuilder.TitleMaterialOf(spec);
                t.scene = Copy(spec.Scene);
                t.scene.reflection = MonopolyArtBuilder.ReflectionOf(spec);
                // The shared parts of the base theme: the fonts of the two roles and the skin of a shared menu.
                t.Fonts.body = t.boldFont;
                t.Fonts.bodyMaterial = t.boldFont.material;
                t.Fonts.title = t.titleFont;
                t.Fonts.titleMaterial = t.titleMaterial;
                FillMenu(t);
            });
            return theme;
        }

        /// <summary>
        /// The base theme's <see cref="MenuSkin"/>, in the game's own look: the kit's shapes and the palette's colours.
        /// The game draws its own menu with these parts already, so the skin is what a shared menu would get.
        /// </summary>
        private static void FillMenu(MonopolyTheme t)
        {
            MenuSkin menu = t.Menu;
            menu.backdrop = t.ui.glow;
            menu.backdropColor = MonopolyStyle.WithAlpha(t.palette.ink, 0.35f);
            menu.window = t.ui.rounded;
            menu.windowColor = t.palette.paper;
            menu.settingsWindow = t.ui.rounded;
            menu.headerBanner = t.ui.logo;
            menu.rowsBackground = t.ui.rounded;
            menu.rowsColor = t.palette.panel;
            menu.rowBackground = t.ui.rounded;
            menu.rowColor = t.palette.paper;
            menu.button = t.ui.rounded;
            menu.buttonHover = t.ui.rounded;
            menu.buttonPressed = t.ui.rounded;
            menu.buttonDisabled = t.ui.rounded;
            menu.arrowButton = t.ui.circle;
            menu.track = t.ui.rounded;
            menu.fill = t.ui.rounded;
            menu.fillColor = t.palette.green;
            menu.knob = t.ui.circle;
            menu.checkBox = t.ui.rounded;
            menu.checkMark = t.ui.circle;
            menu.headerColor = t.palette.ink;
            menu.textColor = t.palette.ink;
            menu.valueColor = t.palette.muted;
            menu.buttonTextColor = t.palette.ink;
            menu.errorColor = t.palette.red;
        }

        private static MonopolyPalette Copy(MonopolyPalette source)
        {
            return JsonUtility.FromJson<MonopolyPalette>(JsonUtility.ToJson(source));
        }

        private static PrintLook Copy(PrintLook source)
        {
            return JsonUtility.FromJson<PrintLook>(JsonUtility.ToJson(source));
        }

        private static SceneLook Copy(SceneLook source)
        {
            return JsonUtility.FromJson<SceneLook>(JsonUtility.ToJson(source));
        }
    }
}
