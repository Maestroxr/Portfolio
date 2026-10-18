using System.Collections.Generic;
using Gamebox;
using NUnit.Framework;
using UnityEngine;

namespace Portfolio.Asteroids.Tests
{
    /// <summary>
    /// The armory's rules: the four new field weapons at every level and their tricks (chains, pierce, shrapnel, growth),
    /// the two power-ups, the twelve new strike items in the Supply Room's catalogue (order, groups, prices, caps, pickups),
    /// the special weapons cycling across the old and the new ones, the equipment's numbers, a pilot's round trip through
    /// the progress (and one saved before the new items existed), and the Supply Room's two pages.
    /// </summary>
    public class ArmoryTest
    {
        private static readonly WeaponType[] NewWeapons = { WeaponType.Arc, WeaponType.Rail, WeaponType.Flak, WeaponType.Wave };

        private static readonly StrikeItem[] NewSpecials =
        {
            StrikeItem.FlakCannon, StrikeItem.ClusterBombs, StrikeItem.Napalm, StrikeItem.SwarmRockets, StrikeItem.ArcLightning, StrikeItem.RailGun
        };

        private static readonly StrikeItem[] NewGear =
        {
            StrikeItem.ArmorPlating, StrikeItem.RepairDrone, StrikeItem.Afterburner, StrikeItem.SalvageMagnet, StrikeItem.WingmanDrones, StrikeItem.RepairKit
        };


        // ------------------------------------------------------------------ field weapons

        [Test]
        public void EveryWeaponHasNumbersAndAVolleyAtEveryLevel()
        {
            var barrels = new List<Barrel>();
            var titles = new HashSet<string>();
            foreach (WeaponType type in System.Enum.GetValues(typeof(WeaponType)))
            {
                Assert.IsTrue(titles.Add(WeaponRules.Title(type)), $"{type} has a title of its own.");
                Assert.That(WeaponRules.ShortTitle(type), Is.Not.Empty);
                Assert.That(WeaponRules.Speed(type), Is.GreaterThan(0f), type.ToString());
                Assert.That(WeaponRules.Lifetime(type), Is.GreaterThan(0f), type.ToString());
                float lastCooldown = float.MaxValue;
                for (int level = 1; level <= WeaponRules.MaxLevel; level++)
                {
                    float cooldown = WeaponRules.Cooldown(type, level);
                    Assert.That(cooldown, Is.InRange(0.05f, 1f), $"{type} {level}");
                    Assert.That(cooldown, Is.LessThanOrEqualTo(lastCooldown), $"{type} fires no slower at level {level}.");
                    lastCooldown = cooldown;
                    Assert.That(WeaponRules.Damage(type, level), Is.GreaterThan(0f), $"{type} {level}");
                    Assert.That(WeaponRules.Pierce(type, level), Is.GreaterThanOrEqualTo(1), $"{type} {level}");
                    WeaponRules.Volley(type, level, barrels);
                    Assert.That(barrels.Count, Is.InRange(1, 12), $"{type} {level} fires.");
                }
            }
            Assert.That(WeaponRules.Title(WeaponType.Arc), Is.EqualTo("Arc Caster"));
            Assert.That(WeaponRules.Title(WeaponType.Rail), Is.EqualTo("Rail Driver"));
            Assert.That(WeaponRules.Title(WeaponType.Flak), Is.EqualTo("Flak Burster"));
            Assert.That((int)WeaponType.Arc, Is.EqualTo(4), "The pickups other campaigns name: Pickups/WeaponArc.");
            Assert.That((int)WeaponType.Rail, Is.EqualTo(5));
            Assert.That((int)WeaponType.Flak, Is.EqualTo(6));
        }


        [Test]
        public void TheNewWeaponsVolleysGrowWithTheLevel()
        {
            var barrels = new List<Barrel>();
            int[] Counts(WeaponType type)
            {
                var counts = new int[WeaponRules.MaxLevel];
                for (int level = 1; level <= WeaponRules.MaxLevel; level++)
                {
                    WeaponRules.Volley(type, level, barrels);
                    counts[level - 1] = barrels.Count;
                }
                return counts;
            }
            Assert.That(Counts(WeaponType.Arc), Is.EqualTo(new[] { 1, 1, 2, 3 }));
            Assert.That(Counts(WeaponType.Rail), Is.EqualTo(new[] { 1, 1, 1, 2 }));
            Assert.That(Counts(WeaponType.Flak), Is.EqualTo(new[] { 1, 1, 2, 3 }));
            Assert.That(Counts(WeaponType.Wave), Is.EqualTo(new[] { 1, 1, 1, 2 }));
            WeaponRules.Volley(WeaponType.Flak, 4, barrels);
            Assert.That(barrels[0].Angle, Is.LessThan(0f), "A fan from one side...");
            Assert.That(barrels[2].Angle, Is.EqualTo(-barrels[0].Angle).Within(1e-4f), "...to the other.");
            Assert.That(barrels[1].Angle, Is.EqualTo(0f).Within(1e-4f), "The middle shell flies straight.");

            for (int level = 2; level <= WeaponRules.MaxLevel; level++)
            {
                Assert.That(WeaponRules.Chains(WeaponType.Arc, level), Is.GreaterThanOrEqualTo(WeaponRules.Chains(WeaponType.Arc, level - 1)));
                Assert.That(WeaponRules.ChainRange(WeaponType.Arc, level), Is.GreaterThan(WeaponRules.ChainRange(WeaponType.Arc, level - 1)));
                Assert.That(WeaponRules.Pierce(WeaponType.Rail, level), Is.GreaterThan(WeaponRules.Pierce(WeaponType.Rail, level - 1)));
                Assert.That(WeaponRules.Damage(WeaponType.Rail, level), Is.GreaterThan(WeaponRules.Damage(WeaponType.Rail, level - 1)));
                Assert.That(WeaponRules.Fragments(WeaponType.Flak, level), Is.GreaterThanOrEqualTo(WeaponRules.Fragments(WeaponType.Flak, level - 1)));
                Assert.That(WeaponRules.Growth(WeaponType.Wave, level), Is.GreaterThan(WeaponRules.Growth(WeaponType.Wave, level - 1)));
            }
            foreach (WeaponType old in new[] { WeaponType.Blaster, WeaponType.Laser, WeaponType.Scatter, WeaponType.Missiles })
            {
                Assert.That(WeaponRules.Chains(old, 4) + WeaponRules.Fragments(old, 4), Is.Zero, $"{old} keeps to its old ways.");
                Assert.That(WeaponRules.Growth(old, 4), Is.Zero);
            }
            Assert.That(WeaponRules.Inherit(WeaponType.Rail), Is.Zero, "The slug flies true, whatever the ship does.");
            Assert.That(WeaponRules.Inherit(WeaponType.Laser), Is.Zero);
            Assert.That(WeaponRules.Inherit(WeaponType.Blaster), Is.EqualTo(0.35f));
        }


        [Test]
        public void ShipWeaponsTakeTheNewCrates()
        {
            var weapons = new ShipWeapons();
            weapons.Upgrade(WeaponType.Arc);
            Assert.That(weapons.Type, Is.EqualTo(WeaponType.Arc));
            Assert.That(weapons.Level, Is.EqualTo(2));
            for (int i = 0; i < 5; i++)
            {
                weapons.Upgrade(WeaponType.Rail);
            }
            Assert.That(weapons.Type, Is.EqualTo(WeaponType.Rail));
            Assert.That(weapons.Level, Is.EqualTo(WeaponRules.MaxLevel));
            weapons.Downgrade();
            Assert.That(weapons.Level, Is.EqualTo(WeaponRules.MaxLevel - 1));
        }


        [Test]
        public void TheShotsTricksFollowTheirGeometry()
        {
            Assert.That(ArcShot.JumpDamage(4f, 0), Is.EqualTo(4f));
            Assert.That(ArcShot.JumpDamage(4f, 1), Is.EqualTo(4f * ArcShot.JumpFalloff).Within(1e-5f));
            Assert.That(ArcShot.JumpDamage(4f, 3), Is.LessThan(ArcShot.JumpDamage(4f, 2)));

            // A slug from (0, 0) to (0, 10) touches a circle of 0.5 at (0.3, 5) at y 4.6, and passes one at (2, 5) by.
            Assert.That(RailShot.Touch(Vector2.zero, new Vector2(0f, 10f), new Vector2(0.3f, 5f), 0.5f), Is.EqualTo(4.6f).Within(1e-4f));
            Assert.That(RailShot.Touch(Vector2.zero, new Vector2(0f, 10f), new Vector2(2f, 5f), 0.5f), Is.EqualTo(-1f));
            Assert.That(RailShot.Touch(Vector2.zero, new Vector2(0f, 10f), new Vector2(0f, 11f), 0.5f), Is.EqualTo(-1f), "Past the end of the step.");
            Assert.That(RailShot.Touch(Vector2.zero, new Vector2(0f, 10f), new Vector2(0f, 0.2f), 0.5f), Is.Zero, "Already touching at the start.");

            Vector2 up = Vector2.up;
            Vector2 first = BurstShot.FragmentDirection(up, 0, 4, 360f, 0f);
            Vector2 second = BurstShot.FragmentDirection(up, 1, 4, 360f, 0f);
            Assert.That(Vector2.Angle(first, second), Is.EqualTo(90f).Within(0.01f), "A ring spreads evenly.");
            Vector2 left = BurstShot.FragmentDirection(up, 0, 3, 160f, 0f);
            Vector2 middle = BurstShot.FragmentDirection(up, 1, 3, 160f, 0f);
            Assert.That(Vector2.Angle(middle, up), Is.EqualTo(0f).Within(0.01f), "A cone spreads around the course.");
            Assert.That(Vector2.Angle(left, up), Is.EqualTo(80f).Within(0.01f));

            Assert.That(WaveShot.RadiusAt(0.4f, 2f, 0f), Is.EqualTo(0.4f).Within(1e-5f));
            Assert.That(WaveShot.RadiusAt(0.4f, 2f, 1f), Is.EqualTo(2f).Within(1e-5f));
            Assert.That(WaveShot.RadiusAt(0.4f, 2f, 0.5f), Is.GreaterThan(1.2f), "It opens quickly, then slows.");

            Assert.IsTrue(FireShot.Touches(Vector2.zero, 2f, new Vector2(2.2f, 0f), 0.6f));
            Assert.IsFalse(FireShot.Touches(Vector2.zero, 2f, new Vector2(3f, 0f), 0.6f));
        }


        [Test]
        public void ThePowerUpsHaveTheirPlaces()
        {
            Assert.That(PowerUps.Count, Is.EqualTo(System.Enum.GetValues(typeof(PowerUpType)).Length), "One timer per power-up.");
            Assert.That(PowerUps.Title(PowerUpType.Piercing), Is.EqualTo("Phase Rounds"));
            Assert.That(PowerUps.Title(PowerUpType.Aegis), Is.EqualTo("Aegis Barrier"));
            Assert.That(PowerUps.Duration(PowerUpType.Piercing), Is.GreaterThan(0f));
            Assert.That(PowerUps.Duration(PowerUpType.Aegis), Is.GreaterThan(0f));
            Assert.That(PowerUps.ExtraPierce, Is.EqualTo(2));
        }


        // ------------------------------------------------------------------ strike catalogue

        [Test]
        public void TheCatalogueHoldsEveryItemInEnumOrder()
        {
            Assert.That(StrikeArmory.ItemCount, Is.EqualTo((int)StrikeItem.RepairKit + 1));
            Assert.That(StrikeArmory.All.Count, Is.EqualTo(StrikeArmory.ItemCount), "One entry per item value.");
            for (int i = 0; i < StrikeArmory.All.Count; i++)
            {
                StrikeItemInfo info = StrikeArmory.All[i];
                Assert.That((int)info.Item, Is.EqualTo(i), "In enum order: Info(item) is All[(int)item].");
                Assert.That(info.Title, Is.Not.Empty);
                Assert.That(info.Description, Is.Not.Empty);
                Assert.That(info.Resale, Is.EqualTo(info.Price / 2));
            }
            int cheapest = StrikeArmory.Price(StrikeItem.AirMissiles);
            int dearest = StrikeArmory.Price(StrikeItem.TwinLaser);
            foreach (StrikeItem item in NewSpecials)
            {
                StrikeItemInfo info = StrikeArmory.Info(item);
                Assert.That(info.Group, Is.EqualTo(ItemGroup.Special), item.ToString());
                Assert.That(info.Max, Is.EqualTo(20), item.ToString());
                Assert.That(info.Price, Is.InRange(cheapest + 1, dearest - 1), $"{item} costs between the cheapest and the dearest special.");
                Assert.IsFalse(info.NotForSale);
            }
            foreach (StrikeItem item in NewGear)
            {
                StrikeItemInfo info = StrikeArmory.Info(item);
                Assert.That(info.Group, Is.EqualTo(item == StrikeItem.RepairKit ? ItemGroup.Consumable : ItemGroup.Equipment), item.ToString());
                Assert.That(info.Price, Is.GreaterThan(0), item.ToString());
                Assert.IsFalse(info.NotForSale);
            }
            Assert.That(StrikeArmory.Info(StrikeItem.ArmorPlating).Max, Is.EqualTo(3));
            Assert.That(StrikeArmory.Info(StrikeItem.RepairKit).Max, Is.EqualTo(3));
            Assert.That(StrikeArmory.Info(StrikeItem.WingmanDrones).Max, Is.EqualTo(1));
            var pickups = new List<StrikeItem>();
            foreach (StrikeItem item in NewSpecials)
            {
                if (StrikeArmory.Info(item).CanBePickup)
                {
                    pickups.Add(item);
                }
            }
            foreach (StrikeItem item in NewGear)
            {
                if (StrikeArmory.Info(item).CanBePickup)
                {
                    pickups.Add(item);
                }
            }
            Assert.That(pickups, Is.EquivalentTo(new[] { StrikeItem.FlakCannon, StrikeItem.ArmorPlating, StrikeItem.Afterburner, StrikeItem.SalvageMagnet, StrikeItem.RepairKit }),
                "What a level may place as a pickup.");
            Assert.That(StrikeArmory.ForSale().Count, Is.EqualTo(28), "28 items in the Supply Room.");
        }


        [Test]
        public void EveryNewSpecialFiresOnItsLayers()
        {
            foreach (StrikeItem item in NewSpecials)
            {
                StrikeWeaponInfo info = StrikeWeaponRules.Info(item);
                Assert.IsTrue(StrikeWeaponRules.IsSpecial(item), item.ToString());
                Assert.IsTrue(StrikeWeaponRules.IsWeapon(item), item.ToString());
                Assert.IsTrue(info.Fires, $"{item} fires.");
                Assert.That(info.Barrels, Is.GreaterThanOrEqualTo(1));
                Assert.That(info.Speed, Is.GreaterThan(0f), $"{item} reaches its target.");
                Assert.That(info.Damage + info.FragmentDamage * Mathf.Max(1, info.Fragments) + info.ChainDamage, Is.GreaterThan(0f), $"{item} hurts.");
                Assert.That((int)info.ShotKind, Is.InRange(11, 19), $"{item} fires one of the armory's shot kinds.");
                Assert.That(info.Pierce, Is.GreaterThanOrEqualTo(1));
            }
            Assert.That(StrikeWeaponRules.Mask(StrikeItem.FlakCannon), Is.EqualTo(Altitude.Air));
            Assert.That(StrikeWeaponRules.Mask(StrikeItem.ClusterBombs), Is.EqualTo(Altitude.Ground));
            Assert.That(StrikeWeaponRules.Mask(StrikeItem.Napalm), Is.EqualTo(Altitude.Ground));
            Assert.That(StrikeWeaponRules.Mask(StrikeItem.SwarmRockets), Is.EqualTo(Altitude.Both));
            Assert.That(StrikeWeaponRules.Mask(StrikeItem.ArcLightning), Is.EqualTo(Altitude.Both));
            Assert.That(StrikeWeaponRules.Mask(StrikeItem.RailGun), Is.EqualTo(Altitude.Both));
            Assert.That(StrikeWeaponRules.Info(StrikeItem.RailGun).Pierce, Is.GreaterThan(20), "The slug goes through everything.");
            Assert.That(StrikeWeaponRules.Info(StrikeItem.FlakCannon).FragmentKind, Is.EqualTo(StrikeShotKind.FlakShard));
            Assert.That(StrikeWeaponRules.Info(StrikeItem.ClusterBombs).FragmentKind, Is.EqualTo(StrikeShotKind.Bomblet));
            Assert.That(StrikeWeaponRules.Info(StrikeItem.Napalm).FragmentKind, Is.EqualTo(StrikeShotKind.NapalmFire));
            Assert.That(StrikeWeaponRules.Info(StrikeItem.ArcLightning).Chains, Is.EqualTo(4));
            foreach (StrikeItem item in NewGear)
            {
                Assert.IsFalse(StrikeWeaponRules.IsWeapon(item), item.ToString());
                Assert.IsFalse(StrikeWeaponRules.Info(item).Fires, item.ToString());
            }
            Assert.That(StrikeGunnery.BeamKindOf(StrikeItem.RailGun), Is.Zero, "No new beams: the pose's beam kinds stay as they were.");
            // The ghost kinds of the new shots (20 + kind) stay clear of the drone's bolt.
            Assert.That(SpawnService.StrikeGhostBase + (int)StrikeShotKind.RailSlug, Is.LessThan(BodyCodec.DroneShot));
        }


        /// <summary>The damage per second one special does to one target it hits with everything (a beam: its on share).</summary>
        private static float DirectDamagePerSecond(StrikeItem item)
        {
            StrikeWeaponInfo info = StrikeWeaponRules.Info(item);
            if (info.Beam)
            {
                return info.Damage * info.Barrels * info.BeamOn / info.Rate;
            }
            float hit = info.Damage > 0f ? info.Damage : info.BurstDamage;
            return hit * info.Barrels / info.Rate;
        }


        [Test]
        public void TheNewSpecialsLeaveTheOldOnesTheirPlace()
        {
            float deathray = DirectDamagePerSecond(StrikeItem.Deathray);
            float twin = DirectDamagePerSecond(StrikeItem.TwinLaser);
            foreach (StrikeItem item in NewSpecials)
            {
                float dps = DirectDamagePerSecond(item);
                Assert.That(dps, Is.LessThan(deathray), $"{item} ({dps:0}) does not out-damage the deathray ({deathray:0}) on one target.");
                Assert.That(dps, Is.LessThan(twin * 0.5f), $"{item} stays far below the twin laser.");
            }
            // What the new ones are for: crowds (the arc, the flak, the swarm), carpets (cluster, napalm), lines (the rail);
            // the cheapest of them is no stronger on one target than the dumbfire it costs less than.
            Assert.That(DirectDamagePerSecond(StrikeItem.FlakCannon), Is.LessThan(DirectDamagePerSecond(StrikeItem.Dumbfire)));
            Assert.That(DirectDamagePerSecond(StrikeItem.ArcLightning), Is.LessThan(DirectDamagePerSecond(StrikeItem.PulseCannon)),
                "The arc lightning trades single-target damage for its jumps.");
            Assert.That(StrikeArmory.Price(StrikeItem.RailGun), Is.GreaterThan(StrikeArmory.Price(StrikeItem.Deathray)), "The rail gun's line is priced over the deathray.");
        }


        [Test]
        public void SpecialsCycleAcrossTheOldAndTheNew()
        {
            Assert.That(StrikeWeaponRules.Specials[0], Is.EqualTo(StrikeItem.Dumbfire));
            Assert.That(StrikeWeaponRules.Specials[StrikeWeaponRules.Specials.Length - 1], Is.EqualTo(StrikeItem.RailGun));
            Assert.That(StrikeWeaponRules.Specials, Has.No.Member(StrikeItem.PowerDisrupter));
            Assert.That(StrikeWeaponRules.Specials.Length, Is.EqualTo(16), "Ten old specials and six new ones.");

            StrikeLoadout pilot = StrikeLoadout.NewPilot();
            pilot.Money = 9000000;
            Assert.That(StrikeArmory.Buy(pilot, StrikeItem.ArcLightning), Is.EqualTo(BuyResult.Bought));
            Assert.That(pilot.Special, Is.EqualTo(StrikeItem.ArcLightning), "A new special as the first one is selected.");
            StrikeArmory.Buy(pilot, StrikeItem.Dumbfire);
            StrikeArmory.Buy(pilot, StrikeItem.TwinLaser);
            StrikeArmory.Buy(pilot, StrikeItem.FlakCannon);
            StrikeArmory.Buy(pilot, StrikeItem.RailGun);
            Assert.That(pilot.OwnedSpecials(), Is.EqualTo(new[] { StrikeItem.Dumbfire, StrikeItem.TwinLaser, StrikeItem.FlakCannon, StrikeItem.ArcLightning, StrikeItem.RailGun }));
            Assert.That(pilot.CycleSpecial(), Is.EqualTo(StrikeItem.RailGun));
            Assert.That(pilot.CycleSpecial(), Is.EqualTo(StrikeItem.Dumbfire), "Cycling wraps from the new to the old.");
            Assert.That(pilot.CycleSpecial(), Is.EqualTo(StrikeItem.TwinLaser));
            Assert.That(pilot.CycleSpecial(), Is.EqualTo(StrikeItem.FlakCannon), "From the old to the new.");

            Assert.IsTrue(StrikeArmory.Sell(pilot, StrikeItem.FlakCannon));
            Assert.That(pilot.Special, Is.EqualTo(StrikeItem.ArcLightning), "Sold out of the selection: the next one is selected.");
            pilot.Special = StrikeItem.RailGun;
            Assert.IsTrue(StrikeArmory.Sell(pilot, StrikeItem.RailGun));
            Assert.That(pilot.Special, Is.EqualTo(StrikeItem.Dumbfire), "...wrapping to the first.");

            // A hit at low energy destroys the selected special, a new one like any other.
            pilot.Special = StrikeItem.ArcLightning;
            pilot.Energy = 5f;
            pilot.PhaseShields = 0;
            Assert.That(pilot.LoseWeapon(), Is.EqualTo(StrikeItem.ArcLightning));
            Assert.IsFalse(pilot.Owns(StrikeItem.ArcLightning));
            Assert.That(pilot.Special, Is.EqualTo(StrikeItem.Dumbfire));
        }


        [Test]
        public void TheGearDoesWhatItSays()
        {
            Assert.That(StrikeArmory.ArmorFactor(0), Is.EqualTo(1f));
            Assert.That(StrikeArmory.ArmorFactor(1), Is.EqualTo(0.9f).Within(1e-5f));
            Assert.That(StrikeArmory.ArmorFactor(3), Is.EqualTo(0.7f).Within(1e-5f));
            Assert.That(StrikeArmory.ArmorFactor(9), Is.EqualTo(0.7f).Within(1e-5f), "Three plates at most.");

            StrikeLoadout pilot = StrikeLoadout.NewPilot();
            pilot.Money = 9000000;
            for (int i = 0; i < 4; i++)
            {
                StrikeArmory.Buy(pilot, StrikeItem.ArmorPlating);
            }
            Assert.That(pilot.Count(StrikeItem.ArmorPlating), Is.EqualTo(3));
            Assert.That(StrikeArmory.CanBuy(pilot, StrikeItem.ArmorPlating), Is.EqualTo(BuyResult.Full));
            Assert.That(pilot.ArmorFactor, Is.EqualTo(0.7f).Within(1e-5f));
            Assert.That(StrikeArmory.Buy(pilot, StrikeItem.WingmanDrones), Is.EqualTo(BuyResult.Bought));
            Assert.That(StrikeArmory.Buy(pilot, StrikeItem.WingmanDrones), Is.EqualTo(BuyResult.Full));

            Assert.IsFalse(pilot.WantsRepairKit, "No kit, no repair.");
            pilot.SetCount(StrikeItem.RepairKit, 2);
            pilot.Energy = 60f;
            Assert.IsFalse(pilot.WantsRepairKit, "Not while the energy is high.");
            pilot.Energy = 20f;
            Assert.IsTrue(pilot.WantsRepairKit);
            int changes = 0;
            pilot.Changed += () => changes++;
            Assert.IsTrue(pilot.UseRepairKit());
            Assert.That(pilot.Energy, Is.EqualTo(20f + StrikeArmory.RepairKitEnergy));
            Assert.That(pilot.Count(StrikeItem.RepairKit), Is.EqualTo(1));
            Assert.That(changes, Is.EqualTo(1));
            pilot.Energy = 90f;
            Assert.IsTrue(pilot.UseRepairKit());
            Assert.That(pilot.Energy, Is.EqualTo(StrikeRules.MaxEnergy), "Never past full.");
            Assert.IsFalse(pilot.UseRepairKit(), "None left.");
            pilot.Energy = 0f;
            pilot.SetCount(StrikeItem.RepairKit, 1);
            Assert.IsFalse(pilot.WantsRepairKit, "Too late for a destroyed ship.");

            Assert.That(pilot.Collect(StrikeItem.RepairKit), Is.Zero, "A kit pickup adds a kit.");
            Assert.That(pilot.Count(StrikeItem.RepairKit), Is.EqualTo(2));
            pilot.SetCount(StrikeItem.RepairKit, 3);
            Assert.That(pilot.Collect(StrikeItem.RepairKit), Is.EqualTo(StrikeArmory.Resale(StrikeItem.RepairKit)), "Over the cap it pays its resale.");
        }


        // ------------------------------------------------------------------ saving

        [Test]
        public void APilotWithTheNewItemsRoundTripsThroughTheProgress()
        {
            var progress = new AsteroidsProgress(new TransientStrategy(), GameType.Asteroids);
            StrikeLoadout pilot = StrikeLoadout.NewPilot();
            pilot.SetCount(StrikeItem.Dumbfire, 2);
            foreach (StrikeItem item in NewSpecials)
            {
                pilot.SetCount(item, 1);
            }
            pilot.SetCount(StrikeItem.ArmorPlating, 3);
            pilot.SetCount(StrikeItem.RepairDrone, 1);
            pilot.SetCount(StrikeItem.Afterburner, 1);
            pilot.SetCount(StrikeItem.SalvageMagnet, 1);
            pilot.SetCount(StrikeItem.WingmanDrones, 1);
            pilot.SetCount(StrikeItem.RepairKit, 2);
            pilot.Special = StrikeItem.RailGun;
            progress.SavePilot(pilot);

            StrikeLoadout loaded = progress.LoadPilot();
            for (int i = 0; i < StrikeArmory.ItemCount; i++)
            {
                var item = (StrikeItem)i;
                Assert.That(loaded.Count(item), Is.EqualTo(pilot.Count(item)), item.ToString());
            }
            Assert.That(loaded.Special, Is.EqualTo(StrikeItem.RailGun), "A new special stays selected.");
            Assert.That(loaded.ArmorFactor, Is.EqualTo(0.7f).Within(1e-5f));

            // Over the caps (a hand-edited save) the counts come back inside them.
            pilot.SetCount(StrikeItem.ArmorPlating, 7);
            pilot.SetCount(StrikeItem.WingmanDrones, 4);
            progress.SavePilot(pilot);
            loaded = progress.LoadPilot();
            Assert.That(loaded.Count(StrikeItem.ArmorPlating), Is.EqualTo(3));
            Assert.That(loaded.Count(StrikeItem.WingmanDrones), Is.EqualTo(1));
        }


        [Test]
        public void APilotSavedBeforeTheArmoryLoads()
        {
            // Yesterday's save: the pilot's values and the items up to the Ion Scanner, nothing of the new items.
            var storage = new TransientStrategy();
            var progress = new AsteroidsProgress(storage, GameType.Asteroids);
            StrikeLoadout pilot = StrikeLoadout.NewPilot();
            pilot.Money = 765432;
            pilot.SetCount(StrikeItem.TwinLaser, 1);
            pilot.SetCount(StrikeItem.IonScanner, 1);
            pilot.Special = StrikeItem.TwinLaser;
            progress.SavePilot(pilot);
            for (int i = (int)StrikeItem.FlakCannon; i < StrikeArmory.ItemCount; i++)
            {
                string key = $"{GameType.Asteroids}.Progress.Value.Strike.Item.{(StrikeItem)i}";
                Assert.IsTrue(storage.DoesKeyExist(key), $"The item's key is {key}.");
                storage.DeleteByKey(key);
            }
            StrikeLoadout loaded = new AsteroidsProgress(storage, GameType.Asteroids).LoadPilot();
            Assert.That(loaded.Money, Is.EqualTo(765432));
            Assert.That(loaded.Special, Is.EqualTo(StrikeItem.TwinLaser));
            Assert.IsTrue(loaded.HasScanner);
            foreach (StrikeItem item in NewSpecials)
            {
                Assert.That(loaded.Count(item), Is.Zero, item.ToString());
            }
            foreach (StrikeItem item in NewGear)
            {
                Assert.That(loaded.Count(item), Is.Zero, item.ToString());
            }
            Assert.That(loaded.ArmorFactor, Is.EqualTo(1f));
        }


        // ------------------------------------------------------------------ Supply Room

        [Test]
        public void TheSupplyRoomHasTwoPages()
        {
            Assert.That(StrikeUI.CategoryOf(StrikeItem.PlasmaCannon), Is.EqualTo(StrikeUI.SupplyCategory.Weapons));
            Assert.That(StrikeUI.CategoryOf(StrikeItem.TwinLaser), Is.EqualTo(StrikeUI.SupplyCategory.Weapons));
            Assert.That(StrikeUI.CategoryOf(StrikeItem.RailGun), Is.EqualTo(StrikeUI.SupplyCategory.Weapons));
            Assert.That(StrikeUI.CategoryOf(StrikeItem.MegaBomb), Is.EqualTo(StrikeUI.SupplyCategory.Gear));
            Assert.That(StrikeUI.CategoryOf(StrikeItem.IonScanner), Is.EqualTo(StrikeUI.SupplyCategory.Gear));
            Assert.That(StrikeUI.CategoryOf(StrikeItem.RepairKit), Is.EqualTo(StrikeUI.SupplyCategory.Gear));

            StrikeLoadout pilot = StrikeLoadout.NewPilot();
            var offers = new List<StrikeItemInfo>();
            StrikeUI.Offers(pilot, false, offers);
            Assert.That(offers.Count, Is.EqualTo(28));
            StrikeUI.KeepCategory(offers, StrikeUI.SupplyCategory.Weapons);
            Assert.That(offers.Count, Is.EqualTo(18), "The two primary weapons and the sixteen specials: six rows of three.");
            for (int i = 1; i < offers.Count; i++)
            {
                Assert.That(offers[i].Price, Is.GreaterThanOrEqualTo(offers[i - 1].Price), "By price.");
            }
            StrikeUI.Offers(pilot, false, offers);
            StrikeUI.KeepCategory(offers, StrikeUI.SupplyCategory.Gear);
            Assert.That(offers.Count, Is.EqualTo(10), "Megabombs, energy, shields, repair kits and six pieces of equipment.");

            pilot.SetCount(StrikeItem.SalvageMagnet, 1);
            StrikeUI.Offers(pilot, true, offers);
            StrikeUI.KeepCategory(offers, StrikeUI.SupplyCategory.Gear);
            Assert.That(offers.Exists(info => info.Item == StrikeItem.SalvageMagnet), "What the pilot owns is for sale on its page.");
            StrikeUI.Offers(pilot, true, offers);
            StrikeUI.KeepCategory(offers, StrikeUI.SupplyCategory.Weapons);
            Assert.That(offers, Is.Empty, "A new pilot has no weapon to sell.");
            Assert.That(StrikeUI.ShortName(StrikeItem.RailGun), Is.EqualTo("RAIL GUN"), "The HUD names the special in words.");
            Assert.That(StrikeUI.ShortName(StrikeItem.SwarmRockets), Is.EqualTo("SWARM ROCKETS"));
            Assert.That(StrikeUI.ShortName(StrikeItem.WingmanDrones), Is.EqualTo("WINGMAN DRONES"));
        }
    }
}
