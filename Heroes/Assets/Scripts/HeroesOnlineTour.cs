using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Gamebox;
using Gamebox.Online;
using Gamebox.UI;
using Portfolio.Heroes.UI;
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
    ///
    /// <c>hostscreen</c> opens the room as its shared screen: nothing is played there, the tour only watches the whole
    /// game (as host the screen moves the computer seats and the wilds) and takes pictures of the map, of the first
    /// battle and its results, and of the standings at the end. <c>joinremote</c> joins as a remote control: the pilot
    /// plays through the panel of the remote (<see cref="RemotePanel"/>: its question answered, its battles fought with
    /// Auto combat, Next Hero and End Turn pressed) and sends its heroes over the map as before, and takes pictures of
    /// the panel; with <c>-heroes-online-switch &lt;seconds&gt;</c> it plays on its own device after that many seconds
    /// of the game and goes back to the remote after as many more, and writes down that the game goes on either way.
    /// <c>joinown</c> joins a room with a shared screen but plays on its own device, as <c>join</c> does.
    /// </summary>
    public class HeroesOnlineTour : OnlineTour
    {
        private const string Argument = "-heroes-online";

        /// <summary>The command line switch that has a remote play on its own device after so many seconds of the game, and go back after as many more.</summary>
        private const string SwitchArgument = "-heroes-online-switch";

        /// <summary>Seconds the shared screen waits for anything to happen on the table before it gives up.</summary>
        private const float ScreenPatience = 150f;

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

        /// <summary>The panel of the remote while this device plays as one, else null (the pilot then plays as on any device).</summary>
        private RemotePanel Panel => Online != null && Online.ShowsRemotePanel ? RemotePanel.Current : null;

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
                int players = NumberArgument(RoomPlayersArgument, TablePlayers + PartyExtras);
                yield return HostRoom("Tour room", -1, manager.Online.ComposeOptions(-1, Options), (byte)players);
                yield return Shot("01_room");
                // The shared screen plays no seat: every player of the room comes from another device.
                yield return WaitForGuests(AsScreen ? players : 1, 150f);
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
            if (AsScreen)
            {
                yield return Watch();
            }
            else
            {
                yield return Play(Resaves ? 2 : Days);
            }
            if (Resaves && !AsScreen)
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
            if (seats.Count == 0)
            {
                return "no seat (the shared screen)";
            }
            string list = string.Join(", ", seats.Select(SeatName));
            return seats.Count == 1 ? $"seat {list}" : $"seats {list}";
        }

        /// <summary>
        /// The shared screen's part: nothing to play, only the whole game to watch (as host it moves the computer seats and
        /// the wilds) until the players are done and the room is back in the lobby. It writes down what the screen shows
        /// (whose realm the panels follow, every battle, which has to come onto this screen) and whatever should never
        /// happen on it (a wait for a human, a question, the hand-over screen), and takes pictures of the first battle, its
        /// results and the standings at the end.
        /// </summary>
        private IEnumerator Watch()
        {
            float idle = 0f;
            int lastDay = -1;
            int battles = 0;
            int shown = 0;
            bool fighting = false;
            bool resultsShot = false;
            bool complained = false;
            int lastApplied = -1;
            Note($"the shared screen: viewer {manager.Viewer}, shared {manager.IsSharedScreen}, fog of nobody {manager.Map.Fog.Viewer == null}, " +
                 $"panels {(manager.HeroesUI.Hud.IsShown ? "shown" : "off")}, following {manager.WatchedState?.name ?? "nobody"}, host {Online.IsHost}");
            while (Game != null && (Server.CurrentRoom == null || Server.CurrentRoom.IsPlaying) && idle < ScreenPatience)
            {
                // The table is alive while actions keep coming (a long battle is many of them): only a silent table counts as idle.
                if (manager.Lockstep != null && manager.Lockstep.Applied != lastApplied)
                {
                    lastApplied = manager.Lockstep.Applied;
                    idle = 0f;
                }
                if (Game.State.day != lastDay)
                {
                    lastDay = Game.State.day;
                    Note($"day {lastDay} begins: the panels follow {manager.WatchedState?.name ?? "nobody"}, it is seat {Game.WaitingPlayer}'s turn");
                    idle = 0f;
                }
                if (Game.InBattle != fighting)
                {
                    fighting = Game.InBattle;
                    if (fighting)
                    {
                        battles++;
                        Note($"battle {battles}: seat {Game.Battle.attackerPlayer} against {Game.Battle.defenderPlayer}, style {Game.Battle.style}");
                        if (battles == 1)
                        {
                            StartCoroutine(IntroShot());
                            StartCoroutine(ShotLater("05_battle", 2.5f));
                        }
                    }
                    else
                    {
                        Note($"battle {battles} over; it came onto this screen: {shown >= battles}");
                    }
                    idle = 0f;
                }
                if (manager.Battle.Running && shown < battles)
                {
                    shown = battles;
                    Note($"battle {battles} is on the screen (shown here {manager.IsLocalBattle})");
                }
                if (manager.HeroesUI.Bar.Results.IsOpen && !resultsShot)
                {
                    resultsShot = true;
                    yield return new WaitForSeconds(0.5f);
                    yield return Shot("06_results");
                    Note("the results of a battle are on the screen; they close by themselves");
                }
                if (!complained && (manager.WaitingForHuman || manager.HeroesUI.Choice.IsOpen || manager.IsHandingOver))
                {
                    complained = true;
                    Note($"FAILED: the shared screen asks somebody: waiting for a human {manager.WaitingForHuman}, choice open {manager.HeroesUI.Choice.IsOpen}, " +
                         $"handing over {manager.IsHandingOver}");
                }
                yield return null;
                if (Idle(ref idle))
                {
                    Note($"watching: seat {Game.WaitingPlayer}, events {Game.HasEvents}, battle {Game.InBattle}, computer move on its way {manager.Online.AwaitingComputer}, " +
                         $"actions {manager.Lockstep.Applied}, day {Game.State.day}");
                }
            }
            Note($"watch loop over: {battles} battles, {shown} on the screen, room {(Server.CurrentRoom != null ? Server.CurrentRoom.Phase.ToString() : "gone")}, " +
                 $"game {(Game == null ? "gone" : Game.IsOver ? $"over, winner {Game.State.winner}" : "running")}, idle {idle:F0} s, " +
                 $"checksum {(Game != null ? manager.Lockstep.Checksum().ToString("X8") : "-")} after {(Game != null ? manager.Lockstep.Applied : 0)} actions");
            // The players left: the room called the game off, and the screen shows the standings (and goes back to the
            // room by itself a while later: OnlineGameController.ScreenResultsSeconds).
            yield return WaitFor(() => manager.HeroesUI.Results.IsOpen || !manager.IsOnlineGame, 15f, "the standings");
            yield return new WaitForSeconds(0.6f);
            yield return Shot("07_standings");
            Note($"the end on the screen: standings {(manager.HeroesUI.Results.IsOpen ? "shown" : manager.IsOnlineGame ? "not shown" : "gone, the screen is back in the room")}, " +
                 $"won here {Game != null && manager.WonHere(Game.State)}");
            // Nobody presses anything on the screen: it leaves the results and goes back to the room by itself.
            float shownAt = Time.unscaledTime;
            yield return WaitFor(() => manager.Online == null || !manager.Online.InCharge, OnlineGameController.ScreenResultsSeconds + 10f, "the screen back in the room");
            yield return new WaitForSeconds(1f);
            yield return Shot("08_back_in_room");
            Note($"the screen after the results: {(manager.Online != null && manager.Online.InCharge ? "FAILED, still in the game" : "back in the room by itself")} " +
                 $"after {Time.unscaledTime - shownAt:F1}s, room {(Server.CurrentRoom != null ? Server.CurrentRoom.Phase.ToString() : "gone")}");
        }

        /// <summary>Whether the window of <paramref name="dialog"/> lies between the header and the footer of the remote frame, in screen pixels.</summary>
        private static bool Fits(Dialog dialog)
        {
            RectTransform window = dialog != null ? dialog.Frame : null;
            if (window == null)
            {
                return false;
            }
            var corners = new Vector3[4];
            window.GetWorldCorners(corners);
            Canvas canvas = window.GetComponentInParent<Canvas>();
            float scale = canvas != null ? canvas.rootCanvas.scaleFactor : 1f;
            float bottom = RemoteControlUI.FooterHeight * scale;
            float top = Screen.height - RemoteControlUI.HeaderHeight * scale;
            return corners[0].y >= bottom - 1f && corners[1].y <= top + 1f;
        }

        /// <summary>
        /// A remote plays on its own device from now on (<paramref name="own"/>), or goes back to being a remote: the
        /// panel goes or comes, the map's panels come back or go, and the game has to go on either way.
        /// </summary>
        private IEnumerator Switch(bool own, int number)
        {
            string answer = null;
            Online.SetRemoteMode(!own, error => answer = error ?? "ok");
            yield return WaitFor(() => answer != null, 15f, own ? "the switch to this device" : "the switch back to the remote");
            yield return WaitFor(() => Online.IsRemote != own, 10f, own ? "own device" : "remote mode");
            yield return new WaitForSeconds(1f);
            Note($"switch {number} {(own ? "to own device" : "back to the remote")}: {answer}; remote {Online.IsRemote}, panel {(Panel != null ? "up" : "gone")}, " +
                 $"map panels {(manager.HeroesUI.Hud.IsShown ? "shown" : "off")}, remote view {manager.HeroesUI.RemoteView}, viewer {manager.Viewer}, " +
                 $"seat {manager.LocalSeat}, deciding {manager.OnlineDeciding()}, choice open {manager.HeroesUI.Choice.IsOpen}");
            yield return Shot(own ? "14_own_device" : "15_remote_again");
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
            // A remote of a shared screen: its panel is pictured once, and it may switch to its own device and back.
            float switchAfter = NumberArgument(SwitchArgument, 0f);
            float switchAt = switchAfter > 0f ? Time.realtimeSinceStartup + switchAfter : float.PositiveInfinity;
            int switches = 0;
            int endedBeforeSwitch = -1;
            bool panelShot = false;
            bool choiceShot = false;
            bool townShot = false;
            bool heroShot = false;
            bool windowShot = false;
            bool battlePanelShot = false;
            // The other client has to get its turns too, so a day here is a couple of moves and then an end.
            while (ended < turns && Game != null && !Game.IsOver && idle < Patience && (Server.CurrentRoom == null || Server.CurrentRoom.IsPlaying))
            {
                if (Game.State.day != lastDay)
                {
                    lastDay = Game.State.day;
                    Note($"day {lastDay} begins");
                }
                if (!panelShot && Panel != null)
                {
                    panelShot = true;
                    yield return new WaitForSeconds(0.5f);
                    yield return Shot("11_panel");
                    Note($"the remote panel shows: mode {Panel.Mode}, map panels {(manager.HeroesUI.Hud.IsShown ? "shown" : "off")}, remote view {manager.HeroesUI.RemoteView}");
                }
                if (Panel != null && manager.HeroesUI.WindowOpen && !manager.HeroesUI.Bar.Results.IsOpen && !manager.HeroesUI.Results.IsOpen)
                {
                    // A window of the game opened under the frame by itself (a dwelling the hero visited, a meeting):
                    // pictured once, with whether it lies between the header and the footer, then closed as a player would.
                    if (!windowShot)
                    {
                        windowShot = true;
                        yield return new WaitForSeconds(0.5f);
                        yield return Shot("17_panel_window");
                        Note($"a window of the game opened under the frame: dwelling {manager.HeroesUI.Dwelling.IsOpen} (fits {Fits(manager.HeroesUI.Dwelling)}), " +
                             $"meeting {manager.HeroesUI.Meeting.IsOpen} (fits {Fits(manager.HeroesUI.Meeting)}); closed");
                    }
                    manager.HeroesUI.CloseScreens();
                    yield return null;
                    continue;
                }
                if (Time.realtimeSinceStartup >= switchAt && !manager.IsPassing && switches < 2)
                {
                    switches++;
                    bool own = switches == 1;
                    if (own)
                    {
                        endedBeforeSwitch = ended;
                    }
                    yield return Switch(own, switches);
                    switchAt = switches < 2 ? Time.realtimeSinceStartup + switchAfter : float.PositiveInfinity;
                    idle = 0f;
                    continue;
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
                    RemotePanel panel = Panel;
                    if (panel != null && !choiceShot)
                    {
                        choiceShot = true;
                        yield return new WaitForSeconds(0.4f);
                        yield return Shot("13_panel_choice");
                        Note($"the panel asks: {Game.State.pending[0].kind} (mode {panel.Mode}); the first option is taken");
                    }
                    if (panel != null)
                    {
                        // The remote answers through its panel (once the map has caught up and the question is its own).
                        panel.PressChoice(0);
                    }
                    else
                    {
                        manager.Commands.Choose(0);
                    }
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
                        RemotePanel panel = Panel;
                        if (panel != null && !battlePanelShot && manager.Battle.Running)
                        {
                            // The battle on a remote: the field framed between the frame's header and the bar above its footer.
                            battlePanelShot = true;
                            yield return new WaitForSeconds(0.5f);
                            yield return Shot("12_panel_battle");
                            Note($"the battle on the remote: bar covers {manager.HeroesUI.Bar.CoveredBottom():0.00} of the bottom, " +
                                 $"{manager.HeroesUI.Bar.CoveredTop():0.00} of the top is the frame's; panel mode {panel.Mode}");
                        }
                        if (panel != null)
                        {
                            // A remote fights through its panel: a move of auto combat at a time.
                            panel.PressAuto();
                        }
                        else
                        {
                            // The seat fights the way the computer would.
                            GameCommand move = BattleAI.Next(Game) ?? GameCommand.BattleDefend(side, current.id);
                            if (!manager.Online.Submit(move))
                            {
                                manager.Commands.BattleDefend(current.id);
                            }
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
                    RemotePanel panel = Panel;
                    if (panel != null && !townShot && manager.WatchedState != null && manager.WatchedState.towns.Count > 0)
                    {
                        // The town screen under the frame: opened as the Town command would, pictured, closed.
                        townShot = true;
                        manager.HeroesUI.OpenTown();
                        yield return new WaitForSeconds(0.6f);
                        yield return Shot("16_panel_town");
                        Note($"the town screen opened under the frame: open {manager.HeroesUI.Town.IsOpen}, fits {Fits(manager.HeroesUI.Town)}; closed");
                        manager.HeroesUI.CloseScreens();
                        yield return new WaitForSeconds(0.3f);
                        continue;
                    }
                    if (panel != null && !heroShot && manager.Selected != null)
                    {
                        heroShot = true;
                        manager.HeroesUI.OpenHero();
                        yield return new WaitForSeconds(0.6f);
                        yield return Shot("18_panel_hero");
                        Note($"the hero screen opened under the frame: open {manager.HeroesUI.Sheet.IsOpen}, fits {Fits(manager.HeroesUI.Sheet)}; closed");
                        manager.HeroesUI.CloseScreens();
                        yield return new WaitForSeconds(0.3f);
                        continue;
                    }
                    HeroState hero = null;
                    if (moves < 2 && panel != null)
                    {
                        // The remote's Next Hero puts the hero in hand; the map is the control for sending him somewhere.
                        panel.PressNextHero();
                        hero = manager.Selected;
                    }
                    else if (moves < 2)
                    {
                        hero = manager.NextHero();
                    }
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
                    Note(manager.LocalSeats.Count > 1 ? $"seat {SeatName(Game.WaitingPlayer)} ends day {Game.State.day}"
                        : panel != null ? $"ending day {Game.State.day} from the panel" : $"ending day {Game.State.day}");
                    if (panel != null)
                    {
                        panel.PressEndTurn();
                    }
                    else
                    {
                        manager.Commands.EndTurn();
                    }
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
            if (endedBeforeSwitch >= 0)
            {
                Note($"the game went on after the switch: {ended - endedBeforeSwitch} turns ended since, switches {switches}, remote now {Online.IsRemote}");
            }
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
