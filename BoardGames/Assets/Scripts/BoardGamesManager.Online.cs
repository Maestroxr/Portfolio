using System.Collections.Generic;
using Gamebox;
using Gamebox.Lockstep;
using Gamebox.Online;
using UnityEngine;

namespace Portfolio.BoardGames
{
    /// <summary>
    /// The online half of the manager: a game of any of the three played in step over the action log of the server
    /// (<see cref="BoardGamesOnlineController"/>, Server/Lib.cs). The table is the room's: nothing but the log changes
    /// it; the controller takes every action to it and tells the manager (<see cref="ILockstepHost{TCommand}"/>), which
    /// reacts in <see cref="AfterEntry"/> as it does to a move at this device. The director offers a decision only to the
    /// people here, and only once the board caught up; the computer in the other seat is played by the client of the
    /// host, through the log like everybody else. The seats played at this device are the player's own and those of the
    /// players who joined at the same device (<see cref="DeviceSeats"/>): with two of them the table is theirs alone, and
    /// the game treats them as local play treats two people, by name, with the hand-over the party asked for in the lobby
    /// and the board turning to the one to act when the setting says so. The results lead back to the room.
    /// </summary>
    public partial class BoardGamesManager : ILockstepHost<BoardCommand>
    {
        [SerializeField] private BoardGamesOnlineController online;

        /// <summary>The table seats played at this device in the running online game; none offline.</summary>
        private DeviceSeats onlineSeats = DeviceSeats.None;
        private TableSetup onlineSetup;
        private SaveData onlineSaved;
        private uint onlineSeed;
        private DeviceSeats preparedSeats = DeviceSeats.None;

        public BoardGamesOnlineController Online => online;

        protected override IOnlineLobby OnlineLobby => online;

        // ------------------------------------------------------------------ starting and ending

        /// <summary>
        /// The online controller hands over the table of the game that is about to start, the seats of it played at this
        /// device (the player's own and their companions'), and the saved game it goes on from.
        /// </summary>
        internal void PrepareOnlineTable(TableSetup setup, IReadOnlyList<int> localSeats, uint seed, SaveData saved)
        {
            onlineSetup = setup;
            preparedSeats = new DeviceSeats(localSeats);
            onlineSeed = seed;
            onlineSaved = saved;
        }

        /// <summary>Seats the table the server set up and starts the game from the seed of the room, as every other client does.</summary>
        private void StartOnlineTable()
        {
            TableSetup setup = onlineSetup;
            SaveData saved = onlineSaved;
            DeviceSeats seats = preparedSeats;
            uint seed = onlineSeed;
            onlineSetup = null;
            onlineSaved = null;
            preparedSeats = DeviceSeats.None;
            if (setup == null || !seats.Any)
            {
                UI?.UpdateError(L.T("The table of the online game did not arrive."));
                return;
            }
            IBoardGame game = null;
            if (saved != null)
            {
                if (!saved.IsWhole())
                {
                    UI?.UpdateError(L.T("The saved game is incomplete."));
                    return;
                }
                // The saved players sit in the seats by index: the sides and the game's options stay as they were.
                game = saved.Game;
                setup.ruleSet = (int)game.RuleSet;
                setup.firstSeat = Mathf.Clamp(saved.setup.firstSeat, 0, 1);
                setup.doublingCube = saved.setup.doublingCube;
                setup.openingRoll = saved.setup.openingRoll;
            }
            if (table != null)
            {
                EndTable();
                EndLocalMatch();
            }
            onlineSeats = seats;
            BeginTable(setup, seed, game);
        }

        /// <summary>The session is over (the results were left, the room was, the connection dropped): the table is cleared.</summary>
        protected override void OnSessionChanged()
        {
            if (InSession || (!onlineSeats.Any && onlineSetup == null))
            {
                return;
            }
            onlineSetup = null;
            onlineSaved = null;
            preparedSeats = DeviceSeats.None;
            onlineSeats = DeviceSeats.None;
            if (table != null)
            {
                EndTable();
            }
        }

        /// <summary>Whether a player at this device (the player, or one who joined here) came first in the room that finished.</summary>
        private bool PlacedFirstHere()
        {
            if (online == null)
            {
                return false;
            }
            foreach (RoomMemberInfo member in online.LocalMembers)
            {
                if (member.Place == 1)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// The seat a tournament match that ended without a winner sends on in the bracket, so a bracket never stalls: the
        /// side with more material (by the game's own measure) goes on, with equal material the seat that moved second.
        /// -1 when the game has a winner or is no tournament match.
        /// </summary>
        private int BracketSeatOfDraw()
        {
            if (table == null || !table.IsOver || table.WinnerSeat >= 0 || online == null || online.Room == null || !online.Room.IsTournamentMatch)
            {
                return -1;
            }
            int first = table.Game.Material(Setup.SideOf(0));
            int second = table.Game.Material(Setup.SideOf(1));
            return first != second ? (first > second ? 0 : 1) : 1 - Mathf.Clamp(Setup.firstSeat, 0, 1);
        }

        /// <summary>
        /// The rules ended the game: the room learns who won (a drawn tournament match: who goes on, see
        /// <see cref="BracketSeatOfDraw"/>) and where every player at this device came, each for the seat they played.
        /// Returns the seat reported as the winner, -1 for none.
        /// </summary>
        private int ReportFinished(int winnerSeat)
        {
            if (online == null || table == null)
            {
                return winnerSeat;
            }
            int reported = winnerSeat >= 0 ? winnerSeat : BracketSeatOfDraw();
            foreach (int seat in onlineSeats.All)
            {
                int place = reported < 0 || reported == seat ? 1 : 2;
                online.ReportTableFinished(seat, reported, place, table.Game.Material(Setup.SideOf(seat)));
            }
            return reported;
        }

        /// <summary>
        /// What every player at this device is worth as the game goes: the score the members are ranked by should the room
        /// end early, reported for each seat played here when it changes.
        /// </summary>
        private void ReportToRoom()
        {
            if (online == null || table == null || table.IsOver)
            {
                return;
            }
            foreach (int seat in onlineSeats.All)
            {
                int worth = table.Game.Material(Setup.SideOf(seat));
                if (worth != reportedWorth[seat])
                {
                    reportedWorth[seat] = worth;
                    online.ReportSeatScore(seat, worth);
                }
            }
        }

        // ------------------------------------------------------------------ what the server sends

        LockstepTable<BoardCommand> ILockstepHost<BoardCommand>.Table => IsOnlineGame ? table : null;

        /// <summary>The seat the people here act for now: the local seat to act, else the first played here (<see cref="LocalSeat"/>).</summary>
        int ILockstepHost<BoardCommand>.TableSeat => LocalSeat;

        string ILockstepHost<BoardCommand>.TurnCaption => OnlineCaption();

        /// <summary>An entry of the log was taken: the table has it already, in the order of the log; the game reacts as to a move here.</summary>
        void ILockstepHost<BoardCommand>.EntryTaken(LockstepEntry<BoardCommand> entry)
        {
            if (!IsOnlineGame || entry == null)
            {
                return;
            }
            BoardCommand command = entry.Kind == LockstepEntryKind.SeatToComputer
                ? BoardCommand.ToComputer(entry.Seat, table.LevelOf(entry.Seat))
                : entry.Command;
            AfterEntry(command, entry.Accepted, entry.Kind, entry.Made);
            ReportToRoom();
        }

        /// <summary>
        /// The room finished before the rules did (the host ended it, too few players): the game is called off here as
        /// it is on every other client, and the results show with the place the room gave.
        /// </summary>
        void ILockstepHost<BoardCommand>.RoomFinished()
        {
            if (!IsOnlineGame || table.IsOver)
            {
                return;
            }
            table.Game.CallOff(null);
            version++;
            WaitingForHuman = false;
            ClearSelection();
            ui?.Toast(L.T("The game was called off."));
            WriteResult();
            UpdateHighlights();
            RaiseChanged();
        }

        /// <summary>A move of the player at this device did not reach the log: the choice is theirs again.</summary>
        void ILockstepHost<BoardCommand>.CommandFailed()
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
