using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Portfolio.Monopoly
{
    /// <summary>A game mode on the new game screen: its icon, name, one line about it and the stars won on it.</summary>
    public class ModeCard : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Image frame;
        [SerializeField] private Image iconBackground;
        [SerializeField] private TMP_Text icon;
        [SerializeField] private TMP_Text title;
        [SerializeField] private TMP_Text tagline;
        [SerializeField] private TMP_Text[] stars = new TMP_Text[3];
        [SerializeField] private GameObject selectedMark;

        private Action<int> select;
        private int index;
        private float lift;
        private bool selected;

        private void Awake()
        {
            button.onClick.AddListener(() => select?.Invoke(index));
        }

        public void Bind(int modeIndex, MonopolyLevel mode, int earned, Action<int> onSelect)
        {
            index = modeIndex;
            select = onSelect;
            gameObject.SetActive(mode != null);
            if (mode == null)
            {
                return;
            }
            iconBackground.color = mode.Accent;
            icon.text = mode.Icon;
            title.text = mode.Title;
            tagline.text = mode.Tagline;
            for (int i = 0; i < stars.Length; i++)
            {
                stars[i].color = i < earned ? MonopolyStyle.Gold : MonopolyStyle.WithAlpha(MonopolyStyle.Muted, 0.35f);
            }
        }

        public void SetSelected(bool value, Color accent)
        {
            selected = value;
            frame.color = value ? accent : Color.white;
            if (selectedMark != null)
            {
                selectedMark.SetActive(value);
            }
        }

        private void Update()
        {
            lift = Mathf.MoveTowards(lift, selected ? 1f : 0f, Time.unscaledDeltaTime * 5f);
            float scale = 1f + 0.05f * Tween.OutCubic(lift);
            transform.localScale = new Vector3(scale, scale, 1f);
        }
    }
}
