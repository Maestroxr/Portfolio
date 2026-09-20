using System;
using System.Collections.Generic;
using System.Linq;

namespace Portfolio.Monopoly
{
    public sealed partial class MonopolyMatch
    {
        // ------------------------------------------------------------------ money

        /// <summary>Moves money between players, the bank and the pot, and records it.</summary>
        private void Transfer(int from, int to, int amount, string reason)
        {
            if (amount <= 0)
            {
                return;
            }
            if (from >= 0)
            {
                players[from].cash -= amount;
            }
            else if (from == Party.Pot)
            {
                pot -= amount;
            }
            if (to >= 0)
            {
                players[to].cash += amount;
            }
            else if (to == Party.Pot)
            {
                pot += amount;
            }
            Emit(MatchEventKind.Transfer, from, to, value: amount, text: reason);
            if (from == Party.Pot || to == Party.Pot)
            {
                Emit(MatchEventKind.PotChanged, value: pot);
            }
        }

        /// <summary>Makes <paramref name="debtor"/> pay: at once when they can, otherwise as a debt they must raise funds for.</summary>
        private void Charge(int debtor, int creditor, int amount, string reason)
        {
            if (amount <= 0 || debtor < 0 || players[debtor].bankrupt)
            {
                return;
            }
            if (creditor >= 0 && players[creditor].bankrupt)
            {
                return;
            }
            if (players[debtor].cash >= amount)
            {
                Transfer(debtor, creditor, amount, reason);
                return;
            }
            debts.Add(new Debt { debtor = debtor, creditor = creditor, amount = amount, reason = reason });
        }

        /// <summary>Pays the first debt when its debtor has the cash by now. Returns true when it was settled or dropped.</summary>
        private bool SettleFirstDebt()
        {
            Debt debt = debts[0];
            PlayerState debtor = players[debt.debtor];
            if (debtor.bankrupt || (debt.creditor >= 0 && players[debt.creditor].bankrupt))
            {
                debts.RemoveAt(0);
                debtAnnounced = false;
                return true;
            }
            if (debtor.cash >= debt.amount)
            {
                debts.RemoveAt(0);
                debtAnnounced = false;
                Transfer(debt.debtor, debt.creditor, debt.amount, debt.reason);
                Emit(MatchEventKind.DebtPaid, debt.debtor, debt.creditor, value: debt.amount);
                return true;
            }
            if (!debtAnnounced)
            {
                debtAnnounced = true;
                Emit(MatchEventKind.DebtStarted, debt.debtor, debt.creditor, value: debt.amount, text: debt.reason);
            }
            return false;
        }

        /// <summary>The debtor pays the first debt once they raised enough.</summary>
        public bool PayDebt()
        {
            if (phase != MatchPhase.RaiseFunds || debts.Count == 0)
            {
                return false;
            }
            Debt debt = debts[0];
            if (players[debt.debtor].cash < debt.amount)
            {
                return false;
            }
            debts.RemoveAt(0);
            debtAnnounced = false;
            Transfer(debt.debtor, debt.creditor, debt.amount, debt.reason);
            Emit(MatchEventKind.DebtPaid, debt.debtor, debt.creditor, value: debt.amount);
            Continue();
            return true;
        }

        /// <summary>The debtor of the first debt gives up: everything goes to the creditor (or the bank).</summary>
        public bool DeclareBankruptcy()
        {
            if (phase != MatchPhase.RaiseFunds || debts.Count == 0)
            {
                return false;
            }
            Debt debt = debts[0];
            debts.RemoveAt(0);
            debtAnnounced = false;
            int creditor = debt.creditor >= 0 ? debt.creditor : Party.Bank;
            GoBankrupt(debt.debtor, creditor);
            Continue();
            return true;
        }

        private void GoBankrupt(int who, int creditor)
        {
            PlayerState player = players[who];
            List<int> owned = PropertiesOf(who).ToList();

            // Buildings go back to the bank for half their price, which adds to what the creditor receives.
            foreach (int space in owned)
            {
                DeedState deed = deeds[space];
                if (deed.houses <= 0)
                {
                    continue;
                }
                int refund = BuildingValue(space) / 2;
                if (deed.houses == Hotel)
                {
                    hotelsLeft++;
                }
                else
                {
                    housesLeft += deed.houses;
                }
                deed.houses = 0;
                Emit(MatchEventKind.SoldBuilding, who, space: space, value: 0);
                Transfer(Party.Bank, who, refund, "Buildings sold");
            }

            if (creditor >= 0)
            {
                Transfer(who, creditor, player.cash, "Bankruptcy");
                int interest = 0;
                foreach (int space in owned)
                {
                    deeds[space].owner = creditor;
                    Emit(MatchEventKind.OwnerChanged, who, creditor, space);
                    if (deeds[space].mortgaged)
                    {
                        interest += (board[space].MortgageValue + 9) / 10;
                    }
                }
                foreach (CardDeckKind card in player.jailCards)
                {
                    players[creditor].jailCards.Add(card);
                    Emit(new MatchEvent { kind = MatchEventKind.JailCardGained, player = creditor, deck = card, other = who, space = -1, from = -1 });
                }
                if (interest > 0)
                {
                    Charge(creditor, Party.Bank, interest, "Interest on mortgaged property");
                }
            }
            else
            {
                Transfer(who, Party.Bank, player.cash, "Bankruptcy");
                foreach (int space in owned)
                {
                    deeds[space].owner = Party.Bank;
                    deeds[space].mortgaged = false;
                    Emit(MatchEventKind.OwnerChanged, who, Party.Bank, space);
                    if (rules.auctions && !auctionQueue.Contains(space))
                    {
                        auctionQueue.Add(space);
                    }
                }
                foreach (CardDeckKind card in player.jailCards)
                {
                    ReturnJailCard(card);
                }
            }
            player.jailCards.Clear();
            player.cash = 0;
            player.inJail = false;
            player.bankrupt = true;
            player.bankruptRound = round;
            player.place = ActiveCount + 1;
            bankruptcies++;
            Emit(MatchEventKind.Bankrupt, who, creditor);

            debts.RemoveAll(debt => debt.debtor == who || debt.creditor == who);
            if (auction.Running && auction.bidders.Contains(who))
            {
                RemoveBidder(who);
            }
            if (who == current)
            {
                extraRoll = false;
                mrMonopolyPending = false;
                pendingMove = 0;
                pendingPurchase = -1;
            }
        }

        private bool CheckGameEnd()
        {
            if (phase == MatchPhase.GameOver)
            {
                return true;
            }
            int active = ActiveCount;
            int limit = rules.bankruptciesToEnd > 0 ? Math.Min(rules.bankruptciesToEnd, players.Count - 1) : int.MaxValue;
            if (active > 1 && bankruptcies < limit)
            {
                return false;
            }
            FinishGame();
            return true;
        }

        private void FinishGame()
        {
            List<PlayerState> ranking = players.Where(p => !p.bankrupt).OrderByDescending(p => NetWorth(p.index))
                .ThenByDescending(p => p.cash).ToList();
            for (int i = 0; i < ranking.Count; i++)
            {
                ranking[i].place = i + 1;
            }
            winner = ranking.Count > 0 ? ranking[0].index : -1;
            debts.Clear();
            auction = new AuctionState();
            auctionQueue.Clear();
            pendingPurchase = -1;
            phase = MatchPhase.GameOver;
            Emit(MatchEventKind.GameOver, winner, value: winner >= 0 ? NetWorth(winner) : 0);
        }

        /// <summary>Ends the match now and ranks the players by net worth (a player quitting a hot seat game early).</summary>
        public void Resign()
        {
            if (phase != MatchPhase.GameOver && phase != MatchPhase.Setup)
            {
                FinishGame();
            }
        }
    }
}
