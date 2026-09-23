using System.Collections.Generic;
using Gamebox.Online;
using UnityEngine;

namespace Portfolio.Heroes
{
    /// <summary>
    /// The online controller: what is Heroes about a scenario played in step with the other players of a room. The
    /// shared <see cref="LockstepOnlineController{TCommand}"/> reads the table the server seated, takes the actions of
    /// the log to the <see cref="LockstepGame"/> and sends what the player at this device asks for; this class adds the
    /// maps a room can play and their options, hands the seats to the manager, and turns the requests of the interface
    /// (<see cref="IHeroesCommands"/>) into commands. Commands count when they come back in the log, in their place among
    /// everybody else's, so every client applies the same commands in the same order to the same map.
    /// </summary>
    public class HeroesOnlineController : LockstepOnlineController<GameCommand>, IHeroesCommands
    {
        public const string SizeOption = "size";
        public const string TreasureOption = "treasure";
        public const string MonstersOption = "monsters";

        /// <summary>Where the battles of the room are fought: 0 on the map where the armies meet, 1 on a battlefield of their own.</summary>
        public const string BattlesOption = "battles";

        private static readonly int[] TurnSeconds = { 0, 60, 120, 240, 480 };
        private static readonly string[] Sizes = { "0", "1", "2" };
        private static readonly string[] SizeLabels = { "Small", "Medium", "Large" };
        private static readonly string[] Amounts = { "1", "2", "3" };
        private static readonly string[] AmountLabels = { "Sparse", "Normal", "Rich" };
        private static readonly string[] Battles = { "0", "1" };
        private static readonly string[] BattleLabels = { "On the map", "Battlefield" };

        private RoomOptionSpec[] optionSpecs;
        private RoomLevelChoice[] levelChoices;

        public HeroesGameManager Heroes => BaseManager as HeroesGameManager;

        private HeroesGame Game => Heroes != null ? Heroes.Game : null;

        public override int MaxPlayersLimit => 4;

        public override int MinPlayersLimit => 2;

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
                if (campaign == null)
                {
                    // Asked before the manager has its campaign: nothing is kept, so the scenarios show once it has.
                    return choices;
                }
                foreach (int index in campaign.Skirmishes)
                {
                    HeroesLevel level = campaign.Scenario(index);
                    if (level != null)
                    {
                        choices.Add(new RoomLevelChoice(index, level.Title));
                    }
                }
                levelChoices = choices.ToArray();
                return levelChoices;
            }
        }

        public override IReadOnlyList<RoomOptionSpec> OptionSpecs => optionSpecs ??= new[]
        {
            new RoomOptionSpec(SizeOption, "Map size", Sizes, SizeLabels, 1),
            new RoomOptionSpec(BattlesOption, "Battles", Battles, BattleLabels, (int)BattleStyle.Battlefield),
            ClockSpec("Turn clock", TurnSeconds, 2),
            ComputersSpec("Computer players", 3),
            ComputerLevelSpec("Computer skill"),
            new RoomOptionSpec(TreasureOption, "Treasure", Amounts, AmountLabels, 1),
            new RoomOptionSpec(MonstersOption, "Wandering armies", Amounts, AmountLabels, 1)
        };

        /// <summary>The map and where its battles are fought; the lobby adds the players itself.</summary>
        public override string DescribeRoom(RoomInfo room)
        {
            int size = room.Option(SizeOption, 1);
            string map = room.Level >= 0 ? base.DescribeRoom(room) : SizeLabels[Mathf.Clamp(size, 0, SizeLabels.Length - 1)] + " map";
            string battles = BattleStyleOf(room) == BattleStyle.Battlefield ? "battlefield" : "battles on the map";
            return $"{map}, {battles}";
        }

        /// <summary>Where the battles of a room are fought: on a battlefield unless the room says otherwise.</summary>
        public static BattleStyle BattleStyleOf(RoomInfo room)
        {
            return (BattleStyle)Mathf.Clamp(room.Option(BattlesOption, (int)BattleStyle.Battlefield), 0, 1);
        }

        // ------------------------------------------------------------------ the room

        /// <summary>The server seated the table: every client hands the manager the same seats and lays out the same map.</summary>
        protected override bool PrepareTable(LockstepSetup setup)
        {
            HeroesGameManager manager = Heroes;
            if (manager == null)
            {
                return false;
            }
            RoomInfo room = setup.Room;
            var table = new HeroesGameManager.OnlineTable
            {
                Seed = setup.Seed,
                Scenario = setup.Level,
                LocalSeat = setup.TableSeat,
                MapSize = room.Option(SizeOption, 1),
                Treasure = Mathf.Clamp(room.Option(TreasureOption, 2), 1, 3),
                Monsters = Mathf.Clamp(room.Option(MonstersOption, 2), 1, 3),
                BattleStyle = BattleStyleOf(room)
            };
            foreach (RoomSeatInfo seat in setup.Seats)
            {
                table.Seats.Add(new PlayerSpec
                {
                    human = seat.IsHuman,
                    name = seat.Name,
                    faction = (Faction)Mathf.Clamp(seat.Look, 0, 2),
                    aiLevel = Mathf.Clamp(seat.BotLevel, 0, 2),
                    team = table.Seats.Count
                });
            }
            manager.PrepareOnlineGame(table);
            return true;
        }

        // ------------------------------------------------------------------ the log

        protected override GameCommand Parse(byte seat, uint kind, string payload)
        {
            return GameCommand.Parse(seat, kind, payload);
        }

        protected override void Encode(GameCommand command, out byte seat, out uint kind, out string payload)
        {
            seat = command.Seat;
            kind = (uint)command.kind;
            payload = command.Payload();
        }

        /// <summary>Sends a command of the player at this device; it counts when it comes back in the log.</summary>
        public bool Submit(GameCommand command)
        {
            return command != null && Heroes != null && command.player == Heroes.LocalSeat && Submit(command, true);
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

        public void BattleAttack(int stackId, int targetStack, int fromCell)
        {
            Do(CommandKind.BattleAttack, stackId, targetStack, fromCell);
        }

        public void BattleShoot(int stackId, int targetStack)
        {
            Do(CommandKind.BattleShoot, stackId, targetStack);
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
