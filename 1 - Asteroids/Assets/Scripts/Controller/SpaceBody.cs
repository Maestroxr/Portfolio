using UnityEngine;

namespace Portfolio.Asteroids
{
    /// <summary>
    /// Anything that moves through the playfield: drifts along its velocity, tumbles its model, wraps around the edges
    /// (or leaves the playfield for good) and goes back to its pool when it is done. The <see cref="SpaceField"/> it is
    /// added to moves it on every frame of a running mission.
    /// </summary>
    public class SpaceBody : MonoBehaviour
    {
        [SerializeField] internal float radius = 0.5f;
        [SerializeField] internal bool wraps = true;
        [Tooltip("The model: it tumbles and flashes while the root keeps the heading.")]
        [SerializeField] internal Transform visual;
        [Tooltip("Largest random tumble in degrees per second, picked when the body spawns.")]
        [SerializeField] internal float randomSpin;
        [SerializeField] internal bool pulledByGravity = true;
        [Tooltip("Seconds before the body removes itself; zero keeps it until something else removes it.")]
        [SerializeField] internal float lifetime;

        public Vector2 Velocity;

        /// <summary>Tumble of the model in degrees per second around each axis.</summary>
        public Vector3 Spin;

        private bool entered;

        public float Radius
        {
            get => radius;
            set => radius = value;
        }

        public bool Wraps => wraps;

        public bool PulledByGravity => pulledByGravity;

        public Transform Visual => visual != null ? visual : transform;

        public float Age { get; private set; }

        public float Lifetime
        {
            get => lifetime;
            set => lifetime = value;
        }

        /// <summary>Whether the body is part of a field: spawned and not removed yet.</summary>
        public bool InPlay { get; internal set; }

        public SpaceField Field { get; internal set; }

        internal IBodyPool Pool { get; set; }

        public Vector2 Position
        {
            get
            {
                Vector3 position = transform.position;
                return new Vector2(position.x, position.y);
            }
            set => transform.position = new Vector3(value.x, value.y, 0f);
        }


        /// <summary>Resets the body for another life. The field calls it when the body is added.</summary>
        public virtual void OnSpawned()
        {
            Age = 0f;
            entered = false;
            if (randomSpin > 0f)
            {
                Spin = Random.onUnitSphere * Random.Range(randomSpin * 0.35f, randomSpin);
            }
        }


        /// <summary>Advances the body by <paramref name="deltaTime"/> seconds of world time.</summary>
        public virtual void Tick(float deltaTime)
        {
            Age += deltaTime;
            Position += Velocity * deltaTime;
            if (visual != null && Spin.sqrMagnitude > 0f)
            {
                visual.localRotation = Quaternion.Euler(Spin * deltaTime) * visual.localRotation;
            }
            if (lifetime > 0f && Age >= lifetime)
            {
                Expire();
                return;
            }
            Playground playground = Field != null ? Field.Playground : null;
            if (playground == null)
            {
                return;
            }
            if (wraps)
            {
                Position = playground.Wrap(Position, radius);
                return;
            }
            entered |= playground.IsInside(Position, -radius);
            if (playground.IsOutside(Position, radius, 1.5f))
            {
                bool leaving = Vector2.Dot(Velocity, Position - (Vector2)playground.Middle) > 0f;
                if (entered || leaving)
                {
                    LeftPlayfield();
                }
            }
        }


        /// <summary>The lifetime ran out.</summary>
        protected virtual void Expire()
        {
            Despawn();
        }


        /// <summary>A body that does not wrap flew off the playfield.</summary>
        protected virtual void LeftPlayfield()
        {
            Despawn();
        }


        /// <summary>Takes the body out of play and returns it to its pool.</summary>
        public void Despawn()
        {
            if (Field != null && InPlay)
            {
                Field.Remove(this);
                return;
            }
            Release();
        }


        internal void Release()
        {
            InPlay = false;
            OnDespawned();
            if (Pool != null)
            {
                Pool.Release(this);
            }
            else if (this != null)
            {
                gameObject.SetActive(false);
            }
        }


        /// <summary>Called when the body leaves play, before it goes back to its pool.</summary>
        protected virtual void OnDespawned()
        {
        }
    }
}
