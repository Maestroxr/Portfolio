using System.Linq;
using NUnit.Framework;
using static Portfolio.Monopoly.Tests.Fixture;

namespace Portfolio.Monopoly.Tests
{
    public class MonopolyMatchTest
    {
        [Test]
        public void StartHandsOutCashAndShufflesTheDecks()
        {
            MonopolyMatch match = New(4);
            Assert.AreEqual(MatchPhase.Roll, match.phase);
            Assert.IsTrue(match.players.All(p => p.cash == 1500 && p.position == 0));
            Assert.AreEqual(16, match.chanceDeck.order.Count);
            Assert.AreEqual(16, match.chestDeck.order.Count);
            Assert.AreEqual(32, match.housesLeft);
            Assert.AreEqual(12, match.hotelsLeft);
            Assert.AreEqual(40, match.deeds.Count);
            Assert.AreEqual(28, match.Board.Properties.Count());
            CheckInvariants(match);
        }

        [Test]
        public void RollMovesTheTokenAndOffersTheProperty()
        {
            MonopolyMatch match = New();
            Assert.IsTrue(match.Roll(new DiceRoll(2, 4)));
            Assert.AreEqual(Damrak, match.players[0].position);
            Assert.AreEqual(MatchPhase.BuyChoice, match.phase);
            Assert.IsTrue(match.Buy());
            Assert.AreEqual(0, match.Owner(Damrak));
            Assert.AreEqual(1400, match.players[0].cash);
            Assert.AreEqual(MatchPhase.EndTurn, match.phase);
            Assert.IsTrue(match.EndTurn());
            Assert.AreEqual(1, match.current);
            Assert.AreEqual(MatchPhase.Roll, match.phase);
        }

        [Test]
        public void PassingGoPaysTheSalary()
        {
            MonopolyMatch match = New();
            Place(match, 0, 36);
            match.Roll(new DiceRoll(3, 4));
            Assert.AreEqual(Alfama, match.players[0].position);
            Assert.AreEqual(1700, match.players[0].cash);
            Assert.IsTrue(match.players[0].passedGo);
        }

        [Test]
        public void LandingOnGoPaysDoubleWithTheHouseRule()
        {
            MonopolyMatch match = New(2, new RuleSet { doubleSalaryOnGo = true });
            Place(match, 0, 37);
            match.Roll(new DiceRoll(1, 2));
            Assert.AreEqual(Go, match.players[0].position);
            Assert.AreEqual(1900, match.players[0].cash);
        }

        [Test]
        public void RentDoublesOnAFullSetAndRisesWithBuildings()
        {
            MonopolyMatch match = New();
            Give(match, 1, RuaAugusta);
            Assert.AreEqual(2, match.Rent(RuaAugusta, 7));
            Give(match, 1, Alfama);
            Assert.AreEqual(4, match.Rent(RuaAugusta, 7));
            match.deeds[RuaAugusta].houses = 2;
            Assert.AreEqual(30, match.Rent(RuaAugusta, 7));
            match.deeds[RuaAugusta].houses = MonopolyMatch.Hotel;
            Assert.AreEqual(250, match.Rent(RuaAugusta, 7));

            Place(match, 0, 38);
            match.Roll(new DiceRoll(1, 2));
            Assert.AreEqual(1500 + 200 - 250, match.players[0].cash);
            Assert.AreEqual(1750, match.players[1].cash);
        }

        [Test]
        public void StationRentDependsOnHowManyAreOwned()
        {
            MonopolyMatch match = New();
            Give(match, 1, GrandCentral);
            Assert.AreEqual(25, match.Rent(GrandCentral, 7));
            Give(match, 1, KingsCross, GareDuNord);
            Assert.AreEqual(100, match.Rent(GrandCentral, 7));
            Give(match, 1, Shinjuku);
            Assert.AreEqual(200, match.Rent(GrandCentral, 7));
        }

        [Test]
        public void UtilityRentIsAMultipleOfTheDice()
        {
            MonopolyMatch match = New();
            Give(match, 1, Electric);
            Place(match, 0, ChanceA);
            match.Roll(new DiceRoll(2, 3));
            Assert.AreEqual(1500 - 20, match.players[0].cash);
            Give(match, 1, WaterWorks);
            Assert.AreEqual(50, match.Rent(Electric, 5));
        }

        [Test]
        public void MortgagedPropertiesChargeNoRent()
        {
            MonopolyMatch match = New();
            Give(match, 1, Damrak);
            match.deeds[Damrak].mortgaged = true;
            match.Roll(new DiceRoll(2, 4));
            Assert.AreEqual(1500, match.players[0].cash);
            Assert.AreEqual(MatchPhase.EndTurn, match.phase);
        }

        [Test]
        public void NoRentFromJailWithTheHouseRule()
        {
            MonopolyMatch match = New(2, new RuleSet { noRentInJail = true });
            Give(match, 1, Damrak);
            match.players[1].inJail = true;
            match.Roll(new DiceRoll(2, 4));
            Assert.AreEqual(1500, match.players[0].cash);
        }

        [Test]
        public void DoublesRollAgainAndThreeGoToJail()
        {
            MonopolyMatch match = New(2, new RuleSet { auctions = false });
            match.Roll(new DiceRoll(3, 3));
            Assert.AreEqual(MatchPhase.BuyChoice, match.phase);
            match.DeclineBuy();
            Assert.AreEqual(MatchPhase.Roll, match.phase);
            match.Roll(new DiceRoll(2, 2));
            Assert.AreEqual(Jail, match.players[0].position);
            Assert.IsFalse(match.players[0].inJail);
            Assert.AreEqual(MatchPhase.Roll, match.phase);
            match.Roll(new DiceRoll(1, 1));
            Assert.IsTrue(match.players[0].inJail);
            Assert.AreEqual(Jail, match.players[0].position);
            Assert.AreEqual(MatchPhase.EndTurn, match.phase);
        }

        [Test]
        public void GoToJailSendsTheTokenToJailWithoutSalary()
        {
            MonopolyMatch match = New();
            Place(match, 0, 27);
            match.Roll(new DiceRoll(1, 2));
            Assert.IsTrue(match.players[0].inJail);
            Assert.AreEqual(Jail, match.players[0].position);
            Assert.AreEqual(1500, match.players[0].cash);
            Assert.AreEqual(MatchPhase.EndTurn, match.phase);
        }

        [Test]
        public void JailFineLetsThePlayerRoll()
        {
            MonopolyMatch match = New();
            match.players[0].inJail = true;
            match.players[0].position = Jail;
            match.phase = MatchPhase.EndTurn;
            match.EndTurn();
            match.phase = MatchPhase.EndTurn;
            match.EndTurn();
            Assert.AreEqual(0, match.current);
            Assert.AreEqual(MatchPhase.JailChoice, match.phase, "A jailed player starts the turn with the jail choice");
            Assert.IsFalse(match.Build(0, RuaAugusta));
            Assert.IsTrue(match.PayJailFine());
            Assert.AreEqual(1450, match.players[0].cash);
            Assert.IsFalse(match.players[0].inJail);
            Assert.AreEqual(MatchPhase.Roll, match.phase);
            match.Roll(new DiceRoll(2, 4));
            Assert.AreEqual(LaRambla, match.players[0].position);
        }

        [Test]
        public void JailCardReturnsToItsDeck()
        {
            MonopolyMatch match = New();
            TopCard(match, CardDeckKind.Chance, CardAction.GetOutOfJail);
            Place(match, 0, Alfama);
            match.Roll(new DiceRoll(1, 3));
            Assert.AreEqual(1, match.players[0].jailCards.Count);
            Assert.AreEqual(15, match.chanceDeck.order.Count);
            CheckInvariants(match);

            match.players[0].inJail = true;
            match.players[0].position = Jail;
            match.phase = MatchPhase.JailChoice;
            Assert.IsTrue(match.UseJailCard());
            Assert.AreEqual(0, match.players[0].jailCards.Count);
            Assert.AreEqual(16, match.chanceDeck.order.Count);
            Assert.AreEqual(MatchPhase.Roll, match.phase);
            CheckInvariants(match);
        }

        [Test]
        public void DoublesInJailFreeThePlayerWithoutAnotherRoll()
        {
            MonopolyMatch match = New(2, new RuleSet { auctions = false });
            match.players[0].inJail = true;
            match.players[0].position = Jail;
            match.phase = MatchPhase.JailChoice;
            match.Roll(new DiceRoll(3, 3));
            Assert.IsFalse(match.players[0].inJail);
            Assert.AreEqual(LaRambla, match.players[0].position);
            match.DeclineBuy();
            Assert.AreEqual(MatchPhase.EndTurn, match.phase);
        }

        [Test]
        public void TheThirdMissedDoublesPaysTheFineAndMoves()
        {
            MonopolyMatch match = New();
            match.players[0].inJail = true;
            match.players[0].position = Jail;
            match.phase = MatchPhase.JailChoice;
            match.Roll(new DiceRoll(1, 2));
            Assert.AreEqual(MatchPhase.EndTurn, match.phase);
            Assert.AreEqual(1, match.players[0].jailTurns);
            match.players[0].jailTurns = 2;
            match.phase = MatchPhase.JailChoice;
            match.Roll(new DiceRoll(1, 2));
            Assert.IsFalse(match.players[0].inJail);
            Assert.AreEqual(Ipanema, match.players[0].position);
            Assert.AreEqual(1450, match.players[0].cash);
        }

        [Test]
        public void TaxesFeedTheJackpotAndFreeParkingPaysIt()
        {
            MonopolyMatch match = New(2, new RuleSet { freeParkingJackpot = true });
            Place(match, 0, RuaAugusta);
            match.Roll(new DiceRoll(1, 2));
            Assert.AreEqual(IncomeTax, match.players[0].position);
            Assert.AreEqual(200, match.pot);
            Assert.AreEqual(1300, match.players[0].cash);
            match.EndTurn();
            Place(match, 1, ChestB);
            match.Roll(new DiceRoll(1, 2));
            Assert.AreEqual(FreeParking, match.players[1].position);
            Assert.AreEqual(0, match.pot);
            Assert.AreEqual(1700, match.players[1].cash);
        }

        [Test]
        public void TaxesGoToTheBankWithoutTheJackpot()
        {
            MonopolyMatch match = New();
            Place(match, 0, RuaAugusta);
            match.Roll(new DiceRoll(1, 2));
            Assert.AreEqual(0, match.pot);
            Assert.AreEqual(1300, match.players[0].cash);
        }

        [Test]
        public void AdvanceToGoCard()
        {
            MonopolyMatch match = New();
            TopCard(match, CardDeckKind.Chance, CardAction.AdvanceTo, 0);
            Place(match, 0, Alfama);
            match.Roll(new DiceRoll(1, 3));
            Assert.AreEqual(Go, match.players[0].position);
            Assert.AreEqual(1700, match.players[0].cash);
        }

        [Test]
        public void NearestStationCardChargesDoubleRent()
        {
            MonopolyMatch match = New();
            Give(match, 1, KingsCross);
            TopCard(match, CardDeckKind.Chance, CardAction.AdvanceToNearest, 2);
            Place(match, 0, Alfama);
            match.Roll(new DiceRoll(1, 3));
            Assert.AreEqual(KingsCross, match.players[0].position);
            Assert.AreEqual(1450, match.players[0].cash);
            Assert.AreEqual(1550, match.players[1].cash);
        }

        [Test]
        public void NearestUtilityCardRollsForTenTimesTheDice()
        {
            MonopolyMatch match = New();
            Give(match, 1, WaterWorks);
            TopCard(match, CardDeckKind.Chance, CardAction.AdvanceToNearest, 10);
            Place(match, 0, 19);
            match.ScriptRoll(new DiceRoll(4, 5));
            match.Roll(new DiceRoll(1, 2));
            Assert.AreEqual(WaterWorks, match.players[0].position);
            Assert.AreEqual(1500 - 90, match.players[0].cash);
        }

        [Test]
        public void BirthdayCollectsFromEveryPlayer()
        {
            MonopolyMatch match = New(3);
            TopCard(match, CardDeckKind.CommunityChest, CardAction.CollectFromEachPlayer);
            Place(match, 0, Copacabana);
            match.Roll(new DiceRoll(1, 2));
            Assert.AreEqual(1520, match.players[0].cash);
            Assert.AreEqual(1490, match.players[1].cash);
            Assert.AreEqual(1490, match.players[2].cash);
        }

        [Test]
        public void GoBackThreeSpacesLandsOnTheTax()
        {
            MonopolyMatch match = New();
            TopCard(match, CardDeckKind.Chance, CardAction.MoveBy, -3);
            Place(match, 0, Alfama);
            match.Roll(new DiceRoll(1, 3));
            Assert.AreEqual(IncomeTax, match.players[0].position);
            Assert.AreEqual(1300, match.players[0].cash);
        }

        [Test]
        public void RepairsChargePerBuilding()
        {
            MonopolyMatch match = New();
            Give(match, 0, RuaAugusta, Alfama);
            match.deeds[RuaAugusta].houses = 2;
            match.deeds[Alfama].houses = MonopolyMatch.Hotel;
            match.housesLeft -= 2;
            match.hotelsLeft -= 1;
            TopCard(match, CardDeckKind.Chance, CardAction.Repairs);
            Place(match, 0, 3);
            match.Roll(new DiceRoll(1, 3));
            Assert.AreEqual(1500 - (2 * 25 + 100), match.players[0].cash);
            CheckInvariants(match);
        }

        [Test]
        public void DeclinedPropertyGoesToAuction()
        {
            MonopolyMatch match = New(3);
            match.Roll(new DiceRoll(2, 4));
            Assert.IsTrue(match.DeclineBuy());
            Assert.AreEqual(MatchPhase.Auction, match.phase);
            Assert.AreEqual(1, match.auction.Bidder);
            Assert.IsFalse(match.PlaceBid(1, 5), "Below the minimum bid");
            Assert.IsTrue(match.PlaceBid(1, 10));
            Assert.AreEqual(2, match.auction.Bidder);
            Assert.IsTrue(match.PlaceBid(2, 50));
            Assert.IsTrue(match.PassBid(0));
            Assert.IsTrue(match.PassBid(1));
            Assert.AreEqual(2, match.Owner(Damrak));
            Assert.AreEqual(1450, match.players[2].cash);
            Assert.AreEqual(MatchPhase.EndTurn, match.phase);
            Assert.AreEqual(0, match.current);
        }

        [Test]
        public void AuctionWithoutBidsLeavesThePropertyWithTheBank()
        {
            MonopolyMatch match = New(2);
            match.Roll(new DiceRoll(2, 4));
            match.DeclineBuy();
            match.PassBid(1);
            Assert.AreEqual(MatchPhase.Auction, match.phase, "The last bidder may still bid");
            match.PassBid(0);
            Assert.IsFalse(match.deeds[Damrak].Owned);
            Assert.AreEqual(MatchPhase.EndTurn, match.phase);
        }

        [Test]
        public void WithoutAuctionsADeclinedPropertyStaysUnsold()
        {
            MonopolyMatch match = New(2, new RuleSet { auctions = false });
            match.Roll(new DiceRoll(2, 4));
            match.DeclineBuy();
            Assert.IsFalse(match.deeds[Damrak].Owned);
            Assert.AreEqual(MatchPhase.EndTurn, match.phase);
        }

        [Test]
        public void BuildingNeedsTheWholeSetAndGoesEvenly()
        {
            MonopolyMatch match = New();
            Give(match, 0, RuaAugusta);
            Assert.IsFalse(match.CanBuild(0, RuaAugusta, out _));
            Give(match, 0, Alfama);
            Assert.IsTrue(match.Build(0, RuaAugusta));
            Assert.IsFalse(match.Build(0, RuaAugusta), "Even building");
            Assert.IsTrue(match.Build(0, Alfama));
            for (int i = 0; i < 3; i++)
            {
                Assert.IsTrue(match.Build(0, RuaAugusta));
                Assert.IsTrue(match.Build(0, Alfama));
            }
            Assert.AreEqual(4, match.deeds[RuaAugusta].houses);
            Assert.AreEqual(24, match.housesLeft);
            Assert.IsTrue(match.Build(0, RuaAugusta));
            Assert.AreEqual(MonopolyMatch.Hotel, match.deeds[RuaAugusta].houses);
            Assert.AreEqual(28, match.housesLeft);
            Assert.AreEqual(11, match.hotelsLeft);
            Assert.AreEqual(1500 - 9 * 50, match.players[0].cash);
            CheckInvariants(match);

            Assert.IsFalse(match.SellBuilding(0, Alfama), "Sell evenly: the hotel goes first");
            Assert.IsTrue(match.SellBuilding(0, RuaAugusta));
            Assert.AreEqual(4, match.deeds[RuaAugusta].houses);
            Assert.AreEqual(1500 - 9 * 50 + 25, match.players[0].cash);
            CheckInvariants(match);
        }

        [Test]
        public void TheBankCanRunOutOfHouses()
        {
            MonopolyMatch match = New();
            Give(match, 0, RuaAugusta, Alfama);
            match.housesLeft = 0;
            Assert.IsFalse(match.CanBuild(0, RuaAugusta, out string reason));
            StringAssert.Contains("no houses", reason);
        }

        [Test]
        public void MortgagesNeedAnEmptySetAndCostInterestToLift()
        {
            MonopolyMatch match = New();
            Give(match, 0, RuaAugusta, Alfama);
            match.Build(0, Alfama);
            Assert.IsFalse(match.CanMortgage(0, RuaAugusta, out _));
            match.SellBuilding(0, Alfama);
            Assert.IsTrue(match.Mortgage(0, RuaAugusta));
            Assert.AreEqual(1500 - 50 + 25 + 30, match.players[0].cash);
            Assert.IsFalse(match.CanBuild(0, Alfama, out _), "No building on a set with a mortgage");
            Assert.IsTrue(match.Unmortgage(0, RuaAugusta));
            Assert.AreEqual(1500 - 50 + 25 + 30 - 33, match.players[0].cash);
        }

        [Test]
        public void TradesMovePropertiesCashAndInterest()
        {
            MonopolyMatch match = New();
            Give(match, 0, RuaAugusta);
            Give(match, 1, Alfama);
            match.deeds[Alfama].mortgaged = true;
            var offer = new TradeOffer { from = 0, to = 1, giveCash = 100 };
            offer.getSpaces.Add(Alfama);
            Assert.IsTrue(match.ExecuteTrade(offer));
            Assert.AreEqual(0, match.Owner(Alfama));
            Assert.AreEqual(1500 - 100 - 3, match.players[0].cash);
            Assert.AreEqual(1600, match.players[1].cash);
            Assert.IsTrue(match.OwnsGroup(0, ColorGroup.Brown));
        }

        [Test]
        public void BuildingsBlockTradingTheirSet()
        {
            MonopolyMatch match = New();
            Give(match, 0, RuaAugusta, Alfama);
            match.Build(0, RuaAugusta);
            var offer = new TradeOffer { from = 0, to = 1 };
            offer.giveSpaces.Add(Alfama);
            Assert.IsFalse(match.CanTrade(offer, out _));
        }

        [Test]
        public void UnpaidRentForcesRaisingFundsThenBankruptcyToTheOwner()
        {
            MonopolyMatch match = New();
            Give(match, 1, FifthAvenue, Champs);
            match.deeds[Champs].houses = MonopolyMatch.Hotel;
            match.hotelsLeft--;
            Give(match, 0, RuaAugusta);
            match.players[0].cash = 100;
            Place(match, 0, ChanceC);
            match.Roll(new DiceRoll(1, 2));
            Assert.AreEqual(MatchPhase.RaiseFunds, match.phase);
            Assert.AreEqual(2000, match.CurrentDebt.amount);
            Assert.AreEqual(0, match.Decider);
            Assert.IsTrue(match.Mortgage(0, RuaAugusta));
            Assert.AreEqual(MatchPhase.RaiseFunds, match.phase);
            Assert.IsTrue(match.DeclareBankruptcy());
            Assert.IsTrue(match.players[0].bankrupt);
            Assert.AreEqual(1, match.Owner(RuaAugusta));
            Assert.AreEqual(MatchPhase.GameOver, match.phase);
            Assert.AreEqual(1, match.winner);
            Assert.AreEqual(1500 + 130 - 3, match.players[1].cash);
            CheckInvariants(match);
        }

        [Test]
        public void RaisedFundsPayTheDebtAutomatically()
        {
            MonopolyMatch match = New();
            Give(match, 1, Damrak);
            Give(match, 0, Kalverstraat, Prinsengracht, FifthAvenue);
            match.players[0].cash = 3;
            match.Roll(new DiceRoll(2, 4));
            Assert.AreEqual(MatchPhase.RaiseFunds, match.phase);
            Assert.IsTrue(match.Mortgage(0, FifthAvenue));
            Assert.AreEqual(MatchPhase.EndTurn, match.phase);
            Assert.AreEqual(3 + 175 - 6, match.players[0].cash);
        }

        [Test]
        public void BankruptcyToTheBankAuctionsTheProperties()
        {
            MonopolyMatch match = New(3);
            Give(match, 0, Damrak, Kalverstraat);
            match.players[0].cash = 10;
            Place(match, 0, RuaAugusta);
            match.Roll(new DiceRoll(1, 2));
            Assert.AreEqual(MatchPhase.RaiseFunds, match.phase);
            match.Mortgage(0, Damrak);
            match.Mortgage(0, Kalverstraat);
            Assert.AreEqual(MatchPhase.RaiseFunds, match.phase);
            Assert.IsTrue(match.DeclareBankruptcy());
            Assert.IsTrue(match.players[0].bankrupt);
            Assert.AreEqual(MatchPhase.Auction, match.phase);
            Assert.IsFalse(match.deeds[Damrak].mortgaged);
            Assert.IsTrue(match.PlaceBid(match.auction.Bidder, 40));
            match.PassBid(match.auction.Bidder);
            Assert.IsTrue(match.deeds[Damrak].Owned);
            Assert.AreEqual(MatchPhase.Auction, match.phase, "The second property is auctioned too");
            match.PassBid(match.auction.Bidder);
            match.PassBid(match.auction.Bidder);
            Assert.IsFalse(match.deeds[Kalverstraat].Owned);
            Assert.AreEqual(MatchPhase.Roll, match.phase);
            Assert.AreEqual(1, match.current);
            CheckInvariants(match);
        }

        [Test]
        public void ShortGameDealsDeedsAndBuildsHotelsOnThreeHouses()
        {
            var rules = new RuleSet { dealtProperties = 2, housesForHotel = 3, bankruptciesToEnd = 2 };
            MonopolyMatch match = New(3, rules);
            foreach (PlayerState player in match.players)
            {
                int[] owned = match.PropertiesOf(player.index).ToArray();
                Assert.AreEqual(2, owned.Length);
                Assert.AreEqual(1500 - owned.Sum(s => match.Board[s].price), player.cash);
            }
            foreach (int space in match.Board.Properties)
            {
                match.deeds[space].owner = -1;
            }
            Give(match, 0, RuaAugusta, Alfama);
            match.players[0].cash = 1500;
            for (int i = 0; i < 3; i++)
            {
                match.Build(0, RuaAugusta);
                match.Build(0, Alfama);
            }
            Assert.IsTrue(match.Build(0, RuaAugusta));
            Assert.AreEqual(MonopolyMatch.Hotel, match.deeds[RuaAugusta].houses);
            Assert.AreEqual(4 * 50, match.BuildingValue(RuaAugusta));
            CheckInvariants(match);
        }

        [Test]
        public void ShortGameEndsAtTheSecondBankruptcy()
        {
            MonopolyMatch match = New(4, new RuleSet { bankruptciesToEnd = 2 });
            Give(match, 3, Champs);
            match.deeds[Champs].houses = MonopolyMatch.Hotel;
            match.hotelsLeft--;
            for (int seat = 0; seat < 2; seat++)
            {
                Assert.AreEqual(seat, match.current);
                match.players[seat].cash = 10;
                Place(match, seat, ChanceC);
                match.Roll(new DiceRoll(1, 2));
                Assert.AreEqual(MatchPhase.RaiseFunds, match.phase);
                match.DeclareBankruptcy();
            }
            Assert.AreEqual(MatchPhase.GameOver, match.phase);
            Assert.AreEqual(3, match.winner);
            Assert.AreEqual(1, match.players[3].place);
            Assert.AreEqual(2, match.players[2].place);
        }

        [Test]
        public void RoundLimitEndsTheMatch()
        {
            MonopolyMatch match = New(2, new RuleSet { roundLimit = 2, auctions = false });
            int turns = 0;
            while (match.phase != MatchPhase.GameOver && turns < 10)
            {
                match.Roll(new DiceRoll(1, 3));
                if (match.phase == MatchPhase.BuyChoice)
                {
                    match.DeclineBuy();
                }
                if (match.phase == MatchPhase.EndTurn)
                {
                    match.EndTurn();
                }
                turns++;
            }
            Assert.AreEqual(MatchPhase.GameOver, match.phase);
            Assert.AreEqual(4, turns);
        }

        [Test]
        public void SpeedDieWaitsUntilThePlayerPassedGo()
        {
            MonopolyMatch match = New(2, new RuleSet { speedDie = true });
            match.Roll(new DiceRoll(1, 2, SpeedFace.Three));
            Assert.AreEqual(Alfama, match.players[0].position, "No speed die before passing GO");
        }

        [Test]
        public void SpeedDieNumbersAddToTheMove()
        {
            MonopolyMatch match = New(2, new RuleSet { speedDie = true });
            match.players[0].passedGo = true;
            match.Roll(new DiceRoll(1, 2, SpeedFace.Two));
            Assert.AreEqual(GrandCentral, match.players[0].position);
        }

        [Test]
        public void MrMonopolyMovesOnToTheNextPropertyForSale()
        {
            MonopolyMatch match = New(2, new RuleSet { speedDie = true });
            match.players[0].passedGo = true;
            match.Roll(new DiceRoll(1, 2, SpeedFace.MrMonopoly));
            Assert.AreEqual(Alfama, match.players[0].position);
            Assert.AreEqual(MatchPhase.BuyChoice, match.phase);
            match.Buy();
            Assert.AreEqual(GrandCentral, match.players[0].position);
            Assert.AreEqual(MatchPhase.BuyChoice, match.phase);
        }

        [Test]
        public void TheBusChoosesADie()
        {
            MonopolyMatch match = New(2, new RuleSet { speedDie = true });
            match.players[0].passedGo = true;
            match.Roll(new DiceRoll(2, 5, SpeedFace.Bus));
            Assert.AreEqual(MatchPhase.BusChoice, match.phase);
            CollectionAssert.AreEqual(new[] { 2, 5, 7 }, match.BusOptions);
            match.ChooseBus(1);
            Assert.AreEqual(GrandCentral, match.players[0].position);
            Assert.AreEqual(MatchPhase.BuyChoice, match.phase);
        }

        [Test]
        public void TriplesMoveAnywhere()
        {
            MonopolyMatch match = New(2, new RuleSet { speedDie = true });
            match.players[0].passedGo = true;
            match.Roll(new DiceRoll(2, 2, SpeedFace.Two));
            Assert.AreEqual(MatchPhase.MoveAnywhere, match.phase);
            Assert.IsTrue(match.ChooseDestination(Champs));
            Assert.AreEqual(Champs, match.players[0].position);
            Assert.AreEqual(MatchPhase.BuyChoice, match.phase);
            match.Buy();
            Assert.AreEqual(MatchPhase.EndTurn, match.phase, "Triples do not roll again");
        }

        [Test]
        public void EventsDescribeTheTurnInOrder()
        {
            MonopolyMatch match = New();
            Give(match, 1, Damrak);
            match.Roll(new DiceRoll(2, 4));
            var kinds = match.TakeEvents().Select(e => e.kind).ToList();
            CollectionAssert.AreEqual(new[]
            {
                MatchEventKind.DiceRolled, MatchEventKind.Moved, MatchEventKind.Landed, MatchEventKind.Rent, MatchEventKind.Transfer
            }, kinds);
        }
    }
}
