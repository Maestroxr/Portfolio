using System.Collections.Generic;
using Gamebox;
using NUnit.Framework;

namespace Portfolio.DeathRace.Tests
{
    /// <summary>
    /// Split screen: the options of the shared setup (<see cref="DeathRaceLocalRules"/>) and the race a match makes
    /// (<see cref="DeathRaceLocalRules.BuildSetup"/>).
    /// </summary>
    public class LocalRulesTest
    {
        private static DeathRaceLocalRules Rules(params int[] open)
        {
            return new DeathRaceLocalRules(open.Length > 0 ? open : new[] { 0, 1, 2 }, null);
        }

        private static LocalMatch Match(DeathRaceLocalRules rules, int players, int fill = 2)
        {
            var match = new LocalMatch(GameType.DeathRace, rules) { Count = players };
            match.SetOption(DeathRaceLocalRules.FillOption, fill);
            return match;
        }

        [Test]
        public void TheSetupIsForTwoToFourPeopleAtOnce()
        {
            DeathRaceLocalRules rules = Rules();
            Assert.IsTrue(rules.Simultaneous);
            Assert.AreEqual(2, rules.MinPlayers);
            Assert.AreEqual(4, rules.MaxPlayers);
            Assert.AreEqual(2, rules.MinHumans);
            Assert.IsTrue(rules.AllowComputers);
            Assert.IsNotNull(rules.Controls);
            Assert.AreEqual(9, rules.Controls.Actions.Count, "pedals, wheel, four weapons and the special");
            ControlAction special = rules.Controls.Find(DeathRaceControls.Special);
            Assert.IsNotNull(special);
            for (int seat = 0; seat < 4; seat++)
            {
                Assert.IsTrue(rules.Controls.DefaultsFor(seat, special)[0].IsSet, $"seat {seat + 1} has a special weapon key");
            }
            LocalMatch match = Match(rules, 2);
            match[1].Kind = SeatKind.Computer;
            Assert.IsNotNull(match.Problem(), "one person is a quick race, not split screen");
        }

        [Test]
        public void OnlyTheOpenTracksAreOffered()
        {
            DeathRaceLocalRules rules = Rules(0, 3, 5, 99);
            LocalOption tracks = rules.FindOption(DeathRaceLocalRules.TrackOption);
            CollectionAssert.AreEqual(new[] { TrackLayouts.Of(0).Name, TrackLayouts.Of(3).Name, TrackLayouts.Of(5).Name }, tracks.Values);
            LocalMatch match = Match(rules, 2);
            match.SetOption(DeathRaceLocalRules.TrackOption, 2);
            Assert.AreEqual(5, rules.TrackOf(match));
            Assert.AreEqual(5, rules.BuildSetup(match, 1).Track);
            Assert.AreEqual(1, new DeathRaceLocalRules(new int[0], null).Tracks.Count, "the first track at least");
        }

        [Test]
        public void ThePeopleGetTheViewsAndComputerCarsFillTheGrid()
        {
            DeathRaceLocalRules rules = Rules();
            LocalMatch match = Match(rules, 2);
            match[0].Name = "Ann";
            match[1].Name = "Bo";
            RaceSetup setup = rules.BuildSetup(match, 7);
            Assert.AreEqual(RaceMode.Local, setup.Mode);
            Assert.AreEqual(4, setup.Entrants.Count);
            for (int i = 0; i < 4; i++)
            {
                Assert.AreEqual(i, setup.Entrants[i].Seat);
                Assert.AreEqual(DeathRaceLocalRules.DefaultCar, setup.Entrants[i].Loadout.model, "everybody in the same car");
            }
            Assert.AreEqual("Ann", setup.Entrants[0].Name);
            Assert.AreEqual(DriverKind.Human, setup.Entrants[0].Driver);
            Assert.AreEqual(0, setup.Entrants[0].Viewport);
            Assert.AreEqual(match[0].Color, setup.Entrants[0].Color);
            Assert.AreEqual(1, setup.Entrants[1].Viewport);
            Assert.AreEqual(DriverKind.Computer, setup.Entrants[2].Driver);
            Assert.AreEqual(-1, setup.Entrants[2].Viewport);
            Assert.AreEqual(DeathRaceLocalRules.SkillOf(1), setup.Entrants[2].Skill, "Normal fills");
            Assert.IsNotNull(setup.Entrants[3].RivalId, "the drivers who fill the grid come from the roster");
            Assert.AreNotEqual(setup.Entrants[2].RivalId, setup.Entrants[3].RivalId);
            Assert.AreEqual(2, setup.Humans);
        }

        [Test]
        public void WithoutComputerCarsOnlyTheSeatsRace()
        {
            DeathRaceLocalRules rules = Rules();
            LocalMatch match = Match(rules, 3, fill: 0);
            match[2].Kind = SeatKind.Computer;
            match[2].Level = 2;
            RaceSetup setup = rules.BuildSetup(match, 7);
            Assert.AreEqual(3, setup.Entrants.Count);
            Assert.AreEqual(DriverKind.Computer, setup.Entrants[2].Driver);
            Assert.AreEqual(DeathRaceLocalRules.SkillOf(2), setup.Entrants[2].Skill, "a computer seat drives at its level");
            Assert.AreEqual(-1, rules.FillLevelOf(match));
        }

        [Test]
        public void FourPeopleFillTheGridThemselves()
        {
            DeathRaceLocalRules rules = Rules();
            RaceSetup setup = rules.BuildSetup(Match(rules, 4), 7);
            Assert.AreEqual(4, setup.Entrants.Count);
            Assert.AreEqual(4, setup.Humans);
            var views = new List<int>();
            foreach (RaceEntrant entrant in setup.Entrants)
            {
                views.Add(entrant.Viewport);
            }
            CollectionAssert.AreEqual(new[] { 0, 1, 2, 3 }, views);
        }

        [Test]
        public void TheLapsAreTheTracksOrThePeoplesChoice()
        {
            DeathRaceLocalRules rules = Rules();
            LocalMatch match = Match(rules, 2);
            match.SetOption(DeathRaceLocalRules.TrackOption, 1);
            Assert.AreEqual(TrackLayouts.Of(1).Laps, rules.LapsOf(match), "the track's own");
            match.SetOption(DeathRaceLocalRules.LapsOption, 6);
            Assert.AreEqual(6, rules.LapsOf(match));
            Assert.AreEqual(6, rules.BuildSetup(match, 3).Laps);
        }

        [Test]
        public void TheCarIsChosenOrDrawnPerSeat()
        {
            DeathRaceLocalRules rules = Rules();
            LocalMatch match = Match(rules, 2);
            match.SetOption(DeathRaceLocalRules.CarOption, (int)CarModel.Tyrant);
            Assert.AreEqual(CarModel.Tyrant, rules.CarOf(match));
            Assert.AreEqual(CarModel.Tyrant, rules.BuildSetup(match, 3).Entrants[3].Loadout.model);
            match.SetOption(DeathRaceLocalRules.CarOption, CarSpecs.Count);
            Assert.IsNull(rules.CarOf(match), "the last choice mixes the cars");
            RaceSetup first = rules.BuildSetup(match, 99);
            RaceSetup again = rules.BuildSetup(match, 99);
            for (int i = 0; i < first.Entrants.Count; i++)
            {
                Assert.AreEqual(first.Entrants[i].Loadout.model, again.Entrants[i].Loadout.model, "the seed draws the cars");
            }
            bool mixed = false;
            for (int seed = 0; seed < 20 && !mixed; seed++)
            {
                RaceSetup drawn = rules.BuildSetup(match, seed);
                mixed = drawn.Entrants[0].Loadout.model != drawn.Entrants[1].Loadout.model;
            }
            Assert.IsTrue(mixed, "a mixed grid has different cars");
        }

        [Test]
        public void EveryCarCarriesTheSameLoad()
        {
            CarLoadout load = DeathRaceLocalRules.Loadout(CarModel.Hornet);
            Assert.AreEqual(1, load.engine);
            Assert.AreEqual(3, load.mines);
            Assert.AreEqual(2, load.rockets);
            Assert.AreEqual(3, load.nitro);
            Assert.AreEqual(1, load.oil, "one of each special weapon");
            Assert.AreEqual(1, load.emp);
            Assert.AreEqual(1, load.mortar);
            Assert.AreEqual(SpecialKind.Oil, load.special);
            DeathRaceLocalRules rules = Rules();
            foreach (RaceEntrant entrant in rules.BuildSetup(Match(rules, 2), 5).Entrants)
            {
                Assert.AreEqual(DeathRaceLocalRules.SpecialsPerSeat, entrant.Loadout.oil, entrant.Name);
                Assert.AreEqual(DeathRaceLocalRules.SpecialsPerSeat, entrant.Loadout.emp, entrant.Name);
                Assert.AreEqual(DeathRaceLocalRules.SpecialsPerSeat, entrant.Loadout.mortar, entrant.Name);
            }
        }

        [Test]
        public void AllTwelveTracksAndCarsAreOffered()
        {
            var all = new List<int>();
            for (int track = 0; track < TrackLayouts.Count; track++)
            {
                all.Add(track);
            }
            DeathRaceLocalRules rules = Rules(all.ToArray());
            Assert.AreEqual(12, rules.Tracks.Count);
            LocalOption tracks = rules.FindOption(DeathRaceLocalRules.TrackOption);
            Assert.AreEqual("Gridlock Boulevard", tracks.Values[11]);
            LocalOption cars = rules.FindOption(DeathRaceLocalRules.CarOption);
            Assert.AreEqual(CarSpecs.Count + 1, cars.Values.Length, "every car and the mixed grid");
            Assert.AreEqual(12, CarSpecs.Count);
            Assert.AreEqual(CarSpecs.Of(CarModel.Apocalypse).Name, cars.Values[(int)CarModel.Apocalypse]);
            Assert.AreEqual(DeathRaceLocalRules.MixedCars, cars.Values[CarSpecs.Count]);
            LocalMatch match = Match(rules, 2);
            match.SetOption(DeathRaceLocalRules.TrackOption, 10);
            match.SetOption(DeathRaceLocalRules.CarOption, (int)CarModel.Goliath);
            RaceSetup setup = rules.BuildSetup(match, 11);
            Assert.AreEqual(10, setup.Track);
            Assert.AreEqual(TrackLayouts.Of(10).Laps, setup.Laps);
            Assert.AreEqual(CarModel.Goliath, setup.Entrants[0].Loadout.model);
            match.SetOption(DeathRaceLocalRules.CarOption, CarSpecs.Count);
            var drawn = new HashSet<CarModel>();
            for (int seed = 0; seed < 60; seed++)
            {
                foreach (RaceEntrant entrant in rules.BuildSetup(match, seed).Entrants)
                {
                    drawn.Add(entrant.Loadout.model);
                }
            }
            Assert.IsTrue(drawn.Contains(CarModel.Apocalypse) && drawn.Contains(CarModel.Jackal), "a mixed grid draws from all twelve");
        }
    }
}
