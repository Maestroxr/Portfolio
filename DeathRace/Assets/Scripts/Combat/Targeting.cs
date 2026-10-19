using System.Collections.Generic;
using UnityEngine;

namespace Portfolio.DeathRace
{
    /// <summary>Which car a weapon goes for: the rockets' homing, and the computer drivers deciding when to shoot.</summary>
    public static class Targeting
    {
        /// <summary>
        /// The nearest running car (not <paramref name="exclude"/>) within <paramref name="range"/> metres of
        /// <paramref name="origin"/> and inside the cone of <paramref name="coneDegrees"/> (whole width) around
        /// <paramref name="forward"/>; null when there is none. A <paramref name="homing"/> seeker (a rocket's) never
        /// picks a car with a radar jammer (<see cref="CarStats.Jammer"/>): it does not see it.
        /// </summary>
        public static Car NearestInCone(IReadOnlyList<Car> cars, Car exclude, Vector3 origin, Vector3 forward, float coneDegrees, float range,
            bool homing = false)
        {
            if (cars == null)
            {
                return null;
            }
            Vector2 from = new Vector2(origin.x, origin.z);
            Vector2 ahead = new Vector2(forward.x, forward.z);
            Car best = null;
            float bestDistance = range * range;
            foreach (Car car in cars)
            {
                if (car == null || car == exclude || car.IsWrecked || (homing && !Lockable(car)))
                {
                    continue;
                }
                Vector2 to = car.Position2D - from;
                float distance = to.sqrMagnitude;
                if (distance > bestDistance || !CombatRules.InCone(ahead, to, coneDegrees))
                {
                    continue;
                }
                best = car;
                bestDistance = distance;
            }
            return best;
        }

        /// <summary>Whether a homing weapon can lock on to <paramref name="car"/>: not when it carries a radar jammer.</summary>
        public static bool Lockable(Car car)
        {
            return car != null && !car.Stats.Jammer;
        }

        /// <summary>The cars of <paramref name="race"/>, or of the scene when there is no race (a test, a check).</summary>
        public static IReadOnlyList<Car> CarsOf(Race race)
        {
            if (race != null)
            {
                return race.Cars;
            }
            return Object.FindObjectsByType<Car>(FindObjectsInactive.Exclude);
        }

        /// <summary>Where to aim at <paramref name="target"/> from <paramref name="origin"/> with a shot of <paramref name="speed"/>: a little ahead of it.</summary>
        public static Vector3 Lead(Vector3 origin, Car target, float speed)
        {
            Vector3 position = target.transform.position;
            Rigidbody body = target.Body != null ? target.Body.Rigidbody : null;
            if (body == null || speed <= 0f)
            {
                return position;
            }
            float time = Mathf.Min(1f, Vector3.Distance(origin, position) / speed);
            Vector3 velocity = body.linearVelocity;
            velocity.y = 0f;
            return position + velocity * time;
        }
    }
}
