using System;
using System.Collections;
using System.IO;
using Gamebox;
using Gamebox.Lockstep;
using Gamebox.Online;
using Gamebox.UI;
using UnityEngine;

namespace Portfolio.Checkers
{
    /// <summary>
    /// Development players only. Started with <c>-checkers-tour &lt;folder&gt;</c>, it plays through the game (the title,
    /// the campaign window, a quick game against the computer in which the tour plays the person's seat through the
    /// same path a click takes until a capture and a king have happened, the pause menu and the settings, the result, a
    /// two-player local game, and the title and board in the game's next look) and saves a numbered screenshot of every
    /// step into the folder, then quits. Without the argument it does nothing; with <c>-gamebox-theme &lt;name&gt;</c> it
    /// tours that look first.
    /// </summary>
    public class CheckersTour : MonoBehaviour
    {
        private const string Argument = "-checkers-tour";
        /// <summary>The quick game stops after this many plies even when no king was crowned.</summary>
        private const int QuickGamePlies = 40;
        /// <summary>Plies the two-player local game is played before the tour moves on.</summary>
        private const int LocalPlies = 10;
        /// <summary>Seconds between the pilot's moves, so the board gets to animate.</summary>
        private const float PilotPace = 0.3f;

        private string folder;
        private CheckersGameManager manager;
        private StreamWriter log;
        private bool pilot;
        private Func<bool> hold;
        private int shot;
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
            if (string.IsNullOrEmpty(target) || FindAnyObjectByType<CheckersTour>() != null)
            {
                return;
            }
            var host = new GameObject("Checkers Tour");
            DontDestroyOnLoad(host);
            host.AddComponent<CheckersTour>().folder = target;
        }

        private CheckersGame Game => manager.Game;

        private bool Over => manager.State.Is(BaseGameState.Victory) || manager.State.Is(BaseGameState.GameOver);

        private IEnumerator Start()
        {
            Directory.CreateDirectory(folder);
            log = new StreamWriter(Path.Combine(folder, "tour.log")) { AutoFlush = true };
            Application.logMessageReceived += (message, stack, type) =>
            {
                if (type == LogType.Exception || type == LogType.Error)
                {
                    Write($"{type}: {message}\n{stack}");
                }
            };
            while ((manager = FindAnyObjectByType<CheckersGameManager>()) == null)
            {
                yield return null;
            }
            manager.MovePlayed += Played;
            yield return new WaitForSeconds(3f);
            yield return Shot("title");

            // The campaign window, as the Campaign button opens it.
            CampaignWindow campaign = CampaignWindow.For(manager);
            campaign.Show();
            yield return new WaitForSeconds(1.2f);
            yield return Shot("campaign");
            campaign.Hide();
            yield return new WaitForSeconds(0.4f);

            // A quick game against the computer; the tour plays the person's seat the way a click does.
            manager.Options.thinkingDelay = 0.2f;
            manager.StartQuickGame();
            yield return WaitFor(() => manager.Table != null && manager.IsGameRunning, 20f);
            yield return WaitFor(() => manager.WaitingForHuman, 30f);
            yield return new WaitForSeconds(0.6f);
            yield return Shot("quick_game");
            if (manager.WaitingForHuman && Game != null && Game.Legal().Count > 0)
            {
                manager.SquareClicked(Game.Legal()[0].From);
                yield return new WaitForSeconds(0.5f);
                yield return Shot("selected_piece");
                Write($"selected {manager.Selected} with {manager.SelectedMoves.Count} moves");
                manager.SquareClicked(-1);
            }
            pilot = true;
            StartCoroutine(Pilot());
            StartCoroutine(ShotWhen("capture", () => sawCapture, 1.2f));
            StartCoroutine(ShotWhen("king", () => sawKing, 1.2f));
            yield return WaitFor(() => Over || Game == null || (sawCapture && sawKing) || Game.ply >= QuickGamePlies, 300f);
            yield return WaitFor(() => Over || manager.WaitingForHuman, 30f);
            Write($"quick game: ply {(Game != null ? Game.ply : -1)}, capture {sawCapture}, king {sawKing}, over {Over}");
            yield return new WaitForSeconds(0.5f);
            yield return Shot("midgame");

            // The pause menu and the settings.
            pilot = false;
            if (!Over)
            {
                manager.TransitionState(BaseGameState.Paused);
                yield return new WaitForSecondsRealtime(1f);
                yield return Shot("pause");
                CheckersUI ui = FindAnyObjectByType<CheckersUI>();
                if (ui != null)
                {
                    ui.ShowSettings();
                    yield return new WaitForSecondsRealtime(1f);
                    yield return Shot("settings");
                    ui.HideSettings();
                }
                manager.TransitionState(BaseGameState.Running);
                yield return new WaitForSeconds(0.5f);

                // The result: the person gives up.
                yield return WaitFor(() => Over || manager.WaitingForHuman, 30f);
                if (!Over)
                {
                    manager.ResignLocal();
                }
            }
            yield return WaitFor(() => Over, 20f);
            yield return new WaitForSeconds(1.5f);
            yield return Shot("result");
            Write($"result: {manager.ResultTitle} / {manager.ResultDetail}");
            manager.ReturnToTitle();
            yield return new WaitForSeconds(1.5f);

            // Two people at this device, without the hand-over screen, for a few plies.
            var match = new LocalMatch(GameType.Checkers, manager.LocalPlay);
            match.Count = 2;
            match[0].Name = "Alice";
            match[0].Kind = SeatKind.Human;
            match[1].Name = "Ben";
            match[1].Kind = SeatKind.Human;
            match.HandOver = false;
            match.SetOption("variant", (int)Variant.Russian);
            // A clock of 30 seconds a move, so the pictures show the clock bar and its seconds.
            match.SetOption(CheckersLocalRules.ClockOption, 1);
            match.Remember();
            manager.OpenLocalPlay();
            yield return new WaitForSeconds(1.2f);
            yield return Shot("local_setup");
            bool started = LocalPlayUI.For(manager).StartMatch();
            Write($"local match started: {started}");
            yield return WaitFor(() => manager.Table != null && manager.IsGameRunning, 20f);
            yield return WaitFor(() => manager.WaitingForHuman, 20f);
            yield return new WaitForSeconds(0.5f);
            yield return Shot("two_players");
            pilot = true;
            yield return WaitFor(() => Over || Game == null || Game.ply >= LocalPlies, 120f);
            yield return WaitFor(() => Over || manager.WaitingForHuman, 20f);
            pilot = false;
            yield return new WaitForSeconds(0.5f);
            yield return Shot("two_players_later");
            Write($"local game: ply {(Game != null ? Game.ply : -1)}, hot seat {manager.IsHotSeat}, bottom {manager.BottomSide}");
            manager.ReturnToTitle();
            yield return new WaitForSecondsRealtime(1.5f);

            // The next look, picked on the title screen: the title and a board in it.
            GameTheme before = GameThemes.Active(GameType.Checkers);
            GameTheme after = GameThemes.SelectNext(GameType.Checkers, 1, false);
            yield return new WaitForSecondsRealtime(1.5f);
            yield return Shot("title_next_look");
            Write($"look switched on the title screen: {(before != null ? before.DisplayName : "none")} -> {(after != null ? after.DisplayName : "none")}");
            manager.StartQuickGame();
            yield return WaitFor(() => manager.Table != null && manager.IsGameRunning, 20f);
            yield return WaitFor(() => manager.WaitingForHuman, 30f);
            pilot = true;
            yield return WaitFor(() => Over || Game == null || Game.ply >= 6, 60f);
            pilot = false;
            yield return new WaitForSeconds(0.6f);
            yield return Shot("board_next_look");
            manager.ReturnToTitle();
            yield return new WaitForSecondsRealtime(1f);
            GameThemes.Select(GameType.Checkers, before, false);

            Write("tour finished");
            manager.MovePlayed -= Played;
            // The tour leaves no progress behind; the login of the online game is not the tour's to forget.
            var online = FindAnyObjectByType<CheckersOnlineController>();
            string server = online != null && online.ServerClient != null ? online.ServerClient.ServerUri : null;
            string token = server != null ? IdentityTokens.Load(server) : "";
            PlayerPrefs.DeleteAll();
            if (!string.IsNullOrEmpty(token))
            {
                IdentityTokens.Save(server, token);
            }
            PlayerPrefs.Save();
            log.Dispose();
            yield return null;
            Application.Quit();
        }

        private void Played(Move move)
        {
            if (move == null)
            {
                return;
            }
            if (move.IsCapture)
            {
                sawCapture = true;
            }
            if (move.promotes)
            {
                sawKing = true;
            }
            Write($"move {move} (ply {(Game != null ? Game.ply : -1)})");
        }

        /// <summary>Plays every seat that is a person at this device: the computer's choice, through the path a click takes.</summary>
        private IEnumerator Pilot()
        {
            while (true)
            {
                yield return new WaitForSeconds(PilotPace);
                if (!pilot || manager.Table == null || Game == null || Game.IsOver)
                {
                    continue;
                }
                if (!manager.WaitingForHuman || (hold != null && hold()))
                {
                    continue;
                }
                Move move = CheckersAI.Choose(Game, 1, random);
                if (move != null && !manager.TryPlay(move))
                {
                    Write($"the manager refused {move}");
                }
            }
        }

        private IEnumerator ShotWhen(string name, Func<bool> condition, float delay)
        {
            while (!condition())
            {
                if (Over)
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
                    Write($"timed out after {timeout} s (ply {(Game != null ? Game.ply : -1)}, state {manager.State})");
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
            Write($"{file}: state {manager.State}, ply {(Game != null ? Game.ply : -1)}, waiting {manager.WaitingForHuman}");
            yield return null;
            yield return null;
        }

        private void Write(string line)
        {
            log?.WriteLine($"{Time.realtimeSinceStartup:0.0} {line}");
        }
    }
}
