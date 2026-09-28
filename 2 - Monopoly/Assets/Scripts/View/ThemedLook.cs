using Gamebox;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Portfolio.Monopoly
{
    /// <summary>
    /// A part of the interface or a printed text of the board that takes its look from the active
    /// <see cref="MonopolyTheme"/>: the sprite of an image, the colour of a graphic (as an expression of the theme's
    /// palette, see <see cref="MonopolyTheme.TryResolveColor"/>), the font and material of a text, the words of a text,
    /// the colour of an outline effect. The builders put it on what they make (<c>MonopolyThemeTagger</c> reads the
    /// classic look back into keys), and it redraws whenever the theme changes. Empty keys keep what the object has.
    /// A part draws each theme once: what the game's code paints on it afterwards (the colour of a seat, the band of a
    /// title deed, the badge of a token) stays when the part is hidden and shown again, and copies of a drawn part
    /// (the rows and lines the interface makes from templates) keep it too. <see cref="MonopolyUI"/> draws the hidden
    /// parts of the interface up front, before the code paints on them.
    /// </summary>
    public class ThemedLook : ThemedElement
    {
        [Tooltip("The theme this part was drawn with last; copied along with the part, so copies are not drawn again.")]
        [SerializeField, HideInInspector] private GameTheme drawnWith;

        [Tooltip("The key of the sprite in the theme (ui.rounded, tokens.2.badge); empty keeps the sprite.")]
        [SerializeField] private string sprite;
        [Tooltip("A colour expression: a colour key, then *f for a shade, ~t for a tint towards the paper, @a for an alpha.")]
        [SerializeField] private string color;
        [Tooltip("The key of the font (bodyFont, heavyFont, iconFont).")]
        [SerializeField] private string font;
        [Tooltip("The key of a material of the font (titleMaterial, textShadowMaterial); empty uses the font's own.")]
        [SerializeField] private string material;
        [Tooltip("The key of the words of the theme the text shows (edition).")]
        [SerializeField] private string words;
        [Tooltip("A colour expression of the outline or shadow effect on the graphic.")]
        [SerializeField] private string effect;

        public string SpriteKey
        {
            get => sprite;
            set => sprite = value;
        }

        public string ColorExpression
        {
            get => color;
            set => color = value;
        }

        public string FontKey
        {
            get => font;
            set => font = value;
        }

        public string MaterialKey
        {
            get => material;
            set => material = value;
        }

        public string WordsKey
        {
            get => words;
            set => words = value;
        }

        public string EffectExpression
        {
            get => effect;
            set => effect = value;
        }

        /// <summary>Whether the part has anything to take from a theme.</summary>
        public bool IsEmpty => string.IsNullOrEmpty(sprite) && string.IsNullOrEmpty(color) && string.IsNullOrEmpty(font)
            && string.IsNullOrEmpty(material) && string.IsNullOrEmpty(words) && string.IsNullOrEmpty(effect);

        /// <summary>Draws the part from the active theme even when it was drawn with it already.</summary>
        public void Redraw()
        {
            drawnWith = null;
            Refresh();
        }

        protected override void Apply(GameTheme theme)
        {
            var look = theme as MonopolyTheme;
            if (look == null || look == drawnWith)
            {
                return;
            }
            drawnWith = look;
            if (TryGetComponent(out TMP_Text text))
            {
                ApplyText(look, text);
            }
            else if (TryGetComponent(out Image image))
            {
                if (!string.IsNullOrEmpty(sprite))
                {
                    Sprite found = look.SpriteOf(sprite);
                    if (found != null)
                    {
                        image.sprite = found;
                    }
                }
                if (look.TryResolveColor(color, out Color tint))
                {
                    image.color = tint;
                }
            }
            else if (TryGetComponent(out Graphic graphic) && look.TryResolveColor(color, out Color plain))
            {
                graphic.color = plain;
            }
            if (!string.IsNullOrEmpty(effect) && TryGetComponent(out Shadow shadow) && look.TryResolveColor(effect, out Color effectColor))
            {
                shadow.effectColor = effectColor;
            }
        }

        private void ApplyText(MonopolyTheme look, TMP_Text text)
        {
            if (!string.IsNullOrEmpty(font))
            {
                TMP_FontAsset found = look.FontOf(font);
                if (found != null)
                {
                    text.font = found;
                    Material fontMaterial = string.IsNullOrEmpty(material) ? null : look.MaterialOf(material);
                    text.fontSharedMaterial = fontMaterial != null ? fontMaterial : found.material;
                }
            }
            if (look.TryResolveColor(color, out Color tint))
            {
                text.color = tint;
            }
            if (!string.IsNullOrEmpty(words))
            {
                string line = look.WordOf(words);
                if (line != null)
                {
                    text.text = line;
                }
            }
        }
    }
}
