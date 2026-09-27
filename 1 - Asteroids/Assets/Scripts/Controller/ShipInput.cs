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

        /// <summary>True on the frame the nova bomb was pressed (the megabomb in strike).</summary>
        bool BombPressed { get; }

        /// <summary>Strike: where to fly, -1 to 1 on each axis (right and up are positive).</summary>
        Vector2 Move { get; }

        /// <summary>Strike: true on the frame the next special weapon was asked for.</summary>
        bool CyclePressed { get; }

        /// <summary>Reads the commands for this frame.</summary>
        void Read(AsteroidsPlayer ship, float deltaTime);
    }


    /// <summary>
    /// The player's controls: the keys of the ship's <see cref="PlayerSettings"/> plus the arrow keys, the input
    /// manager's Horizontal / Vertical axes and fire buttons when the project defines them, and the on-screen
    /// <see cref="ShipTouchControls"/> on phones and tablets. The touch stick turns the ship toward where it points and
    /// thrusts once the nose points roughly that way, so a light touch aims and a full push flies. For the strike mode
    /// the same controls also give a direct 8-way <see cref="Move"/> (the stick moves the ship directly) and
    /// <see cref="CyclePressed"/> (the dash inputs plus Q and Tab; the touch DASH button becomes WEAPON).
    /// </summary>
    public class PlayerShipInput : IShipInput
    {
        /// <summary>Angle between the nose and the stick, in degrees, that turns at full rate.</summary>
        public const float FullTurnAngle = 40f;

        /// <summary>Seconds of the current turn rate counted against the angle left, so the nose settles instead of swinging past.</summary>
        public const float TurnLead = 0.12f;

        /// <summary>A gamepad axis counts from this far off centre.</summary>
        public const float AxisDeadZone = 0.2f;

        /// <summary>Strike: the keys that select the next special weapon (the dash keys among them).</summary>
        public static readonly KeyCode[] CycleKeys = { KeyCode.LeftShift, KeyCode.RightShift, KeyCode.K, KeyCode.Q, KeyCode.Tab };

        /// <summary>The gamepad's dash buttons (X, LB), which select the next special weapon in strike.</summary>
        public static readonly KeyCode[] DashButtons = { KeyCode.JoystickButton2, KeyCode.JoystickButton4 };

        /// <summary>The nova bomb keys next to the ship's own (the megabomb in strike).</summary>
        public static readonly KeyCode[] BombKeys = { KeyCode.B, KeyCode.E, KeyCode.L };

        /// <summary>The gamepad's nova bomb buttons (B, Y).</summary>
        public static readonly KeyCode[] BombButtons = { KeyCode.JoystickButton1, KeyCode.JoystickButton3 };

        private static bool? axesAvailable;

        /// <summary>The on-screen controls read next to the keyboard and gamepad; null without touch controls.</summary>
        public ShipTouchControls Touch { get; set; }

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
            PlayerSettings keys = ship != null ? ship.PlayerSettings : null;
            float turn = 0f;
            float thrust = 0f;
            bool brake = false;
            Vector2 move = Vector2.zero;
            if (Held(keys != null ? keys.Left : KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
            {
                turn += 1f;
                move.x -= 1f;
            }
            if (Held(keys != null ? keys.Right : KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
            {
                turn -= 1f;
                move.x += 1f;
            }
            if (Held(keys != null ? keys.Forward : KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
            {
                thrust = 1f;
                move.y += 1f;
            }
            if (Held(keys != null ? keys.Brake : KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
            {
                brake = true;
                move.y -= 1f;
            }
            bool fire = Held(keys != null ? keys.Shoot : KeyCode.Space) || Input.GetKey(KeyCode.J);
            bool dash = Pressed(keys != null ? keys.Dash : KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.K) || Input.GetKeyDown(KeyCode.RightShift);
            bool bomb = Pressed(keys != null ? keys.Bomb : KeyCode.B) || AnyPressed(BombKeys);
            // Strike: the next special on LeftShift, RightShift, K, Q and Tab, and on every dash input (below).
            bool cycle = AnyPressed(CycleKeys);
            float horizontal = 0f;
            float vertical = 0f;

            if (AxesAvailable)
            {
                horizontal = SafeAxis("Horizontal");
                vertical = SafeAxis("Vertical");
                if (Mathf.Abs(turn) < 0.01f && Mathf.Abs(horizontal) > AxisDeadZone)
                {
                    turn = -horizontal;
                }
                if (thrust < 0.01f && vertical > AxisDeadZone)
                {
                    thrust = vertical;
                }
                brake |= vertical < -0.5f;
            }
            // Gamepad buttons (Xbox layout): A or RB fires, X or LB dashes, B or Y sets off a nova bomb.
            fire |= Input.GetKey(KeyCode.JoystickButton0) || Input.GetKey(KeyCode.JoystickButton5);
            dash |= AnyPressed(DashButtons);
            bomb |= AnyPressed(BombButtons);
            bool steering = false;
            Vector2 stick = Vector2.zero;

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
                if (touch.Steering)
                {
                    // Strike: the stick moves the ship directly.
                    steering = true;
                    stick = touch.Stick;
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
            Move = MoveOf(move, horizontal, vertical, steering, stick);
            CyclePressed = dash || cycle;
        }


        /// <summary>
        /// Strike: where to fly, -1 to 1 per axis. <paramref name="keys"/> is what the arrow and movement keys ask for (-1,
        /// 0 or 1 per axis); an axis without a key follows the gamepad (<paramref name="horizontal"/>,
        /// <paramref name="vertical"/>) past its dead zone; a held touch stick (<paramref name="steering"/>) moves the ship
        /// directly and wins over both.
        /// </summary>
        public static Vector2 MoveOf(Vector2 keys, float horizontal, float vertical, bool steering, Vector2 stick)
        {
            Vector2 move = keys;
            if (Mathf.Abs(move.x) < 0.01f && Mathf.Abs(horizontal) > AxisDeadZone)
            {
                move.x = horizontal;
            }
            if (Mathf.Abs(move.y) < 0.01f && Mathf.Abs(vertical) > AxisDeadZone)
            {
                move.y = vertical;
            }
            if (steering)
            {
                move = stick;
            }
            return new Vector2(Mathf.Clamp(move.x, -1f, 1f), Mathf.Clamp(move.y, -1f, 1f));
        }


        /// <summary>Strike: whether <paramref name="key"/> selects the next special weapon (a cycle key or a dash button).</summary>
        public static bool IsCycleKey(KeyCode key)
        {
            return Array.IndexOf(CycleKeys, key) >= 0 || Array.IndexOf(DashButtons, key) >= 0;
        }


        /// <summary>Whether <paramref name="key"/> sets off the nova bomb (the megabomb in strike) besides the ship's own key.</summary>
        public static bool IsBombKey(KeyCode key)
        {
            return Array.IndexOf(BombKeys, key) >= 0 || Array.IndexOf(BombButtons, key) >= 0;
        }


        private static bool AnyPressed(KeyCode[] keys)
        {
            for (int i = 0; i < keys.Length; i++)
            {
                if (Input.GetKeyDown(keys[i]))
                {
                    return true;
                }
            }
            return false;
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
