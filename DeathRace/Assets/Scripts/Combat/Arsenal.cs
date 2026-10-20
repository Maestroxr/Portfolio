using UnityEngine;

namespace Portfolio.DeathRace
{
    /// <summary>
    /// One gun of the bonnet: how fast it fires, how hard a round hits and how far it reaches, how much it strays, how
    /// many rounds it carries against the machine gun, and how its rounds travel (in a line at once, as a flame in a
    /// cone, or as a bolt that flies and bursts). <see cref="Guns"/> holds the four.
    /// </summary>
    public sealed class GunSpec
    {
        public GunKind Kind;

        /// <summary>The shop's id of the gun (and its icon's name is <see cref="Icon"/>).</summary>
        public string Id;

        public string Name;
        public string Blurb;
        public string Icon;

        public float RoundsPerSecond;

        /// <summary>What a round takes off before the target's armour (a flame: one puff).</summary>
        public float Damage;

        public float Range;

        /// <summary>Degrees either side of straight ahead a round may stray.</summary>
        public float Spread;

        /// <summary>The rounds the gun carries, as a share of the car's machine gun rounds.</summary>
        public float AmmoFactor = 1f;

        /// <summary>Metres per second of a round that flies (a plasma bolt); 0 for a round that hits at once.</summary>
        public float ProjectileSpeed;

        /// <summary>The whole width of the cone a flame reaches, in degrees (0: not a flame).</summary>
        public float Cone;

        /// <summary>The radius of the burst where a bolt lands (0: none).</summary>
        public float Splash;

        /// <summary>The garage's price on a tier 1 car (dearer for better cars, as the other fittings).</summary>
        public int PriceBase;

        public bool IsFlame => Cone > 0f;

        public bool IsProjectile => ProjectileSpeed > 0f;

        /// <summary>Damage a second with the trigger held, before upgrades and armour.</summary>
        public float DamagePerSecond => RoundsPerSecond * Damage;
    }

    /// <summary>The guns: the machine gun every car has, the minigun, the flamethrower and the plasma cannon.</summary>
    public static class Guns
    {
        public static readonly GunSpec[] All =
        {
            new GunSpec
            {
                Kind = GunKind.MachineGun, Id = "gun.machinegun", Icon = "MachineGun", Name = "Machine Gun",
                Blurb = "The gun every car comes with: steady, accurate and good for a long way.",
                RoundsPerSecond = CombatRules.GunRoundsPerSecond, Damage = CombatRules.GunDamage, Range = CombatRules.GunRange,
                Spread = CombatRules.GunSpread, AmmoFactor = 1f, PriceBase = 0
            },
            new GunSpec
            {
                Kind = GunKind.Minigun, Id = "gun.minigun", Icon = "Minigun", Name = "Minigun",
                Blurb = "Six spinning barrels: a storm of lighter rounds that eats through a car and through the ammunition.",
                RoundsPerSecond = CombatRules.MinigunRoundsPerSecond, Damage = CombatRules.MinigunDamage, Range = CombatRules.MinigunRange,
                Spread = CombatRules.MinigunSpread, AmmoFactor = CombatRules.MinigunAmmoFactor, PriceBase = 2500
            },
            new GunSpec
            {
                Kind = GunKind.Flamethrower, Id = "gun.flamethrower", Icon = "Flamethrower", Name = "Flamethrower",
                Blurb = "A jet of burning fuel: short reach, no aiming needed, and whatever it touches keeps burning.",
                RoundsPerSecond = CombatRules.FlameRoundsPerSecond, Damage = CombatRules.FlameDamage, Range = CombatRules.FlameRange,
                Spread = 0f, AmmoFactor = CombatRules.FlameAmmoFactor, Cone = CombatRules.FlameCone, PriceBase = 3500
            },
            new GunSpec
            {
                Kind = GunKind.Plasma, Id = "gun.plasma", Icon = "Plasma", Name = "Plasma Cannon",
                Blurb = "Slow, heavy bolts of plasma that fly true and burst on whatever they hit.",
                RoundsPerSecond = CombatRules.PlasmaRoundsPerSecond, Damage = CombatRules.PlasmaDamage, Range = CombatRules.PlasmaRange,
                Spread = 0f, AmmoFactor = CombatRules.PlasmaAmmoFactor, ProjectileSpeed = CombatRules.PlasmaSpeed, Splash = CombatRules.PlasmaSplash,
                PriceBase = 6000
            }
        };

        public static int Count => All.Length;

        public static GunSpec Of(GunKind kind)
        {
            return All[Mathf.Clamp((int)kind, 0, All.Length - 1)];
        }
    }

    /// <summary>One special weapon: the shop's id (also the career's inventory id), its name, price and how many a car carries.</summary>
    public sealed class SpecialSpec
    {
        public SpecialKind Kind;
        public string Id;
        public string Name;
        public string Blurb;
        public string Icon;
        public int Price;
        public int Max;
    }

    /// <summary>The special weapons: oil slicks, EMP blasts and mortar shells.</summary>
    public static class Specials
    {
        public static readonly SpecialSpec[] All =
        {
            new SpecialSpec
            {
                Kind = SpecialKind.Oil, Id = "oil", Icon = "Oil", Name = "Oil Slick", Price = 120, Max = 6,
                Blurb = "A barrel of sump oil poured behind you: whoever drives through it loses the road for a moment."
            },
            new SpecialSpec
            {
                Kind = SpecialKind.Emp, Id = "emp", Icon = "Emp", Name = "EMP Blast", Price = 300, Max = 4,
                Blurb = "An electric pulse that jams every car near you: no guns, no nitro and a coughing engine for a few seconds."
            },
            new SpecialSpec
            {
                Kind = SpecialKind.Mortar, Id = "mortar", Icon = "Mortar", Name = "Mortar", Price = 280, Max = 5,
                Blurb = "A shell lobbed over the cars ahead that bursts where it lands. Best fired into a crowded corner."
            }
        };

        public static int Count => All.Length;

        public static SpecialSpec Of(SpecialKind kind)
        {
            return All[Mathf.Clamp((int)kind, 0, All.Length - 1)];
        }

        /// <summary>The special of a shop id, or null.</summary>
        public static SpecialSpec Find(string id)
        {
            foreach (SpecialSpec spec in All)
            {
                if (spec.Id == id)
                {
                    return spec;
                }
            }
            return null;
        }
    }

    /// <summary>One fitting of the workshop: the shop's id (also its icon's name), its name and its price on a tier 1 car.</summary>
    public sealed class EquipmentSpec
    {
        public EquipmentKind Kind;
        public string Id;
        public string Name;
        public string Blurb;
        public string Icon;
        public int PriceBase;
    }

    /// <summary>The fittings: the spiked bumper and armour-piercing rounds of the first game, and five more.</summary>
    public static class Equipment
    {
        public static readonly EquipmentSpec[] All =
        {
            new EquipmentSpec
            {
                Kind = EquipmentKind.Spikes, Id = "spikes", Icon = "Spikes", Name = "Spiked Bumper", PriceBase = 900,
                Blurb = "Steel spikes on the nose: rams hurt the other car more and yours less."
            },
            new EquipmentSpec
            {
                Kind = EquipmentKind.Piercing, Id = "piercing", Icon = "Piercing", Name = "Armor-Piercing Rounds", PriceBase = 1400,
                Blurb = "The gun hits half as hard again."
            },
            new EquipmentSpec
            {
                Kind = EquipmentKind.Cage, Id = "cage", Icon = "Cage", Name = "Roll Cage", PriceBase = 1100,
                Blurb = "A welded cage of steel tube: crashes into cars and barriers hurt far less."
            },
            new EquipmentSpec
            {
                Kind = EquipmentKind.Shield, Id = "shield", Icon = "Shield", Name = "Blast Shielding", PriceBase = 1300,
                Blurb = "Plate under the floor and round the tank: mines, rockets, shells and explosions hurt less."
            },
            new EquipmentSpec
            {
                Kind = EquipmentKind.Jammer, Id = "jammer", Icon = "Jammer", Name = "Radar Jammer", PriceBase = 1600,
                Blurb = "A box of noise on the roof: homing rockets never lock on to you."
            },
            new EquipmentSpec
            {
                Kind = EquipmentKind.Welder, Id = "welder", Icon = "Welder", Name = "Field Welder", PriceBase = 2000,
                Blurb = "A robot arm with a torch: the car mends itself slowly while nobody is hitting it."
            },
            new EquipmentSpec
            {
                Kind = EquipmentKind.Magazine, Id = "magazine", Icon = "Magazine", Name = "Extended Magazine", PriceBase = 800,
                Blurb = "Bigger ammunition boxes: the gun carries half as many rounds again."
            }
        };

        public static int Count => All.Length;

        public static EquipmentSpec Of(EquipmentKind kind)
        {
            return All[Mathf.Clamp((int)kind, 0, All.Length - 1)];
        }

        public static EquipmentSpec Find(string id)
        {
            foreach (EquipmentSpec spec in All)
            {
                if (spec.Id == id)
                {
                    return spec;
                }
            }
            return null;
        }
    }
}
