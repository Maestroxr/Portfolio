using UnityEngine;

namespace Portfolio.Asteroids
{
    /// <summary>
    /// Base of the enemies of the field campaigns (the sentry, the minelayer, the phase stalker and the corsair). Such an
    /// enemy can be the marked quarry of a hunt: a turning reticle shows it, it is worth twice its points and the hunt
    /// counts it. It may also go for the freighter or station of an escort or a defence instead of the ships
    /// (<see cref="allyPreference"/>). In a shared mission the mark travels as the body's variant
    /// (<see cref="MarkedVariant"/>), so the other pilots see the same quarry.
    /// </summary>
    public abstract class CampaignEnemy : Enemy
    {
        /// <summary>The variant of a marked enemy on the wire (0 is an unmarked one).</summary>
        public const int MarkedVariant = 1;

        [Tooltip("The kind of hazard the enemy is, for the wave director and the hunt.")]
        [SerializeField] internal HazardKind hazard;
        [Tooltip("Shows that the enemy is the marked quarry of a hunt; hidden otherwise.")]
        [SerializeField] internal Transform reticle;
        [Tooltip("How far the enemy looks for a freighter or station to attack instead of a ship (0: it never does).")]
        [SerializeField] internal float allyRange;
        [Tooltip("The enemy's own sound (a sentry charging, a minelayer dropping a mine, a stalker blinking, a corsair's engine).")]
        [SerializeField] internal AudioClip cue;

        /// <summary>The kind of hazard the enemy is.</summary>
        public HazardKind Hazard => hazard;

        /// <summary>Whether the enemy is the marked quarry of a hunt.</summary>
        public bool Marked { get; private set; }

        /// <summary>Whether the last <see cref="Prey"/> was a freighter or station rather than a ship.</summary>
        protected bool HuntsAlly { get; private set; }

        /// <summary>Marked enemies are worth twice their points.</summary>
        public override int Score => Marked ? base.Score * 2 : base.Score;


        public override void OnSpawned()
        {
            base.OnSpawned();
            // A puppet arrives with the mark of the enemy it stands for (its variant); the simulator marks its own after.
            SetMarked(IsPuppet && NetVariant == MarkedVariant);
            transform.localScale = Vector3.one;
        }


        /// <summary>Marks the enemy as the quarry of a hunt (or clears the mark).</summary>
        public void SetMarked(bool marked)
        {
            Marked = marked;
            if (reticle != null)
            {
                reticle.gameObject.SetActive(marked);
            }
        }


        public override void Tick(float deltaTime)
        {
            base.Tick(deltaTime);
            if (Marked && reticle != null && InPlay)
            {
                // The reticle turns in the plane of the playfield, whatever the enemy's heading.
                reticle.rotation = Quaternion.Euler(0f, 0f, -Age * 90f);
            }
        }


        /// <summary>
        /// What the enemy goes for: a freighter or station within <see cref="allyRange"/> when it has one in its sights,
        /// else the nearest ship. False when there is nothing to go for.
        /// </summary>
        protected bool Prey(out Vector2 position, out Vector2 velocity)
        {
            Ally ally = allyRange > 0f ? Ally.Nearest(Field, Position, allyRange) : null;
            HuntsAlly = ally != null;
            if (ally != null)
            {
                position = ally.Position;
                velocity = ally.Velocity;
                return true;
            }
            AsteroidsPlayer ship = Target;
            position = ship != null ? ship.Position : Position;
            velocity = ship != null ? ship.Velocity : Vector2.zero;
            return ship != null;
        }


        /// <summary>The direction to <paramref name="position"/> across the wrapping edges, with a lead for its motion.</summary>
        protected Vector2 Lead(Vector2 position, Vector2 velocity, float shotSpeed)
        {
            Vector2 offset = Field != null && Field.Playground != null ? Field.Playground.Delta(Position, position) : position - Position;
            float time = offset.magnitude / Mathf.Max(1f, shotSpeed);
            Vector2 aim = offset + velocity * time;
            return aim.sqrMagnitude > 0.0001f ? aim.normalized : Vector2.up;
        }


        /// <summary>The offset to <paramref name="position"/> across the wrapping edges.</summary>
        protected Vector2 OffsetTo(Vector2 position)
        {
            return Field != null && Field.Playground != null ? Field.Playground.Delta(Position, position) : position - Position;
        }


        protected void PlayCue(float volume, float pitch = 1f)
        {
            Field?.Sounds?.Play(cue, volume, pitch, 0.06f, 0.08f);
        }


        protected override void OnDespawned()
        {
            base.OnDespawned();
            SetMarked(false);
            Invulnerable = false;
            transform.localScale = Vector3.one;
        }
    }
}
