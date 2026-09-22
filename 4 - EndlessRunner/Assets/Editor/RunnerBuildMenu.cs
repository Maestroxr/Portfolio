using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Portfolio.EndlessRunner.EditorTools
{
    /// <summary>
    /// Menu of the Endless Runner's generators. "Build Everything" regenerates the art, the content (worlds, levels,
    /// campaign) and the scene; the other items rebuild one step from the assets already on disk. The static methods
    /// double as batch mode entry points (<c>-executeMethod Portfolio.EndlessRunner.EditorTools.RunnerBuildMenu.RebuildScene</c>).
    /// </summary>
    internal static class RunnerBuildMenu
    {
        private static readonly string[] LegacyAssets =
        {
            "Prefabs/Collidable.prefab",
            "Prefabs/Terrain.prefab",
            "Prefabs/TerrainLayer.terrainlayer",
            "Prefabs/Manager.prefab",
            "Materials",
            "Terrain",
            "Scenes/EndlessRunner"
        };

        [MenuItem("Endless Runner/Build Everything", priority = 1)]
        public static void BuildEverything()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }
            try
            {
                RunnerArtBuilder.BuildAll();
                RunnerContentBuilder.BuildAll();
                RunnerSceneBuilder.Build();
                RemoveLegacyAssets();
                AssetDatabase.SaveAssets();
                Debug.Log($"Endless Runner built under {RunnerAssets.Root}.");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        [MenuItem("Endless Runner/Rebuild Art", priority = 20)]
        public static void RebuildArt()
        {
            RunnerArtBuilder.BuildAll();
        }

        [MenuItem("Endless Runner/Rebuild Worlds and Levels", priority = 21)]
        public static void RebuildContent()
        {
            RunnerContentBuilder.BuildAll();
        }

        [MenuItem("Endless Runner/Rebuild Scene", priority = 22)]
        public static void RebuildScene()
        {
            if (Application.isBatchMode || EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                RunnerSceneBuilder.Build();
            }
        }

        /// <summary>
        /// Batch mode only: builds a Windows player of the Endless Runner scene into the folder given after
        /// <c>-runnerPlayer</c>, for trying the game (and its online tour) outside the editor.
        /// </summary>
        public static void BuildPlayer()
        {
            string output = Argument("-runnerPlayer");
            if (string.IsNullOrEmpty(output))
            {
                throw new ArgumentException("Pass the output folder with -runnerPlayer <folder>.");
            }
            Directory.CreateDirectory(output);
            var options = new BuildPlayerOptions
            {
                scenes = new[] { RunnerAssets.Path(RunnerSceneBuilder.ScenePath) },
                locationPathName = Path.Combine(output, "EndlessRunner.exe"),
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development
            };
            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException($"Endless Runner player build {report.summary.result}: {report.summary.totalErrors} errors.");
            }
            Debug.Log($"Endless Runner player built to {options.locationPathName} ({report.summary.totalSize / 1024 / 1024} MB).");
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

        /// <summary>Deletes the assets of the original terrain-and-capsule version that nothing uses any more.</summary>
        private static void RemoveLegacyAssets()
        {
            foreach (string relative in LegacyAssets)
            {
                string path = RunnerAssets.Path(relative);
                if (AssetDatabase.IsValidFolder(path) || AssetDatabase.LoadMainAssetAtPath(path) != null)
                {
                    AssetDatabase.DeleteAsset(path);
                }
            }
        }
    }
}
