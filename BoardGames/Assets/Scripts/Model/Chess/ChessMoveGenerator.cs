using System.Collections.Generic;

namespace Portfolio.BoardGames
{
    /// <summary>
    /// The moves of a chess position: pseudo-legal moves generated on the 64 cells of a <see cref="ChessPosition"/>,
    /// each played and taken back to see whether it leaves the mover's king attacked. Perft (the count of positions a
    /// number of moves deep) checks the generator against the known numbers.
    /// </summary>
    public static class ChessMoveGenerator
    {
        /// <summary>The positions <paramref name="depth"/> moves deep from <paramref name="game"/>'s (1 for depth 0).</summary>
        public static long Perft(ChessGame game, int depth)
        {
            if (game == null)
            {
                return 0;
            }
            if (depth <= 0)
            {
                return 1;
            }
            var position = new ChessPosition();
            position.Load(game);
            var buffer = new int[(depth + 1) * ChessPosition.MaxMoves];
            return position.Perft(depth, buffer);
        }

        /// <summary>Whether a piece of <paramref name="by"/> attacks <paramref name="square"/> in <paramref name="game"/>.</summary>
        public static bool IsAttacked(ChessGame game, int square, Side by)
        {
            if (game == null || game.cells == null || game.cells.Length != 64 || square < 0 || square > 63)
            {
                return false;
            }
            return ChessPosition.Attacked(game.cells, square, (int)by);
        }

        /// <summary>The legal moves of the side to move in <paramref name="game"/>, in the generator's order (each promotion choice a move).</summary>
        public static List<ChessMove> Legal(ChessGame game)
        {
            var position = new ChessPosition();
            position.Load(game);
            var buffer = new int[ChessPosition.MaxMoves];
            int count = position.Legal(buffer);
            var moves = new List<ChessMove>(count);
            for (int i = 0; i < count; i++)
            {
                moves.Add(ToMove(position.cells, buffer[i]));
            }
            return moves;
        }

        /// <summary>A packed move of <see cref="ChessPosition"/> as a <see cref="ChessMove"/>, the pieces read off <paramref name="board"/> before it is played.</summary>
        internal static ChessMove ToMove(sbyte[] board, int move)
        {
            int from = ChessPosition.From(move);
            int to = ChessPosition.To(move);
            int flags = ChessPosition.Flags(move);
            var made = new ChessMove
            {
                from = from,
                to = to,
                piece = board[from],
                promotion = ChessPosition.Promotion(move),
                flags = flags
            };
            if ((flags & ChessMove.EnPassantFlag) != 0)
            {
                made.captured = board[from] > 0 ? to - 8 : to + 8;
                made.capturedPiece = board[made.captured];
            }
            else if (board[to] != 0)
            {
                made.captured = to;
                made.capturedPiece = board[to];
            }
            return made;
        }
    }
}
