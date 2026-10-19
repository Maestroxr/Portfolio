using System.Collections.Generic;

namespace Portfolio.DeathRace
{
    /// <summary>One line of the ladder: a rival (with their id) or the player.</summary>
    public struct LadderEntry
    {
        /// <summary>From 1.</summary>
        public int Rank;

        /// <summary>The rival's id; null for the player.</summary>
        public string Id;

        public string Name;
        public int Points;
        public int Wins;
        public int Tier;
        public CarModel Car;
        public int ColorIndex;

        public bool IsPlayer => Id == null;
    }

    /// <summary>
    /// The ladder of the twenty drivers: more points rank higher; on equal points more wins do, then the driver who stood
    /// higher on the first day (the newcomer, the player, below every rival). The order never depends on how the list
    /// happened to be sorted before, so it does not flicker between two equal drivers.
    /// </summary>
    public static class Ladder
    {
        /// <summary>The whole ladder, first to last.</summary>
        public static List<LadderEntry> Entries(CareerState state)
        {
            var entries = new List<LadderEntry>(state.rivals.Count + 1)
            {
                new LadderEntry
                {
                    Id = null, Name = state.driverName, Points = state.points, Wins = state.wins, Tier = CarSpecs.Of(state.car).Tier,
                    Car = state.car, ColorIndex = state.colorIndex
                }
            };
            foreach (RivalState rival in state.rivals)
            {
                entries.Add(new LadderEntry
                {
                    Id = rival.id, Name = rival.name, Points = rival.points, Wins = rival.wins, Tier = rival.tier, Car = rival.Car,
                    ColorIndex = rival.colorIndex
                });
            }
            entries.Sort(Compare);
            for (int i = 0; i < entries.Count; i++)
            {
                LadderEntry entry = entries[i];
                entry.Rank = i + 1;
                entries[i] = entry;
            }
            return entries;
        }

        /// <summary>The player's rank, 1 to 20.</summary>
        public static int Rank(CareerState state)
        {
            int rank = 1;
            var player = new LadderEntry { Id = null, Points = state.points, Wins = state.wins };
            foreach (RivalState rival in state.rivals)
            {
                var other = new LadderEntry { Id = rival.id, Points = rival.points, Wins = rival.wins };
                if (Compare(other, player) < 0)
                {
                    rank++;
                }
            }
            return rank;
        }

        /// <summary>The rank of the rival with <paramref name="id"/>, or 0 when there is none.</summary>
        public static int RankOf(CareerState state, string id)
        {
            foreach (LadderEntry entry in Entries(state))
            {
                if (entry.Id == id)
                {
                    return entry.Rank;
                }
            }
            return 0;
        }

        /// <summary>
        /// The rivals alone, best first, cut into thirds: <see cref="Difficulty.Hard"/> races draw from the top third,
        /// Medium from the middle, Easy from the bottom. The top third takes the one left over (7, 6, 6 of 19).
        /// </summary>
        public static List<RivalState> Third(CareerState state, Difficulty difficulty)
        {
            var ranked = new List<RivalState>(state.rivals);
            ranked.Sort((a, b) => Compare(Entry(a), Entry(b)));
            int count = ranked.Count;
            int small = count / 3;
            int top = count - 2 * small;
            int start, length;
            switch (difficulty)
            {
                case Difficulty.Easy:
                    start = top + small;
                    length = small;
                    break;
                case Difficulty.Medium:
                    start = top;
                    length = small;
                    break;
                default:
                    start = 0;
                    length = top;
                    break;
            }
            return ranked.GetRange(start, length);
        }

        private static LadderEntry Entry(RivalState rival)
        {
            return new LadderEntry { Id = rival.id, Points = rival.points, Wins = rival.wins };
        }

        /// <summary>Negative when <paramref name="a"/> ranks above <paramref name="b"/>.</summary>
        private static int Compare(LadderEntry a, LadderEntry b)
        {
            if (a.Points != b.Points)
            {
                return b.Points.CompareTo(a.Points);
            }
            if (a.Wins != b.Wins)
            {
                return b.Wins.CompareTo(a.Wins);
            }
            // The roster position of the first day (the player is below everyone, at -1).
            return Seniority(b).CompareTo(Seniority(a));
        }

        private static int Seniority(LadderEntry entry)
        {
            return entry.IsPlayer ? -1 : Roster.IndexOf(entry.Id);
        }
    }
}
