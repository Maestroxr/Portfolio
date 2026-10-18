using UnityEngine;
using Random = UnityEngine.Random;

namespace Portfolio.Asteroids
{
    /// <summary>
    /// The spawning of the field campaigns' bodies (SpawnService.FieldCampaigns.cs): solar flares and ion storms, the
    /// sentries, minelayers, phase stalkers and corsairs (marked as the quarry when the mission hunts their kind), the
    /// freighters and stations of the escort and defence missions, a boss's minions of those kinds, and what the other
    /// clients of a shared mission need to show them (<see cref="DescribeFieldCampaign"/>, <see cref="SpawnFieldCampaignPuppet"/>).
    /// </summary>
    public partial class SpawnService
    {
        [Header("Field campaigns")]
        [SerializeField] internal HazardPool flarePool;
        [SerializeField] internal HazardPool stormPool;
        [SerializeField] internal EnemyPool sentryPool;
        [SerializeField] internal EnemyPool minelayerPool;
        [SerializeField] internal EnemyPool stalkerPool;
        [SerializeField] internal EnemyPool corsairPool;
        [Tooltip("One pool per kind of ally, in AllyKind order.")]
        [SerializeField] internal HazardPool[] allyPools = new HazardPool[0];
        [SerializeField] internal float stormLifetime = 16f;

        private AsteroidsLevel fieldLevel;


        /// <summary>The mission the next spawns are for: its quarry, and its own share of scouts among the saucers.</summary>
        private void ConfigureFieldCampaign(AsteroidsLevel level)
        {
            fieldLevel = level;
            if (level != null && level.ScoutChance >= 0f)
            {
                ScoutChance = Mathf.Clamp01(level.ScoutChance);
            }
        }


        /// <summary>One hazard or enemy of the field campaigns, with its own entrance.</summary>
        private SpaceBody SpawnFieldHazard(HazardKind kind)
        {
            switch (kind)
            {
                case HazardKind.SolarFlare:
                    return SpawnFlare((SolarFlare.Side)Random.Range(0, 4));
                case HazardKind.IonStorm:
                    return SpawnStorm(InnerPoint(3f, 5f));
                case HazardKind.Sentry:
                    return SpawnSentry(ClearOfAllies(2.5f, 7f));
                case HazardKind.Minelayer:
                {
                    var layer = Deploy(minelayerPool) as Minelayer;
                    if (layer == null)
                    {
                        return null;
                    }
                    float height = Playground != null ? Playground.HalfSize.y - 2.5f : 7f;
                    layer.Enter(Random.value < 0.5f, Random.Range(-height, height), Playground);
                    return AddEnemy(layer, kind, 0.9f);
                }
                case HazardKind.Stalker:
                {
                    // A stalker picks the spot it appears at itself, near its prey, as it enters the field.
                    var stalker = Deploy(stalkerPool) as PhaseStalker;
                    return stalker != null ? AddEnemy(stalker, kind, 0.25f) : null;
                }
                default:
                {
                    var corsair = Deploy(corsairPool) as Corsair;
                    if (corsair == null)
                    {
                        return null;
                    }
                    Vector2 inward = Vector2.right;
                    corsair.Position = Playground != null ? Playground.EdgePoint(corsair.Radius, out inward) : Vector2.zero;
                    corsair.Velocity = inward * corsair.speed * 0.7f;
                    return AddEnemy(corsair, kind, 0.2f);
                }
            }
        }


        /// <summary>Puts an enemy of the campaigns into the field as a wave target, marked when the mission hunts its kind.</summary>
        private CampaignEnemy AddEnemy(CampaignEnemy enemy, HazardKind kind, float lootChance)
        {
            field.Add(enemy);
            enemy.CountsForWave = true;
            enemy.Loot = PodLoot;
            enemy.LootChance = lootChance;
            enemy.SetMarked(fieldLevel != null && fieldLevel.IsQuarry(kind) && Random.value < fieldLevel.QuarryShare);
            return enemy;
        }


        /// <summary>A point inside the field away from the ships, and if it can, away from every freighter and station too.</summary>
        private Vector2 ClearOfAllies(float inset, float minDistance)
        {
            Vector2 point = InnerPoint(inset, minDistance);
            for (int attempt = 0; attempt < 8; attempt++)
            {
                Ally ally = Ally.Nearest(field, point, 20f);
                if (ally == null || (ally.Position - point).magnitude > ally.Radius + 4.5f)
                {
                    break;
                }
                point = InnerPoint(inset, minDistance);
            }
            return point;
        }


        /// <summary>The most minions of a kind a boss keeps in play: more only piles them up where nobody can dodge them all.</summary>
        private bool MinionsAtCap(MinionKind kind)
        {
            switch (kind)
            {
                case MinionKind.Corsair:
                    return field.CountTargets(target => target is Corsair) >= 3;
                case MinionKind.Stalker:
                    return field.CountTargets(target => target is PhaseStalker) >= 3;
                case MinionKind.Sentry:
                    return field.CountTargets(target => target is Sentry) >= 3;
                case MinionKind.Minelayer:
                    return field.CountTargets(target => target is Minelayer) >= 2;
                case MinionKind.IonStorm:
                {
                    int storms = 0;
                    foreach (SpaceBody body in field.Bodies)
                    {
                        storms += body is IonStorm && body.InPlay ? 1 : 0;
                    }
                    return storms >= 1;
                }
                default:
                    return false;
            }
        }


        private Sentry SpawnSentry(Vector2 position)
        {
            var sentry = Deploy(sentryPool) as Sentry;
            if (sentry == null)
            {
                return null;
            }
            sentry.Position = position;
            sentry.Velocity = Random.insideUnitCircle.normalized * sentry.drift;
            AddEnemy(sentry, HazardKind.Sentry, 0.35f);
            field.Effects?.WarpIn(position, sentry.Radius * 1.6f, new Color(1f, 0.6f, 0.25f));
            field.Sounds?.WarpIn();
            sentry.WarpedIn = true;
            return sentry;
        }


        /// <summary>A solar flare from <paramref name="side"/>: the warning first, then the wall of fire.</summary>
        public SolarFlare SpawnFlare(SolarFlare.Side side)
        {
            var flare = Deploy(flarePool) as SolarFlare;
            if (flare == null)
            {
                return null;
            }
            flare.NetVariant = (int)side;
            field.Add(flare);
            flare.Begin(side);
            return flare;
        }


        private IonStorm SpawnStorm(Vector2 position)
        {
            var storm = Deploy(stormPool) as IonStorm;
            if (storm == null)
            {
                return null;
            }
            field.Add(storm);
            storm.Lifetime = stormLifetime;
            storm.Begin(position);
            return storm;
        }


        /// <summary>
        /// A freighter or station of <paramref name="hull"/> points, not yet placed (the caller launches or anchors it); null
        /// when the scene has no pool for it.
        /// </summary>
        public Ally SpawnAlly(AllyKind kind, float hull)
        {
            HazardPool pool = (int)kind < allyPools.Length ? allyPools[(int)kind] : null;
            if (pool == null || !pool.CanDeploy)
            {
                WarnOnce($"no {kind} to protect (its pool is {(pool == null ? "missing" : "empty")}).");
                return null;
            }
            var ally = Deploy(pool) as Ally;
            if (ally == null)
            {
                return null;
            }
            ally.Configure(hull);
            ally.NetVariant = AllyVariant(kind, ally.MaxHull);
            field.Add(ally);
            return ally;
        }


        /// <summary>A boss's minion of the campaigns' kinds near <paramref name="position"/>.</summary>
        private SpaceBody SpawnFieldMinion(MinionKind kind, Vector2 position)
        {
            if (MinionsAtCap(kind))
            {
                return null;
            }
            switch (kind)
            {
                case MinionKind.Sentry:
                    return SpawnSentry(position);
                case MinionKind.Stalker:
                    return SpawnFieldHazard(HazardKind.Stalker);
                case MinionKind.IonStorm:
                    return SpawnStorm(position);
                case MinionKind.Minelayer:
                    return SpawnFieldHazard(HazardKind.Minelayer);
                case MinionKind.Corsair:
                {
                    var corsair = Deploy(corsairPool) as Corsair;
                    if (corsair == null)
                    {
                        return null;
                    }
                    corsair.Position = position;
                    corsair.Velocity = Random.insideUnitCircle.normalized * corsair.speed * 0.5f;
                    AddEnemy(corsair, HazardKind.Corsair, 0.2f);
                    field.Effects?.WarpIn(position, 1.3f, new Color(1f, 0.4f, 0.35f));
                    corsair.WarpedIn = true;
                    return corsair;
                }
                default:
                {
                    var well = Deploy(wellPool) as GravityWell;
                    if (well == null)
                    {
                        return null;
                    }
                    well.Position = position;
                    well.Lifetime = 6f;
                    field.Add(well);
                    field.Sounds?.WellOpen();
                    return well;
                }
            }
        }


        /// <summary>What the other clients need to show a body of the campaigns; false for any other body.</summary>
        private bool DescribeFieldCampaign(SpaceBody body, ref BodyKind kind, ref int variant)
        {
            switch (body)
            {
                case SolarFlare flare:
                    kind = BodyKind.SolarFlare;
                    variant = flare.NetVariant;
                    return true;
                case IonStorm _:
                    kind = BodyKind.IonStorm;
                    variant = 0;
                    return true;
                case Ally ally:
                    kind = BodyKind.Ally;
                    variant = AllyVariant(ally.Kind, ally.MaxHull);
                    return true;
                case CampaignEnemy enemy:
                    kind = BodyKindOf(enemy.Hazard);
                    variant = enemy.Marked ? CampaignEnemy.MarkedVariant : 0;
                    return true;
                default:
                    return false;
            }
        }


        /// <summary>The puppet of a body of the campaigns from its pool, or null for any other kind.</summary>
        private SpaceBody SpawnFieldCampaignPuppet(BodyKind kind, int variant)
        {
            switch (kind)
            {
                case BodyKind.SolarFlare: return Deploy(flarePool);
                case BodyKind.IonStorm: return Deploy(stormPool);
                case BodyKind.Sentry: return Deploy(sentryPool);
                case BodyKind.Minelayer: return Deploy(minelayerPool);
                case BodyKind.Stalker: return Deploy(stalkerPool);
                case BodyKind.Corsair: return Deploy(corsairPool);
                case BodyKind.Ally:
                {
                    int ally = variant & AllyKindMask;
                    var puppet = Deploy(ally < allyPools.Length ? allyPools[ally] : null) as Ally;
                    puppet?.Configure(variant >> AllyHullShift);
                    return puppet;
                }
                default: return null;
            }
        }


        private const int AllyKindMask = 0xF;
        private const int AllyHullShift = 4;


        /// <summary>An ally on the wire: its kind in the lowest four bits, its full hull (whole points) above them.</summary>
        internal static int AllyVariant(AllyKind kind, float hull)
        {
            return ((int)kind & AllyKindMask) | (Mathf.Clamp(Mathf.RoundToInt(hull), 1, 100000) << AllyHullShift);
        }


        private static BodyKind BodyKindOf(HazardKind hazard)
        {
            switch (hazard)
            {
                case HazardKind.Sentry: return BodyKind.Sentry;
                case HazardKind.Minelayer: return BodyKind.Minelayer;
                case HazardKind.Stalker: return BodyKind.Stalker;
                default: return BodyKind.Corsair;
            }
        }


        /// <summary>The hazard a body of the campaigns counts as (for the warnings of the HUD), or null.</summary>
        private static HazardKind? FieldHazardOf(BodyKind kind)
        {
            switch (kind)
            {
                case BodyKind.SolarFlare: return HazardKind.SolarFlare;
                case BodyKind.IonStorm: return HazardKind.IonStorm;
                case BodyKind.Sentry: return HazardKind.Sentry;
                case BodyKind.Minelayer: return HazardKind.Minelayer;
                case BodyKind.Stalker: return HazardKind.Stalker;
                case BodyKind.Corsair: return HazardKind.Corsair;
                default: return null;
            }
        }
    }
}
