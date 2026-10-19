using System.Collections.Generic;
using System.Linq;
using Gamebox;
using NUnit.Framework;

namespace Portfolio.DeathRace.Tests
{
    /// <summary>
    /// Careers of every league: the start, the roster and its champion, the rivals' cars and loadouts by the league's
    /// schedule, the tracks and the Showdown, special weapons booked off the inventory, moving up after a title, and the
    /// first league being the career it always was.
    /// </summary>
    public sealed class LeagueCareerTest
    {
        private static CareerState NewCareer(LeagueKind league)
        {
            return Career.New("Tester", 0, CareerTestKit.Seed, league);
        }

        [Test]
        public void TheFirstLeagueIsTheCareerAsItWas()
        {
            LeagueSpec wasteland = Leagues.Wasteland;
            Assert.AreEqual(CarModel.Rustbucket, wasteland.StartCar);
            Assert.AreEqual(1500, wasteland.StartMoney);
            Assert.AreEqual(new[] { 1000, 550, 250, 50 }, wasteland.PrizeTable[0]);
            Assert.AreEqual(new[] { 3500, 2000, 700, 150 }, wasteland.PrizeTable[1]);
            Assert.AreEqual(new[] { 7500, 4200, 1800, 500 }, wasteland.PrizeTable[2]);
            Assert.AreEqual(30000, wasteland.PrizeTable[3][0]);
            Assert.AreEqual(new[] { 250, 500, 1200, 3000 }, wasteland.BountyTable);
            Assert.AreEqual(new[] { 0, 1, 2, 3, 4, 5, 6, 7 }, wasteland.Tracks);
            Assert.AreEqual(8, wasteland.ShowdownTrack);
            Assert.AreEqual(TrackLayouts.Of(8).Laps, wasteland.ShowdownLaps, "the Showdown on The Crucible's own laps");
            Assert.AreSame(Roster.Rivals, wasteland.Rivals);
            Assert.AreSame(Roster.Warden, wasteland.Champion);

            CareerState old = Career.New("Tester", 0, CareerTestKit.Seed);
            CareerState league = NewCareer(LeagueKind.Wasteland);
            Assert.AreEqual(LeagueKind.Wasteland, old.League.Kind, "a career without a league is of the first");
            Assert.AreEqual(old.offers.Select(offer => $"{offer.difficulty}{offer.track}{offer.laps}{offer.seed}{string.Join(",", offer.rivals)}"),
                league.offers.Select(offer => $"{offer.difficulty}{offer.track}{offer.laps}{offer.seed}{string.Join(",", offer.rivals)}"));

            // The first league's rivals keep the kit they always had: no fittings, guns or nitro system before the Showdown.
            foreach (Difficulty difficulty in new[] { Difficulty.Easy, Difficulty.Medium, Difficulty.Hard })
            {
                for (int day = 1; day <= 80; day += 7)
                {
                    CarLoadout car = Career.AiLoadout(wasteland, 5, "vex", day, difficulty, null);
                    Assert.AreEqual(GunKind.MachineGun, car.gun, $"day {day} {difficulty}");
                    Assert.AreEqual(0f, Career.ExtraShare(car), $"day {day} {difficulty}");
                    Assert.AreEqual(difficulty == Difficulty.Hard ? 1 : 0, car.Specials(car.special), $"day {day} {difficulty} specials");
                }
            }
        }

        [TestCase(LeagueKind.Iron)]
        [TestCase(LeagueKind.Hellfire)]
        public void ANewCareerOfALeagueHasItsKitRosterAndTracks(LeagueKind kind)
        {
            CareerState state = NewCareer(kind);
            LeagueSpec league = Leagues.Of(kind);
            Assert.AreEqual((int)kind, state.league);
            Assert.AreSame(league, state.League);
            Assert.AreEqual(league.StartCar, state.car);
            Assert.AreEqual(league.StartMoney, state.wallet.Money);
            Assert.AreEqual(league.StartMines, state.consumables.Get(CareerShop.MineId));
            Assert.AreEqual(league.StartRockets, state.consumables.Get(CareerShop.RocketId));
            Assert.AreEqual(league.StartNitro, state.consumables.Get(CareerShop.NitroId));
            Assert.AreEqual(league.StartOil, state.consumables.Get(CareerShop.SpecialId(SpecialKind.Oil)));
            Assert.AreEqual(league.StartEmp, state.consumables.Get(CareerShop.SpecialId(SpecialKind.Emp)));
            Assert.AreEqual(league.StartMortar, state.consumables.Get(CareerShop.SpecialId(SpecialKind.Mortar)));
            Assert.Greater(state.consumables.Get(CareerShop.SpecialId(state.armed)), 0, "the special armed first is one the career has");
            Assert.AreEqual(league.Rivals.Select(driver => driver.Id), state.rivals.Select(rival => rival.id));
            for (int i = 0; i < league.Rivals.Length; i++)
            {
                Assert.AreEqual(CareerRules.StartTier(league, i), state.rivals[i].tier);
                Assert.AreEqual(CareerRules.StartPoints(league, i), state.rivals[i].points);
            }
            Assert.AreEqual(CareerRules.LadderSize, Ladder.Rank(state), "the newcomer starts at the bottom");
            Assert.AreEqual(league.StartTracks, state.tracksOpen);
            Assert.AreEqual(league.Tracks.Take(league.StartTracks), state.OpenTracks);
            int[] colors = state.rivals.Select(rival => rival.colorIndex).Append(state.colorIndex).ToArray();
            Assert.AreEqual(CareerRules.LadderSize, colors.Distinct().Count(), "nobody shares a colour");
        }

        [Test]
        public void RivalIdsAreUniqueAcrossTheLeagues()
        {
            var ids = new List<string>();
            foreach (LeagueSpec league in Leagues.All)
            {
                Assert.AreEqual(CareerRules.RivalCount, league.Rivals.Length, league.Name);
                Assert.AreEqual(CareerRules.RivalCount, league.StartTiers.Length, league.Name);
                ids.AddRange(league.Rivals.Select(driver => driver.Id));
                ids.Add(league.Champion.Id);
                Assert.AreSame(league, Leagues.OfDriver(league.Champion.Id));
                Assert.IsTrue(league.StartTiers.All(tier => tier >= 1 && tier <= league.MaxRivalTier), league.Name);
                Assert.Greater(CarSpecs.Of(league.ChampionCar).Tier, league.MaxRivalTier, league.Name + ": the champion drives a tier above everyone");
                Assert.IsTrue(league.Tracks.All(track => track >= 0 && track < 12 && track != league.ShowdownTrack), league.Name);
                Assert.AreEqual(league.Tracks.Length, league.Tracks.Distinct().Count(), league.Name);
                Assert.AreEqual(league.Tracks.Length, league.StartTracks + league.TrackRanks.Length, league.Name + ": every track is opened by a rank");
            }
            Assert.AreEqual(ids.Count, ids.Distinct().Count());
            Assert.IsTrue(Leagues.Iron.Tracks.Contains(9) && Leagues.Iron.Tracks.Contains(10) && Leagues.Iron.Tracks.Contains(11), "the new tracks");
        }

        [TestCase(LeagueKind.Wasteland)]
        [TestCase(LeagueKind.Iron)]
        [TestCase(LeagueKind.Hellfire)]
        public void RivalsDriveACarOfTheirOwnInTheLadderTheOffersAndTheRace(LeagueKind kind)
        {
            CareerState state = NewCareer(kind);
            foreach (RivalState rival in state.rivals)
            {
                Assert.AreEqual(CarSpecs.ForDriver(rival.tier, rival.id), rival.Car);
                Assert.AreEqual(rival.tier, CarSpecs.Of(rival.Car).Tier);
            }
            foreach (LadderEntry entry in Ladder.Entries(state).Where(entry => !entry.IsPlayer))
            {
                Assert.AreEqual(state.Rival(entry.Id).Car, entry.Car);
            }
            for (int i = 0; i < state.offers.Count; i++)
            {
                RaceSetup setup = Career.Setup(state, state.offers[i], null);
                Assert.AreEqual(CareerRules.Bounty(state.League, state.offers[i].difficulty), setup.Bounty, "the HUD shows the league's bounty");
                foreach (RaceEntrant entrant in setup.Entrants.Skip(1))
                {
                    Assert.AreEqual(state.Rival(entrant.RivalId).Car, entrant.Loadout.model, entrant.RivalId);
                }
            }
            Assert.IsTrue(Leagues.All.SelectMany(league => league.Rivals).Any(driver => CarSpecs.ForDriver(2, driver.Id) == CarModel.Jackal),
                "some drivers choose the other car of a tier");
        }

        [Test]
        public void TheLaterLeaguesRivalsFightWithEverythingTheShopSells()
        {
            LeagueSpec iron = Leagues.Iron;
            CarLoadout hard = Career.AiLoadout(iron, 6, "kerosene", 1, Difficulty.Hard, null);
            Assert.AreEqual(GunKind.Flamethrower, hard.gun, "Kid Kerosene mounts his flamethrower from the first day");
            Assert.AreEqual(CareerRules.AiSpecials(iron, Difficulty.Hard), hard.Specials(hard.special));
            Assert.AreEqual(Career.SpecialOf("kerosene"), hard.special);
            Assert.AreEqual(CareerRules.AiLevel(iron, 1, Difficulty.Hard), hard.boost, "the nitro system with the other parts");
            Assert.AreEqual(CareerRules.AiLevel(iron, 1, Difficulty.Hard), hard.gunMount);
            Assert.IsTrue(hard.spikes, "the spiked bumper from the first day");
            CarLoadout early = Career.AiLoadout(iron, 6, "kerosene", 1, Difficulty.Easy, null);
            Assert.AreEqual(GunKind.MachineGun, early.gun, "an Easy race's cars mount their guns later");
            CarLoadout late = Career.AiLoadout(iron, 6, "kerosene", 40, Difficulty.Easy, null);
            Assert.AreEqual(GunKind.Flamethrower, late.gun);
            int fittings = CareerRules.LaterFittings.Count(kind => late.Has(kind));
            Assert.AreEqual(CareerRules.AiFittings(iron, 40, Difficulty.Easy), fittings, "the later fittings one by one");
            Assert.AreEqual(Career.FittingOrder("kerosene").Take(fittings), CareerRules.LaterFittings.Where(kind => late.Has(kind)).OrderBy(kind =>
                Career.FittingOrder("kerosene").IndexOf(kind)), "in the driver's own order");
            Assert.AreEqual(GunKind.Minigun, Career.GunOf("surgeon", 3), "the plasma cannon waits for a better car");
            Assert.AreEqual(GunKind.Plasma, Career.GunOf("surgeon", 6));
            Assert.AreEqual(Career.AiLoadout(iron, 6, "ray", 9, Difficulty.Medium, null).gun, Career.AiLoadout(iron, 6, "ray", 30, Difficulty.Medium, null).gun,
                "a driver keeps their gun");
        }

        [TestCase(LeagueKind.Wasteland)]
        [TestCase(LeagueKind.Iron)]
        [TestCase(LeagueKind.Hellfire)]
        public void TheChampionWaitsAtTheTopInTheBestCarOfTheLeague(LeagueKind kind)
        {
            LeagueSpec league = Leagues.Of(kind);
            CarLoadout champion = Career.ChampionLoadout(league);
            Assert.AreEqual(league.ChampionCar, champion.model);
            foreach (UpgradeKind upgrade in CareerRules.Upgrades)
            {
                Assert.AreEqual(champion.Spec.MaxLevelOf(upgrade), champion.Level(upgrade), upgrade.ToString());
            }
            Assert.IsTrue(CareerRules.Fittings.All(champion.Has), "every fitting");
            Assert.AreEqual(league.ChampionGun, champion.gun);
            Assert.AreEqual(league.ChampionSpecial, champion.special);
            Assert.IsTrue(Specials.All.All(special => champion.Specials(special.Kind) > 0), "every special weapon");
            Assert.AreEqual(1f, Career.CarRating(league, champion), 1e-4f);

            CareerState state = NewCareer(kind);
            state.points = 9999;
            state.offers = RaceOffers.Generate(state, state.day);
            RaceOffer showdown = state.offers.Single(offer => offer.IsShowdown);
            Assert.AreEqual(league.ShowdownTrack, showdown.track);
            Assert.AreEqual(league.ShowdownLaps, showdown.laps);
            Assert.AreEqual(league.Champion.Id, showdown.rivals[0]);
            Assert.AreEqual(3, showdown.rivals.Count, "the champion and the two best rivals");
            RaceSetup setup = Career.Enter(state, state.offers.IndexOf(showdown), null);
            RaceEntrant entrant = setup.Entrants.First(racer => racer.RivalId == league.Champion.Id);
            Assert.AreEqual(league.Champion.Name, entrant.Name);
            Assert.AreEqual(league.ChampionPaint, entrant.Color);
            Assert.AreEqual(Career.ChampionSkill, entrant.Skill);
            Assert.AreEqual(league.ChampionCar, entrant.Loadout.model);
            CareerReport won = Career.Apply(state, CareerTestKit.Result(setup, 1));
            Assert.IsTrue(won.Champion);
            Assert.AreEqual(kind, won.League);
            Assert.AreEqual(CareerRules.Prize(league, Difficulty.Showdown, 1), won.Prize);
        }

        [Test]
        public void TheOffersOfALeagueAreOnItsOpenTracks()
        {
            CareerState state = NewCareer(LeagueKind.Iron);
            LeagueSpec league = state.League;
            foreach (int open in new[] { league.StartTracks, 8, league.Tracks.Length })
            {
                state.tracksOpen = open;
                List<int> tracks = league.Tracks.Take(open).ToList();
                for (int day = 1; day <= 25; day++)
                {
                    List<RaceOffer> offers = RaceOffers.Generate(state, day);
                    Assert.AreEqual(3, offers.Select(offer => offer.track).Distinct().Count());
                    foreach (RaceOffer offer in offers)
                    {
                        Assert.IsTrue(tracks.Contains(offer.track), $"{offer.track} is open with {open}");
                        Assert.AreEqual(CareerRules.Laps(offer.difficulty, TrackLayouts.Of(offer.track).Laps), offer.laps);
                    }
                    int hard = offers.First(offer => offer.difficulty == Difficulty.Hard).track;
                    Assert.IsTrue(tracks.Skip(open - 3).Contains(hard), "a Hard race is on a track opened last");
                }
            }
        }

        [Test]
        public void TheLadderOpensTheLeaguesTracksInItsOrder()
        {
            CareerState state = NewCareer(LeagueKind.Hellfire);
            LeagueSpec league = state.League;
            state.points = 9999;
            RaceSetup setup = CareerTestKit.Enter(state, Difficulty.Hard);
            CareerReport report = Career.Apply(state, CareerTestKit.Result(setup, 1));
            Assert.AreEqual(1, report.RankAfter);
            Assert.AreEqual(league.Tracks.Length, state.tracksOpen, "at the top every track of the league is open");
            Assert.AreEqual(league.Tracks.Skip(league.StartTracks), report.TracksOpened);
            Assert.AreEqual(league.Tracks, state.OpenTracks);
            Assert.AreEqual(league.TrackRanks[0], CareerRules.RankOpening(league, league.Tracks[league.StartTracks]));
            Assert.AreEqual(0, CareerRules.RankOpening(league, league.Tracks[0]));
        }

        [Test]
        public void TheSpecialWeaponsUsedAreTakenOffTheInventory()
        {
            CareerState state = NewCareer(LeagueKind.Hellfire);
            state.consumables.Set(CareerShop.SpecialId(SpecialKind.Emp), 3);
            state.armed = SpecialKind.Emp;
            RaceSetup setup = CareerTestKit.Enter(state, Difficulty.Medium);
            CarLoadout player = setup.EntrantAt(0).Loadout;
            Assert.AreEqual(Leagues.Hellfire.StartOil, player.oil, "the player carries every special weapon the career has");
            Assert.AreEqual(3, player.emp);
            Assert.AreEqual(Leagues.Hellfire.StartMortar, player.mortar);
            Assert.AreEqual(SpecialKind.Emp, player.special, "the one armed first");
            RaceResult result = CareerTestKit.Result(setup, 2);
            RacerResult racer = result.Racers.First(entry => entry.Driver == DriverKind.Human);
            racer.OilUsed = 1;
            racer.EmpUsed = 2;
            racer.MortarUsed = 1;
            CareerReport report = Career.Apply(state, result);
            Assert.AreEqual(1, report.OilUsed);
            Assert.AreEqual(2, report.EmpUsed);
            Assert.AreEqual(1, report.MortarUsed);
            Assert.AreEqual(2, report.SpecialsUsed(SpecialKind.Emp));
            Assert.AreEqual(Leagues.Hellfire.StartOil - 1, state.consumables.Get(CareerShop.SpecialId(SpecialKind.Oil)));
            Assert.AreEqual(1, state.consumables.Get(CareerShop.SpecialId(SpecialKind.Emp)));
            Assert.AreEqual(Leagues.Hellfire.StartMortar - 1, state.consumables.Get(CareerShop.SpecialId(SpecialKind.Mortar)));
            Assert.AreEqual(CareerRules.Prize(Leagues.Hellfire, Difficulty.Medium, 2), report.Prize, "the league's purse");
            Assert.AreEqual(CareerRules.Bounty(Leagues.Hellfire, Difficulty.Medium), report.BountyEach);
        }

        [Test]
        public void AChampionMovesUpToTheNextLeagueAsTheSameDriver()
        {
            CareerState wasteland = Career.New("Ada", 7, 11);
            CareerState iron = Career.MoveUp(wasteland, 12);
            Assert.AreEqual(LeagueKind.Iron, iron.League.Kind);
            Assert.AreEqual("Ada", iron.driverName);
            Assert.AreEqual(7, iron.colorIndex);
            Assert.AreEqual(Leagues.Iron.StartCar, iron.car);
            Assert.AreEqual(1, iron.day);
            Assert.IsFalse(iron.champion);
            CareerState hellfire = Career.MoveUp(iron, 13);
            Assert.AreEqual(LeagueKind.Hellfire, hellfire.League.Kind);
            Assert.IsNull(Career.MoveUp(hellfire, 14), "there is no league after the last");
            Assert.AreSame(Leagues.Iron, Leagues.Wasteland.Next);
            Assert.AreSame(Leagues.Iron, Leagues.Hellfire.Previous);
            Assert.IsNull(Leagues.Hellfire.Next);
        }

        [Test]
        public void ALeagueOpensWithTheTitleOfTheOneBefore()
        {
            var progress = new DeathRaceProgress(new TransientStrategy());
            Assert.IsTrue(progress.IsLeagueOpen(0));
            Assert.IsFalse(progress.IsLeagueOpen(1));
            Assert.IsFalse(progress.IsLeagueOpen(2));
            progress.WinLeague(0);
            Assert.IsTrue(progress.IsLeagueOpen(1));
            Assert.IsFalse(progress.IsLeagueOpen(2));
            Assert.AreEqual(1, progress.ChampionshipsWon);
            progress.WinLeague(1);
            Assert.IsTrue(progress.IsLeagueOpen(2));
            Assert.AreEqual(1, progress.LeagueTitles(1));
            Assert.AreEqual(2, progress.ChampionshipsWon);
            foreach (int track in Leagues.Iron.Tracks.Take(Leagues.Iron.StartTracks))
            {
                progress.OpenCareerTrack(track);
            }
            Assert.IsTrue(progress.IsCareerTrackOpen(Leagues.Iron.Tracks[Leagues.Iron.StartTracks - 1]));
        }
    }
}
