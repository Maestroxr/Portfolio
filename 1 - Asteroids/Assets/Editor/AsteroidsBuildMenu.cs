using System;
using System.IO;
using Gamebox;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Portfolio.Asteroids.EditorTools
{
    /// <summary>
    /// Menu of the Asteroids module's generators. "Build Everything" regenerates the art, the hangar, the prefabs, the
    /// campaign content and the scene; the other items rebuild one step from the assets already on disk.
    /// </summary>
    internal static class AsteroidsBuildMenu
    {
        [MenuItem("Asteroids/Build Everything", priority = 1)]
        public static void BuildEverything()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }
            try
            {
                BuildSteps();
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        /// <summary>
        /// Batch mode (<c>-executeMethod</c>): the steps of Build Everything without the question about unsaved scenes. An
        /// exception, or a build problem (<see cref="AsteroidsAssets.Problems"/>: an invalid strike level, a missing strike
        /// prefab), is logged and ends the editor with exit code 1.
        /// </summary>
        public static void BuildEverythingBatch()
        {
            try
            {
                BuildSteps();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorUtility.ClearProgressBar();
                EditorApplication.Exit(1);
                return;
            }
            EditorUtility.ClearProgressBar();
            if (AsteroidsAssets.Problems.Count > 0)
            {
                EditorApplication.Exit(1);
            }
        }

        private static void BuildSteps()
        {
            AsteroidsAssets.Problems.Clear();
            AsteroidsArtBuilder.BuildAll();
            AsteroidsContentBuilder.BuildHulls();
            AsteroidsPrefabBuilder.BuildAll();
            AsteroidsContentBuilder.BuildAll();
            AsteroidsThemeBuilder.BuildAll();
            AsteroidsSceneBuilder.Build();
            AssetDatabase.SaveAssets();
            if (!ReportProblems())
            {
                Debug.Log($"Asteroids built under {AsteroidsAssets.Root}.");
            }
        }

        /// <summary>Logs the build's problems as one error; whether there were any.</summary>
        private static bool ReportProblems()
        {
            if (AsteroidsAssets.Problems.Count == 0)
            {
                return false;
            }
            Debug.LogError($"Asteroids built under {AsteroidsAssets.Root} with {AsteroidsAssets.Problems.Count} problems:\n  " +
                string.Join("\n  ", AsteroidsAssets.Problems));
            return true;
        }

        [MenuItem("Asteroids/Rebuild Art", priority = 20)]
        public static void RebuildArt()
        {
            try
            {
                AsteroidsArtBuilder.BuildAll();
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        [MenuItem("Asteroids/Rebuild Prefabs", priority = 21)]
        public static void RebuildPrefabs()
        {
            AsteroidsAssets.Problems.Clear();
            try
            {
                AsteroidsContentBuilder.BuildHulls();
                AsteroidsPrefabBuilder.BuildAll();
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
            ReportProblems();
        }

        [MenuItem("Asteroids/Rebuild Campaign", priority = 22)]
        public static void RebuildContent()
        {
            AsteroidsAssets.Problems.Clear();
            try
            {
                AsteroidsContentBuilder.BuildAll();
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
            ReportProblems();
        }

        /// <summary>The theme assets (Classic and the generated looks) from the art and prefabs already on disk.</summary>
        [MenuItem("Asteroids/Rebuild Themes", priority = 23)]
        public static void RebuildThemes()
        {
            AsteroidsAssets.Problems.Clear();
            try
            {
                AsteroidsThemeBuilder.BuildAll();
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
            ReportProblems();
        }

        [MenuItem("Asteroids/Rebuild Scene", priority = 24)]
        public static void RebuildScene()
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                AsteroidsAssets.Problems.Clear();
                AsteroidsSceneBuilder.Build();
                ReportProblems();
            }
        }

        /// <summary>
        /// Batch mode only: builds a Windows development player of the Asteroids scene into the folder given after
        /// <c>-asteroidsPlayer</c>, for trying the game (and the tour of its online missions) outside the editor.
        /// </summary>
        public static void BuildPlayer()
        {
            string output = Argument("-asteroidsPlayer");
            if (string.IsNullOrEmpty(output))
            {
                throw new ArgumentException("Pass the output folder with -asteroidsPlayer <folder>.");
            }
            Directory.CreateDirectory(output);
            var options = new BuildPlayerOptions
            {
                scenes = new[] { AsteroidsAssets.Path(AsteroidsSceneBuilder.ScenePath) },
                locationPathName = Path.Combine(output, "Asteroids.exe"),
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development
            };
            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException($"Asteroids player build {report.summary.result}: {report.summary.totalErrors} errors.");
            }
            Debug.Log($"Asteroids player built to {options.locationPathName} ({report.summary.totalSize / 1024 / 1024} MB).");
        }

        private static string Argument(string name)
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == name)
                {
                    return args[i + 1];
                }
            }
            return null;
        }

        /// <summary>
        /// Reads the game's words (Localization/Asteroids.csv) into its string tables, and only those (the shared table is
        /// BaseGame's). Batch mode: <c>-executeMethod Portfolio.Asteroids.EditorTools.AsteroidsBuildMenu.ImportWords</c>.
        /// </summary>
        [MenuItem("Asteroids/Import Words", priority = 30)]
        public static void ImportWords()
        {
            foreach (string csv in Gamebox.Editor.LocalizationTools.FindCsvFiles())
            {
                if (Path.GetFileName(csv) == "Asteroids.csv")
                {
                    Gamebox.Editor.LocalizationTools.ImportCsv(csv);
                    AssetDatabase.SaveAssets();
                    Debug.Log($"Asteroids: the words of {csv} are in the string tables.");
                    return;
                }
            }
            Debug.LogError("Asteroids: Asteroids.csv was not found.");
        }

        /// <summary>Gives every campaign mission three stars in this editor's saved progress (for trying later sectors).</summary>
        [MenuItem("Asteroids/Debug/Unlock All Missions", priority = 100)]
        public static void UnlockAll()
        {
            AsteroidsCampaign campaign = AsteroidsContentBuilder.Campaign;
            if (campaign == null)
            {
                return;
            }
            var progress = new AsteroidsProgress(new PlayerPrefsStrategy(), GameType.Asteroids);
            for (int i = 0; i < campaign.Count; i++)
            {
                progress.RecordLevel(i, 3, 0);
            }
            Debug.Log("Asteroids: every mission unlocked with three stars.");
        }

        [MenuItem("Asteroids/Debug/Reset Progress", priority = 101)]
        public static void ResetProgress()
        {
            AsteroidsCampaign campaign = AsteroidsContentBuilder.Campaign;
            var progress = new AsteroidsProgress(new PlayerPrefsStrategy(), GameType.Asteroids);
            progress.ResetAll(campaign != null ? campaign.Count : 20);
            Debug.Log("Asteroids: progress reset.");
        }

        /// <summary>Adds 2,000,000 money to the strike pilot in this editor's saved progress (for trying the Supply Room).</summary>
        [MenuItem("Asteroids/Debug/Strike: Add 2,000,000", priority = 110)]
        public static void AddStrikeMoney()
        {
            var progress = new AsteroidsProgress(new PlayerPrefsStrategy(), GameType.Asteroids);
            StrikeLoadout pilot = progress.LoadPilot();
            pilot.Money = StrikeRules.AddToWallet(pilot.Money, 2000000);
            progress.SavePilot(pilot);
            Debug.Log($"Asteroids: the strike pilot has {pilot.Money:N0} money.");
        }

        /// <summary>Forgets the strike pilot in this editor's saved progress: the next strike mission starts with a new pilot.</summary>
        [MenuItem("Asteroids/Debug/Strike: Reset Pilot", priority = 111)]
        public static void ResetStrikePilot()
        {
            var progress = new AsteroidsProgress(new PlayerPrefsStrategy(), GameType.Asteroids);
            progress.ResetPilot();
            Debug.Log("Asteroids: the strike pilot was reset.");
        }
    }
}
