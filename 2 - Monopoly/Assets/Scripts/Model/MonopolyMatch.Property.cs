using System.Collections.Generic;
using System.Linq;

namespace Portfolio.Monopoly
{
    public sealed partial class MonopolyMatch
    {
        // ------------------------------------------------------------------ buying and auctions

        /// <summary>Whether the current player can pay for the property they landed on.</summary>
        public bool CanAffordPurchase => phase == MatchPhase.BuyChoice && pendingPurchase >= 0 && Current.cash >= board[pendingPurchase].price;

        public bool Buy()
        {
            if (!CanAffordPurchase)
            {
                return false;
            }
            int space = pendingPurchase;
            int price = board[space].price;
            pendingPurchase = -1;
            Transfer(current, Party.Bank, price, $"Bought {board[space].name}");
            deeds[space].owner = current;
            Emit(MatchEventKind.Bought, current, space: space, value: price);
            Continue();
            return true;
        }

        /// <summary>Leaves the property: it goes to auction, or stays with the bank when auctions are off.</summary>
        public bool DeclineBuy()
        {
            if (phase != MatchPhase.BuyChoice || pendingPurchase < 0)
            {
                return false;
            }
            int space = pendingPurchase;
            pendingPurchase = -1;
            if (rules.auctions)
            {
                StartAuction(space, current);
            }
            Continue();
            return true;
        }

        /// <summary>Opens an auction every active player takes part in, starting with the player after <paramref name="after"/>.</summary>
        private void StartAuction(int space, int after)
        {
            var state = new AuctionState { space = space, highBid = 0, highBidder = Party.Bank };
            int count = players.Count;
            for (int i = 1; i <= count; i++)
            {
                int seat = (after + i) % count;
                if (!players[seat].bankrupt)
                {
                    state.bidders.Add(seat);
                }
            }
            auction = state;
            Emit(MatchEventKind.AuctionStarted, after, space: space, value: board[space].price);
            DropBiddersWhoCannotBid();
        }

        public bool PlaceBid(int player, int amount)
        {
            if (phase != MatchPhase.Auction || !auction.Running || auction.Bidder != player)
            {
                return false;
            }
            if (amount < auction.MinimumBid || amount > players[player].cash)
            {
                return false;
            }
            auction.highBid = amount;
            auction.highBidder = player;
            Emit(MatchEventKind.Bid, player, space: auction.space, value: amount);
            auction.turn = (auction.turn + 1) % auction.bidders.Count;
            DropBiddersWhoCannotBid();
            Continue();
            return true;
        }

        /// <summary>The current bidder drops out of the auction.</summary>
        public bool PassBid(int player)
        {
            if (phase != MatchPhase.Auction || !auction.Running || auction.Bidder != player)
            {
                return false;
            }
            RemoveBidder(player);
            DropBiddersWhoCannotBid();
            Continue();
            return true;
        }

        private void RemoveBidder(int player)
        {
            int index = auction.bidders.IndexOf(player);
            if (index < 0)
            {
                return;
            }
            int turnIndex = auction.bidders.Count > 0 ? auction.turn % auction.bidders.Count : 0;
            auction.bidders.RemoveAt(index);
            Emit(MatchEventKind.BidPassed, player, space: auction.space);
            if (auction.bidders.Count == 0)
            {
                auction.turn = 0;
                return;
            }
            if (index < turnIndex)
            {
                turnIndex--;
            }
            auction.turn = turnIndex % auction.bidders.Count;
        }

        /// <summary>Bidders whose cash does not cover the next bid drop out (the high bidder stays in).</summary>
        private void DropBiddersWhoCannotBid()
        {
            for (int guard = 0; guard < 16 && auction.bidders.Count > 0; guard++)
            {
                int bidder = auction.Bidder;
                if (bidder == auction.highBidder || players[bidder].cash >= auction.MinimumBid)
                {
                    return;
                }
                RemoveBidder(bidder);
            }
        }

        /// <summary>Everybody dropped out, or only the high bidder is left.</summary>
        private bool AuctionDone => auction.bidders.Count == 0 || (auction.bidders.Count == 1 && auction.bidders[0] == auction.highBidder);

        private void CloseAuction()
        {
            int space = auction.space;
            int winnerSeat = auction.highBidder;
            int price = auction.highBid;
            auction = new AuctionState();
            if (winnerSeat >= 0)
            {
                Transfer(winnerSeat, Party.Bank, price, $"Won {board[space].name} at auction");
                deeds[space].owner = winnerSeat;
                Emit(MatchEventKind.Bought, winnerSeat, space: space, value: price, text: "auction");
            }
            else
            {
                Emit(MatchEventKind.AuctionUnsold, space: space);
            }
        }

        // ------------------------------------------------------------------ building and mortgages

        /// <summary>Whether <paramref name="player"/> may build, sell, mortgage or trade now: on their own turn, or while raising funds.</summary>
        public bool CanManage(int player)
        {
            if (player < 0 || player >= players.Count || players[player].bankrupt)
            {
                return false;
            }
            switch (phase)
            {
                case MatchPhase.Roll:
                case MatchPhase.EndTurn:
                case MatchPhase.BuyChoice:
                case MatchPhase.JailChoice:
                    return player == current;
                case MatchPhase.RaiseFunds:
                    return debts.Count > 0 && debts[0].debtor == player;
                default:
                    return false;
            }
        }

        public bool CanBuild(int player, int space, out string reason)
        {
            if (!CanManage(player))
            {
                reason = "Build on your own turn.";
                return false;
            }
            if (phase == MatchPhase.RaiseFunds)
            {
                reason = "Pay your debt first.";
                return false;
            }
            if (!CanBuildRules(player, space, out reason))
            {
                return false;
            }
            if (players[player].cash < board[space].houseCost)
            {
                reason = $"A building costs ${board[space].houseCost}.";
                return false;
            }
            return true;
        }

        /// <summary>The building rules without the money: a complete, unmortgaged set, even building and the bank's supply.</summary>
        private bool CanBuildRules(int player, int space, out string reason)
        {
            SpaceData data = board[space];
            DeedState deed = deeds[space];
            if (data.kind != SpaceKind.Street || deed.owner != player)
            {
                reason = "Build on your own streets.";
                return false;
            }
            int[] group = board.Group(data.group);
            if (!OwnsGroup(player, data.group))
            {
                reason = "Own the whole color set first.";
                return false;
            }
            if (group.Any(s => deeds[s].mortgaged))
            {
                reason = "Lift the mortgages of the set first.";
                return false;
            }
            if (deed.houses >= Hotel)
            {
                reason = "This street already has a hotel.";
                return false;
            }
            if (rules.evenBuilding && deed.houses > group.Min(s => deeds[s].houses))
            {
                reason = "Build evenly across the set.";
                return false;
            }
            bool hotel = deed.houses >= HousesForHotel;
            if (hotel && hotelsLeft <= 0)
            {
                reason = "The bank has no hotels left.";
                return false;
            }
            if (!hotel && housesLeft <= 0)
            {
                reason = "The bank has no houses left.";
                return false;
            }
            reason = null;
            return true;
        }

        /// <summary>Buys a house (or, on a full set of houses, a hotel) for <paramref name="space"/>.</summary>
        public bool Build(int player, int space)
        {
            if (!CanBuild(player, space, out _))
            {
                return false;
            }
            PlaceBuilding(player, space, false);
            return true;
        }

        private void PlaceBuilding(int player, int space, bool free)
        {
            DeedState deed = deeds[space];
            if (!free)
            {
                Transfer(player, Party.Bank, board[space].houseCost, $"Building on {board[space].name}");
            }
            if (deed.houses >= HousesForHotel)
            {
                housesLeft += deed.houses;
                hotelsLeft--;
                deed.houses = Hotel;
            }
            else
            {
                housesLeft--;
                deed.houses++;
            }
            Emit(MatchEventKind.Built, player, space: space, value: deed.houses, text: free ? "free" : null);
        }

        public bool CanSellBuilding(int player, int space, out string reason)
        {
            if (!CanManage(player))
            {
                reason = "Sell on your own turn.";
                return false;
            }
            DeedState deed = deeds[space];
            if (deed.owner != player || deed.houses <= 0)
            {
                reason = "There is nothing to sell here.";
                return false;
            }
            if (rules.evenBuilding && deed.houses < board.Group(board[space].group).Max(s => deeds[s].houses))
            {
                reason = "Sell evenly across the set.";
                return false;
            }
            reason = null;
            return true;
        }

        /// <summary>Sells a building back to the bank for half its price. A hotel turns back into houses when the bank has them.</summary>
        public bool SellBuilding(int player, int space)
        {
            if (!CanSellBuilding(player, space, out _))
            {
                return false;
            }
            DeedState deed = deeds[space];
            int cost = board[space].houseCost;
            int refund;
            if (deed.houses == Hotel)
            {
                int houses = System.Math.Min(HousesForHotel, housesLeft);
                refund = (HousesForHotel + 1 - houses) * cost / 2;
                housesLeft -= houses;
                hotelsLeft++;
                deed.houses = houses;
            }
            else
            {
                refund = cost / 2;
                deed.houses--;
                housesLeft++;
            }
            Transfer(Party.Bank, player, refund, $"Sold a building on {board[space].name}");
            Emit(MatchEventKind.SoldBuilding, player, space: space, value: deed.houses);
            AfterManage();
            return true;
        }

        public bool CanMortgage(int player, int space, out string reason)
        {
            if (!CanManage(player))
            {
                reason = "Mortgage on your own turn.";
                return false;
            }
            DeedState deed = deeds[space];
            if (deed.owner != player || !board[space].IsProperty)
            {
                reason = "Mortgage your own properties.";
                return false;
            }
            if (deed.mortgaged)
            {
                reason = "Already mortgaged.";
                return false;
            }
            if (board[space].kind == SpaceKind.Street && GroupHasBuildings(board[space].group))
            {
                reason = "Sell the buildings of the set first.";
                return false;
            }
            reason = null;
            return true;
        }

        public bool Mortgage(int player, int space)
        {
            if (!CanMortgage(player, space, out _))
            {
                return false;
            }
            deeds[space].mortgaged = true;
            Transfer(Party.Bank, player, board[space].MortgageValue, $"Mortgaged {board[space].name}");
            Emit(MatchEventKind.Mortgaged, player, space: space, value: board[space].MortgageValue);
            AfterManage();
            return true;
        }

        public bool CanUnmortgage(int player, int space, out string reason)
        {
            if (!CanManage(player) || phase == MatchPhase.RaiseFunds)
            {
                reason = "Lift mortgages on your own turn.";
                return false;
            }
            DeedState deed = deeds[space];
            if (deed.owner != player || !deed.mortgaged)
            {
                reason = "Not mortgaged.";
                return false;
            }
            if (players[player].cash < board[space].UnmortgageCost)
            {
                reason = $"Lifting the mortgage costs ${board[space].UnmortgageCost}.";
                return false;
            }
            reason = null;
            return true;
        }

        public bool Unmortgage(int player, int space)
        {
            if (!CanUnmortgage(player, space, out _))
            {
                return false;
            }
            Transfer(player, Party.Bank, board[space].UnmortgageCost, $"Lifted the mortgage of {board[space].name}");
            deeds[space].mortgaged = false;
            Emit(MatchEventKind.Unmortgaged, player, space: space, value: board[space].UnmortgageCost);
            return true;
        }

        /// <summary>While raising funds, the debt is paid as soon as the debtor holds enough.</summary>
        private void AfterManage()
        {
            if (phase == MatchPhase.RaiseFunds && debts.Count > 0 && players[debts[0].debtor].cash >= debts[0].amount)
            {
                PayDebt();
            }
        }

        // ------------------------------------------------------------------ trading

        public bool CanTrade(TradeOffer offer, out string reason)
        {
            if (offer == null || offer.IsEmpty)
            {
                reason = "The trade is empty.";
                return false;
            }
            if (offer.from == offer.to || offer.from < 0 || offer.to < 0 || offer.from >= players.Count || offer.to >= players.Count)
            {
                reason = "Pick another player to trade with.";
                return false;
            }
            if (!CanManage(offer.from))
            {
                reason = "Trade on your own turn.";
                return false;
            }
            PlayerState from = players[offer.from];
            PlayerState to = players[offer.to];
            if (to.bankrupt)
            {
                reason = $"{to.name} is out of the game.";
                return false;
            }
            if (offer.giveSpaces.Distinct().Count() != offer.giveSpaces.Count || offer.getSpaces.Distinct().Count() != offer.getSpaces.Count)
            {
                reason = "A property is listed twice.";
                return false;
            }
            foreach (int space in offer.giveSpaces)
            {
                if (space < 0 || space >= board.Count || deeds[space].owner != offer.from)
                {
                    reason = "You can only give your own properties.";
                    return false;
                }
            }
            foreach (int space in offer.getSpaces)
            {
                if (space < 0 || space >= board.Count || deeds[space].owner != offer.to)
                {
                    reason = $"{to.name} does not own that property.";
                    return false;
                }
            }
            foreach (int space in offer.giveSpaces.Concat(offer.getSpaces))
            {
                if (board[space].kind == SpaceKind.Street && GroupHasBuildings(board[space].group))
                {
                    reason = $"Sell the buildings on the {board[space].group} set before trading it.";
                    return false;
                }
            }
            if (offer.giveCash < 0 || offer.getCash < 0 || offer.giveJailCards < 0 || offer.getJailCards < 0)
            {
                reason = "Amounts cannot be negative.";
                return false;
            }
            if (offer.giveCash > from.cash)
            {
                reason = $"You only have ${from.cash}.";
                return false;
            }
            if (offer.getCash > to.cash)
            {
                reason = $"{to.name} only has ${to.cash}.";
                return false;
            }
            if (offer.giveJailCards > from.jailCards.Count || offer.getJailCards > to.jailCards.Count)
            {
                reason = "Not enough Get Out of Jail Free cards.";
                return false;
            }
            int fromInterest = offer.getSpaces.Where(s => deeds[s].mortgaged).Sum(s => (board[s].MortgageValue + 9) / 10);
            int toInterest = offer.giveSpaces.Where(s => deeds[s].mortgaged).Sum(s => (board[s].MortgageValue + 9) / 10);
            if (from.cash - offer.giveCash + offer.getCash < fromInterest)
            {
                reason = $"You need ${fromInterest} for the interest on the mortgaged properties.";
                return false;
            }
            if (to.cash - offer.getCash + offer.giveCash < toInterest)
            {
                reason = $"{to.name} cannot pay the interest on the mortgaged properties.";
                return false;
            }
            reason = null;
            return true;
        }

        /// <summary>Carries out an accepted trade. Received mortgaged properties cost their new owner ten percent interest.</summary>
        public bool ExecuteTrade(TradeOffer offer)
        {
            if (!CanTrade(offer, out _))
            {
                return false;
            }
            PlayerState from = players[offer.from];
            PlayerState to = players[offer.to];
            Transfer(offer.from, offer.to, offer.giveCash, "Trade");
            Transfer(offer.to, offer.from, offer.getCash, "Trade");
            foreach (int space in offer.giveSpaces)
            {
                deeds[space].owner = offer.to;
                Emit(MatchEventKind.OwnerChanged, offer.from, offer.to, space);
            }
            foreach (int space in offer.getSpaces)
            {
                deeds[space].owner = offer.from;
                Emit(MatchEventKind.OwnerChanged, offer.to, offer.from, space);
            }
            MoveJailCards(from, to, offer.giveJailCards);
            MoveJailCards(to, from, offer.getJailCards);
            foreach (int space in offer.getSpaces.Where(s => deeds[s].mortgaged))
            {
                Transfer(offer.from, Party.Bank, (board[space].MortgageValue + 9) / 10, $"Interest on {board[space].name}");
            }
            foreach (int space in offer.giveSpaces.Where(s => deeds[s].mortgaged))
            {
                Transfer(offer.to, Party.Bank, (board[space].MortgageValue + 9) / 10, $"Interest on {board[space].name}");
            }
            Emit(new MatchEvent { kind = MatchEventKind.Traded, player = offer.from, other = offer.to, trade = offer.Clone(), space = -1, from = -1 });
            AfterManage();
            return true;
        }

        private void MoveJailCards(PlayerState from, PlayerState to, int count)
        {
            for (int i = 0; i < count && from.jailCards.Count > 0; i++)
            {
                CardDeckKind card = from.jailCards[0];
                from.jailCards.RemoveAt(0);
                to.jailCards.Add(card);
                Emit(new MatchEvent { kind = MatchEventKind.JailCardGained, player = to.index, deck = card, other = from.index, space = -1, from = -1 });
            }
        }

        /// <summary>The properties a player could put into a trade now (no buildings on their set).</summary>
        public IEnumerable<int> TradableProperties(int player)
        {
            return PropertiesOf(player).Where(space => board[space].kind != SpaceKind.Street || !GroupHasBuildings(board[space].group));
        }
    }
}
