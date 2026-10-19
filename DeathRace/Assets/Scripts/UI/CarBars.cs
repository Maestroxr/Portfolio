using UnityEngine;

namespace Portfolio.DeathRace
{
    /// <summary>A car's stats as shares of the best the game has (The Warden's Tyrant), for the stat bars of the windows.</summary>
    public static class CarBars
    {
        private static readonly CarStats Best = Career.WardenLoadout().Stats();

        public static float Speed(CarStats stats)
        {
            return stats.TopSpeed / Best.TopSpeed;
        }

        public static float Acceleration(CarStats stats)
        {
            return stats.Acceleration / Best.Acceleration;
        }

        public static float Handling(CarStats stats)
        {
            return (stats.Grip + stats.Steering) / (Best.Grip + Best.Steering);
        }

        /// <summary>How much punishment the car takes before it is wrecked: its health through its armour.</summary>
        public static float Armour(CarStats stats)
        {
            return stats.MaxHealth / Mathf.Max(0.1f, stats.DamageTaken) / (Best.MaxHealth / Mathf.Max(0.1f, Best.DamageTaken));
        }
    }
}
