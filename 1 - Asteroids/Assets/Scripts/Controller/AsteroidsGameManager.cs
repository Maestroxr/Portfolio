using System;
using System.Collections.Generic;
using System.Linq;
using Gamebox;
using Gamebox.UI;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Portfolio.Asteroids
{
    /// <summary>
    /// Asteroids game module. Spawns asteroids while the game runs, keeps the players alive and saves/loads the
    /// playfield through the base game storage. The menu, pause, settings and level flow come from
    /// <see cref="BaseGameManager"/>.
    /// </summary>
    public class AsteroidsGameManager : BaseGameManager
    {
        public delegate void StateChanged(BaseGameState state);
        public event StateChanged StateChangedEvent;

        [SerializeField] private AsteroidSettings asteroidSettings;
        [SerializeField] private AsteroidsController controller;
        [SerializeField] private AsteroidsUI ui;
        [SerializeField] private Campaign campaign;
        public Playground Playground;

        [SerializeField] private Transform rewardParent;
        [SerializeField] private ShotPool shots;
        [SerializeField] private LootablePool lootables;
        [SerializeField] private ExplodablePool explodables;
        [SerializeField] private int initialAsteroids = 10;

        private AsteroidSettings customSettings;
        private AsteroidSettings activeSettings;
        private float timeUntilNextSpawn;

        private const string SavePrefix = "Asteroids.";

        public override IGameController Controller => controller;
        public override IGameUI UI => ui;
        public override ICampaign Campaign => campaign;
        public override IGameSettings DefaultSettings => asteroidSettings;
        public override IGameSettings CustomSettings => customSettings != null ? customSettings : (customSettings = CreateCustomSettings());

        public override IGameSettings Settings
        {
            get => activeSettings != null ? activeSettings : asteroidSettings;
            set => activeSettings = value as AsteroidSettings;
        }

        protected override bool UsesTimer => false;

        /// <summary>The asteroid settings in use (custom or default).</summary>
        public AsteroidSettings AsteroidSettings => Settings as AsteroidSettings ?? asteroidSettings;

        public List<AsteroidsPlayer> AsteroidPlayers => PlayerList.OfType<AsteroidsPlayer>().ToList();

        public Transform RewardParent => rewardParent;


        private AsteroidSettings CreateCustomSettings()
        {
            AsteroidSettings copy = asteroidSettings != null ? Instantiate(asteroidSettings) : ScriptableObject.CreateInstance<AsteroidSettings>();
            copy.name = $"{(asteroidSettings != null ? asteroidSettings.name : "AsteroidSettings")} (custom)";
            return copy;
        }


        protected override void Awake()
        {
            base.Awake();
            foreach (AsteroidsPlayer player in AsteroidPlayers)
            {
                player.ShotsPool = shots;
            }
        }


        protected override void Start()
        {
            base.Start();

            if (asteroidSettings == null)
            {
                UI?.UpdateError("Asteroid settings are missing.");
            }
            else if (!asteroidSettings.AreSettingsValid(out string error))
            {
                UI?.UpdateError($"Asteroid settings error: {error}.");
            }

            timeUntilNextSpawn = AsteroidSettings != null ? AsteroidSettings.AsteroidSpawnRate : 1f;

            foreach (var shootable in transform.GetComponentsInChildren<Shootable>())
            {
                SetupShootable(shootable, false);
            }

            foreach (AsteroidsPlayer player in AsteroidPlayers)
            {
                AsteroidsPlayer captured = player;
                captured.HealthChangedEvent += health =>
                {
                    if (health <= 0 && IsGameRunning)
                    {
                        GameOverForPlayer(captured);
                    }
                };
            }
        }


        protected override void Update()
        {
            base.Update();
            if (!IsGameRunning || AsteroidSettings == null)
            {
                return;
            }
            if (AsteroidSettings.SpawnAsteroid)
            {
                timeUntilNextSpawn -= Time.deltaTime;
                if (timeUntilNextSpawn < 0)
                {
                    Spawn();
                    timeUntilNextSpawn += AsteroidSettings.AsteroidSpawnRate;
                }
            }
        }


        private void Spawn()
        {
            Shootable asteroidPrefab = AsteroidSettings.RandomShootable();
            if (asteroidPrefab == null || Playground == null)
            {
                return;
            }
            Vector3 position = new Vector3(Random.Range(0, Playground.Size.x), Random.Range(0, Playground.Size.y), 0f);
            var shootable = SetupShootable(asteroidPrefab);
            if (shootable == null)
            {
                return;
            }
            shootable.transform.position = position;
            var floatable = shootable.GetComponent<Floatable>();
            if (floatable != null)
            {
                floatable.RandomDirection = true;
            }
        }


        private Shootable SetupShootable(Shootable shootable, bool deploy = true)
        {
            Shootable deployedShootable = null, shootableMirror = null;

            switch (shootable)
            {
                case Lootable lootable:
                    if (deploy)
                    {
                        lootable = lootables.Deploy();
                        deployedShootable = lootable;
                        Cyclical<Shootable> cylic = lootable.GetComponent<Cyclical<Shootable>>();
                        if (cylic != null)
                        {
                            Lootable mirror = lootables.Deploy();
                            shootableMirror = mirror;
                            mirror.OnShotEvent += (wasShot, shot) =>
                            {
                                lootables.Undeploy(wasShot as Lootable);
                            };
                            mirror.RewardParent = rewardParent;
                        }
                    }
                    else
                    {
                        lootables.AddDeployed(lootable);
                        deployedShootable = lootable;
                    }

                    lootable.RewardParent = rewardParent;
                    lootable.OnShotEvent += (wasShot, shot) =>
                    {
                        lootables.Undeploy(wasShot as Lootable);
                    };
                    break;

                case Explodable explodable:
                    if (deploy)
                    {
                        explodable = explodables.Deploy();
                        deployedShootable = explodable;

                        Cyclical<Shootable> cyclic = explodable.GetComponent<Cyclical<Shootable>>();
                        if (cyclic != null)
                        {
                            Explodable mirror = explodables.Deploy();
                            shootableMirror = mirror;
                        }
                    }
                    else
                    {
                        explodables.AddDeployed(explodable);
                        deployedShootable = explodable;
                    }
                    explodable.OnShotEvent += (wasShot, shot) =>
                    {
                        explodables.Undeploy(wasShot as Explodable);
                    };
                    break;
                default:
                    throw new ArgumentException("Unrecognized type of Shootable.");
            }

            if (shootableMirror != null)
            {
                deployedShootable.GetComponent<Cyclical<Shootable>>().Setup(shootableMirror, false, Playground);
                shootableMirror.GetComponent<Cyclical<Shootable>>().Setup(deployedShootable, true, Playground);
            }

            return deployedShootable;
        }


        private void GameOverForPlayer(AsteroidsPlayer lost)
        {
            TransitionState(BaseGameState.GameOver);
        }


        public override void TransitionState(GameState state)
        {
            base.TransitionState(state);
            StateChangedEvent?.Invoke(state.BaseState);
        }


        public override void StartGame()
        {
            ResetGame();
            TransitionState(BaseGameState.Running);
        }


        public override void LoadLevel(int level)
        {
            LevelIndex = level;
            CurrentLevel = LevelData.Create(level);
            if (Level is AsteroidsLevel asteroidsLevel && asteroidsLevel.Settings != null)
            {
                asteroidSettings = asteroidsLevel.Settings;
                if (UsingDefaultSettings)
                {
                    Settings = asteroidSettings;
                }
            }
            UpdateLevel();
        }


        private void UndeployGame()
        {
            lootables.Undeploy(lootables.Deployed);
            explodables.Undeploy(explodables.Deployed);
            shots.Undeploy(shots.Deployed);
            if (rewardParent != null)
            {
                foreach (var reward in rewardParent.GetComponentsInChildren<Reward>())
                {
                    Destroy(reward.gameObject);
                }
            }
        }


        public void ResetGame()
        {
            UndeployGame();

            for (int i = 0; i < initialAsteroids; i++)
            {
                Spawn();
            }

            foreach (AsteroidsPlayer player in AsteroidPlayers)
            {
                player.gameObject.SetActive(true);
                player.ResetForNewGame();
            }
            ResetScore();
        }


        public override bool DoesSaveGameExist()
        {
            IStorageStrategy disk = Disk;
            return disk != null && disk.DoesKeyExist(SavePrefix + "saved") && disk.GetBool(SavePrefix + "saved");
        }


        public override void SaveGame()
        {
            IStorageStrategy disk = Disk;
            if (disk == null || !(IsGameRunning || State.Is(BaseGameState.Paused)))
            {
                return;
            }

            List<AsteroidsPlayer> players = AsteroidPlayers;
            disk.SetBool(SavePrefix + "saved", true);
            disk.SetInt(SavePrefix + "players", players.Count);
            for (int i = 0; i < players.Count; i++)
            {
                AsteroidsPlayer player = players[i];
                disk.SetFloat($"{SavePrefix}player{i}-health", player.Health);
                disk.SetInt($"{SavePrefix}player{i}-points", player.Points);
                disk.SetFloat($"{SavePrefix}player{i}-positionX", player.transform.position.x);
                disk.SetFloat($"{SavePrefix}player{i}-positionY", player.transform.position.y);
                disk.SetFloat($"{SavePrefix}player{i}-rotationX", player.transform.rotation.x);
                disk.SetFloat($"{SavePrefix}player{i}-rotationY", player.transform.rotation.y);
                disk.SetFloat($"{SavePrefix}player{i}-rotationZ", player.transform.rotation.z);
                disk.SetFloat($"{SavePrefix}player{i}-rotationW", player.transform.rotation.w);
            }

            SaveCyclicPool<Lootable, Shootable>(lootables, "lootable");
            SaveCyclicPool<Explodable, Shootable>(explodables, "explodable");
            SavePool(shots, "shot");
            disk.SetFloat(SavePrefix + "score", PlayerScore);

            try
            {
                disk.Persist();
            }
            catch (NotImplementedException notImplemented)
            {
                UI?.UpdateError($"Cannot save game - storage does not support it. {notImplemented.Message}");
                return;
            }
            UI?.EnableLoad();
        }


        public override void LoadGame()
        {
            IStorageStrategy disk = Disk;
            if (disk == null || !DoesSaveGameExist())
            {
                UI?.UpdateError("There is no saved game to load.");
                return;
            }

            UndeployGame();

            List<AsteroidsPlayer> players = AsteroidPlayers;
            int playerCount = disk.GetInt(SavePrefix + "players");
            for (int i = 0; i < playerCount; i++)
            {
                if (players.Count <= i)
                {
                    if (players.Count == 0)
                    {
                        break;
                    }
                    AsteroidsPlayer clone = Instantiate(players[0], players[0].transform.parent);
                    clone.ShotsPool = shots;
                    RegisterPlayer(clone);
                    players.Add(clone);
                }
                AsteroidsPlayer player = players[i];
                player.gameObject.SetActive(true);
                player.Health = disk.GetFloat($"{SavePrefix}player{i}-health");
                player.Points = disk.GetInt($"{SavePrefix}player{i}-points");
                Vector3 position = Vector3.zero;
                position.x = disk.GetFloat($"{SavePrefix}player{i}-positionX");
                position.y = disk.GetFloat($"{SavePrefix}player{i}-positionY");
                player.transform.position = position;
                Quaternion rotation = Quaternion.identity;
                rotation.x = disk.GetFloat($"{SavePrefix}player{i}-rotationX");
                rotation.y = disk.GetFloat($"{SavePrefix}player{i}-rotationY");
                rotation.z = disk.GetFloat($"{SavePrefix}player{i}-rotationZ");
                rotation.w = disk.GetFloat($"{SavePrefix}player{i}-rotationW");
                player.transform.rotation = rotation;
            }

            LoadCyclicPool<Lootable, CyclicalShootable, Shootable>(lootables, "lootable");
            LoadCyclicPool<Explodable, CyclicalShootable, Shootable>(explodables, "explodable");
            LoadPool(shots, "shot");

            foreach (var lootable in lootables.Deployed)
            {
                lootable.RewardParent = rewardParent;
            }

            PlayerScore = disk.DoesKeyExist(SavePrefix + "score") ? disk.GetFloat(SavePrefix + "score") : 0f;
            UpdateScore();
            TransitionState(BaseGameState.Running);
        }


        private void SavePool<T>(ICache<T> pool, string name) where T : MonoBehaviour
        {
            IStorageStrategy disk = Disk;
            var deployed = new List<T>(pool.Deployed);
            disk.SetInt($"{SavePrefix}{name}s", deployed.Count);
            for (int i = 0; i < deployed.Count; i++)
            {
                disk.SetFloat($"{SavePrefix}{name}{i}-positionX", deployed[i].transform.position.x);
                disk.SetFloat($"{SavePrefix}{name}{i}-positionY", deployed[i].transform.position.y);
                Floatable floatable = deployed[i].GetComponent<Floatable>();
                if (floatable != null)
                {
                    disk.SetFloat($"{SavePrefix}{name}{i}-floatX", floatable.Direction.x);
                    disk.SetFloat($"{SavePrefix}{name}{i}-floatY", floatable.Direction.y);
                }
            }
        }


        private void SaveCyclicPool<T, K>(ICache<T> pool, string name) where T : K, ICyclic<K> where K : MonoBehaviour
        {
            IStorageStrategy disk = Disk;
            var poolDeployed = pool.Deployed;
            poolDeployed.RemoveWhere(cyclic => cyclic.IsMirror);
            var deployed = new List<T>(poolDeployed);
            disk.SetInt($"{SavePrefix}{name}s", deployed.Count);

            for (int i = 0; i < deployed.Count; i++)
            {
                disk.SetFloat($"{SavePrefix}{name}{i}-positionX", deployed[i].transform.position.x);
                disk.SetFloat($"{SavePrefix}{name}{i}-positionY", deployed[i].transform.position.y);
                Floatable floatable = deployed[i].GetComponent<Floatable>();
                if (floatable != null)
                {
                    disk.SetFloat($"{SavePrefix}{name}{i}-floatX", floatable.Direction.x);
                    disk.SetFloat($"{SavePrefix}{name}{i}-floatY", floatable.Direction.y);
                }
            }
        }


        private void LoadPool<T>(ICache<T> cache, string name) where T : MonoBehaviour
        {
            IStorageStrategy disk = Disk;
            int count = disk.GetInt($"{SavePrefix}{name}s");
            var deploy = new List<T>(cache.Deploy(count));
            for (int i = 0; i < count; i++)
            {
                T deployItem = deploy[i];
                Vector3 position = Vector3.zero;
                position.x = disk.GetFloat($"{SavePrefix}{name}{i}-positionX");
                position.y = disk.GetFloat($"{SavePrefix}{name}{i}-positionY");
                deployItem.transform.position = position;

                Floatable floatable = deployItem.GetComponent<Floatable>();
                if (floatable != null)
                {
                    Vector3 direction = Vector3.zero;
                    direction.x = disk.GetFloat($"{SavePrefix}{name}{i}-floatX");
                    direction.y = disk.GetFloat($"{SavePrefix}{name}{i}-floatY");
                    floatable.Direction = direction;
                    floatable.RandomDirection = false;
                }
            }
        }


        private void LoadCyclicPool<T, K, U>(ICache<T> cache, string name) where T : U, ICyclic<U> where K : Cyclical<U> where U : MonoBehaviour, ICyclic<U>
        {
            LoadPool(cache, name);

            IStorageStrategy disk = Disk;
            int count = disk.GetInt($"{SavePrefix}{name}s");
            List<T> deploy = new List<T>(cache.Deployed), mirrors = new List<T>(cache.Deploy(count));
            for (int i = 0; i < count && i < deploy.Count && i < mirrors.Count; i++)
            {
                T deployItem = deploy[i];
                K cyclical = deployItem.GetComponent<K>();
                if (cyclical != null)
                {
                    cyclical.Setup(mirrors[i], false, Playground);
                }

                K mirror = mirrors[i].GetComponent<K>();
                if (mirror != null)
                {
                    mirror.Setup(deployItem, true, Playground);
                }
            }
        }
    }
}
