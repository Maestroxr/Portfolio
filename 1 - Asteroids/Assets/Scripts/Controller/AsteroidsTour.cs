using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Gamebox;
using Gamebox.UI;
using UnityEngine;

namespace Portfolio.Asteroids
{
    /// <summary>
    /// Development players only. Started with <c>-asteroids-tour &lt;folder&gt;</c>, it flies the offline game by itself
    /// with the <see cref="StrikeAutopilot"/> (or the asteroid autopilot for field missions) and saves screenshots and a
    /// tour.log into the folder, then quits. <c>-asteroids-tour-plan</c> lists the steps (<c>menu</c>,
    /// <c>strike:&lt;index&gt;</c>, <c>shop</c>, <c>boss:&lt;index&gt;</c>, <c>field:&lt;index&gt;</c>);
    /// <c>-asteroids-tour-god</c>, <c>-asteroids-tour-speed &lt;x&gt;</c> (at most 3), <c>-asteroids-tour-money &lt;n&gt;</c>
    /// and <c>-asteroids-tour-timeout &lt;s&gt;</c> tune it. It backs up the strike progress keys and restores them, also
    /// when a step fails (the tour then ends early) or the process quits before the plan is done.
    /// A strike or boss step takes the strike mission's number (1 = the first strike mission, <c>strike:all</c> flies them
    /// all in order); a field step takes the campaign index of the field mission. The default plan is
    /// <c>menu,shop,strike:1,boss:1,field:0</c>.
    /// </summary>
    public class AsteroidsTour : MonoBehaviour
    {
        /// <summary>The command line argument that starts the tour.</summary>
        public const string Argument = "-asteroids-tour";

        public const string PlanArgument = "-asteroids-tour-plan";
        public const string GodArgument = "-asteroids-tour-god";
        public const string SpeedArgument = "-asteroids-tour-speed";
        public const string MoneyArgument = "-asteroids-tour-money";
        public const string TimeoutArgument = "-asteroids-tour-timeout";

        private const string DefaultPlan = "menu,shop,strike:1,boss:1,field:0";

        /// <summary>Faster than this and shots start to tunnel through the circle tests of the field.</summary>
        private const float MaxSpeed = 3f;

        /// <summary>Screenshots of a strike mission as the scroll passes these shares of the way to the boss.</summary>
        private static readonly float[] ProgressShots = { 0.1f, 0.35f, 0.6f, 0.85f };

        private string folder;
        private StreamWriter log;
        private int shots;
        private int errors;
        private AsteroidsGameManager manager;
        private bool god;
        private float speed = 1f;
        private int money = -1;
        private float timeout = 300f;
        private readonly Dictionary<string, float> savedKeys = new Dictionary<string, float>();
        private readonly List<string> missingKeys = new List<string>();
        private StrikeLoadout savedPilot;
        private bool hadPilot;
        private bool backedUp;
        private bool restored;


        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Launch()
        {
            if (Application.isEditor || !Debug.isDebugBuild)
            {
                return;
            }
            string folder = MobilePlatform.ArgumentValue(Argument);
            if (string.IsNullOrEmpty(folder) || FindAnyObjectByType<AsteroidsTour>() != null)
            {
                return;
            }
            var holder = new GameObject("AsteroidsTour");
            DontDestroyOnLoad(holder);
            holder.AddComponent<AsteroidsTour>().folder = folder;
        }


        private IEnumerator Start()
        {
            Directory.CreateDirectory(folder);
            log = new StreamWriter(Path.Combine(folder, "tour.log"), false) { AutoFlush = true };
            Application.logMessageReceived += OnLog;
            Application.runInBackground = true;
            string plan = MobilePlatform.ArgumentValue(PlanArgument);
            if (string.IsNullOrEmpty(plan))
            {
                plan = DefaultPlan;
            }
            god = HasArgument(GodArgument);
            speed = Mathf.Clamp(Number(SpeedArgument, 1f), 0.1f, MaxSpeed);
            money = (int)Number(MoneyArgument, -1f);
            timeout = Mathf.Max(20f, Number(TimeoutArgument, 300f));
            Note($"tour: plan {plan}, god {god}, speed {speed}, money {money}, timeout {timeout} s, screen {Screen.width}x{Screen.height}");

            float waited = 0f;
            while ((manager == null || manager.Progress == null) && waited < 30f)
            {
                manager = FindAnyObjectByType<AsteroidsGameManager>();
                waited += Time.unscaledDeltaTime;
                yield return null;
            }
            if (manager == null || manager.Progress == null)
            {
                Note("FAIL no Asteroids manager");
                Quit();
                yield break;
            }
            yield return Wait(1.5f);
            Backup();
            if (StrikeMissions().Count == 0)
            {
                AddTestMission();
                yield return Wait(0.5f);
            }
            Note($"missions: {manager.LevelCount}, strike missions: {StrikeMissions().Count}, pilot {Describe(manager.Pilot)}");

            // A step that throws ends the plan (not the process): the progress is put back and the tour quits either way.
            yield return Guarded(Run(plan));
            Time.timeScale = 1f;
            Restore();
            Note($"tour done: {shots} screenshots, {errors} errors");
            Quit();
        }


        /// <summary>The steps of <paramref name="plan"/> one after another.</summary>
        private IEnumerator Run(string plan)
        {
            foreach (string raw in plan.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string step = raw.Trim().ToLowerInvariant();
                Note($"step {step}");
                string[] parts = step.Split(':');
                string argument = parts.Length > 1 ? parts[1] : string.Empty;
                switch (parts[0])
                {
                    case "menu":
                        yield return Menu();
                        break;
                    case "shop":
                        yield return Shop();
                        break;
                    case "strike":
                    case "boss":
                        bool boss = parts[0] == "boss";
                        List<int> strikes = StrikeMissions();
                        if (argument == "all")
                        {
                            for (int n = 1; n <= strikes.Count; n++)
                            {
                                yield return Strike(n, boss);
                            }
                        }
                        else
                        {
                            yield return Strike(ParseInt(argument, 1), boss);
                        }
                        break;
                    case "field":
                        yield return Field(ParseInt(argument, 0));
                        break;
                    default:
                        Note($"unknown step {step}");
                        break;
                }
            }
        }


        /// <summary>
        /// Runs <paramref name="routine"/> with its nested routines like a coroutine would, but catches what one of them
        /// throws: it is noted as a FAIL and the run ends there, so the caller still gets to restore the progress.
        /// </summary>
        private IEnumerator Guarded(IEnumerator routine)
        {
            var stack = new Stack<IEnumerator>();
            stack.Push(routine);
            while (stack.Count > 0)
            {
                IEnumerator top = stack.Peek();
                bool more;
                try
                {
                    more = top.MoveNext();
                }
                catch (Exception exception)
                {
                    errors++;
                    Note($"FAIL {exception.GetType().Name}: {exception.Message}\n{exception.StackTrace}");
                    yield break;
                }
                if (!more)
                {
                    stack.Pop();
                    continue;
                }
                if (top.Current is IEnumerator nested)
                {
                    stack.Push(nested);
                    continue;
                }
                yield return top.Current;
            }
        }


        #region Steps

        private IEnumerator Menu()
        {
            yield return ToMenu();
            manager.SelectMode(MissionMode.Field);
            yield return Wait(1.2f);
            yield return Shot("menu_field");
            manager.SelectMode(MissionMode.Strike);
            yield return Wait(1.5f);
            StrikeTerrain terrain = manager.Field != null ? manager.Field.Terrain : null;
            Note($"strike tab: mode {manager.MenuMode}, selected {manager.LevelIndex} ({(manager.Mission != null ? manager.Mission.Title : "-")}), terrain " +
                 (terrain != null ? $"shown {terrain.IsShown}, tiles {terrain.TilesShown}, distance {terrain.Distance:0.0}" : "missing"));
            yield return Shot("menu_strike");
            List<int> strikes = StrikeMissions();
            if (strikes.Count > 1)
            {
                manager.SelectMission(strikes[strikes.Count - 1]);
                yield return Wait(1f);
                yield return Shot("menu_strike_last");
                manager.SelectMission(strikes[0]);
                yield return Wait(0.5f);
            }
        }


        private IEnumerator Shop()
        {
            yield return ToMenu();
            manager.SelectMode(MissionMode.Strike);
            yield return Wait(0.5f);
            if (money >= 0)
            {
                manager.Pilot.Money = money;
                manager.Pilot.NotifyChanged();
            }
            manager.OpenSupply();
            yield return Wait(1f);
            StrikeUI screens = manager.StrikeScreens;
            Note($"supply open {screens != null && screens.IsSupplyOpen}, pilot {Describe(manager.Pilot)}");
            yield return Shot("shop");
            foreach (StrikeItem item in new[] { StrikeItem.AirMissiles, StrikeItem.IonScanner, StrikeItem.PhaseShield, StrikeItem.MegaBomb, StrikeItem.EnergyModule })
            {
                int before = manager.Pilot.Money;
                BuyResult expected = StrikeArmory.CanBuy(manager.Pilot, item);
                manager.BuyItem(item);
                Note($"buy {item}: expected {expected}, money {before} -> {manager.Pilot.Money}, owned {manager.Pilot.Count(item)}");
                yield return Wait(0.4f);
            }
            yield return Shot("shop_bought");
            if (screens != null)
            {
                screens.SetSelling(true);
                yield return Wait(0.5f);
                yield return Shot("shop_sell");
                int before = manager.Pilot.Money;
                manager.SellItem(StrikeItem.MegaBomb);
                Note($"sell MegaBomb: money {before} -> {manager.Pilot.Money}, megabombs {manager.Pilot.Megabombs}");
                yield return Wait(0.5f);
                yield return Shot("shop_sold");
                screens.SetSelling(false);
            }
            manager.CloseSupply();
            yield return Wait(0.5f);
        }


        private IEnumerator Strike(int number, bool boss)
        {
            List<int> strikes = StrikeMissions();
            if (number < 1 || number > strikes.Count)
            {
                Note($"no strike mission {number} (the campaign has {strikes.Count})");
                yield break;
            }
            int index = strikes[number - 1];
            string name = boss ? $"boss{number}" : $"m{number}";
            yield return ToMenu();
            if (money >= 0 && boss)
            {
                manager.Pilot.Money = Mathf.Max(manager.Pilot.Money, money);
            }
            if (!StartMission(index))
            {
                yield break;
            }
            yield return Wait(1.2f);
            yield return Shot($"{name}_briefing");
            yield return WaitUntil(() => manager.IsMissionActive || Ended, 12f, "mission start");
            if (!manager.IsMissionActive)
            {
                Note("FAIL the strike mission did not start");
                manager.ReturnToMissionSelect();
                yield break;
            }
            Note($"{name} started: {manager.Mission.Title}, difficulty {manager.MissionDifficulty}, playground {manager.Playground.HalfSize} wraps {manager.Playground.Wraps}, " +
                 $"working {Describe(manager.Working)}");
            SetAutopilot(true, true);
            float started = Time.unscaledTime;
            float nextStatus = 0f;
            int progressShot = 0;
            int bossShots = 0;
            float nextBossShot = 0f;
            bool bossSeen = false;
            bool bossDown = false;
            bool skipped = !boss;
            // A mission without a boss (the tour's own test mission) is won by hand to see the fly-off and the results.
            bool noBoss = !(manager.Mission is StrikeLevel flown) || flown.StrikeBossPrefab == null;
            bool wonByHand = false;
            while (!Ended && Time.unscaledTime - started < timeout)
            {
                Time.timeScale = speed;
                KeepAlive();
                float elapsed = Time.unscaledTime - started;
                if (noBoss && !wonByHand && elapsed > (boss ? 4f : 14f) && manager.Objective != null)
                {
                    wonByHand = true;
                    Note("the mission has no boss: the tour wins it by hand");
                    manager.AddMoney(12345, Vector2.zero);
                    manager.Objective.Defeat();
                }
                if (!skipped && elapsed > 1.5f)
                {
                    skipped = true;
                    manager.SkipToBoss();
                    Note($"skip to boss: distance {manager.ScrollDistance:0.0}");
                }
                if (elapsed >= nextStatus)
                {
                    nextStatus = elapsed + 5f;
                    Note(Status());
                }
                if (!boss && progressShot < ProgressShots.Length && manager.ScrollProgress >= ProgressShots[progressShot])
                {
                    yield return Shot($"{name}_scroll_{Mathf.RoundToInt(ProgressShots[progressShot] * 100f)}");
                    progressShot++;
                }
                if (!bossSeen && manager.ActiveBoss != null)
                {
                    bossSeen = true;
                    nextBossShot = elapsed + 4f;
                    Note($"boss arrived: {manager.ActiveBoss.DisplayName} at distance {manager.ScrollDistance:0.0} after {elapsed:0.0} s");
                    yield return Shot($"{name}_boss_arrived");
                }
                if (bossSeen && !bossDown && bossShots < 3 && manager.ActiveBoss != null && elapsed >= nextBossShot)
                {
                    bossShots++;
                    nextBossShot = elapsed + 8f;
                    Note($"boss fight: {manager.ActiveBoss.BarFraction:0.00} left");
                    yield return Shot($"{name}_boss_fight_{bossShots}");
                }
                if (bossSeen && !bossDown && manager.ActiveBoss == null && manager.Objective != null && manager.Objective.IsComplete)
                {
                    bossDown = true;
                    Note($"boss down after {elapsed:0.0} s, money {manager.Scoring.Score}");
                    yield return Shot($"{name}_boss_down");
                }
                yield return null;
            }
            Time.timeScale = 1f;
            SetAutopilot(false, true);
            if (!Ended)
            {
                Note($"TIMEOUT {name} after {timeout} s: {Status()}");
                manager.ReturnToMissionSelect();
                yield return Wait(1f);
                yield break;
            }
            bool won = manager.State.Is(BaseGameState.Victory);
            Note($"{name} outcome: {(won ? "VICTORY" : "DEFEAT")}, money {manager.Scoring.Score}, hostiles {manager.HostilesDestroyed}/{manager.HostilesEntered}, " +
                 $"damage {manager.DamageTaken:0}, landed {manager.HasLanded}, stars {manager.Progress.Stars(index)}, pilot {Describe(manager.Pilot)}");
            yield return Wait(2f);
            yield return Shot($"{name}_results");
            manager.ReturnToMissionSelect();
            yield return Wait(1f);
            yield return Shot($"{name}_menu");
        }


        private IEnumerator Field(int index)
        {
            AsteroidsLevel mission = manager.AsteroidsCampaign != null ? manager.AsteroidsCampaign.Mission(index) : null;
            if (mission == null || mission.Mode != MissionMode.Field)
            {
                Note($"no field mission at {index}");
                yield break;
            }
            yield return ToMenu();
            if (!StartMission(index))
            {
                yield break;
            }
            yield return WaitUntil(() => manager.IsMissionActive || Ended, 12f, "mission start");
            SetAutopilot(true, false);
            Note($"field {index} started: {mission.Title}, playground {manager.Playground.HalfSize} fixed {manager.Playground.IsFixed} wraps {manager.Playground.Wraps}, " +
                 $"terrain shown {manager.Field.Terrain != null && manager.Field.Terrain.IsShown}");
            float started = Time.unscaledTime;
            bool shot = false;
            while (!Ended && Time.unscaledTime - started < 20f)
            {
                Time.timeScale = speed;
                if (!shot && Time.unscaledTime - started > 8f)
                {
                    shot = true;
                    Note($"field {index}: score {manager.Scoring.Score}, lives {manager.Lives}, targets {manager.Field.Targets.Count}");
                    yield return Shot($"field{index}_flying");
                }
                yield return null;
            }
            Time.timeScale = 1f;
            SetAutopilot(false, false);
            Note($"field {index} after 20 s: score {manager.Scoring.Score}, lives {manager.Lives}, state {manager.State.BaseState}");
            manager.ReturnToMissionSelect();
            yield return Wait(1f);
            yield return Shot($"field{index}_menu");
        }

        #endregion


        #region Helpers

        private bool Ended => manager.State != null && (manager.State.Is(BaseGameState.Victory) || manager.State.Is(BaseGameState.GameOver));


        private IEnumerator ToMenu()
        {
            if (manager.State == null || !manager.State.Is(BaseGameState.Initialization))
            {
                manager.ReturnToMissionSelect();
                yield return Wait(0.5f);
            }
        }


        private bool StartMission(int index)
        {
            try
            {
                ((IGameController)manager.Controller).PrepareGame(LevelData.Create(index));
                return true;
            }
            catch (Exception e)
            {
                Note($"FAIL starting mission {index}: {e.Message}");
                return false;
            }
        }


        /// <summary>
        /// Before the campaign has strike missions the tour flies one of its own: a copy of the campaign gets a strike sector
        /// and a plain mission made from the first field mission (no terrain theme, no events, no boss).
        /// </summary>
        private void AddTestMission()
        {
            AsteroidsCampaign original = manager.AsteroidsCampaign;
            AsteroidsLevel template = original != null ? original.Mission(0) : null;
            if (template == null)
            {
                return;
            }
            AsteroidsCampaign copy = Instantiate(original);
            var sectors = new List<AsteroidsCampaign.Sector>(copy.sectors)
            {
                new AsteroidsCampaign.Sector { title = "Test Range", starsRequired = 0, mode = MissionMode.Strike }
            };
            copy.sectors = sectors.ToArray();
            for (int i = 0; i < 3; i++)
            {
                var level = ScriptableObject.CreateInstance<StrikeLevel>();
                JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(template), level);
                JsonUtility.FromJsonOverwrite($"{{\"<Title>k__BackingField\":\"Test Run {i + 1}\"}}", level);
                level.name = $"TourStrike{i + 1}";
                level.description = "The tour's own strike mission: plain ground, no hostiles and no boss, until the campaign has its missions.";
                level.sector = sectors.Count - 1;
                level.objective = LevelObjective.Boss;
                level.waves = new WaveSpec[0];
                level.boss = null;
                level.hints = new string[0];
                level.touchHints = new string[0];
                level.segments = new[] { new TerrainSegment { kind = TerrainKind.Plain, variant = 0, tiles = 30 } };
                level.events = new StrikeEvent[0];
                level.bossAt = 300f;
                copy.LevelList.Add(level);
            }
            manager.CampaignAsset = copy;
            manager.ReturnToMissionSelect();
            Note("the campaign has no strike missions: the tour flies 3 test missions of its own");
        }


        /// <summary>The campaign indices of the strike missions, in order.</summary>
        private List<int> StrikeMissions()
        {
            var strikes = new List<int>();
            AsteroidsCampaign campaign = manager.AsteroidsCampaign;
            for (int i = 0; campaign != null && i < manager.LevelCount; i++)
            {
                if (campaign.Mission(i) is StrikeLevel)
                {
                    strikes.Add(i);
                }
            }
            return strikes;
        }


        private void SetAutopilot(bool on, bool strike)
        {
            AsteroidsPlayer ship = manager.Ship;
            if (ship == null)
            {
                return;
            }
            if (strike)
            {
                if (ship.TryGetComponent(out StrikeAutopilot autopilot))
                {
                    autopilot.enabled = on;
                }
                else if (on)
                {
                    Note("the ship has no StrikeAutopilot: it flies without input");
                }
            }
            else if (ship.TryGetComponent(out AsteroidsAutopilot autopilot))
            {
                autopilot.enabled = on;
            }
        }


        /// <summary>God mode: the working copy's energy stays full, so the autopilot always reaches the boss.</summary>
        private void KeepAlive()
        {
            StrikeLoadout working = manager.Working;
            if (!god || working == null || !manager.Ship.IsAlive || working.Energy >= StrikeRules.MaxEnergy)
            {
                return;
            }
            working.Energy = StrikeRules.MaxEnergy;
            working.NotifyChanged();
        }


        private string Status()
        {
            StrikeLoadout working = manager.Working;
            SpaceField field = manager.Field;
            StrikeTerrain terrain = field != null ? field.Terrain : null;
            Boss boss = manager.ActiveBoss;
            return string.Format(CultureInfo.InvariantCulture,
                "status: distance {0:0.0} ({1:0.00}), speed {2:0.00}, money {3}, energy {4:0}, shields {5}/{6:0}, megabombs {7}, special {8}, hostiles {9}/{10}, " +
                "targets {11}, enemy shots {12}, pickups {13}, tiles {14}, boss {15}",
                manager.ScrollDistance, manager.ScrollProgress, field != null ? field.ScrollSpeed : 0f, manager.Scoring.Score,
                working != null ? working.Energy : 0f, working != null ? working.PhaseShields : 0, working != null ? working.ShieldPoints : 0f,
                working != null ? working.Megabombs : 0, working != null ? working.Special.ToString() : "-",
                manager.HostilesDestroyed, manager.HostilesEntered, field != null ? field.Targets.Count : 0, field != null ? field.EnemyShots.Count : 0,
                field != null ? field.Rewards.Count : 0, terrain != null ? terrain.TilesShown : 0,
                boss != null ? $"{boss.DisplayName} {boss.BarFraction:0.00}" : "-");
        }


        private static string Describe(StrikeLoadout pilot)
        {
            if (pilot == null)
            {
                return "-";
            }
            var owned = new List<string>();
            foreach (StrikeItem item in (StrikeItem[])Enum.GetValues(typeof(StrikeItem)))
            {
                int count = pilot.Count(item);
                if (count > 0)
                {
                    owned.Add(count > 1 ? $"{item} x{count}" : item.ToString());
                }
            }
            return $"money {pilot.Money}, energy {pilot.Energy:0}, shields {pilot.PhaseShields}, megabombs {pilot.Megabombs}, special {pilot.Special}, " +
                   $"{pilot.Difficulty}, items [{string.Join(", ", owned)}]";
        }


        /// <summary>Keeps the progress keys of every mission and the pilot, to put them back when the tour ends.</summary>
        private void Backup()
        {
            hadPilot = manager.Progress.HasPilot;
            savedPilot = manager.Pilot.Clone();
            backedUp = true;
            IStorageStrategy disk = manager.ProgressDisk;
            if (disk == null)
            {
                return;
            }
            // Beyond the campaign too: the tour's own test missions (and missions added later) leave no keys behind.
            for (int i = 0; i < manager.LevelCount + 32; i++)
            {
                foreach (string key in new[] { StarsKey(i), BestKey(i) })
                {
                    if (disk.DoesKeyExist(key))
                    {
                        savedKeys[key] = disk.GetFloat(key);
                    }
                    else
                    {
                        missingKeys.Add(key);
                    }
                }
            }
        }


        /// <summary>
        /// Puts the backed-up keys and pilot back (a player without a saved pilot has none again); once, whether the plan
        /// finished, a step failed or the process is quitting or being torn down.
        /// </summary>
        private void Restore()
        {
            if (!backedUp || restored)
            {
                return;
            }
            restored = true;
            IStorageStrategy disk = manager != null ? manager.ProgressDisk : null;
            if (disk != null)
            {
                foreach (KeyValuePair<string, float> pair in savedKeys)
                {
                    disk.SetFloat(pair.Key, pair.Value);
                }
                foreach (string key in missingKeys)
                {
                    if (disk.DoesKeyExist(key))
                    {
                        disk.DeleteByKey(key);
                    }
                }
            }
            if (savedPilot != null && manager != null)
            {
                manager.Pilot.CopyFrom(savedPilot);
                if (manager.Progress != null)
                {
                    if (hadPilot)
                    {
                        manager.Progress.SavePilot(manager.Pilot);
                    }
                    else
                    {
                        manager.Progress.ResetPilot();
                    }
                }
            }
            disk?.TryPersist();
            Note("progress restored");
        }


        private string StarsKey(int level) => $"{manager.Type}.Progress.L{level}.Stars";

        private string BestKey(int level) => $"{manager.Type}.Progress.L{level}.Best";


        private IEnumerator Shot(string name)
        {
            yield return new WaitForEndOfFrame();
            string file = Path.Combine(folder, $"{shots:00}_{name}.png");
            shots++;
            ScreenCapture.CaptureScreenshot(file);
            Note($"shot {Path.GetFileName(file)}");
            yield return null;
        }


        private static IEnumerator Wait(float seconds)
        {
            float until = Time.unscaledTime + seconds;
            while (Time.unscaledTime < until)
            {
                yield return null;
            }
        }


        private IEnumerator WaitUntil(Func<bool> condition, float patience, string what)
        {
            float until = Time.unscaledTime + patience;
            while (!condition() && Time.unscaledTime < until)
            {
                yield return null;
            }
            if (!condition())
            {
                Note($"TIMEOUT waiting for {what}");
            }
        }


        private void OnLog(string message, string stackTrace, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
            {
                errors++;
                Note($"ERROR {type}: {message}\n{stackTrace}");
            }
            else if (type == LogType.Warning)
            {
                Note($"warning: {message}");
            }
        }


        private void Note(string message)
        {
            string line = $"{Time.realtimeSinceStartup:0.0} {message}";
            log?.WriteLine(line);
        }


        private void Quit()
        {
            Application.logMessageReceived -= OnLog;
            log?.Dispose();
            log = null;
            Application.Quit();
        }


        /// <summary>The process quits before the plan finished (killed by the harness, closed): the progress goes back first.</summary>
        private void OnApplicationQuit()
        {
            Restore();
        }


        private void OnDestroy()
        {
            Restore();
            Application.logMessageReceived -= OnLog;
            log?.Dispose();
            log = null;
        }


        private static bool HasArgument(string name)
        {
            foreach (string argument in Environment.GetCommandLineArgs())
            {
                if (string.Equals(argument, name, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }


        private static float Number(string name, float fallback)
        {
            string value = MobilePlatform.ArgumentValue(name);
            return value != null && float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float number) ? number : fallback;
        }


        private static int ParseInt(string text, int fallback)
        {
            return int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value) ? value : fallback;
        }

        #endregion
    }
}
