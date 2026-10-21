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
            Assert.AreSame(second, race.RunAt(1));
            Assert.IsNull(race.RunAt(2));
        }

        // ------------------------------------------------------------------------------------------------
        // A party online: several runners at this device in a race of a room
        // ------------------------------------------------------------------------------------------------

        [Test]
        public void APartysRunsCarryTheirRunnersOfTheRoom()
        {
            // The device's player sits at seat 1 and starts third; the companion at seat 3, fourth.
            var device = new Racer { Seat = 1, Slot = 2, Name = "Host", Local = true };
            var companion = new Racer { Seat = 3, Slot = 3, Name = "Friend", Local = true };
            var party = new LocalRace(null);
            RunnerRun mine = Run(0f, RunPhase.Running);
            RunnerRun theirs = Run(0f, RunPhase.Running);
            party.Add(mine, new LocalSeat(0) { Name = device.Name }, device);
            party.Add(theirs, new LocalSeat(1) { Name = companion.Name }, companion);
            Assert.IsNull(party.Match, "a party plays no local match");
            Assert.AreSame(device, mine.Racer, "the run is its runner on the scoreboard of the race");
            Assert.AreEqual(2, mine.Racer.Slot, "the place at the start is the race's");
            Assert.AreSame(theirs, party.RunAt(3), "the run of a seat of the room");
            Assert.AreSame(mine, party.RunAt(1));
            Assert.IsNull(party.RunAt(0), "seat 0 runs on another device");
            Assert.AreEqual(1, theirs.Seat.Index, "the views go by the order at this device");

            theirs.Phase = RunPhase.Watching;
            Assert.IsFalse(party.IsOver);
            mine.Phase = RunPhase.Watching;
            Assert.IsTrue(party.IsOver, "every runner here is done: the views follow the others");
        }

        [Test]
        public void ARaceIsAPartyWhenSeveralRunnersRunAtThisDevice()
        {
            var setup = new RunnerGameManager.RaceSetup { LocalSeat = 1 };
            setup.DrivenSeats.Add(1);
            Assert.IsFalse(setup.IsParty, "one runner here races as it always did");
            setup.DrivenSeats.Add(3);
            Assert.IsTrue(setup.IsParty);
        }

        [Test]
        public void TheSharedScreenRunsEveryRemoteAsAPartyAndWatchesWithNone()
        {
            // The screen holds no seat: a remote's seat is never the device's own, so even one is an explicit run.
            // The screen holds no seat (the base's NoSeat, 255).
            var screen = new RunnerGameManager.RaceSetup { LocalSeat = 255, Screen = true };
            Assert.IsTrue(screen.Spectates, "a screen whose players all run on their own devices only watches");
            Assert.IsFalse(screen.IsParty);
            screen.DrivenSeats.Add(2);
            Assert.IsTrue(screen.IsParty, "the seat of a remote is run on the split screen machinery, with its own seat");
            Assert.IsFalse(screen.Spectates);
            screen.DrivenSeats.Remove(2);
            Assert.IsTrue(screen.Spectates, "the last remote went to its own device: the screen watches again");

            // A remote control drives nothing: its screen runs its seat.
            var remote = new RunnerGameManager.RaceSetup { LocalSeat = 0 };
            Assert.IsTrue(remote.Spectates);
            Assert.IsFalse(remote.IsParty);
            remote.DrivenSeats.Add(0);
            Assert.IsFalse(remote.IsParty, "back on its own device, the player's runner races as it always did");
            Assert.IsFalse(remote.Spectates);
        }

        [Test]
        public void ARunLeavesTheRaceWhenItsSeatIsPlayedElsewhere()
        {
            RunnerRun one = Run(40f, RunPhase.Running);
            RunnerRun two = Run(55f, RunPhase.Running);
            RunnerRun three = Run(30f, RunPhase.Running);
            LocalRace race = Race(one, two, three);
            Assert.IsTrue(race.Remove(two));
            Assert.IsFalse(race.Remove(two), "gone already");
            CollectionAssert.AreEqual(new[] { one, three }, race.Runs, "the runs that stay keep their order");
            Assert.AreSame(one, race.Leader());
            Assert.IsTrue(race.TrySpan(out float front, out float back));
            Assert.AreEqual(40f, front);
            Assert.AreEqual(30f, back);
            Assert.IsNull(race.RunAt(1), "the seat of the run that left is nobody's here");
            one.Phase = RunPhase.Watching;
            three.Phase = RunPhase.Watching;
            Assert.IsTrue(race.IsOver, "the run that left does not hold the race up");
        }

        [Test]
        public void ARunTakenOverMidRaceStartsFromWhatIsKnownOfIt()
        {
            var run = new RunnerRun(null, null);
            run.Reset(3);
            run.SeedProgress(2, 14, 10, 123.4f);
            Assert.AreEqual(2, run.Hearts);
            Assert.AreEqual(3, run.MaxHearts);
            Assert.AreEqual(1, run.HeartsLost);
            Assert.AreEqual(14, run.Coins);
            Assert.AreEqual(140f, run.CoinPoints);
            Assert.AreEqual(123.4f, run.Distance, 0.001f);
            Assert.AreEqual(123.4f, run.Z, 0.001f);
            Assert.AreEqual(123 + 140, Mathf.FloorToInt(run.Score));
            run.SeedProgress(7, -3, 10, -5f);
            Assert.AreEqual(3, run.Hearts, "no more hearts than the run has");
            Assert.AreEqual(0, run.Coins);
            Assert.AreEqual(0f, run.Distance);
        }

        [Test]
        public void ANewRunHasReportedNoScore()
        {
            RunnerRun run = Run(120f, RunPhase.Running);
            Assert.AreEqual(-1, run.ReportedScore);
            run.ReportedScore = 120;
            run.Reset(3);
            Assert.AreEqual(-1, run.ReportedScore, "the next race reports its first score again");
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
