using NUnit.Framework;

namespace Portfolio.EndlessRunner.Tests
{
    public class RunnerPoseStateTest
    {
        [Test]
        public void ARunnerStandingAtTheStartPacksToItsOneFlag()
        {
            var state = new RunnerPoseState { Grounded = true };
            Assert.AreEqual(state, RunnerPoseState.Unpack(state.Pack()));
            Assert.AreEqual(0u, new RunnerPoseState().Pack());
            Assert.AreNotEqual(0u, state.Pack());
        }

        [Test]
        public void EveryFlagSurvivesTheTrip()
        {
            var states = new[]
            {
                new RunnerPoseState { Running = true },
                new RunnerPoseState { Grounded = true },
                new RunnerPoseState { Sliding = true },
                new RunnerPoseState { Invulnerable = true },
                new RunnerPoseState { LaneChange = -1 },
                new RunnerPoseState { LaneChange = 1 },
                new RunnerPoseState { Launched = true },
                new RunnerPoseState { SuperJump = true },
                new RunnerPoseState { Shield = true },
                new RunnerPoseState { Magnet = true },
                new RunnerPoseState { Dead = true },
                new RunnerPoseState { Finished = true }
            };
            for (int i = 0; i < states.Length; i++)
            {
                RunnerPoseState back = RunnerPoseState.Unpack(states[i].Pack());
                Assert.AreEqual(states[i], back, $"flag {i}");
                for (int other = 0; other < states.Length; other++)
                {
                    if (other != i)
                    {
                        Assert.AreNotEqual(states[other].Pack(), states[i].Pack(), $"flags {i} and {other} share a bit");
                    }
                }
            }
        }

        [Test]
        public void EveryCombinationSurvivesTheTrip()
        {
            // Ten switches and the three ways of the lane change.
            for (int bits = 0; bits < 1 << 10; bits++)
            {
                for (int lane = -1; lane <= 1; lane++)
                {
                    var state = new RunnerPoseState
                    {
                        Running = (bits & 1) != 0,
                        Grounded = (bits & 2) != 0,
                        Sliding = (bits & 4) != 0,
                        Invulnerable = (bits & 8) != 0,
                        Launched = (bits & 16) != 0,
                        SuperJump = (bits & 32) != 0,
                        Shield = (bits & 64) != 0,
                        Magnet = (bits & 128) != 0,
                        Dead = (bits & 256) != 0,
                        Finished = (bits & 512) != 0,
                        LaneChange = lane
                    };
                    RunnerPoseState back = RunnerPoseState.Unpack(state.Pack());
                    Assert.AreEqual(state.Running, back.Running);
                    Assert.AreEqual(state.Grounded, back.Grounded);
                    Assert.AreEqual(state.Sliding, back.Sliding);
                    Assert.AreEqual(state.Invulnerable, back.Invulnerable);
                    Assert.AreEqual(state.Launched, back.Launched);
                    Assert.AreEqual(state.SuperJump, back.SuperJump);
                    Assert.AreEqual(state.Shield, back.Shield);
                    Assert.AreEqual(state.Magnet, back.Magnet);
                    Assert.AreEqual(state.Dead, back.Dead);
                    Assert.AreEqual(state.Finished, back.Finished);
                    Assert.AreEqual(state.LaneChange, back.LaneChange);
                }
            }
        }

        [Test]
        public void ARunIsDoneWhenTheRunnerIsDeadOrOverTheLine()
        {
            Assert.IsFalse(new RunnerPoseState { Running = true, Grounded = true }.Done);
            Assert.IsTrue(new RunnerPoseState { Dead = true }.Done);
            Assert.IsTrue(new RunnerPoseState { Finished = true }.Done);
        }

        [Test]
        public void ALaneChangeOfAnySizeIsADirection()
        {
            Assert.AreEqual(-1, RunnerPoseState.Unpack(new RunnerPoseState { LaneChange = -3 }.Pack()).LaneChange);
            Assert.AreEqual(1, RunnerPoseState.Unpack(new RunnerPoseState { LaneChange = 2 }.Pack()).LaneChange);
        }
    }
}
