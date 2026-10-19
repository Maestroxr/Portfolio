using System.Collections.Generic;
using Gamebox;
using Gamebox.Launcher;
using UnityEditor;
using UnityEngine;

namespace Portfolio.DeathRace.EditorTools
{
    /// <summary>
    /// Writes the data assets of the game: the default settings, one campaign level per track and the campaign, the
    /// content asset (pointed at the built prefabs, effects and sounds by <see cref="DeathRacePrefabBuilder"/>), the looks
    /// (<see cref="DeathRaceThemeBuilder"/>) and the launcher's game definition. Assets are updated in place, so their
    /// GUIDs and the screenshots of the themes survive a rebuild.
    /// </summary>
    internal static class DeathRaceContentBuilder
    {
        public const string DefaultSettingsPath = "Config/DeathRaceSettings.asset";
        public const string CampaignPath = "Config/Campaign/DeathRaceCampaign.asset";
        public const string ContentPath = "Config/DeathRaceContent.asset";
        public const string DefinitionPath = "Resources/Games/DeathRace.asset";
        public const string ScenePath = "Scenes/DeathRace.unity";
        public const string Database = "skinnerboxes-deathrace";

        public const string DisplayName = "Death Race";
        public const string Description = "Armed cars, nine deadly tracks and a ladder of twenty drivers: race, shoot and ram your way to the top, and spend the prize money on a better car.";

        public static DeathRaceSettings DefaultSettings => DeathRaceAssets.Require<DeathRaceSettings>(DefaultSettingsPath);

        public static DeathRaceCampaign Campaign => DeathRaceAssets.Require<DeathRaceCampaign>(CampaignPath);

        public static DeathRaceContent Content => DeathRaceAssets.Require<DeathRaceContent>(ContentPath);

        public static void BuildAll()
        {
            BuildSettings();
            BuildCampaign();
            BuildContent();
            BuildThemes();
            BuildDefinition();
            AssetDatabase.SaveAssets();
        }

        private static void BuildSettings()
        {
            DeathRaceAssets.SaveScriptable<DeathRaceSettings>(DefaultSettingsPath, s =>
            {
                s.volume = 0.8f;
                s.musicVolume = 0.5f;
                s.cameraDistance = 1;
                s.screenShake = true;
                s.showMinimap = true;
                s.quickLaps = 0;
                s.quickCar = (int)CarModel.Viper;
                s.quickDifficulty = (int)Difficulty.Medium;
            });
        }

        /// <summary>One level per track, in the order of the track table: the level's index is the track's.</summary>
        private static void BuildCampaign()
        {
            var levels = new List<GameLevel>();
            for (int i = 0; i < TrackLayouts.Count; i++)
            {
                TrackLayout layout = TrackLayouts.Of(i);
                int index = i;
                DeathRaceLevel level = DeathRaceAssets.SaveScriptable<DeathRaceLevel>($"Config/Campaign/Track{i + 1}.asset", l =>
                {
                    l.Configure(index, layout.Biome, layout.Laps, layout.Showdown, layout.Description);
                    DeathRaceAssets.SetInt(l, "<Index>k__BackingField", index);
                    DeathRaceAssets.SetString(l, "<Title>k__BackingField", layout.Name);
                });
                if (!level.IsLevelValid(out string message))
                {
                    throw new System.InvalidOperationException($"Track {layout.Name} is not valid: {message}");
                }
                levels.Add(level);
            }
            DeathRaceAssets.SaveScriptable<DeathRaceCampaign>(CampaignPath, c => DeathRaceAssets.SetObjects(c, "<LevelList>k__BackingField", levels.ToArray()));
        }

        private static void BuildContent()
        {
            DeathRaceAssets.SaveScriptable<DeathRaceContent>(ContentPath, DeathRacePrefabBuilder.FillContent);
        }

        /// <summary>Builds every look and lists them on the definition when there is one.</summary>
        public static List<DeathRaceTheme> BuildThemes()
        {
            List<DeathRaceTheme> themes = DeathRaceThemeBuilder.BuildThemes();
            AttachThemes(themes);
            return themes;
        }

        /// <summary>
        /// The launcher's definition: created once, then updated in place. The scene reference is set when the scene file
        /// exists, so the step runs again after the scene builder.
        /// </summary>
        public static void BuildDefinition()
        {
            var definition = DeathRaceAssets.Load<GameDefinition>(DefinitionPath);
            if (definition == null)
            {
                definition = ScriptableObject.CreateInstance<GameDefinition>();
                DeathRaceAssets.EnsureFolder("Resources/Games");
                AssetDatabase.CreateAsset(definition, DeathRaceAssets.Path(DefinitionPath));
            }
            string scenePath = DeathRaceAssets.Path(ScenePath);
            var scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);
            Sprite icon = DeathRaceArtBuilder.Icon("GameIcon");
            DeathRaceAssets.ApplyIfChanged(definition, () =>
            {
                DeathRaceAssets.Set(definition, "type", p => p.intValue = (int)GameType.DeathRace);
                DeathRaceAssets.SetString(definition, "displayName", DisplayName);
                DeathRaceAssets.SetString(definition, "description", Description);
                DeathRaceAssets.SetInt(definition, "sortOrder", 7);
                DeathRaceAssets.SetString(definition, "database", Database);
                if (icon != null)
                {
                    DeathRaceAssets.SetObject(definition, "icon", icon);
                }
                if (scene != null)
                {
                    DeathRaceAssets.SetObject(definition, "scene", scene);
                    DeathRaceAssets.SetString(definition, "scenePath", scenePath);
                }
            });
            var themes = new List<DeathRaceTheme>();
            foreach (string path in new[] { DeathRaceThemeBuilder.ScorchedPath, DeathRaceThemeBuilder.NightfallPath })
            {
                DeathRaceTheme theme = DeathRaceAssets.Load<DeathRaceTheme>(path);
                if (theme != null)
                {
                    themes.Add(theme);
                }
            }
            AttachThemes(themes);
        }

        private static void AttachThemes(List<DeathRaceTheme> themes)
        {
            var definition = DeathRaceAssets.Load<GameDefinition>(DefinitionPath);
            if (definition == null || themes.Count == 0)
            {
                return;
            }
            DeathRaceAssets.ApplyIfChanged(definition, () =>
            {
                foreach (DeathRaceTheme theme in themes)
                {
                    definition.AddTheme(theme);
                }
                definition.Theme = themes[0];
            });
            GameThemes.ForgetDefinitions();
        }
    }
}
