using System;
using System.Collections;
using System.IO;
using System.Linq;
using Gamebox;
using Gamebox.Online;
using UnityEngine;

namespace Portfolio.Heroes
{
    /// <summary>
    /// Started with <c>-heroes-online host &lt;folder&gt;</c> or <c>-heroes-online join &lt;folder&gt;</c>, it plays an
    /// online scenario by itself and saves a screenshot of every step into the folder, then quits: the host opens a
    /// room and starts it once somebody joined and is ready, the other one joins the first room it sees, and both take
    /// their turns. Every day each of them writes down the checksum of its own copy of the game, so the two logs show
    /// whether the clients really stayed in step. Run two players (with <c>-gamebox-identity</c> to tell them apart,
    /// and <c>-gamebox-server</c> / <c>-gamebox-database</c> for a test server) to try a game end to end. Without the
    /// argument it does nothing.
    /// </summary>
    public class HeroesOnlineTour : MonoBehaviour
    {
        private const string Argument = "-heroes-online";

        private bool host;
        private string mode;
        private string folder;
        private HeroesGameManager manager;
        private StreamWriter log;
        private int shot;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 2; i++)
            {
                if (args[i] != Argument)
                {
                    continue;
                }
                var host = new GameObject("Heroes Online Tour");
                DontDestroyOnLoad(host);
                var tour = host.AddComponent<HeroesOnlineTour>();
                tour.mode = args[i + 1];
                tour.host = tour.mode == "host";
                tour.folder = args[i + 2];
                return;
            }
        }

        private HeroesGame Game => manager != null ? manager.Game : null;

        private IEnumerator Start()
        {
            Directory.CreateDirectory(folder);
            log = new StreamWriter(Path.Combine(folder, $"{mode}.log")) { AutoFlush = true };
            Application.logMessageReceived += (message, stack, type) =>
            {
                if (type == LogType.Error || type == LogType.Exception)
                {
                    Note($"{type}: {message}");
                }
            };
            Application.runInBackground = true;
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
            ServerClient server = manager.Online.ServerClient;
            server.Failed += error => Note("server: " + error);
            manager.OpenOnline();
            yield return WaitFor(() => server.IsLoggedIn, 30f, "login");
            if (!server.IsLoggedIn)
            {
                yield return Fail("no login");
                yield break;
            }
            Note($"logged in as {server.LocalPlayer.Name}");
            yield return new WaitForSeconds(0.5f);

            if (host)
            {
                // A small random map with no clock and no computer players: two realms, side by side.
                string options = manager.Online.ComposeOptions(-1, "size=0;bots=0;turn=0;treasure=2;monsters=1");
                server.CreateRoom("Tour room", -1, options, 2, error => Note("create room: " + (error ?? "ok")));
                yield return WaitFor(() => server.InRoom, 15f, "own room");
                yield return Shot("room");
                yield return WaitFor(() => server.Members.Count >= 2 && server.Members.All(member => member.Ready || server.IsLocal(member)),
                    150f, "a ready guest");
                yield return new WaitForSeconds(0.5f);
                yield return Shot("ready");
                server.StartRoom(error => Note("start room: " + (error ?? "ok")));
            }
            else
            {
                yield return WaitFor(() => server.Rooms.Any(room => room.Phase != RoomPhase.Playing), 150f, "a room");
                yield return Shot("rooms");
                RoomInfo open = server.Rooms.First(room => room.Phase != RoomPhase.Playing);
                server.JoinRoom(open.Id, error => Note("join room: " + (error ?? "ok")));
                yield return WaitFor(() => server.InRoom, 15f, "the room");
                server.SetReady(true, error => Note("ready: " + (error ?? "ok")));
                yield return new WaitForSeconds(0.7f);
                yield return Shot("joined");
            }

            yield return WaitFor(() => manager.IsOnlineGame && Game != null, 90f, "the game");
            if (Game == null)
            {
                yield return Fail("the scenario never started");
                yield break;
            }
            Note($"playing seat {manager.LocalSeat} of {Game.State.players.Count}, map " +
                 $"{Game.State.map.grid.columns}x{Game.State.map.grid.rows}, seed {Game.State.seed}");
            yield return new WaitForSeconds(2f);
            yield return Shot("map");
            yield return Play(4);
            yield return Shot("end");
            Note($"done on day {Game.State.day}");
            yield return Quit();
        }

        /// <summary>Takes turns until <paramref name="days"/> of this seat have passed.</summary>
        private IEnumerator Play(int days)
        {
            int passed = 0;
            int idle = 0;
            int lastDay = -1;
            int moves = 0;
            int counted = -1;
            // The other client has to get its turns too, so a day here is a couple of moves and then an end.
            while (passed < days && Game != null && !Game.IsOver && idle < 3600)
            {
                if (Game.State.day != lastDay)
                {
                    lastDay = Game.State.day;
                    Note($"day {lastDay} begins");
                }
                if (manager.Lockstep.Applied != counted && manager.Lockstep.Applied % 5 == 0 && !Game.HasEvents)
                {
                    // The two clients write the same line for the same number of actions, or they have drifted apart.
                    counted = manager.Lockstep.Applied;
                    Note($"after {counted:000} actions: checksum {manager.Lockstep.Checksum():X8}");
                }
                if (Game.State.pending.Count > 0 && Game.State.pending[0].player == manager.LocalSeat)
                {
                    manager.Commands.Choose(0);
                    yield return new WaitForSeconds(0.5f);
                    idle = 0;
                    continue;
                }
                if (Game.InBattle)
                {
                    if (manager.WaitingForHuman && Game.Battle.Current != null &&
                        Game.Battle.PlayerOf(Game.Battle.Current.side) == manager.LocalSeat)
                    {
                        manager.Commands.BattleDefend(Game.Battle.current);
                        yield return new WaitForSeconds(0.4f);
                        idle = 0;
                        continue;
                    }
                    yield return null;
                    idle++;
                    continue;
                }
                if (manager.WaitingForHuman && Game.WaitingPlayer == manager.LocalSeat)
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
                        if (after != null && after.movement == before)
                        {
                            manager.Commands.Sleep(hero.id, true);
                            yield return new WaitForSeconds(0.4f);
                        }
                        idle = 0;
                        continue;
                    }
                    passed++;
                    moves = 0;
                    Note($"ending day {Game.State.day}");
                    manager.Commands.EndTurn();
                    yield return new WaitForSeconds(1.2f);
                    idle = 0;
                    continue;
                }
                yield return null;
                idle++;
                if (idle % 600 == 0)
                {
                    Note($"waiting: seat {Game.WaitingPlayer}, ours {manager.LocalSeat}, human {manager.WaitingForHuman}, " +
                         $"battle {Game.InBattle}, choices {Game.State.pending.Count}, busy {manager.HeroesUI.Busy}, " +
                         $"actions {manager.Lockstep.Applied}");
                }
            }
            string ending = Game == null ? "gone"
                : Game.IsOver ? $"over, winner {Game.State.winner}"
                : "running";
            Note($"play loop over: days {passed}/{days}, idle {idle}, game {ending}, " +
                 $"checksum {(Game != null ? manager.Lockstep.Checksum().ToString("X8") : "-")} after " +
                 $"{(Game != null ? manager.Lockstep.Applied : 0)} actions");
        }

        /// <summary>The nearest cell worth walking to.</summary>
        private int Prize(HeroState hero)
        {
            GameState state = Game.State;
            int best = -1;
            int closest = int.MaxValue;
            foreach (MapObject what in state.objects)
            {
                if (what.removed || what.kind == ObjectKind.Monster || what.owner == hero.owner)
                {
                    continue;
                }
                int distance = state.map.grid.Distance(hero.cell, what.cell);
                if (distance <= 0 || distance >= closest)
                {
                    continue;
                }
                MovePlan plan = manager.PlanFor(hero, what.cell);
                if (plan != null && plan.Cells.Count > 0)
                {
                    closest = distance;
                    best = what.cell;
                }
            }
            return best;
        }

        private IEnumerator WaitFor(Func<bool> until, float patience, string what)
        {
            float deadline = Time.unscaledTime + patience;
            while (!until() && Time.unscaledTime < deadline)
            {
                yield return null;
            }
            Note(until() ? $"{what}: ready" : $"{what}: gave up after {patience:0}s");
        }

        private IEnumerator Shot(string name)
        {
            yield return new WaitForEndOfFrame();
            string file = Path.Combine(folder, $"{mode}_{++shot:00}_{name}.png");
            ScreenCapture.CaptureScreenshot(file);
            Note($"shot {Path.GetFileName(file)}");
            yield return new WaitForSeconds(0.8f);
        }

        private IEnumerator Fail(string why)
        {
            Note("failed: " + why);
            yield return Quit();
        }

        private IEnumerator Quit()
        {
            yield return new WaitForSeconds(0.6f);
            log?.Flush();
            Application.Quit();
        }

        private void Note(string text)
        {
            log?.WriteLine($"[{DateTime.Now:HH:mm:ss}] {text}");
        }
    }
}
