namespace Portfolio.Monopoly
{
    /// <summary>
    /// What the interface asks for in the name of a player: the choices of a turn, managing property and trading.
    /// <see cref="MonopolyController"/> gives the commands straight to the rules engine of a game played at one device;
    /// in an online match <see cref="MonopolyOnlineController"/> sends them to the server, and they count once they come
    /// back in the action log, in their place among the commands of the others. The interface reaches the one in charge
    /// through <see cref="MonopolyGameManager.Commands"/>.
    /// </summary>
    public interface IMonopolyCommands
    {
        void Roll(int seat);

        void Buy(int seat);

        void DeclineBuy(int seat);

        void PayJailFine(int seat);

        void UseJailCard(int seat);

        void ChooseBus(int seat, int option);

        void ChooseDestination(int seat, int space);

        void Bid(int seat, int amount);

        void PassBid(int seat);

        void EndTurn(int seat);

        void DeclareBankruptcy(int seat);

        void Build(int seat, int space);

        void Sell(int seat, int space);

        void Mortgage(int seat, int space);

        void Unmortgage(int seat, int space);

        /// <summary>Opens the property manager for <paramref name="seat"/>, when the seat may manage now.</summary>
        void OpenManager(int seat);

        /// <summary>Opens the trade window for <paramref name="seat"/>, when the seat may trade now.</summary>
        void OpenTrade(int seat);

        void ProposeTrade(TradeOffer offer);
    }
}
