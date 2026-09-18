using Gamebox;
using UnityEngine;

namespace Portfolio.EndlessRunner
{
    /// <summary>A level of the Endless Runner campaign: the run settings played on it.</summary>
    [CreateAssetMenu(fileName = "RunnerLevel", menuName = "Endless Runner/Level", order = 2)]
    public class RunnerLevel : GameLevel
    {
        [field: SerializeField]
        public RunnerSettings Settings { get; private set; }

        public override bool IsLevelValid(out string message)
        {
            if (Settings == null)
            {
                message = "The level has no runner settings.";
                return false;
            }
            return Settings.AreSettingsValid(out message);
        }
    }
}
