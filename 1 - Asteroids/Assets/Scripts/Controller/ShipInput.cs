using System;
using UnityEngine;

namespace Portfolio.Asteroids
{
    /// <summary>Where the ship's commands come from: the player (keyboard, gamepad, touch), or the autopilot.</summary>
    public interface IShipInput
    {
        /// <summary>-1 turns right, 1 turns left.</summary>
        float Turn { get; }

        /// <summary>0 to 1.</summary>
        float Thrust { get; }

        bool Brake { get; }

        bool Fire { get; }

        /// <summary>True on the frame the dash was pressed.</summary>
        bool DashPressed { get; }

        /// <summary>True on the frame the nova bomb was pressed.</summary>
        bool BombPressed { get; }

        /// <summary>Reads the commands for this frame.</summary>
        void Read(AsteroidsPlayer ship, float deltaTime);
    }


    /// <summary>
    /// The player's controls: the keys of the ship's <see cref="PlayerSettings"/> plus the arrow keys, the input
    /// manager's Horizontal / Vertical axes and fire buttons when the project defines them, and the on-screen
    /// <see cref="ShipTouchControls"/> on phones and tablets. The touch stick turns the ship toward where it points and
    /// thrusts once the nose points roughly that way, so a light touch aims and a full push flies.
    /// </summary>
    public class PlayerShipInput : IShipInput
    {
        /// <summary>Angle between the nose and the stick, in degrees, that turns at full rate.</summary>
        public const float FullTurnAngle = 40f;

        /// <summary>Seconds of the current turn rate counted against the angle left, so the nose settles instead of swinging past.</summary>
        public const float TurnLead = 0.12f;

        private static bool? axesAvailable;

        /// <summary>The on-screen controls read next to the keyboard and gamepad; null without touch controls.</summary>
        public ShipTouchControls Touch { get; set; }

        public float Turn { get; private set; }
        public float Thrust { get; private set; }
        public bool Brake { get; private set; }
        public bool Fire { get; private set; }
        public bool DashPressed { get; private set; }
        public bool BombPressed { get; private set; }

        public void Read(AsteroidsPlayer ship, float deltaTime)
        {
            PlayerSettings keys = ship != null ? ship.PlayerSettings : null;
            float turn = 0f;
            float thrust = 0f;
            bool brake = false;
            if (Held(keys != null ? keys.Left : KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
            {
                turn += 1f;
            }
            if (Held(keys != null ? keys.Right : KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
            {
                turn -= 1f;
            }
            if (Held(keys != null ? keys.Forward : KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
            {
                thrust = 1f;
            }
            if (Held(keys != null ? keys.Brake : KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
            {
                brake = true;
            }
            bool fire = Held(keys != null ? keys.Shoot : KeyCode.Space) || Input.GetKey(KeyCode.J);
            bool dash = Pressed(keys != null ? keys.Dash : KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.K) || Input.GetKeyDown(KeyCode.RightShift);
            bool bomb = Pressed(keys != null ? keys.Bomb : KeyCode.B) || Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.L);

            if (AxesAvailable)
            {
                float horizontal = SafeAxis("Horizontal");
                float vertical = SafeAxis("Vertical");
                if (Mathf.Abs(turn) < 0.01f && Mathf.Abs(horizontal) > 0.2f)
                {
                    turn = -horizontal;
                }
                if (thrust < 0.01f && vertical > 0.2f)
                {
                    thrust = vertical;
                }
                brake |= vertical < -0.5f;
            }
            // Gamepad buttons (Xbox layout): A or RB fires, X or LB dashes, B or Y sets off a nova bomb.
            fire |= Input.GetKey(KeyCode.JoystickButton0) || Input.GetKey(KeyCode.JoystickButton5);
            dash |= Input.GetKeyDown(KeyCode.JoystickButton2) || Input.GetKeyDown(KeyCode.JoystickButton4);
            bomb |= Input.GetKeyDown(KeyCode.JoystickButton1) || Input.GetKeyDown(KeyCode.JoystickButton3);

            ShipTouchControls touch = Touch;
            if (touch != null && touch.Active)
            {
                if (touch.Steering && ship != null)
                {
                    float angularVelocity = ship.Simulation != null ? ship.Simulation.AngularVelocity : 0f;
                    Steer(ship.Forward, touch.Stick, angularVelocity, out float stickTurn, out float stickThrust);
                    turn = stickTurn;
                    thrust = Mathf.Max(thrust, stickThrust);
                }
                fire |= touch.Fire;
                dash |= touch.ConsumeDash();
                bomb |= touch.ConsumeBomb();
            }
            Turn = Mathf.Clamp(turn, -1f, 1f);
            Thrust = Mathf.Clamp01(thrust);
            Brake = brake;
            Fire = fire;
            DashPressed = dash;
            BombPressed = bomb;
        }

        /// <summary>
        /// The commands of a touch stick pointing along <paramref name="stick"/> (its length is how far it is pushed) for a
        /// ship whose nose points along <paramref name="forward"/> and turns at <paramref name="angularVelocity"/> degrees
        /// per second: turn toward the stick, easing off near it, and thrust by the push once the nose is within about 70
        /// degrees of it.
        /// </summary>
        public static void Steer(Vector2 forward, Vector2 stick, float angularVelocity, out float turn, out float thrust)
        {
            turn = 0f;
            thrust = 0f;
            if (stick.sqrMagnitude < 1e-6f || forward.sqrMagnitude < 1e-6f)
            {
                return;
            }
            // Positive angles are counter-clockwise, which is a left turn for both the stick and the ship.
            float error = Vector2.SignedAngle(forward, stick);
            turn = Mathf.Clamp((error - angularVelocity * TurnLead) / FullTurnAngle, -1f, 1f);
            float alignment = Mathf.Cos(error * Mathf.Deg2Rad);
            thrust = Mathf.Clamp01(stick.magnitude) * Mathf.Clamp01((alignment - 0.35f) / 0.65f);
        }

        private static bool Held(KeyCode key)
        {
            return key != KeyCode.None && Input.GetKey(key);
        }

        private static bool Pressed(KeyCode key)
        {
            return key != KeyCode.None && Input.GetKeyDown(key);
        }

        /// <summary>Whether the legacy input manager defines the gamepad axes (a project without them throws on read).</summary>
        private static bool AxesAvailable
        {
            get
            {
                if (axesAvailable == null)
                {
                    try
                    {
                        Input.GetAxis("Horizontal");
                        Input.GetAxis("Vertical");
                        axesAvailable = true;
                    }
                    catch (ArgumentException)
                    {
                        axesAvailable = false;
                    }
                }
                return axesAvailable.Value;
            }
        }

        private static float SafeAxis(string axis)
        {
            try
            {
                return Input.GetAxisRaw(axis);
            }
            catch (ArgumentException)
            {
                return 0f;
            }
        }
    }
}
