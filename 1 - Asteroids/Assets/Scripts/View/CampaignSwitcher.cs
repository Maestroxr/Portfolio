using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Portfolio.Asteroids.AsteroidsText;

namespace Portfolio.Asteroids
{
    /// <summary>
    /// The campaign bar of the mission select, next to the mode tabs: the campaign of the mode shown (its place among the
    /// mode's campaigns, its name and its story) with arrows to the campaign before and after it. A campaign that is not
    /// open yet shows a padlock and what opens it in place of its story. One bar serves both tabs; the manager says what
    /// it shows (<see cref="AsteroidsUI.ShowCampaign"/>) and hears the arrows (<see cref="Stepped"/>).
    /// </summary>
    public class CampaignSwitcher : MonoBehaviour
    {
        /// <summary>How long the name takes to settle after the campaign changed (s).</summary>
        private const float PunchTime = 0.25f;

        /// <summary>How far the texts move out to the ends when the bar has no arrows (a mode with one campaign).</summary>
        private const float ArrowRoom = 50f;

        /// <summary>How far the name moves over for the padlock of a closed campaign.</summary>
        private const float LockRoom = 40f;

        [SerializeField] internal Image background;
        [SerializeField] internal Button previous;
        [SerializeField] internal Button next;
        [Tooltip("\"CAMPAIGN 2 / 3\": the campaign's place among the mode's campaigns.")]
        [SerializeField] internal TMP_Text count;
        [SerializeField] internal TMP_Text title;
        [SerializeField] internal TMP_Text description;
        [SerializeField] internal GameObject lockIcon;
        [Tooltip("The bar's colour on the asteroid field tab.")]
        [SerializeField] internal Color fieldColor = new Color(0.3f, 0.6f, 0.9f, 0.9f);
        [Tooltip("The bar's colour on the planet strike tab.")]
        [SerializeField] internal Color strikeColor = new Color(0.9f, 0.55f, 0.3f, 0.9f);

        private bool measured;
        private Vector2 titleAt;
        private Vector2 lockAt;
        private Vector2 countAt;
        private Vector2 descriptionAt;
        private Vector2 descriptionSize;
        private MissionMode shownMode = (MissionMode)(-1);
        private int shownArc = -1;
        private float punch = -1f;

        /// <summary>An arrow was pressed: -1 for the campaign before, +1 for the one after.</summary>
        public event Action<int> Stepped;

        /// <summary>The campaign shown (tests and tours).</summary>
        public CampaignSummary Shown { get; private set; }


        private void Awake()
        {
            if (previous != null)
            {
                previous.onClick.AddListener(() => Stepped?.Invoke(-1));
            }
            if (next != null)
            {
                next.onClick.AddListener(() => Stepped?.Invoke(1));
            }
        }


        /// <summary>
        /// Shows campaign <paramref name="summary"/> in the colours of its mode (the name in <paramref name="accent"/>); a
        /// mode without campaigns hides the bar.
        /// </summary>
        public void Show(CampaignSummary summary, Color accent)
        {
            Shown = summary;
            bool any = summary.Count > 0;
            if (gameObject.activeSelf != any)
            {
                gameObject.SetActive(any);
            }
            if (!any)
            {
                return;
            }
            if (summary.Mode != shownMode || summary.Arc != shownArc)
            {
                // A new campaign: its name settles in with a small punch (not on the first show of a tab).
                punch = summary.Mode == shownMode ? 0f : -1f;
                shownMode = summary.Mode;
                shownArc = summary.Arc;
            }
            if (background != null)
            {
                background.color = summary.Mode == MissionMode.Strike ? strikeColor : fieldColor;
            }
            bool several = summary.Count > 1;
            SetArrow(previous, several, summary.Arc > 0);
            SetArrow(next, several, summary.Arc < summary.Count - 1);
            SetLabel(count, several ? F("CAMPAIGN {0} / {1}", summary.Arc + 1, summary.Count) : T("CAMPAIGN"));
            if (count != null)
            {
                Color dim = accent;
                dim.a = 0.75f;
                count.color = dim;
            }
            SetLabel(title, (summary.Title ?? string.Empty).ToUpperInvariant());
            if (title != null)
            {
                title.color = summary.Open ? accent : new Color(0.75f, 0.8f, 0.88f);
            }
            if (lockIcon != null)
            {
                lockIcon.SetActive(!summary.Open);
            }
            SetLabel(description, summary.Open ? summary.Description : $"<color=#FFB24D>{summary.LockReason}</color>");
            Arrange(several, !summary.Open);
        }


        /// <summary>
        /// Places the texts between the arrows (or out to the ends without them), the name after the padlock of a closed
        /// campaign.
        /// </summary>
        private void Arrange(bool arrows, bool locked)
        {
            if (!measured)
            {
                measured = true;
                titleAt = title != null ? title.rectTransform.anchoredPosition : Vector2.zero;
                lockAt = lockIcon != null ? ((RectTransform)lockIcon.transform).anchoredPosition : Vector2.zero;
                countAt = count != null ? count.rectTransform.anchoredPosition : Vector2.zero;
                descriptionAt = description != null ? description.rectTransform.anchoredPosition : Vector2.zero;
                descriptionSize = description != null ? description.rectTransform.sizeDelta : Vector2.zero;
            }
            float room = arrows ? 0f : ArrowRoom;
            if (title != null)
            {
                title.rectTransform.anchoredPosition = titleAt + new Vector2(-room + (locked && lockIcon != null ? LockRoom : 0f), 0f);
            }
            if (lockIcon != null)
            {
                ((RectTransform)lockIcon.transform).anchoredPosition = lockAt + new Vector2(-room, 0f);
            }
            if (count != null)
            {
                count.rectTransform.anchoredPosition = countAt + new Vector2(room, 0f);
            }
            if (description != null)
            {
                description.rectTransform.anchoredPosition = descriptionAt + new Vector2(-room, 0f);
                description.rectTransform.sizeDelta = descriptionSize + new Vector2(room * 2f, 0f);
            }
        }


        private void Update()
        {
            if (punch < 0f || title == null)
            {
                return;
            }
            punch += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(punch / PunchTime);
            title.transform.localScale = Vector3.one * (1f + 0.08f * (1f - t) * (1f - t));
            if (t >= 1f)
            {
                punch = -1f;
            }
        }


        private static void SetArrow(Button arrow, bool visible, bool enabled)
        {
            if (arrow == null)
            {
                return;
            }
            if (arrow.gameObject.activeSelf != visible)
            {
                arrow.gameObject.SetActive(visible);
            }
            arrow.interactable = enabled;
            // The tint greys the button; the arrow on it fades too.
            if (arrow.TryGetComponent(out CanvasGroup group))
            {
                group.alpha = enabled ? 1f : 0.35f;
            }
        }


        private static void SetLabel(TMP_Text label, string text)
        {
            if (label != null && label.text != text)
            {
                label.text = text;
            }
        }
    }
}
