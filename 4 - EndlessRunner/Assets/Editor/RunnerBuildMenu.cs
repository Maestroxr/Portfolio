using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Portfolio.EndlessRunner.EditorTools
{
    /// <summary>
    /// Menu of the Endless Runner's generators. "Build Everything" regenerates the art, the content (worlds, levels,
    /// campaign) and the scene; the other items rebuild one step from the assets already on disk.
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
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
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
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                RunnerSceneBuilder.Build();
            }
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
