using System.Collections.Generic;
using Gamebox;
using Gamebox.Online;
using UnityEngine;

namespace Portfolio.Checkers
{
    /// <summary>
    /// Online controller of the Checkers module: games with a player on another device, in a room of the game's
    /// SpacetimeDB database. Rooms, ready and start, the action log and its clock, the seats of the table and taking the
    /// log to the table come from the shared <see cref="LockstepOnlineController{TCommand}"/> and the base server; this
    /// class adds what is Checkers. A room plays one rule set (its level), against a clock, with the computer in the
    /// second seat when the host asks for an opponent. The server seats the table (Server/Lib.cs), every client starts
    /// the same game from it (CheckersGameManager.Online.cs), and from then on the table changes only by the actions of
    /// the log: the moves of the person here are sent there instead of into the rules (<see cref="ICheckersCommands"/>),
    /// the host's client sends the moves of the computer, and every action is applied when it arrives, in the order of
    /// the server.
    /// </summary>
    public sealed class CheckersOnlineController : LockstepOnlineController<CheckersCommand>, ICheckersCommands
    {
        private static readonly int[] TurnSeconds = { 0, 30, 60, 120, 300 };

        private RoomOptionSpec[] optionSpecs;

        public CheckersGameManager Checkers => BaseManager as CheckersGameManager;

        public override int MaxPlayersLimit => 2;

        public override int MinPlayersLimit => 2;

        /// <summary>The rule sets a room can play: one level per variant.</summary>
        public override IReadOnlyList<RoomLevelChoice> LevelChoices
        {
            get
            {
                var choices = new List<RoomLevelChoice>();
                for (int i = 0; i < VariantRules.All.Length; i++)
                {
                    choices.Add(new RoomLevelChoice(i, L.T(VariantRules.Of(VariantRules.All[i]).NameKey)));
                }
                return choices;
            }
        }

        public override IReadOnlyList<RoomOptionSpec> OptionSpecs => optionSpecs ??= new[]
        {
            RoomOptionSpec.Clock("Time to move", TurnSeconds, 2),
            RoomOptionSpec.Computers("Computer opponent", 1),
            RoomOptionSpec.ComputerLevel("Computer level")
        };

        public override string DescribeRoom(RoomInfo room)
        {
            int computers = room.Option(RoomOptions.ComputersOption, 0);
            return computers > 0 ? L.F("{0}, computer opponent", base.DescribeRoom(room)) : base.DescribeRoom(room);
        }

        // ------------------------------------------------------------------ the room

        /// <summary>The server seated the table: the manager gets it, then loads the rule set of the room and starts the game.</summary>
        protected override bool PrepareTable(LockstepSetup setup)
        {
            CheckersGameManager manager = Checkers;
            if (manager == null || setup.Seats.Count < 2)
            {
                return false;
            }
            var table = new TableSetup
            {
                variant = Mathf.Clamp(setup.Level, 0, VariantRules.All.Length - 1),
                // Another player opens every game of a room.
                firstSeat = (int)((setup.Round > 0 ? setup.Round - 1 : 0) % 2),
                clockSeconds = 0
            };
            for (int i = 0; i < 2; i++)
            {
                RoomSeatInfo seat = setup.Seats[i];
                table.seats[i] = new TableSeat
                {
                    name = seat.Name,
                    computer = !seat.IsHuman,
                    level = Mathf.Clamp(seat.BotLevel, 0, CheckersAI.Levels - 1)
                };
            }
            SaveData saved = setup.Save != null ? setup.Save.Read<SaveData>() : null;
            manager.PrepareOnlineTable(table, setup.TableSeat, setup.Seed, saved);
            return true;
        }

        /// <summary>The two seats of a saved game, for a room opened from it (null when it is no game of Checkers).</summary>
        protected override IReadOnlyList<SavedSeatInfo> SavedSeats(SavedGame save)
        {
            SaveData data = save != null ? save.Read<SaveData>() : null;
            if (data == null || data.setup == null || data.setup.seats == null || data.setup.seats.Length != 2 || data.game == null)
            {
                return null;
            }
            var seats = new List<SavedSeatInfo>();
            for (int i = 0; i < data.setup.seats.Length; i++)
            {
                TableSeat seat = data.setup.seats[i] ?? new TableSeat();
                seats.Add(new SavedSeatInfo(seat.name, !seat.computer, (byte)i, (byte)Mathf.Clamp(seat.level, 0, CheckersAI.Levels - 1)));
            }
            return seats;
        }

        // ------------------------------------------------------------------ the log

        /// <summary>
        /// The command of an action. The rules' own <see cref="CommandKind.SeatToComputer"/> is not one a player can send:
        /// the server says it with an action of its own.
        /// </summary>
        protected override CheckersCommand Parse(byte seat, uint kind, string payload)
        {
            return kind < (uint)CommandKind.SeatToComputer ? CheckersCommand.Parse(seat, (CommandKind)kind, payload) : null;
        }

        protected override void Encode(CheckersCommand command, out byte seat, out uint kind, out string payload)
        {
            seat = (byte)command.seat;
            kind = (uint)command.kind;
            payload = command.Payload();
        }

        // ------------------------------------------------------------------ what the player may do

        private bool MaySend(int seat)
        {
            return IsPlaying && seat == TableSeat && !AwaitingEcho && Checkers != null && Checkers.Table != null && !Checkers.Table.IsOver;
        }

        public bool Move(int seat, Move move)
        {
            if (move == null || !MaySend(seat) || Checkers.Table.SeatToMove != seat)
            {
                return false;
            }
            return Submit(CheckersCommand.MoveAlong(seat, move), true);
        }

        public bool Resign(int seat)
        {
            if (!MaySend(seat))
            {
                return false;
            }
            return Submit(CheckersCommand.Resign(seat), true);
        }
    }
}
