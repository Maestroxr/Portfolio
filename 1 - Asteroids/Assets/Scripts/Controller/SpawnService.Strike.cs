using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Portfolio.Asteroids
{
    /// <summary>
    /// The strike half of the spawner: the air and ground units of a <see cref="ScrollDirector"/>, the projectiles of the
    /// pilot's strike weapons, the strike pickups (appended to the reward pools, so a co-op claim sees a Reward), the
    /// health scaling of strike bosses, and the replication of what only the strike mode has.
    /// </summary>
    public partial class SpawnService : IStrikeSpawner
    {
        /// <summary>The first ghost shot kind of the strike weapons: kind 20 + <see cref="StrikeShotKind"/>.</summary>
        public const byte StrikeGhostBase = 20;

        [Header("Strike")]
        [Tooltip("One pool per air unit, by StrikeUnit value (0 to 6).")]
        [SerializeField] internal EnemyPool[] airPools = new EnemyPool[0];
        [Tooltip("One pool per ground unit, by StrikeUnit value - 16 (0 to 11).")]
        [SerializeField] internal EnemyPool[] groundPools = new EnemyPool[0];
        [Tooltip("One pool per strike weapon projectile, in StrikeShotKind order.")]
        [SerializeField] internal ShotPool[] strikeShotPools = new ShotPool[0];

        /// <summary>The events whose pickup has dropped in this mission (each drops it once; see <see cref="DropStrikeSpoils"/>).</summary>
        private readonly HashSet<StrikeEvent> droppedEvents = new HashSet<StrikeEvent>();

        /// <summary>What the spawner could not spawn in this mission and has said so (each once, so an empty pool does not flood the log).</summary>
        private readonly HashSet<string> warned = new HashSet<string>();

        /// <summary>The prefab's numbers of each strike projectile, which a ghost's use of the same pooled shot overwrites.</summary>
        private Shot[] strikeShotPrefabs;

        /// <summary>Multiplier of the hull of strike bosses and their parts (difficulty x co-op pilots), set by the manager.</summary>
        public float BossHealthScale { get; set; } = 1f;

        /// <summary>
        /// Multiplier of the shots per attack of strike bosses and their parts (<see cref="StrikeRules.BossBurstScale"/> of the
        /// difficulty), set by the manager; 1 by default.
        /// </summary>
        public float BossBurstScale { get; set; } = 1f;

        /// <summary>Multiplier of the hull of every strike aircraft and ground unit: the mission's toughness.</summary>
        public float Toughness { get; set; } = 1f;

        /// <summary>Money for the core of the mission's strike boss (its parts pay their prefab score).</summary>
        public int BossBounty { get; set; }

        /// <summary>Strike aircraft and ground units in play.</summary>
        public int HostilesAlive => field != null ? field.CountTargets(target => target is StrikeAircraft || target is GroundUnit) : 0;


        /// <summary>The mission's strike numbers (toughness, boss bounty); the neutral ones for a field mission.</summary>
        private void ConfigureStrike(AsteroidsLevel level)
        {
            var strike = level as StrikeLevel;
            Toughness = strike != null ? strike.Toughness : 1f;
            BossBounty = strike != null ? strike.BossBounty : 0;
            droppedEvents.Clear();
            warned.Clear();
        }


        /// <summary>Member <paramref name="member"/> of an air event, from <paramref name="start"/> after <paramref name="delay"/> seconds.</summary>
        public StrikeAircraft SpawnAircraft(StrikeUnit unit, StrikeEvent spawn, int member, Vector2 start, float delay)
        {
            int index = (int)unit;
            EnemyPool pool = StrikeUnitRules.IsAir(unit) && index < airPools.Length ? airPools[index] : null;
            var aircraft = Deploy(pool) as StrikeAircraft;
            if (aircraft == null)
            {
                WarnOnce($"no {unit} to spawn (its air pool is {(pool == null ? "missing" : "empty")}).");
                return null;
            }
            aircraft.ScaleHealth(Toughness);
            aircraft.NetVariant = index;
            aircraft.Launch(spawn, start, delay, spawn != null ? spawn.speed : 1f);
            field.Add(aircraft);
            return aircraft;
        }


        /// <summary>Member <paramref name="member"/> of a ground event at <paramref name="position"/>.</summary>
        public GroundUnit SpawnGround(StrikeUnit unit, StrikeEvent spawn, int member, Vector2 position)
        {
            int index = (int)unit - StrikeUnitRules.FirstGround;
            EnemyPool pool = StrikeUnitRules.IsGround(unit) && index < groundPools.Length ? groundPools[index] : null;
            var ground = Deploy(pool) as GroundUnit;
            if (ground == null)
            {
                WarnOnce($"no {unit} to spawn (its ground pool is {(pool == null ? "missing" : "empty")}).");
                return null;
            }
            ground.ScaleHealth(Toughness);
            ground.NetVariant = (int)unit;
            ground.Place(spawn, member, position);
            field.Add(ground);
            return ground;
        }


        /// <summary>
        /// One projectile of a strike weapon of <paramref name="shooter"/>, hitting the layers of <paramref name="reach"/>;
        /// the other pilots see it as ghost kind 20 + <paramref name="kind"/>. Its lifetime, pierce and blast are the
        /// prefab's.
        /// </summary>
        public Shot FireStrikeShot(StrikeShotKind kind, Vector2 position, Vector2 velocity, float damage, Altitude reach, AsteroidsPlayer shooter)
        {
            int index = (int)kind;
            ShotPool pool = index < strikeShotPools.Length ? strikeShotPools[index] : null;
            var shot = Deploy(pool) as Shot;
            if (shot == null)
            {
                WarnOnce($"no {kind} shot to fire (its pool is {(pool == null ? "missing" : "empty")}).");
                return null;
            }
            Shot prefab = StrikeShotPrefab(index);
            if (prefab != null)
            {
                shot.Lifetime = prefab.Lifetime;
                shot.Pierce = prefab.Pierce;
                shot.BlastRadius = prefab.BlastRadius;
            }
            shot.Position = position;
            shot.Velocity = velocity;
            shot.transform.rotation = Quaternion.identity;
            shot.Damage = damage;
            shot.Reach = reach;
            shot.FiredBy = shooter;
            field.Add(shot);
            field.Link?.ShotFired(shot, (byte)(StrikeGhostBase + index), 1);
            return shot;
        }


        /// <summary>
        /// The pickup of <paramref name="item"/> and the one of exactly <paramref name="money"/> at <paramref name="at"/>
        /// (found by item or money among the reward pools; the money one a little to the side when both drop); nothing for
        /// the machine gun and no money. Returns the item's pickup, else the money's.
        /// </summary>
        public Reward SpawnStrikeReward(StrikeItem item, int money, Vector2 at)
        {
            Reward first = null;
            if (item != StrikeItem.MachineGun)
            {
                first = SpawnStrikePickup(FindStrikePickup(item, 0), at, item.ToString());
            }
            if (money > 0)
            {
                Vector2 place = first != null ? at + new Vector2(0.9f, 0f) : at;
                Reward cash = SpawnStrikePickup(FindStrikePickup(StrikeItem.MachineGun, money), place, "$" + money);
                first ??= cash;
            }
            return first;
        }


        /// <summary>
        /// What a destroyed strike unit of <paramref name="spawn"/> leaves (the simulator only): half of the aircraft a
        /// credit orb, and its event's pickup. The pickup drops once per event (the first member destroyed), except for
        /// the units that always drop it (the transport and the depot), whose every member carries it.
        /// </summary>
        internal void DropStrikeSpoils(StrikeEvent spawn, bool air, Vector2 at)
        {
            if (spawn != null && spawn.HasPickup && (StrikeUnitRules.Info(spawn.unit).AlwaysDrops || droppedEvents.Add(spawn)))
            {
                SpawnStrikeReward(spawn.bonus, spawn.money, at);
                at += new Vector2(-0.9f, 0f);
            }
            if (air && Random.value < StrikeRules.CreditOrbChance)
            {
                SpawnStrikeReward(StrikeItem.MachineGun, StrikeRules.CreditOrb, at);
            }
        }


        /// <summary>The strike pickup prefab for <paramref name="item"/>, or for exactly <paramref name="money"/> when the item is the machine gun.</summary>
        private StrikeReward FindStrikePickup(StrikeItem item, int money)
        {
            if (rewardLookup.Count == 0)
            {
                IndexRewards();
            }
            StrikeReward energy = null;
            foreach (Reward prefab in rewardLookup.Keys)
            {
                if (!(prefab is StrikeReward pickup))
                {
                    continue;
                }
                if (item == StrikeItem.MachineGun)
                {
                    if (pickup.Item == StrikeItem.MachineGun && pickup.Money == money)
                    {
                        return pickup;
                    }
                    continue;
                }
                if (pickup.Item == item)
                {
                    return pickup;
                }
                if (item == StrikeItem.EnergyModule && pickup.Item == StrikeItem.MachineGun && pickup.Energy > 0)
                {
                    energy = pickup;
                }
            }
            return energy;
        }


        private Reward SpawnStrikePickup(StrikeReward prefab, Vector2 at, string what)
        {
            if (prefab == null)
            {
                WarnOnce($"no strike pickup for {what} among the reward pools.");
                return null;
            }
            // Pickups lie on the ground: no velocity of their own.
            return SpawnReward(prefab, at, Vector2.zero);
        }


        /// <summary>Logs <paramref name="message"/> as a warning the first time in a mission.</summary>
        private void WarnOnce(string message)
        {
            if (warned.Add(message))
            {
                Debug.LogWarning($"{name}: {message}", this);
            }
        }


        private Shot StrikeShotPrefab(int index)
        {
            if (strikeShotPrefabs == null || strikeShotPrefabs.Length != strikeShotPools.Length)
            {
                strikeShotPrefabs = new Shot[strikeShotPools.Length];
                for (int i = 0; i < strikeShotPools.Length; i++)
                {
                    GameObject prefab = strikeShotPools[i] != null ? strikeShotPools[i].PooledPrefab : null;
                    strikeShotPrefabs[i] = prefab != null ? prefab.GetComponent<Shot>() : null;
                }
            }
            return index < strikeShotPrefabs.Length ? strikeShotPrefabs[index] : null;
        }


        /// <summary>
        /// Applies <see cref="BossHealthScale"/> to a strike boss and its parts before it is added to the field (a boss of
        /// the asteroid field keeps its hull).
        /// </summary>
        private void ScaleBoss(Boss boss)
        {
            if (!(boss is StrikeBoss strike))
            {
                return;
            }
            strike.ScaleHealth(BossHealthScale);
            foreach (BossPart part in strike.Parts)
            {
                if (part != null)
                {
                    part.ScaleHealth(BossHealthScale);
                }
            }
        }


        /// <summary>The ghost pool of a strike weapon's shot kind (20 and up); null for the field's kinds.</summary>
        private ShotPool StrikeGhostPool(byte kind)
        {
            int index = kind - StrikeGhostBase;
            return kind >= StrikeGhostBase && index < strikeShotPools.Length ? strikeShotPools[index] : null;
        }


        /// <summary>
        /// What the other clients need to show a strike body the field's own cases do not know (design 2.7): an aircraft or
        /// a ground unit by its unit, a boss part by its index (the replication packs its boss's net id in front, which only
        /// it knows). False when not announced.
        /// </summary>
        private bool DescribeStrike(SpaceBody body, ref BodyKind kind, ref int variant)
        {
            switch (body)
            {
                case StrikeAircraft aircraft:
                    kind = BodyKind.StrikeAir;
                    variant = (int)aircraft.Unit;
                    return true;
                case GroundUnit ground:
                    kind = BodyKind.StrikeGround;
                    variant = (int)ground.Unit;
                    return true;
                case BossPart part:
                {
                    if (part.Boss == null)
                    {
                        return false;
                    }
                    kind = BodyKind.BossPart;
                    variant = part.Index;
                    return true;
                }
                default:
                    return false;
            }
        }


        /// <summary>Takes the body a strike puppet stands for out of its pool (the caller adds it); null for boss parts.</summary>
        private SpaceBody SpawnStrikePuppet(BodyKind kind, int variant)
        {
            Enemy unit;
            switch (kind)
            {
                case BodyKind.StrikeAir:
                    unit = Deploy(variant >= 0 && variant < airPools.Length ? airPools[variant] : null) as Enemy;
                    break;
                case BodyKind.StrikeGround:
                {
                    int index = variant - StrikeUnitRules.FirstGround;
                    unit = Deploy(index >= 0 && index < groundPools.Length ? groundPools[index] : null) as Enemy;
                    break;
                }
                default:
                    // Boss parts are bound to the parts of their boss's puppet, never spawned.
                    return null;
            }
            if (unit == null)
            {
                WarnOnce($"no puppet for {kind} {variant} (its pool is missing or empty).");
                return null;
            }
            unit.ScaleHealth(Toughness);
            return unit;
        }
    }
}
