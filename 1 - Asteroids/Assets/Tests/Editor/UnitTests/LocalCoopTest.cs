using System;
using System.Collections.Generic;
using System.Linq;
using Gamebox;
using NUnit.Framework;
using UnityEngine;

namespace Portfolio.Asteroids.Tests
{
    /// <summary>
    /// Local co-op, several pilots at one device: the actions every pilot binds and their defaults, what the setup offers,
    /// how a pilot's controls fly the ship (both kinds of mission), the scores, ships and standings of the squad, and how
    /// the world treats the wingmen.
    /// </summary>
    public class LocalCoopTest
    {
        private readonly TemporaryObjects objects = new TemporaryObjects();
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
            objects.Dispose();
        }

        private void NextFrame()
        {
            input.EndFrame();
            frame++;
        }

        // ------------------------------------------------------------------ the controls

        [Test]
        public void EveryPilotHasEveryActionAndNoTwoShareAControl()
        {
            var controls = new ControlBindings(LocalCoopRules.Scheme);
            Assert.That(LocalCoopRules.Scheme.Actions.Count, Is.EqualTo(8));
            Assert.IsTrue(controls.Complete(CoopRules.MaxPilots, out int seat, out ControlAction action),
                $"Seat {seat} has nothing for {action?.Id}.");
            Assert.That(controls.Conflicts(CoopRules.MaxPilots), Is.Empty, "The defaults of four pilots never clash.");
            Assert.AreEqual(InputBinding.Of(KeyCode.W), controls.Get(0, LocalCoopRules.Thrust, 0));
            Assert.AreEqual(InputBinding.Of(KeyCode.Space), controls.Get(0, LocalCoopRules.Fire, 0));
            Assert.AreEqual(InputBinding.Of(KeyCode.UpArrow), controls.Get(1, LocalCoopRules.Thrust, 0));
            Assert.AreEqual(3, controls.Get(2, LocalCoopRules.Fire, 0).PadNumber, "Pilot 3 flies with pad 3 first.");
            Assert.AreEqual(4, controls.Get(3, LocalCoopRules.Bomb, 0).PadNumber, "Pilot 4 flies with pad 4 first.");
        }

        [Test]
        public void TheActionsHaveTheirRoles()
        {
            ControlScheme scheme = LocalCoopRules.Scheme;
            Assert.AreEqual(ControlRole.Left, scheme.Find(LocalCoopRules.Left).Role);
            Assert.AreEqual(ControlRole.Right, scheme.Find(LocalCoopRules.Right).Role);
            Assert.AreEqual(ControlRole.Up, scheme.Find(LocalCoopRules.Thrust).Role);
            Assert.AreEqual(ControlRole.Down, scheme.Find(LocalCoopRules.Brake).Role);
            Assert.AreEqual(ControlRole.Primary, scheme.Find(LocalCoopRules.Fire).Role);
            Assert.AreEqual(ControlRole.Secondary, scheme.Find(LocalCoopRules.Dash).Role);
            Assert.AreEqual(ControlRole.Tertiary, scheme.Find(LocalCoopRules.Bomb).Role);
            Assert.AreEqual(ControlRole.Quaternary, scheme.Find(LocalCoopRules.Cycle).Role);
        }

        [Test]
        public void TheKeysOfAPilotFlyTheShip()
        {
            var controls = new ControlBindings(LocalCoopRules.Scheme);
            var pilot = new LocalShipInput(controls.For(0));
            input.Press(KeyCode.D);
            input.Press(KeyCode.W);
            input.Press(KeyCode.Space);
            pilot.Read(null, 0.02f);
            Assert.AreEqual(-1f, pilot.Turn, 1e-5f, "D turns right (a negative turn).");
            Assert.AreEqual(1f, pilot.Thrust, 1e-5f);
            Assert.IsTrue(pilot.Fire);
            Assert.IsFalse(pilot.Brake);
            Assert.AreEqual(new Vector2(1f, 1f), pilot.Move, "Strike: up and to the right.");
            NextFrame();

            input.ReleaseAll();
            input.Press(KeyCode.A);
            input.Press(KeyCode.S);
            pilot.Read(null, 0.02f);
            Assert.AreEqual(1f, pilot.Turn, 1e-5f, "A turns left.");
            Assert.AreEqual(0f, pilot.Thrust, 1e-5f);
            Assert.IsTrue(pilot.Brake);
            Assert.IsFalse(pilot.Fire);
            Assert.AreEqual(new Vector2(-1f, -1f), pilot.Move, "Strike: down and to the left.");
            NextFrame();

            input.Press(KeyCode.W);
            input.Press(KeyCode.D);
            pilot.Read(null, 0.02f);
            Assert.AreEqual(Vector2.zero, pilot.Move, "Opposite keys cancel out.");
            Assert.AreEqual(0f, pilot.Turn, 1e-5f);
        }

        [Test]
        public void ThePressesCountInTheirFrameOnly()
        {
            var controls = new ControlBindings(LocalCoopRules.Scheme);
            var pilot = new LocalShipInput(controls.For(0));
            input.Press(KeyCode.LeftShift);
            input.Press(KeyCode.E);
            pilot.Read(null, 0.02f);
            Assert.IsTrue(pilot.DashPressed);
            Assert.IsTrue(pilot.BombPressed);
            Assert.IsTrue(pilot.CyclePressed, "In a strike mission the dash picks the next special, as in the single player game.");
            NextFrame();
            pilot.Read(null, 0.02f);
            Assert.IsFalse(pilot.DashPressed, "Held is not pressed again.");
            Assert.IsFalse(pilot.BombPressed);
            Assert.IsFalse(pilot.CyclePressed);
            NextFrame();
            input.Press(KeyCode.Q);
            pilot.Read(null, 0.02f);
            Assert.IsTrue(pilot.CyclePressed, "Next Special.");
            Assert.IsFalse(pilot.DashPressed);
        }

        [Test]
        public void EveryPilotHearsOnlyTheirOwnControls()
        {
            var controls = new ControlBindings(LocalCoopRules.Scheme);
            var first = new LocalShipInput(controls.For(0));
            var second = new LocalShipInput(controls.For(1));
            input.Press(KeyCode.LeftArrow);
            input.Press(KeyCode.RightControl);
            first.Read(null, 0.02f);
            second.Read(null, 0.02f);
            Assert.AreEqual(0f, first.Turn, 1e-5f, "The arrows are the second pilot's (the single player game's arrows do not count).");
            Assert.IsFalse(first.Fire);
            Assert.AreEqual(1f, second.Turn, 1e-5f);
            Assert.IsTrue(second.Fire);
        }

        [Test]
        public void APadStickFliesByHowFarItIsPushed()
        {
            var controls = new ControlBindings(LocalCoopRules.Scheme);
            var pilot = new LocalShipInput(controls.For(2));
            // Pad 3: the left stick half way to the left and all the way up (Unity's Y axis is down positive).
            input.SetAxis(3, 1, -0.5f);
            input.SetAxis(3, 2, -1f);
            input.Press(InputBinding.PadButton(3, 0).Key);
            pilot.Read(null, 0.02f);
            Assert.AreEqual(0.5f, pilot.Turn, 1e-5f);
            Assert.AreEqual(1f, pilot.Thrust, 1e-5f);
            Assert.IsTrue(pilot.Fire, "A fires.");
            Assert.AreEqual(-0.5f, pilot.Move.x, 1e-5f);
            Assert.AreEqual(1f, pilot.Move.y, 1e-5f);
        }

        [Test]
        public void APilotWithoutControlsDoesNothing()
        {
            var pilot = new LocalShipInput(null);
            input.Press(KeyCode.W);
            input.Press(KeyCode.Space);
            pilot.Read(null, 0.02f);
            Assert.AreEqual(0f, pilot.Thrust);
            Assert.IsFalse(pilot.Fire);
            Assert.AreEqual(Vector2.zero, pilot.Move);
        }

        // ------------------------------------------------------------------ the setup

        [Test]
        public void TheSetupSeatsTwoToFourPilotsInTheColoursOfTheRooms()
        {
            var rules = new AsteroidsLocalRules();
            Assert.AreEqual(2, rules.MinPlayers);
            Assert.AreEqual(4, rules.MaxPlayers);
            Assert.IsTrue(rules.Simultaneous);
            Assert.IsFalse(rules.AllowComputers);
            Assert.AreSame(LocalCoopRules.Scheme, rules.Controls);
            for (int seat = 0; seat < 4; seat++)
            {
                Assert.AreEqual(CoopRules.SeatColor(seat), rules.SeatColor(seat), $"Seat {seat}: the chip, the halo and the HUD agree.");
            }
            Assert.AreEqual("Pilot 2", rules.DefaultName(1));
            var match = new LocalMatch(GameType.Asteroids, rules) { Bindings = new ControlBindings(rules.Controls) };
            Assert.AreEqual(2, match.Count);
            Assert.IsNull(match.Problem(), "Two pilots with the default controls can start.");
        }

        [Test]
        public void TheOptionFollowsTheKindOfMission()
        {
            var rules = new AsteroidsLocalRules();
            rules.ForMode(MissionMode.Field);
            Assert.AreEqual(new[] { LocalCoopRules.LivesKey }, rules.Options.Select(option => option.Key).ToArray());
            var match = new LocalMatch(GameType.Asteroids, rules);
            Assert.AreEqual(3, LocalCoopRules.Lives(match), "Three ships each at first, as in a room.");
            match.SetOption(LocalCoopRules.LivesKey, 4);
            Assert.AreEqual(7, LocalCoopRules.Lives(match));
            match.SetOption(LocalCoopRules.LivesKey, 99);
            Assert.AreEqual(3, LocalCoopRules.Lives(match), "A choice that is not one falls back.");

            rules.ForMode(MissionMode.Strike);
            Assert.AreEqual(new[] { LocalCoopRules.DifficultyKey }, rules.Options.Select(option => option.Key).ToArray());
            match = new LocalMatch(GameType.Asteroids, rules);
            Assert.AreEqual(StrikeDifficulty.Veteran, LocalCoopRules.Difficulty(match));
            match.SetOption(LocalCoopRules.DifficultyKey, (int)StrikeDifficulty.Elite);
            Assert.AreEqual(StrikeDifficulty.Elite, LocalCoopRules.Difficulty(match));
            Assert.AreEqual(StrikeDifficulty.Veteran, LocalCoopRules.Difficulty(null));
        }

        [Test]
        public void ALockedMissionStopsTheStart()
        {
            var rules = new AsteroidsLocalRules { MissionProblem = () => "Pick an open mission on the title first." };
            var match = new LocalMatch(GameType.Asteroids, rules) { Bindings = new ControlBindings(rules.Controls) };
            Assert.AreEqual("Pick an open mission on the title first.", match.Problem());
        }

        // ------------------------------------------------------------------ the squad

        private static LocalSquad Squad(int pilots, int lives)
        {
            var seats = new List<LocalSeat>();
            for (int i = 0; i < pilots; i++)
            {
                seats.Add(new LocalSeat(i) { Name = $"P{i + 1}", Color = CoopRules.SeatColor(i) });
            }
            return new LocalSquad(seats, lives);
        }

        [Test]
        public void APilotIsOutWhenTheLastShipGoesAndTheMissionWhenEveryPilotIs()
        {
            LocalSquad squad = Squad(3, 2);
            SquadPilot first = squad[0];
            Assert.IsTrue(first.LoseShip(false), "One ship left: the pilot comes back.");
            Assert.AreEqual(1, first.Lives);
            Assert.IsFalse(first.LoseShip(false), "The last ship: out.");
            Assert.IsTrue(first.Out);
            Assert.IsFalse(squad.AllOut);
            Assert.AreEqual(2, squad.InPlay);
            Assert.IsFalse(first.LoseShip(false), "An out pilot loses nothing more.");
            Assert.AreEqual(2, first.LivesLost);

            squad[1].LoseShip(false);
            squad[1].LoseShip(false);
            squad[2].LoseShip(false);
            Assert.IsFalse(squad.AllOut);
            squad[2].LoseShip(false);
            Assert.IsTrue(squad.AllOut, "Every pilot is out: the mission is lost.");
        }

        [Test]
        public void AStrikePilotHasOneShip()
        {
            LocalSquad squad = Squad(2, 1);
            Assert.IsFalse(squad[1].LoseShip(true));
            Assert.IsTrue(squad[1].Out);
            Assert.IsFalse(squad.AllOut);
        }

        [Test]
        public void ALifePickupGivesThePilotAShip()
        {
            LocalSquad squad = Squad(2, 3);
            squad[0].AddLife(4);
            squad[0].AddLife(4);
            Assert.AreEqual(4, squad[0].Lives, "Up to the limit.");
            Assert.AreEqual(3, squad[1].Lives, "Only the pilot who took it.");
        }

        [Test]
        public void EveryPilotScoresAndTheSquadAddsUp()
        {
            LocalSquad squad = Squad(3, 3);
            squad[0].Score.AddKill(100);
            squad[1].Score.AddKill(50);
            squad[1].Score.AddKill(50);
            squad[1].Score.ShotFired(4);
            squad[1].Score.ShotLanded();
            squad[2].Score.AddCrystal();
            Assert.AreEqual(squad[0].Score.Score + squad[1].Score.Score + squad[2].Score.Score, squad.TeamScore);
            Assert.AreEqual(3, squad.Kills);
            Assert.AreEqual(1, squad.Crystals);
            Assert.AreEqual(0.25f, squad.Accuracy, 1e-5f);
            Assert.AreEqual(2, squad.MaxCombo);
        }

        [Test]
        public void TheBonusesGoToThePilotsStillFlying()
        {
            LocalSquad squad = Squad(3, 2);
            squad[2].LoseShip(false);
            squad[2].LoseShip(false);
            squad.WaveCleared(250);
            Assert.AreEqual(250, squad[0].Score.Score);
            Assert.AreEqual(250, squad[1].Score.Score);
            Assert.AreEqual(0, squad[2].Score.Score, "An out pilot clears no waves.");
            squad[1].LoseShip(false);
            int bonus = squad.Victory(1000);
            Assert.AreEqual(2000 + 1000, bonus, "Two ships left for the first pilot, one for the second.");
            Assert.AreEqual(2250, squad[0].Score.Score);
            Assert.AreEqual(1250, squad[1].Score.Score);
            Assert.AreEqual(0, squad[2].Score.Score);
        }

        [Test]
        public void TheStandingsRankByScoreAndShareEqualPlaces()
        {
            LocalSquad squad = Squad(4, 3);
            squad[0].Score.Add(100);
            squad[1].Score.Add(300);
            squad[2].Score.Add(100);
            squad[3].Score.Add(50);
            List<SquadPilot> ranked = squad.Ranked(out int[] places);
            Assert.AreEqual(new[] { 1, 0, 2, 3 }, ranked.Select(pilot => pilot.Seat).ToArray(), "Best first, the lower seat first on a tie.");
            Assert.AreEqual(new[] { 1, 2, 2, 4 }, places);
            string standings = squad.Describe(pilot => pilot.Score.Score.ToString(), pilot => pilot.Out ? "out" : null);
            string[] lines = standings.Split('\n');
            Assert.AreEqual(4, lines.Length);
            StringAssert.StartsWith("1st", lines[0]);
            StringAssert.Contains("P2", lines[0]);
            StringAssert.Contains(ColorUtility.ToHtmlStringRGB(CoopRules.SeatColor(1)), lines[0], "The name is in the seat's colour.");
            StringAssert.StartsWith("2nd", lines[2]);
            StringAssert.Contains("50", lines[3]);
        }

        // ------------------------------------------------------------------ the world

        [Test]
        public void AWingmansHitsCountForItsSeatAndThePlayersForThePlayer()
        {
            var player = objects.Component<AsteroidsPlayer>("Player");
            var wingman = objects.Component<AsteroidsPlayer>("Wingman");
            var remote = objects.Component<AsteroidsPlayer>("Remote");
            player.Assign(0, PlayerControl.Local, "P1");
            wingman.Assign(2, PlayerControl.Local, "P3");
            wingman.IsWingman = true;
            remote.Assign(3, PlayerControl.Remote, "Far");
            Assert.IsNull(player.HitSeat);
            Assert.AreEqual(2, wingman.HitSeat);
            Assert.AreEqual(3, remote.HitSeat, "As before for the stand-ins of an online room.");
        }

        [Test]
        public void TheWorldGoesForTheNearestShipWingmenIncluded()
        {
            var field = objects.Component<SpaceField>("Field");
            var player = objects.Component<AsteroidsPlayer>("Player");
            var wingman = objects.Component<AsteroidsPlayer>("Wingman");
            wingman.IsWingman = true;
            player.Position = Vector2.zero;
            wingman.Position = new Vector2(6f, 0f);
            field.Player = player;
            field.AddWingman(wingman);
            field.AddWingman(wingman);
            field.AddWingman(player);
            Assert.AreEqual(2, field.LocalShipCount, "The player and one wingman, each once.");
            Assert.AreSame(player, field.LocalShip(0));
            Assert.AreSame(wingman, field.LocalShip(1));
            Assert.AreSame(wingman, field.NearestShip(new Vector2(5f, 1f)));
            Assert.AreSame(player, field.NearestShip(new Vector2(1f, 1f)));
            wingman.gameObject.SetActive(false);
            Assert.AreSame(player, field.NearestShip(new Vector2(5f, 1f)), "A ship that is down is nobody's target.");
            field.RemoveWingman(wingman);
            Assert.AreEqual(1, field.LocalShipCount);
        }
    }
}
