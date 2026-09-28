using System;
using System.Collections.Generic;
using Gamebox;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Portfolio.Asteroids
{
    /// <summary>
    /// The whole look of the Asteroids module in one asset: the ships of the hangar, the rocks, the shots, the enemies and
    /// bosses, the effects, the pickups, the space behind the playfield with its four sectors, the worlds of the strike
    /// mode, the interface with its icons, fonts and palette, and the shared menu. The typed groups are what the game's own
    /// code reads (<see cref="Ship"/>, <see cref="Asteroid"/>, <see cref="Sector"/>...); the base class's <see cref="GameTheme.Slots"/>
    /// hold the same references by key for the generic themed parts the builders put on every prefab and interface element
    /// (a renderer's key is the path of the material it was built with, so a theme replaces any material by name).
    /// </summary>
    [CreateAssetMenu(fileName = "AsteroidsTheme", menuName = "Asteroids/Theme", order = 6)]
    public class AsteroidsTheme : GameTheme
    {
        /// <summary>How a ship of the hangar looks: the model, its paint and size, the colour of its engines and its hangar picture.</summary>
        [Serializable]
        public class ShipLook
        {
            [Tooltip("The hull's display name (PlayerSettings.DisplayName).")]
            public string hull;
            public Mesh mesh;
            public Material material;
            public float scale = 0.12f;
            public Color engineColor = new Color(0.4f, 0.8f, 1f);
            [Tooltip("The picture on the hangar card.")]
            public Sprite preview;
        }


        /// <summary>The rocks of one kind: the shape variants, their material and the colour they flash when hit.</summary>
        [Serializable]
        public class AsteroidLook
        {
            public AsteroidKind kind;
            public Mesh[] meshes = new Mesh[0];
            public Material material;
            public Color flash = Color.white;
        }


        /// <summary>A material by the name of what it is on (a shot, an enemy, a boss part).</summary>
        [Serializable]
        public class NamedMaterial
        {
            public string name;
            public Material material;
        }


        /// <summary>A sprite by name (an icon).</summary>
        [Serializable]
        public class NamedSprite
        {
            public string name;
            public Sprite sprite;
        }


        /// <summary>A pickup: the glow of its core, the material of the icon floating in it and the icon the HUD shows.</summary>
        [Serializable]
        public class PickupLook
        {
            public string name;
            public Material core;
            public Material icon;
            public Sprite sprite;
        }


        /// <summary>The materials of the effects: particles, glows, flames and shields.</summary>
        [Serializable]
        public class EffectMaterials
        {
            public Material particleAdd;
            public Material particleSpark;
            public Material particleFlare;
            public Material particleShard;
            public Material particleSmoke;
            public Material particleRing;
            public Material glow;
            public Material flare;
            public Material ring;
            public Material dangerRing;
            public Material muzzle;
            public Material engineFlame;
            public Material bossFlame;
            public Material shipShield;
            public Material bossShield;
            public Material shipHalo;
        }


        /// <summary>What the space behind the playfield is drawn with.</summary>
        [Serializable]
        public class BackdropLook
        {
            [Tooltip("The far quad: nebula and stars (or whatever the theme puts there).")]
            public Material space;
            public Material atmosphere;
            public Material halo;
            public Material rings;
            public Material motes;
        }


        /// <summary>The sprites of the interface.</summary>
        [Serializable]
        public class InterfaceLook
        {
            public Sprite panel;
            public Sprite button;
            public Sprite frame;
            public Sprite hexagon;
            public Sprite hexagonFrame;
            public Sprite bar;
            public Sprite timerRing;
            public Sprite vignette;
            public Sprite fade;
            public Sprite lane;
            public Sprite glow;
            [Tooltip("A tiled overlay over the whole interface (scan lines); a clear sprite for none.")]
            public Sprite scanlines;
            [Tooltip("An overlay stretched over the whole interface (the dark corners of a screen); a clear sprite for none.")]
            public Sprite crt;
            public Sprite starFull;
            public Sprite starEmpty;
            [Tooltip("The weapon icons of the HUD, in the order of WeaponType.")]
            public Sprite[] weapons = new Sprite[0];
        }


        /// <summary>The colours of the interface.</summary>
        [Serializable]
        public class Palette
        {
            public Color panel = new Color(0.35f, 0.75f, 1f, 0.95f);
            public Color soft = new Color(0.78f, 0.86f, 0.96f);
            public Color dim = new Color(0.55f, 0.65f, 0.8f);
            public Color green = new Color(0.25f, 0.85f, 0.5f);
            public Color blue = new Color(0.3f, 0.6f, 1f);
            public Color orange = new Color(1f, 0.6f, 0.25f);
            public Color red = new Color(1f, 0.35f, 0.35f);
            public Color gold = new Color(1f, 0.82f, 0.3f);
            public Color cyan = new Color(0.35f, 0.9f, 1f);
            public Color strikeAccent = new Color(1f, 0.6f, 0.25f);
            public Color tabIdle = new Color(0.16f, 0.26f, 0.4f, 0.95f);
            public Color inputField = new Color(0.14f, 0.3f, 0.5f, 1f);
            public Color logoTop = new Color(0.75f, 0.97f, 1f);
            public Color logoBottom = new Color(0.25f, 0.6f, 1f);
            public Color settingsWindow = new Color(0.4f, 0.8f, 1f, 1f);
            public Color settingsRows = new Color(0.03f, 0.08f, 0.16f, 0.92f);
        }


        [Header("Playfield")]
        [SerializeField] internal List<ShipLook> ships = new List<ShipLook>();
        [SerializeField] internal List<AsteroidLook> asteroids = new List<AsteroidLook>();
        [Tooltip("The materials of the shots, by prefab name (PlayerBolt, PlayerLaser, EnemyPlasma...).")]
        [SerializeField] internal List<NamedMaterial> shots = new List<NamedMaterial>();
        [Tooltip("The hull materials of the enemies, hazards and bosses, by prefab name.")]
        [SerializeField] internal List<NamedMaterial> enemies = new List<NamedMaterial>();
        [SerializeField] internal EffectMaterials effects = new EffectMaterials();
        [SerializeField] internal List<PickupLook> pickups = new List<PickupLook>();

        [Header("Space")]
        [SerializeField] internal BackdropLook backdrop = new BackdropLook();
        [Tooltip("The sectors of the campaign in this look, named like the ones the missions refer to.")]
        [SerializeField] internal List<SectorTheme> sectors = new List<SectorTheme>();
        [Tooltip("The worlds of the strike mode in this look; a single one serves every world of the missions.")]
        [SerializeField] internal List<StrikeTheme> strikeThemes = new List<StrikeTheme>();

        [Header("Interface")]
        [SerializeField] internal InterfaceLook ui = new InterfaceLook();
        [Tooltip("The icons of Art/Icons by name.")]
        [SerializeField] internal List<NamedSprite> icons = new List<NamedSprite>();
        [Tooltip("The icons of the strike mode by name.")]
        [SerializeField] internal List<NamedSprite> strikeIcons = new List<NamedSprite>();
        [SerializeField] internal Palette palette = new Palette();

        private Dictionary<string, Material> materialIndex;
        private Dictionary<string, Mesh> meshIndex;
        private Dictionary<string, Sprite> spriteIndex;

        public override GameType Game => GameType.Asteroids;

        public IReadOnlyList<ShipLook> Ships => ships;

        public IReadOnlyList<AsteroidLook> Asteroids => asteroids;

        public EffectMaterials Effects => effects;

        public BackdropLook Backdrop => backdrop;

        public IReadOnlyList<SectorTheme> Sectors => sectors;

        public IReadOnlyList<StrikeTheme> StrikeThemes => strikeThemes;

        public InterfaceLook Interface => ui;

        public Palette Colors => palette;


        /// <summary>The look of the hull called <paramref name="hull"/>, or null.</summary>
        public ShipLook Ship(string hull)
        {
            foreach (ShipLook look in ships)
            {
                if (look != null && string.Equals(look.hull, hull, StringComparison.OrdinalIgnoreCase))
                {
                    return look;
                }
            }
            return null;
        }


        /// <summary>The look of the rocks of <paramref name="kind"/>, or null.</summary>
        public AsteroidLook Asteroid(AsteroidKind kind)
        {
            foreach (AsteroidLook look in asteroids)
            {
                if (look != null && look.kind == kind)
                {
                    return look;
                }
            }
            return null;
        }


        public Material Shot(string name)
        {
            return Named(shots, name);
        }


        public Material Enemy(string name)
        {
            return Named(enemies, name);
        }


        public PickupLook Pickup(string name)
        {
            foreach (PickupLook look in pickups)
            {
                if (look != null && string.Equals(look.name, name, StringComparison.OrdinalIgnoreCase))
                {
                    return look;
                }
            }
            return null;
        }


        /// <summary>An icon of Art/Icons by name, or null.</summary>
        public Sprite Icon(string name)
        {
            return Named(icons, name);
        }


        /// <summary>A strike icon by name (the item, the difficulty, the pickup), or null.</summary>
        public Sprite StrikeIcon(string name)
        {
            return Named(strikeIcons, name);
        }


        /// <summary>The HUD icon of <paramref name="weapon"/>, or null.</summary>
        public Sprite Weapon(WeaponType weapon)
        {
            int index = (int)weapon;
            return ui.weapons != null && index >= 0 && index < ui.weapons.Length ? ui.weapons[index] : null;
        }


        /// <summary>
        /// This theme's counterpart of <paramref name="of"/>, a sector a mission refers to: the listed sector with the same
        /// asset name, else the one at the same position among the sectors of the game's starting theme, else the first
        /// listed. <paramref name="of"/> itself when the theme lists no sectors.
        /// </summary>
        public SectorTheme Sector(SectorTheme of)
        {
            if (sectors == null || sectors.Count == 0)
            {
                return of;
            }
            if (of == null)
            {
                return sectors[0];
            }
            foreach (SectorTheme sector in sectors)
            {
                if (sector == of)
                {
                    return sector;
                }
            }
            foreach (SectorTheme sector in sectors)
            {
                if (sector != null && string.Equals(sector.name, of.name, StringComparison.OrdinalIgnoreCase))
                {
                    return sector;
                }
            }
            int index = IndexAmong(GameThemes.Available(GameType.Asteroids), of);
            if (index >= 0 && index < sectors.Count && sectors[index] != null)
            {
                return sectors[index];
            }
            return sectors[0] != null ? sectors[0] : of;
        }


        /// <summary>
        /// This theme's counterpart of <paramref name="of"/>, the world of a strike mission: the listed world with the same
        /// asset name, else the first listed (a theme with one world shows every mission on it). <paramref name="of"/>
        /// when the theme lists none.
        /// </summary>
        public StrikeTheme Strike(StrikeTheme of)
        {
            if (strikeThemes == null || strikeThemes.Count == 0)
            {
                return of;
            }
            foreach (StrikeTheme world in strikeThemes)
            {
                if (world == of)
                {
                    return world;
                }
            }
            if (of != null)
            {
                foreach (StrikeTheme world in strikeThemes)
                {
                    if (world != null && string.Equals(world.name, of.name, StringComparison.OrdinalIgnoreCase))
                    {
                        return world;
                    }
                }
            }
            return strikeThemes[0] != null ? strikeThemes[0] : of;
        }


        /// <summary>Where <paramref name="sector"/> stands among the sectors of the first listed theme of the game (-1 when nowhere).</summary>
        private static int IndexAmong(IReadOnlyList<GameTheme> themes, SectorTheme sector)
        {
            foreach (GameTheme theme in themes)
            {
                if (theme is AsteroidsTheme first && first.sectors != null)
                {
                    return first.sectors.IndexOf(sector);
                }
            }
            return -1;
        }


        private static Material Named(List<NamedMaterial> list, string name)
        {
            foreach (NamedMaterial entry in list)
            {
                if (entry != null && string.Equals(entry.name, name, StringComparison.OrdinalIgnoreCase))
                {
                    return entry.material;
                }
            }
            return null;
        }


        private static Sprite Named(List<NamedSprite> list, string name)
        {
            foreach (NamedSprite entry in list)
            {
                if (entry != null && string.Equals(entry.name, name, StringComparison.OrdinalIgnoreCase))
                {
                    return entry.sprite;
                }
            }
            return null;
        }

        // ------------------------------------------------------------------ keyed lookups

        /// <summary>The slots indexed for the pooled prefabs, which ask for their materials on every spawn.</summary>
        private void EnsureIndex()
        {
            if (materialIndex != null)
            {
                return;
            }
            materialIndex = Index(Slots.materials);
            meshIndex = Index(Slots.meshes);
            spriteIndex = Index(Slots.sprites);
        }

        private static Dictionary<string, T> Index<T>(List<ThemeSlot<T>> slots) where T : Object
        {
            var index = new Dictionary<string, T>(StringComparer.OrdinalIgnoreCase);
            foreach (ThemeSlot<T> slot in slots)
            {
                if (slot != null && !string.IsNullOrEmpty(slot.key) && slot.value != null)
                {
                    index[slot.key] = slot.value;
                }
            }
            return index;
        }

        /// <summary>Forgets the index of the slots (after the builder changed them).</summary>
        public void Reindex()
        {
            materialIndex = null;
            meshIndex = null;
            spriteIndex = null;
        }

        private void OnValidate()
        {
            Reindex();
        }

        public override Material MaterialOf(string key)
        {
            EnsureIndex();
            return !string.IsNullOrEmpty(key) && materialIndex.TryGetValue(key, out Material found) ? found : base.MaterialOf(key);
        }

        public override Mesh MeshOf(string key)
        {
            EnsureIndex();
            return !string.IsNullOrEmpty(key) && meshIndex.TryGetValue(key, out Mesh found) ? found : base.MeshOf(key);
        }

        public override Sprite SpriteOf(string key)
        {
            EnsureIndex();
            return !string.IsNullOrEmpty(key) && spriteIndex.TryGetValue(key, out Sprite found) ? found : base.SpriteOf(key);
        }

        // ------------------------------------------------------------------ validation

        /// <summary>
        /// The base check (every empty reference), less the two parts of a sector that are optional by design (a sector
        /// without a planet, a planet without clouds), plus the counts the game relies on: a look for every hull of the
        /// hangar and every kind of rock, at least one sector, and worlds that between them provide every kind of ground.
        /// </summary>
        public override bool Validate(List<string> problems)
        {
            int before = problems.Count;
            var found = new List<string>();
            base.Validate(found);
            foreach (string problem in found)
            {
                if (problem.EndsWith(".cloudMaterial is empty", StringComparison.Ordinal) || problem.EndsWith(".planetMaterial is empty", StringComparison.Ordinal))
                {
                    continue;
                }
                if (problem.Contains(".fonts.body.") || problem.Contains(".fonts.title.") || problem.Contains("].value."))
                {
                    // The inside of a font asset (its legacy atlas, its weights) is TextMesh Pro's business, not the theme's.
                    continue;
                }
                problems.Add(problem);
            }
            if (ships.Count == 0)
            {
                problems.Add($"{name}.ships has no hull.");
            }
            foreach (AsteroidKind kind in Enum.GetValues(typeof(AsteroidKind)))
            {
                AsteroidLook look = Asteroid(kind);
                if (look == null)
                {
                    problems.Add($"{name}.asteroids has no {kind} rocks.");
                }
                else if (look.meshes == null || look.meshes.Length == 0)
                {
                    problems.Add($"{name}.asteroids[{kind}] has no shapes.");
                }
            }
            if (sectors.Count == 0)
            {
                problems.Add($"{name}.sectors is empty.");
            }
            if (strikeThemes.Count == 0)
            {
                problems.Add($"{name}.strikeThemes is empty.");
            }
            else
            {
                foreach (TerrainKind kind in Enum.GetValues(typeof(TerrainKind)))
                {
                    bool provided = false;
                    foreach (StrikeTheme world in strikeThemes)
                    {
                        provided |= world != null && world.Provides(kind);
                    }
                    if (!provided)
                    {
                        problems.Add($"{name}.strikeThemes provide no {kind} ground.");
                    }
                }
            }
            if (ui.weapons == null || ui.weapons.Length < Enum.GetValues(typeof(WeaponType)).Length)
            {
                problems.Add($"{name}.ui.weapons has fewer icons than there are weapons.");
            }
            return problems.Count == before;
        }
    }
}
