using System;
using System.IO;
using Gamebox;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Portfolio.MemoryCards.EditorTools
{
    /// <summary>
    /// Menu of the Memory Cards generators. "Build Everything" regenerates the art and sound, the worlds and levels and
    /// the scene; the other items rebuild one step from the assets already on disk. The static methods double as batch
    /// mode entry points (<c>-executeMethod Portfolio.MemoryCards.EditorTools.MemoryCardsBuildMenu.BuildEverything</c>).
    /// </summary>
    internal static class MemoryCardsBuildMenu
    {
        /// <summary>Assets of the original version that nothing uses any more.</summary>
        private static readonly string[] LegacyAssets =
        {
            "Prefabs/GameManager.prefab",
            "Prefabs/ItemLine.prefab",
            "Prefabs/MenuPanel.prefab",
            "Resources/Textures",
            "Resources/SoundBits_FreeSFX",
            "Scenes/MemoryCards",
            "Scenes/MemoryCardsSettings.lighting"
        };

        [MenuItem("Memory Cards/Build Everything", priority = 1)]
        public static void BuildEverything()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }
            try
            {
                MemoryCardsArtBuilder.BuildAll();
                MemoryCardsContentBuilder.BuildAll();
                MemoryCardsSceneBuilder.Build();
                RemoveLegacyAssets();
                AssetDatabase.SaveAssets();
                Debug.Log($"Memory Cards built under {MemoryCardsAssets.Root}.");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        [MenuItem("Memory Cards/Rebuild Art and Sound", priority = 20)]
        public static void RebuildArt()
        {
            try
            {
                MemoryCardsArtBuilder.BuildAll();
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        [MenuItem("Memory Cards/Rebuild Worlds and Levels", priority = 21)]
        public static void RebuildContent()
        {
            MemoryCardsContentBuilder.BuildAll();
        }

        [MenuItem("Memory Cards/Rebuild Scene", priority = 22)]
        public static void RebuildScene()
        {
            if (Application.isBatchMode || EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                MemoryCardsSceneBuilder.Build();
            }
        }

        /// <summary>Gives every campaign level three stars in this editor's saved progress (for trying later worlds).</summary>
        [MenuItem("Memory Cards/Debug/Unlock All Levels", priority = 100)]
        public static void UnlockAll()
        {
            MemoryCardsCampaign campaign = MemoryCardsContentBuilder.Campaign;
            if (campaign == null)
            {
                return;
            }
            var progress = new MemoryCardsProgress(new PlayerPrefsStrategy(), GameType.MemoryCards);
            for (int i = 0; i < campaign.Count; i++)
            {
                if (campaign.IsCampaignLevel(i))
                {
                    progress.RecordLevel(i, 3, 0);
                }
            }
            Debug.Log("Memory Cards: every level unlocked with three stars.");
        }

        [MenuItem("Memory Cards/Debug/Reset Progress", priority = 101)]
        public static void ResetProgress()
        {
            MemoryCardsCampaign campaign = MemoryCardsContentBuilder.Campaign;
            var progress = new MemoryCardsProgress(new PlayerPrefsStrategy(), GameType.MemoryCards);
            progress.ResetAll(campaign != null ? campaign.Count : 24);
            Debug.Log("Memory Cards: progress reset.");
        }

        /// <summary>
        /// Batch mode only: builds a Windows player of the Memory Cards scene into the folder given after
        /// <c>-memoryCardsPlayer</c>, for trying the game (and its autopilot tour) outside the editor.
        /// </summary>
        public static void BuildPlayer()
        {
            string output = Argument("-memoryCardsPlayer");
            if (string.IsNullOrEmpty(output))
            {
                throw new ArgumentException("Pass the output folder with -memoryCardsPlayer <folder>.");
            }
            Directory.CreateDirectory(output);
            var options = new BuildPlayerOptions
            {
                scenes = new[] { MemoryCardsAssets.Path(MemoryCardsSceneBuilder.ScenePath) },
                locationPathName = Path.Combine(output, "MemoryCards.exe"),
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development
            };
            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException($"Memory Cards player build {report.summary.result}: {report.summary.totalErrors} errors.");
            }
            Debug.Log($"Memory Cards player built to {options.locationPathName} ({report.summary.totalSize / 1024 / 1024} MB).");
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
                MemoryCardsAssets.Delete(relative);
            }
        }
    }
}
