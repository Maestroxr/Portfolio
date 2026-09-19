using Gamebox;
using Gamebox.UI;
using TMPro;
using UnityEngine;

namespace Portfolio.Asteroids
{
    /// <summary>
    /// Settings panel of the Asteroids module: ships per mission, hull strength, asteroid speed, how often asteroids
    /// drift in and the blast radius of explosions.
    /// </summary>
    public class AsteroidsSettingsUI : SettingsUI
    {
        [SerializeField] internal TMP_InputField lives;
        [SerializeField] internal TMP_InputField hullStrength;
        [SerializeField] internal TMP_InputField asteroidSpeed;
        [SerializeField] internal TMP_InputField spawnRate;
        [SerializeField] internal TMP_InputField explosionRadius;

        public override void UpdateFromSettings(IGameSettings settings)
        {
            if (!(settings is AsteroidSettings asteroidSettings))
            {
                return;
            }
            SetText(lives, asteroidSettings.Lives);
            SetText(hullStrength, asteroidSettings.HullStrength);
            SetText(asteroidSpeed, asteroidSettings.AsteroidSpeed);
            SetText(spawnRate, asteroidSettings.AsteroidSpawnRate);
            SetText(explosionRadius, asteroidSettings.AsteroidExplosionRadius);
        }

        public override void CopyToSettings(IGameSettings settings)
        {
            if (!(settings is AsteroidSettings asteroidSettings))
            {
                return;
            }
            asteroidSettings.Lives = ParseInt(lives, asteroidSettings.Lives);
            asteroidSettings.HullStrength = ParseFloat(hullStrength, asteroidSettings.HullStrength);
            asteroidSettings.AsteroidSpeed = ParseFloat(asteroidSpeed, asteroidSettings.AsteroidSpeed);
            asteroidSettings.AsteroidSpawnRate = ParseFloat(spawnRate, asteroidSettings.AsteroidSpawnRate);
            asteroidSettings.AsteroidExplosionRadius = ParseFloat(explosionRadius, asteroidSettings.AsteroidExplosionRadius);
        }

        protected override void SetInputsInteractable(bool interactable)
        {
            SetInteractable(lives, interactable);
            SetInteractable(hullStrength, interactable);
            SetInteractable(asteroidSpeed, interactable);
            SetInteractable(spawnRate, interactable);
            SetInteractable(explosionRadius, interactable);
        }
    }
}
