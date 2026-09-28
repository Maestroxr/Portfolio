using Gamebox;
using TMPro;
using UnityEngine;

namespace Portfolio.MemoryCards
{
    /// <summary>
    /// A text in the font of its role and one of the theme's text materials (<see cref="MemoryCardsTheme.TextStyles"/>:
    /// "text.outline", "text.shadow", "text.title"), in a colour of the palette by key ("palette.ink"). Like the base
    /// <see cref="ThemedText"/> it draws itself when it wakes and again on every theme change; unlike it, the material
    /// is picked from the theme, so labels keep their outlines and shadows in every look. A material that does not
    /// belong to the font's atlas is skipped for the font's own.
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    public class ThemedLabel : ThemedElement
    {
        [SerializeField] private TextRole role = TextRole.Body;
        [Tooltip("The key of the material in the theme; empty uses the material of the role.")]
        [SerializeField] private string material;
        [Tooltip("The key of the colour in the theme; empty keeps the colour.")]
        [SerializeField] private string color;

        public TextRole Role
        {
            get => role;
            set => role = value;
        }

        public string MaterialKey
        {
            get => material;
            set => material = value;
        }

        public string ColorKey
        {
            get => color;
            set => color = value;
        }

        protected override void Apply(GameTheme theme)
        {
            var text = GetComponent<TMP_Text>();
            TMP_FontAsset font = theme.Fonts.Font(role);
            Material found = theme.MaterialOf(material);
            if (font != null)
            {
                text.font = font;
                bool fits = found != null && found.mainTexture == font.atlasTexture;
                text.fontSharedMaterial = fits ? found : theme.Fonts.MaterialOf(role);
            }
            else if (found != null)
            {
                text.fontSharedMaterial = found;
            }
            if (theme.TryColor(color, out Color tint))
            {
                text.color = tint;
            }
        }
    }
}
