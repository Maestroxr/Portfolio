using UnityEngine;
using UnityEngine.UI;

namespace Portfolio.Asteroids
{
    public class PlayerUI : MonoBehaviour
    {
        [SerializeField]
        private Text points;
        [SerializeField]
        private HealthBar health;
        private float playerStartingHealth;


        public void Setup(AsteroidsPlayer player)
        {
            player.PointsChangedEvent += OnPointsChanged;
            player.HealthChangedEvent += OnHealthChanged;
            playerStartingHealth = player.PlayerSettings != null ? player.PlayerSettings.StartingLife : 1f;
        }


        private void OnPointsChanged(int amount)
        {
            if (points != null)
            {
                points.text = $"Score\n{amount}";
            }
        }


        private void OnHealthChanged(float amount)
        {
            if (health != null && playerStartingHealth > 0f)
            {
                health.BarValue = amount * 100 / playerStartingHealth;
            }
        }


        public void ResetHealth()
        {
            if (health != null)
            {
                health.enabled = true;
            }
        }
    }
}
