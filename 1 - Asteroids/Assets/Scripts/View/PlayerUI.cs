using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace Portfolio
{
    public class PlayerUI : MonoBehaviour
    {
        [SerializeField]
        private Text points;
        [SerializeField]
        private HealthBar health;
        private float playerStartingHealth;


        public void Setup(Player player)
        {
            player.PointsChangedEvent += OnPointsChanged;
            player.HealthChangedEvent += OnHealthChanged;
            playerStartingHealth = player.PlayerSettings.StartingLife;

        }


        private void OnPointsChanged(int amount)
        {
            points.text = $"Score\n{amount}";
        }


        private void OnHealthChanged(float amount)
        {
            health.BarValue = amount * 100 / playerStartingHealth ;
        }


        public void ResetHealth()
        {
            health.enabled = true;
        }
    }
}