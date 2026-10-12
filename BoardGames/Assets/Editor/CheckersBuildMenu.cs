using Gamebox.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Portfolio.Checkers.EditorTools
{
    /// <summary>
    /// Menu of the Checkers generators. "Build Everything" regenerates the art and sound, the content and the scene;
    /// the other items rebuild one step from the assets already on disk. The static methods double as batch mode entry
    /// points (<c>-executeMethod Portfolio.Checkers.EditorTools.CheckersBuildMenu.BuildEverything</c>).
    /// </summary>
    internal static class CheckersBuildMenu
    {
        [MenuItem("Checkers/Build Everything", priority = 1)]
        public static void BuildEverything()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }
            try
            {
                CheckersArtBuilder.BuildAll();
                CheckersContentBuilder.BuildAll();
                CheckersSceneBuilder.Build();
                // Once more for the definition's scene reference, which needs the saved scene file.
                CheckersContentBuilder.BuildDefinition();
                GameSceneBuildSettings.Sync(true);
                AssetDatabase.SaveAssets();
                Debug.Log($"Checkers built under {CheckersAssets.Root}.");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        [MenuItem("Checkers/Rebuild Art and Sound", priority = 20)]
        public static void RebuildArt()
        {
            try
            {
                CheckersArtBuilder.BuildAll();
                AssetDatabase.SaveAssets();
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        [MenuItem("Checkers/Rebuild Content", priority = 21)]
        public static void RebuildContent()
        {
            CheckersContentBuilder.BuildAll();
            AssetDatabase.SaveAssets();
        }

        [MenuItem("Checkers/Rebuild Themes", priority = 22)]
        public static void RebuildThemes()
        {
            CheckersContentBuilder.BuildThemes();
            AssetDatabase.SaveAssets();
        }

        [MenuItem("Checkers/Rebuild Scene", priority = 23)]
        public static void RebuildScene()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }
            try
            {
                CheckersSceneBuilder.Build();
                CheckersContentBuilder.BuildDefinition();
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
