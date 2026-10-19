using System.Collections.Generic;
using UnityEngine;

namespace Portfolio.DeathRace
{
    /// <summary>
    /// A slick of oil poured behind a car (FOUNDATION STUB: the weapons work fills it in). A car that drives through it
    /// loses most of its grip for a moment; it dries up after <see cref="CombatRules.OilLifetime"/>.
    /// </summary>
    public class OilSlick : MonoBehaviour
    {
        private static readonly List<OilSlick> active = new List<OilSlick>();

        /// <summary>The slicks lying on the track now (the computer drivers steer round them).</summary>
        public static IReadOnlyList<OilSlick> Active => active;

        public Car Owner { get; private set; }

        /// <summary>The number of the slick in its race (online: the action that poured it).</summary>
        public int Id { get; private set; }

        public float Age { get; private set; }
        public float Radius => CombatRules.OilRadius;
        public Vector3 Position => transform.position;

        /// <summary>Online: a slick everyone shows; each device judges only the cars it drives.</summary>
        public bool Authoritative { get; set; } = true;

        /// <summary>Pours a slick of <paramref name="owner"/> at <paramref name="position"/> on the ground of <paramref name="race"/>.</summary>
        public static OilSlick Spawn(Race race, DeathRaceContent content, Vector3 position, Quaternion rotation, Car owner, int id = 0)
        {
            var go = new GameObject("Oil Slick");
            go.transform.SetParent(race != null ? race.Root : null, false);
            go.transform.SetPositionAndRotation(new Vector3(position.x, 0.02f, position.z), Quaternion.Euler(0f, rotation.eulerAngles.y, 0f));
            OilSlick slick = go.AddComponent<OilSlick>();
            slick.Owner = owner;
            slick.Id = id;
            active.Add(slick);
            return slick;
        }

        private void Update()
        {
            Age += Time.deltaTime;
            if (Age >= CombatRules.OilLifetime)
            {
                Destroy(gameObject);
            }
        }

        private void OnDestroy()
        {
            active.Remove(this);
        }
    }
}
