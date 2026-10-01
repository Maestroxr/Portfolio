namespace Portfolio.EndlessRunner
{
    /// <summary>The power-ups a runner can pick up on the track.</summary>
    public enum PowerUpType
    {
        Magnet = 0,
        Shield = 1,
        Multiplier = 2,
        SuperJump = 3
    }


    /// <summary>Names and descriptions of the power-ups, shown when one is picked up (English keys, see <see cref="RunnerText"/>).</summary>
    public static class PowerUps
    {
        public const int Count = 4;

        public static string Title(PowerUpType type)
        {
            switch (type)
            {
                case PowerUpType.Magnet: return RunnerText.Key("Coin Magnet");
                case PowerUpType.Shield: return RunnerText.Key("Shield");
                case PowerUpType.Multiplier: return RunnerText.Key("Double Coins");
                case PowerUpType.SuperJump: return RunnerText.Key("Super Jump");
                default: return type.ToString();
            }
        }

        public static string Description(PowerUpType type)
        {
            switch (type)
            {
                case PowerUpType.Magnet: return RunnerText.Key("Coins fly to you!");
                case PowerUpType.Shield: return RunnerText.Key("Shrug off the next hit!");
                case PowerUpType.Multiplier: return RunnerText.Key("Every coin counts twice!");
                case PowerUpType.SuperJump: return RunnerText.Key("Jump high enough to land on wagons!");
                default: return string.Empty;
            }
        }
    }
}
