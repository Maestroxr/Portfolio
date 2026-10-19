using Gamebox.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Portfolio.DeathRace.EditorTools
{
    /// <summary>
    /// Menu of the Death Race generators. "Build Everything" regenerates the art and sound, the prefabs, the content and
    /// the scene; the other items rebuild one step from the assets already on disk. The static methods double as batch
    /// mode entry points (<c>-executeMethod Portfolio.DeathRace.EditorTools.DeathRaceBuildMenu.BuildEverything</c>).
    /// </summary>
    internal static class DeathRaceBuildMenu
    {
        [MenuItem("Death Race/Build Everything", priority = 1)]
        public static void BuildEverything()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }
            try
            {
                DeathRaceArtBuilder.BuildAll();
                DeathRacePrefabBuilder.BuildAll();
                DeathRaceContentBuilder.BuildAll();
                DeathRaceSceneBuilder.Build();
                // Once more for the definition's scene reference, which needs the saved scene file.
                DeathRaceContentBuilder.BuildDefinition();
                GameSceneBuildSettings.Sync(true);
                AssetDatabase.SaveAssets();
                Debug.Log($"Death Race built under {DeathRaceAssets.Root}.");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        [MenuItem("Death Race/Rebuild Art and Sound", priority = 20)]
        public static void RebuildArt()
        {
            try
            {
                DeathRaceArtBuilder.BuildAll();
                DeathRacePrefabBuilder.BuildAll();
                AssetDatabase.SaveAssets();
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        [MenuItem("Death Race/Rebuild Content", priority = 21)]
        public static void RebuildContent()
        {
            DeathRaceContentBuilder.BuildAll();
            AssetDatabase.SaveAssets();
        }

        [MenuItem("Death Race/Rebuild Themes", priority = 22)]
        public static void RebuildThemes()
        {
            DeathRaceContentBuilder.BuildThemes();
            AssetDatabase.SaveAssets();
        }

        [MenuItem("Death Race/Rebuild Scene", priority = 23)]
        public static void RebuildScene()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }
            try
            {
                DeathRaceSceneBuilder.Build();
                DeathRaceContentBuilder.BuildDefinition();
                GameSceneBuildSettings.Sync(true);
                AssetDatabase.SaveAssets();
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }
    }
}
