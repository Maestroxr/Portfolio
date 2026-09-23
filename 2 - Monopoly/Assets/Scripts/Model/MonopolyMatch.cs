using System;
using System.Collections.Generic;
using System.Linq;
using Gamebox.Lockstep;

namespace Portfolio.Monopoly
{
    /// <summary>
    /// The rules engine of one match. It knows nothing of Unity objects: the game manager calls its actions for the
    /// players (roll, buy, bid, build, trade...), each action applies the rules at once and records what happened as a
    /// list of <see cref="MatchEvent"/>s, which the view takes with <see cref="TakeEvents"/> and animates. Between
    /// actions the match waits in a <see cref="MatchPhase"/> for the decision of <see cref="Decider"/>. The state is
    /// plain serializable data, so a match in progress is saved as JSON and restored with <see cref="Attach"/>.
    /// </summary>
    [Serializable]
    public sealed partial class MonopolyMatch
    {
        /// <summary>The houses value of a property with a hotel.</summary>
        public const int Hotel = 5;
        public const int MinimumBid = 10;
        public const int MinimumRaise = 10;
        public const int MaxPlayers = 6;

        public RuleSet rules = new RuleSet();
        public List<PlayerState> players = new List<PlayerState>();
        public List<DeedState> deeds = new List<DeedState>();
        public int current;
        public MatchPhase phase = MatchPhase.Setup;
        public int round = 1;
        public int turn;
        public int housesLeft;
        public int hotelsLeft;
        /// <summary>The Free Parking pot (jackpot house rule).</summary>
        public int pot;
        public DiceRoll lastRoll;
        public CardDeck chanceDeck = new CardDeck { kind = CardDeckKind.Chance };
        public CardDeck chestDeck = new CardDeck { kind = CardDeckKind.CommunityChest };
        /// <summary>The property the current player is deciding to buy, or -1.</summary>
        public int pendingPurchase = -1;
        public AuctionState auction = new AuctionState();
        /// <summary>Properties of a player bankrupt to the bank, waiting for their auction.</summary>
        public List<int> auctionQueue = new List<int>();
        /// <summary>Debts that could not be paid on the spot; the first is being raised.</summary>
        public List<Debt> debts = new List<Debt>();
        /// <summary>Whether the <see cref="MatchEventKind.DebtStarted"/> of the first debt was recorded.</summary>
        public bool debtAnnounced;
        /// <summary>The current player rolled doubles and rolls again once this move is resolved.</summary>
        public bool extraRoll;
        /// <summary>Speed die: Mr. Monopoly moves the current player on once this move is resolved.</summary>
        public bool mrMonopolyPending;
        /// <summary>Steps the current player still moves once the current debt is paid (the fine after the last jail roll).</summary>
        public int pendingMove;
        public int bankruptcies;
        public int winner = -1;
        /// <summary>The seed the decks were shuffled with (kept for debugging a saved match).</summary>
        public int seed;

        [NonSerialized] private BoardLayout board;
        [NonSerialized] private IRandom random;
        [NonSerialized] private List<MatchEvent> events = new List<MatchEvent>();
        [NonSerialized] private Queue<DiceRoll> scriptedRolls = new Queue<DiceRoll>();

        public MonopolyMatch()
        {
        }

        public MonopolyMatch(BoardLayout board, RuleSet rules, IRandom random)
        {
            this.rules = rules != null ? rules.Clone() : new RuleSet();
            Attach(board, random);
        }

        /// <summary>Gives a deserialized match its board and random numbers back.</summary>
        public void Attach(BoardLayout layout, IRandom randomSource)
        {
            board = layout ?? throw new ArgumentNullException(nameof(layout));
            random = randomSource ?? new SystemRandom();
            events ??= new List<MatchEvent>();
            scriptedRolls ??= new Queue<DiceRoll>();
            rules ??= new RuleSet();
        }

        public BoardLayout Board => board;

        public PlayerState Current => players[current];

        public int SpaceCount => board.Count;

        public int ActiveCount => players.Count(p => !p.bankrupt);

        public IEnumerable<PlayerState> ActivePlayers => players.Where(p => !p.bankrupt);

        public bool IsOver => phase == MatchPhase.GameOver;

        public Debt CurrentDebt => phase == MatchPhase.RaiseFunds && debts.Count > 0 ? debts[0] : null;

        /// <summary>The player who has to act now, or -1 (setup, game over).</summary>
        public int Decider
        {
            get
            {
                switch (phase)
                {
                    case MatchPhase.Auction:
                        return auction.Running ? auction.Bidder : -1;
                    case MatchPhase.RaiseFunds:
                        return debts.Count > 0 ? debts[0].debtor : -1;
                    case MatchPhase.Setup:
                    case MatchPhase.GameOver:
                        return -1;
                    default:
                        return current;
                }
            }
        }

        /// <summary>Whether the speed die is rolled for the current player.</summary>
        public bool UsesSpeedDie(PlayerState player)
        {
            return rules.speedDie && player.passedGo && !player.inJail;
        }

        // ------------------------------------------------------------------ setup

        public PlayerState AddPlayer(string name, int token, bool bot = false, BotLevel level = BotLevel.Normal)
        {
            if (phase != MatchPhase.Setup)
            {
                throw new InvalidOperationException("Players join before the match starts.");
            }
            if (players.Count >= MaxPlayers)
            {
                throw new InvalidOperationException($"A match has at most {MaxPlayers} players.");
            }
            var player = new PlayerState { index = players.Count, name = name, token = token, bot = bot, level = level, color = players.Count };
            players.Add(player);
            return player;
        }

        /// <summary>Shuffles the decks, hands out the starting cash (and the dealt deeds of the short game) and starts the first turn.</summary>
        public void Start(int firstPlayer = 0)
        {
            if (players.Count < 2)
            {
                throw new InvalidOperationException("A match needs at least two players.");
            }
            if (!board.Check(out string error))
            {
                throw new InvalidOperationException(error);
            }
            seed = random.Range(0, int.MaxValue);
            deeds = new List<DeedState>();
            for (int i = 0; i < board.Count; i++)
            {
                deeds.Add(new DeedState());
            }
            housesLeft = rules.limitedBuildings ? board.houses : 999;
            hotelsLeft = rules.limitedBuildings ? board.hotels : 999;
            foreach (PlayerState player in players)
            {
                player.cash = rules.startingCash;
                player.position = 0;
                player.inJail = false;
                player.jailTurns = 0;
                player.jailCards.Clear();
                player.bankrupt = false;
                player.passedGo = false;
                player.doubles = 0;
                player.place = 0;
            }
            chanceDeck = ShuffledDeck(CardDeckKind.Chance);
            chestDeck = ShuffledDeck(CardDeckKind.CommunityChest);
            pot = rules.freeParkingJackpot ? rules.jackpotSeed : 0;
            round = 1;
            turn = 0;
            winner = -1;
            bankruptcies = 0;
            current = Math.Max(0, Math.Min(firstPlayer, players.Count - 1));
            if (rules.dealtProperties > 0)
            {
                DealProperties();
            }
            if (pot > 0)
            {
                Emit(MatchEventKind.PotChanged, value: pot);
            }
            BeginTurn();
        }

        private CardDeck ShuffledDeck(CardDeckKind kind)
        {
            List<CardData> cards = board.Deck(kind);
            var deck = new CardDeck { kind = kind };
            for (int i = 0; i < cards.Count; i++)
            {
                if (!cards[i].party || rules.partyCards)
                {
                    deck.order.Add(i);
                }
            }
            Shuffle(deck.order);
            return deck;
        }

        private void Shuffle<T>(IList<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        /// <summary>Short game: every player is dealt title deeds at random (and pays their printed price).</summary>
        private void DealProperties()
        {
            List<int> pile = board.Properties.ToList();
            Shuffle(pile);
            int next = 0;
            for (int round = 0; round < rules.dealtProperties; round++)
            {
                for (int i = 0; i < players.Count && next < pile.Count; i++)
                {
                    int seat = (current + i) % players.Count;
                    int space = pile[next++];
                    PlayerState player = players[seat];
                    deeds[space].owner = seat;
                    Emit(MatchEventKind.Dealt, seat, space: space, value: board[space].price);
                    if (rules.payForDealtProperties)
                    {
                        int price = Math.Min(board[space].price, player.cash);
                        Transfer(seat, Party.Bank, price, $"Title deed: {board[space].name}");
                    }
                }
            }
        }

        // ------------------------------------------------------------------ events and dice

        /// <summary>The events since the last call, in order.</summary>
        public List<MatchEvent> TakeEvents()
        {
            var taken = new List<MatchEvent>(events);
            events.Clear();
            return taken;
        }

        public bool HasEvents => events.Count > 0;

        /// <summary>Makes the next rolls come out as given (tests and the screenshot tour).</summary>
        public void ScriptRoll(DiceRoll roll)
        {
            scriptedRolls.Enqueue(roll);
        }

        public void ClearScriptedRolls()
        {
            scriptedRolls.Clear();
        }

        private MatchEvent Emit(MatchEventKind kind, int player = -1, int other = -1, int space = -1, int from = -1, int value = 0,
            string text = null)
        {
            var e = new MatchEvent { kind = kind, player = player, other = other, space = space, from = from, value = value, text = text };
            events.Add(e);
            return e;
        }

        private void Emit(MatchEvent e)
        {
            events.Add(e);
        }

        private DiceRoll NextRoll(PlayerState player)
        {
            DiceRoll roll = scriptedRolls.Count > 0 ? scriptedRolls.Dequeue() : new DiceRoll(random.Range(1, 7), random.Range(1, 7));
            if (UsesSpeedDie(player))
            {
                if (roll.speed == SpeedFace.None)
                {
                    SpeedFace[] faces = { SpeedFace.One, SpeedFace.Two, SpeedFace.Three, SpeedFace.MrMonopoly, SpeedFace.MrMonopoly, SpeedFace.Bus };
                    roll.speed = faces[random.Range(0, faces.Length)];
                }
            }
            else
            {
                roll.speed = SpeedFace.None;
            }
            return roll;
        }

        private DiceRoll NextPlainRoll()
        {
            DiceRoll roll = scriptedRolls.Count > 0 ? scriptedRolls.Dequeue() : new DiceRoll(random.Range(1, 7), random.Range(1, 7));
            roll.speed = SpeedFace.None;
            return roll;
        }

        // ------------------------------------------------------------------ queries

        public SpaceData Space(int index)
        {
            return board[index];
        }

        public DeedState Deed(int index)
        {
            return deeds[index];
        }

        public int Owner(int space)
        {
            return deeds[space].owner;
        }

        public bool OwnsGroup(int player, ColorGroup group)
        {
            int[] members = board.Group(group);
            return members.Length > 0 && members.All(space => deeds[space].owner == player);
        }

        public int CountOwned(int player, ColorGroup group)
        {
            return board.Group(group).Count(space => deeds[space].owner == player);
        }

        public bool GroupHasBuildings(ColorGroup group)
        {
            return board.Group(group).Any(space => deeds[space].houses > 0);
        }

        public IEnumerable<int> PropertiesOf(int player)
        {
            return board.Properties.Where(space => deeds[space].owner == player);
        }

        public int Houses(int player)
        {
            return PropertiesOf(player).Where(space => deeds[space].houses < Hotel).Sum(space => deeds[space].houses);
        }

        public int Hotels(int player)
        {
            return PropertiesOf(player).Count(space => deeds[space].houses == Hotel);
        }

        /// <summary>The number of houses a set needs on every property before a hotel.</summary>
        public int HousesForHotel => Math.Max(1, Math.Min(4, rules.housesForHotel));

        /// <summary>What the buildings on a property cost (a hotel includes the houses turned in for it).</summary>
        public int BuildingValue(int space)
        {
            int houses = deeds[space].houses;
            int count = houses == Hotel ? HousesForHotel + 1 : houses;
            return count * board[space].houseCost;
        }

        /// <summary>Cash plus property at its printed price (half when mortgaged) plus buildings at cost: the short game's valuation.</summary>
        public int NetWorth(int player)
        {
            PlayerState p = players[player];
            if (p.bankrupt)
            {
                return 0;
            }
            int total = p.cash;
            foreach (int space in PropertiesOf(player))
            {
                total += deeds[space].mortgaged ? board[space].MortgageValue : board[space].price;
                total += BuildingValue(space);
            }
            return total;
        }

        /// <summary>The cash a player could raise by selling every building and mortgaging every property.</summary>
        public int LiquidValue(int player)
        {
            PlayerState p = players[player];
            int total = p.cash;
            foreach (int space in PropertiesOf(player))
            {
                total += BuildingValue(space) / 2;
                if (!deeds[space].mortgaged)
                {
                    total += board[space].MortgageValue;
                }
            }
            return total;
        }

        /// <summary>The rent a visitor of <paramref name="space"/> would pay now, with <paramref name="diceTotal"/> for utilities.</summary>
        public int Rent(int space, int diceTotal, int multiplier = 1)
        {
            DeedState deed = deeds[space];
            SpaceData data = board[space];
            if (!deed.Owned || deed.mortgaged)
            {
                return 0;
            }
            switch (data.kind)
            {
                case SpaceKind.Street:
                    if (deed.houses == 0)
                    {
                        return data.rent[0] * (OwnsGroup(deed.owner, data.group) ? 2 : 1) * multiplier;
                    }
                    return data.rent[Math.Min(deed.houses, 5)] * multiplier;
                case SpaceKind.Railroad:
                {
                    int owned = Math.Max(1, CountOwned(deed.owner, ColorGroup.Railroad));
                    return data.rent[Math.Min(owned, 4) - 1] * multiplier;
                }
                case SpaceKind.Utility:
                {
                    int owned = CountOwned(deed.owner, ColorGroup.Utility);
                    int factor = multiplier > 1 ? multiplier : owned >= 2 ? data.rent[1] : data.rent[0];
                    return factor * diceTotal;
                }
                default:
                    return 0;
            }
        }

        /// <summary>The standings: active players by net worth, then the bankrupt ones, latest bankruptcy first.</summary>
        public List<PlayerState> Standings()
        {
            if (phase == MatchPhase.GameOver)
            {
                return players.OrderBy(p => p.place == 0 ? int.MaxValue : p.place).ToList();
            }
            return players.Where(p => !p.bankrupt).OrderByDescending(p => NetWorth(p.index)).ThenByDescending(p => p.cash)
                .Concat(players.Where(p => p.bankrupt).OrderByDescending(p => p.bankruptRound)).ToList();
        }

        /// <summary>The Get Out of Jail Free card of a deck that is out of the deck (held by a player), or -1.</summary>
        private int HeldJailCard(CardDeckKind kind)
        {
            CardDeck deck = kind == CardDeckKind.Chance ? chanceDeck : chestDeck;
            List<CardData> cards = board.Deck(kind);
            for (int i = 0; i < cards.Count; i++)
            {
                if (cards[i].action == CardAction.GetOutOfJail && !deck.order.Contains(i))
                {
                    return i;
                }
            }
            return -1;
        }

        private void ReturnJailCard(CardDeckKind kind)
        {
            CardDeck deck = kind == CardDeckKind.Chance ? chanceDeck : chestDeck;
            deck.Return(HeldJailCard(kind));
        }
    }
}
