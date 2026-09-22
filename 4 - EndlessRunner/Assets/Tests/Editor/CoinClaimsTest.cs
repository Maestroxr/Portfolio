using NUnit.Framework;

namespace Portfolio.EndlessRunner.Tests
{
    public class CoinClaimsTest
    {
        private const int Me = 0;
        private const int Rival = 1;

        [Test]
        public void ACoinCountsWhenTheServerSaysItIsOurs()
        {
            var claims = new CoinClaims();
            Assert.IsTrue(claims.Request(12, 1, 1));
            Assert.AreEqual(1, claims.PendingCount);
            Assert.AreEqual(0, claims.CoinsOf(Me), "asking is not having");

            ClaimResult result = claims.Settle(12, Me, 1, Me);
            Assert.AreEqual(ClaimOutcome.Awarded, result.Outcome);
            Assert.AreEqual(1, result.Coins);
            Assert.AreEqual(0, claims.PendingCount);
            Assert.AreEqual(1, claims.CoinsOf(Me));
            Assert.AreEqual(Me, claims.OwnerOf(12));
        }

        [Test]
        public void DoubleCoinsCountsAsItWasWhenTheCoinWasTouched()
        {
            var claims = new CoinClaims();
            claims.Request(5, 5, 2);
            ClaimResult gem = claims.Settle(5, Me, 5, Me);
            Assert.AreEqual(10, gem.Coins, "a gem with double coins");
            Assert.AreEqual(5, claims.CoinsOf(Me), "the server counts the gem once");
        }

        [Test]
        public void ACoinSomebodyElseGotFirstIsLost()
        {
            var claims = new CoinClaims();
            claims.Request(7, 1, 1);
            ClaimResult result = claims.Settle(7, Rival, 1, Me);
            Assert.AreEqual(ClaimOutcome.Lost, result.Outcome);
            Assert.AreEqual(0, result.Coins);
            Assert.AreEqual(0, claims.PendingCount);
            Assert.AreEqual(0, claims.CoinsOf(Me));
            Assert.AreEqual(1, claims.CoinsOf(Rival));
            Assert.AreEqual(Rival, claims.OwnerOf(7));
        }

        [Test]
        public void ACoinOfAnotherRunnerIsJustGone()
        {
            var claims = new CoinClaims();
            ClaimResult result = claims.Settle(3, Rival, 1, Me);
            Assert.AreEqual(ClaimOutcome.Remote, result.Outcome);
            Assert.IsTrue(claims.IsTaken(3));
            Assert.IsFalse(claims.Request(3, 1, 1), "nobody asks for a coin that has an owner");
        }

        [Test]
        public void ACoinIsAskedForOnce()
        {
            var claims = new CoinClaims();
            Assert.IsTrue(claims.Request(9, 1, 1));
            Assert.IsFalse(claims.Request(9, 1, 1));
            Assert.IsTrue(claims.IsPending(9));
            Assert.IsFalse(claims.IsTaken(9));
        }

        [Test]
        public void TheSameAnswerTwiceChangesNothing()
        {
            var claims = new CoinClaims();
            claims.Request(4, 1, 1);
            claims.Settle(4, Me, 1, Me);
            ClaimResult again = claims.Settle(4, Me, 1, Me);
            Assert.AreEqual(ClaimOutcome.Duplicate, again.Outcome);
            Assert.AreEqual(0, again.Coins);
            Assert.AreEqual(1, claims.CoinsOf(Me));
            Assert.AreEqual(ClaimOutcome.Duplicate, claims.Settle(4, Rival, 1, Me).Outcome, "and nobody takes it over");
            Assert.AreEqual(Me, claims.OwnerOf(4));
            Assert.AreEqual(0, claims.CoinsOf(Rival));
        }

        [Test]
        public void TheBooksAddUp()
        {
            var claims = new CoinClaims();
            for (int piece = 0; piece < 20; piece++)
            {
                claims.Request(piece, piece % 5 == 0 ? 5 : 1, 1);
            }
            for (int piece = 0; piece < 30; piece++)
            {
                // The rival is faster on every third coin, and takes ten the local runner never touched.
                int seat = piece % 3 == 0 || piece >= 20 ? Rival : Me;
                claims.Settle(piece, seat, piece % 5 == 0 ? 5 : 1, Me);
            }
            Assert.AreEqual(30, claims.SettledCount);
            Assert.AreEqual(30, claims.PiecesOf(Me) + claims.PiecesOf(Rival));
            Assert.AreEqual(claims.SettledCoins, claims.CoinsOf(Me) + claims.CoinsOf(Rival));
            Assert.AreEqual(0, claims.PendingCount);
            CollectionAssert.IsEmpty(System.Linq.Enumerable.Intersect(claims.PiecesOwnedBy(Me), claims.PiecesOwnedBy(Rival)));
            CollectionAssert.IsOrdered(claims.PiecesOwnedBy(Me));
            Assert.AreEqual(-1, claims.OwnerOf(99));
        }

        [Test]
        public void ANewRaceStartsWithEmptyBooks()
        {
            var claims = new CoinClaims();
            claims.Request(1, 1, 1);
            claims.Settle(2, Rival, 5, Me);
            claims.Clear();
            Assert.AreEqual(0, claims.PendingCount);
            Assert.AreEqual(0, claims.SettledCount);
            Assert.AreEqual(0, claims.SettledCoins);
            Assert.AreEqual(0, claims.CoinsOf(Rival));
            Assert.IsTrue(claims.Request(2, 5, 1));
        }
    }
}
