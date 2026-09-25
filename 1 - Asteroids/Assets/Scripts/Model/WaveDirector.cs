using System;
using System.Collections.Generic;
using Gamebox.Lockstep;
using UnityEngine;

namespace Portfolio.Asteroids
{
    /// <summary>What the wave director needs from the game world.</summary>
    public interface IWaveSpawner
    {
        /// <summary>Asteroids and enemies the current wave still waits for.</summary>
        int WaveTargetsAlive { get; }

        int AsteroidsAlive { get; }

        Asteroid SpawnWaveAsteroid(AsteroidKind kind);

        Asteroid SpawnDrifter(AsteroidKind kind);

        SpaceBody SpawnHazard(HazardKind kind);

        Boss SpawnBoss(Boss prefab);
    }


    /// <summary>
    /// Runs the waves of a mission. A wave starts with its large asteroids and brings its hazards one after the other;
    /// in a clearing mission it ends when everything it counts on is destroyed, in a survival mission when its time is
    /// up. Collection and survival missions repeat their last wave until the objective is met, endless missions build
    /// a new, harder wave every time, and boss missions end with the boss. Between waves the director pauses briefly.
    /// Asteroids keep drifting in during survival, collection, boss and endless missions when the settings allow it.
    /// </summary>
    public class WaveDirector
    {
        public enum Stage
        {
            Idle,
            Break,
            Wave,
            Boss,
            Done
        }

        public const float BreakTime = 2.6f;
        public const int DrifterCap = 16;

        private static readonly HazardKind[] Enemies = { HazardKind.Saucer, HazardKind.Wasp };

        private readonly AsteroidsLevel level;
        private readonly AsteroidSettings settings;
        private readonly IWaveSpawner spawner;
        private readonly System.Random random;
        private readonly List<HazardKind> hazards = new List<HazardKind>();
        private float stageTime;
        private float hazardTimer;
        private float trickleTimer;
        private float waveInterval = 6f;

        public WaveDirector(AsteroidsLevel level, AsteroidSettings settings, IWaveSpawner spawner, System.Random random)
        {
            this.level = level;
            this.settings = settings;
            this.spawner = spawner;
            this.random = random ?? new System.Random();
        }

        public Stage State { get; private set; } = Stage.Idle;

        /// <summary>The wave being played (1 based); zero before the first.</summary>
        public int WaveNumber { get; private set; }

        /// <summary>Waves the mission is planned with; zero when it has no end.</summary>
        public int WaveCount => level == null || level.IsEndless ? 0 : level.Waves.Length;

        public WaveSpec CurrentWave { get; private set; }

        public Boss ActiveBoss { get; private set; }

        /// <summary>Seconds into the current wave or break.</summary>
        public float StageTime => stageTime;

        /// <summary>Hazards of the current wave still to come.</summary>
        public int HazardsPending => hazards.Count;

        public event Action<int, WaveSpec> WaveStarted;

        public event Action<int> WaveCleared;

        public event Action<Boss> BossArrived;

        /// <summary>A clearing mission's last wave is gone (a boss mission's boss comes next).</summary>
        public event Action AllWavesCleared;

        private LevelObjective Objective => level != null ? level.Objective : LevelObjective.ClearWaves;

        private bool TimedWaves => Objective == LevelObjective.Survive;


        /// <summary>Starts with wave <paramref name="firstWave"/> after a short pause (none when <paramref name="immediately"/>).</summary>
        public void Begin(int firstWave = 1, bool immediately = false)
        {
            WaveNumber = Mathf.Max(0, firstWave - 1);
            ActiveBoss = null;
            hazards.Clear();
            trickleTimer = Rate;
            if (immediately)
            {
                StartNextWave();
            }
            else
            {
                State = Stage.Break;
                stageTime = 0f;
            }
        }


        /// <summary>
        /// Continues a saved mission at wave <paramref name="wave"/> without spawning its asteroids again (they were
        /// restored with the game); the hazards of the wave start over. A boss stage summons the boss anew.
        /// </summary>
        public void Resume(int wave, bool bossStage)
        {
            WaveNumber = Mathf.Max(1, wave);
            CurrentWave = BuildWave(WaveNumber);
            ActiveBoss = null;
            hazards.Clear();
            trickleTimer = Rate;
            stageTime = 0f;
            State = Stage.Wave;
            if (CurrentWave != null)
            {
                foreach (HazardKind kind in (HazardKind[])Enum.GetValues(typeof(HazardKind)))
                {
                    for (int i = 0; i < CurrentWave.Count(kind); i++)
                    {
                        hazards.Add(kind);
                    }
                }
                random.Shuffle(hazards);
                waveInterval = Mathf.Max(1.5f, CurrentWave.hazardInterval);
                hazardTimer = Mathf.Min(3f, waveInterval);
            }
            if (!bossStage)
            {
                return;
            }
            Boss boss = Objective == LevelObjective.Endless ? level.BossForWave(WaveNumber) : level != null ? level.BossPrefab : null;
            if (boss != null)
            {
                SummonBoss(boss);
            }
        }


        public void Stop()
        {
            State = Stage.Done;
            hazards.Clear();
        }


        public void Tick(float deltaTime)
        {
            if (State == Stage.Idle || State == Stage.Done)
            {
                return;
            }
            stageTime += deltaTime;
            switch (State)
            {
                case Stage.Break:
                    Trickle(deltaTime);
                    if (stageTime >= BreakTime)
                    {
                        StartNextWave();
                    }
                    break;
                case Stage.Wave:
                    Trickle(deltaTime);
                    SpawnHazards(deltaTime);
                    if (IsWaveOver())
                    {
                        FinishWave();
                    }
                    break;
                case Stage.Boss:
                    Trickle(deltaTime);
                    SpawnHazards(deltaTime);
                    break;
            }
        }


        /// <summary>The boss of the mission (or of an endless wave) was defeated.</summary>
        public void BossDefeated()
        {
            ActiveBoss = null;
            if (State != Stage.Boss)
            {
                return;
            }
            if (Objective == LevelObjective.Endless)
            {
                FinishWave();
            }
            else
            {
                State = Stage.Done;
            }
        }


        private float Rate => settings != null ? Mathf.Max(0.5f, settings.AsteroidSpawnRate) : 6f;


        private void StartNextWave()
        {
            WaveNumber++;
            CurrentWave = BuildWave(WaveNumber);
            State = Stage.Wave;
            stageTime = 0f;
            WaveStarted?.Invoke(WaveNumber, CurrentWave);

            bool crowded = TimedWaves && spawner.AsteroidsAlive > DrifterCap;
            if (!crowded)
            {
                foreach (AsteroidKind kind in (AsteroidKind[])Enum.GetValues(typeof(AsteroidKind)))
                {
                    for (int i = 0; i < CurrentWave.Count(kind); i++)
                    {
                        spawner.SpawnWaveAsteroid(kind);
                    }
                }
            }
            hazards.Clear();
            foreach (HazardKind kind in (HazardKind[])Enum.GetValues(typeof(HazardKind)))
            {
                for (int i = 0; i < CurrentWave.Count(kind); i++)
                {
                    hazards.Add(kind);
                }
            }
            random.Shuffle(hazards);
            waveInterval = Mathf.Max(1.5f, CurrentWave.hazardInterval);
            hazardTimer = Mathf.Min(3f, waveInterval);

            Boss boss = Objective == LevelObjective.Endless ? level.BossForWave(WaveNumber) : null;
            if (boss != null)
            {
                SummonBoss(boss);
            }
        }


        private WaveSpec BuildWave(int number)
        {
            if (level == null)
            {
                return new WaveSpec { rocks = 3 };
            }
            if (level.IsEndless)
            {
                return WaveSpec.Endless(number, random);
            }
            WaveSpec[] waves = level.Waves;
            if (waves.Length == 0)
            {
                return new WaveSpec { rocks = 3 };
            }
            if (number <= waves.Length)
            {
                return waves[number - 1];
            }
            // Past the plan (survival and collection missions): the last wave again, a little denser each time.
            WaveSpec again = waves[waves.Length - 1].Copy();
            again.title = string.Empty;
            int extra = number - waves.Length;
            again.rocks += extra / 2;
            if (Objective == LevelObjective.Collect)
            {
                again.ores += 1 + extra / 3;
            }
            return again;
        }


        private void SpawnHazards(float deltaTime)
        {
            if (hazards.Count == 0)
            {
                return;
            }
            hazardTimer -= deltaTime;
            // Once the rocks are gone, the enemies still due arrive right away instead of on schedule.
            bool hurry = !TimedWaves && spawner.WaveTargetsAlive == 0 && ContainsEnemy();
            if (hazardTimer > 0f && !(hurry && hazardTimer > 1f))
            {
                return;
            }
            hazardTimer = waveInterval * (0.8f + (float)random.NextDouble() * 0.4f);
            HazardKind next = hazards[0];
            if (hurry)
            {
                int enemy = hazards.FindIndex(IsEnemy);
                next = hazards[enemy];
                hazards.RemoveAt(enemy);
            }
            else
            {
                hazards.RemoveAt(0);
            }
            spawner.SpawnHazard(next);
        }


        private bool ContainsEnemy()
        {
            return hazards.Exists(IsEnemy);
        }


        private static bool IsEnemy(HazardKind kind)
        {
            return Array.IndexOf(Enemies, kind) >= 0;
        }


        private void Trickle(float deltaTime)
        {
            if (settings == null || !settings.SpawnAsteroid)
            {
                return;
            }
            bool trickles = Objective == LevelObjective.Survive || Objective == LevelObjective.Collect ||
                            Objective == LevelObjective.Endless || State == Stage.Boss;
            if (!trickles)
            {
                return;
            }
            trickleTimer -= deltaTime * (State == Stage.Boss ? 0.6f : 1f);
            if (trickleTimer > 0f)
            {
                return;
            }
            trickleTimer = Rate * (0.8f + (float)random.NextDouble() * 0.4f);
            if (spawner.AsteroidsAlive >= DrifterCap)
            {
                return;
            }
            spawner.SpawnDrifter(PickKind());
        }


        /// <summary>A kind of asteroid of the current wave, picked by how many of each it has.</summary>
        private AsteroidKind PickKind()
        {
            WaveSpec wave = CurrentWave;
            if (wave == null || wave.AsteroidCount == 0)
            {
                return AsteroidKind.Rock;
            }
            int roll = random.Next(wave.AsteroidCount);
            foreach (AsteroidKind kind in (AsteroidKind[])Enum.GetValues(typeof(AsteroidKind)))
            {
                roll -= wave.Count(kind);
                if (roll < 0)
                {
                    return kind;
                }
            }
            return AsteroidKind.Rock;
        }


        private bool IsWaveOver()
        {
            if (ActiveBoss != null)
            {
                return false;
            }
            if (TimedWaves)
            {
                return CurrentWave != null && stageTime >= Mathf.Max(5f, CurrentWave.duration);
            }
            if (Objective == LevelObjective.Endless && CurrentWave != null && stageTime >= CurrentWave.duration)
            {
                return true;
            }
            return stageTime > 1f && spawner.WaveTargetsAlive == 0 && !ContainsEnemy();
        }


        private void FinishWave()
        {
            hazards.RemoveAll(kind => !IsEnemy(kind));
            WaveCleared?.Invoke(WaveNumber);
            bool planned = WaveCount > 0;
            bool last = planned && WaveNumber >= WaveCount;
            if (last && (Objective == LevelObjective.ClearWaves || Objective == LevelObjective.Boss))
            {
                AllWavesCleared?.Invoke();
                if (Objective == LevelObjective.Boss && level.BossPrefab != null)
                {
                    SummonBoss(level.BossPrefab);
                    if (ActiveBoss == null)
                    {
                        // Nothing to fight (no room in the scene): the mission cannot go on.
                        State = Stage.Done;
                    }
                    return;
                }
                State = Stage.Done;
                return;
            }
            State = Stage.Break;
            stageTime = 0f;
        }


        private void SummonBoss(Boss prefab)
        {
            ActiveBoss = spawner.SpawnBoss(prefab);
            if (ActiveBoss == null)
            {
                return;
            }
            State = Stage.Boss;
            stageTime = 0f;
            BossArrived?.Invoke(ActiveBoss);
        }
    }
}
