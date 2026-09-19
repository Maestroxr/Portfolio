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


    /// <summary>Names and descriptions of the power-ups, shown when one is picked up.</summary>
    public static class PowerUps
    {
        public const int Count = 4;

        public static string Title(PowerUpType type)
        {
            switch (type)
            {
                case PowerUpType.Magnet: return "Coin Magnet";
                case PowerUpType.Shield: return "Shield";
                case PowerUpType.Multiplier: return "Double Coins";
                case PowerUpType.SuperJump: return "Super Jump";
                default: return type.ToString();
            }
        }

        public static string Description(PowerUpType type)
        {
            switch (type)
            {
                case PowerUpType.Magnet: return "Coins fly to you!";
                case PowerUpType.Shield: return "Shrug off the next hit!";
                case PowerUpType.Multiplier: return "Every coin counts twice!";
                case PowerUpType.SuperJump: return "Jump high enough to land on wagons!";
                default: return string.Empty;
            }
        }
    }
}
