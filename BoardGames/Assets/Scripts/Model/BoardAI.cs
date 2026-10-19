using Gamebox.Lockstep;

namespace Portfolio.BoardGames
{
    /// <summary>
    /// The computer of every game behind one call: the next command for the seat a game waits for, at a level
    /// (0 Easy, 1 Normal, 2 Hard). The search counts positions, never the clock, so the same game, level and random
    /// numbers give the same answer on every device. It changes nothing of <paramref name="game"/>'s state the rules
    /// look at, yet callers that think on another thread hand it a copy (<see cref="IBoardGame.CloneGame"/>).
    /// </summary>
    public static class BoardAI
    {
        /// <summary>The command for <paramref name="seat"/>, the seat the game waits for; null when it has nothing to do.</summary>
        public static BoardCommand Choose(IBoardGame game, int seat, int level, IRandom random)
        {
            switch (game)
            {
                case CheckersGame checkers:
                {
                    Move move = CheckersAI.Choose(checkers, level, random);
                    return move != null ? BoardCommand.MoveAlong(seat, move) : null;
                }
                case ChessGame chess:
                    return ChessAI.Choose(chess, seat, level, random);
                case BackgammonGame backgammon:
                    return BackgammonAI.Choose(backgammon, seat, level, random);
                default:
                    return null;
            }
        }

        /// <summary>
        /// The command the computer at its strongest would give for <paramref name="seat"/> (the seat the game waits
        /// for): what the Hint Book of the shop shows a person. Null when the seat has nothing to do.
        /// </summary>
        public static BoardCommand Hint(IBoardGame game, int seat, IRandom random)
        {
            return Choose(game, seat, RuleSets.ComputerLevels - 1, random);
        }
    }
}
