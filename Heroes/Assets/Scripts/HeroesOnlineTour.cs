using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Gamebox;
using Gamebox.Online;
using Gamebox.UI;
using UnityEngine;

namespace Portfolio.Heroes
{
    /// <summary>
    /// Development players only. Started with <c>-heroes-online host &lt;folder&gt;</c> or
    /// <c>-heroes-online join &lt;folder&gt;</c>, it plays an online scenario by itself and saves screenshots and a log into
    /// the folder, then quits (<see cref="OnlineTour"/>): the host opens a room (a small random map, battles on a
    /// battlefield of their own, no clock) and starts it once somebody joined and is ready, the other one joins the first
    /// room it sees, and both take their turns: a hero walks to the nearest thing worth having, wandering armies
    /// included, and a battle of this seat is fought with the moves of the computer's battle tactics. Every action of the
    /// log is written down with the checksum of the game after it, so the two logs show line for line whether the
    /// clients stayed in step. Run the two players with <c>-gamebox-identity</c> to tell them apart, and
    /// <c>-gamebox-server</c> / <c>-gamebox-database</c> for a test server; the host's <c>-heroes-online-turn &lt;seconds&gt;</c>
    /// puts a clock on the turns (shown on the bar of the map and on the battle bar). <c>hostsaves</c> and <c>joinsaves</c>
    /// play two days, then the host saves the game by name and calls it off, opens a new room from the save and the guest
    /// joins it: both write down the game they go on with, which has to be the one saved, and play a day of it.
    ///
    /// <c>hostprivate</c> and <c>joinprivate</c> play the same game in a private room, entered with its code.
    /// <c>hostparty</c> and <c>joinparty</c> bring other players to the device first (<c>-gamebox-companions</c>, one by
    /// default, and <c>-gamebox-guests</c>), who play realms of their own through its connection: the tour plays whichever
    /// realm of this device the game waits for, and writes down whose the screen is whenever it changes hands. With
    /// <c>-heroes-online-handover</c> the party asks for the hand-over screen between its players, which the tour lifts as
    /// they would. The host's room takes two players and the host's own party; a plain host that waits for a party of
    /// another device is given the size with <c>-gamebox-room-players</c>. The roles combine: <c>hostpartysaves</c> plays
    /// the saved game with its party, whose end of the game called off is the standings of every realm.
    /// </summary>
    public class HeroesOnlineTour : OnlineTour
    {
        private const string Argument = "-heroes-online";

        /// <summary>The room of the tour: a small random map with no clock and no computer players, fought out on battlefields.</summary>
        private const string TourOptions = "size=0;bots=0;turn=0;treasure=2;monsters=2;battles=1";

        /// <summary>The players the room of the tour takes besides the players a party brings to the host's device.</summary>
        private const int TablePlayers = 2;

        /// <summary>The room's options, with the clock of <c>-heroes-online-turn</c> when it is given.</summary>
        private static string Options
        {
            get
            {
                string seconds = MobilePlatform.ArgumentValue("-heroes-online-turn");
                return int.TryParse(seconds, out int turn) && turn > 0 ? TourOptions.Replace("turn=0", $"turn={turn}") : TourOptions;
            }
        }

        /// <summary>Days of its own each player plays before it stops.</summary>
        private const int Days = 4;

        /// <summary>Seconds the tour waits for a turn of this device, or for a battle of it to get on, before it gives up.</summary>
        private const float Patience = 90f;

        /// <summary>Seconds between the lines the tour writes while it waits.</summary>
        private const float NoteEvery = 15f;

        private const string OnlineSaveName = "Tour online save";

        private HeroesGameManager manager;

        /// <summary>A tour that saves the game online and plays it on in a room opened from the save.</summary>
        private bool Resaves => Role.EndsWith("saves", System.StringComparison.Ordinal);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Launch()
        {
            Bootstrap<HeroesOnlineTour>(Argument);
        }

        private HeroesGame Game => manager != null ? manager.Game : null;

        private IEnumerator Start()
        {
            OpenLog();
            while ((manager = FindAnyObjectByType<HeroesGameManager>()) == null)
            {
                yield return null;
            }
            yield return new WaitForSeconds(1f);
            if (manager.Online == null)
            {
                yield return Fail("the scene has no online controller");
                yield break;
            }
            yield return LogIn(manager.Online);
            if (!LoggedIn)
            {
                yield return Fail("no login");
                yield break;
            }
            // Written after the controller took the action, so the checksum is the one of the game after it.
            LogActions(kind => ((CommandKind)kind).ToString(), () => manager.Lockstep != null
                ? $"applied {manager.Lockstep.Applied} sum {manager.Lockstep.Checksum():X8} day {Game.State.day}"
                : "no game");
            if (InParty && HasArgument("-heroes-online-handover"))
            {
                // The party covers the map between the turns of its players; the tour lifts the screen.
                manager.Online.HandOverLocalSeats = true;
                Note("the party hands the device over between its players");
            }
            yield return new WaitForSeconds(0.5f);

            if (Hosting)
            {
                yield return HostRoom("Tour room", -1, manager.Online.ComposeOptions(-1, Options), (byte)(TablePlayers + PartyExtras));
                yield return Shot("01_room");
                yield return WaitForGuests(1, 150f);
                yield return new WaitForSeconds(0.5f);
                yield return Shot("02_ready");
                StartRoom();
            }
            else
            {
                yield return JoinFirstRoom(150f);
                yield return new WaitForSeconds(0.7f);
                yield return Shot("02_joined");
            }

            yield return WaitFor(() => manager.IsOnlineGame && Game != null, 90f, "the game");
            if (Game == null)
            {
                yield return Fail("the scenario never started");
                yield break;
            }
            Note($"playing {SeatsHere()} of {Game.State.players.Count}, map " +
                 $"{Game.State.map.grid.columns}x{Game.State.map.grid.rows}, seed {Game.State.seed}, battles {Game.State.rules.battleStyle}");
            yield return new WaitForSeconds(2f);
            yield return Shot("03_map");
            yield return Play(Resaves ? 2 : Days);
            if (Resaves)
            {
                yield return PlayTheSave();
            }
            yield return Shot("04_end");
            Note($"done on day {(Game != null ? Game.State.day : -1)}");
            yield return new WaitForSeconds(0.6f);
            Quit();
        }

        /// <summary>The seats played at this device, by number and name, for the log.</summary>
        private string SeatsHere()
        {
            IReadOnlyList<int> seats = manager.LocalSeats;
            string list = string.Join(", ", seats.Select(SeatName));
            return seats.Count == 1 ? $"seat {list}" : $"seats {list}";
        }

        private string SeatName(int seat)
        {
            PlayerState player = Game != null ? Game.State.Player(seat) : null;
            return $"{seat} ({(player != null ? player.name : "?")})";
        }

        /// <summary>
        /// Takes the turns of the players at this device until <paramref name="days"/> of each have passed: whichever of
        /// them the game waits for acts, on the map and in its battles, and the hand-over screen of a party is lifted as
        /// its players would lift it.
        /// </summary>
        private IEnumerator Play(int days)
        {
            int turns = days * Mathf.Max(1, manager.LocalSeats.Count);
            int ended = 0;
            float idle = 0f;
            int lastDay = -1;
            int moves = 0;
            int battles = 0;
            int results = 0;
            int curtains = 0;
            int handovers = 0;
            int screen = manager.LocalSeat;
            bool fighting = false;
            // The other client has to get its turns too, so a day here is a couple of moves and then an end.
            while (ended < turns && Game != null && !Game.IsOver && idle < Patience && (Server.CurrentRoom == null || Server.CurrentRoom.IsPlaying))
            {
                if (Game.State.day != lastDay)
                {
                    lastDay = Game.State.day;
                    Note($"day {lastDay} begins");
                }
                if (manager.LocalSeat != screen)
                {
                    // The screen went to another player at this device (their turn, their question or their battle).
                    screen = manager.LocalSeat;
                    handovers++;
                    Note($"the screen goes to seat {SeatName(screen)}");
                    if (handovers == 1)
                    {
                        StartCoroutine(ShotLater("09_another_realm", 1.5f));
                    }
                }
                if (manager.IsHandingOver)
                {
                    curtains++;
                    yield return new WaitForSeconds(0.6f);
                    if (curtains == 1)
                    {
                        yield return Shot("10_hand_over");
                    }
                    Note($"hand-over screen {curtains} lifted (the game waits for seat {Game.WaitingPlayer})");
                    TurnCurtain.For(manager).Lift();
                    yield return new WaitForSeconds(0.5f);
                    idle = 0f;
                    continue;
                }
                if (Game.InBattle != fighting)
                {
                    fighting = Game.InBattle;
                    if (fighting)
                    {
                        battles++;
                        // Whose it is comes from its sides: IsLocalBattle is set only when the screen reaches its start.
                        bool ours = manager.IsLocalSeat(Game.Battle.attackerPlayer) || manager.IsLocalSeat(Game.Battle.defenderPlayer);
                        Note($"battle {battles}: seat {Game.Battle.attackerPlayer} against {Game.Battle.defenderPlayer}, ours {ours}, " +
                             $"style {Game.Battle.style}");
                        if (battles == 1)
                        {
                            StartCoroutine(IntroShot());
                            StartCoroutine(ShotLater("05_battle", 2.5f));
                        }
                    }
                    else
                    {
                        Note($"battle {battles} over");
                    }
                }
                if (manager.HeroesUI.Bar.Results.IsOpen)
                {
                    // The results of a battle of this device: looked at, and closed as a player would.
                    results++;
                    if (results == 1)
                    {
                        yield return new WaitForSeconds(0.6f);
                        yield return Shot("06_results");
                    }
                    Note($"results of battle {battles} closed");
                    manager.HeroesUI.Bar.Results.Close();
                    yield return new WaitForSeconds(0.5f);
                    idle = 0f;
                    continue;
                }
                if (Game.State.pending.Count > 0 && Game.State.pending[0].player == manager.LocalSeat && !manager.IsPassing)
                {
                    manager.Commands.Choose(0);
                    yield return new WaitForSeconds(0.5f);
                    idle = 0f;
                    continue;
                }
                if (Game.InBattle)
                {
                    BattleStack current = Game.Battle.Current;
                    int side = current != null ? Game.Battle.PlayerOf(current.side) : -1;
                    if (manager.WaitingForHuman && manager.OnlineDeciding() && current != null && manager.IsLocalSeat(side))
                    {
                        // The seat fights the way the computer would.
                        GameCommand move = BattleAI.Next(Game) ?? GameCommand.BattleDefend(side, current.id);
                        if (!manager.Online.Submit(move))
                        {
                            manager.Commands.BattleDefend(current.id);
                        }
                        yield return new WaitForSeconds(0.4f);
                        idle = 0f;
                        continue;
                    }
                    yield return null;
                    // Another seat's battle takes as long as it takes (it is played out on that screen); only a battle
                    // that waits on this device and gets nowhere counts as being stuck.
                    if (current != null && manager.IsLocalSeat(side) && Idle(ref idle))
                    {
                        Note($"waiting in battle {battles}: stack {current.id} of seat {side}, screen {manager.LocalSeat}, " +
                             $"human {manager.WaitingForHuman}, deciding {manager.OnlineDeciding()}, events {Game.HasEvents}, " +
                             $"busy {manager.HeroesUI.Busy}, shown {manager.Battle.Running}, actions {manager.Lockstep.Applied}");
                    }
                    continue;
                }
                if (manager.WaitingForHuman && Game.WaitingPlayer == manager.LocalSeat && !manager.IsPassing)
                {
                    HeroState hero = moves < 2 ? manager.NextHero() : null;
                    int target = hero != null ? Prize(hero) : -1;
                    if (hero != null && target >= 0)
                    {
                        moves++;
                        int before = hero.movement;
                        manager.Select(hero);
                        manager.Commands.MoveHero(hero.id, target);
                        yield return new WaitForSeconds(2.5f);
                        HeroState after = Game != null ? Game.State.Hero(hero.id) : null;
                        if (after != null && after.alive && after.movement == before && !Game.InBattle)
                        {
                            manager.Commands.Sleep(hero.id, true);
                            yield return new WaitForSeconds(0.4f);
                        }
                        idle = 0f;
                        continue;
                    }
                    ended++;
                    moves = 0;
                    Note(manager.LocalSeats.Count > 1 ? $"seat {SeatName(Game.WaitingPlayer)} ends day {Game.State.day}" : $"ending day {Game.State.day}");
                    manager.Commands.EndTurn();
                    yield return new WaitForSeconds(1.2f);
                    idle = 0f;
                    continue;
                }
                yield return null;
                if (Idle(ref idle))
                {
                    Note($"waiting: seat {Game.WaitingPlayer}, ours {string.Join("/", manager.LocalSeats)}, screen {manager.LocalSeat}, " +
                         $"human {manager.WaitingForHuman}, battle {Game.InBattle}, choices {Game.State.pending.Count}, " +
                         $"busy {manager.HeroesUI.Busy}, actions {manager.Lockstep.Applied}");
                }
            }
            string ending = Game == null ? "gone"
                : Game.IsOver ? $"over, winner {Game.State.winner}"
                : "running";
            Note($"play loop over: turns {ended}/{turns}, battles {battles}, hand-overs {handovers}, screens {curtains}, idle {idle:F0} s, " +
                 $"game {ending}, checksum {(Game != null ? manager.Lockstep.Checksum().ToString("X8") : "-")} after " +
                 $"{(Game != null ? manager.Lockstep.Applied : 0)} actions");
        }

        /// <summary>
        /// Counts the frame that just passed as time spent waiting (<paramref name="idle"/>, in seconds, however fast the
        /// player draws). True when the wait crossed another <see cref="NoteEvery"/> seconds, for a line in the log.
        /// </summary>
        private static bool Idle(ref float idle)
        {
            float before = idle;
            idle += Time.unscaledDeltaTime;
            return (int)(idle / NoteEvery) != (int)(before / NoteEvery);
        }

        /// <summary>The nearest cell worth walking to that the hero can reach: a treasure, a mine, a building, or an army to fight.</summary>
        private int Prize(HeroState hero)
        {
            GameState state = Game.State;
            int best = -1;
            int closest = int.MaxValue;
            foreach (MapObject what in state.objects)
            {
                if (what.removed || what.owner == hero.owner)
                {
                    continue;
                }
                int distance = state.map.grid.Distance(hero.cell, what.cell);
                if (distance <= 0 || distance >= closest)
                {
                    continue;
                }
                // A wandering army is fought from the ground it guards, next to it.
                List<int> goals = what.kind == ObjectKind.Monster ? state.map.grid.Neighbors(what.cell) : new List<int> { what.cell };
                foreach (int goal in goals)
                {
                    MovePlan plan = manager.PlanFor(hero, goal);
                    if (plan != null && plan.Cells.Count > 0)
                    {
                        closest = distance;
                        best = goal;
                        break;
                    }
                }
            }
            return best;
        }

        /// <summary>
        /// The host saves the game by name once it can be saved (out of battle, the map at rest) and calls it off; both leave
        /// the room, the host opens one from the save, the guest joins it, and both write down the game they go on with.
        /// </summary>
        private IEnumerator PlayTheSave()
        {
            if (Hosting)
            {
                yield return WaitFor(() => Game == null || manager.SaveRefusal == null && !Game.HasEvents, 120f, "a moment to save");
                bool saved = manager.SaveAs(OnlineSaveName, out string problem);
                Note($"saved online: {(saved ? "ok" : problem)}; day {Game.State.day}, applied {manager.Lockstep.Applied} sum {manager.Lockstep.Checksum():X8}");
                Server.EndRoom(error => Note("end room: " + (error ?? "ok")));
            }
            yield return WaitFor(() => Server.CurrentRoom == null || Server.CurrentRoom.Phase == RoomPhase.Finished, 90f, "the room to finish");
            // The end of the game called off: how this device's realms did (with several players here, the standings).
            yield return WaitFor(() => manager.HeroesUI.Results.IsOpen, 10f, "the end of the game");
            yield return new WaitForSeconds(0.5f);
            Note($"the game was called off: results {(manager.HeroesUI.Results.IsOpen ? "shown" : "not shown")}, won here {Game != null && manager.WonHere(Game.State)}");
            yield return Shot("06_called_off");
            Note("--- the saved game");
            yield return new WaitForSeconds(Hosting ? 3f : 0.5f);
            Server.LeaveRoom();
            yield return WaitFor(() => !Server.InRoom, 10f, "out of the room");
            if (Hosting)
            {
                yield return new WaitForSeconds(2f);
                yield return HostSavedGame(manager.Online, OnlineSaveName, "Tour saved room", manager.Online.ComposeOptions(-1, Options));
                yield return new WaitForSeconds(0.5f);
                yield return Shot("07_room_of_the_save");
                yield return WaitForGuests(1, 150f);
                StartRoom();
            }
            else
            {
                yield return JoinFirstRoom(150f, room => room.PlaysSave);
                yield return new WaitForSeconds(0.7f);
                yield return Shot("07_joined_the_saved_game");
            }
            yield return WaitFor(() => manager.IsOnlineGame && Game != null && manager.Lockstep != null, 90f, "the saved game");
            if (Game == null || manager.Lockstep == null)
            {
                yield return Fail("the saved game never started");
                yield break;
            }
            Note($"resumed: {SeatsHere()}, day {Game.State.day}, applied {manager.Lockstep.Applied} sum {manager.Lockstep.Checksum():X8}; " +
                 string.Join(", ", Game.State.players.ConvertAll(p => $"{p.name} ({(p.human ? "person" : "computer")})")));
            yield return new WaitForSeconds(2f);
            yield return Shot("08_the_saved_game");
            yield return Play(1);
        }

        /// <summary>
        /// The opening words of the first battle on this screen, a moment after it comes up (with a clock on the turns,
        /// nothing else stands over them: the clock is on the battle bar).
        /// </summary>
        private IEnumerator IntroShot()
        {
            float until = Time.unscaledTime + 20f;
            while (!manager.Battle.Running && Time.unscaledTime < until)
            {
                yield return null;
            }
            if (manager.Battle.Running)
            {
                yield return new WaitForSeconds(1.3f);
                yield return Shot("05_battle_intro");
            }
        }

        private IEnumerator ShotLater(string shotName, float delay)
        {
            yield return new WaitForSeconds(delay);
            yield return Shot(shotName);
        }
    }
}
