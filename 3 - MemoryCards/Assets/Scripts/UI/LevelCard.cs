using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Portfolio.MemoryCards
{
    /// <summary>
    /// A level in the level select, drawn as a card of its world: the number (or an icon for the endless run and free
    /// play), the title, the stars earned and a lock while it is closed. The selected card stands up and bobs.
    /// </summary>
    public class LevelCard : MonoBehaviour
    {
        [SerializeField] internal Button button;
        [SerializeField] internal RectTransform content;
        [SerializeField] internal Image back;
        [SerializeField] internal Image outline;
        [SerializeField] internal TMP_Text number;
        [SerializeField] internal Image icon;
        [SerializeField] internal TMP_Text title;
        [SerializeField] internal Image[] stars = new Image[0];
        [SerializeField] internal GameObject lockRoot;
        [SerializeField] internal Image newBadge;
        [SerializeField] internal Sprite endlessIcon;
        [SerializeField] internal Sprite freePlayIcon;

        private int index = -1;
        private bool selected;
        private float lift;
        private float phase;

        public event Action<int> Clicked;

        private void Awake()
        {
            if (button != null)
            {
                button.onClick.AddListener(() => Clicked?.Invoke(index));
            }
            phase = UnityEngine.Random.Range(0f, 6f);
        }

        public void Show(LevelSummary level, bool isSelected, Sprite starFull, Sprite starEmpty)
        {
            index = level.Index;
            selected = isSelected;
            if (back != null)
            {
                back.sprite = level.CardBack;
                back.color = level.Unlocked ? Color.white : new Color(0.62f, 0.62f, 0.68f);
            }
            if (outline != null)
            {
                outline.color = level.Accent;
                outline.enabled = isSelected;
            }
            bool campaign = level.Kind == LevelKind.Campaign;
            if (number != null)
            {
                number.gameObject.SetActive(campaign);
                number.text = level.Number.ToString();
                number.color = level.AccentDark;
            }
            if (icon != null)
            {
                icon.gameObject.SetActive(!campaign);
                icon.sprite = level.Kind == LevelKind.Endless ? endlessIcon : freePlayIcon;
                icon.color = level.AccentDark;
            }
            if (title != null)
            {
                title.text = level.Title;
            }
            for (int i = 0; i < stars.Length; i++)
            {
                if (stars[i] == null)
                {
                    continue;
                }
                stars[i].gameObject.SetActive(campaign);
                stars[i].sprite = i < level.Stars ? starFull : starEmpty;
            }
            if (lockRoot != null)
            {
                lockRoot.SetActive(!level.Unlocked);
            }
            if (newBadge != null)
            {
                newBadge.gameObject.SetActive(level.Unlocked && campaign && level.Stars == 0 && !string.IsNullOrEmpty(level.Introduces));
            }
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            lift = Mathf.MoveTowards(lift, selected ? 1f : 0f, dt * 5f);
            phase += dt;
            if (content != null)
            {
                float bob = selected ? Mathf.Sin(phase * 3f) * 5f : 0f;
                content.anchoredPosition = new Vector2(0f, lift * 14f + bob);
                content.localScale = Vector3.one * (1f + lift * 0.06f);
                content.localRotation = Quaternion.Euler(0f, 0f, selected ? Mathf.Sin(phase * 2f) * 1.5f : 0f);
            }
        }
    }
}
