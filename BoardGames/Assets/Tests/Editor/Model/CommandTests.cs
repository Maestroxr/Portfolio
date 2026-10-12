using NUnit.Framework;

namespace Portfolio.Checkers.Tests
{
    /// <summary>The commands of the action log: what they write as a payload, what they read back, and what they refuse.</summary>
    public class CommandTests
    {
        [Test]
        public void KindsNeverChange()
        {
            Assert.AreEqual(0u, (uint)CommandKind.Move);
            Assert.AreEqual(1u, (uint)CommandKind.Resign);
            Assert.AreEqual(2u, (uint)CommandKind.SeatToComputer);
        }

        [Test]
        public void MovePayloadRoundTrips()
        {
            Move move = Move.Jump(new[] { 1, 19, 37 }, new[] { 10, 28 }, false);
            CheckersCommand command = CheckersCommand.MoveAlong(1, move);
            Assert.AreEqual(CommandKind.Move, command.Kind);
            Assert.AreEqual(1, command.seat);
            Assert.AreEqual("1,19,37", command.Payload());
            CheckersCommand back = CheckersCommand.Parse(1, CommandKind.Move, command.Payload());
            Assert.IsNotNull(back);
            Assert.AreEqual(CommandKind.Move, back.Kind);
            Assert.AreEqual(1, back.seat);
            CollectionAssert.AreEqual(new[] { 1, 19, 37 }, back.path);
            Assert.IsTrue(move.PathIs(back.path));
            CollectionAssert.AreEqual(new[] { 0, 9 }, CheckersCommand.Parse(0, CommandKind.Move, "0,9").path);
        }

        [Test]
        public void MoveCommandKeepsItsOwnCopyOfThePath()
        {
            Move move = Move.Plain(0, 9);
            CheckersCommand command = CheckersCommand.MoveAlong(0, move);
            command.path[1] = 11;
            Assert.AreEqual(9, move.To);
        }

        [Test]
        public void ResignPayloadRoundTrips()
        {
            CheckersCommand command = CheckersCommand.Resign(0);
            Assert.AreEqual(CommandKind.Resign, command.Kind);
            Assert.AreEqual("", command.Payload());
            CheckersCommand back = CheckersCommand.Parse(0, CommandKind.Resign, "");
            Assert.IsNotNull(back);
            Assert.AreEqual(CommandKind.Resign, back.Kind);
            Assert.AreEqual(0, back.seat);
            Assert.IsNotNull(CheckersCommand.Parse(0, CommandKind.Resign, null));
            Assert.IsNull(CheckersCommand.Parse(0, CommandKind.Resign, "1"));
        }

        [Test]
        public void SeatToComputerPayloadRoundTrips()
        {
            CheckersCommand command = CheckersCommand.ToComputer(1, 2);
            Assert.AreEqual(CommandKind.SeatToComputer, command.Kind);
            Assert.AreEqual("2", command.Payload());
            CheckersCommand back = CheckersCommand.Parse(1, CommandKind.SeatToComputer, "2");
            Assert.IsNotNull(back);
            Assert.AreEqual(2, back.level);
            Assert.AreEqual(1, back.seat);
            Assert.AreEqual(0, CheckersCommand.Parse(1, CommandKind.SeatToComputer, "0").level);
        }

        [TestCase("")]
        [TestCase("5")]
        [TestCase("1,,2")]
        [TestCase("1, 2")]
        [TestCase("a,b")]
        [TestCase("1,200")]
        [TestCase("-1,9")]
        [TestCase("1.5,9")]
        public void MalformedMovePayloadsAreNoCommand(string payload)
        {
            Assert.IsNull(CheckersCommand.Parse(0, CommandKind.Move, payload));
        }

        [Test]
        public void TooLongAPathIsNoCommand()
        {
            var numbers = new int[CheckersCommand.MaxPathLength + 1];
            for (int i = 0; i < numbers.Length; i++)
            {
                numbers[i] = i;
            }
            Assert.IsNull(CheckersCommand.Parse(0, CommandKind.Move, Gamebox.Lockstep.PayloadText.Numbers(',', numbers)));
            var longest = new int[CheckersCommand.MaxPathLength];
            for (int i = 0; i < longest.Length; i++)
            {
                longest[i] = i;
            }
            Assert.IsNotNull(CheckersCommand.Parse(0, CommandKind.Move, Gamebox.Lockstep.PayloadText.Numbers(',', longest)));
        }

        [TestCase("3")]
        [TestCase("-1")]
        [TestCase("")]
        [TestCase("one")]
        [TestCase("1,2")]
        public void MalformedLevelsAreNoCommand(string payload)
        {
            Assert.IsNull(CheckersCommand.Parse(0, CommandKind.SeatToComputer, payload));
        }

        [Test]
        public void UnknownKindIsNoCommand()
        {
            Assert.IsNull(CheckersCommand.Parse(0, (CommandKind)9, "1,2"));
        }
    }
}
