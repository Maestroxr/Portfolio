using Gamebox;
using Gamebox.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Portfolio.Asteroids.AsteroidsText;

namespace Portfolio.Asteroids
{
    /// <summary>
    /// The ship's on-screen controls on phones and tablets: a floating stick for the left thumb that the player's
    /// controls turn into steering and thrust (<see cref="PlayerShipInput.Steer"/>), a fire button that shoots while it
    /// is held, and dash and nova bomb buttons that show when they are ready. The <see cref="TouchLayout"/> on the same
    /// object shows them only when the game is played by touch. In a strike mission (<see cref="ShowMode"/>) the stick
    /// moves the ship directly, the dash button becomes WEAPON (the next special) and the nova button MEGA (the megabomb).
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
        [Tooltip("Label under the dash button (WEAPON in strike). Made from the bomb count's text when not set; then shown only in strike.")]
        [SerializeField] internal TMP_Text dashLabel;
        [Tooltip("Label under the nova button (MEGA in strike). Made from the bomb count's text when not set; then shown only in strike.")]
        [SerializeField] internal TMP_Text bombLabel;

        private bool labelsMade;
        private int shownBombs = -1;

        /// <summary>The kind of mission the controls are laid out for.</summary>
        public MissionMode Mode { get; private set; }

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

        /// <summary>
        /// Lays the buttons out for <paramref name="mode"/>: DASH and NOVA in the asteroid field, WEAPON and MEGA in strike
        /// (no dash recharge ring there).
        /// </summary>
        public void ShowMode(MissionMode mode)
        {
            Mode = mode;
            shownBombs = -1;
            bool strike = mode == MissionMode.Strike;
            if (strike)
            {
                MakeLabels();
            }
            WriteLabels();
            if (dashReady != null)
            {
                dashReady.enabled = !strike;
            }
        }


        /// <summary>The language changed: the button labels are written again.</summary>
        public void RefreshTexts()
        {
            if (dashLabel != null || bombLabel != null)
            {
                WriteLabels();
            }
        }


        private void WriteLabels()
        {
            bool strike = Mode == MissionMode.Strike;
            SetLabel(dashLabel, strike ? T("WEAPON") : T("DASH"), strike);
            SetLabel(bombLabel, strike ? T("MEGA") : T("NOVA"), strike);
        }


        /// <summary>Strike: the megabombs left and the cooldown (0 ready, 1 just used) on the MEGA button.</summary>
        public void ShowStrikeState(int megabombs, float cooldown)
        {
            if (bombCount != null && megabombs != shownBombs)
            {
                shownBombs = megabombs;
                bombCount.text = megabombs.ToString();
            }
            if (bombGroup != null)
            {
                bombGroup.alpha = megabombs > 0 && cooldown <= 0f ? 1f : 0.4f;
            }
        }


        private void SetLabel(TMP_Text label, string text, bool strike)
        {
            if (label == null)
            {
                return;
            }
            // Labels made here are the strike's own: the asteroid field keeps its buttons as they were.
            label.gameObject.SetActive(strike || !labelsMade);
            label.text = text;
        }


        /// <summary>Makes the WEAPON and MEGA labels from the bomb count's text when the builder did not.</summary>
        private void MakeLabels()
        {
            if (labelsMade || bombCount == null || (dashLabel != null && bombLabel != null))
            {
                return;
            }
            labelsMade = true;
            if (dashLabel == null && dash != null)
            {
                dashLabel = MakeLabel(dash.transform);
            }
            if (bombLabel == null && bomb != null)
            {
                bombLabel = MakeLabel(bomb.transform);
            }
        }


        private TMP_Text MakeLabel(Transform button)
        {
            TMP_Text label = Instantiate(bombCount, button);
            label.name = "Label";
            label.raycastTarget = false;
            label.fontSize = 24f;
            label.alignment = TextAlignmentOptions.Center;
            label.enableAutoSizing = false;
            RectTransform rect = label.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -4f);
            rect.sizeDelta = new Vector2(180f, 32f);
            return label;
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
