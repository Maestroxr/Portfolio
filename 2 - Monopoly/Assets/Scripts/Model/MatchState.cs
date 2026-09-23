using System;
using System.Collections.Generic;
using Gamebox.Lockstep;

namespace Portfolio.Monopoly
{
    /// <summary>What the match waits for.</summary>
    public enum MatchPhase
    {
        /// <summary>The players are being seated; <see cref="MonopolyMatch.Start"/> has not run.</summary>
        Setup,
        /// <summary>The current player rolls the dice (and may build, mortgage and trade first).</summary>
        Roll,
        /// <summary>The current player starts the turn in jail: pay the fine, use a card or roll for doubles.</summary>
        JailChoice,
        /// <summary>Speed die: the bus lets the current player move by either white die or their sum.</summary>
        BusChoice,
        /// <summary>Speed die: triples let the current player move to any space.</summary>
        MoveAnywhere,
        /// <summary>The current player landed on a property of the bank: buy it or put it up for auction.</summary>
        BuyChoice,
        /// <summary>An auction runs; <see cref="AuctionState.Bidder"/> bids or drops out.</summary>
        Auction,
        /// <summary>A player owes more than they hold: sell buildings, mortgage, trade, then pay or go bankrupt.</summary>
        RaiseFunds,
        /// <summary>The current player finished moving: build, mortgage, trade, then end the turn.</summary>
        EndTurn,
        GameOver
    }

    public enum BotLevel { Easy, Normal, Hard }

    /// <summary>The faces of the speed die: 1, 2, 3, Mr. Monopoly (twice) and the bus.</summary>
    public enum SpeedFace { None = 0, One = 1, Two = 2, Three = 3, MrMonopoly = 4, Bus = 5 }

    /// <summary>Who owns or pays: a player index, the bank, or the Free Parking pot.</summary>
    public static class Party
    {
        public const int Bank = -1;
        public const int Pot = -2;
    }

    [Serializable]
    public class PlayerState
    {
        public int index;
        public string name;
        /// <summary>Which token the player moves (an index into the token list of the view).</summary>
        public int token;
        /// <summary>The colour of the seat the player sits at (the view's colour index; the rules ignore it).</summary>
        public int color;
        public bool bot;
        public BotLevel level = BotLevel.Normal;
        public int cash;
        public int position;
        public bool inJail;
        /// <summary>Failed attempts at doubles in jail.</summary>
        public int jailTurns;
        /// <summary>Get Out of Jail Free cards held, by deck.</summary>
        public List<CardDeckKind> jailCards = new List<CardDeckKind>();
        public bool bankrupt;
        /// <summary>The round the player went bankrupt in (0: still playing).</summary>
        public int bankruptRound;
        /// <summary>Whether the player has passed GO once (the speed die is used from then on).</summary>
        public bool passedGo;
        /// <summary>Doubles rolled in a row this turn.</summary>
        public int doubles;
        /// <summary>Final place, 1 for the winner (0 while the match runs).</summary>
        public int place;

        // Statistics for the results screen.
        public int rentCollected;
        public int rentPaid;
        public int timesInJail;

        public bool Active => !bankrupt;
    }

    [Serializable]
    public class DeedState
    {
        public int owner = Party.Bank;
        /// <summary>0 to 4 houses, <see cref="MonopolyMatch.Hotel"/> for a hotel.</summary>
        public int houses;
        public bool mortgaged;

        public bool Owned => owner >= 0;
    }

    /// <summary>A deck of cards as indices into the board's card list; Get Out of Jail Free cards leave it while held.</summary>
    [Serializable]
    public class CardDeck
    {
        public CardDeckKind kind;
        public List<int> order = new List<int>();

        /// <summary>Takes the top card; the card goes to the bottom unless it is kept (a jail card).</summary>
        public int Draw(bool keep)
        {
            if (order.Count == 0)
            {
                return -1;
            }
            int card = order[0];
            order.RemoveAt(0);
            if (!keep)
            {
                order.Add(card);
            }
            return card;
        }

        /// <summary>Puts a kept card back at the bottom.</summary>
        public void Return(int card)
        {
            if (card >= 0 && !order.Contains(card))
            {
                order.Add(card);
            }
        }
    }

    /// <summary>Money a player has to pay but could not when it came up.</summary>
    [Serializable]
    public class Debt
    {
        public int debtor;
        /// <summary>A player, <see cref="Party.Bank"/> or <see cref="Party.Pot"/>.</summary>
        public int creditor;
        public int amount;
        public string reason;
    }

    [Serializable]
    public class AuctionState
    {
        public int space = -1;
        public int highBid;
        public int highBidder = Party.Bank;
        /// <summary>Players still bidding, in bidding order.</summary>
        public List<int> bidders = new List<int>();
        /// <summary>Index into <see cref="bidders"/> of the player whose move it is.</summary>
        public int turn;

        public bool Running => space >= 0;

        public int Bidder => bidders.Count > 0 ? bidders[turn % bidders.Count] : Party.Bank;

        /// <summary>The lowest bid the current bidder may make.</summary>
        public int MinimumBid => highBidder < 0 ? MonopolyMatch.MinimumBid : highBid + MonopolyMatch.MinimumRaise;
    }

    /// <summary>A trade between two players: properties, cash and Get Out of Jail Free cards both ways.</summary>
    [Serializable]
    public class TradeOffer
    {
        public int from;
        public int to;
        public List<int> giveSpaces = new List<int>();
        public List<int> getSpaces = new List<int>();
        public int giveCash;
        public int getCash;
        public int giveJailCards;
        public int getJailCards;

        public bool IsEmpty => giveSpaces.Count == 0 && getSpaces.Count == 0 && giveCash == 0 && getCash == 0 && giveJailCards == 0 && getJailCards == 0;

        public TradeOffer Clone()
        {
            return new TradeOffer
            {
                from = from, to = to, giveSpaces = new List<int>(giveSpaces), getSpaces = new List<int>(getSpaces),
                giveCash = giveCash, getCash = getCash, giveJailCards = giveJailCards, getJailCards = getJailCards
            };
        }

        /// <summary>The same trade seen from the other side.</summary>
        public TradeOffer Reversed()
        {
            return new TradeOffer
            {
                from = to, to = from, giveSpaces = new List<int>(getSpaces), getSpaces = new List<int>(giveSpaces),
                giveCash = getCash, getCash = giveCash, giveJailCards = getJailCards, getJailCards = giveJailCards
            };
        }
    }

    [Serializable]
    public struct DiceRoll
    {
        public int a;
        public int b;
        public SpeedFace speed;

        public DiceRoll(int a, int b, SpeedFace speed = SpeedFace.None)
        {
            this.a = a;
            this.b = b;
            this.speed = speed;
        }

        public int Total => a + b;
        public bool IsDoubles => a == b && a > 0;
        public bool HasSpeed => speed != SpeedFace.None;
        public bool IsTriples => HasSpeed && (int)speed <= 3 && a == b && a == (int)speed;
        public int SpeedValue => HasSpeed && (int)speed <= 3 ? (int)speed : 0;

        public override string ToString()
        {
            return HasSpeed ? $"{a}+{b}+{speed}" : $"{a}+{b}";
        }
    }

    public sealed class SystemRandom : IRandom
    {
        private readonly Random random;

        public SystemRandom(int seed)
        {
            random = new Random(seed);
        }

        public SystemRandom() : this(Environment.TickCount)
        {
        }

        public int Range(int min, int max)
        {
            return random.Next(min, max);
        }

        public double Value()
        {
            return random.NextDouble();
        }
    }
}
