using System;

namespace Portfolio.Monopoly
{
    public enum MatchEventKind
    {
        /// <summary>A player's turn begins. <c>player</c>, <c>value</c> = round.</summary>
        TurnStarted,
        /// <summary>A new round begins. <c>value</c> = round.</summary>
        RoundStarted,
        /// <summary><c>player</c> rolled <c>roll</c>.</summary>
        DiceRolled,
        /// <summary>Doubles: <c>player</c> rolls again. <c>value</c> = doubles in a row.</summary>
        Doubles,
        /// <summary>Speed die triples: <c>player</c> may move anywhere.</summary>
        Triples,
        /// <summary>The speed die showed Mr. Monopoly or the bus. <c>value</c> = <see cref="SpeedFace"/>.</summary>
        SpeedDie,
        /// <summary><c>player</c> moves space by space from <c>from</c> to <c>space</c>; <c>value</c> = steps (negative: backwards).</summary>
        Moved,
        /// <summary><c>player</c> is moved straight to <c>space</c> (jail, a card, move anywhere) without stepping.</summary>
        Teleported,
        /// <summary><c>player</c> passed or landed on GO; the money follows as a <see cref="Transfer"/>.</summary>
        PassedGo,
        /// <summary><c>player</c> landed on <c>space</c>.</summary>
        Landed,
        /// <summary>Money moved: <c>player</c> (or <see cref="Party"/>) paid <c>other</c> <c>value</c>; <c>text</c> says why.</summary>
        Transfer,
        /// <summary><c>player</c> must decide whether to buy <c>space</c> (price <c>value</c>).</summary>
        BuyOffered,
        /// <summary><c>player</c> bought <c>space</c> for <c>value</c> (from the bank, or at auction).</summary>
        Bought,
        /// <summary><c>player</c> was dealt <c>space</c> at the start (short game).</summary>
        Dealt,
        /// <summary>Rent is due: <c>player</c> pays <c>other</c> <c>value</c> for <c>space</c>.</summary>
        Rent,
        /// <summary>No rent: <c>space</c> is mortgaged, or its owner <c>other</c> is in jail (<c>value</c> = 1).</summary>
        NoRent,
        /// <summary><c>player</c> paid tax <c>value</c> on <c>space</c>.</summary>
        Tax,
        /// <summary><c>player</c> drew <c>card</c> from <c>deck</c>.</summary>
        CardDrawn,
        /// <summary><c>player</c> goes to jail.</summary>
        WentToJail,
        /// <summary><c>player</c> left jail; <c>value</c>: 0 doubles, 1 paid the fine, 2 used a card, 3 paid after the last attempt.</summary>
        LeftJail,
        /// <summary><c>player</c> missed doubles in jail; <c>value</c> = attempts so far.</summary>
        StayedInJail,
        /// <summary><c>player</c> gained a Get Out of Jail Free card from <c>deck</c>.</summary>
        JailCardGained,
        /// <summary><c>player</c> built on <c>space</c>, which now has <c>value</c> houses (5 = hotel).</summary>
        Built,
        /// <summary><c>player</c> sold a building on <c>space</c>, which now has <c>value</c> houses.</summary>
        SoldBuilding,
        Mortgaged,
        Unmortgaged,
        /// <summary><c>space</c> goes to auction.</summary>
        AuctionStarted,
        /// <summary><c>player</c> bid <c>value</c> for <c>space</c>.</summary>
        Bid,
        /// <summary><c>player</c> dropped out of the auction.</summary>
        BidPassed,
        /// <summary>Nobody bid: <c>space</c> stays with the bank.</summary>
        AuctionUnsold,
        /// <summary><c>player</c> and <c>other</c> traded (<c>trade</c>).</summary>
        Traded,
        /// <summary><c>player</c> owes <c>other</c> <c>value</c> and must raise funds.</summary>
        DebtStarted,
        /// <summary><c>player</c> paid the debt of <c>value</c> to <c>other</c>.</summary>
        DebtPaid,
        /// <summary><c>player</c> is bankrupt; their assets go to <c>other</c> (a player or the bank).</summary>
        Bankrupt,
        /// <summary><c>space</c> changes owner from <c>player</c> to <c>other</c> without money (bankruptcy, trade, returned to bank).</summary>
        OwnerChanged,
        /// <summary>The Free Parking pot now holds <c>value</c>.</summary>
        PotChanged,
        /// <summary><c>player</c> won the Free Parking pot of <c>value</c>.</summary>
        Jackpot,
        /// <summary>The match is over; <c>player</c> won.</summary>
        GameOver,
        /// <summary>Something to tell the players: <c>text</c>.</summary>
        Message
    }

    /// <summary>One thing that happened in a match, in order. The view replays the list to animate a turn.</summary>
    [Serializable]
    public struct MatchEvent
    {
        public MatchEventKind kind;
        public int player;
        public int other;
        public int space;
        public int from;
        public int value;
        public DiceRoll roll;
        public CardDeckKind deck;
        public int card;
        public string text;
        public TradeOffer trade;

        public override string ToString()
        {
            return $"{kind} p{player} o{other} s{space} f{from} v{value}{(string.IsNullOrEmpty(text) ? "" : " " + text)}";
        }
    }
}
