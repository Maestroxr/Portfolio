using System.Collections.Generic;

namespace Portfolio.BoardGames
{
    /// <summary>
    /// What the squares of the board mark, for a game that draws its own pieces on the squares (chess): the selected
    /// square, the squares it may move to (captures apart), the last move, the square under the pointer, the pieces that
    /// may move while nothing is selected and the king in check. -1 or an empty list marks nothing.
    /// See <see cref="BoardView.SetSquareMarks"/>.
    /// </summary>
    public sealed class SquareMarks
    {
        public int selected = -1;
        public List<int> targets = new List<int>();
        public List<int> captures = new List<int>();
        public int lastFrom = -1;
        public int lastTo = -1;
        public int hovered = -1;
        public List<int> movable = new List<int>();
        /// <summary>The square of a king in check: a red mark under it.</summary>
        public int check = -1;
    }
}
