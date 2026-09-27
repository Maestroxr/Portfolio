using System;
using UnityEngine;

namespace Portfolio.Asteroids
{
    /// <summary>A run of terrain tiles of one kind: <see cref="tiles"/> tiles of 20 m, all of <see cref="variant"/>.</summary>
    [Serializable]
    public class TerrainSegment
    {
        public TerrainKind kind;
        [Tooltip("The variant of the kind (modulo the theme's variant count).")]
        public int variant;
        [Tooltip("Tiles of 20 m in this segment.")]
        public int tiles = 1;
    }


    /// <summary>
    /// One spawn of a strike level: <see cref="count"/> members of a unit, placed around a reference point that reaches
    /// the top edge of the screen when the scroll distance is <see cref="at"/>.
    /// </summary>
    [Serializable]
    public class StrikeEvent
    {
        [Tooltip("Level coordinate s of the reference point: it reaches the top edge when the scroll distance is this.")]
        public float at;
        public StrikeUnit unit;
        public int count = 1;
        public Formation formation;
        [Tooltip("Field x of the reference point (-17.78 to 17.78).")]
        public float x;
        [Tooltip("Meters between members (Line and Vee sideways; Column along the path: delay = spacing / speed).")]
        public float spacing = 3f;
        [Tooltip("Air units: the path they fly.")]
        public FlightPath path;
        [Tooltip("Air units: mirror the path left to right.")]
        public bool mirror;
        [Tooltip("Air units: multiplier of the unit's speed.")]
        public float speed = 1f;
        [Tooltip("Air units: what they do at the end of the path.")]
        public FlightType flight;
        [Tooltip("Ground units: degrees, 0 = nose up (+y), 90 = pointing left.")]
        public float heading;
        [Tooltip("Ground units: m/s along the heading relative to the ground (0 = parked); starts on screen.")]
        public float drive;
        [Tooltip("Ground rows: position step between members.")]
        public Vector2 offset = new Vector2(3f, 0f);
        [Tooltip("Dropped when destroyed; Machine Gun = no item.")]
        public StrikeItem bonus = StrikeItem.MachineGun;
        [Tooltip("Money pickup dropped when destroyed: 0, 50, 35200, 55700, 76000, 93800 or 122500.")]
        public int money;
        [Tooltip("Spawned only from this difficulty up (Rookie = always).")]
        public StrikeDifficulty minDifficulty;

        public bool IsAir => StrikeUnitRules.IsAir(unit);

        /// <summary>Whether the event drops anything when destroyed.</summary>
        public bool HasPickup => bonus != StrikeItem.MachineGun || money > 0;
    }


    /// <summary>
    /// A mission of the strike mode: the ship flies up over a world while the ground scrolls down under it. The terrain is
    /// a list of tile segments of a <see cref="StrikeTheme"/>, the units come from a list of events along the level
    /// coordinate s, and the scroll comes to rest for the boss at <see cref="BossAt"/>. Always a boss mission. The
    /// inherited asteroid data (waves, sector theme, loot) is unused; the settings are still required.
    /// </summary>
    [CreateAssetMenu(fileName = "StrikeLevel", menuName = "Asteroids/Strike Level", order = 4)]
    public class StrikeLevel : AsteroidsLevel
    {
        [Header("Strike")]
        [SerializeField] internal StrikeTheme terrain;
        [SerializeField] internal TerrainSegment[] segments = new TerrainSegment[0];
        [Tooltip("Sorted by at.")]
        [SerializeField] internal StrikeEvent[] events = new StrikeEvent[0];
        [SerializeField] internal float scrollSpeed = StrikeRules.ScrollSpeed;
        [Tooltip("The scroll distance at which the scroll comes to rest for the boss.")]
        [SerializeField] internal float bossAt = 300f;
        [Tooltip("Multiplier of every aircraft and ground unit's health.")]
        [SerializeField] internal float toughness = 1f;
        [Tooltip("Money for the boss's core (its parts pay their prefab score).")]
        [SerializeField] internal int bossBounty;

        public override MissionMode Mode => MissionMode.Strike;

        public StrikeTheme Terrain => terrain;

        public TerrainSegment[] Segments => segments ?? new TerrainSegment[0];

        public StrikeEvent[] Events => events ?? new StrikeEvent[0];

        public float ScrollSpeed => scrollSpeed;

        public float BossAt => bossAt;

        public float Toughness => toughness;

        public int BossBounty => bossBounty;

        /// <summary>The strike boss at the end (the inherited boss prefab).</summary>
        public StrikeBoss StrikeBossPrefab => BossPrefab as StrikeBoss;

        /// <summary>The largest level coordinate the terrain covers: 20 m per tile, less the first tile below the screen.</summary>
        public float Length
        {
            get
            {
                int tiles = 0;
                foreach (TerrainSegment segment in Segments)
                {
                    if (segment != null)
                    {
                        tiles += Mathf.Max(0, segment.tiles);
                    }
                }
                return tiles * StrikeRules.TileLength - StrikeRules.TileLength;
            }
        }


        /// <summary>The segment and the variant of terrain tile <paramref name="index"/> (0 = the tile filling the first screen).</summary>
        public TerrainSegment SegmentOfTile(int index)
        {
            int start = 0;
            foreach (TerrainSegment segment in Segments)
            {
                if (segment == null)
                {
                    continue;
                }
                int end = start + Mathf.Max(0, segment.tiles);
                if (index >= start && index < end)
                {
                    return segment;
                }
                start = end;
            }
            return null;
        }


        private void OnEnable()
        {
            objective = LevelObjective.Boss;
        }


        public override bool IsLevelValid(out string message)
        {
            if (Settings == null)
            {
                message = "The level has no asteroid settings.";
                return false;
            }
            if (objective != LevelObjective.Boss)
            {
                message = "A strike level is a boss mission.";
                return false;
            }
            if (terrain == null)
            {
                message = "The level has no strike theme.";
                return false;
            }
            if (Length < bossAt + 40f)
            {
                message = $"The terrain covers {Length:0} m; the boss at {bossAt:0} m needs at least {bossAt + 40f:0} m.";
                return false;
            }
            if (!(BossPrefab is StrikeBoss))
            {
                message = "The level has no strike boss.";
                return false;
            }
            if (scrollSpeed <= 0f || toughness <= 0f)
            {
                message = "The scroll speed and the toughness must be positive.";
                return false;
            }
            foreach (TerrainSegment segment in Segments)
            {
                if (segment == null || segment.tiles <= 0)
                {
                    message = "A terrain segment has no tiles.";
                    return false;
                }
                if (!terrain.Provides(segment.kind))
                {
                    message = $"The theme {terrain.name} has no {segment.kind} tiles.";
                    return false;
                }
            }
            float previous = float.MinValue;
            foreach (StrikeEvent spawn in Events)
            {
                if (spawn == null || spawn.count <= 0)
                {
                    message = "An event spawns nothing.";
                    return false;
                }
                if (spawn.at < previous)
                {
                    message = $"The events are not sorted by at ({spawn.at:0.#} after {previous:0.#}).";
                    return false;
                }
                previous = spawn.at;
                if (!StrikeRules.IsMoneyValue(spawn.money))
                {
                    message = $"The event at {spawn.at:0.#} drops {spawn.money} money, which is no pickup.";
                    return false;
                }
                if (spawn.bonus != StrikeItem.MachineGun && !StrikeArmory.Info(spawn.bonus).CanBePickup)
                {
                    message = $"The event at {spawn.at:0.#} drops {spawn.bonus}, which cannot be a pickup.";
                    return false;
                }
                float last = bossAt + (spawn.IsAir ? 0f : StrikeRules.SpawnLead);
                if (spawn.at > last)
                {
                    message = $"The event at {spawn.at:0.#} comes after the boss at {bossAt:0.#} and would never spawn.";
                    return false;
                }
                if (spawn.IsAir && spawn.speed <= 0f)
                {
                    message = $"The air event at {spawn.at:0.#} has no speed.";
                    return false;
                }
                if (spawn.unit == StrikeUnit.Depot && !spawn.HasPickup)
                {
                    message = $"The depot at {spawn.at:0.#} has no pickup.";
                    return false;
                }
            }
            return Settings.AreSettingsValid(out message);
        }
    }
}
