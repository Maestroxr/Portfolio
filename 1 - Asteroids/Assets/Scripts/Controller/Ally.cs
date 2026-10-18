using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Portfolio.Asteroids
{
    /// <summary>The friendly bodies of the field campaigns. The numbers travel as a body's variant: append only.</summary>
    public enum AllyKind
    {
        /// <summary>A freighter that crosses the sector (escort missions).</summary>
        Freighter = 0,
        /// <summary>A station that stays where it is (defence missions).</summary>
        Station = 1
    }


    /// <summary>
    /// A freighter to escort or a station to defend. It is no target: the pilots' shots, blasts of their weapons and
    /// nova bombs pass it by, and ships fly over it. What the enemies throw at it hurts it: enemy fire, beams and
    /// lightning, the rocks that drift into it (they glance off and chip), mines and comets, blasts that hurt ships. A
    /// freighter flies from one side of the sector to the other and jumps away when it gets there; a station turns
    /// where it was placed. A bar over it shows the hull; at zero it breaks up, and the mission is lost.
    ///
    /// Only the simulator of a shared mission decides what hurts it. The others show a puppet whose hull follows the
    /// simulator's reports (the row's health), which is also how they learn that it was lost.
    /// </summary>
    public class Ally : SpaceBody
    {
        /// <summary>The allies in play, of any field.</summary>
        public static readonly List<Ally> Active = new List<Ally>();

        /// <summary>Seconds before the same body can hurt the ally by running into it again.</summary>
        private const float ContactCooldown = 1.5f;

        [SerializeField] internal AllyKind kind;
        [SerializeField] internal float maxHull = 120f;
        [Tooltip("Share of an enemy shot's damage the hull takes.")]
        [SerializeField] internal float shotFactor = 0.25f;
        [Tooltip("Share of a rock's or an enemy's contact damage the hull takes when it runs into the ally.")]
        [SerializeField] internal float contactFactor = 0.25f;
        [Tooltip("Share of a blast's damage to ships the hull takes.")]
        [SerializeField] internal float blastFactor = 0.3f;
        [SerializeField] internal float cometDamage = 35f;
        [Tooltip("The fill of the hull bar (a quad one unit wide in its parent, the bar): scaled across by the hull left, coloured from green to red.")]
        [SerializeField] internal Transform hullBar;
        [Tooltip("How far above the ally the bar floats; the bar stays level whatever way the ally turns.")]
        [SerializeField] internal float barHeight = 2.4f;
        [SerializeField] internal Renderer hullFill;
        [Tooltip("Turns slowly (a station's ring).")]
        [SerializeField] internal Transform spinner;
        [SerializeField] internal float spinSpeed = 10f;
        [Tooltip("Renderers that flash when the ally is hit.")]
        [SerializeField] internal Renderer[] flashRenderers = new Renderer[0];
        [Tooltip("Engine flames, which flicker while a freighter flies.")]
        [SerializeField] internal Transform[] flames = new Transform[0];
        [Tooltip("Smoke that pours out once the hull is below half.")]
        [SerializeField] internal ParticleSystem smoke;
        [SerializeField] internal AudioClip alarm;
        [SerializeField] internal AudioClip jump;

        private static MaterialPropertyBlock block;
        private static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");
        private static readonly int ColorId = Shader.PropertyToID("_BaseColor");
        private readonly Dictionary<SpaceBody, float> contacts = new Dictionary<SpaceBody, float>();
        private readonly List<SpaceBody> stale = new List<SpaceBody>();
        private float flash;
        private bool flashing;
        private float alarmGap;
        private float dying;
        private float travel;
        private float travelled;
        private bool subscribed;
        private SpaceField listened;

        public AllyKind Kind => kind;

        public float Hull { get; private set; }

        public float MaxHull => maxHull;

        public float HullFraction => maxHull > 0f ? Mathf.Clamp01(Hull / maxHull) : 0f;

        /// <summary>The hull is gone: the ally is breaking up.</summary>
        public bool IsWrecked { get; private set; }

        /// <summary>The share of its crossing a freighter has flown (0 to 1); a station's is 0.</summary>
        public float Share => kind == AllyKind.Freighter && travel > 0f ? Mathf.Clamp01(travelled / travel) : 0f;

        /// <summary>The hull ran out (on every client: a puppet hears it from the simulator).</summary>
        public event Action<Ally> Wrecked;

        /// <summary>A freighter reached the far side and jumped away (the simulator only).</summary>
        public event Action<Ally> Delivered;

        /// <summary>The hull went down (the simulator only), for the warnings of the HUD.</summary>
        public event Action<Ally, float> Damaged;


        /// <summary>The nearest ally of <paramref name="field"/> that still stands, within <paramref name="range"/> of <paramref name="from"/>.</summary>
        public static Ally Nearest(SpaceField field, Vector2 from, float range)
        {
            Ally best = null;
            float bestDistance = range * range;
            for (int i = 0; i < Active.Count; i++)
            {
                Ally ally = Active[i];
                if (ally == null || !ally.InPlay || ally.IsWrecked || ally.Field != field)
                {
                    continue;
                }
                Vector2 offset = field != null && field.Playground != null ? field.Playground.Delta(from, ally.Position) : ally.Position - from;
                float distance = offset.sqrMagnitude;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = ally;
                }
            }
            return best;
        }


        /// <summary>Sets the hull of the next ally to come out of the pool. Call it before the ally is added to the field.</summary>
        public void Configure(float hull)
        {
            maxHull = Mathf.Max(1f, hull);
        }


        public override void OnSpawned()
        {
            base.OnSpawned();
            Hull = maxHull;
            IsWrecked = false;
            dying = 0f;
            flash = 0f;
            alarmGap = 0f;
            travel = 0f;
            travelled = 0f;
            contacts.Clear();
            wraps = false;
            pulledByGravity = false;
            // Allies of a scene that was unloaded never left play: they go from the list here.
            Active.RemoveAll(ally => ally == null);
            if (!Active.Contains(this))
            {
                Active.Add(this);
            }
            if (smoke != null)
            {
                smoke.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
            if (!IsPuppet && Field != null)
            {
                listened = Field;
                listened.Exploded += OnExploded;
                subscribed = true;
            }
            ShowHullBar();
        }


        /// <summary>A freighter's crossing: from <paramref name="from"/> (outside an edge) to <paramref name="to"/> (beyond the far one) in <paramref name="seconds"/>.</summary>
        public void Launch(Vector2 from, Vector2 to, float seconds)
        {
            Position = from;
            travel = Mathf.Max(1f, seconds);
            travelled = 0f;
            Vector2 way = to - from;
            Velocity = way / travel;
            float angle = Mathf.Atan2(way.y, way.x) * Mathf.Rad2Deg - 90f;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }


        /// <summary>A station's place.</summary>
        public void Anchor(Vector2 at)
        {
            Position = at;
            Velocity = Vector2.zero;
            transform.rotation = Quaternion.identity;
        }


        public override void Tick(float deltaTime)
        {
            base.Tick(deltaTime);
            if (!InPlay)
            {
                return;
            }
            if (spinner != null)
            {
                spinner.localRotation = Quaternion.Euler(0f, 0f, spinSpeed * deltaTime) * spinner.localRotation;
            }
            if (kind == AllyKind.Freighter && Velocity.sqrMagnitude > 0.0001f && !IsWrecked)
            {
                float angle = Mathf.Atan2(Velocity.y, Velocity.x) * Mathf.Rad2Deg - 90f;
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.Euler(0f, 0f, angle), 30f * deltaTime);
            }
            for (int i = 0; i < flames.Length; i++)
            {
                if (flames[i] != null)
                {
                    flames[i].localScale = new Vector3(flames[i].localScale.x, IsWrecked ? 0.05f : 0.8f + 0.2f * Mathf.Sin(Age * 30f + i * 1.7f), 1f);
                }
            }
            alarmGap -= deltaTime;
            Animate(deltaTime);
            if (IsWrecked)
            {
                BreakUp(deltaTime);
                return;
            }
            if (!IsPuppet)
            {
                travelled += deltaTime;
                Collide(deltaTime);
            }
        }


        // ------------------------------------------------------------------ what hurts it (the simulator)

        private void Collide(float deltaTime)
        {
            SpaceField field = Field;
            if (field == null)
            {
                return;
            }
            IReadOnlyList<Shot> shots = field.EnemyShots;
            for (int i = 0; i < shots.Count; i++)
            {
                Shot shot = shots[i];
                if (!shot.InPlay)
                {
                    continue;
                }
                if (shot is IBurnsAllies burner)
                {
                    float burned = burner.Burn(this, deltaTime);
                    if (burned > 0f)
                    {
                        Damage(burned * shotFactor);
                    }
                    continue;
                }
                if (!shot.StopsOnHit || !shot.Touches(Position, radius))
                {
                    continue;
                }
                Damage(shot.Damage * shotFactor);
                shot.Impact();
                if (IsWrecked)
                {
                    return;
                }
            }
            IReadOnlyList<Shootable> targets = field.Targets;
            for (int i = 0; i < targets.Count; i++)
            {
                Shootable target = targets[i];
                if (!target.IsAlive || target is Boss || target.ContactDamage <= 0f)
                {
                    continue;
                }
                Vector2 delta = target.Position - Position;
                float reach = radius + target.Radius * 0.9f;
                if (delta.sqrMagnitude > reach * reach)
                {
                    continue;
                }
                float distance = delta.magnitude;
                Vector2 away = distance > 0.001f ? delta / distance : Random.insideUnitCircle.normalized;
                // Pushed clear, so the two do not stay inside each other.
                target.Position += away * (reach - distance);
                if (contacts.TryGetValue(target, out float last) && Age - last < ContactCooldown)
                {
                    continue;
                }
                contacts[target] = Age;
                Damage(target.ContactDamage * contactFactor);
                target.Velocity = Vector2.Reflect(target.Velocity, away) * 0.8f + away * 1.5f;
                if (target is Explodable explodable)
                {
                    explodable.Detonate(false);
                }
                else
                {
                    target.TakeHit(new DamageInfo(1f, away, target.Position - away * target.Radius, DamageSource.Collision, false));
                }
                if (IsWrecked)
                {
                    return;
                }
            }
            IReadOnlyList<Comet> comets = field.Comets;
            for (int i = 0; i < comets.Count; i++)
            {
                Comet comet = comets[i];
                float reach = radius + comet.Radius;
                if (!comet.InPlay || (comet.Position - Position).sqrMagnitude > reach * reach || contacts.ContainsKey(comet))
                {
                    continue;
                }
                contacts[comet] = Age;
                Damage(cometDamage);
            }
            ForgetOldContacts();
        }


        private void ForgetOldContacts()
        {
            if (contacts.Count < 24)
            {
                return;
            }
            stale.Clear();
            foreach (KeyValuePair<SpaceBody, float> entry in contacts)
            {
                if (entry.Key == null || !entry.Key.InPlay || Age - entry.Value > 3f)
                {
                    stale.Add(entry.Key);
                }
            }
            foreach (SpaceBody body in stale)
            {
                contacts.Remove(body);
            }
        }


        private void OnExploded(Blast blast)
        {
            if (!InPlay || IsPuppet || IsWrecked || blast.PlayerDamage <= 0f)
            {
                return;
            }
            float reach = blast.Radius + radius;
            Vector2 delta = Position - blast.Center;
            if (delta.sqrMagnitude > reach * reach)
            {
                return;
            }
            float falloff = 1f - 0.5f * Mathf.Clamp01(delta.magnitude / reach);
            Damage(blast.PlayerDamage * blastFactor * falloff);
        }


        /// <summary>Something burned or struck the ally for <paramref name="amount"/> (the simulator only).</summary>
        public void Damage(float amount)
        {
            if (IsPuppet || IsWrecked || amount <= 0f || !InPlay)
            {
                return;
            }
            Hull = Mathf.Max(0f, Hull - amount);
            Hurt();
            Damaged?.Invoke(this, amount);
            if (Hull <= 0f)
            {
                Wreck();
            }
        }


        /// <summary>The simulator reports the hull of the ally a puppet stands for.</summary>
        internal void ShowHull(float hull)
        {
            if (!IsPuppet || IsWrecked)
            {
                return;
            }
            if (hull < Hull - 0.001f)
            {
                Hurt();
            }
            Hull = Mathf.Clamp(hull, 0f, maxHull);
            if (Hull <= 0f)
            {
                Wreck();
            }
        }


        private void Hurt()
        {
            flash = 1f;
            if (alarmGap <= 0f)
            {
                alarmGap = 1.2f;
                Field?.Sounds?.Play(alarm, 0.45f, 1f, 0f, 0.5f);
            }
        }


        private void Wreck()
        {
            if (IsWrecked)
            {
                return;
            }
            IsWrecked = true;
            dying = 0f;
            Velocity *= 0.25f;
            Field?.Sounds?.Explosion(1.4f);
            Field?.CameraRig?.Shake(0.6f);
            Wrecked?.Invoke(this);
        }


        /// <summary>The ally breaks up in a chain of explosions; the simulator takes it out after them.</summary>
        private void BreakUp(float deltaTime)
        {
            float before = dying;
            dying += deltaTime;
            if (Mathf.FloorToInt(before / 0.18f) != Mathf.FloorToInt(dying / 0.18f))
            {
                Field?.Effects?.Explosion(Position + Random.insideUnitCircle * radius, Random.Range(0.8f, 1.5f), new Color(1f, 0.6f, 0.3f));
                Field?.Sounds?.Explosion(0.6f);
            }
            if (dying >= 1.8f && !IsPuppet)
            {
                Field?.Effects?.Explosion(Position, radius * 1.8f, new Color(1f, 0.55f, 0.25f));
                Field?.Effects?.Shockwave(Position, radius * 4f, new Color(1f, 0.6f, 0.3f));
                Exit = ExitReason.Destroyed;
                Despawn();
            }
        }


        /// <summary>A freighter flew off the far side: it made it.</summary>
        protected override void LeftPlayfield()
        {
            if (kind == AllyKind.Freighter && !IsWrecked)
            {
                Field?.Effects?.WarpIn(Position, radius * 2f, new Color(0.5f, 0.9f, 1f));
                Field?.Sounds?.Play(jump, 0.7f, 1f, 0f, 0.2f);
                Delivered?.Invoke(this);
            }
            base.LeftPlayfield();
        }


        // ------------------------------------------------------------------ looks

        private void Animate(float deltaTime)
        {
            if (flash > 0f)
            {
                flash = Mathf.Max(0f, flash - deltaTime * 6f);
                SetFlash(flash);
            }
            else if (flashing)
            {
                SetFlash(0f);
                flashing = false;
            }
            if (smoke != null)
            {
                bool smoking = HullFraction < 0.5f || IsWrecked;
                if (smoking && !smoke.isPlaying)
                {
                    smoke.Play(true);
                }
                else if (!smoking && smoke.isPlaying)
                {
                    smoke.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                }
            }
            ShowHullBar();
        }


        private void ShowHullBar()
        {
            if (hullBar != null)
            {
                float fill = HullFraction;
                hullBar.localScale = new Vector3(Mathf.Max(0.001f, fill), hullBar.localScale.y, hullBar.localScale.z);
                hullBar.localPosition = new Vector3(-(1f - fill) * 0.5f, hullBar.localPosition.y, hullBar.localPosition.z);
                Transform bar = hullBar.parent;
                if (bar != null && bar != transform)
                {
                    bar.rotation = Quaternion.identity;
                    bar.position = transform.position + new Vector3(0f, barHeight, -1f);
                }
            }
            if (hullFill != null)
            {
                float fill = HullFraction;
                Color color = fill > 0.5f ? Color.Lerp(new Color(1f, 0.85f, 0.2f), new Color(0.35f, 1f, 0.45f), (fill - 0.5f) * 2f)
                    : Color.Lerp(new Color(1f, 0.25f, 0.2f), new Color(1f, 0.85f, 0.2f), fill * 2f);
                block ??= new MaterialPropertyBlock();
                block.Clear();
                block.SetColor(ColorId, color * 2.2f);
                hullFill.SetPropertyBlock(block);
            }
        }


        private void SetFlash(float amount)
        {
            block ??= new MaterialPropertyBlock();
            foreach (Renderer target in flashRenderers)
            {
                if (target == null)
                {
                    continue;
                }
                if (amount <= 0f)
                {
                    target.SetPropertyBlock(null);
                    continue;
                }
                block.Clear();
                block.SetColor(EmissionId, new Color(1f, 0.4f, 0.3f) * (amount * 2.5f));
                target.SetPropertyBlock(block);
            }
            flashing = amount > 0f;
        }


        protected override void OnDespawned()
        {
            base.OnDespawned();
            Active.Remove(this);
            if (subscribed && listened != null)
            {
                listened.Exploded -= OnExploded;
            }
            subscribed = false;
            listened = null;
            contacts.Clear();
            SetFlash(0f);
            Wrecked = null;
            Delivered = null;
            Damaged = null;
            IsWrecked = false;
            if (smoke != null)
            {
                smoke.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }
    }


    /// <summary>A shot that burns what it touches over time (a beam, a lightning strike) and so burns allies too.</summary>
    public interface IBurnsAllies
    {
        /// <summary>The damage the shot does to <paramref name="target"/> (an ally, by its position and radius) in this tick of <paramref name="deltaTime"/>.</summary>
        float Burn(SpaceBody target, float deltaTime);
    }
}
