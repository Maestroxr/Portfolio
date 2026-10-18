using System;

namespace Portfolio.Asteroids
{
    /// <summary>
    /// The layers of the strike mode: what flies (air units, the ship, most shots) and what stands on the ground. A body
    /// is on one layer; a weapon's reach is a mask of the layers it can hit. The numbers travel: append only.
    /// </summary>
    [Flags]
    public enum Altitude : byte
    {
        /// <summary>Hits nothing (a bomb while it falls).</summary>
        None = 0,
        Air = 1,
        Ground = 2,
        Both = 3
    }


    /// <summary>
    /// The units of the strike mode. Air units are 0 to 15, ground units 16 and up (a ground unit's pool index is its
    /// value minus 16). The numbers travel as body variants: append only.
    /// </summary>
    public enum StrikeUnit : byte
    {
        /// <summary>A small fast fighter: aimed flak.</summary>
        Dart = 0,
        /// <summary>A light fighter with two guns firing straight down.</summary>
        Hornet = 1,
        /// <summary>A heavy bomber: angled bolts, then rockets.</summary>
        Bomber = 2,
        /// <summary>A rotor gunship: aimed bursts of flak.</summary>
        Gunship = 3,
        /// <summary>Comes up from behind the ship after a warning at the bottom edge.</summary>
        Interceptor = 4,
        /// <summary>Unarmed: dives at the nearest pilot.</summary>
        Kamikaze = 5,
        /// <summary>A big slow transport: spreads of bolts; always drops its event's pickup.</summary>
        Transport = 6,
        /// <summary>A small attack drone of the Iron Tide: flies in swarms, now and then a shot of flak.</summary>
        Drone = 7,
        /// <summary>A heavy missile cruiser of the Iron Tide: seeker rockets, then spreads of bolts.</summary>
        Cruiser = 8,
        /// <summary>A flying bug of the Hive: spits spores that drift after the pilot.</summary>
        Stinger = 9,
        /// <summary>A small bug of the Hive: unarmed, comes in swarms that dive at the pilot.</summary>
        Mite = 10,

        /// <summary>A gun emplacement: aimed flak.</summary>
        Turret = 16,
        /// <summary>A flak battery: aimed rockets.</summary>
        Flak = 17,
        /// <summary>Drives; its turret aims flak.</summary>
        Tank = 18,
        /// <summary>Drives; unarmed.</summary>
        Truck = 19,
        /// <summary>A heavy bunker: spreads of angled bolts.</summary>
        Bunker = 20,
        /// <summary>Unarmed; explodes and damages the ground units around it.</summary>
        FuelTank = 21,
        /// <summary>Unarmed; its dish spins.</summary>
        Radar = 22,
        /// <summary>Drives on water: aimed flak.</summary>
        Gunboat = 23,
        /// <summary>Unarmed; often holds a pickup.</summary>
        Hut = 24,
        /// <summary>A laser tower: a telegraphed beam.</summary>
        LaserTower = 25,
        /// <summary>A supply depot: explodes, damages the ground units around it and always drops its pickup.</summary>
        Depot = 26,
        /// <summary>A small ground container that holds an item pickup in the open.</summary>
        Crate = 27,
        /// <summary>A surface-to-air launcher: seeker rockets that turn after the pilot.</summary>
        SamSite = 28,
        /// <summary>A howitzer: shells that burst where the pilot was when it fired (the spot is marked).</summary>
        Artillery = 29,
        /// <summary>A walking gun platform: drives like a tank, bursts of flak from twin guns.</summary>
        Walker = 30,
        /// <summary>Unarmed: while it stands, the ground units around it cannot be hurt (its dome shows how far).</summary>
        ShieldGenerator = 31,
        /// <summary>A warship: drives on water, bursts of flak and rockets.</summary>
        Destroyer = 32,
        /// <summary>A hive nest: launches stingers while it is on screen, a few times.</summary>
        HiveSpawner = 33,
        /// <summary>An organic turret of the Hive: bursts of spores.</summary>
        Spitter = 34,
        /// <summary>A crystal pylon: glows, then fires a ring of crystal needles.</summary>
        Prism = 35
    }


    /// <summary>
    /// Everything the Supply Room sells and the pilot can own. Weapons 0 to 13 (0-2 always on, 3-13 specials), then the
    /// consumables and the equipment (14-17), then the armory's specials (18-23) and its equipment and consumables
    /// (24-29). The numbers are saved in the progress: append only.
    /// </summary>
    public enum StrikeItem : byte
    {
        /// <summary>The wing guns every pilot has; never sold, never lost. Also means "no item" in level data.</summary>
        MachineGun = 0,
        PlasmaCannon = 1,
        MicroMissiles = 2,
        Dumbfire = 3,
        MiniGun = 4,
        LaserTurret = 5,
        MissilePods = 6,
        AirMissiles = 7,
        GroundMissiles = 8,
        Bombs = 9,
        /// <summary>Cut: the value is kept, it is not for sale and never dropped.</summary>
        PowerDisrupter = 10,
        PulseCannon = 11,
        Deathray = 12,
        TwinLaser = 13,
        MegaBomb = 14,
        EnergyModule = 15,
        PhaseShield = 16,
        IonScanner = 17,
        /// <summary>A special: shells that burst into shrapnel near aircraft. Air only.</summary>
        FlakCannon = 18,
        /// <summary>A special: a canister that scatters bomblets over the ground. Ground only.</summary>
        ClusterBombs = 19,
        /// <summary>A special: canisters that leave the ground burning for a few seconds. Ground only.</summary>
        Napalm = 20,
        /// <summary>A special: a salvo of six small homing rockets. Air and ground.</summary>
        SwarmRockets = 21,
        /// <summary>A special: a spark whose lightning jumps from target to target. Air and ground.</summary>
        ArcLightning = 22,
        /// <summary>A special: a slug that goes through everything in its line. Air and ground.</summary>
        RailGun = 23,
        /// <summary>Equipment (up to 3): every plate takes a share off every hit.</summary>
        ArmorPlating = 24,
        /// <summary>Equipment: repairs energy slowly, firing or not, on every difficulty.</summary>
        RepairDrone = 25,
        /// <summary>Equipment: the ship flies faster.</summary>
        Afterburner = 26,
        /// <summary>Equipment: pickups and credits drift to the ship from far away.</summary>
        SalvageMagnet = 27,
        /// <summary>Equipment: two drones circle the ship and fire at the nearest enemy.</summary>
        WingmanDrones = 28,
        /// <summary>A consumable (up to 3): restores energy by itself when the energy runs low.</summary>
        RepairKit = 29
    }


    /// <summary>How an item is used: always firing, the selected special, used up, or carried.</summary>
    public enum ItemGroup : byte
    {
        AlwaysOn = 0,
        Special = 1,
        Consumable = 2,
        Equipment = 3
    }


    /// <summary>The screen-space paths air units fly (see <see cref="FlightPaths"/>). Append only.</summary>
    public enum FlightPath : byte
    {
        StraightDown = 0,
        DiveLeft = 1,
        Swoop = 2,
        Zigzag = 3,
        CrossLeft = 4,
        SweepDown = 5,
        HoverTop = 6,
        HoverMid = 7,
        RiseUp = 8,
        Arc = 9,
        Spiral = 10,
        Strafe = 11,
        Swarm = 12,
        Weave = 13,
        Hook = 14
    }


    /// <summary>What an air unit does at the end of its path.</summary>
    public enum FlightType : byte
    {
        /// <summary>Keeps its direction until it leaves the screen.</summary>
        Linear = 0,
        /// <summary>Loops the path from its repeat point and leaves after a while.</summary>
        Repeat = 1,
        /// <summary>Locks onto the nearest pilot once and dives at it.</summary>
        Kamikaze = 2
    }


    /// <summary>How the members of an event are placed around its reference point.</summary>
    public enum Formation : byte
    {
        Single = 0,
        /// <summary>One after the other along the path (air: delayed starts).</summary>
        Column = 1,
        /// <summary>Side by side.</summary>
        Line = 2,
        /// <summary>A V opening backwards.</summary>
        Vee = 3,
        /// <summary>Two side by side.</summary>
        Pair = 4
    }


    /// <summary>The kinds of terrain tile a strike theme provides (tile geometry: design section 4.1).</summary>
    public enum TerrainKind : byte
    {
        Plain = 0,
        Rough = 1,
        Road = 2,
        River = 3,
        Lake = 4,
        Coast = 5,
        Sea = 6,
        Base = 7,
        City = 8,
        Field = 9,
        /// <summary>Pack ice: floes in dark water, open leads between them.</summary>
        Ice = 10,
        /// <summary>Open sea with small islands.</summary>
        Islands = 11,
        /// <summary>A hive nest: organic mounds and brood pits.</summary>
        Nest = 12,
        /// <summary>A field of tall crystal clusters with lanes between them.</summary>
        Crystals = 13
    }


    /// <summary>The pilot's difficulty: what they take, how tough bosses are and which events a level spawns.</summary>
    public enum StrikeDifficulty : byte
    {
        Rookie = 0,
        Veteran = 1,
        Elite = 2
    }


    /// <summary>How a strike unit or boss part attacks.</summary>
    public enum AttackPattern : byte
    {
        None = 0,
        /// <summary>One shot at the nearest pilot.</summary>
        Aimed = 1,
        /// <summary>A burst of aimed shots.</summary>
        AimedBurst = 2,
        /// <summary>Straight down the screen.</summary>
        Down = 3,
        /// <summary>Two shots at plus and minus 45 degrees from down.</summary>
        Angled = 4,
        /// <summary>A fan of shots.</summary>
        Spread = 5,
        /// <summary>Rockets (aimed).</summary>
        Rockets = 6,
        /// <summary>A telegraphed beam.</summary>
        Laser = 7,
        /// <summary>Sky mines that drift down.</summary>
        Mines = 8,
        /// <summary>The big plasma balls of the field mode.</summary>
        HeavyPlasma = 9,
        /// <summary>A ring of shots in every direction, every other ring turned by half a step.</summary>
        Ring = 10,
        /// <summary>A small fan that sweeps from side to side over time (a hose of shots).</summary>
        Sweep = 11,
        /// <summary>Artillery shells that burst where the nearest pilot is when they are fired.</summary>
        Barrage = 12,
        /// <summary>Launches aircraft (the unit's <see cref="UnitLauncher"/>).</summary>
        Launch = 13
    }


    /// <summary>The projectiles of the pilot's strike weapons (pool index; ghost kind = 20 + value). Append only.</summary>
    public enum StrikeShotKind : byte
    {
        Bullet = 0,
        PlasmaBolt = 1,
        MicroMissile = 2,
        Dumbfire = 3,
        MiniGunRound = 4,
        PodMissile = 5,
        AirMissile = 6,
        GroundMissile = 7,
        Bomb = 8,
        /// <summary>Cut with the Power Disrupter; the value is kept.</summary>
        DisrupterOrb = 9,
        Pulse = 10,
        /// <summary>The flak cannon's shell; it bursts into <see cref="FlakShard"/>s.</summary>
        FlakShell = 11,
        /// <summary>A piece of a burst flak shell (each client bursts its own shells: never sent).</summary>
        FlakShard = 12,
        /// <summary>The cluster bombs' canister; it scatters <see cref="Bomblet"/>s.</summary>
        ClusterShell = 13,
        /// <summary>A bomblet of a cluster canister (never sent).</summary>
        Bomblet = 14,
        /// <summary>The napalm canister; it leaves a <see cref="NapalmFire"/> on the ground.</summary>
        NapalmCanister = 15,
        /// <summary>A burning patch of ground (never sent).</summary>
        NapalmFire = 16,
        SwarmRocket = 17,
        /// <summary>The arc lightning's spark; its lightning jumps on from what it hits.</summary>
        ArcBolt = 18,
        RailSlug = 19
    }


    /// <summary>What came of trying to buy an item in the Supply Room.</summary>
    public enum BuyResult : byte
    {
        Bought = 0,
        NoMoney = 1,
        Full = 2,
        NotForSale = 3
    }
}
