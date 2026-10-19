namespace Portfolio.DeathRace
{
    /// <summary>
    /// The driver of a car that a person on another device drives (online). The car itself follows the poses that arrive
    /// (<see cref="RemoteCar"/>), so the pedals and the wheel do nothing here; what this driver passes on are the state
    /// bits of the last pose: the trigger held (the car's gun fires here too, tracers, sparks and all, and a round that
    /// hits a car of this device hurts it: the victim's device judges), the nitro burning and the brakes. Mines and
    /// rockets do not come from here: they are actions of the room, laid and launched by the online controller.
    /// </summary>
    public sealed class RemoteDriver : ICarDriver
    {
        /// <summary>The state bits of the newest pose (<see cref="OnlineRaceRules.PackState"/>).</summary>
        public uint State { get; set; }

        public bool Firing => OnlineRaceRules.Has(State, OnlineRaceRules.FiringBit);
        public bool Nitro => OnlineRaceRules.Has(State, OnlineRaceRules.NitroBit);
        public bool Braking => OnlineRaceRules.Has(State, OnlineRaceRules.BrakingBit);
        public bool Wrecked => OnlineRaceRules.Has(State, OnlineRaceRules.WreckedBit);
        public bool Finished => OnlineRaceRules.Has(State, OnlineRaceRules.FinishedBit);

        public CarInput Drive(Car car, float deltaTime)
        {
            return Input(State);
        }

        /// <summary>The input a remote car's state bits stand for (plain, for the tests).</summary>
        public static CarInput Input(uint state)
        {
            bool wrecked = OnlineRaceRules.Has(state, OnlineRaceRules.WreckedBit);
            return new CarInput
            {
                Throttle = OnlineRaceRules.Has(state, OnlineRaceRules.BrakingBit) ? -1f : 1f,
                Steer = 0f,
                Fire = !wrecked && OnlineRaceRules.Has(state, OnlineRaceRules.FiringBit),
                Nitro = !wrecked && OnlineRaceRules.Has(state, OnlineRaceRules.NitroBit)
            };
        }
    }
}
