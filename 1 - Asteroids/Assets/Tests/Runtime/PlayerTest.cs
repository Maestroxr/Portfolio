using System.Collections;
using System.Collections.Generic;
using Gamebox;
using NUnit.Framework;
using Portfolio.Asteroids;
using UnityEngine;
using UnityEngine.TestTools;

namespace Portfolio.Asteroids.Tests
{
    public class PlayerTest
    {
        private float secondsTillDeath = 3f;

        [UnityTest]
        public IEnumerator PlayerDeathTest()
        {
            yield return TestScenes.Load("PlayerDeathTest");

            var players = Object.FindObjectsByType<AsteroidsPlayer>(FindObjectsSortMode.None);
            var player = new List<AsteroidsPlayer>(players).Find(p => !p.IsMirror);
            Assert.NotNull(player, "No player in the test scene");
            Assert.Positive(player.Health, "Player dead before test start");
            TestScenes.StartRunning();
            yield return null;

            yield return new WaitForSeconds(secondsTillDeath);
            Assert.LessOrEqual(player.Health, 0, $"Player {player.name} isn't dead");
        }
    }

    /// <summary>Loads the test scenes without requiring them in the build settings.</summary>
    public static class TestScenes
    {
        public const string Folder = "Tests/Runtime/Scenes/";

        public static IEnumerator Load(string sceneName)
        {
#if UNITY_EDITOR
            UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(
                $"{RootPath}/{Folder}{sceneName}.unity",
                new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single));
#else
            UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
#endif
            yield return null;
        }

#if UNITY_EDITOR
        /// <summary>
        /// Asset path of the folder these tests ship in: "Assets" in the game's own project, the package folder
        /// when BaseGame references that Assets folder as the package com.skinnerboxes.asteroids.
        /// </summary>
        private static string RootPath
        {
            get
            {
                var package = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(TestScenes).Assembly);
                return package != null ? package.assetPath : "Assets";
            }
        }
#endif

        /// <summary>Puts the scene's Asteroids manager into the running state so players and asteroids simulate.</summary>
        public static void StartRunning()
        {
            var manager = Object.FindFirstObjectByType<AsteroidsGameManager>();
            if (manager != null)
            {
                manager.TransitionState(BaseGameState.Running);
            }
        }
    }
}
