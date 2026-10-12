using System.Linq;
using Gamebox;
using NUnit.Framework;

namespace Portfolio.Checkers.Tests
{
    /// <summary>
    /// The local play rules: two seats, computers allowed, the three options with their choices, and the table setup
    /// made from a match (rules, who moves first, the clock, the seats' names and levels, the sides the seats play).
    /// </summary>
    public class LocalRulesTest
    {
        private static CheckersLocalRules Rules => new CheckersLocalRules();

        private static LocalMatch Match(CheckersLocalRules rules)
        {
            return new LocalMatch(GameType.Checkers, rules);
        }

        [Test]
        public void TwoSeatsOnePersonAtLeastNoHandOverScreen()
        {
            CheckersLocalRules rules = Rules;
            Assert.AreEqual(GameType.Checkers, rules.Game);
            Assert.AreEqual("Local Play", rules.Title);
            Assert.IsFalse(string.IsNullOrEmpty(rules.Hint));
            Assert.AreEqual(2, rules.MinPlayers);
            Assert.AreEqual(2, rules.MaxPlayers);
            Assert.AreEqual(2, rules.DefaultPlayers);
            Assert.IsTrue(rules.AllowComputers);
            Assert.AreEqual(1, rules.MinHumans);
            Assert.IsFalse(rules.OffersHandOver);
            Assert.AreEqual(CheckersAI.Levels, rules.ComputerLevels.Length);
            Assert.AreEqual(2, rules.SeatColors.Length);
            Assert.AreNotEqual(rules.SeatColors[0], rules.SeatColors[1]);
        }

        [Test]
        public void TheOptionsAreTheRulesWhoMovesFirstAndTheClock()
        {
            CheckersLocalRules rules = Rules;
            CollectionAssert.AreEqual(new[] { CheckersLocalRules.VariantOption, CheckersLocalRules.FirstOption, CheckersLocalRules.ClockOption },
                rules.Options.Select(option => option.Key).ToArray());
            LocalOption variant = rules.FindOption(CheckersLocalRules.VariantOption);
            Assert.AreEqual("Rules", variant.Label);
            CollectionAssert.AreEqual(VariantRules.All.Select(v => VariantRules.Of(v).NameKey).ToArray(), variant.Values);
            Assert.AreEqual(0, variant.Index);
            LocalOption first = rules.FindOption(CheckersLocalRules.FirstOption);
            Assert.AreEqual("Who moves first", first.Label);
            CollectionAssert.AreEqual(new[] { "Rules decide", "Player 1", "Player 2" }, first.Values);
            LocalOption clock = rules.FindOption(CheckersLocalRules.ClockOption);
            Assert.AreEqual("Time per move", clock.Label);
            Assert.AreEqual(CheckersLocalRules.ClockSeconds.Length, clock.Values.Length);
            Assert.AreEqual("None", clock.Values[0]);
            Assert.AreEqual(0, CheckersLocalRules.ClockSeconds[0]);
        }

        [Test]
        public void TheRulesDecideWhoMovesFirstUnlessTheSetupSays()
        {
            Assert.AreEqual(1, CheckersLocalRules.FirstSeatOf((int)Variant.English, 0), "Dark opens the English game: seat 1 plays Dark");
            Assert.AreEqual(0, CheckersLocalRules.FirstSeatOf((int)Variant.Russian, 0));
            Assert.AreEqual(0, CheckersLocalRules.FirstSeatOf((int)Variant.International, 0));
            Assert.AreEqual(0, CheckersLocalRules.FirstSeatOf((int)Variant.English, 1));
            Assert.AreEqual(1, CheckersLocalRules.FirstSeatOf((int)Variant.Russian, 2));
            Assert.AreEqual(Side.Dark, CheckersLocalRules.SideOfSeat(1, (int)Variant.English, 0));
            Assert.AreEqual(Side.Light, CheckersLocalRules.SideOfSeat(0, (int)Variant.English, 0));
            Assert.AreEqual(Side.Light, CheckersLocalRules.SideOfSeat(1, (int)Variant.Russian, 2));
            Assert.AreEqual(Side.Dark, CheckersLocalRules.SideOfSeat(0, (int)Variant.Russian, 2));
        }

        [Test]
        public void TheDefaultMatchMakesAnEnglishGameOfTwoPeopleWithoutAClock()
        {
            CheckersLocalRules rules = Rules;
            LocalMatch match = Match(rules);
            Assert.AreEqual(2, match.Count);
            Assert.AreEqual(2, match.Humans);
            Assert.IsNull(match.Problem());
            TableSetup setup = CheckersLocalRules.SetupOf(match);
            Assert.AreEqual((int)Variant.English, setup.variant);
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
            CheckersLocalRules rules = Rules;
            LocalMatch match = Match(rules);
            match.SetOption(CheckersLocalRules.VariantOption, (int)Variant.International);
            match.SetOption(CheckersLocalRules.FirstOption, 2);
            match.SetOption(CheckersLocalRules.ClockOption, 1);
            rules.OptionsChanged(match);
            match[0].Name = "Alice";
            match[1].Kind = SeatKind.Computer;
            match[1].Level = 2;
            Assert.AreEqual(1, match.Humans);
            Assert.IsNull(match.Problem());
            TableSetup setup = CheckersLocalRules.SetupOf(match);
            Assert.AreEqual((int)Variant.International, setup.variant);
            Assert.AreEqual(10, setup.Rules.Size);
            Assert.AreEqual(1, setup.firstSeat, "Player 2 moves first");
            Assert.AreEqual(30, setup.clockSeconds);
            Assert.AreEqual(30, CheckersLocalRules.ClockSecondsOf(match));
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
            CheckersLocalRules rules = Rules;
            LocalMatch match = Match(rules);
            match[0].Kind = SeatKind.Computer;
            match[1].Kind = SeatKind.Computer;
            Assert.AreEqual(0, match.Humans);
            Assert.IsNotNull(match.Problem());
        }

        [Test]
        public void TheSetupRoundTripsThroughItsSavedText()
        {
            CheckersLocalRules rules = Rules;
            LocalMatch match = Match(rules);
            match.SetOption(CheckersLocalRules.VariantOption, (int)Variant.Russian);
            match.SetOption(CheckersLocalRules.ClockOption, 3);
            match[0].Name = "Ben";
            LocalMatch again = Match(rules);
            again.Load(match.Save());
            Assert.AreEqual((int)Variant.Russian, again.Option(CheckersLocalRules.VariantOption));
            Assert.AreEqual(3, again.Option(CheckersLocalRules.ClockOption));
            Assert.AreEqual("Ben", again[0].Name);
            TableSetup setup = CheckersLocalRules.SetupOf(again);
            Assert.AreEqual(120, setup.clockSeconds);
            Assert.AreEqual(0, setup.firstSeat);
        }
    }
}
