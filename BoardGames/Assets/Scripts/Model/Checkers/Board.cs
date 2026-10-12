using System;

namespace Portfolio.BoardGames
{
    /// <summary>
    /// The squares of one game and what stands on them: <c>size * size</c> cells read as <see cref="Pieces"/>, indexed
    /// <c>row * size + col</c>, row 0 being Light's home rank. Only the dark squares, where <c>(row + col)</c> is even,
    /// are ever played on; the others stay empty.
    /// </summary>
    [Serializable]
    public sealed class Board
    {
        public int size;
        public sbyte[] cells;

        /// <summary>For the serializers; the cells are set by them.</summary>
        public Board()
        {
            size = 0;
            cells = new sbyte[0];
        }

        public Board(int size)
        {
            this.size = size;
            cells = new sbyte[size * size];
        }

        public Board Clone()
        {
            var copy = new Board(size);
            Array.Copy(cells, copy.cells, cells.Length);
            return copy;
        }

        public sbyte this[int index]
        {
            get => cells[index];
            set => cells[index] = value;
        }

        public sbyte this[int row, int col]
        {
            get => cells[row * size + col];
            set => cells[row * size + col] = value;
        }

        public int Size => size;

        public static int Index(int size, int row, int col)
        {
            return row * size + col;
        }

        public static int Row(int size, int index)
        {
            return index / size;
        }

        public static int Col(int size, int index)
        {
            return index % size;
        }

        /// <summary>Whether the square is a dark one, which the pieces play on.</summary>
        public static bool Playable(int row, int col)
        {
            return ((row + col) & 1) == 0;
        }

        /// <summary>The starting position of <paramref name="rules"/>: Light's men on the low rows, Dark's on the high ones.</summary>
        public static Board Initial(VariantRules rules)
        {
            var board = new Board(rules.Size);
            for (int row = 0; row < rules.Size; row++)
            {
                sbyte piece;
                if (row < rules.RowsOfMen)
                {
                    piece = Pieces.LightMan;
                }
                else if (row >= rules.Size - rules.RowsOfMen)
                {
                    piece = Pieces.DarkMan;
                }
                else
                {
                    continue;
                }
                for (int col = 0; col < rules.Size; col++)
                {
                    if (Playable(row, col))
                    {
                        board.cells[row * rules.Size + col] = piece;
                    }
                }
            }
            return board;
        }

        /// <summary>Pieces of <paramref name="side"/> on the board, men and kings together.</summary>
        public int Count(Side side)
        {
            int count = 0;
            for (int i = 0; i < cells.Length; i++)
            {
                if (Pieces.Belongs(cells[i], side))
                {
                    count++;
                }
            }
            return count;
        }

        /// <summary>Kings of <paramref name="side"/> when <paramref name="kings"/>, else its men.</summary>
        public int Count(Side side, bool kings)
        {
            sbyte wanted = kings ? Pieces.King(side) : Pieces.Man(side);
            int count = 0;
            for (int i = 0; i < cells.Length; i++)
            {
                if (cells[i] == wanted)
                {
                    count++;
                }
            }
            return count;
        }
    }
}
