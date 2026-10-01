using System;
using System.Collections;
using System.IO;
using Gamebox;
using Gamebox.Online;
using Gamebox.UI;
using UnityEngine;

namespace Portfolio.MemoryCards
{
    /// <summary>
    /// Started with <c>-memorycards-tour &lt;folder&gt;</c>, it walks through the game with the
    /// <see cref="MemoryCardsAutopilot"/> (level select, memorize, a win, bombs, the parade, triplets, ice, the pause
    /// menu and settings, the wild card, the endless run, a loss and a versus game for three at one device: two people
    /// and a computer, set up on the shared local play screen, with the hand-over screen on) and saves a screenshot of
    /// every step into the folder, then quits. <c>-memorycards-versus &lt;folder&gt;</c> plays only the versus game. The
    /// player's saved progress is cleared at the end. Without an argument it does nothing.
    /// </summary>
    public class MemoryCardsTour : MonoBehaviour
    {
        private string folder;
        private bool versusOnly;
        private MemoryCardsGameManager manager;
        private MemoryCardsAutopilot pilot;
        private StreamWriter log;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == "-memorycards-tour" || args[i] == "-memorycards-versus")
                {
                    var host = new GameObject("Memory Cards Tour");
                    DontDestroyOnLoad(host);
                    var tour = host.AddComponent<MemoryCardsTour>();
                    tour.folder = args[i + 1];
                    tour.versusOnly = args[i] == "-memorycards-versus";
                    return;
                }
            }
        }

        private IEnumerator Start()
        {
            Directory.CreateDirectory(folder);
            log = new StreamWriter(Path.Combine(folder, "tour.log")) { AutoFlush = true };
            while ((manager = FindAnyObjectByType<MemoryCardsGameManager>()) == null || manager.Progress == null)
            {
                yield return null;
            }
            pilot = gameObject.AddComponent<MemoryCardsAutopilot>();
            pilot.manager = manager;
            pilot.enabled = false;
            MemoryCardsCampaign campaign = (MemoryCardsCampaign)manager.Campaign;
            int levels = campaign.Count;
            manager.Progress.ResetAll(levels);
            manager.ReturnToLevelSelect();
            yield return new WaitForSeconds(1.5f);
            if (versusOnly)
            {
                manager.Progress.RecordLevel(0, 3, 1000);
                yield return Versus();
                yield return Finish(levels);
                yield break;
            }
            yield return Shot("01_title");

            int[] stars = { 3, 3, 2, 3, 1, 3, 2, 3, 1 };
            for (int i = 0; i < stars.Length; i++)
            {
                manager.Progress.RecordLevel(i, stars[i], 1000 + i * 350);
            }
            manager.Progress.RecordGame(57, 4);
            manager.SelectWorld(1);
            yield return new WaitForSeconds(1.2f);
            yield return Shot("02_jungle_select");

            // Peek-a-Boo: the memorize phase, play and the results.
            yield return Play(0, 2, 0.2f, 0.45f);
            yield return WaitFor(() => manager.CurrentPhase == "Preview", 10f);
            yield return new WaitForSeconds(1.3f);
            yield return Shot("03_memorize");
            yield return WaitFor(() => manager.Round != null && manager.Round.MatchedSets >= 4, 40f);
            yield return new WaitForSeconds(0.3f);
            yield return Shot("04_playing");
            yield return WaitFor(() => !manager.IsGameRunning, 90f);
            yield return new WaitForSeconds(2.2f);
            yield return Shot("05_results_win");

            // Snake Pit: a bomb goes off.
            yield return Play(1, 8, 0.1f, 0.5f);
            yield return WaitFor(() => manager.IsPlaying, 15f);
            pilot.avoidBombs = false;
            yield return WaitFor(() => manager.Round != null && manager.Round.BombsHit > 0, 20f);
            pilot.avoidBombs = true;
            yield return new WaitForSeconds(0.55f);
            yield return Shot("06_bomb");

            // Parrot Parade.
            yield return Play(1, 9, 0.15f, 0.5f);
            yield return WaitFor(() => manager.Round != null && manager.Round.MatchedSets >= 3, 40f);
            yield return new WaitForSeconds(0.4f);
            yield return Shot("07_parade");

            // Triple Trouble (opens once the parade is done).
            manager.Progress.RecordLevel(9, 2, 1500);
            yield return Play(1, 10, 0.1f, 0.45f);
            yield return WaitFor(() => manager.Round != null && manager.Round.MatchedSets >= 2, 40f);
            yield return new WaitForSeconds(0.2f);
            yield return Shot("08_triplets");

            // Welcome to the Jungle: the wild card.
            pilot.wildFirst = true;
            yield return Play(1, 6, 0.05f, 0.5f);
            yield return WaitFor(() => manager.Round != null && manager.Round.MatchedSets >= 1, 30f);
            yield return new WaitForSeconds(0.5f);
            yield return Shot("09_wild");
            pilot.wildFirst = false;

            // Frosty Shores opens with more stars; Thin Ice.
            for (int i = 9; i < 12; i++)
            {
                manager.Progress.RecordLevel(i, 3, 2000);
            }
            manager.ReturnToLevelSelect();
            manager.SelectWorld(2);
            yield return new WaitForSeconds(1.2f);
            yield return Shot("10_frosty_select");
            yield return Play(2, 12, 0.2f, 0.5f);
            yield return WaitFor(() => manager.IsPlaying, 15f);
            yield return new WaitForSeconds(3f);
            yield return Shot("11_ice");

            // Pause menu and the settings panel.
            manager.TransitionState(BaseGameState.Paused);
            yield return new WaitForSecondsRealtime(0.8f);
            yield return Shot("12_pause");
            manager.CardsUI.ShowSettings();
            yield return new WaitForSecondsRealtime(0.8f);
            yield return Shot("13_settings");
            manager.CardsUI.HideSettings();
            manager.TransitionState(BaseGameState.Running);

            // The endless run.
            manager.ReturnToLevelSelect();
            manager.SelectWorld(3);
            yield return new WaitForSeconds(1.2f);
            yield return Shot("14_carnival_select");
            yield return Play(3, campaign.IndexOf(LevelKind.Endless), 0.1f, 0.3f);
            yield return WaitFor(() => manager.Round != null && manager.Round.Rules.Cards >= 16 && manager.IsPlaying, 90f);
            yield return new WaitForSeconds(1.5f);
            yield return Shot("15_endless");

            // Hen Party lost on purpose.
            yield return Play(0, 5, 1f, 0.35f);
            yield return WaitFor(() => !manager.IsGameRunning, 60f);
            yield return new WaitForSeconds(2f);
            yield return Shot("16_results_lost");

            yield return Versus();
            yield return Finish(levels);
        }

        /// <summary>
        /// A versus game for three at one device: the setup (two people and a computer, the hand-over screen on), the
        /// curtain before a person's turn, the computer's turn, a turn nobody uses, the results. The autopilot plays the
        /// people's turns (on the computer's it is ignored) and the tour lifts the curtain for them.
        /// </summary>
        private IEnumerator Versus()
        {
            pilot.enabled = false;
            if (!manager.State.Is(BaseGameState.Initialization))
            {
                manager.ReturnToLevelSelect();
                yield return new WaitForSeconds(0.5f);
            }
            manager.SelectWorld(0);
            manager.SelectLevel(1);
            yield return new WaitForSeconds(0.8f);
            yield return Shot("17_versus_select");

            // The setup as the players would leave it; the screen opens on the last setup of the game.
            var setup = new LocalMatch(GameType.MemoryCards, manager.LocalPlay) { Count = 3, HandOver = true };
            setup[0].Name = "Maya";
            setup[1].Name = "Leo";
            setup[2].Kind = SeatKind.Computer;
            setup[2].Level = 1;
            setup.SetOption(LocalVersus.ClockOption, LocalVersus.ClockIndex(15f));
            setup.Remember();
            manager.OpenLocalPlay();
            yield return new WaitForSecondsRealtime(0.8f);
            yield return Shot("18_versus_setup");
            LocalPlayUI ui = LocalPlayUI.For(manager);
            Write($"setup: {ui.Match.Count} players, hand-over {ui.Match.HandOver}, {ui.Match.Humans} people");
            ui.StartMatch();

            pilot.mistakeRate = 0.35f;
            pilot.interval = 0.5f;
            pilot.enabled = true;
            Write("playing a versus game for three");
            yield return WaitFor(() => manager.IsHandingOver, 20f);
            yield return new WaitForSecondsRealtime(0.6f);
            yield return Shot("19_versus_hand_over");
            TurnCurtain curtain = TurnCurtain.For(manager);
            curtain.Lift();

            // Turns go round; the curtain is lifted for every person, and the computer plays its own.
            bool computerShown = false;
            bool timeUpShown = false;
            float start = Time.realtimeSinceStartup;
            while (manager.IsVersus && (manager.IsGameRunning || manager.IsHandingOver) && Time.realtimeSinceStartup - start < 300f)
            {
                if (manager.IsHandingOver)
                {
                    yield return new WaitForSecondsRealtime(0.7f);
                    if (!timeUpShown && manager.Versus.TurnNumber >= 4)
                    {
                        // Nobody moves: the clock, which started only once the player was ready, runs out.
                        timeUpShown = true;
                        pilot.enabled = false;
                        curtain.Lift();
                        int turn = manager.Versus.TurnNumber;
                        yield return WaitFor(() => !manager.IsVersus || manager.Versus.TurnNumber > turn, 15f + 8f);
                        yield return new WaitForSeconds(0.4f);
                        yield return Shot("21_versus_time_up");
                        Write(manager.IsVersus ? $"turn {turn} ran out, now turn {manager.Versus.TurnNumber} of seat {manager.Versus.Current}" : "the versus game is gone");
                        pilot.mistakeRate = 0.1f;
                        pilot.interval = 0.3f;
                        pilot.enabled = true;
                        continue;
                    }
                    curtain.Lift();
                }
                if (!computerShown && manager.IsComputerTurn && manager.Round != null && manager.Round.Revealed.Count > 0)
                {
                    computerShown = true;
                    yield return new WaitForSeconds(0.5f);
                    yield return Shot("20_versus_computer");
                }
                yield return null;
            }
            yield return WaitFor(() => !manager.IsGameRunning, 30f);
            yield return new WaitForSeconds(2.4f);
            yield return Shot("22_versus_results");
            if (manager.IsVersus)
            {
                foreach (VersusSeat seat in manager.Versus.Standings())
                {
                    Write($"{seat.Name}: {seat.Score} points, {seat.Sets} sets, {seat.Mistakes} mistakes");
                }
            }

            // Back to the level select: the game at this device is over.
            pilot.enabled = false;
            manager.ReturnToLevelSelect();
            yield return new WaitForSeconds(0.5f);
            Write(manager.InLocalMatch ? "the local match is still on" : "the local match ended");
        }

        private IEnumerator Finish(int levels)
        {
            pilot.enabled = false;
            manager.Progress.ResetAll(levels);
            // Everything the tour saved goes, except who the player is online: the token is the only key to that user.
            string server = manager.online != null && manager.online.ServerClient != null ? manager.online.ServerClient.ServerUri : null;
            string token = server != null ? IdentityTokens.Load(server) : null;
            PlayerPrefs.DeleteAll();
            if (!string.IsNullOrEmpty(token))
            {
                IdentityTokens.Save(server, token);
            }
            PlayerPrefs.Save();
            Write("tour finished");
            log.Dispose();
            yield return null;
            Application.Quit();
        }

        private IEnumerator Play(int world, int level, float mistakes, float interval)
        {
            pilot.enabled = false;
            if (manager.IsGameRunning || manager.State.Is(BaseGameState.Paused) || manager.State.Is(BaseGameState.Victory) || manager.State.Is(BaseGameState.GameOver))
            {
                manager.ReturnToLevelSelect();
                yield return new WaitForSeconds(0.5f);
            }
            manager.SelectWorld(world);
            manager.SelectLevel(level);
            yield return new WaitForSeconds(0.4f);
            manager.PlaySelectedLevel();
            pilot.mistakeRate = mistakes;
            pilot.interval = interval;
            pilot.enabled = true;
            Write($"playing level {level} in world {world}");
        }

        private IEnumerator WaitFor(Func<bool> condition, float timeout)
        {
            float start = Time.realtimeSinceStartup;
            while (!condition())
            {
                if (Time.realtimeSinceStartup - start > timeout)
                {
                    Write($"timed out after {timeout} s (phase {manager.CurrentPhase})");
                    yield break;
                }
                yield return null;
            }
        }

        private IEnumerator Shot(string name)
        {
            yield return new WaitForEndOfFrame();
            string path = Path.Combine(folder, name + ".png");
            ScreenCapture.CaptureScreenshot(path);
            Write($"{name}: phase {manager.CurrentPhase}, state {manager.State}, score {manager.PlayerScore}");
            yield return null;
            yield return null;
        }

        private void Write(string line)
        {
            log?.WriteLine($"{Time.realtimeSinceStartup:0.0} {line}");
        }
    }
}
