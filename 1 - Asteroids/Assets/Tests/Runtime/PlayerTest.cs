using System.Collections;
using Gamebox;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Portfolio.Asteroids.Tests
{
    public class PlayerTest
    {
        [UnityTest]
        public IEnumerator ShipLosesALifeAndRespawns()
        {
            yield return TestScenes.StartFirstMission();
            AsteroidsGameManager manager = TestScenes.Manager;
            AsteroidsPlayer ship = manager.Ship;
            int lives = manager.Lives;
            Assert.IsTrue(ship.IsAlive, "The ship is alive at the start.");

            yield return TestScenes.WaitForInvulnerability(ship);
            ship.TakeDamage(new DamageInfo(ship.MaxHealth + ship.MaxShield + 1f, Vector2.up, ship.Position, DamageSource.Hazard, false));
            Assert.IsFalse(ship.IsAlive, "The ship was destroyed.");
            Assert.That(manager.Lives, Is.EqualTo(lives - 1), "Losing the ship cost a life.");

            yield return new WaitForSeconds(manager.respawnDelay + 0.5f);
            Assert.IsTrue(ship.IsAlive, "A new ship arrived.");
            Assert.Greater(ship.InvulnerableTime, 0f, "The new ship is briefly invulnerable.");
        }


        [UnityTest]
        public IEnumerator ShieldAbsorbsHitsBeforeTheHull()
        {
            yield return TestScenes.StartFirstMission();
            AsteroidsPlayer ship = TestScenes.Manager.Ship;
            yield return TestScenes.WaitForInvulnerability(ship);
            ship.RestoreShield(ship.MaxShield);
            float hull = ship.Health;
            Assert.IsTrue(ship.TakeDamage(new DamageInfo(20f, Vector2.up, ship.Position, DamageSource.Enemy, false)));
            Assert.That(ship.Health, Is.EqualTo(hull), "The shield took the hit.");
            Assert.That(ship.Shield, Is.EqualTo(ship.MaxShield - 20f).Within(0.01f));
        }


        [UnityTest]
        public IEnumerator PickupsUpgradeTheShip()
        {
            yield return TestScenes.StartFirstMission();
            AsteroidsGameManager manager = TestScenes.Manager;
            AsteroidsPlayer ship = manager.Ship;
            SpawnService spawner = manager.spawner;
            WeaponReward weapon = null;
            foreach (RewardPool pool in spawner.rewardPools)
            {
                var reward = pool.PooledPrefab.GetComponent<WeaponReward>();
                if (reward != null && reward.Weapon == WeaponType.Laser)
                {
                    weapon = reward;
                }
            }
            Assert.NotNull(weapon, "The scene has a laser crate.");
            int level = ship.Weapons.Level;
            spawner.SpawnReward(weapon, ship.Position, Vector2.zero);
            yield return new WaitForSeconds(0.4f);
            Assert.That(ship.Weapons.Type, Is.EqualTo(WeaponType.Laser), "Flying through the crate switched weapons.");
            Assert.That(ship.Weapons.Level, Is.EqualTo(level + 1), "The crate raised the weapon level.");
        }
    }


    /// <summary>Loads and starts the game scene for the play tests.</summary>
    public static class TestScenes
    {
        public const string GameScene = "Scenes/Asteroids.unity";

        public static AsteroidsGameManager Manager => Object.FindFirstObjectByType<AsteroidsGameManager>();

        public static IEnumerator Load(string relativePath)
        {
#if UNITY_EDITOR
            UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(
                $"{RootPath}/{relativePath}",
                new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single));
#else
            UnityEngine.SceneManagement.SceneManager.LoadScene(System.IO.Path.GetFileNameWithoutExtension(relativePath));
#endif
            yield return null;
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

        /// <summary>Loads the game and starts its first mission, then waits for the countdown to end.</summary>
        public static IEnumerator StartFirstMission()
        {
            yield return Load(GameScene);
            AsteroidsGameManager manager = Manager;
            Assert.NotNull(manager, "The game scene has no Asteroids manager.");
            manager.Progress?.ResetAll(manager.LevelCount);
            ((IGameController)manager.Controller).PrepareGame(LevelData.Create(0));
            float waited = 0f;
            while (!manager.IsMissionActive && waited < 6f)
            {
                waited += Time.deltaTime;
                yield return null;
            }
            Assert.IsTrue(manager.IsMissionActive, "The mission did not start.");
        }

        public static IEnumerator WaitForInvulnerability(AsteroidsPlayer ship)
        {
            float waited = 0f;
            while (ship.IsInvulnerable && waited < 5f)
            {
                waited += Time.deltaTime;
                yield return null;
            }
        }

        /// <summary>Kept from the original tests: puts the scene's Asteroids manager into the running state.</summary>
        public static void StartRunning()
        {
            AsteroidsGameManager manager = Manager;
            if (manager != null)
            {
                manager.TransitionState(BaseGameState.Running);
            }
        }
    }
}
