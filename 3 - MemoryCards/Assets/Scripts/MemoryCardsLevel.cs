using Gamebox;
using UnityEngine;

namespace Portfolio.MemoryCards
{
    /// <summary>A level of the Memory Cards campaign: the deck settings played on it.</summary>
    [CreateAssetMenu(fileName = "MemoryCardsLevel", menuName = "Memory Cards/Level", order = 2)]
    public class MemoryCardsLevel : GameLevel
    {
        [field: SerializeField]
        public MemoryCardsSettings Settings { get; private set; }

        public override bool IsLevelValid(out string message)
        {
            if (Settings == null)
            {
                message = "The level has no card settings.";
                return false;
            }
            return Settings.AreSettingsValid(out message);
        }
    }
}
