using UnityEngine;

namespace Portfolio.DeathRace
{
    /// <summary>
    /// A car's stats as shares of the best the game has, for the stat bars of the windows: each stat against the highest
    /// any car reaches with every part at its maximum (the top speed of the Apocalypse, the armour of the Goliath...), so
    /// no bar of any car, however upgraded, runs past its end.
    /// </summary>
    public static class CarBars
    {
        private static CarStats? best;

        /// <summary>The highest of every stat over all the cars, fully upgraded.</summary>
        private static CarStats Best
        {
            get
            {
                if (best.HasValue)
                {
                    return best.Value;
                }
                CarStats top = default;
                float armour = 0f;
                foreach (CarSpec spec in CarSpecs.All)
                {
                    CarStats stats = CarLoadout.Upgraded(spec.Model, 99).Stats();
                    top.TopSpeed = Mathf.Max(top.TopSpeed, stats.TopSpeed);
                    top.Acceleration = Mathf.Max(top.Acceleration, stats.Acceleration);
                    top.Grip = Mathf.Max(top.Grip, stats.Grip);
                    top.Steering = Mathf.Max(top.Steering, stats.Steering);
                    if (Toughness(stats) > armour)
                    {
                        armour = Toughness(stats);
                        top.MaxHealth = stats.MaxHealth;
                        top.DamageTaken = stats.DamageTaken;
                    }
                }
                best = top;
                return top;
            }
        }

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
            return Toughness(stats) / Toughness(Best);
        }

        private static float Toughness(CarStats stats)
        {
            return stats.MaxHealth / Mathf.Max(0.1f, stats.DamageTaken);
        }
    }
}
