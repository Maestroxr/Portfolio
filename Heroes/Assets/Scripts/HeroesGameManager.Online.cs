using System.Collections;
using System.Collections.Generic;
using Gamebox;
using Portfolio.Heroes.UI;
using UnityEngine;

namespace Portfolio.Heroes
{
    /// <summary>
    /// The online half of the manager: a scenario played in step with the other players of a room
    /// (<see cref="HeroesOnlineController"/>). Every client lays out the same map from the seed of the room and runs a
    /// <see cref="LockstepGame"/> that nothing but the action log of the server changes. What arrives is applied at
    /// once, so the rules may run ahead of the map, which plays the events out as it does in a game at one device. The
    /// director offers a decision only to the player at this device; the computer seats are moved by the host's client,
    /// through the log like everybody else. There is no saving and no pausing, and the end leads back to the room.
    /// </summary>
    public partial class HeroesGameManager
    {
        /// <summary>What the online controller hands over before a scenario of the room starts.</summary>
        internal sealed class OnlineTable
        {
            /// <summary>One entry per seat, in the order the server seated them.</summary>
            public List<PlayerSpec> Seats = new List<PlayerSpec>();
            /// <summary>The seat of the player at this device.</summary>
            public int LocalSeat = -1;
            /// <summary>The seed of the room: every client lays out the same map from it.</summary>
            public uint Seed;
            /// <summary>The scenario of the room, or -1 for a map made up from the room's options.</summary>
            public int Scenario = -1;
            public int MapSize = 1;
            public int Treasure = 2;
            public int Monsters = 2;
        }

        /// <summary>How much faster the map plays while the rules are ahead of it.</summary>
        private const float CatchUpPace = 1.7f;

        /// <summary>Seconds the host waits for a move it sent for a computer seat before it thinks again.</summary>
        private const float ComputerPatience = 4f;

        [SerializeField] private HeroesOnlineController online;

        private OnlineTable onlineTable;
        private LockstepGame lockstep;
        private int localSeat = -1;
        /// <summary>Counts what arrived from the server: the director looks at the game again when it changes.</summary>
        private int onlineVersion;
        private int reportedDay = -1;
        private bool finishReported;

        public HeroesOnlineController Online => online;

        /// <summary>Whether the scenario is played online, in step with the other players of a room.</summary>
        public bool IsOnlineGame => lockstep != null;

        public int LocalSeat => localSeat;

        internal LockstepGame Lockstep => lockstep;

        /// <summary>The map of an online game plays faster while the rules are ahead of it.</summary>
        private float OnlinePace => lockstep != null && Game != null && Game.HasEvents ? CatchUpPace : 1f;

        /// <summary>The online button of the title screen: opens the lobby of the game's server.</summary>
        public void OpenOnline()
        {
            if (online == null)
            {
                UI?.UpdateError("Online play is not set up in this scene.");
                return;
            }
            online.OpenLobby();
        }

        // ------------------------------------------------------------------ starting and ending

        internal void PrepareOnlineGame(OnlineTable table)
        {
            onlineTable = table;
        }

        /// <summary>Lays out the map of the room and starts the scenario, exactly as every other client does.</summary>
        private void StartOnlineGame()
        {
            OnlineTable table = onlineTable;
            onlineTable = null;
            if (table == null || table.Seats.Count < 2 || table.LocalSeat < 0)
            {
                UI?.UpdateError("The table of the online game did not arrive.");
                return;
            }
            MapSpec spec = OnlineMap(table);
            GameState state = MapGenerator.Generate(spec);
            lockstep = new LockstepGame(state);
            lockstep.Game.Begin();
            localSeat = table.LocalSeat;
            onlineVersion = 0;
            reportedDay = -1;
            finishReported = false;
            Direct(lockstep.Game, true);
            Viewer = localSeat;
            Map.Fog.Show(state.Player(localSeat));
            HeroesUI.Refresh();
        }

        /// <summary>The map of the room: the scenario it was opened on, or one made up from its options and seed.</summary>
        private MapSpec OnlineMap(OnlineTable table)
        {
            HeroesLevel scenario = table.Scenario >= 0 && campaign != null ? campaign.Scenario(table.Scenario) : null;
            MapSpec spec = scenario != null ? scenario.Map.Clone() : new MapSpec();
            if (scenario == null)
            {
                Vector2Int size = HeroesSettings.MapDimensions(table.MapSize);
                spec.columns = size.x;
                spec.rows = size.y;
                spec.treasure = table.Treasure;
                spec.monsters = table.Monsters;
                spec.name = "Skirmish";
            }
            spec.seed = table.Seed;
            spec.players.Clear();
            foreach (PlayerSpec seat in table.Seats)
            {
                spec.players.Add(seat.Clone());
            }
            return spec;
        }

        /// <summary>The session is over (the room was left, the connection dropped): the table is cleared.</summary>
        protected override void OnSessionChanged()
        {
            if (InSession || lockstep == null && onlineTable == null)
            {
                return;
            }
            StopDirecting();
            lockstep = null;
            onlineTable = null;
            localSeat = -1;
            Game = null;
            if (Map != null)
            {
                Destroy(Map.gameObject);
                Map = null;
            }
            ui.CloseAll();
            sound?.PlayMenuMusic();
        }

        private void LeaveOnline()
        {
            lockstep = null;
            localSeat = -1;
        }

        // ------------------------------------------------------------------ what the server sends

        /// <summary>An action of the log arrived: it counts now, in the order the log put it in.</summary>
        internal void OnlineActionArrived(int seat, uint kind, string payload, uint stamp, bool timeout)
        {
            if (lockstep == null || Game == null)
            {
                return;
            }
            if (timeout)
            {
                GameCommand made = lockstep.Timeout(stamp);
                if (made != null)
                {
                    ui.Log($"Time is up for {NameOfSeat(made.player)}.", made.player);
                }
            }
            else
            {
                GameCommand command = GameCommand.Parse(payload);
                if (command != null && lockstep.Apply(command, stamp) && command.kind == CommandKind.SeatToComputer)
                {
                    ui.Log($"{NameOfSeat(command.player)} left. The computer plays on.", command.player);
                }
            }
            onlineVersion++;
            ReportToRoom();
        }

        private string NameOfSeat(int seat)
        {
            PlayerState player = Game?.State.Player(seat);
            return player != null ? player.name : $"Seat {seat + 1}";
        }

        /// <summary>The room learns how the player at this device is doing, and that they are done once it is over.</summary>
        private void ReportToRoom()
        {
            if (online == null || Game == null || localSeat < 0)
            {
                return;
            }
            PlayerState me = Game.State.Player(localSeat);
            int worth = me != null ? Worth(me) : 0;
            if (Game.IsOver)
            {
                if (!finishReported)
                {
                    finishReported = true;
                    online.ReportFinished(worth);
                }
            }
            else if (Game.State.day != reportedDay)
            {
                reportedDay = Game.State.day;
                online.ReportScore(worth);
            }
        }

        /// <summary>What a player is worth, for the ranking of the room: their lands, armies and treasury.</summary>
        private int Worth(PlayerState player)
        {
            int worth = player.resources.Gold / 20 + player.towns.Count * 500;
            foreach (int id in player.heroes)
            {
                HeroState hero = Game.State.Hero(id);
                if (hero != null && hero.alive)
                {
                    worth += 100 + hero.level * 50 + Game.Strength(hero) / 8;
                }
            }
            return worth;
        }

        /// <summary>The room finished before the scenario did: the game ends here as it does on every other client.</summary>
        internal void OnlineRoomFinished()
        {
            if (lockstep == null || Game == null || Game.IsOver)
            {
                return;
            }
            ui.Log("The game was called off.", -1);
            Game.State.over = true;
            onlineVersion++;
        }

        /// <summary>A command of the player at this device did not reach the log: the choice is theirs again.</summary>
        internal void OnlineCommandFailed()
        {
            sound?.Play(Sfx.Error, 0.7f);
            onlineVersion++;
        }

        /// <summary>Who the game waits for, for the clock of the lobby.</summary>
        internal string OnlineCaption()
        {
            if (lockstep == null || Game == null)
            {
                return "";
            }
            int seat = Game.WaitingPlayer;
            if (seat < 0 || Game.IsOver)
            {
                return "Game over";
            }
            return seat == localSeat ? Game.InBattle ? "Your move" : "Your turn" : $"{NameOfSeat(seat)}'s turn";
        }

        /// <summary>Whether the player at this device is the one the rules are waiting for, and the map has caught up.</summary>
        internal bool OnlineDeciding()
        {
            return lockstep != null && IsGameRunning && WaitingForHuman && !Game.HasEvents &&
                   Game.WaitingPlayer == localSeat;
        }

        private bool SendOnline(GameCommand command)
        {
            return online != null && online.Submit(command);
        }

        // ------------------------------------------------------------------ directing

        private IEnumerator DirectOnline()
        {
            yield return null;
            while (Game != null && lockstep != null)
            {
                if (!IsGameRunning)
                {
                    yield return null;
                    continue;
                }
                if (Game.HasEvents)
                {
                    WaitingForHuman = false;
                    ui.SetBusy(true);
                    foreach (GameEvent what in Game.TakeEvents())
                    {
                        yield return Play(what);
                    }
                    ui.SetBusy(false);
                    ui.Refresh();
                    continue;
                }
                if (Game.IsOver)
                {
                    yield return Finish();
                    director = null;
                    yield break;
                }
                int seat = Game.WaitingPlayer;
                // The wandering armies of a battle have no seat; the host moves them like a computer player.
                if (seat < 0 && !(seat == -1 && Game.InBattle))
                {
                    yield return null;
                    continue;
                }
                int version = onlineVersion;
                bool host = online != null && online.IsHost;
                float patience = float.PositiveInfinity;

                if (Game.IsComputer(seat))
                {
                    if (host)
                    {
                        yield return new WaitForSeconds((Game.InBattle ? 0.25f : 0.12f) / Speed);
                        if (!Same(version))
                        {
                            continue;
                        }
                        GameCommand move = Game.InBattle ? BattleAI.Next(Game) : brain.Next(Game, seat);
                        move ??= GameCommand.Of(Game.InBattle ? CommandKind.BattleDefend : CommandKind.EndTurn, seat,
                            Game.InBattle ? Game.Battle.current : 0);
                        online.PlayComputer(move);
                        patience = Time.unscaledTime + ComputerPatience;
                    }
                }
                else if (seat == localSeat)
                {
                    if (Game.State.pending.Count > 0)
                    {
                        ui.ShowChoice(Game.State.pending[0]);
                    }
                    else if (Game.InBattle)
                    {
                        ui.ShowBattleTurn(Game.Battle);
                    }
                    else
                    {
                        ui.ShowTurn(seat);
                    }
                    WaitingForHuman = true;
                }
                else
                {
                    // Somebody else is thinking: nothing on this screen is theirs to press.
                    WaitingForHuman = false;
                    ui.SetBusy(false);
                }

                while (Same(version) && host == (online != null && online.IsHost) && Time.unscaledTime < patience)
                {
                    yield return null;
                }
                WaitingForHuman = false;
            }
            director = null;
        }

        /// <summary>Whether nothing arrived since the director last looked.</summary>
        private bool Same(int version)
        {
            return Game != null && lockstep != null && version == onlineVersion && !Game.HasEvents && IsGameRunning;
        }
    }
}
