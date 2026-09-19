using Gamebox;

namespace Portfolio.MemoryCards
{
    /// <summary>
    /// The player's progress through the Memory Cards campaign: the stars and best scores the base
    /// <see cref="CampaignProgress"/> keeps per level, plus the endless records, a few lifetime counters and the world
    /// last shown in the level select.
    /// </summary>
    public class MemoryCardsProgress : CampaignProgress
    {
        private const string EndlessBoardsKey = "EndlessBoards";
        private const string EndlessScoreKey = "EndlessScore";
        private const string BestComboKey = "BestCombo";
        private const string SetsFoundKey = "SetsFound";
        private const string WorldKey = "World";

        public MemoryCardsProgress(IStorageStrategy storage, GameType type) : base(storage, type)
        {
        }

        public int EndlessBestBoards => (int)Record(EndlessBoardsKey);

        public int EndlessBestScore => (int)Record(EndlessScoreKey);

        public int BestCombo => (int)Record(BestComboKey);

        /// <summary>Sets matched over every game played.</summary>
        public int SetsFound => Value(SetsFoundKey);

        /// <summary>The world the level select showed last.</summary>
        public int SelectedWorld
        {
            get => Value(WorldKey);
            set
            {
                if (value != SelectedWorld)
                {
                    SetValue(WorldKey, value);
                }
            }
        }

        /// <summary>Keeps the best endless run. Returns true when <paramref name="score"/> is a new record.</summary>
        public bool RecordEndless(int boards, int score)
        {
            RecordMax(EndlessBoardsKey, boards);
            return RecordMax(EndlessScoreKey, score);
        }

        /// <summary>Adds a finished game's sets to the lifetime count and keeps the best combo. Returns true on a new best combo.</summary>
        public bool RecordGame(int setsFound, int bestCombo)
        {
            if (setsFound > 0)
            {
                SetValue(SetsFoundKey, SetsFound + setsFound);
            }
            return RecordMax(BestComboKey, bestCombo);
        }

        /// <summary>Forgets every star, score, record and counter.</summary>
        public void ResetAll(int levelCount)
        {
            Reset(levelCount, new[] { EndlessBoardsKey, EndlessScoreKey, BestComboKey }, new[] { SetsFoundKey, WorldKey });
        }
    }
}
