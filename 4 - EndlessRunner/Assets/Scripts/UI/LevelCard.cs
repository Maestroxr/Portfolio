using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Portfolio.EndlessRunner
{
    /// <summary>A level in the level select strip: number, name, stars, lock and selection frame.</summary>
    public class LevelCard : MonoBehaviour
    {
        [SerializeField] internal Button button;
        [SerializeField] internal Image background;
        [SerializeField] internal Image frame;
        [SerializeField] internal TMP_Text number;
        [SerializeField] internal TMP_Text title;
        [SerializeField] internal Image[] stars = new Image[0];
        [SerializeField] internal GameObject lockIcon;
        [SerializeField] internal GameObject endlessIcon;

        private static readonly Color LockedColor = new Color(0.36f, 0.38f, 0.45f);

        public event Action<int> Clicked;

        public int LevelIndex { get; private set; }

        private void Awake()
        {
            if (button != null)
            {
                button.onClick.AddListener(() => Clicked?.Invoke(LevelIndex));
            }
        }

        public void Show(LevelSummary summary, bool selected, Sprite starFull, Sprite starEmpty)
        {
            LevelIndex = summary.Index;
            if (number != null)
            {
                number.text = summary.Endless ? string.Empty : (summary.Index + 1).ToString();
            }
            if (endlessIcon != null)
            {
                endlessIcon.SetActive(summary.Endless);
            }
            if (title != null)
            {
                title.text = summary.Title;
            }
            if (background != null)
            {
                background.color = summary.Unlocked ? summary.Accent : LockedColor;
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
                frame.enabled = selected;
            }
            transform.localScale = Vector3.one * (selected ? 1.08f : 1f);
        }
    }
}
