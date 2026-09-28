using Gamebox;
using TMPro;
using UnityEngine;

namespace Portfolio.Asteroids
{
    /// <summary>A text with a vertical colour gradient whose two colours come from the theme by key (the logo).</summary>
    [RequireComponent(typeof(TMP_Text))]
    public class ThemedTextGradient : ThemedElement
    {
        [Tooltip("The key of the colour at the top of the letters.")]
        [SerializeField] private string top;
        [Tooltip("The key of the colour at the bottom of the letters.")]
        [SerializeField] private string bottom;

        public string TopKey
        {
            get => top;
            set => top = value;
        }

        public string BottomKey
        {
            get => bottom;
            set => bottom = value;
        }

        protected override void Apply(GameTheme theme)
        {
            if (!theme.TryColor(top, out Color upper) || !theme.TryColor(bottom, out Color lower))
            {
                return;
            }
            var text = GetComponent<TMP_Text>();
            text.enableVertexGradient = true;
            text.colorGradient = new VertexGradient(upper, upper, lower, lower);
        }
    }
}
