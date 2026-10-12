using System;
using System.Text;

namespace Portfolio.Checkers
{
    /// <summary>
    /// One move of one piece: the squares it visits in order (a plain move visits two, a capture one more per jump),
    /// the pieces it takes along the way, and whether it ends the move as a new king. The whole capture sequence is one
    /// move: the rules never let a player stop halfway.
    /// </summary>
    [Serializable]
    public sealed class Move
    {
        public int[] path;
        public int[] captured;
        public bool promotes;

        /// <summary>For the serializers; the arrays are set by them.</summary>
        public Move()
        {
            path = new int[0];
            captured = new int[0];
        }

        private Move(int[] path, int[] captured, bool promotes)
        {
            this.path = path;
            this.captured = captured;
            this.promotes = promotes;
        }

        public int From => path[0];

        public int To => path[path.Length - 1];

        public bool IsCapture => captured != null && captured.Length > 0;

        public int Jumps => captured == null ? 0 : captured.Length;

        /// <summary>Whether <paramref name="other"/> visits the same squares and takes the same pieces (promotion not compared).</summary>
        public bool SamePathAs(Move other)
        {
            return other != null && SameNumbers(path, other.path) && SameNumbers(captured, other.captured);
        }

        /// <summary>Whether this move goes along exactly <paramref name="squares"/>.</summary>
        public bool PathIs(int[] squares)
        {
            return SameNumbers(path, squares);
        }

        private static bool SameNumbers(int[] a, int[] b)
        {
            int lengthA = a == null ? 0 : a.Length;
            int lengthB = b == null ? 0 : b.Length;
            if (lengthA != lengthB)
            {
                return false;
            }
            for (int i = 0; i < lengthA; i++)
            {
                if (a[i] != b[i])
                {
                    return false;
                }
            }
            return true;
        }

        public static Move Plain(int from, int to)
        {
            return new Move(new[] { from, to }, new int[0], false);
        }

        public static Move Plain(int from, int to, bool promotes)
        {
            return new Move(new[] { from, to }, new int[0], promotes);
        }

        public static Move Jump(int[] path, int[] captured, bool promotes)
        {
            return new Move(path, captured ?? new int[0], promotes);
        }

        /// <summary>The squares as humans number them, from 1: "9-14" for a step, "22x15x8" for a double jump.</summary>
        public override string ToString()
        {
            var text = new StringBuilder();
            string joint = IsCapture ? "x" : "-";
            for (int i = 0; i < path.Length; i++)
            {
                if (i > 0)
                {
                    text.Append(joint);
                }
                text.Append(path[i] + 1);
            }
            return text.ToString();
        }
    }
}
