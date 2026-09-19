using UnityEngine;

namespace Portfolio.Asteroids
{
    /// <summary>A weapon crate: switches to its weapon and raises the weapon level by one.</summary>
    public class WeaponReward : Reward
    {
        [field: SerializeField]
        public WeaponType Weapon { get; private set; }

        public override string Title => WeaponRules.Title(Weapon);


        public override void Award(AsteroidsPlayer player)
        {
            player.Weapons.Upgrade(Weapon);
        }
    }
}
