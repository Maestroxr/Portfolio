using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Portfolio
{
    [CreateAssetMenu(fileName = "AsteroidSettings", menuName = "Asteroids/AsteroidSettings", order = 1)]
    public class AsteroidSettings : ScriptableObject
    {
        [field: SerializeField]
        public List<Shootable> AsteroidObjects { get; private set; }
        [field: SerializeField]
        public List<int> SpawnProbabilities { get; private set; }
        [field: SerializeField]
        public float AsteroidExplosionRadius { get; private set; }
        [field: SerializeField]
        public float AsteroidSpawnRate { get; private set; }
        [field: SerializeField]
        public bool SpawnAsteroid { get; private set; }


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
            else
            {
                return null;
            }
        }


        public bool Assert(out string error)
        {
            if (AsteroidObjects.Count != SpawnProbabilities.Count || AsteroidObjects.Count == 0)
            {
                error = $"Probabilities{SpawnProbabilities.Count} and asteroid amounts{AsteroidObjects.Count} are invalid";
                return false;
            }
            int probabilitySum = 0;
            for (int i = 0; i < SpawnProbabilities.Count; i++)
            {
                int probability = SpawnProbabilities[i];
                if (probability <= 0)
                {
                    error = $"Non-positive probability";
                    return false;
                }

                probabilitySum += probability;
            }

            if (probabilitySum > 100)
            {
                error = $"Probabilities amount to more than 100";
                return false;
            }

            error = null;
            return true;
        }
    }
}