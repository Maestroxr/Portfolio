using UnityEngine;

namespace Portfolio.Asteroids
{
    /// <summary>
    /// A pickup of the strike mode, lying on the ground: money (paid at once through the manager, with a "$" popup), an
    /// item for the pilot's working loadout (a weapon adds a copy, the first special is selected; over the cap it pays its
    /// resale instead) or energy. It moves with the ground and never runs out (see <see cref="Reward"/>).
    /// </summary>
    public class StrikeReward : Reward
    {
        [Header("Strike pickup")]
        [Tooltip("The item it gives; Machine Gun = none.")]
        [SerializeField] internal StrikeItem item = StrikeItem.MachineGun;
        [Tooltip("The money it pays.")]
        [SerializeField] internal int money;
        [Tooltip("The energy it gives.")]
        [SerializeField] internal int energy;

        public StrikeItem Item => item;

        public int Money => money;

        public int Energy => energy;

        /// <summary>Whether it only pays money (a credit orb or a money pickup).</summary>
        public bool IsMoney => money > 0 && item == StrikeItem.MachineGun && energy <= 0;


        /// <summary>
        /// Pays its money through <see cref="AsteroidsGameManager.AddMoney"/> (the mission's score), puts its item into the
        /// pilot's working loadout (<see cref="StrikeLoadout.Collect"/>; what is over the cap pays its resale, also as
        /// mission money) and gives energy as an energy module does. A ship outside strike (no gunnery) only gets the money.
        /// </summary>
        public override void Award(AsteroidsPlayer player)
        {
            if (player == null)
            {
                return;
            }
            var manager = player.GameManager as AsteroidsGameManager;
            int paid = money;
            StrikeLoadout loadout = player.Strike != null ? player.Strike.Loadout : null;
            if (loadout != null)
            {
                if (item != StrikeItem.MachineGun)
                {
                    paid += loadout.Collect(item);
                }
                if (energy > 0 && item != StrikeItem.EnergyModule)
                {
                    paid += loadout.Collect(StrikeItem.EnergyModule);
                }
            }
            if (paid > 0 && manager != null)
            {
                manager.AddMoney(paid, Position, player);
            }
        }


        /// <summary>Money rings the till; anything else is an item pickup.</summary>
        protected override void PlayPickupSound()
        {
            AsteroidsAudio sounds = Field != null ? Field.Sounds : null;
            if (sounds == null)
            {
                return;
            }
            if (IsMoney)
            {
                sounds.Cash();
            }
            else
            {
                sounds.ItemPickup();
            }
        }
    }
}
