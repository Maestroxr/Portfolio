using System.Collections.Generic;

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
    /// a checksum to compare the devices by.
    /// </summary>
    public sealed class LockstepMatch
    {
        private readonly SeededRandom random;

        public LockstepMatch(BoardLayout board, RuleSet rules, uint seed)
        {
            random = new SeededRandom(seed);
            Match = new MonopolyMatch(board, rules, random);
        }

        public MonopolyMatch Match { get; }

        /// <summary>The offer on the table, or null.</summary>
        public TradeOffer PendingOffer { get; private set; }

        /// <summary>The offer of the last answer, and what the answer was: for the view to tell.</summary>
        public TradeOffer AnsweredOffer { get; private set; }

        public bool AnswerAccepted { get; private set; }

        /// <summary>Entries of the log taken so far, whether the match accepted them or not.</summary>
        public int Applied { get; private set; }

        /// <summary>The seat the table waits for: the recipient of the offer on the table, else the one the match waits for; -1 for nobody.</summary>
        public int Waiting => Match.IsOver ? -1 : PendingOffer != null ? PendingOffer.to : Match.Decider;

        /// <summary>Whether <paramref name="seat"/> may build, sell, mortgage or propose a trade now.</summary>
        public bool CanManage(int seat)
        {
            return PendingOffer == null && Match.CanManage(seat);
        }

        /// <summary>
        /// Takes an action of the log: <paramref name="command"/> (null for one that could not be read) with the number
        /// the server stamped on it. Returns whether the match accepted it; a refused action changes nothing.
        /// </summary>
        public bool Apply(MatchCommand command, uint stamp)
        {
            Applied++;
            AnsweredOffer = null;
            if (command == null || Match.IsOver || command.seat < 0 || command.seat >= Match.players.Count)
            {
                return false;
            }
            random.Reseed(stamp);
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
        /// long enough. Returns the commands that were made.
        /// </summary>
        public List<MatchCommand> Timeout(uint stamp)
        {
            Applied++;
            AnsweredOffer = null;
            var made = new List<MatchCommand>();
            if (Match.IsOver)
            {
                return made;
            }
            random.Reseed(stamp);
            if (PendingOffer != null)
            {
                made.Add(MatchCommand.Answer(PendingOffer.to, false));
                Answer(false);
                return made;
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
            return made;
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
        public uint Checksum()
        {
            MonopolyMatch m = Match;
            var sum = new Fold();
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

        /// <summary>FNV-1a over the numbers it is given.</summary>
        private sealed class Fold
        {
            public uint Value { get; private set; } = 2166136261u;

            public Fold Add(int number)
            {
                uint bits = (uint)number;
                for (int i = 0; i < 4; i++)
                {
                    Value = (Value ^ (bits & 0xFFu)) * 16777619u;
                    bits >>= 8;
                }
                return this;
            }

            public Fold Add(bool flag)
            {
                return Add(flag ? 1 : 0);
            }

            public Fold Add(List<int> numbers)
            {
                Add(numbers.Count);
                foreach (int number in numbers)
                {
                    Add(number);
                }
                return this;
            }
        }
    }
}
