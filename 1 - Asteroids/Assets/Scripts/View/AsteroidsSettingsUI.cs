using Gamebox;
using Gamebox.UI;
using TMPro;
using UnityEngine;

namespace Portfolio.Asteroids
{
    /// <summary>Settings panel of the Asteroids module: spawn rate and explosion radius of the asteroids.</summary>
    public class AsteroidsSettingsUI : SettingsUI
    {
        [SerializeField] private TMP_InputField spawnRate;
        [SerializeField] private TMP_InputField explosionRadius;

        public override void UpdateFromSettings(IGameSettings settings)
        {
            if (!(settings is AsteroidSettings asteroidSettings))
            {
                return;
            }
            SetText(spawnRate, asteroidSettings.AsteroidSpawnRate);
            SetText(explosionRadius, asteroidSettings.AsteroidExplosionRadius);
        }

        public override void CopyToSettings(IGameSettings settings)
        {
            if (!(settings is AsteroidSettings asteroidSettings))
            {
                return;
            }
            asteroidSettings.AsteroidSpawnRate = ParseFloat(spawnRate, asteroidSettings.AsteroidSpawnRate);
            asteroidSettings.AsteroidExplosionRadius = ParseFloat(explosionRadius, asteroidSettings.AsteroidExplosionRadius);
        }

        protected override void SetInputsInteractable(bool interactable)
        {
            SetInteractable(spawnRate, interactable);
            SetInteractable(explosionRadius, interactable);
        }
    }
}
