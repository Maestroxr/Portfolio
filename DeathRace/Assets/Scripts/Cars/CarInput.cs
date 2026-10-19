namespace Portfolio.DeathRace
{
    /// <summary>
    /// What a driver asks of a car for one step: the pedals and the wheel, and the weapons. A person, the computer, a
    /// tour or the network all speak this to the car, so the car never knows who drives it.
    /// </summary>
    public struct CarInput
    {
        /// <summary>-1 to 1: forward is the accelerator, back is the brake and then reverse.</summary>
        public float Throttle;

        /// <summary>-1 to 1: positive turns right.</summary>
        public float Steer;

        /// <summary>The machine gun, while held.</summary>
        public bool Fire;

        /// <summary>Nitro, while held (a charge burns once it is lit).</summary>
        public bool Nitro;

        /// <summary>Drops a mine (pressed this step).</summary>
        public bool DropMine;

        /// <summary>Fires a rocket (pressed this step).</summary>
        public bool FireRocket;

        public static CarInput Idle => default;

        public override string ToString()
        {
            return $"throttle {Throttle:0.00} steer {Steer:0.00}{(Fire ? " fire" : "")}{(Nitro ? " nitro" : "")}{(DropMine ? " mine" : "")}{(FireRocket ? " rocket" : "")}";
        }
    }

    /// <summary>Whoever drives a car: asked once per physics step what to do.</summary>
    public interface ICarDriver
    {
        CarInput Drive(Car car, float deltaTime);
    }
}
