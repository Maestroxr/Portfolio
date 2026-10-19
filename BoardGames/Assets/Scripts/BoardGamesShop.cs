using Gamebox;
using Gamebox.Economy;

namespace Portfolio.BoardGames
{
    /// <summary>
    /// What Board Games sells through the cloud (see <see cref="GameShopDefinition"/>): two perks for a person playing
    /// the computer at this device. The Hint Book, sold for money, adds a Hint button to the HUD that marks the move the
    /// computer would play at its strongest; Take Back, sold for Tokens, adds a Take Back button that undoes the person's
    /// last move together with the computer's reply. Both are read where they apply
    /// (<see cref="BoardGamesManager.HintOffered"/>, <see cref="BoardGamesManager.TakeBackOffered"/>), so nothing is
    /// delivered when they are bought. The looks have no pictures of the pieces (they are meshes), so the items wear the
    /// game's icon.
    /// </summary>
    public sealed class BoardGamesShop : GameShopDefinition
    {
        public const string HintBookId = "BOARDGAMES_HINT_BOOK";
        public const int HintBookCents = 100;
        public const string HintBookProductId = "com.skinnerboxes.boardgames.hintbook";
        public const string TakeBackId = "BOARDGAMES_TAKE_BACK";
        public const int TakeBackTokens = 150;

        public override GameType Game => GameType.BoardGames;

        /// <summary>Whether the account owns the Hint Book (as last known, so it holds offline too).</summary>
        public static bool HasHintBook => CloudEconomy.Owns(HintBookId);

        /// <summary>Whether the account owns Take Back.</summary>
        public static bool HasTakeBack => CloudEconomy.Owns(TakeBackId);

        public override void DefineCloud(CloudCatalog catalog)
        {
            catalog.AddForMoney(Game, HintBookId, "Hint Book", "A Hint button against the computer", HintBookCents, HintBookProductId);
            catalog.AddForTokens(Game, TakeBackId, "Take Back", "Take a move back against the computer", TakeBackTokens);
        }
    }
}
