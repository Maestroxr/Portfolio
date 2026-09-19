using UnityEngine;

namespace Portfolio.EndlessRunner
{
    public enum ObstacleKind
    {
        Hurdle,
        Barrier,
        Block,
        Platform,
        Ramp,
        Cart,
        Bridge
    }


    /// <summary>
    /// A solid piece on the track. Its colliders block the runner: running into the front of a hazard is a crash,
    /// running into its side while switching lanes bounces the runner back, and its top can be stood on. A crash
    /// knocks the obstacle away cartoon style. Carts start rolling toward the runner when it gets close.
    /// </summary>
    public class Obstacle : TrackPiece
    {
        private const float KnockGravity = 30f;
        private const float KnockDuration = 1.4f;

        [SerializeField] internal ObstacleKind kind;
        [Tooltip("Carts: speed toward the runner once it is within Wake Distance, in meters per second.")]
        [SerializeField] internal float moveSpeed;
        [SerializeField] internal float wakeDistance = 55f;
        [Tooltip("Wheels spun while the obstacle rolls.")]
        [SerializeField] internal Transform[] wheels = new Transform[0];
        [SerializeField] internal float wheelRadius = 0.3f;

        private Collider[] colliders;
        private Vector3 knockVelocity;
        private Vector3 knockSpin;
        private float knockTimer;

        public ObstacleKind Kind => kind;

        public bool IsKnocked { get; private set; }

        public bool IsMoving { get; private set; }

        /// <summary>Whether running into the front of the obstacle is a crash. Ramps and bridges are walked on.</summary>
        public bool IsHazard => kind != ObstacleKind.Ramp && kind != ObstacleKind.Bridge;

        private void Awake()
        {
            colliders = GetComponentsInChildren<Collider>(true);
        }

        public override void OnSpawned()
        {
            base.OnSpawned();
            IsKnocked = false;
            IsMoving = false;
            knockTimer = 0f;
            SetColliders(true);
        }

        /// <summary>World bounds of the obstacle's colliders.</summary>
        public Bounds GetBounds()
        {
            Bounds bounds = new Bounds(transform.position, Vector3.zero);
            bool first = true;
            foreach (Collider collider in colliders)
            {
                if (collider == null || !collider.enabled)
                {
                    continue;
                }
                if (first)
                {
                    bounds = collider.bounds;
                    first = false;
                }
                else
                {
                    bounds.Encapsulate(collider.bounds);
                }
            }
            return bounds;
        }

        /// <summary>Sends the obstacle flying off the track; it stops colliding right away.</summary>
        public void Knock(Vector3 impactPoint, float runnerSpeed)
        {
            if (IsKnocked)
            {
                return;
            }
            IsKnocked = true;
            IsMoving = false;
            SetColliders(false);
            float side = impactPoint.x >= transform.position.x ? -1f : 1f;
            if (Mathf.Abs(transform.position.x) > 1f)
            {
                side = Mathf.Sign(transform.position.x);
            }
            knockVelocity = new Vector3(side * Random.Range(5f, 8f), Random.Range(7f, 10f), runnerSpeed * 0.6f + 4f);
            knockSpin = new Vector3(Random.Range(-360f, -180f), Random.Range(-180f, 180f), side * Random.Range(-420f, -200f));
            knockTimer = 0f;
        }

        internal void Tick(float deltaTime, float runnerZ)
        {
            if (IsKnocked)
            {
                knockTimer += deltaTime;
                knockVelocity += Vector3.down * (KnockGravity * deltaTime);
                transform.position += knockVelocity * deltaTime;
                transform.rotation = Quaternion.Euler(knockSpin * deltaTime) * transform.rotation;
                if (knockTimer > KnockDuration)
                {
                    Expired = true;
                }
                return;
            }
            if (moveSpeed <= 0f)
            {
                return;
            }
            if (!IsMoving && transform.position.z - runnerZ < wakeDistance)
            {
                IsMoving = true;
            }
            if (IsMoving)
            {
                float step = moveSpeed * deltaTime;
                transform.position += Vector3.back * step;
                float angle = -step / Mathf.Max(0.05f, wheelRadius) * Mathf.Rad2Deg;
                foreach (Transform wheel in wheels)
                {
                    if (wheel != null)
                    {
                        wheel.Rotate(angle, 0f, 0f, Space.Self);
                    }
                }
            }
        }

        private void SetColliders(bool enabled)
        {
            if (colliders == null)
            {
                colliders = GetComponentsInChildren<Collider>(true);
            }
            foreach (Collider collider in colliders)
            {
                if (collider != null)
                {
                    collider.enabled = enabled;
                }
            }
        }
    }
}
