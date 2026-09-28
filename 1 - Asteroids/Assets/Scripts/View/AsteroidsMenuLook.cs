using Gamebox;
using Gamebox.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Portfolio.Asteroids
{
    /// <summary>
    /// The pause menu in the game's own look, on top of what the theme's <see cref="MenuSkin"/> gives the shared menu:
    /// every button in the theme's button sprite with its own colour of the palette (resume green, restart orange,
    /// abandon red...), a colour tint for hover and press, the arrows of the theme choice in blue, the full screen
    /// panel sliced in the panel colour, and the words of the settings rows in the plain body font (the input boxes are
    /// the skin's). It applies the skin itself first, so the outcome does not depend on whether the
    /// <see cref="ThemedMenu"/> beside it ran before (it also runs after it: a later execution order), when the scene
    /// starts and again on every theme change.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public class AsteroidsMenuLook : ThemedElement
    {
        /// <summary>The buttons of the shared menu and the palette colour of each.</summary>
        private static readonly (string name, string color)[] Tints =
        {
            ("ReturnToGame", "green"), ("StartNewGame", "orange"), ("SaveGame", "blue"), ("LoadGame", "blue"), ("Game Settings", "blue"),
            ("ExitGame", "red"), ("Custom Settings", "blue"), ("SaveSettingsButton", "green"), ("LoadSettingsButton", "blue"), ("BackToMenu", "orange")
        };

        protected override void Apply(GameTheme theme)
        {
            if (!(theme is AsteroidsTheme look))
            {
                return;
            }
            look.Menu?.Apply(transform, look.Fonts);
            // The skin stretches its backdrop; the game's is its panel, sliced, in the panel colour.
            Transform backdrop = MenuSkin.Find(transform, "MenuPanel");
            if (backdrop != null && backdrop.TryGetComponent(out Image panel))
            {
                Swap(panel, look.Interface.panel);
                panel.color = look.ColorOf("palette.panel", panel.color);
            }
            Sprite sprite = look.Interface.button;
            foreach ((string name, string color) in Tints)
            {
                Transform target = MenuSkin.Find(transform, name);
                if (target != null)
                {
                    Style(target, sprite, look.ColorOf("palette." + color, Color.white));
                }
            }
            Color blue = look.ColorOf("palette.blue", Color.white);
            foreach (ChoiceSelector choice in GetComponentsInChildren<ChoiceSelector>(true))
            {
                foreach (Button arrow in choice.GetComponentsInChildren<Button>(true))
                {
                    Style(arrow.transform, sprite, blue);
                }
            }
            Words(look);
        }


        /// <summary>
        /// The words the skin cannot reach: the labels of buttons nested below their own child, in the body font, and the
        /// rows of the settings panel (their labels in the soft colour, their values and fields in the plain body font). The
        /// skin looks for the rows under a "Rows" child of the input panel, which this menu does not have.
        /// </summary>
        private void Words(AsteroidsTheme look)
        {
            foreach (Button button in GetComponentsInChildren<Button>(true))
            {
                if (button.name == "PauseButton")
                {
                    continue;
                }
                foreach (TMP_Text label in button.GetComponentsInChildren<TMP_Text>(true))
                {
                    if (label.GetComponentInParent<Selectable>(true) == button)
                    {
                        look.Fonts.Apply(label, TextRole.Body);
                    }
                }
            }
            Transform rows = MenuSkin.Find(transform, "InputPanel");
            if (rows == null)
            {
                return;
            }
            TMP_FontAsset plain = look.FontOf("plain");
            Color soft = look.ColorOf("palette.soft", Color.white);
            foreach (TMP_Text text in rows.GetComponentsInChildren<TMP_Text>(true))
            {
                if (text.GetComponentInParent<Button>(true) != null)
                {
                    continue;
                }
                if (plain != null)
                {
                    text.font = plain;
                    text.fontSharedMaterial = plain.material;
                }
                if (text.name == "Label" && text.transform.parent != null && text.transform.parent.parent == rows)
                {
                    text.color = soft;
                }
            }
        }


        /// <summary>A button in <paramref name="sprite"/> tinted <paramref name="color"/>, lit up on hover and darkened when pressed.</summary>
        private static void Style(Transform target, Sprite sprite, Color color)
        {
            if (target.TryGetComponent(out Image image))
            {
                Swap(image, sprite);
                image.color = color;
            }
            if (target.TryGetComponent(out Button button))
            {
                button.transition = Selectable.Transition.ColorTint;
                button.colors = AsteroidsUI.ButtonColors;
            }
        }


        private static void Swap(Image image, Sprite sprite)
        {
            if (sprite != null)
            {
                image.sprite = sprite;
                image.type = Image.Type.Sliced;
            }
        }
    }
}
