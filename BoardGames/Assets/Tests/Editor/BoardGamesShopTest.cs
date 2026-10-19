using Gamebox;
using Gamebox.Economy;
using Gamebox.Lockstep;
using NUnit.Framework;

namespace Portfolio.BoardGames.Tests
{
    /// <summary>
    /// The shop of the games: the definition sells the Hint Book for a dollar and Take Back for Tokens under the ids the
    /// cloud knows, both as perks, and the hint the Hint Book shows is a legal move of the game it is asked about that
    /// changes nothing of it.
    /// </summary>
    public class BoardGamesShopTest
    {
        [Test]
        public void TheDefinitionSellsTheTwoPerks()
        {
            var catalog = new CloudCatalog();
            var shop = new BoardGamesShop();
            shop.DefineCloud(catalog);
            Assert.AreEqual(GameType.BoardGames, shop.Game);
            Assert.AreEqual(2, catalog.Count);

            CloudItem book = catalog["BOARDGAMES_HINT_BOOK"];
            Assert.IsNotNull(book);
            Assert.AreEqual(GameType.BoardGames, book.Game);
            Assert.AreEqual(100, book.Cents);
            Assert.AreEqual(0, book.Tokens);
            Assert.AreEqual(ItemKind.Owned, book.Kind);
            Assert.AreEqual("com.skinnerboxes.boardgames.hintbook", book.AppleProductId);
            Assert.AreEqual("com.skinnerboxes.boardgames.hintbook", book.GooglePlayProductId);

            CloudItem takeBack = catalog["BOARDGAMES_TAKE_BACK"];
            Assert.IsNotNull(takeBack);
            Assert.AreEqual(GameType.BoardGames, takeBack.Game);
            Assert.AreEqual(150, takeBack.Tokens);
            Assert.AreEqual(0, takeBack.Cents);
            Assert.AreEqual(ItemKind.Owned, takeBack.Kind);

            Assert.AreEqual(new[] { "BOARDGAMES_HINT_BOOK" }, catalog.ForMoney().ConvertAll(item => item.Id));
            Assert.AreEqual(new[] { "BOARDGAMES_TAKE_BACK" }, catalog.ForTokens().ConvertAll(item => item.Id));
        }

        [Test]
        public void TheHintIsALegalChessMove()
        {
            ChessGame game = ChessGame.NewGame();
            BoardCommand hint = BoardAI.Hint(game, 0, new SeededRandom(7));
            Assert.IsNotNull(hint);
            Assert.AreEqual(CommandKind.Move, hint.Kind);
            Assert.AreEqual(0, hint.seat);
            Assert.AreEqual(3, hint.path.Length, "from, to, promotion");
            Assert.IsNotNull(game.FindLegal(hint.path[0], hint.path[1], hint.path[2]), "the hint is a legal move");
            Assert.AreEqual(0, game.ply, "a hint changes nothing of the game");
        }

        [Test]
        public void TheHintIsALegalDraughtsMove()
        {
            var game = new CheckersGame(Variant.English);
            BoardCommand hint = BoardAI.Hint(game, 1, new SeededRandom(7));
            Assert.IsNotNull(hint);
            Assert.AreEqual(CommandKind.Move, hint.Kind);
            Assert.AreEqual(1, hint.seat);
            Assert.IsNotNull(game.FindLegal(hint.path), "the hint is a legal move");
            Assert.AreEqual(0, game.ply, "a hint changes nothing of the game");
        }
    }
}
