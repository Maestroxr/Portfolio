using Gamebox;
using UnityEngine;

namespace Portfolio.Checkers
{
    /// <summary>
    /// The six games against the computer, from an easy English game to the wide board against a hard opponent. A game
    /// opens once the one before it is won (the base rule). Stars: one for the win, one more for keeping enough pieces,
    /// one more for winning quickly enough; the targets are the level's.
    /// </summary>
    [CreateAssetMenu(fileName = "CheckersCampaign", menuName = "Checkers/Campaign", order = 3)]
    public sealed class CheckersCampaign : Campaign
    {
        public override int MaxStarsPerLevel => 3;

        public CheckersLevel Level(int index)
        {
            return this[index] as CheckersLevel;
        }

        /// <summary>
        /// The stars a finished campaign game earns the player on <paramref name="playerSide"/>: none without a win, one
        /// for the win, one more when the player kept at least the level's pieces, one more when the game took at most
        /// the level's plies.
        /// </summary>
        public static int StarsFor(CheckersLevel level, CheckersGame game, Side playerSide)
        {
            if (level == null || game == null || !game.IsOver || game.Winner != playerSide)
            {
                return 0;
            }
            int stars = 1;
            if (game.board.Count(playerSide) >= level.PiecesKept)
            {
                stars++;
            }
            if (game.ply <= level.MaxPlies)
            {
                stars++;
            }
            return stars;
        }

        /// <summary>The score of a finished campaign game: pieces left times ten plus the plies saved, at least one.</summary>
        public static int ScoreFor(CheckersGame game, Side playerSide)
        {
            if (game == null)
            {
                return 1;
            }
            return Mathf.Max(1, game.board.Count(playerSide) * 10 + 1000 - game.ply);
        }

        public const string StarRules = "1 star: win.  2 stars: keep enough pieces.  3 stars: win quickly.";
    }
}
