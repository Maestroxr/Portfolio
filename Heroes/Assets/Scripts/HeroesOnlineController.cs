using System.Collections.Generic;
using System.Linq;
using Gamebox;
using Gamebox.Online;
using Portfolio.Heroes.Server;
using UnityEngine;

namespace Portfolio.Heroes
{
    /// <summary>
    /// The online controller: it reads the table the server seated (Server/Lib.cs, one <c>HeroesSeat</c> row per
    /// seat), hands it to the manager, and sends everything the player at this device asks for to the action log of
    /// the server. Commands count when they come back in the log, in their place among everybody else's, so every
    /// client applies the same commands in the same order to the same map.
    /// </summary>
    public class HeroesOnlineController : OnlineGameController, IHeroesCommands
    {
        public const string TurnOption = "turn";
        public const string ComputersOption = "bots";
        public const string LevelOption = "botlevel";
        public const string SizeOption = "size";
        public const string TreasureOption = "treasure";
        public const string MonstersOption = "monsters";

        /// <summary>A human seat, as the server marks it.</summary>
        private const byte HumanSeat = 0;

        /// <summary>The seat the wandering armies of the wilds act under, which only the host may send for.</summary>
        private const byte WildsSeat = 254;

        private static readonly string[] TurnSeconds = { "0", "60", "120", "240", "480" };
        private static readonly string[] TurnLabels = { "No clock", "1 minute", "2 minutes", "4 minutes", "8 minutes" };
        private static readonly string[] Computers = { "0", "1", "2", "3" };
        private static readonly string[] ComputerLabels = { "None", "Up to 1", "Up to 2", "Up to 3" };
        private static readonly string[] Levels = { "0", "1", "2" };
        private static readonly string[] LevelLabels = { "Easy", "Normal", "Hard" };
        private static readonly string[] Sizes = { "0", "1", "2" };
        private static readonly string[] SizeLabels = { "Small", "Medium", "Large" };
        private static readonly string[] Amounts = { "1", "2", "3" };
        private static readonly string[] AmountLabels = { "Sparse", "Normal", "Rich" };

        /// <summary>Seconds a command of this device may be on its way before the choice is offered again.</summary>
        private const float EchoPatience = 5f;

        private RoomOptionSpec[] optionSpecs;
        private RoomLevelChoice[] levelChoices;
        private bool awaitingEcho;
        private float echoDeadline;

        public HeroesGameManager Heroes => BaseManager as HeroesGameManager;

        private HeroesGame Game => Heroes != null ? Heroes.Game : null;

        private GameServerClient Client => Server as GameServerClient;

        public override int MaxPlayersLimit => 4;

        public override int MinPlayersLimit => 2;

        /// <summary>Whether a command of this device is still on its way to the log.</summary>
        public bool AwaitingEcho => awaitingEcho && Time.unscaledTime < echoDeadline;

        public override IReadOnlyList<RoomLevelChoice> LevelChoices
        {
            get
            {
                if (levelChoices != null)
                {
                    return levelChoices;
                }
                var choices = new List<RoomLevelChoice> { new RoomLevelChoice(-1, "Random Map") };
                HeroesCampaign campaign = Heroes != null ? Heroes.Campaign as HeroesCampaign : null;
                if (campaign != null)
                {
                    foreach (int index in campaign.Skirmishes)
                    {
                        HeroesLevel level = campaign.Scenario(index);
                        if (level != null)
                        {
                            choices.Add(new RoomLevelChoice(index, level.Title));
                        }
                    }
                }
                levelChoices = choices.ToArray();
                return levelChoices;
            }
        }

        public override IReadOnlyList<RoomOptionSpec> OptionSpecs => optionSpecs ??= new[]
        {
            new RoomOptionSpec(SizeOption, "Map size", Sizes, SizeLabels, 1),
            new RoomOptionSpec(TurnOption, "Turn clock", TurnSeconds, TurnLabels, 2),
            new RoomOptionSpec(ComputersOption, "Computer players", Computers, ComputerLabels, 0),
            new RoomOptionSpec(LevelOption, "Computer skill", Levels, LevelLabels, 1),
            new RoomOptionSpec(TreasureOption, "Treasure", Amounts, AmountLabels, 1),
            new RoomOptionSpec(MonstersOption, "Wandering armies", Amounts, AmountLabels, 1)
        };

        public override string DescribeRoom(RoomInfo room)
        {
            int size = room.Option(SizeOption, 1);
            string map = room.Level >= 0 ? "Scenario" : SizeLabels[Mathf.Clamp(size, 0, SizeLabels.Length - 1)] + " map";
            return $"{map}, {Server.MemberCount(room.Id)}/{room.MaxPlayers} players";
        }

        public override string TurnCaption(RoomTurnInfo turn)
        {
            string caption = Heroes != null ? Heroes.OnlineCaption() : "";
            return string.IsNullOrEmpty(caption) ? base.TurnCaption(turn) : caption;
        }

        /// <summary>The seats of a room are the game's own table, so the client has to ask for them.</summary>
        protected override IEnumerable<string> RoomQueries(RoomInfo room)
        {
            yield return $"SELECT * FROM heroes_seat WHERE room_id = {room.Id}";
        }

        // ------------------------------------------------------------------ the room

        /// <summary>The room started: every client seats the same table and lays out the same map.</summary>
        protected override void OnRoomStarted(RoomInfo room)
        {
            HeroesGameManager manager = Heroes;
            if (manager == null)
            {
                Report("The game is not set up for online play.");
                return;
            }
            var table = new HeroesGameManager.OnlineTable
            {
                Seed = room.Seed,
                Scenario = room.Level,
                MapSize = room.Option(SizeOption, 1),
                Treasure = Mathf.Clamp(room.Option(TreasureOption, 2), 1, 3),
                Monsters = Mathf.Clamp(room.Option(MonstersOption, 2), 1, 3)
            };
            DbConnection connection = Client != null ? Client.Connection : null;
            if (connection == null)
            {
                Report("The game is not connected to its server.");
                return;
            }
            // The server seated the table; every client reads the same rows and lays out the same map from them.
            foreach (HeroesSeat seat in connection.Db.HeroesSeat.RoomId.Filter(room.Id).OrderBy(row => row.Seat))
            {
                if (seat.Kind == HumanSeat && Server.Identity.HasValue && seat.Player == Server.Identity.Value)
                {
                    table.LocalSeat = table.Seats.Count;
                }
                table.Seats.Add(new PlayerSpec
                {
                    human = seat.Kind == HumanSeat,
                    name = seat.Name,
                    faction = (Faction)Mathf.Clamp(seat.Faction, 0, 2),
                    aiLevel = Mathf.Clamp(seat.BotLevel, 0, 2),
                    team = table.Seats.Count
                });
            }
            if (table.Seats.Count < 2 || table.LocalSeat < 0)
            {
                Report("The table of the game did not arrive.");
                LeaveMatch();
                return;
            }
            awaitingEcho = false;
            manager.PrepareOnlineGame(table);
            base.OnRoomStarted(room);
            if (!manager.IsOnlineGame)
            {
                LeaveMatch();
            }
        }

        /// <summary>Every action of the log goes to the game, this device's own included.</summary>
        protected override void OnActionReceived(RoomActionInfo action)
        {
            HeroesGameManager manager = Heroes;
            if (!InCharge || manager == null)
            {
                return;
            }
            if (action.Seat == manager.LocalSeat && Server.Identity.HasValue && action.Sender == Server.Identity.Value)
            {
                awaitingEcho = false;
            }
            manager.OnlineActionArrived(action.Seat, action.Kind, action.Payload, action.Random, action.IsTimeout);
        }

        protected override void OnRoomFinished(RoomInfo room)
        {
            if (InCharge)
            {
                Heroes?.OnlineRoomFinished();
            }
        }

        internal void ReportScore(int worth)
        {
            if (IsPlaying)
            {
                Server.ReportScore(worth);
            }
        }

        internal void ReportFinished(int worth)
        {
            if (IsPlaying)
            {
                Server.FinishPlaying(worth);
            }
        }

        // ------------------------------------------------------------------ sending commands

        /// <summary>The host's client sends the move it worked out for a computer seat.</summary>
        internal void PlayComputer(GameCommand command)
        {
            if (IsPlaying && IsHost && command != null)
            {
                Send(command, false);
            }
        }

        /// <summary>Sends a command of the player at this device; it counts when it comes back in the log.</summary>
        public bool Submit(GameCommand command)
        {
            if (!IsPlaying || command == null || AwaitingEcho || Heroes == null || command.player != Heroes.LocalSeat)
            {
                return false;
            }

            awaitingEcho = true;
            echoDeadline = Time.unscaledTime + EchoPatience;
            Send(command, true);
            return true;
        }

        private void Send(GameCommand command, bool awaited)
        {
            // The armies of the wilds have no seat of their own; the payload still says who acted.
            byte seat = command.player >= 0 ? (byte)command.player : WildsSeat;
            Server.SubmitAction(seat, (uint)command.kind, command.Payload(), error =>
            {
                if (error == null)
                {
                    return;
                }
                Report(error);
                if (awaited)
                {
                    awaitingEcho = false;
                    Heroes?.OnlineCommandFailed();
                }
            });
        }

        /// <summary>Whether the game is waiting for the player at this device right now.</summary>
        private bool Ready => IsPlaying && Heroes != null && Heroes.OnlineDeciding() && !AwaitingEcho;

        private int Seat => Heroes != null ? Heroes.LocalSeat : -1;

        private void Do(CommandKind kind, int a = 0, int b = 0, int c = 0, int d = 0, int e = 0)
        {
            if (Ready)
            {
                Submit(GameCommand.Of(kind, Seat, a, b, c, d, e));
            }
        }

        // ------------------------------------------------------------------ the adventure map

        public void MoveHero(int heroId, int cell)
        {
            Do(CommandKind.MoveHero, heroId, cell);
        }

        public void EndTurn()
        {
            Do(CommandKind.EndTurn);
        }

        public void Build(int townId, BuildingId building)
        {
            Do(CommandKind.Build, townId, (int)building);
        }

        public void Recruit(int townId, int tier, int count, bool toHero)
        {
            Do(CommandKind.Recruit, townId, tier, count, toHero ? 1 : 0);
        }

        public void RecruitAt(int objectId, int heroId, int count)
        {
            Do(CommandKind.Recruit, objectId, 0, count, 2, heroId);
        }

        public void Hire(int townId, int slot)
        {
            Do(CommandKind.HireHero, townId, slot);
        }

        public void MoveArmy(int from, int fromSlot, int to, int toSlot, int count)
        {
            Do(CommandKind.MoveArmy, from, fromSlot, to, toSlot, count);
        }

        public void Trade(ResourceKind give, ResourceKind take, int amount)
        {
            Do(CommandKind.Trade, (int)give, (int)take, amount);
        }

        public void Choose(int option)
        {
            GameState state = Game?.State;
            if (state != null && state.pending.Count > 0 && state.pending[0].player == Seat)
            {
                Submit(GameCommand.Of(CommandKind.Choose, Seat, option));
            }
        }

        public void Dismiss(int holder, int slot)
        {
            Do(CommandKind.DismissStack, holder, slot);
        }

        public void Sleep(int heroId, bool sleeping)
        {
            Do(CommandKind.SleepHero, heroId, sleeping ? 1 : 0);
        }

        // ------------------------------------------------------------------ battle

        public void BattleMove(int stackId, int cell)
        {
            Do(CommandKind.BattleMove, stackId, cell);
        }

        public void BattleAttack(int stackId, int targetCell, int fromCell)
        {
            Do(CommandKind.BattleAttack, stackId, targetCell, fromCell);
        }

        public void BattleShoot(int stackId, int targetCell)
        {
            Do(CommandKind.BattleShoot, stackId, targetCell);
        }

        public void BattleWait(int stackId)
        {
            Do(CommandKind.BattleWait, stackId);
        }

        public void BattleDefend(int stackId)
        {
            Do(CommandKind.BattleDefend, stackId);
        }

        public void BattleCast(SpellId spell, int cell)
        {
            Do(CommandKind.BattleCast, (int)spell, cell);
        }

        public void BattleRetreat()
        {
            Do(CommandKind.BattleRetreat);
        }
    }
}
