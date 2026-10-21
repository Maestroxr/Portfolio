using System.Collections.Generic;
using Gamebox;
using NUnit.Framework;
using UnityEngine;

namespace Portfolio.Asteroids.Tests
{
    /// <summary>
    /// Several pilots of an online room at one device (a party: the device's player and their companions): which seats are
    /// flown here and under which seat a ship's deeds go out, the scores of the party's squad, and what the field tells
    /// its link about the pilot behind a nova, a new ship or a pickup.
    /// </summary>
    public class PartyTest
    {
        private readonly TemporaryObjects objects = new TemporaryObjects();
        private SpaceField field;


        /// <summary>A link that writes down which pilot the field names.</summary>
        private sealed class RecordingLink : IFieldLink
        {
            public bool Replica;
            public readonly List<int?> NovaSeats = new List<int?>();
            public readonly List<int?> RoomSeats = new List<int?>();
            public readonly List<AsteroidsPlayer> Touched = new List<AsteroidsPlayer>();
            public readonly List<int?> HitSeats = new List<int?>();

            public bool Simulates => !Replica;
            public void BodyAdded(SpaceBody body) { }
            public void BodyRemoved(SpaceBody body) { }
            public void BlastSetOff(Blast blast) { }
            public void PuppetHit(Shootable puppet, DamageInfo hit) => HitSeats.Add(hit.Seat);
            public void PuppetRammed(Shootable puppet, AsteroidsPlayer ship, Vector2 direction, bool dashing) { }
            public void PuppetShotAbsorbed(Shot puppet) { }
            public void RewardTouched(Reward reward, AsteroidsPlayer ship) => Touched.Add(ship);
            public void ShotFired(Shot shot, byte kind, int level) { }
            public void NovaFired(Vector2 center, float damage, float bossDamage, int? seat) => NovaSeats.Add(seat);
            public void RoomWanted(Vector2 center, int? seat) => RoomSeats.Add(seat);
            public void ScrollReported(float distance, float speed) { }
        }


        [SetUp]
        public void SetUp()
        {
            Playground playground = objects.Component<Playground>("Playground");
            // The playfield of a shared mission: 16:9 at a half height of 10.
            playground.Fix(new Vector2(10f * 16f / 9f, 10f));
            field = objects.Component<SpaceField>("Field");
            field.playground = playground;
        }


        [TearDown]
        public void TearDown()
        {
            if (field != null)
            {
                field.Link = null;
            }
            objects.Dispose();
        }


        // ------------------------------------------------------------------ the seats of the device

        [Test]
        public void TheDevicesOwnSeatComesFirstAndTheCompanionsFollowBySeat()
        {
            CollectionAssert.AreEqual(new byte[] { 2, 0, 3 }, DeviceSeats.Order(2, new byte[] { 3, 2, 0, 3 }), "The device, then the others by seat, each once.");
            CollectionAssert.AreEqual(new byte[] { 1 }, DeviceSeats.Order(1, new byte[] { 1 }), "Alone at the device.");
            CollectionAssert.AreEqual(new byte[] { 0 }, DeviceSeats.Order(0, null));
            CollectionAssert.AreEqual(new byte[] { 0, 1 }, DeviceSeats.Order(DeviceSeats.NoSeat, new byte[] { 1, 0 }), "Without a seat of its own (a shared screen) the device flies the seats it drives, by seat.");
        }


        [Test]
        public void AShipsDeedsGoOutUnderItsPilotsSeat()
        {
            var seats = new DeviceSeats(DeviceSeats.Order(2, new byte[] { 0, 2 }));
            Assert.AreEqual((byte)2, seats.Device);
            Assert.IsTrue(seats.IsParty);
            Assert.IsTrue(seats.Contains(0));
            Assert.IsTrue(seats.Contains(2));
            Assert.IsFalse(seats.Contains(1), "Seat 1 is a pilot of another device.");
            Assert.IsFalse(seats.Contains(DeviceSeats.NoSeat));
            Assert.IsFalse(seats.Contains(-1));
            Assert.AreEqual((byte)2, seats.Of(null), "The device's own ship names no seat: its deeds are the device's.");
            Assert.AreEqual((byte)0, seats.Of(0), "A wingman's are its pilot's.");
            Assert.AreEqual((byte)2, seats.Of(1), "Never a stranger's seat: what is done here goes out as the device's.");
        }


        [Test]
        public void APilotAloneIsNoParty()
        {
            var alone = new DeviceSeats(new byte[] { 1 });
            Assert.IsFalse(alone.IsParty);
            Assert.AreEqual((byte)1, alone.Of(null));
            var none = new DeviceSeats(null);
            Assert.AreEqual(DeviceSeats.NoSeat, none.Device, "Not in a room.");
            Assert.AreEqual(DeviceSeats.NoSeat, none.Of(null));
            Assert.IsFalse(none.Contains(0));
            CollectionAssert.AreEqual(new byte[] { 3, 1 }, new DeviceSeats(new byte[] { 3, 1, 3, DeviceSeats.NoSeat }).All, "A seat once, no seat left out.");
        }


        // ------------------------------------------------------------------ the squad of a party

        [Test]
        public void TheDevicesPilotKeepsTheMissionsScoreAndTheCompanionsTheirOwn()
        {
            var mission = new ScoreKeeper();
            var seats = new List<LocalSeat> { new LocalSeat(2) { Name = "Host" }, new LocalSeat(0) { Name = "Guest" } };
            var squad = new LocalSquad(seats, 3, mission);
            Assert.AreSame(mission, squad[2].Score, "What the shared mission says about the device's player stays as it is alone.");
            Assert.AreNotSame(mission, squad[0].Score);
            squad[2].Score.AddKill(100);
            squad[0].Score.Add(40);
            Assert.AreEqual(mission.Score, squad[2].Score.Score);
            Assert.AreEqual(mission.Score + 40, squad.TeamScore);
            Assert.IsNull(squad[1], "A seat of another device is no pilot of the squad.");
            Assert.AreEqual(3, squad[0].Lives, "Every pilot has the ships of the room.");

            var local = new LocalSquad(seats, 3);
            Assert.AreNotSame(local[2].Score, local[0].Score, "Local co-op keeps a score of their own for every pilot.");
        }


        [Test]
        public void APartyIsOutWhenEveryPilotAtTheDeviceIs()
        {
            var seats = new List<LocalSeat> { new LocalSeat(1), new LocalSeat(3) };
            var squad = new LocalSquad(seats, 1);
            Assert.IsFalse(squad[3].LoseShip(false), "One ship: the companion is out at once.");
            Assert.IsFalse(squad.AllOut, "The device's player flies on.");
            squad[1].LoseShip(false);
            Assert.IsTrue(squad.AllOut, "This device watches the others now.");
        }


        // ------------------------------------------------------------------ what the field tells the link

        [Test]
        public void ANovaGoesOutWithThePilotWhoSetItOff()
        {
            var link = new RecordingLink();
            field.Link = link;
            field.Nova(Vector2.zero, 1f, 1f);
            field.Nova(Vector2.zero, 1f, 1f, 3);
            field.Nova(Vector2.zero, 1f, 1f, 1, false);
            CollectionAssert.AreEqual(new int?[] { null, 3 }, link.NovaSeats,
                "The device's own nova names nobody, a wingman's names its pilot, and one of another device is not sent back.");
        }


        [Test]
        public void ANewShipOnAReplicaAsksForRoomForItsPilot()
        {
            var link = new RecordingLink { Replica = true };
            field.Link = link;
            field.MakeRoom(Vector2.zero);
            field.MakeRoom(Vector2.one, 2);
            CollectionAssert.AreEqual(new int?[] { null, 2 }, link.RoomSeats);
        }


        [Test]
        public void AWingmansHitOnAPuppetGoesToTheSimulatorAndAStrangersDoesNot()
        {
            var wingman = objects.Component<AsteroidsPlayer>("Wingman");
            wingman.Assign(2, PlayerControl.Local, "P3");
            wingman.IsWingman = true;
            field.Player = objects.Component<AsteroidsPlayer>("Player");
            field.AddWingman(wingman);
            var link = new RecordingLink { Replica = true };
            field.Link = link;
            var rock = objects.Component<Asteroid>("Rock");
            rock.IsPuppet = true;
            field.Add(rock);
            rock.Health = 10f;
            Assert.IsTrue(field.IsWingmanSeat(2));
            Assert.IsFalse(field.IsWingmanSeat(3));

            rock.TakeHit(new DamageInfo(1f, Vector2.up, Vector2.zero, DamageSource.PlayerShot, true));
            rock.TakeHit(new DamageInfo(1f, Vector2.up, Vector2.zero, DamageSource.PlayerShot, true) { Seat = 2 });
            rock.TakeHit(new DamageInfo(1f, Vector2.up, Vector2.zero, DamageSource.PlayerShot, true) { Seat = 3 });
            CollectionAssert.AreEqual(new int?[] { null, 2 }, link.HitSeats,
                "The player's hit and the wingman's go to the simulator; seat 3 flies elsewhere and reports its own.");
        }


        [Test]
        public void APickupTheServerGaveAWingmanIsTheWingmans()
        {
            var player = objects.Component<AsteroidsPlayer>("Player");
            var wingman = objects.Component<AsteroidsPlayer>("Wingman");
            wingman.Assign(2, PlayerControl.Local, "P3");
            wingman.IsWingman = true;
            field.Player = player;
            field.AddWingman(wingman);
            field.Link = new RecordingLink { Replica = true };
            int playerLives = 0;
            int wingmanLives = 0;
            player.LifeAwarded += () => playerLives++;
            wingman.LifeAwarded += () => wingmanLives++;

            var life = objects.Component<LifeReward>("Life");
            field.Add(life);
            field.GrantReward(life, wingman);
            Assert.AreEqual(1, wingmanLives, "The pilot whose ship the server named gets it.");
            Assert.AreEqual(0, playerLives);
            Assert.IsFalse(life.InPlay, "Collected.");

            var other = objects.Component<LifeReward>("Other");
            field.Add(other);
            field.GrantReward(other);
            Assert.AreEqual(1, playerLives, "Without a ship named it is the player's, as before.");
        }
    }
}
