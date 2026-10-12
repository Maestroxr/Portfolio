using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Gamebox;
using Gamebox.Lockstep;
using Gamebox.Online;
using UnityEngine;

namespace Portfolio.BoardGames
{
    /// <summary>
    /// Development players only. Started with <c>-boardgames-online &lt;role&gt; &lt;folder&gt; [name]</c>, it plays an online
    /// game by itself and saves screenshots and a log (named after the role, or the name) into the folder, then quits
    /// (<see cref="OnlineTour"/>). The <c>host</c> opens a room (the rule set of <c>-boardgames-level</c>: 0-2 draughts, 3 chess,
    /// 4 backgammon; English draughts by default; a short clock) and starts it once somebody joined and is ready; <c>join</c> joins the first room it sees
    /// and leaves its first move to the clock of the room. Both play their seats with the computer's Normal choice,
    /// through the same path a click takes. <c>joinquits</c> leaves the game after a while: the server hands its seat to
    /// the computer and the host plays on. <c>hosttournament</c> opens a cup of <c>-boardgames-entrants</c> players
    /// (four by default) that start a match <c>-boardgames-ready</c> seconds after it is drawn, and <c>jointournament</c>
    /// enters the first open one; everybody plays their matches and the bracket is photographed after each. Every action
    /// of the log is written down with the checksum of the table after it, so the logs of the players show whether they
    /// stayed in step. Run the players with <c>-gamebox-identity</c> to tell them apart, and <c>-gamebox-server</c> /
    /// <c>-gamebox-database</c> for a test server. Without the argument it does nothing.
    /// </summary>
    public class BoardGamesOnlineTour : OnlineTour
    {
        private const string Argument = "-boardgames-online";
        private const string LevelArgument = "-boardgames-level";
        private const string EntrantsArgument = "-boardgames-entrants";
        private const string ReadyArgument = "-boardgames-ready";
        /// <summary>Seconds a move may take in the rooms of the tour.</summary>
        private const int TurnSeconds = 20;
        /// <summary>Minutes after which the host calls the game off, should it not be over by then.</summary>
        private const float Patience = 12f;
        /// <summary>Seconds a quitter plays before leaving, and a guest who inherited the room before calling the game off.</summary>
        private const float QuitAfter = 40f;
        private const float CallOffAfter = 60f;
        /// <summary>The computer level the pilot plays with.</summary>
        private const int PilotLevel = 1;

        private BoardGamesManager manager;
        private BoardGamesOnlineController online;
        private bool piloting;
        private bool pilotRunning;
        private int sleepy;
        private bool timeoutShot;
        private readonly IRandom random = new SystemRandom(11);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Launch()
        {
            Bootstrap<BoardGamesOnlineTour>(Argument);
        }

        private bool Quits => Role.EndsWith("quits", StringComparison.Ordinal);

        private IBoardGame Game => manager.Current;

        private bool Over => manager.State.Is(BaseGameState.Victory) || manager.State.Is(BaseGameState.GameOver);

        /// <summary>The online game is on: the room's table is running at this device.</summary>
        private bool Playing => manager.IsOnlineGame && manager.IsGameRunning;

        private IEnumerator Start()
        {
            OpenLog(true);
            while ((manager = FindAnyObjectByType<BoardGamesManager>()) == null)
            {
                yield return null;
            }
            yield return new WaitForSeconds(2f);
            online = FindAnyObjectByType<BoardGamesOnlineController>();
            if (online == null)
            {
                yield return Fail("the scene has no online controller");
                yield break;
            }
            yield return Shot("00_title");
            yield return LogIn(online);
            if (!LoggedIn)
            {
                yield return Fail("no login");
                yield break;
            }
            // After the controller, so the table took the action by the time it is written down here.
            LogActions(kind => kind <= (uint)CommandKind.Drop ? ((CommandKind)kind).ToString() : $"kind {kind}", TableState);
            Server.ActionReceived += ShootTimeout;
            yield return new WaitForSeconds(0.5f);

            if (InTournament)
            {
                yield return Tournament();
                yield break;
            }

            if (Hosting)
            {
                yield return HostRoom("Tour room", Level(), RoomOptions.Write(RoomOptions.TurnOption, TurnSeconds), 2);
                yield return new WaitForSeconds(0.5f);
                yield return Shot("01_room");
                yield return WaitForGuests(1, 120f);
                yield return new WaitForSeconds(0.5f);
                yield return Shot("02_everybody_ready");
                StartRoom();
            }
            else
            {
                yield return JoinFirstRoom(120f);
                yield return new WaitForSeconds(0.7f);
                yield return Shot("02_room");
                // The first move is left to the clock of the room.
                sleepy = Quits ? 0 : 1;
            }

            yield return WaitFor(() => Playing, 60f, "the game");
            if (!manager.IsOnlineGame)
            {
                yield return Fail("the game did not start");
                yield break;
            }
            Note($"playing seat {manager.LocalSeat} ({manager.Setup.SideOf(manager.LocalSeat)}) of {Seats()}");
            piloting = true;
            StartCoroutine(Pilot());
            StartCoroutine(ShotWhen("04_the_clock_runs_down", () => WaitingForSomebodyElse && Server.Turn != null && Server.Turn.Remaining < 6f));
            StartCoroutine(ShotWhen("05_turn_10", () => Game != null && Game.Turn >= 10 && manager.WaitingForHuman, 0.4f));
            StartCoroutine(ShotWhen(HighlightShot(), Highlight, 1f));
            yield return new WaitForSeconds(5f);
            yield return Shot("03_first_moves");

            float started = Time.realtimeSinceStartup;
            float inherited = -1f;
            while (!Over && Server.InRoom && Time.realtimeSinceStartup - started < Patience * 60f)
            {
                if (Quits && Time.realtimeSinceStartup - started > QuitAfter)
                {
                    yield return LeaveTheGame();
                    yield break;
                }
                if (!Hosting && Server.IsHost)
                {
                    // The host left and the room passed on: this player moves the computer seat now.
                    if (inherited < 0f)
                    {
                        inherited = Time.realtimeSinceStartup;
                        Note("the room is mine now: " + Seats());
                        StartCoroutine(ShotLater("10_inherited_the_room", 6f));
                    }
                    else if (Time.realtimeSinceStartup - inherited > CallOffAfter)
                    {
                        break;
                    }
                }
                yield return null;
            }
            yield return EndTheGame();
            // The guest leaves first, so the host still sees the room lose its member.
            yield return new WaitForSeconds(Hosting ? 3f : 0.5f);
            Server.LeaveRoom();
            yield return new WaitForSeconds(1f);
            Note("done");
            Quit();
        }

        /// <summary>The room level (the rule set) from the command line, clamped to what the controller offers.</summary>
        private int Level()
        {
            IReadOnlyList<RoomLevelChoice> choices = online.LevelChoices;
            if (choices.Count == 0)
            {
                return 0;
            }
            return choices[Mathf.Clamp(NumberArgument(LevelArgument, 0), 0, choices.Count - 1)].Index;
        }

        /// <summary>What the log writes after every action: the table's step and checksum and the position in a few numbers.</summary>
        private string TableState()
        {
            BoardTable table = manager != null ? manager.Table : null;
            if (table == null)
            {
                return "no table";
            }
            IBoardGame game = table.Game;
            string position = game is ChessGame chess ? chess.ToFen()
                : game is BackgammonGame backgammon
                    ? $"{backgammon.Phase} dice {backgammon.die1}-{backgammon.die2} pips {backgammon.PipCount(Side.Light)}/{backgammon.PipCount(Side.Dark)} cube {backgammon.cubeValue}"
                : game is CheckersGame checkers ? $"men {checkers.board.Count(Side.Light)}/{checkers.board.Count(Side.Dark)}" : "";
            return $"applied {table.Applied} sum {table.Checksum():X8} turn {game.Turn} to act {table.SeatToAct} {position} result {game.Result}";
        }

        private string Seats()
        {
            TableSetup setup = manager.Setup;
            if (setup == null)
            {
                return "no table";
            }
            return string.Join(", ", setup.seats.Select((seat, i) => $"{i}: {seat.name}{(seat.computer ? $" (computer {seat.level})" : "")}"));
        }

        /// <summary>A tournament: the host opens and starts it, everybody plays their matches with the pilot.</summary>
        private IEnumerator Tournament()
        {
            int entrants = Mathf.Clamp(NumberArgument(EntrantsArgument, 4), 2, 64);
            if (Hosting)
            {
                string options = RoomOptions.Write(RoomOptions.TurnOption, TurnSeconds);
                yield return HostTournament("Tour cup", Level(), options, 2, (byte)entrants, (uint)Mathf.Clamp(NumberArgument(ReadyArgument, 30), 10, 3600));
                yield return Shot("t0_open");
                yield return WaitForEntrants(entrants - 1, 180f);
                yield return new WaitForSeconds(0.5f);
                yield return Shot("t0_entrants");
                StartTournament();
            }
            else
            {
                yield return JoinFirstTournament(180f);
                yield return new WaitForSeconds(0.5f);
                yield return Shot("t0_entered");
            }
            // Back to the room after every match (PlayAgain online); ReturnToTitle would leave the room and the cup.
            yield return PlayTournament(() => Playing, PlayMatch, manager.PlayAgain, online.LobbyUI, 300f);
            yield return new WaitForSeconds(1f);
            Note("done");
            Quit();
        }

        /// <summary>One match of the tournament, with the pilot, until the game is over and the result showed.</summary>
        private IEnumerator PlayMatch(int number)
        {
            Note($"playing match {number}: seat {manager.LocalSeat} of {Seats()}");
            piloting = true;
            if (!pilotRunning)
            {
                StartCoroutine(Pilot());
            }
            yield return new WaitForSeconds(6f);
            yield return Shot($"t{number}_playing");
            yield return WaitFor(() => Over, 600f, "the end of the game");
            piloting = false;
            yield return new WaitForSeconds(2.5f);
            yield return Shot($"t{number}_results");
            Note($"result: {manager.State}, {manager.ResultTitle}; {TableState()}");
        }

        /// <summary>The host calls the game off when it is not over; everybody waits for the result and goes back to the room.</summary>
        private IEnumerator EndTheGame()
        {
            if (!Over && Server.IsHost && Server.InRoom)
            {
                Note("calling the game off");
                Server.EndRoom(error => Note("end room: " + (error ?? "ok")));
            }
            yield return WaitFor(() => Over, 90f, "the result");
            piloting = false;
            yield return new WaitForSeconds(2f);
            yield return Shot("08_result");
            Note($"result: {manager.State}, {manager.ResultTitle} / {manager.ResultDetail}; {TableState()}");
            yield return WaitFor(() => Server.CurrentRoom != null && Server.CurrentRoom.Phase == RoomPhase.Finished, 60f, "the room to finish");
            Note("places: " + string.Join(", ", Server.Members.Select(member => $"{Server.NameOf(member.Identity)} {member.Place}. with {member.Score}")));

            // "Back to the room" of the result panel (ReturnToTitle is "Leave the match": it leaves the room).
            manager.PlayAgain();
            yield return new WaitForSeconds(1.5f);
            yield return Shot("09_back_in_the_room");
            Note($"back in the room: session {manager.InSession}, table {manager.Table != null}, state {manager.State}, lobby {online.LobbyUI.IsOpen}");
        }

        private bool WaitingForSomebodyElse
        {
            get
            {
                BoardTable table = manager.Table;
                if (table == null || Over)
                {
                    return false;
                }
                int seat = table.SeatToMove;
                return seat >= 0 && seat != manager.LocalSeat && !table.IsComputer(seat);
            }
        }

        /// <summary>Leaves through the match menu, as a player who has had enough does.</summary>
        private IEnumerator LeaveTheGame()
        {
            piloting = false;
            manager.ActiveController.TransitionState(BaseGameState.Paused);
            yield return new WaitForSeconds(1f);
            yield return Shot("10_match_menu");
            Note($"leaving the game at turn {(Game != null ? Game.Turn : -1)}");
            online.LeaveMatch();
            yield return new WaitForSeconds(1.5f);
            yield return Shot("11_left_the_game");
            Note($"left: session {manager.InSession}, table {manager.Table != null}, state {manager.State}, in a room {Server.InRoom}");
            Note("done");
            Quit();
        }

        /// <summary>Plays the seat of this player with the computer's choice, through the same path a click takes.</summary>
        private IEnumerator Pilot()
        {
            pilotRunning = true;
            while (true)
            {
                yield return new WaitForSeconds(0.45f);
                if (!piloting || manager.Table == null || !manager.IsOnlineGame || !manager.WaitingForHuman)
                {
                    continue;
                }
                if (sleepy > 0)
                {
                    sleepy--;
                    Note("leaving the move to the clock");
                    int applied = manager.Table.Applied;
                    yield return WaitFor(() => manager.Table == null || manager.Table.Applied != applied, TurnSeconds + 20f, "the clock");
                    continue;
                }
                BoardCommand command = Game != null ? BoardAI.Choose(Game.CloneGame(), manager.Table.SeatToAct, PilotLevel, random) : null;
                if (command == null)
                {
                    continue;
                }
                if (!manager.TrySend(command))
                {
                    Note($"the manager refused {command}");
                }
            }
        }

        /// <summary>The moment of the game worth a picture: a king in draughts, a check in chess, a hit or the cube in backgammon.</summary>
        private bool Highlight()
        {
            switch (Game)
            {
                case CheckersGame checkers:
                    return checkers.board.Count(Side.Light, true) + checkers.board.Count(Side.Dark, true) > 0;
                case ChessGame chess:
                    return chess.InCheck;
                case BackgammonGame backgammon:
                    return backgammon.bar[0] + backgammon.bar[1] > 0 || backgammon.cubeValue > 1 || backgammon.Phase == BackgammonPhase.Answering;
                default:
                    return false;
            }
        }

        private string HighlightShot()
        {
            switch (manager.Mode)
            {
                case GameMode.Chess:
                    return "06_check";
                case GameMode.Backgammon:
                    return "06_a_hit_or_the_cube";
                default:
                    return "06_a_king";
            }
        }

        /// <summary>The first time the clock of the room runs out, the screen shows it.</summary>
        private void ShootTimeout(RoomActionInfo action)
        {
            if (action.IsTimeout && !timeoutShot && manager.Table != null)
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
    }
}
