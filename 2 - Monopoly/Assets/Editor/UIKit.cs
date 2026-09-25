using Gamebox.Editor;
using Gamebox.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Portfolio.Monopoly.EditorTools
{
    /// <summary>
    /// Builds the pieces of the interface in the game's style: white rounded cards with soft shadows, bold rounded
    /// buttons with a glossy top, Poppins text and Font Awesome icons. Positions are in reference pixels (1920 x 1080).
    /// </summary>
    internal static class UIKit
    {
        public static TMP_FontAsset Body => MonopolyArtBuilder.Body;
        public static TMP_FontAsset Bold => MonopolyArtBuilder.Bold;
        public static TMP_FontAsset Heavy => MonopolyArtBuilder.Heavy;
        public static TMP_FontAsset IconFont => MonopolyArtBuilder.IconFont;

        public static RectTransform Rect(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            return rect;
        }

        /// <summary>Anchors a rect at one point of its parent (0..1) with a pivot, an offset and a size.</summary>
        public static RectTransform Place(RectTransform rect, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        public static RectTransform Center(RectTransform rect, Vector2 position, Vector2 size)
        {
            return Place(rect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, size);
        }

        public static RectTransform Stretch(RectTransform rect, float left = 0f, float bottom = 0f, float right = 0f, float top = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
            return rect;
        }

        public static Image Image(Transform parent, string name, Sprite sprite, Color color, bool raycast = false)
        {
            RectTransform rect = Rect(parent, name);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = raycast;
            if (sprite != null && sprite.border != Vector4.zero)
            {
                image.type = UnityEngine.UI.Image.Type.Sliced;
            }
            return image;
        }

        /// <summary>A rounded card; <paramref name="radius"/> scales the corners (1 = 40 pixels).</summary>
        public static Image Card(Transform parent, string name, Color color, float radius = 0.6f, bool shadow = true)
        {
            RectTransform rect = Rect(parent, name);
            if (shadow)
            {
                Image drop = Image(rect, "Shadow", MonopolyArtBuilder.UI("Shadow"), new Color(0f, 0.02f, 0.08f, 0.35f));
                drop.pixelsPerUnitMultiplier = 1f / Mathf.Max(0.2f, radius);
                Stretch(drop.rectTransform, -22f, -30f, -22f, -14f);
                drop.transform.SetAsFirstSibling();
            }
            Image face = Image(rect, "Face", MonopolyArtBuilder.UI("Rounded"), color, true);
            face.pixelsPerUnitMultiplier = 1f / Mathf.Max(0.05f, radius);
            Stretch(face.rectTransform);
            // The card itself is the face; the rect holds the shadow and the face.
            return face;
        }

        public static Image Rounded(Transform parent, string name, Color color, float radius = 0.4f, bool raycast = false)
        {
            Image image = Image(parent, name, MonopolyArtBuilder.UI("Rounded"), color, raycast);
            image.pixelsPerUnitMultiplier = 1f / Mathf.Max(0.05f, radius);
            return image;
        }

        public static TextMeshProUGUI Text(Transform parent, string name, string text, float size, Color color, TMP_FontAsset font = null,
            TextAlignmentOptions alignment = TextAlignmentOptions.Center)
        {
            RectTransform rect = Rect(parent, name);
            var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.font = font != null ? font : Bold;
            label.text = text;
            label.fontSize = size;
            label.color = color;
            label.alignment = alignment;
            label.raycastTarget = false;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.overflowMode = TextOverflowModes.Overflow;
            return label;
        }

        public static TextMeshProUGUI Icon(Transform parent, string name, string glyph, float size, Color color)
        {
            TextMeshProUGUI icon = Text(parent, name, glyph, size, color, IconFont);
            icon.textWrappingMode = TextWrappingModes.NoWrap;
            return icon;
        }

        /// <summary>
        /// A rounded button: a coloured face with a glossy top, an optional icon left of the label, a darker lip under
        /// it, and a little give when pressed.
        /// </summary>
        public static Button Button(Transform parent, string name, string label, string icon, Color color, Vector2 size, float fontSize = 26f,
            Color? textColor = null)
        {
            RectTransform rect = Rect(parent, name);
            rect.sizeDelta = size;
            Image lip = Rounded(rect, "Lip", MonopolyStyle.Shade(color, 0.72f), 0.45f);
            Stretch(lip.rectTransform, 0f, -5f, 0f, 5f);
            Image face = Rounded(rect, "Face", color, 0.45f, true);
            Stretch(face.rectTransform);
            Image sheen = Image(face.transform, "Sheen", MonopolyArtBuilder.UI("Sheen"), new Color(1f, 1f, 1f, 0.5f));
            Stretch(sheen.rectTransform, 6f, size.y * 0.45f, 6f, 3f);
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = face;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.06f, 1.06f, 1.06f, 1f);
            colors.pressedColor = new Color(0.88f, 0.88f, 0.88f, 1f);
            colors.disabledColor = new Color(0.8f, 0.8f, 0.8f, 0.55f);
            colors.fadeDuration = 0.08f;
            button.colors = colors;
            Color ink = textColor ?? (color.grayscale > 0.72f ? MonopolyStyle.Ink : Color.white);
            RectTransform content = Rect(face.transform, "Content");
            Stretch(content, 10f, 0f, 10f, 0f);
            var layout = content.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = 10f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;
            if (!string.IsNullOrEmpty(icon))
            {
                TextMeshProUGUI glyph = Icon(content, "Icon", icon, fontSize * 0.95f, ink);
                glyph.alignment = TextAlignmentOptions.Center;
            }
            if (!string.IsNullOrEmpty(label))
            {
                TextMeshProUGUI text = Text(content, "Label", label, fontSize, ink, Heavy);
                text.textWrappingMode = TextWrappingModes.NoWrap;
            }
            Pressable(rect);
            return button;
        }

        /// <summary>Makes a control grow a touch under the pointer and give a little under a press, the Monopoly way.</summary>
        public static PressFeedback Pressable(RectTransform rect)
        {
            return PressFeedback.Attach(rect.gameObject, 1.03f, 0.93f);
        }

        public static TextMeshProUGUI LabelOf(Button button)
        {
            Transform label = GameMenuInstaller.FindChild(button.transform, "Label");
            return label != null ? label.GetComponent<TextMeshProUGUI>() : null;
        }

        public static TextMeshProUGUI IconOf(Button button)
        {
            Transform icon = GameMenuInstaller.FindChild(button.transform, "Icon");
            return icon != null ? icon.GetComponent<TextMeshProUGUI>() : null;
        }

        public static Image FaceOf(Button button)
        {
            Transform face = GameMenuInstaller.FindChild(button.transform, "Face");
            return face != null ? face.GetComponent<Image>() : null;
        }

        /// <summary>A round icon-only button.</summary>
        public static Button RoundButton(Transform parent, string name, string icon, Color color, float size, Color iconColor)
        {
            RectTransform rect = Rect(parent, name);
            rect.sizeDelta = new Vector2(size, size);
            Image shadow = Image(rect, "Shadow", MonopolyArtBuilder.UI("Circle"), new Color(0f, 0f, 0.1f, 0.25f));
            Stretch(shadow.rectTransform, 0f, -4f, 0f, 4f);
            Image face = Image(rect, "Face", MonopolyArtBuilder.UI("Circle"), color, true);
            Stretch(face.rectTransform);
            TextMeshProUGUI glyph = Icon(face.transform, "Icon", icon, size * 0.42f, iconColor);
            Stretch(glyph.rectTransform);
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = face;
            Pressable(rect);
            return button;
        }

        /// <summary>A switch with a label on the left, as a Toggle.</summary>
        public static Toggle Switch(Transform parent, string name, string label, float width, float height = 56f)
        {
            RectTransform row = Rect(parent, name);
            row.sizeDelta = new Vector2(width, height);
            var element = row.gameObject.AddComponent<LayoutElement>();
            element.preferredHeight = height;
            element.minHeight = height;
            TextMeshProUGUI caption = Text(row, "Label", label, 24f, MonopolyStyle.Ink, Body, TextAlignmentOptions.MidlineLeft);
            Stretch(caption.rectTransform, 8f, 0f, 110f, 0f);
            Image track = Rounded(row, "Track", MonopolyStyle.Tint(MonopolyStyle.Muted, 0.55f), 0.8f, true);
            Place(track.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-8f, 0f), new Vector2(84f, 42f));
            Image on = Rounded(track.transform, "On", MonopolyStyle.Green, 0.8f);
            Stretch(on.rectTransform);
            Image knob = Image(track.transform, "Knob", MonopolyArtBuilder.UI("Circle"), Color.white);
            Place(knob.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-4f, 0f), new Vector2(34f, 34f));
            var toggle = row.gameObject.AddComponent<Toggle>();
            toggle.targetGraphic = track;
            toggle.graphic = on;
            toggle.toggleTransition = Toggle.ToggleTransition.Fade;
            var knobMover = row.gameObject.AddComponent<SwitchKnob>();
            MonopolyAssets.SetObject(knobMover, "knob", knob.rectTransform);
            MonopolyAssets.SetObject(knobMover, "toggle", toggle);
            return toggle;
        }

        /// <summary>A number input with a label on the left.</summary>
        public static TMP_InputField NumberInput(Transform parent, string name, string label, float width, float height = 56f)
        {
            RectTransform row = Rect(parent, name);
            row.sizeDelta = new Vector2(width, height);
            var element = row.gameObject.AddComponent<LayoutElement>();
            element.preferredHeight = height;
            element.minHeight = height;
            TextMeshProUGUI caption = Text(row, "Label", label, 24f, MonopolyStyle.Ink, Body, TextAlignmentOptions.MidlineLeft);
            Stretch(caption.rectTransform, 8f, 0f, 190f, 0f);
            Image box = Rounded(row, "Input", MonopolyStyle.Panel, 0.3f, true);
            Place(box.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-8f, 0f), new Vector2(170f, height - 8f));
            var outline = box.gameObject.AddComponent<Outline>();
            outline.effectColor = MonopolyStyle.Tint(MonopolyStyle.Muted, 0.3f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);
            RectTransform area = Rect(box.transform, "Text Area");
            Stretch(area, 14f, 4f, 14f, 4f);
            area.gameObject.AddComponent<RectMask2D>();
            TextMeshProUGUI text = Text(area, "Text", "", 26f, MonopolyStyle.Ink, Bold, TextAlignmentOptions.MidlineRight);
            Stretch(text.rectTransform);
            TextMeshProUGUI placeholder = Text(area, "Placeholder", "0", 26f, MonopolyStyle.Muted, Bold, TextAlignmentOptions.MidlineRight);
            Stretch(placeholder.rectTransform);
            var field = box.gameObject.AddComponent<TMP_InputField>();
            field.textViewport = area;
            field.textComponent = text;
            field.placeholder = placeholder;
            field.contentType = TMP_InputField.ContentType.IntegerNumber;
            field.fontAsset = Bold;
            field.pointSize = 26f;
            field.targetGraphic = box;
            return field;
        }

        public static TMP_InputField TextInput(Transform parent, string name, string placeholderText, Vector2 size, float fontSize = 24f)
        {
            Image box = Rounded(parent, name, Color.white, 0.3f, true);
            box.rectTransform.sizeDelta = size;
            var outline = box.gameObject.AddComponent<Outline>();
            outline.effectColor = MonopolyStyle.Tint(MonopolyStyle.Muted, 0.3f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);
            RectTransform area = Rect(box.transform, "Text Area");
            Stretch(area, 12f, 2f, 12f, 2f);
            area.gameObject.AddComponent<RectMask2D>();
            TextMeshProUGUI text = Text(area, "Text", "", fontSize, MonopolyStyle.Ink, Bold, TextAlignmentOptions.Center);
            Stretch(text.rectTransform);
            TextMeshProUGUI placeholder = Text(area, "Placeholder", placeholderText, fontSize, MonopolyStyle.Muted, Body, TextAlignmentOptions.Center);
            Stretch(placeholder.rectTransform);
            var field = box.gameObject.AddComponent<TMP_InputField>();
            field.textViewport = area;
            field.textComponent = text;
            field.placeholder = placeholder;
            field.characterLimit = 12;
            field.fontAsset = Bold;
            field.pointSize = fontSize;
            field.targetGraphic = box;
            return field;
        }

        /// <summary>A vertical scroll list; items go into <paramref name="content"/>.</summary>
        public static ScrollRect ScrollList(Transform parent, string name, out RectTransform content, float spacing = 8f)
        {
            RectTransform root = Rect(parent, name);
            var scroll = root.gameObject.AddComponent<ScrollRect>();
            Image viewportImage = Image(root, "Viewport", MonopolyArtBuilder.UI("Rounded"), Color.white, true);
            Stretch(viewportImage.rectTransform);
            viewportImage.gameObject.AddComponent<RectMask2D>();
            viewportImage.color = new Color(1f, 1f, 1f, 0f);
            content = Rect(viewportImage.transform, "Content");
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.sizeDelta = new Vector2(0f, 0f);
            var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = spacing;
            layout.padding = new RectOffset(4, 4, 4, 4);
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport = viewportImage.rectTransform;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 30f;
            return scroll;
        }

        public static CanvasGroup Group(GameObject go)
        {
            CanvasGroup group = go.GetComponent<CanvasGroup>();
            return group != null ? group : go.AddComponent<CanvasGroup>();
        }

        public static LayoutElement Size(Component component, float width, float height)
        {
            LayoutElement element = component.GetComponent<LayoutElement>() ?? component.gameObject.AddComponent<LayoutElement>();
            if (width > 0f)
            {
                element.preferredWidth = width;
                element.minWidth = width;
            }
            if (height > 0f)
            {
                element.preferredHeight = height;
                element.minHeight = height;
            }
            return element;
        }

        public static HorizontalLayoutGroup Row(Transform parent, string name, float spacing, TextAnchor alignment = TextAnchor.MiddleCenter)
        {
            RectTransform rect = Rect(parent, name);
            var layout = rect.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = spacing;
            layout.childAlignment = alignment;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            return layout;
        }

        public static VerticalLayoutGroup Column(Transform parent, string name, float spacing, TextAnchor alignment = TextAnchor.UpperCenter)
        {
            RectTransform rect = Rect(parent, name);
            var layout = rect.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = spacing;
            layout.childAlignment = alignment;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            return layout;
        }

        /// <summary>A popup root: a full screen layer (optionally dimming what is behind) with a card in it.</summary>
        public static RectTransform PopupRoot(Transform parent, string name, Vector2 position, Vector2 size, float dim, out CanvasGroup group, out RectTransform card)
        {
            RectTransform root = Rect(parent, name);
            Stretch(root);
            group = Group(root.gameObject);
            if (dim > 0f)
            {
                Image back = Image(root, "Dim", null, new Color(0.02f, 0.03f, 0.06f, dim), true);
                Stretch(back.rectTransform, -400f, -400f, -400f, -400f);
            }
            card = Rect(root, "Card");
            Center(card, position, size);
            Image face = Card(card, "Body", Color.white, 0.6f);
            Stretch((RectTransform)face.transform.parent);
            return root;
        }
    }
}
