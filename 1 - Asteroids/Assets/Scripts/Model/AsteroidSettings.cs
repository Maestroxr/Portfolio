using Gamebox;
using UnityEngine;

namespace Portfolio.Asteroids
{
    /// <summary>
    /// The user configurable part of a mission: how many ships the player has, how much a hull takes, how fast the
    /// asteroids fly, how often new ones drift in and how far explosions reach. Every campaign level carries its own
    /// settings asset; the settings menu edits a custom copy that the base game saves and applies to every level.
    /// </summary>
    [CreateAssetMenu(fileName = "AsteroidSettings", menuName = "Asteroids/AsteroidSettings", order = 1)]
    public class AsteroidSettings : GameSettings
    {
        public const int LivesLimit = 9;

        [field: SerializeField, Tooltip("Ships the player can lose before the mission fails.")]
        public int Lives { get; set; } = 3;

        [field: SerializeField, Tooltip("Hull points of the ship; every hit takes some away, the shield absorbs hits first.")]
        public float HullStrength { get; set; } = 100f;

        [field: SerializeField, Tooltip("Speed multiplier of the asteroids.")]
        public float AsteroidSpeed { get; set; } = 1f;

        [field: SerializeField, Tooltip("Seconds between the asteroids that keep drifting in during survival, collection and boss missions.")]
        public float AsteroidSpawnRate { get; set; } = 6f;

        [field: SerializeField, Tooltip("Whether asteroids keep drifting in between the waves.")]
        public bool SpawnAsteroid { get; set; } = true;

        [field: SerializeField, Tooltip("Blast radius of exploding magma rocks, mines and bombs, in meters.")]
        public float AsteroidExplosionRadius { get; set; } = 3f;


        /// <summary>Kept for the original call sites; same as <see cref="AreSettingsValid"/>.</summary>
        public bool Assert(out string error)
        {
            return AreSettingsValid(out error);
        }


        public override bool AreSettingsValid(out string message)
        {
            if (Lives < 1 || Lives > LivesLimit)
            {
                message = $"Lives {Lives} has to be between 1 and {LivesLimit}";
                return false;
            }
            if (HullStrength < 10f || HullStrength > 1000f)
            {
                message = $"Hull strength {HullStrength} has to be between 10 and 1000";
                return false;
            }
            if (AsteroidSpeed < 0.2f || AsteroidSpeed > 4f)
            {
                message = $"Asteroid speed {AsteroidSpeed} has to be between 0.2 and 4";
                return false;
            }
            if (AsteroidSpawnRate <= 0f || AsteroidSpawnRate > 120f)
            {
                message = $"Asteroid spawn rate {AsteroidSpawnRate} has to be positive and at most 120 seconds";
                return false;
            }
            if (AsteroidExplosionRadius < 0f || AsteroidExplosionRadius > 12f)
            {
                message = $"Asteroid explosion radius {AsteroidExplosionRadius} has to be between 0 and 12";
                return false;
            }
            message = "OK";
            return true;
        }


        public override void SaveSettings(IStorageStrategy storage, string prefix)
        {
            if (!AreSettingsValid(out string error))
            {
                throw new GameSettingsException($"Cannot save invalid settings. Reason: {error}");
            }
            storage.SetInt($"{prefix}Lives", Lives);
            storage.SetFloat($"{prefix}HullStrength", HullStrength);
            storage.SetFloat($"{prefix}AsteroidSpeed", AsteroidSpeed);
            storage.SetFloat($"{prefix}AsteroidSpawnRate", AsteroidSpawnRate);
            storage.SetFloat($"{prefix}AsteroidExplosionRadius", AsteroidExplosionRadius);
            storage.SetBool($"{prefix}SpawnAsteroid", SpawnAsteroid);
        }


        public override void LoadSettings(IStorageStrategy storage, string prefix)
        {
            if (storage.DoesKeyExist($"{prefix}Lives"))
            {
                Lives = storage.GetInt($"{prefix}Lives");
            }
            HullStrength = storage.GetFloat($"{prefix}HullStrength", HullStrength);
            AsteroidSpeed = storage.GetFloat($"{prefix}AsteroidSpeed", AsteroidSpeed);
            AsteroidSpawnRate = storage.GetFloat($"{prefix}AsteroidSpawnRate", AsteroidSpawnRate);
            AsteroidExplosionRadius = storage.GetFloat($"{prefix}AsteroidExplosionRadius", AsteroidExplosionRadius);
            if (storage.DoesKeyExist($"{prefix}SpawnAsteroid"))
            {
                SpawnAsteroid = storage.GetBool($"{prefix}SpawnAsteroid");
            }
        }


        public override void CopySettings(IGameSettings other)
        {
            if (!(other is AsteroidSettings source))
            {
                return;
            }
            Lives = source.Lives;
            HullStrength = source.HullStrength;
            AsteroidSpeed = source.AsteroidSpeed;
            AsteroidSpawnRate = source.AsteroidSpawnRate;
            SpawnAsteroid = source.SpawnAsteroid;
            AsteroidExplosionRadius = source.AsteroidExplosionRadius;
        }
    }
}
