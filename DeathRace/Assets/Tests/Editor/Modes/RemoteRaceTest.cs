using System.Collections.Generic;
using Gamebox.Online;
using NUnit.Framework;
using UnityEngine;

namespace Portfolio.DeathRace.Tests
{
    /// <summary>
    /// The shared screen and the remote controls of an online race: how the race here ends when the cars driven here
    /// change mid-race (<see cref="RaceRules.NextOnline"/>), what a car handed to this device carries
    /// (<see cref="HandedOverStock"/>), the status the screen publishes for a remote (<see cref="RemoteRacePanel"/>)
    /// and the pad state the tour's pilot drives a remote by.
    /// </summary>
    public class RemoteRaceTest
    {
        private static RaceRules.Runner Runner(bool human, bool finished = false, bool wrecked = false)
        {
            return new RaceRules.Runner { Human = human, Finished = finished, Out = wrecked };
        }

        [Test]
        public void WithNobodyDrivenHereTheRaceWaitsForEveryCar()
        {
            float since = 0f;
            var runners = new List<RaceRules.Runner> { Runner(false, true), Runner(false), Runner(false) };
            Assert.AreEqual(RacePhase.Racing, RaceRules.NextOnline(RacePhase.Racing, runners, 100f, ref since), "a finisher does not end a screen's race");
            Assert.AreEqual(RacePhase.Racing, RaceRules.NextOnline(RacePhase.Racing, runners, 10000f, ref since), "and no clock does");
            runners[1] = Runner(false, true);
            runners[2] = Runner(false, wrecked: true);
            Assert.AreEqual(RacePhase.Over, RaceRules.NextOnline(RacePhase.Racing, runners, 120f, ref since), "every car done ends it");
        }

        [Test]
        public void TheCarsDrivenHereEndTheRaceAsBefore()
        {
            float since = 0f;
            var runners = new List<RaceRules.Runner> { Runner(true), Runner(false) };
            Assert.AreEqual(RacePhase.Racing, RaceRules.NextOnline(RacePhase.Racing, runners, 50f, ref since));
            runners[0] = Runner(true, true);
            Assert.AreEqual(RacePhase.Finishing, RaceRules.NextOnline(RacePhase.Racing, runners, 60f, ref since));
            Assert.AreEqual(60f, since);
            Assert.AreEqual(RacePhase.Finishing, RaceRules.NextOnline(RacePhase.Finishing, runners, 60f + RaceRules.ComputerGrace - 1f, ref since));
            Assert.AreEqual(RacePhase.Over, RaceRules.NextOnline(RacePhase.Finishing, runners, 60f + RaceRules.ComputerGrace, ref since));
        }

        [Test]
        public void ASeatHandedHereWhileFinishingBringsTheRaceBack()
        {
            float since = 60f;
            var runners = new List<RaceRules.Runner> { Runner(true, true), Runner(true) };
            Assert.AreEqual(RacePhase.Racing, RaceRules.NextOnline(RacePhase.Finishing, runners, 70f, ref since), "a car driven here still runs");
            runners[1] = Runner(false);
            Assert.AreEqual(RacePhase.Finishing, RaceRules.NextOnline(RacePhase.Racing, runners, 71f, ref since), "handed away again: the grace starts over");
            Assert.AreEqual(71f, since);
            Assert.AreEqual(RacePhase.Over, RaceRules.NextOnline(RacePhase.Over, runners, 80f, ref since), "an over race stays over");
        }

        [Test]
        public void AHandedOverCarCarriesWhatTheLogLeftIt()
        {
            CarLoadout loadout = DeathRaceLocalRules.Loadout(CarModel.Viper);
            var usage = new HandedOverStock.Usage();
            HandedOverStock.Count(ref usage, OnlineRaceRules.MineAction);
            HandedOverStock.Count(ref usage, OnlineRaceRules.MineAction);
            HandedOverStock.Count(ref usage, OnlineRaceRules.RocketAction);
            HandedOverStock.Count(ref usage, OnlineRaceRules.OilAction);
            HandedOverStock.Count(ref usage, OnlineRaceRules.WreckAction);
            usage.RoundsFired = 70;
            usage.AmmoPicked = 30;
            usage.NitroBurned = 1;
            usage.NitroPicked = 2;
            HandedOverStock.Stock stock = HandedOverStock.Reckon(loadout, 100, usage);
            Assert.AreEqual(60, stock.Ammo);
            Assert.AreEqual(loadout.mines - 2, stock.Mines);
            Assert.AreEqual(loadout.rockets - 1, stock.Rockets);
            Assert.AreEqual(loadout.nitro + 1, stock.Nitro);
            Assert.AreEqual(loadout.oil - 1, stock.Oil);
            Assert.AreEqual(loadout.emp, stock.Emp);
            Assert.AreEqual(loadout.mortar, stock.Mortar, "a wreck uses nothing");
        }

        [Test]
        public void TheStockNeverGoesBelowZeroNorAboveTheGun()
        {
            CarLoadout loadout = DeathRaceLocalRules.Loadout(CarModel.Viper);
            var usage = new HandedOverStock.Usage { Mines = 9, Rockets = 9, Oil = 9, Emp = 9, Mortar = 9, NitroBurned = 9, AmmoPicked = 500 };
            HandedOverStock.Stock stock = HandedOverStock.Reckon(loadout, 100, usage);
            Assert.AreEqual(100, stock.Ammo);
            Assert.AreEqual(0, stock.Mines + stock.Rockets + stock.Nitro + stock.Oil + stock.Emp + stock.Mortar);
            Assert.AreEqual(0, HandedOverStock.Reckon(null, 0, default).Ammo, "no loadout, no gun: nothing");
        }

        [Test]
        public void TheStatusCarriesTheRaceOfTheSeat()
        {
            string status = RemoteRacePanel.Compose(2, 4, 2, 3, 78, 112, 1, 2, 3, Specials.Of(SpecialKind.Emp).Name, 1, 150, RemoteRacePanel.Racing);
            Assert.AreEqual(2, RoomOptions.Number(status, RemoteRacePanel.PlaceKey, 0));
            Assert.AreEqual(4, RoomOptions.Number(status, RemoteRacePanel.CarsKey, 0));
            Assert.AreEqual(2, RoomOptions.Number(status, RemoteRacePanel.LapKey, 0));
            Assert.AreEqual(3, RoomOptions.Number(status, RemoteRacePanel.LapsKey, 0));
            Assert.AreEqual(78, RoomOptions.Number(status, RemoteRacePanel.HullKey, 0));
            Assert.AreEqual(112, RoomOptions.Number(status, RemoteRacePanel.AmmoKey, 0));
            Assert.AreEqual(1, RoomOptions.Number(status, RemoteRacePanel.MinesKey, 0));
            Assert.AreEqual(2, RoomOptions.Number(status, RemoteRacePanel.RocketsKey, 0));
            Assert.AreEqual(3, RoomOptions.Number(status, RemoteRacePanel.NitroKey, 0));
            Assert.AreEqual("EMP Blast", RoomOptions.Value(status, RemoteRacePanel.SpecialKey));
            Assert.AreEqual(1, RoomOptions.Number(status, RemoteRacePanel.KillsKey, 0));
            Assert.AreEqual(150, RoomOptions.Number(status, RemoteRacePanel.CashKey, 0));
            Assert.AreEqual(RemoteRacePanel.Racing, RoomOptions.Value(status, RemoteRacePanel.StateKey));
            Assert.Less(status.Length, 200, "short enough for the server's status row many times over");
            StringAssert.Contains("2", RemoteRaceStatus.Headline(status));
            StringAssert.Contains("2/3", RemoteRaceStatus.Headline(status));
            StringAssert.Contains("FINAL", RemoteRaceStatus.Headline(RemoteRacePanel.Compose(1, 4, 3, 3, 50, 0, 0, 0, 0, "", 0, 0, RemoteRacePanel.Racing)),
                "the last lap is the final lap");
            string wrecked = RemoteRacePanel.Compose(4, 4, 1, 3, 0, 0, 0, 0, 0, "", 0, 0, RemoteRacePanel.Wrecked);
            Assert.AreEqual("", RoomOptions.Value(wrecked, RemoteRacePanel.SpecialKey) ?? "", "no special left reads as none");
            Assert.AreNotEqual(RemoteRaceStatus.Headline(status), RemoteRaceStatus.Headline(wrecked), "a wreck shows in the headline");
        }

        [Test]
        public void ThePadStateStandsForTheInput()
        {
            var input = new CarInput { Throttle = 0.6f, Steer = -0.4f, Fire = true, Nitro = false, DropMine = true, FireRocket = false, Special = true };
            RemoteRacePanel.PadStateOf(input, out Vector2 stick, out uint held, out uint pressed);
            Assert.AreEqual(new Vector2(-0.4f, 0.6f), stick);
            Assert.AreEqual(RemoteRacePanel.Bit(DeathRaceControls.Fire), held);
            Assert.AreEqual(RemoteRacePanel.Bit(DeathRaceControls.Mine) | RemoteRacePanel.Bit(DeathRaceControls.Special), pressed);
            Assert.AreNotEqual(0u, RemoteRacePanel.Bit(DeathRaceControls.Special), "the special has a bit of its own past the pad's four buttons");
            // The bits are those of the scheme's order, as the screen reads them back (RemoteActionSource, tested with the base).
            Assert.AreEqual(1u << 4, RemoteRacePanel.Bit(DeathRaceControls.Fire));
            Assert.AreEqual(1u << 5, RemoteRacePanel.Bit(DeathRaceControls.Nitro));
            Assert.AreEqual(1u << 6, RemoteRacePanel.Bit(DeathRaceControls.Mine));
            Assert.AreEqual(1u << 7, RemoteRacePanel.Bit(DeathRaceControls.Rocket));
            Assert.AreEqual(1u << 8, RemoteRacePanel.Bit(DeathRaceControls.Special));
            RemoteRacePanel.PadStateOf(new CarInput { Throttle = -1.5f, Steer = 2f }, out stick, out held, out pressed);
            Assert.AreEqual(new Vector2(1f, -1f), stick, "clamped to the stick's range");
            Assert.AreEqual(0u, held | pressed);
        }
    }
}
