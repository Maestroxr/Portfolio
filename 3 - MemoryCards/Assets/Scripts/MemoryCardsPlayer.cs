using Gamebox;

namespace Portfolio.MemoryCards
{
    /// <summary>
    /// A player of a Memory Cards game: counts the sets found, the mistakes and the cards flipped in the current game
    /// (an endless run counts over all of its boards). A game for one has one; a versus game has one per seat, played
    /// at this device or, online, by somebody else (<see cref="PlayerBase.Control"/>), each with a score of their own.
    /// </summary>
    public class MemoryCardsPlayer : PlayerBase
    {
        /// <summary>The points of the player in a versus game.</summary>
        public int Score { get; set; }

        public int Matches { get; private set; }

        public int Mistakes { get; private set; }

        public int Flips { get; private set; }

        public void RegisterFlip()
        {
            Flips++;
        }

        public void RegisterMatch()
        {
            Matches++;
        }

        public void RegisterMistake()
        {
            Mistakes++;
        }

        public void ResetMatches()
        {
            Score = 0;
            Matches = 0;
            Mistakes = 0;
            Flips = 0;
        }
    }
}
