using System.Collections.Generic;
using System.Linq;
using Gamebox;
using UnityEditor;
using UnityEngine;

namespace Portfolio.Asteroids.EditorTools
{
    /// <summary>
    /// The second and third asteroid field campaigns (AsteroidsContentBuilder.FieldCampaigns.cs): NOVA FRONTIER, around a
    /// dying star, and THE LAST GATE, in a rift where an ancient gate wakes. Each has four sectors of three missions (the
    /// third of every sector a boss), its own story, the sector themes in the Classic and the generated looks, its drop
    /// tables and its battle music. The campaigns come after everything that was there before them (arcs 1 and 2 of the
    /// field), so no older index moves; the endless mission adds their sectors and bosses to its rotation.
    /// The numbers live here: ScoreGoal (the third star) is about 55% of what the autopilot scores on a mission, as for the
    /// first campaign (the median of its won flights; for a boss, of the flights it won without god mode, and where it
    /// never did, a goal set by hand; the Storm Herald, Sunbreaker and Gatekeeper were re-measured from their no-god wins
    /// after the tuning of 2026-10-18);
    /// re-measure after changing waves, scores or bosses (the tour's fly step logs the scores).
    /// </summary>
    internal static partial class AsteroidsContentBuilder
    {
        /// <summary>A mission of the field campaigns: the first campaign's numbers and the settings of the new objectives.</summary>
        private sealed class FieldMission
        {
            public string Title;
            public string Description;
            public string Introduces;
            public string[] Hints = new string[0];
            public string[] TouchHints = new string[0];
            /// <summary>The sector within the campaign (0 to 3).</summary>
            public int Sector;
            public LevelObjective Objective;
            public int Target;
            public WaveSpec[] Waves;
            public string Boss;
            public int ScoreGoal;
            public float Speed = 1f;
            public float SpawnRate = 6f;
            public float Explosion = 3.2f;
            public HazardKind[] Quarry = new HazardKind[0];
            public float QuarryShare = 1f;
            public float AllyHull = 120f;
            public float Crossing = 45f;
        }


        /// <summary>A campaign: its name and story, its folder, its four sectors (title, theme, stars of the campaign to enter) and its missions.</summary>
        private sealed class FieldCampaign
        {
            public string Title;
            public string Description;
            public string Folder;
            public (string title, string theme, int stars)[] Sectors;
            public FieldMission[] Missions;
        }


        /// <summary>The name of the second asteroid field campaign.</summary>
        public const string NovaCampaignTitle = "Nova Frontier";

        /// <summary>The name of the third asteroid field campaign.</summary>
        public const string GateCampaignTitle = "The Last Gate";

        /// <summary>The sector themes of the two campaigns, by asset name (Config/Themes/{name}): their sectors' looks.</summary>
        public static readonly string[] FieldCampaignSectorNames = { "Ember", "IonReach", "IronShoals", "Corona", "WreckRift", "Phantom", "HydraNebula", "Gate" };


        /// <summary>A wave of the field campaigns: the first campaign's counts and the new ones.</summary>
        private static WaveSpec FW(int rocks = 0, int ores = 0, int magma = 0, int ice = 0, int crystals = 0, int metal = 0, int hydra = 0, int wrecks = 0,
            int mines = 0, int bombs = 0, int comets = 0, int wells = 0, int saucers = 0, int wasps = 0, int flares = 0, int storms = 0, int sentries = 0,
            int minelayers = 0, int stalkers = 0, int corsairs = 0, int pods = 0, float interval = 6f, float duration = 30f, string title = "")
        {
            WaveSpec wave = W(rocks, ores, magma, ice, crystals, mines, bombs, comets, wells, saucers, wasps, pods, interval, duration, title);
            wave.metal = metal;
            wave.hydra = hydra;
            wave.wrecks = wrecks;
            wave.flares = flares;
            wave.storms = storms;
            wave.sentries = sentries;
            wave.minelayers = minelayers;
            wave.stalkers = stalkers;
            wave.corsairs = corsairs;
            return wave;
        }


        private static readonly FieldCampaign[] FieldCampaigns =
        {
            new FieldCampaign
            {
                Title = NovaCampaignTitle,
                Description = "Beyond the Void Core a dying star is about to go nova. Escort the colony convoys out through its flares and storms, and break the warlord who hunts them.",
                Folder = "Nova",
                Sectors = new[] { ("Ember Drift", "Ember", 0), ("Ion Reach", "IonReach", 3), ("Iron Shoals", "IronShoals", 8), ("Corona", "Corona", 15) },
                Missions = new[]
                {
                    new FieldMission
                    {
                        Title = "Dying Light", Sector = 0, Objective = LevelObjective.ClearWaves, ScoreGoal = 13000, Speed = 1f, SpawnRate = 6.5f,
                        Description = "The Void Core is behind you; ahead, a red giant is dying. Its flares sweep the Ember Drift - clear the rocks from the convoy lanes.",
                        Introduces = "Solar flares sweep across the sector. Dash through the fire.",
                        Hints = new[]
                        {
                            "The edge of the screen glows before a flare. Its wall of fire crosses the whole sector.",
                            "A dash (SHIFT) carries you through the fire unharmed - so does a full shield, at a price."
                        },
                        TouchHints = new[] { null, "A DASH carries you through the fire unharmed - so does a full shield, at a price." },
                        Waves = new[] { FW(rocks: 3, flares: 1, pods: 1), FW(rocks: 4, ores: 1, flares: 2, interval: 5f), FW(rocks: 4, magma: 1, flares: 2, pods: 1, interval: 5f) }
                    },
                    new FieldMission
                    {
                        Title = "Iron Rain", Sector = 0, Objective = LevelObjective.ClearWaves, ScoreGoal = 18000, Speed = 1f, SpawnRate = 6.5f,
                        Description = "The star's heat has boiled the drift down to iron. Armoured rocks shrug off your shots - but not a good blast.",
                        Introduces = "Iron rocks take half damage from shots and break in blasts.",
                        Hints = new[] { "Iron rocks ring when your shots glance off - they take half damage.", "Blasts crack iron: set off mines and magma rocks next to it." },
                        Waves = new[]
                        {
                            FW(rocks: 2, metal: 2, mines: 2, interval: 5f),
                            FW(rocks: 2, metal: 3, magma: 2, flares: 1, pods: 1),
                            FW(metal: 4, magma: 2, mines: 3, flares: 1, pods: 1, interval: 4.5f)
                        }
                    },
                    new FieldMission
                    {
                        Title = "Slag Titan", Sector = 0, Objective = LevelObjective.Boss, Boss = "SlagTitan", ScoreGoal = 30500, Speed = 1f, SpawnRate = 10f,
                        Description = "A titan of molten iron rolls out of the star's glare, hurling slag. It answers to nothing but heat.",
                        Introduces = "Boss: the Slag Titan.",
                        Hints = new[] { "The Slag Titan throws iron rocks - blasts break them fastest.", "Cornered, it calls the star's flares down on you." },
                        Waves = new[] { FW(rocks: 1, flares: 1, pods: 1, interval: 8f, title: "Heat haze") }
                    },
                    new FieldMission
                    {
                        Title = "Convoy", Sector = 1, Objective = LevelObjective.Escort, Target = 2, ScoreGoal = 16500, Speed = 1f, SpawnRate = 6f, AllyHull = 130f, Crossing = 42f,
                        Description = "The first colony freighters make their run through the Ion Reach. Raider corsairs are waiting for them. Get them across.",
                        Introduces = "Escort the freighters across. Corsairs go for them first. New weapon: the Arc Caster.",
                        Hints = new[]
                        {
                            "The freighter crosses the sector from one side to the other. If it is destroyed, the mission fails.",
                            "Corsairs dive at the freighter - shoot them down before they reach it.",
                            "Rocks that drift into the freighter damage it. Clear its path."
                        },
                        Waves = new[]
                        {
                            FW(rocks: 3, corsairs: 2, pods: 1, interval: 6f),
                            FW(rocks: 3, metal: 1, corsairs: 3, interval: 5f),
                            FW(rocks: 3, ores: 1, corsairs: 3, pods: 1, interval: 5f)
                        }
                    },
                    new FieldMission
                    {
                        Title = "Static", Sector = 1, Objective = LevelObjective.Survive, Target = 100, ScoreGoal = 16000, Speed = 1.05f, SpawnRate = 5.5f,
                        Description = "An ion storm front rolls over the Reach. Hold on for a hundred seconds while the lightning hunts you.",
                        Introduces = "Ion storms slow you down, drain your shield and call lightning.",
                        Hints = new[] { "Stay out of the ion clouds: they drag you and drain your shield.", "A ring marks where lightning will strike - get out of it." },
                        Waves = new[]
                        {
                            FW(rocks: 3, storms: 1, corsairs: 1, interval: 6f, duration: 30f),
                            FW(rocks: 3, metal: 2, storms: 2, corsairs: 2, pods: 1, interval: 5f, duration: 35f),
                            FW(rocks: 2, metal: 2, storms: 2, flares: 1, corsairs: 2, interval: 4.5f, duration: 35f)
                        }
                    },
                    new FieldMission
                    {
                        Title = "Storm Herald", Sector = 1, Objective = LevelObjective.Boss, Boss = "StormHerald", ScoreGoal = 11000, Speed = 1.05f, SpawnRate = 12f,
                        Description = "Something rides the storm: a living manta crackling with ion fire. The raiders call it the Herald.",
                        Introduces = "Boss: the Storm Herald circles the sector.",
                        Hints = new[] { "The Herald circles the sector and calls lightning on you - keep moving.", "It brings storms and corsairs when hurt. Save a nova bomb (B)." },
                        TouchHints = new[] { null, "It brings storms and corsairs when hurt. Save a nova bomb (NOVA)." },
                        Waves = new[] { FW(corsairs: 1, pods: 1, interval: 9f, title: "Storm front") }
                    },
                    new FieldMission
                    {
                        Title = "Gun Line", Sector = 2, Objective = LevelObjective.ClearWaves, ScoreGoal = 17500, Speed = 1.05f, SpawnRate = 5.5f,
                        Description = "The raiders have anchored sentry guns among the iron shoals. Break the line so the convoys can pass.",
                        Introduces = "Sentry guns glow before they fire. New weapon: the Rail Driver.",
                        Hints = new[] { "A sentry's muzzle glows before a burst - get out of its line.", "The Rail Driver punches through iron and through lines of targets." },
                        Waves = new[]
                        {
                            FW(metal: 3, sentries: 2, pods: 1, interval: 5f),
                            FW(rocks: 2, metal: 3, sentries: 3, mines: 2, interval: 4.5f),
                            FW(metal: 4, ores: 2, sentries: 3, corsairs: 2, pods: 1, interval: 4.5f)
                        }
                    },
                    new FieldMission
                    {
                        Title = "Holdout", Sector = 2, Objective = LevelObjective.Defend, Target = 110, ScoreGoal = 42000, Speed = 1.05f, SpawnRate = 5f, AllyHull = 450f,
                        Description = "A refuelling station sits in the shoals, the last stop before the Corona. Minelayers are closing in. Keep it standing.",
                        Introduces = "Defend the station. Minelayers sow mines as they cross.",
                        Hints = new[] { "Minelayers cross the sector sowing mines - stop them early.", "Mine blasts next to the station damage it. Set mines off while they are far from it." },
                        Waves = new[]
                        {
                            FW(rocks: 3, metal: 1, minelayers: 1, corsairs: 2, interval: 6f, duration: 35f),
                            FW(rocks: 2, metal: 2, minelayers: 1, sentries: 1, corsairs: 2, pods: 1, interval: 5f, duration: 35f),
                            FW(metal: 3, minelayers: 2, corsairs: 2, flares: 1, pods: 1, interval: 4.5f, duration: 40f)
                        }
                    },
                    new FieldMission
                    {
                        Title = "Bastion", Sector = 2, Objective = LevelObjective.Boss, Boss = "Bastion", ScoreGoal = 26000, Speed = 1.1f, SpawnRate = 5.5f,
                        Description = "The warlord's fortress guards the way out of the shoals: a ring of guns, lances and mine bays.",
                        Introduces = "Boss: the Bastion fires lances - a thin line warns where the beam will burn.",
                        Hints = new[] { "A thin red line warns of a lance. Get off the line before it burns.", "The Bastion sends out sentries and mines - clear them as you go." },
                        Waves = new[] { FW(sentries: 2, pods: 1, interval: 8f, title: "Outer guns") }
                    },
                    new FieldMission
                    {
                        Title = "Bounty", Sector = 3, Objective = LevelObjective.Hunt, Target = 6, ScoreGoal = 55500, Speed = 1.1f, SpawnRate = 5f,
                        Quarry = new[] { HazardKind.Corsair }, QuarryShare = 0.5f,
                        Description = "The warlord's aces fly in the glare of the corona, and six of them carry his codes. Find the marked ones and bring them down.",
                        Introduces = "Hunt the marked targets. Marked enemies are worth double.",
                        Hints = new[] { "Marked targets wear a gold reticle and are worth double points.", "Only the marked ones count - but the others still shoot." },
                        Waves = new[]
                        {
                            FW(rocks: 2, metal: 2, corsairs: 3, flares: 1, interval: 5f),
                            FW(rocks: 2, metal: 2, corsairs: 4, sentries: 1, flares: 1, pods: 1, interval: 4.5f),
                            FW(metal: 3, corsairs: 4, minelayers: 1, flares: 2, pods: 1, interval: 4f)
                        }
                    },
                    new FieldMission
                    {
                        Title = "Exodus", Sector = 3, Objective = LevelObjective.Escort, Target = 3, ScoreGoal = 35000, Speed = 1.1f, SpawnRate = 5f, AllyHull = 140f, Crossing = 40f,
                        Description = "The star is about to blow. The last three freighters run the Corona with everything the warlord has left on their tails.",
                        Introduces = "Everything at once - and the convoy must get through.",
                        Hints = new[]
                        {
                            "Flares burn the freighters too. Clear their way and keep the corsairs off them.",
                            "A freighter that gets through earns a bonus for the hull it has left."
                        },
                        Waves = new[]
                        {
                            FW(rocks: 2, metal: 2, corsairs: 3, flares: 1, interval: 5f),
                            FW(metal: 2, magma: 2, corsairs: 3, sentries: 1, storms: 1, pods: 1, interval: 4.5f),
                            FW(rocks: 2, metal: 3, corsairs: 3, minelayers: 1, flares: 2, pods: 1, interval: 4.5f)
                        }
                    },
                    new FieldMission
                    {
                        Title = "Sunbreaker", Sector = 3, Objective = LevelObjective.Boss, Boss = "Sunbreaker", ScoreGoal = 15000, Speed = 1.15f, SpawnRate = 12f,
                        Description = "The warlord himself, aboard the Sunbreaker: a flagship built around a cannon that drinks starlight. End his war before the star ends everything.",
                        Introduces = "The last boss of the Frontier: the Sunbreaker.",
                        Hints = new[]
                        {
                            "Its sun cannon sweeps a beam across the sector - watch the warning line and get behind it.",
                            "Nova orbs burst into rings of fire. Shoot them early or give them room."
                        },
                        Waves = new[] { FW(corsairs: 1, pods: 1, interval: 9f, title: "Flagship escort") }
                    }
                }
            },
            new FieldCampaign
            {
                Title = GateCampaignTitle,
                Description = "In a rift strewn with the wrecks of a forgotten war, an ancient gate is waking. Fight through the graveyard and what comes through it, and close the gate for good.",
                Folder = "Gate",
                Sectors = new[] { ("Wreck Rift", "WreckRift", 0), ("Phantom Reach", "Phantom", 4), ("Hydra Nebula", "HydraNebula", 10), ("The Gate", "Gate", 17) },
                Missions = new[]
                {
                    new FieldMission
                    {
                        Title = "Graveyard", Sector = 0, Objective = LevelObjective.ClearWaves, ScoreGoal = 17000, Speed = 1.05f, SpawnRate = 6f,
                        Description = "Past the dead star lies a rift full of wrecks from a war nobody remembers. Something in it is calling. Clear a way in.",
                        Introduces = "Wreckage holds salvage, and pieces with a glowing tank explode. New weapon: the Flak Burster.",
                        Hints = new[] { "Big wrecks drop salvage crystals when they break.", "A wreck with a glowing red tank explodes - use it on the rocks around it." },
                        Waves = new[]
                        {
                            FW(rocks: 2, wrecks: 2, pods: 1),
                            FW(rocks: 2, wrecks: 3, mines: 2, corsairs: 1, interval: 5f),
                            FW(wrecks: 4, metal: 2, mines: 2, corsairs: 2, pods: 1, interval: 4.5f)
                        }
                    },
                    new FieldMission
                    {
                        Title = "Salvage Rights", Sector = 0, Objective = LevelObjective.Collect, Target = 30, ScoreGoal = 16500, Speed = 1.05f, SpawnRate = 5.5f,
                        Description = "Scavengers and raiders pick over the graveyard. Take the salvage before they do - and before the minelayers seal the field.",
                        Introduces = "Minelayers and corsairs fight you for the salvage.",
                        Hints = new[] { "Wrecks drop salvage crystals - fly close to scoop them up.", "Minelayers sow mines behind them. Stop them early." },
                        Waves = new[]
                        {
                            FW(rocks: 1, wrecks: 3, corsairs: 2, pods: 1, interval: 5.5f),
                            FW(wrecks: 3, metal: 1, minelayers: 1, corsairs: 2, interval: 5f),
                            FW(wrecks: 4, minelayers: 1, corsairs: 3, pods: 1, interval: 4.5f)
                        }
                    },
                    new FieldMission
                    {
                        Title = "Scrap Leviathan", Sector = 0, Objective = LevelObjective.Boss, Boss = "ScrapLeviathan", ScoreGoal = 40500, Speed = 1.05f, SpawnRate = 5.5f,
                        Description = "The graveyard has a keeper: a leviathan of welded wrecks, with a maw that drags whole ships in.",
                        Introduces = "Boss: the Scrap Leviathan. Its maw pulls like a black hole.",
                        Hints = new[] { "When its maw opens, thrust away from the pull.", "It hurls wreckage - the pieces with tanks explode." },
                        Waves = new[] { FW(wrecks: 3, corsairs: 2, pods: 1, title: "Debris field") }
                    },
                    new FieldMission
                    {
                        Title = "Ghost Signal", Sector = 1, Objective = LevelObjective.ClearWaves, ScoreGoal = 15500, Speed = 1.1f, SpawnRate = 5.5f,
                        Description = "The gate's signal grows stronger, and things answer it - ships that fade in and out of the dark.",
                        Introduces = "Phase stalkers blink in close. Shoot them while they show.",
                        Hints = new[] { "A ring of light shows where a stalker will appear.", "Out of phase, a stalker cannot be hurt. Fire when it appears." },
                        Waves = new[]
                        {
                            FW(rocks: 3, wrecks: 1, stalkers: 2, interval: 6f),
                            FW(rocks: 2, wrecks: 2, stalkers: 3, mines: 2, pods: 1, interval: 5f),
                            FW(rocks: 2, metal: 2, stalkers: 4, corsairs: 1, pods: 1, interval: 4.5f)
                        }
                    },
                    new FieldMission
                    {
                        Title = "Ghost Hunt", Sector = 1, Objective = LevelObjective.Hunt, Target = 8, ScoreGoal = 48000, Speed = 1.1f, SpawnRate = 5f,
                        Quarry = new[] { HazardKind.Stalker }, QuarryShare = 0.6f,
                        Description = "Some of the stalkers carry the gate's key codes - eight of them. Hunt down the marked ones through the storms.",
                        Introduces = "Hunt the marked stalkers - through ion storms.",
                        Hints = new[] { "Marked stalkers wear a gold reticle. Only they count.", "Ion storms drain your shield - fight at their edges, not inside." },
                        Waves = new[]
                        {
                            FW(rocks: 2, wrecks: 2, stalkers: 3, storms: 1, interval: 5.5f),
                            FW(rocks: 2, metal: 2, stalkers: 4, storms: 1, pods: 1, interval: 5f),
                            FW(wrecks: 2, stalkers: 4, storms: 2, corsairs: 2, pods: 1, interval: 4.5f)
                        }
                    },
                    new FieldMission
                    {
                        Title = "Wraith Matriarch", Sector = 1, Objective = LevelObjective.Boss, Boss = "WraithMatriarch", ScoreGoal = 28000, Speed = 1.1f, SpawnRate = 5.5f,
                        Description = "The stalkers have a mother. She blinks across the Reach faster than any of her brood.",
                        Introduces = "Boss: the Wraith Matriarch blinks from place to place.",
                        Hints = new[] { "She cannot be hurt while she blinks - a ring shows where she will appear.", "Wounded, she calls her stalkers in swarms." },
                        Waves = new[] { FW(rocks: 2, stalkers: 2, pods: 1, title: "Shadows") }
                    },
                    new FieldMission
                    {
                        Title = "Overgrowth", Sector = 2, Objective = LevelObjective.ClearWaves, ScoreGoal = 28000, Speed = 1.15f, SpawnRate = 5.5f,
                        Description = "The nebula beyond the Reach is alive. Its rocks grow, split and grow again. Cut a path before it chokes the rift.",
                        Introduces = "Hydra rocks split in three, and their small pieces grow back if left alone.",
                        Hints = new[] { "Finish the small hydra pieces quickly - a swelling one is about to grow back.", "A hydra rock is only gone when every piece of it is." },
                        Waves = new[]
                        {
                            FW(rocks: 2, hydra: 2, interval: 6f),
                            FW(rocks: 1, hydra: 3, corsairs: 2, pods: 1, interval: 5f),
                            FW(hydra: 4, metal: 1, stalkers: 2, mines: 2, pods: 1, interval: 4.5f)
                        }
                    },
                    new FieldMission
                    {
                        Title = "Lifeline", Sector = 2, Objective = LevelObjective.Escort, Target = 2, ScoreGoal = 39000, Speed = 1.15f, SpawnRate = 5.5f, AllyHull = 140f, Crossing = 45f,
                        Description = "A survey freighter has charted the way to the gate. Bring it and its twin through the nebula alive.",
                        Introduces = "Escort through the hydra fields, with stalkers on the prowl.",
                        Hints = new[]
                        {
                            "Hydra rocks swarm around the freighter - keep its path clear.",
                            "Stalkers appear next to you, not the freighter. Do not let them draw you away for long."
                        },
                        Waves = new[]
                        {
                            FW(rocks: 2, hydra: 2, corsairs: 2, interval: 5.5f),
                            FW(hydra: 3, stalkers: 2, corsairs: 2, pods: 1, interval: 5f),
                            FW(hydra: 3, metal: 2, stalkers: 2, corsairs: 3, pods: 1, interval: 4.5f)
                        }
                    },
                    new FieldMission
                    {
                        Title = "The Hydra", Sector = 2, Objective = LevelObjective.Boss, Boss = "Hydra", ScoreGoal = 35500, Speed = 1.15f, SpawnRate = 5.5f,
                        Description = "At the heart of the nebula waits the mother of the hydra rocks, three-headed and spitting acid. Its wounds close as you watch.",
                        Introduces = "Boss: the Hydra heals when you let up.",
                        Hints = new[] { "Once wounded, the Hydra heals whenever you stop hitting it. Keep up the fire.", "It seeds hydra rocks - clear them before they grow." },
                        Waves = new[] { FW(rocks: 2, corsairs: 2, pods: 1, title: "The brood") }
                    },
                    new FieldMission
                    {
                        Title = "Threshold", Sector = 3, Objective = LevelObjective.Defend, Target = 130, ScoreGoal = 90000, Speed = 1.2f, SpawnRate = 5f, AllyHull = 450f,
                        Description = "Engineers have reached the gate with a station that can seal it. They need time - and everything in the rift is coming for them.",
                        Introduces = "Defend the sealing station at the gate.",
                        Hints = new[] { "Corsairs go for the station first. Stay close to it.", "Lightning and flares burn the station too." },
                        Waves = new[]
                        {
                            FW(rocks: 2, wrecks: 2, corsairs: 3, sentries: 1, interval: 5f, duration: 35f),
                            FW(hydra: 2, metal: 2, corsairs: 3, minelayers: 1, storms: 1, pods: 1, interval: 4.5f, duration: 40f),
                            FW(wrecks: 2, hydra: 2, corsairs: 4, stalkers: 2, flares: 1, pods: 1, interval: 4f, duration: 45f)
                        }
                    },
                    new FieldMission
                    {
                        Title = "Last Stand", Sector = 3, Objective = LevelObjective.Survive, Target = 150, ScoreGoal = 41500, Speed = 1.2f, SpawnRate = 4.5f,
                        Description = "The seal is failing. Hold the gate for two and a half minutes while everything the rift ever swallowed comes through.",
                        Introduces = "Everything the rift has - at once.",
                        Hints = new[] { "Wing drones and the chrono field help when you are swamped.", "Keep moving, and keep a nova bomb for the worst moment." },
                        Waves = new[]
                        {
                            FW(rocks: 2, hydra: 2, wrecks: 2, stalkers: 2, corsairs: 2, flares: 1, interval: 4.5f, duration: 35f),
                            FW(metal: 2, hydra: 2, sentries: 2, minelayers: 1, storms: 1, corsairs: 2, pods: 1, interval: 4f, duration: 40f),
                            FW(wrecks: 2, magma: 2, stalkers: 3, corsairs: 3, flares: 2, mines: 2, interval: 4f, duration: 40f),
                            FW(hydra: 3, metal: 2, stalkers: 3, sentries: 2, corsairs: 3, storms: 1, pods: 1, interval: 3.5f, duration: 40f)
                        }
                    },
                    new FieldMission
                    {
                        Title = "The Gatekeeper", Sector = 3, Objective = LevelObjective.Boss, Boss = "Gatekeeper", ScoreGoal = 21500, Speed = 1.25f, SpawnRate = 12f,
                        Description = "The gate opens its eye. Whatever built it left a keeper behind - and it has been waiting a very long time.",
                        Introduces = "The last boss: the Gatekeeper, in four phases.",
                        Hints = new[] { "The Gatekeeper holds the middle of the sector - do not fly into it.", "Every phase brings something new. Watch for warning lines and rings." },
                        Waves = new[] { FW(stalkers: 1, pods: 1, interval: 9f, title: "The threshold") }
                    }
                }
            }
        };


        // ------------------------------------------------------------------ campaigns, sectors and missions

        static partial void AppendFieldCampaigns(List<GameLevel> levels, List<AsteroidsCampaign.Sector> sectors, List<AsteroidsCampaign.Arc> arcs)
        {
            var bosses = new List<Boss>();
            var themes = new List<SectorTheme>();
            foreach (FieldCampaign campaign in FieldCampaigns)
            {
                int arc = arcs.Count(entry => entry.mode == MissionMode.Field);
                arcs.Add(new AsteroidsCampaign.Arc { title = campaign.Title, description = campaign.Description, mode = MissionMode.Field, starsRequired = 0 });
                var sectorIndex = new int[campaign.Sectors.Length];
                for (int s = 0; s < campaign.Sectors.Length; s++)
                {
                    (string title, string theme, int stars) = campaign.Sectors[s];
                    sectors.Add(new AsteroidsCampaign.Sector { title = title, theme = Theme(theme), starsRequired = stars, mode = MissionMode.Field, arc = arc });
                    sectorIndex[s] = sectors.Count - 1;
                    themes.Add(Theme(theme));
                }
                for (int i = 0; i < campaign.Missions.Length; i++)
                {
                    FieldMission mission = campaign.Missions[i];
                    AsteroidSettings settings = BuildSettings($"Config/Field/{campaign.Folder}/Settings/Mission{i + 1}.asset", mission.Speed, mission.SpawnRate, mission.Explosion);
                    int index = levels.Count;
                    int sector = sectorIndex[mission.Sector];
                    string themeName = campaign.Sectors[mission.Sector].theme;
                    string loot = $"{campaign.Folder}{mission.Sector + 1}";
                    Boss boss = string.IsNullOrEmpty(mission.Boss) ? null : AsteroidsPrefabBuilder.Load<Boss>($"Bosses/{mission.Boss}");
                    if (!string.IsNullOrEmpty(mission.Boss) && boss == null)
                    {
                        AsteroidsAssets.Problem($"the boss {mission.Boss} of {mission.Title} does not exist.");
                    }
                    if (boss != null && !bosses.Contains(boss))
                    {
                        bosses.Add(boss);
                    }
                    AsteroidsLevel level = AsteroidsAssets.SaveScriptable<AsteroidsLevel>($"Config/Field/{campaign.Folder}/Mission{i + 1}.asset", l =>
                    {
                        Auto(l, nameof(GameLevel.Index), p => p.intValue = index);
                        Auto(l, nameof(GameLevel.Title), p => p.stringValue = mission.Title);
                        Auto(l, nameof(AsteroidsLevel.Settings), p => p.objectReferenceValue = settings);
                        l.description = mission.Description;
                        l.introduces = mission.Introduces;
                        l.hints = mission.Hints;
                        l.touchHints = mission.TouchHints;
                        l.sector = sector;
                        l.theme = Theme(themeName);
                        l.themeRotation = new SectorTheme[0];
                        l.objective = mission.Objective;
                        l.objectiveTarget = mission.Target;
                        l.waves = mission.Waves.Select(wave => wave.Copy()).ToArray();
                        l.boss = boss;
                        l.bossRotation = new Boss[0];
                        l.asteroidLoot = LoadLoot($"{loot}Rocks");
                        l.lootChance = 0.07f + mission.Sector * 0.005f;
                        l.podLoot = LoadLoot($"{loot}Pods");
                        l.scoreGoal = mission.ScoreGoal;
                        l.speedMultiplier = 1f;
                        l.quarry = mission.Quarry;
                        l.quarryShare = mission.QuarryShare;
                        l.allyHull = mission.AllyHull;
                        l.crossing = mission.Crossing;
                        // The first campaign's rule would make every saucer a scout this deep into the list of sectors.
                        l.scoutChance = Mathf.Clamp01(0.3f + mission.Sector * 0.12f);
                    });
                    levels.Add(level);
                }
            }
            AddToEndless(levels, themes, bosses);
        }


        /// <summary>The endless mission rotates through the new sectors and sends the new bosses too, after the first campaign's.</summary>
        private static void AddToEndless(List<GameLevel> levels, List<SectorTheme> themes, List<Boss> bosses)
        {
            AsteroidsLevel endless = levels.OfType<AsteroidsLevel>().FirstOrDefault(level => level.IsEndless);
            if (endless == null)
            {
                return;
            }
            endless.themeRotation = (endless.themeRotation ?? new SectorTheme[0]).Concat(themes).Where(theme => theme != null).Distinct().ToArray();
            endless.bossRotation = (endless.bossRotation ?? new Boss[0]).Concat(bosses).Where(boss => boss != null).Distinct().ToArray();
            EditorUtility.SetDirty(endless);
        }


        // ------------------------------------------------------------------ drop tables

        static partial void BuildFieldCampaignLoot()
        {
            FieldTable("Nova1Rocks", ("Repair", 3), ("Shield", 3), ("WeaponBlaster", 2), ("WeaponScatter", 2), ("PowerUpOverdrive", 2), ("NovaBomb", 1));
            FieldTable("Nova1Pods", ("WeaponScatter", 3), ("WeaponBlaster", 2), ("Shield", 3), ("Repair", 2), ("PowerUpOverdrive", 1), ("NovaBomb", 1));
            FieldTable("Nova2Rocks", ("Repair", 3), ("Shield", 3), ("WeaponArc", 2), ("WeaponScatter", 1), ("PowerUpOverdrive", 2), ("PowerUpMagnet", 1), ("NovaBomb", 1));
            FieldTable("Nova2Pods", ("WeaponArc", 4), ("WeaponLaser", 1), ("Shield", 2), ("Repair", 2), ("PowerUpDrones", 1), ("NovaBomb", 1));
            FieldTable("Nova3Rocks", ("Repair", 3), ("Shield", 3), ("WeaponRail", 2), ("WeaponArc", 1), ("PowerUpOverdrive", 1), ("PowerUpChrono", 1), ("NovaBomb", 1));
            FieldTable("Nova3Pods", ("WeaponRail", 4), ("WeaponMissiles", 1), ("Shield", 2), ("Repair", 2), ("PowerUpDrones", 1), ("NovaBomb", 1));
            FieldTable("Nova4Rocks", ("Repair", 3), ("Shield", 3), ("WeaponRail", 1), ("WeaponArc", 1), ("WeaponMissiles", 1), ("PowerUpOverdrive", 1),
                ("PowerUpDrones", 1), ("PowerUpChrono", 1), ("NovaBomb", 1), ("ExtraShip", 1));
            FieldTable("Nova4Pods", ("WeaponRail", 2), ("WeaponArc", 2), ("WeaponMissiles", 1), ("Shield", 2), ("PowerUpDrones", 2), ("NovaBomb", 2), ("ExtraShip", 1));
            FieldTable("Gate1Rocks", ("Repair", 3), ("Shield", 3), ("WeaponFlak", 2), ("WeaponScatter", 1), ("PowerUpMagnet", 2), ("PowerUpOverdrive", 1), ("NovaBomb", 1));
            FieldTable("Gate1Pods", ("WeaponFlak", 4), ("WeaponLaser", 1), ("Shield", 2), ("Repair", 2), ("NovaBomb", 1));
            FieldTable("Gate2Rocks", ("Repair", 3), ("Shield", 3), ("WeaponFlak", 1), ("WeaponArc", 1), ("WeaponLaser", 1), ("PowerUpOverdrive", 1), ("PowerUpChrono", 1), ("NovaBomb", 1));
            FieldTable("Gate2Pods", ("WeaponArc", 2), ("WeaponFlak", 2), ("WeaponMissiles", 1), ("Shield", 2), ("PowerUpDrones", 1), ("NovaBomb", 1));
            FieldTable("Gate3Rocks", ("Repair", 3), ("Shield", 3), ("WeaponFlak", 2), ("WeaponRail", 1), ("PowerUpOverdrive", 1), ("PowerUpDrones", 1), ("NovaBomb", 1));
            FieldTable("Gate3Pods", ("WeaponFlak", 2), ("WeaponRail", 2), ("WeaponMissiles", 1), ("Shield", 2), ("PowerUpDrones", 1), ("PowerUpChrono", 1), ("NovaBomb", 1));
            FieldTable("Gate4Rocks", ("Repair", 3), ("Shield", 3), ("WeaponRail", 1), ("WeaponArc", 1), ("WeaponFlak", 1), ("WeaponMissiles", 1), ("PowerUpOverdrive", 1),
                ("PowerUpDrones", 1), ("PowerUpChrono", 1), ("NovaBomb", 1), ("ExtraShip", 1));
            FieldTable("Gate4Pods", ("WeaponRail", 2), ("WeaponArc", 1), ("WeaponFlak", 2), ("WeaponMissiles", 1), ("Shield", 2), ("PowerUpDrones", 2), ("NovaBomb", 2), ("ExtraShip", 1));
        }


        /// <summary>
        /// A drop table of the field campaigns. A pickup that does not exist (yet: the armory's new weapons come with their
        /// own branch) is left out with a note, so the table still works with what there is.
        /// </summary>
        private static void FieldTable(string name, params (string reward, int weight)[] entries)
        {
            var kept = new List<(Reward reward, int weight)>();
            foreach ((string reward, int weight) in entries)
            {
                var prefab = AsteroidsPrefabBuilder.Load<Reward>($"Pickups/{reward}");
                if (prefab == null)
                {
                    Debug.Log($"Asteroids: the drop table {name} leaves out {reward}, which is not built in this project.");
                    continue;
                }
                kept.Add((prefab, weight));
            }
            AsteroidsAssets.SaveScriptable<Loot>($"Config/Loot/{name}.asset", loot =>
            {
                loot.Rewards = kept.Select(entry => entry.reward).ToList();
                loot.Weights = kept.Select(entry => entry.weight).ToList();
            });
        }


        // ------------------------------------------------------------------ sector themes

        static partial void BuildFieldCampaignThemes()
        {
            void Sector(string name, string title, string accent, Color space, Color nebula, Color highlight, Color dust, float stars, string planet, Color atmosphere,
                Vector3 placement, float size, Vector3 tilt, bool rings, Color ring, Color sun, float sunIntensity, Vector3 sunAngles, Color ambient, Color motes)
            {
                AsteroidsAssets.SaveScriptable<SectorTheme>($"Config/Themes/{name}.asset", t =>
                {
                    t.title = title;
                    t.accent = Hex(accent);
                    t.spaceColor = space;
                    t.nebulaColor = nebula;
                    t.highlightColor = highlight;
                    t.dustColor = dust;
                    t.starDensity = stars;
                    t.planetMaterial = AsteroidsArtBuilder.FieldMaterial(planet);
                    t.cloudMaterial = null;
                    t.atmosphereColor = atmosphere;
                    t.planetPlacement = placement;
                    t.planetSize = size;
                    t.planetTilt = tilt;
                    t.rings = rings;
                    t.ringColor = ring;
                    t.sunColor = sun;
                    t.sunIntensity = sunIntensity;
                    t.sunAngles = sunAngles;
                    t.ambientColor = ambient;
                    t.motesColor = motes;
                });
            }

            // NOVA FRONTIER: the dying star's red drift, the blue ion storms, the grey iron shoals, the white-hot corona.
            Sector("Ember", "Ember Drift", "FF8A3D", new Color(0.025f, 0.008f, 0.004f), new Color(0.45f, 0.12f, 0.03f), new Color(1f, 0.55f, 0.15f),
                new Color(0.035f, 0.01f, 0.004f), 0.55f, "PlanetEmber", new Color(1.6f, 0.45f, 0.15f), new Vector3(0.93f, 0.9f, 210f), 125f, new Vector3(10f, 0f, 5f),
                false, Color.clear, new Color(1f, 0.7f, 0.45f), 1.7f, new Vector3(30f, 45f, 0f), new Color(0.26f, 0.14f, 0.1f), new Color(1f, 0.55f, 0.25f));
            Sector("IonReach", "Ion Reach", "6FA8FF", new Color(0.006f, 0.008f, 0.03f), new Color(0.1f, 0.14f, 0.45f), new Color(0.45f, 0.6f, 1.2f),
                new Color(0.01f, 0.012f, 0.04f), 0.8f, "PlanetIon", new Color(0.5f, 0.7f, 1.5f), new Vector3(0.15f, 0.78f, 160f), 105f, new Vector3(20f, 0f, -18f),
                false, Color.clear, new Color(0.8f, 0.88f, 1f), 1.45f, new Vector3(35f, -30f, 0f), new Color(0.14f, 0.16f, 0.3f), new Color(0.6f, 0.75f, 1f));
            Sector("IronShoals", "Iron Shoals", "B8C7D9", new Color(0.012f, 0.014f, 0.018f), new Color(0.16f, 0.2f, 0.24f), new Color(0.6f, 0.7f, 0.8f),
                new Color(0.02f, 0.024f, 0.03f), 0.9f, "PlanetIron", new Color(0.5f, 0.55f, 0.65f), new Vector3(0.82f, 0.2f, 150f), 85f, new Vector3(25f, 0f, 15f),
                true, new Color(0.65f, 0.68f, 0.72f, 0.5f), new Color(1f, 0.97f, 0.92f), 1.6f, new Vector3(40f, -40f, 0f), new Color(0.18f, 0.2f, 0.24f),
                new Color(0.75f, 0.8f, 0.9f));
            Sector("Corona", "Corona", "FFE08A", new Color(0.03f, 0.022f, 0.01f), new Color(0.55f, 0.38f, 0.12f), new Color(1.3f, 1.05f, 0.6f),
                new Color(0.04f, 0.03f, 0.012f), 0.4f, "PlanetCorona", new Color(1.2f, 0.7f, 0.25f), new Vector3(0.95f, 1.05f, 240f), 220f, Vector3.zero,
                false, Color.clear, new Color(1f, 0.92f, 0.75f), 2f, new Vector3(25f, 0f, 0f), new Color(0.3f, 0.25f, 0.16f), new Color(1f, 0.9f, 0.6f));

            // THE LAST GATE: the green-black graveyard, the violet reach of the stalkers, the living nebula, the golden gate.
            Sector("WreckRift", "Wreck Rift", "5FD6B0", new Color(0.004f, 0.014f, 0.012f), new Color(0.04f, 0.22f, 0.2f), new Color(0.3f, 0.85f, 0.7f),
                new Color(0.008f, 0.02f, 0.018f), 0.7f, "PlanetShattered", new Color(1.2f, 0.5f, 0.25f), new Vector3(0.15f, 0.25f, 150f), 110f, new Vector3(15f, 0f, 20f),
                true, new Color(0.5f, 0.45f, 0.4f, 0.55f), new Color(0.85f, 1f, 0.95f), 1.4f, new Vector3(35f, 30f, 0f), new Color(0.12f, 0.2f, 0.2f),
                new Color(0.5f, 1f, 0.85f));
            Sector("Phantom", "Phantom Reach", "B48CFF", new Color(0.012f, 0.006f, 0.022f), new Color(0.2f, 0.08f, 0.32f), new Color(0.6f, 0.9f, 0.75f),
                new Color(0.015f, 0.006f, 0.03f), 0.75f, "PlanetPhantom", new Color(0.6f, 1.2f, 0.9f), new Vector3(0.85f, 0.75f, 170f), 90f, new Vector3(30f, 0f, -10f),
                false, Color.clear, new Color(0.85f, 0.8f, 1f), 1.35f, new Vector3(40f, -35f, 0f), new Color(0.18f, 0.13f, 0.26f), new Color(0.75f, 0.6f, 1f));
            Sector("HydraNebula", "Hydra Nebula", "9CFF57", new Color(0.008f, 0.018f, 0.004f), new Color(0.15f, 0.36f, 0.04f), new Color(0.8f, 1f, 0.3f),
                new Color(0.015f, 0.03f, 0.005f), 0.6f, "PlanetHydra", new Color(0.8f, 1.5f, 0.3f), new Vector3(0.12f, 0.8f, 160f), 120f, new Vector3(18f, 0f, -25f),
                false, Color.clear, new Color(0.95f, 1f, 0.8f), 1.5f, new Vector3(30f, 40f, 0f), new Color(0.16f, 0.22f, 0.12f), new Color(0.7f, 1f, 0.4f));
            Sector("Gate", "The Gate", "FFC94A", new Color(0.012f, 0.009f, 0.004f), new Color(0.3f, 0.2f, 0.05f), new Color(1.1f, 0.85f, 0.35f),
                new Color(0.02f, 0.014f, 0.004f), 1.1f, "PlanetGate", new Color(1.4f, 1f, 0.3f), new Vector3(0.82f, 0.22f, 150f), 100f, new Vector3(60f, 0f, 15f),
                true, new Color(1.2f, 0.9f, 0.3f, 0.7f), new Color(1f, 0.92f, 0.75f), 1.5f, new Vector3(35f, -30f, 0f), new Color(0.2f, 0.17f, 0.1f),
                new Color(1f, 0.85f, 0.4f));
        }


        /// <summary>
        /// The eight sectors in the look of <paramref name="spec"/>, named like the Classic ones: a synthwave sky in each
        /// sector's colours and a neon globe for its star or world, each globe drawn in a colour of its own.
        /// </summary>
        static partial void BuildFieldCampaignThemeSectors(ThemeSpec spec)
        {
            Color H(string hex) => SpaceTextures.Hex(hex);
            var looks = new[]
            {
                (Look("Ember", "Ember Drift", spec.Orange, H("140602"), new Color(1f, 0.35f, 0.05f) * 2.2f, spec.Orange * 0.9f, spec.Orange * 1.5f, 0.6f,
                    new Vector3(0.9f, 0.86f, 190f), 170f, new Vector3(10f, 0f, 5f), false, spec.Orange, spec.Orange), spec.Orange),
                (Look("IonReach", "Ion Reach", Color.Lerp(spec.Cyan, spec.Violet, 0.4f), H("04051C"), spec.Cyan * 2f, spec.Violet, Color.Lerp(spec.Cyan, spec.Violet, 0.3f) * 1.7f,
                    0.9f, new Vector3(0.15f, 0.78f, 160f), 105f, new Vector3(20f, 0f, -18f), false, spec.Cyan, spec.Cyan), spec.Cyan),
                (Look("IronShoals", "Iron Shoals", Color.Lerp(spec.White, spec.Cyan, 0.3f), H("06080F"), spec.White * 1.8f, spec.Cyan * 0.6f, spec.White * 1.3f, 1.1f,
                    new Vector3(0.82f, 0.2f, 150f), 85f, new Vector3(25f, 0f, 15f), true, new Color(spec.White.r, spec.White.g, spec.White.b, 0.6f), spec.White), spec.White),
                (Look("Corona", "Corona", spec.Yellow, H("160E02"), spec.Yellow * 2.6f, spec.Orange, spec.Yellow * 1.5f, 0.5f,
                    new Vector3(0.5f, 1.02f, 230f), 260f, Vector3.zero, false, spec.Yellow, spec.Yellow), spec.Yellow),
                (Look("WreckRift", "Wreck Rift", spec.Green, H("02100C"), spec.Green * 1.8f, Color.Lerp(spec.Green, spec.Cyan, 0.5f) * 0.8f, spec.Green * 1.5f, 0.9f,
                    new Vector3(0.15f, 0.25f, 150f), 110f, new Vector3(15f, 0f, 20f), true, new Color(spec.Green.r, spec.Green.g, spec.Green.b, 0.6f), spec.Green), spec.Green),
                (Look("Phantom", "Phantom Reach", Color.Lerp(spec.Violet, spec.Magenta, 0.2f), H("0A0216"), Color.Lerp(spec.Green, spec.White, 0.4f) * 1.8f, spec.Violet * 1.1f,
                    spec.Violet * 1.9f, 0.9f, new Vector3(0.85f, 0.75f, 170f), 90f, new Vector3(30f, 0f, -10f), false, spec.Violet, spec.Violet), spec.Violet),
                (Look("HydraNebula", "Hydra Nebula", Color.Lerp(spec.Green, spec.Yellow, 0.4f), H("071002"), spec.Yellow * 2f, spec.Green * 0.9f,
                    Color.Lerp(spec.Green, spec.Yellow, 0.3f) * 1.6f, 0.8f, new Vector3(0.12f, 0.8f, 160f), 120f, new Vector3(18f, 0f, -25f), false, spec.Green, spec.Green),
                    spec.Green),
                (Look("Gate", "The Gate", Color.Lerp(spec.Yellow, spec.Orange, 0.3f), H("120A02"), Color.Lerp(spec.Yellow, spec.White, 0.3f) * 2.3f, spec.Magenta * 0.7f,
                    spec.Yellow * 1.6f, 1.2f, new Vector3(0.82f, 0.22f, 150f), 100f, new Vector3(60f, 0f, 15f), true, new Color(spec.Yellow.r, spec.Yellow.g, spec.Yellow.b, 0.7f),
                    spec.Yellow), spec.Yellow)
            };
            foreach ((SectorLook look, Color globe) in looks)
            {
                Material planet = NeonGlobe(spec, look.Name, globe);
                AsteroidsAssets.SaveScriptable<SectorTheme>($"{spec.ConfigFolder}/{look.Name}.asset", t =>
                {
                    t.title = look.Title;
                    t.accent = look.Accent;
                    t.spaceColor = look.Sky;
                    t.nebulaColor = look.Grid;
                    t.highlightColor = look.Sun;
                    t.dustColor = look.Glow;
                    t.starDensity = look.Stars;
                    t.planetMaterial = planet;
                    t.cloudMaterial = null;
                    t.atmosphereColor = globe * 1.2f;
                    t.planetPlacement = look.Placement;
                    t.planetSize = look.Size;
                    t.planetTilt = look.Tilt;
                    t.rings = look.Rings;
                    t.ringColor = look.Ring;
                    t.sunColor = Color.white;
                    t.sunIntensity = 1.2f;
                    t.sunAngles = new Vector3(35f, -40f, 0f);
                    t.ambientColor = new Color(0.2f, 0.16f, 0.3f);
                    t.motesColor = look.Motes;
                });
            }
        }


        private static SectorLook Look(string name, string title, Color accent, Color sky, Color sun, Color glow, Color grid, float stars, Vector3 placement, float size,
            Vector3 tilt, bool rings, Color ring, Color motes)
        {
            return new SectorLook
            {
                Name = name, Title = title, Accent = accent, Sky = sky, Sun = sun, Glow = glow, Grid = grid, Stars = stars, Placement = placement, Size = size, Tilt = tilt,
                Rings = rings, Ring = ring, Motes = motes
            };
        }


        /// <summary>A neon globe of <paramref name="spec"/> for a sector's star or world, its grid in <paramref name="edge"/> (as a theme draws the Classic planets).</summary>
        private static Material NeonGlobe(ThemeSpec spec, string name, Color edge)
        {
            Shader shader = AsteroidsArtBuilder.NeonSurfaceShader(spec);
            if (shader == null)
            {
                return null;
            }
            return AsteroidsAssets.SaveMaterial($"{spec.ArtFolder}/Materials/Field Globe {name}.mat", shader, m =>
            {
                m.SetColor("_EdgeColor", new Color(edge.r * spec.EdgeIntensity, edge.g * spec.EdgeIntensity, edge.b * spec.EdgeIntensity, 1f));
                m.SetColor("_FillColor", new Color(spec.Night.r + edge.r * 0.05f, spec.Night.g + edge.g * 0.05f, spec.Night.b + edge.b * 0.05f, 1f));
                m.SetFloat("_EdgeWidth", 1.2f);
                m.SetFloat("_Wire", 0f);
                m.SetFloat("_Grid", 1f);
                m.SetVector("_GridScale", new Vector4(24f, 12f, 0f, 0f));
                m.SetFloat("_Rim", 0.9f);
                m.SetFloat("_RimPower", 2.5f);
                m.SetFloat("_Pulse", 0.05f);
                m.SetColor("_EmissionColor", Color.black);
            });
        }
    }
}
