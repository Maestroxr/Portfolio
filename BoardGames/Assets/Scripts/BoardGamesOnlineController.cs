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
    /// the log: the decisions of the person here are sent there instead of into the rules (<see cref="IBoardCommands"/>),
    /// the host's client sends the computer's, and every action is applied when it arrives, in the order of the server.
    /// </summary>
    public sealed class BoardGamesOnlineController : LockstepOnlineController<BoardCommand>, IBoardCommands
    {
        private static readonly int[] TurnSeconds = { 0, 30, 60, 120, 300 };

        /// <summary>The room option of the backgammon doubling cube: "1" plays with it (the default), "0" without.</summary>
        public const string CubeOption = "cube";

        private RoomOptionSpec[] optionSpecs;

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
        /// campaign: nothing to unlock.
        /// </summary>
        protected override void RecordOnlineResult(RoomInfo room, RoomMemberInfo me)
        {
            int first = 0;
            foreach (RoomMemberInfo member in Server.Members)
            {
                if (member.Place == 1)
                {
                    first++;
                }
            }
            bool drawn = first > 1;
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
        /// The server seated the table: the manager gets it, then loads the rule set of the room and starts the game.
        /// Every new game of a room swaps who plays the side that moves first (White in chess and backgammon, where the
        /// opening roll then decides who starts).
        /// </summary>
        protected override bool PrepareTable(LockstepSetup setup)
        {
            BoardGamesManager manager = Games;
            if (manager == null || setup.Seats.Count < 2)
            {
                return false;
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
            manager.PrepareOnlineTable(table, setup.TableSeat, setup.Seed, saved);
            return true;
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

        // ------------------------------------------------------------------ what the player may do

        private bool MaySend(int seat)
        {
            return IsPlaying && seat == TableSeat && !AwaitingEcho && Games != null && Games.Table != null && !Games.Table.IsOver;
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
