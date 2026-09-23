namespace Portfolio.Heroes
{
    /// <summary>Random numbers for the rules: dice of damage, luck and morale, the map generator.</summary>
    public interface IRandom
    {
        /// <summary>A whole number from <paramref name="min"/> up to but not including <paramref name="max"/>.</summary>
        int Range(int min, int max);
    }

    /// <summary>
    /// Random numbers that come out the same on every device for the same seed: whole number arithmetic only
    /// (xorshift), so no runtime's own generator is involved. An online game seeds it with the number the server drew
    /// for the room and again with the number the server stamped on every action, so every player sees the same dice.
    /// </summary>
    public sealed class SeededRandom : IRandom
    {
        private uint state;

        public SeededRandom(uint seed)
        {
            Reseed(seed);
        }

        /// <summary>Where the sequence is, to save and restore it.</summary>
        public uint State
        {
            get => state;
            set => state = value != 0u ? value : 0x6D2B79F5u;
        }

        public void Reseed(uint seed)
        {
            // Seeds close together (1, 2, 3...) must not give sequences that are: stir the bits first.
            uint mixed = seed + 0x9E3779B9u;
            mixed = (mixed ^ (mixed >> 16)) * 0x85EBCA6Bu;
            mixed = (mixed ^ (mixed >> 13)) * 0xC2B2AE35u;
            mixed ^= mixed >> 16;
            state = mixed != 0u ? mixed : 0x6D2B79F5u;
        }

        public uint Next()
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

        /// <summary>True with a chance of <paramref name="percent"/> in a hundred.</summary>
        public bool Chance(int percent)
        {
            return Range(0, 100) < percent;
        }

        /// <summary>A derived seed, for a sub-generator that must not disturb this sequence.</summary>
        public uint Fork(uint salt)
        {
            return Next() ^ (salt * 0x27D4EB2Fu);
        }
    }

    public static class RandomExtensions
    {
        public static bool Chance(this IRandom random, int percent)
        {
            return random.Range(0, 100) < percent;
        }

        public static T Pick<T>(this IRandom random, System.Collections.Generic.IList<T> list)
        {
            return list[random.Range(0, list.Count)];
        }

        public static void Shuffle<T>(this IRandom random, System.Collections.Generic.IList<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
