using System.Collections.Generic;
using Gamebox;
using Gamebox.Online;
using UnityEngine;

namespace Portfolio.BoardGames
{
    /// <summary>
    /// Online controller of the Board Games module: games with a player on another device, in a room of the game's
    /// SpacetimeDB database. Rooms, ready and start, the action log and its clock, the seats of the table and taking the
    /// log to the table come from the shared <see cref="LockstepOnlineController{TCommand}"/> and the base server; this
    /// class adds what is Board Games. A room plays one rule set (its level: the three draughts rules, chess or
    /// backgammon), against a clock, with the computer in the second seat when the host asks for an opponent, and for
    /// backgammon with or without the doubling cube. The server seats the table (Server/Lib.cs), every client starts
    /// the same game from it (BoardGamesManager.Online.cs), and from then on the table changes only by the actions of
    /// the log: the decisions of the people here are sent there instead of into the rules (<see cref="IBoardCommands"/>),
    /// the host's client sends the computer's, and every action is applied when it arrives, in the order of the server.
    /// The people here are the player and whoever joined at the same device (the base's companions): each plays a seat
    /// of their own (<see cref="LockstepOnlineController{TCommand}.TableSeats"/>), and a command may be sent for whichever
    /// of their seats the table waits for. With two seats a party of two would fill the table, and two people at one
    /// device playing each other is local play: the server does not start such a room (Server/Lib.cs).
    ///
    /// A room may play on a shared screen (<see cref="OnlineGameController.IsScreen"/>): the screen runs the table with
    /// no seat of its own and shows the whole game (the manager plays nobody's turn there, and the computer's as the
    /// host). A player's device that is a remote control of it (<see cref="OnlineGameController.IsRemote"/>) runs the
    /// game as every client does and sends its moves the same way: the board stays its control under the remote frame,
    /// which does not cover the game (<see cref="RemotePanelCoversGame"/>); the game's HUD moves in between the frame's
    /// header and footer (<see cref="BoardGamesUI.SetRemoteLayout"/>), and the frame's header carries the game's own
    /// words for the turn and the clock. Switching between the remote and the device itself mid-game changes the
    /// presentation alone (<see cref="OnPresentationChanged"/>).
    /// </summary>
    public sealed class BoardGamesOnlineController : LockstepOnlineController<BoardCommand>, IBoardCommands
    {
        private static readonly int[] TurnSeconds = { 0, 30, 60, 120, 300 };

        /// <summary>The room option of the backgammon doubling cube: "1" plays with it (the default), "0" without.</summary>
        public const string CubeOption = "cube";

        /// <summary>The frame's header (with its gap) and footer, for the HUD's layout before the frame is built to be asked.</summary>
        private const float FrameTop = 160f;
        private const float FrameBottom = 110f;

        private RoomOptionSpec[] optionSpecs;

        /// <summary>The table seat of the player at this device themselves in the game that started last, or -1.</summary>
        private int ownTableSeat = -1;

        public BoardGamesManager Games => BaseManager as BoardGamesManager;

        public override int MaxPlayersLimit => 2;

        public override int MinPlayersLimit => 2;

        /// <summary>The rule sets a room can play: one level per rule set (the number is the <see cref="RuleSet"/>).</summary>
        protected override CampaignProgress CreateOnlineProgress(IStorageStrategy storage)
        {
            return new BoardGamesProgress(storage, GameType.BoardGames);
        }

        /// <summary>
        /// A game played online counts among the games, wins and draws. A room plays a rule set, not a level of the
        /// campaign: nothing to unlock. A draw by the rules is the table's to say: the room places nobody first then (every
        /// member is second), so the places tell it only of a game ended early, ranked by worth, where equal worth shares
        /// the first place. The table is still there when the room finishes.
        /// </summary>
        protected override void RecordOnlineResult(RoomInfo room, RoomMemberInfo me)
        {
            BoardTable table = Games != null ? Games.Table : null;
            bool drawn = table != null && table.IsOver && table.WinnerSeat < 0 && table.Game.Reason != EndReason.CalledOff;
            if (!drawn)
            {
                int first = 0;
                foreach (RoomMemberInfo member in Server.Members)
                {
                    if (member.Place == 1)
                    {
                        first++;
                    }
                }
                drawn = first > 1;
            }
            ((BoardGamesProgress)OnlineProgress).RecordGame(me.Place == 1 && !drawn, drawn);
        }

        /// <summary>Every rule set is open online, so an admin's tournaments offer the same.</summary>
        public override IReadOnlyList<RoomLevelChoice> AllLevelChoices => LevelChoices;

        public override IReadOnlyList<RoomLevelChoice> LevelChoices
        {
            get
            {
                var choices = new List<RoomLevelChoice>();
                for (int i = 0; i < RuleSets.Count; i++)
                {
                    choices.Add(new RoomLevelChoice(i, L.T(RuleSets.NameKey(RuleSets.All[i]))));
                }
                return choices;
            }
        }

        public override IReadOnlyList<RoomOptionSpec> OptionSpecs => optionSpecs ??= new[]
        {
            RoomOptionSpec.Clock("Time to move", TurnSeconds, 2),
            RoomOptionSpec.Computers("Computer opponent", 1),
            RoomOptionSpec.ComputerLevel("Computer level"),
            new RoomOptionSpec(CubeOption, "Doubling cube", new[] { "1", "0" }, new[] { "On", "Off" }, 0)
        };

        public override string DescribeRoom(RoomInfo room)
        {
            int computers = room.Option(RoomOptions.ComputersOption, 0);
            return computers > 0 ? L.F("{0}, computer opponent", base.DescribeRoom(room)) : base.DescribeRoom(room);
        }

        // ------------------------------------------------------------------ the room

        /// <summary>
        /// The server seated the table: the manager gets it with the seats played at this device, then loads the rule set
        /// of the room and starts the game. Every new game of a room swaps who plays the side that moves first (White in
        /// chess and backgammon, where the opening roll then decides who starts).
        /// </summary>
        protected override bool PrepareTable(LockstepSetup setup)
        {
            BoardGamesManager manager = Games;
            if (manager == null || setup.Seats.Count < 2)
            {
                return false;
            }
            ownTableSeat = -1;
            for (int i = 0; i < setup.Seats.Count; i++)
            {
                if (setup.Seats[i].IsHuman && Server.Identity.HasValue && setup.Seats[i].Player == Server.Identity.Value)
                {
                    ownTableSeat = i;
                }
            }
            var table = new TableSetup
            {
                ruleSet = Mathf.Clamp(setup.Level, 0, RuleSets.Count - 1),
                firstSeat = (int)((setup.Round > 0 ? setup.Round - 1 : 0) % 2),
                clockSeconds = 0,
                doublingCube = setup.Room == null || setup.Room.Option(CubeOption, 1) != 0,
                openingRoll = true
            };
            for (int i = 0; i < 2; i++)
            {
                RoomSeatInfo seat = setup.Seats[i];
                table.seats[i] = new TableSeat
                {
                    name = seat.Name,
                    computer = !seat.IsHuman,
                    level = Mathf.Clamp(seat.BotLevel, 0, RuleSets.ComputerLevels - 1)
                };
            }
            SaveData saved = setup.Save != null ? setup.Save.Read<SaveData>() : null;
            manager.PrepareOnlineTable(table, setup.TableSeats, setup.Seed, saved);
            return true;
        }

        /// <summary>
        /// Whether table seat <paramref name="tableSeat"/> is played by the player at this device themselves, and not by a
        /// player who joined at this device (whose seat <see cref="LockstepOnlineController{TCommand}.IsLocalTableSeat"/> knows
        /// too). It is read when the table is seated, so the results still know it once the room let its seats go.
        /// </summary>
        public bool IsOwnTableSeat(int tableSeat)
        {
            return tableSeat >= 0 && tableSeat == ownTableSeat;
        }

        /// <summary>
        /// Before a player at this device acts for table seat <paramref name="tableSeat"/> after another player here did: the
        /// base shows the hand-over screen when the party asked for one in the lobby, else it calls <paramref name="ready"/>
        /// at once (<see cref="OnlineGameController.HandOver"/>).
        /// </summary>
        public void HandOverSeat(int tableSeat, System.Action ready, string detail = null)
        {
            HandOver(RoomSeatOf(tableSeat), ready, detail);
        }

        /// <summary>The two seats of a saved game of any of the three games, for a room opened from it (null when it is no whole game).</summary>
        protected override IReadOnlyList<SavedSeatInfo> SavedSeats(SavedGame save)
        {
            SaveData data = save != null ? save.Read<SaveData>() : null;
            if (data == null || !data.IsWhole())
            {
                return null;
            }
            var seats = new List<SavedSeatInfo>();
            for (int i = 0; i < data.setup.seats.Length; i++)
            {
                TableSeat seat = data.setup.seats[i] ?? new TableSeat();
                seats.Add(new SavedSeatInfo(seat.name, !seat.computer, (byte)i, (byte)Mathf.Clamp(seat.level, 0, RuleSets.ComputerLevels - 1)));
            }
            return seats;
        }

        // ------------------------------------------------------------------ the shared screen and the remote

        /// <summary>On a remote the board stays the control: the frame is drawn around the game, which keeps showing.</summary>
        public override bool RemotePanelCoversGame => false;

        /// <summary>
        /// The frame's header on a remote: the game's own words for the turn ("Roll the dice", "{0} doubles: take or
        /// drop?", "Your move") and the seconds left on the room's clock; null while the device is no remote, for the
        /// frame's own line.
        /// </summary>
        public string RemoteStatusLine
        {
            get
            {
                BoardGamesManager manager = Games;
                if (!ShowsRemotePanel || manager == null || manager.Table == null)
                {
                    return null;
                }
                string words = manager.TurnWords ?? "";
                float remaining = manager.ClockRemaining;
                return remaining >= 0f ? $"{words}  {Mathf.CeilToInt(remaining)}" : words;
            }
        }

        /// <summary>
        /// The remote panel adds nothing of its own: the board under the frame takes the moves, and the game's HUD, laid
        /// out between the frame's header and footer (<see cref="BoardGamesUI.SetRemoteLayout"/>), keeps the seat panels,
        /// the backgammon buttons, the promotion picker and Resign; the frame's header carries <see cref="RemoteStatusLine"/>.
        /// </summary>
        protected override void BuildRemotePanel(RectTransform content, RemotePanelKit kit)
        {
        }

        /// <summary>This device became a remote of the room's screen or stopped being one, in the lobby or mid-game: the HUD moves in under the frame, or back.</summary>
        protected override void OnPresentationChanged()
        {
            ApplyPresentation();
        }

        /// <summary>Once a frame while connected: the frame's header follows the game's words for the turn.</summary>
        protected override void OnServerTick()
        {
            base.OnServerTick();
            RemoteControlUI frame = RemoteUI;
            if (frame != null)
            {
                frame.SetStatusLine(RemoteStatusLine);
            }
        }

        /// <summary>The HUD's layout for the presentation now: under the frame while the remote panel shows, else the usual one.</summary>
        private void ApplyPresentation()
        {
            BoardGamesManager manager = Games;
            BoardGamesUI ui = manager != null ? manager.UI as BoardGamesUI : null;
            if (ui == null)
            {
                return;
            }
            bool remote = ShowsRemotePanel;
            RectTransform content = RemoteUI != null ? RemoteUI.Content : null;
            float top = content != null ? -content.offsetMax.y : FrameTop;
            float bottom = content != null ? content.offsetMin.y : FrameBottom;
            if (ui.IsRemoteLayout != remote)
            {
                ui.SetRemoteLayout(remote, top, bottom);
            }
        }

        // ------------------------------------------------------------------ the log

        /// <summary>
        /// The command of an action. The rules' own <see cref="CommandKind.SeatToComputer"/> is not one a player can send:
        /// the server says it with an action of its own.
        /// </summary>
        protected override BoardCommand Parse(byte seat, uint kind, string payload)
        {
            return BoardCommand.PlayerMaySend(kind) ? BoardCommand.Parse(seat, (CommandKind)kind, payload) : null;
        }

        protected override void Encode(BoardCommand command, out byte seat, out uint kind, out string payload)
        {
            seat = (byte)command.seat;
            kind = (uint)command.kind;
            payload = command.Payload();
        }

        // ------------------------------------------------------------------ what the players here may do

        /// <summary>Whether a command for <paramref name="seat"/>, a seat played at this device, may go to the log now.</summary>
        private bool MaySend(int seat)
        {
            return IsPlaying && IsLocalTableSeat(seat) && !AwaitingEcho && Games != null && Games.Table != null && !Games.Table.IsOver;
        }

        public bool Send(BoardCommand command)
        {
            if (command == null || !BoardCommand.PlayerMaySend((uint)command.kind) || command.Kind == CommandKind.Resign || !MaySend(command.seat)
                || Games.Table.SeatToAct != command.seat)
            {
                return false;
            }
            return Submit(command, true);
        }

        public bool Move(int seat, Move move)
        {
            return move != null && Send(BoardCommand.MoveAlong(seat, move));
        }

        public bool Resign(int seat)
        {
            if (!MaySend(seat))
            {
                return false;
            }
            return Submit(BoardCommand.Resign(seat), true);
        }
    }
}
