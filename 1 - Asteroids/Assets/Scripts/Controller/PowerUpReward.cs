using UnityEngine;

namespace Portfolio.Asteroids
{
    /// <summary>A timed power-up: overdrive, tractor magnet, chrono field or wing drones.</summary>
    public class PowerUpReward : Reward
    {
        [field: SerializeField]
        public PowerUpType PowerUp { get; private set; }

        public override string Title => PowerUps.Title(PowerUp);


        public override void Award(AsteroidsPlayer player)
        {
            player.ActivatePowerUp(PowerUp, PowerUps.Duration(PowerUp));
        }
    }
}
