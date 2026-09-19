using System;
using UnityEngine;

namespace Portfolio.Asteroids
{
    /// <summary>
    /// The ship's gun: which weapon it fires and at which level. Every weapon crate switches to its weapon and adds a
    /// level (up to <see cref="WeaponRules.MaxLevel"/>); losing a ship costs one level.
    /// </summary>
    public class ShipWeapons
    {
        public WeaponType Type { get; private set; } = WeaponType.Blaster;

        public int Level { get; private set; } = 1;

        public bool IsMaxed => Level >= WeaponRules.MaxLevel;

        /// <summary>The weapon or its level changed.</summary>
        public event Action Changed;

        public void Reset()
        {
            Set(WeaponType.Blaster, 1);
        }

        public void Set(WeaponType type, int level)
        {
            Type = type;
            Level = Mathf.Clamp(level, 1, WeaponRules.MaxLevel);
            Changed?.Invoke();
        }

        /// <summary>A weapon crate: switch to <paramref name="type"/> and gain a level.</summary>
        public void Upgrade(WeaponType type)
        {
            Set(type, Level + 1);
        }

        /// <summary>Loses a level (never below the first).</summary>
        public void Downgrade()
        {
            Set(Type, Level - 1);
        }
    }
}
