using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Portfolio.MemoryCards
{
    /// <summary>A world in the level select: its mascot, name and stars, or a lock with the stars it needs.</summary>
    public class WorldTab : MonoBehaviour
    {
        [SerializeField] internal Button button;
        [SerializeField] internal RectTransform content;
        [SerializeField] internal Image background;
        [SerializeField] internal Image mascot;
        [SerializeField] internal TMP_Text title;
        [SerializeField] internal TMP_Text subtitle;
        [SerializeField] internal Image lockIcon;
        [SerializeField] internal Image starIcon;
        [SerializeField] internal Color textColor = new Color(0.2f, 0.22f, 0.3f);
        [SerializeField] internal Color textSelected = Color.white;
        [Tooltip("What the accent of the world blends towards while the tab is not selected.")]
        [SerializeField] internal Color idleBlend = Color.white;
        [SerializeField] internal Color lockedColor = new Color(0.7f, 0.72f, 0.78f);

        private int index;
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
        }

        /// <summary>Takes the colours of <paramref name="palette"/>; nothing changes without one.</summary>
        public void ApplyLook(MemoryCardsTheme.Palette palette)
        {
            if (palette == null)
            {
                return;
            }
            textColor = palette.tabText;
            textSelected = palette.tabTextSelected;
            idleBlend = palette.tabIdle;
            lockedColor = palette.tabLocked;
        }

        public void Show(WorldSummary world, bool isSelected)
        {
            index = world.Index;
            selected = isSelected;
            if (background != null)
            {
                background.color = world.Unlocked ? (isSelected ? world.Accent : Color.Lerp(world.Accent, idleBlend, 0.55f)) : lockedColor;
            }
            if (mascot != null)
            {
                mascot.sprite = world.Mascot;
                mascot.color = world.Unlocked ? Color.white : new Color(0.25f, 0.25f, 0.3f, 0.8f);
            }
            if (title != null)
            {
                title.text = world.Title;
                title.color = isSelected && world.Unlocked ? textSelected : textColor;
            }
            if (subtitle != null)
            {
                subtitle.text = world.Unlocked ? (world.MaxStars > 0 ? $"{world.Stars}/{world.MaxStars}" : MemoryCardsText.T("Bonus")) : MemoryCardsText.F("{0} needed", world.StarsRequired);
                Color words = isSelected && world.Unlocked ? textSelected : textColor;
                subtitle.color = new Color(words.r, words.g, words.b, 0.9f);
            }
            if (lockIcon != null)
            {
                lockIcon.gameObject.SetActive(!world.Unlocked);
            }
            if (starIcon != null)
            {
                starIcon.gameObject.SetActive(world.MaxStars > 0 || !world.Unlocked);
            }
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            lift = Mathf.MoveTowards(lift, selected ? 1f : 0f, dt * 6f);
            phase += dt;
            if (content != null)
            {
                content.anchoredPosition = new Vector2(0f, lift * 8f);
            }
            if (mascot != null)
            {
                float wiggle = selected ? Mathf.Sin(phase * 4f) * 6f : 0f;
                mascot.rectTransform.localRotation = Quaternion.Euler(0f, 0f, wiggle);
            }
        }
    }
}
