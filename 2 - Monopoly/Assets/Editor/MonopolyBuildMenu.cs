using System;
using System.IO;
using Gamebox;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Portfolio.Monopoly.EditorTools
{
    /// <summary>
    /// Menu of the Monopoly generators. "Build Everything" regenerates the art and sound, the board and game modes and
    /// the scene; the other items rebuild one step from the assets already on disk. The static methods double as batch
    /// mode entry points (<c>-executeMethod Portfolio.Monopoly.EditorTools.MonopolyBuildMenu.BuildEverything</c>).
    /// </summary>
    internal static class MonopolyBuildMenu
    {
        /// <summary>Assets of the original version that nothing uses any more.</summary>
        private static readonly string[] LegacyAssets =
        {
            "Config/Assets",
            "Config/RandomReward.asset",
            "Config/StartReward.asset",
            "Prefabs/AssetTile.prefab",
            "Prefabs/BoardClockwise.prefab",
            "Prefabs/Dialog.prefab",
            "Prefabs/Dice.prefab",
            "Prefabs/Player.prefab",
            "Prefabs/PlayerUI.prefab",
            "Prefabs/RewardTile.prefab",
            "Prefabs/StartTile.prefab",
            "Resources/Textures",
            "Resources/Sounds",
            "Scenes/MonopolySettings.lighting"
        };

        [MenuItem("Monopoly/Build Everything", priority = 1)]
        public static void BuildEverything()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }
            try
            {
                RemoveLegacyAssets();
                MonopolyArtBuilder.BuildAll();
                MonopolyContentBuilder.BuildAll();
                MonopolySceneBuilder.Build();
                AssetDatabase.SaveAssets();
                Debug.Log($"Monopoly built under {MonopolyAssets.Root}.");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        [MenuItem("Monopoly/Rebuild Art and Sound", priority = 20)]
        public static void RebuildArt()
        {
            try
            {
                MonopolyArtBuilder.BuildAll();
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        [MenuItem("Monopoly/Rebuild Board and Modes", priority = 21)]
        public static void RebuildContent()
        {
            MonopolyContentBuilder.BuildAll();
        }

        [MenuItem("Monopoly/Rebuild Scene", priority = 22)]
        public static void RebuildScene()
        {
            if (Application.isBatchMode || EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                MonopolySceneBuilder.Build();
            }
        }

        [MenuItem("Monopoly/Debug/Reset Progress", priority = 100)]
        public static void ResetProgress()
        {
            MonopolyCampaign campaign = MonopolyContentBuilder.Campaign;
            var progress = new MonopolyProgress(new PlayerPrefsStrategy(), GameType.Monopoly);
            progress.ResetAll(campaign != null ? campaign.Count : 6);
            Debug.Log("Monopoly: progress reset.");
        }

        /// <summary>
        /// Batch mode only: builds a Windows development player of the Monopoly scene into the folder given after
        /// <c>-monopolyPlayer</c>, for trying the game (and its screenshot tour) outside the editor.
        /// </summary>
        public static void BuildPlayer()
        {
            string output = Argument("-monopolyPlayer");
            if (string.IsNullOrEmpty(output))
            {
                throw new ArgumentException("Pass the output folder with -monopolyPlayer <folder>.");
            }
            Directory.CreateDirectory(output);
            var options = new BuildPlayerOptions
            {
                scenes = new[] { MonopolyAssets.Path(MonopolySceneBuilder.ScenePath) },
                locationPathName = Path.Combine(output, "Monopoly.exe"),
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development
            };
            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException($"Monopoly player build {report.summary.result}: {report.summary.totalErrors} errors.");
            }
            Debug.Log($"Monopoly player built to {options.locationPathName} ({report.summary.totalSize / 1024 / 1024} MB).");
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

        private static void RemoveLegacyAssets()
        {
            foreach (string relative in LegacyAssets)
            {
                MonopolyAssets.Delete(relative);
            }
        }
    }
}
