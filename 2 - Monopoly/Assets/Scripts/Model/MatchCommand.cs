using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Portfolio.Monopoly
{
    /// <summary>
    /// What a player can tell the rules engine. The numbers travel in the action log of an online match and the server
    /// knows them (Server/Lib.cs), so they never change: the commands up to <see cref="DeclareBankruptcy"/> answer what
    /// the match waits for, the ones after it manage property between moves.
    /// </summary>
    public enum CommandKind
    {
        Roll = 0,
        Buy = 1,
        DeclineBuy = 2,
        PayJailFine = 3,
        UseJailCard = 4,
        /// <summary>The argument is the bus option: the first die, the second die or both.</summary>
        ChooseBus = 5,
        /// <summary>The argument is the space to move to.</summary>
        ChooseDestination = 6,
        /// <summary>The argument is the amount bid.</summary>
        Bid = 7,
        PassBid = 8,
        EndTurn = 9,
        PayDebt = 10,
        DeclareBankruptcy = 11,
        /// <summary>The argument of the four property commands is the space.</summary>
        Build = 12,
        Sell = 13,
        Mortgage = 14,
        Unmortgage = 15,
        /// <summary>Puts <see cref="MatchCommand.offer"/> on the table; the other side answers it.</summary>
        ProposeTrade = 16,
        /// <summary>The answer to the offer on the table: the argument is 1 to accept, 0 to decline.</summary>
        AnswerTrade = 17,
        /// <summary>Made by the server when a player leaves: the computer plays the seat on, at the level of the argument.</summary>
        SeatToComputer = 18
    }

    /// <summary>
    /// One command of a player to the rules engine, as data: what the computer players decide on, what an online match
    /// sends through the server before anybody applies it, and what the server's clock falls back on
    /// (<see cref="DefaultMove"/>). <see cref="Payload"/> and <see cref="Parse"/> turn the arguments into the short text
    /// an action of the log carries.
    /// </summary>
    public sealed class MatchCommand
    {
        private const char FieldSeparator = '|';
        private const char ListSeparator = ',';

        public CommandKind kind;
        public int seat;
        /// <summary>The number the command needs, if any: a space, an amount, an option.</summary>
        public int argument;
        /// <summary>The trade of a <see cref="CommandKind.ProposeTrade"/>.</summary>
        public TradeOffer offer;

        public static MatchCommand Of(CommandKind kind, int seat, int argument = 0)
        {
            return new MatchCommand { kind = kind, seat = seat, argument = argument };
        }

        public static MatchCommand Propose(TradeOffer offer)
        {
            return new MatchCommand { kind = CommandKind.ProposeTrade, seat = offer.from, offer = offer.Clone() };
        }

        public static MatchCommand Answer(int seat, bool accept)
        {
            return Of(CommandKind.AnswerTrade, seat, accept ? 1 : 0);
        }

        /// <summary>Whether the command manages property or trades, rather than answering what the match waits for.</summary>
        public bool IsManagement => kind >= CommandKind.Build && kind <= CommandKind.AnswerTrade;

        /// <summary>Whether the dice (or a card) decide what the command does: nobody knows its outcome before it is made.</summary>
        public bool InvolvesChance => kind == CommandKind.Roll;

        // ------------------------------------------------------------------ applying

        /// <summary>
        /// Gives the command to the rules engine, which accepts it only from the seat that may give it now. Trades need a
        /// table that remembers the offer between the proposal and the answer: <see cref="LockstepMatch"/>.
        /// </summary>
        public bool Apply(MonopolyMatch match)
        {
            if (match == null || seat < 0 || seat >= match.players.Count)
            {
                return false;
            }
            switch (kind)
            {
                case CommandKind.Build:
                    return IsSpace(match) && match.Build(seat, argument);
                case CommandKind.Sell:
                    return IsSpace(match) && match.SellBuilding(seat, argument);
                case CommandKind.Mortgage:
                    return IsSpace(match) && match.Mortgage(seat, argument);
                case CommandKind.Unmortgage:
                    return IsSpace(match) && match.Unmortgage(seat, argument);
                case CommandKind.Bid:
                    return match.PlaceBid(seat, argument);
                case CommandKind.PassBid:
                    return match.PassBid(seat);
            }
            if (match.Decider != seat)
            {
                return false;
            }
            switch (kind)
            {
                case CommandKind.Roll:
                    return match.Roll();
                case CommandKind.Buy:
                    return match.Buy();
                case CommandKind.DeclineBuy:
                    return match.DeclineBuy();
                case CommandKind.PayJailFine:
                    return match.PayJailFine();
                case CommandKind.UseJailCard:
                    return match.UseJailCard();
                case CommandKind.ChooseBus:
                    return match.ChooseBus(argument);
                case CommandKind.ChooseDestination:
                    return match.ChooseDestination(argument);
                case CommandKind.EndTurn:
                    return match.EndTurn();
                case CommandKind.PayDebt:
                    return match.PayDebt();
                case CommandKind.DeclareBankruptcy:
                    return match.DeclareBankruptcy();
                default:
                    return false;
            }
        }

        private bool IsSpace(MonopolyMatch match)
        {
            return argument >= 0 && argument < match.SpaceCount;
        }

        // ------------------------------------------------------------------ the default move

        /// <summary>
        /// The move made for the player who keeps the table waiting when the clock of an online match runs out: roll,
        /// leave the property to the auction, drop out of the bidding, take the first bus, move to GO, end the turn, and
        /// in debt mortgage and sell what covers it or give up. It is worked out in whole numbers from the state alone,
        /// so every client makes the same one. Null when the match waits for nobody.
        /// </summary>
        public static MatchCommand DefaultMove(MonopolyMatch match)
        {
            int seat = match != null ? match.Decider : -1;
            if (seat < 0)
            {
                return null;
            }
            switch (match.phase)
            {
                case MatchPhase.Roll:
                case MatchPhase.JailChoice:
                    return Of(CommandKind.Roll, seat);
                case MatchPhase.BusChoice:
                    return Of(CommandKind.ChooseBus, seat, 0);
                case MatchPhase.MoveAnywhere:
                    // GO pays the salary and charges no rent.
                    return Of(CommandKind.ChooseDestination, seat, 0);
                case MatchPhase.BuyChoice:
                    return Of(CommandKind.DeclineBuy, seat);
                case MatchPhase.Auction:
                    return Of(CommandKind.PassBid, seat);
                case MatchPhase.RaiseFunds:
                    return DefaultFunds(match, seat);
                case MatchPhase.EndTurn:
                    return Of(CommandKind.EndTurn, seat);
                default:
                    return null;
            }
        }

        /// <summary>One step of raising a debt: loose properties are mortgaged first, then buildings go, then the sets.</summary>
        private static MatchCommand DefaultFunds(MonopolyMatch match, int seat)
        {
            Debt debt = match.CurrentDebt;
            if (debt == null)
            {
                return null;
            }
            if (match.players[seat].cash >= debt.amount)
            {
                return Of(CommandKind.PayDebt, seat);
            }
            if (match.LiquidValue(seat) < debt.amount)
            {
                return Of(CommandKind.DeclareBankruptcy, seat);
            }
            List<int> owned = match.PropertiesOf(seat).ToList();
            foreach (int space in owned.Where(s => !InOwnedSet(match, seat, s)).OrderBy(s => match.Board[s].MortgageValue))
            {
                if (match.CanMortgage(seat, space, out _))
                {
                    return Of(CommandKind.Mortgage, seat, space);
                }
            }
            foreach (int space in owned.OrderByDescending(s => match.deeds[s].houses))
            {
                if (match.CanSellBuilding(seat, space, out _))
                {
                    return Of(CommandKind.Sell, seat, space);
                }
            }
            foreach (int space in owned.OrderBy(s => match.Board[s].MortgageValue))
            {
                if (match.CanMortgage(seat, space, out _))
                {
                    return Of(CommandKind.Mortgage, seat, space);
                }
            }
            return Of(CommandKind.DeclareBankruptcy, seat);
        }

        private static bool InOwnedSet(MonopolyMatch match, int seat, int space)
        {
            SpaceData data = match.Board[space];
            return data.kind == SpaceKind.Street && match.OwnsGroup(seat, data.group);
        }

        // ------------------------------------------------------------------ text

        /// <summary>
        /// The arguments as the text an action carries: nothing, the one number, or for a trade
        /// <c>to|give cash|get cash|give cards|get cards|spaces given|spaces got</c> with the spaces separated by commas.
        /// </summary>
        public string Payload()
        {
            switch (kind)
            {
                case CommandKind.ChooseBus:
                case CommandKind.ChooseDestination:
                case CommandKind.Bid:
                case CommandKind.Build:
                case CommandKind.Sell:
                case CommandKind.Mortgage:
                case CommandKind.Unmortgage:
                case CommandKind.AnswerTrade:
                case CommandKind.SeatToComputer:
                    return Number(argument);
                case CommandKind.ProposeTrade:
                    if (offer == null)
                    {
                        return "";
                    }
                    return new StringBuilder()
                        .Append(Number(offer.to)).Append(FieldSeparator)
                        .Append(Number(offer.giveCash)).Append(FieldSeparator)
                        .Append(Number(offer.getCash)).Append(FieldSeparator)
                        .Append(Number(offer.giveJailCards)).Append(FieldSeparator)
                        .Append(Number(offer.getJailCards)).Append(FieldSeparator)
                        .Append(string.Join(ListSeparator.ToString(), offer.giveSpaces.Select(Number))).Append(FieldSeparator)
                        .Append(string.Join(ListSeparator.ToString(), offer.getSpaces.Select(Number)))
                        .ToString();
                default:
                    return "";
            }
        }

        /// <summary>The command of an action of the log, or null when the action is none this game knows how to read.</summary>
        public static MatchCommand Parse(int seat, uint kind, string payload)
        {
            if (kind > (uint)CommandKind.SeatToComputer || seat < 0)
            {
                return null;
            }
            var command = new MatchCommand { kind = (CommandKind)kind, seat = seat };
            payload = payload ?? "";
            switch (command.kind)
            {
                case CommandKind.ChooseBus:
                case CommandKind.ChooseDestination:
                case CommandKind.Bid:
                case CommandKind.Build:
                case CommandKind.Sell:
                case CommandKind.Mortgage:
                case CommandKind.Unmortgage:
                case CommandKind.AnswerTrade:
                case CommandKind.SeatToComputer:
                    return TryNumber(payload, out command.argument) ? command : null;
                case CommandKind.ProposeTrade:
                    command.offer = ParseOffer(seat, payload);
                    return command.offer != null ? command : null;
                default:
                    return command;
            }
        }

        private static TradeOffer ParseOffer(int from, string payload)
        {
            string[] fields = payload.Split(FieldSeparator);
            var offer = new TradeOffer { from = from };
            if (fields.Length != 7
                || !TryNumber(fields[0], out offer.to)
                || !TryNumber(fields[1], out offer.giveCash)
                || !TryNumber(fields[2], out offer.getCash)
                || !TryNumber(fields[3], out offer.giveJailCards)
                || !TryNumber(fields[4], out offer.getJailCards)
                || !TryNumbers(fields[5], offer.giveSpaces)
                || !TryNumbers(fields[6], offer.getSpaces))
            {
                return null;
            }
            return offer;
        }

        private static string Number(int value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        private static bool TryNumber(string text, out int value)
        {
            return int.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out value);
        }

        private static bool TryNumbers(string text, List<int> values)
        {
            if (text.Length == 0)
            {
                return true;
            }
            foreach (string part in text.Split(ListSeparator))
            {
                // No board has more spaces than this; a longer list is not a trade.
                if (!TryNumber(part, out int value) || values.Count >= 64)
                {
                    return false;
                }
                values.Add(value);
            }
            return true;
        }

        public override string ToString()
        {
            string payload = Payload();
            return payload.Length > 0 ? $"{kind} by seat {seat}: {payload}" : $"{kind} by seat {seat}";
        }
    }
}
