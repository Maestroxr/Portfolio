using System;
using System.Collections.Generic;
using System.Linq;
using Gamebox;
using Gamebox.Economy;
using Gamebox.Lockstep;
using NUnit.Framework;

namespace Portfolio.Monopoly.Tests
{
    /// <summary>
    /// The shop of the game: the definition sells Lucky Start for money and Golden Dice for Tokens, and Lucky Start's
    /// bonus reaches the people at the table of a game played here through the rules engine, never the computer players
    /// and never an online match.
    /// </summary>
    public class MonopolyShopTest
    {
        [Test]
        public void TheDefinitionSellsLuckyStartForMoneyAndGoldenDiceForTokens()
        {
            var definition = new MonopolyShop();
            Assert.AreEqual(GameType.Monopoly, definition.Game);
            var catalog = new CloudCatalog();
            definition.DefineCloud(catalog);
            Assert.AreEqual(2, catalog.Count);

            CloudItem lucky = catalog["MONOPOLY_LUCKY_START"];
            Assert.IsNotNull(lucky, "Lucky Start is missing");
            Assert.AreEqual(GameType.Monopoly, lucky.Game);
            Assert.AreEqual(100, lucky.Cents);
            Assert.AreEqual("$1.00", lucky.MoneyPrice);
            Assert.AreEqual(0, lucky.Tokens);
            Assert.AreEqual(ItemKind.Owned, lucky.Kind);
            Assert.AreEqual("com.skinnerboxes.monopoly.luckystart", lucky.AppleProductId);
            Assert.AreEqual("com.skinnerboxes.monopoly.luckystart", lucky.GooglePlayProductId);
            Assert.AreEqual("MONOPOLY_LUCKY_START_FOR_MONEY", lucky.MoneyPurchaseId);

            CloudItem dice = catalog["MONOPOLY_GOLDEN_DICE"];
            Assert.IsNotNull(dice, "Golden Dice is missing");
            Assert.AreEqual(GameType.Monopoly, dice.Game);
            Assert.AreEqual(150, dice.Tokens);
            Assert.AreEqual(0, dice.Cents);
            Assert.AreEqual(ItemKind.Owned, dice.Kind);
            Assert.AreEqual("MONOPOLY_GOLDEN_DICE_FOR_TOKENS", dice.TokensPurchaseId);

            CollectionAssert.AreEqual(new[] { "MONOPOLY_LUCKY_START" }, catalog.ForMoney().Select(item => item.Id).ToList());
            CollectionAssert.AreEqual(new[] { "MONOPOLY_GOLDEN_DICE" }, catalog.ForTokens().Select(item => item.Id).ToList());
        }

        [Test]
        public void LuckyStartPaysOnlyInAGamePlayedHere()
        {
            Assert.AreEqual(500, MonopolyShop.StartingCashBonus(local: true, owned: true));
            Assert.AreEqual(0, MonopolyShop.StartingCashBonus(local: true, owned: false), "not bought");
            Assert.AreEqual(0, MonopolyShop.StartingCashBonus(local: false, owned: true), "an online match is the same on every client");
            Assert.AreEqual(0, MonopolyShop.StartingCashBonus(local: false, owned: false));
        }

        [Test]
        public void TheBonusGoesToThePeopleAtTheTableNotTheComputer()
        {
            var match = new MonopolyMatch(WorldTourBoard.Create(), new RuleSet(), new SystemRandom(7));
            match.AddPlayer("You", 0);
            match.AddPlayer("Ada", 1, bot: true);
            match.AddPlayer("Player 3", 2);
            match.Start();
            match.TakeEvents();

            match.GiveStartingBonus(MonopolyShop.StartingCashBonus(local: true, owned: true));

            Assert.AreEqual(2000, match.players[0].cash);
            Assert.AreEqual(1500, match.players[1].cash, "the computer player gets nothing");
            Assert.AreEqual(2000, match.players[2].cash);
            List<MatchEvent> events = match.TakeEvents();
            Assert.AreEqual(2, events.Count(e => e.kind == MatchEventKind.Transfer && e.player == Party.Bank && e.value == 500), "two payments from the bank");
            Assert.AreEqual(MatchPhase.Roll, match.phase, "the first turn goes on as it was");
            Fixture.CheckInvariants(match);
        }

        [Test]
        public void NoBonusLeavesTheMatchAsItIs()
        {
            MonopolyMatch match = Fixture.New(3);
            match.GiveStartingBonus(MonopolyShop.StartingCashBonus(local: false, owned: true));
            Assert.IsTrue(match.players.All(p => p.cash == 1500));
            Assert.IsFalse(match.HasEvents);
        }

        [Test]
        public void TheBonusComesAfterTheStart()
        {
            var match = new MonopolyMatch(WorldTourBoard.Create(), new RuleSet(), new SystemRandom(7));
            match.AddPlayer("You", 0);
            match.AddPlayer("Ada", 1, bot: true);
            Assert.Throws<InvalidOperationException>(() => match.GiveStartingBonus(500), "the start would hand out the starting cash over it");
        }
    }
}
