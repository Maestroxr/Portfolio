using System;
using System.Text;
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
            Image image = Tiled(Sprite(parent, name, Art != null ? Art.frame : null, Color.white));
            image.raycastTarget = true;
            return image;
        }

        public static Image Panel(Transform parent, string name, float alpha = 1f)
        {
            Image image = Tiled(Sprite(parent, name, Art != null ? Art.panel : null, new Color(1f, 1f, 1f, alpha)));
            image.raycastTarget = true;
            return image;
        }

        public static Image Parchment(Transform parent, string name)
        {
            return Tiled(Sprite(parent, name, Art != null ? Art.parchment : null, Color.white));
        }

        /// <summary>
        /// Repeats the middle and the edges of a framed sprite instead of stretching them, so the grain of the leather,
        /// wood and paper stays as fine on a whole screen as on a card. The kit's sprites are drawn to tile.
        /// </summary>
        private static Image Tiled(Image image)
        {
            if (image.sprite != null && image.sprite.border.sqrMagnitude > 0f)
            {
                image.type = Image.Type.Tiled;
            }
            return image;
        }

        // ------------------------------------------------------------------ the kit

        /// <summary>
        /// How far in from its left, bottom, right and top edges the content of <paramref name="framed"/> goes: the
        /// width of the frame drawn on its sprite, in the units of its canvas.
        /// </summary>
        public static Vector4 Inset(Image framed)
        {
            if (framed == null || framed.sprite == null)
            {
                return Vector4.zero;
            }
            Vector4 inset = Art != null ? Art.ContentInset(framed.sprite) : framed.sprite.border;
            Canvas canvas = framed.canvas;
            float reference = canvas != null ? canvas.referencePixelsPerUnit : 100f;
            return inset * (reference / (framed.sprite.pixelsPerUnit * Mathf.Max(0.01f, framed.pixelsPerUnitMultiplier)));
        }

        /// <summary>
        /// A rectangle inside <paramref name="framed"/> that keeps clear of its frame, and <paramref name="extra"/> more:
        /// where the text, icons and lists of a framed widget go.
        /// </summary>
        public static RectTransform Content(Image framed, float extra = 6f, string name = "Content")
        {
            Vector4 inset = Inset(framed);
            return Stretch(Rect(framed.transform, name), inset.x + extra, inset.y + extra, inset.z + extra, inset.w + extra);
        }

        /// <summary>The same for a rectangle carrying a framed image (or any rectangle, kept <paramref name="extra"/> clear).</summary>
        public static RectTransform Content(RectTransform framed, float extra = 6f, string name = "Content")
        {
            Image image = framed.GetComponent<Image>();
            return image != null ? Content(image, extra, name) : Stretch(Rect(framed, name), extra, extra, extra, extra);
        }

        /// <summary>The padding a layout group on <paramref name="framed"/> needs to keep its children off the frame.</summary>
        public static RectOffset Padding(Image framed, float extra = 6f)
        {
            Vector4 inset = Inset(framed);
            return new RectOffset(Mathf.CeilToInt(inset.x + extra), Mathf.CeilToInt(inset.z + extra),
                Mathf.CeilToInt(inset.w + extra), Mathf.CeilToInt(inset.y + extra));
        }

        /// <summary>A bar along an edge of the screen: dark wood between two thin gold bands.</summary>
        public static Image Strip(Transform parent, string name)
        {
            Image image = Tiled(Sprite(parent, name, Kit(Art != null ? Art.strip : null, Art != null ? Art.panel : null), Color.white));
            image.raycastTarget = true;
            return image;
        }

        /// <summary>A small card or a row of a list, with a narrow gold rim.</summary>
        public static Image Card(Transform parent, string name)
        {
            Image image = Tiled(Sprite(parent, name, Kit(Art != null ? Art.card : null, Art != null ? Art.panel : null), Color.white));
            image.raycastTarget = true;
            return image;
        }

        /// <summary>A card that answers a click: brighter under the pointer, darker when pressed.</summary>
        public static Button CardButton(Transform parent, string name, Action onClick)
        {
            Image card = Card(parent, name);
            var button = card.gameObject.AddComponent<Button>();
            button.targetGraphic = card;
            button.transition = Selectable.Transition.SpriteSwap;
            button.spriteState = new SpriteState
            {
                highlightedSprite = Art != null ? Art.cardHover : null,
                pressedSprite = Art != null ? Art.cardSelected : null,
                disabledSprite = Art != null ? Art.card : null
            };
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            if (onClick != null)
            {
                button.onClick.AddListener(() => onClick());
            }
            card.gameObject.AddComponent<Clicker>();
            Gamebox.UI.PressFeedback.Attach(card.gameObject, 1.015f, 0.985f);
            return button;
        }

        /// <summary>Shows a card picked (a bright rim) or not.</summary>
        public static void Select(Image card, bool picked)
        {
            if (card != null && Art != null && Art.card != null)
            {
                card.sprite = picked && Art.cardSelected != null ? Art.cardSelected : Art.card;
            }
        }

        /// <summary>A box sunk into a panel, for a figure that must read at a glance: the date, a price, an amount.</summary>
        public static Image Recess(Transform parent, string name)
        {
            return Sprite(parent, name, Kit(Art != null ? Art.inset : null, Art != null ? Art.slot : null), Color.white);
        }

        /// <summary>
        /// A gold rule across <paramref name="parent"/> with an ornament in its middle, <paramref name="height"/> tall:
        /// two halves, the right one mirrored, so the ornament stays whole at any length.
        /// </summary>
        public static RectTransform Divider(Transform parent, string name, float height = 20f)
        {
            RectTransform rule = Rect(parent, name);
            rule.sizeDelta = new Vector2(rule.sizeDelta.x, height);
            Sprite half = Art != null ? Art.divider : null;
            for (int side = 0; side < 2; side++)
            {
                Image image = Sprite(rule, side == 0 ? "Left" : "Right", half, Color.white);
                var rect = (RectTransform)image.transform;
                rect.anchorMin = new Vector2(side * 0.5f, 0f);
                rect.anchorMax = new Vector2(0.5f + side * 0.5f, 1f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                // The right half is the left one turned over in place, so the two halves of the ornament meet in the middle.
                rect.localScale = new Vector3(side == 0 ? 1f : -1f, 1f, 1f);
            }
            return rule;
        }

        /// <summary>
        /// The crimson banner a window's title sits on, with the title written on it in gold. Size it to the words (about
        /// 1.6 times their width), and let it cross the top edge of its window.
        /// </summary>
        public static Image Ribbon(Transform parent, string name, string text, float fontSize = 30f)
        {
            Image ribbon = Sprite(parent, name, Kit(Art != null ? Art.ribbon : null, Art != null ? Art.banner : null), Color.white);
            ribbon.raycastTarget = false;
            TextMeshProUGUI label = Title(ribbon.transform, "Label", text, fontSize, Gold);
            Vector4 inset = Inset(ribbon);
            Stretch((RectTransform)label.transform, inset.x, inset.y, inset.z, inset.w);
            label.textWrappingMode = TextWrappingModes.NoWrap;
            Look(label, TextLook.Gold);
            return ribbon;
        }

        /// <summary>
        /// A portrait in its gold frame, square or round: a dark backdrop, the picture on it (cut to the disc when round),
        /// and the frame over both. Returns the picture, whose sprite can be changed later; the rectangle named
        /// <paramref name="name"/> that holds all three is <c>picture.transform.parent.parent</c>, and is what to size.
        /// </summary>
        public static Image PortraitFrame(Transform parent, string name, Sprite portrait, bool round = false)
        {
            RectTransform holder = Rect(parent, name);
            Sprite backSprite = Art != null ? round ? Art.portraitBackRound : Art.portraitBack : null;
            Image back = Sprite(holder, "Back", backSprite, Color.white);
            back.type = Image.Type.Simple;
            back.raycastTarget = true;
            Stretch((RectTransform)back.transform);
            if (round && backSprite != null)
            {
                back.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            }
            Image picture = Sprite(back.transform, "Portrait", portrait, Color.white);
            picture.type = Image.Type.Simple;
            picture.preserveAspect = true;
            float margin = round ? 2f : 5f;
            Stretch((RectTransform)picture.transform, margin, margin, margin, margin);
            Sprite frameSprite = Art != null ? round ? Art.portraitFrameRound : Art.portraitFrame : null;
            if (frameSprite != null)
            {
                Image frame = Sprite(holder, "Frame", frameSprite, Color.white);
                frame.type = round ? Image.Type.Simple : Image.Type.Sliced;
                Stretch((RectTransform)frame.transform);
            }
            return picture;
        }

        /// <summary>A pennant hanging from a gold pole, its cloth in <paramref name="color"/> (a player's color).</summary>
        public static Image Pennant(Transform parent, string name, Color color)
        {
            Image cloth = Sprite(parent, name, Art != null ? Art.pennant : null, color);
            cloth.type = Image.Type.Simple;
            if (Art != null && Art.pennantTrim != null)
            {
                Image trim = Sprite(cloth.transform, "Trim", Art.pennantTrim, Color.white);
                trim.type = Image.Type.Simple;
                Stretch((RectTransform)trim.transform);
            }
            return cloth;
        }

        /// <summary>Dark over the whole of <paramref name="parent"/>, deepest at the bottom: behind a dialog.</summary>
        public static Image Shade(Transform parent, string name, float alpha = 0.8f)
        {
            Image shade = Sprite(parent, name, Art != null ? Art.shade : null, new Color(1f, 1f, 1f, alpha));
            shade.type = Image.Type.Simple;
            Stretch((RectTransform)shade.transform);
            return shade;
        }

        /// <summary>A soft halo of <paramref name="color"/> behind what is picked or hovered.</summary>
        public static Image Halo(Transform parent, string name, Color color)
        {
            Image halo = Sprite(parent, name, Art != null ? Art.glow : null, color);
            halo.type = Image.Type.Simple;
            return halo;
        }

        /// <summary>A bar in its sunken gold rimmed trough; returns the fill, whose fillAmount is the value.</summary>
        public static Image Meter(Transform parent, string name, Color color)
        {
            if (Art == null || Art.barFrame == null)
            {
                return Bar(parent, name, color);
            }
            Image back = Sprite(parent, name, Art.barFrame, Color.white);
            Image fill = Sprite(back.transform, "Fill", Art.barFill != null ? Art.barFill : Art.bar, color);
            Vector4 inset = Inset(back);
            Stretch((RectTransform)fill.transform, inset.x, inset.y, inset.z, inset.w);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            return fill;
        }

        /// <summary>Gives a label one of the looks the builder made for its font (gold titles, shadowed text over the map).</summary>
        public static TextMeshProUGUI Look(TextMeshProUGUI label, TextLook look)
        {
            if (label != null && Art != null && label.font != null)
            {
                Material material = Art.TextMaterial(label.font, look);
                if (material != null)
                {
                    label.fontSharedMaterial = material;
                }
            }
            return label;
        }

        /// <summary>A title in gold with a dark outline and a shadow: the heading of a window or a panel.</summary>
        public static TextMeshProUGUI Heading(Transform parent, string name, string text, float size,
            TextAlignmentOptions align = TextAlignmentOptions.Center)
        {
            TextMeshProUGUI label = Title(parent, name, text, size, Color.white, align);
            label.enableVertexGradient = true;
            label.colorGradient = new VertexGradient(new Color(1f, 0.93f, 0.7f), new Color(1f, 0.93f, 0.7f),
                new Color(0.86f, 0.66f, 0.3f), new Color(0.86f, 0.66f, 0.3f));
            return Look(label, TextLook.Gold);
        }

        /// <summary>The name of the game in its decorative capitals.</summary>
        public static TextMeshProUGUI Logo(Transform parent, string name, string text, float size)
        {
            TextMeshProUGUI label = Label(parent, name, text, size, Color.white, TextAlignmentOptions.Center, true);
            if (Art != null && Art.logoFont != null)
            {
                label.font = Art.logoFont;
            }
            label.enableVertexGradient = true;
            label.colorGradient = new VertexGradient(new Color(1f, 0.95f, 0.75f), new Color(1f, 0.95f, 0.75f),
                new Color(0.82f, 0.58f, 0.22f), new Color(0.82f, 0.58f, 0.22f));
            return Look(label, TextLook.Logo);
        }

        /// <summary>A kit sprite, or an older one of the same use while the kit has not been built.</summary>
        private static Sprite Kit(Sprite sprite, Sprite fallback)
        {
            return sprite != null ? sprite : fallback;
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
            if (Art != null && Art.iconSprites != null)
            {
                // So a line can carry the icons of the game: <sprite name="gold"> 500.
                label.spriteAsset = Art.iconSprites;
            }
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
                disabledSprite = Art != null ? Kit(Art.buttonDisabled, Art.buttonPressed) : null
            };
            // A button clicked with the mouse does not stay lit as the selected one.
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            if (!string.IsNullOrEmpty(text))
            {
                TextMeshProUGUI label = Label(background.transform, "Label", text, fontSize, Ink, TextAlignmentOptions.Center);
                Stretch((RectTransform)label.transform, 12f, 6f, 12f, 6f);
                label.textWrappingMode = TextWrappingModes.NoWrap;
                FitLine(label, fontSize, Mathf.Min(fontSize, 14f));
                Look(label, TextLook.Shadow);
            }
            if (onClick != null)
            {
                button.onClick.AddListener(() => onClick());
            }
            background.gameObject.AddComponent<Clicker>();
            Gamebox.UI.PressFeedback.Attach(background.gameObject);
            return button;
        }

        /// <summary>A round button holding an icon: the small commands of the map bar.</summary>
        public static Button Icon(Transform parent, string name, Sprite icon, Action onClick, string tip = null)
        {
            Image background = Sprite(parent, name, Art != null ? Art.round : null, Color.white);
            background.raycastTarget = true;
            background.preserveAspect = true;
            var button = background.gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            if (Art != null && Art.roundHover != null)
            {
                button.transition = Selectable.Transition.SpriteSwap;
                button.spriteState = new SpriteState
                {
                    highlightedSprite = Art.roundHover,
                    pressedSprite = Kit(Art.roundPressed, Art.roundHover),
                    disabledSprite = null
                };
            }
            else
            {
                button.transition = Selectable.Transition.ColorTint;
                button.colors = Tint();
            }
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            if (icon != null)
            {
                Image image = Sprite(background.transform, "Icon", icon, Gold);
                image.preserveAspect = true;
                // The icon sits inside the gold ring, a quarter of the button in from every side.
                RectTransform glyph = (RectTransform)image.transform;
                glyph.anchorMin = new Vector2(0.24f, 0.24f);
                glyph.anchorMax = new Vector2(0.76f, 0.76f);
                glyph.offsetMin = Vector2.zero;
                glyph.offsetMax = Vector2.zero;
            }
            if (onClick != null)
            {
                button.onClick.AddListener(() => onClick());
            }
            background.gameObject.AddComponent<Clicker>();
            Gamebox.UI.PressFeedback.Attach(background.gameObject, 1.08f, 0.92f);
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
            // A round button greys its icon too.
            Transform icon = button.transform.Find("Icon");
            if (icon != null && icon.TryGetComponent(out Image glyph))
            {
                glyph.color = on ? Gold : new Color(0.45f, 0.42f, 0.36f);
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

        /// <summary>
        /// A figure in a sunken box, <paramref name="width"/> by <paramref name="height"/>: an icon on the left (gold when
        /// <paramref name="tinted"/>, as drawn otherwise) and a line of text; returns the text.
        /// </summary>
        public static TextMeshProUGUI Figure(Transform parent, string name, Sprite icon, bool tinted, string tip, float width,
            float height = 60f)
        {
            Image box = Recess(parent, name);
            Fit((RectTransform)box.transform, width, height);
            box.raycastTarget = true;
            RectTransform content = Content(box, 2f);
            float side = Mathf.Min(38f, height - 18f);
            Image image = Sprite(content, "Icon", icon, tinted ? Gold : Color.white);
            image.preserveAspect = true;
            Pin((RectTransform)image.transform, new Vector2(0f, 0.5f), new Vector2(4f, 0f), new Vector2(side, side));
            TextMeshProUGUI text = Label(content, "Text", "", 20f, Ink, TextAlignmentOptions.MidlineLeft);
            Stretch((RectTransform)text.transform, side + 12f, 0f, 4f, 0f);
            FitLine(text, 20f, 12f);
            if (!string.IsNullOrEmpty(tip))
            {
                Tooltip.Attach(box.gameObject, tip);
            }
            return text;
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

        // ------------------------------------------------------------------ words with icons

        /// <summary>The colours of words on parchment: brown ink, and a lighter brown for what matters less.</summary>
        public static readonly Color InkOnParchment = new Color(0.24f, 0.15f, 0.07f);
        public static readonly Color DimOnParchment = new Color(0.42f, 0.3f, 0.17f);

        /// <summary>The names the icons of the resources have in a line of text, by <see cref="ResourceKind"/>.</summary>
        private static readonly string[] ResourceSprites = { "gold", "wood", "ore", "mercury", "sulfur", "crystal", "gems" };

        /// <summary>
        /// An icon inside a line of text, by its name in the sprite asset of the interface: the resources, attack,
        /// defense, power, knowledge, damage, health, speed, movement, mana, morale, luck, experience, star, star_empty.
        /// </summary>
        public static string Glyph(string name)
        {
            return $"<sprite name=\"{name}\">";
        }

        public static string Glyph(ResourceKind kind)
        {
            return Glyph(ResourceSprites[Mathf.Clamp((int)kind, 0, ResourceSprites.Length - 1)]);
        }

        /// <summary>
        /// A price with the icon of each resource in it, <paramref name="times"/> over; amounts the holder of
        /// <paramref name="held"/> cannot pay are written in red.
        /// </summary>
        public static string Cost(ResourceSet cost, ResourceSet held = null, int times = 1)
        {
            var text = new StringBuilder();
            for (int i = 0; cost != null && i < ResourceSet.Kinds; i++)
            {
                int amount = cost.values[i] * times;
                if (amount == 0)
                {
                    continue;
                }
                if (text.Length > 0)
                {
                    text.Append("  ");
                }
                bool missing = held != null && held.values[i] < amount;
                text.Append(Glyph((ResourceKind)i)).Append(missing ? "<color=#EE7A66>" : "").Append(amount)
                    .Append(missing ? "</color>" : "");
            }
            return text.Length > 0 ? text.ToString() : "Free";
        }

        /// <summary><paramref name="count"/> stars of <paramref name="of"/>, as inline icons.</summary>
        public static string StarText(int count, int of = 3)
        {
            var text = new StringBuilder();
            for (int i = 0; i < of; i++)
            {
                text.Append(Glyph(i < count ? "star" : "star_empty"));
            }
            return text.ToString();
        }

        /// <summary>A number with a sign: +2, -1, 0.</summary>
        public static string Signed(int value)
        {
            return value > 0 ? $"+{value}" : value.ToString();
        }

        /// <summary>
        /// Keeps a label on one line: its words shrink from <paramref name="max"/> down to <paramref name="min"/> points
        /// to stay inside its box rather than wrapping or running out of it.
        /// </summary>
        public static TextMeshProUGUI FitLine(TextMeshProUGUI label, float max, float min = 14f)
        {
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Overflow;
            label.enableAutoSizing = true;
            label.fontSizeMax = max;
            label.fontSizeMin = Mathf.Min(min, max);
            label.fontSize = max;
            return label;
        }

        /// <summary>Stars in a row, filled up to <paramref name="count"/>: the record of a chapter.</summary>
        public static RectTransform Stars(Transform parent, string name, int count, float size, int of = 3, float spacing = 4f)
        {
            RectTransform row = Rect(parent, name);
            row.sizeDelta = new Vector2(of * size + (of - 1) * spacing, size);
            for (int i = 0; i < of; i++)
            {
                Image star = Sprite(row, $"Star{i}", Art != null ? i < count ? Art.star : Art.starEmpty : null, Color.white);
                star.preserveAspect = true;
                RectTransform rect = (RectTransform)star.transform;
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 0.5f);
                rect.anchoredPosition = new Vector2(i * (size + spacing), 0f);
                rect.sizeDelta = new Vector2(size, size);
            }
            return row;
        }

        /// <summary>The heading of a part of a window: gold words over a gold rule, across the top of <paramref name="parent"/>.</summary>
        public static TextMeshProUGUI SectionTitle(Transform parent, string name, string text, float size = 26f)
        {
            RectTransform holder = Rect(parent, name);
            holder.anchorMin = new Vector2(0f, 1f);
            holder.anchorMax = new Vector2(1f, 1f);
            holder.pivot = new Vector2(0.5f, 1f);
            holder.anchoredPosition = Vector2.zero;
            holder.sizeDelta = new Vector2(0f, size * 1.3f + 16f);
            TextMeshProUGUI title = Heading(holder, "Title", text, size);
            RectTransform titleRect = (RectTransform)title.transform;
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.anchoredPosition = Vector2.zero;
            titleRect.sizeDelta = new Vector2(0f, size * 1.3f);
            FitLine(title, size, 16f);
            RectTransform rule = Divider(holder, "Rule", 16f);
            rule.anchorMin = new Vector2(0.08f, 1f);
            rule.anchorMax = new Vector2(0.92f, 1f);
            rule.pivot = new Vector2(0.5f, 1f);
            rule.anchoredPosition = new Vector2(0f, -size * 1.3f);
            rule.sizeDelta = new Vector2(0f, 16f);
            return title;
        }

        /// <summary>Takes the children of <paramref name="rect"/> away at once (a layout no longer counts them).</summary>
        public static void Clear(Transform rect)
        {
            for (int i = rect.childCount - 1; i >= 0; i--)
            {
                GameObject child = rect.GetChild(i).gameObject;
                child.SetActive(false);
                UnityEngine.Object.Destroy(child);
            }
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

        /// <summary>
        /// A scrolling area with a column inside it, for lists that can grow past the screen; a thin gold bar on its right
        /// shows how far it goes when there is more than fits (none when <paramref name="bar"/> is false).
        /// </summary>
        public static RectTransform Scroll(Transform parent, string name, out ScrollRect scroll, bool bar = true)
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
            if (bar)
            {
                scroll.verticalScrollbar = ScrollBar(outer);
                scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
                scroll.verticalScrollbarSpacing = 4f;
            }
            return content;
        }

        /// <summary>A thin upright bar: a dark groove and a gold handle as long as the part of the list in sight.</summary>
        private static Scrollbar ScrollBar(RectTransform parent)
        {
            Image groove = Sprite(parent, "Scrollbar", Art != null ? Art.bar : null, new Color(0f, 0f, 0f, 0.4f));
            RectTransform rect = groove.rectTransform;
            rect.anchorMin = new Vector2(1f, 0f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 0.5f);
            rect.sizeDelta = new Vector2(8f, 0f);
            rect.anchoredPosition = Vector2.zero;
            RectTransform area = Stretch(Rect(rect, "Sliding Area"), 1f, 1f, 1f, 1f);
            Image handle = Sprite(area, "Handle", Art != null ? Art.bar : null, new Color(0.88f, 0.74f, 0.4f, 0.85f));
            handle.raycastTarget = true;
            Stretch(handle.rectTransform);
            var scrollbar = groove.gameObject.AddComponent<Scrollbar>();
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            scrollbar.handleRect = handle.rectTransform;
            scrollbar.targetGraphic = handle;
            scrollbar.navigation = new Navigation { mode = Navigation.Mode.None };
            scrollbar.colors = Tint();
            return scrollbar;
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

    /// <summary>
    /// Marks something that explains itself: while the pointer rests on it, its words show in the one
    /// <see cref="TooltipBox"/>. A text of several lines has its first line as the title.
    /// </summary>
    public sealed class Tooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public string Text { get; set; }

        /// <summary>A title over <see cref="Text"/>, instead of its first line.</summary>
        public string Title { get; set; }

        /// <summary>A picture beside the words: a portrait, a building, an icon.</summary>
        public Sprite Picture { get; set; }

        public static Tooltip Attach(GameObject target, string text)
        {
            Tooltip tip = target.GetComponent<Tooltip>();
            if (tip == null)
            {
                tip = target.AddComponent<Tooltip>();
            }
            tip.Text = text;
            tip.Title = null;
            tip.Picture = null;
            var graphic = target.GetComponent<Graphic>();
            if (graphic != null)
            {
                graphic.raycastTarget = true;
            }
            return tip;
        }

        public static Tooltip Attach(GameObject target, string title, string body, Sprite picture = null)
        {
            Tooltip tip = Attach(target, body);
            tip.Title = title;
            tip.Picture = picture;
            return tip;
        }

        /// <summary>Makes the one box every tooltip is shown in, on the canvas of the interface.</summary>
        public static void Prepare(Transform canvas)
        {
            TooltipBox.Prepare(canvas);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (string.IsNullOrEmpty(Text) && string.IsNullOrEmpty(Title))
            {
                return;
            }
            if (Title != null)
            {
                TooltipBox.Show(this, Title, Text, Picture);
                return;
            }
            int line = Text.IndexOf('\n');
            if (line > 0)
            {
                TooltipBox.Show(this, Text.Substring(0, line), Text.Substring(line + 1), Picture);
            }
            else
            {
                TooltipBox.Show(this, null, Text, Picture);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            TooltipBox.Hide(this);
        }

        private void OnDisable()
        {
            TooltipBox.Hide(this);
        }
    }

    /// <summary>
    /// The box the tooltips show in. It waits a moment under a resting pointer, then fades in beside it: a picture on
    /// its left when there is one, a title in gold, and the words under it, as wide as they need up to a limit. It turns
    /// back at the edges of the screen and stays over every screen opened after it.
    /// </summary>
    public sealed class TooltipBox : MonoBehaviour
    {
        private const float Delay = 0.28f;
        private const float FadeTime = 0.12f;
        private const float MaxWidth = 420f;
        private const float PictureSize = 76f;

        private static TooltipBox instance;

        /// <summary>Development tours only: where the pointer is taken to be, instead of the mouse. Null for the mouse.</summary>
        public static Vector2? Pointer { get; set; }

        private RectTransform box;
        private CanvasGroup group;
        private Image picture;
        private TextMeshProUGUI title;
        private TextMeshProUGUI body;
        private LayoutElement words;
        private object owner;
        private float since;
        private bool open;
        private float closedAt = -10f;

        public static void Prepare(Transform canvas)
        {
            if (instance != null)
            {
                return;
            }
            Image frame = UIKit.Art != null && UIKit.Art.tooltip != null
                ? UIKit.Sprite(canvas, "Tooltip", UIKit.Art.tooltip, Color.white)
                : UIKit.Panel(canvas, "Tooltip");
            frame.raycastTarget = false;
            instance = frame.gameObject.AddComponent<TooltipBox>();
            instance.Build(frame);
        }

        private void Build(Image frame)
        {
            box = (RectTransform)frame.transform;
            box.anchorMin = box.anchorMax = new Vector2(0.5f, 0.5f);
            group = frame.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;
            RectOffset padding = UIKit.Padding(frame, 6f);
            padding.left = Mathf.Max(padding.left, 14);
            padding.right = Mathf.Max(padding.right, 14);
            padding.top = Mathf.Max(padding.top, 10);
            padding.bottom = Mathf.Max(padding.bottom, 12);
            HorizontalLayoutGroup row = UIKit.Layout<HorizontalLayoutGroup>(box, 12f, padding);
            row.childAlignment = TextAnchor.UpperLeft;

            picture = UIKit.PortraitFrame(box, "Picture", null);
            var pictureHolder = (RectTransform)picture.transform.parent.parent;
            UIKit.Fit(pictureHolder, PictureSize, PictureSize);

            RectTransform column = UIKit.Rect(box, "Words");
            VerticalLayoutGroup stack = UIKit.Layout<VerticalLayoutGroup>(column, 2f);
            stack.childForceExpandWidth = true;
            words = column.gameObject.AddComponent<LayoutElement>();
            title = UIKit.Title(column, "Title", "", 22f, UIKit.Gold, TextAlignmentOptions.TopLeft);
            title.characterSpacing = 1f;
            UIKit.Look(title, TextLook.Gold);
            body = UIKit.Label(column, "Body", "", 20f, UIKit.Ink, TextAlignmentOptions.TopLeft);
            var fitter = box.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            group.alpha = 0f;
        }

        /// <summary>Shows words for <paramref name="who"/> after the pause of a resting pointer (at once if already open).</summary>
        public static void Show(object who, string heading, string text, Sprite image = null)
        {
            if (instance == null || who == null)
            {
                return;
            }
            instance.Fill(heading, text, image);
            if (instance.owner != who)
            {
                // Moving from one thing that explains itself straight to the next does not wait again.
                bool moving = instance.open || Time.unscaledTime - instance.closedAt < 0.2f;
                instance.owner = who;
                instance.open = false;
                instance.since = moving ? Time.unscaledTime - Delay : Time.unscaledTime;
            }
        }

        public static void Hide(object who)
        {
            if (instance == null || instance.owner != who || who == null)
            {
                return;
            }
            if (instance.open)
            {
                instance.closedAt = Time.unscaledTime;
            }
            instance.owner = null;
            instance.open = false;
            instance.group.alpha = 0f;
        }

        private void Fill(string heading, string text, Sprite image)
        {
            bool hasTitle = !string.IsNullOrEmpty(heading);
            bool hasBody = !string.IsNullOrEmpty(text);
            title.gameObject.SetActive(hasTitle);
            body.gameObject.SetActive(hasBody);
            title.text = heading ?? "";
            body.text = text ?? "";
            picture.transform.parent.parent.gameObject.SetActive(image != null);
            picture.sprite = image;
            // As wide as the longest line, up to the limit; longer lines wrap.
            float width = 0f;
            if (hasTitle)
            {
                width = Mathf.Max(width, title.GetPreferredValues(title.text).x);
            }
            if (hasBody)
            {
                width = Mathf.Max(width, body.GetPreferredValues(body.text).x);
            }
            words.preferredWidth = Mathf.Clamp(Mathf.Ceil(width) + 2f, 60f, MaxWidth);
        }

        /// <summary>The box stays on while hidden (at no opacity), so it can wait, fade and follow the pointer itself.</summary>
        private void LateUpdate()
        {
            if (owner == null)
            {
                return;
            }
            if (!open)
            {
                if (Time.unscaledTime - since < Delay)
                {
                    return;
                }
                open = true;
                group.alpha = 0f;
                LayoutRebuilder.ForceRebuildLayoutImmediate(box);
            }
            group.alpha = Mathf.MoveTowards(group.alpha, 1f, Time.unscaledDeltaTime / FadeTime);
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
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas, Pointer ?? (Vector2)Input.mousePosition, null, out Vector2 point);
            // Below and to the right of the pointer, turning back at the edges of the screen.
            Vector2 size = box.rect.size;
            Rect area = canvas.rect;
            bool left = point.x + 22f + size.x > area.xMax;
            bool up = point.y - 26f - size.y < area.yMin;
            box.pivot = new Vector2(left ? 1f : 0f, up ? 0f : 1f);
            box.anchoredPosition = new Vector2(point.x + (left ? -14f : 22f), point.y + (up ? 18f : -26f));
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }
    }
}
