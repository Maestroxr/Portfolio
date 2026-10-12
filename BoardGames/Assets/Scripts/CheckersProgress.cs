using Gamebox;

namespace Portfolio.Checkers
{
    /// <summary>
    /// Checkers' records on top of the campaign progress: games played, won and drawn against the computer, next to the
    /// stars and best score the base class keeps for every campaign level.
    /// </summary>
    public sealed class CheckersProgress : CampaignProgress
    {
        public const string GamesKey = "Games";
        public const string WinsKey = "Wins";
        public const string DrawsKey = "Draws";

        public CheckersProgress(IStorageStrategy storage, GameType type) : base(storage, type)
        {
        }

        public int Games => Value(GamesKey);

        public int Wins => Value(WinsKey);

        public int Draws => Value(DrawsKey);

        /// <summary>Counts a finished game against the computer.</summary>
        public void RecordGame(bool won, bool drawn)
        {
            SetValue(GamesKey, Games + 1);
            if (won)
            {
                SetValue(WinsKey, Wins + 1);
            }
            else if (drawn)
            {
                SetValue(DrawsKey, Draws + 1);
            }
        }

        public void ResetAll(int levels)
        {
            Reset(levels, null, new[] { GamesKey, WinsKey, DrawsKey });
        }
    }
}
