using Gamebox.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Portfolio.Heroes.UI
{
    /// <summary>
    /// The look of a choice between a few named values, in a sunken box: what it is about on the left, the value in gold
    /// in the middle and a round arrow on either side. The stepping itself (round from the last value to the first) is
    /// the framework's <see cref="ChoiceSelector"/>, the same as the settings panel's rows. The campaign screen chooses
    /// where battles are fought with one.
    /// </summary>
    public static class Stepper
    {
        /// <summary>
        /// Makes the box, <paramref name="width"/> by <paramref name="height"/>: <paramref name="caption"/>, then the arrows
        /// around the value.
        /// </summary>
        public static ChoiceSelector Make(Transform parent, string name, string caption, string[] options, float width, float height = 54f,
            float captionWidth = 110f)
        {
            Image box = UIKit.Recess(parent, name);
            var rect = (RectTransform)box.transform;
            rect.sizeDelta = new Vector2(width, height);
            RectTransform content = UIKit.Content(box, 2f);

            TextMeshProUGUI label = UIKit.Label(content, "Caption", caption, 20f, UIKit.Dim, TextAlignmentOptions.MidlineLeft);
            RectTransform labelRect = (RectTransform)label.transform;
            labelRect.anchorMin = new Vector2(0f, 0f);
            labelRect.anchorMax = new Vector2(0f, 1f);
            labelRect.pivot = new Vector2(0f, 0.5f);
            labelRect.offsetMin = new Vector2(10f, 0f);
            labelRect.offsetMax = new Vector2(10f + captionWidth, 0f);
            UIKit.FitLine(label, 20f, 13f);

            float arrow = Mathf.Min(height - 12f, 40f);
            RectTransform area = UIKit.Rect(content, "Choice");
            UIKit.Stretch(area, captionWidth + 14f, 0f, 2f, 0f);
            Button previous = Arrow(area, "Previous", arrow, false);
            Button next = Arrow(area, "Next", arrow, true);
            TextMeshProUGUI shown = UIKit.Label(area, "Value", "", 21f, UIKit.Gold, TextAlignmentOptions.Center, true);
            UIKit.Stretch((RectTransform)shown.transform, arrow + 6f, 0f, arrow + 6f, 0f);
            UIKit.FitLine(shown, 21f, 13f);
            shown.characterSpacing = 1f;

            var choice = box.gameObject.AddComponent<ChoiceSelector>();
            choice.Setup(shown, previous, next, options);
            return choice;
        }

        /// <summary>A round button with a gold triangle turned to point back or on.</summary>
        private static Button Arrow(RectTransform area, string name, float size, bool forward)
        {
            Button button = UIKit.Icon(area, name, null, null);
            UIKit.Pin((RectTransform)button.transform, new Vector2(forward ? 1f : 0f, 0.5f), Vector2.zero, new Vector2(size, size));
            TextMeshProUGUI glyph = UIKit.Label(button.transform, "Icon", "▲", size * 0.42f, UIKit.Gold, TextAlignmentOptions.Center);
            UIKit.Stretch((RectTransform)glyph.transform);
            glyph.rectTransform.localEulerAngles = new Vector3(0f, 0f, forward ? -90f : 90f);
            return button;
        }
    }
}
