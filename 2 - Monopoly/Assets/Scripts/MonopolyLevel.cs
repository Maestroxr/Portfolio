using Gamebox;
using UnityEngine;

namespace Portfolio.Monopoly
{
    /// <summary>A level of the Monopoly campaign: the board settings played on it.</summary>
    [CreateAssetMenu(fileName = "MonopolyLevel", menuName = "Monopoly/Level", order = 2)]
    public class MonopolyLevel : GameLevel
    {
        [field: SerializeField]
        public MonopolySettings Settings { get; private set; }

        public override bool IsLevelValid(out string message)
        {
            if (Settings == null)
            {
                message = "The level has no Monopoly settings.";
                return false;
            }
            return Settings.AreSettingsValid(out message);
        }
    }
}
