using System.Linq;
using NUnit.Framework;
using Gamebox.Lockstep;

namespace Portfolio.Monopoly.Tests
{
    /// <summary>Builds matches on the World Tour board and checks the invariants of the rules engine.</summary>
    internal static class Fixture
    {
        // Spaces of the World Tour board used by the tests.
        public const int Go = 0, RuaAugusta = 1, ChestA = 2, Alfama = 3, IncomeTax = 4, GrandCentral = 5, Damrak = 6, ChanceA = 7,
            Kalverstraat = 8, Prinsengracht = 9, Jail = 10, Lapa = 11, Electric = 12, Ipanema = 13, Copacabana = 14, KingsCross = 15,
            LaRambla = 16, ChestB = 17, FreeParking = 20, ChanceB = 22, GareDuNord = 25, WaterWorks = 28, GoToJail = 30,
            Shinjuku = 35, ChanceC = 36, FifthAvenue = 37, LuxuryTax = 38, Champs = 39;

        public static MonopolyMatch New(int players = 2, RuleSet rules = null, int seed = 7)
        {
            var match = new MonopolyMatch(WorldTourBoard.Create(), rules ?? new RuleSet(), new SystemRandom(seed));
            for (int i = 0; i < players; i++)
            {
                match.AddPlayer($"P{i}", i);
            }
            match.Start();
            match.TakeEvents();
            return match;
        }

        public static void Give(MonopolyMatch match, int player, params int[] spaces)
        {
            foreach (int space in spaces)
            {
                match.deeds[space].owner = player;
            }
        }

        public static void Place(MonopolyMatch match, int player, int space)
        {
            match.players[player].position = space;
        }

        /// <summary>Puts the first card of <paramref name="deck"/> with <paramref name="action"/> (and value) on top.</summary>
        public static void TopCard(MonopolyMatch match, CardDeckKind deck, CardAction action, int? value = null)
        {
            CardDeck cards = deck == CardDeckKind.Chance ? match.chanceDeck : match.chestDeck;
            var list = match.Board.Deck(deck);
            int index = cards.order.First(i => list[i].action == action && (value == null || list[i].value == value.Value));
            cards.order.Remove(index);
            cards.order.Insert(0, index);
        }

        public static void CheckInvariants(MonopolyMatch match)
        {
            foreach (PlayerState player in match.players)
            {
                Assert.GreaterOrEqual(player.cash, 0, $"{player.name} has negative cash");
                if (player.bankrupt)
                {
                    Assert.AreEqual(0, match.PropertiesOf(player.index).Count(), $"{player.name} is bankrupt but owns property");
                }
            }
            int houses = 0;
            int hotels = 0;
            foreach (int space in match.Board.Properties)
            {
                DeedState deed = match.deeds[space];
                if (deed.houses == MonopolyMatch.Hotel)
                {
                    hotels++;
                }
                else
                {
                    houses += deed.houses;
                }
                if (deed.houses > 0)
                {
                    Assert.IsTrue(deed.Owned, $"Buildings on unowned space {space}");
                    Assert.IsFalse(deed.mortgaged, $"Buildings on mortgaged space {space}");
                }
                if (deed.Owned)
                {
                    Assert.Less(deed.owner, match.players.Count);
                }
            }
            if (match.rules.limitedBuildings)
            {
                Assert.AreEqual(match.Board.houses, houses + match.housesLeft, "House supply does not add up");
                Assert.AreEqual(match.Board.hotels, hotels + match.hotelsLeft, "Hotel supply does not add up");
            }
            int heldChance = match.players.Sum(p => p.jailCards.Count(c => c == CardDeckKind.Chance));
            int heldChest = match.players.Sum(p => p.jailCards.Count(c => c == CardDeckKind.CommunityChest));
            int chanceCards = match.Board.chance.Count(c => !c.party || match.rules.partyCards);
            int chestCards = match.Board.communityChest.Count(c => !c.party || match.rules.partyCards);
            Assert.AreEqual(chanceCards, match.chanceDeck.order.Count + heldChance, "Chance cards went missing");
            Assert.AreEqual(chestCards, match.chestDeck.order.Count + heldChest, "Community Chest cards went missing");
            Assert.GreaterOrEqual(match.pot, 0);
        }
    }
}
