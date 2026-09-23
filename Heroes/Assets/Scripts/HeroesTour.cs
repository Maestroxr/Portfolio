using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Gamebox;
using Portfolio.Heroes.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Portfolio.Heroes
{
    /// <summary>
    /// Development players only. Started with <c>-heroes-tour &lt;folder&gt;</c>, it plays the battles of the game by
    /// itself and saves a screenshot of every step into the folder, then quits; without the argument it does nothing.
    /// <c>-heroes-tour-plan field,map,siege,walls,resume,teardown</c> picks the battles (all six by default): a battle
    /// against a wandering army on a battlefield of its own, the same on the map, the siege of a walled town, a close
    /// look at the wall of a town with three arrow towers (its gate, a breach, a tower shot down), a battle taken up
    /// again from a copy of the game made in the middle of it, as a saved game would be, and a scenario left for
    /// the title while its battlefield is loading and again while it is up. The step <c>skirmish</c> (not in the
    /// plan unless asked for) starts a skirmish map on a large, rich, deadly and hard setting and logs what the map
    /// came out as. It changes a copy of the settings only.
    /// For each it starts the first chapter in that style, stands the hero a few steps from his foe (the one thing it
    /// does behind the rules' back, with a few spells for his book and, for the siege, the walls and a bigger army),
    /// walks him in, and fights as a player would: it points at the field, holds the right button on a stack, aims and
    /// casts a spell and clicks a move, a shot and a blow, then leaves the rest to auto combat, which sends the
    /// computer's battle sense as the player's own commands. It closes the results and looks at the map it comes back to.
    /// </summary>
    public class HeroesTour : MonoBehaviour
    {
        private string folder;
        private string[] plan = { "field", "map", "siege", "walls", "resume", "teardown" };
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
            var tour = host.AddComponent<HeroesTour>();
            tour.folder = target;
            string steps = MobilePlatform.ArgumentValue("-heroes-tour-plan");
            if (!string.IsNullOrEmpty(steps))
            {
                tour.plan = steps.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            }
        }

        private HeroesGame Game => manager != null ? manager.Game : null;

        private BattleBar Bar => manager.HeroesUI.Bar;

        private IEnumerator Start()
        {
            Directory.CreateDirectory(folder);
            log = new StreamWriter(Path.Combine(folder, "tour.log")) { AutoFlush = true };
            Application.logMessageReceived += (message, stack, type) =>
            {
                if (type == LogType.Exception || type == LogType.Error || type == LogType.Warning && message.StartsWith("Heroes"))
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
            // A copy of the settings in use for the tour to change (where battles are fought, how fast they play):
            // neither the defaults nor the player's own saved settings are touched.
            HeroesSettings own = Instantiate(manager.Options);
            own.name = "Heroes Tour Settings";
            manager.Settings = own;
            yield return Shot("title");
            float speed = manager.Options.battleSpeed;
            foreach (string step in plan)
            {
                Write($"== {step}");
                manager.Options.battleSpeed = speed;
                switch (step.Trim())
                {
                    case "field":
                        yield return Wanderers(BattleStyle.Battlefield, "field");
                        break;
                    case "map":
                        yield return Wanderers(BattleStyle.OnTheMap, "map");
                        break;
                    case "siege":
                        yield return Siege();
                        break;
                    case "walls":
                        yield return Walls();
                        break;
                    case "resume":
                        yield return Resume();
                        break;
                    case "teardown":
                        yield return Teardown();
                        break;
                    case "skirmish":
                        yield return Skirmish();
                        break;
                    default:
                        Write($"No such step: {step}.");
                        break;
                }
            }
            manager.Options.battleSpeed = speed;
            yield return Shot("last");
            Write("Tour over.");
            yield return Quit();
        }

        // ------------------------------------------------------------------ the battles

        /// <summary>A battle against the weakest wandering army near the hero, fought in <paramref name="style"/>.</summary>
        private IEnumerator Wanderers(BattleStyle style, string tag)
        {
            yield return Scenario(style);
            HeroState hero = FirstHero();
            int goal = hero != null ? Approach(hero) : -1;
            if (goal < 0)
            {
                Write("No hero, or no wandering army with free ground near it.");
                yield break;
            }
            yield return new WaitForSeconds(1.2f);
            yield return Shot($"{tag}_map_before");
            manager.Commands.MoveHero(hero.id, goal);
            yield return Fight(tag, style == BattleStyle.Battlefield);
            yield return AfterBattle(tag);
        }

        /// <summary>
        /// Readies the hero for a fight with the wandering army he stands the best chance against (a few spells, twice
        /// the troops, so the first battle of the chapter is one he can win in a few rounds) and stands him near it.
        /// Returns the cell to walk to (the ground next to the army, which it guards), or -1.
        /// </summary>
        private int Approach(HeroState hero)
        {
            Spellbook(hero);
            foreach (ArmySlot slot in hero.army.slots)
            {
                if (!slot.IsEmpty)
                {
                    slot.count *= 2;
                }
            }
            foreach (MapObject monster in Wandering(hero))
            {
                int goal = StandNear(hero, Game.State.map.grid.Neighbors(monster.cell));
                if (goal >= 0)
                {
                    Write($"{hero.Name} (strength {Game.Strength(hero)}) goes for {monster.amount} {Creatures.Get(monster.subtype).Plural} " +
                          $"(value {Creatures.Get(monster.subtype).Value * monster.amount}) at cell {monster.cell}.");
                    return goal;
                }
            }
            return -1;
        }

        /// <summary>
        /// A game taken up again in the middle of a battle, as a saved one would be: a battle on a battlefield of its own
        /// is begun, and after a move the state of the game is written out and read back, and the game goes on from the
        /// copy, with the battle on the screen again.
        /// </summary>
        private IEnumerator Resume()
        {
            yield return Scenario(BattleStyle.Battlefield);
            HeroState hero = FirstHero();
            int goal = hero != null ? Approach(hero) : -1;
            if (goal < 0)
            {
                Write("No battle to take up again.");
                yield break;
            }
            yield return new WaitForSeconds(1.2f);
            yield return WaitForTurn(10f);
            manager.Commands.MoveHero(hero.id, goal);
            float until = Time.unscaledTime + 40f;
            while (Game != null && !manager.Battle.Running && Time.unscaledTime < until)
            {
                yield return null;
            }
            yield return WaitForOurs(20f);
            Send(BattleAI.Next(Game));
            yield return WaitForTheirs(5f);
            yield return WaitForOurs(20f);
            if (Game == null || !Game.InBattle)
            {
                Write("The battle was over before it could be taken up again.");
                yield break;
            }
            var copy = JsonUtility.FromJson<GameState>(JsonUtility.ToJson(Game.State));
            Write($"Taking the game up again in round {copy.battle.round} of its battle, stack {copy.battle.current} to move.");
            manager.Continue(copy);
            until = Time.unscaledTime + 40f;
            while (Game != null && !manager.Battle.Running && Time.unscaledTime < until)
            {
                yield return null;
            }
            yield return WaitForOurs(20f);
            yield return new WaitForSeconds(2f);
            int loaded = 0;
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                if (SceneManager.GetSceneAt(i).name == BattlefieldScene.SceneName)
                {
                    loaded++;
                }
            }
            Write($"Taken up: battle on the screen {manager.Battle.Running}, round {(manager.Battle.Shown != null ? manager.Battle.Shown.round : -1)}, " +
                  $"{loaded} battle scene(s) loaded, map shown {manager.Map != null && manager.Map.gameObject.activeSelf}.");
            yield return Shot("resume_battle");
            manager.Options.battleSpeed = Mathf.Max(manager.Options.battleSpeed, 2.5f);
            Bar.SetAuto(true);
            until = Time.unscaledTime + 180f;
            while (Game != null && !Bar.Results.IsOpen && Time.unscaledTime < until)
            {
                yield return null;
            }
            if (Bar.Results.IsOpen)
            {
                yield return new WaitForSeconds(0.6f);
                yield return Shot("resume_results");
                Bar.Results.Close();
            }
            Bar.SetAuto(false);
            yield return AfterBattle("resume");
        }

        /// <summary>The siege of the nearest town that is not the hero's, walled for the occasion.</summary>
        private IEnumerator Siege()
        {
            yield return Scenario(BattleStyle.Battlefield);
            if (!Besiege(new[] { BuildingId.Fort, BuildingId.Citadel }, out HeroState hero, out TownState town))
            {
                yield break;
            }
            yield return new WaitForSeconds(1.2f);
            yield return Shot("siege_map_before");
            manager.Commands.MoveHero(hero.id, town.cell);
            yield return Fight("siege", true);
            yield return AfterBattle("siege");
        }

        /// <summary>
        /// A close look at the wall of a town with a castle (three arrow towers): the wall down the field, its gatehouse
        /// and a breach seen from near by, then an arrow tower shot at by the player's archers until it falls, watched as
        /// it goes and what it leaves; the rest of the siege is fought by auto combat.
        /// </summary>
        private IEnumerator Walls()
        {
            yield return Scenario(BattleStyle.Battlefield);
            if (!Besiege(new[] { BuildingId.Fort, BuildingId.Citadel, BuildingId.Castle }, out HeroState hero, out TownState town))
            {
                yield break;
            }
            // Archers enough to bring a tower down in a volley or two, and knights to finish the siege.
            hero.army.Clear();
            hero.army.Add((int)CreatureId.Archer, 60);
            hero.army.Add((int)CreatureId.Crusader, 30);
            yield return new WaitForSeconds(1.2f);
            manager.Commands.MoveHero(hero.id, town.cell);
            float until = Time.unscaledTime + 40f;
            while (Game != null && !manager.Battle.Running && Time.unscaledTime < until)
            {
                yield return null;
            }
            if (!manager.Battle.Running || manager.Field == null)
            {
                Write($"No siege came on the screen (in battle {Game != null && Game.InBattle}).");
                yield break;
            }
            BattleState shown = manager.Battle.Shown;
            HexGrid field = shown.field;
            Write($"Siege on the screen: {shown.walls.Count} wall cells, gate {shown.gate}, towers at " +
                  $"{string.Join(", ", shown.stacks.FindAll(s => s.IsTower).ConvertAll(s => $"row {field.Row(s.cell)}"))}.");
            foreach (Transform piece in ((Component)manager.Field).GetComponentsInChildren<Transform>(true))
            {
                if (piece.name.StartsWith("Flag", StringComparison.Ordinal) || piece.name == "Gate")
                {
                    Write($"  {piece.name} at {piece.position:F2} active {piece.gameObject.activeInHierarchy} scale {piece.lossyScale:F2}.");
                }
            }
            yield return new WaitForSeconds(0.6f);
            yield return Shot("walls_opening");
            yield return WaitForOurs(20f);
            yield return new WaitForSeconds(1.6f);
            yield return Shot("walls_start");

            // From near by, on the besiegers' side: the gatehouse, a breach, and the wall where it runs off the field.
            int column = Battlefields.WallColumn;
            IBattlefield on = manager.Field;
            float line = (on.Point(field.Index(column, 0)).x + on.Point(field.Index(column, 1)).x) * 0.5f;
            float gateZ = on.Point(shown.gate).z;
            yield return Look("walls_gate_close", new Vector3(line, 1.4f, gateZ), new Vector3(-7.5f, 5.5f, -5f));
            float breachZ = on.Point(field.Index(column, Battlefields.BreachRows[1])).z;
            yield return Look("walls_breach_close", new Vector3(line, 0.8f, breachZ), new Vector3(-6.5f, 5f, -6f));
            yield return Look("walls_back_close", new Vector3(line, 1.2f, breachZ), new Vector3(7f, 6f, -6.5f));

            // The archers bring down the tower by the gate.
            BattleStack target = Game.Battle.stacks.Find(s => s.IsTower && s.alive);
            if (target == null)
            {
                Write("The town has no arrow tower.");
                yield break;
            }
            int tower = target.id;
            int towerCell = target.cell;
            bool pointed = false;
            until = Time.unscaledTime + 150f;
            while (Game != null && Game.InBattle && Time.unscaledTime < until && !Bar.Results.IsOpen)
            {
                BattleStack standing = Game.Battle.Stack(tower);
                if (standing == null || !standing.alive)
                {
                    break;
                }
                if (!Ours())
                {
                    yield return null;
                    continue;
                }
                BattleStack stack = Game.Battle.Current;
                if (!Game.CanShoot(stack))
                {
                    // The rest stand on guard, so the garrison lasts until the tower is down.
                    Send(null);
                    yield return WaitForTheirs(5f);
                    continue;
                }
                Bar.TourPointer = Screen(towerCell);
                yield return new WaitForSeconds(0.4f);
                if (!pointed)
                {
                    pointed = true;
                    yield return Shot("walls_tower_aim");
                }
                Write($"Pointer over the tower: {Bar.Pointing}.");
                Bar.TourClick = true;
                yield return null;
                yield return null;
                Bar.TourPointer = null;
                yield return WaitForTheirs(5f);
            }
            BattleStack fallen = Game != null && Game.InBattle ? Game.Battle.Stack(tower) : null;
            Write($"The tower in the rules: {(fallen == null ? "the battle is over" : fallen.alive ? "STILL STANDING" : "fallen")}.");
            if (fallen != null && !fallen.alive)
            {
                // The view catches up: the tower is taken off the field as it starts to fall.
                until = Time.unscaledTime + 20f;
                while (manager.Battle.Stack(tower) != null && Time.unscaledTime < until)
                {
                    yield return null;
                }
                Write($"The tower falls on the screen; its cell is blocked {Game.Battle.IsBlocked(towerCell)}, " +
                      $"wall {Game.Battle.walls.Contains(towerCell)}, passable to the besiegers {Game.Battle.Passable(towerCell, 0)}.");
                yield return new WaitForSeconds(0.2f);
                yield return Shot("walls_tower_shudders");
                yield return new WaitForSeconds(0.35f);
                yield return Shot("walls_tower_falling");
                yield return new WaitForSeconds(0.5f);
                yield return Shot("walls_tower_down");
                yield return new WaitForSeconds(1.4f);
                yield return Shot("walls_tower_fallen");
                float towerZ = on.Point(towerCell).z;
                yield return Look("walls_ruin_close", new Vector3(line, 1f, towerZ), new Vector3(-6.5f, 5.5f, -5.5f));
            }

            Write("Auto combat for the rest of the siege.");
            manager.Options.battleSpeed = Mathf.Max(manager.Options.battleSpeed, 2.5f);
            Bar.SetAuto(true);
            until = Time.unscaledTime + 240f;
            while (Game != null && !Bar.Results.IsOpen && Time.unscaledTime < until)
            {
                yield return null;
            }
            if (Bar.Results.IsOpen)
            {
                yield return new WaitForSeconds(0.8f);
                yield return Shot("walls_results");
                Bar.Results.Close();
            }
            Bar.SetAuto(false);
            yield return AfterBattle("walls");
        }

        /// <summary>
        /// The first skirmish map started on settings of a large, rich, deadly map and hard computer players (the tour's
        /// copy of the settings, put back afterwards): the log tells the size, riches and dangers the map was laid out
        /// with and how well its computer players play, against the ones the map was made with.
        /// </summary>
        private IEnumerator Skirmish()
        {
            var campaign = manager.Campaign as HeroesCampaign;
            int level = campaign != null && campaign.Skirmishes.Count > 0 ? campaign.Skirmishes[0] : -1;
            if (level < 0)
            {
                Write("No skirmish map.");
                yield break;
            }
            if (Game != null)
            {
                manager.ReturnToTitle();
                yield return new WaitForSeconds(1f);
            }
            HeroesSettings options = manager.Options;
            HeroesSettings before = Instantiate(options);
            options.mapSize = 2;
            options.treasure = 3;
            options.monsters = 4;
            options.difficulty = 2;
            MapSpec made = campaign.Scenario(level).Map;
            MapSpec shaped = manager.NewGameMap(campaign.Scenario(level));
            manager.BeginScenario(null, level);
            float until = Time.unscaledTime + 40f;
            while (Game == null && Time.unscaledTime < until)
            {
                yield return null;
            }
            yield return WaitForTurn(30f);
            if (Game != null)
            {
                GameState state = Game.State;
                var computers = new List<string>();
                foreach (PlayerState player in state.players)
                {
                    if (!player.human)
                    {
                        computers.Add($"{player.name} level {player.aiLevel} gold {player.resources.Gold}");
                    }
                }
                Write($"Skirmish {manager.ScenarioName}: made {made.columns}x{made.rows}, treasure {made.treasure}, monsters {made.monsters}, " +
                      $"levels {string.Join("/", made.players.ConvertAll(p => p.human ? "you" : p.aiLevel.ToString()))}; " +
                      $"shaped {shaped.columns}x{shaped.rows}, treasure {shaped.treasure}, monsters {shaped.monsters}; " +
                      $"laid out {state.map.grid.columns}x{state.map.grid.rows}, {state.objects.FindAll(o => o.kind == ObjectKind.Monster).Count} wandering armies; " +
                      $"computers {string.Join(", ", computers)}.");
                yield return new WaitForSeconds(1f);
                yield return Shot("skirmish_start");
            }
            else
            {
                Write("The skirmish never started.");
            }
            options.CopySettings(before);
            Destroy(before);
        }

        /// <summary>
        /// Readies the siege of the town nearest the first hero that is not his own: <paramref name="walls"/> built in
        /// it, a garrison if it has none, four times the hero's troops and a few spells; stands him before it.
        /// </summary>
        private bool Besiege(BuildingId[] walls, out HeroState hero, out TownState town)
        {
            town = null;
            hero = FirstHero();
            if (hero == null)
            {
                Write("No hero to besiege with.");
                return false;
            }
            int closest = int.MaxValue;
            foreach (TownState each in Game.State.towns)
            {
                int distance = Game.State.map.grid.Distance(hero.cell, each.cell);
                if (each.owner != hero.owner && distance < closest)
                {
                    closest = distance;
                    town = each;
                }
            }
            if (town == null)
            {
                Write("No town to besiege.");
                return false;
            }
            // Walls and towers for the town, a garrison behind them, and an army that can take them.
            foreach (BuildingId building in walls)
            {
                if (!town.Has(building))
                {
                    town.built.Add((int)building);
                }
            }
            if (town.garrison.IsEmpty)
            {
                town.garrison.slots[0] = new ArmySlot { creature = (int)Creatures.OfTier(town.faction, 1), count = 14 };
                town.garrison.slots[1] = new ArmySlot { creature = (int)Creatures.OfTier(town.faction, 2), count = 7 };
                town.garrison.slots[2] = new ArmySlot { creature = (int)Creatures.OfTier(town.faction, 3), count = 4 };
            }
            foreach (ArmySlot slot in hero.army.slots)
            {
                if (!slot.IsEmpty)
                {
                    slot.count *= 4;
                }
            }
            Spellbook(hero);
            Write($"{hero.Name} (strength {Game.Strength(hero)}) besieges {town.name} ({town.faction}), garrison strength {Game.Strength(town.garrison)}.");
            if (StandNear(hero, new List<int> { town.cell }) < 0)
            {
                Write("No free ground before the town.");
                return false;
            }
            return true;
        }

        /// <summary>
        /// A shot of the battlefield from near by: the battle's camera stood at <paramref name="offset"/> from
        /// <paramref name="target"/>, looking at it, and put back where it was.
        /// </summary>
        private IEnumerator Look(string name, Vector3 target, Vector3 offset)
        {
            Camera view = manager.Field != null ? manager.Field.Camera : null;
            if (view == null)
            {
                yield break;
            }
            Transform camera = view.transform;
            Vector3 position = camera.position;
            Quaternion rotation = camera.rotation;
            camera.SetPositionAndRotation(target + offset, Quaternion.LookRotation(-offset));
            yield return null;
            yield return Shot(name);
            camera.SetPositionAndRotation(position, rotation);
        }

        /// <summary>
        /// Fights the battle that is starting as a player would: pointing, holding, aiming and clicking for the first
        /// few moves, then with auto combat, until the results are shown and closed.
        /// </summary>
        private IEnumerator Fight(string tag, bool scene)
        {
            float until = Time.unscaledTime + 40f;
            while (Game != null && !manager.Battle.Running && Time.unscaledTime < until)
            {
                yield return null;
            }
            if (!manager.Battle.Running)
            {
                Write($"No battle came on the screen (in battle {Game != null && Game.InBattle}).");
                yield break;
            }
            Scene battleScene = SceneManager.GetSceneByName(BattlefieldScene.SceneName);
            Write($"Battle on the screen: scene {(battleScene.IsValid() ? battleScene.name + " loaded " + battleScene.isLoaded : "none")}, " +
                  $"active {SceneManager.GetActiveScene().name}, map shown {manager.Map != null && manager.Map.gameObject.activeSelf}, " +
                  $"field {manager.Field?.GetType().Name}, style {(manager.Battle.Shown.IsField ? "field" : "map")}, " +
                  $"{manager.Battle.Shown.stacks.Count} stacks, {manager.Battle.Shown.obstacleCells.Count} obstacles, " +
                  $"{manager.Battle.Shown.walls.Count} wall cells, gate {manager.Battle.Shown.gate}, terrain {(TerrainType)manager.Battle.Shown.terrain}.");
            var sizes = new List<string>();
            foreach (BattleStack each in manager.Battle.Shown.stacks)
            {
                UnitView view = manager.Battle.Stack(each.id);
                if (view != null)
                {
                    sizes.Add($"{each.Def.Plural} {view.transform.localScale.x:F2}");
                }
            }
            Write($"Drawn at: {string.Join(", ", sizes)}.");
            if (scene)
            {
                yield return new WaitForSeconds(0.5f);
                yield return Shot($"{tag}_opening");
            }
            yield return WaitForOurs(20f);
            yield return new WaitForSeconds(scene ? 1.6f : 0.8f);
            yield return Shot($"{tag}_start");
            Framed();
            Shroud();

            // The pause menu's Save refuses in the middle of a battle, and its Leave to the Title asks again.
            Write($"Can save in the battle: {manager.CanSave}.");
            manager.SaveGame();
            if (tag == "field")
            {
                yield return LeaveAsks(tag);
            }

            bool held = false, probed = false, cast = false, struck = false, moved = false, meleed = false;
            int turns = 0;
            until = Time.unscaledTime + 240f;
            while (Game != null && Time.unscaledTime < until)
            {
                if (Bar.Results.IsOpen)
                {
                    yield return new WaitForSeconds(0.8f);
                    yield return Shot($"{tag}_results");
                    Bar.Results.Close();
                    break;
                }
                if (!Ours())
                {
                    yield return null;
                    continue;
                }
                if (Bar.AutoCombat)
                {
                    yield return null;
                    continue;
                }
                turns++;
                BattleState battle = Game.Battle;
                BattleStack stack = battle.Current;
                BattleStack enemy = Nearest(Game.BattleGrid, battle, stack);
                if (!held && enemy != null)
                {
                    held = true;
                    Bar.TourPointer = Screen(enemy.cell);
                    Bar.TourHold = true;
                    yield return new WaitForSeconds(0.4f);
                    yield return Shot($"{tag}_stack_card");
                    Bar.TourHold = false;
                    Bar.TourPointer = null;
                    yield return null;
                }
                if (!probed && enemy != null)
                {
                    probed = true;
                    yield return Probe(tag, battle, stack, enemy);
                    continue;
                }
                if (!cast && CastNow(battle, stack, out SpellId spell, out int aim))
                {
                    cast = true;
                    Bar.OpenSpells();
                    yield return new WaitForSeconds(0.6f);
                    yield return Shot($"{tag}_spellbook");
                    Bar.Pick(spell);
                    Bar.TourPointer = Screen(aim);
                    yield return new WaitForSeconds(0.4f);
                    yield return Shot($"{tag}_spell_aim");
                    Write($"Pointer aiming {spell}: {Bar.Pointing}.");
                    Bar.TourClick = true;
                    yield return new WaitForSeconds(0.55f);
                    Bar.TourPointer = null;
                    yield return Shot($"{tag}_spell_cast");
                    yield return new WaitForSeconds(0.25f);
                    yield return Shot($"{tag}_spell_struck");
                    yield return WaitForTheirs(5f);
                    continue;
                }
                if (!meleed && !Game.CanShoot(stack) && Reachable(battle, stack, out BattleStack foe, out int side))
                {
                    // A blow, aimed from the side of the foe the pointer leans to, as a player picks where to strike from.
                    meleed = true;
                    Bar.TourPointer = Vector3.Lerp(Screen(foe.cell), Screen(side), 0.3f);
                    yield return new WaitForSeconds(0.4f);
                    yield return Shot($"{tag}_melee_hover");
                    Write($"Pointer over {foe.Def.Plural} from cell {side}: {Bar.Pointing}.");
                    Bar.TourClick = true;
                    yield return null;
                    yield return null;
                    Bar.TourPointer = null;
                    yield return WaitForTheirs(5f);
                    continue;
                }
                GameCommand move = BattleAI.Next(Game);
                if (!struck && move != null && (move.kind == CommandKind.BattleAttack || move.kind == CommandKind.BattleShoot))
                {
                    struck = true;
                    BattleStack target = battle.Stack(move.b);
                    Vector3 at = Screen(target.cell);
                    if (move.kind == CommandKind.BattleAttack && move.c >= 0)
                    {
                        // Toward the side the blow comes from: the pointer's place in the hexagon picks it.
                        at = Vector3.Lerp(at, Screen(move.c), 0.3f);
                    }
                    Bar.TourPointer = at;
                    yield return new WaitForSeconds(0.4f);
                    yield return Shot($"{tag}_{(move.kind == CommandKind.BattleShoot ? "shoot" : "attack")}_hover");
                    Write($"Pointer over {target.Def.Plural}: {Bar.Pointing}.");
                    Bar.TourClick = true;
                    yield return null;
                    yield return null;
                    Bar.TourPointer = null;
                    yield return WaitForTheirs(5f);
                    continue;
                }
                if (!moved && move != null && move.kind == CommandKind.BattleMove)
                {
                    moved = true;
                    Bar.TourPointer = Screen(move.b);
                    yield return new WaitForSeconds(0.4f);
                    yield return Shot($"{tag}_move_hover");
                    Write($"Pointer over cell {move.b}: {Bar.Pointing}.");
                    Bar.TourClick = true;
                    yield return null;
                    yield return null;
                    Bar.TourPointer = null;
                    yield return new WaitForSeconds(0.5f / manager.Battle.Speed);
                    yield return Shot($"{tag}_moving");
                    yield return WaitForTheirs(5f);
                    continue;
                }
                if (turns >= 6 || struck && cast && meleed)
                {
                    Write($"Auto combat after {turns} turns.");
                    manager.Options.battleSpeed = Mathf.Max(manager.Options.battleSpeed, 2.5f);
                    Bar.SetAuto(true);
                    continue;
                }
                Send(move);
                yield return WaitForTheirs(5f);
            }
            Bar.SetAuto(false);
            Bar.TourPointer = null;
            if (Time.unscaledTime >= until)
            {
                Write($"The battle did not end in time (round {(Game != null && Game.InBattle ? Game.Battle.round : -1)}).");
            }
        }

        /// <summary>
        /// Leaves the scenario for the title while its battlefield is on its way in, again while the battle is up, and
        /// once more while its results are shown: nothing of the battle may stay behind (no battlefield scene, no black
        /// screen, no dialog, the map's camera back on).
        /// </summary>
        private IEnumerator Teardown()
        {
            for (int round = 0; round < 3; round++)
            {
                string when = round == 0 ? "loading" : round == 1 ? "fought" : "over";
                yield return Scenario(BattleStyle.Battlefield);
                HeroState hero = FirstHero();
                int goal = hero != null ? Approach(hero) : -1;
                if (goal < 0)
                {
                    Write("No battle to leave.");
                    yield break;
                }
                yield return new WaitForSeconds(1.2f);
                manager.Commands.MoveHero(hero.id, goal);
                float until = Time.unscaledTime + 40f;
                while (Game != null && Time.unscaledTime < until && (round == 0 ? !BattlefieldScene.Loading && !manager.Battle.Running : !Ours()))
                {
                    yield return null;
                }
                if (round == 2)
                {
                    float speed = manager.Options.battleSpeed;
                    manager.Options.battleSpeed = Mathf.Max(speed, 2.5f);
                    Bar.SetAuto(true);
                    until = Time.unscaledTime + 120f;
                    while (Game != null && !Bar.Results.IsOpen && Time.unscaledTime < until)
                    {
                        yield return null;
                    }
                    manager.Options.battleSpeed = speed;
                }
                Write($"Leaving for the title while the battle is {when}: loading {BattlefieldScene.Loading}, on the screen {manager.Battle.Running}, " +
                      $"black {Bar.Blackout:F2}, results shown {Bar.Results.IsOpen}.");
                manager.ReturnToTitle();
                yield return new WaitForSeconds(2.5f);
                int left = 0;
                for (int i = 0; i < SceneManager.sceneCount; i++)
                {
                    if (SceneManager.GetSceneAt(i).name.StartsWith(BattlefieldScene.SceneName, StringComparison.Ordinal))
                    {
                        left++;
                    }
                }
                Write($"On the title after leaving a battle that was {when}: {left} battle scene(s) left, active {SceneManager.GetActiveScene().name}, " +
                      $"black {Bar.Blackout:F2}, results shown {Bar.Results.IsOpen}, " +
                      $"rig camera {(manager.Rig != null && manager.Rig.View != null && manager.Rig.View.enabled)}, " +
                      $"locked {(manager.Rig != null && manager.Rig.Locked)}.");
                yield return Shot($"teardown_{when}");
            }
        }

        /// <summary>An enemy the stack can reach with a blow, and a cell to strike it from.</summary>
        private bool Reachable(BattleState battle, BattleStack stack, out BattleStack foe, out int side)
        {
            Dictionary<int, int> reach = Game.BattleReach(stack);
            foreach (BattleStack other in battle.stacks)
            {
                if (!other.alive || other.side == stack.side || other.IsTower)
                {
                    continue;
                }
                List<int> cells = Game.AttackCells(stack, other, reach);
                if (cells.Count > 0)
                {
                    foe = other;
                    side = cells[cells.Count - 1];
                    return true;
                }
            }
            foe = null;
            side = -1;
            return false;
        }

        /// <summary>Where the field sits on the screen: its first, middle and last cells, in pixels.</summary>
        /// <summary>A battle on the map: how many of its cells the player has not explored, and how many the shroud still covers.</summary>
        private void Shroud()
        {
            BattleState shown = manager.Battle.Shown;
            PlayerState me = manager.ViewerState;
            if (shown == null || shown.IsField || me == null || manager.Map == null)
            {
                return;
            }
            int unexplored = 0, shrouded = 0, stacks = 0;
            foreach (int cell in shown.cells)
            {
                bool under = manager.Map.Fog.Shrouds(manager.Map.Point(cell));
                unexplored += me.Explored(cell) ? 0 : 1;
                shrouded += under ? 1 : 0;
                stacks += under && shown.StackAt(cell) != null ? 1 : 0;
            }
            Write($"The shroud over the field: {unexplored} of {shown.cells.Count} cells unexplored, {shrouded} shrouded on the screen, {stacks} stacks under it.");
            shroudedField = new List<int>(shown.cells);
        }

        /// <summary>The cells of the last battle fought on the map, to look at the shroud over them after it.</summary>
        private List<int> shroudedField;

        private void Framed()
        {
            IBattlefield field = manager.Field;
            BattleState shown = manager.Battle.Shown;
            if (field == null || shown == null || shown.cells.Count == 0)
            {
                return;
            }
            Vector3 first = Screen(shown.cells[0]);
            Vector3 middle = Screen(shown.center);
            Vector3 last = Screen(shown.cells[shown.cells.Count - 1]);
            Write($"Framed: first cell at {first:F0}, center at {middle:F0}, last cell at {last:F0} of {UnityEngine.Screen.width}x{UnityEngine.Screen.height}" +
                  (manager.Rig != null ? $"; map camera target {manager.Rig.Target:F1} distance {manager.Rig.Distance:F1} goal {manager.Rig.Goal:F1}" : ""));
        }

        /// <summary>Back on the map: the questions a victory brings are answered, and the map is looked at.</summary>
        private IEnumerator AfterBattle(string tag)
        {
            float until = Time.unscaledTime + 20f;
            while (Game != null && (manager.Battle.Running || Game.InBattle) && Time.unscaledTime < until)
            {
                yield return null;
            }
            yield return new WaitForSeconds(1.5f);
            Scene battleScene = SceneManager.GetSceneByName(BattlefieldScene.SceneName);
            Write($"After the battle: scene {(battleScene.IsValid() && battleScene.isLoaded ? "STILL LOADED" : "gone")}, " +
                  $"active {SceneManager.GetActiveScene().name}, map shown {manager.Map != null && manager.Map.gameObject.activeSelf}, " +
                  $"rig camera {(manager.Rig != null && manager.Rig.View != null && manager.Rig.View.enabled)}, locked {(manager.Rig != null && manager.Rig.Locked)}.");
            if (shroudedField != null && manager.ViewerState != null && manager.Map != null)
            {
                // The shroud lifted off a battle on the map lies again over what is still unexplored.
                int unexplored = 0, shrouded = 0;
                foreach (int cell in shroudedField)
                {
                    unexplored += manager.ViewerState.Explored(cell) ? 0 : 1;
                    shrouded += manager.Map.Fog.Shrouds(manager.Map.Point(cell)) ? 1 : 0;
                }
                Write($"The shroud after the battle: {unexplored} of the field's {shroudedField.Count} cells unexplored, {shrouded} shrouded on the screen.");
                shroudedField = null;
            }
            if (Game != null && Game.State.pending.Count > 0)
            {
                yield return Shot($"{tag}_choice");
                manager.Commands.Choose(0);
                yield return WaitForTurn(10f);
            }
            yield return Shot($"{tag}_map_after");
        }

        // ------------------------------------------------------------------ setting the stage

        private IEnumerator Scenario(BattleStyle style)
        {
            if (Game != null)
            {
                manager.ReturnToTitle();
                yield return new WaitForSeconds(1f);
            }
            manager.Options.battleStyle = (int)style;
            manager.BeginScenario(null, 0);
            float until = Time.unscaledTime + 40f;
            while (Game == null && Time.unscaledTime < until)
            {
                yield return null;
            }
            yield return WaitForTurn(30f);
            Write(Game != null ? $"Scenario started, battles {Game.State.rules.battleStyle}." : "The scenario never started.");
        }

        private HeroState FirstHero()
        {
            if (Game == null)
            {
                return null;
            }
            HeroState hero = manager.Selected;
            if (hero == null && manager.ViewerState != null && manager.ViewerState.heroes.Count > 0)
            {
                hero = Game.State.Hero(manager.ViewerState.heroes[0]);
            }
            return hero;
        }

        /// <summary>The wandering armies, those the hero stands the best chance against first, the nearer the better.</summary>
        private List<MapObject> Wandering(HeroState hero)
        {
            int strength = Game.Strength(hero);
            var armies = new List<(MapObject army, float score)>();
            foreach (MapObject what in Game.State.objects)
            {
                if (what.removed || what.kind != ObjectKind.Monster)
                {
                    continue;
                }
                CreatureDef def = Creatures.Get(what.subtype);
                int value = def != null ? def.Value * what.amount : int.MaxValue;
                float distance = Game.State.map.grid.Distance(hero.cell, what.cell);
                // A fight the hero can win, with a few of the enemy for the picture; after that, the nearest.
                float score = (value > strength * 0.7f ? 10000f : 0f) + (what.amount < 3 ? 500f : 0f) +
                              (def != null && def.Has(Ability.Regenerate) ? 2000f : 0f) + distance;
                armies.Add((what, score));
            }
            armies.Sort((a, b) => a.score.CompareTo(b.score));
            return armies.ConvertAll(entry => entry.army);
        }

        /// <summary>A few spells for the hero's book and the mana to cast them, so the battle can show one.</summary>
        private void Spellbook(HeroState hero)
        {
            foreach (SpellId spell in new[] { SpellId.MagicArrow, SpellId.LightningBolt, SpellId.Fireball, SpellId.Bless, SpellId.Haste, SpellId.Cure })
            {
                if (!hero.Knows(spell))
                {
                    hero.spells.Add((int)spell);
                }
            }
            hero.mana = Mathf.Max(hero.mana, Mathf.Min(60, Game.MaxMana(hero) + 30));
        }

        /// <summary>
        /// Stands the hero on free open ground a few steps from one of <paramref name="goals"/>, not guarded by any
        /// monster, from where a path leads there; the map and the fog follow. Returns the goal, or -1.
        /// </summary>
        private int StandNear(HeroState hero, List<int> goals)
        {
            GameState state = Game.State;
            HexGrid grid = state.map.grid;
            int from = hero.cell;
            foreach (int cell in goals)
            {
                int found = StandNear(hero, cell, state, grid, from);
                if (found >= 0)
                {
                    return found;
                }
            }
            return -1;
        }

        private int StandNear(HeroState hero, int cell, GameState state, HexGrid grid, int from)
        {
            for (int ring = 1; ring <= 4; ring++)
            {
                for (int candidate = 0; candidate < grid.Count; candidate++)
                {
                    if (grid.Distance(candidate, cell) != ring || !state.map.Open(candidate) || state.map.occupant[candidate] >= 0 ||
                        state.HeroAt(candidate) != null || Game.GuardOf(candidate) != null)
                    {
                        continue;
                    }
                    hero.cell = candidate;
                    MovePlan plan = manager.PlanFor(hero, cell);
                    if (plan != null && plan.Cells.Count > 1 && plan.Cells.Count <= ring + 3)
                    {
                        hero.movement = hero.maxMovement;
                        Game.Reveal(hero.owner, candidate, 6);
                        manager.Map.Sync();
                        manager.Map.Fog.Show(manager.ViewerState);
                        manager.Select(hero);
                        manager.Rig?.Snap(manager.Map.Point(candidate));
                        Write($"{hero.Name} stands at cell {candidate}, {ring} steps from {cell}.");
                        return cell;
                    }
                    hero.cell = from;
                }
            }
            hero.cell = from;
            return -1;
        }

        // ------------------------------------------------------------------ in the battle

        /// <summary>Whether the battle waits for the player here, with the view caught up.</summary>
        private bool Ours()
        {
            return Game != null && Game.InBattle && manager.WaitingForHuman && !manager.HeroesUI.Busy &&
                   Game.State.pending.Count == 0 && Game.Battle.Current != null && !Bar.Results.IsOpen;
        }

        private IEnumerator WaitForOurs(float patience)
        {
            float until = Time.unscaledTime + patience;
            while (Game != null && !Ours() && !Bar.Results.IsOpen && Time.unscaledTime < until)
            {
                yield return null;
            }
        }

        private IEnumerator WaitForTheirs(float patience)
        {
            float until = Time.unscaledTime + patience;
            while (Game != null && Ours() && Time.unscaledTime < until)
            {
                yield return null;
            }
        }

        private static BattleStack Nearest(HexGrid grid, BattleState battle, BattleStack stack)
        {
            BattleStack best = null;
            int closest = int.MaxValue;
            foreach (BattleStack other in battle.stacks)
            {
                if (!other.alive || other.side == stack.side || other.IsTower)
                {
                    continue;
                }
                int distance = grid.Distance(stack.cell, other.cell);
                if (distance < closest)
                {
                    closest = distance;
                    best = other;
                }
            }
            return best;
        }

        /// <summary>A damage spell the hero can cast now, and the enemy with the most creatures to cast it on.</summary>
        /// <summary>
        /// What the pointer on a creature's head picks (the creature, not the ground behind it); the retreat's question,
        /// asked and taken back with Escape; and Escape on the pause menu opened while a spell is aimed, which closes the
        /// menu and leaves the aim as it was.
        /// </summary>
        private IEnumerator Probe(string tag, BattleState battle, BattleStack stack, BattleStack enemy)
        {
            UnitView body = manager.Battle.Stack(enemy.id);
            IBattlefield field = manager.Field;
            if (body != null && field != null && field.Camera != null)
            {
                Vector3 head = field.Camera.WorldToScreenPoint(body.transform.position + Vector3.up * (body.Height * 0.85f));
                head.z = 0f;
                int picked = manager.Battle.StackUnder(head, out _);
                int ground = field.CellAt(head, out _);
                Bar.TourPointer = head;
                yield return new WaitForSeconds(0.4f);
                yield return Shot($"{tag}_head_hover");
                Write($"Pointer on the head of the {enemy.Def.Plural} (cell {enemy.cell}): the body picks {picked}, the ground there is {ground}; pointer {Bar.Pointing}.");
                Bar.TourPointer = null;
                yield return null;
            }
            Bar.Retreat();
            yield return new WaitForSeconds(0.4f);
            yield return Shot($"{tag}_retreat_question");
            bool asked = Bar.Asking;
            manager.Back();
            yield return null;
            Write($"Retreat asked: {asked}; after Escape asking {Bar.Asking}, still in the battle {Game != null && Game.InBattle}.");
            if (CastNow(battle, stack, out SpellId spell, out _))
            {
                Bar.Pick(spell);
                yield return null;
                manager.OpenMenu();
                yield return null;
                yield return null;
                bool paused = !manager.IsGameRunning;
                manager.Back();
                yield return null;
                yield return null;
                Write($"Aiming {spell}, the menu opened: paused {paused}; after Escape running {manager.IsGameRunning}, still aiming {Bar.Casting}.");
                manager.Back();
                yield return null;
                Write($"Escape again lets the aim go: {Bar.Casting}.");
            }
        }

        private bool CastNow(BattleState battle, BattleStack stack, out SpellId spell, out int aim)
        {
            aim = -1;
            foreach (SpellId candidate in new[] { SpellId.LightningBolt, SpellId.Fireball, SpellId.MagicArrow })
            {
                spell = candidate;
                if (Game.CannotCast(stack.side, spell) != null)
                {
                    continue;
                }
                int most = -1;
                foreach (BattleStack other in battle.stacks)
                {
                    if (other.alive && other.side != stack.side && !other.IsTower && other.count > most &&
                        Game.ValidSpellTarget(stack.side, spell, other.cell))
                    {
                        most = other.count;
                        aim = other.cell;
                    }
                }
                if (aim >= 0)
                {
                    return true;
                }
            }
            spell = SpellId.None;
            return false;
        }

        /// <summary>Where a cell of the field is on the screen.</summary>
        private Vector3 Screen(int cell)
        {
            IBattlefield field = manager.Field;
            Camera view = field != null ? field.Camera : null;
            if (view == null)
            {
                return Vector3.zero;
            }
            Vector3 at = view.WorldToScreenPoint(field.Point(cell) + Vector3.up * 0.1f);
            return new Vector3(at.x, at.y, 0f);
        }

        /// <summary>A move of the computer's battle sense, sent as the player's own command.</summary>
        private void Send(GameCommand move)
        {
            IHeroesCommands commands = manager.Commands;
            if (move == null)
            {
                commands.BattleDefend(Game.Battle.current);
                return;
            }
            switch (move.kind)
            {
                case CommandKind.BattleMove:
                    commands.BattleMove(move.a, move.b);
                    break;
                case CommandKind.BattleAttack:
                    commands.BattleAttack(move.a, move.b, move.c);
                    break;
                case CommandKind.BattleShoot:
                    commands.BattleShoot(move.a, move.b);
                    break;
                case CommandKind.BattleWait:
                    commands.BattleWait(move.a);
                    break;
                case CommandKind.BattleCast:
                    commands.BattleCast((SpellId)move.a, move.b);
                    break;
                default:
                    commands.BattleDefend(move.a);
                    break;
            }
        }

        // ------------------------------------------------------------------ plumbing

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

        /// <summary>
        /// In the middle of a battle, which cannot be saved, the pause menu's Leave to the Title only warns at the first
        /// press and asks again; Escape takes the menu away with the question.
        /// </summary>
        private IEnumerator LeaveAsks(string tag)
        {
            manager.OpenMenu();
            yield return new WaitForSecondsRealtime(0.5f);
            GameObject leave = GameObject.Find("ExitToTitle");
            if (leave == null || !leave.TryGetComponent(out UnityEngine.UI.Button button))
            {
                Write("No Leave to the Title in the pause menu.");
                manager.Back();
                yield break;
            }
            var words = button.GetComponentInChildren<TMPro.TMP_Text>(true);
            button.onClick.Invoke();
            yield return new WaitForSecondsRealtime(0.4f);
            yield return Shot($"{tag}_leave_asks");
            Write($"Leave to the Title pressed once in the battle: still in it {Game != null && Game.InBattle}, " +
                  $"the button says \"{(words != null ? words.text : "?")}\", pointer {Bar.Pointing}.");
            manager.Back();
            yield return new WaitForSecondsRealtime(0.5f);
            Write($"Escape: running {manager.IsGameRunning}, still in the battle {Game != null && Game.InBattle}, " +
                  $"the button says \"{(words != null ? words.text : "?")}\" again.");
        }

        private IEnumerator Shot(string name)
        {
            yield return new WaitForEndOfFrame();
            string file = Path.Combine(folder, $"{++shot:00}_{name}.png");
            ScreenCapture.CaptureScreenshot(file);
            Write($"shot {file}");
            yield return new WaitForSecondsRealtime(0.3f);
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
