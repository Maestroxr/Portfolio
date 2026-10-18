using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gamebox;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Portfolio.Asteroids.EditorTools
{
    /// <summary>
    /// The second and third campaigns of Planet Strike, appended after everything that was there before them (arcs 1 and 2
    /// of the strike mode, each with its three sectors): IRON TIDE, against the Kranemark Consortium's war fleet over the ice
    /// of Boreas and the islands of Thalassa, and HIVE FALL, into the alien Hive across its living world, the crystal wastes
    /// and the orbital Halo. Every mission is planned like the Shadow Strike's (<see cref="StrikeMission"/>: terrain, hand
    /// placed pickups and set pieces, then the seeded filler and the difficulty tags), with the campaigns' own units: the
    /// filler places them by the layout of the new terrain kinds (<see cref="CampaignTiles"/>: guns on floes and islands,
    /// boats in the leads, spawners on mounds, vehicles in the crystal lanes) and flies drones and mites in swarms. Both
    /// campaigns are open from the start, with gates of 0, 4 and 10 stars of their own inside them; they are tuned for the
    /// strike pilot every campaign shares: the first missions for a pilot with little more than the machine gun, the later
    /// ones for the kit a pilot buys on the way.
    /// </summary>
    internal static partial class AsteroidsContentBuilder
    {
        /// <summary>A campaign of the strike mode: its name, story, folder, sectors (title, stars, the field sector it borrows) and missions.</summary>
        private sealed class StrikeCampaignSpec
        {
            public string Title;
            public string Description;
            public string Folder;
            public (string title, int stars, string backdrop)[] Sectors;
            public StrikeMission[] Missions;
        }

        // ------------------------------------------------------------------ Iron Tide

        private static readonly StrikeCampaignSpec IronTide = new StrikeCampaignSpec
        {
            Title = "Iron Tide",
            Description = "Nine missions over an ice world and an ocean of islands: the Kranemark Consortium sends its war fleet, the Iron Tide, " +
                "to take the free worlds for itself. Turn the tide.",
            Folder = "IronTide",
            Sectors = new[] { ("BOREAS SHELF", 0, "Frost"), ("THALASSA REACH", 4, "Kepler"), ("THE IRON DEEP", 10, "Crimson") },
            Missions = new[]
            {
                new StrikeMission
                {
                    Title = "Cold Open",
                    Description = "The Shadow is gone, and the Kranemark Consortium's war fleet has come to take what it left. Its convoys are " +
                        "stripping the ice of Boreas: stop the Glacier Hauler before it reaches the coast.",
                    Introduces = "The Iron Tide: swarms of attack drones, and SAM sites whose rockets turn after you.",
                    Hints = new[]
                    {
                        "A drone swarm flies loop after loop: sweep it with the machine gun.",
                        "Seeker rockets turn after you only for a moment: draw them aside, then dodge.",
                        "The hauler's reactor is armoured until its turrets and racks fall."
                    },
                    Theme = "Glacier", Boss = "GlacierHauler", BossAt = 300f, Toughness = 1.05f, Bounty = 9000,
                    Rookie = 70, Veteran = 90, Elite = 110, AirShare = 0.55f, Seed = 1101, Income = 105000,
                    Air = new[] { StrikeUnit.Drone, StrikeUnit.Dart, StrikeUnit.Hornet, StrikeUnit.Drone, StrikeUnit.Hornet },
                    Ground = new[] { StrikeUnit.Turret, StrikeUnit.Tank, StrikeUnit.Truck, StrikeUnit.Hut, StrikeUnit.Turret, StrikeUnit.SamSite, StrikeUnit.Radar },
                    Terrain = b =>
                    {
                        b.Tiles(TerrainKind.Plain, 3);
                        b.Tiles(TerrainKind.Road, 3);
                        b.Tiles(TerrainKind.Rough, 2);
                        b.Tiles(TerrainKind.Plain, 2, 1);
                        b.Tiles(TerrainKind.Road, 2, 1);
                        b.Tiles(TerrainKind.Rough, 1, 1);
                        b.Tiles(TerrainKind.Base, 5);
                    },
                    Pickups = b =>
                    {
                        b.Keep(b.Air(36f, StrikeUnit.Drone, 6, Formation.Column, -4f, FlightPath.Swarm, false, FlightType.Repeat, 2.2f, 0.9f));
                        b.Keep(b.Ground(3, 8f, 2f, StrikeUnit.Crate, bonus: StrikeItem.EnergyModule));
                        b.Keep(b.Ground(1, -10f, -3f, StrikeUnit.Crate, bonus: StrikeItem.SalvageMagnet));
                        b.Keep(b.Ground(5, -7f, -3f, StrikeUnit.Hut, money: StrikeRules.SmallArms));
                        b.Keep(b.Ground(8, -6f, 2f, StrikeUnit.SamSite));
                        b.Keep(b.Ground(8, 6f, 2f, StrikeUnit.SamSite));
                        b.Keep(b.Ground(9, -9f, -2f, StrikeUnit.Hut, money: StrikeRules.SmallArms));
                        b.Keep(b.Ground(11, 0f, 0f, StrikeUnit.Crate, bonus: StrikeItem.AirMissiles));
                    }
                },
                new StrikeMission
                {
                    Title = "Whiteout",
                    Description = "In the blizzard belt the Consortium has dug in its guns. Artillery shells the sky over the ice fields, and " +
                        "walkers patrol the roads to the Frost Battery.",
                    Introduces = "Artillery shells burst where you were: a red ring marks the spot. Walkers stride along the roads.",
                    Hints = new[]
                    {
                        "When a red ring opens under you, move: the shell lands as it closes.",
                        "Ground missiles and bombs make short work of artillery.",
                        "The battery's radome is armoured until its howitzers and racks fall."
                    },
                    Theme = "Glacier", Boss = "FrostBattery", BossAt = 340f, Toughness = 1.2f, Bounty = 14000,
                    Rookie = 85, Veteran = 110, Elite = 135, AirShare = 0.5f, Seed = 1202, Income = 125000,
                    Air = new[] { StrikeUnit.Drone, StrikeUnit.Hornet, StrikeUnit.Gunship, StrikeUnit.Dart, StrikeUnit.Drone },
                    Ground = new[] { StrikeUnit.Artillery, StrikeUnit.Walker, StrikeUnit.Tank, StrikeUnit.SamSite, StrikeUnit.Flak, StrikeUnit.Turret, StrikeUnit.FuelTank, StrikeUnit.Walker, StrikeUnit.Truck },
                    Terrain = b =>
                    {
                        b.Tiles(TerrainKind.Plain, 2);
                        b.Tiles(TerrainKind.Road, 2);
                        b.Tiles(TerrainKind.Rough, 2);
                        b.Tiles(TerrainKind.Base, 3);
                        b.Tiles(TerrainKind.Road, 2, 2);
                        b.Tiles(TerrainKind.Plain, 2, 1);
                        b.Tiles(TerrainKind.Rough, 2, 1);
                        b.Tiles(TerrainKind.Base, 5);
                    },
                    Pickups = b =>
                    {
                        b.Keep(b.Ground(3, 0f, 6f, StrikeUnit.Walker, 180f, 1.1f));
                        b.Keep(b.Ground(3, 8f, 2f, StrikeUnit.Crate, bonus: StrikeItem.EnergyModule));
                        b.Keep(b.Ground(11, -8f, 3f, StrikeUnit.Crate, bonus: StrikeItem.RepairKit));
                        b.Keep(b.Ground(5, 9f, -3f, StrikeUnit.Hut, money: StrikeRules.Isotopes));
                        b.Keep(b.Ground(7, -9f, 0f, StrikeUnit.Artillery, count: 3, offset: new Vector2(9f, 0f)));
                        b.Keep(b.Ground(8, 10f, -2f, StrikeUnit.Depot, bonus: StrikeItem.MegaBomb));
                        b.Keep(b.Ground(13, -5f, 0f, StrikeUnit.Crate, bonus: StrikeItem.GroundMissiles));
                    }
                },
                new StrikeMission
                {
                    Title = "Pack Ice",
                    Description = "The fleet's icebreaker is cutting a channel to the open sea. Destroyers hold the leads between the floes and " +
                        "missile cruisers circle overhead. Sink the Icebreaker.",
                    Introduces = "Pack ice: destroyers sail the leads, guns stand on the floes. Missile cruisers fire seeker rockets.",
                    Hints = new[]
                    {
                        "Destroyers are tough: hit them with everything that reaches the ground.",
                        "A cruiser hovers for a while before it leaves: bring it down for a fortune."
                    },
                    Theme = "Glacier", Boss = "Icebreaker", BossAt = 380f, Toughness = 1.35f, Bounty = 110000,
                    Rookie = 95, Veteran = 120, Elite = 145, AirShare = 0.5f, Seed = 1303, Income = 300000,
                    Air = new[] { StrikeUnit.Drone, StrikeUnit.Dart, StrikeUnit.Cruiser, StrikeUnit.Gunship, StrikeUnit.Kamikaze, StrikeUnit.Hornet },
                    Ground = new[] { StrikeUnit.Destroyer, StrikeUnit.Gunboat, StrikeUnit.SamSite, StrikeUnit.Artillery, StrikeUnit.Turret, StrikeUnit.Flak, StrikeUnit.Gunboat },
                    Terrain = b =>
                    {
                        b.Tiles(TerrainKind.Plain, 2);
                        b.Tiles(TerrainKind.Road, 2);
                        b.Tiles(TerrainKind.Base, 2);
                        b.Tiles(TerrainKind.Coast, 1);
                        b.Tiles(TerrainKind.Ice, 2);
                        b.Tiles(TerrainKind.Sea, 1);
                        b.Tiles(TerrainKind.Ice, 2, 1);
                        b.Tiles(TerrainKind.Sea, 2, 1);
                        b.Tiles(TerrainKind.Ice, 3);
                        b.Tiles(TerrainKind.Sea, 6);
                    },
                    Pickups = b =>
                    {
                        b.Keep(b.Ground(4, 9f, 3f, StrikeUnit.Hut, money: StrikeRules.SmallArms));
                        b.Keep(b.Ground(7, -11f, 2.5f, StrikeUnit.Crate, bonus: StrikeItem.PhaseShield));
                        b.Keep(b.Air(150f, StrikeUnit.Cruiser, 1, Formation.Single, 0f, FlightPath.HoverTop, false, FlightType.Repeat));
                        b.Keep(b.Ground(9, -2f, 0f, StrikeUnit.Destroyer, 90f, 0.8f, money: StrikeRules.Thaelite));
                        b.Keep(b.Ground(14, 12.5f, 3.5f, StrikeUnit.Crate, bonus: StrikeItem.Dumbfire));
                    }
                },
                new StrikeMission
                {
                    Title = "Coral Strand",
                    Description = "Thalassa's islands are the Consortium's harbour in the south. Its batteries hide under shield domes, and the " +
                        "Gale Carrier launches drone swarms from the clouds.",
                    Introduces = "Shield generators: nothing under their dome can be hurt while they stand. Shoot the generator first.",
                    Hints = new[]
                    {
                        "Shots only spark on what a dome covers: find its generator.",
                        "The carrier's hangars launch swarms until they are destroyed."
                    },
                    Theme = "Reef", Boss = "GaleCarrier", BossAt = 380f, Toughness = 1.55f, Bounty = 22000,
                    Rookie = 95, Veteran = 120, Elite = 145, AirShare = 0.55f, Seed = 1404, Income = 190000,
                    Air = new[] { StrikeUnit.Drone, StrikeUnit.Dart, StrikeUnit.Hornet, StrikeUnit.Interceptor, StrikeUnit.Drone, StrikeUnit.Cruiser },
                    Ground = new[] { StrikeUnit.SamSite, StrikeUnit.Turret, StrikeUnit.Gunboat, StrikeUnit.Destroyer, StrikeUnit.Artillery, StrikeUnit.Flak, StrikeUnit.Hut },
                    Terrain = b =>
                    {
                        b.Tiles(TerrainKind.Plain, 2);
                        b.Tiles(TerrainKind.Road, 2);
                        b.Tiles(TerrainKind.Base, 1);
                        b.Tiles(TerrainKind.Coast, 1);
                        b.Tiles(TerrainKind.Islands, 2);
                        b.Tiles(TerrainKind.Sea, 1);
                        b.Tiles(TerrainKind.Islands, 2, 1);
                        b.Tiles(TerrainKind.Sea, 1, 1);
                        b.Tiles(TerrainKind.Islands, 2, 2);
                        b.Tiles(TerrainKind.Sea, 2, 2);
                        b.Tiles(TerrainKind.Islands, 1);
                        b.Tiles(TerrainKind.Sea, 5);
                    },
                    Pickups = b =>
                    {
                        b.Keep(b.Ground(1, -9f, -2f, StrikeUnit.Crate, bonus: StrikeItem.Afterburner));
                        b.Keep(b.Ground(3, 8f, 3f, StrikeUnit.Crate, bonus: StrikeItem.EnergyModule));
                        ShieldPost(b, 6, -11f, 2f, (3.2f, 2.2f, StrikeUnit.SamSite), (-3.2f, -2f, StrikeUnit.Turret), (2.2f, -3.2f, StrikeUnit.Turret));
                        b.Keep(b.Ground(7, 12f, -3.5f, StrikeUnit.Depot, money: StrikeRules.FusionCore));
                        ShieldPost(b, 9, 2f, 0f, (3.5f, 3f, StrikeUnit.Artillery), (-3.5f, -2.5f, StrikeUnit.SamSite), (-3f, 3f, StrikeUnit.Flak));
                        b.Keep(b.Ground(12, -8f, -3f, StrikeUnit.Crate, bonus: StrikeItem.AirMissiles));
                    }
                },
                new StrikeMission
                {
                    Title = "Lagoon Run",
                    Description = "Convoys run the lagoon between the reefs, destroyers escorting fat transports. Take their cargo and storm the " +
                        "Reef Bastion.",
                    Introduces = "Convoys: transports under destroyer escort. The Reef Bastion's towers shell the whole lagoon.",
                    Hints = new[]
                    {
                        "A transport always drops what it carries.",
                        "The bastion's keep opens when its towers and guns are gone."
                    },
                    Theme = "Reef", Boss = "ReefBastion", BossAt = 400f, Toughness = 1.75f, Bounty = 32000,
                    Rookie = 100, Veteran = 130, Elite = 160, AirShare = 0.5f, Seed = 1505, Income = 220000,
                    Air = new[] { StrikeUnit.Drone, StrikeUnit.Hornet, StrikeUnit.Gunship, StrikeUnit.Cruiser, StrikeUnit.Bomber, StrikeUnit.Dart },
                    Ground = new[] { StrikeUnit.Destroyer, StrikeUnit.Gunboat, StrikeUnit.Artillery, StrikeUnit.SamSite, StrikeUnit.Walker, StrikeUnit.Turret, StrikeUnit.FuelTank },
                    Terrain = b =>
                    {
                        b.Tiles(TerrainKind.Sea, 2);
                        b.Tiles(TerrainKind.Islands, 2, 1);
                        b.Tiles(TerrainKind.Sea, 1);
                        b.Tiles(TerrainKind.Islands, 2);
                        b.Tiles(TerrainKind.Coast, 1, 1);
                        b.Tiles(TerrainKind.Plain, 2);
                        b.Tiles(TerrainKind.Road, 2, 1);
                        b.Tiles(TerrainKind.Base, 2);
                        b.Tiles(TerrainKind.Coast, 1);
                        b.Tiles(TerrainKind.Islands, 2, 2);
                        b.Tiles(TerrainKind.Sea, 1);
                        b.Tiles(TerrainKind.Islands, 5, 2);
                    },
                    Pickups = b =>
                    {
                        Convoy(b, 4, 90f, StrikeUnit.Destroyer, 2);
                        b.Keep(b.Air(64f, StrikeUnit.Transport, 1, Formation.Single, -6f, FlightPath.StraightDown, bonus: StrikeItem.PlasmaCannon));
                        b.Keep(b.Ground(8, 9f, 2f, StrikeUnit.Crate, bonus: StrikeItem.EnergyModule));
                        b.Keep(b.Ground(12, -6f, 0f, StrikeUnit.Depot, bonus: StrikeItem.PhaseShield));
                        b.Keep(b.Air(250f, StrikeUnit.Transport, 1, Formation.Single, 7f, FlightPath.StraightDown, money: StrikeRules.Thaelite));
                        Convoy(b, 17, 270f, StrikeUnit.Gunboat, 3);
                    }
                },
                new StrikeMission
                {
                    Title = "Twin Wakes",
                    Description = "Two hunter boats, the Sea Wolves, have sunk every ship that came to Thalassa's aid. Fly out over the open sea " +
                        "and send them to the bottom.",
                    Introduces = "The Sea Wolves: a pack of two boats without a core. Sink both.",
                    Hints = new[]
                    {
                        "The pack goes down with its last gun.",
                        "Interceptors climb from behind over the open water too."
                    },
                    Theme = "Reef", Boss = "SeaWolves", BossAt = 420f, Toughness = 1.95f, Bounty = 240000,
                    Rookie = 100, Veteran = 130, Elite = 160, AirShare = 0.6f, Seed = 1606, Income = 470000,
                    Air = new[] { StrikeUnit.Interceptor, StrikeUnit.Kamikaze, StrikeUnit.Drone, StrikeUnit.Cruiser, StrikeUnit.Bomber, StrikeUnit.Hornet, StrikeUnit.Drone },
                    Ground = new[] { StrikeUnit.Destroyer, StrikeUnit.Gunboat, StrikeUnit.SamSite, StrikeUnit.Artillery, StrikeUnit.Gunboat, StrikeUnit.Turret },
                    Terrain = b =>
                    {
                        b.Tiles(TerrainKind.Islands, 2, 1);
                        b.Tiles(TerrainKind.Sea, 2);
                        b.Tiles(TerrainKind.Islands, 1, 2);
                        b.Tiles(TerrainKind.Sea, 2, 1);
                        b.Tiles(TerrainKind.Islands, 2);
                        b.Tiles(TerrainKind.Sea, 3, 2);
                        b.Tiles(TerrainKind.Islands, 2, 2);
                        b.Tiles(TerrainKind.Sea, 2);
                        b.Tiles(TerrainKind.Islands, 1, 1);
                        b.Tiles(TerrainKind.Sea, 7, 1);
                    },
                    Pickups = b =>
                    {
                        b.Keep(b.Ground(1, 2f, 3f, StrikeUnit.Crate, bonus: StrikeItem.EnergyModule));
                        Convoy(b, 6, 90f, StrikeUnit.Destroyer, 3);
                        b.Keep(b.Ground(8, 12f, -3.5f, StrikeUnit.Depot, money: StrikeRules.FreyliumOre));
                        b.Keep(b.Air(240f, StrikeUnit.Transport, 1, Formation.Single, 0f, FlightPath.StraightDown, bonus: StrikeItem.PhaseShield));
                        b.Keep(b.Ground(13, 9f, 4.5f, StrikeUnit.Crate, bonus: StrikeItem.MegaBomb));
                    }
                },
                new StrikeMission
                {
                    Title = "Harbour Lights",
                    Description = "Night over Port Kranemark, where the Iron Tide refits. Shield domes guard the batteries along the docks, and the " +
                        "fleet's new warship waits in the Drydock.",
                    Introduces = "A night raid on the Consortium's port: shield generators in the streets, walkers and SAM nests.",
                    Hints = new[]
                    {
                        "Walkers stride along the streets: fire down the street lanes.",
                        "Take out a dome's generator before you waste fire on what it covers."
                    },
                    Theme = "City", Boss = "Drydock", BossAt = 420f, Toughness = 2.2f, Bounty = 48000,
                    Rookie = 110, Veteran = 140, Elite = 170, AirShare = 0.5f, Seed = 1707, Income = 300000,
                    Air = new[] { StrikeUnit.Drone, StrikeUnit.Cruiser, StrikeUnit.Gunship, StrikeUnit.Interceptor, StrikeUnit.Dart, StrikeUnit.Bomber },
                    Ground = new[] { StrikeUnit.Walker, StrikeUnit.Tank, StrikeUnit.SamSite, StrikeUnit.Artillery, StrikeUnit.Flak, StrikeUnit.Truck, StrikeUnit.Walker, StrikeUnit.Gunboat },
                    Terrain = b =>
                    {
                        b.Tiles(TerrainKind.Plain, 2);
                        b.Tiles(TerrainKind.Road, 2);
                        b.Tiles(TerrainKind.City, 4);
                        b.Tiles(TerrainKind.River, 1);
                        b.Tiles(TerrainKind.City, 3, 1);
                        b.Tiles(TerrainKind.Base, 2);
                        b.Tiles(TerrainKind.City, 3);
                        b.Tiles(TerrainKind.River, 1, 1);
                        b.Tiles(TerrainKind.Road, 2, 2);
                        b.Tiles(TerrainKind.Base, 3, 1);
                    },
                    Pickups = b =>
                    {
                        b.Keep(b.Ground(1, 10f, -2f, StrikeUnit.Crate, bonus: StrikeItem.ArmorPlating));
                        b.Keep(b.Ground(3, -6f, 3f, StrikeUnit.Crate, bonus: StrikeItem.EnergyModule));
                        ShieldPost(b, 5, -12f, 0f, (0f, 4f, StrikeUnit.SamSite), (0f, -4f, StrikeUnit.Flak), (3.5f, 0f, StrikeUnit.Turret));
                        ShieldPost(b, 11, 0f, 0f, (0f, 4.5f, StrikeUnit.Artillery), (0f, -4.5f, StrikeUnit.SamSite), (-4f, 0f, StrikeUnit.Turret), (4f, 0f, StrikeUnit.Turret));
                        b.Keep(b.Ground(12, 12f, 6f, StrikeUnit.Depot, money: StrikeRules.FusionCore));
                        ShieldPost(b, 15, 0f, 0f, (-4.5f, 2f, StrikeUnit.SamSite), (4.5f, 2f, StrikeUnit.SamSite), (0f, -4f, StrikeUnit.Walker));
                        b.Keep(b.Ground(16, -12f, -5f, StrikeUnit.Crate, bonus: StrikeItem.MegaBomb));
                        b.Keep(b.Air(280f, StrikeUnit.Transport, 1, Formation.Single, -5f, FlightPath.StraightDown, money: StrikeRules.Isotopes));
                    }
                },
                new StrikeMission
                {
                    Title = "Armada",
                    Description = "The Iron Tide sails at last: line after line of destroyers, cruisers above, and at its heart the Battleship. " +
                        "Break the armada before it reaches the free worlds.",
                    Introduces = "Destroyer lines and the Battleship: three rings of guns, three phases.",
                    Hints = new[]
                    {
                        "The Battleship's main turrets shell the sky: keep moving.",
                        "Save megabombs for its last phase."
                    },
                    Theme = "Reef", Boss = "Battleship", BossAt = 440f, Toughness = 2.5f, Bounty = 70000,
                    Rookie = 110, Veteran = 140, Elite = 170, AirShare = 0.5f, Seed = 1808, Income = 400000,
                    Air = new[] { StrikeUnit.Cruiser, StrikeUnit.Drone, StrikeUnit.Interceptor, StrikeUnit.Bomber, StrikeUnit.Gunship, StrikeUnit.Kamikaze, StrikeUnit.Drone },
                    Ground = new[] { StrikeUnit.Destroyer, StrikeUnit.Destroyer, StrikeUnit.Gunboat, StrikeUnit.SamSite, StrikeUnit.Artillery, StrikeUnit.Gunboat },
                    Terrain = b =>
                    {
                        b.Tiles(TerrainKind.Islands, 1, 2);
                        b.Tiles(TerrainKind.Sea, 3);
                        b.Tiles(TerrainKind.Islands, 1, 1);
                        b.Tiles(TerrainKind.Sea, 4, 1);
                        b.Tiles(TerrainKind.Islands, 1);
                        b.Tiles(TerrainKind.Sea, 4, 2);
                        b.Tiles(TerrainKind.Islands, 1, 2);
                        b.Tiles(TerrainKind.Sea, 10);
                    },
                    Pickups = b =>
                    {
                        b.Keep(b.Ground(0, -8f, -3f, StrikeUnit.Crate, bonus: StrikeItem.EnergyModule));
                        Convoy(b, 3, 90f, StrikeUnit.Destroyer, 3);
                        b.Keep(b.Ground(4, 2f, 3f, StrikeUnit.Depot, bonus: StrikeItem.PhaseShield));
                        Convoy(b, 8, 270f, StrikeUnit.Destroyer, 3);
                        b.Keep(b.Ground(9, -11f, 2f, StrikeUnit.Depot, money: StrikeRules.Thaelite));
                        b.Keep(b.Air(200f, StrikeUnit.Transport, 1, Formation.Single, 6f, FlightPath.StraightDown, money: StrikeRules.FusionCore));
                        Convoy(b, 13, 90f, StrikeUnit.Destroyer, 4);
                        b.Keep(b.Ground(14, -8f, -3f, StrikeUnit.Crate, bonus: StrikeItem.MegaBomb));
                        Convoy(b, 18, 270f, StrikeUnit.Gunboat, 4);
                    }
                },
                new StrikeMission
                {
                    Title = "Leviathan",
                    Description = "The Consortium's flagship rises over the deep ocean of Thalassa: the Leviathan, a flying fortress with a fleet " +
                        "in its hangars. End the Iron Tide.",
                    Introduces = "The final battle of the Iron Tide: the Leviathan's wings, hangars and reactor crown.",
                    Hints = new[]
                    {
                        "Its hangars launch drones until they fall: take them early in the second phase.",
                        "The reactor crown opens last: keep a shield for it."
                    },
                    Theme = "Reef", Boss = "Leviathan", BossAt = 480f, Toughness = 2.8f, Bounty = 520000,
                    Rookie = 115, Veteran = 150, Elite = 185, AirShare = 0.55f, Seed = 1909, Income = 900000,
                    Air = new[] { StrikeUnit.Cruiser, StrikeUnit.Drone, StrikeUnit.Interceptor, StrikeUnit.Bomber, StrikeUnit.Gunship, StrikeUnit.Kamikaze, StrikeUnit.Transport, StrikeUnit.Drone },
                    Ground = new[] { StrikeUnit.Destroyer, StrikeUnit.SamSite, StrikeUnit.Artillery, StrikeUnit.Gunboat, StrikeUnit.Turret, StrikeUnit.Destroyer },
                    Terrain = b =>
                    {
                        b.Tiles(TerrainKind.Islands, 2);
                        b.Tiles(TerrainKind.Sea, 2);
                        b.Tiles(TerrainKind.Islands, 2, 1);
                        b.Tiles(TerrainKind.Sea, 3, 1);
                        b.Tiles(TerrainKind.Islands, 2, 2);
                        b.Tiles(TerrainKind.Sea, 2);
                        b.Tiles(TerrainKind.Islands, 1);
                        b.Tiles(TerrainKind.Sea, 4, 2);
                        b.Tiles(TerrainKind.Islands, 1, 1);
                        b.Tiles(TerrainKind.Sea, 8);
                    },
                    Pickups = b =>
                    {
                        b.Keep(b.Ground(1, -11f, 2f, StrikeUnit.Depot, money: StrikeRules.FusionCore));
                        ShieldPost(b, 5, 2f, 0f, (3.5f, 3f, StrikeUnit.SamSite), (-3.5f, 3f, StrikeUnit.SamSite), (3.5f, -3f, StrikeUnit.Artillery));
                        Convoy(b, 7, 270f, StrikeUnit.Destroyer, 3);
                        b.Keep(b.Ground(9, 9f, 4.5f, StrikeUnit.Crate, bonus: StrikeItem.MegaBomb));
                        b.Keep(b.Air(300f, StrikeUnit.Transport, 1, Formation.Single, 0f, FlightPath.StraightDown, money: StrikeRules.FreyliumOre));
                        Convoy(b, 14, 90f, StrikeUnit.Destroyer, 4);
                        b.Keep(b.Ground(18, 2f, 2f, StrikeUnit.Crate, bonus: StrikeItem.MegaBomb));
                    }
                }
            }
        };

        // ------------------------------------------------------------------ Hive Fall

        private static readonly StrikeCampaignSpec HiveFall = new StrikeCampaignSpec
        {
            Title = "Hive Fall",
            Description = "Nine missions into the Hive: burn its brood on its living world, cross the crystal wastes where it grows its " +
                "weapons, take back the orbital Halo and kill the Hive Mother.",
            Folder = "HiveFall",
            Sectors = new[] { ("THE BROODLANDS", 0, "Crimson"), ("SHARD WASTES", 4, "Void"), ("THE HALO", 10, "Kepler") },
            Missions = new[]
            {
                new StrikeMission
                {
                    Title = "First Contact",
                    Description = "Something has eaten the colony of Mireth from the ground up. Fly over the Broodlands, burn what crawls and flies, " +
                        "and kill the Brood Mound that feeds it.",
                    Introduces = "The Hive: stingers spit spores that drift after you, and spitters burst them from the ground.",
                    Hints = new[]
                    {
                        "Spores are slow but follow you for a while: keep moving and they fall behind.",
                        "The mound's heart is armoured until its maws and sac are gone."
                    },
                    Theme = "Hive", Boss = "BroodMound", BossAt = 300f, Toughness = 1.25f, Bounty = 10000,
                    Rookie = 75, Veteran = 95, Elite = 115, AirShare = 0.55f, Seed = 2101, Income = 85000,
                    Air = new[] { StrikeUnit.Stinger, StrikeUnit.Stinger, StrikeUnit.Dart, StrikeUnit.Hornet },
                    Ground = new[] { StrikeUnit.Spitter, StrikeUnit.Spitter, StrikeUnit.Turret, StrikeUnit.Tank, StrikeUnit.Hut, StrikeUnit.Bunker },
                    Terrain = b =>
                    {
                        b.Tiles(TerrainKind.Base, 2);
                        b.Tiles(TerrainKind.Plain, 3);
                        b.Tiles(TerrainKind.Rough, 2);
                        b.Tiles(TerrainKind.Plain, 2, 1);
                        b.Tiles(TerrainKind.River, 1);
                        b.Tiles(TerrainKind.Plain, 2, 2);
                        b.Tiles(TerrainKind.Rough, 1, 1);
                        b.Tiles(TerrainKind.Nest, 5);
                    },
                    Pickups = b =>
                    {
                        b.Keep(b.Ground(2, 9f, 3f, StrikeUnit.Crate, bonus: StrikeItem.EnergyModule));
                        b.Keep(b.Air(46f, StrikeUnit.Stinger, 3, Formation.Column, 4f, FlightPath.Hook, true, spacing: 3f));
                        b.Keep(b.Ground(4, -10f, 3f, StrikeUnit.Crate, bonus: StrikeItem.RepairKit));
                        b.Keep(b.Ground(5, -8f, -2f, StrikeUnit.Hut, money: StrikeRules.SmallArms));
                        b.Keep(b.Ground(10, 0f, 7.5f, StrikeUnit.Crate, bonus: StrikeItem.AirMissiles));
                    }
                },
                new StrikeMission
                {
                    Title = "Hatchery",
                    Description = "The Hive breeds in pits of acid. Its spawners launch stingers while they live: hit them first, then the Spore Towers.",
                    Introduces = "Hive spawners launch stingers a few times: destroy them before they empty.",
                    Hints = new[]
                    {
                        "A spawner falls silent when it is destroyed, or spent.",
                        "The Spore Towers have no core: bring both down."
                    },
                    Theme = "Hive", Boss = "SporeTowers", BossAt = 340f, Toughness = 1.4f, Bounty = 16000,
                    Rookie = 85, Veteran = 110, Elite = 135, AirShare = 0.5f, Seed = 2202, Income = 170000,
                    Air = new[] { StrikeUnit.Stinger, StrikeUnit.Stinger, StrikeUnit.Gunship, StrikeUnit.Hornet, StrikeUnit.Stinger },
                    Ground = new[] { StrikeUnit.Spitter, StrikeUnit.Spitter, StrikeUnit.HiveSpawner, StrikeUnit.Flak, StrikeUnit.Turret, StrikeUnit.FuelTank },
                    Terrain = b =>
                    {
                        b.Tiles(TerrainKind.Plain, 2);
                        b.Tiles(TerrainKind.Lake, 2);
                        b.Tiles(TerrainKind.Nest, 2);
                        b.Tiles(TerrainKind.Plain, 2, 1);
                        b.Tiles(TerrainKind.River, 1, 1);
                        b.Tiles(TerrainKind.Nest, 2, 1);
                        b.Tiles(TerrainKind.Lake, 2, 1);
                        b.Tiles(TerrainKind.Rough, 2);
                        b.Tiles(TerrainKind.Plain, 5, 2);
                    },
                    Pickups = b =>
                    {
                        b.Keep(b.Ground(2, 9f, 3f, StrikeUnit.Crate, bonus: StrikeItem.EnergyModule));
                        b.Keep(b.Ground(1, -9f, -3f, StrikeUnit.Crate, bonus: StrikeItem.FlakCannon));
                        b.Keep(b.Ground(5, -9f, 2f, StrikeUnit.HiveSpawner));
                        b.Keep(b.Ground(6, 8f, -3f, StrikeUnit.Depot, bonus: StrikeItem.MegaBomb, money: StrikeRules.SmallArms));
                        b.Keep(b.Ground(7, -6f, 3f, StrikeUnit.Hut, money: StrikeRules.Isotopes));
                        b.Keep(b.Ground(10, 0f, -1f, StrikeUnit.HiveSpawner));
                        b.Keep(b.Ground(12, -10f, 0f, StrikeUnit.Crate, bonus: StrikeItem.GroundMissiles));
                    }
                },
                new StrikeMission
                {
                    Title = "Burrows",
                    Description = "The nests run deep. Mites swarm out of every burrow, and the Burrow Wyrm rises to meet you.",
                    Introduces = "Mite swarms dive at you. The Burrow Wyrm: tail, neck and head, three phases.",
                    Hints = new[]
                    {
                        "Mites have no guns, but they ram: sweep them before they dive.",
                        "The wyrm's head is armoured until its segments fall."
                    },
                    Theme = "Hive", Boss = "BurrowWyrm", BossAt = 380f, Toughness = 1.6f, Bounty = 120000,
                    Rookie = 95, Veteran = 120, Elite = 145, AirShare = 0.6f, Seed = 2303, Income = 300000,
                    Air = new[] { StrikeUnit.Mite, StrikeUnit.Stinger, StrikeUnit.Mite, StrikeUnit.Stinger, StrikeUnit.Gunship },
                    Ground = new[] { StrikeUnit.Spitter, StrikeUnit.HiveSpawner, StrikeUnit.Spitter, StrikeUnit.Bunker, StrikeUnit.Flak },
                    Terrain = b =>
                    {
                        b.Tiles(TerrainKind.Nest, 2, 2);
                        b.Tiles(TerrainKind.Plain, 2);
                        b.Tiles(TerrainKind.Nest, 3);
                        b.Tiles(TerrainKind.Rough, 2, 1);
                        b.Tiles(TerrainKind.Lake, 1);
                        b.Tiles(TerrainKind.Nest, 3, 1);
                        b.Tiles(TerrainKind.Plain, 2, 1);
                        b.Tiles(TerrainKind.Nest, 2, 2);
                        b.Tiles(TerrainKind.Rough, 5);
                    },
                    Pickups = b =>
                    {
                        b.Keep(b.Air(30f, StrikeUnit.Mite, 6, Formation.Column, 0f, FlightPath.Swoop, false, FlightType.Kamikaze, 1.8f));
                        b.Keep(b.Ground(3, 8f, 3f, StrikeUnit.Hut, money: StrikeRules.Isotopes));
                        b.Keep(b.Ground(8, -12f, -4f, StrikeUnit.Crate, bonus: StrikeItem.PhaseShield));
                        b.Keep(b.Ground(13, 10f, 3f, StrikeUnit.Depot, money: StrikeRules.Thaelite));
                        b.Keep(b.Ground(16, -6f, 2f, StrikeUnit.Crate, bonus: StrikeItem.Bombs));
                    }
                },
                new StrikeMission
                {
                    Title = "Glass Desert",
                    Description = "Beyond the Broodlands the Hive grows crystal. Prism pylons sing rings of needles across the wastes, and " +
                        "something huge sleeps among them.",
                    Introduces = "Prism pylons glow, then fire a ring of crystal needles.",
                    Hints = new[]
                    {
                        "When a prism glows, back away: its ring spreads out.",
                        "The Shard Colossus's arms fire lasers down their columns."
                    },
                    Theme = "Crystal", Boss = "ShardColossus", BossAt = 380f, Toughness = 1.8f, Bounty = 24000,
                    Rookie = 95, Veteran = 125, Elite = 150, AirShare = 0.5f, Seed = 2404, Income = 200000,
                    Air = new[] { StrikeUnit.Stinger, StrikeUnit.Mite, StrikeUnit.Interceptor, StrikeUnit.Stinger, StrikeUnit.Bomber },
                    Ground = new[] { StrikeUnit.Prism, StrikeUnit.Spitter, StrikeUnit.Prism, StrikeUnit.Tank, StrikeUnit.Flak, StrikeUnit.LaserTower, StrikeUnit.Bunker },
                    Terrain = b =>
                    {
                        b.Tiles(TerrainKind.Plain, 2);
                        b.Tiles(TerrainKind.Road, 2);
                        b.Tiles(TerrainKind.Rough, 2);
                        b.Tiles(TerrainKind.Plain, 2, 1);
                        b.Tiles(TerrainKind.Lake, 2);
                        b.Tiles(TerrainKind.Crystals, 2);
                        b.Tiles(TerrainKind.Plain, 2, 2);
                        b.Tiles(TerrainKind.Rough, 2, 1);
                        b.Tiles(TerrainKind.Base, 6);
                    },
                    Pickups = b =>
                    {
                        b.Keep(b.Ground(3, 6f, 2f, StrikeUnit.Crate, bonus: StrikeItem.EnergyModule));
                        b.Keep(b.Ground(7, -9f, 3f, StrikeUnit.Crate, bonus: StrikeItem.ArmorPlating));
                        b.Keep(b.Ground(6, -8f, 2f, StrikeUnit.Prism));
                        b.Keep(b.Ground(6, 8f, -2f, StrikeUnit.Prism));
                        b.Keep(b.Ground(9, 0f, 0f, StrikeUnit.Depot, money: StrikeRules.FusionCore));
                        b.Keep(b.Ground(14, 9f, -4f, StrikeUnit.Crate, bonus: StrikeItem.GroundMissiles));
                    }
                },
                new StrikeMission
                {
                    Title = "Prism Lanes",
                    Description = "Crystal forests leave only narrow lanes to fly along. At their end the Hive has grown a Prism Array.",
                    Introduces = "Crystal fields: the units hold the lanes. The Prism Array has no core.",
                    Hints = new[]
                    {
                        "Line up on a lane to hit everything on it.",
                        "Stand between the array's beams when they fire."
                    },
                    Theme = "Crystal", Boss = "PrismArray", BossAt = 400f, Toughness = 1.9f, Bounty = 34000,
                    Rookie = 100, Veteran = 130, Elite = 160, AirShare = 0.5f, Seed = 2505, Income = 275000,
                    Air = new[] { StrikeUnit.Stinger, StrikeUnit.Mite, StrikeUnit.Gunship, StrikeUnit.Interceptor, StrikeUnit.Stinger, StrikeUnit.Transport },
                    Ground = new[] { StrikeUnit.Prism, StrikeUnit.Tank, StrikeUnit.Spitter, StrikeUnit.Truck, StrikeUnit.Tank, StrikeUnit.Flak, StrikeUnit.LaserTower },
                    Terrain = b =>
                    {
                        b.Tiles(TerrainKind.Plain, 2);
                        b.Tiles(TerrainKind.Crystals, 3);
                        b.Tiles(TerrainKind.Road, 1, 2);
                        b.Tiles(TerrainKind.Crystals, 3, 1);
                        b.Tiles(TerrainKind.Lake, 2, 1);
                        b.Tiles(TerrainKind.Crystals, 3);
                        b.Tiles(TerrainKind.Plain, 1, 1);
                        b.Tiles(TerrainKind.Crystals, 2, 1);
                        b.Tiles(TerrainKind.Base, 5, 1);
                    },
                    Pickups = b =>
                    {
                        b.Keep(b.Ground(1, -8f, 2f, StrikeUnit.Crate, bonus: StrikeItem.EnergyModule));
                        b.Keep(b.Ground(4, -8f, -4f, StrikeUnit.Tank, 180f, 1.4f, 3, new Vector2(0f, 4f)));
                        b.Keep(b.Air(120f, StrikeUnit.Transport, 1, Formation.Single, 4f, FlightPath.StraightDown, bonus: StrikeItem.PhaseShield));
                        b.Keep(b.Ground(10, -10f, 0f, StrikeUnit.Depot, money: StrikeRules.FusionCore));
                        b.Keep(b.Ground(14, 8f, -2f, StrikeUnit.Hut, money: StrikeRules.Thaelite));
                        b.Keep(b.Ground(13, 8f, -4f, StrikeUnit.Prism, count: 3, offset: new Vector2(0f, 4f)));
                        b.Keep(b.Ground(16, 12f, 2f, StrikeUnit.Crate, bonus: StrikeItem.MegaBomb));
                    }
                },
                new StrikeMission
                {
                    Title = "Singing Spires",
                    Description = "The crystals' song grows louder: the Resonator at the heart of the wastes turns a ring of emitters round its core.",
                    Introduces = "The Resonator: a turning ring of crystal emitters.",
                    Hints = new[]
                    {
                        "The emitters turn with the ring: shoot the ones swinging into your line.",
                        "The core fires a beam once the ring is gone."
                    },
                    Theme = "Crystal", Boss = "Resonator", BossAt = 420f, Toughness = 2.2f, Bounty = 260000,
                    Rookie = 105, Veteran = 135, Elite = 165, AirShare = 0.55f, Seed = 2606, Income = 480000,
                    Air = new[] { StrikeUnit.Stinger, StrikeUnit.Mite, StrikeUnit.Interceptor, StrikeUnit.Bomber, StrikeUnit.Stinger, StrikeUnit.Gunship },
                    Ground = new[] { StrikeUnit.Prism, StrikeUnit.Spitter, StrikeUnit.LaserTower, StrikeUnit.Prism, StrikeUnit.Bunker, StrikeUnit.HiveSpawner },
                    Terrain = b =>
                    {
                        b.Tiles(TerrainKind.Rough, 2);
                        b.Tiles(TerrainKind.Crystals, 2);
                        b.Tiles(TerrainKind.Lake, 2);
                        b.Tiles(TerrainKind.Crystals, 2, 1);
                        b.Tiles(TerrainKind.Plain, 2, 1);
                        b.Tiles(TerrainKind.Rough, 2, 1);
                        b.Tiles(TerrainKind.Crystals, 3);
                        b.Tiles(TerrainKind.Lake, 1, 1);
                        b.Tiles(TerrainKind.Plain, 7);
                    },
                    Pickups = b =>
                    {
                        b.Keep(b.Ground(1, 10f, 2f, StrikeUnit.Crate, bonus: StrikeItem.EnergyModule));
                        b.Keep(b.Ground(6, 2f, -1f, StrikeUnit.HiveSpawner));
                        b.Keep(b.Ground(9, -8f, 0f, StrikeUnit.Depot, bonus: StrikeItem.PhaseShield));
                        b.Keep(b.Air(260f, StrikeUnit.Transport, 1, Formation.Single, -6f, FlightPath.StraightDown, money: StrikeRules.FreyliumOre));
                        b.Keep(b.Ground(17, 6f, 0f, StrikeUnit.Crate, bonus: StrikeItem.MegaBomb));
                    }
                },
                new StrikeMission
                {
                    Title = "Ring Breach",
                    Description = "The Hive has climbed into orbit: the Halo, the colonies' great ring station, is crawling with it. Breach the ring " +
                        "and take back its gate.",
                    Introduces = "The Halo: the Hive grows over the deck, and breaches open onto space.",
                    Hints = new[]
                    {
                        "The gate's sacs launch stingers as its ring turns: kill them first.",
                        "Spawners on the deck launch stingers: take them on the way."
                    },
                    Theme = "Halo", Boss = "HaloGate", BossAt = 420f, Toughness = 2.45f, Bounty = 52000,
                    Rookie = 110, Veteran = 140, Elite = 170, AirShare = 0.55f, Seed = 2707, Income = 320000,
                    Air = new[] { StrikeUnit.Stinger, StrikeUnit.Mite, StrikeUnit.Gunship, StrikeUnit.Interceptor, StrikeUnit.Stinger, StrikeUnit.Kamikaze },
                    Ground = new[] { StrikeUnit.Spitter, StrikeUnit.Turret, StrikeUnit.HiveSpawner, StrikeUnit.Flak, StrikeUnit.Bunker, StrikeUnit.LaserTower, StrikeUnit.Spitter },
                    Terrain = b =>
                    {
                        b.Tiles(TerrainKind.Plain, 3);
                        b.Tiles(TerrainKind.Base, 2);
                        b.Tiles(TerrainKind.Lake, 1);
                        b.Tiles(TerrainKind.Road, 2);
                        b.Tiles(TerrainKind.Plain, 2, 1);
                        b.Tiles(TerrainKind.Lake, 2, 1);
                        b.Tiles(TerrainKind.Base, 2, 1);
                        b.Tiles(TerrainKind.Road, 2, 1);
                        b.Tiles(TerrainKind.Plain, 2, 2);
                        b.Tiles(TerrainKind.Base, 5);
                    },
                    Pickups = b =>
                    {
                        b.Keep(b.Ground(2, -9f, 3f, StrikeUnit.Crate, bonus: StrikeItem.EnergyModule));
                        b.Keep(b.Ground(4, 8f, 0f, StrikeUnit.HiveSpawner));
                        b.Keep(b.Ground(8, -12f, -2f, StrikeUnit.Depot, money: StrikeRules.FusionCore));
                        b.Keep(b.Ground(11, -6f, 0f, StrikeUnit.HiveSpawner));
                        b.Keep(b.Ground(13, -6f, 4f, StrikeUnit.Crate, bonus: StrikeItem.MegaBomb));
                        b.Keep(b.Air(280f, StrikeUnit.Transport, 1, Formation.Single, 5f, FlightPath.StraightDown, money: StrikeRules.Thaelite));
                    }
                },
                new StrikeMission
                {
                    Title = "Brood Ship",
                    Description = "A living ship has docked on the Halo, carrying the next brood to the free worlds. Kill it before it casts off.",
                    Introduces = "The Brood Ship: maws, stinger sacs and sweeping spines.",
                    Hints = new[]
                    {
                        "The spines sweep spores from side to side: wait for the gap.",
                        "A megabomb clears the spores of a crowded sky."
                    },
                    Theme = "Halo", Boss = "BroodShip", BossAt = 440f, Toughness = 2.75f, Bounty = 160000,
                    Rookie = 110, Veteran = 145, Elite = 175, AirShare = 0.6f, Seed = 2808, Income = 410000,
                    Air = new[] { StrikeUnit.Stinger, StrikeUnit.Mite, StrikeUnit.Gunship, StrikeUnit.Bomber, StrikeUnit.Stinger, StrikeUnit.Interceptor, StrikeUnit.Mite },
                    Ground = new[] { StrikeUnit.Spitter, StrikeUnit.HiveSpawner, StrikeUnit.Flak, StrikeUnit.LaserTower, StrikeUnit.Turret, StrikeUnit.Spitter },
                    Terrain = b =>
                    {
                        b.Tiles(TerrainKind.Base, 2, 1);
                        b.Tiles(TerrainKind.Plain, 2);
                        b.Tiles(TerrainKind.Lake, 2);
                        b.Tiles(TerrainKind.Road, 2, 1);
                        b.Tiles(TerrainKind.Plain, 3, 1);
                        b.Tiles(TerrainKind.Base, 2);
                        b.Tiles(TerrainKind.Lake, 1, 1);
                        b.Tiles(TerrainKind.Plain, 2, 2);
                        b.Tiles(TerrainKind.Road, 2);
                        b.Tiles(TerrainKind.Plain, 7);
                    },
                    Pickups = b =>
                    {
                        b.Keep(b.Ground(1, 8f, 3f, StrikeUnit.Crate, bonus: StrikeItem.EnergyModule));
                        b.Keep(b.Ground(5, 8f, 0f, StrikeUnit.HiveSpawner));
                        b.Keep(b.Ground(9, 9f, -3f, StrikeUnit.Depot, bonus: StrikeItem.PhaseShield, money: StrikeRules.Thaelite));
                        b.Keep(b.Ground(12, -4f, 0f, StrikeUnit.HiveSpawner));
                        b.Keep(b.Air(300f, StrikeUnit.Transport, 1, Formation.Single, 0f, FlightPath.StraightDown, money: StrikeRules.FusionCore));
                        b.Keep(b.Ground(18, 10f, 2f, StrikeUnit.Crate, bonus: StrikeItem.MegaBomb));
                    }
                },
                new StrikeMission
                {
                    Title = "Heart of the Hive",
                    Description = "With the Halo free, the way to the Hive's heart lies open. Fly down into the oldest nest of the Broodlands and " +
                        "kill the Hive Mother.",
                    Introduces = "The final battle: the Hive Mother and her two turning rings.",
                    Hints = new[]
                    {
                        "Her outer ring of sacs shields the inner ring, and the inner ring her heart.",
                        "Save megabombs and shields for her last phase."
                    },
                    Theme = "Hive", Boss = "HiveMother", BossAt = 480f, Toughness = 3.1f, Bounty = 600000,
                    Rookie = 120, Veteran = 155, Elite = 190, AirShare = 0.55f, Seed = 2909, Income = 950000,
                    Air = new[] { StrikeUnit.Stinger, StrikeUnit.Mite, StrikeUnit.Gunship, StrikeUnit.Bomber, StrikeUnit.Stinger, StrikeUnit.Interceptor, StrikeUnit.Mite, StrikeUnit.Transport },
                    Ground = new[] { StrikeUnit.Spitter, StrikeUnit.HiveSpawner, StrikeUnit.Prism, StrikeUnit.Spitter, StrikeUnit.Bunker, StrikeUnit.Flak },
                    Terrain = b =>
                    {
                        b.Tiles(TerrainKind.Plain, 2);
                        b.Tiles(TerrainKind.Nest, 2);
                        b.Tiles(TerrainKind.River, 1);
                        b.Tiles(TerrainKind.Nest, 3, 1);
                        b.Tiles(TerrainKind.Rough, 2);
                        b.Tiles(TerrainKind.Lake, 2, 1);
                        b.Tiles(TerrainKind.Nest, 3, 2);
                        b.Tiles(TerrainKind.Rough, 2, 1);
                        b.Tiles(TerrainKind.Nest, 2);
                        b.Tiles(TerrainKind.Plain, 3, 1);
                        b.Tiles(TerrainKind.Nest, 5);
                    },
                    Pickups = b =>
                    {
                        b.Keep(b.Ground(1, -10f, 2f, StrikeUnit.Depot, money: StrikeRules.FusionCore));
                        b.Keep(b.Ground(7, 9f, -3f, StrikeUnit.Crate, bonus: StrikeItem.MegaBomb));
                        b.Keep(b.Air(240f, StrikeUnit.Transport, 1, Formation.Single, -5f, FlightPath.StraightDown, money: StrikeRules.FreyliumOre));
                        b.Keep(b.Ground(14, 12f, -4f, StrikeUnit.Depot, bonus: StrikeItem.PhaseShield));
                        b.Keep(b.Ground(19, -7f, 2f, StrikeUnit.Crate, bonus: StrikeItem.MegaBomb));
                    }
                }
            }
        };

        /// <summary>The strike campaigns this file adds, in the order they are appended (arcs 1 and 2 of the strike mode).</summary>
        private static StrikeCampaignSpec[] StrikeCampaigns => new[] { IronTide, HiveFall };

        // ------------------------------------------------------------------ set pieces

        /// <summary>A shield generator at tile-local (<paramref name="x"/>, <paramref name="y"/>) with the units it covers around it, for every difficulty.</summary>
        private static void ShieldPost(StrikeLevelBuilder b, int tile, float x, float y, params (float dx, float dy, StrikeUnit unit)[] guards)
        {
            b.Keep(b.Ground(tile, x, y, StrikeUnit.ShieldGenerator));
            foreach ((float dx, float dy, StrikeUnit unit) in guards)
            {
                b.Keep(b.Ground(tile, x + dx, y + dy, unit));
            }
        }


        /// <summary>
        /// A line of <paramref name="count"/> boats steaming across the water of <paramref name="tile"/> (heading 90 to the left,
        /// 270 to the right), one after another; the first is kept for every difficulty, the rest the filler tags.
        /// </summary>
        private static void Convoy(StrikeLevelBuilder b, int tile, float heading, StrikeUnit unit, int count)
        {
            float step = unit == StrikeUnit.Destroyer ? 8.5f : 5f;
            float start = heading == 90f ? 6f : -6f;
            for (int i = 0; i < count; i++)
            {
                float x = start + (heading == 90f ? i : -i) * step;
                StrikeEvent boat = b.Ground(tile, x, i % 2 == 0 ? -1.5f : 1.5f, unit, heading, unit == StrikeUnit.Destroyer ? 0.9f : 1.3f);
                if (i == 0)
                {
                    b.Keep(boat);
                }
            }
        }

        // ------------------------------------------------------------------ building

        public static string StrikeCampaignLevelPath(string folder, int mission)
        {
            return $"{StrikeConfig}/{folder}/Level{mission + 1}.asset";
        }


        static partial void AppendStrikeCampaigns(List<GameLevel> levels, List<AsteroidsCampaign.Sector> sectors, List<AsteroidsCampaign.Arc> arcs)
        {
            AsteroidSettings settings = BuildSettings($"{StrikeConfig}/Settings.asset", 1f, 6f, 3f);
            var report = new StringBuilder("Strike campaign levels (hostiles Rookie / Veteran / Elite, Veteran income at 75% kills against the budget):\n");
            foreach (StrikeCampaignSpec campaign in StrikeCampaigns)
            {
                int arc = arcs.Count(entry => entry != null && entry.mode == MissionMode.Strike);
                arcs.Add(new AsteroidsCampaign.Arc { title = campaign.Title, description = campaign.Description, mode = MissionMode.Strike, starsRequired = 0 });
                int firstSector = sectors.Count;
                foreach ((string title, int stars, string backdrop) in campaign.Sectors)
                {
                    sectors.Add(new AsteroidsCampaign.Sector { title = title, theme = Theme(backdrop), starsRequired = stars, mode = MissionMode.Strike, arc = arc });
                }
                report.AppendLine($"{campaign.Title} (strike campaign {arc}):");
                for (int i = 0; i < campaign.Missions.Length; i++)
                {
                    StrikeMission mission = campaign.Missions[i];
                    int sector = firstSector + Mathf.Min(i / 3, campaign.Sectors.Length - 1);
                    StrikeLevel level = SaveCampaignLevel(campaign, i, levels.Count, sector, settings, report);
                    levels.Add(level);
                }
            }
            Debug.Log(report.ToString());
        }


        /// <summary>Plans mission <paramref name="number"/> of <paramref name="campaign"/> and writes its level as the mission at <paramref name="index"/>.</summary>
        private static StrikeLevel SaveCampaignLevel(StrikeCampaignSpec campaign, int number, int index, int sector, AsteroidSettings settings, StringBuilder report)
        {
            StrikeMission mission = campaign.Missions[number];
            StrikeLevelBuilder plan = PlanCampaign(mission);
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
            string backdrop = campaign.Sectors[Mathf.Min(number / 3, campaign.Sectors.Length - 1)].backdrop;
            StrikeLevel level = AsteroidsAssets.SaveScriptable<StrikeLevel>(StrikeCampaignLevelPath(campaign.Folder, number), l =>
            {
                Auto(l, nameof(GameLevel.Index), p => p.intValue = index);
                Auto(l, nameof(GameLevel.Title), p => p.stringValue = mission.Title);
                Auto(l, nameof(AsteroidsLevel.Settings), p => p.objectReferenceValue = settings);
                l.description = mission.Description;
                l.introduces = mission.Introduces;
                l.hints = mission.Hints;
                l.touchHints = mission.TouchHints;
                l.sector = sector;
                l.theme = Theme(backdrop);
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
            report.AppendLine(Describe(mission, plan, boss));
            if (!level.IsLevelValid(out string message))
            {
                report.AppendLine($"    not valid: {message}");
                AsteroidsAssets.Problem($"strike {campaign.Folder} Level{number + 1} ({mission.Title}) is not valid: {message}");
            }
            return level;
        }


        /// <summary>Plans a campaign mission: its terrain, its pickups and set pieces, the filler with the campaigns' units, and the difficulty tags.</summary>
        private static StrikeLevelBuilder PlanCampaign(StrikeMission mission)
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
            FillCampaignGround(b, mission, filler - air, rng);
            FillCampaignAir(b, mission, air, rng);
            TagDifficulties(b, mission);
            return b;
        }

        // ------------------------------------------------------------------ the filler

        /// <summary>Spreads <paramref name="count"/> ground units over the tiles between the first screen and the boss's ground (<see cref="CampaignSpot"/>).</summary>
        private static void FillCampaignGround(StrikeLevelBuilder b, StrikeMission mission, int count, System.Random rng)
        {
            int lastTile = Mathf.FloorToInt((mission.BossAt - 32f) / StrikeRules.TileLength);
            var tiles = new List<int>();
            for (int t = 1; t <= lastTile && t < b.TileCount; t++)
            {
                tiles.Add(t);
            }
            if (tiles.Count == 0)
            {
                return;
            }
            var taken = new Dictionary<int, List<(Vector2 at, float radius)>>();
            foreach (StrikeEvent spawn in b.Events.Where(e => !e.IsAir))
            {
                for (int member = 0; member < spawn.count; member++)
                {
                    float at = spawn.at + spawn.offset.y * member;
                    int tile = Mathf.FloorToInt(at / StrikeRules.TileLength) + 1;
                    if (!taken.TryGetValue(tile, out List<(Vector2, float)> list))
                    {
                        taken[tile] = list = new List<(Vector2, float)>();
                    }
                    float radius = StrikeUnitRules.Info(spawn.unit).Radius;
                    if (spawn.unit == StrikeUnit.ShieldGenerator)
                    {
                        // Nothing else stands under a dome by chance: what it covers is placed on purpose.
                        radius = StrikeCampaignRules.ShieldRadius;
                    }
                    list.Add((new Vector2(spawn.x + spawn.offset.x * member, at - StrikeLevelBuilder.Centre(tile)), radius));
                }
            }
            int placed = 0;
            int attempts = 0;
            while (placed < count && attempts < count * 40)
            {
                attempts++;
                int tile = tiles[(placed + attempts) % tiles.Count];
                (TerrainKind kind, int variant) = b.TileKinds[tile];
                StrikeUnit unit = mission.Ground[rng.Next(mission.Ground.Length)];
                if (!CampaignSpot(kind, variant, unit, rng, out Vector2 local, out float heading, out float drive))
                {
                    continue;
                }
                if (!taken.TryGetValue(tile, out List<(Vector2 at, float radius)> spots))
                {
                    taken[tile] = spots = new List<(Vector2, float)>();
                }
                float own = StrikeUnitRules.Info(unit).Radius;
                if (spots.Any(spot => (spot.at - local).magnitude < own + spot.radius + 1.2f))
                {
                    continue;
                }
                spots.Add((local, own));
                b.Ground(tile, Round(local.x), Round(local.y), unit, heading, drive);
                placed++;
            }
        }


        /// <summary>
        /// A tile-local spot where <paramref name="unit"/> may stand on a tile of <paramref name="kind"/>. On the campaigns' kinds
        /// (by <see cref="CampaignTiles"/>): boats in the leads and the open water, everything else on the floes and islands;
        /// spawners on the mounds of a nest and the others in its open ground; everything in the lanes of a crystal field,
        /// vehicles driving along them. On the older kinds the new units stand where their older likeness would
        /// (<see cref="Spot"/>): the destroyer as a gunboat, the walker as a tank, the emplacements as turrets and bunkers.
        /// </summary>
        private static bool CampaignSpot(TerrainKind kind, int variant, StrikeUnit unit, System.Random rng, out Vector2 local, out float heading, out float drive)
        {
            local = Vector2.zero;
            heading = 0f;
            drive = 0f;
            float Range(float min, float max) => min + (float)rng.NextDouble() * (max - min);
            bool boat = unit == StrikeUnit.Gunboat || unit == StrikeUnit.Destroyer;
            bool vehicle = unit == StrikeUnit.Tank || unit == StrikeUnit.Truck || unit == StrikeUnit.Walker;
            float radius = StrikeUnitRules.Info(unit).Radius;
            switch (kind)
            {
                case TerrainKind.Ice:
                case TerrainKind.Islands:
                {
                    (Vector2 at, float radius)[] features = kind == TerrainKind.Ice ? CampaignTiles.Floes(variant) : CampaignTiles.Islands(variant);
                    if (boat)
                    {
                        for (int i = 0; i < 24; i++)
                        {
                            var p = new Vector2(Range(-15f, 15f), Range(-8f, 8f));
                            if (CampaignTiles.Field(features, p, 0, 0f) > radius + (kind == TerrainKind.Ice ? 1.2f : 1.8f))
                            {
                                local = p;
                                heading = rng.Next(2) == 0 ? 90f : 270f;
                                drive = unit == StrikeUnit.Destroyer ? Range(0.5f, 1f) : Range(0.6f, 1.4f);
                                return true;
                            }
                        }
                        return false;
                    }
                    List<(Vector2 at, float radius)> ground = CampaignTiles.OnScreen(features).Where(f => f.radius >= radius + 1.9f).ToList();
                    if (ground.Count == 0)
                    {
                        return false;
                    }
                    (Vector2 centre, float size) = ground[rng.Next(ground.Count)];
                    float angle = Range(0f, Mathf.PI * 2f);
                    local = centre + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * Range(0f, size - radius - 1.5f);
                    if (vehicle)
                    {
                        heading = rng.Next(2) == 0 ? 0f : 180f;
                    }
                    return true;
                }
                case TerrainKind.Nest:
                {
                    if (boat)
                    {
                        return false;
                    }
                    (Vector2 at, float radius)[] mounds = CampaignTiles.Mounds(variant);
                    if (unit == StrikeUnit.HiveSpawner)
                    {
                        List<(Vector2 at, float radius)> onScreen = CampaignTiles.OnScreen(mounds);
                        if (onScreen.Count == 0)
                        {
                            return false;
                        }
                        local = onScreen[rng.Next(onScreen.Count)].at;
                        return true;
                    }
                    (Vector2 at, float radius)[] pits = CampaignTiles.Pits(variant);
                    for (int i = 0; i < 24; i++)
                    {
                        var p = new Vector2(Range(-15f, 15f), Range(-8f, 8f));
                        if (CampaignTiles.Field(pits, p, 0, 0f) > radius + 0.8f && mounds.All(m => (m.at - p).magnitude > 2.6f + radius))
                        {
                            local = p;
                            if (vehicle)
                            {
                                heading = 180f;
                                drive = Range(0.5f, 1.2f);
                            }
                            return true;
                        }
                    }
                    return false;
                }
                case TerrainKind.Crystals:
                {
                    if (boat)
                    {
                        return false;
                    }
                    float[] lanes = CampaignTiles.Lanes(variant);
                    float lane = lanes[rng.Next(lanes.Length)];
                    local = new Vector2(lane + Range(-1f, 1f), Range(-8f, 8f));
                    if (vehicle)
                    {
                        local.x = lane;
                        heading = rng.Next(3) == 0 ? 0f : 180f;
                        drive = Range(0.8f, 1.6f);
                    }
                    return true;
                }
                default:
                    return Spot(kind, variant, Likeness(unit), rng, out local, out heading, out drive);
            }
        }


        /// <summary>The older unit a campaign unit stands like on the older kinds of ground.</summary>
        private static StrikeUnit Likeness(StrikeUnit unit)
        {
            switch (unit)
            {
                case StrikeUnit.Destroyer: return StrikeUnit.Gunboat;
                case StrikeUnit.Walker: return StrikeUnit.Tank;
                case StrikeUnit.HiveSpawner:
                case StrikeUnit.ShieldGenerator: return StrikeUnit.Bunker;
                case StrikeUnit.SamSite:
                case StrikeUnit.Artillery:
                case StrikeUnit.Prism:
                case StrikeUnit.Spitter: return StrikeUnit.Turret;
                default: return unit;
            }
        }


        /// <summary>
        /// Sends <paramref name="count"/> aircraft in squadrons spread evenly from the start to just before the boss: the
        /// Shadow Strike's squadrons as <see cref="FillAir"/> flies them, drones in swarms that loop the figure of eight or weave
        /// down, mites in diving swarms, stingers in hooks and weaves, cruisers alone.
        /// </summary>
        private static void FillCampaignAir(StrikeLevelBuilder b, StrikeMission mission, int count, System.Random rng)
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
                    case StrikeUnit.Drone: size = 5 + rng.Next(4); break;
                    case StrikeUnit.Mite: size = 5 + rng.Next(4); break;
                    case StrikeUnit.Stinger: size = 2 + rng.Next(3); break;
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
                    case StrikeUnit.Drone:
                        if (rng.Next(2) == 0)
                        {
                            b.Air(at, unit, size, Formation.Column, x * 0.5f, FlightPath.Swarm, mirror, FlightType.Repeat, 2.2f, 0.9f);
                        }
                        else
                        {
                            b.Air(at, unit, size, size > 6 ? Formation.Pair : Formation.Column, x * 0.4f, FlightPath.Weave, mirror, spacing: 2.2f);
                        }
                        break;
                    case StrikeUnit.Mite:
                    {
                        FlightPath[] paths = { FlightPath.Swoop, FlightPath.DiveLeft, FlightPath.Weave };
                        b.Air(at, unit, size, Formation.Column, x, paths[rng.Next(paths.Length)], mirror, FlightType.Kamikaze, 1.8f);
                        break;
                    }
                    case StrikeUnit.Stinger:
                    {
                        FlightPath[] paths = { FlightPath.Hook, FlightPath.Weave, FlightPath.DiveLeft, FlightPath.SweepDown };
                        b.Air(at, unit, size, size == 1 ? Formation.Single : size == 2 ? Formation.Pair : Formation.Column, x, paths[rng.Next(paths.Length)], mirror,
                            spacing: 3f);
                        break;
                    }
                    case StrikeUnit.Cruiser:
                        b.Air(at, unit, 1, Formation.Single, x * 0.5f, rng.Next(3) == 0 ? FlightPath.StraightDown : FlightPath.HoverTop, mirror,
                            rng.Next(3) == 0 ? FlightType.Linear : FlightType.Repeat);
                        break;
                    case StrikeUnit.Dart:
                    {
                        FlightPath[] paths = { FlightPath.Swoop, FlightPath.DiveLeft, FlightPath.Arc, FlightPath.Zigzag, FlightPath.StraightDown, FlightPath.Spiral };
                        Formation[] shapes = { Formation.Vee, Formation.Line, Formation.Column };
                        b.Air(at, unit, size, size == 1 ? Formation.Single : shapes[rng.Next(shapes.Length)], x, paths[rng.Next(paths.Length)], mirror);
                        break;
                    }
                    case StrikeUnit.Hornet:
                    {
                        FlightPath[] paths = { FlightPath.SweepDown, FlightPath.StraightDown, FlightPath.CrossLeft, FlightPath.Strafe, FlightPath.Hook };
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

        // ------------------------------------------------------------------ report

        /// <summary>
        /// The strike campaigns' part of <see cref="StrikeLevelReportBatch"/>: the plan of every mission (counts and income)
        /// and whether its saved level is valid against a stand-in theme that has every kind of ground.
        /// </summary>
        private static void ReportStrikeCampaigns(StringBuilder report)
        {
            var holder = new GameObject("Stand-in tile") { hideFlags = HideFlags.HideAndDontSave };
            TerrainTile tile = holder.AddComponent<TerrainTile>();
            StrikeTheme standIn = ScriptableObject.CreateInstance<StrikeTheme>();
            standIn.tiles = ((TerrainKind[])Enum.GetValues(typeof(TerrainKind)))
                .Select(kind => new StrikeTheme.TileSet { kind = kind, variants = new[] { tile, tile } }).ToArray();
            int total = 0;
            foreach (StrikeCampaignSpec campaign in StrikeCampaigns)
            {
                report.AppendLine($"{campaign.Title}:");
                for (int i = 0; i < campaign.Missions.Length; i++)
                {
                    StrikeMission mission = campaign.Missions[i];
                    StrikeLevelBuilder plan = PlanCampaign(mission);
                    report.AppendLine(Describe(mission, plan, AsteroidsPrefabBuilder.Load<StrikeBoss>($"Strike/Bosses/{mission.Boss}")));
                    total += mission.Income;
                    var level = AsteroidsAssets.Load<StrikeLevel>(StrikeCampaignLevelPath(campaign.Folder, i));
                    if (level == null)
                    {
                        report.AppendLine($"    {campaign.Folder}/Level{i + 1}: not built yet.");
                        continue;
                    }
                    StrikeTheme theme = level.terrain;
                    level.terrain = standIn;
                    bool valid = level.IsLevelValid(out string message);
                    level.terrain = theme;
                    report.AppendLine($"    {campaign.Folder}/Level{i + 1} with a stand-in theme: {(valid ? "valid" : message)}; its own theme: {(theme != null ? theme.name : "missing")}.");
                }
            }
            report.AppendLine($"Budgeted income of the two campaigns: {total:N0}.");
            Object.DestroyImmediate(standIn);
            Object.DestroyImmediate(holder);
        }
    }
}
