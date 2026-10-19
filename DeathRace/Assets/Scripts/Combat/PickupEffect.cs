using System.Collections.Generic;

namespace Portfolio.DeathRace
{
    /// <summary>
    /// What one pickup gives a car: cash for the race, health, rounds or a nitro charge. Worked out from the car and the
    /// race apart from either, so the field, an online claim and the tests agree.
    /// </summary>
    public struct PickupEffect
    {
        public PickupKind Kind;
        public int Cash;
        public float Repair;
        public int Ammo;
        public int Nitro;

        /// <summary>What a <paramref name="kind"/> pickup gives a car of <paramref name="maxHealth"/> carrying <paramref name="ammoCapacity"/> rounds.</summary>
        public static PickupEffect Of(PickupKind kind, int cashPerPickup, float maxHealth, int ammoCapacity)
        {
            var effect = new PickupEffect { Kind = kind };
            switch (kind)
            {
                case PickupKind.Cash:
                    effect.Cash = System.Math.Max(0, cashPerPickup);
                    break;
                case PickupKind.Repair:
                    effect.Repair = CombatRules.RepairPickup(maxHealth);
                    break;
                case PickupKind.Ammo:
                    effect.Ammo = CombatRules.AmmoPickup(ammoCapacity);
                    break;
                default:
                    effect.Nitro = CombatRules.NitroPerPickup;
                    break;
            }
            return effect;
        }

        /// <summary>The number the car's event carries (the cash, the health put back, the rounds, the charges).</summary>
        public int Amount
        {
            get
            {
                switch (Kind)
                {
                    case PickupKind.Cash: return Cash;
                    case PickupKind.Repair: return (int)System.Math.Round(Repair);
                    case PickupKind.Ammo: return Ammo;
                    default: return Nitro;
                }
            }
        }

        /// <summary>
        /// The kinds of a cluster of <paramref name="count"/> pickups: mostly cash, then ammo, repairs and nitro, never a
        /// cluster of one kind only (a choice to make when there are two or more). Seeded, so a race lays the same field.
        /// </summary>
        public static List<PickupKind> KindsFor(int count, System.Random random)
        {
            var kinds = new List<PickupKind>(count);
            for (int i = 0; i < count; i++)
            {
                kinds.Add(Draw(random));
            }
            if (count >= 2 && kinds.TrueForAll(k => k == kinds[0]))
            {
                PickupKind other;
                do
                {
                    other = Draw(random);
                }
                while (other == kinds[0]);
                kinds[random.Next(count)] = other;
            }
            return kinds;
        }

        private static PickupKind Draw(System.Random random)
        {
            double roll = random.NextDouble();
            if (roll < 0.4)
            {
                return PickupKind.Cash;
            }
            if (roll < 0.62)
            {
                return PickupKind.Ammo;
            }
            return roll < 0.82 ? PickupKind.Repair : PickupKind.Nitro;
        }
    }
}
