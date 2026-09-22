namespace Portfolio.Monopoly
{
    /// <summary>
    /// Random numbers that come out the same on every device for the same seed: whole number arithmetic only (xorshift),
    /// written out here so no runtime's own generator is involved. An online match seeds it with the number the server
    /// drew for the room, and again with the number the server stamped on every action, so the dice and the cards fall
    /// the same for every player and nobody can know them before the server has spoken.
    /// </summary>
    public sealed class SeededRandom : IRandom
    {
        private uint state;

        public SeededRandom(uint seed)
        {
            Reseed(seed);
        }

        /// <summary>Starts the sequence of <paramref name="seed"/> over.</summary>
        public void Reseed(uint seed)
        {
            // Seeds that are close together (1, 2, 3...) must not give sequences that are: stir the bits first.
            uint mixed = seed + 0x9E3779B9u;
            mixed = (mixed ^ (mixed >> 16)) * 0x85EBCA6Bu;
            mixed = (mixed ^ (mixed >> 13)) * 0xC2B2AE35u;
            mixed ^= mixed >> 16;
            // The generator never leaves zero.
            state = mixed != 0u ? mixed : 0x6D2B79F5u;
        }

        private uint Next()
        {
            uint x = state;
            x ^= x << 13;
            x ^= x >> 17;
            x ^= x << 5;
            state = x;
            return x;
        }

        public int Range(int min, int max)
        {
            if (max <= min)
            {
                return min;
            }
            uint span = (uint)((long)max - min);
            return (int)(min + (long)(Next() % span));
        }

        public double Value()
        {
            // 24 bits: every value is exact as a double, whatever the device.
            return (Next() >> 8) / 16777216.0;
        }
    }
}
