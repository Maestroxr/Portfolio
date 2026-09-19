using UnityEngine;

namespace Portfolio.EndlessRunner
{
    /// <summary>A floating power-up.</summary>
    public class PowerUpPickup : Collidable
    {
        [SerializeField] internal PowerUpType type;

        public PowerUpType Type => type;

        protected override void OnTouched(RunnerGameManager manager, RunnerPlayer player)
        {
            manager.CollectPowerUp(this);
        }
    }
}
