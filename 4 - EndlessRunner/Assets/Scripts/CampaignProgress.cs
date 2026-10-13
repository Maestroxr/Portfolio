using System.Collections.Generic;
using Gamebox;

namespace Portfolio.EndlessRunner
{
    /// <summary>
    /// Campaign progress: the stars and best score of every level (BaseGame's progress, which also tells the avatar what a
    /// level result is worth) and the endless record. Kept in the game's storage under keys prefixed with the game type,
    /// or in memory when the game has no storage.
    /// </summary>
    public class CampaignProgress : Gamebox.CampaignProgress
    {
        public CampaignProgress(IStorageStrategy storage, GameType type) : base(storage, type)
        {
        }

        public float EndlessBestDistance => Get("Endless.Distance");

        public int EndlessBestScore => (int)Get("Endless.Score");

        /// <summary>The first level is always open; every other one opens once the level before it is finished.</summary>
        public bool IsUnlocked(int level)
        {
            return level <= 0 || Stars(level - 1) > 0;
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
            Persist();
            return best;
        }

        public void Reset(int levelCount)
        {
            Delete("Endless.Distance");
            Delete("Endless.Score");
            Reset(levelCount, null, null);
        }
    }
}
