using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Gamebox;
using Gamebox.Lockstep;
using Gamebox.Online;
using Gamebox.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Portfolio.BoardGames
{
    /// <summary>
    /// Development players only. Started with <c>-boardgames-tour &lt;folder&gt;</c>, it plays through the three games and
    /// saves a numbered screenshot of every step into the folder, then quits:
    /// <list type="bullet">
    /// <item>checkers: the title, the campaign window, a quick game against the computer (a selected piece, a capture and
    /// a king), the pause menu and the settings, giving up, two people at this device with a clock;</item>
    /// <item>chess: the title, the campaign window, a move by clicks against the computer, a scripted game of two people
    /// played by clicks (en passant, check, a promotion through the picker, castling), saving it and loading it back, the
    /// scholar's mate, a campaign level;</item>
    /// <item>backgammon: the title, the campaign window, a whole game against the computer played by clicks on the board
    /// (a checker taken back once, the cube offered and answered), a campaign level, two people without the opening
    /// roll or the cube with the board turning to the player to act, saving and loading;</item>
    /// <item>the next look: every game's title, and a chess and a backgammon board in it.</item>
    /// </list>
    /// <c>-boardgames-tour-games checkers,chess,backgammon</c> picks the games (all by default). Every step that does not
    /// do what it should writes a FAIL line to <c>tour.log</c>, errors and exceptions too. Without the argument it does
    /// nothing; with <c>-gamebox-theme &lt;name&gt;</c> it tours that look first.
    /// </summary>
    public class BoardGamesTour : MonoBehaviour
    {
        private const string Argument = "-boardgames-tour";
        private const string GamesArgument = "-boardgames-tour-games";
        /// <summary>The checkers quick game stops after this many plies even when no king was crowned.</summary>
        private const int QuickGamePlies = 40;
        /// <summary>Plies the two-player checkers game is played before the tour moves on.</summary>
        private const int LocalPlies = 10;
        /// <summary>Decisions after which the backgammon game against the computer stops even when it goes on.</summary>
        private const int BackgammonTurns = 260;
        /// <summary>Seconds between the pilot's decisions, so the board gets to animate.</summary>
        private const float PilotPace = 0.3f;

        /// <summary>A short game of two people: en passant, a check, a promotion with a capture, castling.</summary>
        private static readonly string[] ChessOpening =
        {
            "e2e4", "d7d5", "e4e5", "f7f5", "e5f6", "b8c6", "d1h5", "g7g6", "f6f7", "e8d7", "f7g8q", "g6h5", "g8h8", "e7e5",
            "g1f3", "f8d6", "f1e2", "d8f6", "e1g1", "f6f3", "e2f3"
        };

        private static readonly string[] ScholarsMate = { "e2e4", "e7e5", "f1c4", "b8c6", "d1h5", "g8f6", "h5f7" };

        private string folder;
        private BoardGamesManager manager;
        private StreamWriter log;
        private bool pilot;
        private int shot;
        private int failures;
        private int errors;
        private int mismatches;
        private bool sawCapture;
        private bool sawKing;
        private readonly IRandom random = new SystemRandom(7);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (!Debug.isDebugBuild || Application.isEditor)
            {
                return;
            }
            string target = MobilePlatform.ArgumentValue(Argument);
            if (string.IsNullOrEmpty(target) || FindAnyObjectByType<BoardGamesTour>() != null)
            {
                return;
            }
            var host = new GameObject("Board Games Tour");
            DontDestroyOnLoad(host);
            host.AddComponent<BoardGamesTour>().folder = target;
        }

        private bool Over => manager.State.Is(BaseGameState.Victory) || manager.State.Is(BaseGameState.GameOver);

        private IBoardGame Current => manager.Current;

        private IEnumerator Start()
        {
            Directory.CreateDirectory(folder);
            log = new StreamWriter(Path.Combine(folder, "tour.log")) { AutoFlush = true };
            Application.logMessageReceived += (message, stack, type) =>
            {
                if (type == LogType.Exception || type == LogType.Error)
                {
                    errors++;
                    Write($"{type}: {message}\n{stack}");
                }
            };
            while ((manager = FindAnyObjectByType<BoardGamesManager>()) == null)
            {
                yield return null;
            }
            manager.MovePlayed += CheckersPlayed;
            yield return new WaitForSeconds(3f);
            manager.Options.thinkingDelay = 0.2f;
            manager.Options.turnBoard = false;
            StartCoroutine(Pilot());

            string games = MobilePlatform.ArgumentValue(GamesArgument);
            if (string.IsNullOrEmpty(games))
            {
                games = "checkers,chess,backgammon";
            }
            if (games.Contains("checkers"))
            {
                yield return Checkers();
            }
            if (games.Contains("chess"))
            {
                yield return Chess();
            }
            if (games.Contains("backgammon"))
            {
                yield return Backgammon();
            }
            yield return NextLook();

            Write($"tour finished: {failures} FAIL, {errors} errors, {mismatches} board mismatches");
            manager.MovePlayed -= CheckersPlayed;
            manager.SelectMode(GameMode.Checkers);
            // The tour leaves no progress behind; the login of the online game is not the tour's to forget.
            var online = FindAnyObjectByType<BoardGamesOnlineController>();
            string server = online != null && online.ServerClient != null ? online.ServerClient.ServerUri : null;
            string token = server != null ? IdentityTokens.Load(server) : "";
            PlayerPrefs.DeleteAll();
            if (!string.IsNullOrEmpty(token))
            {
                IdentityTokens.Save(server, token);
            }
            PlayerPrefs.Save();
            log.Dispose();
            log = null;
            yield return null;
            Application.Quit();
        }

        // ------------------------------------------------------------------ checkers

        private IEnumerator Checkers()
        {
            yield return Title(GameMode.Checkers);
            yield return Campaign("checkers");

            // A quick game against the computer; the tour plays the person's seat the way a click does.
            manager.StartQuickGame();
            yield return WaitFor(() => manager.Table != null && manager.IsGameRunning, 20f);
            yield return WaitFor(() => manager.WaitingForHuman, 30f);
            yield return new WaitForSeconds(0.6f);
            yield return Shot("checkers_quick_game");
            CheckersGame game = manager.CheckersGame;
            if (game == null)
            {
                Fail("the quick game on the checkers title is no game of checkers");
                yield break;
            }
            if (manager.WaitingForHuman && game.Legal().Count > 0)
            {
                manager.SquareClicked(game.Legal()[0].From);
                yield return new WaitForSeconds(0.5f);
                yield return Shot("checkers_selected_piece");
                if (manager.Selected < 0)
                {
                    Fail("a click on a piece that may move did not select it");
                }
                manager.SquareClicked(-1);
            }
            pilot = true;
            StartCoroutine(ShotWhen("checkers_capture", () => sawCapture, 1.2f));
            StartCoroutine(ShotWhen("checkers_king", () => sawKing, 1.2f));
            yield return WaitFor(() => Over || manager.CheckersGame == null || (sawCapture && sawKing) || manager.CheckersGame.ply >= QuickGamePlies, 300f);
            yield return WaitFor(() => Over || manager.WaitingForHuman, 30f);
            pilot = false;
            Write($"checkers quick game: {Describe()}, capture {sawCapture}, king {sawKing}");
            yield return new WaitForSeconds(0.5f);
            yield return Shot("checkers_midgame");

            // The pause menu and the settings, then the person gives up.
            if (!Over)
            {
                manager.TransitionState(BaseGameState.Paused);
                yield return new WaitForSecondsRealtime(1f);
                yield return Shot("pause");
                BoardGamesUI ui = FindAnyObjectByType<BoardGamesUI>();
                if (ui != null)
                {
                    ui.ShowSettings();
                    yield return new WaitForSecondsRealtime(1f);
                    yield return Shot("settings");
                    ui.HideSettings();
                }
                manager.TransitionState(BaseGameState.Running);
                yield return new WaitForSeconds(0.5f);
                yield return WaitFor(() => Over || manager.WaitingForHuman, 30f);
                if (!Over)
                {
                    manager.ResignLocal();
                }
            }
            yield return WaitFor(() => Over, 20f);
            yield return new WaitForSeconds(1.5f);
            yield return Shot("checkers_result");
            Write($"checkers result: {manager.ResultTitle} / {manager.ResultDetail}");
            manager.ReturnToTitle();
            yield return new WaitForSeconds(1.5f);

            // Two people at this device, Russian rules, a clock of 30 seconds a move (the clock bar and its seconds show).
            yield return StartLocal(RuleSet.RussianDraughts, 1, true);
            yield return Shot("checkers_two_players");
            pilot = true;
            yield return WaitFor(() => Over || manager.CheckersGame == null || manager.CheckersGame.ply >= LocalPlies, 120f);
            yield return WaitFor(() => Over || manager.WaitingForHuman, 20f);
            pilot = false;
            yield return new WaitForSeconds(0.5f);
            yield return Shot("checkers_two_players_later");
            Write($"checkers local game: {Describe()}, hot seat {manager.IsHotSeat}, bottom {manager.BottomSide}");
            manager.ReturnToTitle();
            yield return new WaitForSecondsRealtime(1.5f);
        }

        private void CheckersPlayed(Move move)
        {
            if (move == null)
            {
                return;
            }
            sawCapture |= move.IsCapture;
            sawKing |= move.promotes;
        }

        // ------------------------------------------------------------------ chess

        private IEnumerator Chess()
        {
            yield return Title(GameMode.Chess);
            yield return Campaign("chess");
            yield return PlayCampaignLevel(GameMode.Chess);

            // Against the computer: the person's first move by clicks, the computer's reply animated.
            manager.StartQuickGame();
            yield return WaitFor(() => manager.Table != null && manager.WaitingForHuman && manager.ChessGame != null, 30f);
            yield return new WaitForSeconds(0.5f);
            yield return Shot("chess_quick_game");
            if (manager.ChessGame == null)
            {
                Fail("the quick game on the chess title is no game of chess");
                yield break;
            }
            manager.TargetClicked(ChessGame.Square(6, 0));
            yield return new WaitForSeconds(0.5f);
            yield return Shot("chess_knight_selected");
            if (manager.ChessSelection != ChessGame.Square(6, 0))
            {
                Fail("a click on the knight did not pick it up");
            }
            manager.TargetClicked(ChessGame.Square(5, 2));
            yield return WaitFor(() => Over || (manager.WaitingForHuman && manager.ChessGame != null && manager.ChessGame.ply >= 2), 30f);
            yield return new WaitForSeconds(0.4f);
            yield return Shot("chess_computer_replied");
            Write($"chess after the reply: {Describe()}  seat lines '{manager.SeatLine(0)}' / '{manager.SeatLine(1)}'");
            manager.ReturnToTitle();
            yield return new WaitForSecondsRealtime(1.5f);

            // Two people at this device: the scripted game, every move by clicks.
            yield return StartLocal(RuleSet.Chess, 0, false);
            for (int i = 0; i < ChessOpening.Length && !Over; i++)
            {
                yield return PlayChess(ChessOpening[i]);
                switch (i)
                {
                    case 4:
                        yield return Shot("chess_en_passant");
                        break;
                    case 6:
                        yield return Shot("chess_check");
                        break;
                    case 9:
                        yield return WaitFor(() => manager.WaitingForHuman, 10f);
                        manager.TargetClicked(ChessGame.Square(5, 6));
                        yield return new WaitForSeconds(0.4f);
                        yield return Shot("chess_promotion_targets");
                        manager.TargetClicked(-1);
                        break;
                    case 10:
                        yield return Shot("chess_promoted");
                        break;
                    case 18:
                        yield return Shot("chess_castled");
                        break;
                }
            }
            yield return new WaitForSeconds(0.6f);
            yield return Shot("chess_opening_done");
            Write($"chess opening: {Describe()}  seat lines '{manager.SeatLine(0)}' / '{manager.SeatLine(1)}'");

            // Saved on the way to the title, and loaded back: the same position goes on.
            string fen = manager.ChessGame != null ? manager.ChessGame.ToFen() : "";
            manager.ReturnToTitle();
            yield return new WaitForSecondsRealtime(1.2f);
            manager.LoadGame();
            yield return WaitFor(() => manager.Table != null && manager.ChessGame != null && manager.WaitingForHuman, 20f);
            yield return new WaitForSeconds(0.8f);
            yield return Shot("chess_loaded");
            string loaded = manager.ChessGame != null ? manager.ChessGame.ToFen() : "none";
            if (loaded != fen)
            {
                Fail($"the loaded chess game is not the saved one: {loaded} instead of {fen}");
            }
            manager.ReturnToTitle();
            yield return new WaitForSecondsRealtime(1.2f);

            // The scholar's mate: the result's words.
            yield return StartLocal(RuleSet.Chess, 0, false);
            foreach (string move in ScholarsMate)
            {
                yield return PlayChess(move);
            }
            yield return WaitFor(() => Over, 20f);
            yield return new WaitForSeconds(1.5f);
            yield return Shot("chess_checkmate");
            Write($"chess result: {manager.ResultTitle} / {manager.ResultDetail}");
            if (manager.ChessGame == null || manager.ChessGame.Reason != EndReason.Checkmate)
            {
                Fail("the scholar's mate did not end in checkmate");
            }
            manager.ReturnToTitle();
            yield return new WaitForSecondsRealtime(1.5f);
        }

        /// <summary>Plays <paramref name="uci"/> ("e2e4", "f7g8q") for the seat to move by two clicks; a promotion presses the picker's button.</summary>
        private IEnumerator PlayChess(string uci)
        {
            yield return WaitFor(() => Over || manager.WaitingForHuman, 20f);
            ChessGame game = manager.ChessGame;
            if (Over || game == null)
            {
                yield break;
            }
            int from = ChessGame.Square(uci[0] - 'a', uci[1] - '1');
            int to = ChessGame.Square(uci[2] - 'a', uci[3] - '1');
            int ply = game.ply;
            manager.TargetClicked(from);
            if (manager.ChessSelection != from)
            {
                Fail($"{uci}: clicking {ChessMove.SquareName(from)} did not pick the piece up");
            }
            yield return new WaitForSeconds(0.15f);
            manager.TargetClicked(to);
            if (uci.Length > 4)
            {
                yield return new WaitForSeconds(0.4f);
                if (!manager.ChessChoosingPromotion)
                {
                    Fail($"{uci}: no promotion picker");
                }
                yield return Shot("chess_promotion_picker");
                PressPromotion(char.ToUpperInvariant(uci[4]));
            }
            yield return WaitFor(() => Over || manager.ChessGame == null || manager.ChessGame.ply > ply, 10f);
            ChessMove last = manager.ChessGame != null ? manager.ChessGame.LastMove : null;
            if (last == null || last.from != from || last.to != to)
            {
                Fail($"{uci}: the table did not take it ({Describe()})");
            }
            else
            {
                Write($"{manager.ChessGame.ply,3} {last.san}");
            }
        }

        /// <summary>Presses the picker's button of the piece of <paramref name="letter"/> (Q R B N), as a click does.</summary>
        private void PressPromotion(char letter)
        {
            string name = letter == 'R' ? "Rook" : letter == 'B' ? "Bishop" : letter == 'N' ? "Knight" : "Queen";
            ChessHud hud = FindAnyObjectByType<ChessHud>();
            Button button = null;
            if (hud != null)
            {
                foreach (Button candidate in hud.GetComponentsInChildren<Button>(false))
                {
                    if (candidate.name == name + "Button")
                    {
                        button = candidate;
                    }
                }
            }
            if (button != null)
            {
                button.onClick.Invoke();
                return;
            }
            Fail($"no {name} button on the picker");
            manager.ChessChoosePromotion(letter == 'R' ? ChessPieceKind.Rook : letter == 'B' ? ChessPieceKind.Bishop : letter == 'N' ? ChessPieceKind.Knight : ChessPieceKind.Queen);
        }

        // ------------------------------------------------------------------ backgammon

        private IEnumerator Backgammon()
        {
            yield return Title(GameMode.Backgammon);
            yield return Campaign("backgammon");
            yield return PlayCampaignLevel(GameMode.Backgammon);

            manager.Options.thinkingDelay = 0.15f;
            manager.StartQuickGame();
            yield return WaitFor(() => manager.Table != null && manager.IsGameRunning, 20f);
            yield return WaitFor(() => manager.WaitingForHuman, 30f);
            yield return new WaitForSeconds(0.5f);
            yield return Shot("backgammon_opening");
            if (manager.BackgammonGame == null)
            {
                Fail("the quick game on the backgammon title is no game of backgammon");
                yield break;
            }
            Write($"backgammon opening: words '{manager.TurnWords}', offer '{manager.BackgammonOfferLine}', can roll {manager.BackgammonCanRoll}");
            if (manager.BackgammonCanRoll)
            {
                manager.BackgammonRoll();
                yield return new WaitForSeconds(0.35f);
                yield return Shot("backgammon_throw");
            }
            else
            {
                Fail("the opening roll is not offered");
            }
            yield return BackgammonByClicks();
            yield return WaitFor(() => Over, 30f);
            yield return new WaitForSeconds(1.5f);
            Compare("the end");
            yield return Shot("backgammon_result");
            Write($"backgammon result: '{manager.ResultTitle}' / '{manager.ResultDetail}', {Describe()}");
            manager.ReturnToTitle();
            yield return new WaitForSecondsRealtime(1.5f);

            // Two people, Ben first without the opening roll, no cube, the board turning to the player to act: a few
            // turns, saved on the way out, loaded back.
            manager.Options.turnBoard = true;
            yield return StartLocal(RuleSet.Backgammon, 0, false, 2, false);
            BackgammonGame start = manager.BackgammonGame;
            if (start == null || start.Phase != BackgammonPhase.Rolling || manager.Table.SeatToAct != 1 || start.cube)
            {
                Fail($"the game of two people did not start with Ben's ordinary roll and no cube ({Describe()})");
            }
            yield return new WaitForSeconds(1.2f);
            yield return Shot("backgammon_two_players_ben_first");
            pilot = true;
            StartCoroutine(ShotWhen("backgammon_turned_to_alice", () => manager.WaitingForHuman && manager.Table != null && manager.Table.SeatToAct == 0
                && manager.BackgammonView != null && !manager.BackgammonView.IsAnimating, 1.2f));
            yield return WaitFor(() => Over || manager.BackgammonGame == null || manager.BackgammonGame.Turn >= 12, 120f);
            yield return WaitFor(() => Over || (manager.WaitingForHuman && !manager.IsHandingOver && manager.BackgammonView != null && !manager.BackgammonView.IsAnimating), 30f);
            pilot = false;
            yield return new WaitForSeconds(0.6f);
            yield return Shot("backgammon_two_players");
            if (manager.BackgammonCanDouble)
            {
                Fail("the cube may be offered in a game without it");
            }
            manager.Options.turnBoard = false;
            BackgammonGame before = manager.BackgammonGame != null ? manager.BackgammonGame.Clone() : null;
            manager.ReturnToTitle();
            yield return new WaitForSecondsRealtime(1.2f);
            manager.LoadGame();
            yield return WaitFor(() => manager.Table != null && manager.BackgammonGame != null && (manager.WaitingForHuman || manager.IsHandingOver), 20f);
            yield return new WaitForSeconds(1f);
            yield return Shot("backgammon_loaded");
            if (before == null || manager.BackgammonGame == null || manager.BackgammonGame.Checksum() != before.Checksum())
            {
                Fail("the loaded backgammon game is not the saved one");
            }
            if (manager.IsHandingOver)
            {
                TurnCurtain.For(manager).Lift();
                yield return new WaitForSeconds(0.8f);
            }
            Compare("after loading");
            manager.ReturnToTitle();
            yield return new WaitForSecondsRealtime(1.2f);
        }

        /// <summary>
        /// Plays the person's seat of a backgammon game by clicks on the board (a checker, then a lit target), takes one
        /// move back, offers the cube once and takes an offered one, until the game ends; checks the board after every
        /// decision.
        /// </summary>
        private IEnumerator BackgammonByClicks()
        {
            bool picked = false;
            bool undone = false;
            bool doubled = false;
            bool answered = false;
            int lastTurn = -1;
            float start = Time.realtimeSinceStartup;
            while (!Over && manager.BackgammonGame != null && manager.BackgammonGame.Turn < BackgammonTurns && Time.realtimeSinceStartup - start < 420f)
            {
                yield return new WaitForSeconds(0.2f);
                BackgammonGame game = manager.BackgammonGame;
                if (game == null || !manager.WaitingForHuman || manager.BackgammonView == null)
                {
                    continue;
                }
                if (game.Turn != lastTurn)
                {
                    lastTurn = game.Turn;
                    Compare("turn " + game.Turn);
                }
                if (manager.BackgammonAnswering)
                {
                    if (!answered)
                    {
                        answered = true;
                        yield return Shot("backgammon_cube_offered");
                    }
                    manager.BackgammonTake();
                    continue;
                }
                if (manager.BackgammonCanRoll)
                {
                    if (!doubled && manager.BackgammonCanDouble && game.Turn > 12)
                    {
                        doubled = true;
                        manager.BackgammonDouble();
                        yield return new WaitForSeconds(0.5f);
                        yield return Shot("backgammon_doubled");
                        continue;
                    }
                    manager.BackgammonRoll();
                    continue;
                }
                if (game.Phase != BackgammonPhase.Moving)
                {
                    continue;
                }
                IReadOnlyList<CheckerMove> next = game.NextMoves(manager.BackgammonMovesDone);
                if (next.Count == 0)
                {
                    Fail("no next move while the game waits for a play");
                    continue;
                }
                CheckerMove move = next[random.Range(0, next.Count)];
                int done = manager.BackgammonMovesDone.Count;
                int turn = game.Turn;
                manager.TargetClicked(move.from);
                yield return new WaitForSeconds(0.2f);
                if (manager.BackgammonSelected == move.from)
                {
                    if (!picked)
                    {
                        picked = true;
                        yield return new WaitForSeconds(0.3f);
                        yield return Shot("backgammon_picked");
                    }
                    manager.TargetHovered(move.to);
                    manager.TargetClicked(move.to);
                    yield return new WaitForSeconds(0.2f);
                }
                bool moved = manager.BackgammonMovesDone.Count > done || manager.BackgammonGame == null || manager.BackgammonGame.Turn != turn;
                if (!moved)
                {
                    Fail($"{move} did not move by clicks (selected {manager.BackgammonSelected})");
                    manager.TargetClicked(-1);
                    BackgammonPlay play = BackgammonAI.ChoosePlay(game, 0, random);
                    if (play != null && !manager.TrySend(BoardCommand.Play(manager.Table.SeatToAct, play.Path())))
                    {
                        Fail($"the manager refused {play}");
                    }
                    continue;
                }
                if (!undone && manager.BackgammonCanUndo)
                {
                    undone = true;
                    yield return new WaitForSeconds(0.6f);
                    yield return Shot("backgammon_moved");
                    manager.BackgammonUndo();
                    yield return new WaitForSeconds(0.8f);
                    yield return Shot("backgammon_undone");
                }
            }
            Write($"backgammon by clicks: picked {picked}, undone {undone}, doubled {doubled}, answered {answered}, {Describe()}");
        }

        /// <summary>Writes a mismatch when the still backgammon board does not show the game.</summary>
        private void Compare(string when)
        {
            BackgammonGame game = manager.BackgammonGame;
            BackgammonBoard board = manager.BackgammonView;
            if (game == null || board == null || board.IsAnimating || manager.BackgammonMovesDone.Count > 0)
            {
                return;
            }
            if (!board.ShowsPosition(game))
            {
                mismatches++;
                Write($"FAIL MISMATCH {when}: the board does not show the game (turn {game.Turn})");
            }
        }

        /// <summary>The first campaign level of <paramref name="mode"/>, started as the campaign window's Play starts it, then left.</summary>
        private IEnumerator PlayCampaignLevel(GameMode mode)
        {
            var campaign = manager.Campaign as BoardGamesCampaign;
            IReadOnlyList<int> levels = campaign != null ? campaign.Levels(mode) : null;
            if (levels == null || levels.Count == 0)
            {
                Fail($"the campaign has no {mode} levels");
                yield break;
            }
            manager.StartCampaignLevel(levels[0]);
            yield return WaitFor(() => manager.Table != null && manager.IsGameRunning, 20f);
            yield return new WaitForSeconds(1.2f);
            yield return Shot(mode.ToString().ToLowerInvariant() + "_campaign_level");
            if (manager.CampaignLevel != levels[0] || manager.Table == null || manager.Table.Mode != mode)
            {
                Fail($"the campaign level {levels[0]} did not start a game of {mode} ({Describe()})");
            }
            Write($"campaign level {levels[0]}: {Describe()}, seats {manager.NameOf(0)} / {manager.NameOf(1)}");
            manager.ReturnToTitle();
            yield return new WaitForSecondsRealtime(1.2f);
        }

        // ------------------------------------------------------------------ the next look

        private IEnumerator NextLook()
        {
            GameTheme before = GameThemes.Active(GameType.BoardGames);
            GameTheme after = GameThemes.SelectNext(GameType.BoardGames, 1, false);
            Write($"look: {(before != null ? before.DisplayName : "none")} -> {(after != null ? after.DisplayName : "none")}");
            foreach (GameMode mode in RuleSets.Modes)
            {
                manager.SelectMode(mode);
                yield return new WaitForSecondsRealtime(1.5f);
                yield return Shot("next_look_title_" + mode.ToString().ToLowerInvariant());
            }
            foreach (GameMode mode in new[] { GameMode.Chess, GameMode.Backgammon })
            {
                manager.SelectMode(mode);
                yield return new WaitForSecondsRealtime(0.5f);
                manager.StartQuickGame();
                yield return WaitFor(() => manager.Table != null && manager.IsGameRunning, 20f);
                pilot = true;
                yield return WaitFor(() => Over || Current == null || Current.Turn >= 8, 90f);
                yield return WaitFor(() => Over || manager.WaitingForHuman, 30f);
                pilot = false;
                yield return new WaitForSeconds(0.8f);
                yield return Shot("next_look_board_" + mode.ToString().ToLowerInvariant());
                manager.ReturnToTitle();
                yield return new WaitForSecondsRealtime(1.2f);
            }
            GameThemes.Select(GameType.BoardGames, before, false);
        }

        // ------------------------------------------------------------------ shared steps

        private IEnumerator Title(GameMode mode)
        {
            manager.SelectMode(mode);
            yield return new WaitForSecondsRealtime(1.5f);
            yield return Shot(mode.ToString().ToLowerInvariant() + "_title");
            if (manager.TitleMode != mode)
            {
                Fail($"the title shows {manager.TitleMode} after selecting {mode}");
            }
        }

        private IEnumerator Campaign(string game)
        {
            CampaignWindow campaign = CampaignWindow.For(manager);
            campaign.Show();
            yield return new WaitForSeconds(1.2f);
            yield return Shot(game + "_campaign");
            campaign.Hide();
            yield return new WaitForSeconds(0.4f);
        }

        /// <summary>A game of two people here (Alice and Ben) at <paramref name="ruleSet"/>, started through the setup window.</summary>
        private IEnumerator StartLocal(RuleSet ruleSet, int clock, bool shootSetup, int first = 0, bool cube = true)
        {
            var match = new LocalMatch(GameType.BoardGames, manager.LocalPlay) { Count = 2, HandOver = false };
            match[0].Name = "Alice";
            match[0].Kind = SeatKind.Human;
            match[1].Name = "Ben";
            match[1].Kind = SeatKind.Human;
            match.SetOption(BoardGamesLocalRules.RuleSetOption, (int)ruleSet);
            match.SetOption(BoardGamesLocalRules.FirstOption, first);
            match.SetOption(BoardGamesLocalRules.ClockOption, clock);
            match.SetOption(BoardGamesLocalRules.CubeOption, cube ? 0 : 1);
            match.Remember();
            manager.OpenLocalPlay();
            yield return new WaitForSeconds(1.2f);
            if (shootSetup)
            {
                yield return Shot("local_setup");
            }
            bool started = LocalPlayUI.For(manager).StartMatch();
            if (!started)
            {
                Fail($"the local {ruleSet} game did not start");
            }
            yield return WaitFor(() => manager.Table != null && manager.IsGameRunning, 20f);
            if (manager.Table != null && manager.Table.RuleSet != ruleSet)
            {
                Fail($"the local game plays {manager.Table.RuleSet} instead of {ruleSet}");
            }
            yield return WaitFor(() => manager.WaitingForHuman, 20f);
            yield return new WaitForSeconds(0.4f);
        }

        /// <summary>
        /// Plays every seat that is a person at this device while <see cref="pilot"/> is on: the computer's Normal choice,
        /// through the same path a click takes; lifts the hand-over screen.
        /// </summary>
        private IEnumerator Pilot()
        {
            while (true)
            {
                yield return new WaitForSeconds(PilotPace);
                if (!pilot || manager.Table == null || Current == null || Current.IsOver)
                {
                    continue;
                }
                if (manager.IsHandingOver)
                {
                    TurnCurtain.For(manager).Lift();
                    continue;
                }
                if (!manager.WaitingForHuman || (manager.BackgammonGame != null && manager.BackgammonMovesDone.Count > 0))
                {
                    continue;
                }
                int seat = manager.Table.SeatToAct;
                BoardCommand command = BoardAI.Choose(Current.CloneGame(), seat, 1, random);
                if (command != null && !manager.TrySend(command))
                {
                    Fail($"the manager refused {command}");
                }
            }
        }

        private string Describe()
        {
            IBoardGame game = Current;
            if (game == null)
            {
                return "no game";
            }
            string position = game is ChessGame chess ? chess.ToFen()
                : game is BackgammonGame backgammon ? $"pips {backgammon.PipCount(Side.Light)}/{backgammon.PipCount(Side.Dark)} off {backgammon.off[0]}/{backgammon.off[1]} cube {backgammon.cubeValue}"
                : game is CheckersGame checkers ? $"men {checkers.board.Count(Side.Light)}/{checkers.board.Count(Side.Dark)}" : "";
            return $"{game.RuleSet} turn {game.Turn}, {position}, over {game.IsOver} ({game.Reason})";
        }

        private void Fail(string what)
        {
            failures++;
            Write("FAIL " + what);
        }

        private IEnumerator ShotWhen(string name, Func<bool> condition, float delay)
        {
            while (!condition())
            {
                if (Over || manager.Table == null)
                {
                    yield break;
                }
                yield return null;
            }
            yield return new WaitForSeconds(delay);
            yield return Shot(name);
        }

        private IEnumerator WaitFor(Func<bool> condition, float timeout)
        {
            float start = Time.realtimeSinceStartup;
            while (!condition())
            {
                if (Time.realtimeSinceStartup - start > timeout)
                {
                    Write($"TIMEOUT after {timeout} s ({Describe()}, state {manager.State})");
                    yield break;
                }
                yield return null;
            }
        }

        private IEnumerator Shot(string name)
        {
            yield return new WaitForEndOfFrame();
            string file = $"{++shot:00}_{name}.png";
            ScreenCapture.CaptureScreenshot(Path.Combine(folder, file));
            Write($"{file}: state {manager.State}, {Describe()}, waiting {manager.WaitingForHuman}");
            yield return null;
            yield return null;
        }

        private void Write(string line)
        {
            log?.WriteLine($"{Time.realtimeSinceStartup:0.0} {line}");
        }
    }
}
