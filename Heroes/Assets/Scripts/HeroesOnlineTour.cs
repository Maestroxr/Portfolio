using System.Collections;
using System.Collections.Generic;
using Gamebox;
using Gamebox.Online;
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
    /// puts a clock on the turns (shown on the bar of the map and on the battle bar).
    /// </summary>
    public class HeroesOnlineTour : OnlineTour
    {
        private const string Argument = "-heroes-online";

        /// <summary>The room of the tour: a small random map with no clock and no computer players, fought out on battlefields.</summary>
        private const string TourOptions = "size=0;bots=0;turn=0;treasure=2;monsters=2;battles=1";

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

        private HeroesGameManager manager;

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
            yield return new WaitForSeconds(0.5f);

            if (Hosting)
            {
                yield return HostRoom("Tour room", -1, manager.Online.ComposeOptions(-1, Options), 2);
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
            Note($"playing seat {manager.LocalSeat} of {Game.State.players.Count}, map " +
                 $"{Game.State.map.grid.columns}x{Game.State.map.grid.rows}, seed {Game.State.seed}, battles {Game.State.rules.battleStyle}");
            yield return new WaitForSeconds(2f);
            yield return Shot("03_map");
            yield return Play(Days);
            yield return Shot("04_end");
            Note($"done on day {(Game != null ? Game.State.day : -1)}");
            yield return new WaitForSeconds(0.6f);
            Quit();
        }

        /// <summary>Takes turns until <paramref name="days"/> of this seat have passed.</summary>
        private IEnumerator Play(int days)
        {
            int passed = 0;
            int idle = 0;
            int lastDay = -1;
            int moves = 0;
            int battles = 0;
            int results = 0;
            bool fighting = false;
            // The other client has to get its turns too, so a day here is a couple of moves and then an end.
            while (passed < days && Game != null && !Game.IsOver && idle < 3600)
            {
                if (Game.State.day != lastDay)
                {
                    lastDay = Game.State.day;
                    Note($"day {lastDay} begins");
                }
                if (Game.InBattle != fighting)
                {
                    fighting = Game.InBattle;
                    if (fighting)
                    {
                        battles++;
                        // Whose it is comes from its sides: IsLocalBattle is set only when the screen reaches its start.
                        bool ours = Game.Battle.attackerPlayer == manager.LocalSeat || Game.Battle.defenderPlayer == manager.LocalSeat;
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
                    // The results of a battle of this seat: looked at, and closed as a player would.
                    results++;
                    if (results == 1)
                    {
                        yield return new WaitForSeconds(0.6f);
                        yield return Shot("06_results");
                    }
                    Note($"results of battle {battles} closed");
                    manager.HeroesUI.Bar.Results.Close();
                    yield return new WaitForSeconds(0.5f);
                    idle = 0;
                    continue;
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
                    BattleStack current = Game.Battle.Current;
                    if (manager.WaitingForHuman && manager.OnlineDeciding() && current != null &&
                        Game.Battle.PlayerOf(current.side) == manager.LocalSeat)
                    {
                        // The seat fights the way the computer would.
                        GameCommand move = BattleAI.Next(Game) ?? GameCommand.BattleDefend(manager.LocalSeat, current.id);
                        if (!manager.Online.Submit(move))
                        {
                            manager.Commands.BattleDefend(current.id);
                        }
                        yield return new WaitForSeconds(0.4f);
                        idle = 0;
                        continue;
                    }
                    yield return null;
                    // Another seat's battle takes as long as it takes (it is played out on that screen); only a battle
                    // that waits on this seat and gets nowhere counts as being stuck.
                    if (current != null && Game.Battle.PlayerOf(current.side) == manager.LocalSeat)
                    {
                        idle++;
                    }
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
                        if (after != null && after.alive && after.movement == before && !Game.InBattle)
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
            Note($"play loop over: days {passed}/{days}, battles {battles}, idle {idle}, game {ending}, " +
                 $"checksum {(Game != null ? manager.Lockstep.Checksum().ToString("X8") : "-")} after " +
                 $"{(Game != null ? manager.Lockstep.Applied : 0)} actions");
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
