using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Portfolio.Asteroids
{
    /// <summary>A mission on the sector map: number, name, stars, a boss or endless badge, the lock and the selection glow.</summary>
    public class MissionNode : MonoBehaviour
    {
        [SerializeField] internal Button button;
        [SerializeField] internal Image background;
        [SerializeField] internal Image frame;
        [SerializeField] internal TMP_Text number;
        [SerializeField] internal TMP_Text title;
        [SerializeField] internal Image[] stars = new Image[0];
        [SerializeField] internal GameObject lockIcon;
        [SerializeField] internal GameObject bossIcon;
        [SerializeField] internal GameObject endlessIcon;

        private static readonly Color LockedColor = new Color(0.2f, 0.22f, 0.3f, 0.9f);
        private bool selected;

        public event Action<int> Clicked;

        public int MissionIndex { get; private set; }


        private void Awake()
        {
            if (button != null)
            {
                button.onClick.AddListener(() => Clicked?.Invoke(MissionIndex));
            }
        }


        public void Show(MissionSummary summary, bool isSelected, Sprite starFull, Sprite starEmpty)
        {
            MissionIndex = summary.Index;
            selected = isSelected;
            if (number != null)
            {
                // A locked mission shows its padlock in place of the number.
                number.text = summary.Endless || !summary.Unlocked ? string.Empty : summary.Number.ToString();
            }
            if (title != null)
            {
                title.text = summary.Title;
            }
            if (endlessIcon != null)
            {
                endlessIcon.SetActive(summary.Endless);
            }
            if (bossIcon != null)
            {
                bossIcon.SetActive(summary.Boss);
            }
            if (background != null)
            {
                Color accent = summary.Accent;
                background.color = summary.Unlocked ? new Color(accent.r * 0.55f, accent.g * 0.55f, accent.b * 0.55f, 0.92f) : LockedColor;
            }
            for (int i = 0; i < stars.Length; i++)
            {
                if (stars[i] == null)
                {
                    continue;
                }
                stars[i].gameObject.SetActive(!summary.Endless);
                stars[i].sprite = i < summary.Stars ? starFull : starEmpty;
            }
            if (lockIcon != null)
            {
                lockIcon.SetActive(!summary.Unlocked);
            }
            if (frame != null)
            {
                frame.enabled = isSelected;
                frame.color = summary.Accent;
            }
            transform.localScale = Vector3.one * (isSelected ? 1.07f : 1f);
        }


        private void Update()
        {
            if (!selected || frame == null)
            {
                return;
            }
            Color color = frame.color;
            color.a = 0.65f + 0.35f * Mathf.Sin(Time.unscaledTime * 5f);
            frame.color = color;
        }
    }
}
