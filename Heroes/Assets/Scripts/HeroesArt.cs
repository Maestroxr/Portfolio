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

        [Header("Interface")]
        public TMP_FontAsset titleFont;
        public TMP_FontAsset bodyFont;
        public Sprite frame;
        public Sprite panel;
        public Sprite parchment;
        public Sprite button;
        public Sprite buttonHover;
        public Sprite buttonPressed;
        public Sprite slot;
        public Sprite bar;
        public Sprite round;
        public Sprite glow;
        public Sprite star;
        public Sprite starEmpty;
        public Sprite banner;
        public Sprite titleArt;
        public Sprite[] resourceIcons = new Sprite[7];
        public Sprite[] statIcons = new Sprite[4];
        public Sprite[] skillIcons = new Sprite[13];
        public Sprite[] spellIcons = new Sprite[17];
        public Sprite[] artifactIcons = new Sprite[24];
        /// <summary>Icons of the interface: end turn, next hero, sleep, spellbook, kingdom, menu, move, attack...</summary>
        public List<Sprite> icons = new List<Sprite>();
        public List<string> iconNames = new List<string>();
        public Texture2D[] cursors = new Texture2D[0];

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
