using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Portfolio;

namespace Tests
{
    public class ShootableTest
    {
        private float secondsTillShotHits = 3f;

        [UnityTest]
        public IEnumerator LootableDropsLootTest()
        {
            SceneManager.LoadScene("LootableDropsLootTest");
            yield return null;

            var lootables = GameObject.FindObjectsOfType<Lootable>();
            var lootable = new List<Lootable>(lootables).Find(p => p.isActiveAndEnabled);
            Assert.True(lootable.gameObject.activeInHierarchy, "Asteroid already inactive.");

            Assert.Zero(GameObject.FindObjectsOfType<Reward>().Length, "Rewards exist in scene before the test.");

            bool wasHit = false;
            lootable.OnShotEvent += (shootable, shot) => wasHit = true;
            yield return new WaitForSeconds(secondsTillShotHits);
            Assert.True(wasHit, "Asteroid wasn't hit.");
            Assert.False(lootable.gameObject.activeInHierarchy, "Asteroid didn't deactivate when hit.");
            Assert.NotZero(GameObject.FindObjectsOfType<Reward>().Length, "Lootable did not drop loot.");
        }


        [UnityTest]
        public IEnumerator ExplodableExplodesOthersTest()
        {
            SceneManager.LoadScene("ExplodableExplodesOthersTest");
            yield return null;

            var shootablesInScene = GameObject.FindObjectsOfType<Shootable>();
            var shootables = new List<Shootable>(shootablesInScene).FindAll(e => e.gameObject.activeInHierarchy && !e.IsMirror);
            Assert.Greater(shootables.Count, 1, "Not enough active shootables for test.");

            var explodablesInScene = GameObject.FindObjectsOfType<Explodable>();
            var explodables = new List<Explodable>(explodablesInScene).FindAll(e => e.gameObject.activeInHierarchy && !e.IsMirror);
            var explodable = explodables.Find(e => e.name == "TestExplodable");
            Assert.NotNull(explodable, "Origin explodable not found.");

            yield return new WaitForSeconds(secondsTillShotHits);
            Assert.False(explodable.gameObject.activeInHierarchy, "Origin asteroid wasn't hit.");
            Assert.Zero(explodables.FindAll(e => e.gameObject.activeInHierarchy).Count, "Not all asteroids were destroyed.");
        }
    }
}