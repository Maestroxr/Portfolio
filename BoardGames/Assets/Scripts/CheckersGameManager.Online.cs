using Gamebox;
using Gamebox.Lockstep;
using Gamebox.Online;
using UnityEngine;

namespace Portfolio.Checkers
{
    /// <summary>
    /// The online half of the manager: a game played in step with a player on another device
    /// (<see cref="CheckersOnlineController"/>, Server/Lib.cs). The table is the room's: nothing but the action log of
    /// the server changes it; the controller takes every action to it and tells the manager
    /// (<see cref="ILockstepHost{TCommand}"/>), which reacts in <see cref="AfterEntry"/> as it does to a move at this
    /// device. The director offers a move only to the person here, and only once the board caught up; the computer in
    /// the other seat is moved by the client of the host, through the log like everybody else. The results lead back to
    /// the room.
    /// </summary>
    public partial class CheckersGameManager : ILockstepHost<CheckersCommand>
    {
        [SerializeField] private CheckersOnlineController online;

        private int onlineSeat = -1;
        private TableSetup onlineSetup;
        private SaveData onlineSaved;
        private uint onlineSeed;
        private int preparedSeat = -1;

        public CheckersOnlineController Online => online;

        protected override IOnlineLobby OnlineLobby => online;

        // ------------------------------------------------------------------ starting and ending

        /// <summary>The online controller hands over the table of the game that is about to start, and the saved game it goes on from.</summary>
        internal void PrepareOnlineTable(TableSetup setup, int localSeat, uint seed, SaveData saved)
        {
            onlineSetup = setup;
            preparedSeat = localSeat;
            onlineSeed = seed;
            onlineSaved = saved;
        }

        /// <summary>Seats the table the server set up and starts the game from the seed of the room, as every other client does.</summary>
        private void StartOnlineTable()
        {
            TableSetup setup = onlineSetup;
            SaveData saved = onlineSaved;
            int seat = preparedSeat;
            uint seed = onlineSeed;
            onlineSetup = null;
            onlineSaved = null;
            preparedSeat = -1;
            if (setup == null || seat < 0 || seat > 1)
            {
                UI?.UpdateError(L.T("The table of the online game did not arrive."));
                return;
            }
            CheckersGame game = null;
            if (saved != null)
            {
                if (!SaveIsWhole(saved))
                {
                    UI?.UpdateError(L.T("The saved game is incomplete."));
                    return;
                }
                // The saved players sit in the seats by index: the sides stay as they were.
                game = saved.game;
                setup.variant = saved.game.variant;
                setup.firstSeat = Mathf.Clamp(saved.setup.firstSeat, 0, 1);
            }
            if (table != null)
            {
                EndTable();
                EndLocalMatch();
            }
            onlineSeat = seat;
            BeginTable(setup, seed, game);
        }

        /// <summary>The session is over (the results were left, the room was, the connection dropped): the table is cleared.</summary>
        protected override void OnSessionChanged()
        {
            if (InSession || (onlineSeat < 0 && onlineSetup == null))
            {
                return;
            }
            onlineSetup = null;
            onlineSaved = null;
            preparedSeat = -1;
            onlineSeat = -1;
            if (table != null)
            {
                EndTable();
            }
        }

        /// <summary>The place of the member at this device in the room that finished (0 while it has none).</summary>
        private int LocalPlace()
        {
            RoomMemberInfo me = online != null && online.ServerClient != null ? online.ServerClient.LocalMember : null;
            return me != null ? (int)me.Place : 0;
        }

        /// <summary>
        /// The rules ended the game: the room learns who won and where the player here came. In a tournament a draw is
        /// decided for the report only, so a bracket never stalls: the side with more material wins, equal ones the seat
        /// that moved second.
        /// </summary>
        private void ReportFinished(int winnerSeat)
        {
            if (online == null || table == null)
            {
                return;
            }
            int reported = winnerSeat;
            if (reported < 0 && online.Room != null && online.Room.IsTournamentMatch)
            {
                int first = Game.Material(Setup.SideOf(0));
                int second = Game.Material(Setup.SideOf(1));
                reported = first != second ? (first > second ? 0 : 1) : 1 - Mathf.Clamp(Setup.firstSeat, 0, 1);
            }
            int place = reported < 0 || reported == onlineSeat ? 1 : 2;
            online.ReportTableFinished(reported, place, Game.Material(Setup.SideOf(onlineSeat)));
        }

        /// <summary>What the player at this device is worth as the game goes: the score the members are ranked by should the room end early.</summary>
        private void ReportToRoom()
        {
            if (online == null || table == null || table.IsOver)
            {
                return;
            }
            int worth = Game.Material(Setup.SideOf(onlineSeat));
            if (worth != reportedWorth)
            {
                reportedWorth = worth;
                online.ReportScore(worth);
            }
        }

        // ------------------------------------------------------------------ what the server sends

        LockstepTable<CheckersCommand> ILockstepHost<CheckersCommand>.Table => IsOnlineGame ? table : null;

        int ILockstepHost<CheckersCommand>.TableSeat => onlineSeat;

        string ILockstepHost<CheckersCommand>.TurnCaption => OnlineCaption();

        /// <summary>An entry of the log was taken: the table has it already, in the order of the log; the game reacts as to a move here.</summary>
        void ILockstepHost<CheckersCommand>.EntryTaken(LockstepEntry<CheckersCommand> entry)
        {
            if (!IsOnlineGame || entry == null)
            {
                return;
            }
            CheckersCommand command = entry.Kind == LockstepEntryKind.SeatToComputer
                ? CheckersCommand.ToComputer(entry.Seat, table.LevelOf(entry.Seat))
                : entry.Command;
            AfterEntry(command, entry.Accepted, entry.Kind, entry.Made);
            ReportToRoom();
        }

        /// <summary>
        /// The room finished before the rules did (the host ended it, too few players): the game is called off here as
        /// it is on every other client, and the results show with the place the room gave.
        /// </summary>
        void ILockstepHost<CheckersCommand>.RoomFinished()
        {
            if (!IsOnlineGame || table.IsOver)
            {
                return;
            }
            Game.CallOff(null);
            version++;
            WaitingForHuman = false;
            ClearSelection();
            ui?.Toast(L.T("The game was called off."));
            WriteResult();
            UpdateHighlights();
            RaiseChanged();
        }

        /// <summary>A move of the player at this device did not reach the log: the choice is theirs again.</summary>
        void ILockstepHost<CheckersCommand>.CommandFailed()
        {
            sound?.Play(ClipKind.Click);
            version++;
        }

        /// <summary>Who the table waits for, for the clock of the lobby.</summary>
        private string OnlineCaption()
        {
            if (!IsOnlineGame)
            {
                return "";
            }
            string caption = TurnWords;
            // The banner has room for a short line; long names get smaller letters.
            return caption.Length <= 17 ? caption : caption.Length <= 22 ? $"<size=80%>{caption}</size>" : $"<size=62%>{caption}</size>";
        }
    }
}
