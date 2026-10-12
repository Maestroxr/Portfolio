using System.Collections.Generic;
using Gamebox;
using Gamebox.Launcher;
using UnityEditor;
using UnityEngine;

namespace Portfolio.BoardGames.EditorTools
{
    /// <summary>
    /// Writes the data assets of the games: the default settings, the twelve campaign levels (draughts, chess and
    /// backgammon) and the campaign, one theme per look (filled from the art the art builder made, the chess and
    /// backgammon parts by <see cref="ChessArt"/> and <see cref="BackgammonArt"/>) and the launcher's game definition. Assets are updated in
    /// place so their GUIDs, and the screenshots of the themes, survive a rebuild.
    /// </summary>
    internal static class BoardGamesContentBuilder
    {
        public const string DefaultSettingsPath = "Config/BoardGamesSettings.asset";
        public const string CampaignPath = "Config/Campaign/BoardGamesCampaign.asset";
        public const string DefinitionPath = "Resources/Games/BoardGames.asset";
        public const string ScenePath = "Scenes/BoardGames.unity";

        public const string DisplayName = "Board Games";
        public const string Description = "Checkers, chess and backgammon on a 3D table: against the computer, two at one device, or online with tournaments.";

        private struct LevelSpec
        {
            public string file;
            public string title;
            public RuleSet ruleSet;
            public int computer;
            public Side side;
            public int piecesKept;
            public int maxPlies;
            public string description;
        }

        /// <summary>
        /// The twelve levels: six of draughts, three of chess, three of backgammon, each game from Easy to Hard. For chess
        /// the pieces kept are the player's pieces other than the king left at the end; for backgammon they are the
        /// points to win (2 = a gammon) and the plies are the game's turns (every roll, play and cube action).
        /// </summary>
        private static readonly LevelSpec[] Levels =
        {
            new LevelSpec { file = "Level1", title = "First Moves", ruleSet = RuleSet.EnglishDraughts, computer = 0, side = Side.Dark, piecesKept = 8, maxPlies = 60,
                description = "An easy English game to learn the moves. You play Dark and move first." },
            new LevelSpec { file = "Level2", title = "Crowning Glory", ruleSet = RuleSet.EnglishDraughts, computer = 1, side = Side.Light, piecesKept = 7, maxPlies = 80,
                description = "English rules against a thoughtful opponent. Reach the far rank to crown a king." },
            new LevelSpec { file = "Level3", title = "Flying Kings", ruleSet = RuleSet.RussianDraughts, computer = 1, side = Side.Light, piecesKept = 7, maxPlies = 80,
                description = "Russian rules: men capture backward and kings fly across the board." },
            new LevelSpec { file = "Level4", title = "The Wide Board", ruleSet = RuleSet.InternationalDraughts, computer = 1, side = Side.Light, piecesKept = 12, maxPlies = 120,
                description = "Ten by ten and twenty men a side. The sequence that captures the most pieces is the one you must take." },
            new LevelSpec { file = "Level5", title = "Grandmaster's Table", ruleSet = RuleSet.EnglishDraughts, computer = 2, side = Side.Dark, piecesKept = 6, maxPlies = 100,
                description = "English rules against a hard opponent who sees far ahead." },
            new LevelSpec { file = "Level6", title = "World Championship", ruleSet = RuleSet.InternationalDraughts, computer = 2, side = Side.Light, piecesKept = 10, maxPlies = 140,
                description = "The wide board against the strongest opponent. Keep your pieces and finish it quickly." },
            new LevelSpec { file = "Level7", title = "King's Pawn", ruleSet = RuleSet.Chess, computer = 0, side = Side.Light, piecesKept = 8, maxPlies = 120,
                description = "Chess against an easy opponent. You play White and move first." },
            new LevelSpec { file = "Level8", title = "The Black Pieces", ruleSet = RuleSet.Chess, computer = 1, side = Side.Dark, piecesKept = 6, maxPlies = 140,
                description = "Chess with Black against a thoughtful opponent. Answer the first move and strike back." },
            new LevelSpec { file = "Level9", title = "Checkmate Master", ruleSet = RuleSet.Chess, computer = 2, side = Side.Light, piecesKept = 4, maxPlies = 160,
                description = "Chess against the strongest opponent. Keep your pieces and checkmate quickly." },
            new LevelSpec { file = "Level10", title = "Bear Them Off", ruleSet = RuleSet.Backgammon, computer = 0, side = Side.Light, piecesKept = 2, maxPlies = 110,
                description = "Backgammon against a beginner. Race your checkers home and bear them off. The doubling cube is in play." },
            new LevelSpec { file = "Level11", title = "Hit and Run", ruleSet = RuleSet.Backgammon, computer = 1, side = Side.Dark, piecesKept = 2, maxPlies = 100,
                description = "Backgammon against a thoughtful opponent. Hit lone checkers and make points to win a gammon." },
            new LevelSpec { file = "Level12", title = "Double or Nothing", ruleSet = RuleSet.Backgammon, computer = 2, side = Side.Light, piecesKept = 2, maxPlies = 100,
                description = "Backgammon against the strongest opponent. Mind the cube and win quickly." }
        };

        public static BoardGamesSettings DefaultSettings => BoardGamesAssets.Require<BoardGamesSettings>(DefaultSettingsPath);

        public static BoardGamesCampaign Campaign => BoardGamesAssets.Require<BoardGamesCampaign>(CampaignPath);

        public static BoardGamesTheme Theme(BoardGamesThemeSpec spec)
        {
            return BoardGamesAssets.Require<BoardGamesTheme>(spec.AssetPath);
        }

        public static BoardGamesTheme ClassicTheme => Theme(BoardGamesThemeSpec.Classic);

        public static void BuildAll()
        {
            BuildSettings();
            BuildCampaign();
            BuildThemes();
            BuildDefinition();
            AssetDatabase.SaveAssets();
        }

        private static void BuildSettings()
        {
            BoardGamesAssets.SaveScriptable<BoardGamesSettings>(DefaultSettingsPath, s =>
            {
                s.showMoves = true;
                s.turnBoard = false;
                s.thinkingDelay = 0.6f;
                s.volume = 0.8f;
                s.mode = (int)GameMode.Checkers;
                s.variant = (int)Variant.English;
                s.doublingCube = true;
                s.computerLevel = 1;
            });
        }

        private static void BuildCampaign()
        {
            var levels = new List<GameLevel>();
            for (int i = 0; i < Levels.Length; i++)
            {
                LevelSpec spec = Levels[i];
                int index = i;
                BoardGamesLevel level = BoardGamesAssets.SaveScriptable<BoardGamesLevel>($"Config/Campaign/{spec.file}.asset", l =>
                {
                    l.Configure(spec.ruleSet, spec.computer, (int)spec.side, spec.piecesKept, spec.maxPlies, spec.description);
                    BoardGamesAssets.SetInt(l, "<Index>k__BackingField", index);
                    BoardGamesAssets.SetString(l, "<Title>k__BackingField", spec.title);
                });
                if (!level.IsLevelValid(out string message))
                {
                    throw new System.InvalidOperationException($"Campaign level {spec.title} is not valid: {message}");
                }
                levels.Add(level);
            }
            BoardGamesAssets.SaveScriptable<BoardGamesCampaign>(CampaignPath, c => BoardGamesAssets.SetObjects(c, "<LevelList>k__BackingField", levels.ToArray()));
        }

        /// <summary>Builds every theme and lists it on the definition when there is one.</summary>
        public static List<BoardGamesTheme> BuildThemes()
        {
            var themes = new List<BoardGamesTheme>();
            foreach (BoardGamesThemeSpec spec in BoardGamesThemeSpec.All)
            {
                themes.Add(BuildTheme(spec));
            }
            AttachThemes(themes);
            return themes;
        }

        public static BoardGamesTheme BuildTheme(BoardGamesThemeSpec spec)
        {
            BoardGamesTheme theme = BoardGamesAssets.SaveScriptable<BoardGamesTheme>(spec.AssetPath, t =>
            {
                BoardGamesAssets.SetString(t, "displayName", spec.Name);
                BoardGamesAssets.SetString(t, "description", spec.Description);
                BoardGamesAssets.SetObject(t, "preview", BoardGamesArtBuilder.Preview(spec));
                t.sides = new[]
                {
                    new SideLook { nameKey = "Light", piece = BoardGamesArtBuilder.Material("LightPiece", spec), crown = BoardGamesArtBuilder.Material("LightCrown", spec), color = spec.LightColor },
                    new SideLook { nameKey = "Dark", piece = BoardGamesArtBuilder.Material("DarkPiece", spec), crown = BoardGamesArtBuilder.Material("DarkCrown", spec), color = spec.DarkColor }
                };
                t.lightSquare = BoardGamesArtBuilder.Material("LightSquare", spec);
                t.darkSquare = BoardGamesArtBuilder.Material("DarkSquare", spec);
                t.slab = BoardGamesArtBuilder.Material("Slab", spec);
                t.table = BoardGamesArtBuilder.Material("Table", spec);
                t.highlight = BoardGamesArtBuilder.Material("Highlight", spec);
                t.contactShadow = BoardGamesArtBuilder.Material("ContactShadow", spec);
                t.pieceMesh = BoardGamesArtBuilder.Mesh("Piece");
                t.crownMesh = BoardGamesArtBuilder.Mesh("Crown");
                t.squareMesh = BoardGamesArtBuilder.Mesh("Square");
                t.slabMesh = BoardGamesArtBuilder.Mesh("Slab");
                t.tableMesh = BoardGamesArtBuilder.Mesh("Table");
                t.selectedColor = spec.Selected;
                t.legalColor = spec.Legal;
                t.captureColor = spec.Capture;
                t.lastMoveColor = spec.LastMove;
                t.hoverColor = spec.Hover;
                t.background = spec.Background;
                t.ambientSky = spec.AmbientSky;
                t.ambientEquator = spec.AmbientEquator;
                t.ambientGround = spec.AmbientGround;
                t.sunColor = spec.SunColor;
                t.sunIntensity = spec.SunIntensity;
                t.gameIcon = spec == BoardGamesThemeSpec.Classic ? BoardGamesArtBuilder.Icon("GameIcon") : null;
                // The shared parts of the base theme: the fonts of the two roles and the skin of the shared menu.
                t.Fonts.body = BoardGamesArtBuilder.BoldFont;
                t.Fonts.bodyMaterial = BoardGamesArtBuilder.BoldFont.material;
                t.Fonts.title = BoardGamesArtBuilder.TitleFont;
                t.Fonts.titleMaterial = BoardGamesArtBuilder.TitleFont.material;
                FillMenu(t, spec);
                ChessArt.Dress(t, spec);
                BackgammonArt.Dress(t, spec);
            });
            var problems = new List<string>();
            if (!theme.Validate(problems))
            {
                throw new System.InvalidOperationException($"Theme {spec.Name} is not complete: {string.Join("; ", problems)}");
            }
            return theme;
        }

        private static void FillMenu(BoardGamesTheme t, BoardGamesThemeSpec spec)
        {
            Sprite rounded = BoardGamesArtBuilder.UI("Rounded");
            Sprite circle = BoardGamesArtBuilder.UI("Circle");
            MenuSkin menu = t.Menu;
            menu.backdrop = BoardGamesArtBuilder.UI("Glow");
            menu.backdropColor = WithAlpha(spec.Ink, 0.4f);
            menu.window = rounded;
            menu.windowColor = spec.Paper;
            menu.tileWindow = false;
            menu.settingsWindow = rounded;
            menu.headerBanner = BoardGamesArtBuilder.UI("Banner");
            menu.rowsBackground = rounded;
            menu.rowsColor = spec.Panel;
            menu.rowBackground = rounded;
            menu.rowColor = spec.Paper;
            menu.button = rounded;
            menu.buttonHover = rounded;
            menu.buttonPressed = rounded;
            menu.buttonDisabled = rounded;
            menu.arrowButton = circle;
            menu.track = rounded;
            menu.fill = rounded;
            menu.fillColor = spec.Accent;
            menu.knob = circle;
            menu.checkBox = rounded;
            menu.checkMark = circle;
            // The buttons and the header banner keep their white sprites in every look: their words are dark.
            menu.headerColor = spec.ButtonInk;
            menu.textColor = spec.Ink;
            menu.valueColor = spec.Muted;
            menu.buttonTextColor = spec.ButtonInk;
            menu.errorColor = spec.Error;
        }

        private static Color WithAlpha(Color c, float alpha)
        {
            return new Color(c.r, c.g, c.b, alpha);
        }

        /// <summary>
        /// The launcher's definition: created once, then updated in place. The scene reference is set when the scene
        /// file exists, so the step runs again after the scene builder.
        /// </summary>
        public static void BuildDefinition()
        {
            var definition = BoardGamesAssets.Load<GameDefinition>(DefinitionPath);
            if (definition == null)
            {
                definition = ScriptableObject.CreateInstance<GameDefinition>();
                BoardGamesAssets.EnsureFolder("Resources/Games");
                AssetDatabase.CreateAsset(definition, BoardGamesAssets.Path(DefinitionPath));
            }
            string scenePath = BoardGamesAssets.Path(ScenePath);
            var scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);
            BoardGamesAssets.ApplyIfChanged(definition, () =>
            {
                BoardGamesAssets.Set(definition, "type", p => p.intValue = (int)GameType.BoardGames);
                BoardGamesAssets.SetString(definition, "displayName", DisplayName);
                BoardGamesAssets.SetString(definition, "description", Description);
                BoardGamesAssets.SetInt(definition, "sortOrder", 6);
                BoardGamesAssets.SetObject(definition, "icon", BoardGamesArtBuilder.Icon("GameIcon"));
                if (scene != null)
                {
                    BoardGamesAssets.SetObject(definition, "scene", scene);
                    BoardGamesAssets.SetString(definition, "scenePath", scenePath);
                }
            });
            var themes = new List<BoardGamesTheme>();
            foreach (BoardGamesThemeSpec spec in BoardGamesThemeSpec.All)
            {
                BoardGamesTheme theme = BoardGamesAssets.Load<BoardGamesTheme>(spec.AssetPath);
                if (theme != null)
                {
                    themes.Add(theme);
                }
            }
            AttachThemes(themes);
        }

        private static void AttachThemes(List<BoardGamesTheme> themes)
        {
            var definition = BoardGamesAssets.Load<GameDefinition>(DefinitionPath);
            if (definition == null || themes.Count == 0)
            {
                return;
            }
            BoardGamesAssets.ApplyIfChanged(definition, () =>
            {
                foreach (BoardGamesTheme theme in themes)
                {
                    definition.AddTheme(theme);
                }
                definition.Theme = themes[0];
            });
            GameThemes.ForgetDefinitions();
        }
    }
}
