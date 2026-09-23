using System;
using System.Collections;
using System.IO;
using Gamebox;
using Portfolio.Heroes.UI;
using UnityEngine;

namespace Portfolio.Heroes
{
    /// <summary>
    /// Development players only. Started with <c>-heroes-tour &lt;folder&gt;</c>, it plays a scenario by itself (the
    /// title screen, the map, a hero walking, a town, a hero's book, a battle on the hexagons and the end of a turn)
    /// and saves a screenshot of every step into the folder, then quits. Without the argument it does nothing.
    /// </summary>
    public class HeroesTour : MonoBehaviour
    {
        private string folder;
        private HeroesGameManager manager;
        private StreamWriter log;
        private int shot;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (!Debug.isDebugBuild || Application.isEditor)
            {
                return;
            }
            string target = MobilePlatform.ArgumentValue("-heroes-tour");
            if (string.IsNullOrEmpty(target) || FindAnyObjectByType<HeroesTour>() != null)
            {
                return;
            }
            var host = new GameObject("Heroes Tour");
            DontDestroyOnLoad(host);
            host.AddComponent<HeroesTour>().folder = target;
        }

        private HeroesGame Game => manager != null ? manager.Game : null;

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
            Application.runInBackground = true;
            while ((manager = FindAnyObjectByType<HeroesGameManager>()) == null)
            {
                yield return null;
            }
            yield return new WaitForSeconds(2.5f);
            yield return Shot("title");

            Write("Choosing the first chapter.");
            manager.HeroesUI.OpenCampaign();
            yield return new WaitForSeconds(1f);
            yield return Shot("campaign");
            manager.BeginScenario(null, 0);
            yield return WaitForMap(40f);
            if (Game == null)
            {
                Write("The scenario never started.");
                yield return Quit();
                yield break;
            }
            Write($"Map: {Game.State.map.grid.columns}x{Game.State.map.grid.rows}, " +
                  $"{Game.State.objects.Count} objects, {Game.State.heroes.Count} heroes, {Game.State.towns.Count} towns.");
            Terrain terrain = manager.Map.Terrain;
            TerrainData data = terrain.terrainData;
            Write($"Terrain: size {data.size}, material {(terrain.materialTemplate != null ? terrain.materialTemplate.shader.name : "NONE")}, " +
                  $"{data.terrainLayers.Length} layers, first {(data.terrainLayers.Length > 0 && data.terrainLayers[0] != null ? data.terrainLayers[0].name + " tex " + (data.terrainLayers[0].diffuseTexture != null) : "null")}, " +
                  $"trees {data.treeInstanceCount}, height at hero {terrain.SampleHeight(manager.Map.Point(Game.State.heroes[0].cell))}");
            yield return WaitForTurn(30f);
            yield return Shot("adventure");

            // The hero's own book, and the town he came from.
            HeroState hero = manager.Selected;
            if (hero != null)
            {
                manager.HeroesUI.OpenHero();
                yield return new WaitForSeconds(0.8f);
                yield return Shot("hero");
                manager.HeroesUI.Sheet.Close();
            }
            PlayerState me = manager.ViewerState;
            if (me != null && me.towns.Count > 0)
            {
                manager.HeroesUI.OpenTown();
                yield return new WaitForSeconds(1f);
                yield return Shot("town");
                manager.HeroesUI.Town.Close();
                yield return new WaitForSeconds(0.4f);
            }

            // Walk the hero as far as the day carries him, toward the nearest thing worth having.
            if (hero != null)
            {
                int target = NearestPrize(hero);
                if (target >= 0)
                {
                    Write($"Sending {hero.Name} from cell {hero.cell} to {target}.");
                    manager.Commands.MoveHero(hero.id, target);
                    yield return WaitForTurn(40f);
                    yield return Shot("moved");
                }
            }

            // Let the days pass until a battle starts, taking a picture of each one.
            int days = 0;
            int idle = 0;
            while (days < 6 && Game != null && !Game.IsOver && idle < 600)
            {
                if (Game.State.pending.Count > 0)
                {
                    // A question has to be answered before anything else moves.
                    yield return new WaitForSeconds(0.6f);
                    yield return Shot($"choice_{days:00}");
                    manager.Commands.Choose(0);
                    yield return new WaitForSeconds(0.4f);
                    idle = 0;
                    continue;
                }
                if (Game.InBattle)
                {
                    yield return new WaitForSeconds(1.2f);
                    yield return Shot($"battle_{days:00}");
                    yield return Fight(30f);
                    idle = 0;
                    continue;
                }
                if (manager.WaitingForHuman)
                {
                    HeroState next = manager.NextHero();
                    if (next != null && next.movement > 0)
                    {
                        int target = NearestPrize(next);
                        int before = next.movement;
                        if (target >= 0 && target != next.cell)
                        {
                            manager.Select(next);
                            manager.Commands.MoveHero(next.id, target);
                            yield return WaitForTurn(30f);
                            HeroState after = Game != null ? Game.State.Hero(next.id) : null;
                            // A hero who cannot get any further goes to sleep rather than be asked again.
                            idle = after != null && after.alive && after.movement == before ? idle + 1 : 0;
                            if (idle > 2 && after != null)
                            {
                                manager.Commands.Sleep(next.id, true);
                            }
                            continue;
                        }
                        manager.Commands.Sleep(next.id, true);
                        yield return null;
                        idle++;
                        continue;
                    }
                    days++;
                    idle = 0;
                    Write($"Day {Game.State.day} ends.");
                    manager.Commands.EndTurn();
                    yield return WaitForTurn(60f);
                    if (days % 4 == 0)
                    {
                        yield return Shot($"day_{Game.State.day:00}");
                    }
                    continue;
                }
                yield return null;
                idle++;
            }
            if (idle >= 600)
            {
                Write($"Nothing moved: waiting on seat {(Game != null ? Game.WaitingPlayer : -1)}, " +
                      $"battle {(Game != null && Game.InBattle)}, choices {(Game != null ? Game.State.pending.Count : 0)}, " +
                      $"human {manager.WaitingForHuman}, busy {manager.HeroesUI.Busy}.");
            }
            yield return Shot("last");
            Write($"Tour over on day {(Game != null ? Game.State.day : 0)}.");
            yield return Quit();
        }

        /// <summary>The nearest cell of something the hero would want: a pile, a mine, a town, a building.</summary>
        private int NearestPrize(HeroState hero)
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
                if (distance > 0 && distance < closest)
                {
                    MovePlan plan = manager.PlanFor(hero, what.cell);
                    if (plan != null && plan.Cells.Count > 0)
                    {
                        closest = distance;
                        best = what.cell;
                    }
                }
            }
            return best;
        }

        /// <summary>Plays a battle out by defending with everything until it ends.</summary>
        private IEnumerator Fight(float patience)
        {
            float until = Time.unscaledTime + patience;
            while (Game != null && Game.InBattle && Time.unscaledTime < until)
            {
                if (manager.WaitingForHuman && Game.Battle.Current != null)
                {
                    manager.Commands.BattleDefend(Game.Battle.current);
                }
                yield return null;
            }
            yield return new WaitForSeconds(0.6f);
        }

        private IEnumerator WaitForMap(float patience)
        {
            float until = Time.unscaledTime + patience;
            while (Game == null && Time.unscaledTime < until)
            {
                yield return null;
            }
        }

        private IEnumerator WaitForTurn(float patience)
        {
            float until = Time.unscaledTime + patience;
            yield return new WaitForSeconds(0.3f);
            while (Game != null && !Game.IsOver && !manager.WaitingForHuman && Time.unscaledTime < until)
            {
                yield return null;
            }
            yield return new WaitForSeconds(0.25f);
        }

        private IEnumerator Shot(string name)
        {
            yield return new WaitForEndOfFrame();
            string file = Path.Combine(folder, $"{++shot:00}_{name}.png");
            ScreenCapture.CaptureScreenshot(file);
            Write($"shot {file}");
            yield return new WaitForSeconds(0.8f);
        }

        private IEnumerator Quit()
        {
            yield return new WaitForSeconds(0.5f);
            log?.Flush();
            Application.Quit();
        }

        private void Write(string text)
        {
            log?.WriteLine($"[{DateTime.Now:HH:mm:ss}] {text}");
        }
    }
}
