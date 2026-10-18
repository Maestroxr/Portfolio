using NUnit.Framework;

namespace Portfolio.BoardGames.Tests
{
    /// <summary>
    /// The seats an online table is played from at one device: which seats count, who acts for the people here, and
    /// which side the camera shows them.
    /// </summary>
    public class DeviceSeatsTests
    {
        private static TableSetup Chess(int firstSeat)
        {
            return new TableSetup { ruleSet = (int)RuleSet.Chess, firstSeat = firstSeat };
        }

        [Test]
        public void NobodyHereIsNoSeat()
        {
            DeviceSeats none = DeviceSeats.None;
            Assert.AreEqual(0, none.Count);
            Assert.IsFalse(none.Any);
            Assert.IsFalse(none.Several);
            Assert.AreEqual(-1, none.First);
            Assert.IsFalse(none.Has(0));
            Assert.AreEqual(-1, none.Acting(0));
            Assert.AreEqual(Side.Light, none.HomeSide(Chess(1)));
            Assert.AreEqual("no seat", none.ToString());
        }

        [Test]
        public void OneSeatActsForItselfAndSitsAtTheBottom()
        {
            var one = new DeviceSeats(new[] { 1 });
            Assert.AreEqual(1, one.Count);
            Assert.IsTrue(one.Any);
            Assert.IsFalse(one.Several);
            Assert.AreEqual(1, one.First);
            Assert.IsTrue(one.Has(1));
            Assert.IsFalse(one.Has(0));
            Assert.AreEqual(1, one.Acting(1), "its own turn");
            Assert.AreEqual(1, one.Acting(0), "the other seat's turn: still the one seat here");
            Assert.AreEqual(1, one.Acting(-1), "the game is over");
            Assert.AreEqual(Side.Dark, one.HomeSide(Chess(0)), "seat 0 plays White, so seat 1 is Black and sits at the bottom");
            Assert.AreEqual(Side.Light, one.HomeSide(Chess(1)));
            Assert.AreEqual("1", one.ToString());
        }

        [Test]
        public void TwoSeatsFillTheTableAndFollowTheTurn()
        {
            var both = new DeviceSeats(new[] { 1, 0 });
            Assert.AreEqual(2, both.Count);
            Assert.IsTrue(both.Several);
            CollectionAssert.AreEqual(new[] { 0, 1 }, both.All, "in seat order whatever the order given");
            Assert.AreEqual(0, both.First);
            Assert.AreEqual(0, both.Acting(0));
            Assert.AreEqual(1, both.Acting(1));
            Assert.AreEqual(0, both.Acting(-1), "the game is over: the first seat");
            Assert.AreEqual(Side.Light, both.HomeSide(Chess(1)), "two people at the device see the board from White's side");
            Assert.AreEqual("0, 1", both.ToString());
        }

        [Test]
        public void SeatsATableDoesNotHaveAreDropped()
        {
            var odd = new DeviceSeats(new[] { -1, 1, 2, 1, 7 });
            Assert.AreEqual(1, odd.Count);
            Assert.IsTrue(odd.Has(1));
            Assert.IsFalse(odd.Has(2));
            Assert.AreEqual(0, new DeviceSeats(null).Count);
            Assert.AreEqual(0, new DeviceSeats(new[] { 5 }).Count);
        }
    }
}
