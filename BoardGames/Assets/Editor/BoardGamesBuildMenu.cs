using Gamebox.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Portfolio.BoardGames.EditorTools
{
    /// <summary>
    /// Menu of the Board Games generators. "Build Everything" regenerates the art and sound of the three games, the
    /// content and the scene; the other items rebuild one step from the assets already on disk. The static methods double
    /// as batch mode entry points (<c>-executeMethod Portfolio.BoardGames.EditorTools.BoardGamesBuildMenu.BuildEverything</c>).
    /// </summary>
    internal static class BoardGamesBuildMenu
    {
        [MenuItem("Board Games/Build Everything", priority = 1)]
        public static void BuildEverything()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }
            try
            {
                BoardGamesArtBuilder.BuildAll();
                BoardGamesContentBuilder.BuildAll();
                BoardGamesSceneBuilder.Build();
                // Once more for the definition's scene reference, which needs the saved scene file.
                BoardGamesContentBuilder.BuildDefinition();
                GameSceneBuildSettings.Sync(true);
                AssetDatabase.SaveAssets();
                Debug.Log($"Board Games built under {BoardGamesAssets.Root}.");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        [MenuItem("Board Games/Rebuild Art and Sound", priority = 20)]
        public static void RebuildArt()
        {
            try
            {
                BoardGamesArtBuilder.BuildAll();
                AssetDatabase.SaveAssets();
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        [MenuItem("Board Games/Rebuild Content", priority = 21)]
        public static void RebuildContent()
        {
            BoardGamesContentBuilder.BuildAll();
            AssetDatabase.SaveAssets();
        }

        [MenuItem("Board Games/Rebuild Themes", priority = 22)]
        public static void RebuildThemes()
        {
            BoardGamesContentBuilder.BuildThemes();
            AssetDatabase.SaveAssets();
        }

        [MenuItem("Board Games/Rebuild Scene", priority = 23)]
        public static void RebuildScene()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }
            try
            {
                BoardGamesSceneBuilder.Build();
                BoardGamesContentBuilder.BuildDefinition();
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
