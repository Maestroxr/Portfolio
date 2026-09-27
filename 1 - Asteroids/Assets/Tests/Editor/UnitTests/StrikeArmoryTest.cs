using System.Collections.Generic;
using NUnit.Framework;

namespace Portfolio.Asteroids.Tests
{
    public class StrikeArmoryTest
    {
        private static StrikeLoadout Rich()
        {
            StrikeLoadout pilot = StrikeLoadout.NewPilot();
            pilot.Money = 5000000;
            return pilot;
        }


        [Test]
        public void ANewPilotHasTheMachineGunAndTenThousand()
        {
            StrikeLoadout pilot = StrikeLoadout.NewPilot();
            Assert.That(pilot.Money, Is.EqualTo(10000));
            Assert.That(pilot.Energy, Is.EqualTo(75f));
            Assert.That(pilot.Difficulty, Is.EqualTo(StrikeDifficulty.Veteran));
            Assert.That(pilot.Count(StrikeItem.MachineGun), Is.EqualTo(1));
            Assert.That(pilot.PhaseShields, Is.Zero);
            Assert.That(pilot.Megabombs, Is.Zero);
            Assert.IsFalse(pilot.HasScanner);
            Assert.IsFalse(pilot.HasSpecial);
            Assert.That(pilot.Special, Is.EqualTo(StrikeItem.MachineGun));
        }


        [Test]
        public void PricesResaleAndSorting()
        {
            Assert.That(StrikeArmory.Price(StrikeItem.TwinLaser), Is.EqualTo(1750000));
            Assert.That(StrikeArmory.Price(StrikeItem.AirMissiles), Is.EqualTo(63500));
            foreach (StrikeItemInfo info in StrikeArmory.All)
            {
                Assert.That(info.Resale, Is.EqualTo(info.Price / 2), info.Item.ToString());
            }
            List<StrikeItemInfo> sorted = StrikeArmory.Sorted();
            for (int i = 1; i < sorted.Count; i++)
            {
                StrikeItemInfo a = sorted[i - 1];
                StrikeItemInfo b = sorted[i];
                Assert.IsTrue(a.Price < b.Price || a.Price == b.Price && a.Item < b.Item, $"{a.Item} before {b.Item}");
            }
            List<StrikeItemInfo> specials = StrikeArmory.ForSale(ItemGroup.Special);
            Assert.That(specials[0].Item, Is.EqualTo(StrikeItem.AirMissiles), "The cheapest special comes first.");
            Assert.That(specials[specials.Count - 1].Item, Is.EqualTo(StrikeItem.TwinLaser));
            Assert.IsFalse(specials.Exists(info => info.Item == StrikeItem.PowerDisrupter), "The disrupter is not for sale.");
            Assert.IsFalse(StrikeArmory.ForSale().Exists(info => info.NotForSale));
            Assert.IsTrue(StrikeArmory.Info(StrikeItem.Dumbfire).CanBePickup, "145,200 may be a pickup.");
            Assert.IsFalse(StrikeArmory.Info(StrikeItem.MiniGun).CanBePickup);
        }


        [Test]
        public void BuyingTakesMoneyUntilTheCap()
        {
            StrikeLoadout pilot = Rich();
            int changes = 0;
            pilot.Changed += () => changes++;
            Assert.That(StrikeArmory.Buy(pilot, StrikeItem.PlasmaCannon), Is.EqualTo(BuyResult.Bought));
            Assert.That(pilot.Money, Is.EqualTo(5000000 - 78800));
            Assert.That(changes, Is.EqualTo(1));
            Assert.That(StrikeArmory.Buy(pilot, StrikeItem.PlasmaCannon), Is.EqualTo(BuyResult.Full), "One plasma cannon at most.");
            Assert.That(StrikeArmory.Buy(pilot, StrikeItem.MachineGun), Is.EqualTo(BuyResult.NotForSale));
            Assert.That(StrikeArmory.Buy(pilot, StrikeItem.PowerDisrupter), Is.EqualTo(BuyResult.NotForSale));
            for (int i = 0; i < 5; i++)
            {
                Assert.That(StrikeArmory.Buy(pilot, StrikeItem.MegaBomb), Is.EqualTo(BuyResult.Bought));
            }
            Assert.That(StrikeArmory.CanBuy(pilot, StrikeItem.MegaBomb), Is.EqualTo(BuyResult.Full));
            Assert.That(pilot.Megabombs, Is.EqualTo(5));
            Assert.That(changes, Is.EqualTo(6), "A refused purchase changes nothing.");

            StrikeLoadout poor = StrikeLoadout.NewPilot();
            Assert.That(StrikeArmory.Buy(poor, StrikeItem.AirMissiles), Is.EqualTo(BuyResult.NoMoney));
            Assert.That(poor.Money, Is.EqualTo(10000));
            Assert.That(StrikeArmory.Buy(poor, StrikeItem.IonScanner), Is.EqualTo(BuyResult.Bought));
            Assert.IsTrue(poor.HasScanner);
            Assert.That(poor.Money, Is.Zero);
            Assert.That(StrikeArmory.Buy(poor, StrikeItem.IonScanner), Is.EqualTo(BuyResult.Full), "Full comes before NoMoney.");
        }


        [Test]
        public void TheFirstSpecialIsSelectedAndSpecialsCycleInEnumOrder()
        {
            StrikeLoadout pilot = Rich();
            StrikeArmory.Buy(pilot, StrikeItem.GroundMissiles);
            Assert.That(pilot.Special, Is.EqualTo(StrikeItem.GroundMissiles));
            StrikeArmory.Buy(pilot, StrikeItem.AirMissiles);
            StrikeArmory.Buy(pilot, StrikeItem.Dumbfire);
            StrikeArmory.Buy(pilot, StrikeItem.TwinLaser);
            Assert.That(pilot.Special, Is.EqualTo(StrikeItem.GroundMissiles), "Buying more keeps the selection.");
            Assert.That(pilot.CycleSpecial(), Is.EqualTo(StrikeItem.TwinLaser));
            Assert.That(pilot.CycleSpecial(), Is.EqualTo(StrikeItem.Dumbfire), "Cycling wraps.");
            Assert.That(pilot.CycleSpecial(), Is.EqualTo(StrikeItem.AirMissiles));
            Assert.That(pilot.CycleSpecial(), Is.EqualTo(StrikeItem.GroundMissiles));
            Assert.That(pilot.OwnedSpecials(), Is.EqualTo(new[] { StrikeItem.Dumbfire, StrikeItem.AirMissiles, StrikeItem.GroundMissiles, StrikeItem.TwinLaser }));

            StrikeLoadout none = StrikeLoadout.NewPilot();
            Assert.That(none.CycleSpecial(), Is.EqualTo(StrikeItem.MachineGun), "Nothing to cycle.");
            StrikeLoadout one = Rich();
            StrikeArmory.Buy(one, StrikeItem.Bombs);
            Assert.That(one.CycleSpecial(), Is.EqualTo(StrikeItem.Bombs), "A single special stays selected.");
        }


        [Test]
        public void SellingPaysHalfAndHandsTheSelectionOn()
        {
            StrikeLoadout pilot = Rich();
            StrikeArmory.Buy(pilot, StrikeItem.AirMissiles);
            StrikeArmory.Buy(pilot, StrikeItem.GroundMissiles);
            int money = pilot.Money;
            Assert.IsFalse(StrikeArmory.CanSell(pilot, StrikeItem.MachineGun), "The machine gun cannot be sold.");
            Assert.IsFalse(StrikeArmory.Sell(pilot, StrikeItem.Bombs), "Nothing to sell.");
            Assert.IsTrue(StrikeArmory.Sell(pilot, StrikeItem.AirMissiles));
            Assert.That(pilot.Money, Is.EqualTo(money + 31750));
            Assert.That(pilot.Special, Is.EqualTo(StrikeItem.GroundMissiles), "The next owned special is selected.");
            Assert.IsTrue(StrikeArmory.Sell(pilot, StrikeItem.GroundMissiles));
            Assert.That(pilot.Special, Is.EqualTo(StrikeItem.MachineGun));
            Assert.IsFalse(pilot.HasSpecial);
            Assert.That(pilot.Count(StrikeItem.MachineGun), Is.EqualTo(1));
        }


        [Test]
        public void TheEnergyModuleIsPricedPerPoint()
        {
            StrikeLoadout pilot = Rich();
            Assert.That(pilot.Energy, Is.EqualTo(75f));
            Assert.That(StrikeArmory.BuyPrice(pilot, StrikeItem.EnergyModule), Is.EqualTo(10000), "25 points at 400.");
            Assert.That(StrikeArmory.Buy(pilot, StrikeItem.EnergyModule), Is.EqualTo(BuyResult.Bought));
            Assert.That(pilot.Energy, Is.EqualTo(100f));
            Assert.That(pilot.Money, Is.EqualTo(5000000 - 10000));
            Assert.That(StrikeArmory.CanBuy(pilot, StrikeItem.EnergyModule), Is.EqualTo(BuyResult.Full));

            pilot.Energy = 90f;
            Assert.That(StrikeArmory.BuyPrice(pilot, StrikeItem.EnergyModule), Is.EqualTo(4000), "Only the 10 points missing.");
            StrikeArmory.Buy(pilot, StrikeItem.EnergyModule);
            Assert.That(pilot.Energy, Is.EqualTo(100f));

            Assert.That(StrikeArmory.SellPrice(pilot, StrikeItem.EnergyModule), Is.EqualTo(5000));
            int money = pilot.Money;
            Assert.IsTrue(StrikeArmory.Sell(pilot, StrikeItem.EnergyModule));
            Assert.That(pilot.Energy, Is.EqualTo(75f));
            Assert.That(pilot.Money, Is.EqualTo(money + 5000));
            StrikeArmory.Sell(pilot, StrikeItem.EnergyModule);
            StrikeArmory.Sell(pilot, StrikeItem.EnergyModule);
            Assert.That(pilot.Energy, Is.EqualTo(25f));
            Assert.IsFalse(StrikeArmory.CanSell(pilot, StrikeItem.EnergyModule), "Energy is sold only while above 25.");
        }


        [Test]
        public void OnlyAnUndamagedPhaseShieldCanBeSold()
        {
            StrikeLoadout pilot = Rich();
            Assert.That(StrikeArmory.Buy(pilot, StrikeItem.PhaseShield), Is.EqualTo(BuyResult.Bought));
            Assert.That(pilot.PhaseShields, Is.EqualTo(1));
            Assert.That(pilot.ShieldPoints, Is.EqualTo(100f));
            pilot.AbsorbDamage(10f);
            Assert.IsFalse(StrikeArmory.CanSell(pilot, StrikeItem.PhaseShield), "The one in use is damaged.");
            StrikeArmory.Buy(pilot, StrikeItem.PhaseShield);
            Assert.That(pilot.ShieldPoints, Is.EqualTo(90f), "A bought spare leaves the one in use as it is.");
            Assert.IsTrue(StrikeArmory.Sell(pilot, StrikeItem.PhaseShield), "The spare is whole.");
            Assert.That(pilot.PhaseShields, Is.EqualTo(1));
            Assert.That(pilot.ShieldPoints, Is.EqualTo(90f));
            Assert.IsFalse(StrikeArmory.CanSell(pilot, StrikeItem.PhaseShield));
        }


        [Test]
        public void PhaseShieldsAbsorbFirstThenTheNextOneThenEnergy()
        {
            StrikeLoadout pilot = StrikeLoadout.NewPilot();
            pilot.Energy = 50f;
            pilot.PhaseShields = 2;
            pilot.ShieldPoints = 30f;
            Assert.That(pilot.AbsorbDamage(20f), Is.EqualTo(20f));
            Assert.That(pilot.ShieldPoints, Is.EqualTo(10f));
            Assert.That(pilot.Energy, Is.EqualTo(50f), "Energy is untouched while a shield holds.");
            Assert.That(pilot.AbsorbDamage(25f), Is.EqualTo(25f));
            Assert.That(pilot.PhaseShields, Is.EqualTo(1), "The used-up shield is dropped.");
            Assert.That(pilot.ShieldPoints, Is.EqualTo(85f), "The next one takes the rest.");
            Assert.That(pilot.AbsorbDamage(95f), Is.EqualTo(95f));
            Assert.That(pilot.PhaseShields, Is.Zero);
            Assert.That(pilot.ShieldPoints, Is.Zero);
            Assert.That(pilot.Energy, Is.EqualTo(40f));
            Assert.That(pilot.AbsorbDamage(70f), Is.EqualTo(40f), "Only what the energy held is taken.");
            Assert.That(pilot.Energy, Is.Zero);
            Assert.That(pilot.AbsorbDamage(0f), Is.Zero);
        }


        [Test]
        public void WeaponsAreLostAtLowEnergyWithSparesFirst()
        {
            StrikeLoadout pilot = Rich();
            StrikeArmory.Buy(pilot, StrikeItem.PlasmaCannon);
            StrikeArmory.Buy(pilot, StrikeItem.MicroMissiles);
            StrikeArmory.Buy(pilot, StrikeItem.AirMissiles);
            StrikeArmory.Buy(pilot, StrikeItem.AirMissiles);
            StrikeArmory.Buy(pilot, StrikeItem.Bombs);
            pilot.Energy = 50f;
            Assert.IsFalse(pilot.WeaponsAtRisk);
            Assert.That(pilot.LoseWeapon(), Is.EqualTo(StrikeItem.MachineGun), "Nothing is lost above 10 energy.");
            pilot.Energy = 10f;
            pilot.PhaseShields = 1;
            pilot.ShieldPoints = 5f;
            Assert.That(pilot.LoseWeapon(), Is.EqualTo(StrikeItem.MachineGun), "Nothing is lost behind a phase shield.");
            pilot.PhaseShields = 0;
            pilot.ShieldPoints = 0f;
            Assert.IsTrue(pilot.WeaponsAtRisk);
            Assert.That(pilot.Special, Is.EqualTo(StrikeItem.AirMissiles));
            Assert.That(pilot.LoseWeapon(), Is.EqualTo(StrikeItem.AirMissiles));
            Assert.That(pilot.Count(StrikeItem.AirMissiles), Is.EqualTo(1), "The spare takes its place.");
            Assert.That(pilot.Special, Is.EqualTo(StrikeItem.AirMissiles));
            Assert.That(pilot.LoseWeapon(), Is.EqualTo(StrikeItem.AirMissiles));
            Assert.That(pilot.Special, Is.EqualTo(StrikeItem.Bombs), "The next owned special is selected.");
            Assert.That(pilot.LoseWeapon(), Is.EqualTo(StrikeItem.Bombs));
            Assert.IsFalse(pilot.HasSpecial);
            Assert.That(pilot.LoseWeapon(), Is.EqualTo(StrikeItem.MicroMissiles));
            Assert.That(pilot.LoseWeapon(), Is.EqualTo(StrikeItem.PlasmaCannon));
            Assert.That(pilot.LoseWeapon(), Is.EqualTo(StrikeItem.MachineGun), "The machine gun is never lost.");
            Assert.That(pilot.Count(StrikeItem.MachineGun), Is.EqualTo(1));
        }


        [Test]
        public void PickupsOverTheCapPayTheirResale()
        {
            StrikeLoadout pilot = StrikeLoadout.NewPilot();
            int money = pilot.Money;
            Assert.That(pilot.Collect(StrikeItem.AirMissiles), Is.Zero);
            Assert.That(pilot.Special, Is.EqualTo(StrikeItem.AirMissiles), "The first special collected is selected.");
            Assert.That(pilot.Collect(StrikeItem.PlasmaCannon), Is.Zero);
            Assert.That(pilot.Collect(StrikeItem.PlasmaCannon), Is.EqualTo(39400), "A second plasma cannon pays its resale.");
            Assert.That(pilot.Money, Is.EqualTo(money), "The caller pays the resale (mission money is the score).");
            for (int i = 0; i < 5; i++)
            {
                Assert.That(pilot.Collect(StrikeItem.PhaseShield), Is.Zero);
            }
            Assert.That(pilot.PhaseShields, Is.EqualTo(5));
            Assert.That(pilot.ShieldPoints, Is.EqualTo(100f));
            Assert.That(pilot.Collect(StrikeItem.PhaseShield), Is.EqualTo(39250));
            Assert.That(pilot.Collect(StrikeItem.MachineGun), Is.Zero, "No item.");
            Assert.That(pilot.Count(StrikeItem.MachineGun), Is.EqualTo(1));
        }


        [Test]
        public void EnergyPickupsFillEnergyThenTheShield()
        {
            StrikeLoadout pilot = StrikeLoadout.NewPilot();
            pilot.Energy = 90f;
            pilot.Collect(StrikeItem.EnergyModule);
            Assert.That(pilot.Energy, Is.EqualTo(100f));
            pilot.PhaseShields = 1;
            pilot.ShieldPoints = 50f;
            pilot.Collect(StrikeItem.EnergyModule);
            Assert.That(pilot.ShieldPoints, Is.EqualTo(56.25f), "At full energy a quarter goes to the shield.");
            pilot.AddEnergy(-150f);
            Assert.That(pilot.Energy, Is.Zero);
            pilot.AddEnergy(30f);
            Assert.That(pilot.Energy, Is.EqualTo(30f));
        }


        [Test]
        public void MegabombsAreUsedOneByOne()
        {
            StrikeLoadout pilot = StrikeLoadout.NewPilot();
            Assert.IsFalse(pilot.UseMegabomb());
            pilot.Collect(StrikeItem.MegaBomb);
            Assert.IsTrue(pilot.UseMegabomb());
            Assert.That(pilot.Megabombs, Is.Zero);
        }


        [Test]
        public void TheWalletIsCapped()
        {
            StrikeLoadout pilot = StrikeLoadout.NewPilot();
            pilot.AddMoney(StrikeRules.WalletCap);
            Assert.That(pilot.Money, Is.EqualTo(9999999));
            pilot.Money = 9000000;
            StrikeArmory.Buy(pilot, StrikeItem.TwinLaser);
            StrikeArmory.Buy(pilot, StrikeItem.PhaseShield);
            pilot.Money = StrikeRules.WalletCap;
            Assert.IsTrue(StrikeArmory.Sell(pilot, StrikeItem.TwinLaser));
            Assert.That(pilot.Money, Is.EqualTo(StrikeRules.WalletCap), "Selling never passes the cap.");
            pilot.AddMoney(-20000000);
            Assert.That(pilot.Money, Is.Zero);
        }


        [Test]
        public void TheWorkingCopyIsCommittedOnlyOnAWin()
        {
            StrikeLoadout saved = StrikeLoadout.NewPilot();
            saved.Energy = 10f;
            StrikeLoadout working = saved.Clone();
            working.EnsureLaunchEnergy();
            Assert.That(working.Energy, Is.EqualTo(25f), "A mission starts with at least 25 energy.");
            Assert.That(saved.Energy, Is.EqualTo(10f), "The saved pilot is untouched.");
            working.Collect(StrikeItem.AirMissiles);
            working.AbsorbDamage(5f);
            Assert.IsFalse(saved.Owns(StrikeItem.AirMissiles), "The copies are independent.");

            // A failed mission drops the working copy: nothing to do. A won one lands:
            int changes = 0;
            saved.Changed += () => changes++;
            saved.CommitMission(working, 75000);
            Assert.That(changes, Is.EqualTo(1));
            Assert.IsTrue(saved.Owns(StrikeItem.AirMissiles));
            Assert.That(saved.Energy, Is.EqualTo(20f));
            Assert.That(saved.Money, Is.EqualTo(85000));
            saved.CommitMission(saved.Clone(), StrikeRules.WalletCap);
            Assert.That(saved.Money, Is.EqualTo(StrikeRules.WalletCap));

            StrikeLoadout healthy = StrikeLoadout.NewPilot();
            healthy.EnsureLaunchEnergy();
            Assert.That(healthy.Energy, Is.EqualTo(75f));
        }


        [Test]
        public void NormaliseRepairsABrokenPilot()
        {
            var pilot = new StrikeLoadout { Money = -5, Energy = 150f, PhaseShields = 9, ShieldPoints = 0f, Megabombs = 12, Special = StrikeItem.Deathray };
            pilot.SetCount(StrikeItem.PlasmaCannon, 4);
            pilot.SetCount(StrikeItem.Dumbfire, 2);
            pilot.Normalise();
            Assert.That(pilot.Money, Is.Zero);
            Assert.That(pilot.Energy, Is.EqualTo(100f));
            Assert.That(pilot.PhaseShields, Is.EqualTo(5));
            Assert.That(pilot.ShieldPoints, Is.EqualTo(100f));
            Assert.That(pilot.Megabombs, Is.EqualTo(5));
            Assert.That(pilot.Count(StrikeItem.PlasmaCannon), Is.EqualTo(1));
            Assert.That(pilot.Count(StrikeItem.MachineGun), Is.EqualTo(1));
            Assert.That(pilot.Special, Is.EqualTo(StrikeItem.Dumbfire), "An unowned selection moves to an owned special.");
        }
    }
}
