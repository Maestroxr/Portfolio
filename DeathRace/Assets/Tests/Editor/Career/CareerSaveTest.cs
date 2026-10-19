using System.Linq;
using Gamebox;
using NUnit.Framework;
using UnityEngine;

namespace Portfolio.DeathRace.Tests
{
    /// <summary>A career goes through Unity's JsonUtility and the named YAML saves and comes back the same.</summary>
    public sealed class CareerSaveTest
    {
        private static CareerState Played()
        {
            CareerState state = CareerTestKit.NewCareer();
            var shop = new CareerShop(state);
            state.wallet.Add(20000);
            shop.Buy(CareerShop.EngineId);
            shop.Buy(CareerShop.SpikesId);
            shop.Buy(CareerShop.RocketId);
            RaceSetup setup = CareerTestKit.Enter(state, Difficulty.Medium);
            Career.Apply(state, CareerTestKit.Result(setup, 2, kills: 1, cash: 200, health: 0.4f, mines: 1));
            new CareerShop(state).Sabotage(state.offers[1].rivals[0]);
            return state;
        }

        private static void AssertSame(CareerState expected, CareerState actual)
        {
            Assert.AreEqual(expected.driverName, actual.driverName);
            Assert.AreEqual(expected.colorIndex, actual.colorIndex);
            Assert.AreEqual(expected.day, actual.day);
            Assert.AreEqual(expected.wallet.Money, actual.wallet.Money);
            Assert.AreEqual(expected.car, actual.car);
            Assert.AreEqual(expected.Current.loadout.engine, actual.Current.loadout.engine);
            Assert.AreEqual(expected.Current.loadout.spikes, actual.Current.loadout.spikes);
            Assert.AreEqual(expected.Current.loadout.health, actual.Current.loadout.health, 1e-5f);
            Assert.AreEqual(expected.consumables.Get(CareerShop.MineId), actual.consumables.Get(CareerShop.MineId));
            Assert.AreEqual(expected.consumables.Get(CareerShop.RocketId), actual.consumables.Get(CareerShop.RocketId));
            Assert.AreEqual(expected.rivals.Select(rival => rival.id + rival.points + rival.tier + rival.wins + rival.colorIndex),
                actual.rivals.Select(rival => rival.id + rival.points + rival.tier + rival.wins + rival.colorIndex));
            Assert.AreEqual(expected.offers.Select(offer => $"{offer.difficulty}{offer.track}{offer.laps}{offer.seed}{string.Join(",", offer.rivals)}"),
                actual.offers.Select(offer => $"{offer.difficulty}{offer.track}{offer.laps}{offer.seed}{string.Join(",", offer.rivals)}"));
            Assert.AreEqual(expected.points, actual.points);
            Assert.AreEqual(expected.races, actual.races);
            Assert.AreEqual(expected.kills, actual.kills);
            Assert.AreEqual(expected.earned, actual.earned);
            Assert.AreEqual(expected.sabotage, actual.sabotage);
            Assert.AreEqual(expected.seed, actual.seed);
            Assert.AreEqual(expected.tracksOpen, actual.tracksOpen);
        }

        [Test]
        public void ACareerSurvivesJsonUtility()
        {
            CareerState state = Played();
            string json = JsonUtility.ToJson(state);
            CareerState back = JsonUtility.FromJson<CareerState>(json);
            AssertSame(state, back);
            Assert.IsNull(Career.Validate(back));
            AssertSame(state, back);
        }

        [Test]
        public void ACareerSurvivesANamedSave()
        {
            CareerState state = Played();
            string yaml = SavedGame.Of(GameType.DeathRace, "Before the Showdown", state, state.tracksOpen, "Tester, day 2").ToYaml();
            SavedGame save = SavedGame.Parse(yaml);
            Assert.AreEqual("Before the Showdown", save.Name);
            AssertSame(state, save.Read<CareerState>());
        }

        [Test]
        public void ALoadedCareerRacesOnTheSame()
        {
            CareerState state = Played();
            CareerState back = JsonUtility.FromJson<CareerState>(JsonUtility.ToJson(state));
            RaceSetup original = CareerTestKit.Enter(state, Difficulty.Hard);
            RaceSetup loaded = CareerTestKit.Enter(back, Difficulty.Hard);
            Assert.AreEqual(original.Entrants.Select(entrant => entrant.RivalId + entrant.Loadout.model + entrant.Loadout.engine),
                loaded.Entrants.Select(entrant => entrant.RivalId + entrant.Loadout.model + entrant.Loadout.engine));
            CareerReport first = Career.Apply(state, CareerTestKit.Result(original, 1));
            CareerReport second = Career.Apply(back, CareerTestKit.Result(loaded, 1));
            Assert.AreEqual(first.OtherRaces.SelectMany(race => race.Order), second.OtherRaces.SelectMany(race => race.Order), "the same day goes the same way");
            Assert.AreEqual(state.wallet.Money, back.wallet.Money);
        }
    }
}
