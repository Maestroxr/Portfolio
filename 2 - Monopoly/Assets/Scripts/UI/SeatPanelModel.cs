using System;
using System.Collections.Generic;
using System.Linq;

namespace Portfolio.Monopoly
{
    /// <summary>What a button of the remote panel does for its seat (<see cref="SeatPanelModel.Perform"/>).</summary>
    public enum PanelChoiceKind
    {
        Roll,
        PayFine,
        UseCard,
        /// <summary>The argument is the bus option.</summary>
        Bus,
        /// <summary>The argument is the space to move to.</summary>
        Destination,
        Buy,
        DeclineBuy,
        /// <summary>The argument is the amount.</summary>
        Bid,
        PassBid,
        EndTurn,
        Bankrupt,
        Accept,
        Decline,
        /// <summary>The argument of the four property choices is the space.</summary>
        Build,
        Sell,
        Mortgage,
        Unmortgage,
        /// <summary>Opens the trade builder of the panel (<see cref="TradeDraft"/>); sends nothing by itself.</summary>
        Trade
    }

    /// <summary>
    /// A trade a player puts together on the remote panel before proposing it: the other side, the properties and the
    /// cash each gives, a Get Out of Jail Free card each way. <see cref="ToOffer"/> is the offer the trade window of a
    /// device builds from its ticks and amounts, so the proposal that goes out is the same command either way.
    /// </summary>
    public sealed class TradeDraft
    {
        /// <summary>The seat that proposes the trade.</summary>
        public int from;
        /// <summary>The seat the offer goes to; -1 while none is picked.</summary>
        public int to = -1;
        /// <summary>The properties the proposer gives, in board order.</summary>
        public readonly List<int> giveSpaces = new List<int>();
        /// <summary>The properties the proposer asks for, in board order.</summary>
        public readonly List<int> getSpaces = new List<int>();
        /// <summary>The cash the proposer gives.</summary>
        public int giveCash;
        /// <summary>The cash the proposer asks for.</summary>
        public int getCash;
        /// <summary>Whether the proposer gives a Get Out of Jail Free card.</summary>
        public bool giveCard;
        /// <summary>Whether the proposer asks for a Get Out of Jail Free card.</summary>
        public bool getCard;

        /// <summary>The offer as the device's trade window builds it (one card at most each way, as its tick boxes allow).</summary>
        public TradeOffer ToOffer()
        {
            var offer = new TradeOffer { from = from, to = to, giveCash = giveCash, getCash = getCash, giveJailCards = giveCard ? 1 : 0, getJailCards = getCard ? 1 : 0 };
            offer.giveSpaces.AddRange(giveSpaces);
            offer.getSpaces.AddRange(getSpaces);
            return offer;
        }

        /// <summary>A text that changes exactly when the draft does: the panel redraws the builder on it.</summary>
        public string Key => $"{from}>{to}|{string.Join(",", giveSpaces)}|{string.Join(",", getSpaces)}|{giveCash}|{getCash}|{giveCard}|{getCard}";
    }

    /// <summary>A button of the remote panel: what it says, what it sends, and whether it can be pressed now.</summary>
    public sealed class PanelChoice
    {
        public PanelChoiceKind kind;
        public string label;
        /// <summary>The number the choice carries: an option, an amount, a space.</summary>
        public int argument;
        public bool enabled = true;
        /// <summary>The choice the seat is most likely to take: drawn in the accent colour.</summary>
        public bool primary;

        public static PanelChoice Of(PanelChoiceKind kind, string label, int argument = 0, bool enabled = true, bool primary = false)
        {
            return new PanelChoice { kind = kind, label = label, argument = argument, enabled = enabled, primary = primary };
        }

        /// <summary>Whether the choice manages property (a button of a property row) rather than answering what the match waits for.</summary>
        public bool IsManagement => kind >= PanelChoiceKind.Build && kind <= PanelChoiceKind.Unmortgage;

        public override string ToString()
        {
            return $"{kind} {argument} ({label}{(enabled ? "" : ", disabled")})";
        }
    }

    /// <summary>A property of the seat as the remote panel lists it, with the buttons it may press for it now.</summary>
    public sealed class PanelProperty
    {
        public int space;
        public string name;
        public ColorGroup group;
        /// <summary>Houses on it, <see cref="MonopolyMatch.Hotel"/> for a hotel.</summary>
        public int houses;
        public bool mortgaged;
        /// <summary>The buildings and the rent, or that it is mortgaged.</summary>
        public string state;
        public readonly List<PanelChoice> actions = new List<PanelChoice>();
    }

    /// <summary>A line of the standings the remote panel shows once the match is over.</summary>
    public sealed class PanelStanding
    {
        public int place;
        public int seat;
        public string name;
        public int worth;
        public bool bankrupt;
        public int bankruptRound;
    }

    /// <summary>
    /// What the remote panel of a seat shows, worked out from the match alone (<see cref="MonopolyRemotePanel"/> draws it,
    /// the online tour presses it, the tests read it): the seat's cash, square and worth, a line about what the table
    /// waits for, the choices of the seat as big buttons (<see cref="Choices"/>: the same decisions the action panel, the
    /// title deed, the auction and the offer window ask for on a full screen), the seat's properties with the build, sell
    /// and mortgage buttons of the property manager (<see cref="Properties"/>), and the standings once the match is over.
    /// A choice is sent through the one seam the interface uses (<see cref="Perform"/>, <see cref="IMonopolyCommands"/>).
    /// Whether the seat may decide, manage or answer right now is the caller's to say: it is what the online controller
    /// checks before it sends (<see cref="MonopolyOnlineController.MayDecide"/>), so a button shows exactly when the
    /// command behind it would go. A trade is put together in a <see cref="TradeDraft"/> (the partners, the properties
    /// either side may offer, the cash steps, the rules' verdict) and proposed as the device's trade window would.
    /// </summary>
    public static class SeatPanelModel
    {
        /// <summary>The raises the bid buttons offer over the high bid (the auction window's).</summary>
        public static readonly int[] BidRaises = { 10, 50, 100 };

        /// <summary>The steps the cash of a trade moves by on the panel (a minus and a plus button each).</summary>
        public static readonly int[] CashSteps = { 10, 100 };

        // ------------------------------------------------------------------ the seat

        /// <summary>The seat's cash as shown.</summary>
        public static string Cash(MonopolyMatch match, int seat)
        {
            return Holds(match, seat) ? MonopolyStyle.Money(match.players[seat].cash) : "";
        }

        /// <summary>The seat's net worth as shown, or that it is out of the game.</summary>
        public static string Worth(MonopolyMatch match, int seat)
        {
            if (!Holds(match, seat))
            {
                return "";
            }
            return match.players[seat].bankrupt ? L.T("Out of the game") : L.F("Net worth {0}", MonopolyStyle.Money(match.NetWorth(seat)));
        }

        /// <summary>Where the seat's token stands: the square, or jail; with the Get Out of Jail Free cards it holds.</summary>
        public static string Position(MonopolyMatch match, int seat)
        {
            if (!Holds(match, seat))
            {
                return "";
            }
            PlayerState player = match.players[seat];
            string where = player.inJail ? L.T("In jail") : L.F("On {0}", MonopolyStyle.SpaceName(match.Space(player.position)));
            if (player.jailCards.Count > 0)
            {
                where += "  •  " + (player.jailCards.Count == 1 ? L.T("1 Get Out of Jail Free card") : L.F("{0} Get Out of Jail Free cards", player.jailCards.Count));
            }
            return where;
        }

        // ------------------------------------------------------------------ what the table waits for

        /// <summary>
        /// The line over the buttons: who the table waits for and what for. Written for the player in <paramref name="seat"/>
        /// ("Roll the dice!" when it is their move), and about everybody else by name; <paramref name="seat"/> may be -1
        /// for a viewer with no seat (the shared screen), who is never spoken to as "you".
        /// </summary>
        public static string Headline(MonopolyMatch match, LockstepMatch table, int seat)
        {
            if (match == null)
            {
                return "";
            }
            if (match.IsOver)
            {
                return match.winner >= 0 ? L.F("{0} wins!", MonopolyStyle.NameOf(match.players[match.winner])) : L.T("Game over");
            }
            TradeOffer offer = table != null ? table.PendingOffer : null;
            if (offer != null)
            {
                return offer.to == seat ? L.F("{0} offers you a trade", MonopolyStyle.NameOf(match.players[offer.from]))
                    : L.F("{0} answers", MonopolyStyle.NameOf(match.players[offer.to]));
            }
            int decider = match.Decider;
            if (decider < 0)
            {
                return "";
            }
            PlayerState player = match.players[decider];
            if (decider != seat)
            {
                return match.phase == MatchPhase.Auction ? L.F("Auction: {0}", MonopolyStyle.SpaceName(match.Space(match.auction.space)))
                    : L.F("{0}'s move", MonopolyStyle.NameOf(player));
            }
            switch (match.phase)
            {
                case MatchPhase.Roll:
                    return player.doubles > 0 ? L.T("Doubles! Roll again.") : L.T("Roll the dice!");
                case MatchPhase.JailChoice:
                    return L.F("Attempt {0} of {1}: roll doubles to get out.", player.jailTurns + 1, match.rules.maxJailTurns);
                case MatchPhase.BusChoice:
                    return L.T("All aboard the bus!");
                case MatchPhase.MoveAnywhere:
                    return L.T("Triples! Pick where to go.");
                case MatchPhase.BuyChoice:
                    return L.F("For sale: {0}", MonopolyStyle.Money(match.Space(match.pendingPurchase).price));
                case MatchPhase.Auction:
                    return L.F("Auction: {0}", MonopolyStyle.SpaceName(match.Space(match.auction.space)));
                case MatchPhase.RaiseFunds:
                {
                    Debt debt = match.CurrentDebt;
                    return debt != null ? L.F("You owe {0}", MonopolyStyle.Money(debt.amount)) : "";
                }
                case MatchPhase.EndTurn:
                    return player.inJail ? L.T("Locked up! Build or trade, then end your turn.") : L.T("Build, trade, or end your turn.");
                default:
                    return "";
            }
        }

        /// <summary>A second line under the headline with the detail of the decision, "" when there is none.</summary>
        public static string Detail(MonopolyMatch match, LockstepMatch table, int seat)
        {
            if (match == null || match.IsOver)
            {
                return "";
            }
            TradeOffer offer = table != null ? table.PendingOffer : null;
            if (offer != null)
            {
                return offer.to == seat ? DescribeOffer(match, offer) : "";
            }
            int decider = match.Decider;
            if (decider < 0)
            {
                return "";
            }
            switch (match.phase)
            {
                case MatchPhase.BuyChoice:
                    // The headline of the buyer says the price; everybody else is told what is up for sale.
                    return decider == seat ? MonopolyStyle.SpaceName(match.Space(match.pendingPurchase))
                        : L.F("For sale: {0}", MonopolyStyle.SpaceName(match.Space(match.pendingPurchase)));
                case MatchPhase.Auction:
                {
                    AuctionState auction = match.auction;
                    string bidder = auction.Bidder == seat ? L.T("Your bid") : L.F("{0}'s bid", MonopolyStyle.NameOf(match.players[Math.Max(0, auction.Bidder)]));
                    return auction.highBidder >= 0
                        ? $"{L.F("{0} leads", MonopolyStyle.NameOf(match.players[auction.highBidder]))} {MonopolyStyle.Money(auction.highBid)}  •  {bidder}"
                        : $"{L.T("No bids yet")}  •  {bidder}";
                }
                case MatchPhase.RaiseFunds when decider == seat:
                {
                    Debt debt = match.CurrentDebt;
                    if (debt == null)
                    {
                        return "";
                    }
                    string creditor = debt.creditor >= 0 ? MonopolyStyle.NameOf(match.players[debt.creditor]) : debt.creditor == Party.Pot ? L.T("the Free Parking pot") : L.T("the bank");
                    return L.F("To {0}. Raise {1} by selling buildings or mortgaging, or give up.", creditor, MonopolyStyle.Money(debt.amount - match.players[seat].cash));
                }
                case MatchPhase.BusChoice when decider == seat:
                    return L.T("Move by either die, or both.");
                default:
                    return "";
            }
        }

        /// <summary>The offer on the table as its recipient reads it: what they get, what they give.</summary>
        public static string DescribeOffer(MonopolyMatch match, TradeOffer offer)
        {
            string get = DescribeSide(match, offer.giveSpaces, offer.giveCash, offer.giveJailCards);
            string give = DescribeSide(match, offer.getSpaces, offer.getCash, offer.getJailCards);
            return $"{L.T("You get")}: {get}  •  {L.T("You give")}: {give}";
        }

        private static string DescribeSide(MonopolyMatch match, List<int> spaces, int cash, int cards)
        {
            var parts = new List<string>();
            foreach (int space in spaces.OrderBy(s => s))
            {
                parts.Add(MonopolyStyle.SpaceName(match.Space(space)) + (match.Deed(space).mortgaged ? $" ({L.T("mortgaged")})" : ""));
            }
            if (cash > 0)
            {
                parts.Add(MonopolyStyle.Money(cash));
            }
            if (cards > 0)
            {
                parts.Add(cards == 1 ? L.T("1 Get Out of Jail Free card") : L.F("{0} Get Out of Jail Free cards", cards));
            }
            return parts.Count == 0 ? L.T("Nothing") : string.Join(", ", parts);
        }

        // ------------------------------------------------------------------ the choices

        /// <summary>
        /// The big buttons of the seat: the answer to the offer on the table when <paramref name="answering"/>, else the
        /// decision the match waits for when <paramref name="deciding"/> (by phase, as the full screen asks it), plus
        /// Trade (the builder) when <paramref name="managing"/> and somebody is left to trade with. Managing property has
        /// buttons of its own in <see cref="Properties"/>.
        /// </summary>
        public static List<PanelChoice> Choices(MonopolyMatch match, LockstepMatch table, int seat, bool deciding, bool answering, bool managing = false)
        {
            var choices = new List<PanelChoice>();
            if (!Holds(match, seat) || match.IsOver)
            {
                return choices;
            }
            TradeOffer offer = table != null ? table.PendingOffer : null;
            if (offer != null)
            {
                if (answering && offer.to == seat)
                {
                    choices.Add(PanelChoice.Of(PanelChoiceKind.Accept, L.T("Accept"), primary: true));
                    choices.Add(PanelChoice.Of(PanelChoiceKind.Decline, L.T("Decline")));
                }
                return choices;
            }
            if (deciding && match.Decider == seat)
            {
                choices.AddRange(Decisions(match, seat));
            }
            if (CanTrade(match, seat, managing))
            {
                choices.Add(PanelChoice.Of(PanelChoiceKind.Trade, L.T("Trade")));
            }
            return choices;
        }

        /// <summary>The decision the match waits for from <paramref name="seat"/>, who is its decider, as buttons.</summary>
        private static List<PanelChoice> Decisions(MonopolyMatch match, int seat)
        {
            var choices = new List<PanelChoice>();
            PlayerState player = match.players[seat];
            switch (match.phase)
            {
                case MatchPhase.Roll:
                    choices.Add(PanelChoice.Of(PanelChoiceKind.Roll, L.T("Roll"), primary: true));
                    break;
                case MatchPhase.JailChoice:
                    choices.Add(PanelChoice.Of(PanelChoiceKind.Roll, L.T("Roll"), primary: true));
                    choices.Add(PanelChoice.Of(PanelChoiceKind.PayFine, L.F("Pay {0}", MonopolyStyle.Money(match.rules.jailFine)), enabled: player.cash >= match.rules.jailFine));
                    if (player.jailCards.Count > 0)
                    {
                        choices.Add(PanelChoice.Of(PanelChoiceKind.UseCard, L.T("Use card")));
                    }
                    break;
                case MatchPhase.BusChoice:
                {
                    int[] bus = match.BusOptions;
                    for (int i = 0; i < bus.Length; i++)
                    {
                        int target = (player.position + bus[i]) % match.SpaceCount;
                        choices.Add(PanelChoice.Of(PanelChoiceKind.Bus, $"{bus[i]}: {MonopolyStyle.SpaceName(match.Space(target))}", i, primary: i == 2));
                    }
                    break;
                }
                case MatchPhase.MoveAnywhere:
                {
                    int best = SuggestDestination(match, seat);
                    choices.Add(PanelChoice.Of(PanelChoiceKind.Destination, $"{L.T("Best pick")}: {MonopolyStyle.SpaceName(match.Space(best))}", best, primary: true));
                    if (best != 0)
                    {
                        choices.Add(PanelChoice.Of(PanelChoiceKind.Destination, MonopolyStyle.SpaceName(match.Space(0)), 0));
                    }
                    break;
                }
                case MatchPhase.BuyChoice:
                {
                    SpaceData lot = match.Space(match.pendingPurchase);
                    bool affordable = match.CanAffordPurchase;
                    choices.Add(PanelChoice.Of(PanelChoiceKind.Buy, affordable ? L.F("Buy {0}", MonopolyStyle.Money(lot.price)) : L.F("Need {0}", MonopolyStyle.Money(lot.price)), enabled: affordable, primary: true));
                    choices.Add(PanelChoice.Of(PanelChoiceKind.DeclineBuy, match.rules.auctions ? L.T("Auction") : L.T("Pass")));
                    break;
                }
                case MatchPhase.Auction:
                {
                    AuctionState auction = match.auction;
                    bool anyBid = auction.highBidder >= 0;
                    for (int i = 0; i < BidRaises.Length; i++)
                    {
                        int amount = anyBid ? auction.highBid + BidRaises[i] : Math.Max(auction.MinimumBid, BidRaises[i]);
                        choices.Add(PanelChoice.Of(PanelChoiceKind.Bid, L.F("Bid {0}", MonopolyStyle.Money(amount)), amount, enabled: amount <= player.cash, primary: i == 0));
                    }
                    choices.Add(PanelChoice.Of(PanelChoiceKind.PassBid, L.T("Pass")));
                    break;
                }
                case MatchPhase.RaiseFunds:
                    choices.Add(PanelChoice.Of(PanelChoiceKind.Bankrupt, L.T("Go bankrupt")));
                    break;
                case MatchPhase.EndTurn:
                    choices.Add(PanelChoice.Of(PanelChoiceKind.EndTurn, L.T("End turn"), primary: true));
                    break;
            }
            return choices;
        }

        // ------------------------------------------------------------------ trading

        /// <summary>Whether the seat may open the trade builder now: it may manage, and somebody else is still in the game (what the trade window's button checks).</summary>
        public static bool CanTrade(MonopolyMatch match, int seat, bool managing)
        {
            return managing && Holds(match, seat) && !match.IsOver && match.ActiveCount > 1;
        }

        /// <summary>The seats the player may trade with: everybody still in the game but themselves, in seat order (as the trade window lists them).</summary>
        public static List<int> TradePartners(MonopolyMatch match, int seat)
        {
            var partners = new List<int>();
            if (!Holds(match, seat))
            {
                return partners;
            }
            foreach (PlayerState player in match.players)
            {
                if (!player.bankrupt && player.index != seat)
                {
                    partners.Add(player.index);
                }
            }
            return partners;
        }

        /// <summary>A fresh draft of <paramref name="seat"/> with the first partner picked, or null when nobody is left to trade with.</summary>
        public static TradeDraft NewTrade(MonopolyMatch match, int seat)
        {
            List<int> partners = TradePartners(match, seat);
            if (partners.Count == 0)
            {
                return null;
            }
            var draft = new TradeDraft { from = seat };
            SelectPartner(draft, partners[0]);
            return draft;
        }

        /// <summary>Picks the other side of <paramref name="draft"/>: what was ticked and counted for the one before goes (as in the trade window).</summary>
        public static void SelectPartner(TradeDraft draft, int partner)
        {
            if (draft == null)
            {
                return;
            }
            draft.to = partner;
            draft.giveSpaces.Clear();
            draft.getSpaces.Clear();
            draft.giveCash = 0;
            draft.getCash = 0;
            draft.giveCard = false;
            draft.getCard = false;
        }

        /// <summary>The properties <paramref name="seat"/> may put into a trade (none of a set with buildings on it), in set order.</summary>
        public static List<int> Offerable(MonopolyMatch match, int seat)
        {
            if (!Holds(match, seat))
            {
                return new List<int>();
            }
            return match.TradableProperties(seat)
                .OrderBy(space => Array.IndexOf(MonopolyStyle.Groups, match.Space(space).group))
                .ThenBy(space => space)
                .ToList();
        }

        /// <summary>Ticks <paramref name="space"/> into the side the player gives (<paramref name="give"/>) or asks for, or out of it again.</summary>
        public static void ToggleSpace(TradeDraft draft, int space, bool give)
        {
            if (draft == null)
            {
                return;
            }
            List<int> side = give ? draft.giveSpaces : draft.getSpaces;
            if (!side.Remove(space))
            {
                side.Add(space);
                side.Sort();
            }
        }

        /// <summary>Moves the cash a side gives by <paramref name="step"/>: never below nothing, never above what that side has (as the trade window clamps it).</summary>
        public static void ChangeCash(MonopolyMatch match, TradeDraft draft, int step, bool give)
        {
            if (draft == null || match == null)
            {
                return;
            }
            int side = give ? draft.from : draft.to;
            int limit = Holds(match, side) ? match.players[side].cash : 0;
            if (give)
            {
                draft.giveCash = Math.Max(0, Math.Min(limit, draft.giveCash + step));
            }
            else
            {
                draft.getCash = Math.Max(0, Math.Min(limit, draft.getCash + step));
            }
        }

        /// <summary>Ticks a Get Out of Jail Free card into the side the player gives (<paramref name="give"/>) or asks for, or out again.</summary>
        public static void ToggleCard(TradeDraft draft, bool give)
        {
            if (draft == null)
            {
                return;
            }
            if (give)
            {
                draft.giveCard = !draft.giveCard;
            }
            else
            {
                draft.getCard = !draft.getCard;
            }
        }

        /// <summary>Whether the rules take <paramref name="draft"/> as an offer (<see cref="MonopolyMatch.CanTrade"/>), and why not, in the language shown.</summary>
        public static bool ValidateTrade(MonopolyMatch match, TradeDraft draft, out string reason)
        {
            if (match == null || draft == null || draft.to < 0)
            {
                reason = L.T("Pick another player to trade with.");
                return false;
            }
            bool ok = match.CanTrade(draft.ToOffer(), out string why);
            reason = ok ? null : TradeUI.Reason(why);
            return ok;
        }

        /// <summary>The line under the draft: who answers it and how when the rules take it, else why they refuse it.</summary>
        public static string TradeStatus(MonopolyMatch match, TradeDraft draft)
        {
            if (ValidateTrade(match, draft, out string reason))
            {
                PlayerState partner = match.players[draft.to];
                string name = MonopolyStyle.NameOf(partner);
                return partner.bot ? L.F("{0} weighs every deal by the sets it makes.", name) : L.F("{0} answers on their own device.", name);
            }
            return reason;
        }

        /// <summary>
        /// The seat's properties in set order, each with the buttons the property manager would offer for it now when
        /// <paramref name="managing"/> (building, selling a building, mortgaging or lifting the mortgage; a button the
        /// rules forbid is there but disabled, as in the manager).
        /// </summary>
        public static List<PanelProperty> Properties(MonopolyMatch match, int seat, bool managing)
        {
            var list = new List<PanelProperty>();
            if (!Holds(match, seat))
            {
                return list;
            }
            IEnumerable<int> owned = match.PropertiesOf(seat)
                .OrderBy(space => Array.IndexOf(MonopolyStyle.Groups, match.Space(space).group))
                .ThenBy(space => space);
            foreach (int space in owned)
            {
                SpaceData data = match.Space(space);
                DeedState deed = match.Deed(space);
                var property = new PanelProperty { space = space, name = MonopolyStyle.SpaceName(data), group = data.group, houses = deed.houses, mortgaged = deed.mortgaged };
                string buildings = deed.houses == MonopolyMatch.Hotel ? L.T("Hotel") : deed.houses == 1 ? L.T("1 house") : deed.houses > 1 ? L.F("{0} houses", deed.houses) : "";
                string rent = deed.mortgaged ? L.T("Mortgaged") : L.F("Rent {0}", MonopolyStyle.Money(match.Rent(space, 7)));
                property.state = string.IsNullOrEmpty(buildings) ? rent : $"{buildings}  •  {rent}";
                if (managing)
                {
                    if (data.kind == SpaceKind.Street)
                    {
                        property.actions.Add(PanelChoice.Of(PanelChoiceKind.Build, L.F("Build {0}", MonopolyStyle.Money(data.houseCost)), space, match.CanBuild(seat, space, out _)));
                        property.actions.Add(PanelChoice.Of(PanelChoiceKind.Sell, L.F("Sell +{0}", MonopolyStyle.Money(data.houseCost / 2)), space, match.CanSellBuilding(seat, space, out _)));
                    }
                    if (deed.mortgaged)
                    {
                        property.actions.Add(PanelChoice.Of(PanelChoiceKind.Unmortgage, L.F("Lift {0}", MonopolyStyle.Money(data.UnmortgageCost)), space, match.CanUnmortgage(seat, space, out _)));
                    }
                    else
                    {
                        property.actions.Add(PanelChoice.Of(PanelChoiceKind.Mortgage, L.F("Mortgage +{0}", MonopolyStyle.Money(data.MortgageValue)), space, match.CanMortgage(seat, space, out _)));
                    }
                }
                list.Add(property);
            }
            return list;
        }

        /// <summary>The standings of the match (<see cref="MonopolyMatch.Standings"/>) as the panel lists them.</summary>
        public static List<PanelStanding> Standings(MonopolyMatch match)
        {
            var list = new List<PanelStanding>();
            if (match == null)
            {
                return list;
            }
            int place = 0;
            foreach (PlayerState player in match.Standings())
            {
                place++;
                list.Add(new PanelStanding
                {
                    place = place,
                    seat = player.index,
                    name = MonopolyStyle.NameOf(player),
                    worth = player.bankrupt ? 0 : match.NetWorth(player.index),
                    bankrupt = player.bankrupt,
                    bankruptRound = player.bankruptRound
                });
            }
            return list;
        }

        /// <summary>
        /// Where to go on triples: the space a computer player would pick (an unowned property by its value, GO for the
        /// salary; never somebody's property, never Go To Jail).
        /// </summary>
        public static int SuggestDestination(MonopolyMatch match, int seat)
        {
            int best = 0;
            float bestScore = float.MinValue;
            for (int space = 0; space < match.SpaceCount; space++)
            {
                SpaceData data = match.Space(space);
                float score = data.IsProperty && !match.Deed(space).Owned ? BotBrain.Valuation(match, seat, space) : data.kind == SpaceKind.Go ? match.rules.salary : 0f;
                if (data.IsProperty && match.Deed(space).Owned && match.Owner(space) != seat)
                {
                    score = -match.Rent(space, 7);
                }
                if (data.kind == SpaceKind.GoToJail)
                {
                    score = -200f;
                }
                if (score > bestScore)
                {
                    bestScore = score;
                    best = space;
                }
            }
            return best;
        }

        // ------------------------------------------------------------------ pressing

        /// <summary>
        /// Sends what <paramref name="choice"/> stands for in the name of <paramref name="seat"/>: through
        /// <paramref name="commands"/> (the one seam of the interface), the answer to an offer through
        /// <paramref name="answer"/> (seat, accept). Returns whether the choice was one that sends anything.
        /// </summary>
        public static bool Perform(PanelChoice choice, int seat, IMonopolyCommands commands, Action<int, bool> answer)
        {
            if (choice == null || commands == null)
            {
                return false;
            }
            switch (choice.kind)
            {
                case PanelChoiceKind.Roll: commands.Roll(seat); return true;
                case PanelChoiceKind.PayFine: commands.PayJailFine(seat); return true;
                case PanelChoiceKind.UseCard: commands.UseJailCard(seat); return true;
                case PanelChoiceKind.Bus: commands.ChooseBus(seat, choice.argument); return true;
                case PanelChoiceKind.Destination: commands.ChooseDestination(seat, choice.argument); return true;
                case PanelChoiceKind.Buy: commands.Buy(seat); return true;
                case PanelChoiceKind.DeclineBuy: commands.DeclineBuy(seat); return true;
                case PanelChoiceKind.Bid: commands.Bid(seat, choice.argument); return true;
                case PanelChoiceKind.PassBid: commands.PassBid(seat); return true;
                case PanelChoiceKind.EndTurn: commands.EndTurn(seat); return true;
                case PanelChoiceKind.Bankrupt: commands.DeclareBankruptcy(seat); return true;
                case PanelChoiceKind.Accept: answer?.Invoke(seat, true); return answer != null;
                case PanelChoiceKind.Decline: answer?.Invoke(seat, false); return answer != null;
                case PanelChoiceKind.Build: commands.Build(seat, choice.argument); return true;
                case PanelChoiceKind.Sell: commands.Sell(seat, choice.argument); return true;
                case PanelChoiceKind.Mortgage: commands.Mortgage(seat, choice.argument); return true;
                case PanelChoiceKind.Unmortgage: commands.Unmortgage(seat, choice.argument); return true;
                // Trade opens the builder on the panel; the proposal goes through IMonopolyCommands.ProposeTrade once built.
                default: return false;
            }
        }

        /// <summary>
        /// A text that changes exactly when what the panel shows for <paramref name="seat"/> would: the panel redraws
        /// itself when it does.
        /// </summary>
        public static string Signature(MonopolyMatch match, LockstepMatch table, int seat, bool deciding, bool managing, bool answering)
        {
            if (match == null)
            {
                return "";
            }
            var parts = new List<string>
            {
                Cash(match, seat), Worth(match, seat), Position(match, seat), Headline(match, table, seat), Detail(match, table, seat),
                string.Join("|", Choices(match, table, seat, deciding, answering, managing).Select(c => c.ToString())),
                string.Join("|", Properties(match, seat, managing).Select(p => $"{p.space}:{p.state}:{string.Join(",", p.actions.Select(a => a.ToString()))}")),
                match.IsOver ? string.Join("|", Standings(match).Select(s => $"{s.place}:{s.seat}:{s.worth}:{s.bankrupt}")) : ""
            };
            return string.Join("\n", parts);
        }

        private static bool Holds(MonopolyMatch match, int seat)
        {
            return match != null && seat >= 0 && seat < match.players.Count;
        }
    }
}
