using Gamebox;
using Gamebox.Online;
using NUnit.Framework;
using UnityEngine;

namespace Portfolio.DeathRace.Tests
{
    /// <summary>
    /// How a device builds the race of an online room (<see cref="OnlineRaceSetup.Build"/>) and what a room
    /// tells the server about its track (<see cref="OnlineRaceSetup.OptionsFor"/>).
    /// </summary>
    public class OnlineSetupTest
    {
        private static OnlineRaceSetup.RoomRacer Member(byte seat, int localIndex = -1, PlayerControls controls = null)
        {
            return new OnlineRaceSetup.RoomRacer { Seat = seat, Name = "Seat " + seat, Local = localIndex >= 0, LocalIndex = localIndex, Controls = controls };
        }

        private static RaceSetup Build(int track, string options, params OnlineRaceSetup.RoomRacer[] members)
        {
            return OnlineRaceSetup.Build(track, options, 12345u, members, seat => Color.HSVToRGB(seat * 0.25f, 1f, 1f));
        }

        [Test]
        public void EveryMemberIsACarInTheOrderOfTheSeats()
        {
            RaceSetup setup = Build(1, OnlineRaceRules.CarOption + "=" + (int)CarModel.Bulldog, Member(3), Member(0), Member(2, 0));
            Assert.AreEqual(RaceMode.Online, setup.Mode);
            Assert.AreEqual(1, setup.Track);
            Assert.AreEqual(3, setup.Entrants.Count);
            int[] roomSeats = { 0, 2, 3 };
            for (int i = 0; i < 3; i++)
            {
                RaceEntrant entrant = setup.Entrants[i];
                Assert.AreEqual(i, entrant.Seat, "the grid follows the seats");
                Assert.AreEqual(roomSeats[i], entrant.RoomSeat);
                Assert.AreEqual(CarModel.Bulldog, entrant.Loadout.model, "everybody in the room's car");
                Assert.AreEqual("Seat " + roomSeats[i], entrant.Name);
                Assert.AreEqual(Color.HSVToRGB(roomSeats[i] * 0.25f, 1f, 1f), entrant.Color, "a member's colour is their seat's on every device");
            }
            Assert.AreEqual(DriverKind.Remote, setup.Entrants[0].Driver);
            Assert.AreEqual(DriverKind.Human, setup.Entrants[1].Driver);
            Assert.AreEqual(0, setup.Entrants[1].Viewport);
            Assert.IsNull(setup.Entrants[1].Controls, "alone at the device the player keeps every keyboard layout");
            Assert.AreEqual(DriverKind.Remote, setup.Entrants[2].Driver);
            Assert.AreEqual(-1, setup.Entrants[2].Viewport);
            Assert.AreEqual(12345, setup.Seed);
            Assert.AreEqual(1, setup.Humans);
        }

        [Test]
        public void APartyDrivesOnASetOfControlsEachInAViewEach()
        {
            var bindings = new ControlBindings(DeathRaceControls.Scheme());
            PlayerControls first = bindings.For(0);
            PlayerControls second = bindings.For(1);
            RaceSetup setup = Build(0, "", Member(0, 1, second), Member(1), Member(2, 0, first));
            Assert.AreEqual(DriverKind.Human, setup.Entrants[0].Driver);
            Assert.AreEqual(DriverKind.Human, setup.Entrants[2].Driver);
            Assert.AreSame(second, setup.Entrants[0].Controls);
            Assert.AreSame(first, setup.Entrants[2].Controls);
            Assert.AreEqual(1, setup.Entrants[0].Viewport);
            Assert.AreEqual(0, setup.Entrants[2].Viewport, "the device's own player has the first view");
            Assert.AreEqual(2, setup.Humans);
        }

        [Test]
        public void TheLapsAreTheRoomsOrTheTracks()
        {
            Assert.AreEqual(TrackLayouts.Of(2).Laps, Build(2, "", Member(0, 0), Member(1)).Laps);
            Assert.AreEqual(3, Build(2, OnlineRaceRules.LapsOption + "=3", Member(0, 0), Member(1)).Laps);
            Assert.AreEqual(DeathRaceLocalRules.DefaultCar, Build(2, "", Member(0, 0)).Entrants[0].Loadout.model);
        }

        [Test]
        public void ARoomTellsTheServerItsTrack()
        {
            for (int track = 0; track < TrackLayouts.Count; track++)
            {
                string options = OnlineRaceSetup.OptionsFor(track, "laps=3;car=4");
                Assert.AreEqual(3, RoomOptions.Number(options, OnlineRaceRules.LapsOption, -1));
                Assert.AreEqual(4, RoomOptions.Number(options, OnlineRaceRules.CarOption, -1));
                Assert.AreEqual(TrackLayouts.Of(track).Laps, RoomOptions.Number(options, OnlineRaceRules.TrackLapsOption, -1));
                int length = RoomOptions.Number(options, OnlineRaceRules.LengthOption, -1);
                Assert.That(length, Is.InRange(OnlineRaceRules.MinLength, OnlineRaceRules.MaxLength), TrackLayouts.Of(track).Name);
                Assert.That(length, Is.InRange(350, 1500), "the tracks are 450 to 1400 m: " + TrackLayouts.Of(track).Name);
            }
            Assert.AreEqual((int)DeathRaceLocalRules.DefaultCar, RoomOptions.Number(OnlineRaceSetup.OptionsFor(0, ""), OnlineRaceRules.CarOption, -1));
        }
    }
}
