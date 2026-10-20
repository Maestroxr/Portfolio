using NUnit.Framework;

namespace Portfolio.DeathRace.Tests
{
    /// <summary>
    /// What the devices of an online race and the server agree on (<see cref="OnlineRaceRules"/>, compiled into the server
    /// module too): the payloads of the room's actions, the bits of a pose, the keys of the claims and the scores.
    /// </summary>
    public class OnlineRaceRulesTest
    {
        [Test]
        public void AMineReadsBackWhereItWasDropped()
        {
            var mine = new OnlineRaceRules.MineDrop { Id = 7, X = -123.456f, Z = 88.01f, Yaw = 370f };
            string payload = OnlineRaceRules.EncodeMine(mine);
            Assert.IsTrue(OnlineRaceRules.TryDecodeMine(payload, out OnlineRaceRules.MineDrop read), payload);
            Assert.AreEqual(7, read.Id);
            Assert.AreEqual(-123.46f, read.X, 0.006f);
            Assert.AreEqual(88.01f, read.Z, 0.006f);
            Assert.AreEqual(10f, read.Yaw, 0.06f, "the yaw comes back within a turn");
            Assert.LessOrEqual(payload.Length, OnlineRaceRules.MaxPayloadLength);
        }

        [Test]
        public void ARocketReadsBackWithAUnitDirection()
        {
            var shot = new OnlineRaceRules.RocketShot { X = 10.5f, Y = 1.2f, Z = -40.25f, DirectionX = 0.6f, DirectionZ = -0.8f, Speed = 72.5f };
            string payload = OnlineRaceRules.EncodeRocket(shot);
            Assert.IsTrue(OnlineRaceRules.TryDecodeRocket(payload, out OnlineRaceRules.RocketShot read), payload);
            Assert.AreEqual(10.5f, read.X, 0.006f);
            Assert.AreEqual(1.2f, read.Y, 0.006f);
            Assert.AreEqual(-40.25f, read.Z, 0.006f);
            Assert.AreEqual(1f, read.DirectionX * read.DirectionX + read.DirectionZ * read.DirectionZ, 1e-4f);
            Assert.AreEqual(0.6f, read.DirectionX, 0.002f);
            Assert.AreEqual(72.5f, read.Speed, 0.006f);
            Assert.LessOrEqual(payload.Length, OnlineRaceRules.MaxPayloadLength);
        }

        [Test]
        public void AWreckNamesItsKillerOrNobody()
        {
            Assert.IsTrue(OnlineRaceRules.TryDecodeWreck(OnlineRaceRules.EncodeWreck(2), out int killer));
            Assert.AreEqual(2, killer);
            Assert.IsTrue(OnlineRaceRules.TryDecodeWreck(OnlineRaceRules.EncodeWreck(-1), out killer));
            Assert.AreEqual(-1, killer);
            Assert.AreEqual("-", OnlineRaceRules.EncodeWreck(-1));
        }

        [Test]
        public void TheSpecialWeaponsReadBack()
        {
            var slick = new OnlineRaceRules.MineDrop { Id = 3, X = 12.25f, Z = -7.5f, Yaw = 90f };
            Assert.IsTrue(OnlineRaceRules.TryDecodeOil(OnlineRaceRules.EncodeOil(slick), out OnlineRaceRules.MineDrop oil));
            Assert.AreEqual(3, oil.Id);
            Assert.AreEqual(-7.5f, oil.Z, 0.006f);

            string empPayload = OnlineRaceRules.EncodeEmp(new OnlineRaceRules.EmpPulse { X = -300.5f, Z = 41.04f });
            Assert.IsTrue(OnlineRaceRules.TryDecodeEmp(empPayload, out OnlineRaceRules.EmpPulse emp), empPayload);
            Assert.AreEqual(-300.5f, emp.X, 0.006f);
            Assert.AreEqual(41.04f, emp.Z, 0.006f);

            var lob = new OnlineRaceRules.MortarLob { X = 5f, Y = 1.4f, Z = 10f, TargetX = 25f, TargetZ = 40f };
            string mortarPayload = OnlineRaceRules.EncodeMortar(lob);
            Assert.IsTrue(OnlineRaceRules.TryDecodeMortar(mortarPayload, out OnlineRaceRules.MortarLob mortar), mortarPayload);
            Assert.AreEqual(1.4f, mortar.Y, 0.006f);
            Assert.AreEqual(25f, mortar.TargetX, 0.006f);
            Assert.AreEqual(40f, mortar.TargetZ, 0.006f);
            Assert.LessOrEqual(mortarPayload.Length, OnlineRaceRules.MaxPayloadLength);

            Assert.IsFalse(OnlineRaceRules.TryDecodeEmp("1,2,3", out _), "a pulse is two numbers");
            Assert.IsFalse(OnlineRaceRules.TryDecodeMortar(OnlineRaceRules.EncodeMortar(new OnlineRaceRules.MortarLob { TargetX = 500f }), out _),
                "no shell lands that far away");
            Assert.AreNotEqual(OnlineRaceRules.OilAction, OnlineRaceRules.MineAction);
            Assert.AreNotEqual(OnlineRaceRules.EmpAction, OnlineRaceRules.WreckAction);
            Assert.AreNotEqual(OnlineRaceRules.MortarAction, OnlineRaceRules.RocketAction);
        }

        [Test]
        public void EveryCarOfTheGameCanBeARoomsCar()
        {
            Assert.AreEqual(CarSpecs.Count, OnlineRaceRules.CarCount, "the server holds a room's car to this many");
            Assert.GreaterOrEqual(OnlineRaceRules.MaxTracks, TrackLayouts.Count);
        }

        [Test]
        public void PayloadsThatDoNotReadAreRefused()
        {
            Assert.IsFalse(OnlineRaceRules.TryDecodeMine("", out _));
            Assert.IsFalse(OnlineRaceRules.TryDecodeMine("1,2,3", out _));
            Assert.IsFalse(OnlineRaceRules.TryDecodeMine("a,b,c,d", out _));
            Assert.IsFalse(OnlineRaceRules.TryDecodeMine(new string('1', OnlineRaceRules.MaxPayloadLength + 1), out _));
            Assert.IsFalse(OnlineRaceRules.TryDecodeRocket("1,2,3,0,0,5", out _), "a rocket needs a direction");
            Assert.IsFalse(OnlineRaceRules.TryDecodeRocket(null, out _));
            Assert.IsFalse(OnlineRaceRules.TryDecodeWreck("x", out _));
            Assert.IsFalse(OnlineRaceRules.TryDecodeWreck("300", out _));
        }

        [Test]
        public void ThePoseBitsSayWhatACarDoes()
        {
            uint state = OnlineRaceRules.PackState(firing: true, nitro: false, braking: true, wrecked: false, finished: false);
            Assert.IsTrue(OnlineRaceRules.Has(state, OnlineRaceRules.FiringBit));
            Assert.IsFalse(OnlineRaceRules.Has(state, OnlineRaceRules.NitroBit));
            Assert.IsTrue(OnlineRaceRules.Has(state, OnlineRaceRules.BrakingBit));
            Assert.IsFalse(OnlineRaceRules.Has(state, OnlineRaceRules.WreckedBit));
            Assert.AreEqual(31u, OnlineRaceRules.PackState(true, true, true, true, true), "five distinct bits");
            Assert.AreEqual(0u, OnlineRaceRules.PackState(false, false, false, false, false));
        }

        [Test]
        public void ARemoteCarFiresWhatItsPoseSaysButNotOnceWrecked()
        {
            CarInput firing = RemoteDriver.Input(OnlineRaceRules.PackState(true, true, false, false, false));
            Assert.IsTrue(firing.Fire);
            Assert.IsTrue(firing.Nitro);
            Assert.IsFalse(firing.DropMine, "mines come from the room's actions");
            Assert.IsFalse(firing.FireRocket, "rockets come from the room's actions");
            Assert.IsFalse(firing.Special, "so do the special weapons");
            CarInput wrecked = RemoteDriver.Input(OnlineRaceRules.PackState(true, true, false, true, false));
            Assert.IsFalse(wrecked.Fire);
            Assert.IsFalse(wrecked.Nitro);
            Assert.Less(RemoteDriver.Input(OnlineRaceRules.BrakingBit).Throttle, 0f, "the brake lights");
        }

        [Test]
        public void HealthTravelsPerMille()
        {
            Assert.AreEqual(1000, OnlineRaceRules.PackHealth(1f));
            Assert.AreEqual(0, OnlineRaceRules.PackHealth(-0.2f));
            Assert.AreEqual(1000, OnlineRaceRules.PackHealth(3f));
            Assert.AreEqual(0.427f, OnlineRaceRules.UnpackHealth(OnlineRaceRules.PackHealth(0.4271f)), 1e-6f);
            Assert.AreEqual(1f, OnlineRaceRules.UnpackHealth(5000));
        }

        [Test]
        public void EveryClaimHasAKeyOfItsOwn()
        {
            ulong pickup = OnlineRaceRules.ClaimKey(5, OnlineRaceRules.PickupClaim, OnlineRaceRules.PickupItem(12, 0));
            ulong again = OnlineRaceRules.ClaimKey(5, OnlineRaceRules.PickupClaim, OnlineRaceRules.PickupItem(12, 1));
            ulong mine = OnlineRaceRules.ClaimKey(5, OnlineRaceRules.MineClaim, OnlineRaceRules.PickupItem(12, 0));
            ulong otherRoom = OnlineRaceRules.ClaimKey(6, OnlineRaceRules.PickupClaim, OnlineRaceRules.PickupItem(12, 0));
            Assert.AreNotEqual(pickup, again, "a pickup that came back is claimed afresh");
            Assert.AreNotEqual(pickup, mine, "a mine and a pickup of the same number");
            Assert.AreNotEqual(pickup, otherRoom);
            Assert.AreEqual(pickup, OnlineRaceRules.ClaimKey(5, OnlineRaceRules.PickupClaim, OnlineRaceRules.PickupItem(12, 0)));

            uint item = OnlineRaceRules.PickupItem(4095, 65535);
            Assert.AreEqual(4095, OnlineRaceRules.PickupIndexOf(item));
            Assert.AreEqual(65535, OnlineRaceRules.PickupGenerationOf(item));
            uint dropped = OnlineRaceRules.MineItem(3, 21);
            Assert.AreEqual(3, OnlineRaceRules.MineOwnerOf(dropped));
            Assert.AreEqual(21, OnlineRaceRules.MineIdOf(dropped));
            Assert.AreNotEqual(OnlineRaceRules.MineItem(2, 21), dropped, "two cars' mines of the same number");
        }

        [Test]
        public void ScoresRankLikeTheRace()
        {
            long fast = OnlineRaceRules.Score(true, false, 80_000, 400_000);
            long slow = OnlineRaceRules.Score(true, false, 95_000, 400_000);
            long farRunning = OnlineRaceRules.Score(false, false, 0, 350_000);
            long nearRunning = OnlineRaceRules.Score(false, false, 0, 120_000);
            long farWreck = OnlineRaceRules.Score(false, true, 60_000, 390_000);
            long nearWreck = OnlineRaceRules.Score(false, true, 10_000, 5_000);
            Assert.Greater(fast, slow);
            Assert.Greater(slow, farRunning, "every finisher before everybody still running");
            Assert.Greater(farRunning, nearRunning);
            Assert.Greater(nearRunning, farWreck, "the wrecked come last, however far they got");
            Assert.Greater(farWreck, nearWreck);
            Assert.IsTrue(OnlineRaceRules.IsFinishedScore(slow));
            Assert.IsFalse(OnlineRaceRules.IsFinishedScore(farRunning));
            Assert.AreEqual(95_000, OnlineRaceRules.TimeOfScore(slow));
            Assert.AreEqual(0, OnlineRaceRules.TimeOfScore(farWreck));
            Assert.LessOrEqual(OnlineRaceRules.Score(false, false, 0, long.MaxValue), OnlineRaceRules.MostRunningScore, "a running score never reaches the finishers'");
        }

        [Test]
        public void ResultsAreHeldToTheTrack()
        {
            Assert.AreEqual(32_000, OnlineRaceRules.FastestTimeMs(4, 800));
            Assert.AreEqual(5L * 800 * 100, OnlineRaceRules.MostProgressCm(4, 800), "a lap more than the race");
            Assert.AreEqual(4, OnlineRaceRules.LapsOf(0, 4), "0 laps is the track's own");
            Assert.AreEqual(2, OnlineRaceRules.LapsOf(2, 4));
            Assert.AreEqual(OnlineRaceRules.MaxLaps, OnlineRaceRules.LapsOf(40, 4));
        }
    }
}
