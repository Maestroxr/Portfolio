using Gamebox;
using Gamebox.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Portfolio.Asteroids
{
    /// <summary>
    /// The ship's on-screen controls on phones and tablets: a floating stick for the left thumb that the player's
    /// controls turn into steering and thrust (<see cref="PlayerShipInput.Steer"/>), a fire button that shoots while it
    /// is held, and dash and nova bomb buttons that show when they are ready. The <see cref="TouchLayout"/> on the same
    /// object shows them only when the game is played by touch.
    /// </summary>
    public class ShipTouchControls : MonoBehaviour
    {
        [SerializeField] internal VirtualJoystick stick;
        [SerializeField] internal TouchButton fire;
        [SerializeField] internal TouchButton dash;
        [SerializeField] internal TouchButton bomb;
        [Tooltip("Icon of the fire button; shows the current weapon.")]
        [SerializeField] internal Image fireIcon;
        [Tooltip("Ring around the dash button that fills as the dash recharges.")]
        [SerializeField] internal Image dashReady;
        [SerializeField] internal TMP_Text bombCount;
        [SerializeField] internal CanvasGroup bombGroup;

        /// <summary>Whether the controls are on screen and in use.</summary>
        public bool Active => isActiveAndEnabled && MobilePlatform.UsesTouch;

        /// <summary>Where the stick points, -1 to 1 on both axes.</summary>
        public Vector2 Stick => stick != null ? stick.Value : Vector2.zero;

        /// <summary>Whether a thumb holds the stick outside its dead zone.</summary>
        public bool Steering => stick != null && stick.IsHeld && stick.Value.sqrMagnitude > 0f;

        public bool Fire => fire != null && fire.IsHeld;

        /// <summary>Whether the dash button was pressed since the last call.</summary>
        public bool ConsumeDash()
        {
            return dash != null && dash.ConsumePress();
        }

        /// <summary>Whether the nova bomb button was pressed since the last call.</summary>
        public bool ConsumeBomb()
        {
            return bomb != null && bomb.ConsumePress();
        }

        /// <summary>Shows the dash recharge (0 ready, 1 just used) and the nova bombs left.</summary>
        public void ShowState(float dashRecharge, int bombs)
        {
            if (dashReady != null)
            {
                dashReady.fillAmount = 1f - Mathf.Clamp01(dashRecharge);
                dashReady.color = dashRecharge <= 0f ? new Color(0.5f, 0.9f, 1f) : new Color(0.35f, 0.45f, 0.6f);
            }
            if (bombCount != null)
            {
                bombCount.text = bombs.ToString();
            }
            if (bombGroup != null)
            {
                bombGroup.alpha = bombs > 0 ? 1f : 0.4f;
            }
        }

        /// <summary>Shows the icon of the current weapon on the fire button.</summary>
        public void ShowWeapon(Sprite icon)
        {
            if (fireIcon != null && icon != null)
            {
                fireIcon.sprite = icon;
            }
        }
    }
}
