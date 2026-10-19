using System.Linq;
using Gamebox;
using NUnit.Framework;
using UnityEngine;

namespace Portfolio.DeathRace.Tests
{
    /// <summary>
    /// Saves of the leagues: a career of a later league with everything the workshop sells comes back the same, a save
    /// from before the leagues loads as a career of the first league, and the new fields are checked and mended.
    /// </summary>
    public sealed class LeagueSaveTest
    {
        private static CareerState Equipped()
        {
            CareerState state = Career.New("Tester", 4, CareerTestKit.Seed, LeagueKind.Iron);
            state.wallet.Add(500000);
            var shop = new CareerShop(state);
            shop.Buy(CareerShop.BoostId);
            shop.Buy(CareerShop.FittingId(EquipmentKind.Shield));
            shop.Buy(CareerShop.GunId(GunKind.Plasma));
            shop.Buy(CareerShop.GunId(GunKind.Minigun));
            shop.Buy(CareerShop.SpecialId(SpecialKind.Emp));
            shop.Arm(SpecialKind.Emp);
            RaceSetup setup = CareerTestKit.Enter(state, Difficulty.Medium);
            Career.Apply(state, CareerTestKit.Result(setup, 2, health: 0.6f));
            return state;
        }

        [Test]
        public void ACareerOfALaterLeagueSurvivesANamedSave()
        {
            CareerState state = Equipped();
            string yaml = SavedGame.Of(GameType.DeathRace, "Iron", state, state.tracksOpen, DeathRaceManager.SummaryOf(state)).ToYaml();
            CareerState back = SavedGame.Parse(yaml).Read<CareerState>();
            Assert.IsNull(Career.Validate(back));
            Assert.AreEqual(LeagueKind.Iron, back.League.Kind);
            Assert.AreEqual(state.rivals.Select(rival => rival.id + rival.points + rival.tier), back.rivals.Select(rival => rival.id + rival.points + rival.tier));
            CarLoadout car = back.Current.loadout;
            Assert.AreEqual(1, car.boost);
            Assert.IsTrue(car.shield);
            Assert.AreEqual(GunKind.Minigun, car.gun);
            Assert.IsTrue(car.OwnsGun(GunKind.Plasma));
            Assert.AreEqual(SpecialKind.Emp, back.armed);
            Assert.AreEqual(state.consumables.Get("emp"), back.consumables.Get("emp"));
            Assert.AreEqual(state.offers.Select(offer => $"{offer.track}{offer.seed}"), back.offers.Select(offer => $"{offer.track}{offer.seed}"));
            StringAssert.Contains(L.Data(Leagues.Iron.Name), DeathRaceManager.SummaryOf(back), "the summary names the league");
        }

        [Test]
        public void ASaveFromBeforeTheLeaguesLoadsAsACareerOfTheFirstLeague()
        {
            CareerState state = CareerTestKit.NewCareer();
            new CareerShop(state).Buy(CareerShop.EngineId);
            string json = JsonUtility.ToJson(state);
            // A version 1 save has none of the fields the leagues brought.
            json = json.Replace("\"version\":2", "\"version\":1").Replace("\"league\":0,", "").Replace("\"armed\":0,", "");
            StringAssert.DoesNotContain("\"league\"", json);
            CareerState back = JsonUtility.FromJson<CareerState>(json);
            Assert.AreEqual(1, back.version);
            Assert.IsNull(Career.Validate(back));
            Assert.AreEqual(CareerState.CurrentVersion, back.version);
            Assert.AreEqual(LeagueKind.Wasteland, back.League.Kind);
            Assert.AreEqual(1, back.Current.loadout.engine);
            Assert.AreEqual(GunKind.MachineGun, back.Current.loadout.gun);
            Assert.AreEqual(Roster.Rivals.Select(driver => driver.Id), back.rivals.Select(rival => rival.id));
        }

        [Test]
        public void TheNewFieldsAreCheckedWhenACareerLoads()
        {
            CareerState state = Equipped();
            CarLoadout car = state.Current.loadout;
            car.gun = GunKind.Flamethrower;
            car.guns |= 1 << 9;
            car.oil = 3;
            car.boost = 99;
            state.consumables.Set("oil", 99);
            state.consumables.Set("mortar", 99);
            state.armed = (SpecialKind)7;
            state.tracksOpen = 99;
            state.rivals[3].tier = 12;
            state.rivals.RemoveAt(0);
            Assert.IsNull(Career.Validate(state));
            Assert.AreEqual(GunKind.MachineGun, car.gun, "a gun the car does not have is not mounted");
            Assert.AreEqual(0, car.guns & (1 << 9));
            Assert.IsTrue(car.OwnsGun(GunKind.Plasma), "the guns it has stay");
            Assert.AreEqual(0, car.oil, "the special weapons are the career's, not the car's");
            Assert.AreEqual(car.Spec.MaxLevelOf(UpgradeKind.Boost), car.boost);
            Assert.AreEqual(Specials.Of(SpecialKind.Oil).Max, state.consumables.Get("oil"), "no more than the market sells");
            Assert.AreEqual(Specials.Of(SpecialKind.Mortar).Max, state.consumables.Get("mortar"));
            Assert.That((int)state.armed, Is.InRange(0, Specials.Count - 1));
            Assert.AreEqual(Leagues.Iron.Tracks.Length, state.tracksOpen);
            Assert.IsTrue(state.rivals.All(rival => rival.tier >= 1 && rival.tier <= Leagues.Iron.MaxRivalTier), "no rival past the league's tiers");
            Assert.AreEqual(CareerRules.RivalCount, state.rivals.Count, "the missing rival is back from the league's roster");
            Assert.IsNotNull(state.Rival(Leagues.Iron.Rivals[0].Id));

            Assert.AreEqual("This career was saved by a newer version of the game.", Career.Validate(new CareerState { league = Leagues.Count }));
            Assert.AreEqual("The saved career is incomplete.", Career.Validate(new CareerState { league = -1 }));
            CareerState wrongLeague = CareerTestKit.NewCareer();
            wrongLeague.league = (int)LeagueKind.Hellfire;
            Assert.IsNull(Career.Validate(wrongLeague));
            Assert.AreEqual(Leagues.Hellfire.Rivals.Select(driver => driver.Id).OrderBy(id => id), wrongLeague.rivals.Select(rival => rival.id).OrderBy(id => id),
                "a ladder that is not the league's is the league's again");
        }
    }
}
