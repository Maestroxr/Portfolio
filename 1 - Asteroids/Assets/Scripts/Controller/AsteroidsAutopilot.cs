using UnityEngine;

namespace Portfolio.Asteroids
{
    /// <summary>
    /// Flies the ship by itself: leads and shoots the closest target, steers out of the way of whatever is about to
    /// hit it (dashing when it is close), keeps away from mines and black holes, grabs pickups when it is safe and
    /// sets off a nova bomb when swamped. Add it to the ship in play mode to watch or test a mission end to end.
    /// </summary>
    public class AsteroidsAutopilot : MonoBehaviour, IShipInput
    {
        [SerializeField] internal float lookAhead = 1.1f;
        [SerializeField] internal float safetyMargin = 0.9f;
        [SerializeField] internal float preferredRange = 6.5f;

        private AsteroidsPlayer ship;

        public float Turn { get; private set; }
        public float Thrust { get; private set; }
        public bool Brake { get; private set; }
        public bool Fire { get; private set; }
        public bool DashPressed { get; private set; }
        public bool BombPressed { get; private set; }


        private void OnEnable()
        {
            ship = GetComponent<AsteroidsPlayer>();
            if (ship != null)
            {
                ship.Input = this;
            }
        }


        private void OnDisable()
        {
            if (ship != null && ReferenceEquals(ship.Input, this))
            {
                ship.Input = null;
            }
        }


        public void Read(AsteroidsPlayer player, float deltaTime)
        {
            Turn = 0f;
            Thrust = 0f;
            Brake = false;
            Fire = false;
            DashPressed = false;
            BombPressed = false;
            SpaceField field = player.Field;
            if (field == null || field.Playground == null)
            {
                return;
            }
            Playground playground = field.Playground;
            Vector2 position = player.Position;
            Vector2 forward = player.Forward;
            Vector2 velocity = player.Velocity;

            // Threats: the soonest thing on a collision course within the look-ahead.
            float soonest = float.MaxValue;
            Vector2 escape = Vector2.zero;
            int crowd = 0;
            void Consider(Vector2 at, Vector2 moving, float radius)
            {
                Vector2 offset = playground.Delta(position, at);
                if (offset.sqrMagnitude < 25f)
                {
                    crowd++;
                }
                Vector2 relative = moving - velocity;
                float speed2 = relative.sqrMagnitude;
                float time = speed2 > 0.0001f ? Mathf.Clamp(-Vector2.Dot(offset, relative) / speed2, 0f, lookAhead) : 0f;
                Vector2 closest = offset + relative * time;
                float clearance = radius + player.Radius + safetyMargin;
                if (closest.sqrMagnitude > clearance * clearance || time >= soonest)
                {
                    return;
                }
                soonest = time;
                Vector2 away = closest.sqrMagnitude > 0.0001f ? -closest.normalized : new Vector2(-relative.y, relative.x).normalized;
                escape = away;
            }

            var targets = field.Targets;
            for (int i = 0; i < targets.Count; i++)
            {
                Shootable target = targets[i];
                if (!target.IsAlive || target.ContactDamage <= 0f)
                {
                    continue;
                }
                float radius = target.Radius;
                if (target is Mine)
                {
                    radius += 2.5f;
                }
                Consider(target.Position, target.Velocity, radius);
            }
            var shots = field.EnemyShots;
            for (int i = 0; i < shots.Count; i++)
            {
                if (shots[i].InPlay)
                {
                    Consider(shots[i].Position, shots[i].Velocity, shots[i].Radius);
                }
            }
            var comets = field.Comets;
            for (int i = 0; i < comets.Count; i++)
            {
                if (comets[i].InPlay)
                {
                    Consider(comets[i].Position, comets[i].Velocity, comets[i].Radius + 1f);
                }
            }
            var wells = field.Wells;
            for (int i = 0; i < wells.Count; i++)
            {
                Vector2 offset = playground.Delta(position, wells[i].Position);
                if (wells[i].InPlay && offset.sqrMagnitude < 36f)
                {
                    soonest = Mathf.Min(soonest, 0.5f);
                    escape = -offset.normalized;
                }
            }

            if (soonest < lookAhead)
            {
                SteerToward(forward, escape, 1f);
                float facing = Vector2.Dot(forward, escape);
                Thrust = facing > 0.2f ? 1f : 0f;
                Brake = facing < -0.5f;
                DashPressed = soonest < 0.35f && facing > 0.4f;
                BombPressed = crowd >= 7 && player.Bombs > 0;
                Fire = true;
                return;
            }

            // Safe: grab a nearby pickup, otherwise hunt.
            Reward pickup = null;
            float pickupDistance = 7f * 7f;
            var rewards = field.Rewards;
            for (int i = 0; i < rewards.Count; i++)
            {
                if (!rewards[i].InPlay)
                {
                    continue;
                }
                float distance = playground.Delta(position, rewards[i].Position).sqrMagnitude;
                if (distance < pickupDistance)
                {
                    pickupDistance = distance;
                    pickup = rewards[i];
                }
            }
            Shootable prey = field.NearestTarget(position, 60f);
            if (pickup != null && (prey == null || playground.Delta(position, prey.Position).sqrMagnitude > 16f))
            {
                Vector2 toPickup = playground.Delta(position, pickup.Position);
                SteerToward(forward, toPickup.normalized, 1f);
                Thrust = Vector2.Dot(forward, toPickup.normalized) > 0.8f ? 0.8f : 0f;
                Brake = velocity.magnitude > 6f;
                Fire = prey != null;
                return;
            }
            if (prey == null)
            {
                Brake = velocity.magnitude > 1f;
                return;
            }
            Vector2 offsetToPrey = playground.Delta(position, prey.Position);
            float shotSpeed = WeaponRules.Speed(player.Weapons.Type);
            float lead = offsetToPrey.magnitude / Mathf.Max(5f, shotSpeed);
            Vector2 aim = offsetToPrey + (prey.Velocity - velocity) * lead;
            float angle = SteerToward(forward, aim.normalized, 1f);
            Fire = Mathf.Abs(angle) < 14f && offsetToPrey.magnitude < 20f;
            float distanceToPrey = offsetToPrey.magnitude;
            if (distanceToPrey > preferredRange + 4f && Mathf.Abs(angle) < 30f)
            {
                Thrust = 0.6f;
            }
            else if (distanceToPrey < preferredRange - 2f || velocity.magnitude > 7f)
            {
                Brake = true;
            }
        }


        /// <summary>Turns toward <paramref name="wanted"/>; returns the angle still to turn in degrees.</summary>
        private float SteerToward(Vector2 forward, Vector2 wanted, float urgency)
        {
            if (wanted.sqrMagnitude < 0.0001f)
            {
                return 0f;
            }
            float angle = Vector2.SignedAngle(forward, wanted);
            Turn = Mathf.Clamp(angle / 20f, -1f, 1f) * urgency;
            return angle;
        }
    }
}
