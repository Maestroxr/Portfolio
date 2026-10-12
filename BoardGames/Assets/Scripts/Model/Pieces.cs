namespace Portfolio.Checkers
{
    /// <summary>
    /// What a cell of the board holds, as one signed byte: 0 empty, +1 a light man, +2 a light king, -1 a dark man,
    /// -2 a dark king. The sign is the side, the magnitude the rank.
    /// </summary>
    public static class Pieces
    {
        public const sbyte None = 0;
        public const sbyte LightMan = 1;
        public const sbyte LightKing = 2;
        public const sbyte DarkMan = -1;
        public const sbyte DarkKing = -2;

        /// <summary>The side of a piece; an empty cell counts as Light, so ask <see cref="Belongs"/> when the cell may be empty.</summary>
        public static Side SideOf(sbyte piece)
        {
            return piece < 0 ? Side.Dark : Side.Light;
        }

        public static bool IsKing(sbyte piece)
        {
            return piece == LightKing || piece == DarkKing;
        }

        public static bool IsMan(sbyte piece)
        {
            return piece == LightMan || piece == DarkMan;
        }

        /// <summary>Whether the cell holds a piece of <paramref name="side"/> (an empty cell belongs to nobody).</summary>
        public static bool Belongs(sbyte piece, Side side)
        {
            return side == Side.Light ? piece > 0 : piece < 0;
        }

        public static sbyte Man(Side side)
        {
            return side == Side.Light ? LightMan : DarkMan;
        }

        public static sbyte King(Side side)
        {
            return side == Side.Light ? LightKing : DarkKing;
        }
    }
}
