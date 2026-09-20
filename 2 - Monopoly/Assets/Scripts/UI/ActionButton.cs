using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Portfolio.Monopoly
{
    /// <summary>A colored button with an icon and a label that runs an <see cref="ActionOption"/>; it bounces when pressed.</summary>
    public class ActionButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [SerializeField] private Button button;
        [SerializeField] private Image background;
        [Tooltip("The darker edge under the button, recoloured with the face.")]
        [SerializeField] private Image lip;
        [SerializeField] private TMP_Text label;
        [SerializeField] private TMP_Text icon;

        private Action onClick;
        private float pressed;
        private bool down;
        private float labelSize;
        private float iconSize;

        private void Awake()
        {
            if (button != null)
            {
                button.onClick.AddListener(() => onClick?.Invoke());
            }
            CaptureSizes();
        }

        private void CaptureSizes()
        {
            if (labelSize <= 0f)
            {
                labelSize = label != null ? label.fontSize : 22f;
                iconSize = icon != null ? icon.fontSize : 21f;
            }
        }

        /// <summary>The width the icon and the label need at full size.</summary>
        public float NeededWidth()
        {
            CaptureSizes();
            float width = 48f;
            if (label != null && !string.IsNullOrEmpty(label.text))
            {
                label.fontSize = labelSize;
                width += label.GetPreferredValues(label.text, 4000f, 200f).x;
            }
            if (icon != null && icon.gameObject.activeSelf)
            {
                icon.fontSize = iconSize;
                width += icon.GetPreferredValues(icon.text, 4000f, 200f).x + 10f;
            }
            return width;
        }

        /// <summary>Sets the width, shrinking the icon and the label by <paramref name="scale"/> when the row is crowded.</summary>
        public void Fit(float width, float scale)
        {
            CaptureSizes();
            var rect = (RectTransform)transform;
            rect.sizeDelta = new Vector2(width, rect.sizeDelta.y);
            if (label != null)
            {
                label.fontSize = labelSize * scale;
            }
            if (icon != null)
            {
                icon.fontSize = iconSize * scale;
            }
        }

        public void Set(ActionOption option)
        {
            onClick = option.onClick;
            if (label != null)
            {
                label.text = option.label;
            }
            if (icon != null)
            {
                icon.text = option.icon ?? "";
                icon.gameObject.SetActive(!string.IsNullOrEmpty(option.icon));
            }
            Color face = option.enabled ? option.color : MonopolyStyle.Tint(MonopolyStyle.Muted, 0.55f);
            if (background != null)
            {
                background.color = face;
            }
            if (lip != null)
            {
                lip.color = MonopolyStyle.Shade(face, option.color.grayscale > 0.9f ? 0.78f : 0.7f);
            }
            if (button != null)
            {
                button.interactable = option.enabled;
            }
            Color text = option.color.grayscale > 0.72f ? MonopolyStyle.Ink : Color.white;
            if (label != null)
            {
                label.color = option.enabled ? text : MonopolyStyle.WithAlpha(Color.white, 0.8f);
            }
            if (icon != null)
            {
                icon.color = label != null ? label.color : text;
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            down = button == null || button.interactable;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            down = false;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            down = false;
        }

        private void Update()
        {
            pressed = Mathf.MoveTowards(pressed, down ? 1f : 0f, Time.unscaledDeltaTime * 12f);
            float scale = 1f - 0.07f * pressed;
            transform.localScale = new Vector3(scale, scale, 1f);
        }
    }
}
