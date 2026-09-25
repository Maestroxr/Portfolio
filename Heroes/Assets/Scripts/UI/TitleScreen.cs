using System.Collections.Generic;
using Gamebox;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Portfolio.Heroes.UI
{
    /// <summary>
    /// The first screen of the game: a small living valley behind it (<see cref="TitleDiorama"/>), the name of the game
    /// in its decorative capitals, and the ways in: Continue, the campaign, the skirmish maps, the online lobby, the
    /// settings, the credits and the way out. It takes the place of the shared menu while the game is not running.
    /// </summary>
    public sealed class TitleScreen : MonoBehaviour
    {
        private const float ButtonWidth = 440f;
        private const float ButtonHeight = 64f;
        private const float Left = 110f;
        private const float Column = 820f;

        private HeroesGameManager manager;
        private HeroesUI ui;
        private CanvasGroup group;
        private CanvasGroup words;
        private bool inSession;
        private RectTransform menu;
        private Button resume;
        private Tooltip resumeTip;
        private Button online;
        private TitleDiorama diorama;
        private Texture2D ramp;
        private float shownAt;

        public bool IsShown => gameObject.activeSelf;

        /// <summary>The valley behind the title while it shows (null otherwise).</summary>
        public TitleDiorama Diorama => diorama;

        public static TitleScreen Make(RectTransform parent, RectTransform safe, HeroesGameManager manager, HeroesUI ui)
        {
            // The shading reaches the edges of the screen; the words and buttons keep inside its safe area.
            RectTransform root = UIKit.Stretch(UIKit.Rect(parent, "Title"));
            var title = root.gameObject.AddComponent<TitleScreen>();
            title.manager = manager;
            title.ui = ui;
            title.group = root.gameObject.AddComponent<CanvasGroup>();
            title.Build(root, safe);
            root.SetAsFirstSibling();
            root.gameObject.SetActive(false);
            return title;
        }

        private void Build(RectTransform root, RectTransform safe)
        {
            HeroesArt art = manager.Art;
            // Dark on the left, where the words are, fading out over the valley; darker toward the edges.
            Image vignette = UIKit.Sprite(root, "Vignette", art.vignette, new Color(0f, 0f, 0f, 0.7f));
            UIKit.Stretch((RectTransform)vignette.transform);
            ramp = Ramp();
            var side = UIKit.Rect(root, "Shade").gameObject.AddComponent<RawImage>();
            side.texture = ramp;
            side.raycastTarget = false;
            side.color = new Color(0.02f, 0.012f, 0.006f, 1f);
            RectTransform sideRect = side.rectTransform;
            sideRect.anchorMin = Vector2.zero;
            sideRect.anchorMax = new Vector2(0f, 1f);
            sideRect.pivot = new Vector2(0f, 0.5f);
            sideRect.offsetMin = Vector2.zero;
            sideRect.offsetMax = new Vector2(1180f, 0f);

            RectTransform area = UIKit.Rect(root, "Safe");
            area.anchorMin = safe.anchorMin;
            area.anchorMax = safe.anchorMax;
            area.offsetMin = safe.offsetMin;
            area.offsetMax = safe.offsetMax;
            // The words give way to a window opened over the title (the campaign, the settings, the lobby).
            words = area.gameObject.AddComponent<CanvasGroup>();

            TextMeshProUGUI logo = UIKit.Logo(area, "Logo", "Heroes", 180f);
            UIKit.Pin((RectTransform)logo.transform, new Vector2(0f, 1f), new Vector2(Left, -34f), new Vector2(Column, 200f));
            UIKit.FitLine(logo, 180f, 110f);
            logo.alignment = TextAlignmentOptions.Center;
            logo.characterSpacing = 4f;
            TextMeshProUGUI subtitle = UIKit.Heading(area, "Subtitle", "The Shattered Crown", 40f);
            UIKit.Pin((RectTransform)subtitle.transform, new Vector2(0f, 1f), new Vector2(Left, -232f), new Vector2(Column, 52f));
            UIKit.FitLine(subtitle, 40f, 24f);
            subtitle.characterSpacing = 6f;
            RectTransform rule = UIKit.Divider(area, "Rule", 20f);
            UIKit.Pin(rule, new Vector2(0f, 1f), new Vector2(Left + 150f, -292f), new Vector2(Column - 300f, 20f));
            TextMeshProUGUI tagline = UIKit.Label(area, "Tagline", "Heroes, towns and armies on a land of hexagons", 25f,
                UIKit.Dim, TextAlignmentOptions.Center);
            tagline.fontStyle = FontStyles.Italic;
            UIKit.Pin((RectTransform)tagline.transform, new Vector2(0f, 1f), new Vector2(Left, -318f), new Vector2(Column, 36f));
            UIKit.FitLine(tagline, 25f, 16f);
            UIKit.Look(tagline, TextLook.Shadow);

            menu = UIKit.Rect(area, "Menu");
            UIKit.Pin(menu, new Vector2(0f, 1f), new Vector2(Left + (Column - ButtonWidth) * 0.5f, -384f), new Vector2(ButtonWidth, 7f * 76f));
            VerticalLayoutGroup layout = UIKit.Layout<VerticalLayoutGroup>(menu, 12f);
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childForceExpandWidth = true;

            resume = Entry("Continue", Continue, "The scenario you left, where you left it.");
            resumeTip = resume.GetComponent<Tooltip>();
            Entry("Campaign", () => ui.OpenCampaign(false), "The Shattered Crown\nEight chapters, one after the other.");
            Entry("Skirmish", () => ui.OpenCampaign(true), "Skirmish\nA single map against the computer.");
            online = Entry("Play Online", () => manager.OpenOnline(), "Play Online\nA room on the server, with players on other devices.");
            Entry("Settings", ui.ShowSettings, "Settings\nWhere battles are fought, speeds, sound, and the rules of a skirmish.");
            Entry("Credits", ui.OpenCredits, "Credits\nWho made what the game shows and plays.");
            Entry("Exit", () => manager.ExitGame(), "Exit\nLeave the game.");

            TextMeshProUGUI version = UIKit.Label(area, "Version", $"Version {Application.version}", 16f, new Color(0.72f, 0.67f, 0.56f, 0.6f),
                TextAlignmentOptions.BottomLeft);
            UIKit.Pin((RectTransform)version.transform, new Vector2(0f, 0f), new Vector2(24f, 16f), new Vector2(400f, 24f));
        }

        /// <summary>A texture that goes from opaque on its left to clear on its right, eased, for the shade behind the words.</summary>
        private static Texture2D Ramp()
        {
            const int width = 256;
            var texture = new Texture2D(width, 1, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var pixels = new Color32[width];
            for (int x = 0; x < width; x++)
            {
                float u = x / (width - 1f);
                float fade = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.38f, 1f, u));
                pixels[x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(255f * 0.93f * fade));
            }
            texture.SetPixels32(pixels);
            texture.Apply(false);
            return texture;
        }

        private Button Entry(string label, System.Action onClick, string tip)
        {
            Button button = UIKit.Push(menu, label, label, onClick, 30f);
            UIKit.Fit((RectTransform)button.transform, ButtonWidth, ButtonHeight);
            Tooltip.Attach(button.gameObject, tip);
            return button;
        }

        /// <summary>Shows the title over the valley, which is built (again) behind it.</summary>
        public void Show()
        {
            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
                shownAt = Time.unscaledTime;
                group.alpha = 0f;
            }
            transform.SetAsFirstSibling();
            if (diorama == null)
            {
                diorama = TitleDiorama.Build(manager.Art);
            }
            Refresh();
        }

        public void Hide()
        {
            gameObject.SetActive(false);
            if (diorama != null)
            {
                diorama.Tear();
                diorama = null;
            }
        }

        /// <summary>Continue shows only with a saved scenario to go on with; its tooltip names it.</summary>
        public void Refresh()
        {
            int level = SavedLevel();
            bool saved = level >= 0;
            resume.gameObject.SetActive(saved);
            if (saved)
            {
                HeroesLevel scenario = manager.Campaign is HeroesCampaign campaign ? campaign.Scenario(level) : null;
                resumeTip.Title = scenario != null ? $"Continue: {scenario.Title}" : "Continue";
                resumeTip.Text = "The scenario you left, where you left it.";
            }
            online.gameObject.SetActive(manager.Online != null);
        }

        /// <summary>
        /// The level whose save Continue takes up: the one saved last, or -1. The manager is pointed at it, which only
        /// decides which save the load reads.
        /// </summary>
        private int SavedLevel()
        {
            IStorageStrategy disk = manager.Storage?.SelectedStorage;
            if (disk == null || manager.InSession)
            {
                return -1;
            }
            int level = disk.GetInt(manager.LastSavedLevelKey, manager.LevelIndex);
            if (level < 0)
            {
                return -1;
            }
            if (manager.LevelIndex != level)
            {
                manager.LoadLevel(level);
            }
            return manager.DoesSaveGameExist() ? level : -1;
        }

        private void Continue()
        {
            if (SavedLevel() >= 0)
            {
                manager.LoadGame();
            }
        }

        private void Update()
        {
            if (group.alpha < 1f)
            {
                group.alpha = Mathf.Clamp01((Time.unscaledTime - shownAt) / 0.6f);
            }
            // The logo and the buttons would peek out around a window opened over them: they fade while one is.
            bool covered = Covered;
            words.alpha = Mathf.MoveTowards(words.alpha, covered ? 0f : 1f, Time.unscaledDeltaTime / 0.2f);
            words.interactable = !covered;
            words.blocksRaycasts = !covered;
            // Continue is not offered while a session of the lobby is in charge; it is again once the session ends.
            if (manager.InSession != inSession)
            {
                inSession = manager.InSession;
                Refresh();
            }
        }

        /// <summary>Whether a window lies over the title: the campaign, the credits, the settings or the online lobby.</summary>
        private bool Covered
        {
            get
            {
                if (ui.Campaign != null && ui.Campaign.IsOpen || ui.Message != null && ui.Message.IsOpen || ui.IsSettingsShown)
                {
                    return true;
                }
                HeroesOnlineController online = manager.Online;
                return online != null && online.LobbyUI != null && online.LobbyUI.IsOpen;
            }
        }

        private void OnDisable()
        {
            if (diorama != null)
            {
                diorama.Tear();
                diorama = null;
            }
        }

        private void OnDestroy()
        {
            if (ramp != null)
            {
                Destroy(ramp);
            }
        }
    }

    /// <summary>
    /// The valley behind the title: a patch of land made at runtime far from the map, with a castle town at the end of a
    /// track, pines and hills around it, a hero on his horse riding out of the town, two soldiers at its gate and a
    /// dragon watching from a knoll, all idling in their own animations. A camera of its own drifts slowly over it under
    /// the sky of the grasslands, looking at the town from the side the words of the title leave free.
    /// </summary>
    public sealed class TitleDiorama : MonoBehaviour
    {
        /// <summary>Far from the map, so the camera of the map never sees it.</summary>
        private static readonly Vector3 Place = new Vector3(-4000f, 0f, -4000f);
        /// <summary>
        /// How the valley is turned, so the sun of the scene falls on it from over the shoulder of the camera. A terrain
        /// cannot be turned, so the valley is laid out in this frame and the ground's heights are worked out in it.
        /// </summary>
        private static readonly Quaternion Frame = Quaternion.Euler(0f, 118f, 0f);
        private const float Size = 200f;

        /// <summary>Where the camera can stand and what it looks at, in the valley's frame (the first is the one used).</summary>
        private static readonly (Vector3 eye, Vector3 look)[] Views =
        {
            (new Vector3(-12f, 6.5f, -4f), new Vector3(4f, 3f, 22f)),
            (new Vector3(-6f, 9f, -8f), new Vector3(8f, 2.5f, 22f)),
            (new Vector3(-16f, 4.5f, 4f), new Vector3(6f, 3.5f, 24f)),
            (new Vector3(-3f, 6.2f, -17f), new Vector3(5.5f, 3.2f, 16f))
        };

        private static readonly Vector2 TownAt = new Vector2(10f, 27f);
        private static readonly Vector2 KnollAt = new Vector2(19f, 17f);

        private HeroesArt art;
        private Camera view;
        private Terrain ground;
        private Material skyBefore;
        private Vector3 eye;
        private Vector3 look;
        private readonly List<Vector2> road = new List<Vector2>();

        /// <summary>How many ways the camera can look at the valley (development tours compare them).</summary>
        public static int ViewCount => Views.Length;

        public static TitleDiorama Build(HeroesArt art)
        {
            var root = new GameObject("Title Valley");
            root.transform.SetPositionAndRotation(Place, Quaternion.identity);
            var diorama = root.AddComponent<TitleDiorama>();
            diorama.art = art;
            diorama.Make();
            return diorama;
        }

        /// <summary>Takes the valley away and gives the scene its own sky back.</summary>
        public void Tear()
        {
            RenderSettings.skybox = skyBefore;
            if (ground != null && ground.terrainData != null)
            {
                Destroy(ground.terrainData);
            }
            Destroy(gameObject);
        }

        /// <summary>Looks at the valley the <paramref name="index"/>th way.</summary>
        public void View(int index)
        {
            (Vector3 from, Vector3 to) = Views[Mathf.Clamp(index, 0, Views.Length - 1)];
            eye = from;
            look = to;
            Drift();
        }

        private void Make()
        {
            road.AddRange(new[]
            {
                new Vector2(-12f, -30f), new Vector2(-5f, -12f), new Vector2(1f, 0f), new Vector2(4.5f, 10f), new Vector2(8f, 19f),
                new Vector2(TownAt.x, TownAt.y - 2f)
            });
            Ground();
            Scenery();
            Figures();

            skyBefore = RenderSettings.skybox;
            HeroesArt.SkyArt sky = art != null ? art.Sky(TerrainType.Grass) : null;
            if (sky != null && sky.material != null)
            {
                RenderSettings.skybox = sky.material;
            }

            var cameraObject = new GameObject("Title Camera");
            cameraObject.transform.SetParent(transform, false);
            view = cameraObject.AddComponent<Camera>();
            Camera main = Camera.main;
            view.depth = (main != null ? main.depth : 0f) + 5f;
            view.clearFlags = CameraClearFlags.Skybox;
            view.fieldOfView = 38f;
            view.nearClipPlane = 0.3f;
            view.farClipPlane = 500f;
            View(0);
        }

        /// <summary>A point of the valley's frame, relative to the valley's place.</summary>
        private static Vector3 Turned(Vector3 local)
        {
            return Frame * local;
        }

        /// <summary>A point on the ground, from its place in the valley's frame.</summary>
        private Vector3 On(float x, float z)
        {
            Vector3 flat = Turned(new Vector3(x, 0f, z));
            float height = ground != null ? ground.SampleHeight(Place + flat) : 0f;
            return new Vector3(flat.x, height, flat.z);
        }

        /// <summary>The height of the ground, 0 to 1, at a point of the valley's frame.</summary>
        private float Relief(float wx, float wz)
        {
            float rise = Mathf.Clamp01((wz - 30f) / 40f);
            float sides = Mathf.Clamp01((Mathf.Abs(wx - 4f) - 26f) / 30f);
            float behind = Mathf.Clamp01((-wz - 30f) / 30f);
            float rolling = Mathf.PerlinNoise(wx * 0.045f + 11f, wz * 0.045f + 7f);
            float knoll = Mathf.Exp(-((wx - KnollAt.x) * (wx - KnollAt.x) + (wz - KnollAt.y) * (wz - KnollAt.y)) / 30f);
            float h = 0.02f + rolling * 0.04f + rise * rise * 0.55f + sides * sides * 0.35f + behind * 0.2f + knoll * 0.1f;
            return Mathf.Clamp01(h * FlatNear(wx, wz));
        }

        /// <summary>A point of the valley's frame under a point of the (unturned) ground, both relative to the valley's place.</summary>
        private static Vector2 Local(float x, float z)
        {
            Vector3 local = Quaternion.Inverse(Frame) * new Vector3(x, 0f, z);
            return new Vector2(local.x, local.z);
        }

        /// <summary>A patch of the map's own terrain: a meadow that rises into hills away from the camera, a track through it.</summary>
        private void Ground()
        {
            if (art == null || art.layers == null || art.layers.Length == 0)
            {
                return;
            }
            var data = new TerrainData { heightmapResolution = 257, alphamapResolution = 256, baseMapResolution = 512 };
            data.size = new Vector3(Size, 18f, Size);
            int n = data.heightmapResolution;
            var heights = new float[n, n];
            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    Vector2 local = Local(x / (float)(n - 1) * Size - Size * 0.5f, y / (float)(n - 1) * Size - Size * 0.5f);
                    heights[y, x] = Relief(local.x, local.y);
                }
            }
            data.SetHeights(0, 0, heights);
            data.terrainLayers = art.layers;
            int layers = art.layers.Length;
            int r = data.alphamapResolution;
            var splat = new float[r, r, layers];
            for (int y = 0; y < r; y++)
            {
                for (int x = 0; x < r; x++)
                {
                    Vector2 local = Local(x / (float)(r - 1) * Size - Size * 0.5f, y / (float)(r - 1) * Size - Size * 0.5f);
                    float wx = local.x;
                    float wz = local.y;
                    // A worn track of earth rather than the map's cobbles, which are too coarse this close.
                    float path = Mathf.Clamp01(1f - (RoadDistance(wx, wz) - 0.9f) / 1.4f);
                    float patches = Mathf.Clamp01((Mathf.PerlinNoise(wx * 0.08f, wz * 0.08f) - 0.66f) * 3f) * (1f - path);
                    float forest = Mathf.Clamp01((Mathf.Abs(wx - 4f) - 18f) / 6f) * (1f - path);
                    Weight(splat, y, x, (int)GroundLayer.Dirt, Mathf.Max(path, patches * 0.6f), layers);
                    Weight(splat, y, x, (int)GroundLayer.ForestFloor, forest * 0.7f, layers);
                    float rest = 1f;
                    for (int l = 0; l < layers; l++)
                    {
                        rest -= splat[y, x, l];
                    }
                    Weight(splat, y, x, (int)GroundLayer.Grass, Mathf.Max(0f, rest), layers);
                }
            }
            data.SetAlphamaps(0, 0, splat);

            var terrainObject = new GameObject("Ground");
            terrainObject.transform.SetParent(transform, false);
            terrainObject.transform.localPosition = new Vector3(-Size * 0.5f, 0f, -Size * 0.5f);
            ground = terrainObject.AddComponent<Terrain>();
            ground.terrainData = data;
            if (art.ground != null)
            {
                ground.materialTemplate = art.ground;
            }
            // Instanced terrain draws flat in a player build.
            ground.drawInstanced = false;
            ground.heightmapPixelError = 3f;
            ground.basemapDistance = 400f;
        }

        /// <summary>Flattens the ground where the town, the track and the figures stand.</summary>
        private float FlatNear(float x, float z)
        {
            float town = Mathf.Clamp01(((x - TownAt.x) * (x - TownAt.x) + (z - TownAt.y) * (z - TownAt.y)) / 120f);
            return Mathf.Lerp(0.3f, 1f, town);
        }

        private static void Weight(float[,,] splat, int y, int x, int layer, float value, int layers)
        {
            if (layer >= 0 && layer < layers)
            {
                splat[y, x, layer] = value;
            }
        }

        private float RoadDistance(float x, float z)
        {
            float best = float.MaxValue;
            var p = new Vector2(x, z);
            for (int i = 0; i + 1 < road.Count; i++)
            {
                Vector2 a = road[i];
                Vector2 b = road[i + 1];
                Vector2 ab = b - a;
                float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
                best = Mathf.Min(best, Vector2.Distance(p, a + ab * t));
            }
            return best;
        }

        /// <summary>The town at the end of the track, and pines around the meadow and on the hills behind it.</summary>
        private void Scenery()
        {
            if (art == null)
            {
                return;
            }
            var random = new System.Random(7);
            Put(art.Town(Faction.Castle, 0), new Vector3(TownAt.x, 0f, TownAt.y), 205f, 2f);

            // Forest all round the meadow and up the hills behind the town, thinning toward the horizon.
            GameObject[] pines = art.pineTrees.Length > 0 ? art.pineTrees : art.forestTrees;
            for (int i = 0; i < 260 && pines.Length > 0; i++)
            {
                float x = (float)random.NextDouble() * 140f - 66f;
                float z = (float)random.NextDouble() * 110f - 20f;
                if (Clear(x, z))
                {
                    continue;
                }
                Put(pines[random.Next(pines.Length)], new Vector3(x, 0f, z), random.Next(360), 0.9f + (float)random.NextDouble() * 0.7f);
            }
        }

        /// <summary>Where no tree may stand: the meadow before the town, the track, the knoll and the camera's near view.</summary>
        private bool Clear(float x, float z)
        {
            bool meadow = Mathf.Abs(x - 6f) < 17f && z > -6f && z < 36f;
            bool knoll = (new Vector2(x, z) - KnollAt).sqrMagnitude < 36f;
            bool near = false;
            foreach ((Vector3 from, Vector3 to) in Views)
            {
                var start = new Vector2(from.x, from.z);
                Vector2 toward = (new Vector2(to.x, to.z) - start).normalized;
                Vector2 offset = new Vector2(x, z) - start;
                float along = Vector2.Dot(offset, toward);
                float across = Mathf.Abs(offset.x * toward.y - offset.y * toward.x);
                near |= along > -4f && along < 16f && across < 6f + along * 0.5f;
            }
            return meadow || knoll || near || RoadDistance(x, z) < 3.5f;
        }

        private float RoadX(float z)
        {
            for (int i = 0; i + 1 < road.Count; i++)
            {
                if (z >= road[i].y && z <= road[i + 1].y)
                {
                    float t = (z - road[i].y) / (road[i + 1].y - road[i].y);
                    return Mathf.Lerp(road[i].x, road[i + 1].x, t);
                }
            }
            return road[road.Count - 1].x;
        }

        /// <summary>A hero of the red realm riding out of the town, soldiers at its gate, and a red dragon on the knoll.</summary>
        private void Figures()
        {
            HeroDef knight = null;
            foreach (HeroDef def in HeroData.AllHeroes)
            {
                if (def.Class == HeroClass.Knight)
                {
                    knight = def;
                    break;
                }
            }
            if (knight != null)
            {
                var riderObject = new GameObject("Hero");
                riderObject.transform.SetParent(transform, false);
                riderObject.transform.localPosition = On(RoadX(10f) + 0.3f, 10f);
                riderObject.transform.localRotation = Frame * Quaternion.Euler(0f, 222f, 0f);
                riderObject.transform.localScale = Vector3.one * 1.3f;
                riderObject.AddComponent<HeroView>().Setup(art, new HeroState { def = knight.Id, owner = 0, alive = true });
            }
            Unit(CreatureId.RedDragon, new Vector3(KnollAt.x, 0f, KnollAt.y), 238f, 1.25f);
            Unit(CreatureId.Swordsman, new Vector3(7.4f, 0f, 22.2f), 215f, 1.1f);
            Unit(CreatureId.Archer, new Vector3(11.6f, 0f, 22.6f), 225f, 1.1f);
        }

        private void Unit(CreatureId creature, Vector3 at, float yaw, float scale)
        {
            if (art == null || art.Unit(creature) == null || art.Unit(creature).creature != creature)
            {
                return;
            }
            var unitObject = new GameObject(creature.ToString());
            unitObject.transform.SetParent(transform, false);
            unitObject.transform.localPosition = On(at.x, at.z);
            unitObject.transform.localRotation = Frame * Quaternion.Euler(0f, yaw, 0f);
            unitObject.transform.localScale = Vector3.one * scale;
            unitObject.AddComponent<UnitView>().Setup(art, creature);
        }

        private void Put(GameObject prefab, Vector3 at, float yaw, float scale)
        {
            if (prefab == null)
            {
                return;
            }
            GameObject made = Instantiate(prefab, transform);
            made.transform.localPosition = On(at.x, at.z) - Vector3.up * 0.05f;
            made.transform.localRotation = Frame * Quaternion.Euler(0f, yaw, 0f);
            made.transform.localScale = prefab.transform.localScale * scale;
        }

        /// <summary>The camera sways slowly around what it looks at and rises and falls a little, in unscaled time.</summary>
        private void Drift()
        {
            if (view == null)
            {
                return;
            }
            float t = Time.unscaledTime;
            float sway = Mathf.Sin(t * 0.11f) * 5f;
            Vector3 offset = Quaternion.Euler(0f, sway, 0f) * (eye - look);
            Vector3 position = look + offset + new Vector3(0f, Mathf.Sin(t * 0.07f) * 0.6f, 0f);
            view.transform.localPosition = Turned(position);
            view.transform.localRotation = Frame * Quaternion.LookRotation(look - position + new Vector3(Mathf.Sin(t * 0.05f) * 0.5f, 0f, 0f));
        }

        private void LateUpdate()
        {
            Drift();
        }
    }
}
