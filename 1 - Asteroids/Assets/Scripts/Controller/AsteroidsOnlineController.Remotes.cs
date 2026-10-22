using System.Collections.Generic;
using Gamebox;
using Gamebox.Online;
using UnityEngine;

namespace Portfolio.Asteroids
{
    /// <summary>
    /// The shared screen and the remote controls half of the online controller. On the shared screen (which holds no
    /// seat) the seats of the remotes are driven here (<see cref="OnlineGameController.DrivesSeat"/>): the manager flies
    /// each as a wingman of its party from the controls that arrive over the network
    /// (<see cref="OnlineGameController.ControlsOf"/>), this controller sends their poses, scores and ends as it does for
    /// a party, and tells every remote what its seat is up to (<see cref="OnlineGameController.PublishStatus"/>, from
    /// <see cref="RemoteSeatStatus"/>). On a remote control's device nothing is flown: the scene's ship is parked, the
    /// device shows the panel (<see cref="BuildRemotePanel"/>: the rows of the status and the pad of
    /// <see cref="LocalCoopRules.Scheme"/>) over the world it keeps as puppets, and its own seat is a stand-in like any
    /// other. A player who switches to their own device mid-mission takes the seat over from the last pose
    /// (<see cref="OnSeatDriverChanged"/>, AsteroidsGameManager.Screen.cs), and the screen turns its wingman into a
    /// stand-in; switching back goes the other way. In a strike mission a remote sends its loadout as a room action
    /// (<see cref="LoadoutAction"/>), so the screen flies the seat with the pilot's own kit.
    /// </summary>
    public partial class AsteroidsOnlineController
    {
        /// <summary>The kind of the room action a remote sends with its strike loadout (<see cref="StrikeLoadoutText"/>), for the screen that flies its seat.</summary>
        internal const uint LoadoutAction = 1;

        private readonly Dictionary<byte, StrikeLoadout> remoteLoadouts = new Dictionary<byte, StrikeLoadout>();
        private bool ownSeatFlying = true;
        private string lastRemoteStatus;
        private StrikeLoadout lastRemoteLoadout;

        /// <summary>
        /// The loadout the shared screen last said this device's seat flies with (from its status, kept here because the
        /// status rows go when the room finishes); null before the first status of a strike mission.
        /// </summary>
        internal StrikeLoadout LastRemoteLoadout => lastRemoteLoadout;

        /// <summary>The panel is the whole interface of a remote: the world goes on underneath, as puppets, for a switch to this device.</summary>
        public override bool RemotePanelCoversGame => true;

        /// <summary>The device's own seat is flown here, by the scene's ship (not on the shared screen, nor on a remote control).</summary>
        internal bool DrivesOwnSeat => LocalSeat != RoomTurnInfo.NoSeat && DrivesSeat(LocalSeat);

        /// <summary>The score the server has for the device's own seat (what the shared screen reported for it).</summary>
        internal long OwnSeatScore => Server != null && Server.LocalMember != null ? Server.LocalMember.Score : 0;

        /// <summary>
        /// Whether the device's own seat still flew as its last pose said (the shared screen sent it while the mission
        /// ran; the poses themselves are gone by the time the results show).
        /// </summary>
        internal bool OwnSeatFlying => ownSeatFlying;

        /// <summary>The score of the whole room: every seat's, the ones flown here as the manager counts them (the shared screen's HUD).</summary>
        internal long RoomScore
        {
            get
            {
                long total = 0;
                if (Server == null || Asteroids == null)
                {
                    return total;
                }
                foreach (RoomMemberInfo member in Server.Members)
                {
                    total += DrivesSeat(member.Seat) ? Asteroids.CoopScoreOf(member.Seat) : member.Score;
                }
                return total;
            }
        }


        /// <summary>A pose arrived: the device's own seat's says whether its ship still flies (the shared screen flies it).</summary>
        private void NoteOwnPose(RoomPoseInfo pose)
        {
            if (Server != null && Server.Identity.HasValue && pose.Identity.Equals(Server.Identity.Value))
            {
                ownSeatFlying = ShipPose.Unpack(pose.State, pose.Value).Alive;
            }
        }


        /// <summary>The remote panel: the rows of the seat's status for the kind of mission, and the pad of the pilots' scheme.</summary>
        protected override void BuildRemotePanel(RectTransform content, RemotePanelKit kit)
        {
            MissionMode mode = Asteroids != null && Asteroids.IsStrike ? MissionMode.Strike : MissionMode.Field;
            kit.StatusRows(RemoteSeatStatus.PanelKeys(mode), RemoteSeatStatus.Labels);
            kit.Pad(LocalCoopRules.Scheme, LocalBindings);
        }


        /// <summary>The panel showed or went: the pilots list follows the seats flown here (the frame covers the game otherwise).</summary>
        protected override void OnPresentationChanged()
        {
            if (InCharge)
            {
                ShowPilots();
            }
        }


        /// <summary>
        /// A seat changed hands in the middle of the mission. The device's own seat: taken over from the shared screen
        /// (the scene's ship flies again from the seat's last pose) or handed to it (the ship parks; in a strike mission the
        /// loadout goes to the screen first). A remote's seat on the screen: a wingman is made from its last pose, or let
        /// go for a stand-in. The replication's seats and the pilots list follow.
        /// </summary>
        protected override void OnSeatDriverChanged(byte seat, bool drivenHere)
        {
            AsteroidsGameManager manager = Asteroids;
            if (!InCharge || manager == null || !manager.IsCoop || replication == null)
            {
                return;
            }
            if (seat == LocalSeat)
            {
                if (drivenHere)
                {
                    SyncShips();
                    manager.TakeOwnSeat(OwnSeatSeed());
                }
                else
                {
                    if (manager.IsStrike)
                    {
                        SubmitLoadout(manager.Working);
                        // Until the screen's first status about the seat arrives, the kit this device flew is the seat's.
                        lastRemoteLoadout = manager.Working != null ? manager.Working.Clone() : lastRemoteLoadout;
                    }
                    manager.GiveUpOwnSeat();
                    SyncShips();
                }
            }
            else if (drivenHere)
            {
                SyncShips();
                RoomMemberInfo member = Server.MemberAt(seat);
                if (member != null && member.Playing)
                {
                    remoteLoadouts.TryGetValue(seat, out StrikeLoadout loadout);
                    manager.AddDrivenPilot(seat, NameOf(member), SlotOf(seat), ControlsOf(seat), SeedOf(member, loadout));
                }
            }
            else
            {
                manager.ReleaseDrivenPilot(seat);
                SyncShips();
            }
            replication.SetSeats(PartySeats());
            ShowPilots();
        }


        /// <summary>A remote's strike loadout arrived: the screen flies the seat with it (kept for a seat it takes over later).</summary>
        protected override void OnActionReceived(RoomActionInfo action)
        {
            if (action.Kind != LoadoutAction)
            {
                return;
            }
            StrikeLoadout loadout = StrikeLoadoutText.Parse(action.Payload);
            byte seat = SeatOfSender(action);
            if (loadout == null || seat == RoomTurnInfo.NoSeat)
            {
                return;
            }
            remoteLoadouts[seat] = loadout;
            if (IsScreen && Asteroids != null)
            {
                Asteroids.ApplyDrivenLoadout(seat, loadout);
            }
        }


        /// <summary>The seat of the member who sent <paramref name="action"/>; the action's own seat when the sender is not a player.</summary>
        private byte SeatOfSender(RoomActionInfo action)
        {
            foreach (RoomMemberInfo member in Server.Members)
            {
                if (member.Identity.Equals(action.Sender))
                {
                    return member.Seat;
                }
            }
            return action.Seat;
        }


        /// <summary>A remote tells the screen the loadout its seat flies with (nothing happens on a device that is not a remote).</summary>
        private void SubmitLoadout(StrikeLoadout loadout)
        {
            if (loadout == null || Server == null || !Server.InRoom || !IsRemote)
            {
                return;
            }
            Server.SubmitAction(LocalSeat, LoadoutAction, StrikeLoadoutText.Write(loadout), error =>
            {
                if (error != null)
                {
                    Debug.LogWarning("The strike loadout was not sent to the shared screen: " + error, this);
                }
            });
        }


        /// <summary>The loadouts the remotes sent before the mission's pilots were made go to their wingmen now.</summary>
        private void ApplyRemoteLoadouts(AsteroidsGameManager manager)
        {
            if (!IsScreen || manager == null)
            {
                return;
            }
            foreach (KeyValuePair<byte, StrikeLoadout> entry in remoteLoadouts)
            {
                manager.ApplyDrivenLoadout(entry.Key, entry.Value);
            }
        }


        private void ForgetRemoteLoadouts()
        {
            remoteLoadouts.Clear();
        }


        /// <summary>The shared screen tells every remote what its seat is up to (the base sends a status when it changed, a few times a second).</summary>
        private void PublishStatuses(AsteroidsGameManager manager)
        {
            if (!IsScreen || !IsPlaying)
            {
                return;
            }
            foreach (RoomMemberInfo member in Server.Members)
            {
                if (!DrivesSeat(member.Seat))
                {
                    continue;
                }
                string status = manager.DrivenSeatStatus(member.Seat);
                if (status != null)
                {
                    PublishStatus(member.Seat, status);
                }
            }
        }


        /// <summary>A remote keeps what the screen last said about its seat: the loadout of the status, for the mission's end and a switch.</summary>
        private void RememberRemoteStatus()
        {
            if (!IsRemote)
            {
                return;
            }
            string status = RemoteStatus;
            if (string.IsNullOrEmpty(status) || status == lastRemoteStatus)
            {
                return;
            }
            lastRemoteStatus = status;
            StrikeLoadout loadout = StrikeLoadoutText.Parse(RoomOptions.Value(status, RemoteSeatStatus.Loadout));
            if (loadout != null)
            {
                lastRemoteLoadout = loadout;
            }
        }


        /// <summary>What <paramref name="member"/>'s seat is seeded with when this device starts flying it: its last pose, the server's score, a loadout.</summary>
        private AsteroidsGameManager.SeatSeed SeedOf(RoomMemberInfo member, StrikeLoadout loadout)
        {
            RoomPoseInfo? pose = Server.Poses.TryGetValue(member.Identity, out RoomPoseInfo known) ? known : (RoomPoseInfo?)null;
            return AsteroidsGameManager.SeatSeed.From(pose, member.Score, loadout);
        }


        /// <summary>The seed of the device's own seat, as the shared screen last reported it (its status carries the strike loadout).</summary>
        private AsteroidsGameManager.SeatSeed OwnSeatSeed()
        {
            RoomMemberInfo me = Server.LocalMember;
            if (me == null)
            {
                return default;
            }
            RememberRemoteStatus();
            StrikeLoadout loadout = Asteroids != null && Asteroids.IsStrike ? lastRemoteLoadout : null;
            return SeedOf(me, loadout);
        }


        /// <summary>The place of <paramref name="seat"/> among the pilots of the room, by seat (the start positions).</summary>
        private int SlotOf(byte seat)
        {
            IReadOnlyList<RoomMemberInfo> members = Server.Members;
            for (int i = 0; i < members.Count; i++)
            {
                if (members[i].Seat == seat)
                {
                    return i;
                }
            }
            return 0;
        }
    }
}
