using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Portfolio.Heroes.EditorTools
{
    /// <summary>
    /// The Heroes menu: builds the whole game from what was downloaded and what the builders draw, in the order the
    /// pieces depend on each other. Everything can be rebuilt on its own, and everything is written in place, so the
    /// prefabs and assets the scene points at keep their identities.
    /// </summary>
    internal static class HeroesBuildMenu
    {
        [MenuItem("Heroes/Build Everything", false, 1)]
        public static void BuildEverything()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }
            try
            {
                EditorUtility.DisplayProgressBar("Heroes", "Art and sound...", 0.1f);
                HeroesArtBuilder.Build(HeroesThemeSpec.Classic);
                EditorUtility.DisplayProgressBar("Heroes", "The Grim Realm...", 0.4f);
                HeroesArtBuilder.Build(HeroesThemeSpec.GrimRealm);
                EditorUtility.DisplayProgressBar("Heroes", "Scenarios...", 0.6f);
                HeroesContentBuilder.BuildAll();
                EditorUtility.DisplayProgressBar("Heroes", "Scene...", 0.85f);
                HeroesSceneBuilder.Build();
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
            Debug.Log("Heroes: everything built.");
        }

        /// <summary>
        /// The art of the second theme and the theme assets (with the definition's list of them), without the classic art
        /// or the scenes, which do not change with it. In batch mode
        /// -executeMethod Portfolio.Heroes.EditorTools.HeroesBuildMenu.BuildGrimRealmAndThemes.
        /// </summary>
        [MenuItem("Heroes/Build Grim Realm and Themes", false, 2)]
        public static void BuildGrimRealmAndThemes()
        {
            try
            {
                EditorUtility.DisplayProgressBar("Heroes", "The Grim Realm...", 0.1f);
                HeroesArtBuilder.Build(HeroesThemeSpec.GrimRealm);
                EditorUtility.DisplayProgressBar("Heroes", "Themes...", 0.9f);
                HeroesThemeBuilder.BuildAll();
                AssetDatabase.SaveAssets();
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
            Debug.Log("Heroes: the Grim Realm and the themes built.");
        }

        [MenuItem("Heroes/Debug/Reset Progress", false, 100)]
        public static void ResetProgress()
        {
            HeroesCampaign campaign = HeroesContentBuilder.Campaign;
            var progress = new HeroesProgress(new Gamebox.PlayerPrefsStrategy(), Gamebox.GameType.Heroes);
            progress.ResetAll(campaign != null ? campaign.Count : 12);
            Debug.Log("Heroes: progress reset.");
        }

        [MenuItem("Heroes/Debug/Clear Saved Games", false, 101)]
        public static void ClearSaves()
        {
            for (int i = 0; i < 24; i++)
            {
                PlayerPrefs.DeleteKey($"Heroes.Save.{i}");
            }
            PlayerPrefs.DeleteKey("Heroes.Save.Level");
            PlayerPrefs.Save();
            Debug.Log("Heroes: saved games cleared.");
        }
    }
}
