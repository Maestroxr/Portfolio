using UnityEngine;

namespace Portfolio.DeathRace
{
    /// <summary>
    /// A mortar shell lobbed from a car to a point ahead (FOUNDATION STUB: the weapons work fills it in): it flies for
    /// <see cref="CombatRules.MortarFlightSeconds"/> and bursts where it lands.
    /// </summary>
    public class MortarShell : MonoBehaviour
    {
        public Car Owner { get; private set; }

        /// <summary>Where the shell left from and where it lands (on the ground).</summary>
        public Vector3 From { get; private set; }

        public Vector3 Target { get; private set; }

        /// <summary>Online: every device shows the shell; each judges the blast only on the cars it drives.</summary>
        public bool Authoritative { get; set; } = true;

        /// <summary>Lobs a shell of <paramref name="owner"/> from <paramref name="from"/> to land at <paramref name="target"/>.</summary>
        public static MortarShell Launch(Race race, DeathRaceContent content, Vector3 from, Vector3 target, Car owner, bool authoritative = true)
        {
            var go = new GameObject("Mortar Shell");
            go.transform.SetParent(race != null ? race.Root : null, false);
            go.transform.position = from;
            MortarShell shell = go.AddComponent<MortarShell>();
            shell.Owner = owner;
            shell.From = from;
            shell.Target = new Vector3(target.x, 0f, target.z);
            shell.Authoritative = authoritative;
            Destroy(go, CombatRules.MortarFlightSeconds);
            return shell;
        }
    }
}
