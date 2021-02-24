using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Portfolio;

namespace Tests
{
    public class PlayerTest
    {
        private float secondsTillDeath = 3f;
        [UnityTest]
        public IEnumerator PlayerDeathTest()
        {
            SceneManager.LoadScene("PlayerDeathTest");
            yield return null;

            var players = GameObject.FindObjectsOfType<Player>();
            var player = new List<Player>(players).Find(p => !p.IsMirror);
            Assert.Positive(player.Health, "Player dead before test start");
            yield return null;

            yield return new WaitForSeconds(secondsTillDeath);
            Assert.LessOrEqual(player.Health, 0, $"Player {player.name} isn't dead");
        }
    }
}
