using NUnit.Framework;

namespace Portfolio.EndlessRunner.Tests
{
    /// <summary>
    /// The shared screen and the remote controls: what the screen tells a remote about its seat
    /// (<see cref="RunnerRemoteStatus"/>) and how it reads back, and the state of a run as the remote hears it.
    /// </summary>
    public class RemoteControlTest
    {
        [Test]
        public void AStatusReadsBackAsItWasWritten()
        {
            var status = new RunnerRemoteStatus
            {
                Hearts = 2,
                MaxHearts = 3,
                Coins = 14,
                Score = 263,
                Distance = 123,
                Length = 800,
                Place = 2,
                State = RemoteRunState.Running
            };
            status.SetPowerUpSeconds(PowerUpType.Shield, 6.2f);
            status.SetPowerUpSeconds(PowerUpType.SuperJump, 0.4f);
            string data = status.Write();
            Assert.AreEqual("h=2;m=3;c=14;s=263;d=123;l=800;p=2;st=1;pu=0,7,0,1", data, "numbers only, in the form of the room options");

            RunnerRemoteStatus read = RunnerRemoteStatus.Parse(data);
            Assert.IsNotNull(read);
            Assert.AreEqual(2, read.Hearts);
            Assert.AreEqual(3, read.MaxHearts);
            Assert.AreEqual(14, read.Coins);
            Assert.AreEqual(263, read.Score);
            Assert.AreEqual(123, read.Distance);
            Assert.AreEqual(800, read.Length);
            Assert.AreEqual(2, read.Place);
            Assert.AreEqual(RemoteRunState.Running, read.State);
            Assert.AreEqual(0f, read.PowerUpSeconds(PowerUpType.Magnet));
            Assert.AreEqual(7f, read.PowerUpSeconds(PowerUpType.Shield), "whole seconds, rounded up: a power-up about to end still shows");
            Assert.AreEqual(0f, read.PowerUpSeconds(PowerUpType.Multiplier));
            Assert.AreEqual(1f, read.PowerUpSeconds(PowerUpType.SuperJump));
        }

        [Test]
        public void NothingOrAnotherGamesStatusIsNoStatus()
        {
            Assert.IsNull(RunnerRemoteStatus.Parse(null));
            Assert.IsNull(RunnerRemoteStatus.Parse(string.Empty));
            Assert.IsNull(RunnerRemoteStatus.Parse("lives=3;weapon=Laser"), "a status of another game has no hearts");
        }

        [Test]
        public void AStatusMissingPartsReadsWhatItHas()
        {
            RunnerRemoteStatus read = RunnerRemoteStatus.Parse("h=1;m=3;st=9;pu=3,x");
            Assert.IsNotNull(read);
            Assert.AreEqual(1, read.Hearts);
            Assert.AreEqual(0, read.Coins);
            Assert.AreEqual(0, read.Place, "unknown until the race has places");
            Assert.AreEqual(RemoteRunState.Out, read.State, "a state the panel does not know is the last one it does");
            Assert.AreEqual(3f, read.PowerUpSeconds(PowerUpType.Magnet));
            Assert.AreEqual(0f, read.PowerUpSeconds(PowerUpType.Shield), "a value that is no number is none");
        }

        [Test]
        public void APowerUpThatIsOverHasNoSeconds()
        {
            var status = new RunnerRemoteStatus();
            status.SetPowerUpSeconds(PowerUpType.Magnet, -2f);
            status.SetPowerUpSeconds(PowerUpType.Shield, 0f);
            Assert.AreEqual(0f, status.PowerUpSeconds(PowerUpType.Magnet));
            Assert.AreEqual(0f, status.PowerUpSeconds(PowerUpType.Shield));
            Assert.AreEqual("h=0;m=0;c=0;s=0;d=0;l=0;p=0;st=0;pu=0,0,0,0", status.Write());
        }

        [Test]
        public void TheStateOfARunIsWhatTheRemoteHears()
        {
            Assert.AreEqual(RemoteRunState.Ready, RunnerRemoteStatus.StateOf(RunPhase.Menu, false));
            Assert.AreEqual(RemoteRunState.Ready, RunnerRemoteStatus.StateOf(RunPhase.Countdown, false));
            Assert.AreEqual(RemoteRunState.Running, RunnerRemoteStatus.StateOf(RunPhase.Running, false));
            Assert.AreEqual(RemoteRunState.Finished, RunnerRemoteStatus.StateOf(RunPhase.Finishing, true));
            Assert.AreEqual(RemoteRunState.Out, RunnerRemoteStatus.StateOf(RunPhase.Dying, false));
            Assert.AreEqual(RemoteRunState.Finished, RunnerRemoteStatus.StateOf(RunPhase.Watching, true), "done and over the line");
            Assert.AreEqual(RemoteRunState.Out, RunnerRemoteStatus.StateOf(RunPhase.Watching, false), "done and out of hearts");
        }
    }
}
