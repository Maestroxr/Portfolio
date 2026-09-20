using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Portfolio.Monopoly
{
    /// <summary>A choice offered in the action panel.</summary>
    public struct ActionOption
    {
        public string label;
        public string icon;
        public Color color;
        public Action onClick;
        public bool enabled;
        /// <summary>A keyboard shortcut (Space for the main choice), or none.</summary>
        public KeyCode key;

        public static ActionOption Of(string label, string icon, Color color, Action onClick, bool enabled = true, KeyCode key = KeyCode.None)
        {
            return new ActionOption { label = label, icon = icon, color = color, onClick = onClick, enabled = enabled, key = key };
        }
    }

    /// <summary>
    /// The prompt in the middle of the board: whose turn it is, what is happening, and the choices of the player who has
    /// to decide (roll, buy, end the turn, pay the fine...). While a computer player thinks it shows a spinner instead.
    /// </summary>
    public class ActionPanel : MonoBehaviour
    {
        [SerializeField] private CanvasGroup group;
        [SerializeField] private Image badge;
        [SerializeField] private Image tokenIcon;
        [SerializeField] private TMP_Text title;
        [SerializeField] private TMP_Text subtitle;
        [SerializeField] private ActionButton[] buttons = new ActionButton[0];
        [Tooltip("The row holding the buttons; they are sized to their labels and share its width.")]
        [SerializeField] private HorizontalLayoutGroup row;
        [SerializeField] private float minimumButtonWidth = 150f;
        [SerializeField] private GameObject thinking;
        [SerializeField] private RectTransform spinner;

        private readonly List<ActionOption> shown = new List<ActionOption>();
        private float visible;
        private bool wanted;

        public void Show(string heading, string detail, Color accent, Sprite token, IList<ActionOption> options, bool isThinking)
        {
            wanted = true;
            gameObject.SetActive(true);
            if (title != null)
            {
                title.text = heading;
            }
            if (subtitle != null)
            {
                subtitle.text = detail;
                subtitle.gameObject.SetActive(!string.IsNullOrEmpty(detail));
            }
            if (badge != null)
            {
                badge.color = accent;
            }
            if (tokenIcon != null)
            {
                tokenIcon.sprite = token;
                tokenIcon.enabled = token != null;
            }
            shown.Clear();
            for (int i = 0; i < buttons.Length; i++)
            {
                bool used = options != null && i < options.Count;
                buttons[i].gameObject.SetActive(used);
                if (used)
                {
                    buttons[i].Set(options[i]);
                    shown.Add(options[i]);
                }
            }
            FitButtons();
            if (thinking != null)
            {
                thinking.SetActive(isThinking);
            }
        }

        /// <summary>Sizes the buttons to their labels, shrinking them all evenly when they would not fit the row.</summary>
        private void FitButtons()
        {
            var needs = new float[buttons.Length];
            float total = 0f;
            int count = 0;
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i].gameObject.activeSelf)
                {
                    needs[i] = Mathf.Max(minimumButtonWidth, buttons[i].NeededWidth());
                    total += needs[i];
                    count++;
                }
            }
            if (count == 0)
            {
                return;
            }
            float spacing = row != null ? row.spacing : 12f;
            float available = (row != null ? ((RectTransform)row.transform).rect.width : 670f) - spacing * (count - 1);
            float scale = Mathf.Min(1f, available / total);
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i].gameObject.activeSelf)
                {
                    buttons[i].Fit(needs[i] * scale, scale);
                }
            }
        }

        public void Hide()
        {
            wanted = false;
            shown.Clear();
        }

        /// <summary>Whether the panel is offering choices right now.</summary>
        public bool HasChoices => wanted && shown.Count > 0;

        private void Update()
        {
            visible = Mathf.MoveTowards(visible, wanted ? 1f : 0f, Time.unscaledDeltaTime * 6f);
            if (group != null)
            {
                group.alpha = visible;
                group.interactable = wanted;
                group.blocksRaycasts = wanted && visible > 0.5f;
            }
            if (!wanted && visible <= 0f)
            {
                gameObject.SetActive(false);
                return;
            }
            if (spinner != null && thinking != null && thinking.activeSelf)
            {
                spinner.localRotation = Quaternion.Euler(0f, 0f, -Time.unscaledTime * 300f);
            }
            if (!wanted || Time.timeScale <= 0f)
            {
                return;
            }
            foreach (ActionOption option in shown)
            {
                if (option.enabled && option.key != KeyCode.None && Input.GetKeyDown(option.key))
                {
                    option.onClick?.Invoke();
                    return;
                }
            }
        }
    }
}
