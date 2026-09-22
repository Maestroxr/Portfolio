using System;
using System.Collections.Generic;
using Gamebox;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Portfolio.Asteroids
{
    /// <summary>
    /// Takes bodies out of their pools, places them and puts them into the <see cref="SpaceField"/>: the asteroids of a
    /// wave and their fragments, the hazards (each with its own entrance), the ship's and the enemies' projectiles,
    /// pickups, a boss's minions and the boss itself. Comets are announced before they arrive.
    ///
    /// In a shared mission the simulator's spawner works as ever (placing things away from every ship), and the other
    /// clients use it to show what the simulator announces: <see cref="SpawnPuppet"/> takes the same prefab from the same
    /// pool, and <see cref="FireGhostShot"/> shows the shots of the other pilots.
    /// </summary>
    public class SpawnService : MonoBehaviour, IWaveSpawner
    {
        private struct PendingComet
        {
            public Vector2 From;
            public Vector2 Direction;
            public float Delay;
        }

        [SerializeField] internal SpaceField field;
        [Tooltip("One pool per asteroid kind, in AsteroidKind order.")]
        [SerializeField] internal AsteroidPool[] asteroidPools = new AsteroidPool[0];
        [Tooltip("One pool per weapon, in WeaponType order.")]
        [SerializeField] internal ShotPool[] playerShotPools = new ShotPool[0];
        [SerializeField] internal ShotPool droneShotPool;
        [Tooltip("One pool per enemy projectile, in EnemyShotKind order.")]
        [SerializeField] internal ShotPool[] enemyShotPools = new ShotPool[0];
        [SerializeField] internal ExplodablePool minePool;
        [SerializeField] internal ExplodablePool bombPool;
        [SerializeField] internal LootablePool podPool;
        [SerializeField] internal EnemyPool saucerPool;
        [SerializeField] internal EnemyPool scoutPool;
        [SerializeField] internal EnemyPool waspPool;
        [SerializeField] internal HazardPool cometPool;
        [SerializeField] internal HazardPool wellPool;
        [SerializeField] internal RewardPool[] rewardPools = new RewardPool[0];
        [SerializeField] internal Reward crystal;
        [SerializeField] internal Transform bossParent;
        [SerializeField] internal float cometWarning = 1.6f;
        [SerializeField] internal float wellLifetime = 13f;

        private readonly Dictionary<Reward, RewardPool> rewardLookup = new Dictionary<Reward, RewardPool>();
        private readonly List<PendingComet> pendingComets = new List<PendingComet>();

        public SpaceField Field => field;

        public AsteroidSettings Settings { get; private set; }

        /// <summary>Speed multiplier of the asteroids: the settings times the mission's own.</summary>
        public float SpeedMultiplier { get; private set; } = 1f;

        public float ExplosionRadius => Settings != null ? Settings.AsteroidExplosionRadius : 3f;

        public Loot AsteroidLoot { get; set; }

        public float LootChance { get; set; }

        public Loot PodLoot { get; set; }

        /// <summary>Chance that a saucer is the small, aiming scout.</summary>
        public float ScoutChance { get; set; } = 0.3f;

        /// <summary>A comet will cross from the first point along the direction after the given seconds.</summary>
        public event Action<Vector2, Vector2, float> CometIncoming;

        /// <summary>A hazard or enemy entered the field.</summary>
        public event Action<HazardKind, SpaceBody> HazardSpawned;

        /// <summary>
        /// Every boss of the campaign, in an order all clients share: a boss is announced to the other pilots of a shared
        /// mission by its place in this list. Set by the manager.
        /// </summary>
        public IReadOnlyList<Boss> BossCatalog { get; set; } = new Boss[0];

        private Playground Playground => field != null ? field.Playground : null;

        /// <summary>The playfield is shared with other pilots, whose clients have to be able to show what is spawned.</summary>
        private bool IsShared => field != null && field.Link != null;

        /// <summary>Where a ship is, for aiming things at it: one of the living ships, picked at random when there are several.</summary>
        private Vector2 ShipPosition
        {
            get
            {
                if (field == null)
                {
                    return Vector2.zero;
                }
                var living = new List<AsteroidsPlayer>();
                if (field.Player != null && field.Player.IsAlive)
                {
                    living.Add(field.Player);
                }
                foreach (AsteroidsPlayer remote in field.RemoteShips)
                {
                    if (remote != null && remote.IsAlive)
                    {
                        living.Add(remote);
                    }
                }
                if (living.Count == 0)
                {
                    return field.Player != null ? field.Player.Position : Vector2.zero;
                }
                return living[Random.Range(0, living.Count)].Position;
            }
        }

        public int WaveTargetsAlive => field != null ? field.WaveTargetsAlive : 0;

        public int AsteroidsAlive => field != null ? field.CountTargets(target => target is Asteroid) : 0;


        private void Awake()
        {
            IndexRewards();
        }


        private void IndexRewards()
        {
            rewardLookup.Clear();
            foreach (RewardPool pool in rewardPools)
            {
                if (pool == null || pool.PooledPrefab == null)
                {
                    continue;
                }
                var prefab = pool.PooledPrefab.GetComponent<Reward>();
                if (prefab != null)
                {
                    rewardLookup[prefab] = pool;
                }
            }
        }


        /// <summary>Sets the numbers and drop tables of a mission.</summary>
        public void Configure(AsteroidSettings settings, AsteroidsLevel level)
        {
            Settings = settings;
            float levelSpeed = level != null ? level.SpeedMultiplier : 1f;
            SpeedMultiplier = (settings != null ? settings.AsteroidSpeed : 1f) * levelSpeed;
            AsteroidLoot = level != null ? level.AsteroidLoot : null;
            LootChance = level != null ? level.LootChance : 0f;
            PodLoot = level != null ? level.PodLoot : null;
            ScoutChance = level != null ? Mathf.Clamp01(0.15f + level.Sector * 0.18f) : 0.3f;
            pendingComets.Clear();
        }


        /// <summary>Launches the comets whose warning ran out.</summary>
        public void Tick(float deltaTime)
        {
            for (int i = pendingComets.Count - 1; i >= 0; i--)
            {
                PendingComet pending = pendingComets[i];
                pending.Delay -= deltaTime;
                if (pending.Delay > 0f)
                {
                    pendingComets[i] = pending;
                    continue;
                }
                pendingComets.RemoveAt(i);
                var comet = Deploy(cometPool) as Comet;
                if (comet != null)
                {
                    comet.Launch(pending.From, pending.Direction);
                    field.Add(comet);
                    field.Sounds?.CometPass();
                    HazardSpawned?.Invoke(HazardKind.Comet, comet);
                }
            }
        }


        /// <summary>Forgets comets that were announced but have not arrived.</summary>
        public void ClearPending()
        {
            pendingComets.Clear();
        }


        // ------------------------------------------------------------------ asteroids

        public Asteroid SpawnAsteroid(AsteroidKind kind, AsteroidSize size, Vector2 position, Vector2 direction, bool countsForWave)
        {
            AsteroidPool pool = (int)kind < asteroidPools.Length ? asteroidPools[(int)kind] : null;
            var asteroid = Deploy(pool) as Asteroid;
            if (asteroid == null)
            {
                return null;
            }
            asteroid.Position = position;
            asteroid.transform.rotation = Quaternion.identity;
            asteroid.Configure(size, SpeedMultiplier);
            asteroid.Drift(direction);
            field.Add(asteroid);
            asteroid.CountsForWave = countsForWave;
            asteroid.Loot = AsteroidLoot;
            asteroid.LootChance = LootChance;
            return asteroid;
        }


        /// <summary>A large asteroid of a wave, warping in on screen away from the ship.</summary>
        public Asteroid SpawnWaveAsteroid(AsteroidKind kind)
        {
            float radius = AsteroidRules.Radius(kind, AsteroidSize.Large);
            Vector2 position = OuterPoint(radius, 7f);
            Vector2 toMiddle = -position.normalized;
            Vector2 direction = Quaternion.Euler(0f, 0f, Random.Range(-70f, 70f)) * toMiddle;
            Asteroid asteroid = SpawnAsteroid(kind, AsteroidSize.Large, position, direction, true);
            if (asteroid != null)
            {
                field.Effects?.WarpIn(position, radius * 1.4f, AsteroidRules.Tint(kind));
                asteroid.WarpedIn = true;
            }
            return asteroid;
        }


        /// <summary>A large asteroid drifting in from beyond an edge (the steady trickle between waves).</summary>
        public Asteroid SpawnDrifter(AsteroidKind kind)
        {
            float radius = AsteroidRules.Radius(kind, AsteroidSize.Large);
            if (Playground == null)
            {
                return SpawnAsteroid(kind, AsteroidSize.Large, Random.insideUnitCircle * 10f, Random.insideUnitCircle, false);
            }
            Vector2 position = Playground.EdgePoint(radius, out Vector2 inward);
            return SpawnAsteroid(kind, AsteroidSize.Large, position, inward, false);
        }


        /// <summary>Breaks <paramref name="parent"/> into <paramref name="count"/> fragments of <paramref name="size"/>.</summary>
        public void SpawnFragments(Asteroid parent, int count, AsteroidSize size, DamageInfo hit)
        {
            Vector2 away = hit.Direction.sqrMagnitude > 0.01f ? hit.Direction.normalized : Random.insideUnitCircle.normalized;
            float baseAngle = Mathf.Atan2(away.y, away.x) * Mathf.Rad2Deg + 90f;
            Vector2 range = AsteroidRules.SpeedRange(size);
            float boost = parent.Kind == AsteroidKind.Ice ? 1.4f : 1f;
            for (int i = 0; i < count; i++)
            {
                float angle = (baseAngle + 360f * i / count + Random.Range(-25f, 25f)) * Mathf.Deg2Rad;
                var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                Asteroid fragment = SpawnAsteroid(parent.Kind, size, parent.Position + direction * parent.Radius * 0.45f, direction, parent.CountsForWave);
                if (fragment == null)
                {
                    continue;
                }
                fragment.Velocity = parent.Velocity * 0.35f + direction * Random.Range(range.x, range.y) * parent.SpeedMultiplier * boost + away * 0.6f;
            }
        }


        // ------------------------------------------------------------------ projectiles

        /// <summary>One projectile of the ship's weapon.</summary>
        public Shot FirePlayerShot(WeaponType type, int level, Vector2 position, Vector2 direction, Vector2 shipVelocity, AsteroidsPlayer shooter)
        {
            ShotPool pool = (int)type < playerShotPools.Length ? playerShotPools[(int)type] : null;
            var shot = Deploy(pool) as Shot;
            if (shot == null)
            {
                return null;
            }
            float speed = WeaponRules.Speed(type) * (type == WeaponType.Scatter ? Random.Range(0.85f, 1.15f) : 1f);
            float inherit = type == WeaponType.Laser ? 0f : 0.35f;
            shot.Position = position;
            shot.Velocity = direction.normalized * speed + shipVelocity * inherit;
            shot.Lifetime = WeaponRules.Lifetime(type) * (type == WeaponType.Scatter ? Random.Range(0.85f, 1.1f) : 1f);
            shot.Damage = WeaponRules.Damage(type, level);
            shot.Pierce = WeaponRules.Pierce(type, level);
            shot.BlastRadius = WeaponRules.BlastRadius(type);
            shot.FiredBy = shooter;
            field.Add(shot);
            field.Link?.ShotFired(shot, (byte)type, level);
            return shot;
        }


        /// <summary>A wing drone's bolt.</summary>
        public Shot FireDroneShot(Vector2 position, Vector2 direction, AsteroidsPlayer owner)
        {
            var shot = Deploy(droneShotPool) as Shot;
            if (shot == null)
            {
                return null;
            }
            shot.Position = position;
            shot.Velocity = direction.normalized * 24f;
            shot.Lifetime = 0.8f;
            shot.Damage = 0.75f;
            shot.Pierce = 1;
            shot.BlastRadius = 0f;
            shot.FiredBy = owner;
            field.Add(shot);
            field.Link?.ShotFired(shot, BodyCodec.DroneShot, 1);
            return shot;
        }


        /// <summary>
        /// The shot of a pilot on another device, as their client reported it: <paramref name="kind"/> is the weapon, or
        /// <see cref="BodyCodec.DroneShot"/>. It looks and flies like the real one and hurts nothing.
        /// </summary>
        public Shot FireGhostShot(byte kind, int level, Vector2 position, Vector2 velocity, float lifetime)
        {
            bool drone = kind == BodyCodec.DroneShot;
            ShotPool pool = drone ? droneShotPool : kind < playerShotPools.Length ? playerShotPools[kind] : null;
            var shot = Deploy(pool) as Shot;
            if (shot == null)
            {
                return null;
            }
            var type = drone ? WeaponType.Blaster : (WeaponType)kind;
            shot.Position = position;
            shot.Velocity = velocity;
            shot.Lifetime = Mathf.Max(0.05f, lifetime);
            shot.Damage = 0f;
            shot.Pierce = drone ? 1 : WeaponRules.Pierce(type, level);
            shot.BlastRadius = drone ? 0f : WeaponRules.BlastRadius(type);
            shot.IsGhost = true;
            field.Add(shot);
            return shot;
        }


        public Shot FireEnemyShot(EnemyShotKind kind, Vector2 position, Vector2 velocity)
        {
            ShotPool pool = (int)kind < enemyShotPools.Length ? enemyShotPools[(int)kind] : null;
            var shot = Deploy(pool) as Shot;
            if (shot == null)
            {
                return null;
            }
            shot.Position = position;
            shot.Velocity = velocity;
            shot.NetVariant = (int)kind;
            field.Add(shot);
            return shot;
        }


        /// <summary>The homing shards a void crystal releases.</summary>
        public void ReleaseVoidShards(Vector2 position, int count, float radius)
        {
            float offset = Random.Range(0f, 360f);
            for (int i = 0; i < count; i++)
            {
                float angle = (offset + 360f * i / count) * Mathf.Deg2Rad;
                var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                FireEnemyShot(EnemyShotKind.Shard, position + direction * radius * 0.6f, direction * 5f);
            }
        }


        // ------------------------------------------------------------------ pickups

        public Reward SpawnReward(Reward prefab, Vector2 position, Vector2 velocity)
        {
            if (prefab == null)
            {
                return null;
            }
            if (rewardLookup.Count == 0)
            {
                IndexRewards();
            }
            if (IsShared)
            {
                prefab = SharedSubstitute(prefab);
            }
            if (!rewardLookup.TryGetValue(prefab, out RewardPool pool))
            {
                Debug.LogWarning($"{name}: no pool for the reward {prefab.name}.", this);
                return null;
            }
            var reward = Deploy(pool) as Reward;
            if (reward == null)
            {
                return null;
            }
            reward.Position = position;
            reward.Velocity = velocity;
            reward.transform.rotation = Quaternion.identity;
            reward.NetVariant = Array.IndexOf(rewardPools, pool);
            field.Add(reward);
            return reward;
        }


        /// <summary>
        /// A chrono field slows the whole world for one ship, which a world shared with other pilots cannot do: there its
        /// crate holds an overdrive instead.
        /// </summary>
        private Reward SharedSubstitute(Reward prefab)
        {
            if (!(prefab is PowerUpReward powerUp) || powerUp.PowerUp != PowerUpType.Chrono)
            {
                return prefab;
            }
            foreach (Reward candidate in rewardLookup.Keys)
            {
                if (candidate is PowerUpReward other && other.PowerUp == PowerUpType.Overdrive)
                {
                    return candidate;
                }
            }
            return prefab;
        }


        public Reward SpawnCrystal(Vector2 position, Vector2 velocity)
        {
            return SpawnReward(crystal, position, velocity);
        }


        /// <summary>Rolls <paramref name="loot"/> against <paramref name="chance"/> and drops the result.</summary>
        public Reward DropLoot(Loot loot, float chance, Vector2 position)
        {
            if (loot == null || Random.value >= chance)
            {
                return null;
            }
            return SpawnReward(loot.Pick(), position, Random.insideUnitCircle * 1.2f);
        }


        // ------------------------------------------------------------------ hazards

        /// <summary>Spawns one hazard of <paramref name="kind"/> with its own entrance. Comets are announced first.</summary>
        public SpaceBody SpawnHazard(HazardKind kind)
        {
            SpaceBody body = null;
            switch (kind)
            {
                case HazardKind.Mine:
                {
                    Vector2 position = InnerPoint(2.5f, 7f);
                    body = SpawnMine(position, Random.insideUnitCircle.normalized * Random.Range(0.3f, 0.8f));
                    break;
                }
                case HazardKind.ClusterBomb:
                {
                    var bomb = Deploy(bombPool) as Explodable;
                    if (bomb != null)
                    {
                        Vector2 position = InnerPoint(3f, 6f);
                        bomb.Position = position;
                        bomb.Velocity = Random.insideUnitCircle * 0.4f;
                        field.Add(bomb);
                        field.Effects?.WarpIn(position, 1.6f, new Color(1f, 0.5f, 0.2f));
                        field.Sounds?.WarpIn();
                        bomb.WarpedIn = true;
                        body = bomb;
                    }
                    break;
                }
                case HazardKind.Comet:
                    AnnounceComet();
                    return null;
                case HazardKind.GravityWell:
                {
                    var well = Deploy(wellPool) as GravityWell;
                    if (well != null)
                    {
                        Vector2 position = InnerPoint(3.5f, 7f);
                        well.Position = position;
                        well.Lifetime = wellLifetime;
                        field.Add(well);
                        field.Sounds?.WellOpen();
                        body = well;
                    }
                    break;
                }
                case HazardKind.Saucer:
                {
                    bool scout = Random.value < ScoutChance;
                    var saucer = Deploy(scout ? scoutPool : saucerPool) as Saucer;
                    if (saucer != null)
                    {
                        float height = Playground != null ? Playground.HalfSize.y - 2.5f : 7f;
                        saucer.Enter(Random.value < 0.5f, Random.Range(-height, height), Playground);
                        field.Add(saucer);
                        saucer.CountsForWave = true;
                        saucer.Loot = PodLoot;
                        saucer.LootChance = scout ? 0.8f : 0.5f;
                        body = saucer;
                    }
                    break;
                }
                case HazardKind.Wasp:
                    body = SpawnWasp(null);
                    break;
                default:
                {
                    var pod = Deploy(podPool) as Lootable;
                    if (pod != null)
                    {
                        Vector2 inward = Vector2.right;
                        Vector2 position = Playground != null ? Playground.EdgePoint(pod.Radius, out inward) : Vector2.zero;
                        pod.Position = position;
                        pod.Velocity = inward * Random.Range(1.4f, 2.2f);
                        field.Add(pod);
                        pod.Loot = PodLoot;
                        pod.LootChance = 0f;
                        body = pod;
                    }
                    break;
                }
            }
            if (body != null)
            {
                HazardSpawned?.Invoke(kind, body);
            }
            return body;
        }


        public Mine SpawnMine(Vector2 position, Vector2 velocity)
        {
            var mine = Deploy(minePool) as Mine;
            if (mine == null)
            {
                return null;
            }
            mine.Position = position;
            mine.Velocity = velocity;
            field.Add(mine);
            field.Effects?.WarpIn(position, 1.3f, new Color(0.3f, 0.8f, 1f));
            mine.WarpedIn = true;
            return mine;
        }


        private Wasp SpawnWasp(Vector2? at)
        {
            var wasp = Deploy(waspPool) as Wasp;
            if (wasp == null)
            {
                return null;
            }
            Vector2 inward = Vector2.zero;
            Vector2 position = at ?? (Playground != null ? Playground.EdgePoint(wasp.Radius, out inward) : Vector2.zero);
            wasp.Position = position;
            wasp.Velocity = inward * 2f;
            field.Add(wasp);
            wasp.CountsForWave = true;
            wasp.Loot = AsteroidLoot;
            wasp.LootChance = 0.12f;
            return wasp;
        }


        private void AnnounceComet()
        {
            if (Playground == null)
            {
                return;
            }
            Vector2 half = Playground.HalfSize;
            // Aim through the ship's neighbourhood so the comet matters, entering from the side it is farthest from.
            Vector2 target = ShipPosition + Random.insideUnitCircle * 3f;
            Vector2 direction = Random.insideUnitCircle.normalized;
            if (direction.sqrMagnitude < 0.01f)
            {
                direction = Vector2.right;
            }
            float reach = Mathf.Max(half.x, half.y) * 2.5f;
            Vector2 from = target - direction * reach;
            // Pull the entry point back to just outside the playfield along the line.
            for (int i = 0; i < 60 && Playground.IsOutside(from, 1.2f, 0.5f); i++)
            {
                from += direction * reach / 60f;
            }
            from -= direction * 2f;
            pendingComets.Add(new PendingComet { From = from, Direction = direction, Delay = cometWarning });
            CometIncoming?.Invoke(from, direction, cometWarning);
            field.Sounds?.Warning();
        }


        // ------------------------------------------------------------------ boss

        public Boss SpawnBoss(Boss prefab)
        {
            if (prefab == null)
            {
                return null;
            }
            Boss boss = Instantiate(prefab, bossParent != null ? bossParent : transform);
            boss.name = prefab.name;
            boss.NetVariant = CatalogIndex(prefab);
            field.Add(boss);
            boss.CountsForWave = true;
            boss.Loot = PodLoot;
            boss.LootChance = 1f;
            return boss;
        }


        /// <summary>A boss's minion near <paramref name="position"/>.</summary>
        public SpaceBody SpawnMinion(MinionKind kind, AsteroidKind asteroidKind, Vector2 position)
        {
            Vector2 outward = (position - ShipPosition).sqrMagnitude > 0.01f ? Random.insideUnitCircle.normalized : Vector2.up;
            switch (kind)
            {
                case MinionKind.Wasp:
                    return SpawnWasp(position);
                case MinionKind.Saucer:
                    return SpawnHazard(HazardKind.Saucer);
                case MinionKind.Mine:
                    return SpawnMine(Clamp(position, 1f), Random.insideUnitCircle * 0.8f);
                default:
                {
                    Asteroid asteroid = SpawnAsteroid(asteroidKind, AsteroidSize.Medium, Clamp(position, 0.5f), outward, false);
                    if (asteroid != null)
                    {
                        field.Effects?.WarpIn(asteroid.Position, asteroid.Radius * 1.4f, AsteroidRules.Tint(asteroidKind));
                        asteroid.WarpedIn = true;
                    }
                    return asteroid;
                }
            }
        }


        // ------------------------------------------------------------------ shared missions

        /// <summary>
        /// What the other clients of a shared mission need to know to show <paramref name="body"/>: its kind, and the
        /// variant that picks the pool or prefab and its looks. False for what is not announced: the ships' own shots
        /// (their pilots report them) and anything the clients could not rebuild.
        /// </summary>
        public bool Describe(SpaceBody body, out BodyKind kind, out int variant)
        {
            kind = BodyKind.Asteroid;
            variant = 0;
            switch (body)
            {
                case Asteroid asteroid:
                    variant = BodyCodec.PackAsteroid(asteroid.Kind, asteroid.Size, asteroid.Shape);
                    return true;
                case Mine _:
                    kind = BodyKind.Mine;
                    return true;
                case ClusterBomb _:
                    kind = BodyKind.ClusterBomb;
                    return true;
                case Lootable _:
                    kind = BodyKind.SupplyPod;
                    return true;
                case Saucer saucer:
                    kind = saucer.aims ? BodyKind.Scout : BodyKind.Saucer;
                    return true;
                case Wasp _:
                    kind = BodyKind.Wasp;
                    return true;
                case Boss boss:
                    kind = BodyKind.Boss;
                    variant = boss.NetVariant;
                    return variant >= 0;
                case Comet _:
                    kind = BodyKind.Comet;
                    return true;
                case GravityWell _:
                    kind = BodyKind.GravityWell;
                    return true;
                case Shot shot:
                    kind = BodyKind.EnemyShot;
                    variant = shot.NetVariant;
                    return shot.IsEnemy;
                case Reward reward:
                    kind = BodyKind.Reward;
                    variant = reward.NetVariant;
                    return variant >= 0;
                default:
                    return false;
            }
        }


        /// <summary>
        /// Shows a body the simulator of a shared mission announced: the same prefab from the same pool, in play as a
        /// puppet. The caller places it (some bodies pick a place of their own when they enter the field).
        /// </summary>
        public SpaceBody SpawnPuppet(BodyKind kind, int variant, float lifetime)
        {
            SpaceBody body;
            switch (kind)
            {
                case BodyKind.Asteroid:
                {
                    BodyCodec.UnpackAsteroid(variant, out AsteroidKind asteroidKind, out AsteroidSize size, out int shape);
                    AsteroidPool pool = (int)asteroidKind < asteroidPools.Length ? asteroidPools[(int)asteroidKind] : null;
                    var asteroid = Deploy(pool) as Asteroid;
                    if (asteroid != null)
                    {
                        asteroid.transform.rotation = Quaternion.identity;
                        asteroid.Configure(size, SpeedMultiplier, shape);
                    }
                    body = asteroid;
                    break;
                }
                case BodyKind.Mine:
                    body = Deploy(minePool);
                    break;
                case BodyKind.ClusterBomb:
                    body = Deploy(bombPool);
                    break;
                case BodyKind.SupplyPod:
                    body = Deploy(podPool);
                    break;
                case BodyKind.Saucer:
                    body = Deploy(saucerPool);
                    break;
                case BodyKind.Scout:
                    body = Deploy(scoutPool);
                    break;
                case BodyKind.Wasp:
                    body = Deploy(waspPool);
                    break;
                case BodyKind.Comet:
                    body = Deploy(cometPool);
                    break;
                case BodyKind.GravityWell:
                    body = Deploy(wellPool);
                    break;
                case BodyKind.EnemyShot:
                    body = Deploy(variant >= 0 && variant < enemyShotPools.Length ? enemyShotPools[variant] : null);
                    break;
                case BodyKind.Reward:
                    body = Deploy(variant >= 0 && variant < rewardPools.Length ? rewardPools[variant] : null);
                    break;
                case BodyKind.Boss:
                {
                    Boss prefab = variant >= 0 && variant < BossCatalog.Count ? BossCatalog[variant] : null;
                    Boss boss = prefab != null ? Instantiate(prefab, bossParent != null ? bossParent : transform) : null;
                    if (boss != null)
                    {
                        boss.name = prefab.name;
                    }
                    body = boss;
                    break;
                }
                default:
                    body = null;
                    break;
            }
            if (body == null)
            {
                return null;
            }
            body.IsPuppet = true;
            body.NetVariant = variant;
            field.Add(body);
            if (body is GravityWell && lifetime > 0f)
            {
                // A black hole closes as its time runs out, which shows.
                body.Lifetime = lifetime;
            }
            HazardKind? hazard = HazardOf(kind);
            if (hazard.HasValue)
            {
                HazardSpawned?.Invoke(hazard.Value, body);
            }
            return body;
        }


        /// <summary>The entrance of a puppet whose body warped in, and the sound of the ones that announce themselves.</summary>
        public void PlayEntrance(SpaceBody puppet, BodyKind kind, bool warpedIn)
        {
            if (warpedIn)
            {
                switch (puppet)
                {
                    case Asteroid asteroid:
                        field.Effects?.WarpIn(puppet.Position, puppet.Radius * 1.4f, AsteroidRules.Tint(asteroid.Kind));
                        break;
                    case ClusterBomb _:
                        field.Effects?.WarpIn(puppet.Position, 1.6f, new Color(1f, 0.5f, 0.2f));
                        field.Sounds?.WarpIn();
                        break;
                    default:
                        field.Effects?.WarpIn(puppet.Position, 1.3f, new Color(0.3f, 0.8f, 1f));
                        break;
                }
            }
            switch (kind)
            {
                case BodyKind.Comet:
                    field.Sounds?.CometPass();
                    break;
                case BodyKind.GravityWell:
                    field.Sounds?.WellOpen();
                    break;
            }
        }


        /// <summary>The simulator of a shared mission announced a comet: the warning shows here too.</summary>
        public void ShowCometWarning(Vector2 from, Vector2 direction, float delay)
        {
            CometIncoming?.Invoke(from, direction, delay);
            field.Sounds?.Warning();
        }


        private int CatalogIndex(Boss prefab)
        {
            for (int i = 0; i < BossCatalog.Count; i++)
            {
                if (BossCatalog[i] == prefab)
                {
                    return i;
                }
            }
            return -1;
        }


        private static HazardKind? HazardOf(BodyKind kind)
        {
            switch (kind)
            {
                case BodyKind.Mine: return HazardKind.Mine;
                case BodyKind.ClusterBomb: return HazardKind.ClusterBomb;
                case BodyKind.SupplyPod: return HazardKind.SupplyPod;
                case BodyKind.Saucer:
                case BodyKind.Scout: return HazardKind.Saucer;
                case BodyKind.Wasp: return HazardKind.Wasp;
                case BodyKind.Comet: return HazardKind.Comet;
                case BodyKind.GravityWell: return HazardKind.GravityWell;
                default: return null;
            }
        }


        // ------------------------------------------------------------------ helpers

        private static SpaceBody Deploy<T>(PrefabPool<T> pool) where T : SpaceBody
        {
            if (pool == null || !pool.CanDeploy)
            {
                return null;
            }
            T body = pool.Deploy();
            body.Pool = pool as IBodyPool;
            return body;
        }


        /// <summary>A point in the outer band of the playfield at least <paramref name="minDistance"/> from the ship.</summary>
        private Vector2 OuterPoint(float radius, float minDistance)
        {
            if (Playground == null)
            {
                return Random.insideUnitCircle * 8f;
            }
            Vector2 half = Playground.HalfSize - Vector2.one * (radius + 0.3f);
            Vector2 best = Vector2.zero;
            float bestDistance = -1f;
            for (int i = 0; i < 16; i++)
            {
                Vector2 candidate;
                if (Random.value < 0.5f)
                {
                    candidate = new Vector2(Random.Range(-half.x, half.x), (Random.value < 0.5f ? -1f : 1f) * Random.Range(half.y * 0.55f, half.y));
                }
                else
                {
                    candidate = new Vector2((Random.value < 0.5f ? -1f : 1f) * Random.Range(half.x * 0.6f, half.x), Random.Range(-half.y, half.y));
                }
                float distance = field.ShipClearance(candidate);
                if (distance >= minDistance)
                {
                    return candidate;
                }
                if (distance > bestDistance)
                {
                    bestDistance = distance;
                    best = candidate;
                }
            }
            return best;
        }


        /// <summary>A point inside the playfield, <paramref name="inset"/> from the edges and away from every ship.</summary>
        private Vector2 InnerPoint(float inset, float minDistance)
        {
            return Playground != null ? Playground.RandomPointAwayFrom(field.ShipClearance, minDistance, inset) : Random.insideUnitCircle * 6f;
        }


        private Vector2 Clamp(Vector2 position, float inset)
        {
            if (Playground == null)
            {
                return position;
            }
            Vector2 half = Playground.HalfSize - Vector2.one * inset;
            return new Vector2(Mathf.Clamp(position.x, -half.x, half.x), Mathf.Clamp(position.y, -half.y, half.y));
        }
    }
}
