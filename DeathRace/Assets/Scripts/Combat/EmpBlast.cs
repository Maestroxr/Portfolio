using UnityEngine;

namespace Portfolio.DeathRace
{
    /// <summary>
    /// An electric pulse round a car (FOUNDATION STUB: the weapons work fills it in): every other car within
    /// <see cref="CombatRules.EmpRadius"/> is jammed for <see cref="CombatRules.EmpJamSeconds"/>.
    /// </summary>
    public class EmpBlast : MonoBehaviour
    {
        public Car Owner { get; private set; }
        public Vector3 Position => transform.position;

        /// <summary>Online: every device shows the pulse; each jams only the cars it drives.</summary>
        public bool Authoritative { get; set; } = true;

        /// <summary>Sets off a pulse of <paramref name="owner"/> at <paramref name="position"/>.</summary>
        public static EmpBlast Fire(Race race, DeathRaceContent content, Vector3 position, Car owner, bool authoritative = true)
        {
            var go = new GameObject("EMP Blast");
            go.transform.SetParent(race != null ? race.Root : null, false);
            go.transform.position = new Vector3(position.x, 0f, position.z);
            EmpBlast blast = go.AddComponent<EmpBlast>();
            blast.Owner = owner;
            blast.Authoritative = authoritative;
            Destroy(go, 1f);
            return blast;
        }
    }
}
