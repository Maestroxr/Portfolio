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

        public void Show(WorldSummary world, bool isSelected)
        {
            index = world.Index;
            selected = isSelected;
            if (background != null)
            {
                background.color = world.Unlocked ? (isSelected ? world.Accent : Color.Lerp(world.Accent, Color.white, 0.55f)) : new Color(0.7f, 0.72f, 0.78f);
            }
            if (mascot != null)
            {
                mascot.sprite = world.Mascot;
                mascot.color = world.Unlocked ? Color.white : new Color(0.25f, 0.25f, 0.3f, 0.8f);
            }
            if (title != null)
            {
                title.text = world.Title;
                title.color = isSelected && world.Unlocked ? Color.white : new Color(0.2f, 0.22f, 0.3f);
            }
            if (subtitle != null)
            {
                subtitle.text = world.Unlocked ? (world.MaxStars > 0 ? $"{world.Stars}/{world.MaxStars}" : "Bonus") : $"{world.StarsRequired} needed";
                subtitle.color = isSelected && world.Unlocked ? new Color(1f, 1f, 1f, 0.9f) : new Color(0.25f, 0.27f, 0.36f, 0.9f);
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
