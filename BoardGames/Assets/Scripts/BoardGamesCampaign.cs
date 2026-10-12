using System.Collections.Generic;
using Gamebox;
using UnityEngine;

namespace Portfolio.BoardGames
{
    /// <summary>
    /// The games against the computer, in one list: the draughts levels, then the chess levels, then the backgammon
    /// levels, each run from easy to hard. The campaign window shows the levels of the title's game; the first level of
    /// each game is open, any other once the level before it in the same game is completed. Stars: one for the win, one
    /// more for the level's second target (pieces kept; a gammon in backgammon), one more for winning quickly enough.
    /// </summary>
    [CreateAssetMenu(fileName = "BoardGamesCampaign", menuName = "Board Games/Campaign", order = 3)]
    public sealed class BoardGamesCampaign : Campaign
    {
        public const string StarRules = "1 star: win.  2 stars: keep enough pieces.  3 stars: win quickly.";

        public const string BackgammonStarRules = "1 star: win.  2 stars: win a gammon.  3 stars: win quickly.";

        public override int MaxStarsPerLevel => 3;

        public BoardGamesLevel Level(int index)
        {
            return this[index] as BoardGamesLevel;
        }

        /// <summary>The game of level <paramref name="index"/> (checkers for a level that is not there).</summary>
        public GameMode ModeOf(int index)
        {
            BoardGamesLevel level = Level(index);
            return level != null ? level.Mode : GameMode.Checkers;
        }

        /// <summary>The indices of the levels of <paramref name="mode"/>, in the order they are played.</summary>
        public IReadOnlyList<int> Levels(GameMode mode)
        {
            var indices = new List<int>();
            for (int i = 0; i < Count; i++)
            {
                BoardGamesLevel level = Level(i);
                if (level != null && level.Mode == mode)
                {
                    indices.Add(i);
                }
            }
            return indices;
        }

        /// <summary>
        /// Whether level <paramref name="index"/> can be played: the first level of its game always, any other once the
        /// level before it of the same game is completed. Without progress everything is open.
        /// </summary>
        public override bool IsUnlocked(int index, CampaignProgress progress)
        {
            if (index < 0 || index >= Count)
            {
                return false;
            }
            if (progress == null)
            {
                return true;
            }
            GameMode mode = ModeOf(index);
            for (int i = index - 1; i >= 0; i--)
            {
                if (ModeOf(i) == mode)
                {
                    return progress.IsCompleted(i);
                }
            }
            return true;
        }

        /// <summary>The star rules line of a game's levels (an English key).</summary>
        public static string StarRulesOf(GameMode mode)
        {
            return mode == GameMode.Backgammon ? BackgammonStarRules : StarRules;
        }

        /// <summary>
        /// The stars a finished campaign game earns the player on <paramref name="playerSide"/>: none without a win, one
        /// for the win, one more when the level's second target is met (draughts: pieces left; chess: own pieces other
        /// than the king left; backgammon: what the win is worth before the cube), one more when the game took at most
        /// the level's plies (backgammon: decisions).
        /// </summary>
        public static int StarsFor(BoardGamesLevel level, IBoardGame game, Side playerSide)
        {
            if (level == null || game == null || !game.IsOver || game.Winner != playerSide)
            {
                return 0;
            }
            int stars = 1;
            if (Kept(game, playerSide) >= level.PiecesKept)
            {
                stars++;
            }
            if (Length(game) <= level.MaxPlies)
            {
                stars++;
            }
            return stars;
        }

        /// <summary>
        /// The score of a finished campaign game, at least one: pieces left times ten plus the plies saved of a thousand
        /// (draughts, chess); the points of the win times a hundred plus the decisions saved of a thousand (backgammon).
        /// </summary>
        public static int ScoreFor(IBoardGame game, Side playerSide)
        {
            if (game == null)
            {
                return 1;
            }
            if (game is BackgammonGame backgammon)
            {
                return Mathf.Max(1, backgammon.Points * 100 + 1000 - backgammon.Turn);
            }
            return Mathf.Max(1, Kept(game, playerSide) * 10 + 1000 - Length(game));
        }

        /// <summary>What the second star counts for <paramref name="side"/>.</summary>
        private static int Kept(IBoardGame game, Side side)
        {
            switch (game)
            {
                case CheckersGame checkers:
                    return checkers.board != null ? checkers.board.Count(side) : 0;
                case ChessGame chess:
                {
                    int pieces = 0;
                    for (int square = 0; chess.cells != null && square < chess.cells.Length; square++)
                    {
                        sbyte piece = chess.cells[square];
                        if (ChessPieces.Belongs(piece, side) && ChessPieces.KindOf(piece) != ChessPieceKind.King)
                        {
                            pieces++;
                        }
                    }
                    return pieces;
                }
                case BackgammonGame backgammon:
                    // A gammon is worth 2 whatever the cube says: the target counts the kind of win.
                    return backgammon.Winner == side ? backgammon.Points / Mathf.Max(1, backgammon.cubeValue) : 0;
                default:
                    return 0;
            }
        }

        /// <summary>How long the game took: plies in draughts and chess, decisions in backgammon.</summary>
        private static int Length(IBoardGame game)
        {
            switch (game)
            {
                case CheckersGame checkers:
                    return checkers.ply;
                case ChessGame chess:
                    return chess.ply;
                default:
                    return game.Turn;
            }
        }
    }
}
