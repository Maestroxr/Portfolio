using System.Collections.Generic;
using Gamebox;
using UnityEngine;

namespace Portfolio.Asteroids
{
    /// <summary>
    /// Spawn profile of the asteroid field. The spawn rate, explosion radius and spawning switch are the user
    /// configurable part that the settings menu edits and the base game saves as custom settings.
    /// </summary>
    [CreateAssetMenu(fileName = "AsteroidSettings", menuName = "Asteroids/AsteroidSettings", order = 1)]
    public class AsteroidSettings : GameSettings
    {
        [field: SerializeField]
        public List<Shootable> AsteroidObjects { get; private set; } = new List<Shootable>();
        [field: SerializeField]
        public List<int> SpawnProbabilities { get; private set; } = new List<int>();
        [field: SerializeField]
        public float AsteroidExplosionRadius { get; set; }
        [field: SerializeField]
        public float AsteroidSpawnRate { get; set; }
        [field: SerializeField]
        public bool SpawnAsteroid { get; set; }


        public Shootable RandomShootable()
        {
            int random = Random.Range(0, 100);
            int probability = 0;
            for (int i = 0; i < SpawnProbabilities.Count; i++)
            {
                probability += SpawnProbabilities[i];
                if (random <= probability)
                {
                    return AsteroidObjects[i];
                }
            }
            if (probability > 100)
            {
                throw new System.ArgumentException("Reward has not been asserted");
            }
            return null;
        }


        /// <summary>Kept for the original call sites; same as <see cref="AreSettingsValid"/>.</summary>
        public bool Assert(out string error)
        {
            return AreSettingsValid(out error);
        }


        public override bool AreSettingsValid(out string message)
        {
            if (AsteroidObjects == null || SpawnProbabilities == null ||
                AsteroidObjects.Count != SpawnProbabilities.Count || AsteroidObjects.Count == 0)
            {
                message = $"Probabilities {SpawnProbabilities?.Count ?? 0} and asteroid amounts {AsteroidObjects?.Count ?? 0} are invalid";
                return false;
            }
            int probabilitySum = 0;
            for (int i = 0; i < SpawnProbabilities.Count; i++)
            {
                int probability = SpawnProbabilities[i];
                if (probability <= 0)
                {
                    message = "Non-positive probability";
                    return false;
                }

                probabilitySum += probability;
            }

            if (probabilitySum > 100)
            {
                message = "Probabilities amount to more than 100";
                return false;
            }
            if (AsteroidSpawnRate <= 0f)
            {
                message = $"Asteroid spawn rate {AsteroidSpawnRate} has to be positive";
                return false;
            }
            if (AsteroidExplosionRadius < 0f)
            {
                message = $"Asteroid explosion radius {AsteroidExplosionRadius} cannot be negative";
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
            storage.SetFloat($"{prefix}AsteroidSpawnRate", AsteroidSpawnRate);
            storage.SetFloat($"{prefix}AsteroidExplosionRadius", AsteroidExplosionRadius);
            storage.SetBool($"{prefix}SpawnAsteroid", SpawnAsteroid);
        }


        public override void LoadSettings(IStorageStrategy storage, string prefix)
        {
            if (storage.DoesKeyExist($"{prefix}AsteroidSpawnRate"))
            {
                AsteroidSpawnRate = storage.GetFloat($"{prefix}AsteroidSpawnRate");
            }
            if (storage.DoesKeyExist($"{prefix}AsteroidExplosionRadius"))
            {
                AsteroidExplosionRadius = storage.GetFloat($"{prefix}AsteroidExplosionRadius");
            }
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
            AsteroidObjects = new List<Shootable>(source.AsteroidObjects);
            SpawnProbabilities = new List<int>(source.SpawnProbabilities);
            AsteroidExplosionRadius = source.AsteroidExplosionRadius;
            AsteroidSpawnRate = source.AsteroidSpawnRate;
            SpawnAsteroid = source.SpawnAsteroid;
        }
    }
}
