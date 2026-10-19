using Gamebox;

namespace Portfolio.DeathRace
{
    /// <summary>
    /// The controls of a car: the pedals, the wheel and the four weapon buttons. Their roles give every seat of a split
    /// screen race the shared defaults that never clash (seat 1 on WASD with Space, Left Shift, E and Q; seat 2 on the
    /// arrows with Right Ctrl, Right Shift, Enter and /; pads for all four).
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

        public static ControlScheme Scheme()
        {
            return new ControlScheme()
                .Add(Throttle, "Accelerate", ControlRole.Up)
                .Add(Brake, "Brake / Reverse", ControlRole.Down)
                .Add(Left, "Steer left", ControlRole.Left)
                .Add(Right, "Steer right", ControlRole.Right)
                .Add(Fire, "Machine gun", ControlRole.Primary)
                .Add(Nitro, "Nitro", ControlRole.Secondary)
                .Add(Mine, "Drop mine", ControlRole.Tertiary)
                .Add(Rocket, "Fire rocket", ControlRole.Quaternary);
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
