using UnityEngine;

namespace Portfolio.DeathRace.Tests
{
    /// <summary>What the career tests share: a fresh career, and the result of a race of an offer as the test wants it to have gone.</summary>
    public static class CareerTestKit
    {
        public const int Seed = 4242;

        public static CareerState NewCareer()
        {
            return Career.New("Tester", 0, Seed);
        }

        /// <summary>The offer of <paramref name="difficulty"/> today, entered.</summary>
        public static RaceSetup Enter(CareerState state, Difficulty difficulty)
        {
            int index = state.offers.FindIndex(offer => offer.difficulty == difficulty);
            return Career.Enter(state, index, color => Color.white);
        }

        /// <summary>
        /// A result of <paramref name="setup"/> with the player in <paramref name="place"/> and the rivals filling the other
        /// places in seat order.
        /// </summary>
        public static RaceResult Result(RaceSetup setup, int place, bool wrecked = false, int kills = 0, int cash = 0, float health = 1f,
            int mines = 0, int rockets = 0, int nitro = 0)
        {
            var result = new RaceResult { Setup = setup };
            int next = 1;
            foreach (RaceEntrant entrant in setup.Entrants)
            {
                bool player = entrant.IsHuman;
                if (!player && next == place)
                {
                    next++;
                }
                result.Racers.Add(new RacerResult
                {
                    Seat = entrant.Seat, Name = entrant.Name, Driver = entrant.Driver, RivalId = entrant.RivalId,
                    Place = player ? place : next++, Finished = !(player && wrecked), Wrecked = player && wrecked,
                    Time = 100f, BestLap = 25f, Kills = player ? kills : 0, CashPicked = player ? cash : 0,
                    HealthLeft = player ? (wrecked ? 0f : health) : 0.5f,
                    MinesUsed = player ? mines : 0, RocketsUsed = player ? rockets : 0, NitroUsed = player ? nitro : 0
                });
            }
            result.Racers.Sort((a, b) => a.Place.CompareTo(b.Place));
            return result;
        }
    }
}
