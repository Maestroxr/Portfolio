using System.Linq;
using Gamebox;
using NUnit.Framework;

namespace Portfolio.BoardGames.Tests
{
    /// <summary>
    /// The local play rules: two seats, computers allowed, the four options with their choices, and the table setup
    /// made from a match for every rule set (rules, who moves first, the clock, the doubling cube and the opening roll,
    /// the seats' names and levels, the sides the seats play and their words).
    /// </summary>
    public class LocalRulesTest
    {
        private static BoardGamesLocalRules Rules => new BoardGamesLocalRules();

        private static LocalMatch Match(BoardGamesLocalRules rules)
        {
            return new LocalMatch(GameType.BoardGames, rules);
        }

        [Test]
        public void TwoSeatsOnePersonAtLeastNoHandOverScreen()
        {
            BoardGamesLocalRules rules = Rules;
            Assert.AreEqual(GameType.BoardGames, rules.Game);
            Assert.AreEqual("Local Play", rules.Title);
            Assert.IsFalse(string.IsNullOrEmpty(rules.Hint));
            Assert.AreEqual(2, rules.MinPlayers);
            Assert.AreEqual(2, rules.MaxPlayers);
            Assert.AreEqual(2, rules.DefaultPlayers);
            Assert.IsTrue(rules.AllowComputers);
            Assert.AreEqual(1, rules.MinHumans);
            Assert.IsFalse(rules.OffersHandOver);
            Assert.AreEqual(RuleSets.ComputerLevels, rules.ComputerLevels.Length);
            Assert.AreEqual(2, rules.SeatColors.Length);
            Assert.AreNotEqual(rules.SeatColors[0], rules.SeatColors[1]);
        }

        [Test]
        public void TheOptionsAreTheRulesWhoMovesFirstTheClockAndTheCube()
        {
            BoardGamesLocalRules rules = Rules;
            CollectionAssert.AreEqual(
                new[] { BoardGamesLocalRules.RuleSetOption, BoardGamesLocalRules.FirstOption, BoardGamesLocalRules.ClockOption, BoardGamesLocalRules.CubeOption },
                rules.Options.Select(option => option.Key).ToArray());
            LocalOption ruleSet = rules.FindOption(BoardGamesLocalRules.RuleSetOption);
            Assert.AreEqual("Rules", ruleSet.Label);
            CollectionAssert.AreEqual(RuleSets.All.Select(RuleSets.NameKey).ToArray(), ruleSet.Values);
            CollectionAssert.AreEqual(new[] { "English draughts", "Russian draughts", "International draughts", "Chess", "Backgammon" }, ruleSet.Values);
            Assert.AreEqual(0, ruleSet.Index);
            LocalOption cube = rules.FindOption(BoardGamesLocalRules.CubeOption);
            Assert.AreEqual("Doubling cube", cube.Label);
            CollectionAssert.AreEqual(new[] { "On", "Off" }, cube.Values);
            Assert.AreEqual(0, cube.Index, "the cube is on by default");
            LocalOption first = rules.FindOption(BoardGamesLocalRules.FirstOption);
            Assert.AreEqual("Who moves first", first.Label);
            CollectionAssert.AreEqual(new[] { "Rules decide", "Player 1", "Player 2" }, first.Values);
            LocalOption clock = rules.FindOption(BoardGamesLocalRules.ClockOption);
            Assert.AreEqual("Time per move", clock.Label);
            Assert.AreEqual(BoardGamesLocalRules.ClockSeconds.Length, clock.Values.Length);
            Assert.AreEqual("None", clock.Values[0]);
            Assert.AreEqual(0, BoardGamesLocalRules.ClockSeconds[0]);
        }

        [Test]
        public void TheRulesDecideWhoMovesFirstUnlessTheSetupSays()
        {
            Assert.AreEqual(1, BoardGamesLocalRules.FirstSeatOf((int)Variant.English, 0), "Dark opens the English game: seat 1 plays Dark");
            Assert.AreEqual(0, BoardGamesLocalRules.FirstSeatOf((int)Variant.Russian, 0));
            Assert.AreEqual(0, BoardGamesLocalRules.FirstSeatOf((int)Variant.International, 0));
            Assert.AreEqual(0, BoardGamesLocalRules.FirstSeatOf((int)Variant.English, 1));
            Assert.AreEqual(1, BoardGamesLocalRules.FirstSeatOf((int)Variant.Russian, 2));
            Assert.AreEqual(Side.Dark, BoardGamesLocalRules.SideOfSeat(1, (int)Variant.English, 0));
            Assert.AreEqual(Side.Light, BoardGamesLocalRules.SideOfSeat(0, (int)Variant.English, 0));
            Assert.AreEqual(Side.Light, BoardGamesLocalRules.SideOfSeat(1, (int)Variant.Russian, 2));
            Assert.AreEqual(Side.Dark, BoardGamesLocalRules.SideOfSeat(0, (int)Variant.Russian, 2));
        }

        [Test]
        public void TheDefaultMatchMakesAnEnglishGameOfTwoPeopleWithoutAClock()
        {
            BoardGamesLocalRules rules = Rules;
            LocalMatch match = Match(rules);
            Assert.AreEqual(2, match.Count);
            Assert.AreEqual(2, match.Humans);
            Assert.IsNull(match.Problem());
            TableSetup setup = BoardGamesLocalRules.SetupOf(match);
            Assert.AreEqual((int)Variant.English, setup.ruleSet);
            Assert.AreEqual(1, setup.firstSeat);
            Assert.AreEqual(0, setup.clockSeconds);
            Assert.AreEqual(2, setup.Humans);
            Assert.AreEqual(2, setup.seats.Length);
            Assert.AreEqual(match[0].Name, setup.seats[0].name);
            Assert.AreEqual(match[1].Name, setup.seats[1].name);
            Assert.IsFalse(setup.seats[0].computer);
            Assert.IsFalse(setup.seats[1].computer);
            Assert.AreEqual(Side.Dark, setup.SideOf(1));
            Assert.AreEqual(Side.Light, setup.SideOf(0));
            Assert.AreEqual(1, setup.SeatOf(Side.Dark));
            Assert.AreEqual(Variant.English, setup.Rules.Variant);
        }

        [Test]
        public void TheOptionsAndTheSeatsReachTheTableSetup()
        {
            BoardGamesLocalRules rules = Rules;
            LocalMatch match = Match(rules);
            match.SetOption(BoardGamesLocalRules.RuleSetOption, (int)Variant.International);
            match.SetOption(BoardGamesLocalRules.FirstOption, 2);
            match.SetOption(BoardGamesLocalRules.ClockOption, 1);
            rules.OptionsChanged(match);
            match[0].Name = "Alice";
            match[1].Kind = SeatKind.Computer;
            match[1].Level = 2;
            Assert.AreEqual(1, match.Humans);
            Assert.IsNull(match.Problem());
            TableSetup setup = BoardGamesLocalRules.SetupOf(match);
            Assert.AreEqual((int)Variant.International, setup.ruleSet);
            Assert.AreEqual(10, setup.Rules.Size);
            Assert.AreEqual(1, setup.firstSeat, "Player 2 moves first");
            Assert.AreEqual(30, setup.clockSeconds);
            Assert.AreEqual(30, BoardGamesLocalRules.ClockSecondsOf(match));
            Assert.AreEqual("Alice", setup.seats[0].name);
            Assert.IsFalse(setup.seats[0].computer);
            Assert.IsTrue(setup.seats[1].computer);
            Assert.AreEqual(2, setup.seats[1].level);
            Assert.AreEqual(1, setup.Humans);
            Assert.AreEqual(Side.Light, setup.SideOf(1), "International opens with Light, and seat 1 opens");
            Assert.AreEqual(Side.Dark, setup.SideOf(0));
            Assert.AreEqual(L.T("Light"), rules.SeatDetail(match, 1));
            Assert.AreEqual(L.T("Dark"), rules.SeatDetail(match, 0));
            Assert.AreNotEqual(match[0].Color, match[1].Color);
        }

        [Test]
        public void TwoComputersAreNotAGame()
        {
            BoardGamesLocalRules rules = Rules;
            LocalMatch match = Match(rules);
            match[0].Kind = SeatKind.Computer;
            match[1].Kind = SeatKind.Computer;
            Assert.AreEqual(0, match.Humans);
            Assert.IsNotNull(match.Problem());
        }

        [Test]
        public void TheSetupRoundTripsThroughItsSavedText()
        {
            BoardGamesLocalRules rules = Rules;
            LocalMatch match = Match(rules);
            match.SetOption(BoardGamesLocalRules.RuleSetOption, (int)Variant.Russian);
            match.SetOption(BoardGamesLocalRules.ClockOption, 3);
            match[0].Name = "Ben";
            LocalMatch again = Match(rules);
            again.Load(match.Save());
            Assert.AreEqual((int)Variant.Russian, again.Option(BoardGamesLocalRules.RuleSetOption));
            Assert.AreEqual(3, again.Option(BoardGamesLocalRules.ClockOption));
            Assert.AreEqual("Ben", again[0].Name);
            TableSetup setup = BoardGamesLocalRules.SetupOf(again);
            Assert.AreEqual(120, setup.clockSeconds);
            Assert.AreEqual(0, setup.firstSeat);
        }

        [Test]
        public void ChessLetsPlayerOnePlayWhiteUnlessTheSetupSays()
        {
            BoardGamesLocalRules rules = Rules;
            LocalMatch match = Match(rules);
            match.SetOption(BoardGamesLocalRules.RuleSetOption, (int)RuleSet.Chess);
            rules.OptionsChanged(match);
            TableSetup setup = BoardGamesLocalRules.SetupOf(match);
            Assert.AreEqual((int)RuleSet.Chess, setup.ruleSet);
            Assert.AreEqual(GameMode.Chess, setup.Mode);
            Assert.AreEqual(0, setup.firstSeat, "White moves first, and player 1 plays White");
            Assert.AreEqual(Side.Light, setup.SideOf(0));
            Assert.AreEqual(L.T("White"), rules.SeatDetail(match, 0));
            Assert.AreEqual(L.T("Black"), rules.SeatDetail(match, 1));
            Assert.AreNotEqual(match[0].Color, match[1].Color);

            match.SetOption(BoardGamesLocalRules.FirstOption, 2);
            rules.OptionsChanged(match);
            setup = BoardGamesLocalRules.SetupOf(match);
            Assert.AreEqual(1, setup.firstSeat, "player 2 plays White and moves first");
            Assert.AreEqual(Side.Dark, setup.SideOf(0));
            Assert.AreEqual(L.T("Black"), rules.SeatDetail(match, 0));
            Assert.AreEqual(L.T("White"), rules.SeatDetail(match, 1));
        }

        [Test]
        public void BackgammonStartsWithTheOpeningRollUnlessASeatIsChosen()
        {
            BoardGamesLocalRules rules = Rules;
            LocalMatch match = Match(rules);
            match.SetOption(BoardGamesLocalRules.RuleSetOption, (int)RuleSet.Backgammon);
            TableSetup setup = BoardGamesLocalRules.SetupOf(match);
            Assert.AreEqual(GameMode.Backgammon, setup.Mode);
            Assert.IsTrue(setup.openingRoll, "the rules decide: the opening roll");
            Assert.IsTrue(setup.doublingCube, "the cube is on by default");
            Assert.AreEqual(0, setup.firstSeat, "player 1 plays White");
            Assert.AreEqual(L.T("White"), rules.SeatDetail(match, 0));

            match.SetOption(BoardGamesLocalRules.FirstOption, 2);
            match.SetOption(BoardGamesLocalRules.CubeOption, 1);
            setup = BoardGamesLocalRules.SetupOf(match);
            Assert.IsFalse(setup.openingRoll, "a chosen seat starts without the opening roll");
            Assert.AreEqual(1, setup.firstSeat, "player 2 plays White and starts");
            Assert.AreEqual(Side.Light, setup.SideOf(1));
            Assert.IsFalse(setup.doublingCube);

            match.SetOption(BoardGamesLocalRules.FirstOption, 1);
            setup = BoardGamesLocalRules.SetupOf(match);
            Assert.IsFalse(setup.openingRoll);
            Assert.AreEqual(0, setup.firstSeat, "player 1 plays White and starts");
        }

        [Test]
        public void EveryRuleSetMakesATableOfItsGame()
        {
            BoardGamesLocalRules rules = Rules;
            foreach (RuleSet ruleSet in RuleSets.All)
            {
                LocalMatch match = Match(rules);
                match.SetOption(BoardGamesLocalRules.RuleSetOption, (int)ruleSet);
                match[1].Kind = SeatKind.Computer;
                match[1].Level = 0;
                TableSetup setup = BoardGamesLocalRules.SetupOf(match);
                Assert.AreEqual((int)ruleSet, setup.ruleSet, ruleSet.ToString());
                Assert.AreEqual(RuleSets.FirstMover(ruleSet), setup.SideOf(setup.firstSeat), ruleSet.ToString());
                Assert.AreEqual(L.T(RuleSets.SideNameKey(ruleSet, setup.SideOf(0))), rules.SeatDetail(match, 0), ruleSet.ToString());
                Assert.IsTrue(setup.seats[1].computer, ruleSet.ToString());
                Assert.AreEqual(0, setup.seats[1].level, ruleSet.ToString());
            }
        }

        [Test]
        public void TheSetupOpensOnTheTitlesGame()
        {
            BoardGamesLocalRules rules = Rules;
            LocalMatch match = Match(rules);
            match.SetOption(BoardGamesLocalRules.RuleSetOption, (int)RuleSet.RussianDraughts);
            Assert.IsFalse(BoardGamesLocalRules.Preselect(match, RuleSet.EnglishDraughts, true), "a draughts setup stays as it is for draughts");
            Assert.AreEqual((int)RuleSet.RussianDraughts, match.Option(BoardGamesLocalRules.RuleSetOption));
            Assert.IsTrue(BoardGamesLocalRules.Preselect(match, RuleSet.Backgammon, false));
            Assert.AreEqual((int)RuleSet.Backgammon, match.Option(BoardGamesLocalRules.RuleSetOption));
            Assert.IsFalse(BoardGamesLocalRules.CubeOf(match), "the settings' cube comes with the game");
            Assert.IsTrue(BoardGamesLocalRules.Preselect(match, RuleSet.Chess, true));
            Assert.AreEqual((int)RuleSet.Chess, match.Option(BoardGamesLocalRules.RuleSetOption));
            Assert.IsFalse(BoardGamesLocalRules.Preselect(null, RuleSet.Chess, true));
        }
    }
}
