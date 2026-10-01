using System;
using System.Collections.Generic;
using System.Linq;
using Gamebox;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Portfolio.EndlessRunner.Tests
{
    /// <summary>
    /// The race of several runners at one device: the controls every seat binds, each runner on its own seat's controls,
    /// a run per runner, who leads and how far the field is spread, the places and the winner, and who gets a coin.
    /// </summary>
    public class LocalRaceTest
    {
        private readonly List<Object> made = new List<Object>();
        private ScriptedInput input;
        private Func<int> frameBefore;
        private int frame;

        [SetUp]
        public void SetUp()
        {
            input = new ScriptedInput();
            ControlInput.Source = input;
            frameBefore = PlayerControls.Frame;
            frame = 1;
            PlayerControls.Frame = () => frame;
        }

        [TearDown]
        public void TearDown()
        {
            ControlInput.Source = null;
            PlayerControls.Frame = frameBefore;
            foreach (Object thing in made)
            {
                if (thing != null)
                {
                    Object.DestroyImmediate(thing);
                }
            }
            made.Clear();
        }

        private void NextFrame()
        {
            input.EndFrame();
            frame++;
        }

        private T Make<T>(string name) where T : Component
        {
            var holder = new GameObject(name);
            made.Add(holder);
            return holder.AddComponent<T>();
        }

        private static RunnerRun Run(float z, RunPhase phase, int hearts = 3)
        {
            var run = new RunnerRun(null, null);
            run.Reset(hearts);
            run.RunTo(z);
            run.Phase = phase;
            return run;
        }

        private static LocalRace Race(params RunnerRun[] runs)
        {
            var race = new LocalRace(null);
            for (int i = 0; i < runs.Length; i++)
            {
                race.Add(runs[i], new LocalSeat(i) { Name = $"P{i + 1}", Color = Color.white });
            }
            return race;
        }

        // ------------------------------------------------------------------------------------------------
        // Controls
        // ------------------------------------------------------------------------------------------------

        [Test]
        public void TheFourMovesAreBoundToTheDirectionsOfEverySeat()
        {
            ControlScheme scheme = SeatInput.Scheme();
            CollectionAssert.AreEqual(new[] { "left", "right", "jump", "slide" }, scheme.Actions.Select(action => action.Id));
            CollectionAssert.AreEqual(new[] { ControlRole.Left, ControlRole.Right, ControlRole.Up, ControlRole.Down }, scheme.Actions.Select(action => action.Role));
            var bindings = new ControlBindings(scheme);
            Assert.AreEqual(InputBinding.Of(KeyCode.A), bindings.Get(0, "left", 0));
            Assert.AreEqual(InputBinding.Of(KeyCode.W), bindings.Get(0, "jump", 0));
            Assert.AreEqual(InputBinding.Of(KeyCode.RightArrow), bindings.Get(1, "right", 0));
            Assert.AreEqual(InputBinding.Of(KeyCode.DownArrow), bindings.Get(1, "slide", 0));
            Assert.IsEmpty(bindings.Conflicts(ControlBindings.MaxSeats), "four runners play at once without changing a thing");
            Assert.IsTrue(bindings.Complete(ControlBindings.MaxSeats, out _, out _));
        }

        [Test]
        public void EveryRunnerMovesOnItsOwnSeatsControls()
        {
            var bindings = new ControlBindings(SeatInput.Scheme());
            var first = new SeatInput(bindings.For(0));
            var second = new SeatInput(bindings.For(1));

            input.Press(KeyCode.W);
            input.Press(KeyCode.LeftArrow);
            first.Read();
            second.Read();
            Assert.IsTrue(first.Jump);
            Assert.IsFalse(first.Left);
            Assert.IsTrue(second.Left);
            Assert.IsFalse(second.Jump);

            NextFrame();
            first.Read();
            second.Read();
            Assert.IsFalse(first.Jump, "a key held down jumps once");
            Assert.IsFalse(second.Left);

            first.Press(RunnerAction.Slide);
            first.Read();
            Assert.IsTrue(first.Slide, "the autopilot presses through the seat's input");
            NextFrame();
            first.Read();
            Assert.IsFalse(first.Slide);
        }

        [Test]
        public void ARunnerAloneKeepsItsKeysAndARaceHandsItsSeatsControls()
        {
            RunnerPlayer player = Make<RunnerPlayer>("Runner");
            Assert.IsInstanceOf<RunnerInput>(player.InputSource);
            var seat = new SeatInput(new ControlBindings(SeatInput.Scheme()).For(2));
            player.InputSource = seat;
            Assert.AreSame(seat, player.InputSource);
            player.InputSource = null;
            Assert.IsInstanceOf<RunnerInput>(player.InputSource);
        }

        [Test]
        public void TheRulesAreARaceOfTwoToFourInTheRacersColours()
        {
            RunnerGameManager manager = Make<RunnerGameManager>("Game");
            LocalPlayRules rules = manager.LocalPlay;
            Assert.IsTrue(rules.Simultaneous);
            Assert.AreEqual(2, rules.MinPlayers);
            Assert.AreEqual(4, rules.MaxPlayers);
            Assert.IsFalse(rules.AllowComputers);
            Assert.AreEqual(4, rules.Controls.Actions.Count);
            for (int seat = 0; seat < 4; seat++)
            {
                Assert.AreEqual(manager.racerColors[seat], rules.SeatColor(seat), "the chips of the setup and the runners agree");
            }
            Assert.AreSame(rules, manager.LocalPlay);
        }

        // ------------------------------------------------------------------------------------------------
        // A run per runner
        // ------------------------------------------------------------------------------------------------

        [Test]
        public void EveryRunCountsItsOwnCoinsHeartsAndPowerUps()
        {
            RunnerRun a = Run(0f, RunPhase.Running);
            RunnerRun b = Run(0f, RunPhase.Running);
            a.GivePowerUp(PowerUpType.Multiplier, 12f);
            a.Collect(1, 10);
            b.Collect(1, 10);
            b.Collect(5, 10);
            Assert.AreEqual(2, a.Coins, "double coins count twice");
            Assert.AreEqual(6, b.Coins);
            Assert.IsTrue(a.IsPowerUpActive(PowerUpType.Multiplier));
            Assert.IsFalse(b.IsPowerUpActive(PowerUpType.Multiplier));

            a.RunTo(120.7f);
            a.RunTo(119f);
            Assert.AreEqual(120.7f, a.Distance, 0.001f, "the distance is the farthest the runner got");
            Assert.AreEqual(119f, a.Z, 0.001f);
            Assert.AreEqual(120f + 20f, a.Score, 0.001f);

            Assert.IsFalse(b.LoseHeart());
            Assert.IsFalse(b.LoseHeart());
            Assert.IsTrue(b.LoseHeart(), "the third heart was the last");
            Assert.AreEqual(0, b.Hearts);
            Assert.AreEqual(3, a.Hearts);

            a.TickPowerUps(5f);
            Assert.AreEqual(7f, a.PowerUpLeft(PowerUpType.Multiplier), 0.001f);
            a.TickPowerUps(10f);
            Assert.IsFalse(a.IsPowerUpActive(PowerUpType.Multiplier));

            b.Reset(4);
            Assert.AreEqual(4, b.Hearts);
            Assert.AreEqual(0, b.Coins);
            Assert.AreEqual(0, b.HeartsLost);
            Assert.AreEqual(0f, b.Score);
        }

        // ------------------------------------------------------------------------------------------------
        // The race
        // ------------------------------------------------------------------------------------------------

        [Test]
        public void TheLeaderIsTheFirstRunnerStillRunning()
        {
            RunnerRun back = Run(80f, RunPhase.Running);
            RunnerRun front = Run(140f, RunPhase.Running);
            RunnerRun finished = Run(300f, RunPhase.Watching);
            LocalRace race = Race(back, front, finished);
            Assert.AreSame(front, race.Leader());
            front.Phase = RunPhase.Dying;
            Assert.AreSame(back, race.Leader(), "a runner who fell leads nobody");
            back.Phase = RunPhase.Finishing;
            Assert.IsNull(race.Leader());
        }

        [Test]
        public void TheTrackReachesFromTheLastRunnerOnItToTheFirst()
        {
            RunnerRun last = Run(40f, RunPhase.Running);
            RunnerRun first = Run(190f, RunPhase.Finishing);
            RunnerRun done = Run(10f, RunPhase.Watching);
            LocalRace race = Race(last, first, done);
            Assert.IsTrue(race.TrySpan(out float front, out float back));
            Assert.AreEqual(190f, front);
            Assert.AreEqual(40f, back, "a runner who is done watches the others and needs no track");
            last.Phase = first.Phase = RunPhase.Watching;
            Assert.IsFalse(race.TrySpan(out _, out _));
        }

        [Test]
        public void TheRaceIsOverWhenEveryRunIs()
        {
            RunnerRun a = Run(10f, RunPhase.Watching);
            RunnerRun b = Run(10f, RunPhase.Dying);
            LocalRace race = Race(a, b);
            Assert.IsFalse(race.IsOver, "a runner who is falling is not done yet");
            b.Phase = RunPhase.Watching;
            Assert.IsTrue(race.IsOver);
        }

        [Test]
        public void ThePlacesGoByScoreAndTheWinnerIsFirst()
        {
            RunnerRun a = Run(300f, RunPhase.Watching);
            RunnerRun b = Run(250f, RunPhase.Watching);
            RunnerRun c = Run(320f, RunPhase.Running);
            b.Collect(10, 10);
            LocalRace race = Race(a, b, c);
            race.Rank();
            CollectionAssert.AreEqual(new[] { 1, 2, 0 }, race.Ranking.Select(racer => racer.Seat));
            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, race.Ranking.Select(racer => racer.Place));
            Assert.AreEqual(350, race.Ranking[0].Score);
            Assert.IsTrue(race.Ranking[0].Done);
            Assert.IsFalse(race.Ranking[1].Done);
            Assert.AreEqual(10, race.Ranking[0].Coins);
            CollectionAssert.AreEqual(new[] { b }, race.Winners());
            Assert.AreEqual(1, b.Racer.Place);
            Assert.AreEqual("P2", b.Racer.Name);
        }

        [Test]
        public void EqualScoresShareTheFirstPlace()
        {
            RunnerRun a = Run(200f, RunPhase.Watching);
            RunnerRun b = Run(200f, RunPhase.Watching);
            RunnerRun c = Run(150f, RunPhase.Watching);
            LocalRace race = Race(a, b, c);
            race.Rank();
            CollectionAssert.AreEqual(new[] { a, b }, race.Winners(), "a tie: nobody wins alone");
            Assert.AreEqual(3, c.Racer.Place);
        }

        [Test]
        public void EveryRunnerHasItsRunInTheRace()
        {
            RunnerPlayer one = Make<RunnerPlayer>("One");
            RunnerPlayer two = Make<RunnerPlayer>("Two");
            var first = new RunnerRun(one, null);
            var second = new RunnerRun(two, null);
            LocalRace race = Race(first, second);
            Assert.AreSame(first, race.RunOf(one));
            Assert.AreSame(second, race.RunOf(two));
            Assert.IsNull(race.RunOf(null));
            Assert.AreEqual(1, second.Racer.Slot);
            Assert.AreEqual(1, second.Seat.Index);
        }

        // ------------------------------------------------------------------------------------------------
        // Pickups
        // ------------------------------------------------------------------------------------------------

        [Test]
        public void ACoinOrAPowerUpIsTheFirstRunnersOnly()
        {
            RunnerPlayer one = Make<RunnerPlayer>("One");
            RunnerPlayer two = Make<RunnerPlayer>("Two");
            Coin coin = Make<Coin>("Coin");
            coin.OnSpawned();
            Assert.IsTrue(coin.TryTouch(one));
            Assert.IsFalse(coin.TryTouch(two), "the coin is gone for the others");
            Assert.IsFalse(coin.TryTouch(one));
            Assert.IsTrue(coin.Collected);

            PowerUpPickup shield = Make<PowerUpPickup>("Shield");
            shield.OnSpawned();
            Assert.IsTrue(shield.TryTouch(two));
            Assert.IsFalse(shield.TryTouch(one));

            coin.OnSpawned();
            Assert.IsTrue(coin.TryTouch(two), "a pooled coin out on the track again is anybody's");
        }

        [Test]
        public void ABouncePadThrowsEveryRunnerOnce()
        {
            RunnerPlayer one = Make<RunnerPlayer>("One");
            RunnerPlayer two = Make<RunnerPlayer>("Two");
            JumpPad pad = Make<JumpPad>("Pad");
            pad.OnSpawned();
            Assert.IsTrue(pad.TryTouch(one));
            Assert.IsFalse(pad.TryTouch(one), "standing on the pad bounces once");
            Assert.IsTrue(pad.TryTouch(two), "the next runner bounces too");
            Assert.IsFalse(pad.Collected);
            two.transform.position = new Vector3(0f, 3f, 0f);
            pad.OnSpawned();
            Assert.IsFalse(pad.TryTouch(two), "a runner flying over the pad does not bounce");
        }
    }
}
