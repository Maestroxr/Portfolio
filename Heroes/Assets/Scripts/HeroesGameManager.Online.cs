using System.Collections;
using System.Collections.Generic;
using Gamebox;
using Gamebox.Lockstep;
using Gamebox.Online;
using Portfolio.Heroes.UI;
using UnityEngine;

namespace Portfolio.Heroes
{
    /// <summary>
    /// The online half of the manager: a scenario played in step with the other players of a room
    /// (<see cref="HeroesOnlineController"/>). Every client lays out the same map from the seed of the room and runs a
    /// <see cref="LockstepGame"/> that nothing but the action log of the server changes: the controller takes every action
    /// to it and tells the manager (<see cref="ILockstepHost{TCommand}"/>). What arrives is applied at once, so the rules
    /// may run ahead of the map, which plays the events out as it does in a game at one device. The director offers a
    /// decision only to the player at this device; the computer seats and the wandering armies are moved by the host's
    /// client, through the log like everybody else. Battles the player at this device does not fight are told, not
    /// played out (<see cref="IsLocalBattle"/>). There is no saving and no pausing, the campaign at this device is left
    /// alone, and the end leads back to the room.
    /// </summary>
    public partial class HeroesGameManager : ILockstepHost<GameCommand>
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
            /// <summary>Where the battles of the room are fought.</summary>
            public BattleStyle BattleStyle = BattleStyle.Battlefield;
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
        /// <summary>The room ended the game before its rules did.</summary>
        private bool calledOff;
        /// <summary>The level of the title screen while a session plays the level of its room.</summary>
        private int levelBeforeSession = -1;
        private bool sessionLevel;

        public HeroesOnlineController Online => online;

        /// <summary>Whether the scenario is played online, in step with the other players of a room.</summary>
        public bool IsOnlineGame => lockstep != null;

        public int LocalSeat => localSeat;

        /// <summary>
        /// Whether the battle the map is playing out (from its BattleStarted event to its BattleEnded) is fought by the
        /// player at this device: always at one device, and online when they attack or defend in it. The battles of the
        /// others at the table are not played out here: the director tells them in the log and moves on, so a
        /// client that only watches never holds the table up.
        /// </summary>
        public bool IsLocalBattle { get; private set; } = true;

        internal LockstepGame Lockstep => lockstep;

        protected override IOnlineLobby OnlineLobby => online;

        /// <summary>The map of an online game plays faster while the rules are ahead of it.</summary>
        private float OnlinePace => lockstep != null && Game != null && Game.HasEvents ? CatchUpPace : 1f;

        /// <summary>Whether the player at this device fights the battle that <paramref name="started"/> (its BattleStarted event) begins.</summary>
        public bool TakesPart(GameEvent started)
        {
            if (!IsOnlineGame || started == null)
            {
                return true;
            }
            int attacker = started.battle != null ? started.battle.attackerPlayer : started.player;
            int defender = started.battle != null ? started.battle.defenderPlayer : started.b;
            return attacker == localSeat || defender == localSeat;
        }

        // ------------------------------------------------------------------ starting and ending

        internal void PrepareOnlineGame(OnlineTable table)
        {
            onlineTable = table;
        }

        /// <summary>
        /// The level of the room: a scenario of the campaign, or -1 for a map made up from the room's seed, which is no
        /// chapter of it. The title screen gets its own level back when the session ends, so its saved game goes on.
        /// </summary>
        private void LoadSessionLevel(int level)
        {
            if (!sessionLevel)
            {
                sessionLevel = true;
                levelBeforeSession = LevelIndex;
            }
            LevelIndex = campaign != null && level >= 0 && level < campaign.Count ? level : -1;
            CurrentLevel = LevelData.Create(LevelIndex);
            UpdateLevel();
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
            calledOff = false;
            IsLocalBattle = true;
            // The map is seen from the seat of the player at this device, and the camera starts on their first hero.
            Direct(lockstep.Game, true);
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
            // Part of the rules, so part of the state every client generates: the room decides it, not the settings here.
            spec.rules.battleStyle = table.BattleStyle;
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
            if (!InSession && sessionLevel)
            {
                sessionLevel = false;
                LevelIndex = levelBeforeSession;
                CurrentLevel = LevelData.Create(LevelIndex);
            }
            if (InSession || lockstep == null && onlineTable == null)
            {
                return;
            }
            StopDirecting();
            CloseBattleNow(false);
            lockstep = null;
            onlineTable = null;
            localSeat = -1;
            IsLocalBattle = true;
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
            IsLocalBattle = true;
        }

        /// <summary>
        /// The scenario of the room is over. The ending belongs to the player at this device: a victory when their realm
        /// won, or, when the room called the game off, when the room ranked them first. Nothing at this device changes for
        /// it: the campaign keeps its stars and unlocks, and the saved game of the title screen stays.
        /// </summary>
        private IEnumerator FinishOnline(GameState state, int days)
        {
            RoomMemberInfo me = online != null && online.ServerClient != null ? online.ServerClient.LocalMember : null;
            bool won = calledOff ? me != null && me.Place == 1 : state.winner >= 0 && state.winner == localSeat;
            if (won)
            {
                sound?.PlayVictoryMusic();
            }
            else
            {
                sound?.PlayDefeatMusic();
            }
            yield return new WaitForSeconds(0.6f);
            ui.ShowEnd(won, 0, days);
            TransitionState(new Gamebox.GameState(won ? BaseGameState.Victory : BaseGameState.GameOver));
        }

        // ------------------------------------------------------------------ what the server sends

        LockstepTable<GameCommand> ILockstepHost<GameCommand>.Table => lockstep;

        int ILockstepHost<GameCommand>.TableSeat => localSeat;

        string ILockstepHost<GameCommand>.TurnCaption => OnlineCaption();

        /// <summary>An entry of the log was taken: it counts now, in the order the log put it in.</summary>
        void ILockstepHost<GameCommand>.EntryTaken(LockstepEntry<GameCommand> entry)
        {
            if (lockstep == null || Game == null)
            {
                return;
            }
            switch (entry.Kind)
            {
                case LockstepEntryKind.Timeout when entry.Made.Count > 0:
                    ui.Log($"Time is up for {NameOfSeat(entry.Made[0].player)}.", entry.Made[0].player);
                    break;
                case LockstepEntryKind.SeatToComputer when entry.Accepted:
                    ui.Log($"{NameOfSeat(entry.Seat)} left. The computer plays on.", entry.Seat);
                    break;
                case LockstepEntryKind.Command when entry.FromHere && entry.Command != null && Game.IsComputer(entry.Command.player):
                    // A move the host worked out for the computer: as at one device, a refused one is not tried again,
                    // and after a few the computer falls back on the default move (the director).
                    if (entry.Accepted)
                    {
                        refused = 0;
                    }
                    else
                    {
                        brain?.Refused(entry.Command);
                        refused++;
                        if (refused == 12)
                        {
                            Debug.LogWarning($"Heroes: {entry.Command.kind} keeps being refused for seat {entry.Command.player} online.");
                        }
                    }
                    break;
            }
            onlineVersion++;
            ReportToRoom();
        }

        /// <summary>The room finished before the scenario did: the game ends here as it does on every other client.</summary>
        void ILockstepHost<GameCommand>.RoomFinished()
        {
            if (lockstep == null || Game == null || Game.IsOver)
            {
                return;
            }
            ui.Log("The game was called off.", -1);
            calledOff = true;
            Game.State.over = true;
            onlineVersion++;
        }

        /// <summary>A command of the player at this device did not reach the log: the choice is theirs again.</summary>
        void ILockstepHost<GameCommand>.CommandFailed()
        {
            sound?.Play(Sfx.Error, 0.7f);
            onlineVersion++;
        }

        private string NameOfSeat(int seat)
        {
            if (seat < 0)
            {
                return "the wandering armies";
            }
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
            long worth = me != null ? Worth(me) : 0;
            if (Game.IsOver)
            {
                // The room gives the members the places of their realms, a computer player that won before them all.
                online.ReportTableFinished(Game.State.winner, PlaceAtTable(localSeat), worth);
            }
            else if (Game.State.day != reportedDay)
            {
                reportedDay = Game.State.day;
                online.ReportScore(worth);
            }
        }

        /// <summary>
        /// The place of <paramref name="seat"/> at the table of a scenario that is over: the winner first, then the realms
        /// still standing, then those that fell, each by what they are worth. The computer players count, so every client
        /// gives every seat the same place.
        /// </summary>
        private int PlaceAtTable(int seat)
        {
            PlayerState me = Game.State.Player(seat);
            if (me == null)
            {
                return 0;
            }
            int place = 1;
            int worth = Worth(me);
            foreach (PlayerState other in Game.State.players)
            {
                if (other.index == seat)
                {
                    continue;
                }
                bool before;
                if ((other.index == Game.State.winner) != (seat == Game.State.winner))
                {
                    before = other.index == Game.State.winner;
                }
                else if (other.alive != me.alive)
                {
                    before = other.alive;
                }
                else
                {
                    int theirs = Worth(other);
                    before = theirs != worth ? theirs > worth : other.index < seat;
                }
                if (before)
                {
                    place++;
                }
            }
            return place;
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

        /// <summary>Who the game waits for, for the clock of the lobby.</summary>
        private string OnlineCaption()
        {
            if (lockstep == null || Game == null)
            {
                return "";
            }
            int seat = Game.WaitingPlayer;
            if (Game.IsOver)
            {
                return "Game over";
            }
            if (seat < 0)
            {
                return Game.InBattle ? "The wilds move" : "";
            }
            return seat == localSeat ? Game.InBattle ? "Your move" : "Your turn" : $"{NameOfSeat(seat)}'s turn";
        }

        /// <summary>Whether the player at this device is the one the rules are waiting for, and the map has caught up.</summary>
        internal bool OnlineDeciding()
        {
            return lockstep != null && IsGameRunning && WaitingForHuman && !Game.HasEvents &&
                   Game.WaitingPlayer == localSeat;
        }

        /// <summary>
        /// Whether the rules wait for a move only this device works out: on the host, a move of a computer player or of
        /// the wilds in a battle (the director's). Every other client waits for this screen then.
        /// </summary>
        private bool TableWaitsForThisDevice
        {
            get
            {
                if (lockstep == null || Game == null || Game.IsOver || online == null || !online.IsHost)
                {
                    return false;
                }
                int seat = Game.WaitingPlayer;
                return seat >= 0 ? Game.IsComputer(seat) : seat == -1 && Game.InBattle;
            }
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
                        if (what.kind == EventKind.BattleStarted)
                        {
                            IsLocalBattle = TakesPart(what);
                        }
                        yield return IsLocalBattle ? Play(what) : Tell(what);
                        if (what.kind == EventKind.BattleEnded)
                        {
                            IsLocalBattle = true;
                        }
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

                // A choice answered without its dialog (the clock ran out on it, the rules took the first card) leaves no
                // dialog behind: it closes by itself only when one of its cards is picked.
                if (ui.Choice.IsOpen && (Game.State.pending.Count == 0 || Game.State.pending[0].player != localSeat))
                {
                    ui.Choice.Close();
                }

                if (Game.IsComputer(seat))
                {
                    if (host && online.AwaitingComputer)
                    {
                        // The move sent for the computer is still on its way (something else arrived first): it is not
                        // worked out a second time, only waited for.
                        patience = Time.unscaledTime;
                    }
                    else if (host)
                    {
                        yield return new WaitForSeconds(ThinkDelay);
                        if (!Same(version))
                        {
                            continue;
                        }
                        // After a few refused moves the computer makes the one the clock would make, which always works.
                        GameCommand move = refused < 6 ? Game.InBattle ? BattleAI.Next(Game) : brain.Next(Game, seat) : null;
                        online.PlayComputer(move ?? LockstepGame.DefaultMove(Game) ?? Fallback(seat));
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

                // A move of the host for a computer seat that is still on its way is not worked out again.
                while (Same(version) && host == (online != null && online.IsHost) &&
                       (Time.unscaledTime < patience || online != null && online.AwaitingComputer))
                {
                    yield return null;
                }
                WaitingForHuman = false;
            }
            director = null;
        }

        /// <summary>
        /// An event of a battle the player at this device does not fight: the start and the end are told in the log and
        /// the map catches up with the losses; the stacks, their strikes and their spells are not on this screen.
        /// </summary>
        private IEnumerator Tell(GameEvent what)
        {
            switch (what.kind)
            {
                case EventKind.BattleStarted:
                {
                    int defender = what.battle != null ? what.battle.defenderPlayer : what.b;
                    ui.Log($"{NameOfSeat(what.player)} fights {(defender >= 0 ? NameOfSeat(defender) : "an army of the wilds")}.", what.player);
                    break;
                }
                case EventKind.BattleEnded:
                    // How it went, as a battle nobody watches at one device is told (a battle against nobody has no story).
                    if (what.battle != null && what.battle.stacks.Count > 0)
                    {
                        ui.Log(!string.IsNullOrEmpty(what.text) ? what.text : ResultLine(what.battle, (BattleResult)what.a), what.player);
                    }
                    Map.Sync();
                    ui.Refresh();
                    break;
                case EventKind.Message:
                case EventKind.HeroDefeated:
                case EventKind.PlayerEliminated:
                case EventKind.GameOver:
                    yield return Play(what);
                    break;
            }
        }

        /// <summary>Whether nothing arrived since the director last looked.</summary>
        private bool Same(int version)
        {
            return Game != null && lockstep != null && version == onlineVersion && !Game.HasEvents && IsGameRunning;
        }
    }
}
