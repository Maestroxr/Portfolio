using System.Collections.Generic;
using Gamebox.Lockstep;

namespace Portfolio.Monopoly
{
    /// <summary>
    /// A match that several devices play in step: each of them runs this class with the same board, rules, seats and
    /// seed, and feeds it the same log of actions in the same order (the server's), so all of them hold the same match
    /// at every entry of the log. Nothing else may change the match. The dice of an action are seeded with the number
    /// the server stamped on it, which nobody knows before the action is in the log.
    ///
    /// Next to the rules engine it keeps what only a table of several devices needs: the trade offer waiting for the
    /// answer of its recipient (while it waits nothing else is accepted), the move made when the server calls time, and
    /// a checksum to compare the devices by. How the entries of the log are taken is the shared
    /// <see cref="LockstepTable{TCommand}"/>.
    /// </summary>
    public sealed class LockstepMatch : LockstepTable<MatchCommand>
    {
        public LockstepMatch(BoardLayout board, RuleSet rules, uint seed) : base(seed)
        {
            Match = new MonopolyMatch(board, rules, Random);
        }

        public MonopolyMatch Match { get; }

        public override bool IsOver => Match.IsOver;

        /// <summary>The offer on the table, or null.</summary>
        public TradeOffer PendingOffer { get; private set; }

        /// <summary>The offer of the last answer, and what the answer was: for the view to tell.</summary>
        public TradeOffer AnsweredOffer { get; private set; }

        public bool AnswerAccepted { get; private set; }

        /// <summary>The seat the table waits for: the recipient of the offer on the table, else the one the match waits for; -1 for nobody.</summary>
        public int Waiting => Match.IsOver ? -1 : PendingOffer != null ? PendingOffer.to : Match.Decider;

        /// <summary>Whether <paramref name="seat"/> may build, sell, mortgage or propose a trade now.</summary>
        public bool CanManage(int seat)
        {
            return PendingOffer == null && Match.CanManage(seat);
        }

        /// <summary>
        /// Gives a command of the log to the match: the table itself keeps the offers and their answers, the rules engine
        /// takes the rest. A refused command changes nothing.
        /// </summary>
        protected override bool Accept(MatchCommand command)
        {
            if (Match.IsOver || command.seat < 0 || command.seat >= Match.players.Count)
            {
                return false;
            }
            switch (command.kind)
            {
                case CommandKind.SeatToComputer:
                {
                    PlayerState player = Match.players[command.seat];
                    player.bot = true;
                    player.level = (BotLevel)System.Math.Max(0, System.Math.Min((int)BotLevel.Hard, command.argument));
                    return true;
                }
                case CommandKind.ProposeTrade:
                    if (PendingOffer != null || command.offer == null || command.offer.from != command.seat || !Match.CanTrade(command.offer, out _))
                    {
                        return false;
                    }
                    PendingOffer = command.offer.Clone();
                    return true;
                case CommandKind.AnswerTrade:
                    if (PendingOffer == null || PendingOffer.to != command.seat)
                    {
                        return false;
                    }
                    Answer(command.argument != 0);
                    return true;
                default:
                    // The table waits for the answer to the offer: nothing else moves until it is there.
                    return PendingOffer == null && command.Apply(Match);
            }
        }

        /// <summary>
        /// The server called time: the offer on the table is declined, else the default move is made for the player the
        /// match waits for. A player who ran out of time over a debt has it raised in one go, because the table waited
        /// long enough.
        /// </summary>
        protected override void DefaultMoves(List<MatchCommand> made)
        {
            if (PendingOffer != null)
            {
                made.Add(MatchCommand.Answer(PendingOffer.to, false));
                Answer(false);
                return;
            }
            int seat = Match.Decider;
            bool inDebt = Match.phase == MatchPhase.RaiseFunds;
            do
            {
                MatchCommand command = MatchCommand.DefaultMove(Match);
                if (command == null || !command.Apply(Match))
                {
                    break;
                }
                made.Add(command);
            }
            while (inDebt && made.Count < 64 && Match.phase == MatchPhase.RaiseFunds && Match.Decider == seat);
        }

        /// <summary>The computer plays the seat of a player who left, at the level of the seat.</summary>
        protected override bool SeatToComputer(int seat, int level)
        {
            return Accept(MatchCommand.Of(CommandKind.SeatToComputer, seat, level));
        }

        /// <summary>The answer of the last entry is told once: every entry of the log starts without one.</summary>
        protected override void BeforeEntry()
        {
            AnsweredOffer = null;
        }

        private void Answer(bool accept)
        {
            AnsweredOffer = PendingOffer;
            PendingOffer = null;
            // An offer that was fair when it was made still is: nothing moved since.
            AnswerAccepted = accept && Match.ExecuteTrade(AnsweredOffer);
        }

        /// <summary>
        /// A number that is the same on two devices exactly when their matches are: everything the rules look at, folded
        /// into 32 bits.
        /// </summary>
        public override uint Checksum()
        {
            MonopolyMatch m = Match;
            var sum = new StateChecksum();
            sum.Add((int)m.phase).Add(m.current).Add(m.round).Add(m.turn).Add(m.housesLeft).Add(m.hotelsLeft).Add(m.pot);
            sum.Add(m.lastRoll.a).Add(m.lastRoll.b).Add((int)m.lastRoll.speed).Add(m.pendingPurchase).Add(m.pendingMove);
            sum.Add(m.extraRoll).Add(m.mrMonopolyPending).Add(m.debtAnnounced).Add(m.bankruptcies).Add(m.winner);
            foreach (PlayerState player in m.players)
            {
                sum.Add(player.cash).Add(player.position).Add(player.inJail).Add(player.jailTurns).Add(player.bankrupt).Add(player.bankruptRound);
                sum.Add(player.passedGo).Add(player.doubles).Add(player.place).Add(player.bot).Add((int)player.level);
                sum.Add(player.rentCollected).Add(player.rentPaid).Add(player.timesInJail).Add(player.jailCards.Count);
                foreach (CardDeckKind card in player.jailCards)
                {
                    sum.Add((int)card);
                }
            }
            foreach (DeedState deed in m.deeds)
            {
                sum.Add(deed.owner).Add(deed.houses).Add(deed.mortgaged);
            }
            sum.Add(m.chanceDeck.order).Add(m.chestDeck.order).Add(m.auctionQueue);
            sum.Add(m.auction.space).Add(m.auction.highBid).Add(m.auction.highBidder).Add(m.auction.turn).Add(m.auction.bidders);
            sum.Add(m.debts.Count);
            foreach (Debt debt in m.debts)
            {
                sum.Add(debt.debtor).Add(debt.creditor).Add(debt.amount);
            }
            sum.Add(PendingOffer != null);
            if (PendingOffer != null)
            {
                sum.Add(PendingOffer.from).Add(PendingOffer.to).Add(PendingOffer.giveCash).Add(PendingOffer.getCash);
                sum.Add(PendingOffer.giveJailCards).Add(PendingOffer.getJailCards).Add(PendingOffer.giveSpaces).Add(PendingOffer.getSpaces);
            }
            return sum.Value;
        }
    }
}
