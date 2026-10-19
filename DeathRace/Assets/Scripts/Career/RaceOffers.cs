using System.Collections.Generic;
using Gamebox.Lockstep;

namespace Portfolio.DeathRace
{
    /// <summary>
    /// The races of a day: one Easy, one Medium and one Hard, each on a track the career has opened and against three
    /// rivals drawn from the matching third of the ladder (so the three offers never share a driver), and, when the
    /// player starts the day first on the ladder, the Showdown against The Warden on The Crucible. Seeded by the career
    /// and the day, so the same day always offers the same races.
    /// </summary>
    public static class RaceOffers
    {
        /// <summary>The Warden brings the two best rivals to the Showdown.</summary>
        public const int ShowdownRivals = 2;

        private static readonly Difficulty[] Ordinary = { Difficulty.Easy, Difficulty.Medium, Difficulty.Hard };

        /// <summary>The offers of <paramref name="day"/> for <paramref name="state"/> as it stands at the start of the day.</summary>
        public static List<RaceOffer> Generate(CareerState state, int day)
        {
            var random = new SeededRandom(SeededRandom.Mix((uint)state.seed, (uint)day, 0x0FFE5u));
            var offers = new List<RaceOffer>(CareerRules.OffersPerDay + 1);
            var used = new List<int>();
            foreach (Difficulty difficulty in Ordinary)
            {
                int track = PickTrack(state.tracksOpen, difficulty, used, random);
                used.Add(track);
                var offer = new RaceOffer
                {
                    difficulty = difficulty,
                    track = track,
                    laps = CareerRules.Laps(difficulty, TrackLayouts.Of(track).Laps),
                    seed = (int)random.Next()
                };
                List<RivalState> pool = Ladder.Third(state, difficulty);
                random.Shuffle(pool);
                for (int i = 0; i < pool.Count && offer.rivals.Count < CareerRules.CarsPerRace - 1; i++)
                {
                    offer.rivals.Add(pool[i].id);
                }
                offers.Add(offer);
            }
            if (ShowdownDue(state))
            {
                offers.Add(Showdown(state, random));
            }
            return offers;
        }

        /// <summary>Whether the Showdown is on offer: the player starts the day first on the ladder and is not champion yet.</summary>
        public static bool ShowdownDue(CareerState state)
        {
            return !state.champion && Ladder.Rank(state) == 1;
        }

        private static RaceOffer Showdown(CareerState state, SeededRandom random)
        {
            var offer = new RaceOffer
            {
                difficulty = Difficulty.Showdown,
                track = CareerRules.ShowdownTrack,
                laps = TrackLayouts.Of(CareerRules.ShowdownTrack).Laps,
                seed = (int)random.Next()
            };
            offer.rivals.Add(Roster.WardenId);
            foreach (LadderEntry entry in Ladder.Entries(state))
            {
                if (!entry.IsPlayer && offer.rivals.Count <= ShowdownRivals)
                {
                    offer.rivals.Add(entry.Id);
                }
            }
            return offer;
        }

        /// <summary>
        /// A track for a race of <paramref name="difficulty"/>: an Easy race may be on any open track, a Medium one on the
        /// five opened last, a Hard one on the three opened last; a track another offer of the day has is avoided.
        /// </summary>
        private static int PickTrack(int open, Difficulty difficulty, List<int> used, SeededRandom random)
        {
            open = System.Math.Max(1, System.Math.Min(open, CareerRules.ShowdownTrack));
            int span = difficulty == Difficulty.Easy ? open : difficulty == Difficulty.Medium ? 5 : 3;
            int first = System.Math.Max(0, open - span);
            var choices = new List<int>();
            for (int track = first; track < open; track++)
            {
                if (!used.Contains(track))
                {
                    choices.Add(track);
                }
            }
            if (choices.Count == 0)
            {
                for (int track = 0; track < open; track++)
                {
                    if (!used.Contains(track))
                    {
                        choices.Add(track);
                    }
                }
            }
            return choices.Count > 0 ? random.Pick(choices) : random.Range(first, open);
        }
    }
}
