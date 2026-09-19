using System.Collections.Generic;
using UnityEngine;

namespace Portfolio.MemoryCards
{
    /// <summary>How the ambient shapes of a world's backdrop move.</summary>
    public enum AmbientMotion
    {
        /// <summary>Drift slowly sideways (clouds).</summary>
        Drift,
        /// <summary>Fall and sway (leaves, snow).</summary>
        Fall,
        /// <summary>Rise and wobble (bubbles, balloons).</summary>
        Rise
    }


    /// <summary>
    /// The look of one world of the campaign: its colours, card back, mascot, backdrop and the animals dealt there.
    /// </summary>
    [CreateAssetMenu(fileName = "CardWorld", menuName = "Memory Cards/World", order = 4)]
    public class CardWorld : ScriptableObject
    {
        public string displayName = "World";
        [TextArea] public string tagline;

        [Header("Colours")]
        public Color skyTop = new Color(0.45f, 0.78f, 1f);
        public Color skyBottom = new Color(0.72f, 0.93f, 0.6f);
        [Tooltip("Buttons, highlights and the level cards of the world.")]
        public Color accent = new Color(0.98f, 0.62f, 0.2f);
        public Color accentDark = new Color(0.78f, 0.38f, 0.1f);
        [Tooltip("Colour of the card backs; the pattern is baked into the card back sprite.")]
        public Color cardColor = new Color(0.3f, 0.7f, 0.35f);

        [Header("Sprites")]
        public Sprite cardBack;
        [Tooltip("The card back without its medallion, for the level cards of the level select.")]
        public Sprite levelCardBack;
        public Sprite mascot;
        [Tooltip("Shape floating in the backdrop.")]
        public Sprite ambient;
        public Color ambientColor = new Color(1f, 1f, 1f, 0.6f);
        public AmbientMotion motion = AmbientMotion.Drift;

        [Header("Deck")]
        [Tooltip("Animals dealt on the world's levels.")]
        public List<Sprite> animals = new List<Sprite>();
    }
}
