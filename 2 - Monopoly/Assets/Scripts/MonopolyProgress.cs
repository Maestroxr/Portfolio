using Gamebox;

namespace Portfolio.Monopoly
{
    /// <summary>
    /// Monopoly's records on top of the campaign progress: games played and won, and the richest win, next to the stars
    /// and best net worth the base class keeps for every mode.
    /// </summary>
    public class MonopolyProgress : CampaignProgress
    {
        public const string GamesKey = "Games";
        public const string WinsKey = "Wins";
        public const string RichestKey = "RichestWin";

        public MonopolyProgress(IStorageStrategy storage, GameType type) : base(storage, type)
        {
        }

        public int GamesPlayed => Value(GamesKey);

        public int Wins => Value(WinsKey);

        public int RichestWin => (int)Record(RichestKey);

        /// <summary>Counts a finished match. Returns true when the winner's net worth is a new best for the mode.</summary>
        public bool RecordMatch(int mode, bool humanWon, int stars, int netWorth)
        {
            SetValue(GamesKey, GamesPlayed + 1);
            if (!humanWon)
            {
                return false;
            }
            SetValue(WinsKey, Wins + 1);
            RecordMax(RichestKey, netWorth);
            return RecordLevel(mode, stars, netWorth);
        }

        public void ResetAll(int modes)
        {
            Reset(modes, new[] { RichestKey }, new[] { GamesKey, WinsKey });
        }
    }
}
