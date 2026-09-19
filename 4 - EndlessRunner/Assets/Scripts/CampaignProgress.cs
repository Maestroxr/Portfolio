using System.Collections.Generic;
using Gamebox;

namespace Portfolio.EndlessRunner
{
    /// <summary>
    /// Campaign progress: the stars and best score of every level and the endless record. Kept in the game's storage
    /// under keys prefixed with the game type, or in memory when the game has no storage.
    /// </summary>
    public class CampaignProgress
    {
        private readonly IStorageStrategy storage;
        private readonly string prefix;
        private readonly Dictionary<string, float> memory = new Dictionary<string, float>();

        public CampaignProgress(IStorageStrategy storage, GameType type)
        {
            this.storage = storage;
            prefix = $"{type}.Progress.";
        }

        public int Stars(int level)
        {
            return (int)Get($"L{level}.Stars");
        }

        public int BestScore(int level)
        {
            return (int)Get($"L{level}.Best");
        }

        public float EndlessBestDistance => Get("Endless.Distance");

        public int EndlessBestScore => (int)Get("Endless.Score");

        /// <summary>The first level is always open; every other one opens once the level before it is finished.</summary>
        public bool IsUnlocked(int level)
        {
            return level <= 0 || Stars(level - 1) > 0;
        }

        public int TotalStars(int levelCount)
        {
            int total = 0;
            for (int i = 0; i < levelCount; i++)
            {
                total += Stars(i);
            }
            return total;
        }

        /// <summary>Keeps the better of the stored and the new result. Returns true when the score is a new best.</summary>
        public bool RecordLevel(int level, int stars, int score)
        {
            if (stars > Stars(level))
            {
                Set($"L{level}.Stars", stars);
            }
            bool best = score > BestScore(level);
            if (best)
            {
                Set($"L{level}.Best", score);
            }
            storage?.Persist();
            return best;
        }

        /// <summary>Keeps the longest endless run. Returns true when <paramref name="distance"/> is a new record.</summary>
        public bool RecordEndless(float distance, int score)
        {
            bool best = distance > EndlessBestDistance;
            if (best)
            {
                Set("Endless.Distance", distance);
            }
            if (score > EndlessBestScore)
            {
                Set("Endless.Score", score);
            }
            storage?.Persist();
            return best;
        }

        public void Reset(int levelCount)
        {
            for (int i = 0; i < levelCount; i++)
            {
                Delete($"L{i}.Stars");
                Delete($"L{i}.Best");
            }
            Delete("Endless.Distance");
            Delete("Endless.Score");
            storage?.Persist();
        }

        private float Get(string key)
        {
            if (storage == null)
            {
                return memory.TryGetValue(key, out float value) ? value : 0f;
            }
            return storage.DoesKeyExist(prefix + key) ? storage.GetFloat(prefix + key) : 0f;
        }

        private void Set(string key, float value)
        {
            if (storage == null)
            {
                memory[key] = value;
                return;
            }
            storage.SetFloat(prefix + key, value);
        }

        private void Delete(string key)
        {
            if (storage == null)
            {
                memory.Remove(key);
                return;
            }
            if (storage.DoesKeyExist(prefix + key))
            {
                storage.DeleteByKey(prefix + key);
            }
        }
    }
}
