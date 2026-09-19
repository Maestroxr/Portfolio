using System;
using UnityEngine;

namespace Portfolio.Asteroids
{
    /// <summary>Where the ship's commands come from: the keyboard and a gamepad, or the autopilot.</summary>
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
    /// Keyboard and gamepad controls: the keys of the ship's <see cref="PlayerSettings"/> plus the arrow keys, and the
    /// input manager's Horizontal / Vertical axes and fire buttons when the project defines them.
    /// </summary>
    public class KeyboardShipInput : IShipInput
    {
        private static bool? axesAvailable;

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
            Turn = Mathf.Clamp(turn, -1f, 1f);
            Thrust = Mathf.Clamp01(thrust);
            Brake = brake;
            Fire = fire;
            DashPressed = dash;
            BombPressed = bomb;
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
