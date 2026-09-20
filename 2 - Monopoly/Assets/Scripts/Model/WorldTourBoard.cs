using System.Collections.Generic;

namespace Portfolio.Monopoly
{
    /// <summary>
    /// The World Tour board: the classic 40 spaces and prices with every colour set a city, the stations of four capitals
    /// and decks of travel themed cards. The content builder writes it into the Board asset; the tests play on it.
    /// </summary>
    public static class WorldTourBoard
    {
        public static BoardLayout Create()
        {
            var board = new BoardLayout { title = "Monopoly", edition = "World Tour" };
            List<SpaceData> s = board.spaces;
            s.Add(Corner("GO", SpaceKind.Go, "Collect $200 salary as you pass"));
            s.Add(Street("Rua Augusta", "Lisbon", ColorGroup.Brown, 60, 50, 2, 10, 30, 90, 160, 250));
            s.Add(Special("Community Chest", SpaceKind.CommunityChest));
            s.Add(Street("Alfama", "Lisbon", ColorGroup.Brown, 60, 50, 4, 20, 60, 180, 320, 450));
            s.Add(Tax("Income Tax", 200));
            s.Add(Station("Grand Central", "New York"));
            s.Add(Street("Damrak", "Amsterdam", ColorGroup.LightBlue, 100, 50, 6, 30, 90, 270, 400, 550));
            s.Add(Special("Chance", SpaceKind.Chance));
            s.Add(Street("Kalverstraat", "Amsterdam", ColorGroup.LightBlue, 100, 50, 6, 30, 90, 270, 400, 550));
            s.Add(Street("Prinsengracht", "Amsterdam", ColorGroup.LightBlue, 120, 50, 8, 40, 100, 300, 450, 600));
            s.Add(Corner("Jail", SpaceKind.Jail, "Just visiting"));
            s.Add(Street("Lapa", "Rio de Janeiro", ColorGroup.Pink, 140, 100, 10, 50, 150, 450, 625, 750));
            s.Add(Utility("Electric Company"));
            s.Add(Street("Ipanema", "Rio de Janeiro", ColorGroup.Pink, 140, 100, 10, 50, 150, 450, 625, 750));
            s.Add(Street("Copacabana", "Rio de Janeiro", ColorGroup.Pink, 160, 100, 12, 60, 180, 500, 700, 900));
            s.Add(Station("King's Cross", "London"));
            s.Add(Street("La Rambla", "Barcelona", ColorGroup.Orange, 180, 100, 14, 70, 200, 550, 750, 950));
            s.Add(Special("Community Chest", SpaceKind.CommunityChest));
            s.Add(Street("Plaça Reial", "Barcelona", ColorGroup.Orange, 180, 100, 14, 70, 200, 550, 750, 950));
            s.Add(Street("Passeig de Gràcia", "Barcelona", ColorGroup.Orange, 200, 100, 16, 80, 220, 600, 800, 1000));
            s.Add(Corner("Free Parking", SpaceKind.FreeParking, ""));
            s.Add(Street("Shibuya", "Tokyo", ColorGroup.Red, 220, 150, 18, 90, 250, 700, 875, 1050));
            s.Add(Special("Chance", SpaceKind.Chance));
            s.Add(Street("Omotesando", "Tokyo", ColorGroup.Red, 220, 150, 18, 90, 250, 700, 875, 1050));
            s.Add(Street("Ginza", "Tokyo", ColorGroup.Red, 240, 150, 20, 100, 300, 750, 925, 1100));
            s.Add(Station("Gare du Nord", "Paris"));
            s.Add(Street("Via Appia", "Rome", ColorGroup.Yellow, 260, 150, 22, 110, 330, 800, 975, 1150));
            s.Add(Street("Via del Corso", "Rome", ColorGroup.Yellow, 260, 150, 22, 110, 330, 800, 975, 1150));
            s.Add(Utility("Water Works"));
            s.Add(Street("Via Condotti", "Rome", ColorGroup.Yellow, 280, 150, 24, 120, 360, 850, 1025, 1200));
            s.Add(Corner("Go to Jail", SpaceKind.GoToJail, ""));
            s.Add(Street("Abbey Road", "London", ColorGroup.Green, 300, 200, 26, 130, 390, 900, 1100, 1275));
            s.Add(Street("Carnaby Street", "London", ColorGroup.Green, 300, 200, 26, 130, 390, 900, 1100, 1275));
            s.Add(Special("Community Chest", SpaceKind.CommunityChest));
            s.Add(Street("Savile Row", "London", ColorGroup.Green, 320, 200, 28, 150, 450, 1000, 1200, 1400));
            s.Add(Station("Shinjuku", "Tokyo"));
            s.Add(Special("Chance", SpaceKind.Chance));
            s.Add(Street("Fifth Avenue", "New York", ColorGroup.DarkBlue, 350, 200, 35, 175, 500, 1100, 1300, 1500));
            s.Add(Tax("Luxury Tax", 100));
            s.Add(Street("Champs-Élysées", "Paris", ColorGroup.DarkBlue, 400, 200, 50, 200, 600, 1400, 1700, 2000));

            List<CardData> c = board.chance;
            c.Add(Card(CardDeckKind.Chance, "Advance to GO. Collect $200.", CardAction.AdvanceTo, 0));
            c.Add(Card(CardDeckKind.Chance, "Go shopping on the Champs-Élysées. Advance there.", CardAction.AdvanceTo, 39));
            c.Add(Card(CardDeckKind.Chance, "Advance to Ginza. If you pass GO, collect $200.", CardAction.AdvanceTo, 24));
            c.Add(Card(CardDeckKind.Chance, "Advance to Lapa. If you pass GO, collect $200.", CardAction.AdvanceTo, 11));
            c.Add(Card(CardDeckKind.Chance, "Catch a train at Grand Central. If you pass GO, collect $200.", CardAction.AdvanceTo, 5));
            c.Add(Nearest("All aboard! Advance to the nearest station. If it is owned, pay the owner twice the rent.", ColorGroup.Railroad, 2));
            c.Add(Nearest("All aboard! Advance to the nearest station. If it is owned, pay the owner twice the rent.", ColorGroup.Railroad, 2));
            c.Add(Nearest("Advance to the nearest utility. If it is owned, roll the dice and pay the owner ten times the roll.", ColorGroup.Utility, 10));
            c.Add(Card(CardDeckKind.Chance, "Your travel blog goes viral. Collect $50.", CardAction.Collect, 50));
            c.Add(Card(CardDeckKind.Chance, "Get Out of Jail Free. Keep this card until you need it, or trade it.", CardAction.GetOutOfJail, 0));
            c.Add(Card(CardDeckKind.Chance, "Wrong turn! Go back three spaces.", CardAction.MoveBy, -3));
            c.Add(Card(CardDeckKind.Chance, "Go to Jail. Move directly to Jail. Do not pass GO, do not collect $200.", CardAction.GoToJail, 0));
            c.Add(Card(CardDeckKind.Chance, "Your hotels need a makeover. Pay $25 for each house and $100 for each hotel.", CardAction.Repairs, 25, 100));
            c.Add(Card(CardDeckKind.Chance, "Speeding ticket on the autobahn. Pay $15.", CardAction.Pay, 15));
            c.Add(Card(CardDeckKind.Chance, "You treat the whole tour group to dinner. Pay each player $50.", CardAction.PayEachPlayer, 50));
            c.Add(Card(CardDeckKind.Chance, "Your travel savings mature. Collect $150.", CardAction.Collect, 150));
            c.Add(Party(CardDeckKind.Chance, "Hitch a ride to Free Parking. Advance there.", CardAction.AdvanceTo, 20));
            c.Add(Party(CardDeckKind.Chance, "Robin Hood! The richest player pays you $100.", CardAction.CollectFromRichest, 100));
            c.Add(Party(CardDeckKind.Chance, "Tailwind! Move forward five spaces.", CardAction.MoveBy, 5));

            List<CardData> k = board.communityChest;
            k.Add(Card(CardDeckKind.CommunityChest, "Advance to GO. Collect $200.", CardAction.AdvanceTo, 0));
            k.Add(Card(CardDeckKind.CommunityChest, "The bank made a mistake in your favor. Collect $200.", CardAction.Collect, 200));
            k.Add(Card(CardDeckKind.CommunityChest, "Travel insurance co-payment. Pay $50.", CardAction.Pay, 50));
            k.Add(Card(CardDeckKind.CommunityChest, "You sell your holiday photos. Collect $50.", CardAction.Collect, 50));
            k.Add(Card(CardDeckKind.CommunityChest, "Get Out of Jail Free. Keep this card until you need it, or trade it.", CardAction.GetOutOfJail, 0));
            k.Add(Card(CardDeckKind.CommunityChest, "Go to Jail. Move directly to Jail. Do not pass GO, do not collect $200.", CardAction.GoToJail, 0));
            k.Add(Card(CardDeckKind.CommunityChest, "Your holiday fund matures. Collect $100.", CardAction.Collect, 100));
            k.Add(Card(CardDeckKind.CommunityChest, "Tax refund. Collect $20.", CardAction.Collect, 20));
            k.Add(Card(CardDeckKind.CommunityChest, "It is your birthday! Collect $10 from every player.", CardAction.CollectFromEachPlayer, 10));
            k.Add(Card(CardDeckKind.CommunityChest, "Your life insurance matures. Collect $100.", CardAction.Collect, 100));
            k.Add(Card(CardDeckKind.CommunityChest, "Hospital bill after a ski trip. Pay $100.", CardAction.Pay, 100));
            k.Add(Card(CardDeckKind.CommunityChest, "Language school fees. Pay $50.", CardAction.Pay, 50));
            k.Add(Card(CardDeckKind.CommunityChest, "You guide a city tour. Collect a $25 fee.", CardAction.Collect, 25));
            k.Add(Card(CardDeckKind.CommunityChest, "Street repairs. Pay $40 for each house and $115 for each hotel.", CardAction.Repairs, 40, 115));
            k.Add(Card(CardDeckKind.CommunityChest, "You win second prize in a sandcastle contest. Collect $10.", CardAction.Collect, 10));
            k.Add(Card(CardDeckKind.CommunityChest, "You inherit $100 from a long lost aunt.", CardAction.Collect, 100));
            k.Add(Party(CardDeckKind.CommunityChest, "Charity gala! Every player pays $25 into the Free Parking pot.", CardAction.EveryonePaysPot, 25));
            k.Add(Party(CardDeckKind.CommunityChest, "Free upgrade! A free house on your cheapest complete set.", CardAction.FreeHouse, 0));
            return board;
        }

        private static SpaceData Corner(string name, SpaceKind kind, string city)
        {
            return new SpaceData { name = name, city = city, kind = kind, group = ColorGroup.None };
        }

        private static SpaceData Special(string name, SpaceKind kind)
        {
            return new SpaceData { name = name, city = "", kind = kind, group = ColorGroup.None };
        }

        private static SpaceData Tax(string name, int amount)
        {
            return new SpaceData { name = name, city = $"Pay ${amount}", kind = SpaceKind.Tax, group = ColorGroup.None, tax = amount };
        }

        private static SpaceData Street(string name, string city, ColorGroup group, int price, int houseCost, params int[] rent)
        {
            return new SpaceData { name = name, city = city, kind = SpaceKind.Street, group = group, price = price, houseCost = houseCost, rent = rent };
        }

        private static SpaceData Station(string name, string city)
        {
            return new SpaceData
            {
                name = name, city = city, kind = SpaceKind.Railroad, group = ColorGroup.Railroad, price = 200,
                rent = new[] { 25, 50, 100, 200, 0, 0 }
            };
        }

        private static SpaceData Utility(string name)
        {
            return new SpaceData { name = name, city = "", kind = SpaceKind.Utility, group = ColorGroup.Utility, price = 150, rent = new[] { 4, 10, 0, 0, 0, 0 } };
        }

        private static CardData Card(CardDeckKind deck, string text, CardAction action, int value, int value2 = 0)
        {
            return new CardData { deck = deck, text = text, action = action, value = value, value2 = value2 };
        }

        private static CardData Nearest(string text, ColorGroup group, int multiplier)
        {
            return new CardData { deck = CardDeckKind.Chance, text = text, action = CardAction.AdvanceToNearest, group = group, value = multiplier };
        }

        private static CardData Party(CardDeckKind deck, string text, CardAction action, int value)
        {
            return new CardData { deck = deck, text = text, action = action, value = value, party = true };
        }
    }
}
