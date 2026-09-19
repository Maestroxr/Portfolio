using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Portfolio.Asteroids
{
    /// <summary>A ship in the hangar: its picture, name, ratings and what it takes to unlock it.</summary>
    public class ShipCard : MonoBehaviour
    {
        [SerializeField] internal Button button;
        [SerializeField] internal Image frame;
        [SerializeField] internal Image preview;
        [SerializeField] internal TMP_Text title;
        [SerializeField] internal TMP_Text description;
        [SerializeField] internal TMP_Text status;
        [Tooltip("Rating bars: speed, handling, hull, fire rate.")]
        [SerializeField] internal Image[] bars = new Image[0];
        [SerializeField] internal GameObject lockIcon;

        public event Action<int> Clicked;

        public int ShipIndex { get; private set; }


        private void Awake()
        {
            if (button != null)
            {
                button.onClick.AddListener(() => Clicked?.Invoke(ShipIndex));
            }
        }


        public void Show(HangarShip ship, Color accent)
        {
            ShipIndex = ship.Index;
            if (title != null)
            {
                title.text = ship.Name.ToUpperInvariant();
            }
            if (description != null)
            {
                description.text = ship.Description;
            }
            if (status != null)
            {
                status.text = ship.Selected ? "SELECTED" : ship.Unlocked ? "CLICK TO FLY" : $"{ship.StarsToUnlock} STARS TO UNLOCK";
                status.color = ship.Selected ? accent : ship.Unlocked ? Color.white : new Color(1f, 0.8f, 0.4f);
            }
            float[] ratings = { ship.Speed, ship.Handling, ship.Hull, ship.FireRate };
            for (int i = 0; i < bars.Length && i < ratings.Length; i++)
            {
                if (bars[i] != null)
                {
                    bars[i].fillAmount = Mathf.Clamp01(ratings[i]);
                    bars[i].color = ship.Unlocked ? accent : new Color(0.4f, 0.42f, 0.5f);
                }
            }
            if (preview != null)
            {
                preview.color = ship.Unlocked ? Color.white : new Color(0.1f, 0.1f, 0.15f, 0.9f);
            }
            if (lockIcon != null)
            {
                lockIcon.SetActive(!ship.Unlocked);
            }
            if (frame != null)
            {
                frame.enabled = ship.Selected;
                frame.color = accent;
            }
        }
    }
}
