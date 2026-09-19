using Gamebox;

namespace Portfolio.Asteroids
{
    /// <summary>
    /// The player's progress through the Asteroids campaign: the stars and best scores the base
    /// <see cref="CampaignProgress"/> keeps per mission, plus the ship chosen in the hangar and the endless records.
    /// </summary>
    public class AsteroidsProgress : CampaignProgress
    {
        private const string ShipKey = "Ship";
        private const string EndlessWaveKey = "EndlessWave";
        private const string EndlessScoreKey = "EndlessScore";

        public AsteroidsProgress(IStorageStrategy storage, GameType type) : base(storage, type)
        {
        }

        public int SelectedShip
        {
            get => Value(ShipKey);
            set => SetValue(ShipKey, value);
        }

        public int EndlessBestWave => (int)Record(EndlessWaveKey);

        public int EndlessBestScore => (int)Record(EndlessScoreKey);

        /// <summary>Keeps the best endless run. Returns true when <paramref name="score"/> is a new record.</summary>
        public bool RecordEndless(int wave, int score)
        {
            RecordMax(EndlessWaveKey, wave);
            return RecordMax(EndlessScoreKey, score);
        }

        /// <summary>Forgets every star, score, record and the hangar choice.</summary>
        public void ResetAll(int levelCount)
        {
            Reset(levelCount, new[] { EndlessWaveKey, EndlessScoreKey }, new[] { ShipKey });
        }
    }
}
