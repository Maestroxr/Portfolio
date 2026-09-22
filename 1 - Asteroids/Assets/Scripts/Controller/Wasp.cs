using UnityEngine;

namespace Portfolio.Asteroids
{
    /// <summary>
    /// An alien wasp: it hunts the ship across the wrapping edges, buzzing and weaving, and dives into it. It dies in
    /// the collision, so the trick is to shoot it before it gets there.
    /// </summary>
    public class Wasp : Enemy
    {
        [SerializeField] internal float speed = 3.8f;
        [SerializeField] internal float acceleration = 5.5f;
        [SerializeField] internal float wobble = 1.4f;
        [SerializeField] internal Transform[] wings = new Transform[0];
        [SerializeField] internal float flapSpeed = 26f;
        [SerializeField] internal float flapAngle = 32f;

        private Quaternion[] wingRest;
        private float wobblePhase;


        private void Awake()
        {
            wingRest = new Quaternion[wings.Length];
            for (int i = 0; i < wings.Length; i++)
            {
                wingRest[i] = wings[i] != null ? wings[i].localRotation : Quaternion.identity;
            }
        }


        public override void OnSpawned()
        {
            base.OnSpawned();
            wobblePhase = Random.Range(0f, 10f);
        }


        public override void Tick(float deltaTime)
        {
            if (!IsPuppet)
            {
                Hunt(deltaTime);
            }
            base.Tick(deltaTime);
            if (!InPlay)
            {
                return;
            }
            Face(Velocity, deltaTime, 360f);
            float flap = Mathf.Sin(Age * flapSpeed) * flapAngle;
            for (int i = 0; i < wings.Length; i++)
            {
                if (wings[i] != null)
                {
                    // Flap around the body's long axis (the parent's z), mirrored between the left and right wings.
                    float sign = i % 2 == 0 ? 1f : -1f;
                    wings[i].localRotation = Quaternion.AngleAxis(flap * sign, Vector3.forward) * wingRest[i];
                }
            }
        }


        private void Hunt(float deltaTime)
        {
            AsteroidsPlayer target = Target;
            Vector2 desired;
            if (target != null && Field != null && Field.Playground != null)
            {
                Vector2 toShip = Field.Playground.Delta(Position, target.Position);
                Vector2 side = new Vector2(-toShip.y, toShip.x).normalized;
                desired = toShip.normalized * speed + side * Mathf.Sin(Age * 2.3f + wobblePhase) * wobble;
            }
            else
            {
                desired = Velocity.sqrMagnitude > 0.01f ? Velocity.normalized * speed * 0.6f : Random.insideUnitCircle * speed;
            }
            Velocity = Vector2.MoveTowards(Velocity, desired, acceleration * deltaTime);
        }


        public override void OnRammed(AsteroidsPlayer player, Vector2 direction, bool dashing)
        {
            TakeHit(new DamageInfo(maxHealth, direction, Position, DamageSource.Collision, true) { Seat = SeatOf(player) });
        }
    }
}
