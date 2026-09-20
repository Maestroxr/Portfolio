using System.Collections;
using Gamebox;
using UnityEngine;

namespace Portfolio.Monopoly
{
    /// <summary>
    /// The local controller of the Monopoly module: it takes the commands of the human players from the interface
    /// (roll, buy, bid, build, trade...), checks that the player may give them now and passes them to the rules engine
    /// of the running match. Starting a game goes through the base <see cref="OfflineGameController"/> flow into
    /// <see cref="MonopolyGameManager.StartGame"/>.
    /// </summary>
    public class MonopolyController : OfflineGameController
    {
        public MonopolyGameManager Monopoly => BaseManager as MonopolyGameManager;

        private MonopolyMatch Match => Monopoly != null ? Monopoly.Match : null;

        /// <summary>Whether <paramref name="seat"/> is a human player the match waits for right now.</summary>
        private bool Deciding(int seat)
        {
            MonopolyMatch match = Match;
            return match != null && Monopoly.IsGameRunning && match.Decider == seat && !match.players[seat].bot && !match.HasEvents;
        }

        /// <summary>Whether <paramref name="seat"/> is a human player who may build, sell, mortgage or trade now.</summary>
        private bool Managing(int seat)
        {
            MonopolyMatch match = Match;
            return match != null && Monopoly.IsGameRunning && match.CanManage(seat) && !match.players[seat].bot && !match.HasEvents;
        }

        private void Done(bool accepted)
        {
            if (accepted)
            {
                Monopoly.HumanActed();
            }
            else
            {
                Monopoly.MonopolyUI.Sound?.Play(Sfx.Error, 0.7f);
            }
        }

        public void Roll(int seat)
        {
            if (Deciding(seat))
            {
                Monopoly.MonopolyUI.Manage.Close();
                Done(Match.Roll());
            }
        }

        public void Buy(int seat)
        {
            if (Deciding(seat))
            {
                Done(Match.Buy());
            }
        }

        public void DeclineBuy(int seat)
        {
            if (Deciding(seat))
            {
                Done(Match.DeclineBuy());
            }
        }

        public void PayJailFine(int seat)
        {
            if (Deciding(seat))
            {
                Done(Match.PayJailFine());
            }
        }

        public void UseJailCard(int seat)
        {
            if (Deciding(seat))
            {
                Done(Match.UseJailCard());
            }
        }

        public void ChooseBus(int seat, int option)
        {
            if (Deciding(seat))
            {
                Done(Match.ChooseBus(option));
            }
        }

        public void ChooseDestination(int seat, int space)
        {
            if (Deciding(seat))
            {
                Monopoly.Board.ClearHighlights();
                Done(Match.ChooseDestination(space));
            }
        }

        public void Bid(int seat, int amount)
        {
            if (Deciding(seat))
            {
                Done(Match.PlaceBid(seat, amount));
            }
        }

        public void PassBid(int seat)
        {
            if (Deciding(seat))
            {
                Done(Match.PassBid(seat));
            }
        }

        public void EndTurn(int seat)
        {
            if (Deciding(seat))
            {
                Monopoly.MonopolyUI.Manage.Close();
                Done(Match.EndTurn());
            }
        }

        public void DeclareBankruptcy(int seat)
        {
            if (Deciding(seat))
            {
                Monopoly.MonopolyUI.Manage.Close();
                Done(Match.DeclareBankruptcy());
            }
        }

        public void Build(int seat, int space)
        {
            if (Managing(seat))
            {
                Done(Match.Build(seat, space));
            }
        }

        public void Sell(int seat, int space)
        {
            if (Managing(seat))
            {
                Done(Match.SellBuilding(seat, space));
            }
        }

        public void Mortgage(int seat, int space)
        {
            if (Managing(seat))
            {
                Done(Match.Mortgage(seat, space));
            }
        }

        public void Unmortgage(int seat, int space)
        {
            if (Managing(seat))
            {
                Done(Match.Unmortgage(seat, space));
            }
        }

        public void OpenManager(int seat)
        {
            if (Managing(seat))
            {
                Monopoly.MonopolyUI.Trade.Close();
                Monopoly.MonopolyUI.Manage.Show(Match, seat, this);
            }
        }

        public void OpenTrade(int seat)
        {
            if (Managing(seat) && Match.ActiveCount > 1)
            {
                Monopoly.MonopolyUI.Manage.Close();
                Monopoly.MonopolyUI.Trade.Show(Match, seat, this, Monopoly.MonopolyUI.TokenSprite);
            }
        }

        /// <summary>A human player proposes a trade: the other side answers (on screen for a human, at once for a computer).</summary>
        public void ProposeTrade(TradeOffer offer)
        {
            if (offer == null || !Managing(offer.from) || !Match.CanTrade(offer, out _))
            {
                Done(false);
                return;
            }
            StartCoroutine(Propose(offer));
        }

        private IEnumerator Propose(TradeOffer offer)
        {
            yield return Monopoly.ResolveOffer(offer);
            Monopoly.HumanActed();
        }
    }
}
