using UnityEngine;

namespace Portfolio.Asteroids
{
    /// <summary>
    /// A corsair: a raider fighter that hunts across the wrapping edges. It closes in on its prey, then circles it at
    /// gun range and fires pairs of aimed bolts. Freighters and stations come first: with one in range it leaves the
    /// ships alone, so in an escort or a defence the corsairs are the ones to shoot down.
    /// </summary>
    public class Corsair : CampaignEnemy
    {
        [SerializeField] internal float speed = 5.4f;
        [SerializeField] internal float acceleration = 7f;
        [Tooltip("The distance at which it circles its prey.")]
        [SerializeField] internal float orbit = 6f;
        [Tooltip("Meters between the two guns.")]
        [SerializeField] internal float gunSpacing = 0.5f;
        [Tooltip("The engine flames, which stretch with the thrust.")]
        [SerializeField] internal Transform[] flames = new Transform[0];

        private float side = 1f;
        private float swap;


        public override void OnSpawned()
        {
            base.OnSpawned();
            side = Random.value < 0.5f ? -1f : 1f;
            swap = Random.Range(3f, 6f);
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
            Face(Velocity, deltaTime, 300f);
            float thrust = Mathf.Clamp01(Velocity.magnitude / Mathf.Max(0.1f, speed));
            foreach (Transform flame in flames)
            {
                if (flame != null)
                {
                    flame.localScale = new Vector3(flame.localScale.x, 0.4f + thrust * (0.8f + 0.2f * Mathf.Sin(Age * 40f)), 1f);
                }
            }
            if (!IsPuppet && ReadyToFire(deltaTime) && Prey(out Vector2 at, out Vector2 moving) && OffsetTo(at).sqrMagnitude < 14f * 14f)
            {
                Vector2 aim = Lead(at, moving, shotSpeed);
                Vector2 across = new Vector2(-aim.y, aim.x) * gunSpacing * 0.5f;
                // At a freighter or station it fires one gun at a time: the pilots get the time to come to its aid.
                int first = HuntsAlly ? (Random.value < 0.5f ? -1 : 1) : -1;
                int last = HuntsAlly ? first : 1;
                for (int i = first; i <= last; i += 2)
                {
                    Vector2 direction = Quaternion.Euler(0f, 0f, Random.Range(-aimError, aimError)) * aim;
                    Field.Spawner?.FireEnemyShot(shotKind, Position + across * i + direction * (radius + 0.2f), direction * shotSpeed);
                }
                Field.Sounds?.EnemyShot();
                if (HuntsAlly)
                {
                    // And it takes its time between bolts at a freighter or station.
                    fireTimer += fireInterval * 0.7f;
                }
            }
        }


        private void Hunt(float deltaTime)
        {
            swap -= deltaTime;
            if (swap <= 0f)
            {
                swap = Random.Range(3f, 6f);
                side = -side;
            }
            Vector2 desired;
            if (Prey(out Vector2 at, out _))
            {
                Vector2 offset = OffsetTo(at);
                float distance = offset.magnitude;
                Vector2 toward = distance > 0.001f ? offset / distance : Vector2.up;
                Vector2 around = new Vector2(-toward.y, toward.x) * side;
                // Close in from afar, circle at gun range, back off when too close.
                float approach = Mathf.Clamp((distance - orbit) / 3f, -1f, 1f);
                desired = (toward * approach + around * (1f - Mathf.Abs(approach) * 0.5f)).normalized * speed;
            }
            else
            {
                desired = Velocity.sqrMagnitude > 0.01f ? Velocity.normalized * speed * 0.6f : Random.insideUnitCircle.normalized * speed;
            }
            Velocity = Vector2.MoveTowards(Velocity, desired, acceleration * deltaTime);
        }
    }
}
