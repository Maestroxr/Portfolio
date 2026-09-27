using System.Collections.Generic;
using UnityEngine;

namespace Portfolio.Asteroids
{
    /// <summary>
    /// A boss of the strike mode: a core with parts (turrets, launchers, modules) that each have their own hull and guns
    /// and die on their own. A part (or the core, from <see cref="coreTier"/>) is armoured while any part of a lower tier
    /// lives; clearing a tier moves the boss into its next phase. A coreless boss is defeated when its last part dies.
    /// A ground boss rests on the ground where the scroll stops; an air boss flies in to its hover line.
    ///
    /// The core fires the inherited <see cref="Boss.attacks"/> (fewer shots per use on Rookie); the parts fire their own
    /// patterns. When the core goes down first, the parts that still stand go up with it at the end.
    /// </summary>
    public class StrikeBoss : Boss
    {
        [Header("Strike boss")]
        [Tooltip("The parts in a fixed order (their index travels to the other pilots).")]
        [SerializeField] internal BossPart[] parts = new BossPart[0];
        [Tooltip("Tier of the core: armoured while a part of a lower tier lives; 0 is never armoured.")]
        [SerializeField] internal int coreTier;
        [Tooltip("Defeated when the last part dies; the core cannot be hit.")]
        [SerializeField] internal bool coreless;
        [Tooltip("Stands on the ground and moves with it (else it flies).")]
        [SerializeField] internal bool groundBoss;
        [Tooltip("Where it comes to rest, as a fraction of the playfield's half height above the middle.")]
        [SerializeField, Range(0f, 1f)] internal float holdLine = 0.55f;
        [Tooltip("Meters of side-to-side motion.")]
        [SerializeField] internal float sweep;
        [SerializeField] internal float sweepSpeed = 1f;
        [Tooltip("Parents of parts that rotate (at the matching Ring Speeds, degrees per second).")]
        [SerializeField] internal Transform[] rings = new Transform[0];
        [SerializeField] internal float[] ringSpeeds = new float[0];
        [Tooltip("Loops the boss alarm while it fights.")]
        [SerializeField] internal bool alarm;
        [Tooltip("Items it leaves when its death sequence ends.")]
        [SerializeField] internal StrikeItem[] spoils = new StrikeItem[0];
        [Tooltip("Money pickups it leaves when its death sequence ends (values of StrikeRules.MoneyValues).")]
        [SerializeField] internal int[] spoilMoney = new int[0];

        private Quaternion[] ringRest;
        private float fightTime;
        private float originX;
        private bool shieldShown;
        private bool alarmOn;

        /// <summary>The parts in their fixed order (dead ones included).</summary>
        public IReadOnlyList<BossPart> Parts => parts ?? new BossPart[0];

        public bool IsCoreless => coreless;

        public bool IsGroundBoss => groundBoss;

        public float HoldLine => holdLine;

        public int CoreTier => coreTier;

        /// <summary>The y where it comes to rest: <see cref="holdLine"/> of the playfield's half height above the middle.</summary>
        public float HoldY
        {
            get
            {
                Playground playground = Field != null ? Field.Playground : null;
                return playground != null ? playground.Middle.y + holdLine * playground.HalfSize.y : holdLine * StrikeRules.HalfSize.y;
            }
        }

        /// <summary>The parts that still stand.</summary>
        public int PartsAlive
        {
            get
            {
                int count = 0;
                if (parts != null)
                {
                    foreach (BossPart part in parts)
                    {
                        if (part != null && part.IsAlive)
                        {
                            count++;
                        }
                    }
                }
                return count;
            }
        }

        /// <summary>The core's bounty: the mission's (<see cref="SpawnService.BossBounty"/>) when it sets one, else the prefab's.</summary>
        public override int Score
        {
            get
            {
                SpawnService spawner = Field != null ? Field.Spawner : null;
                return spawner != null && spawner.BossBounty > 0 ? spawner.BossBounty : score;
            }
        }

        private bool HasParts => parts != null && parts.Length > 0;

        /// <summary>A boss with parts moves into its next phase when a tier of parts is cleared, not by its hull.</summary>
        protected override bool PhasesFollowHealth => !HasParts;


        /// <summary>
        /// Puts the parts into play after the boss itself (the spawner calls it right after adding the boss, on the
        /// simulator and on the other pilots' clients): each learns its boss and index and is added to the field.
        /// </summary>
        public void AttachParts()
        {
            if (Field == null || parts == null)
            {
                return;
            }
            for (int i = 0; i < parts.Length; i++)
            {
                BossPart part = parts[i];
                if (part == null || part.InPlay)
                {
                    continue;
                }
                part.Boss = this;
                part.index = i;
                part.IsPuppet = IsPuppet;
                part.altitude = altitude;
                if (!part.gameObject.activeSelf)
                {
                    part.gameObject.SetActive(true);
                }
                Field.Add(part);
            }
            UpdateArmour();
        }


        /// <summary>A part died (the simulator's own parts tell it): armour, phases and the coreless defeat follow.</summary>
        internal void PartDestroyed(BossPart part)
        {
            PartDestroyed(part, new DamageInfo(0f, Vector2.down, part != null ? part.Position : Position, DamageSource.Explosion, false));
        }


        /// <summary>A part died of <paramref name="hit"/>: the last one of a coreless boss defeats it, for whoever made the hit.</summary>
        internal void PartDestroyed(BossPart part, DamageInfo hit)
        {
            if (IsPuppet || !InPlay)
            {
                return;
            }
            UpdateArmour();
            UpdatePhase();
            if (coreless && PartsAlive == 0 && state != State.Dying)
            {
                Health = 0f;
                Die(hit);
            }
        }


        /// <summary>The core's hull plus every part's, over their maxima (a coreless boss counts its parts only).</summary>
        public override float BarFraction
        {
            get
            {
                if (!HasParts)
                {
                    return base.BarFraction;
                }
                float health = coreless ? 0f : Mathf.Max(0f, Health);
                float max = coreless ? 0f : MaxHealth;
                foreach (BossPart part in parts)
                {
                    if (part == null)
                    {
                        continue;
                    }
                    max += part.MaxHealth;
                    if (part.IsAlive)
                    {
                        health += part.Health;
                    }
                }
                return max > 0f ? Mathf.Clamp01(health / max) : 0f;
            }
        }


        public override void OnSpawned()
        {
            base.OnSpawned();
            altitude = groundBoss ? Altitude.Ground : Altitude.Air;
            wraps = false;
            Playground playground = Field != null ? Field.Playground : null;
            float top = playground != null ? playground.Top : StrikeRules.HalfSize.y;
            Position = new Vector2(0f, top + radius + 1f);
            fightTime = 0f;
            originX = 0f;
            shieldShown = true;
            alarmOn = false;
            if (ringRest == null && rings != null)
            {
                ringRest = new Quaternion[rings.Length];
                for (int i = 0; i < rings.Length; i++)
                {
                    ringRest[i] = rings[i] != null ? rings[i].localRotation : Quaternion.identity;
                }
            }
        }


        public override void Tick(float deltaTime)
        {
            base.Tick(deltaTime);
            if (!InPlay)
            {
                return;
            }
            if (groundBoss && Field != null)
            {
                // Standing on the ground: its velocity is its own motion there, the scroll carries it.
                Position += Field.ScrollVelocity * deltaTime;
            }
            TurnRings();
            if (!IsPuppet)
            {
                UpdateArmour();
                UpdatePhase();
            }
            else if (state == State.Entering)
            {
                // The simulator's boss comes in by moving itself, not by a velocity that travels: a puppet comes in alike.
                ComeIn(deltaTime);
            }
            UpdateAlarm();
        }


        /// <summary>
        /// The entrance. A ground boss rides the ground in and fights once the scroll has stopped (the director lets it rest
        /// exactly on its hold line; where the ground already stands it glides there). An air boss flies in to its hover line.
        /// </summary>
        protected override void Enter(float deltaTime)
        {
            Velocity = Vector2.zero;
            bool resting = ComeIn(deltaTime);
            if (groundBoss ? resting : StateTime >= entrySeconds || resting)
            {
                BeginFight();
            }
        }


        /// <summary>
        /// One step of the entrance, on the simulator and on puppets alike: a ground boss rides the ground until the
        /// scroll stops, then glides to its hold line; an air boss eases down to its hover line. True once it is there.
        /// </summary>
        private bool ComeIn(float deltaTime)
        {
            float holdY = HoldY;
            if (groundBoss)
            {
                bool scrolling = Field != null && Field.ScrollSpeed > 0.01f;
                if (scrolling)
                {
                    return false;
                }
                if (Position.y > holdY + 0.05f)
                {
                    Position = new Vector2(Position.x, Mathf.MoveTowards(Position.y, holdY, 4f * deltaTime));
                    return false;
                }
                return true;
            }
            Position = Vector2.Lerp(Position, new Vector2(Position.x, holdY), 1f - Mathf.Exp(-1.6f * deltaTime));
            return Mathf.Abs(Position.y - holdY) < 0.05f;
        }


        private void BeginFight()
        {
            SetState(State.Fighting);
            fightTime = 0f;
            originX = Position.x;
            invulnerable = false;
            SetShield(false);
            shieldShown = false;
            UpdateArmour();
        }


        /// <summary>Sweeps from side to side (deterministic by the time it has fought) on its hold line.</summary>
        protected override void Move(float deltaTime)
        {
            fightTime += deltaTime;
            float x = originX + sweep * Mathf.Sin(fightTime * sweepSpeed);
            if (groundBoss)
            {
                Velocity = new Vector2((x - Position.x) * 4f, 0f);
                return;
            }
            Velocity = (new Vector2(x, HoldY) - Position) * 3f;
        }


        /// <summary>Fewer shots per attack on Rookie (<see cref="SpawnService.BossBurstScale"/>).</summary>
        protected override int AttackCount(BossAttack attack)
        {
            float scale = Field != null && Field.Spawner != null ? Field.Spawner.BossBurstScale : 1f;
            return Mathf.Max(1, Mathf.RoundToInt(base.AttackCount(attack) * scale));
        }


        /// <summary>
        /// Armour follows the tiers: a part is armoured while a part of a lower tier stands, the core while one below
        /// <see cref="coreTier"/> stands (always when coreless); everything while it enters or dies. The core's shield shows
        /// while it is armoured.
        /// </summary>
        private void UpdateArmour()
        {
            if (!HasParts && !coreless)
            {
                return;
            }
            bool closed = state == State.Entering || state == State.Dying;
            int lowest = LowestLivingTier();
            foreach (BossPart part in parts)
            {
                if (part != null && part.IsAlive)
                {
                    part.invulnerable = closed || part.tier > lowest;
                }
            }
            bool armoured = coreless || (coreTier > 0 && lowest < coreTier);
            invulnerable = closed || armoured;
            bool shielded = state == State.Entering || (armoured && !coreless && state != State.Dying);
            if (shielded != shieldShown)
            {
                shieldShown = shielded;
                SetShield(shielded);
            }
        }


        /// <summary>A tier of parts was cleared: the next phase (at most 3), announced.</summary>
        private void UpdatePhase()
        {
            if (!HasParts || state == State.Dying)
            {
                return;
            }
            int phase = Mathf.Min(3, ClearedTiers());
            if (phase > Phase)
            {
                Phase = phase;
                AnnouncePhase();
            }
        }


        /// <summary>The lowest tier among the parts that stand; int.MaxValue when none does.</summary>
        private int LowestLivingTier()
        {
            int lowest = int.MaxValue;
            if (parts == null)
            {
                return lowest;
            }
            foreach (BossPart part in parts)
            {
                if (part != null && part.IsAlive && part.tier < lowest)
                {
                    lowest = part.tier;
                }
            }
            return lowest;
        }


        /// <summary>How many of the parts' tiers have no part standing any more (all of them below the lowest that stands).</summary>
        private int ClearedTiers()
        {
            int lowest = LowestLivingTier();
            int cleared = 0;
            for (int i = 0; i < parts.Length; i++)
            {
                BossPart part = parts[i];
                if (part == null || part.tier >= lowest)
                {
                    continue;
                }
                bool counted = false;
                for (int j = 0; j < i; j++)
                {
                    if (parts[j] != null && parts[j].tier == part.tier)
                    {
                        counted = true;
                        break;
                    }
                }
                if (!counted)
                {
                    cleared++;
                }
            }
            return cleared;
        }


        /// <summary>The rings turn by the time since the boss spawned, so every client shows them alike.</summary>
        private void TurnRings()
        {
            if (rings == null || ringRest == null)
            {
                return;
            }
            for (int i = 0; i < rings.Length && i < ringRest.Length; i++)
            {
                if (rings[i] == null)
                {
                    continue;
                }
                float speed = ringSpeeds != null && i < ringSpeeds.Length ? ringSpeeds[i] : 0f;
                rings[i].localRotation = Quaternion.Euler(0f, 0f, speed * Age) * ringRest[i];
            }
        }


        /// <summary>
        /// The alarm loops while the boss fights and a pilot is there to fight it: once the last ship is down the mission
        /// is lost, and a boss that only then finishes its entrance does not start the alarm again over the defeat.
        /// </summary>
        private void UpdateAlarm()
        {
            bool wanted = alarm && InPlay && state != State.Entering && state != State.Dying && Field != null &&
                          Field.NearestShip(Position) != null;
            SetAlarm(wanted);
        }


        private void SetAlarm(bool on)
        {
            if (alarmOn == on)
            {
                return;
            }
            alarmOn = on;
            Field?.Sounds?.SetBossAlarm(on);
        }


        /// <summary>As on the simulator: the shield shows while it comes in and while its core is armoured (never for a coreless boss).</summary>
        protected override bool ShowsShield(bool shielded, bool entering)
        {
            return entering || (shielded && !coreless);
        }


        /// <summary>A boss leaves only when it is defeated.</summary>
        protected override bool HasLeft(Playground playground)
        {
            return false;
        }


        /// <summary>The last explosion: the parts that still stand go up with it, and a ground boss leaves rubble.</summary>
        protected override void FinalBlast()
        {
            base.FinalBlast();
            SetAlarm(false);
            if (Field == null)
            {
                return;
            }
            if (parts != null)
            {
                foreach (BossPart part in parts)
                {
                    if (part != null && part.IsAlive)
                    {
                        Field.Effects?.Explosion(part.Position, Mathf.Max(0.8f, part.Radius * 1.5f), explosionTint);
                    }
                }
            }
            if (groundBoss && Field.Terrain != null)
            {
                Field.Terrain.AddCrater(Position, radius * 1.6f);
            }
        }


        /// <summary>What it leaves (the simulator only): its items and money pickups in a ring, no crystals.</summary>
        protected override void DropSpoils()
        {
            SpawnService spawner = Field != null ? Field.Spawner : null;
            if (spawner == null)
            {
                return;
            }
            int itemCount = spoils != null ? spoils.Length : 0;
            int moneyCount = spoilMoney != null ? spoilMoney.Length : 0;
            int total = itemCount + moneyCount;
            for (int i = 0; i < total; i++)
            {
                float angle = total > 1 ? i * Mathf.PI * 2f / total : 0f;
                float spread = total > 1 ? Mathf.Min(radius * 0.6f, 2.5f) : 0f;
                Vector2 at = Position + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * spread;
                if (i < itemCount)
                {
                    spawner.SpawnStrikeReward(spoils[i], 0, at);
                }
                else
                {
                    spawner.SpawnStrikeReward(StrikeItem.MachineGun, spoilMoney[i - itemCount], at);
                }
            }
        }


        protected override void OnDespawned()
        {
            // The parts leave with the boss (before the boss object goes away).
            if (parts != null)
            {
                foreach (BossPart part in parts)
                {
                    if (part != null && part.InPlay && part.Field != null)
                    {
                        part.Field.Remove(part);
                    }
                }
            }
            SetAlarm(false);
            base.OnDespawned();
        }
    }
}
