using NUnit.Framework;

namespace Portfolio.DeathRace.Tests
{
    /// <summary>What a car carries and spends: rounds at the gun's rate, mines, rockets, nitro charges and the used counts.</summary>
    public class WeaponStockTest
    {
        private const float Step = 0.02f;

        [Test]
        public void TheGunFiresNineRoundsASecond()
        {
            var stock = new WeaponStock(200, 0, 0, 0);
            int rounds = 0;
            for (int i = 0; i < 50; i++)
            {
                rounds += stock.Fire(true, Step);
            }
            Assert.That(rounds, Is.InRange(9, 10));
            Assert.AreEqual(200 - rounds, stock.Ammo);
            Assert.AreEqual(rounds, stock.RoundsFired);
        }

        [Test]
        public void TheFirstRoundOfABurstGoesAtOnce()
        {
            var stock = new WeaponStock(50, 0, 0, 0);
            Assert.AreEqual(1, stock.Fire(true, Step));
            Assert.AreEqual(0, stock.Fire(true, Step));
            for (int i = 0; i < 20; i++)
            {
                stock.Fire(false, Step);
            }
            Assert.AreEqual(1, stock.Fire(true, Step), "released for a while, the next press fires at once");
        }

        [Test]
        public void TheGunStopsWhenTheRoundsRunOut()
        {
            var stock = new WeaponStock(5, 0, 0, 0);
            int rounds = 0;
            for (int i = 0; i < 200; i++)
            {
                rounds += stock.Fire(true, Step);
            }
            Assert.AreEqual(5, rounds);
            Assert.AreEqual(0, stock.Ammo);
            Assert.AreEqual(3, stock.AddAmmo(3));
            Assert.AreEqual(2, stock.AddAmmo(10), "never over the capacity");
            Assert.AreEqual(5, stock.Ammo);
        }

        [Test]
        public void MinesAndRocketsAreCountedOut()
        {
            var stock = new WeaponStock(100, 2, 1, 0);
            Assert.IsTrue(stock.TakeMine());
            Assert.IsTrue(stock.TakeMine());
            Assert.IsFalse(stock.TakeMine(), "none left");
            Assert.AreEqual(0, stock.Mines);
            Assert.AreEqual(2, stock.MinesUsed);
            Assert.IsTrue(stock.TakeRocket());
            Assert.IsFalse(stock.TakeRocket());
            Assert.AreEqual(1, stock.RocketsUsed);
            stock.AddMines(3);
            Assert.AreEqual(3, stock.Mines);
            Assert.AreEqual(2, stock.MinesUsed, "adding is not using");
        }

        [Test]
        public void ANitroChargeBurnsForItsSecondsThenGoesOut()
        {
            var stock = new WeaponStock(100, 0, 0, 2);
            Assert.IsTrue(stock.Nitro(true, Step), "pressing lights a charge");
            Assert.AreEqual(1, stock.NitroCharges);
            Assert.AreEqual(1, stock.NitroUsed);
            float burned = 0f;
            while (stock.NitroActive && burned < 10f)
            {
                stock.Nitro(false, Step);
                burned += Step;
            }
            Assert.AreEqual(CombatRules.NitroSeconds, burned, Step * 1.5f);
            Assert.AreEqual(1, stock.NitroCharges, "released, the next charge waits");
        }

        [Test]
        public void HoldingNitroChainsTheCharges()
        {
            var stock = new WeaponStock(100, 0, 0, 2);
            int lit = 0;
            float held = 0f;
            while (held < CombatRules.NitroSeconds * 3f)
            {
                lit += stock.Nitro(true, Step) ? 1 : 0;
                held += Step;
            }
            Assert.AreEqual(2, lit);
            Assert.AreEqual(0, stock.NitroCharges);
            Assert.AreEqual(2, stock.NitroUsed);
            Assert.IsFalse(stock.NitroActive, "both burned out");
        }

        [Test]
        public void PressingNitroWhileOneBurnsLightsNoOther()
        {
            var stock = new WeaponStock(100, 0, 0, 3);
            stock.Nitro(true, Step);
            stock.Nitro(false, Step);
            Assert.IsFalse(stock.Nitro(true, Step));
            Assert.AreEqual(2, stock.NitroCharges);
        }

        [Test]
        public void TheStockFiresAtTheGunsRate()
        {
            var stock = new WeaponStock(500, 0, 0, 0, 0, 0, 0, CombatRules.MinigunRoundsPerSecond);
            int rounds = 0;
            for (int i = 0; i < 50; i++)
            {
                rounds += stock.Fire(true, Step);
            }
            Assert.That(rounds, Is.InRange(16, 17), "the minigun: sixteen a second");
            var plasma = new WeaponStock(500, 0, 0, 0, 0, 0, 0, CombatRules.PlasmaRoundsPerSecond);
            rounds = 0;
            for (int i = 0; i < 100; i++)
            {
                rounds += plasma.Fire(true, Step);
            }
            Assert.That(rounds, Is.InRange(6, 7), "the plasma cannon: three a second");
            Assert.AreEqual(CombatRules.GunRoundsPerSecond, new WeaponStock(10, 0, 0, 0).RoundsPerSecond, "the machine gun's by default");
        }

        [Test]
        public void SpecialsAreCountedOut()
        {
            var stock = new WeaponStock(100, 0, 0, 0, 2, 1, 0);
            Assert.AreEqual(2, stock.SpecialsLeft(SpecialKind.Oil));
            Assert.AreEqual(1, stock.SpecialsLeft(SpecialKind.Emp));
            Assert.AreEqual(0, stock.SpecialsLeft(SpecialKind.Mortar));
            Assert.IsTrue(stock.TakeSpecial(SpecialKind.Emp));
            Assert.IsFalse(stock.TakeSpecial(SpecialKind.Emp), "none left");
            Assert.IsFalse(stock.TakeSpecial(SpecialKind.Mortar));
            Assert.AreEqual(1, stock.SpecialsUsed(SpecialKind.Emp));
            Assert.AreEqual(0, stock.SpecialsUsed(SpecialKind.Mortar));
            stock.AddSpecials(SpecialKind.Mortar, 2);
            Assert.AreEqual(2, stock.SpecialsLeft(SpecialKind.Mortar));
            Assert.AreEqual(0, stock.SpecialsUsed(SpecialKind.Mortar), "adding is not using");
        }

        [Test]
        public void TheButtonFiresTheArmedSpecialThenTheNextThereIs()
        {
            var stock = new WeaponStock(100, 0, 0, 0, 1, 0, 2);
            Assert.AreEqual(SpecialKind.Oil, stock.NextSpecial(SpecialKind.Oil));
            Assert.AreEqual(SpecialKind.Mortar, stock.NextSpecial(SpecialKind.Mortar));
            Assert.AreEqual(SpecialKind.Mortar, stock.NextSpecial(SpecialKind.Emp), "none of the armed one: the next round from it");
            stock.TakeSpecial(SpecialKind.Mortar);
            stock.TakeSpecial(SpecialKind.Mortar);
            Assert.AreEqual(SpecialKind.Oil, stock.NextSpecial(SpecialKind.Mortar), "round past the end of the list");
            stock.TakeSpecial(SpecialKind.Oil);
            Assert.IsNull(stock.NextSpecial(SpecialKind.Oil), "none at all");
        }

        [Test]
        public void ANitroSystemChargeBurnsLonger()
        {
            var stock = new WeaponStock(100, 0, 0, 1, 0, 0, 0, 0f, 3.4f);
            stock.Nitro(true, Step);
            float burned = 0f;
            while (stock.NitroActive && burned < 10f)
            {
                stock.Nitro(true, Step);
                burned += Step;
            }
            Assert.AreEqual(3.4f, burned, Step * 1.5f);
        }

        [Test]
        public void DousingPutsTheChargeOut()
        {
            var stock = new WeaponStock(100, 0, 0, 1);
            stock.Nitro(true, Step);
            stock.Douse();
            Assert.IsFalse(stock.NitroActive);
            Assert.AreEqual(1, stock.NitroUsed, "the burnt charge is still used");
        }
    }
}
