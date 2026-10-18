using UnityEngine;

namespace Portfolio.Asteroids
{
    /// <summary>
    /// The aircraft a strike unit or boss part launches with the <see cref="AttackPattern.Launch"/> pattern (a hive
    /// spawner's stingers, a carrier's drones): <see cref="perLaunch"/> of <see cref="unit"/> at a time, each flying one of
    /// <see cref="paths"/> from where the launcher stands, until it has launched <see cref="launches"/> times in this life.
    /// Only the simulator launches; the aircraft travel to the other pilots like any others and count as hostiles that
    /// came on screen. They drop nothing but the credit orbs every aircraft may drop.
    /// </summary>
    public class UnitLauncher : MonoBehaviour
    {
        [Tooltip("The air unit launched.")]
        [SerializeField] internal StrikeUnit unit = StrikeUnit.Stinger;
        [Tooltip("Aircraft per launch.")]
        [SerializeField] internal int perLaunch = 2;
        [Tooltip("Launches in one life; 0 for no limit.")]
        [SerializeField] internal int launches = StrikeCampaignRules.SpawnerLaunches;
        [Tooltip("The paths the aircraft fly from the launcher, in turn (every other one mirrored).")]
        [SerializeField] internal FlightPath[] paths = { FlightPath.Swoop, FlightPath.DiveLeft };
        [Tooltip("Multiplier of the unit's speed.")]
        [SerializeField] internal float speed = 1f;
        [Tooltip("Where the aircraft appear, relative to the launcher (they start there and fly their path's shape on).")]
        [SerializeField] internal Vector2 offset;

        private Enemy owner;
        private float ownerAge = float.MaxValue;
        private int launched;
        private int flights;

        /// <summary>Launches made in the owner's current life.</summary>
        public int Launched => launched;


        /// <summary>
        /// One launch for <paramref name="from"/> (the unit or part this sits on): nothing once it is spent. A new life of the
        /// owner (its age went back) starts the count again.
        /// </summary>
        public void Launch(Enemy from)
        {
            if (from == null || from.Field == null || from.Field.Spawner == null || from.IsPuppet)
            {
                return;
            }
            if (from != owner || from.Age < ownerAge)
            {
                owner = from;
                launched = 0;
                flights = 0;
            }
            ownerAge = from.Age;
            if (launches > 0 && launched >= launches)
            {
                return;
            }
            launched++;
            SpawnService spawner = from.Field.Spawner;
            Vector2 start = from.Position + offset;
            int count = Mathf.Max(1, perLaunch);
            for (int i = 0; i < count; i++)
            {
                FlightPath path = paths != null && paths.Length > 0 ? paths[flights % paths.Length] : FlightPath.Swoop;
                var flight = new StrikeEvent
                {
                    unit = unit,
                    count = 1,
                    path = path,
                    mirror = flights % 2 == 1,
                    speed = speed,
                    flight = FlightPaths.DefaultType(path),
                    x = start.x
                };
                flights++;
                spawner.SpawnAircraft(unit, flight, 0, start, i * StrikeCampaignRules.LaunchGap);
            }
            from.Field.Effects?.WarpIn(start, 1.6f, new Color(1f, 0.5f, 0.9f));
            from.Field.Sounds?.BossRoar(0.3f);
        }
    }
}
