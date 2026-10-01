using System.Collections.Generic;
using Gamebox;
using NUnit.Framework;

namespace Portfolio.MemoryCards.Tests
{
    /// <summary>
    /// The versus game at one device on the shared local play setup: the rules it offers, the seats the setup makes, the
    /// hand-over screen holding the clock and the clicks, the computer's turns, and how the computer players remember and
    /// choose cards.
    /// </summary>
    public class LocalVersusTest
    {
        private static LocalMatch Setup(int players, bool handOver = false, params int[] computers)
        {
            var setup = new LocalMatch(GameType.MemoryCards, LocalVersus.CreateRules()) { Count = players, HandOver = handOver };
            for (int i = 0; i < players; i++)
            {
                setup[i].Name = $"P{i + 1}";
            }
            foreach (int seat in computers)
            {
                setup[seat].Kind = SeatKind.Computer;
            }
            return setup;
        }

        private static MemoryRound Round(string layout, int matchSize = 2)
        {
            Deal deal = Boards.Make(layout);
            return new MemoryRound(VersusMatch.RulesFor(Boards.Rules(deal.Cards.Count, 4, matchSize)), deal);
        }

        private static MemoryCardsComputer Computer(int memory = int.MaxValue, float slip = 0f, int seed = 7)
        {
            return new MemoryCardsComputer(new MemoryCardsComputer.Skill { Memory = memory, Slip = slip, Interval = 0.5f }, new System.Random(seed));
        }

        [Test]
        public void TheRulesOfferAVersusForTwoToFourWithComputersAndTheClock()
        {
            LocalPlayRules rules = LocalVersus.CreateRules();
            Assert.AreEqual(2, rules.MinPlayers);
            Assert.AreEqual(4, rules.MaxPlayers);
            Assert.IsFalse(rules.Simultaneous);
            Assert.IsTrue(rules.AllowComputers);
            Assert.AreEqual(1, rules.MinHumans, "one person against the computer is a game");
            Assert.AreEqual(MemoryCardsComputer.Levels.Length, rules.ComputerLevels.Length);
            Assert.IsTrue(rules.OffersHandOver);
            Assert.IsFalse(rules.HandOverByDefault, "the board is the same for everybody");
            LocalOption clock = rules.FindOption(LocalVersus.ClockOption);
            Assert.IsNotNull(clock);
            Assert.AreEqual(LocalVersus.ClockSeconds.Length, clock.Values.Length);
            Assert.AreEqual(VersusMatch.DefaultTurnSeconds, LocalVersus.ClockSeconds[clock.Index]);
        }

        [Test]
        public void TheSetupBecomesTheSeatsOfTheGame()
        {
            LocalMatch setup = Setup(3, false, 2);
            setup.SetOption(LocalVersus.ClockOption, LocalVersus.ClockIndex(15f));
            var local = new LocalVersus(setup, new System.Random(1));
            Assert.AreEqual(3, local.Match.Seats.Count);
            Assert.AreEqual("P1", local.Match.Seats[0].Name);
            Assert.AreEqual("P3", local.Match.Seats[2].Name);
            Assert.AreEqual(15f, local.Match.TurnSeconds);
            Assert.IsFalse(local.IsComputer(0));
            Assert.IsTrue(local.IsComputer(2));
            Assert.AreEqual(PlayerControl.Local, local.SeatOf(0).Control);
            Assert.AreEqual(PlayerControl.Computer, local.SeatOf(2).Control);
            Assert.AreEqual(1, new List<MemoryCardsComputer>(local.Computers).Count);
        }

        [Test]
        public void TheClockCanBeOff()
        {
            LocalMatch setup = Setup(2);
            setup.SetOption(LocalVersus.ClockOption, 0);
            var local = new LocalVersus(setup, new System.Random(1));
            Assert.IsFalse(local.Match.HasTurnLimit);
            local.BeginTurn();
            Assert.IsFalse(local.Tick(100f));
        }

        [Test]
        public void TheHandOverHoldsTheClockAndTheClicksUntilThePlayerIsReady()
        {
            var local = new LocalVersus(Setup(2, true), new System.Random(1));
            Assert.IsTrue(local.BeginTurn(), "a person's turn waits behind the curtain");
            Assert.IsFalse(local.TurnReady);
            Assert.IsFalse(local.Tick(VersusMatch.DefaultTurnSeconds + 5f));
            Assert.AreEqual(VersusMatch.DefaultTurnSeconds, local.Match.TurnLeft, "the clock holds behind the curtain");
            Assert.IsFalse(local.Accepts(false), "no clicks behind the curtain");
            local.Ready();
            Assert.IsTrue(local.Accepts(false));
            Assert.IsFalse(local.Tick(5f));
            Assert.AreEqual(VersusMatch.DefaultTurnSeconds - 5f, local.Match.TurnLeft, 0.001f);
            Assert.IsTrue(local.Tick(VersusMatch.DefaultTurnSeconds));
        }

        [Test]
        public void WithoutTheHandOverATurnIsUnderWayAtOnce()
        {
            var local = new LocalVersus(Setup(3), new System.Random(1));
            Assert.IsFalse(local.BeginTurn());
            Assert.IsTrue(local.TurnReady);
            Assert.IsTrue(local.Accepts(false));
            local.Tick(3f);
            Assert.AreEqual(VersusMatch.DefaultTurnSeconds - 3f, local.Match.TurnLeft, 0.001f);
        }

        [Test]
        public void TheComputerNeedsNoCurtainAndPlaysOnlyItsOwnTurn()
        {
            var local = new LocalVersus(Setup(3, true, 1), new System.Random(1));
            local.BeginTurn();
            local.Ready();
            Assert.IsFalse(local.ComputerTurn);
            Assert.IsNull(local.CurrentComputer);
            Assert.IsFalse(local.Accepts(true), "the computer waits for its turn");
            local.Match.PassTurn();
            Assert.IsFalse(local.BeginTurn(), "nobody takes the device for the computer");
            Assert.IsTrue(local.ComputerTurn);
            Assert.IsNotNull(local.CurrentComputer);
            Assert.IsTrue(local.Accepts(true));
            Assert.IsFalse(local.Accepts(false), "people's clicks are ignored on the computer's turn");
            local.Match.PassTurn();
            Assert.IsTrue(local.BeginTurn(), "the next person waits behind the curtain again");
        }

        [Test]
        public void OnePersonAgainstTheComputerIsNeverHandedOver()
        {
            var local = new LocalVersus(Setup(2, true, 1), new System.Random(1));
            Assert.IsFalse(local.HandsOver(0));
            Assert.IsFalse(local.BeginTurn());
        }

        [Test]
        public void TheComputerTakesASetItRemembers()
        {
            MemoryRound round = Round("0 1 2 0 1 2");
            MemoryCardsComputer computer = Computer();
            computer.See(round.Cards[1]);
            computer.See(round.Cards[4]);
            MemoryCard first = computer.Choose(round);
            Assert.IsTrue(first == round.Cards[1] || first == round.Cards[4]);
            round.Flip(first);
            MemoryCard second = computer.Choose(round);
            Assert.AreNotSame(first, second);
            Assert.AreEqual(1, second.Animal);
            Assert.AreEqual(FlipOutcome.Matched, round.Flip(second).Outcome);
        }

        [Test]
        public void TheComputerExploresWhatItHasNotSeenAndFinishesTheSet()
        {
            MemoryRound round = Round("0 1 0 1");
            MemoryCardsComputer computer = Computer();
            computer.See(round.Cards[2]);
            MemoryCard first = computer.Choose(round);
            Assert.IsFalse(computer.Knows(first), "without a set in mind it turns a card it has not seen");
            computer.See(first);
            round.Flip(first);
            MemoryCard second = computer.Choose(round);
            if (first.Animal == 0)
            {
                Assert.AreSame(round.Cards[2], second, "it remembers the partner of the card it turned");
            }
            else
            {
                Assert.IsFalse(computer.Knows(second), "it knows no partner, so it tries a new card");
            }
        }

        [Test]
        public void TheComputerNeverTurnsABombItKnows()
        {
            MemoryRound round = Round("B 0 0");
            MemoryCardsComputer computer = Computer(slip: 1f);
            computer.See(round.Cards[0]);
            for (int i = 0; i < 20; i++)
            {
                Assert.AreNotSame(round.Cards[0], computer.Choose(round));
            }
        }

        [Test]
        public void ASmallMemoryForgetsTheOldestCards()
        {
            MemoryRound round = Round("0 1 2 3 0 1 2 3");
            MemoryCardsComputer computer = Computer(memory: 3);
            for (int i = 0; i < 5; i++)
            {
                computer.See(round.Cards[i]);
            }
            Assert.AreEqual(3, computer.Remembered);
            Assert.IsFalse(computer.Knows(round.Cards[0]));
            Assert.IsFalse(computer.Knows(round.Cards[1]));
            Assert.IsTrue(computer.Knows(round.Cards[4]));
            // Seeing a card again makes it the newest.
            computer.See(round.Cards[2]);
            computer.See(round.Cards[5]);
            Assert.IsTrue(computer.Knows(round.Cards[2]));
            Assert.IsFalse(computer.Knows(round.Cards[3]));
        }

        [Test]
        public void ComputersPlayAWholeBoardToTheEnd()
        {
            MemoryRound round = Round("0 1 2 3 4 5 0 1 2 3 4 5 P W");
            var local = new LocalVersus(Setup(3, true, 1, 2), new System.Random(3));
            VersusMatch match = local.Match;
            // The person's turns are played by a computer with a perfect memory, standing in for them here.
            MemoryCardsComputer person = Computer(seed: 11);
            int flips = 0;
            while (!round.IsOver && flips < 400)
            {
                if (round.MismatchShowing)
                {
                    round.HideMismatch();
                    match.PassTurn();
                    continue;
                }
                MemoryCardsComputer player = local.CurrentComputer ?? person;
                MemoryCard card = player.Choose(round);
                Assert.IsNotNull(card);
                FlipResult result = round.Flip(card);
                flips++;
                if (result.Outcome != FlipOutcome.Cracked)
                {
                    local.See(card);
                    person.See(card);
                }
                match.Apply(result, round);
            }
            Assert.IsTrue(round.IsCleared, $"the board is cleared in {flips} flips");
            int sets = 0;
            foreach (VersusSeat seat in match.Seats)
            {
                sets += seat.Sets;
            }
            Assert.AreEqual(round.Sets, sets);
        }
    }
}
