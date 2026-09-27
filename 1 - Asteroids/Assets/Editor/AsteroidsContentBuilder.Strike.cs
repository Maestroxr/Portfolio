using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gamebox;
using Gamebox.Launcher;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Portfolio.Asteroids.EditorTools
{
    /// <summary>
    /// The strike campaign "SHADOW STRIKE" (design 1.3, 4.3): nine levels in three sectors (ARES FLATS, VERDANT DELTA, OUTER
    /// COLONIES), appended after the field missions so no field index moves. Each level is written with a small builder:
    /// the terrain as runs of tiles honouring the tile geometry of design 4.1, the pickups the economy budget places by
    /// hand, and a seeded filler that spreads the mission's air squadrons and ground units over the level (ground units only
    /// where their tile allows them: boats on water, vehicles on roads and streets, emplacements on base pads) and tags
    /// the extra hostiles Veteran and Elite so each difficulty meets its count. Themes are loaded by path from the ground
    /// content (Config/Strike/Themes/{name}.asset) and may be missing until it is merged.
    /// </summary>
    internal static partial class AsteroidsContentBuilder
    {
        /// <summary>The folder of the strike content.</summary>
        public const string StrikeConfig = "Config/Strike";

        /// <summary>The first strike sector's index (after the four field sectors).</summary>
        public const int FirstStrikeSector = 4;

        private static readonly (string title, int stars)[] StrikeSectors = { ("ARES FLATS", 0), ("VERDANT DELTA", 4), ("OUTER COLONIES", 10) };

        /// <summary>The field sector theme a strike sector borrows for anything that expects one (menus, backdrops).</summary>
        private static readonly string[] StrikeSectorBackdrops = { "Crimson", "Kepler", "Void" };

        /// <summary>One strike mission: its text, world, boss and the numbers of design 1.3.</summary>
        private sealed class StrikeMission
        {
            public string Title;
            public string Description;
            public string Introduces;
            public string[] Hints = new string[0];
            public string[] TouchHints = new string[0];
            public string Theme;
            public string Boss;
            public float BossAt;
            public float Toughness;
            public int Bounty;
            public int Rookie;
            public int Veteran;
            public int Elite;
            /// <summary>Share of the hostiles that fly (the rest are on the ground).</summary>
            public float AirShare;
            public StrikeUnit[] Air;
            public StrikeUnit[] Ground;
            public int Seed;
            public int Income;
            public Action<StrikeLevelBuilder> Terrain;
            public Action<StrikeLevelBuilder> Pickups;
        }


        // ------------------------------------------------------------------ the level builder

        /// <summary>
        /// Writes one strike level: tiles (tile i covers s in [20(i - 1), 20 i), its centre at s = 20 i - 10), events at level
        /// coordinates, and the seeded filler. Tile-local coordinates are x in [-32, 32] and y in [-10, 10].
        /// </summary>
        private sealed class StrikeLevelBuilder
        {
            public readonly List<TerrainSegment> Segments = new List<TerrainSegment>();
            public readonly List<(TerrainKind kind, int variant)> TileKinds = new List<(TerrainKind, int)>();
            public readonly List<StrikeEvent> Events = new List<StrikeEvent>();
            public readonly HashSet<StrikeEvent> Fixed = new HashSet<StrikeEvent>();

            public int TileCount => TileKinds.Count;

            /// <summary>The level coordinate of tile <paramref name="tile"/>'s centre.</summary>
            public static float Centre(int tile)
            {
                return StrikeRules.TileLength * tile - StrikeRules.TileLength * 0.5f;
            }

            /// <summary>Appends <paramref name="count"/> tiles of <paramref name="kind"/>; returns the index of the first.</summary>
            public int Tiles(TerrainKind kind, int count, int variant = 0)
            {
                int first = TileKinds.Count;
                Segments.Add(new TerrainSegment { kind = kind, variant = variant, tiles = count });
                for (int i = 0; i < count; i++)
                {
                    TileKinds.Add((kind, variant));
                }
                return first;
            }

            /// <summary>A ground unit at tile-local (<paramref name="x"/>, <paramref name="y"/>) of <paramref name="tile"/>.</summary>
            public StrikeEvent Ground(int tile, float x, float y, StrikeUnit unit, float heading = 0f, float drive = 0f, int count = 1, Vector2 offset = default,
                StrikeItem bonus = StrikeItem.MachineGun, int money = 0)
            {
                var spawn = new StrikeEvent
                {
                    at = Centre(tile) + y,
                    unit = unit,
                    count = count,
                    formation = count > 1 ? Formation.Line : Formation.Single,
                    x = x,
                    heading = heading,
                    drive = drive,
                    offset = offset == default ? new Vector2(3f, 0f) : offset,
                    bonus = bonus,
                    money = money
                };
                Events.Add(spawn);
                return spawn;
            }

            /// <summary>An air squadron that sets off when the scroll reaches <paramref name="at"/>.</summary>
            public StrikeEvent Air(float at, StrikeUnit unit, int count, Formation formation, float x, FlightPath path, bool mirror = false,
                FlightType? flight = null, float spacing = 3f, float speed = 1f, StrikeItem bonus = StrikeItem.MachineGun, int money = 0)
            {
                var spawn = new StrikeEvent
                {
                    at = at,
                    unit = unit,
                    count = count,
                    formation = formation,
                    x = x,
                    spacing = spacing,
                    path = path,
                    mirror = mirror,
                    speed = speed,
                    flight = flight ?? (unit == StrikeUnit.Kamikaze ? FlightType.Kamikaze : FlightPaths.DefaultType(path)),
                    bonus = bonus,
                    money = money
                };
                Events.Add(spawn);
                return spawn;
            }

            /// <summary>Marks an event as placed by hand: the filler's difficulty tags leave it for every difficulty.</summary>
            public StrikeEvent Keep(StrikeEvent spawn)
            {
                Fixed.Add(spawn);
                return spawn;
            }
        }


        // ------------------------------------------------------------------ the missions

        private static readonly StrikeMission[] StrikeMissions =
        {
            new StrikeMission
            {
                Title = "Dust Devil",
                Description = "The Shadow's raiders have dug in on the Ares Flats. Fly low over the desert, break their outposts and convoys, " +
                    "and stop the Sand Crawler before it reaches the colony.",
                Introduces = "Planet strike: the ground scrolls under you. Air and ground targets, money for every kill.",
                Hints = new[] { "The machine gun hits both air and ground. Ground units are flown over, aircraft can be rammed.", "Shoot the crawler's turrets first: its cannon is armoured until they fall." },
                Theme = "Desert", Boss = "SandCrawler", BossAt = 300f, Toughness = 1f, Bounty = 8000,
                Rookie = 70, Veteran = 90, Elite = 110, AirShare = 0.55f, Seed = 101, Income = 75000,
                Air = new[] { StrikeUnit.Dart, StrikeUnit.Hornet, StrikeUnit.Hornet },
                Ground = new[] { StrikeUnit.Turret, StrikeUnit.Tank, StrikeUnit.Truck, StrikeUnit.Hut, StrikeUnit.Turret },
                Terrain = b =>
                {
                    b.Tiles(TerrainKind.Plain, 3);
                    b.Tiles(TerrainKind.Rough, 2);
                    b.Tiles(TerrainKind.Road, 3);
                    b.Tiles(TerrainKind.Plain, 2, 1);
                    b.Tiles(TerrainKind.Road, 2, 1);
                    b.Tiles(TerrainKind.Rough, 2, 1);
                    b.Tiles(TerrainKind.Base, 5);
                },
                Pickups = b =>
                {
                    b.Keep(b.Ground(3, 9f, 2f, StrikeUnit.Crate, bonus: StrikeItem.EnergyModule));
                    b.Keep(b.Ground(6, 6f, -3f, StrikeUnit.Hut, money: StrikeRules.SmallArms));
                    b.Keep(b.Ground(11, 0f, 0f, StrikeUnit.Crate, bonus: StrikeItem.AirMissiles));
                }
            },
            new StrikeMission
            {
                Title = "Refinery Row",
                Description = "A line of refineries feeds the Shadow's war machines. Burn the fuel depots, clear the convoys on the supply road " +
                    "and bring down the Refinery Guardian.",
                Introduces = "Fuel tanks and depots explode and take their neighbours with them. Gunships hover and fight.",
                Hints = new[] { "Shoot a fuel tank next to other ground targets: the blast destroys them too.", "Collect the megabomb and save it for a crowded sky." },
                Theme = "Desert", Boss = "RefineryGuardian", BossAt = 340f, Toughness = 1.15f, Bounty = 12000,
                Rookie = 85, Veteran = 110, Elite = 135, AirShare = 0.5f, Seed = 202, Income = 125000,
                Air = new[] { StrikeUnit.Dart, StrikeUnit.Hornet, StrikeUnit.Dart, StrikeUnit.Gunship },
                Ground = new[] { StrikeUnit.Turret, StrikeUnit.Tank, StrikeUnit.Flak, StrikeUnit.FuelTank, StrikeUnit.FuelTank, StrikeUnit.Flak, StrikeUnit.Radar },
                Terrain = b =>
                {
                    b.Tiles(TerrainKind.Plain, 2);
                    b.Tiles(TerrainKind.Road, 3);
                    b.Tiles(TerrainKind.Base, 3);
                    b.Tiles(TerrainKind.Road, 2, 2);
                    b.Tiles(TerrainKind.Rough, 2);
                    b.Tiles(TerrainKind.Base, 2, 1);
                    b.Tiles(TerrainKind.Plain, 2, 1);
                    b.Tiles(TerrainKind.Base, 5);
                },
                Pickups = b =>
                {
                    b.Keep(b.Ground(3, -8f, 3f, StrikeUnit.Crate, bonus: StrikeItem.EnergyModule));
                    b.Keep(b.Ground(7, -6f, 0f, StrikeUnit.Depot, money: StrikeRules.Isotopes));
                    b.Keep(b.Ground(13, 5f, -2f, StrikeUnit.Depot, bonus: StrikeItem.MegaBomb));
                }
            },
            new StrikeMission
            {
                Title = "Offshore",
                Description = "Beyond the coast, the Shadow pumps the seabed from a fortified rig. Cross the beaches, sink the gunboats and take " +
                    "the Sea Fortress apart, launcher by launcher.",
                Introduces = "Gunboats, bombers and kamikaze divers. The rig pays a fortune.",
                Hints = new[] { "Air-to-ground missiles hit ground targets only, and hard.", "Kamikazes dive at you once they have chosen: sidestep late." },
                Theme = "Desert", Boss = "SeaFortress", BossAt = 380f, Toughness = 1.3f, Bounty = 120000,
                Rookie = 95, Veteran = 120, Elite = 145, AirShare = 0.55f, Seed = 303, Income = 280000,
                Air = new[] { StrikeUnit.Dart, StrikeUnit.Hornet, StrikeUnit.Gunship, StrikeUnit.Kamikaze, StrikeUnit.Bomber },
                Ground = new[] { StrikeUnit.Turret, StrikeUnit.Tank, StrikeUnit.Flak, StrikeUnit.Gunboat, StrikeUnit.Gunboat, StrikeUnit.Truck, StrikeUnit.Bunker },
                Terrain = b =>
                {
                    b.Tiles(TerrainKind.Plain, 2);
                    b.Tiles(TerrainKind.Base, 2);
                    b.Tiles(TerrainKind.Road, 2, 1);
                    b.Tiles(TerrainKind.Plain, 2, 1);
                    b.Tiles(TerrainKind.Coast, 1);
                    b.Tiles(TerrainKind.Sea, 14);
                },
                Pickups = b =>
                {
                    b.Keep(b.Ground(3, 7f, 2f, StrikeUnit.Hut, money: StrikeRules.SmallArms));
                    b.Keep(b.Ground(5, -9f, -4f, StrikeUnit.Crate, bonus: StrikeItem.GroundMissiles));
                    b.Keep(b.Ground(13, -6f, 0f, StrikeUnit.Gunboat, 90f, 1.2f, money: StrikeRules.Thaelite));
                }
            },
            new StrikeMission
            {
                Title = "Green Hell",
                Description = "The Verdant Delta's jungle hides the Shadow's airfields. Follow the roads and rivers north and meet the Twin " +
                    "Rotor, a flying gun platform.",
                Introduces = "Interceptors climb from behind you with a warning. Phase shields absorb damage first.",
                Hints = new[] { "An interceptor warning at the bottom edge means: move aside.", "Take the Twin Rotor's cannons first; its hull is armoured until they fall." },
                Theme = "Jungle", Boss = "TwinRotor", BossAt = 380f, Toughness = 1.5f, Bounty = 20000,
                Rookie = 95, Veteran = 120, Elite = 145, AirShare = 0.55f, Seed = 404, Income = 170000,
                Air = new[] { StrikeUnit.Hornet, StrikeUnit.Dart, StrikeUnit.Gunship, StrikeUnit.Interceptor, StrikeUnit.Bomber, StrikeUnit.Kamikaze },
                Ground = new[] { StrikeUnit.Tank, StrikeUnit.Bunker, StrikeUnit.Turret, StrikeUnit.Gunboat, StrikeUnit.Hut, StrikeUnit.Flak, StrikeUnit.Bunker, StrikeUnit.Radar },
                Terrain = b =>
                {
                    b.Tiles(TerrainKind.Plain, 2);
                    b.Tiles(TerrainKind.Field, 2);
                    b.Tiles(TerrainKind.Road, 3);
                    b.Tiles(TerrainKind.River, 1);
                    b.Tiles(TerrainKind.Rough, 3);
                    b.Tiles(TerrainKind.Field, 2, 1);
                    b.Tiles(TerrainKind.River, 1, 1);
                    b.Tiles(TerrainKind.Plain, 2, 1);
                    b.Tiles(TerrainKind.Road, 2, 1);
                    b.Tiles(TerrainKind.Rough, 2, 1);
                    b.Tiles(TerrainKind.Base, 3);
                },
                Pickups = b =>
                {
                    b.Keep(b.Ground(5, 4f, 3f, StrikeUnit.Hut, money: StrikeRules.Thaelite));
                    b.Keep(b.Ground(16, 10f, 0f, StrikeUnit.Depot, bonus: StrikeItem.PhaseShield));
                }
            },
            new StrikeMission
            {
                Title = "Delta Run",
                Description = "Rivers and lakes braid the delta, and boats run the Shadow's cargo through them. At the delta's end two laser " +
                    "silos guard the launch field: silence both.",
                Introduces = "Transports carry supplies: shoot them down for their cargo. The silos have no core.",
                Hints = new[] { "A transport always drops what it carries.", "The Twin Silos fire in turns. Stand between their beams." },
                Theme = "Jungle", Boss = "TwinSilos", BossAt = 400f, Toughness = 1.7f, Bounty = 30000,
                Rookie = 100, Veteran = 130, Elite = 160, AirShare = 0.5f, Seed = 505, Income = 210000,
                Air = new[] { StrikeUnit.Hornet, StrikeUnit.Dart, StrikeUnit.Gunship, StrikeUnit.Interceptor, StrikeUnit.Bomber, StrikeUnit.Kamikaze },
                Ground = new[] { StrikeUnit.Gunboat, StrikeUnit.Gunboat, StrikeUnit.Tank, StrikeUnit.Bunker, StrikeUnit.Turret, StrikeUnit.Flak, StrikeUnit.Bunker, StrikeUnit.FuelTank, StrikeUnit.Radar },
                Terrain = b =>
                {
                    b.Tiles(TerrainKind.Field, 2);
                    b.Tiles(TerrainKind.River, 1);
                    b.Tiles(TerrainKind.Plain, 1);
                    b.Tiles(TerrainKind.Lake, 2);
                    b.Tiles(TerrainKind.River, 1, 1);
                    b.Tiles(TerrainKind.Field, 2, 1);
                    b.Tiles(TerrainKind.Lake, 2, 1);
                    b.Tiles(TerrainKind.River, 1);
                    b.Tiles(TerrainKind.Road, 3, 2);
                    b.Tiles(TerrainKind.Plain, 2, 1);
                    b.Tiles(TerrainKind.River, 1, 1);
                    b.Tiles(TerrainKind.Field, 1);
                    b.Tiles(TerrainKind.Base, 5);
                },
                Pickups = b =>
                {
                    b.Keep(b.Air(120f, StrikeUnit.Transport, 1, Formation.Single, 0f, FlightPath.StraightDown, bonus: StrikeItem.Dumbfire));
                    b.Keep(b.Ground(10, -3f, 3f, StrikeUnit.Depot, money: StrikeRules.FusionCore));
                }
            },
            new StrikeMission
            {
                Title = "Night Raid",
                Description = "Under cover of night, strike the occupied city of New Halden. Its streets are full of armour and its roofs of " +
                    "flak, and the Skyhammer waits above the harbour.",
                Introduces = "A night mission. The Skyhammer drops sky mines: they cannot be shot, only avoided or megabombed.",
                Hints = new[] { "Vehicles drive along the streets: fire down the street lanes.", "A megabomb clears every enemy shot on screen, sky mines included." },
                Theme = "City", Boss = "Skyhammer", BossAt = 420f, Toughness = 1.9f, Bounty = 250000,
                Rookie = 100, Veteran = 130, Elite = 160, AirShare = 0.55f, Seed = 606, Income = 450000,
                Air = new[] { StrikeUnit.Dart, StrikeUnit.Hornet, StrikeUnit.Gunship, StrikeUnit.Interceptor, StrikeUnit.Bomber, StrikeUnit.Kamikaze },
                Ground = new[] { StrikeUnit.Tank, StrikeUnit.Truck, StrikeUnit.Tank, StrikeUnit.Turret, StrikeUnit.Flak, StrikeUnit.Radar, StrikeUnit.Bunker, StrikeUnit.Gunboat },
                Terrain = b =>
                {
                    b.Tiles(TerrainKind.Plain, 2);
                    b.Tiles(TerrainKind.Road, 2);
                    b.Tiles(TerrainKind.City, 4);
                    b.Tiles(TerrainKind.River, 1);
                    b.Tiles(TerrainKind.City, 4, 1);
                    b.Tiles(TerrainKind.Road, 2, 2);
                    b.Tiles(TerrainKind.City, 3);
                    b.Tiles(TerrainKind.River, 1, 1);
                    b.Tiles(TerrainKind.City, 3, 1);
                    b.Tiles(TerrainKind.Base, 3);
                },
                Pickups = b =>
                {
                    b.Keep(b.Ground(5, 0f, 4f, StrikeUnit.Crate, bonus: StrikeItem.PhaseShield));
                    b.Keep(b.Ground(9, -12f, -5f, StrikeUnit.Depot, money: StrikeRules.Thaelite));
                    b.Keep(b.Air(250f, StrikeUnit.Transport, 1, Formation.Single, 6f, FlightPath.StraightDown, money: StrikeRules.Isotopes));
                    b.Keep(b.Ground(18, 12f, 3f, StrikeUnit.Crate, bonus: StrikeItem.MegaBomb));
                }
            },
            new StrikeMission
            {
                Title = "Moonbase",
                Description = "The Outer Colonies begin on a dead moon, where the Shadow has built a chain of bases. Their laser towers see " +
                    "far: strike fast and bring down the Dome Fortress.",
                Introduces = "Laser towers glow before they fire a beam down their column.",
                Hints = new[] { "When a laser tower glows, leave its column.", "The dome is armoured until its four turrets fall." },
                Theme = "Moon", Boss = "DomeFortress", BossAt = 420f, Toughness = 2.2f, Bounty = 45000,
                Rookie = 110, Veteran = 140, Elite = 170, AirShare = 0.5f, Seed = 707, Income = 290000,
                Air = new[] { StrikeUnit.Dart, StrikeUnit.Hornet, StrikeUnit.Gunship, StrikeUnit.Interceptor, StrikeUnit.Bomber, StrikeUnit.Kamikaze },
                Ground = new[] { StrikeUnit.Turret, StrikeUnit.Flak, StrikeUnit.Bunker, StrikeUnit.Radar, StrikeUnit.LaserTower, StrikeUnit.Tank, StrikeUnit.LaserTower, StrikeUnit.Bunker },
                Terrain = b =>
                {
                    b.Tiles(TerrainKind.Plain, 3);
                    b.Tiles(TerrainKind.Rough, 3);
                    b.Tiles(TerrainKind.Base, 3);
                    b.Tiles(TerrainKind.Plain, 2, 1);
                    b.Tiles(TerrainKind.Rough, 3, 1);
                    b.Tiles(TerrainKind.Base, 2, 1);
                    b.Tiles(TerrainKind.Plain, 2);
                    b.Tiles(TerrainKind.Rough, 2);
                    b.Tiles(TerrainKind.Base, 5);
                },
                Pickups = b =>
                {
                    b.Keep(b.Ground(4, -5f, 2f, StrikeUnit.Crate, bonus: StrikeItem.EnergyModule));
                    b.Keep(b.Ground(8, 8f, 0f, StrikeUnit.Depot, money: StrikeRules.FreyliumOre));
                    b.Keep(b.Ground(15, -8f, -2f, StrikeUnit.Crate, bonus: StrikeItem.EnergyModule));
                }
            },
            new StrikeMission
            {
                Title = "Magma Works",
                Description = "On a volcanic world the Shadow forges its war machines over rivers of lava. Wreck the foundries and hunt the " +
                    "Foundry Crawler through the smoke.",
                Introduces = "Heavy armour and lava lakes. The Foundry Crawler's vents fire walls of bolts.",
                Hints = new[] { "Bunkers and laser towers are worth a fortune: bring ground weapons.", "The crawler's cannon opens when its turrets and vents are gone." },
                Theme = "Volcanic", Boss = "FoundryCrawler", BossAt = 440f, Toughness = 2.5f, Bounty = 60000,
                Rookie = 110, Veteran = 140, Elite = 170, AirShare = 0.5f, Seed = 808, Income = 360000,
                Air = new[] { StrikeUnit.Dart, StrikeUnit.Hornet, StrikeUnit.Gunship, StrikeUnit.Interceptor, StrikeUnit.Bomber, StrikeUnit.Kamikaze, StrikeUnit.Transport },
                Ground = new[] { StrikeUnit.Tank, StrikeUnit.Flak, StrikeUnit.Bunker, StrikeUnit.LaserTower, StrikeUnit.FuelTank, StrikeUnit.Bunker, StrikeUnit.LaserTower },
                Terrain = b =>
                {
                    b.Tiles(TerrainKind.Plain, 2);
                    b.Tiles(TerrainKind.Rough, 3);
                    b.Tiles(TerrainKind.Lake, 2);
                    b.Tiles(TerrainKind.Base, 2);
                    b.Tiles(TerrainKind.Rough, 2, 1);
                    b.Tiles(TerrainKind.Lake, 2, 1);
                    b.Tiles(TerrainKind.Plain, 2, 1);
                    b.Tiles(TerrainKind.Base, 3, 1);
                    b.Tiles(TerrainKind.Rough, 3);
                    b.Tiles(TerrainKind.Base, 5);
                },
                Pickups = b =>
                {
                    b.Keep(b.Ground(8, 6f, 2f, StrikeUnit.Depot, money: StrikeRules.FreyliumOre));
                    b.Keep(b.Air(210f, StrikeUnit.Transport, 1, Formation.Single, -6f, FlightPath.StraightDown, money: StrikeRules.Isotopes));
                    b.Keep(b.Ground(15, -4f, -3f, StrikeUnit.Depot, bonus: StrikeItem.PhaseShield));
                }
            },
            new StrikeMission
            {
                Title = "Shadow Station",
                Description = "The Shadow itself: a fortress station at the edge of the colonies. Everything it has stands between you and " +
                    "its core. Break its rings, then break its heart.",
                Introduces = "The final battle: three rings of defence, three phases.",
                Hints = new[] { "The outer ring shields the inner ring, and the inner ring shields the core.", "Save megabombs for the final phase." },
                Theme = "Station", Boss = "TheShadow", BossAt = 480f, Toughness = 2.8f, Bounty = 500000,
                Rookie = 115, Veteran = 150, Elite = 185, AirShare = 0.55f, Seed = 909, Income = 850000,
                Air = new[] { StrikeUnit.Dart, StrikeUnit.Hornet, StrikeUnit.Gunship, StrikeUnit.Interceptor, StrikeUnit.Bomber, StrikeUnit.Kamikaze, StrikeUnit.Transport },
                Ground = new[] { StrikeUnit.Turret, StrikeUnit.Flak, StrikeUnit.Bunker, StrikeUnit.LaserTower, StrikeUnit.Radar, StrikeUnit.Turret },
                Terrain = b =>
                {
                    b.Tiles(TerrainKind.Plain, 3);
                    b.Tiles(TerrainKind.Base, 3);
                    b.Tiles(TerrainKind.Rough, 2);
                    b.Tiles(TerrainKind.Plain, 2, 1);
                    b.Tiles(TerrainKind.Base, 4, 1);
                    b.Tiles(TerrainKind.Rough, 3, 1);
                    b.Tiles(TerrainKind.Plain, 2);
                    b.Tiles(TerrainKind.Base, 4);
                    b.Tiles(TerrainKind.Rough, 2);
                    b.Tiles(TerrainKind.Base, 3, 1);
                },
                Pickups = b =>
                {
                    b.Keep(b.Ground(5, 0f, 0f, StrikeUnit.Depot, money: StrikeRules.FusionCore));
                    b.Keep(b.Ground(11, 9f, 2f, StrikeUnit.Crate, bonus: StrikeItem.MegaBomb));
                    b.Keep(b.Air(330f, StrikeUnit.Transport, 1, Formation.Single, 0f, FlightPath.StraightDown, money: StrikeRules.FreyliumOre));
                    b.Keep(b.Ground(20, -9f, -2f, StrikeUnit.Crate, bonus: StrikeItem.MegaBomb));
                }
            }
        };


        // ------------------------------------------------------------------ building

        public static string StrikeLevelPath(int mission)
        {
            return $"{StrikeConfig}/Level{mission + 1}.asset";
        }


        /// <summary>A strike theme of the ground content (Config/Strike/Themes/{name}.asset); null until it exists.</summary>
        public static StrikeTheme StrikeThemeOf(string name)
        {
            return AsteroidsAssets.Load<StrikeTheme>($"{StrikeConfig}/Themes/{name}.asset");
        }


        static partial void AppendStrikeContent(List<GameLevel> levels, List<AsteroidsCampaign.Sector> sectors)
        {
            AsteroidSettings settings = BuildSettings($"{StrikeConfig}/Settings.asset", 1f, 6f, 3f);
            int firstIndex = levels.Count;
            var report = new StringBuilder("Strike levels (hostiles Rookie / Veteran / Elite, Veteran income at 75% kills against the budget):\n");
            for (int i = 0; i < StrikeMissions.Length; i++)
            {
                StrikeMission mission = StrikeMissions[i];
                StrikeLevelBuilder plan = Plan(mission);
                int index = firstIndex + i;
                int sector = FirstStrikeSector + i / 3;
                StrikeTheme theme = StrikeThemeOf(mission.Theme);
                if (theme == null)
                {
                    AsteroidsAssets.Problem($"the strike theme {mission.Theme} does not exist; {mission.Title} has no terrain.");
                }
                var boss = AsteroidsPrefabBuilder.Load<StrikeBoss>($"Strike/Bosses/{mission.Boss}");
                if (boss == null)
                {
                    AsteroidsAssets.Problem($"the strike boss {mission.Boss} does not exist; {mission.Title} has no boss.");
                }
                StrikeEvent[] events = plan.Events.OrderBy(e => e.at).ThenBy(e => (int)e.unit).ThenBy(e => e.x).ToArray();
                StrikeLevel level = AsteroidsAssets.SaveScriptable<StrikeLevel>(StrikeLevelPath(i), l =>
                {
                    Auto(l, nameof(GameLevel.Index), p => p.intValue = index);
                    Auto(l, nameof(GameLevel.Title), p => p.stringValue = mission.Title);
                    Auto(l, nameof(AsteroidsLevel.Settings), p => p.objectReferenceValue = settings);
                    l.description = mission.Description;
                    l.introduces = mission.Introduces;
                    l.hints = mission.Hints;
                    l.touchHints = mission.TouchHints;
                    l.sector = sector;
                    l.theme = Theme(StrikeSectorBackdrops[i / 3]);
                    l.themeRotation = new SectorTheme[0];
                    l.objective = LevelObjective.Boss;
                    l.objectiveTarget = 1;
                    l.waves = new WaveSpec[0];
                    l.boss = boss;
                    l.bossRotation = new Boss[0];
                    l.asteroidLoot = null;
                    l.lootChance = 0f;
                    l.podLoot = null;
                    l.scoreGoal = 0;
                    l.speedMultiplier = 1f;
                    l.terrain = theme;
                    l.segments = plan.Segments.ToArray();
                    l.events = events;
                    l.scrollSpeed = StrikeRules.ScrollSpeed;
                    l.bossAt = mission.BossAt;
                    l.toughness = mission.Toughness;
                    l.bossBounty = mission.Bounty;
                });
                levels.Add(level);
                report.AppendLine(Describe(mission, plan, boss));
                if (!level.IsLevelValid(out string message))
                {
                    report.AppendLine($"    not valid: {message}");
                    AsteroidsAssets.Problem($"strike Level{i + 1} ({mission.Title}) is not valid: {message}");
                }
            }
            for (int s = 0; s < StrikeSectors.Length; s++)
            {
                sectors.Add(new AsteroidsCampaign.Sector
                {
                    title = StrikeSectors[s].title,
                    theme = Theme(StrikeSectorBackdrops[s]),
                    starsRequired = StrikeSectors[s].stars,
                    mode = MissionMode.Strike
                });
            }
            Debug.Log(report.ToString());
        }


        /// <summary>Plans a mission: its terrain, its pickups, the filler, and the difficulty tags.</summary>
        private static StrikeLevelBuilder Plan(StrikeMission mission)
        {
            var b = new StrikeLevelBuilder();
            mission.Terrain(b);
            int needed = Mathf.CeilToInt((mission.BossAt + 40f + StrikeRules.TileLength) / StrikeRules.TileLength);
            if (b.TileCount < needed)
            {
                b.Tiles(TerrainKind.Plain, needed - b.TileCount);
            }
            mission.Pickups(b);
            var rng = new System.Random(mission.Seed);
            int fixedHostiles = b.Events.Sum(e => e.count);
            int filler = Mathf.Max(0, mission.Elite - fixedHostiles);
            int air = Mathf.RoundToInt(filler * mission.AirShare);
            FillGround(b, mission, filler - air, rng);
            FillAir(b, mission, air, rng);
            TagDifficulties(b, mission);
            return b;
        }


        // ------------------------------------------------------------------ the filler

        /// <summary>Spreads <paramref name="count"/> ground units over the tiles between the first screen and the boss's ground.</summary>
        private static void FillGround(StrikeLevelBuilder b, StrikeMission mission, int count, System.Random rng)
        {
            int firstTile = 1;
            int lastTile = Mathf.FloorToInt((mission.BossAt - 32f) / StrikeRules.TileLength);
            var tiles = new List<int>();
            for (int t = firstTile; t <= lastTile && t < b.TileCount; t++)
            {
                tiles.Add(t);
            }
            if (tiles.Count == 0)
            {
                return;
            }
            var taken = new Dictionary<int, List<Vector2>>();
            foreach (StrikeEvent spawn in b.Events.Where(e => !e.IsAir))
            {
                int tile = Mathf.FloorToInt(spawn.at / StrikeRules.TileLength) + 1;
                if (!taken.TryGetValue(tile, out List<Vector2> list))
                {
                    taken[tile] = list = new List<Vector2>();
                }
                list.Add(new Vector2(spawn.x, spawn.at - StrikeLevelBuilder.Centre(tile)));
            }
            int placed = 0;
            int attempts = 0;
            while (placed < count && attempts < count * 40)
            {
                attempts++;
                int tile = tiles[(placed + attempts) % tiles.Count];
                (TerrainKind kind, int variant) = b.TileKinds[tile];
                StrikeUnit unit = mission.Ground[rng.Next(mission.Ground.Length)];
                if (!Spot(kind, variant, unit, rng, out Vector2 local, out float heading, out float drive))
                {
                    continue;
                }
                if (!taken.TryGetValue(tile, out List<Vector2> spots))
                {
                    taken[tile] = spots = new List<Vector2>();
                }
                float clearance = StrikeUnitRules.Info(unit).Radius * 2f + 1.2f;
                if (spots.Any(p => (p - local).magnitude < clearance))
                {
                    continue;
                }
                spots.Add(local);
                b.Ground(tile, Round(local.x), Round(local.y), unit, heading, drive);
                placed++;
            }
        }


        private static float Round(float value)
        {
            return Mathf.Round(value * 2f) / 2f;
        }


        /// <summary>
        /// A tile-local spot where <paramref name="unit"/> may stand on a tile of <paramref name="kind"/> (design 4.1): boats on
        /// the water (the river band, the lake, the sea half of a coast, the sea), vehicles on the roads and streets or in the
        /// open, emplacements on base pads and in the open ground (clear of the rough tiles' tall props). False when the tile
        /// has no place for it.
        /// </summary>
        private static bool Spot(TerrainKind kind, int variant, StrikeUnit unit, System.Random rng, out Vector2 local, out float heading, out float drive)
        {
            local = Vector2.zero;
            heading = 0f;
            drive = 0f;
            float Range(float min, float max) => min + (float)rng.NextDouble() * (max - min);
            bool boat = unit == StrikeUnit.Gunboat;
            bool vehicle = unit == StrikeUnit.Tank || unit == StrikeUnit.Truck;
            switch (kind)
            {
                case TerrainKind.River:
                    if (boat)
                    {
                        local = new Vector2(Range(-14f, 14f), Range(-2f, 2f));
                        heading = rng.Next(2) == 0 ? 90f : 270f;
                        drive = Range(0.8f, 1.8f);
                        return true;
                    }
                    if (vehicle && variant % 2 == 0)
                    {
                        // Over the bridge at x = 0.
                        local = new Vector2(0f, Range(-2f, 2f));
                        heading = 180f;
                        drive = 1.5f;
                        return true;
                    }
                    if (unit == StrikeUnit.Turret || unit == StrikeUnit.Flak || unit == StrikeUnit.Hut)
                    {
                        // On a bank.
                        local = new Vector2(Range(-14f, 14f), rng.Next(2) == 0 ? Range(-8.5f, -6f) : Range(6f, 8.5f));
                        return true;
                    }
                    return false;
                case TerrainKind.Lake:
                {
                    float lakeX = variant % 2 == 0 ? -10f : 10f;
                    if (boat)
                    {
                        float angle = Range(0f, Mathf.PI * 2f);
                        float r = Range(0f, 4f);
                        local = new Vector2(lakeX + Mathf.Cos(angle) * r, Mathf.Sin(angle) * r);
                        heading = rng.Next(2) == 0 ? 90f : 270f;
                        drive = Range(0.4f, 0.9f);
                        return true;
                    }
                    if (vehicle || unit == StrikeUnit.Hut)
                    {
                        return false;
                    }
                    // Beside the lake, on the other side of the field.
                    local = new Vector2(-lakeX * Range(0.2f, 1.2f), Range(-7f, 7f));
                    return true;
                }
                case TerrainKind.Coast:
                {
                    // Variant 0: land below y = 0, sea above; variant 1 the reverse.
                    bool seaAbove = variant % 2 == 0;
                    if (boat)
                    {
                        local = new Vector2(Range(-15f, 15f), (seaAbove ? 1f : -1f) * Range(3f, 8f));
                        heading = rng.Next(2) == 0 ? 90f : 270f;
                        drive = Range(0.6f, 1.4f);
                        return true;
                    }
                    local = new Vector2(Range(-14f, 14f), (seaAbove ? -1f : 1f) * Range(3f, 8f));
                    return true;
                }
                case TerrainKind.Sea:
                    if (!boat)
                    {
                        return false;
                    }
                    local = new Vector2(Range(-15f, 15f), Range(-8f, 8f));
                    heading = rng.Next(2) == 0 ? 90f : 270f;
                    drive = Range(0.6f, 1.6f);
                    return true;
                case TerrainKind.Road:
                {
                    if (boat)
                    {
                        return false;
                    }
                    float roadX = variant % 3 == 1 ? -9f : variant % 3 == 2 ? 9f : 0f;
                    if (vehicle)
                    {
                        local = new Vector2(roadX + (rng.Next(2) == 0 ? -1.2f : 1.2f), Range(-8f, 8f));
                        heading = local.x < roadX ? 180f : 0f;
                        drive = heading == 180f ? Range(1f, 2.2f) : Range(0.8f, 1.6f);
                        return true;
                    }
                    // Beside the road.
                    float side = rng.Next(2) == 0 ? -1f : 1f;
                    local = new Vector2(Mathf.Clamp(roadX + side * Range(5f, 12f), -15f, 15f), Range(-8f, 8f));
                    return true;
                }
                case TerrainKind.City:
                {
                    if (boat)
                    {
                        return false;
                    }
                    float[] streets = { -12f, 0f, 12f };
                    float street = streets[rng.Next(streets.Length)];
                    if (vehicle)
                    {
                        local = new Vector2(street + (rng.Next(2) == 0 ? -1f : 1f), Range(-8f, 8f));
                        heading = local.x < street ? 180f : 0f;
                        drive = Range(0.8f, 1.8f);
                        return true;
                    }
                    // Emplacements at the crossings and along the cross street.
                    local = new Vector2(street + Range(-4f, 4f) * 0.25f, Range(-1.2f, 1.2f));
                    if (unit == StrikeUnit.Hut || unit == StrikeUnit.Bunker)
                    {
                        local = new Vector2(Range(-14f, 14f), Range(-1f, 1f));
                    }
                    return true;
                }
                case TerrainKind.Base:
                    if (boat)
                    {
                        return false;
                    }
                    local = new Vector2(Range(-12.5f, 12.5f), Range(-8f, 8f));
                    if (vehicle)
                    {
                        heading = rng.Next(2) == 0 ? 180f : 0f;
                        drive = 0f;
                    }
                    return true;
                case TerrainKind.Rough:
                    if (boat)
                    {
                        return false;
                    }
                    local = new Vector2(Range(-13f, 13f), Range(-8f, 8f));
                    if (vehicle)
                    {
                        heading = 180f;
                        drive = Range(0.5f, 1.2f);
                    }
                    return true;
                default:
                    if (boat)
                    {
                        return false;
                    }
                    local = new Vector2(Range(-15f, 15f), Range(-8f, 8f));
                    if (vehicle)
                    {
                        heading = rng.Next(4) == 0 ? 0f : 180f;
                        drive = Range(0.6f, 1.6f);
                    }
                    return true;
            }
        }


        /// <summary>Sends <paramref name="count"/> aircraft in squadrons spread evenly from the start to just before the boss.</summary>
        private static void FillAir(StrikeLevelBuilder b, StrikeMission mission, int count, System.Random rng)
        {
            var squads = new List<(StrikeUnit unit, int size)>();
            int total = 0;
            while (total < count)
            {
                StrikeUnit unit = mission.Air[rng.Next(mission.Air.Length)];
                int size;
                switch (unit)
                {
                    case StrikeUnit.Dart: size = 3 + rng.Next(3); break;
                    case StrikeUnit.Hornet: size = 2 + rng.Next(2); break;
                    case StrikeUnit.Kamikaze: size = 3 + rng.Next(2); break;
                    case StrikeUnit.Interceptor: size = 2; break;
                    case StrikeUnit.Gunship: size = 1 + rng.Next(2); break;
                    default: size = 1; break;
                }
                size = Mathf.Min(size, count - total);
                squads.Add((unit, size));
                total += size;
            }
            float start = 12f;
            float end = mission.BossAt - 24f;
            for (int i = 0; i < squads.Count; i++)
            {
                (StrikeUnit unit, int size) = squads[i];
                float at = Round(Mathf.Lerp(start, end, squads.Count > 1 ? i / (float)(squads.Count - 1) : 0f) + ((float)rng.NextDouble() - 0.5f) * 4f);
                bool mirror = rng.Next(2) == 0;
                float x = new[] { -9f, -5f, 0f, 5f, 9f }[rng.Next(5)];
                switch (unit)
                {
                    case StrikeUnit.Dart:
                    {
                        FlightPath[] paths = { FlightPath.Swoop, FlightPath.DiveLeft, FlightPath.Arc, FlightPath.Zigzag, FlightPath.StraightDown, FlightPath.Spiral };
                        Formation[] shapes = { Formation.Vee, Formation.Line, Formation.Column };
                        b.Air(at, unit, size, size == 1 ? Formation.Single : shapes[rng.Next(shapes.Length)], x, paths[rng.Next(paths.Length)], mirror);
                        break;
                    }
                    case StrikeUnit.Hornet:
                    {
                        FlightPath[] paths = { FlightPath.SweepDown, FlightPath.StraightDown, FlightPath.CrossLeft, FlightPath.Strafe };
                        b.Air(at, unit, size, size == 1 ? Formation.Single : size == 2 ? Formation.Pair : Formation.Line, x, paths[rng.Next(paths.Length)], mirror, spacing: 4f);
                        break;
                    }
                    case StrikeUnit.Gunship:
                        b.Air(at, unit, size, size == 1 ? Formation.Single : Formation.Pair, x * 0.6f, rng.Next(2) == 0 ? FlightPath.HoverTop : FlightPath.HoverMid, mirror,
                            FlightType.Repeat, 8f);
                        break;
                    case StrikeUnit.Interceptor:
                        b.Air(at, unit, size, Formation.Pair, x, rng.Next(3) == 0 ? FlightPath.StraightDown : FlightPath.RiseUp, mirror, spacing: 5f);
                        break;
                    case StrikeUnit.Kamikaze:
                        b.Air(at, unit, size, Formation.Column, x, rng.Next(2) == 0 ? FlightPath.DiveLeft : FlightPath.Swoop, mirror, FlightType.Kamikaze, 2.5f);
                        break;
                    case StrikeUnit.Bomber:
                        b.Air(at, unit, 1, Formation.Single, x, rng.Next(2) == 0 ? FlightPath.StraightDown : FlightPath.Arc, mirror);
                        break;
                    default:
                        b.Air(at, unit, 1, Formation.Single, x, rng.Next(2) == 0 ? FlightPath.StraightDown : FlightPath.CrossLeft, mirror);
                        break;
                }
            }
        }


        /// <summary>
        /// Tags the filler so each difficulty meets its count: evenly spread events become Elite only until the Elite extra is
        /// met, then others Veteran only. Hand-placed events (pickups) stay for every difficulty.
        /// </summary>
        private static void TagDifficulties(StrikeLevelBuilder b, StrikeMission mission)
        {
            List<StrikeEvent> filler = b.Events.Where(e => !b.Fixed.Contains(e)).OrderBy(e => e.at).ThenBy(e => e.x).ToList();
            int eliteExtra = mission.Elite - mission.Veteran;
            int veteranExtra = mission.Veteran - mission.Rookie;
            Tag(filler, eliteExtra, StrikeDifficulty.Elite, 0.37f);
            Tag(filler, veteranExtra, StrikeDifficulty.Veteran, 0.71f);
        }


        private static void Tag(List<StrikeEvent> filler, int members, StrikeDifficulty difficulty, float phase)
        {
            var open = filler.Where(e => e.minDifficulty == StrikeDifficulty.Rookie).ToList();
            int tagged = 0;
            if (open.Count == 0)
            {
                return;
            }
            float step = open.Count / Mathf.Max(1f, members * 0.5f);
            for (float cursor = phase * step; tagged < members && open.Count > 0; cursor += step)
            {
                int index = Mathf.FloorToInt(cursor) % open.Count;
                StrikeEvent spawn = open[index];
                if (tagged + spawn.count > members + 1)
                {
                    open.RemoveAt(index);
                    continue;
                }
                spawn.minDifficulty = difficulty;
                tagged += spawn.count;
                open.RemoveAt(index);
            }
        }


        /// <summary>One line of the build log: hostile counts per difficulty, the Veteran income estimate against the budget.</summary>
        private static string Describe(StrikeMission mission, StrikeLevelBuilder plan, StrikeBoss boss)
        {
            int Count(StrikeDifficulty difficulty) => plan.Events.Where(e => e.minDifficulty <= difficulty).Sum(e => e.count);
            List<StrikeEvent> veteran = plan.Events.Where(e => e.minDifficulty <= StrikeDifficulty.Veteran).ToList();
            float bounties = veteran.Sum(e => StrikeUnitRules.Info(e.unit).Bounty * e.count);
            float orbs = veteran.Where(e => e.IsAir).Sum(e => e.count) * 0.75f * StrikeRules.CreditOrbChance * StrikeRules.CreditOrb;
            int money = plan.Events.Sum(e => e.money);
            int parts = boss != null ? boss.Parts.Sum(p => p != null ? p.score : 0) : 0;
            float income = bounties * 0.75f + orbs + money + mission.Bounty + parts;
            int air = veteran.Where(e => e.IsAir).Sum(e => e.count);
            return $"  {mission.Title}: {Count(StrikeDifficulty.Rookie)} / {Count(StrikeDifficulty.Veteran)} / {Count(StrikeDifficulty.Elite)} hostiles " +
                $"(budget {mission.Rookie} / {mission.Veteran} / {mission.Elite}; Veteran air {air}), {plan.TileCount} tiles, income ~{income:N0} " +
                $"(budget ~{mission.Income:N0}: kills {bounties * 0.75f:N0}, pickups {money:N0}, boss {mission.Bounty + parts:N0}).";
        }


        /// <summary>
        /// Batch mode: logs the plan of every strike level (counts and income) without writing anything, for tuning.
        /// </summary>
        public static void StrikeLevelReportBatch()
        {
            var report = new StringBuilder("Strike level plans:\n");
            foreach (StrikeMission mission in StrikeMissions)
            {
                StrikeLevelBuilder plan = Plan(mission);
                report.AppendLine(Describe(mission, plan, AsteroidsPrefabBuilder.Load<StrikeBoss>($"Strike/Bosses/{mission.Boss}")));
            }

            // The saved levels against a stand-in theme that has every kind (the real themes come with the ground content):
            // everything IsLevelValid checks besides the theme itself.
            var holder = new GameObject("Stand-in tile") { hideFlags = HideFlags.HideAndDontSave };
            TerrainTile tile = holder.AddComponent<TerrainTile>();
            StrikeTheme standIn = ScriptableObject.CreateInstance<StrikeTheme>();
            standIn.tiles = ((TerrainKind[])Enum.GetValues(typeof(TerrainKind)))
                .Select(kind => new StrikeTheme.TileSet { kind = kind, variants = new[] { tile, tile } }).ToArray();
            for (int i = 0; i < StrikeMissions.Length; i++)
            {
                var level = AsteroidsAssets.Load<StrikeLevel>(StrikeLevelPath(i));
                if (level == null)
                {
                    report.AppendLine($"  Level{i + 1}: not built yet.");
                    continue;
                }
                StrikeTheme theme = level.terrain;
                level.terrain = standIn;
                bool valid = level.IsLevelValid(out string message);
                level.terrain = theme;
                report.AppendLine($"  Level{i + 1} with a stand-in theme: {(valid ? "valid" : message)}; its own theme: {(theme != null ? theme.name : "missing")}.");
            }
            Object.DestroyImmediate(standIn);
            Object.DestroyImmediate(holder);
            Debug.Log(report.ToString());
        }
    }
}
