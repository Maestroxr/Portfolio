using System.Collections.Generic;
using Gamebox;
using NUnit.Framework;
using UnityEngine;

namespace Portfolio.Asteroids.Tests
{
    /// <summary>
    /// The shared screen and the remote controls: the seats a device without a seat of its own flies, what a pose carries
    /// for a seat that changes hands, the status the screen publishes for a remote's seat and the rows of the panel, a
    /// strike loadout as text, what the pad sends for an autopilot, and a squad taking pilots in and letting them go.
    /// </summary>
    public class RemoteSeatsTest
    {
        /// <summary>Commands as an autopilot would give them.</summary>
        private sealed class Commands : IShipInput
        {
            public float Turn { get; set; }
            public float Thrust { get; set; }
            public bool Brake { get; set; }
            public bool Fire { get; set; }
            public bool DashPressed { get; set; }
            public bool BombPressed { get; set; }
            public Vector2 Move { get; set; }
            public bool CyclePressed { get; set; }

            public void Read(AsteroidsPlayer ship, float deltaTime)
            {
            }
        }


        /// <summary>The value of <paramref name="key"/> in a status of the <c>key=value;key=value</c> form, or null.</summary>
        private static string Value(string status, string key)
        {
            foreach (string entry in status.Split(';'))
            {
                int separator = entry.IndexOf('=');
                if (separator > 0 && entry.Substring(0, separator) == key)
                {
                    return entry.Substring(separator + 1);
                }
            }
            return null;
        }


        [Test]
        public void ASeatlessDeviceFliesTheSeatsItDrives()
        {
            CollectionAssert.AreEqual(new byte[] { 1, 3 }, DeviceSeats.Order(DeviceSeats.NoSeat, new byte[] { 3, 1, 3 }), "The shared screen: the seats of its remotes, by seat, each once.");
            CollectionAssert.IsEmpty(DeviceSeats.Order(DeviceSeats.NoSeat, null), "A remote control's device flies nothing.");
            var seats = new DeviceSeats(DeviceSeats.Order(DeviceSeats.NoSeat, new byte[] { 2, 0 }));
            Assert.That(seats.Device, Is.EqualTo(0), "The first seat flown stands for the device.");
            Assert.That(seats.IsParty, Is.True);
            Assert.That(seats.Of(2), Is.EqualTo(2));
            Assert.That(seats.Of(1), Is.EqualTo(0), "A seat not flown here reports under the first one.");
        }


        [Test]
        public void ThePoseCarriesWhatASeatNeedsToChangeHands()
        {
            var pose = new ShipPose
            {
                Alive = true, Thrusting = true, Beam = true, BeamKind = 2, Hull = 2, Health = 0.5f, Shield = 0.25f,
                Lives = 5, Bombs = 3, Weapon = (int)WeaponType.Rail, WeaponLevel = 4
            };
            ShipPose back = ShipPose.Unpack(pose.PackState(), pose.PackValue());
            Assert.That(back.Alive, Is.True);
            Assert.That(back.Thrusting, Is.True);
            Assert.That(back.Dashing, Is.False);
            Assert.That(back.Beam, Is.True);
            Assert.That(back.BeamKind, Is.EqualTo(2));
            Assert.That(back.Hull, Is.EqualTo(2));
            Assert.That(back.Health, Is.EqualTo(0.5f).Within(0.01f));
            Assert.That(back.Shield, Is.EqualTo(0.25f).Within(0.01f));
            Assert.That(back.Lives, Is.EqualTo(5));
            Assert.That(back.Bombs, Is.EqualTo(3));
            Assert.That(back.Weapon, Is.EqualTo((int)WeaponType.Rail));
            Assert.That(back.WeaponLevel, Is.EqualTo(4));

            var old = ShipPose.Unpack(1u | 2u | 64u, 0);
            Assert.That(old.Alive && old.Thrusting && old.Beam, Is.True, "The old bits of the state are where they were.");
            Assert.That(old.Lives, Is.EqualTo(0));
            Assert.That(old.Weapon, Is.EqualTo((int)WeaponType.Blaster));

            var big = ShipPose.Unpack(new ShipPose { Lives = 20, Bombs = 9, Weapon = 40, WeaponLevel = 12 }.PackState(), 0);
            Assert.That(big.Lives, Is.EqualTo(15), "Clamped to its bits.");
            Assert.That(big.Bombs, Is.EqualTo(7));
            Assert.That(big.Weapon, Is.EqualTo(15));
            Assert.That(big.WeaponLevel, Is.EqualTo(7));
        }


        [Test]
        public void TheFieldStatusHasTheRowsOfThePanel()
        {
            string status = RemoteSeatStatus.WriteField(3, 12345, WeaponType.Laser, 2, 1, 0.5f, 0.75f, true);
            foreach (string key in RemoteSeatStatus.PanelKeys(MissionMode.Field))
            {
                Assert.That(Value(status, key), Is.Not.Null, key);
                Assert.That(RemoteSeatStatus.Labels.ContainsKey(key), Is.True, "A label for " + key);
            }
            Assert.That(Value(status, RemoteSeatStatus.Ships), Is.EqualTo("3"));
            Assert.That(Value(status, RemoteSeatStatus.Score), Is.EqualTo("12,345"));
            Assert.That(Value(status, RemoteSeatStatus.Weapon), Is.EqualTo("LASER"), "The weapon's short title, a word of the table.");
            Assert.That(Value(status, RemoteSeatStatus.Level), Is.EqualTo("2"));
            Assert.That(Value(status, RemoteSeatStatus.Novas), Is.EqualTo("1"));
            Assert.That(Value(status, RemoteSeatStatus.Hull), Is.EqualTo("50%"));
            Assert.That(Value(status, RemoteSeatStatus.Shield), Is.EqualTo("75%"));
            Assert.That(RemoteSeatStatus.IsFlying(status), Is.True);
            Assert.That(RemoteSeatStatus.IsFlying(RemoteSeatStatus.WriteField(0, 10, WeaponType.Blaster, 1, 0, 0f, 0f, false)), Is.False);
        }


        [Test]
        public void TheStrikeStatusCarriesTheLoadout()
        {
            StrikeLoadout loadout = StrikeLoadout.NewPilot();
            loadout.Energy = 61.4f;
            loadout.Megabombs = 2;
            loadout.PhaseShields = 1;
            loadout.ShieldPoints = 33.25f;
            loadout.SetCount(StrikeItem.Deathray, 1);
            loadout.Special = StrikeItem.Deathray;
            string status = RemoteSeatStatus.WriteStrike(400, loadout.Energy, loadout.PhaseShields, loadout.Megabombs, "DEATHRAY", false, StrikeLoadoutText.Write(loadout));
            foreach (string key in RemoteSeatStatus.PanelKeys(MissionMode.Strike))
            {
                Assert.That(Value(status, key), Is.Not.Null, key);
                Assert.That(RemoteSeatStatus.Labels.ContainsKey(key), Is.True, "A label for " + key);
            }
            Assert.That(Value(status, RemoteSeatStatus.Money), Is.EqualTo("400"));
            Assert.That(Value(status, RemoteSeatStatus.Energy), Is.EqualTo("61"));
            Assert.That(Value(status, RemoteSeatStatus.Shields), Is.EqualTo("1"));
            Assert.That(Value(status, RemoteSeatStatus.Megabombs), Is.EqualTo("2"));
            Assert.That(Value(status, RemoteSeatStatus.Special), Is.EqualTo("DEATHRAY"));
            Assert.That(RemoteSeatStatus.IsFlying(status), Is.False);

            StrikeLoadout back = StrikeLoadoutText.Parse(Value(status, RemoteSeatStatus.Loadout));
            Assert.That(back, Is.Not.Null, "The loadout travels inside the status.");
            Assert.That(back.Money, Is.EqualTo(loadout.Money));
            Assert.That(back.Energy, Is.EqualTo(61.4f).Within(0.001f));
            Assert.That(back.PhaseShields, Is.EqualTo(1));
            Assert.That(back.ShieldPoints, Is.EqualTo(33.25f).Within(0.001f));
            Assert.That(back.Megabombs, Is.EqualTo(2));
            Assert.That(back.Special, Is.EqualTo(StrikeItem.Deathray));
            Assert.That(back.Count(StrikeItem.Deathray), Is.EqualTo(1));
            Assert.That(back.Count(StrikeItem.MachineGun), Is.EqualTo(1));
            Assert.That(back.Count(StrikeItem.PlasmaCannon), Is.EqualTo(0));
            Assert.That(back.Difficulty, Is.EqualTo(loadout.Difficulty));
            Assert.That(StrikeLoadoutText.Parse(""), Is.Null);
            Assert.That(StrikeLoadoutText.Parse("junk"), Is.Null);
            Assert.That(StrikeLoadoutText.Write(null), Is.EqualTo(""));
        }


        [Test]
        public void ThePadPacketSaysWhatTheAutopilotWants()
        {
            var commands = new Commands { Turn = 1f, Thrust = 0.5f, Fire = true, DashPressed = true };
            RemotePadPacket.From(commands, false, out Vector2 stick, out uint held, out uint pressed);
            Assert.That(stick.x, Is.LessThan(0f), "A positive turn is to the left: the stick goes left.");
            Assert.That(stick.y, Is.GreaterThan(0f), "Thrust pushes the stick up.");
            Assert.That(stick.magnitude, Is.LessThanOrEqualTo(1f + 0.001f));
            Assert.That(held, Is.EqualTo(RemotePadPacket.Bit(LocalCoopRules.Fire)));
            Assert.That(pressed, Is.EqualTo(RemotePadPacket.Bit(LocalCoopRules.Dash)));
            Assert.That(RemotePadPacket.Bit(LocalCoopRules.Fire), Is.EqualTo(1u << LocalCoopRules.Scheme.IndexOf(LocalCoopRules.Fire)), "A bit per action, in the order of the scheme.");

            RemotePadPacket.From(new Commands { Brake = true, BombPressed = true }, false, out stick, out held, out pressed);
            Assert.That(stick, Is.EqualTo(new Vector2(0f, -1f)), "The brake pulls the stick down.");
            Assert.That(held, Is.EqualTo(0u));
            Assert.That(pressed, Is.EqualTo(RemotePadPacket.Bit(LocalCoopRules.Bomb)));

            RemotePadPacket.From(new Commands { Move = new Vector2(0.3f, -0.6f), Fire = true, CyclePressed = true }, true, out stick, out held, out pressed);
            Assert.That(stick.x, Is.EqualTo(0.3f).Within(0.001f), "A strike ship is moved by the stick itself.");
            Assert.That(stick.y, Is.EqualTo(-0.6f).Within(0.001f));
            Assert.That(pressed, Is.EqualTo(RemotePadPacket.Bit(LocalCoopRules.Cycle)));

            RemotePadPacket.From(null, false, out stick, out held, out pressed);
            Assert.That(stick, Is.EqualTo(Vector2.zero));
            Assert.That(held | pressed, Is.EqualTo(0u));
        }


        [Test]
        public void ASquadTakesPilotsInAndLetsThemGoMidMission()
        {
            var squad = new LocalSquad(new List<LocalSeat>(), 3);
            Assert.That(squad.AllOut, Is.False, "An empty squad is not out.");
            SquadPilot pilot = squad.Add(new LocalSeat(2) { Name = "Pat" }, 2, 150);
            Assert.That(squad.Count, Is.EqualTo(1));
            Assert.That(squad[2], Is.SameAs(pilot));
            Assert.That(pilot.Lives, Is.EqualTo(2));
            Assert.That(pilot.Score.Score, Is.EqualTo(150), "The score the seat had so far.");
            Assert.That(pilot.Out, Is.False);
            Assert.That(squad.TeamScore, Is.EqualTo(150));
            Assert.That(squad.Remove(2), Is.True);
            Assert.That(squad.Count, Is.EqualTo(0));
            Assert.That(squad.Remove(2), Is.False, "Gone already.");
        }


        [Test]
        public void TheFlownKitComesBackToThePilot()
        {
            StrikeLoadout pilot = StrikeLoadout.NewPilot();
            pilot.Difficulty = StrikeDifficulty.Elite;
            pilot.SetCount(StrikeItem.PlasmaCannon, 1);
            StrikeLoadout working = pilot.Clone();
            working.Difficulty = StrikeDifficulty.Rookie;
            // What the screen flew the seat with: the plasma cannon was destroyed, a phase shield and megabombs were picked up.
            StrikeLoadout flown = working.Clone();
            flown.SetCount(StrikeItem.PlasmaCannon, 0);
            flown.Megabombs = 2;
            flown.PhaseShields = 1;
            flown.ShieldPoints = 40f;
            flown.Energy = 63f;
            string status = RemoteSeatStatus.WriteStrike(1500, flown.Energy, flown.PhaseShields, flown.Megabombs, null, true, StrikeLoadoutText.Write(flown));
            StrikeLoadout back = StrikeLoadoutText.Parse(Value(status, RemoteSeatStatus.Loadout));
            int money = pilot.Money;
            RemoteSeatStatus.BankFlownSeat(pilot, working, back, 1500);
            Assert.That(pilot.Count(StrikeItem.PlasmaCannon), Is.EqualTo(0), "The weapon the seat lost is gone from the saved pilot.");
            Assert.That(pilot.Count(StrikeItem.MachineGun), Is.EqualTo(1));
            Assert.That(pilot.Megabombs, Is.EqualTo(2));
            Assert.That(pilot.PhaseShields, Is.EqualTo(1));
            Assert.That(pilot.ShieldPoints, Is.EqualTo(40f).Within(0.001f));
            Assert.That(pilot.Energy, Is.EqualTo(63f).Within(0.001f));
            Assert.That(pilot.Money, Is.EqualTo(money + 1500), "The money the seat earned, on top of the wallet.");
            Assert.That(pilot.Difficulty, Is.EqualTo(StrikeDifficulty.Elite), "The room's difficulty was for that room only.");

            // Without a word from the screen only the money is banked, as for a party's wingman.
            StrikeLoadout other = StrikeLoadout.NewPilot();
            other.SetCount(StrikeItem.PlasmaCannon, 1);
            int before = other.Money;
            RemoteSeatStatus.BankFlownSeat(other, other.Clone(), null, 300);
            Assert.That(other.Count(StrikeItem.PlasmaCannon), Is.EqualTo(1));
            Assert.That(other.Money, Is.EqualTo(before + 300));
        }
    }
}
