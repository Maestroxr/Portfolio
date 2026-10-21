using NUnit.Framework;
using UnityEngine;

namespace Portfolio.MemoryCards.Tests
{
    /// <summary>
    /// The board of the remote panel (<see cref="RemoteBoardLayout"/>): the grid of a phone's panel keeps the shape of
    /// the cards and the order of the board, every card shows what the mirror says, and the hint follows the turn.
    /// </summary>
    public class RemoteBoardTest
    {
        [Test]
        public void TheCardsKeepTheirShapeAndFitThePanel()
        {
            // Four by three cards in a wide area: the height limits them.
            Vector2 cell = RemoteBoardLayout.CellSize(4, 3, new Vector2(1800f, 600f), 10f);
            Assert.AreEqual(RemoteBoardLayout.Aspect, cell.x / cell.y, 0.001f);
            Assert.AreEqual((600f - 20f) / 3f, cell.y, 0.001f);
            Assert.LessOrEqual(4 * cell.x + 30f, 1800f);
            // The same cards in a tall area (a phone held upright): the width limits them.
            Vector2 tall = RemoteBoardLayout.CellSize(4, 3, new Vector2(1000f, 3000f), 10f);
            Assert.AreEqual((1000f - 30f) / 4f, tall.x, 0.001f);
            Assert.AreEqual(tall.x / RemoteBoardLayout.Aspect, tall.y, 0.001f);
            Assert.AreEqual(Vector2.zero, RemoteBoardLayout.CellSize(0, 3, new Vector2(1000f, 3000f), 10f), "no columns, no cards");
            Assert.AreEqual(3, RemoteBoardLayout.Rows(12, 4));
            Assert.AreEqual(4, RemoteBoardLayout.Rows(13, 4), "a part row counts");
            Assert.AreEqual(0, RemoteBoardLayout.Rows(0, 4));
        }

        [Test]
        public void TheCardsGoAlongTheRowsFromTheTopLeft()
        {
            var cell = new Vector2(100f, 150f);
            Vector2 first = RemoteBoardLayout.CellPosition(0, 4, 2, cell, 10f);
            Vector2 second = RemoteBoardLayout.CellPosition(1, 4, 2, cell, 10f);
            Vector2 fifth = RemoteBoardLayout.CellPosition(4, 4, 2, cell, 10f);
            Vector2 last = RemoteBoardLayout.CellPosition(7, 4, 2, cell, 10f);
            Assert.AreEqual(110f, second.x - first.x, 0.001f, "the next card lies a cell and a gap to the right");
            Assert.AreEqual(first.y, second.y, 0.001f);
            Assert.AreEqual(first.x, fifth.x, 0.001f, "the fifth card starts the second row");
            Assert.AreEqual(-160f, fifth.y - first.y, 0.001f);
            Assert.AreEqual(-first.x, last.x, 0.001f, "the grid is centred");
            Assert.AreEqual(-first.y, last.y, 0.001f);
        }

        [Test]
        public void EveryCardShowsWhatTheMirrorSays()
        {
            Assert.AreEqual(RemoteCardLook.Back, RemoteBoardLayout.LookOf(new MemoryCard { State = CardState.Hidden }));
            Assert.AreEqual(RemoteCardLook.Face, RemoteBoardLayout.LookOf(new MemoryCard { State = CardState.Revealed }));
            Assert.AreEqual(RemoteCardLook.Found, RemoteBoardLayout.LookOf(new MemoryCard { State = CardState.Matched }));
            Assert.AreEqual(RemoteCardLook.Gone, RemoteBoardLayout.LookOf(new MemoryCard { State = CardState.Spent }));
            Assert.AreEqual(RemoteCardLook.Back, RemoteBoardLayout.LookOf(null));
            Assert.AreEqual(Color.white, RemoteBoardLayout.TintOf(RemoteCardLook.Back, false, true));
            Assert.Less(RemoteBoardLayout.TintOf(RemoteCardLook.Back, false, false).r, 1f, "a back is dimmed while the player may not tap");
            Assert.AreEqual(1f, RemoteBoardLayout.TintOf(RemoteCardLook.Back, true, true).b);
            Assert.Less(RemoteBoardLayout.TintOf(RemoteCardLook.Back, true, true).r, 1f, "ice is bluish");
            Assert.AreEqual(Color.white, RemoteBoardLayout.TintOf(RemoteCardLook.Face, false, false), "a face that is up is plain, whoever's turn it is");
            Assert.Less(RemoteBoardLayout.TintOf(RemoteCardLook.Found, false, true).a, 0.5f, "a set found stays faintly");
            Assert.Less(RemoteBoardLayout.TintOf(RemoteCardLook.Gone, false, true).a, RemoteBoardLayout.TintOf(RemoteCardLook.Found, false, true).a);
        }

        [Test]
        public void TheHintFollowsTheTurn()
        {
            Assert.AreEqual("Memorize the board", RemoteBoardLayout.HintKey(true, false));
            Assert.AreEqual("Memorize the board", RemoteBoardLayout.HintKey(true, true));
            Assert.AreEqual("Tap a card", RemoteBoardLayout.HintKey(false, true));
            Assert.AreEqual("Wait for your turn", RemoteBoardLayout.HintKey(false, false));
        }
    }
}
