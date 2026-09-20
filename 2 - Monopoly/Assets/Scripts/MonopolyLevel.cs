using Gamebox;
using UnityEngine;

namespace Portfolio.Monopoly
{
    /// <summary>
    /// A game mode of the campaign: the classic game or one of the variants (speed die, quick deal, house rules, a time
    /// limit), with the settings it is played with. A mode without settings of its own plays the custom rules of the
    /// settings panel.
    /// </summary>
    [CreateAssetMenu(fileName = "MonopolyLevel", menuName = "Monopoly/Level", order = 2)]
    public class MonopolyLevel : GameLevel
    {
        [field: SerializeField]
        public MonopolySettings Settings { get; private set; }

        [field: SerializeField]
        public string Tagline { get; private set; }

        [field: SerializeField, TextArea(3, 8)]
        public string Description { get; private set; }

        /// <summary>A Font Awesome icon (see <see cref="Icons"/>) for the mode's card.</summary>
        [field: SerializeField]
        public string Icon { get; private set; }

        [field: SerializeField]
        public Color Accent { get; private set; } = Color.red;

        /// <summary>Whether the mode plays the custom rules of the settings panel instead of settings of its own.</summary>
        public bool UsesCustomRules => Settings == null;

        public override bool IsLevelValid(out string message)
        {
            if (UsesCustomRules)
            {
                message = "OK";
                return true;
            }
            return Settings.AreSettingsValid(out message);
        }

#if UNITY_EDITOR
        /// <summary>Editor only: the content builder fills the mode in.</summary>
        public void Configure(MonopolySettings settings, string tagline, string description, string icon, Color accent)
        {
            Settings = settings;
            Tagline = tagline;
            Description = description;
            Icon = icon;
            Accent = accent;
        }
#endif
    }
}
