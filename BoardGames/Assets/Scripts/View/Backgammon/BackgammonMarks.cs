using System.Collections.Generic;

namespace Portfolio.BoardGames
{
    /// <summary>
    /// What the backgammon board marks, by place (0 to 23 the points, 24 the bar, 25 the tray): the checkers the side to
    /// act may move, the one picked, the places it may go to (one under the pointer brighter), and the last play.
    /// </summary>
    public sealed class BackgammonMarks
    {
        /// <summary>The side whose checkers <see cref="movable"/>, <see cref="selected"/> and <see cref="targets"/> are about.</summary>
        public Side side = Side.Light;
        /// <summary>Places whose top checker may move (shown while nothing is picked).</summary>
        public List<int> movable = new List<int>();
        public int selected = -1;
        /// <summary>Places the picked checker may reach: points, or 25 to bear it off.</summary>
        public List<int> targets = new List<int>();
        public int hovered = -1;
        /// <summary>Who played the last play, and the places it left and reached.</summary>
        public Side lastSide = Side.Light;
        public List<int> lastFrom = new List<int>();
        public List<int> lastTo = new List<int>();
        /// <summary>The numbers of the dice still to play (the others are dimmed); null shows every die bright.</summary>
        public int[] diceLeft;

        public void Clear()
        {
            movable.Clear();
            selected = -1;
            targets.Clear();
            hovered = -1;
            lastFrom.Clear();
            lastTo.Clear();
            diceLeft = null;
        }
    }
}
