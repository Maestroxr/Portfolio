using NUnit.Framework;

namespace Portfolio.MemoryCards.Tests
{
    /// <summary>
    /// The seats of an online game as the device sees them (<see cref="OnlineSeats"/>): board seats against room seats,
    /// which seats are played here, and who is spoken to as "you".
    /// </summary>
    public class OnlineSeatsTest
    {
        [Test]
        public void BoardSeatsFollowTheOrderOfTheRoomSeats()
        {
            var seats = new OnlineSeats(new[] { 0, 2, 3 }, new[] { 2 }, 2);
            Assert.AreEqual(3, seats.Count);
            Assert.AreEqual(0, seats.BoardSeatOf(0));
            Assert.AreEqual(1, seats.BoardSeatOf(2));
            Assert.AreEqual(2, seats.BoardSeatOf(3));
            Assert.AreEqual(-1, seats.BoardSeatOf(1), "nobody sits in room seat 1");
            Assert.AreEqual(3, seats.RoomSeatOf(2));
            Assert.AreEqual(-1, seats.RoomSeatOf(3), "the board has three seats");
            Assert.AreEqual(-1, seats.RoomSeatOf(-1));
        }

        [Test]
        public void OnePlayerAtTheDeviceIsYou()
        {
            var seats = new OnlineSeats(new[] { 0, 1 }, new[] { 1 }, 1);
            Assert.IsFalse(seats.IsParty);
            Assert.AreEqual(1, seats.Device);
            CollectionAssert.AreEqual(new[] { 1 }, seats.Local);
            Assert.IsTrue(seats.IsLocal(1));
            Assert.IsFalse(seats.IsLocal(0));
            Assert.IsTrue(seats.IsYou(1));
            Assert.IsFalse(seats.IsYou(0));
        }

        [Test]
        public void APartyAtTheDeviceGoesByName()
        {
            // The device sits in room seat 0 and its companion in 3; room seat 1 is somebody else's and 2 is empty.
            var seats = new OnlineSeats(new[] { 0, 1, 3 }, new[] { 3, 0 }, 0);
            Assert.IsTrue(seats.IsParty);
            CollectionAssert.AreEqual(new[] { 0, 2 }, seats.Local, "the seats played here, in seat order");
            Assert.AreEqual(0, seats.Device);
            Assert.IsTrue(seats.IsLocal(0));
            Assert.IsFalse(seats.IsLocal(1));
            Assert.IsTrue(seats.IsLocal(2));
            Assert.IsFalse(seats.IsYou(0), "with several people here nobody is just you");
            Assert.IsFalse(seats.IsYou(2));
            Assert.AreEqual(3, seats.RoomSeatOf(2), "the companion's flips go out for room seat 3");
        }

        [Test]
        public void SeatsNobodyHoldsAreNeitherLocalNorTheDevice()
        {
            var seats = new OnlineSeats(new[] { 0, 1 }, new[] { 5, 1, 1 }, -1);
            CollectionAssert.AreEqual(new[] { 1 }, seats.Local, "a seat nobody holds is dropped, a seat named twice counts once");
            Assert.AreEqual(-1, seats.Device);
            Assert.IsFalse(seats.IsLocal(0));
            Assert.IsTrue(seats.IsYou(1));
        }
    }
}
