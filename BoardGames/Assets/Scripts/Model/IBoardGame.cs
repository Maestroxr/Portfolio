namespace Portfolio.BoardGames
{
    /// <summary>How a game stands: still on, won by a side, or drawn.</summary>
    public enum GameResult
    {
        Playing = 0,
        LightWins = 1,
        DarkWins = 2,
        Draw = 3
    }

    /// <summary>Why a game ended. The numbers are saved with the games: never change them.</summary>
    public enum EndReason
    {
        None = 0,
        /// <summary>Draughts: the side to move had no legal move.</summary>
        NoMoves = 1,
        Resigned = 2,
        /// <summary>Draughts: fifty plies without a capture or a man moving.</summary>
        DrawByPlies = 3,
        /// <summary>The room ended the game before the rules did.</summary>
        CalledOff = 4,
        Checkmate = 5,
        Stalemate = 6,
        /// <summary>Chess: the same position for the third time.</summary>
        Repetition = 7,
        /// <summary>Chess: fifty moves of each side without a capture or a pawn moving.</summary>
        FiftyMoves = 8,
        /// <summary>Chess: neither side has the pieces left to mate.</summary>
        InsufficientMaterial = 9,
        /// <summary>Backgammon: a side bore off all fifteen checkers.</summary>
        BorneOff = 10,
        /// <summary>Backgammon: a side refused the doubling cube and gave the game up at its value.</summary>
        DoubleDeclined = 11
    }

    /// <summary>
    /// What every game of the package has, for the table, the director, the interface and the server reports: whose
    /// decision the game waits for, how it ended and what the result is worth. Each game keeps everything its rules
    /// look at in public fields, so a saved game and a game played in step on two devices are the game and nothing more.
    /// </summary>
    public interface IBoardGame
    {
        RuleSet RuleSet { get; }

        /// <summary>
        /// The side whose decision the game waits for: the side to move in draughts and chess; in backgammon the side
        /// to roll or play, or the side that answers the doubling cube.
        /// </summary>
        Side SideToAct { get; }

        bool IsOver { get; }

        GameResult Result { get; }

        EndReason Reason { get; }

        /// <summary>The side that won; null while the game is on and on a draw.</summary>
        Side? Winner { get; }

        /// <summary>Counts every decision the game took (a move, a roll, an answer to the cube): it changes with each one.</summary>
        int Turn { get; }

        /// <summary>
        /// What the result is worth to the winner: 1 in draughts and chess; in backgammon the cube's value times 1, 2 or
        /// 3 (a single game, a gammon, a backgammon). 0 while the game is on and on a draw.
        /// </summary>
        int Points { get; }

        /// <summary>What a side has left, by the game's own measure (pieces and their values): ties, rankings, reports.</summary>
        int Material(Side side);

        /// <summary><paramref name="side"/> gives up: the other side wins. False once the game is over.</summary>
        bool Resign(Side side);

        /// <summary>The room ended the game early: <paramref name="winner"/> wins, or nobody does.</summary>
        void CallOff(Side? winner);

        /// <summary>Everything the rules look at, folded: the same on two devices exactly when their games are.</summary>
        uint Checksum();

        /// <summary>A deep copy (the computer thinks on one, a save holds one).</summary>
        IBoardGame CloneGame();
    }
}
