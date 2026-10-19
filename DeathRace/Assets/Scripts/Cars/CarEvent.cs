namespace Portfolio.DeathRace
{
    /// <summary>What happened to a car that its driver's screen tells them about.</summary>
    public enum CarEventKind
    {
        /// <summary>This car wrecked <see cref="CarEvent.Other"/>.</summary>
        Kill = 0,
        /// <summary>This car was wrecked (by <see cref="CarEvent.Other"/> when it was another car).</summary>
        Wrecked = 1,
        /// <summary>Picked something up: <see cref="CarEvent.Pickup"/>, worth <see cref="CarEvent.Amount"/>.</summary>
        Pickup = 2,
        /// <summary>Completed lap <see cref="CarEvent.Amount"/>.</summary>
        Lap = 3,
        /// <summary>Crossed the line for the last time, in place <see cref="CarEvent.Amount"/>.</summary>
        Finished = 4,
        /// <summary>Took <see cref="CarEvent.Amount"/> damage (rounded) from <see cref="CarEvent.Other"/>, if a car.</summary>
        Damaged = 5,
        /// <summary>Nitro lit.</summary>
        Nitro = 6,
        /// <summary>A weapon was used but had nothing left (no mines, no rockets, no nitro, no rounds).</summary>
        Empty = 7,
        /// <summary>A new fastest lap of the race, <see cref="CarEvent.Value"/> seconds.</summary>
        BestLap = 8
    }

    /// <summary>Something that happened to a car (<see cref="Car.Happened"/>): the HUD shows it, the audio plays it.</summary>
    public struct CarEvent
    {
        public CarEventKind Kind;
        public int Amount;
        public float Value;
        public PickupKind Pickup;
        public Car Other;

        public CarEvent(CarEventKind kind, int amount = 0, Car other = null)
        {
            Kind = kind;
            Amount = amount;
            Value = 0f;
            Pickup = PickupKind.Cash;
            Other = other;
        }
    }
}
