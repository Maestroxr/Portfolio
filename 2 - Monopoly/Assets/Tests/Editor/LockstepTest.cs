using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using static Portfolio.Monopoly.Tests.Fixture;

namespace Portfolio.Monopoly.Tests
{
    /// <summary>
    /// What an online match stands on: commands that survive the action log as text, random numbers that are the same
    /// everywhere, tables that stay in step when they are fed the same log, and the default move of the server's clock.
    /// </summary>
    public class LockstepTest
    {
        private static IEnumerable<TestCaseData> RuleSets()
        {
            yield return new TestCaseData(new RuleSet()).SetName("classic");
            yield return new TestCaseData(new RuleSet { speedDie = true }).SetName("speed die");
            yield return new TestCaseData(new RuleSet { dealtProperties = 2, housesForHotel = 3, bankruptciesToEnd = 2 }).SetName("quick deal");
            yield return new TestCaseData(new RuleSet { freeParkingJackpot = true, jackpotSeed = 100, doubleSalaryOnGo = true, partyCards = true, auctions = false })
                .SetName("house rules");
            yield return new TestCaseData(new RuleSet { roundLimit = 20, speedDie = true, startingCash = 2000 }).SetName("tycoon rush");
        }

        private static LockstepMatch Table(RuleSet rules, uint seed, int players = 4, bool bots = true)
        {
            var table = new LockstepMatch(WorldTourBoard.Create(), rules, seed);
            for (int seat = 0; seat < players; seat++)
            {
                table.Match.AddPlayer($"P{seat}", seat, bots, (BotLevel)(seat % 3));
            }
            table.Match.Start(1);
            return table;
        }

        /// <summary>What the log does to a command: only the seat, the kind and the text arrive.</summary>
        private static MatchCommand ThroughTheLog(MatchCommand command)
        {
            return MatchCommand.Parse(command.seat, (uint)command.kind, command.Payload());
        }

        // ------------------------------------------------------------------ commands as text

        [Test]
        public void CommandsSurviveTheLog()
        {
            foreach (CommandKind kind in System.Enum.GetValues(typeof(CommandKind)))
            {
                if (kind == CommandKind.ProposeTrade)
                {
                    continue;
                }
                MatchCommand sent = MatchCommand.Of(kind, 2, 37);
                MatchCommand arrived = ThroughTheLog(sent);
                Assert.IsNotNull(arrived, kind.ToString());
                Assert.AreEqual(kind, arrived.kind);
                Assert.AreEqual(2, arrived.seat);
                // Commands without an argument carry none.
                Assert.AreEqual(sent.Payload().Length > 0 ? 37 : 0, arrived.argument, kind.ToString());
            }
            Assert.AreEqual(-5, ThroughTheLog(MatchCommand.Of(CommandKind.Bid, 0, -5)).argument, "A number keeps its sign");
        }

        [Test]
        public void TradeOffersSurviveTheLog()
        {
            var offer = new TradeOffer { from = 1, to = 3, giveCash = 120, getCash = 0, giveJailCards = 1, getJailCards = 2 };
            offer.giveSpaces.AddRange(new[] { Damrak, Kalverstraat });
            offer.getSpaces.Add(Champs);
            MatchCommand arrived = ThroughTheLog(MatchCommand.Propose(offer));
            Assert.IsNotNull(arrived);
            Assert.AreEqual(CommandKind.ProposeTrade, arrived.kind);
            Assert.AreEqual(JsonUtility.ToJson(offer), JsonUtility.ToJson(arrived.offer));

            var cashOnly = new TradeOffer { from = 0, to = 1, getCash = 50 };
            Assert.AreEqual(JsonUtility.ToJson(cashOnly), JsonUtility.ToJson(ThroughTheLog(MatchCommand.Propose(cashOnly)).offer));
        }

        [Test]
        public void GarbageIsNotACommand()
        {
            Assert.IsNull(MatchCommand.Parse(0, 99, ""), "An unknown kind");
            Assert.IsNull(MatchCommand.Parse(0, uint.MaxValue, ""), "The timeout is no command of a player");
            Assert.IsNull(MatchCommand.Parse(-1, (uint)CommandKind.Roll, ""), "No seat");
            Assert.IsNull(MatchCommand.Parse(0, (uint)CommandKind.Bid, "a lot"), "An amount that is no number");
            Assert.IsNull(MatchCommand.Parse(0, (uint)CommandKind.Build, ""), "A missing space");
            Assert.IsNull(MatchCommand.Parse(0, (uint)CommandKind.ProposeTrade, "1|2|3"), "Half an offer");
            Assert.IsNull(MatchCommand.Parse(0, (uint)CommandKind.ProposeTrade, "1|0|0|0|0|6,x|"), "A space that is no number");
            Assert.IsNotNull(MatchCommand.Parse(0, (uint)CommandKind.Roll, "whatever"), "Text nobody reads does no harm");

            // Numbers that are no spaces reach the engine as refusals, not as exceptions.
            LockstepMatch table = Table(new RuleSet(), 5, 2, false);
            int waiting = table.Waiting;
            foreach (CommandKind kind in new[] { CommandKind.Build, CommandKind.Sell, CommandKind.Mortgage, CommandKind.Unmortgage, CommandKind.ChooseDestination })
            {
                Assert.IsFalse(table.Apply(MatchCommand.Of(kind, waiting, 4000), 1), kind.ToString());
                Assert.IsFalse(table.Apply(MatchCommand.Of(kind, waiting, -3), 1), kind.ToString());
            }
            Assert.IsFalse(table.Apply(MatchCommand.Of(CommandKind.Roll, 17), 1), "A seat nobody sits at");
            Assert.IsFalse(table.Apply(null, 1));
        }

        // ------------------------------------------------------------------ random numbers

        [Test]
        public void TheSeededRandomIsReproducible()
        {
            var first = new SeededRandom(12345);
            var second = new SeededRandom(12345);
            var other = new SeededRandom(12346);
            var numbers = new List<int>();
            bool differs = false;
            for (int i = 0; i < 500; i++)
            {
                int number = first.Range(1, 7);
                numbers.Add(number);
                Assert.AreEqual(number, second.Range(1, 7));
                differs |= number != other.Range(1, 7);
                Assert.That(number, Is.InRange(1, 6));
            }
            Assert.IsTrue(differs, "A neighbouring seed gives other dice");
            for (int face = 1; face <= 6; face++)
            {
                Assert.That(numbers.Count(n => n == face), Is.InRange(50, 120), $"The die favours or avoids {face}");
            }

            first.Reseed(7);
            second.Reseed(7);
            for (int i = 0; i < 100; i++)
            {
                double value = first.Value();
                Assert.AreEqual(value, second.Value());
                Assert.That(value, Is.GreaterThanOrEqualTo(0.0).And.LessThan(1.0));
            }
            Assert.AreEqual(3, new SeededRandom(1).Range(3, 3), "An empty range gives its start");

            // The numbers themselves are part of the protocol: every build of the game has to roll these.
            var pinned = new SeededRandom(2024);
            CollectionAssert.AreEqual(new[] { 1, 4, 3, 4, 3, 1, 5, 1 }, Enumerable.Range(0, 8).Select(_ => pinned.Range(1, 7)).ToArray());
        }

        // ------------------------------------------------------------------ tables in step

        [TestCaseSource(nameof(RuleSets))]
        public void TwoTablesFedTheSameLogStayInStep(RuleSet rules)
        {
            int finished = 0;
            for (uint game = 0; game < 3; game++)
            {
                LockstepMatch host = Table(rules, 500 + game);
                LockstepMatch guest = Table(rules, 500 + game);
                Assert.AreEqual(JsonUtility.ToJson(host.Match), JsonUtility.ToJson(guest.Match), "The same seed deals the same start");
                // Only the host thinks for the computer players; the guest sees nothing but the log.
                var brain = new BotBrain(new SystemRandom((int)game));
                var server = new SystemRandom(77 + (int)game);
                int proposedTurn = -1;
                int entries = 0;
                int trades = 0;
                int timeouts = 0;
                while (!host.Match.IsOver && entries < 30000 && host.Match.turn <= 1500)
                {
                    entries++;
                    uint stamp = (uint)server.Range(0, int.MaxValue);
                    if (entries % 23 == 0)
                    {
                        // Now and then the clock runs out instead.
                        List<MatchCommand> made = host.Timeout(stamp);
                        CollectionAssert.AreEqual(made.Select(c => c.ToString()), guest.Timeout(stamp).Select(c => c.ToString()));
                        Assert.IsNotEmpty(made, $"The default move got stuck in {host.Match.phase}");
                        timeouts++;
                    }
                    else
                    {
                        MatchCommand command = null;
                        if (host.PendingOffer != null)
                        {
                            bool accept = brain.WouldAccept(host.Match, host.PendingOffer);
                            if (!accept)
                            {
                                brain.Refused(host.Match, host.PendingOffer);
                            }
                            command = MatchCommand.Answer(host.PendingOffer.to, accept);
                            trades += accept ? 1 : 0;
                        }
                        else if (host.Match.phase == MatchPhase.Roll && proposedTurn != host.Match.turn)
                        {
                            proposedTurn = host.Match.turn;
                            TradeOffer offer = brain.ProposeTrade(host.Match, host.Match.current);
                            command = offer != null ? MatchCommand.Propose(offer) : null;
                        }
                        command ??= brain.Decide(host.Match);
                        Assert.IsNotNull(command, $"The computer player has no move in {host.Match.phase}");
                        bool accepted = host.Apply(ThroughTheLog(command), stamp);
                        Assert.AreEqual(accepted, guest.Apply(ThroughTheLog(command), stamp));
                        Assert.IsTrue(accepted, $"{command} was refused in {host.Match.phase}");
                    }
                    CollectionAssert.AreEqual(host.Match.TakeEvents().Select(e => e.ToString()), guest.Match.TakeEvents().Select(e => e.ToString()));
                    Assert.AreEqual(host.Checksum(), guest.Checksum(), $"Out of step after {entries} entries");
                    if (entries % 50 == 0)
                    {
                        Assert.AreEqual(JsonUtility.ToJson(host.Match), JsonUtility.ToJson(guest.Match), $"Out of step after {entries} entries");
                        CheckInvariants(host.Match);
                    }
                }
                Assert.AreEqual(JsonUtility.ToJson(host.Match), JsonUtility.ToJson(guest.Match));
                Assert.AreEqual(host.Applied, guest.Applied);
                CheckInvariants(guest.Match);
                finished += host.Match.IsOver ? 1 : 0;
                TestContext.WriteLine($"game {game}: {entries} entries, {timeouts} timeouts, {trades} trades, over {host.Match.IsOver}, checksum {host.Checksum():X8}");
            }
            Assert.GreaterOrEqual(finished, 2, "Most matches should come to an end");
        }

        [Test]
        public void TheChecksumTellsTablesApart()
        {
            LockstepMatch one = Table(new RuleSet(), 9);
            LockstepMatch two = Table(new RuleSet(), 9);
            Assert.AreEqual(one.Checksum(), two.Checksum());
            one.Match.players[2].cash += 1;
            Assert.AreNotEqual(one.Checksum(), two.Checksum());
            Assert.AreNotEqual(Table(new RuleSet(), 10).Checksum(), two.Checksum(), "Another seed shuffles the decks another way");
        }

        [Test]
        public void ARefusedActionChangesNothing()
        {
            LockstepMatch table = Table(new RuleSet(), 11, 3, false);
            uint before = table.Checksum();
            int other = (table.Waiting + 1) % 3;
            Assert.IsFalse(table.Apply(MatchCommand.Of(CommandKind.Roll, other), 1), "It is not their turn");
            Assert.IsFalse(table.Apply(MatchCommand.Of(CommandKind.Buy, table.Waiting), 2), "There is nothing to buy");
            Assert.IsFalse(table.Apply(MatchCommand.Of(CommandKind.AnswerTrade, other, 1), 3), "There is no offer");
            Assert.AreEqual(before, table.Checksum());
            Assert.AreEqual(3, table.Applied, "Refused entries of the log count as taken");
        }

        // ------------------------------------------------------------------ trades

        private static LockstepMatch TradingTable(out TradeOffer offer)
        {
            LockstepMatch table = Table(new RuleSet(), 21, 3, false);
            int from = table.Match.current;
            int to = (from + 1) % 3;
            Give(table.Match, from, Damrak);
            Give(table.Match, to, Champs);
            offer = new TradeOffer { from = from, to = to, giveCash = 100 };
            offer.giveSpaces.Add(Damrak);
            offer.getSpaces.Add(Champs);
            return table;
        }

        [Test]
        public void AnOfferHoldsTheTableUntilItIsAnswered()
        {
            LockstepMatch table = TradingTable(out TradeOffer offer);
            int from = offer.from;
            int to = offer.to;
            Assert.IsTrue(table.CanManage(from));
            Assert.IsTrue(table.Apply(ThroughTheLog(MatchCommand.Propose(offer)), 1));
            Assert.AreEqual(to, table.Waiting, "The table waits for the answer");
            Assert.IsFalse(table.CanManage(from));
            Assert.IsFalse(table.Apply(MatchCommand.Of(CommandKind.Roll, from), 2), "Nothing moves while the offer is on the table");
            Assert.IsFalse(table.Apply(MatchCommand.Propose(offer), 3), "One offer at a time");
            Assert.IsFalse(table.Apply(MatchCommand.Answer(from, true), 4), "The proposer cannot answer their own offer");

            Assert.IsTrue(table.Apply(ThroughTheLog(MatchCommand.Answer(to, true)), 5));
            Assert.IsNull(table.PendingOffer);
            Assert.IsTrue(table.AnswerAccepted);
            Assert.AreEqual(to, table.Match.Owner(Damrak));
            Assert.AreEqual(from, table.Match.Owner(Champs));
            Assert.AreEqual(from, table.Waiting);
            Assert.IsTrue(table.Apply(MatchCommand.Of(CommandKind.Roll, from), 6), "The game goes on");
        }

        [Test]
        public void ADeclinedOfferLeavesEverythingWhereItWas()
        {
            LockstepMatch table = TradingTable(out TradeOffer offer);
            uint before = table.Checksum();
            Assert.IsTrue(table.Apply(MatchCommand.Propose(offer), 1));
            Assert.AreNotEqual(before, table.Checksum(), "An offer on the table is part of the state");
            Assert.IsTrue(table.Apply(MatchCommand.Answer(offer.to, false), 2));
            Assert.IsFalse(table.AnswerAccepted);
            Assert.AreEqual(before, table.Checksum());

            // The clock declines an offer nobody answers.
            Assert.IsTrue(table.Apply(MatchCommand.Propose(offer), 3));
            List<MatchCommand> made = table.Timeout(4);
            Assert.AreEqual(1, made.Count);
            Assert.AreEqual(CommandKind.AnswerTrade, made[0].kind);
            Assert.AreEqual(0, made[0].argument);
            Assert.IsNull(table.PendingOffer);
            Assert.AreEqual(before, table.Checksum());
        }

        [Test]
        public void AnOfferTheRulesForbidNeverReachesTheTable()
        {
            LockstepMatch table = TradingTable(out TradeOffer offer);
            offer.giveCash = 100000;
            Assert.IsFalse(table.Apply(MatchCommand.Propose(offer), 1), "More cash than the proposer has");
            offer.giveCash = 0;
            MatchCommand forged = MatchCommand.Propose(offer);
            forged.seat = offer.to;
            Assert.IsFalse(table.Apply(forged, 2), "An offer in the name of another seat");
            Assert.IsNull(table.PendingOffer);
        }

        [Test]
        public void ALeaverIsPlayedByTheComputer()
        {
            LockstepMatch table = Table(new RuleSet(), 31, 3, false);
            Assert.IsTrue(table.Apply(ThroughTheLog(MatchCommand.Of(CommandKind.SeatToComputer, 1, (int)BotLevel.Hard)), 1));
            Assert.IsTrue(table.Match.players[1].bot);
            Assert.AreEqual(BotLevel.Hard, table.Match.players[1].level);
            Assert.IsFalse(table.Match.players[0].bot);
            Assert.IsTrue(table.Apply(MatchCommand.Of(CommandKind.SeatToComputer, 2, 99), 2));
            Assert.AreEqual(BotLevel.Hard, table.Match.players[2].level, "A level out of range is brought into it");
        }

        // ------------------------------------------------------------------ the clock

        [Test]
        public void TimeoutsAloneFinishAGame()
        {
            // Nobody at the table ever does anything: the clock rolls, passes on every property and ends the turns.
            LockstepMatch table = Table(new RuleSet { roundLimit = 20, speedDie = true, startingCash = 2000 }, 41, 4, false);
            int timeouts = 0;
            while (!table.Match.IsOver && timeouts < 5000)
            {
                List<MatchCommand> made = table.Timeout((uint)(1000 + timeouts));
                Assert.IsNotEmpty(made, $"The default move got stuck in {table.Match.phase}");
                table.Match.TakeEvents();
                timeouts++;
            }
            Assert.IsTrue(table.Match.IsOver, "Twenty rounds of default moves end the game");
            Assert.GreaterOrEqual(table.Match.winner, 0);
            Assert.AreEqual(20, table.Match.round);
            CheckInvariants(table.Match);
        }

        [TestCaseSource(nameof(RuleSets))]
        public void TheDefaultMoveIsAlwaysAccepted(RuleSet rules)
        {
            // Two computer players and two seats nobody plays: the clock moves those, into debt and out of the game.
            int finished = 0;
            for (uint game = 0; game < 4; game++)
            {
                LockstepMatch table = Table(rules, 600 + game);
                table.Match.players[2].bot = false;
                table.Match.players[3].bot = false;
                var brain = new BotBrain(new SystemRandom((int)game));
                int steps = 0;
                while (!table.Match.IsOver && steps < 30000 && table.Match.turn <= 1500)
                {
                    steps++;
                    PlayerState waiting = table.Match.players[table.Waiting];
                    if (waiting.bot)
                    {
                        Assert.IsTrue(table.Apply(brain.Decide(table.Match), (uint)steps), $"The computer player got stuck in {table.Match.phase}");
                    }
                    else
                    {
                        MatchPhase phase = table.Match.phase;
                        List<MatchCommand> made = table.Timeout((uint)steps);
                        Assert.IsNotEmpty(made, $"The default move got stuck in {phase}");
                        if (phase == MatchPhase.RaiseFunds)
                        {
                            Assert.IsFalse(table.Match.phase == MatchPhase.RaiseFunds && table.Match.Decider == waiting.index,
                                "A debt is raised, or given up on, in one go");
                        }
                    }
                    table.Match.TakeEvents();
                    if (steps % 50 == 0)
                    {
                        CheckInvariants(table.Match);
                    }
                }
                CheckInvariants(table.Match);
                finished += table.Match.IsOver ? 1 : 0;
            }
            Assert.GreaterOrEqual(finished, 3, "Most matches should come to an end");
        }

        [Test]
        public void TheClockRaisesADebtInOneGo()
        {
            LockstepMatch table = Table(new RuleSet(), 51, 2, false);
            MonopolyMatch match = table.Match;
            int debtor = match.current;
            int owner = 1 - debtor;
            Give(match, debtor, RuaAugusta, Alfama, GrandCentral, KingsCross);
            Give(match, owner, Champs, FifthAvenue);
            match.deeds[Champs].houses = MonopolyMatch.Hotel;
            match.deeds[FifthAvenue].houses = MonopolyMatch.Hotel;
            match.hotelsLeft -= 2;
            match.players[debtor].cash = 1750;
            Place(match, debtor, Shinjuku);
            // Four steps from Shinjuku is the hotel on the Champs-Elysees: more than the cash at hand, less than everything.
            Assert.IsTrue(match.Roll(new DiceRoll(1, 3)));
            Assert.AreEqual(MatchPhase.RaiseFunds, match.phase);
            int owed = match.CurrentDebt.amount;
            Assert.Greater(owed, match.players[debtor].cash);
            Assert.LessOrEqual(owed, match.LiquidValue(debtor));

            List<MatchCommand> made = table.Timeout(1);
            Assert.Greater(made.Count, 1, "Several properties had to go");
            Assert.IsTrue(made.All(c => c.kind == CommandKind.Mortgage), string.Join(", ", made));
            Assert.AreNotEqual(MatchPhase.RaiseFunds, match.phase);
            Assert.IsFalse(match.players[debtor].bankrupt);
            Assert.AreEqual(0, match.debts.Count);
            CheckInvariants(match);
        }

        [Test]
        public void TheClockGivesUpForAPlayerWhoCannotPay()
        {
            LockstepMatch table = Table(new RuleSet(), 52, 2, false);
            MonopolyMatch match = table.Match;
            int debtor = match.current;
            Give(match, 1 - debtor, Champs, FifthAvenue);
            match.deeds[Champs].houses = MonopolyMatch.Hotel;
            match.deeds[FifthAvenue].houses = MonopolyMatch.Hotel;
            match.hotelsLeft -= 2;
            match.players[debtor].cash = 100;
            Place(match, debtor, Shinjuku);
            Assert.IsTrue(match.Roll(new DiceRoll(1, 3)));
            Assert.AreEqual(MatchPhase.RaiseFunds, match.phase);

            List<MatchCommand> made = table.Timeout(1);
            Assert.AreEqual(CommandKind.DeclareBankruptcy, made.Last().kind);
            Assert.IsTrue(match.players[debtor].bankrupt);
            Assert.IsTrue(match.IsOver);
            Assert.IsEmpty(table.Timeout(2), "Nobody is waited for once the match is over");
        }

        [Test]
        public void TheDefaultMovesAreTheCautiousOnes()
        {
            MonopolyMatch match = New(2, new RuleSet { speedDie = true });
            Assert.AreEqual(CommandKind.Roll, MatchCommand.DefaultMove(match).kind);

            match.Roll(new DiceRoll(1, 2));
            Assert.AreEqual(MatchPhase.BuyChoice, match.phase);
            Assert.AreEqual(CommandKind.DeclineBuy, MatchCommand.DefaultMove(match).kind);
            Assert.IsTrue(MatchCommand.DefaultMove(match).Apply(match));
            Assert.AreEqual(MatchPhase.Auction, match.phase);
            MatchCommand pass = MatchCommand.DefaultMove(match);
            Assert.AreEqual(CommandKind.PassBid, pass.kind);
            Assert.AreEqual(match.auction.Bidder, pass.seat, "The bidder is the one the match waits for");
            Assert.IsTrue(pass.Apply(match));
            Assert.IsTrue(MatchCommand.DefaultMove(match).Apply(match));
            Assert.AreEqual(MatchPhase.EndTurn, match.phase);
            Assert.AreEqual(CommandKind.EndTurn, MatchCommand.DefaultMove(match).kind);

            match.players[match.current].passedGo = true;
            match.EndTurn();
            match.players[match.current].passedGo = true;
            match.Roll(new DiceRoll(2, 5, SpeedFace.Bus));
            MatchCommand bus = MatchCommand.DefaultMove(match);
            Assert.AreEqual(CommandKind.ChooseBus, bus.kind);
            Assert.AreEqual(0, bus.argument);
            Assert.IsTrue(bus.Apply(match));

            MonopolyMatch triples = New(2, new RuleSet { speedDie = true });
            triples.players[0].passedGo = true;
            Place(triples, 0, Lapa);
            triples.Roll(new DiceRoll(2, 2, SpeedFace.Two));
            Assert.AreEqual(MatchPhase.MoveAnywhere, triples.phase);
            MatchCommand anywhere = MatchCommand.DefaultMove(triples);
            Assert.AreEqual(CommandKind.ChooseDestination, anywhere.kind);
            Assert.AreEqual(Go, anywhere.argument);
            int cash = triples.players[0].cash;
            Assert.IsTrue(anywhere.Apply(triples));
            Assert.AreEqual(cash + triples.rules.salary, triples.players[0].cash, "GO pays the salary");

            MonopolyMatch over = New(2);
            over.Resign();
            Assert.IsNull(MatchCommand.DefaultMove(over));
        }

        [Test]
        public void ComputerPlayersDecideWithoutTouchingTheMatch()
        {
            MonopolyMatch match = New(2);
            match.players[0].bot = true;
            var brain = new BotBrain(new SystemRandom(1));
            string before = JsonUtility.ToJson(match);
            MatchCommand command = brain.Decide(match);
            Assert.IsNotNull(command);
            Assert.AreEqual(CommandKind.Roll, command.kind);
            Assert.IsTrue(command.InvolvesChance);
            Assert.AreEqual(before, JsonUtility.ToJson(match), "Deciding is not doing");
            match.players[0].bot = false;
            Assert.IsNull(brain.Decide(match), "A person decides for themselves");
        }
    }
}
