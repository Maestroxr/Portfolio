using System;
using System.Collections.Generic;
using Gamebox;
using UnityEngine;

namespace Portfolio.EndlessRunner
{
    /// <summary>
    /// The runner's model: one mesh per animated body part (pivot at the joint), the material they share and where the
    /// joints of the skeleton sit, so a theme can bring a runner of other proportions to the same
    /// <see cref="RunnerAnimator"/>.
    /// </summary>
    [Serializable]
    public class RunnerLook
    {
        [Header("Body parts, pivot at the joint")]
        public Mesh torso;
        public Mesh head;
        public Mesh upperArm;
        public Mesh forearm;
        public Mesh thigh;
        public Mesh shin;
        public Material material;

        [Header("Joints")]
        [Tooltip("Height of the hips above the ground in the rest pose.")]
        public float hipsHeight = 0.78f;
        [Tooltip("The neck joint, from the hips.")]
        public Vector3 neck = new Vector3(0f, 0.54f, 0f);
        [Tooltip("The right shoulder from the hips; the left one is mirrored.")]
        public Vector3 shoulder = new Vector3(0.265f, 0.46f, 0f);
        [Tooltip("The elbow below the shoulder.")]
        public float elbow = -0.25f;
        [Tooltip("How far the hip joints sit from the middle.")]
        public float hip = 0.11f;
        [Tooltip("The knee below the hip joint.")]
        public float knee = -0.36f;
    }


    /// <summary>The materials of the track pieces and of the runner's auras.</summary>
    [Serializable]
    public class PieceMaterials
    {
        [Tooltip("The palette every model is coloured through.")]
        public Material palette;
        public Material coin;
        public Material haloGem;
        public Material haloMagnet;
        public Material haloShield;
        public Material haloMultiplier;
        public Material haloSuperJump;
        public Material shieldRing;
    }


    /// <summary>The materials of the one-shot effects: sparkles, puffs, debris, confetti.</summary>
    [Serializable]
    public class ParticleMaterials
    {
        public Material sparkle;
        public Material glow;
        public Material soft;
        public Material smoke;
        public Material confetti;
        public Material debris;
    }


    /// <summary>Every sprite of the game's own canvas: the icons, the panel and button shapes, the overlays.</summary>
    [Serializable]
    public class InterfaceSprites
    {
        [Header("Icons")]
        public Sprite coin;
        public Sprite gem;
        public Sprite heart;
        public Sprite heartEmpty;
        public Sprite star;
        public Sprite starEmpty;
        public Sprite lockIcon;
        public Sprite magnet;
        public Sprite shield;
        public Sprite multiplier;
        public Sprite spring;
        public Sprite pause;
        public Sprite flag;
        public Sprite runner;
        public Sprite infinity;
        public Sprite play;
        public Sprite retry;
        public Sprite levels;
        public Sprite settings;
        public Sprite exit;
        public Sprite check;

        [Header("Shapes")]
        public Sprite panel;
        public Sprite button;
        public Sprite buttonHover;
        public Sprite buttonPressed;
        public Sprite buttonDisabled;
        public Sprite fade;
        public Sprite vignette;
        public Sprite glow;
        public Sprite ring;
        [Tooltip("Tiled over the whole screen in the grain colour: film grain when that colour is visible.")]
        public Sprite grain;
        [Tooltip("The full screen picture behind the pause menu.")]
        public Sprite backdrop;
    }


    /// <summary>The colours of the game's own canvas and of the pause menu's buttons.</summary>
    [Serializable]
    public class InterfaceColors
    {
        [Header("Palette")]
        public Color panel = new Color(0.06f, 0.08f, 0.2f, 0.86f);
        public Color accent = new Color(1f, 0.83f, 0.26f);
        public Color play = new Color(0.3f, 0.76f, 0.34f);
        public Color warning = new Color(0.98f, 0.58f, 0.2f);
        public Color info = new Color(0.26f, 0.55f, 0.95f);
        public Color danger = new Color(0.9f, 0.32f, 0.32f);
        public Color soft = new Color(0.84f, 0.87f, 0.95f);
        public Color badge = new Color(0.55f, 0.25f, 0.9f, 0.95f);
        public Color hint = new Color(0.7f, 0.74f, 0.85f);
        public Color locked = new Color(0.36f, 0.38f, 0.45f);
        public Color goals = new Color(1f, 0.93f, 0.7f);

        [Header("Title")]
        public Color logoEndlessTop = new Color(1f, 0.97f, 0.55f);
        public Color logoEndlessBottom = new Color(1f, 0.62f, 0.12f);
        public Color logoRunnerTop = new Color(0.55f, 0.95f, 1f);
        public Color logoRunnerBottom = new Color(0.15f, 0.5f, 1f);

        [Header("Overlays")]
        [Tooltip("A vignette over the whole screen; invisible with an alpha of zero.")]
        public Color overlay = new Color(0f, 0f, 0f, 0f);
        [Tooltip("The film grain over the whole screen; invisible with an alpha of zero.")]
        public Color grain = new Color(1f, 1f, 1f, 0f);
        public Color curtain = new Color(0.02f, 0.03f, 0.08f, 1f);

        [Header("Power-ups")]
        public Color magnet = new Color(1f, 0.35f, 0.35f);
        public Color shield = new Color(0.35f, 0.75f, 1f);
        public Color multiplier = new Color(0.75f, 0.45f, 1f);
        public Color superJump = new Color(0.4f, 1f, 0.45f);

        [Header("Pause menu buttons")]
        public Color menuResume = new Color(0.3f, 0.76f, 0.34f);
        public Color menuRestart = new Color(0.98f, 0.58f, 0.2f);
        public Color menuSettings = new Color(0.26f, 0.55f, 0.95f);
        public Color menuQuit = new Color(0.9f, 0.32f, 0.32f);

        [Header("Settings panel (an alpha of zero keeps the menu's own)")]
        [Tooltip("The words of the settings panel's own buttons (Back To Menu, Load Settings...).")]
        public Color menuText = new Color(1f, 1f, 1f, 0f);
        [Tooltip("The boxes of the settings fields.")]
        public Color field = new Color(1f, 1f, 1f, 0f);
        [Tooltip("The words typed into the settings fields.")]
        public Color fieldText = new Color(1f, 1f, 1f, 0f);

        public Color PowerUp(PowerUpType type)
        {
            switch (type)
            {
                case PowerUpType.Magnet: return magnet;
                case PowerUpType.Shield: return shield;
                case PowerUpType.Multiplier: return multiplier;
                case PowerUpType.SuperJump: return superJump;
                default: return Color.white;
            }
        }
    }


    /// <summary>What a theme calls a level of the campaign: its name and what the level select says about it.</summary>
    [Serializable]
    public class LevelWords
    {
        public string title;
        [TextArea] public string description;
    }


    /// <summary>A word the theme says differently in the hints, goals and tips ("the crates" become "the dumpsters").</summary>
    [Serializable]
    public class WordSwap
    {
        public string from;
        public string to;
    }


    /// <summary>
    /// The whole look of the Endless Runner in one asset: the worlds (biomes) the levels run through, the catalog of
    /// track pieces, the runner's model, the materials of the pieces and the effects, the sky, and every sprite, colour
    /// and font of the interface. The campaign keeps pointing at the biomes of the classic theme; another theme brings
    /// its own list, and <see cref="Resolve"/> maps a level's biome to the one in the same position (or of the same
    /// name) of the active theme. The pieces of every theme have the same names, lengths, kinds and colliders, so a
    /// race lays out the same track, coin for coin, whatever theme a device shows.
    /// </summary>
    [CreateAssetMenu(fileName = "RunnerGameTheme", menuName = "Endless Runner/Game Theme", order = 5)]
    public class RunnerGameTheme : GameTheme
    {
        [Header("World")]
        [Tooltip("The worlds in the order of the campaign: a level's classic world is swapped for the one in the same position.")]
        [SerializeField] internal List<RunnerTheme> biomes = new List<RunnerTheme>();
        [Tooltip("The track pieces of this theme; their names, lengths and colliders match the classic ones.")]
        [SerializeField] internal PieceCatalog pieces;
        [SerializeField] internal RunnerLook runner = new RunnerLook();
        [SerializeField] internal PieceMaterials materials = new PieceMaterials();
        [SerializeField] internal ParticleMaterials particles = new ParticleMaterials();
        [Tooltip("The sky material; its colours come from the world, the material decides the horizon.")]
        [SerializeField] internal Material sky;

        [Header("Interface")]
        [SerializeField] internal InterfaceSprites sprites = new InterfaceSprites();
        [SerializeField] internal InterfaceColors colors = new InterfaceColors();

        [Header("Words")]
        [Tooltip("The names and descriptions of the campaign's levels in this theme, in campaign order; an empty one keeps the level's own.")]
        [SerializeField] internal List<LevelWords> levels = new List<LevelWords>();
        [Tooltip("Words of the hints, goals and tips this theme says differently, applied in order.")]
        [SerializeField] internal List<WordSwap> words = new List<WordSwap>();

        public override GameType Game => GameType.EndlessRunner;

        public IReadOnlyList<RunnerTheme> Biomes => biomes;

        public PieceCatalog Pieces => pieces;

        public RunnerLook Runner => runner;

        public PieceMaterials Materials => materials;

        public ParticleMaterials Particles => particles;

        public Material Sky => sky;

        public InterfaceSprites Sprites => sprites;

        public InterfaceColors Colors => colors;

        /// <summary>The theme the Endless Runner shows now, or null when the game has none.</summary>
        public static RunnerGameTheme Active => GameThemes.Active<RunnerGameTheme>(GameType.EndlessRunner);

        /// <summary>
        /// The world of this theme that stands for <paramref name="reference"/>: the reference itself when it is one of
        /// this theme's worlds, else the world in the same position as the reference has in another listed theme, else
        /// the world of the same name, else the reference.
        /// </summary>
        public RunnerTheme Biome(RunnerTheme reference)
        {
            if (reference == null || biomes == null || biomes.Count == 0 || biomes.Contains(reference))
            {
                return reference;
            }
            foreach (GameTheme listed in GameThemes.Available(Game))
            {
                if (!(listed is RunnerGameTheme other) || other == this || other.biomes == null)
                {
                    continue;
                }
                int index = other.biomes.IndexOf(reference);
                if (index >= 0)
                {
                    return biomes[index % biomes.Count];
                }
            }
            foreach (RunnerTheme biome in biomes)
            {
                if (biome != null && (biome.displayName == reference.displayName || biome.name == reference.name))
                {
                    return biome;
                }
            }
            return reference;
        }

        /// <summary>The world the active theme shows for <paramref name="reference"/>; the reference itself without a theme.</summary>
        public static RunnerTheme Resolve(RunnerTheme reference)
        {
            RunnerGameTheme active = Active;
            return active != null ? active.Biome(reference) : reference;
        }

        private LevelWords WordsOf(RunnerLevel level)
        {
            return level != null && levels != null && level.Index >= 0 && level.Index < levels.Count ? levels[level.Index] : null;
        }

        /// <summary>The name of <paramref name="level"/> in this theme: its own unless the theme names it.</summary>
        public string TitleOf(RunnerLevel level)
        {
            LevelWords named = WordsOf(level);
            return named != null && !string.IsNullOrEmpty(named.title) ? named.title : level != null ? level.Title : string.Empty;
        }

        public string DescriptionOf(RunnerLevel level)
        {
            LevelWords named = WordsOf(level);
            return named != null && !string.IsNullOrEmpty(named.description) ? named.description : level != null ? level.Description : string.Empty;
        }

        /// <summary><paramref name="text"/> in this theme's words (see <see cref="words"/>).</summary>
        public string Say(string text)
        {
            if (string.IsNullOrEmpty(text) || words == null)
            {
                return text;
            }
            foreach (WordSwap swap in words)
            {
                if (swap != null && !string.IsNullOrEmpty(swap.from))
                {
                    text = text.Replace(swap.from, swap.to ?? string.Empty);
                }
            }
            return text;
        }

        /// <summary>The name of a level in the active theme.</summary>
        public static string TitleFor(RunnerLevel level)
        {
            RunnerGameTheme active = Active;
            return active != null ? active.TitleOf(level) : level != null ? level.Title : string.Empty;
        }

        public static string DescriptionFor(RunnerLevel level)
        {
            RunnerGameTheme active = Active;
            return active != null ? active.DescriptionOf(level) : level != null ? level.Description : string.Empty;
        }

        /// <summary><paramref name="text"/> in the words of the active theme.</summary>
        public static string Words(string text)
        {
            RunnerGameTheme active = Active;
            return active != null ? active.Say(text) : text;
        }

        public override bool Validate(List<string> problems)
        {
            bool complete = base.Validate(problems);
            if (biomes == null || biomes.Count == 0)
            {
                problems.Add($"{name}.biomes has no worlds");
                complete = false;
            }
            if (Fonts.body == null)
            {
                problems.Add($"{name}.fonts.body is empty");
                complete = false;
            }
            return complete;
        }
    }
}
