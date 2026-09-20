using System;
using System.Collections.Generic;
using System.Linq;

namespace Portfolio.Monopoly
{
    /// <summary>
    /// The computer players. <see cref="Act"/> makes exactly one move for the player the match waits for (build a house,
    /// roll, bid...), so the view can animate every step. Trades are judged by <see cref="TradeGain"/>: cash and
    /// properties at their price, plus what the sets the trade completes are worth to either side. <see cref="WouldAccept"/>
    /// answers offers made to a computer player and <see cref="ProposeTrade"/> looks for a deal that completes one of its
    /// sets. The three levels differ in the cash they keep back, how hard they bid and build, and how shrewdly they trade.
    /// </summary>
    public sealed class BotBrain
    {
        private readonly IRandom random;

        /// <summary>Offers a player turned down, with the round they were turned down in, so they are not made again at once.</summary>
        private readonly Dictionary<string, int> refused = new Dictionary<string, int>();

        /// <summary>
        /// How much of the worth of a set the other side completes counts against a trade: nearly all of it against a single
        /// opponent, less when the new set threatens several opponents at once.
        /// </summary>
        private static float OpponentFactor(MonopolyMatch match)
        {
            return 0.9f * (float)Math.Pow(1.0 / Math.Max(1, match.ActiveCount - 1), 0.7);
        }

        /// <summary>Rounds before a turned down offer may be made again.</summary>
        private const int RefusalMemory = 8;

        public BotBrain(IRandom random)
        {
            this.random = random ?? new SystemRandom();
        }

        private struct Profile
        {
            public int reserve;
            public float buyChance;
            public float bidFactor;
            public float buildFactor;
            public float tradeChance;
            public float greed;
        }

        private static Profile ProfileOf(BotLevel level)
        {
            switch (level)
            {
                case BotLevel.Easy:
                    return new Profile { reserve = 50, buyChance = 0.75f, bidFactor = 0.8f, buildFactor = 0.7f, tradeChance = 0f, greed = 1.0f };
                case BotLevel.Hard:
                    return new Profile { reserve = 110, buyChance = 1f, bidFactor = 1.2f, buildFactor = 1f, tradeChance = 0.6f, greed = 1.15f };
                default:
                    return new Profile { reserve = 90, buyChance = 0.95f, bidFactor = 1.05f, buildFactor = 0.9f, tradeChance = 0.35f, greed = 1.08f };
            }
        }

        /// <summary>Makes one move for the player the match waits for. Returns false when that player is not a computer player.</summary>
        public bool Act(MonopolyMatch match)
        {
            int seat = match.Decider;
            if (seat < 0 || !match.players[seat].bot)
            {
                return false;
            }
            Profile profile = ProfileOf(match.players[seat].level);
            switch (match.phase)
            {
                case MatchPhase.Roll:
                    return Manage(match, seat, profile) || match.Roll();
                case MatchPhase.JailChoice:
                    return DecideJail(match, seat, profile);
                case MatchPhase.BusChoice:
                    return match.ChooseBus(BestBusOption(match, seat));
                case MatchPhase.MoveAnywhere:
                    return match.ChooseDestination(BestDestination(match, seat));
                case MatchPhase.BuyChoice:
                    return DecideBuy(match, seat, profile);
                case MatchPhase.Auction:
                    return DecideBid(match, seat, profile);
                case MatchPhase.RaiseFunds:
                    return RaiseFunds(match, seat);
                case MatchPhase.EndTurn:
                    return Manage(match, seat, profile) || match.EndTurn();
                default:
                    return false;
            }
        }

        // ------------------------------------------------------------------ decisions

        private bool DecideJail(MonopolyMatch match, int seat, Profile profile)
        {
            PlayerState me = match.players[seat];
            // Early on it pays to get out and buy; once the board is built up, jail is a safe place to wait.
            bool dangerous = DangerousBoard(match, seat);
            if (!dangerous)
            {
                if (me.jailCards.Count > 0)
                {
                    return match.UseJailCard();
                }
                if (me.cash >= match.rules.jailFine + profile.reserve)
                {
                    return match.PayJailFine();
                }
            }
            return match.Roll();
        }

        /// <summary>Whether opponents have enough houses that roaming the board is costly.</summary>
        private static bool DangerousBoard(MonopolyMatch match, int seat)
        {
            int threat = 0;
            foreach (int space in match.Board.Properties)
            {
                DeedState deed = match.deeds[space];
                if (deed.Owned && deed.owner != seat && deed.houses >= 3)
                {
                    threat++;
                }
            }
            return threat >= 3;
        }

        private bool DecideBuy(MonopolyMatch match, int seat, Profile profile)
        {
            int space = match.pendingPurchase;
            PlayerState me = match.players[seat];
            SpaceData data = match.Board[space];
            bool completes = CompletesGroup(match, seat, space);
            bool blocks = BlocksOpponent(match, seat, space);
            int reserve = profile.reserve + Math.Min(ExpectedDanger(match, seat), 800) / 4;
            bool wanted = completes || blocks || me.cash - data.price >= reserve;
            if (wanted && random.Value() <= profile.buyChance)
            {
                if (me.cash >= data.price)
                {
                    return match.Buy();
                }
                // Worth mortgaging a loose property to complete a set.
                if (completes && RaiseCash(match, seat, data.price - me.cash, keepGroups: true))
                {
                    return true;
                }
            }
            return match.DeclineBuy();
        }

        private bool DecideBid(MonopolyMatch match, int seat, Profile profile)
        {
            AuctionState auction = match.auction;
            PlayerState me = match.players[seat];
            float value = Valuation(match, seat, auction.space) * profile.bidFactor;
            // A little noise so two computer players do not always stop at the same price.
            value *= 0.92f + (float)random.Value() * 0.16f;
            int limit = Math.Min((int)value, me.cash - profile.reserve / 2);
            int bid = auction.MinimumBid;
            if (bid <= limit)
            {
                // Jump ahead when far below the limit, to keep auctions short.
                int step = limit - bid > 150 ? 50 : limit - bid > 60 ? 20 : 0;
                return match.PlaceBid(seat, Math.Min(limit, bid + step));
            }
            return match.PassBid(seat);
        }

        private bool RaiseFunds(MonopolyMatch match, int seat)
        {
            Debt debt = match.CurrentDebt;
            if (debt == null)
            {
                return false;
            }
            PlayerState me = match.players[seat];
            if (me.cash >= debt.amount)
            {
                return match.PayDebt();
            }
            if (match.LiquidValue(seat) < debt.amount)
            {
                return match.DeclareBankruptcy();
            }
            if (RaiseCash(match, seat, debt.amount - me.cash, keepGroups: true) || RaiseCash(match, seat, debt.amount - me.cash, keepGroups: false))
            {
                return true;
            }
            return match.DeclareBankruptcy();
        }

        /// <summary>
        /// One step towards <paramref name="needed"/> cash: mortgage a property outside the sets, then (unless the sets
        /// are to be kept) sell a building or mortgage anything. Returns false when nothing could be done.
        /// </summary>
        private static bool RaiseCash(MonopolyMatch match, int seat, int needed, bool keepGroups)
        {
            if (needed <= 0)
            {
                return false;
            }
            List<int> owned = match.PropertiesOf(seat).ToList();
            foreach (int space in owned.Where(s => !IsInOwnedGroup(match, seat, s)).OrderBy(s => match.Board[s].MortgageValue))
            {
                if (match.CanMortgage(seat, space, out _))
                {
                    return match.Mortgage(seat, space);
                }
            }
            if (keepGroups)
            {
                return false;
            }
            foreach (int space in owned.OrderByDescending(s => match.deeds[s].houses))
            {
                if (match.CanSellBuilding(seat, space, out _))
                {
                    return match.SellBuilding(seat, space);
                }
            }
            foreach (int space in owned.OrderBy(s => match.Board[s].MortgageValue))
            {
                if (match.CanMortgage(seat, space, out _))
                {
                    return match.Mortgage(seat, space);
                }
            }
            return false;
        }

        /// <summary>Builds and lifts mortgages on the player's own turn. Returns true when it did something.</summary>
        private bool Manage(MonopolyMatch match, int seat, Profile profile)
        {
            PlayerState me = match.players[seat];
            int reserve = profile.reserve + Math.Min(ExpectedDanger(match, seat), 800) / 3;

            // Lift mortgages of sets first, which lets them be built on again.
            foreach (int space in match.PropertiesOf(seat).Where(s => match.deeds[s].mortgaged)
                         .OrderByDescending(s => IsInOwnedGroup(match, seat, s)).ThenBy(s => match.Board[s].UnmortgageCost))
            {
                if (me.cash - match.Board[space].UnmortgageCost >= reserve * 2 && match.CanUnmortgage(seat, space, out _))
                {
                    return match.Unmortgage(seat, space);
                }
            }

            // Build evenly, the best return first, keeping a reserve.
            int bestSpace = -1;
            float bestScore = 0f;
            foreach (int space in match.PropertiesOf(seat))
            {
                if (!match.CanBuild(seat, space, out _))
                {
                    continue;
                }
                SpaceData data = match.Board[space];
                if (me.cash - data.houseCost < reserve)
                {
                    continue;
                }
                int houses = match.deeds[space].houses;
                int next = Math.Min(houses + 1, 5);
                float gain = (data.rent[next] - data.rent[Math.Min(houses, 5)]) / (float)data.houseCost;
                // The rent jumps up to the third house; those come first.
                float score = gain * (houses < 3 ? 1.5f : 1f) * profile.buildFactor;
                if (score > bestScore)
                {
                    bestScore = score;
                    bestSpace = space;
                }
            }
            if (bestSpace >= 0 && random.Value() < 0.9f + profile.buildFactor * 0.1f)
            {
                return match.Build(seat, bestSpace);
            }
            return false;
        }

        // ------------------------------------------------------------------ trading

        /// <summary>
        /// How much better off <paramref name="seat"/> is after <paramref name="offer"/>, in dollars: cash and property
        /// at their price, plus the worth of the sets the trade completes for them, minus most of the worth of the sets it
        /// completes for the other side.
        /// </summary>
        public static float TradeGain(MonopolyMatch match, TradeOffer offer, int seat)
        {
            bool proposer = seat == offer.from;
            int other = proposer ? offer.to : offer.from;
            List<int> incoming = proposer ? offer.getSpaces : offer.giveSpaces;
            List<int> outgoing = proposer ? offer.giveSpaces : offer.getSpaces;
            float gain = proposer ? offer.getCash - offer.giveCash : offer.giveCash - offer.getCash;
            gain += (proposer ? offer.getJailCards - offer.giveJailCards : offer.giveJailCards - offer.getJailCards) * 40f;
            foreach (int space in incoming)
            {
                gain += Worth(match, space) - (match.deeds[space].mortgaged ? (match.Board[space].MortgageValue + 9) / 10 : 0);
            }
            foreach (int space in outgoing)
            {
                gain -= Worth(match, space);
            }
            Func<int, int> before = space => match.deeds[space].owner;
            Func<int, int> after = space => incoming.Contains(space) ? seat : outgoing.Contains(space) ? other : match.deeds[space].owner;
            float opponent = OpponentFactor(match);
            foreach (ColorGroup group in incoming.Concat(outgoing).Select(space => match.Board[space].group).Distinct())
            {
                gain += SetWorth(match, group, seat, after) - SetWorth(match, group, seat, before);
                gain -= opponent * (SetWorth(match, group, other, after) - SetWorth(match, group, other, before));
            }
            return gain;
        }

        private static float Worth(MonopolyMatch match, int space)
        {
            return match.deeds[space].mortgaged ? match.Board[space].price * 0.5f : match.Board[space].price;
        }

        /// <summary>
        /// The worth of a set to a player beyond the prices of its properties: a complete street set is worth about its
        /// price again (the rent doubles and houses can go up), stations and utilities by how many are held.
        /// </summary>
        private static float SetWorth(MonopolyMatch match, ColorGroup group, int seat, Func<int, int> owner)
        {
            int[] members = match.Board.Group(group);
            if (members.Length == 0 || group == ColorGroup.None)
            {
                return 0f;
            }
            int held = members.Count(space => owner(space) == seat);
            if (group == ColorGroup.Railroad)
            {
                return held >= 4 ? 450f : held == 3 ? 220f : held == 2 ? 70f : 0f;
            }
            if (group == ColorGroup.Utility)
            {
                return held >= 2 ? 90f : 0f;
            }
            float total = members.Sum(space => (float)match.Board[space].price);
            if (held == members.Length)
            {
                return total * SetFactor(group);
            }
            return held == members.Length - 1 ? total * 0.12f : 0f;
        }

        /// <summary>Sets whose rents repay their houses fastest are worth more.</summary>
        private static float SetFactor(ColorGroup group)
        {
            switch (group)
            {
                case ColorGroup.Orange:
                    return 1.5f;
                case ColorGroup.Red:
                case ColorGroup.LightBlue:
                    return 1.35f;
                case ColorGroup.Pink:
                    return 1.3f;
                case ColorGroup.Yellow:
                    return 1.2f;
                case ColorGroup.DarkBlue:
                    return 1.15f;
                case ColorGroup.Green:
                    return 1.05f;
                default:
                    return 1.1f;
            }
        }

        /// <summary>The margin a player wants from a trade: a share of what changes hands, smaller once a match drags on.</summary>
        private static float Threshold(MonopolyMatch match, TradeOffer offer, BotLevel level)
        {
            float volume = offer.giveCash + offer.getCash + offer.giveSpaces.Concat(offer.getSpaces).Sum(space => match.Board[space].price);
            float margin = (ProfileOf(level).greed - 1f) * volume + 5f;
            return match.round > 30 ? margin * 0.5f : margin;
        }

        /// <summary>Whether the computer player <paramref name="offer"/>.to accepts the offer.</summary>
        public bool WouldAccept(MonopolyMatch match, TradeOffer offer)
        {
            if (!match.CanTrade(offer, out _))
            {
                return false;
            }
            PlayerState me = match.players[offer.to];
            Profile profile = ProfileOf(me.level);
            if (offer.getCash > 0 && me.cash - offer.getCash + offer.giveCash < profile.reserve / 2)
            {
                return false;
            }
            return TradeGain(match, offer, offer.to) >= Threshold(match, offer, me.level);
        }

        /// <summary>A trade the computer player <paramref name="seat"/> would like to offer now, or null.</summary>
        public TradeOffer ProposeTrade(MonopolyMatch match, int seat)
        {
            PlayerState me = match.players[seat];
            Profile profile = ProfileOf(me.level);
            if (!me.bot || profile.tradeChance <= 0f || !match.CanManage(seat) || random.Value() > profile.tradeChance)
            {
                return null;
            }
            TradeOffer best = null;
            float bestGain = 0f;
            foreach (ColorGroup group in Enum.GetValues(typeof(ColorGroup)))
            {
                int[] members = match.Board.Group(group);
                if (members.Length < 2 || group == ColorGroup.Utility)
                {
                    continue;
                }
                List<int> missing = members.Where(space => match.deeds[space].owner != seat).ToList();
                if (missing.Count != 1)
                {
                    continue;
                }
                int wanted = missing[0];
                int owner = match.deeds[wanted].owner;
                if (owner < 0 || match.players[owner].bankrupt || (match.Board[wanted].kind == SpaceKind.Street && match.GroupHasBuildings(group)))
                {
                    continue;
                }
                foreach (TradeOffer candidate in Candidates(match, seat, owner, wanted, profile))
                {
                    if (WasRefused(match, candidate) || !match.CanTrade(candidate, out _))
                    {
                        continue;
                    }
                    float mine = TradeGain(match, candidate, seat);
                    float theirs = TradeGain(match, candidate, owner);
                    if (mine > bestGain && theirs >= Threshold(match, candidate, match.players[owner].level))
                    {
                        best = candidate;
                        bestGain = mine;
                    }
                }
            }
            return best;
        }

        /// <summary>
        /// Offers for <paramref name="wanted"/>: a loose property of the proposer (or none) with the least cash either
        /// way that should satisfy the owner.
        /// </summary>
        private IEnumerable<TradeOffer> Candidates(MonopolyMatch match, int seat, int owner, int wanted, Profile profile)
        {
            PlayerState me = match.players[seat];
            PlayerState them = match.players[owner];
            int budget = Math.Max(0, me.cash - profile.reserve);
            var gifts = new List<int> { -1 };
            gifts.AddRange(match.TradableProperties(seat)
                .Where(space => !IsInOwnedGroup(match, seat, space) && match.Board[space].group != match.Board[wanted].group)
                .OrderByDescending(space => match.Board[space].price).Take(8));
            float greed = ProfileOf(them.level).greed - 1f;
            foreach (int gift in gifts)
            {
                var offer = new TradeOffer { from = seat, to = owner };
                offer.getSpaces.Add(wanted);
                if (gift >= 0)
                {
                    offer.giveSpaces.Add(gift);
                }
                // What the owner makes of it without cash, and the cash that lifts it over their margin.
                float theirs = TradeGain(match, offer, owner);
                float volume = offer.giveSpaces.Concat(offer.getSpaces).Sum(space => match.Board[space].price);
                float need = greed * volume + 5f - theirs;
                if (match.round > 30)
                {
                    need = (greed * volume + 5f) * 0.5f - theirs;
                }
                int cash = need >= 0f ? RoundUpTen(need / Math.Max(0.5f, 1f - greed)) + 10 : -RoundDownTen(-need / (1f + greed));
                if (cash > 0)
                {
                    if (cash > budget)
                    {
                        continue;
                    }
                    offer.giveCash = cash;
                }
                else if (cash < 0)
                {
                    offer.getCash = Math.Min(-cash, Math.Max(0, them.cash - 50));
                }
                if (!offer.IsEmpty)
                {
                    yield return offer;
                }
            }
        }

        private bool WasRefused(MonopolyMatch match, TradeOffer offer)
        {
            return refused.TryGetValue(Key(offer), out int round) && match.round - round < RefusalMemory;
        }

        /// <summary>Remembers a declined offer so it is not made again for a while.</summary>
        public void Refused(MonopolyMatch match, TradeOffer offer)
        {
            refused[Key(offer)] = match.round;
        }

        private static string Key(TradeOffer offer)
        {
            return $"{offer.from}>{offer.to}:{string.Join(",", offer.giveSpaces)}|{string.Join(",", offer.getSpaces)}";
        }

        private static int RoundUpTen(float value)
        {
            return (int)Math.Ceiling(value / 10f) * 10;
        }

        private static int RoundDownTen(float value)
        {
            return (int)Math.Floor(value / 10f) * 10;
        }

        // ------------------------------------------------------------------ valuation helpers

        /// <summary>What a property is worth to the player at auction: its price, more when it completes or blocks a set.</summary>
        public static float Valuation(MonopolyMatch match, int seat, int space)
        {
            SpaceData data = match.Board[space];
            float value = data.price;
            if (CompletesGroup(match, seat, space))
            {
                value *= 1.9f;
            }
            else if (BlocksOpponent(match, seat, space))
            {
                value *= 1.35f;
            }
            else if (match.CountOwned(seat, data.group) > 0)
            {
                value *= 1.2f;
            }
            if (data.kind == SpaceKind.Railroad)
            {
                value *= 1f + 0.15f * match.CountOwned(seat, ColorGroup.Railroad);
            }
            if (data.kind == SpaceKind.Utility)
            {
                value *= 0.85f;
            }
            return value;
        }

        private static bool CompletesGroup(MonopolyMatch match, int seat, int space)
        {
            int[] members = match.Board.Group(match.Board[space].group);
            return members.Length > 1 && members.All(s => s == space || match.deeds[s].owner == seat);
        }

        private static bool BlocksOpponent(MonopolyMatch match, int seat, int space)
        {
            int[] members = match.Board.Group(match.Board[space].group);
            if (members.Length < 2)
            {
                return false;
            }
            foreach (PlayerState other in match.players)
            {
                if (other.index != seat && !other.bankrupt && members.All(s => s == space || match.deeds[s].owner == other.index))
                {
                    return true;
                }
            }
            return false;
        }

        private static bool IsInOwnedGroup(MonopolyMatch match, int seat, int space)
        {
            SpaceData data = match.Board[space];
            return data.kind == SpaceKind.Street && match.OwnsGroup(seat, data.group);
        }

        /// <summary>The worst rent an opponent's property charges now: the cash worth keeping back.</summary>
        private static int ExpectedDanger(MonopolyMatch match, int seat)
        {
            int worst = 0;
            foreach (int space in match.Board.Properties)
            {
                DeedState deed = match.deeds[space];
                if (deed.Owned && deed.owner != seat && !deed.mortgaged)
                {
                    worst = Math.Max(worst, match.Rent(space, 7));
                }
            }
            return worst;
        }

        private static int BestBusOption(MonopolyMatch match, int seat)
        {
            int[] options = match.BusOptions;
            int best = 2;
            float bestScore = float.MinValue;
            for (int i = 0; i < options.Length; i++)
            {
                float score = SpaceScore(match, seat, (match.players[seat].position + options[i]) % match.SpaceCount);
                if (score > bestScore)
                {
                    bestScore = score;
                    best = i;
                }
            }
            return best;
        }

        private static int BestDestination(MonopolyMatch match, int seat)
        {
            int best = 0;
            float bestScore = float.MinValue;
            for (int space = 0; space < match.SpaceCount; space++)
            {
                float score = SpaceScore(match, seat, space);
                if (score > bestScore)
                {
                    bestScore = score;
                    best = space;
                }
            }
            return best;
        }

        /// <summary>How good landing on <paramref name="space"/> would be for the player.</summary>
        private static float SpaceScore(MonopolyMatch match, int seat, int space)
        {
            SpaceData data = match.Board[space];
            PlayerState me = match.players[seat];
            if (data.IsProperty)
            {
                DeedState deed = match.deeds[space];
                if (!deed.Owned)
                {
                    return me.cash >= data.price ? Valuation(match, seat, space) : 20f;
                }
                if (deed.owner == seat || deed.mortgaged)
                {
                    return 10f;
                }
                return -match.Rent(space, 7);
            }
            switch (data.kind)
            {
                case SpaceKind.GoToJail:
                    return -150f;
                case SpaceKind.Tax:
                    return -data.tax;
                case SpaceKind.FreeParking:
                    return match.rules.freeParkingJackpot ? match.pot : 5f;
                case SpaceKind.Go:
                    return match.rules.salary * (match.rules.doubleSalaryOnGo ? 2f : 1f);
                case SpaceKind.Chance:
                case SpaceKind.CommunityChest:
                    return 15f;
                default:
                    return 5f;
            }
        }
    }
}
