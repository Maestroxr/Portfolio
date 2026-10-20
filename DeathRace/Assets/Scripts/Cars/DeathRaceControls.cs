using Gamebox;
using UnityEngine;

namespace Portfolio.DeathRace
{
    /// <summary>
    /// The controls of a car: the pedals, the wheel and the five weapon buttons. Their roles give every seat of a split
    /// screen race the shared defaults that never clash (seat 1 on WASD with Space, Left Shift, E and Q; seat 2 on the
    /// arrows with Right Ctrl, Right Shift, Enter and /; pads for all four). The special weapon has no role of its own,
    /// so its defaults are the game's: R, full stop, H and the number pad's minus, and the left shoulder of a pad.
    /// </summary>
    public static class DeathRaceControls
    {
        public const string Throttle = "throttle";
        public const string Brake = "brake";
        public const string Left = "left";
        public const string Right = "right";
        public const string Fire = "fire";
        public const string Nitro = "nitro";
        public const string Mine = "mine";
        public const string Rocket = "rocket";
        public const string Special = "special";

        /// <summary>The special weapon's keys per seat (the seat's keyboard set) and its pad button (the left shoulder).</summary>
        private static readonly KeyCode[] SpecialKeys = { KeyCode.R, KeyCode.Period, KeyCode.H, KeyCode.KeypadMinus };

        private const int SpecialPadButton = 4;

        public static ControlScheme Scheme()
        {
            var scheme = new ControlScheme()
                .Add(Throttle, "Accelerate", ControlRole.Up)
                .Add(Brake, "Brake / Reverse", ControlRole.Down)
                .Add(Left, "Steer left", ControlRole.Left)
                .Add(Right, "Steer right", ControlRole.Right)
                .Add(Fire, "Machine gun", ControlRole.Primary)
                .Add(Nitro, "Nitro", ControlRole.Secondary)
                .Add(Mine, "Drop mine", ControlRole.Tertiary)
                .Add(Rocket, "Fire rocket", ControlRole.Quaternary)
                .Add(Special, "Special weapon", ControlRole.None);
            scheme.Defaults = SpecialDefaults;
            return scheme;
        }

        /// <summary>The defaults of the special weapon (the other actions keep their role's).</summary>
        private static InputBinding[] SpecialDefaults(int seat, ControlAction action)
        {
            if (action.Id != Special || seat < 0 || seat >= SpecialKeys.Length)
            {
                return null;
            }
            InputBinding keys = InputBinding.Of(SpecialKeys[seat]);
            InputBinding pad = seat < GamepadAxes.Pads ? InputBinding.PadButton(seat + 1, SpecialPadButton) : InputBinding.None;
            // As the presets: the first two seats keyboard first, the last two pad first.
            return seat < 2 ? new[] { keys, pad } : new[] { pad, keys };
        }

        /// <summary>
        /// The controls of the one person racing at this device: the first seat's bindings as the player left them, and the
        /// second seat's defaults beside them, so both WASD and the arrows drive.
        /// </summary>
        public static PlayerControls[] Solo()
        {
            ControlBindings remembered = ControlBindings.Load(GameType.DeathRace, Scheme());
            var defaults = new ControlBindings(Scheme());
            return new[] { remembered.For(0), defaults.For(1) };
        }
    }
}
