using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Portfolio.Asteroids;
using UnityEngine;
using UnityEngine.TestTools;

namespace Portfolio.Asteroids.Tests
{
    public class ShootableTest
    {
        private float secondsTillShotHits = 3f;

        [UnityTest]
        public IEnumerator LootableDropsLootTest()
        {
            yield return TestScenes.Load("LootableDropsLootTest");
            TestScenes.StartRunning();

            var lootables = Object.FindObjectsByType<Lootable>(FindObjectsSortMode.None);
            var lootable = new List<Lootable>(lootables).Find(p => p.isActiveAndEnabled);
            Assert.NotNull(lootable, "No active lootable in the test scene.");
            Assert.True(lootable.gameObject.activeInHierarchy, "Asteroid already inactive.");

            Assert.Zero(Object.FindObjectsByType<Reward>(FindObjectsSortMode.None).Length, "Rewards exist in scene before the test.");

            bool wasHit = false;
            lootable.OnShotEvent += (shootable, shot) => wasHit = true;
            yield return new WaitForSeconds(secondsTillShotHits);
            Assert.True(wasHit, "Asteroid wasn't hit.");
            Assert.False(lootable.gameObject.activeInHierarchy, "Asteroid didn't deactivate when hit.");
            Assert.NotZero(Object.FindObjectsByType<Reward>(FindObjectsSortMode.None).Length, "Lootable did not drop loot.");
        }


        [UnityTest]
        public IEnumerator ExplodableExplodesOthersTest()
        {
            yield return TestScenes.Load("ExplodableExplodesOthersTest");
            TestScenes.StartRunning();

            var shootablesInScene = Object.FindObjectsByType<Shootable>(FindObjectsSortMode.None);
            var shootables = new List<Shootable>(shootablesInScene).FindAll(e => e.gameObject.activeInHierarchy && !e.IsMirror);
            Assert.Greater(shootables.Count, 1, "Not enough active shootables for test.");

            var explodablesInScene = Object.FindObjectsByType<Explodable>(FindObjectsSortMode.None);
            var explodables = new List<Explodable>(explodablesInScene).FindAll(e => e.gameObject.activeInHierarchy && !e.IsMirror);
            var explodable = explodables.Find(e => e.name == "TestExplodable");
            Assert.NotNull(explodable, "Origin explodable not found.");

            yield return new WaitForSeconds(secondsTillShotHits);
            foreach (var shootable in shootables)
            {
                Assert.False(shootable.gameObject.activeInHierarchy, $"{shootable.name} was not exploded.");
            }
        }
    }
}
