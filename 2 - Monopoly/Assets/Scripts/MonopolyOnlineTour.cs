using System;
using System.Collections;
using System.IO;
using System.Linq;
using Gamebox;
using Gamebox.Online;
using UnityEngine;

namespace Portfolio.Monopoly
{
    /// <summary>
    /// Development players only. Started with <c>-monopoly-online &lt;role&gt; &lt;folder&gt; [name]</c>, it plays an online
    /// match by itself and saves screenshots and a log (named after the role, or the name) into the folder, then quits.
    /// The <c>host</c> opens a room (Tycoon Rush, which ends after twenty rounds, one computer player, a short clock)
    /// and starts it once somebody joined and is ready; <c>join</c> joins the first room it sees. Both play their seats
    /// with a simple autopilot, and a guest leaves its first decisions to the clock of the room. <c>joinquits</c> and
    /// <c>hostquits</c> leave the match after a while (the latter waits for two guests first): the computer plays their
    /// seats on, the room passes to a guest, who calls the match off a little later. Every action of the log is written
    /// down with the checksum of the match after it, so the logs of the players show whether they stayed in step. Run
    /// the players with <c>-gamebox-identity</c> to tell them apart, and <c>-gamebox-server</c> /
    /// <c>-gamebox-database</c> for a test server. Without the argument it does nothing.
    /// </summary>
    public class MonopolyOnlineTour : MonoBehaviour
    {
        private const string Argument = "-monopoly-online";
        /// <summary>The mode the room plays: Tycoon Rush ends after twenty rounds.</summary>
        private const int Mode = 4;
        /// <summary>Minutes after which the host calls the match off, should it not be over by then.</summary>
        private const float Patience = 16f;
        /// <summary>Seconds a quitter plays before leaving, and a guest who inherited the room before calling the match off.</summary>
        private const float QuitAfter = 50f;
        private const float CallOffAfter = 70f;

        private string role;
        private string who;
        private string folder;
        private bool host;
        private bool quits;
        private MonopolyGameManager manager;
        private ServerClient server;
        private StreamWriter log;
        private bool piloting;
        private int sleepy;
        private bool proposed;
        private bool timeoutShot;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (!Debug.isDebugBuild || Application.isEditor)
            {
                return;
            }
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 2; i++)
            {
                if (args[i] == Argument && FindAnyObjectByType<MonopolyOnlineTour>() == null)
                {
                    var tourObject = new GameObject("Monopoly Online Tour");
                    DontDestroyOnLoad(tourObject);
                    var tour = tourObject.AddComponent<MonopolyOnlineTour>();
                    tour.role = args[i + 1];
                    tour.folder = args[i + 2];
                    tour.who = i + 3 < args.Length && !args[i + 3].StartsWith("-") ? args[i + 3] : tour.role;
                    tour.host = tour.role.StartsWith("host");
                    tour.quits = tour.role.EndsWith("quits");
                    return;
                }
            }
        }

        private MonopolyMatch Match => manager.Match;

        private bool Over => manager.State.Is(BaseGameState.Victory) || manager.State.Is(BaseGameState.GameOver);

        private IEnumerator Start()
        {
            Directory.CreateDirectory(folder);
            log = new StreamWriter(Path.Combine(folder, $"{who}.log")) { AutoFlush = true };
            Application.logMessageReceived += (message, stack, type) =>
            {
                if (type == LogType.Error || type == LogType.Exception || type == LogType.Warning)
                {
                    Note($"{type}: {message}");
                }
            };
            while ((manager = FindAnyObjectByType<MonopolyGameManager>()) == null)
            {
                yield return null;
            }
            yield return new WaitForSeconds(2f);
            if (manager.Online == null)
            {
                yield return Fail("the scene has no online controller");
                yield break;
            }
            yield return Shot("00_title");
            server = manager.Online.ServerClient;
            server.Failed += error => Note("server: " + error);
            manager.OpenOnline();
            yield return WaitFor(() => server.IsLoggedIn, 30f, "login");
            if (!server.IsLoggedIn)
            {
                yield return Fail("no login");
                yield break;
            }
            Note($"logged in as {server.LocalPlayer.Name}");
            // After the controller, so the match took an action by the time it is written down here.
            server.ActionReceived += WriteAction;
            yield return new WaitForSeconds(0.5f);

            if (host)
            {
                int guests = quits ? 2 : 1;
                string options = RoomOptions.Write(MonopolyOnlineController.TurnOption, 20, MonopolyOnlineController.ComputersOption, 1,
                    MonopolyOnlineController.LevelOption, 1);
                server.CreateRoom("Tour table", Mode, options, 4, error => Note("create room: " + (error ?? "ok")));
                yield return WaitFor(() => server.InRoom, 15f, "own room");
                yield return new WaitForSeconds(0.5f);
                yield return Shot("01_room");
                yield return WaitFor(() => server.Members.Count > guests && server.Members.All(member => member.Ready || server.IsLocal(member)), 120f,
                    "the guests to be ready");
                yield return new WaitForSeconds(0.5f);
                yield return Shot("02_everybody_ready");
                server.StartRoom(error => Note("start room: " + (error ?? "ok")));
            }
            else
            {
                yield return WaitFor(() => server.Rooms.Any(room => room.Phase != RoomPhase.Playing), 120f, "a room");
                yield return Shot("01_rooms");
                RoomInfo open = server.Rooms.First(room => room.Phase != RoomPhase.Playing);
                server.JoinRoom(open.Id, error => Note("join room: " + (error ?? "ok")));
                yield return WaitFor(() => server.InRoom, 15f, "the room");
                server.SetReady(true, error => Note("ready: " + (error ?? "ok")));
                yield return new WaitForSeconds(0.7f);
                yield return Shot("02_room");
                // The first decisions are left to the clock of the room.
                sleepy = quits ? 0 : 2;
            }

            yield return WaitFor(() => manager.IsOnlineMatch && manager.IsGameRunning, 60f, "the match");
            if (!manager.IsOnlineMatch)
            {
                yield return Fail("the match did not start");
                yield break;
            }
            Note($"playing seat {manager.LocalSeat} of {Match.players.Count}: " + string.Join(", ", Match.players.Select(p => $"{p.name}{(p.bot ? " (computer)" : "")}")));
            piloting = true;
            StartCoroutine(Pilot());
            StartCoroutine(ShotWhen("04_the_clock_runs_down", () => WaitingForSomebodyElse && server.Turn != null && server.Turn.Remaining < 6f));
            StartCoroutine(ShotWhen("05_trade_offer", () => manager.MonopolyUI.Offer.IsOpen, 0.6f));
            StartCoroutine(ShotWhen("06_round_4", () => Match != null && Match.round >= 4 && manager.WaitingForHuman));
            StartCoroutine(ShotWhen("07_round_12", () => Match != null && Match.round >= 12 && !manager.WaitingForHuman));
            yield return new WaitForSeconds(6f);
            yield return Shot("03_first_turns");

            float started = Time.realtimeSinceStartup;
            float inherited = -1f;
            while (!Over && server.InRoom && Time.realtimeSinceStartup - started < Patience * 60f)
            {
                if (quits && Time.realtimeSinceStartup - started > QuitAfter)
                {
                    yield return LeaveTheMatch();
                    yield break;
                }
                if (!host && server.IsHost)
                {
                    // The host left and the room passed on: this player moves the computer players now.
                    if (inherited < 0f)
                    {
                        inherited = Time.realtimeSinceStartup;
                        Note("the room is mine now: " + string.Join(", ", Match.players.Select(p => $"{p.name}{(p.bot ? " (computer)" : "")}")));
                        StartCoroutine(ShotLater("10_inherited_the_room", 8f));
                    }
                    else if (Time.realtimeSinceStartup - inherited > CallOffAfter)
                    {
                        break;
                    }
                }
                yield return null;
            }
            if (!Over && server.IsHost && server.InRoom)
            {
                Note("calling the match off");
                server.EndRoom(error => Note("end room: " + (error ?? "ok")));
            }
            yield return WaitFor(() => Over && manager.MonopolyUI.Results.IsOpen, 90f, "the results");
            piloting = false;
            yield return new WaitForSeconds(2f);
            yield return Shot("08_results");
            if (Match != null)
            {
                Note($"result: {manager.State}, round {Match.round}; " + string.Join(", ", Match.Standings().Select(p => $"{p.place}. {p.name} {Match.NetWorth(p.index)}")));
                Note($"final: applied {manager.Lockstep.Applied} sum {manager.Lockstep.Checksum():X8}");
            }
            yield return WaitFor(() => server.CurrentRoom != null && server.CurrentRoom.Phase == RoomPhase.Finished, 60f, "the room to finish");
            Note("places: " + string.Join(", ", server.Members.Select(member => $"{server.NameOf(member.Identity)} {member.Place}. with {member.Score}")));

            manager.ActiveController.TransitionState(BaseGameState.Initialization);
            yield return new WaitForSeconds(1.5f);
            yield return Shot("09_back_in_the_room");
            Note($"back in the room: session {manager.InSession}, match {manager.Match != null}, state {manager.State}, lobby {manager.Online.LobbyUI.IsOpen}");
            // The guests leave first, so the host still sees the room lose its members.
            yield return new WaitForSeconds(host ? 3f : 0.5f);
            server.LeaveRoom();
            yield return new WaitForSeconds(1f);
            Note("done");
            Quit();
        }

        private bool WaitingForSomebodyElse
        {
            get
            {
                int seat = manager.Lockstep != null ? manager.Lockstep.Waiting : -1;
                return seat >= 0 && seat != manager.LocalSeat && !Match.players[seat].bot && !Match.HasEvents;
            }
        }

        /// <summary>Leaves through the match menu, as a player who has had enough does.</summary>
        private IEnumerator LeaveTheMatch()
        {
            piloting = false;
            manager.ActiveController.TransitionState(BaseGameState.Paused);
            yield return new WaitForSeconds(1f);
            yield return Shot("10_match_menu");
            Note($"leaving the match in round {Match.round}");
            manager.Online.LeaveMatch();
            yield return new WaitForSeconds(1.5f);
            yield return Shot("11_left_the_match");
            Note($"left: session {manager.InSession}, match {manager.Match != null}, state {manager.State}, in a room {server.InRoom}");
            Note("done");
            Quit();
        }

        /// <summary>Plays the seat of this player: rolls, buys what it can afford, builds, bids a little, pays or gives up, ends turns.</summary>
        private IEnumerator Pilot()
        {
            while (true)
            {
                yield return new WaitForSeconds(0.45f);
                if (!piloting || Match == null || !manager.IsOnlineMatch)
                {
                    continue;
                }
                int seat = manager.LocalSeat;
                if (manager.MonopolyUI.Cards.IsOpen)
                {
                    manager.MonopolyUI.Cards.Confirm();
                    continue;
                }
                TradeOffer pending = manager.Lockstep.PendingOffer;
                if (pending != null && pending.to == seat && manager.MonopolyUI.Offer.IsOpen)
                {
                    yield return new WaitForSeconds(1.2f);
                    bool accept = BotBrain.TradeGain(Match, pending, seat) >= 0f;
                    Note($"answering the offer of seat {pending.from}: {(accept ? "accept" : "decline")}");
                    manager.Online.AnswerTrade(seat, accept);
                    continue;
                }
                if (!manager.WaitingForHuman || Match.Decider != seat || pending != null)
                {
                    continue;
                }
                if (sleepy > 0)
                {
                    sleepy--;
                    Note($"leaving {Match.phase} to the clock");
                    int applied = manager.Lockstep.Applied;
                    yield return WaitFor(() => manager.Lockstep == null || manager.Lockstep.Applied != applied, 40f, "the clock");
                    continue;
                }
                IMonopolyCommands commands = manager.Commands;
                switch (Match.phase)
                {
                    case MatchPhase.Roll:
                    case MatchPhase.JailChoice:
                        commands.Roll(seat);
                        break;
                    case MatchPhase.BusChoice:
                        commands.ChooseBus(seat, 2);
                        break;
                    case MatchPhase.MoveAnywhere:
                        commands.ChooseDestination(seat, 39);
                        break;
                    case MatchPhase.BuyChoice:
                        if (Match.CanAffordPurchase && Match.players[seat].cash > 250)
                        {
                            commands.Buy(seat);
                        }
                        else
                        {
                            commands.DeclineBuy(seat);
                        }
                        break;
                    case MatchPhase.Auction:
                        if (Match.auction.MinimumBid <= 60 && Match.players[seat].cash > 400)
                        {
                            commands.Bid(seat, Match.auction.MinimumBid);
                        }
                        else
                        {
                            commands.PassBid(seat);
                        }
                        break;
                    case MatchPhase.RaiseFunds:
                    {
                        int space = Match.PropertiesOf(seat).Where(s => Match.CanMortgage(seat, s, out _)).DefaultIfEmpty(-1).First();
                        if (space >= 0)
                        {
                            commands.Mortgage(seat, space);
                        }
                        else
                        {
                            commands.DeclareBankruptcy(seat);
                        }
                        break;
                    }
                    case MatchPhase.EndTurn:
                        if (!EndTurnExtras(seat, commands))
                        {
                            manager.MonopolyUI.Manage.Close();
                            commands.EndTurn(seat);
                        }
                        break;
                }
            }
        }

        /// <summary>Managing before the turn ends: a house where one can go up, and once a small gift to another person at the table.</summary>
        private bool EndTurnExtras(int seat, IMonopolyCommands commands)
        {
            int site = Match.PropertiesOf(seat).Where(s => Match.CanBuild(seat, s, out _) && Match.players[seat].cash - Match.Space(s).houseCost > 400)
                .DefaultIfEmpty(-1).First();
            if (site >= 0)
            {
                Note($"building on {Match.Space(site).name}");
                commands.Build(seat, site);
                return true;
            }
            PlayerState other = Match.players.FirstOrDefault(p => !p.bot && !p.bankrupt && p.index != seat);
            if (host && !proposed && Match.round >= 2 && other != null && Match.players[seat].cash > 300)
            {
                proposed = true;
                var offer = new TradeOffer { from = seat, to = other.index, giveCash = 25 };
                Note($"offering {other.name} a gift of $25");
                commands.ProposeTrade(offer);
                return true;
            }
            return false;
        }

        /// <summary>Every action of the log with the state of the match after it: the players compare these lines.</summary>
        private void WriteAction(RoomActionInfo action)
        {
            LockstepMatch table = manager.Lockstep;
            if (table == null)
            {
                return;
            }
            string what = action.IsTimeout ? "timeout" : ((CommandKind)action.Kind).ToString();
            string cash = string.Join("/", table.Match.players.Select(p => p.cash));
            log?.WriteLine($"action {action.Id} seat {action.Seat} {what} {action.Payload} -> applied {table.Applied} sum {table.Checksum():X8} cash {cash}");
            if (action.IsTimeout && !timeoutShot)
            {
                timeoutShot = true;
                StartCoroutine(ShotLater("04_time_is_up", 0.6f));
            }
        }

        private IEnumerator ShotWhen(string shotName, Func<bool> condition, float delay = 0f)
        {
            while (!condition())
            {
                if (Over)
                {
                    yield break;
                }
                yield return null;
            }
            yield return ShotLater(shotName, delay);
        }

        private IEnumerator ShotLater(string shotName, float delay)
        {
            if (delay > 0f)
            {
                yield return new WaitForSeconds(delay);
            }
            yield return Shot(shotName);
        }

        private IEnumerator WaitFor(Func<bool> condition, float timeout, string what)
        {
            float waited = 0f;
            while (!condition() && waited < timeout)
            {
                waited += Time.unscaledDeltaTime;
                yield return null;
            }
            Note(condition() ? $"got {what} after {waited:0.0}s" : $"TIMEOUT waiting for {what}");
        }

        private IEnumerator Shot(string shotName)
        {
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(folder, $"{who}_{shotName}.png"));
            Note($"shot {shotName}: state {manager.State}, phase {(Match != null ? Match.phase.ToString() : "none")}, round {(Match != null ? Match.round : 0)}");
            yield return null;
        }

        private IEnumerator Fail(string reason)
        {
            Note("FAILED: " + reason);
            yield return Shot("failed");
            Quit();
        }

        private void Note(string message)
        {
            log?.WriteLine($"{Time.realtimeSinceStartup:0.0}s {message}");
        }

        private void Quit()
        {
            log?.Dispose();
            log = null;
            Application.Quit();
        }
    }
}
