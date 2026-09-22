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
                AsteroidsArtBuilder.BuildAll();
                AsteroidsContentBuilder.BuildHulls();
                AsteroidsPrefabBuilder.BuildAll();
                AsteroidsContentBuilder.BuildAll();
                AsteroidsSceneBuilder.Build();
                AssetDatabase.SaveAssets();
                Debug.Log($"Asteroids built under {AsteroidsAssets.Root}.");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
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
            try
            {
                AsteroidsContentBuilder.BuildHulls();
                AsteroidsPrefabBuilder.BuildAll();
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        [MenuItem("Asteroids/Rebuild Campaign", priority = 22)]
        public static void RebuildContent()
        {
            try
            {
                AsteroidsContentBuilder.BuildAll();
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        [MenuItem("Asteroids/Rebuild Scene", priority = 23)]
        public static void RebuildScene()
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                AsteroidsSceneBuilder.Build();
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
    }
}
