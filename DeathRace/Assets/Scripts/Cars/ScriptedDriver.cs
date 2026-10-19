using System;

namespace Portfolio.DeathRace
{
    /// <summary>
    /// A driver made of a function, for tours and tests: whatever the function answers is what the car does (a straight
    /// run at full throttle, a circle, a scripted crash). Without one it sits still.
    /// </summary>
    public sealed class ScriptedDriver : ICarDriver
    {
        private readonly Func<Car, float, CarInput> script;

        public ScriptedDriver(Func<Car, float, CarInput> script = null)
        {
            this.script = script;
        }

        /// <summary>A driver that always does the same.</summary>
        public static ScriptedDriver Constant(CarInput input)
        {
            return new ScriptedDriver((car, deltaTime) => input);
        }

        public CarInput Drive(Car car, float deltaTime)
        {
            return script != null ? script(car, deltaTime) : CarInput.Idle;
        }
    }
}
