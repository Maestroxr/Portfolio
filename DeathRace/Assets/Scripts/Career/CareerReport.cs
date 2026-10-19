using System.Collections.Generic;

namespace Portfolio.DeathRace
{
    /// <summary>A race of the day the player was not in, as the career simulated it: the rivals in the order they finished.</summary>
    public sealed class DayRace
    {
        public Difficulty Difficulty;
        public int Track;

        /// <summary>Rival ids, first place first.</summary>
        public List<string> Order = new List<string>();

        /// <summary>Per entry of <see cref="Order"/>: whether that car was wrecked (they come last).</summary>
        public List<bool> Wrecked = new List<bool>();
    }

    /// <summary>A rival who earned a better car.</summary>
    public struct Promotion
    {
        public string Id;
        public string Name;
        public int Tier;
        public CarModel Car;
    }

    /// <summary>How a rival of the player's own race did on the ladder.</summary>
    public struct RivalOutcome
    {
        public string Id;
        public int Place;
        public bool Wrecked;
        public int Points;
    }

    /// <summary>
    /// What a career race came to (<see cref="Career.Apply"/>): the place, the money (prize, bounty, cash picked up), the
    /// points and the move on the ladder, the state of the car, what was used up, the other races of the day and what
    /// changed around the ladder (rivals in better cars, tracks opened, the title). The results window shows it.
    /// </summary>
    public sealed class CareerReport
    {
        /// <summary>The day raced (the career is on the next one now).</summary>
        public int Day;

        public RaceOffer Offer;
        public Difficulty Difficulty;
        public int Track;

        public int Place;
        public bool Finished;
        public bool Wrecked;
        public int Kills;

        public int Prize;
        public int Bounty;
        public int Cash;

        public int Points;
        public int RankBefore;
        public int RankAfter;

        /// <summary>The health the car came back with, 0 to 1.</summary>
        public float HealthLeft;

        /// <summary>The health the car has now: the crew patches a wreck up to <see cref="CareerRules.MinStartHealth"/>.</summary>
        public float HealthNow;

        public bool Patched;

        /// <summary>What mending the car fully costs now.</summary>
        public int RepairCost;

        public int MinesUsed;
        public int RocketsUsed;
        public int NitroUsed;

        /// <summary>This race won the championship.</summary>
        public bool Champion;

        public bool ShowdownLost;

        public List<RivalOutcome> Rivals = new List<RivalOutcome>();
        public List<DayRace> OtherRaces = new List<DayRace>();
        public List<Promotion> Promotions = new List<Promotion>();

        /// <summary>Tracks the climb opened with this race.</summary>
        public List<int> TracksOpened = new List<int>();

        public int Earned => Prize + Bounty + Cash;

        public int RankChange => RankBefore - RankAfter;
    }
}
