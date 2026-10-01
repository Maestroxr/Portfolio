using Gamebox;
using UnityEngine;

namespace Portfolio.Asteroids
{
    /// <summary>
    /// The controls of one pilot of a local co-op mission: the keys, mouse buttons and pad controls the pilot bound to the
    /// actions of <see cref="LocalCoopRules.Scheme"/> on the setup's controls page, and nothing else (the keys of the
    /// single player game and the touch controls belong to <see cref="PlayerShipInput"/>). In a strike mission the turn,
    /// thrust and brake actions fly the ship in eight directions (<see cref="Move"/>), and the dash, which a strike ship does
    /// not have, selects the next special weapon like the pilot's own Next Special does, as in the single player game.
    /// </summary>
    public sealed class LocalShipInput : IShipInput
    {
        public LocalShipInput(PlayerControls controls)
        {
            Controls = controls;
        }

        /// <summary>The bindings of the pilot's seat.</summary>
        public PlayerControls Controls { get; }

        public float Turn { get; private set; }
        public float Thrust { get; private set; }
        public bool Brake { get; private set; }
        public bool Fire { get; private set; }
        public bool DashPressed { get; private set; }
        public bool BombPressed { get; private set; }
        public Vector2 Move { get; private set; }
        public bool CyclePressed { get; private set; }

        public void Read(AsteroidsPlayer ship, float deltaTime)
        {
            if (Controls == null)
            {
                Turn = Thrust = 0f;
                Brake = Fire = DashPressed = BombPressed = CyclePressed = false;
                Move = Vector2.zero;
                return;
            }
            // Every action is read every frame, so a press is seen in the frame it happens whatever the ship does with it.
            float right = Controls.Value(LocalCoopRules.Right);
            float left = Controls.Value(LocalCoopRules.Left);
            float thrust = Controls.Value(LocalCoopRules.Thrust);
            float brake = Controls.Value(LocalCoopRules.Brake);
            bool brakeHeld = Controls.Held(LocalCoopRules.Brake);
            bool fire = Controls.Held(LocalCoopRules.Fire);
            bool dash = Controls.Pressed(LocalCoopRules.Dash);
            bool bomb = Controls.Pressed(LocalCoopRules.Bomb);
            bool cycle = Controls.Pressed(LocalCoopRules.Cycle);
            // A positive turn is to the left.
            Turn = Mathf.Clamp(left - right, -1f, 1f);
            Thrust = Mathf.Clamp01(thrust);
            Brake = brakeHeld;
            Fire = fire;
            DashPressed = dash;
            BombPressed = bomb;
            Move = new Vector2(Mathf.Clamp(right - left, -1f, 1f), Mathf.Clamp(thrust - brake, -1f, 1f));
            CyclePressed = cycle || dash;
        }
    }
}
