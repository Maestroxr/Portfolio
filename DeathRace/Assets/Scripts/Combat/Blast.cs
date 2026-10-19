using System.Collections.Generic;
using UnityEngine;

namespace Portfolio.DeathRace
{
    /// <summary>
    /// A blast on the ground (a mine, a rocket, a car blowing up): every car within its radius takes damage that falls
    /// off with the distance to the nearest point of its body, and is thrown sideways away from the centre with a little
    /// spin. Who set it off is the attacker of every hit (a car hurt by its own blast is nobody's kill). A car wrecked by
    /// a blast blows up in turn, so the cars caught are gathered first and hurt after.
    /// </summary>
    public static class Blast
    {
        private static readonly Collider[] Found = new Collider[24];

        private struct Victim
        {
            public Car Car;
            public Vector3 Nearest;
            public float Distance;
        }

        /// <summary>
        /// Sets off a blast at <paramref name="centre"/>; <paramref name="pushSpeed"/> is the speed (m/s) it gives a car at
        /// its centre. <paramref name="spare"/> (the car blowing up, say) is left out. Returns how many cars it hurt.
        /// </summary>
        public static int Explode(Race race, Vector3 centre, float radius, float damage, float pushSpeed, DamageSource source, Car attacker, Car spare = null)
        {
            // Cars stand on the ground: the blast is measured at the height of their bodies.
            Vector3 middle = new Vector3(centre.x, 0.7f, centre.z);
            int count = Physics.OverlapSphereNonAlloc(middle, radius, Found, DeathRaceLayers.CarMask, QueryTriggerInteraction.Ignore);
            var victims = new List<Victim>(count);
            for (int i = 0; i < count; i++)
            {
                Collider collider = Found[i];
                Car car = collider != null ? collider.GetComponentInParent<Car>() : null;
                if (car == null || car == spare || car.IsWrecked || (race != null && car.Race != null && car.Race != race) || victims.Exists(v => v.Car == car))
                {
                    continue;
                }
                Vector3 nearest = collider.ClosestPoint(middle);
                victims.Add(new Victim { Car = car, Nearest = nearest, Distance = new Vector2(nearest.x - centre.x, nearest.z - centre.z).magnitude });
            }
            int hurt = 0;
            foreach (Victim victim in victims)
            {
                Car car = victim.Car;
                float amount = CombatRules.BlastDamage(damage, radius, victim.Distance);
                if (car == null || car.IsWrecked || amount <= 0f)
                {
                    continue;
                }
                Vector3 away = new Vector3(car.transform.position.x - centre.x, 0f, car.transform.position.z - centre.z);
                away = away.sqrMagnitude > 0.01f ? away.normalized : car.transform.forward;
                if (car.Body != null && pushSpeed > 0f)
                {
                    float mass = car.Body.Rigidbody != null ? car.Body.Rigidbody.mass : car.Stats.Mass;
                    float push = CombatRules.BlastPush(radius, victim.Distance) * pushSpeed;
                    car.Body.Push(away * push * mass);
                    float side = Vector3.Cross(car.transform.forward, away).y >= 0f ? 1f : -1f;
                    car.Body.Spin(side * push * 18f);
                }
                if (car.Health != null)
                {
                    car.Health.TakeDamage(new DamageInfo(amount, source, attacker, victim.Nearest, away));
                }
                hurt++;
            }
            return hurt;
        }
    }
}
