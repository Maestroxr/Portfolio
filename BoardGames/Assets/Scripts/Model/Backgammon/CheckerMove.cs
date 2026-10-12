using System;
using System.Collections.Generic;

namespace Portfolio.BoardGames
{
    /// <summary>
    /// One checker moved by one die: from a point (0 to 23) or the bar (<see cref="BackgammonGame.Bar"/>) to a point or
    /// off the board (<see cref="BackgammonGame.Off"/>).
    /// </summary>
    [Serializable]
    public struct CheckerMove : IEquatable<CheckerMove>
    {
        public int from;
        public int to;
        /// <summary>The die the move uses (a bearing off may use a die larger than the distance).</summary>
        public int die;

        public CheckerMove(int from, int to, int die)
        {
            this.from = from;
            this.to = to;
            this.die = die;
        }

        public bool FromBar => from == BackgammonGame.Bar;

        public bool BearsOff => to == BackgammonGame.Off;

        /// <summary>Same checker path (the die may differ when bearing off).</summary>
        public bool SamePath(CheckerMove other)
        {
            return from == other.from && to == other.to;
        }

        public bool Equals(CheckerMove other)
        {
            return from == other.from && to == other.to && die == other.die;
        }

        public override bool Equals(object obj)
        {
            return obj is CheckerMove other && Equals(other);
        }

        public override int GetHashCode()
        {
            return (from * 31 + to) * 31 + die;
        }

        public override string ToString()
        {
            string a = from == BackgammonGame.Bar ? "bar" : (from + 1).ToString();
            string b = to == BackgammonGame.Off ? "off" : (to + 1).ToString();
            return a + "/" + b;
        }
    }

    /// <summary>A whole play of a turn: the checkers moved, in order (none when the dice allow nothing).</summary>
    [Serializable]
    public sealed class BackgammonPlay
    {
        public CheckerMove[] moves = new CheckerMove[0];

        public BackgammonPlay()
        {
        }

        public BackgammonPlay(IReadOnlyList<CheckerMove> moves)
        {
            this.moves = new CheckerMove[moves != null ? moves.Count : 0];
            for (int i = 0; i < this.moves.Length; i++)
            {
                this.moves[i] = moves[i];
            }
        }

        public int Count => moves != null ? moves.Length : 0;

        /// <summary>The numbers of the move command: <c>{ from, to, from, to, ... }</c>.</summary>
        public int[] Path()
        {
            var path = new int[Count * 2];
            for (int i = 0; i < Count; i++)
            {
                path[i * 2] = moves[i].from;
                path[i * 2 + 1] = moves[i].to;
            }
            return path;
        }

        public override string ToString()
        {
            return Count == 0 ? "(no move)" : string.Join(" ", Array.ConvertAll(moves, m => m.ToString()));
        }
    }
}
