using Gamebox;
using UnityEngine;

namespace Portfolio.DeathRace
{
    /// <summary>
    /// A person at this device: reads their controls (one seat of a split screen race, or every keyboard layout the
    /// single player may use, and the on-screen pad of a phone, <see cref="Touch"/>) and smooths the keys a little, so a
    /// tap steers gently and a held key steers fully.
    /// </summary>
    public sealed class HumanDriver : ICarDriver
    {
        /// <summary>How quickly the wheel follows a key, per second (a stick is followed at once).</summary>
        private const float SteerRate = 7f;

        private readonly PlayerControls[] controls;
        private float steer;

        public HumanDriver(params PlayerControls[] controls)
        {
            this.controls = controls != null && controls.Length > 0 ? controls : DeathRaceControls.Solo();
        }

        /// <summary>Set by tests and tours to drive through code instead of the controls.</summary>
        public CarInput? Override { get; set; }

        /// <summary>The on-screen pad of a phone or a tablet (<see cref="RaceTouchControls"/>), read beside the keys; null without one.</summary>
        public PlayerControls Touch { get; set; }

        public CarInput Drive(Car car, float deltaTime)
        {
            if (Override.HasValue)
            {
                return Override.Value;
            }
            float throttle = 0f, brake = 0f, left = 0f, right = 0f;
            bool fire = false, nitro = false, mine = false, rocket = false, special = false;
            for (int i = 0; i <= controls.Length; i++)
            {
                PlayerControls seat = i < controls.Length ? controls[i] : Touch;
                if (seat == null)
                {
                    continue;
                }
                throttle = Mathf.Max(throttle, seat.Value(DeathRaceControls.Throttle));
                brake = Mathf.Max(brake, seat.Value(DeathRaceControls.Brake));
                left = Mathf.Max(left, seat.Value(DeathRaceControls.Left));
                right = Mathf.Max(right, seat.Value(DeathRaceControls.Right));
                fire |= seat.Held(DeathRaceControls.Fire);
                nitro |= seat.Held(DeathRaceControls.Nitro);
                mine |= seat.Pressed(DeathRaceControls.Mine);
                rocket |= seat.Pressed(DeathRaceControls.Rocket);
                special |= seat.Pressed(DeathRaceControls.Special);
            }
            float target = Mathf.Clamp(right - left, -1f, 1f);
            // Keys are all or nothing: ease the wheel towards them; an analogue stick in between is taken as it is.
            bool analogue = Mathf.Abs(target) > 0.05f && Mathf.Abs(target) < 0.95f;
            steer = analogue ? target : Mathf.MoveTowards(steer, target, SteerRate * deltaTime);
            return new CarInput
            {
                Throttle = Mathf.Clamp(throttle - brake, -1f, 1f),
                Steer = steer,
                Fire = fire,
                Nitro = nitro,
                DropMine = mine,
                FireRocket = rocket,
                Special = special
            };
        }
    }
}
