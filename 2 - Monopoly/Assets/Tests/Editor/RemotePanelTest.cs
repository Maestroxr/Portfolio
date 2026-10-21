using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using static Portfolio.Monopoly.Tests.Fixture;

namespace Portfolio.Monopoly.Tests
{
    /// <summary>
    /// The remote panel of a seat (<see cref="SeatPanelModel"/>): what it shows for every decision of the match, what a
    /// press sends through the commands seam, what the properties list offers, and what a viewer with no seat (the shared
    /// screen of a room) is told.
    /// </summary>
    public class RemotePanelTest
    {
        /// <summary>Writes down every command the panel sends.</summary>
        private sealed class Recorder : IMonopolyCommands
        {
            public readonly List<string> sent = new List<string>();
            public readonly List<string> answers = new List<string>();

            public void Roll(int seat) => sent.Add($"Roll {seat}");
            public void Buy(int seat) => sent.Add($"Buy {seat}");
            public void DeclineBuy(int seat) => sent.Add($"DeclineBuy {seat}");
            public void PayJailFine(int seat) => sent.Add($"PayJailFine {seat}");
            public void UseJailCard(int seat) => sent.Add($"UseJailCard {seat}");
            public void ChooseBus(int seat, int option) => sent.Add($"ChooseBus {seat} {option}");
            public void ChooseDestination(int seat, int space) => sent.Add($"ChooseDestination {seat} {space}");
            public void Bid(int seat, int amount) => sent.Add($"Bid {seat} {amount}");
            public void PassBid(int seat) => sent.Add($"PassBid {seat}");
            public void EndTurn(int seat) => sent.Add($"EndTurn {seat}");
            public void DeclareBankruptcy(int seat) => sent.Add($"DeclareBankruptcy {seat}");
            public void Build(int seat, int space) => sent.Add($"Build {seat} {space}");
            public void Sell(int seat, int space) => sent.Add($"Sell {seat} {space}");
            public void Mortgage(int seat, int space) => sent.Add($"Mortgage {seat} {space}");
            public void Unmortgage(int seat, int space) => sent.Add($"Unmortgage {seat} {space}");
            public void OpenManager(int seat) => sent.Add($"OpenManager {seat}");
            public void OpenTrade(int seat) => sent.Add($"OpenTrade {seat}");
            public void ProposeTrade(TradeOffer offer) => sent.Add($"ProposeTrade {offer.from}>{offer.to}");

            public void Answer(int seat, bool accept) => answers.Add($"{seat} {(accept ? "accept" : "decline")}");
        }

        /// <summary>A table of three people on the World Tour board, the first seat to move.</summary>
        private static LockstepMatch Table(RuleSet rules = null)
        {
            var table = new LockstepMatch(WorldTourBoard.Create(), rules ?? new RuleSet(), 11);
            for (int seat = 0; seat < 3; seat++)
            {
                table.Match.AddPlayer($"P{seat}", seat);
            }
            table.Match.Start(0);
            table.Match.TakeEvents();
            return table;
        }

        private static IEnumerable<PanelChoiceKind> Kinds(IEnumerable<PanelChoice> choices)
        {
            return choices.Select(choice => choice.kind);
        }

        // ------------------------------------------------------------------ who is spoken to

        [Test]
        public void AViewerWithNoSeatIsNeverSpokenTo()
        {
            LockstepMatch table = Table();
            MonopolyMatch match = table.Match;
            Assert.AreEqual(-1, table.ActingSeat(new int[0]), "The shared screen plays no seat");
            Assert.AreEqual("P0's move", SeatPanelModel.Headline(match, table, -1), "The screen is told whose move it is, by name");
            Assert.IsEmpty(SeatPanelModel.Choices(match, table, -1, true, true), "Nothing is asked of the screen");
            Assert.IsEmpty(SeatPanelModel.Properties(match, -1, true));
            Assert.AreEqual("", SeatPanelModel.Cash(match, -1));
            Assert.AreEqual("", SeatPanelModel.Worth(match, -1));
            Assert.AreEqual("", SeatPanelModel.Position(match, -1));
            Assert.AreEqual("", SeatPanelModel.Signature(match, table, -1, true, true, true).Split('\n')[0], "No cash of its own");
        }

        [Test]
        public void TheSeatToMoveIsAskedToRollAndNobodyElse()
        {
            LockstepMatch table = Table();
            MonopolyMatch match = table.Match;
            Assert.AreEqual("Roll the dice!", SeatPanelModel.Headline(match, table, 0));
            Assert.AreEqual("P0's move", SeatPanelModel.Headline(match, table, 1));
            List<PanelChoice> choices = SeatPanelModel.Choices(match, table, 0, true, false);
            CollectionAssert.AreEqual(new[] { PanelChoiceKind.Roll }, Kinds(choices));
            Assert.IsTrue(choices[0].primary && choices[0].enabled);
            Assert.IsEmpty(SeatPanelModel.Choices(match, table, 1, true, false), "Somebody else's move");
            Assert.IsEmpty(SeatPanelModel.Choices(match, table, 0, false, false), "Not deciding: the board is still playing, or a command is on its way");
            Assert.AreEqual("$1,500", SeatPanelModel.Cash(match, 0));
            Assert.AreEqual("On GO", SeatPanelModel.Position(match, 0));
            StringAssert.StartsWith("Net worth ", SeatPanelModel.Worth(match, 0));
        }

        // ------------------------------------------------------------------ the decisions, phase by phase

        [Test]
        public void JailOffersRollingPayingAndTheCard()
        {
            LockstepMatch table = Table();
            MonopolyMatch match = table.Match;
            PlayerState player = match.players[0];
            player.inJail = true;
            player.position = Jail;
            match.phase = MatchPhase.JailChoice;
            CollectionAssert.AreEqual(new[] { PanelChoiceKind.Roll, PanelChoiceKind.PayFine }, Kinds(SeatPanelModel.Choices(match, table, 0, true, false)));
            Assert.AreEqual("In jail", SeatPanelModel.Position(match, 0));
            StringAssert.StartsWith("Attempt 1 of", SeatPanelModel.Headline(match, table, 0));

            player.jailCards.Add(CardDeckKind.Chance);
            CollectionAssert.AreEqual(new[] { PanelChoiceKind.Roll, PanelChoiceKind.PayFine, PanelChoiceKind.UseCard }, Kinds(SeatPanelModel.Choices(match, table, 0, true, false)));
            StringAssert.Contains("Get Out of Jail Free", SeatPanelModel.Position(match, 0));

            player.cash = 20;
            PanelChoice pay = SeatPanelModel.Choices(match, table, 0, true, false).First(c => c.kind == PanelChoiceKind.PayFine);
            Assert.IsFalse(pay.enabled, "The fine cannot be paid with $20");
            Assert.AreEqual("Pay $50", pay.label);
        }

        [Test]
        public void TheBusAndTriplesOfferTheirPicks()
        {
            LockstepMatch table = Table(new RuleSet { speedDie = true });
            MonopolyMatch match = table.Match;
            match.phase = MatchPhase.BusChoice;
            match.lastRoll = new DiceRoll(2, 5, SpeedFace.Bus);
            List<PanelChoice> bus = SeatPanelModel.Choices(match, table, 0, true, false);
            CollectionAssert.AreEqual(new[] { 0, 1, 2 }, bus.Select(c => c.argument));
            Assert.IsTrue(bus.All(c => c.kind == PanelChoiceKind.Bus));
            StringAssert.StartsWith("7: ", bus[2].label);
            Assert.IsTrue(bus[2].primary, "Both dice is the usual pick");

            match.phase = MatchPhase.MoveAnywhere;
            List<PanelChoice> picks = SeatPanelModel.Choices(match, table, 0, true, false);
            Assert.IsTrue(picks.All(c => c.kind == PanelChoiceKind.Destination));
            int best = SeatPanelModel.SuggestDestination(match, 0);
            Assert.AreEqual(best, picks[0].argument, "The first pick is the best one");
            Assert.IsTrue(picks[0].primary);
            Assert.AreEqual(best == 0 ? 1 : 2, picks.Count, "GO is the other one, unless it is the best pick itself");
            Assert.AreEqual(0, picks[picks.Count - 1].argument);
        }

        [Test]
        public void ALandingOffersBuyingOrTheAuction()
        {
            LockstepMatch table = Table();
            MonopolyMatch match = table.Match;
            match.phase = MatchPhase.BuyChoice;
            match.pendingPurchase = Champs;
            Place(match, 0, Champs);
            List<PanelChoice> choices = SeatPanelModel.Choices(match, table, 0, true, false);
            CollectionAssert.AreEqual(new[] { PanelChoiceKind.Buy, PanelChoiceKind.DeclineBuy }, Kinds(choices));
            Assert.IsTrue(choices[0].enabled && choices[0].primary);
            Assert.AreEqual($"Buy ${match.Space(Champs).price}", choices[0].label);
            Assert.AreEqual("Auction", choices[1].label);
            StringAssert.StartsWith("For sale: $", SeatPanelModel.Headline(match, table, 0));

            match.players[0].cash = 10;
            choices = SeatPanelModel.Choices(match, table, 0, true, false);
            Assert.IsFalse(choices[0].enabled, "Too poor to buy");
            StringAssert.StartsWith("Need $", choices[0].label);

            LockstepMatch noAuctions = Table(new RuleSet { auctions = false });
            noAuctions.Match.phase = MatchPhase.BuyChoice;
            noAuctions.Match.pendingPurchase = Champs;
            Assert.AreEqual("Pass", SeatPanelModel.Choices(noAuctions.Match, noAuctions, 0, true, false)[1].label);
        }

        [Test]
        public void TheAuctionOffersThreeBidsAndPassingToTheBidder()
        {
            LockstepMatch table = Table();
            MonopolyMatch match = table.Match;
            match.phase = MatchPhase.Auction;
            match.auction.space = Champs;
            match.auction.bidders.AddRange(new[] { 0, 1, 2 });
            match.auction.turn = 1;
            Assert.AreEqual(1, match.Decider, "Seat 1 bids");
            List<PanelChoice> choices = SeatPanelModel.Choices(match, table, 1, true, false);
            CollectionAssert.AreEqual(new[] { PanelChoiceKind.Bid, PanelChoiceKind.Bid, PanelChoiceKind.Bid, PanelChoiceKind.PassBid }, Kinds(choices));
            CollectionAssert.AreEqual(new[] { 10, 50, 100 }, choices.Take(3).Select(c => c.argument), "Opening bids");
            Assert.AreEqual(match.auction.MinimumBid, choices[0].argument, "The first bid is the least the auction takes");
            Assert.IsEmpty(SeatPanelModel.Choices(match, table, 0, true, false), "Not seat 0's bid");
            StringAssert.StartsWith("Auction: ", SeatPanelModel.Headline(match, table, 0));
            StringAssert.Contains("No bids yet", SeatPanelModel.Detail(match, table, 1));
            StringAssert.Contains("Your bid", SeatPanelModel.Detail(match, table, 1));

            match.auction.highBid = 120;
            match.auction.highBidder = 0;
            match.players[1].cash = 200;
            choices = SeatPanelModel.Choices(match, table, 1, true, false);
            CollectionAssert.AreEqual(new[] { 130, 170, 220 }, choices.Take(3).Select(c => c.argument), "Raises over the high bid");
            CollectionAssert.AreEqual(new[] { true, true, false, true }, choices.Select(c => c.enabled), "A bid above the cash is disabled");
            StringAssert.Contains("P0 leads $120", SeatPanelModel.Detail(match, table, 1));
        }

        [Test]
        public void ADebtOffersGivingUpAndTheProperties()
        {
            LockstepMatch table = Table();
            MonopolyMatch match = table.Match;
            Give(match, 0, Damrak, Kalverstraat);
            match.players[0].cash = 30;
            match.debts.Add(new Debt { debtor = 0, creditor = 1, amount = 200, reason = "rent" });
            match.phase = MatchPhase.RaiseFunds;
            Assert.AreEqual(0, match.Decider);
            CollectionAssert.AreEqual(new[] { PanelChoiceKind.Bankrupt }, Kinds(SeatPanelModel.Choices(match, table, 0, true, false)));
            Assert.AreEqual("You owe $200", SeatPanelModel.Headline(match, table, 0));
            StringAssert.StartsWith("To P1. Raise $170", SeatPanelModel.Detail(match, table, 0));
            List<PanelProperty> properties = SeatPanelModel.Properties(match, 0, true);
            Assert.AreEqual(2, properties.Count);
            foreach (PanelProperty property in properties)
            {
                Assert.IsTrue(property.actions.First(a => a.kind == PanelChoiceKind.Mortgage).enabled, $"{property.name} can be mortgaged for the debt");
                Assert.IsFalse(property.actions.First(a => a.kind == PanelChoiceKind.Build).enabled, "No building while in debt");
            }
        }

        [Test]
        public void TheEndOfTheTurnOffersEndingIt()
        {
            LockstepMatch table = Table();
            MonopolyMatch match = table.Match;
            match.phase = MatchPhase.EndTurn;
            List<PanelChoice> choices = SeatPanelModel.Choices(match, table, 0, true, false);
            CollectionAssert.AreEqual(new[] { PanelChoiceKind.EndTurn }, Kinds(choices));
            Assert.AreEqual("Build, trade, or end your turn.", SeatPanelModel.Headline(match, table, 0));
            match.players[0].inJail = true;
            StringAssert.StartsWith("Locked up!", SeatPanelModel.Headline(match, table, 0));
        }

        // ------------------------------------------------------------------ offers

        [Test]
        public void AnOfferIsAnsweredFromThePanelOfItsRecipient()
        {
            LockstepMatch table = Table();
            MonopolyMatch match = table.Match;
            var offer = new TradeOffer { from = 0, to = 1, giveCash = 25 };
            Assert.IsTrue(table.Apply(MatchCommand.Propose(offer), 5));
            Assert.IsNotNull(table.PendingOffer);
            Assert.AreEqual("P0 offers you a trade", SeatPanelModel.Headline(match, table, 1));
            Assert.AreEqual("P1 answers", SeatPanelModel.Headline(match, table, 0));
            Assert.AreEqual("P1 answers", SeatPanelModel.Headline(match, table, -1), "The screen too");
            StringAssert.Contains("You get: $25", SeatPanelModel.Detail(match, table, 1));
            StringAssert.Contains("You give: Nothing", SeatPanelModel.Detail(match, table, 1));
            CollectionAssert.AreEqual(new[] { PanelChoiceKind.Accept, PanelChoiceKind.Decline }, Kinds(SeatPanelModel.Choices(match, table, 1, false, true)));
            Assert.IsEmpty(SeatPanelModel.Choices(match, table, 0, true, true), "The proposer waits");
            Assert.IsEmpty(SeatPanelModel.Choices(match, table, 1, true, false), "Not answering: the answer is on its way");

            var recorder = new Recorder();
            PanelChoice accept = SeatPanelModel.Choices(match, table, 1, false, true)[0];
            Assert.IsTrue(SeatPanelModel.Perform(accept, 1, recorder, recorder.Answer));
            CollectionAssert.AreEqual(new[] { "1 accept" }, recorder.answers);
            Assert.IsEmpty(recorder.sent, "An answer is no command of the rules");
        }

        // ------------------------------------------------------------------ pressing

        [Test]
        public void PressingAButtonSendsItsCommand()
        {
            var recorder = new Recorder();
            var presses = new List<PanelChoice>
            {
                PanelChoice.Of(PanelChoiceKind.Roll, "Roll"),
                PanelChoice.Of(PanelChoiceKind.PayFine, "Pay"),
                PanelChoice.Of(PanelChoiceKind.UseCard, "Card"),
                PanelChoice.Of(PanelChoiceKind.Bus, "Bus", 2),
                PanelChoice.Of(PanelChoiceKind.Destination, "Go", 39),
                PanelChoice.Of(PanelChoiceKind.Buy, "Buy"),
                PanelChoice.Of(PanelChoiceKind.DeclineBuy, "Auction"),
                PanelChoice.Of(PanelChoiceKind.Bid, "Bid", 60),
                PanelChoice.Of(PanelChoiceKind.PassBid, "Pass"),
                PanelChoice.Of(PanelChoiceKind.EndTurn, "End"),
                PanelChoice.Of(PanelChoiceKind.Bankrupt, "Bankrupt"),
                PanelChoice.Of(PanelChoiceKind.Build, "Build", Damrak),
                PanelChoice.Of(PanelChoiceKind.Sell, "Sell", Damrak),
                PanelChoice.Of(PanelChoiceKind.Mortgage, "Mortgage", Kalverstraat),
                PanelChoice.Of(PanelChoiceKind.Unmortgage, "Lift", Kalverstraat),
                PanelChoice.Of(PanelChoiceKind.Decline, "Decline")
            };
            foreach (PanelChoice choice in presses)
            {
                Assert.IsTrue(SeatPanelModel.Perform(choice, 2, recorder, recorder.Answer), choice.ToString());
            }
            CollectionAssert.AreEqual(new[]
            {
                "Roll 2", "PayJailFine 2", "UseJailCard 2", "ChooseBus 2 2", "ChooseDestination 2 39", "Buy 2", "DeclineBuy 2", "Bid 2 60", "PassBid 2",
                "EndTurn 2", "DeclareBankruptcy 2", $"Build 2 {Damrak}", $"Sell 2 {Damrak}", $"Mortgage 2 {Kalverstraat}", $"Unmortgage 2 {Kalverstraat}"
            }, recorder.sent);
            CollectionAssert.AreEqual(new[] { "2 decline" }, recorder.answers);
            Assert.IsFalse(SeatPanelModel.Perform(null, 2, recorder, recorder.Answer));
            Assert.IsFalse(SeatPanelModel.Perform(presses[0], 2, null, recorder.Answer), "Nowhere to send to");
        }

        // ------------------------------------------------------------------ the properties

        [Test]
        public void ThePropertiesCarryTheManagerButtons()
        {
            LockstepMatch table = Table();
            MonopolyMatch match = table.Match;
            Give(match, 0, Damrak, Kalverstraat, Prinsengracht, GrandCentral);
            Assert.IsEmpty(SeatPanelModel.Properties(match, 1, true), "Seat 1 owns nothing");
            List<PanelProperty> properties = SeatPanelModel.Properties(match, 0, true);
            CollectionAssert.AreEqual(new[] { Damrak, Kalverstraat, Prinsengracht, GrandCentral }, properties.Select(p => p.space), "Streets by set, then the station");
            foreach (PanelProperty street in properties.Take(3))
            {
                CollectionAssert.AreEqual(new[] { PanelChoiceKind.Build, PanelChoiceKind.Sell, PanelChoiceKind.Mortgage }, Kinds(street.actions), street.name);
                Assert.IsTrue(street.actions[0].enabled, $"{street.name}: the set is complete, a house can go up");
                Assert.IsFalse(street.actions[1].enabled, $"{street.name}: nothing to sell yet");
                Assert.IsTrue(street.actions[2].enabled, $"{street.name}: can be mortgaged");
                StringAssert.StartsWith("Rent $", street.state);
            }
            CollectionAssert.AreEqual(new[] { PanelChoiceKind.Mortgage }, Kinds(properties[3].actions), "A station has no buildings");
            Assert.IsTrue(SeatPanelModel.Properties(match, 0, false).All(p => p.actions.Count == 0), "Not managing: no buttons");

            Assert.IsTrue(match.Mortgage(0, Damrak));
            properties = SeatPanelModel.Properties(match, 0, true);
            PanelProperty mortgaged = properties.First(p => p.space == Damrak);
            Assert.IsTrue(mortgaged.mortgaged);
            Assert.AreEqual("Mortgaged", mortgaged.state);
            CollectionAssert.AreEqual(new[] { PanelChoiceKind.Build, PanelChoiceKind.Sell, PanelChoiceKind.Unmortgage }, Kinds(mortgaged.actions));
            Assert.IsTrue(mortgaged.actions[2].enabled, "The mortgage can be lifted");
            Assert.IsFalse(properties.First(p => p.space == Kalverstraat).actions[0].enabled, "No building on a set with a mortgaged street");

            Assert.IsTrue(match.Unmortgage(0, Damrak));
            Assert.IsTrue(match.Build(0, Damrak) && match.Build(0, Kalverstraat) && match.Build(0, Prinsengracht));
            properties = SeatPanelModel.Properties(match, 0, true);
            StringAssert.StartsWith("1 house", properties[0].state);
            Assert.IsTrue(properties[0].actions[1].enabled, "The house can be sold");
            Assert.IsFalse(properties[0].actions[2].enabled, "Not mortgaged with a house on it");
        }

        // ------------------------------------------------------------------ the end, and the picture as a whole

        [Test]
        public void TheEndShowsTheStandings()
        {
            LockstepMatch table = Table();
            MonopolyMatch match = table.Match;
            Give(match, 2, Champs, FifthAvenue);
            match.Resign();
            Assert.IsTrue(match.IsOver);
            Assert.AreEqual("P2 wins!", SeatPanelModel.Headline(match, table, 0), "The richest player");
            Assert.AreEqual("P2 wins!", SeatPanelModel.Headline(match, table, -1));
            Assert.IsEmpty(SeatPanelModel.Choices(match, table, 0, true, true));
            List<PanelStanding> standings = SeatPanelModel.Standings(match);
            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, standings.Select(s => s.place));
            Assert.AreEqual(2, standings[0].seat);
            Assert.AreEqual(match.NetWorth(2), standings[0].worth);
            Assert.IsTrue(standings.All(s => !s.bankrupt));
        }

        [Test]
        public void TheSuggestedDestinationIsNeitherJailNorSomebodysProperty()
        {
            LockstepMatch table = Table();
            MonopolyMatch match = table.Match;
            foreach (int space in match.Board.Properties)
            {
                match.deeds[space].owner = space == Champs ? 0 : 1;
            }
            int pick = SeatPanelModel.SuggestDestination(match, 0);
            Assert.AreNotEqual(GoToJail, pick);
            Assert.IsFalse(match.Space(pick).IsProperty && match.Owner(pick) == 1, "Not a space where rent is due");
            MonopolyMatch fresh = Table().Match;
            int best = SeatPanelModel.SuggestDestination(fresh, 0);
            Assert.IsTrue(best == Go || fresh.Space(best).IsProperty && !fresh.Deed(best).Owned, "On a fresh board a property to buy, or the salary");
        }

        [Test]
        public void TheSignatureFollowsTheMatch()
        {
            LockstepMatch table = Table();
            MonopolyMatch match = table.Match;
            string before = SeatPanelModel.Signature(match, table, 0, true, true, false);
            Assert.AreEqual(before, SeatPanelModel.Signature(match, table, 0, true, true, false), "The same picture twice");
            Assert.AreNotEqual(before, SeatPanelModel.Signature(match, table, 0, false, true, false), "The buttons went");
            Assert.AreNotEqual(before, SeatPanelModel.Signature(match, table, 1, true, true, false), "Another seat");
            Assert.IsTrue(table.Apply(MatchCommand.Of(CommandKind.Roll, 0), 3));
            match.TakeEvents();
            Assert.AreNotEqual(before, SeatPanelModel.Signature(match, table, 0, true, true, false), "The dice moved the seat");
            Assert.AreEqual("", SeatPanelModel.Signature(null, table, 0, true, true, false));
        }
    }
}
