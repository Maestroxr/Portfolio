using System.Collections.Generic;
using Gamebox;
using Gamebox.Launcher;
using UnityEditor;
using UnityEngine;

namespace Portfolio.Checkers.EditorTools
{
    /// <summary>
    /// Writes the data assets of the game: the default settings, the six campaign levels and the campaign, one theme
    /// per look (filled from the art the art builder made) and the launcher's game definition. Assets are updated in
    /// place so their GUIDs, and the screenshots of the themes, survive a rebuild.
    /// </summary>
    internal static class CheckersContentBuilder
    {
        public const string DefaultSettingsPath = "Config/CheckersSettings.asset";
        public const string CampaignPath = "Config/Campaign/CheckersCampaign.asset";
        public const string DefinitionPath = "Resources/Games/Checkers.asset";
        public const string ScenePath = "Scenes/Checkers.unity";

        public const string DisplayName = "Checkers";
        public const string Description = "Draughts for two: English, Russian or International rules, against the computer, at one device or online.";

        private struct LevelSpec
        {
            public string file;
            public string title;
            public Variant variant;
            public int computer;
            public Side side;
            public int piecesKept;
            public int maxPlies;
            public string description;
        }

        private static readonly LevelSpec[] Levels =
        {
            new LevelSpec { file = "Level1", title = "First Moves", variant = Variant.English, computer = 0, side = Side.Dark, piecesKept = 8, maxPlies = 60,
                description = "An easy English game to learn the moves. You play Dark and move first." },
            new LevelSpec { file = "Level2", title = "Crowning Glory", variant = Variant.English, computer = 1, side = Side.Light, piecesKept = 7, maxPlies = 80,
                description = "English rules against a thoughtful opponent. Reach the far rank to crown a king." },
            new LevelSpec { file = "Level3", title = "Flying Kings", variant = Variant.Russian, computer = 1, side = Side.Light, piecesKept = 7, maxPlies = 80,
                description = "Russian rules: men capture backward and kings fly across the board." },
            new LevelSpec { file = "Level4", title = "The Wide Board", variant = Variant.International, computer = 1, side = Side.Light, piecesKept = 12, maxPlies = 120,
                description = "Ten by ten and twenty men a side. The sequence that captures the most pieces is the one you must take." },
            new LevelSpec { file = "Level5", title = "Grandmaster's Table", variant = Variant.English, computer = 2, side = Side.Dark, piecesKept = 6, maxPlies = 100,
                description = "English rules against a hard opponent who sees far ahead." },
            new LevelSpec { file = "Level6", title = "World Championship", variant = Variant.International, computer = 2, side = Side.Light, piecesKept = 10, maxPlies = 140,
                description = "The wide board against the strongest opponent. Keep your pieces and finish it quickly." }
        };

        public static CheckersSettings DefaultSettings => CheckersAssets.Require<CheckersSettings>(DefaultSettingsPath);

        public static CheckersCampaign Campaign => CheckersAssets.Require<CheckersCampaign>(CampaignPath);

        public static CheckersTheme Theme(CheckersThemeSpec spec)
        {
            return CheckersAssets.Require<CheckersTheme>(spec.AssetPath);
        }

        public static CheckersTheme ClassicTheme => Theme(CheckersThemeSpec.Classic);

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
            CheckersAssets.SaveScriptable<CheckersSettings>(DefaultSettingsPath, s =>
            {
                s.showMoves = true;
                s.turnBoard = false;
                s.thinkingDelay = 0.6f;
                s.volume = 0.8f;
                s.variant = (int)Variant.English;
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
                CheckersLevel level = CheckersAssets.SaveScriptable<CheckersLevel>($"Config/Campaign/{spec.file}.asset", l =>
                {
                    l.Configure((int)spec.variant, spec.computer, (int)spec.side, spec.piecesKept, spec.maxPlies, spec.description);
                    CheckersAssets.SetInt(l, "<Index>k__BackingField", index);
                    CheckersAssets.SetString(l, "<Title>k__BackingField", spec.title);
                });
                if (!level.IsLevelValid(out string message))
                {
                    throw new System.InvalidOperationException($"Campaign level {spec.title} is not valid: {message}");
                }
                levels.Add(level);
            }
            CheckersAssets.SaveScriptable<CheckersCampaign>(CampaignPath, c => CheckersAssets.SetObjects(c, "<LevelList>k__BackingField", levels.ToArray()));
        }

        /// <summary>Builds every theme and lists it on the definition when there is one.</summary>
        public static List<CheckersTheme> BuildThemes()
        {
            var themes = new List<CheckersTheme>();
            foreach (CheckersThemeSpec spec in CheckersThemeSpec.All)
            {
                themes.Add(BuildTheme(spec));
            }
            AttachThemes(themes);
            return themes;
        }

        public static CheckersTheme BuildTheme(CheckersThemeSpec spec)
        {
            CheckersTheme theme = CheckersAssets.SaveScriptable<CheckersTheme>(spec.AssetPath, t =>
            {
                CheckersAssets.SetString(t, "displayName", spec.Name);
                CheckersAssets.SetString(t, "description", spec.Description);
                CheckersAssets.SetObject(t, "preview", CheckersArtBuilder.Preview(spec));
                t.sides = new[]
                {
                    new SideLook { nameKey = "Light", piece = CheckersArtBuilder.Material("LightPiece", spec), crown = CheckersArtBuilder.Material("LightCrown", spec), color = spec.LightColor },
                    new SideLook { nameKey = "Dark", piece = CheckersArtBuilder.Material("DarkPiece", spec), crown = CheckersArtBuilder.Material("DarkCrown", spec), color = spec.DarkColor }
                };
                t.lightSquare = CheckersArtBuilder.Material("LightSquare", spec);
                t.darkSquare = CheckersArtBuilder.Material("DarkSquare", spec);
                t.slab = CheckersArtBuilder.Material("Slab", spec);
                t.table = CheckersArtBuilder.Material("Table", spec);
                t.highlight = CheckersArtBuilder.Material("Highlight", spec);
                t.contactShadow = CheckersArtBuilder.Material("ContactShadow", spec);
                t.pieceMesh = CheckersArtBuilder.Mesh("Piece");
                t.crownMesh = CheckersArtBuilder.Mesh("Crown");
                t.squareMesh = CheckersArtBuilder.Mesh("Square");
                t.slabMesh = CheckersArtBuilder.Mesh("Slab");
                t.tableMesh = CheckersArtBuilder.Mesh("Table");
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
                t.gameIcon = spec == CheckersThemeSpec.Classic ? CheckersArtBuilder.Icon("GameIcon") : null;
                // The shared parts of the base theme: the fonts of the two roles and the skin of the shared menu.
                t.Fonts.body = CheckersArtBuilder.BoldFont;
                t.Fonts.bodyMaterial = CheckersArtBuilder.BoldFont.material;
                t.Fonts.title = CheckersArtBuilder.TitleFont;
                t.Fonts.titleMaterial = CheckersArtBuilder.TitleFont.material;
                FillMenu(t, spec);
            });
            var problems = new List<string>();
            if (!theme.Validate(problems))
            {
                throw new System.InvalidOperationException($"Theme {spec.Name} is not complete: {string.Join("; ", problems)}");
            }
            return theme;
        }

        private static void FillMenu(CheckersTheme t, CheckersThemeSpec spec)
        {
            Sprite rounded = CheckersArtBuilder.UI("Rounded");
            Sprite circle = CheckersArtBuilder.UI("Circle");
            MenuSkin menu = t.Menu;
            menu.backdrop = CheckersArtBuilder.UI("Glow");
            menu.backdropColor = WithAlpha(spec.Ink, 0.4f);
            menu.window = rounded;
            menu.windowColor = spec.Paper;
            menu.tileWindow = false;
            menu.settingsWindow = rounded;
            menu.headerBanner = CheckersArtBuilder.UI("Banner");
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
            var definition = CheckersAssets.Load<GameDefinition>(DefinitionPath);
            if (definition == null)
            {
                definition = ScriptableObject.CreateInstance<GameDefinition>();
                CheckersAssets.EnsureFolder("Resources/Games");
                AssetDatabase.CreateAsset(definition, CheckersAssets.Path(DefinitionPath));
            }
            string scenePath = CheckersAssets.Path(ScenePath);
            var scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);
            CheckersAssets.ApplyIfChanged(definition, () =>
            {
                CheckersAssets.Set(definition, "type", p => p.intValue = (int)GameType.Checkers);
                CheckersAssets.SetString(definition, "displayName", DisplayName);
                CheckersAssets.SetString(definition, "description", Description);
                CheckersAssets.SetInt(definition, "sortOrder", 6);
                CheckersAssets.SetObject(definition, "icon", CheckersArtBuilder.Icon("GameIcon"));
                if (scene != null)
                {
                    CheckersAssets.SetObject(definition, "scene", scene);
                    CheckersAssets.SetString(definition, "scenePath", scenePath);
                }
            });
            var themes = new List<CheckersTheme>();
            foreach (CheckersThemeSpec spec in CheckersThemeSpec.All)
            {
                CheckersTheme theme = CheckersAssets.Load<CheckersTheme>(spec.AssetPath);
                if (theme != null)
                {
                    themes.Add(theme);
                }
            }
            AttachThemes(themes);
        }

        private static void AttachThemes(List<CheckersTheme> themes)
        {
            var definition = CheckersAssets.Load<GameDefinition>(DefinitionPath);
            if (definition == null || themes.Count == 0)
            {
                return;
            }
            CheckersAssets.ApplyIfChanged(definition, () =>
            {
                foreach (CheckersTheme theme in themes)
                {
                    definition.AddTheme(theme);
                }
                definition.Theme = themes[0];
            });
            GameThemes.ForgetDefinitions();
        }
    }
}
