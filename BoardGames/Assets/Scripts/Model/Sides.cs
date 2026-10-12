namespace Portfolio.BoardGames
{
    /// <summary>
    /// The two sides of a table. Light is the side that starts on row 0 of a square board and the first mover's side
    /// where the rules have one: White in chess and in backgammon, the light men in draughts (where the English rules
    /// let Dark move first). Seat and side are tied by <see cref="TableSetup.SideOf"/>.
    /// </summary>
    public enum Side : byte
    {
        Light = 0,
        Dark = 1
    }

    /// <summary>What the rules ask of a side: the other one, and the English word for it (a CSV key).</summary>
    public static class Sides
    {
        public static Side Other(Side side)
        {
            return side == Side.Light ? Side.Dark : Side.Light;
        }

        /// <summary>"Light" or "Dark": the key of a side's name in draughts, in the localization table.</summary>
        public static string NameKey(Side side)
        {
            return side == Side.Light ? "Light" : "Dark";
        }

        /// <summary>The key of a side's name in <paramref name="mode"/>: Light and Dark in draughts, White and Black in chess and backgammon.</summary>
        public static string NameKey(Side side, GameMode mode)
        {
            if (mode == GameMode.Checkers)
            {
                return NameKey(side);
            }
            return side == Side.Light ? "White" : "Black";
        }
    }
}
