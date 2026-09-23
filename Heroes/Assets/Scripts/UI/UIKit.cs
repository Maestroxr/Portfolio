using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Portfolio.Heroes.UI
{
    /// <summary>
    /// The pieces the interface is put together from: framed panels, gold rimmed buttons, labels, icons and army
    /// slots, all cut from the sprites and fonts of <see cref="HeroesArt"/>. The screens build themselves from these
    /// when the game starts rather than being laid out by hand, because almost everything in them depends on what the
    /// scenario turns out to hold: how many heroes, which buildings, how many creatures in a town.
    /// </summary>
    public static class UIKit
    {
        public static readonly Color Ink = new Color(0.94f, 0.9f, 0.8f);
        public static readonly Color Dim = new Color(0.72f, 0.67f, 0.56f);
        public static readonly Color Gold = new Color(0.88f, 0.74f, 0.4f);
        public static readonly Color Bad = new Color(0.92f, 0.45f, 0.38f);
        public static readonly Color Good = new Color(0.55f, 0.88f, 0.5f);

        public static HeroesArt Art { get; set; }

        // ------------------------------------------------------------------ rectangles

        public static RectTransform Rect(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            return rect;
        }

        public static RectTransform Stretch(RectTransform rect, float left = 0f, float bottom = 0f, float right = 0f, float top = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
            return rect;
        }

        /// <summary>Pins a rectangle of a size to a corner or an edge, by an anchor both ends share.</summary>
        public static RectTransform Pin(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        // ------------------------------------------------------------------ panels and frames

        public static Image Sprite(Transform parent, string name, Sprite sprite, Color color)
        {
            var image = Rect(parent, name).gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.type = sprite != null && sprite.border.sqrMagnitude > 0f ? Image.Type.Sliced : Image.Type.Simple;
            image.raycastTarget = false;
            return image;
        }

        /// <summary>A framed panel: the dark leather with its gold border that everything else sits on.</summary>
        public static Image Frame(Transform parent, string name)
        {
            Image image = Sprite(parent, name, Art != null ? Art.frame : null, Color.white);
            image.raycastTarget = true;
            return image;
        }

        public static Image Panel(Transform parent, string name, float alpha = 1f)
        {
            Image image = Sprite(parent, name, Art != null ? Art.panel : null, new Color(1f, 1f, 1f, alpha));
            image.raycastTarget = true;
            return image;
        }

        public static Image Parchment(Transform parent, string name)
        {
            return Sprite(parent, name, Art != null ? Art.parchment : null, Color.white);
        }

        // ------------------------------------------------------------------ words

        public static TextMeshProUGUI Label(Transform parent, string name, string text, float size, Color color,
            TextAlignmentOptions align = TextAlignmentOptions.Left, bool title = false)
        {
            var label = Rect(parent, name).gameObject.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = size;
            label.color = color;
            label.alignment = align;
            label.raycastTarget = false;
            label.font = Art != null ? title ? Art.titleFont : Art.bodyFont : null;
            label.overflowMode = TextOverflowModes.Overflow;
            label.textWrappingMode = TextWrappingModes.Normal;
            return label;
        }

        public static TextMeshProUGUI Title(Transform parent, string name, string text, float size, Color color,
            TextAlignmentOptions align = TextAlignmentOptions.Center)
        {
            TextMeshProUGUI label = Label(parent, name, text, size, color, align, true);
            label.characterSpacing = 4f;
            return label;
        }

        // ------------------------------------------------------------------ buttons

        /// <summary>A button with the three states of the interface, and a label that dims when it is off.</summary>
        public static Button Push(Transform parent, string name, string text, Action onClick, float fontSize = 26f)
        {
            Image background = Sprite(parent, name, Art != null ? Art.button : null, Color.white);
            background.raycastTarget = true;
            var button = background.gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            button.transition = Selectable.Transition.SpriteSwap;
            button.spriteState = new SpriteState
            {
                highlightedSprite = Art != null ? Art.buttonHover : null,
                pressedSprite = Art != null ? Art.buttonPressed : null,
                selectedSprite = Art != null ? Art.buttonHover : null,
                disabledSprite = Art != null ? Art.buttonPressed : null
            };
            if (!string.IsNullOrEmpty(text))
            {
                TextMeshProUGUI label = Label(background.transform, "Label", text, fontSize, Ink, TextAlignmentOptions.Center);
                Stretch((RectTransform)label.transform, 12f, 6f, 12f, 6f);
                label.textWrappingMode = TextWrappingModes.NoWrap;
            }
            if (onClick != null)
            {
                button.onClick.AddListener(() => onClick());
            }
            background.gameObject.AddComponent<Clicker>();
            return button;
        }

        /// <summary>A round button holding an icon: the small commands of the map bar.</summary>
        public static Button Icon(Transform parent, string name, Sprite icon, Action onClick, string tip = null)
        {
            Image background = Sprite(parent, name, Art != null ? Art.round : null, Color.white);
            background.raycastTarget = true;
            var button = background.gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            button.transition = Selectable.Transition.ColorTint;
            button.colors = Tint();
            if (icon != null)
            {
                Image image = Sprite(background.transform, "Icon", icon, Gold);
                Stretch((RectTransform)image.transform, 14f, 14f, 14f, 14f);
            }
            if (onClick != null)
            {
                button.onClick.AddListener(() => onClick());
            }
            background.gameObject.AddComponent<Clicker>();
            if (!string.IsNullOrEmpty(tip))
            {
                Tooltip.Attach(background.gameObject, tip);
            }
            return button;
        }

        public static ColorBlock Tint()
        {
            ColorBlock colors = ColorBlock.defaultColorBlock;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 0.95f, 0.8f);
            colors.pressedColor = new Color(0.7f, 0.65f, 0.55f);
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color(0.45f, 0.45f, 0.45f, 0.6f);
            colors.fadeDuration = 0.08f;
            return colors;
        }

        /// <summary>Greys a button out and stops it answering.</summary>
        public static void Enable(Button button, bool on)
        {
            if (button == null)
            {
                return;
            }
            button.interactable = on;
            var label = button.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
            {
                label.color = on ? Ink : new Color(0.55f, 0.52f, 0.45f);
            }
        }

        // ------------------------------------------------------------------ pieces of the game

        /// <summary>A resource with its icon and its amount, as the top bar shows it.</summary>
        public static TextMeshProUGUI Resource(Transform parent, ResourceKind kind, float height)
        {
            RectTransform row = Rect(parent, Land.ResourceName(kind));
            Image icon = Sprite(row, "Icon", Art != null ? Art.Resource(kind) : null, Color.white);
            Pin((RectTransform)icon.transform, new Vector2(0f, 0.5f), new Vector2(2f, 0f), new Vector2(height, height));
            ((RectTransform)icon.transform).pivot = new Vector2(0f, 0.5f);
            TextMeshProUGUI amount = Label(row, "Amount", "0", height * 0.6f, Ink, TextAlignmentOptions.MidlineLeft);
            Stretch((RectTransform)amount.transform, height + 8f, 0f, 4f, 0f);
            amount.textWrappingMode = TextWrappingModes.NoWrap;
            return amount;
        }

        /// <summary>The square an army slot is shown in: the creature, how many, and whether it is picked.</summary>
        public static Image Slot(Transform parent, string name)
        {
            Image image = Sprite(parent, name, Art != null ? Art.slot : null, Color.white);
            image.raycastTarget = true;
            return image;
        }

        public static Image Bar(Transform parent, string name, Color color)
        {
            Image back = Sprite(parent, name, Art != null ? Art.bar : null, new Color(0.12f, 0.1f, 0.08f, 0.9f));
            Image fill = Sprite(back.transform, "Fill", Art != null ? Art.bar : null, color);
            RectTransform rect = Stretch((RectTransform)fill.transform, 2f, 2f, 2f, 2f);
            rect.anchorMax = new Vector2(1f, 1f);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            return fill;
        }

        /// <summary>A column or a row that lays its children out for you.</summary>
        public static T Layout<T>(RectTransform rect, float spacing, RectOffset padding = null) where T : HorizontalOrVerticalLayoutGroup
        {
            T group = rect.gameObject.AddComponent<T>();
            group.spacing = spacing;
            group.padding = padding ?? new RectOffset(0, 0, 0, 0);
            group.childControlWidth = true;
            group.childControlHeight = true;
            group.childForceExpandWidth = false;
            group.childForceExpandHeight = false;
            return group;
        }

        public static GridLayoutGroup Grid(RectTransform rect, Vector2 cell, Vector2 spacing, int columns)
        {
            var grid = rect.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = cell;
            grid.spacing = spacing;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = columns;
            return grid;
        }

        /// <summary>A scrolling area with a column inside it, for lists that can grow past the screen.</summary>
        public static RectTransform Scroll(Transform parent, string name, out ScrollRect scroll)
        {
            RectTransform outer = Rect(parent, name);
            scroll = outer.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.scrollSensitivity = 28f;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            RectTransform viewport = Rect(outer, "Viewport");
            Stretch(viewport);
            // A nearly invisible image is enough to catch a drag, but it makes a stencil Mask clip everything away,
            // so the viewport is cut to its rectangle instead.
            viewport.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.004f);
            viewport.gameObject.AddComponent<RectMask2D>();
            RectTransform content = Rect(viewport, "Content");
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.sizeDelta = new Vector2(0f, 0f);
            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport = viewport;
            scroll.content = content;
            return content;
        }

        public static void Fit(RectTransform rect, float width = 0f, float height = 0f, bool expandWidth = false)
        {
            var element = rect.gameObject.AddComponent<LayoutElement>();
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
            element.flexibleWidth = expandWidth ? 1f : 0f;
        }
    }

    /// <summary>Clicks and hovers play the sounds of the interface.</summary>
    public sealed class Clicker : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler
    {
        public static HeroesAudio Audio { get; set; }

        public void OnPointerClick(PointerEventData eventData)
        {
            var button = GetComponent<Button>();
            Audio?.Play(button == null || button.interactable ? Sfx.Click : Sfx.Error, 0.7f);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            Audio?.Play(Sfx.Hover, 0.25f);
        }
    }

    /// <summary>The line of text that follows the pointer over anything that explains itself.</summary>
    public sealed class Tooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private static Tooltip showing;
        private static RectTransform box;
        private static TextMeshProUGUI label;

        public string Text { get; set; }

        public static void Attach(GameObject target, string text)
        {
            Tooltip tip = target.GetComponent<Tooltip>() ?? target.AddComponent<Tooltip>();
            tip.Text = text;
            var graphic = target.GetComponent<Graphic>();
            if (graphic != null)
            {
                graphic.raycastTarget = true;
            }
        }

        /// <summary>Makes the one box every tooltip is shown in, on the topmost canvas.</summary>
        public static void Prepare(Transform canvas)
        {
            if (box != null)
            {
                return;
            }
            Image frame = UIKit.Panel(canvas, "Tooltip");
            box = (RectTransform)frame.transform;
            box.anchorMin = box.anchorMax = new Vector2(0f, 0f);
            box.pivot = new Vector2(0f, 0f);
            frame.raycastTarget = false;
            // The box is a column of one line of text: a fixed width, and as tall as the words need.
            VerticalLayoutGroup layout = UIKit.Layout<VerticalLayoutGroup>(box, 0f, new RectOffset(14, 14, 10, 10));
            layout.childForceExpandWidth = true;
            label = UIKit.Label(box, "Text", "", 22f, UIKit.Ink);
            UIKit.Fit((RectTransform)label.transform, 352f);
            box.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            box.sizeDelta = new Vector2(380f, 40f);
            box.gameObject.SetActive(false);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (box == null || string.IsNullOrEmpty(Text))
            {
                return;
            }
            showing = this;
            label.text = Text;
            box.gameObject.SetActive(true);
            box.SetAsLastSibling();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (showing == this && box != null)
            {
                showing = null;
                box.gameObject.SetActive(false);
            }
        }

        private void Update()
        {
            if (showing != this || box == null)
            {
                return;
            }
            var canvas = box.parent as RectTransform;
            if (canvas == null)
            {
                return;
            }
            // Screens opened after the box would otherwise cover it.
            if (box.GetSiblingIndex() != canvas.childCount - 1)
            {
                box.SetAsLastSibling();
            }
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas, Input.mousePosition, null, out Vector2 point);
            // The box sits above and to the right of the pointer, and turns back at the edges of the screen.
            Vector2 size = box.rect.size;
            float x = point.x + 18f;
            float y = point.y + 18f;
            Rect area = canvas.rect;
            if (x + size.x > area.xMax)
            {
                x = point.x - 18f - size.x;
            }
            if (y + size.y > area.yMax)
            {
                y = point.y - 18f - size.y;
            }
            box.anchoredPosition = new Vector2(x, y);
        }

        private void OnDisable()
        {
            if (showing == this && box != null)
            {
                showing = null;
                box.gameObject.SetActive(false);
            }
        }
    }
}
