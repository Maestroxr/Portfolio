using System.Linq;
using NUnit.Framework;

namespace Portfolio.DeathRace.Tests
{
    /// <summary>A new career, the race setup of an offer, and what <see cref="Career.Apply"/> does with a race's result.</summary>
    public sealed class CareerApplyTest
    {
        [Test]
        public void ANewCareerHasTheStartingCarMoneyAndKit()
        {
            CareerState state = Career.New("  Ada ", 5, 9);
            Assert.AreEqual("Ada", state.driverName);
            Assert.AreEqual(CareerRules.StartMoney, state.wallet.Money);
            Assert.AreEqual(CarModel.Rustbucket, state.car);
            Assert.AreEqual(1, state.garage.Count);
            Assert.AreEqual(1f, state.Current.loadout.health);
            Assert.AreEqual(CareerRules.StartMines, state.consumables.Get(CareerShop.MineId));
            Assert.AreEqual(CareerRules.StartNitro, state.consumables.Get(CareerShop.NitroId));
            Assert.AreEqual(3, state.offers.Count);
            Assert.AreEqual(1, state.day);
            Assert.AreEqual("Rookie", Career.New(" ", 0, 1).driverName);
        }

        [Test]
        public void NobodySharesThePlayersColour()
        {
            CareerState state = Career.New("Ada", 5, 9);
            int[] colors = state.rivals.Select(rival => rival.colorIndex).Append(state.colorIndex).ToArray();
            Assert.AreEqual(CareerRules.LadderSize, colors.Distinct().Count());
            Assert.IsTrue(colors.All(color => color >= 0 && color < Career.Colors));
        }

        [Test]
        public void AnOfferBecomesARaceOfThePlayerAndItsRivals()
        {
            CareerState state = CareerTestKit.NewCareer();
            state.consumables.Set(CareerShop.RocketId, 3);
            state.Current.loadout.health = 0.05f;
            RaceSetup setup = CareerTestKit.Enter(state, Difficulty.Medium);
            RaceOffer offer = state.offers[state.entered];
            Assert.AreEqual(RaceMode.Career, setup.Mode);
            Assert.AreEqual(offer.track, setup.Track);
            Assert.AreEqual(offer.laps, setup.Laps);
            Assert.AreEqual(Difficulty.Medium, setup.Difficulty);
            Assert.AreEqual(CareerRules.CashPerPickup(Difficulty.Medium), setup.CashPerPickup);
            Assert.AreEqual(4, setup.Entrants.Count);
            RaceEntrant player = setup.EntrantAt(0);
            Assert.AreEqual(DriverKind.Human, player.Driver);
            Assert.AreEqual("Tester", player.Name);
            Assert.AreEqual(3, player.Loadout.rockets, "the player carries everything the career has");
            Assert.AreEqual(CareerRules.StartMines, player.Loadout.mines);
            Assert.AreEqual(CareerRules.MinStartHealth, player.Loadout.health, 1e-5f, "the crew patches a wreck up to start");
            Assert.AreEqual(offer.rivals, setup.Entrants.Skip(1).Select(entrant => entrant.RivalId));
            foreach (RaceEntrant rival in setup.Entrants.Skip(1))
            {
                Assert.AreEqual(DriverKind.Computer, rival.Driver);
                Assert.AreEqual(state.Rival(rival.RivalId).Car, rival.Loadout.model, "rivals drive the car of their tier");
                Assert.AreEqual(1f, rival.Loadout.health);
                Assert.That(rival.Skill, Is.InRange(0.4f, 0.6f), "a Medium race's drivers are in the Medium band of the AI");
            }
            Assert.That(setup.Entrants.Skip(1).Max(entrant => entrant.Skill) - setup.Entrants.Skip(1).Min(entrant => entrant.Skill),
                Is.GreaterThan(0.01f), "the drivers of a race differ");
            Assert.AreEqual(new[] { 1, 2, 3 }, setup.Entrants.Skip(1).Select(entrant => entrant.Seat));
        }

        [Test]
        public void ASabotagedRivalStartsHurt()
        {
            CareerState state = CareerTestKit.NewCareer();
            string target = state.offers[0].rivals[1];
            state.sabotage = target;
            RaceSetup setup = CareerTestKit.Enter(state, Difficulty.Easy);
            Assert.AreEqual(CareerRules.SabotageHealth, setup.Entrants.First(entrant => entrant.RivalId == target).Loadout.health, 1e-5f);
            Assert.AreEqual(2, setup.Entrants.Skip(1).Count(entrant => entrant.Loadout.health == 1f), "the other rivals start whole");
        }

        [Test]
        public void AWinPaysThePrizeBountiesAndCashAndScores()
        {
            CareerState state = CareerTestKit.NewCareer();
            RaceSetup setup = CareerTestKit.Enter(state, Difficulty.Easy);
            int before = state.wallet.Money;
            CareerReport report = Career.Apply(state, CareerTestKit.Result(setup, 1, kills: 2, cash: 150, health: 0.6f, mines: 1, nitro: 2));
            Assert.AreEqual(1000, report.Prize);
            Assert.AreEqual(2 * CareerRules.Bounty(Difficulty.Easy), report.Bounty);
            Assert.AreEqual(150, report.Cash);
            Assert.AreEqual(report.Prize + report.Bounty + report.Cash, report.Earned);
            Assert.AreEqual(before + report.Earned, state.wallet.Money);
            Assert.AreEqual(3, report.Points);
            Assert.AreEqual(3, state.points);
            Assert.AreEqual(1, state.wins);
            Assert.AreEqual(1, state.races);
            Assert.AreEqual(2, state.kills);
            Assert.AreEqual(report.Earned, state.earned);
            Assert.AreEqual(0.6f, state.Current.loadout.health, 1e-5f, "the damage is carried into the garage");
            Assert.AreEqual(Career.RepairCost(state.Current.loadout), report.RepairCost);
            Assert.Greater(report.RepairCost, 0);
            Assert.AreEqual(CareerRules.StartMines - 1, state.consumables.Get(CareerShop.MineId), "what was used is gone");
            Assert.AreEqual(CareerRules.StartNitro - 2, state.consumables.Get(CareerShop.NitroId));
            Assert.AreEqual(2, state.day, "the next day starts");
            Assert.AreEqual(-1, state.entered);
            Assert.AreEqual(3, state.offers.Count);
        }

        [Test]
        public void AWreckPaysNoPrizeAndScoresNothingButKeepsBountiesAndCash()
        {
            CareerState state = CareerTestKit.NewCareer();
            RaceSetup setup = CareerTestKit.Enter(state, Difficulty.Hard);
            CareerReport report = Career.Apply(state, CareerTestKit.Result(setup, 2, wrecked: true, kills: 1, cash: 300));
            Assert.IsTrue(report.Wrecked);
            Assert.AreEqual(0, report.Prize);
            Assert.AreEqual(0, report.Points);
            Assert.AreEqual(CareerRules.Bounty(Difficulty.Hard), report.Bounty);
            Assert.AreEqual(300, report.Cash);
            Assert.AreEqual(0, state.wins);
            Assert.AreEqual(1, state.wrecks);
            Assert.IsTrue(report.Patched);
            Assert.AreEqual(CareerRules.MinStartHealth, state.Current.loadout.health, 1e-5f, "towed back and patched up");
        }

        [Test]
        public void TheRivalsOfTheRaceScoreByTheirPlacesAndTheOtherRacesAreSimulated()
        {
            CareerState state = CareerTestKit.NewCareer();
            RaceSetup setup = CareerTestKit.Enter(state, Difficulty.Medium);
            var before = state.rivals.ToDictionary(rival => rival.id, rival => rival.points);
            CareerReport report = Career.Apply(state, CareerTestKit.Result(setup, 4));
            Assert.AreEqual(3, report.Rivals.Count);
            RivalOutcome winner = report.Rivals.First(outcome => outcome.Place == 1);
            Assert.AreEqual(before[winner.Id] + CareerRules.Points(Difficulty.Medium, 1), state.Rival(winner.Id).points);
            Assert.AreEqual(1, state.Rival(winner.Id).wins);
            Assert.AreEqual(2, report.OtherRaces.Count, "the Easy and the Hard race of the day went on without the player");
            foreach (DayRace race in report.OtherRaces)
            {
                Assert.AreNotEqual(Difficulty.Medium, race.Difficulty);
                Assert.AreEqual(4, race.Order.Count, "the three offered and one more of the same third");
                Assert.AreEqual(4, race.Order.Distinct().Count());
            }
            var raced = report.OtherRaces.SelectMany(race => race.Order).Concat(report.Rivals.Select(outcome => outcome.Id)).ToList();
            Assert.AreEqual(raced.Count, raced.Distinct().Count(), "nobody races twice in a day");
            int scored = state.rivals.Sum(rival => rival.points - before[rival.id]);
            Assert.Greater(scored, CareerRules.Points(Difficulty.Medium, 1), "the rivals of the other races scored too");
        }

        [Test]
        public void TheRankMovesAndTracksOpenAsThePlayerClimbs()
        {
            CareerState state = CareerTestKit.NewCareer();
            state.points = 26;
            RaceSetup setup = CareerTestKit.Enter(state, Difficulty.Hard);
            CareerReport report = Career.Apply(state, CareerTestKit.Result(setup, 1));
            Assert.AreEqual(Ladder.Rank(state), report.RankAfter);
            Assert.Less(report.RankAfter, report.RankBefore);
            Assert.Greater(report.RankChange, 0);
            Assert.AreEqual(state.bestRank, report.RankAfter);
            Assert.AreEqual(CareerRules.TracksOpenAt(report.RankAfter), state.tracksOpen);
            Assert.AreEqual(Enumerable.Range(CareerRules.StartTracks, state.tracksOpen - CareerRules.StartTracks), report.TracksOpened);
            Assert.IsNotEmpty(report.TracksOpened);
        }

        [Test]
        public void RivalsWhoScoreEnoughMoveUpATier()
        {
            CareerState state = CareerTestKit.NewCareer();
            RivalState terry = state.Rival("terry");
            terry.points = CareerRules.PointsPerTier - 1;
            // Terry on the first seat of the Easy race behind the player wins it (the player is last).
            RaceOffer easy = state.offers.First(offer => offer.difficulty == Difficulty.Easy);
            easy.rivals.Remove("terry");
            easy.rivals.Insert(0, "terry");
            easy.rivals.RemoveRange(3, easy.rivals.Count - 3);
            RaceSetup setup = CareerTestKit.Enter(state, Difficulty.Easy);
            CareerReport report = Career.Apply(state, CareerTestKit.Result(setup, 4));
            Assert.AreEqual(2, terry.tier);
            Assert.AreEqual(CarModel.Hornet, terry.Car);
            Assert.IsTrue(report.Promotions.Any(promotion => promotion.Id == "terry" && promotion.Car == CarModel.Hornet));
        }

        [Test]
        public void WinningTheShowdownMakesTheChampion()
        {
            CareerState state = CareerTestKit.NewCareer();
            state.points = 999;
            state.offers = RaceOffers.Generate(state, state.day);
            RaceSetup setup = CareerTestKit.Enter(state, Difficulty.Showdown);
            RaceEntrant warden = setup.Entrants.First(entrant => entrant.RivalId == Roster.WardenId);
            Assert.AreEqual(CarModel.Tyrant, warden.Loadout.model);
            Assert.AreEqual(Career.WardenSkill, warden.Skill);
            Assert.AreEqual(Career.WardenPaint, warden.Color);
            CareerReport lost = Career.Apply(state, CareerTestKit.Result(setup, 2));
            Assert.IsTrue(lost.ShowdownLost);
            Assert.IsFalse(state.champion);
            Assert.AreEqual(1, state.showdownsLost);
            Assert.AreEqual(0, lost.Points, "the Showdown scores no points");
            Assert.AreEqual(3, lost.OtherRaces.Count, "every ordinary race of the day went on");

            state.offers = RaceOffers.Generate(state, state.day);
            setup = CareerTestKit.Enter(state, Difficulty.Showdown);
            int money = state.wallet.Money;
            CareerReport won = Career.Apply(state, CareerTestKit.Result(setup, 1));
            Assert.IsTrue(won.Champion);
            Assert.IsTrue(state.champion);
            Assert.AreEqual(won.Day, state.championDay);
            Assert.AreEqual(money + CareerRules.Prize(Difficulty.Showdown, 1), state.wallet.Money);
            Assert.IsFalse(state.offers.Any(offer => offer.IsShowdown), "no second Showdown");
        }

        [Test]
        public void ARaceNotEnteredThroughAnOfferIsStillBooked()
        {
            CareerState state = CareerTestKit.NewCareer();
            RaceSetup setup = Career.Setup(state, state.offers[2], null);
            CareerReport report = Career.Apply(state, CareerTestKit.Result(setup, 1));
            Assert.AreEqual(Difficulty.Hard, report.Difficulty, "matched by difficulty and track");
            Assert.AreEqual(CareerRules.Prize(Difficulty.Hard, 1), report.Prize);
        }

        [Test]
        public void ValidateMendsAnIncompleteSave()
        {
            CareerState state = CareerTestKit.NewCareer();
            state.rivals.RemoveAt(4);
            state.garage.Clear();
            state.offers.Clear();
            state.consumables.Set(CareerShop.MineId, 99);
            state.day = 0;
            state.entered = 1;
            Assert.IsNull(Career.Validate(state));
            Assert.AreEqual(CareerRules.RivalCount, state.rivals.Count);
            Assert.AreEqual(1, state.garage.Count);
            Assert.AreEqual(3, state.offers.Count);
            Assert.AreEqual(CareerRules.MineMax, state.consumables.Get(CareerShop.MineId));
            Assert.AreEqual(1, state.day);
            Assert.AreEqual(-1, state.entered);
            Assert.IsNotNull(Career.Validate(null));
            Assert.IsNotNull(Career.Validate(new CareerState { version = CareerState.CurrentVersion + 1 }));
        }
    }
}
