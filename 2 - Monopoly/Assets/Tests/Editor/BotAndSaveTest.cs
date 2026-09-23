using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using static Portfolio.Monopoly.Tests.Fixture;
using Gamebox.Lockstep;

namespace Portfolio.Monopoly.Tests
{
    public class BotAndSaveTest
    {
        private static IEnumerable<TestCaseData> RuleSets()
        {
            yield return new TestCaseData(new RuleSet()).SetName("Bots finish classic games");
            yield return new TestCaseData(new RuleSet { speedDie = true }).SetName("Bots finish speed die games");
            yield return new TestCaseData(new RuleSet { dealtProperties = 2, housesForHotel = 3, bankruptciesToEnd = 2 }).SetName("Bots finish quick deal games");
            yield return new TestCaseData(new RuleSet { freeParkingJackpot = true, doubleSalaryOnGo = true, partyCards = true, auctions = false })
                .SetName("Bots finish house rules games");
            yield return new TestCaseData(new RuleSet { roundLimit = 20, speedDie = true, startingCash = 2000 }).SetName("Bots finish tycoon rush games");
        }

        [TestCaseSource(nameof(RuleSets))]
        public void BotsPlayWholeMatches(RuleSet rules)
        {
            int finished = 0;
            int totalTurns = 0;
            int trades = 0;
            int accepted = 0;
            const int games = 24;
            for (int game = 0; game < games; game++)
            {
                var random = new SystemRandom(1000 + game);
                var match = new MonopolyMatch(WorldTourBoard.Create(), rules, random);
                for (int seat = 0; seat < 4; seat++)
                {
                    match.AddPlayer($"Bot {seat}", seat, true, (BotLevel)((game + seat) % 3));
                }
                match.Start();
                var brain = new BotBrain(new SystemRandom(game));
                int steps = 0;
                int proposedTurn = -1;
                while (!match.IsOver && steps < 20000)
                {
                    if (match.phase == MatchPhase.Roll && proposedTurn != match.turn)
                    {
                        proposedTurn = match.turn;
                        TradeOffer offer = brain.ProposeTrade(match, match.current);
                        if (offer != null)
                        {
                            trades++;
                            if (brain.WouldAccept(match, offer))
                            {
                                Assert.IsTrue(match.ExecuteTrade(offer), "An accepted trade has to go through");
                                accepted++;
                            }
                            else
                            {
                                brain.Refused(match, offer);
                            }
                        }
                    }
                    Assert.IsTrue(brain.Act(match), $"The bot got stuck in {match.phase} (game {game}, step {steps})");
                    match.TakeEvents();
                    steps++;
                    if (steps % 50 == 0)
                    {
                        CheckInvariants(match);
                    }
                    if (match.turn > 1500)
                    {
                        break;
                    }
                }
                CheckInvariants(match);
                if (match.IsOver)
                {
                    finished++;
                    Assert.GreaterOrEqual(match.winner, 0);
                    Assert.AreEqual(1, match.players[match.winner].place);
                }
                totalTurns += match.turn;
            }
            TestContext.WriteLine($"{finished}/{games} finished, {totalTurns / (float)games:F0} turns on average, {accepted}/{trades} trades accepted");
            Assert.GreaterOrEqual(finished, games * 2 / 3, "Most matches should come to an end");
        }

        [Test]
        public void SavedMatchRestoresAndPlaysOn()
        {
            MonopolyMatch match = New(3, new RuleSet { freeParkingJackpot = true });
            Give(match, 1, Damrak);
            match.Roll(new DiceRoll(2, 4));
            match.EndTurn();
            match.players[2].jailCards.Add(CardDeckKind.Chance);
            match.chanceDeck.order.Remove(match.Board.chance.FindIndex(c => c.action == CardAction.GetOutOfJail));
            string json = JsonUtility.ToJson(match);

            var restored = JsonUtility.FromJson<MonopolyMatch>(json);
            restored.Attach(WorldTourBoard.Create(), new SystemRandom(3));
            Assert.AreEqual(match.current, restored.current);
            Assert.AreEqual(match.phase, restored.phase);
            Assert.AreEqual(match.players.Select(p => p.cash), restored.players.Select(p => p.cash));
            Assert.AreEqual(match.players.Select(p => p.position), restored.players.Select(p => p.position));
            Assert.AreEqual(1, restored.Owner(Damrak));
            Assert.IsTrue(restored.rules.freeParkingJackpot);
            Assert.AreEqual(1, restored.players[2].jailCards.Count);
            CollectionAssert.AreEqual(match.chanceDeck.order, restored.chanceDeck.order);
            CheckInvariants(restored);
            Assert.IsTrue(restored.Roll(new DiceRoll(1, 2)));
        }

        [Test]
        public void BotsJudgeTradesByTheirSets()
        {
            MonopolyMatch match = New(2);
            match.players[1].bot = true;
            Give(match, 0, RuaAugusta);
            Give(match, 1, Alfama, Damrak);
            var brain = new BotBrain(new SystemRandom(1));

            var lowball = new TradeOffer { from = 0, to = 1, giveCash = 10 };
            lowball.getSpaces.Add(Alfama);
            Assert.IsFalse(brain.WouldAccept(match, lowball), "Handing over a set for pocket money");

            var generous = new TradeOffer { from = 0, to = 1, giveCash = 600 };
            generous.getSpaces.Add(Damrak);
            Assert.IsTrue(brain.WouldAccept(match, generous), "Six times the price for a loose property");
        }

        [Test]
        public void BotsOfferSwapsThatCompleteSets()
        {
            // With a third player at the table, handing a set to one opponent hurts less than winning one.
            MonopolyMatch match = New(3);
            match.players[0].bot = true;
            match.players[0].level = BotLevel.Hard;
            Give(match, 0, Alfama, Damrak, Kalverstraat);
            Give(match, 1, RuaAugusta, Prinsengracht);
            TradeOffer offer = null;
            var brain = new BotBrain(new SystemRandom(5));
            for (int i = 0; i < 40 && offer == null; i++)
            {
                offer = brain.ProposeTrade(match, 0);
            }
            Assert.IsNotNull(offer);
            CollectionAssert.AreEqual(new[] { Prinsengracht }, offer.getSpaces);
            CollectionAssert.AreEqual(new[] { Alfama }, offer.giveSpaces);
            Assert.IsTrue(match.CanTrade(offer, out string reason), reason);
            Assert.Greater(BotBrain.TradeGain(match, offer, 0), 0f);
        }

        [Test]
        public void RuleSetsAndTheBoardAreValid()
        {
            Assert.IsTrue(WorldTourBoard.Create().Check(out string error), error);
            Assert.IsTrue(new RuleSet().IsValid(out string message), message);
            Assert.IsFalse(new RuleSet { startingCash = 0 }.IsValid(out _));
            Assert.IsFalse(new RuleSet { housesForHotel = 5 }.IsValid(out _));
        }
    }
}
