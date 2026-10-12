using System;
using System.Collections.Generic;

namespace Portfolio.Checkers.Tests
{
    /// <summary>
    /// Hand-made positions for the rules tests: an empty board of a variant with pieces put down by row and column,
    /// written as "l 2 3" (a light man on row 2, column 3), "L" a light king, "d" a dark man, "D" a dark king.
    /// </summary>
    public static class Positions
    {
        public static CheckersGame Game(Variant variant, Side toMove, params string[] pieces)
        {
            var game = new CheckersGame(variant);
            Array.Clear(game.board.cells, 0, game.board.cells.Length);
            game.sideToMove = (int)toMove;
            foreach (string piece in pieces)
            {
                string[] parts = piece.Split(' ');
                int row = int.Parse(parts[1]);
                int col = int.Parse(parts[2]);
                if (!Board.Playable(row, col))
                {
                    throw new ArgumentException("Not a dark square: " + piece);
                }
                game.board[row, col] = Piece(parts[0]);
            }
            return game;
        }

        public static sbyte Piece(string letter)
        {
            switch (letter)
            {
                case "l":
                    return Pieces.LightMan;
                case "L":
                    return Pieces.LightKing;
                case "d":
                    return Pieces.DarkMan;
                case "D":
                    return Pieces.DarkKing;
                default:
                    throw new ArgumentException("No piece: " + letter);
            }
        }

        public static int Square(int size, int row, int col)
        {
            return Board.Index(size, row, col);
        }

        /// <summary>The moves as "from-to" / "fromxoverxto" texts in generation order, for one-line assertions.</summary>
        public static List<string> Texts(IReadOnlyList<Move> moves)
        {
            var texts = new List<string>();
            foreach (Move move in moves)
            {
                texts.Add(move.ToString());
            }
            return texts;
        }

        /// <summary>The square's 1-based number, as <see cref="Move.ToString"/> writes it.</summary>
        public static int Human(int size, int row, int col)
        {
            return Board.Index(size, row, col) + 1;
        }

        /// <summary>Plays the whole game out between two computer levels; returns the finished game.</summary>
        public static CheckersGame PlayOut(Variant variant, int lightLevel, int darkLevel, int seed, int maxPlies = 400)
        {
            var game = new CheckersGame(variant);
            var random = new Gamebox.Lockstep.SystemRandom(seed);
            while (!game.IsOver && game.ply < maxPlies)
            {
                int level = game.SideToMove == Side.Light ? lightLevel : darkLevel;
                Move move = CheckersAI.Choose(game, level, random);
                if (move == null || !game.Play(move))
                {
                    throw new InvalidOperationException("The computer made no legal move at ply " + game.ply);
                }
            }
            return game;
        }
    }
}
