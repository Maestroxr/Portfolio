using Gamebox;
using UnityEngine;

namespace Portfolio.Asteroids
{
    /// <summary>A level of the Asteroids campaign: the spawn profile played on that level.</summary>
    [CreateAssetMenu(fileName = "AsteroidsLevel", menuName = "Asteroids/Level", order = 2)]
    public class AsteroidsLevel : GameLevel
    {
        [field: SerializeField]
        public AsteroidSettings Settings { get; private set; }

        public override bool IsLevelValid(out string message)
        {
            if (Settings == null)
            {
                message = "The level has no asteroid settings.";
                return false;
            }
            return Settings.AreSettingsValid(out message);
        }
    }
}
