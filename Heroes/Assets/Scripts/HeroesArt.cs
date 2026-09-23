using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Portfolio.Heroes
{
    public enum ProjectileKind
    {
        None,
        Arrow,
        Bolt,
        HolyLight,
        DeathCloud,
        Lightning,
        Fire
    }

    public enum Sfx
    {
        Click,
        Hover,
        Error,
        Gold,
        Resource,
        Treasure,
        Artifact,
        Build,
        Recruit,
        LevelUp,
        Flag,
        Footstep,
        Horse,
        BattleStart,
        Swing,
        Hit,
        Shoot,
        Death,
        Spell,
        Fireball,
        Lightning,
        Heal,
        Buff,
        Curse,
        Victory,
        Defeat,
        NewDay,
        NewWeek,
        Page,
        Coins
    }

    /// <summary>The terrain layers, in the order of the terrain's splat map.</summary>
    public enum GroundLayer
    {
        Grass = 0,
        Dirt = 1,
        Sand = 2,
        Snow = 3,
        Swamp = 4,
        Rough = 5,
        Wasteland = 6,
        Rock = 7,
        Road = 8,
        ForestFloor = 9
    }

    /// <summary>
    /// The pointers of the game, in <see cref="HeroesArt.cursors"/> order. The six strikes are the melee sword turned
    /// the way the blow goes on screen (a stack standing west of its target strikes east), with the hot spot on the tip.
    /// </summary>
    public enum CursorKind
    {
        /// <summary>The gauntlet: nothing in particular under the pointer.</summary>
        Default = 0,
        /// <summary>A pointing hand: something of the interface that answers a click.</summary>
        Hand = 1,
        /// <summary>Battle: the stack walks there.</summary>
        Move = 2,
        /// <summary>Battle: the stack flies there.</summary>
        Fly = 3,
        /// <summary>Battle: a melee blow with no side to it.</summary>
        Attack = 4,
        /// <summary>Battle: the stack shoots at what is under the pointer.</summary>
        Shoot = 5,
        /// <summary>Battle and map: a spell goes there.</summary>
        Cast = 6,
        /// <summary>Nothing can be done there.</summary>
        Blocked = 7,
        /// <summary>The computer or another player is thinking.</summary>
        Wait = 8,
        /// <summary>Look at what is under the pointer (a stack's card, an object's name).</summary>
        Info = 9,
        /// <summary>Map: the hero travels there.</summary>
        Travel = 10,
        /// <summary>Map: the hero visits what stands there (a town, a mine, a shrine).</summary>
        Visit = 11,
        /// <summary>Map: the hero attacks the army standing there.</summary>
        Fight = 12,
        /// <summary>Map: the hero picks up what lies there, or trades with a hero of his own.</summary>
        Take = 13,
        StrikeEast = 14,
        StrikeNorthEast = 15,
        StrikeNorthWest = 16,
        StrikeWest = 17,
        StrikeSouthWest = 18,
        StrikeSouthEast = 19
    }

    /// <summary>The looks of text the interface keeps a material for, see <see cref="HeroesArt.TextMaterial"/>.</summary>
    public enum TextLook
    {
        /// <summary>The font's own material.</summary>
        Plain = 0,
        /// <summary>Titles: gold with a dark outline and a shadow under it.</summary>
        Gold = 1,
        /// <summary>Body text over the map or a picture: a soft dark shadow around the letters.</summary>
        Shadow = 2,
        /// <summary>Body text that has to stand out anywhere: a dark outline.</summary>
        Outline = 3,
        /// <summary>The name of the game: the decorative capitals with a heavy outline and a glow.</summary>
        Logo = 4
    }

    /// <summary>
    /// Everything the game shows and plays, made by the art builder (Heroes > Build Everything) from the downloaded
    /// models, textures, fonts, icons, sounds and music, and referenced by the scene. The rules never look at it.
    /// </summary>
    [CreateAssetMenu(fileName = "HeroesArt", menuName = "Heroes/Art", order = 10)]
    public class HeroesArt : ScriptableObject
    {
        [Serializable]
        public sealed class UnitArt
        {
            public CreatureId creature;
            public GameObject prefab;
            /// <summary>Height of the model in meters, for the count badge and the camera of the portrait.</summary>
            public float height = 1.6f;
            public string idle = "Idle";
            public string walk = "Walk";
            public string attack = "Attack";
            public string shoot = "";
            public string hit = "Hit";
            public string death = "Death";
            public string cast = "";
            public ProjectileKind projectile;
            public bool flies;
            /// <summary>The creature at rest in a three quarter view, 256 pixels square, transparent around it.</summary>
            public Sprite portrait;
        }

        [Serializable]
        public sealed class HeroArt
        {
            public HeroClass heroClass;
            public GameObject rider;
            public GameObject mount;
            public string riderPose = "";
            public string riderCast = "";
            public string mountIdle = "Idle";
            public string mountWalk = "Walk";
            /// <summary>Where the rider sits on the mount, in the mount's space.</summary>
            public Vector3 seat = new Vector3(0f, 1f, 0f);
            /// <summary>Head and shoulders of the rider, 256 pixels square, transparent around them.</summary>
            public Sprite portrait;
            /// <summary>The rider on his mount, whole, 256 pixels square.</summary>
            public Sprite mounted;
        }

        [Serializable]
        public sealed class ObjectArt
        {
            public ObjectKind kind;
            /// <summary>-1 for every subtype, else the resource kind (mines, piles) or other subtype it is for.</summary>
            public int subtype = -1;
            public GameObject prefab;
        }

        [Serializable]
        public sealed class TownArt
        {
            public Faction faction;
            /// <summary>By player color, the fifth for a town nobody owns.</summary>
            public GameObject[] byColor = new GameObject[5];
            /// <summary>The town seen from the air in three quarters, by player color like <see cref="byColor"/>.</summary>
            public Sprite[] portraits = new Sprite[5];
        }

        /// <summary>A sky of the battlefield and the light that goes with it, so the field is lit like its sky.</summary>
        [Serializable]
        public sealed class SkyArt
        {
            public string name;
            /// <summary>A Skybox/Panoramic material over a latitude-longitude picture of the sky.</summary>
            public Material material;
            public Color sun = new Color(1f, 0.96f, 0.88f);
            public float sunIntensity = 1.4f;
            /// <summary>Euler angles of the sun's light (pitch, yaw), roughly where the picture has its brightest part.</summary>
            public Vector2 sunAngles = new Vector2(46f, 138f);
            public Color ambientSky = new Color(0.48f, 0.55f, 0.66f);
            public Color ambientEquator = new Color(0.40f, 0.42f, 0.40f);
            public Color ambientGround = new Color(0.24f, 0.21f, 0.17f);
            /// <summary>Fog toward the horizon, the color of the bottom of the sky.</summary>
            public Color fog = new Color(0.62f, 0.68f, 0.75f);
        }

        /// <summary>
        /// The looks of one obstacle of the battlefield on one ground: every prefab stands on the ground at its origin and
        /// fits inside one cell (2.08 m across), except the walls, which run across a cell from side to side.
        /// </summary>
        [Serializable]
        public sealed class ObstacleArt
        {
            public BattleObstacle kind;
            /// <summary>The <see cref="TerrainType"/> these looks are for, or -1 for any ground without looks of its own.</summary>
            public int terrain = -1;
            public GameObject[] variants = new GameObject[0];
        }

        /// <summary>Hills, mountains and woods that ring a battlefield of one ground, well outside the cells.</summary>
        [Serializable]
        public sealed class BackdropArt
        {
            public TerrainType terrain;
            public GameObject[] prefabs = new GameObject[0];
        }

        [Header("Creatures and heroes")]
        public List<UnitArt> units = new List<UnitArt>();
        public List<HeroArt> heroes = new List<HeroArt>();

        [Header("Map")]
        public List<ObjectArt> objects = new List<ObjectArt>();
        public List<TownArt> towns = new List<TownArt>();
        /// <summary>Banners by player color, the fifth white.</summary>
        public GameObject[] flags = new GameObject[5];
        /// <summary>The arrow towers a besieged town fights with, by the owner's color.</summary>
        public GameObject[] towers = new GameObject[5];
        public GameObject[] forestTrees = new GameObject[0];
        public GameObject[] pineTrees = new GameObject[0];
        public GameObject[] deadTrees = new GameObject[0];
        public GameObject[] mountains = new GameObject[0];
        public GameObject[] rocks = new GameObject[0];
        public GameObject[] waterPlants = new GameObject[0];
        public TerrainLayer[] layers = new TerrainLayer[10];
        /// <summary>The material the terrain itself is drawn with: without one it falls back to a shader no build has.</summary>
        public Material ground;
        public Material water;
        public Material fog;
        public Material marker;
        public Material ring;
        public GameObject arrow;
        public GameObject bolt;

        /// <summary>Soft round particles blended over what is behind them (dust, smoke).</summary>
        [Header("Effects")]
        public Material particleMaterial;
        /// <summary>Soft particles that add light (sparks, flames, orbs, lightning), tinted by the particle's color.</summary>
        public Material glowMaterial;

        [Header("Battlefield")]
        public List<SkyArt> skies = new List<SkyArt>();
        /// <summary>By <see cref="TerrainType"/>, the index in <see cref="skies"/> of the sky a battle on it is fought under.</summary>
        public int[] terrainSkies = new int[9];
        /// <summary>Index in <see cref="skies"/> of the evening sky, for a battle that wants a darker mood.</summary>
        public int duskSky = -1;
        public List<ObstacleArt> obstacles = new List<ObstacleArt>();
        public List<BackdropArt> backdrops = new List<BackdropArt>();

        [Header("Interface")]
        public TMP_FontAsset titleFont;
        public TMP_FontAsset bodyFont;
        /// <summary>The decorative capitals of the name of the game.</summary>
        public TMP_FontAsset logoFont;
        /// <summary>
        /// Materials of <see cref="titleFont"/>, <see cref="bodyFont"/> and <see cref="logoFont"/> by
        /// <see cref="TextLook"/> (the first of each is the font's own); see <see cref="TextMaterial"/>.
        /// </summary>
        public Material[] titleLooks = new Material[5];
        public Material[] bodyLooks = new Material[5];
        public Material[] logoLooks = new Material[5];
        /// <summary>
        /// The icons text can show inline: &lt;sprite name="gold"&gt; for every resource (gold, wood, ore, mercury,
        /// sulfur, crystal, gems), the primary skills (attack, defense, power, knowledge), and damage, health, speed,
        /// movement, mana, morale, luck, experience, star and star_empty. ★ and ☆ also draw as the stars.
        /// </summary>
        public TMP_SpriteAsset iconSprites;
        /// <summary>The window: dark leather inside a gold frame with ornaments in its corners.</summary>
        public Sprite frame;
        /// <summary>A section inside a window: a narrow dark gold rim, no ornaments.</summary>
        public Sprite panel;
        public Sprite parchment;
        public Sprite button;
        public Sprite buttonHover;
        public Sprite buttonPressed;
        public Sprite slot;
        /// <summary>A plain rounded white bar, tinted where it is used.</summary>
        public Sprite bar;
        /// <summary>The round button of an icon command; <see cref="roundHover"/> and <see cref="roundPressed"/> are its states.</summary>
        public Sprite round;
        /// <summary>A soft white halo, to put behind what is picked or hovered.</summary>
        public Sprite glow;
        public Sprite star;
        public Sprite starEmpty;
        /// <summary>A red pennant with a gold edge (old; see <see cref="pennant"/> for one in a player's color).</summary>
        public Sprite banner;
        public Sprite titleArt;

        /// <summary>A long bar along an edge of the screen: dark wood between thin gold bands (resources, commands).</summary>
        [Header("Interface kit")]
        public Sprite strip;
        /// <summary>A small card or a row of a list: a narrow gold rim on dark leather.</summary>
        public Sprite card;
        public Sprite cardHover;
        public Sprite cardSelected;
        /// <summary>A box sunk into a panel, for what must read at a glance: the date, a price, an amount.</summary>
        public Sprite inset;
        public Sprite buttonDisabled;
        public Sprite roundHover;
        public Sprite roundPressed;
        public Sprite slotHover;
        public Sprite slotSelected;
        /// <summary>The sunken trough of a bar inside its rim; <see cref="barFill"/> goes inside it, tinted.</summary>
        public Sprite barFrame;
        public Sprite barFill;
        public Sprite tooltip;
        /// <summary>
        /// Half of a gold rule: it fades in from the left and ends in half an ornament on the right, so two of them, the
        /// second mirrored, make a rule of any length with the ornament in its middle (see UIKit.Divider).
        /// </summary>
        public Sprite divider;
        /// <summary>The crimson banner a window's title is written on; its folded ends are its borders.</summary>
        public Sprite ribbon;
        /// <summary>A gold frame around a portrait, with nothing in its middle, and <see cref="portraitBack"/> behind it.</summary>
        public Sprite portraitFrame;
        public Sprite portraitFrameRound;
        /// <summary>The dark backdrop of a portrait, lighter in its middle; round for the round frame.</summary>
        public Sprite portraitBack;
        public Sprite portraitBackRound;
        /// <summary>A hanging pennant in white, tinted with a player's color, and the gold pole and trim drawn over it.</summary>
        public Sprite pennant;
        public Sprite pennantTrim;
        /// <summary>Black that fades out toward the top: behind a dialog, or over the bottom of the screen.</summary>
        public Sprite shade;
        /// <summary>Black that fades out toward the middle: around a screen that takes over.</summary>
        public Sprite vignette;
        public Sprite checkBox;
        public Sprite checkMark;
        /// <summary>The knob of a slider.</summary>
        public Sprite knob;
        /// <summary>
        /// How far in from each side of a kit sprite its content goes (left, bottom, right, top, in pixels at the
        /// reference 1920x1080), where that differs from the sprite's nine slice border: a frame whose corner ornaments
        /// need a wide border has a narrow band all the same. See <see cref="ContentInset"/>.
        /// </summary>
        public List<Sprite> insetSprites = new List<Sprite>();
        public List<Vector4> insets = new List<Vector4>();

        /// <summary>In color, by <see cref="ResourceKind"/>.</summary>
        [Header("Icons")]
        public Sprite[] resourceIcons = new Sprite[7];
        public Sprite[] statIcons = new Sprite[4];
        public Sprite[] skillIcons = new Sprite[13];
        public Sprite[] spellIcons = new Sprite[17];
        public Sprite[] artifactIcons = new Sprite[24];
        /// <summary>Icons of the interface: end turn, next hero, sleep, spellbook, kingdom, menu, move, attack...</summary>
        public List<Sprite> icons = new List<Sprite>();
        public List<string> iconNames = new List<string>();
        /// <summary>By <see cref="CursorKind"/>, with the point that clicks in <see cref="cursorHotspots"/>.</summary>
        public Texture2D[] cursors = new Texture2D[0];
        public Vector2[] cursorHotspots = new Vector2[0];

        /// <summary>Who made what the game shows and plays, and under which licence, for a credits panel.</summary>
        [Header("Credits")]
        public TextAsset credits;

        [Header("Sound")]
        public AudioClip menuMusic;
        public AudioClip[] adventureMusic = new AudioClip[0];
        public AudioClip[] battleMusic = new AudioClip[0];
        public AudioClip[] townMusic = new AudioClip[3];
        public AudioClip victoryMusic;
        public AudioClip defeatMusic;
        public AudioClip[] sounds = new AudioClip[Enum.GetValues(typeof(Sfx)).Length];

        public UnitArt Unit(CreatureId creature)
        {
            foreach (UnitArt unit in units)
            {
                if (unit.creature == creature)
                {
                    return unit;
                }
            }
            return units.Count > 0 ? units[0] : null;
        }

        public HeroArt Hero(HeroClass heroClass)
        {
            foreach (HeroArt hero in heroes)
            {
                if (hero.heroClass == heroClass)
                {
                    return hero;
                }
            }
            return heroes.Count > 0 ? heroes[0] : null;
        }

        public GameObject Object(ObjectKind kind, int subtype)
        {
            GameObject fallback = null;
            foreach (ObjectArt art in objects)
            {
                if (art.kind != kind)
                {
                    continue;
                }
                if (art.subtype == subtype)
                {
                    return art.prefab;
                }
                if (art.subtype < 0 && fallback == null)
                {
                    fallback = art.prefab;
                }
            }
            return fallback;
        }

        public GameObject Town(Faction faction, int color)
        {
            foreach (TownArt town in towns)
            {
                if (town.faction == faction)
                {
                    int index = color >= 0 && color < 4 ? color : 4;
                    return town.byColor[index] != null ? town.byColor[index] : town.byColor[0];
                }
            }
            return null;
        }

        public GameObject Flag(int color)
        {
            int index = color >= 0 && color < 4 ? color : 4;
            return flags.Length > index ? flags[index] : null;
        }

        public GameObject Tower(int color)
        {
            int index = color >= 0 && color < 4 ? color : 4;
            return towers.Length > index ? towers[index] : null;
        }

        public Sprite Icon(string name)
        {
            int index = iconNames.IndexOf(name);
            return index >= 0 && index < icons.Count ? icons[index] : null;
        }

        public Sprite Resource(ResourceKind kind)
        {
            int index = (int)kind;
            return index < resourceIcons.Length ? resourceIcons[index] : null;
        }

        public Sprite Spell(SpellId spell)
        {
            int index = (int)spell;
            return index >= 0 && index < spellIcons.Length ? spellIcons[index] : null;
        }

        public Sprite Skill(SkillId skill)
        {
            int index = (int)skill;
            return index >= 0 && index < skillIcons.Length ? skillIcons[index] : null;
        }

        public Sprite Artifact(ArtifactId artifact)
        {
            int index = (int)artifact;
            return index >= 0 && index < artifactIcons.Length ? artifactIcons[index] : null;
        }

        // ------------------------------------------------------------------ portraits

        public Sprite Portrait(CreatureId creature)
        {
            foreach (UnitArt unit in units)
            {
                if (unit.creature == creature)
                {
                    return unit.portrait;
                }
            }
            return null;
        }

        /// <summary>Head and shoulders of a hero of the class.</summary>
        public Sprite HeroPortrait(HeroClass heroClass)
        {
            HeroArt hero = Hero(heroClass);
            return hero != null ? hero.portrait : null;
        }

        /// <summary>A hero of the class on his mount, whole.</summary>
        public Sprite MountedPortrait(HeroClass heroClass)
        {
            HeroArt hero = Hero(heroClass);
            return hero != null ? hero.mounted : null;
        }

        /// <summary>A town of the faction in a player's color (0-3), or grey and bannerless for anything else.</summary>
        public Sprite TownPortrait(Faction faction, int color = 4)
        {
            int index = color >= 0 && color < 4 ? color : 4;
            TownArt fallback = null;
            foreach (TownArt town in towns)
            {
                if (town.faction == faction)
                {
                    return town.portraits != null && town.portraits.Length > index ? town.portraits[index] : null;
                }
                fallback = fallback ?? town;
            }
            return fallback != null && fallback.portraits != null && fallback.portraits.Length > index ? fallback.portraits[index] : null;
        }

        // ------------------------------------------------------------------ the interface

        /// <summary>
        /// The material of <paramref name="font"/> for a look: one of the presets the builder made for the title, body
        /// and logo fonts, or the font's own material for any other font or look.
        /// </summary>
        public Material TextMaterial(TMP_FontAsset font, TextLook look)
        {
            if (font == null)
            {
                return null;
            }
            Material[] looks = font == titleFont ? titleLooks : font == bodyFont ? bodyLooks : font == logoFont ? logoLooks : null;
            int index = (int)look;
            return looks != null && index > 0 && index < looks.Length && looks[index] != null ? looks[index] : font.material;
        }

        /// <summary>
        /// How far in from its left, bottom, right and top edges what sits on a sprite should start, in pixels of the
        /// reference resolution: the width of its frame. Nothing for no sprite.
        /// </summary>
        public Vector4 ContentInset(Sprite sprite)
        {
            if (sprite == null)
            {
                return Vector4.zero;
            }
            int index = insetSprites.IndexOf(sprite);
            return index >= 0 && index < insets.Count ? insets[index] : sprite.border;
        }

        public Texture2D Cursor(CursorKind kind)
        {
            int index = (int)kind;
            return index >= 0 && index < cursors.Length ? cursors[index] : null;
        }

        /// <summary>The pixel of <see cref="Cursor"/> that clicks, from its top left corner.</summary>
        public Vector2 CursorHotspot(CursorKind kind)
        {
            int index = (int)kind;
            return index >= 0 && index < cursorHotspots.Length ? cursorHotspots[index] : Vector2.zero;
        }

        /// <summary>Makes a pointer of the game the system's pointer (the default one if the kind has no picture).</summary>
        public void UseCursor(CursorKind kind)
        {
            if (Cursor(kind) == null)
            {
                kind = CursorKind.Default;
            }
            Texture2D texture = Cursor(kind);
            UnityEngine.Cursor.SetCursor(texture, texture != null ? CursorHotspot(kind) : Vector2.zero, CursorMode.Auto);
        }

        /// <summary>
        /// The melee pointer for a blow going in <paramref name="screenDirection"/> (from the cell the stack strikes from
        /// toward its target, in screen space, y up): the nearest of the six sides of a pointy topped hexagon.
        /// </summary>
        public static CursorKind Strike(Vector2 screenDirection)
        {
            if (screenDirection.sqrMagnitude < 1e-8f)
            {
                return CursorKind.Attack;
            }
            float angle = Mathf.Atan2(screenDirection.y, screenDirection.x) * Mathf.Rad2Deg;
            int side = Mathf.RoundToInt(((angle % 360f) + 360f) % 360f / 60f) % 6;
            return CursorKind.StrikeEast + side;
        }

        // ------------------------------------------------------------------ the battlefield

        /// <summary>The sky a battle on <paramref name="terrain"/> is fought under, with its light.</summary>
        public SkyArt Sky(TerrainType terrain)
        {
            int t = (int)terrain;
            int index = t >= 0 && t < terrainSkies.Length ? terrainSkies[t] : 0;
            return index >= 0 && index < skies.Count ? skies[index] : skies.Count > 0 ? skies[0] : null;
        }

        /// <summary>The evening sky, or the first one if there is none.</summary>
        public SkyArt Dusk => duskSky >= 0 && duskSky < skies.Count ? skies[duskSky] : skies.Count > 0 ? skies[0] : null;

        /// <summary>The skybox material (Skybox/Panoramic) of a battle on <paramref name="terrain"/>.</summary>
        public Material BattleSky(TerrainType terrain)
        {
            SkyArt sky = Sky(terrain);
            return sky != null ? sky.material : null;
        }

        /// <summary>
        /// The looks an obstacle has on a ground: its own if the ground has some, else those for any ground. Empty when
        /// there are none (<see cref="BattleObstacle.None"/>).
        /// </summary>
        public GameObject[] ObstacleVariants(BattleObstacle kind, TerrainType terrain)
        {
            GameObject[] any = null;
            foreach (ObstacleArt art in obstacles)
            {
                if (art.kind != kind || art.variants == null || art.variants.Length == 0)
                {
                    continue;
                }
                if (art.terrain == (int)terrain)
                {
                    return art.variants;
                }
                if (art.terrain < 0 && any == null)
                {
                    any = art.variants;
                }
            }
            return any ?? new GameObject[0];
        }

        /// <summary>One look of an obstacle on a ground; any number picks one, so a cell's index or a seed will do.</summary>
        public GameObject Obstacle(BattleObstacle kind, TerrainType terrain, int variant)
        {
            GameObject[] variants = ObstacleVariants(kind, terrain);
            if (variants.Length == 0)
            {
                return null;
            }
            int index = variant % variants.Length;
            return variants[index < 0 ? index + variants.Length : index];
        }

        /// <summary>The hills, mountains and woods that ring a battlefield of <paramref name="terrain"/>.</summary>
        public GameObject[] Backdrop(TerrainType terrain)
        {
            GameObject[] first = null;
            foreach (BackdropArt art in backdrops)
            {
                if (art.terrain == terrain)
                {
                    return art.prefabs;
                }
                first = first ?? art.prefabs;
            }
            return first ?? new GameObject[0];
        }

        // ------------------------------------------------------------------ sound

        public AudioClip Sound(Sfx sfx)
        {
            int index = (int)sfx;
            return index < sounds.Length ? sounds[index] : null;
        }

        /// <summary>The colors of the players: red, blue, green, tan, and grey for nobody.</summary>
        public static Color PlayerColor(int color)
        {
            switch (color)
            {
                case 0: return new Color(0.86f, 0.16f, 0.14f);
                case 1: return new Color(0.2f, 0.42f, 0.9f);
                case 2: return new Color(0.2f, 0.7f, 0.25f);
                case 3: return new Color(0.86f, 0.68f, 0.3f);
                default: return new Color(0.7f, 0.7f, 0.7f);
            }
        }
    }
}
