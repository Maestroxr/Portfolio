using UnityEngine;

namespace Portfolio.Asteroids
{
    /// <summary>
    /// A wing drone of the Wing Drones power-up: it circles the ship and fires at the closest target in range.
    /// </summary>
    public class Drone : MonoBehaviour
    {
        [SerializeField] internal float orbitRadius = 1.6f;
        [SerializeField] internal float orbitSpeed = 130f;
        [SerializeField] internal float fireInterval = 0.38f;
        [SerializeField] internal float range = 11f;
        [SerializeField] internal Transform model;

        private float angle;
        private float cooldown;


        public void SetActive(bool active, int index, int count)
        {
            if (gameObject.activeSelf == active)
            {
                return;
            }
            gameObject.SetActive(active);
            angle = 360f * index / Mathf.Max(1, count);
            cooldown = 0.15f * index;
        }


        public void Tick(AsteroidsPlayer ship, float deltaTime)
        {
            angle += orbitSpeed * deltaTime;
            float radians = angle * Mathf.Deg2Rad;
            Vector2 center = ship.Position;
            Vector2 position = center + new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * orbitRadius;
            transform.position = new Vector3(position.x, position.y, -0.2f);
            if (model != null)
            {
                model.localRotation = Quaternion.Euler(0f, 0f, 360f * deltaTime) * model.localRotation;
            }
            cooldown -= deltaTime;
            SpaceField field = ship.Field;
            if (cooldown > 0f || field == null || field.Spawner == null || ship.IsRemote)
            {
                return;
            }
            if (ship.Mode == MissionMode.Strike)
            {
                FireStrike(ship, field, position);
                return;
            }
            Shootable target = field.NearestTarget(position, range);
            if (target == null)
            {
                cooldown = 0.1f;
                return;
            }
            cooldown = fireInterval;
            Vector2 lead = target.Position + target.Velocity * ((target.Position - position).magnitude / 24f);
            Vector2 direction = (lead - position).normalized;
            transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f);
            field.Spawner.FireDroneShot(position + direction * 0.3f, direction, ship);
            field.Sounds?.DroneShot();
        }


        /// <summary>
        /// Strike (the wingman drones of the pilot's equipment): a strike round at the nearest enemy ahead of the drone, air
        /// or ground, led by where it is going (a ground unit rides the scroll).
        /// </summary>
        private void FireStrike(AsteroidsPlayer ship, SpaceField field, Vector2 position)
        {
            Shootable target = field.NearestTarget(position, StrikeArmory.WingmanRange, candidate => candidate.Position.y > position.y - 1f);
            if (target == null)
            {
                cooldown = 0.1f;
                return;
            }
            cooldown = StrikeArmory.WingmanInterval;
            Vector2 velocity = target is GroundUnit ? target.Velocity + field.ScrollVelocity : target.Velocity;
            Vector2 lead = target.Position + velocity * ((target.Position - position).magnitude / StrikeArmory.WingmanShotSpeed);
            Vector2 direction = (lead - position).sqrMagnitude > 0.0001f ? (lead - position).normalized : Vector2.up;
            transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f);
            Shot shot = field.Spawner.FireStrikeShot(StrikeShotKind.Bullet, position + direction * 0.3f, direction * StrikeArmory.WingmanShotSpeed,
                StrikeArmory.WingmanDamage, Altitude.Both, ship);
            if (shot != null)
            {
                field.Sounds?.DroneShot();
            }
        }
    }
}
