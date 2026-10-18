using UnityEngine;

namespace Portfolio.Asteroids
{
    /// <summary>
    /// A shell or canister that bursts into fragments: the Flak Burster's shell (asteroid field), and the strike flak
    /// cannon's shell, the cluster bombs' canister and the napalm canister. It bursts when it hits something, when its time
    /// runs out, or (with a fuse) as soon as a target of its <see cref="fuseLayers"/> comes within <see cref="fuseRadius"/>.
    /// The fragments come from the spawner's pools (<see cref="SpawnService.FireFragment"/>): the field's flak shrapnel or
    /// a strike shot kind (shards, bomblets, a patch of fire).
    ///
    /// Each client bursts the shells it shows, so the fragments are never sent to the other pilots: a ghost shell bursts
    /// into ghost fragments that look the same and hurt nothing.
    /// </summary>
    public class BurstShot : Shot
    {
        /// <summary>The sound a burst makes.</summary>
        public enum BurstSound : byte
        {
            None = 0,
            Flak = 1,
            Cluster = 2,
            Napalm = 3
        }

        /// <summary>The <see cref="fragmentKind"/> of the asteroid field's flak shrapnel (not a strike shot kind).</summary>
        public const int FieldShrapnel = -1;

        [Header("Burst")]
        [Tooltip("How many fragments it bursts into.")]
        [SerializeField] internal int fragments = 6;
        [Tooltip("The StrikeShotKind of the fragments; -1 for the asteroid field's flak shrapnel.")]
        [SerializeField] internal int fragmentKind = FieldShrapnel;
        [Tooltip("Damage of one fragment (bomblets and fire take theirs from their own prefab).")]
        [SerializeField] internal float fragmentDamage = 1f;
        [SerializeField] internal float fragmentSpeed = 14f;
        [Tooltip("Seconds the fragments fly; each a little more or less.")]
        [SerializeField] internal float fragmentLifetime = 0.3f;
        [Tooltip("Degrees the fragments spread over around the shell's course; 360 is a ring.")]
        [SerializeField] internal float fragmentCone = 360f;
        [Tooltip("How much of the shell's velocity the fragments keep.")]
        [SerializeField] internal float fragmentInherit = 0.3f;
        [Tooltip("The fragments start this far from the burst point (m): a cluster canister scatters its bomblets.")]
        [SerializeField] internal float scatter;
        [Tooltip("Bursts as soon as a target of the fuse layers is this close (m); zero bursts only on a hit or at the end.")]
        [SerializeField] internal float fuseRadius;
        [SerializeField] internal Altitude fuseLayers = Altitude.Both;
        [Tooltip("The flash of the burst (none for zero).")]
        [SerializeField] internal float flashSize = 0.6f;
        [SerializeField] internal Color burstTint = new Color(1f, 0.8f, 0.35f);
        [SerializeField] internal BurstSound sound = BurstSound.Flak;

        private bool burst;

        public int Fragments
        {
            get => fragments;
            set => fragments = Mathf.Max(0, value);
        }

        public float FragmentDamage
        {
            get => fragmentDamage;
            set => fragmentDamage = Mathf.Max(0f, value);
        }

        /// <summary>The StrikeShotKind of the fragments, or <see cref="FieldShrapnel"/>.</summary>
        public int FragmentKind => fragmentKind;

        /// <summary>Whether the shell has burst (it goes right after).</summary>
        public bool HasBurst => burst;


        /// <summary>
        /// The direction of fragment <paramref name="index"/> of <paramref name="count"/> spread over <paramref name="cone"/>
        /// degrees around <paramref name="heading"/> (a ring for 360), turned by up to <paramref name="jitter"/> degrees.
        /// </summary>
        public static Vector2 FragmentDirection(Vector2 heading, int index, int count, float cone, float jitter)
        {
            float start = Mathf.Atan2(heading.y, heading.x) * Mathf.Rad2Deg;
            float angle;
            if (cone >= 359f)
            {
                angle = start + 360f * index / Mathf.Max(1, count);
            }
            else
            {
                float t = count <= 1 ? 0.5f : index / (count - 1f);
                angle = start + (t - 0.5f) * cone;
            }
            angle = (angle + jitter) * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        }


        public override void OnSpawned()
        {
            base.OnSpawned();
            burst = false;
        }


        public override void Tick(float deltaTime)
        {
            base.Tick(deltaTime);
            if (!InPlay || fuseRadius <= 0f || Field == null)
            {
                return;
            }
            Altitude layers = fuseLayers;
            Shootable near = Field.NearestTarget(Position, fuseRadius, target => (target.Altitude & layers) != 0);
            if (near == null)
            {
                return;
            }
            // Bursting next to a target is the shell finding its mark.
            Field.NoteLanded(this);
            Burst();
            Despawn();
        }


        public override void Hit(Shootable target)
        {
            if (InPlay)
            {
                Burst();
            }
            base.Hit(target);
        }


        protected override void Expire()
        {
            Burst();
            base.Expire();
        }


        /// <summary>Releases the fragments (once) where the shell is, with a flash and the burst's sound.</summary>
        private void Burst()
        {
            if (burst || Field == null)
            {
                return;
            }
            burst = true;
            SpawnService spawner = Field.Spawner;
            Vector2 heading = Velocity.sqrMagnitude > 0.01f ? Velocity.normalized : Vector2.up;
            Vector2 carried = Velocity * fragmentInherit;
            float offset = Random.Range(0f, 360f / Mathf.Max(1, fragments));
            for (int i = 0; spawner != null && i < fragments; i++)
            {
                float jitter = fragmentCone >= 359f ? offset : Random.Range(-6f, 6f);
                Vector2 direction = FragmentDirection(heading, i, fragments, fragmentCone, jitter);
                Vector2 at = Position + direction * scatter * Random.Range(0.55f, 1f);
                Vector2 velocity = carried + direction * fragmentSpeed * Random.Range(0.85f, 1.15f);
                spawner.FireFragment(this, at, velocity, fragmentDamage, fragmentLifetime * Random.Range(0.8f, 1.2f));
            }
            if (flashSize > 0f)
            {
                Field.Effects?.FlakBurst(Position, flashSize, burstTint);
            }
            Field.Sounds?.Burst(sound);
        }
    }
}
