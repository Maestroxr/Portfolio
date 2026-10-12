using System;

namespace Portfolio.BoardGames
{
    /// <summary>
    /// A game as it is written to a named save (and the autosave): the table it was set up with, the position with its
    /// history counters, and the campaign level it belongs to (-1 for a quick game, a two-player game or an online one).
    /// The game is the field of the setup's rule set (<see cref="Game"/>); JsonUtility gives the other two back as empty
    /// objects. Serialized to YAML by the base <c>SavedGame</c>; the same shape travels to the server for online saved
    /// games.
    /// </summary>
    [Serializable]
    public sealed class SaveData
    {
        public TableSetup setup;
        public CheckersGame checkers;
        public ChessGame chess;
        public BackgammonGame backgammon;
        public int campaignLevel = -1;

        /// <summary>The game of the setup's rule set; null without a setup.</summary>
        public IBoardGame Game
        {
            get
            {
                if (setup == null)
                {
                    return null;
                }
                switch (setup.Mode)
                {
                    case GameMode.Chess:
                        return chess;
                    case GameMode.Backgammon:
                        return backgammon;
                    default:
                        return checkers;
                }
            }
        }

        /// <summary>A save of <paramref name="game"/> (copied) at <paramref name="setup"/> (copied, its rule set the game's).</summary>
        public static SaveData Of(TableSetup setup, IBoardGame game, int campaignLevel)
        {
            var data = new SaveData { setup = setup.Clone(), campaignLevel = campaignLevel };
            data.setup.ruleSet = (int)game.RuleSet;
            switch (game.CloneGame())
            {
                case ChessGame chess:
                    data.chess = chess;
                    break;
                case BackgammonGame backgammon:
                    data.backgammon = backgammon;
                    break;
                case CheckersGame checkers:
                    data.checkers = checkers;
                    break;
            }
            return data;
        }

        /// <summary>
        /// Whether the save holds a whole game: two seats, a first seat of the two, a rule set, and a position of that
        /// game that can be played on: a draughts board of the variant's size, the 64 squares of chess with one king a side,
        /// the 24 points of backgammon with fifteen checkers a side and a phase it knows. JsonUtility gives a missing game
        /// back as an empty one, which must not count.
        /// </summary>
        public bool IsWhole()
        {
            if (setup == null || setup.seats == null || setup.seats.Length != 2 || setup.seats[0] == null || setup.seats[1] == null
                || !RuleSets.IsValid(setup.ruleSet) || setup.firstSeat < 0 || setup.firstSeat > 1)
            {
                return false;
            }
            switch (setup.Mode)
            {
                case GameMode.Chess:
                    return chess != null && chess.cells != null && chess.cells.Length == 64 && Kings(chess, Side.Light) == 1 && Kings(chess, Side.Dark) == 1
                        && (chess.sideToMove == 0 || chess.sideToMove == 1);
                case GameMode.Backgammon:
                    return backgammon != null && backgammon.board != null && backgammon.board.Length == BackgammonGame.PointCount
                        && backgammon.bar != null && backgammon.bar.Length == 2 && backgammon.off != null && backgammon.off.Length == 2
                        && Checkers(backgammon, Side.Light) == BackgammonGame.CheckersPerSide && Checkers(backgammon, Side.Dark) == BackgammonGame.CheckersPerSide
                        && backgammon.phase >= (int)BackgammonPhase.Opening && backgammon.phase <= (int)BackgammonPhase.Over
                        && (backgammon.sideToAct == 0 || backgammon.sideToAct == 1);
                default:
                {
                    if (checkers == null || checkers.board == null || checkers.board.cells == null)
                    {
                        return false;
                    }
                    VariantRules rules = VariantRules.Of(checkers.variant);
                    return checkers.board.size == rules.Size && checkers.board.cells.Length == rules.Squares;
                }
            }
        }

        private static int Kings(ChessGame game, Side side)
        {
            sbyte king = ChessPieces.Make(side, ChessPieceKind.King);
            int kings = 0;
            foreach (sbyte cell in game.cells)
            {
                if (cell == king)
                {
                    kings++;
                }
            }
            return kings;
        }

        /// <summary>The checkers of <paramref name="side"/> on the points, the bar and off the board.</summary>
        private static int Checkers(BackgammonGame game, Side side)
        {
            int count = game.bar[(int)side] + game.off[(int)side];
            foreach (int point in game.board)
            {
                if (side == Side.Light ? point > 0 : point < 0)
                {
                    count += side == Side.Light ? point : -point;
                }
            }
            return count;
        }
    }
}
