namespace Portfolio.DeathRace
{
    /// <summary>
    /// The physics layers of a race, by number: BaseGame's project, where the game also runs, names no layers of its
    /// own, so the game never looks a layer up by name. Unnamed layers work like any other.
    /// </summary>
    public static class DeathRaceLayers
    {
        public const int Car = 20;
        public const int Wall = 21;
        public const int Pickup = 22;
        public const int Hazard = 23;

        public const int CarMask = 1 << Car;
        public const int WallMask = 1 << Wall;

        /// <summary>What a bullet or a rocket stops at.</summary>
        public const int ShotMask = CarMask | WallMask;
    }
}
