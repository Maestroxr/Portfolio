using System;
using System.Collections;
using System.Collections.Generic;
using Gamebox.Launcher;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace Portfolio.Heroes
{
    /// <summary>
    /// A battlefield of its own (<see cref="BattleStyle.Battlefield"/>), in a scene of its own: the HeroesBattle scene
    /// (a camera, a sun, a post processing volume and this component), loaded next to the game's scene while a battle
    /// is fought and unloaded after it. From the battle as it was deployed it builds the field: the ground of the land the
    /// armies met on, the fifteen by eleven hexagons, the obstacles of that land, a town's wall and gate, hills and woods
    /// around, the sky and the light of the land. The camera looks down on it from the south at the fixed angle of the
    /// old battle screens, the whole field and both heroes in view above the battle bar. When the scene is not in the
    /// build, a scene made in code gets the same component, which then makes its own camera, sun and volume.
    /// </summary>
    public sealed class BattlefieldScene : MonoBehaviour, IBattlefield
    {
        /// <summary>The name the scene is loaded by (the same wherever the game's files are mounted).</summary>
        public const string SceneName = "HeroesBattle";

        private const float Radius = HexLayout.DefaultRadius;
        private const float Pitch = 54f;
        private const float FieldOfView = 36f;
        private const float IntroLength = 2.4f;
        /// <summary>
        /// How much taller and thicker than the lengths of wall of the art (one across a cell, 1.2 m high) the wall of a
        /// town is stood; the builder makes the gatehouse to go with it.
        /// </summary>
        public const float WallHeight = 2f;
        public const float WallThickness = 1.3f;
        /// <summary>How far the wall runs on past the near and far ends of the field.</summary>
        private const float WallOverrun = 2.4f;
        /// <summary>The length of the stepped down, broken end of the wall beside a breach.</summary>
        private const float BrokenEnd = 1.1f;

        [SerializeField] private HeroesArt art;
        [SerializeField] private Camera view;
        [SerializeField] private Light sun;
        [SerializeField] private Volume volume;
        [Tooltip("The gatehouse in the wall of a town under siege, drawbridge and all, made at the size it stands: it runs along its x and faces the besiegers along its +z")]
        [SerializeField] private GameObject gate;
        [Tooltip("A heap of the wall's stones, standing on the ground at its origin: where an arrow tower fell, and at the broken ends of a breach")]
        [SerializeField] private GameObject ruin;
        [Tooltip("A few loose stones of the wall, strewn over the ground of a breach")]
        [SerializeField] private GameObject stones;
        [Tooltip("The banners the gatehouse hangs out by the color of the town's owner (red, blue, green, yellow), the fifth for nobody; each stands on its lower edge, the cloth across its x")]
        [SerializeField] private GameObject[] banners = new GameObject[0];

        private readonly HexGrid field = new HexGrid(BattleState.FieldColumns, BattleState.FieldRows);
        private Vector2 size;
        private Terrain terrain;
        private TerrainData ground;
        private BattlefieldGrid grid;
        private Transform props;
        private Transform units;
        private BattleState battle;
        private VolumeProfile madeProfile;
        private System.Random dice;
        private float noiseX;
        private float noiseZ;
        private Rect viewport = new Rect(0.015f, 0.27f, 0.97f, 0.715f);
        private Vector3 restPosition;
        private Quaternion restRotation;
        private Vector3 introPosition;
        private Quaternion introRotation;
        private float introStart = -1f;
        private Vector2Int framedFor;
        private Vector3 townSpot = new Vector3(float.NaN, 0f, 0f);
        /// <summary>Where the wall of a town runs down the field (its x), or NaN without one.</summary>
        private float wallLine = float.NaN;
        private int townColor = 4;
        private bool unloading;

        /// <summary>The scene on its way in, while <see cref="Load"/> waits for it.</summary>
        private static AsyncOperation loading;

        /// <summary>The field last handed over by <see cref="Load"/>, until it is unloaded.</summary>
        private static BattlefieldScene current;

        public Camera Camera => view;

        public Transform Root => units != null ? units : transform;

        public float CellWidth => Radius * 1.7320508f;

        public float UnitScale => 1.8f;

        public bool IsDragging => false;

        /// <summary>Whether a battlefield is on its way in (<see cref="Load"/> waits for the scene).</summary>
        public static bool Loading => loading != null && !loading.isDone;

        /// <summary>Whether the camera is still gliding in at the start of the battle.</summary>
        public bool InIntro => introStart >= 0f && Time.unscaledTime - introStart < IntroLength;

        // ------------------------------------------------------------------ loading and unloading

        /// <summary>
        /// Loads the HeroesBattle scene next to the ones open (or, when the build does not have it, makes a scene in code
        /// with a field of its own) and hands its field over once it is there.
        /// </summary>
        public static IEnumerator Load(HeroesArt art, Action<BattlefieldScene> ready)
        {
            BattlefieldScene found = null;
            if (GameLauncher.CanLoadScene(SceneName))
            {
                AsyncOperation operation = GameLauncher.LoadSceneAdditive(SceneName);
                loading = operation;
                while (operation != null && !operation.isDone)
                {
                    yield return null;
                }
                if (loading == operation)
                {
                    loading = null;
                }
                // The scene just loaded is the last one open; one of the same name may still be on its way out.
                Scene scene = SceneManager.GetSceneAt(SceneManager.sceneCount - 1);
                if (scene.name != SceneName)
                {
                    scene = SceneManager.GetSceneByName(SceneName);
                }
                if (scene.IsValid() && scene.isLoaded)
                {
                    foreach (GameObject root in scene.GetRootGameObjects())
                    {
                        found = root.GetComponentInChildren<BattlefieldScene>(true);
                        if (found != null)
                        {
                            break;
                        }
                    }
                    if (found == null)
                    {
                        Debug.LogWarning($"Heroes: the scene {SceneName} has no battlefield; one is made in code.");
                        yield return SceneManager.UnloadSceneAsync(scene);
                    }
                }
            }
            if (found == null)
            {
                Scene made = SceneManager.CreateScene($"{SceneName} (made)");
                var root = new GameObject("Battlefield");
                SceneManager.MoveGameObjectToScene(root, made);
                found = root.AddComponent<BattlefieldScene>();
            }
            if (found.art == null)
            {
                found.art = art;
            }
            found.Prepare();
            current = found;
            ready(found);
        }

        /// <summary>
        /// Takes down every battlefield that is up or on its way in, for a game that is being left while its battle
        /// opens or closes (the director that would have taken the field over, or unloaded it, was stopped): a scene
        /// still loading goes as soon as it is in, unless a newer battle has claimed a field by then. The game's own scene
        /// must be the active one again before this is called.
        /// </summary>
        public static void Abandon()
        {
            AsyncOperation pending = loading;
            loading = null;
            if (pending != null && !pending.isDone)
            {
                pending.completed += _ => Sweep(false);
            }
            Sweep(true);
        }

        /// <summary>Unloads the battlefields that are up, all of them or only those no battle has claimed.</summary>
        private static void Sweep(bool claimedToo)
        {
            for (int i = SceneManager.sceneCount - 1; i >= 0; i--)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                // The scene of the build, and one made in code in its place.
                if (!scene.isLoaded || !scene.name.StartsWith(SceneName, StringComparison.Ordinal))
                {
                    continue;
                }
                BattlefieldScene field = null;
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    field = root.GetComponentInChildren<BattlefieldScene>(true);
                    if (field != null)
                    {
                        break;
                    }
                }
                if (field == null)
                {
                    SceneManager.UnloadSceneAsync(scene);
                }
                else if (!field.unloading && (claimedToo || field != current))
                {
                    field.Unload();
                }
            }
        }

        /// <summary>
        /// Takes the field down: the terrain data made for it goes, then the scene. The game's own scene must be the
        /// active one again before this is called.
        /// </summary>
        public AsyncOperation Unload()
        {
            if (unloading)
            {
                return null;
            }
            unloading = true;
            if (current == this)
            {
                current = null;
            }
            if (terrain != null)
            {
                Destroy(terrain.gameObject);
                terrain = null;
            }
            if (ground != null)
            {
                Destroy(ground);
                ground = null;
            }
            if (madeProfile != null)
            {
                Destroy(madeProfile);
                madeProfile = null;
            }
            Scene scene = gameObject.scene;
            return scene.IsValid() && scene.isLoaded ? SceneManager.UnloadSceneAsync(scene) : null;
        }

        /// <summary>The camera, the sun and the grading the scene holds, made here when it was made in code.</summary>
        private void Prepare()
        {
            if (view == null)
            {
                var cameraObject = new GameObject("Battle Camera", typeof(Camera));
                cameraObject.transform.SetParent(transform, false);
                view = cameraObject.GetComponent<Camera>();
                Configure(view);
            }
            if (sun == null)
            {
                var sunObject = new GameObject("Battle Sun");
                sunObject.transform.SetParent(transform, false);
                sun = sunObject.AddComponent<Light>();
                sun.type = LightType.Directional;
                sun.shadows = LightShadows.Soft;
                sun.shadowStrength = 0.75f;
            }
            if (volume == null)
            {
                var post = new GameObject("Battle Grading");
                post.transform.SetParent(transform, false);
                volume = post.AddComponent<Volume>();
                volume.isGlobal = true;
                madeProfile = ScriptableObject.CreateInstance<VolumeProfile>();
                Grade(madeProfile);
                volume.sharedProfile = madeProfile;
            }
            // Only the field is drawn: the fog of the map (its own layer) is not the battle's.
            view.cullingMask &= ~(1 << FogOfWar.Layer);
        }

        /// <summary>How the camera of a battlefield is set up, in the scene the builder saves and in one made in code.</summary>
        public static void Configure(Camera camera)
        {
            camera.clearFlags = CameraClearFlags.Skybox;
            camera.backgroundColor = new Color(0.35f, 0.45f, 0.55f);
            camera.fieldOfView = FieldOfView;
            camera.nearClipPlane = 0.5f;
            camera.farClipPlane = 420f;
            camera.cullingMask = ~(1 << FogOfWar.Layer);
            UniversalAdditionalCameraData data = camera.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = true;
            data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            data.renderShadows = true;
        }

        /// <summary>
        /// The grading of a battlefield: a touch of bloom on the bright metal and magic, a little more contrast and a
        /// little less color than the bright map, and darker corners that keep the eye on the field.
        /// </summary>
        public static void Grade(VolumeProfile profile)
        {
            Bloom bloom = profile.TryGet(out Bloom existing) ? existing : profile.Add<Bloom>();
            bloom.active = true;
            bloom.threshold.Override(1.05f);
            bloom.intensity.Override(0.45f);
            bloom.scatter.Override(0.65f);
            ColorAdjustments color = profile.TryGet(out ColorAdjustments existingColor) ? existingColor : profile.Add<ColorAdjustments>();
            color.active = true;
            color.contrast.Override(14f);
            color.saturation.Override(-8f);
            color.postExposure.Override(-0.12f);
            Vignette vignette = profile.TryGet(out Vignette existingVignette) ? existingVignette : profile.Add<Vignette>();
            vignette.active = true;
            vignette.intensity.Override(0.24f);
            vignette.smoothness.Override(0.45f);
            vignette.color.Override(new Color(0.08f, 0.05f, 0.03f));
        }

        // ------------------------------------------------------------------ building the field

        /// <summary>
        /// Lays out the field of <paramref name="shown"/> (the battle as it was deployed): ground, sky and light, grid,
        /// obstacles, a town's wall and gate, and what stands around. The scene becomes the active one, so its sky and
        /// light are the ones drawn. <paramref name="town"/> is the besieged town's look, stood behind the defenders, and
        /// <paramref name="color"/> the color of its owner (as a number, 4 for nobody), which its gatehouse flies.
        /// </summary>
        public void Build(BattleState shown, GameObject town, int color = 4)
        {
            battle = shown;
            townColor = color;
            SceneManager.SetActiveScene(gameObject.scene);
            field.Size(out double width, out double depth);
            size = new Vector2((float)width * Radius, (float)depth * Radius);
            dice = new System.Random((int)(shown.fieldSeed & 0x7fffffff));
            noiseX = shown.fieldSeed % 997 * 0.37f;
            noiseZ = shown.fieldSeed / 997 % 991 * 0.41f;
            var land = (TerrainType)shown.terrain;

            props = new GameObject("Props").transform;
            props.SetParent(transform, false);
            units = new GameObject("Units").transform;
            units.SetParent(transform, false);

            Sky(land);
            Ground(land);
            Grid(land);
            Obstacles(land);
            Walls();
            Town(town);
            Surroundings(land);
            Frame();
        }

        private void Sky(TerrainType land)
        {
            HeroesArt.SkyArt sky = art != null ? art.Sky(land) : null;
            if (sky != null && sky.material != null)
            {
                RenderSettings.skybox = sky.material;
                view.clearFlags = CameraClearFlags.Skybox;
            }
            else
            {
                view.clearFlags = CameraClearFlags.SolidColor;
            }
            Color sunColor = sky != null ? sky.sun : new Color(1f, 0.96f, 0.88f);
            sun.color = sunColor;
            sun.intensity = sky != null ? sky.sunIntensity : 1.4f;
            Vector2 angles = sky != null ? sky.sunAngles : new Vector2(46f, 138f);
            sun.transform.rotation = Quaternion.Euler(angles.x, angles.y, 0f);
            sun.shadows = LightShadows.Soft;
            RenderSettings.sun = sun;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = sky != null ? sky.ambientSky : new Color(0.48f, 0.55f, 0.66f);
            RenderSettings.ambientEquatorColor = sky != null ? sky.ambientEquator : new Color(0.40f, 0.42f, 0.40f);
            RenderSettings.ambientGroundColor = sky != null ? sky.ambientGround : new Color(0.24f, 0.21f, 0.17f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = sky != null ? sky.fog : new Color(0.62f, 0.68f, 0.75f);
            RenderSettings.fogStartDistance = 75f;
            RenderSettings.fogEndDistance = 290f;
            view.backgroundColor = RenderSettings.fogColor;
            DynamicGI.UpdateEnvironment();
        }

        private void Ground(TerrainType land)
        {
            var corner = new Vector3(-52f, -BattlefieldGround.Base, -34f);
            var extent = new Vector2(size.x + 104f, size.y + 96f);
            ground = BattlefieldGround.Build(art, land, battle.fieldSeed, size, corner, extent);
            var terrainObject = new GameObject("Ground");
            terrainObject.transform.SetParent(transform, false);
            terrainObject.transform.position = corner;
            terrain = terrainObject.AddComponent<Terrain>();
            terrain.terrainData = ground;
            if (art != null && art.ground != null)
            {
                terrain.materialTemplate = art.ground;
            }
            // Instanced terrain needs shader variants a player build strips, and comes out flat and blank.
            terrain.drawInstanced = false;
            terrain.heightmapPixelError = 3f;
            terrain.basemapDistance = 500f;
            terrain.shadowCastingMode = ShadowCastingMode.On;
        }

        private void Grid(TerrainType land)
        {
            var gridObject = new GameObject("Grid");
            gridObject.transform.SetParent(transform, false);
            grid = gridObject.AddComponent<BattlefieldGrid>();
            // Dark lines read on every ground but the darkest, where they turn pale.
            bool dark = land == TerrainType.Wasteland || land == TerrainType.Swamp;
            Color lines = dark ? new Color(1f, 0.93f, 0.75f, 0.30f) : new Color(0.07f, 0.055f, 0.03f, 0.42f);
            grid.Setup(field, Radius, art != null ? art.particleMaterial : null, lines);
            grid.Visible = false;
        }

        private int Variant(int cell, int salt)
        {
            return (int)(TerrainBuilder.Hash(cell + salt * 977, (int)(battle.fieldSeed & 0x7fff)) & 0x7fffffff);
        }

        private void Obstacles(TerrainType land)
        {
            if (art == null)
            {
                return;
            }
            for (int i = 0; i < battle.obstacleCells.Count; i++)
            {
                var kind = (BattleObstacle)battle.obstacleKinds[i];
                if (kind == BattleObstacle.Wall || kind == BattleObstacle.None)
                {
                    continue;
                }
                int cell = battle.obstacleCells[i];
                GameObject prefab = art.Obstacle(kind, land, Variant(cell, 1));
                if (prefab == null)
                {
                    continue;
                }
                GameObject piece = Instantiate(prefab, props);
                piece.transform.position = Point(cell);
                piece.transform.rotation = Quaternion.Euler(0f, Variant(cell, 2) % 360, 0f);
            }
        }

        /// <summary>
        /// The wall of a town under siege, as the old games drew it: one unbroken line of wall down its column (the
        /// hexes of the column zigzag half a cell either way; the straight line between them runs through nothing but
        /// the column's own hexes), standing through the rows of wall and of the arrow towers, which stand in it, and
        /// running on past both ends of the field. At a breach it is broken off, its ends stepping down into heaps of
        /// its stones with loose stones strewn between; across the gate's row stands the gatehouse, drawbridge down,
        /// flying the colors of the town's owner. A tower already fallen (a battle taken up again) lies in a heap.
        /// </summary>
        private void Walls()
        {
            wallLine = float.NaN;
            if (battle.walls.Count == 0 && battle.gate < 0)
            {
                return;
            }
            int column = Battlefields.WallColumn;
            wallLine = (Point(field.Index(column, 0)).x + Point(field.Index(column, 1)).x) * 0.5f;
            GameObject wall = art != null ? art.Obstacle(BattleObstacle.Wall, (TerrainType)battle.terrain, 0) : null;
            // Its own dice, so the field around it comes out as it would without a wall.
            var rubble = new System.Random((int)(battle.fieldSeed & 0x7fffffff) ^ 0x3a61);
            var fort = new HashSet<int>(battle.walls);
            foreach (BattleStack stack in battle.stacks)
            {
                if (stack.IsTower)
                {
                    fort.Add(stack.cell);
                    if (!stack.alive)
                    {
                        Heap(Point(stack.cell), 1.5f, rubble);
                    }
                }
            }
            // Half a row: where the hexes of two rows meet along the line.
            float half = 0.75f * Radius;
            int last = field.rows - 1;
            for (int row = 0; row <= last; row++)
            {
                int cell = field.Index(column, row);
                float middle = Point(cell).z;
                if (cell == battle.gate)
                {
                    // An arrow tower just before the gate would hide half of it from the camera: it stands a little
                    // up the field then, clear of the tower.
                    bool tower = row > 0 && battle.stacks.Exists(s => s.IsTower && s.cell == field.Index(column, row - 1));
                    Gatehouse(middle + (tower ? 0.35f : 0f));
                    continue;
                }
                if (!fort.Contains(cell))
                {
                    Breach(middle, half, rubble);
                    continue;
                }
                int end = row;
                while (end < last && fort.Contains(field.Index(column, end + 1)))
                {
                    end++;
                }
                if (wall != null)
                {
                    float from = middle - half - (row == 0 ? WallOverrun : 0f);
                    float to = Point(field.Index(column, end)).z + half + (end == last ? WallOverrun : 0f);
                    // Next to a run, a row that is not the gate's is a breach.
                    bool brokenBelow = row > 0 && field.Index(column, row - 1) != battle.gate;
                    bool brokenAbove = end < last && field.Index(column, end + 1) != battle.gate;
                    WallRun(wall, from, to, brokenBelow, brokenAbove, rubble);
                }
                row = end;
            }
        }

        /// <summary>
        /// A straight run of wall up the field from <paramref name="from"/> to <paramref name="to"/> (along z), broken off
        /// at the ends that meet a breach.
        /// </summary>
        private void WallRun(GameObject wall, float from, float to, bool brokenBelow, bool brokenAbove, System.Random rubble)
        {
            Lay(wall, from + (brokenBelow ? BrokenEnd : 0f), to - (brokenAbove ? BrokenEnd : 0f), 1f);
            if (brokenBelow)
            {
                Broken(wall, from, 1f, rubble);
            }
            if (brokenAbove)
            {
                Broken(wall, to, -1f, rubble);
            }
        }

        /// <summary>
        /// Lengths of wall stood end to end on the line from <paramref name="from"/> to <paramref name="to"/>, each
        /// stretched a little to fill the run exactly, <paramref name="height"/> of the wall's full height.
        /// </summary>
        private void Lay(GameObject wall, float from, float to, float height)
        {
            float length = to - from;
            if (length < 0.05f)
            {
                return;
            }
            // A length of the art's wall runs across a cell, and a little over.
            float piece = CellWidth + 0.1f;
            int count = Mathf.Max(1, Mathf.RoundToInt(length / piece));
            float each = length / count;
            for (int i = 0; i < count; i++)
            {
                // A hair longer than the gap, so the joins do not show daylight.
                OnLine(wall, from + (i + 0.5f) * each, (each + 0.05f) / piece, WallHeight * height, WallThickness);
            }
        }

        /// <summary>
        /// The end of the wall at a breach: at <paramref name="edge"/> the wall is gone, and toward
        /// <paramref name="inward"/> (+1 up the field, -1 down it) it steps back up in two broken lengths, a heap of its
        /// stones at their foot.
        /// </summary>
        private void Broken(GameObject wall, float edge, float inward, System.Random rubble)
        {
            float piece = CellWidth + 0.1f;
            float whole = edge + inward * BrokenEnd;
            float step = edge + inward * BrokenEnd * 0.45f;
            OnLine(wall, (step + whole) * 0.5f, (Mathf.Abs(whole - step) + 0.05f) / piece, WallHeight * 0.62f, WallThickness * 0.96f);
            float low = edge - inward * 0.15f;
            OnLine(wall, (low + step) * 0.5f + inward * 0.05f, (Mathf.Abs(step - low) + 0.1f) / piece, WallHeight * 0.3f, WallThickness * 0.9f);
            Heap(new Vector3(wallLine + Spread(rubble, 0.25f), 0f, edge + inward * 0.1f), 0.65f, rubble);
        }

        /// <summary>A breach in the wall at <paramref name="z"/>: loose stones of the wall on its ground, either side of the line.</summary>
        private void Breach(float z, float half, System.Random rubble)
        {
            if (stones == null)
            {
                return;
            }
            for (int side = -1; side <= 1; side += 2)
            {
                var at = new Vector3(wallLine + side * (0.55f + (float)rubble.NextDouble() * 0.6f), 0f, z + Spread(rubble, half * 0.7f));
                at.y = HeightAt(at.x, at.z);
                GameObject piece = Instantiate(stones, props);
                piece.transform.SetPositionAndRotation(at, Quaternion.Euler(0f, rubble.Next(360), 0f));
            }
        }

        /// <summary>
        /// The gatehouse across the gate's row at <paramref name="z"/>, drawbridge down: the banners of the town's owner
        /// hang either side of its doors on the face the besiegers see, and its flags fly at the ends of its top. Without
        /// a gatehouse of its own the gate is the opening the wall leaves there.
        /// </summary>
        private void Gatehouse(float z)
        {
            if (gate == null)
            {
                return;
            }
            GameObject house = OnLine(gate, z);
            house.name = "Gate";
            Transform bridge = house.transform.Find("Drawbridge");
            if (!Measure(house, out Bounds bounds, bridge))
            {
                return;
            }
            int color = townColor >= 0 && townColor < 4 ? townColor : 4;
            GameObject banner = color < banners.Length ? banners[color] : null;
            if (banner != null)
            {
                for (int end = -1; end <= 1; end += 2)
                {
                    GameObject cloth = Instantiate(banner, props);
                    float tall = Measure(cloth, out Bounds hanging) ? hanging.size.y : 1.6f;
                    // Clear of the doors, which are about half the gatehouse long.
                    cloth.transform.SetPositionAndRotation(new Vector3(bounds.min.x - 0.1f, bounds.max.y - 0.4f - tall, z + end * bounds.extents.z * 0.77f),
                        Quaternion.Euler(0f, -90f, 0f));
                }
            }
            GameObject flag = art != null ? art.Flag(color) : null;
            if (flag == null)
            {
                return;
            }
            for (int end = -1; end <= 1; end += 2)
            {
                GameObject pole = Instantiate(flag, props);
                // The cloth to the camera, which looks up the field.
                pole.transform.SetPositionAndRotation(new Vector3(wallLine, bounds.max.y - 0.25f, z + end * bounds.extents.z * 0.7f),
                    Quaternion.Euler(0f, 90f, 0f));
                // Tall enough to fly over the tower beside the gate, as the camera sees them.
                pole.transform.localScale *= 1.4f;
            }
        }

        /// <summary>A heap of the wall's stones on the ground at <paramref name="at"/> (its height is found there).</summary>
        private void Heap(Vector3 at, float scale, System.Random rubble)
        {
            if (ruin == null)
            {
                return;
            }
            at.y = HeightAt(at.x, at.z) - 0.05f;
            GameObject heap = Instantiate(ruin, props);
            heap.transform.SetPositionAndRotation(at, Quaternion.Euler(0f, rubble.Next(360), 0f));
            heap.transform.localScale *= scale;
        }

        /// <summary>
        /// Stands a piece that runs along its x on the wall's line at <paramref name="z"/>, running up the field with its
        /// +z to the besiegers; <paramref name="length"/>, <paramref name="height"/> and <paramref name="thickness"/>
        /// stretch it along the wall, up and across.
        /// </summary>
        private GameObject OnLine(GameObject prefab, float z, float length = 1f, float height = 1f, float thickness = 1f)
        {
            GameObject piece = Instantiate(prefab, props);
            piece.SetActive(true);
            float reach = (CellWidth + 0.1f) * length * 0.5f;
            // On the lowest of the ground under it, so no end floats where the field gives way to the hills.
            float y = Mathf.Min(HeightAt(wallLine, z), Mathf.Min(HeightAt(wallLine, z - reach), HeightAt(wallLine, z + reach)));
            piece.transform.SetPositionAndRotation(new Vector3(wallLine, y - 0.04f, z), Quaternion.Euler(0f, -90f, 0f));
            piece.transform.localScale = Vector3.Scale(piece.transform.localScale, new Vector3(length, height, thickness));
            return piece;
        }

        private static float Spread(System.Random random, float reach)
        {
            return ((float)random.NextDouble() * 2f - 1f) * reach;
        }

        /// <summary>The bounds of what a piece draws, in the world, leaving out what is under <paramref name="skip"/>.</summary>
        private static bool Measure(GameObject piece, out Bounds bounds, Transform skip = null)
        {
            bounds = default;
            bool found = false;
            foreach (Renderer renderer in piece.GetComponentsInChildren<Renderer>())
            {
                if (skip != null && renderer.transform.IsChildOf(skip))
                {
                    continue;
                }
                if (found)
                {
                    bounds.Encapsulate(renderer.bounds);
                }
                else
                {
                    bounds = renderer.bounds;
                    found = true;
                }
            }
            return found;
        }

        /// <summary>Whether a spot is by the wall of a town, or by where it runs on past the ends of the field.</summary>
        private bool NearWall(Vector3 at, float distance)
        {
            return !float.IsNaN(wallLine) && Mathf.Abs(at.x - wallLine) < distance && at.z > -WallOverrun - distance &&
                   at.z < size.y + WallOverrun + distance;
        }

        /// <summary>The hills, woods and stones around the field: behind it and at its sides, never before the camera.</summary>
        private void Surroundings(TerrainType land)
        {
            if (art == null)
            {
                return;
            }
            var spots = new List<Vector3>();
            GameObject[] hills = art.Backdrop(land);
            if (hills.Length > 0)
            {
                // An arc of crags and copses up the slope behind the field, half sunk into it, so their tops show over
                // the back of the field and their blocky feet do not.
                for (int i = 0; i < 9; i++)
                {
                    float t = i / 8f;
                    float x = Mathf.Lerp(-26f, size.x + 26f, t) + Range(-3f, 3f);
                    float z = size.y + 19f + Range(0f, 8f) + Mathf.Abs(t - 0.5f) * 6f;
                    GameObject piece = Stand(hills[dice.Next(hills.Length)], new Vector3(x, 0f, z), Range(1.05f, 1.5f), spots);
                    Sink(piece, 0.4f);
                }
            }
            GameObject[] trees = Trees(land);
            if (trees.Length > 0)
            {
                // Close by, in view: a fringe of woods along the back of the field and down its sides (none in front,
                // where they would hide the troops); farther off, the woods the hills rise out of.
                int planted = 0;
                for (int tries = 0; tries < 500 && planted < 24; tries++)
                {
                    // Mostly down the sides: a tree just behind the field shows only its foot, its crown off the top.
                    bool back = dice.Next(5) == 0;
                    float x = back ? Range(-6f, size.x + 6f) : dice.Next(2) == 0 ? Range(-9f, -3f) : Range(size.x + 3f, size.x + 9f);
                    float z = back ? Range(size.y + 2.2f, size.y + 7f) : Range(0f, size.y + 5f);
                    var at = new Vector3(x, 0f, z);
                    if (NearHeroes(at, 3.4f) || NearTown(at, 6.5f) || NearWall(at, 2.2f) || Crowded(at, spots, 2.3f))
                    {
                        continue;
                    }
                    Stand(trees[dice.Next(trees.Length)], at, Range(0.8f, 1.2f), spots, 0.05f);
                    planted++;
                }
                planted = 0;
                for (int tries = 0; tries < 400 && planted < 30; tries++)
                {
                    bool behind = dice.Next(2) == 0;
                    float x = behind ? Range(-26f, size.x + 26f) : dice.Next(2) == 0 ? Range(-26f, -9f) : Range(size.x + 9f, size.x + 26f);
                    float z = behind ? Range(size.y + 7f, size.y + 18f) : Range(-3f, size.y + 10f);
                    var at = new Vector3(x, 0f, z);
                    if (NearHeroes(at, 3.4f) || NearTown(at, 6.5f) || NearWall(at, 2.2f) || Crowded(at, spots, 2.6f))
                    {
                        continue;
                    }
                    Stand(trees[dice.Next(trees.Length)], at, Range(0.9f, 1.35f), spots, 0.05f);
                    planted++;
                }
            }
            // Stones and fallen wood of the land itself at the edges of the field, made as its own obstacles are.
            var litter = new List<GameObject>();
            foreach (BattleObstacle kind in new[] { BattleObstacle.Boulder, BattleObstacle.Rock, BattleObstacle.Stump, BattleObstacle.Logs })
            {
                litter.AddRange(art.ObstacleVariants(kind, land) ?? new GameObject[0]);
            }
            litter.RemoveAll(prefab => prefab == null);
            if (litter.Count > 0)
            {
                for (int i = 0; i < 18; i++)
                {
                    // At the sides, and a few low ones along the front edge, below the troops of the first row.
                    Vector3 at;
                    if (i % 3 == 2)
                    {
                        at = new Vector3(Range(-4f, size.x + 4f), 0f, Range(-3.8f, -1.9f));
                    }
                    else
                    {
                        bool left = i % 2 == 0;
                        at = new Vector3(left ? Range(-7f, -2.6f) : Range(size.x + 2.6f, size.x + 7f), 0f, Range(-1.5f, size.y + 2f));
                    }
                    if (!NearHeroes(at, 3f) && !Crowded(at, spots, 2f) && !NearTown(at, 7f) && !NearWall(at, 1.6f))
                    {
                        Stand(litter[dice.Next(litter.Count)], at, Range(0.75f, 1.15f), spots, 0f);
                    }
                }
            }
        }

        private bool NearTown(Vector3 at, float distance)
        {
            if (float.IsNaN(townSpot.x))
            {
                return false;
            }
            at.y = 0f;
            Vector3 spot = townSpot;
            spot.y = 0f;
            return (spot - at).sqrMagnitude < distance * distance;
        }

        private GameObject[] Trees(TerrainType land)
        {
            switch (land)
            {
                case TerrainType.Snow:
                    return art.pineTrees;
                case TerrainType.Sand:
                case TerrainType.Wasteland:
                case TerrainType.Rough:
                case TerrainType.Rock:
                case TerrainType.Swamp:
                    return art.deadTrees;
                default:
                    var mixed = new List<GameObject>(art.forestTrees);
                    mixed.AddRange(art.pineTrees);
                    return mixed.ToArray();
            }
        }

        private float Range(float min, float max)
        {
            return min + (float)dice.NextDouble() * (max - min);
        }

        private bool NearHeroes(Vector3 at, float distance)
        {
            for (int side = 0; side < 2; side++)
            {
                HeroSpot(side, out Vector3 spot, out _);
                spot.y = 0f;
                at.y = 0f;
                if ((spot - at).sqrMagnitude < distance * distance)
                {
                    return true;
                }
            }
            return false;
        }

        private static bool Crowded(Vector3 at, List<Vector3> spots, float distance)
        {
            foreach (Vector3 spot in spots)
            {
                if ((spot - at).sqrMagnitude < distance * distance)
                {
                    return true;
                }
            }
            return false;
        }

        private GameObject Stand(GameObject prefab, Vector3 at, float scale, List<Vector3> spots, float sink = 0.45f)
        {
            if (prefab == null)
            {
                return null;
            }
            at.y = HeightAt(at.x, at.z) - sink;
            GameObject piece = Instantiate(prefab, props);
            piece.transform.position = at;
            piece.transform.rotation = Quaternion.Euler(0f, dice.Next(360), 0f);
            piece.transform.localScale *= scale;
            spots?.Add(at);
            return piece;
        }

        /// <summary>Sinks a piece a part of its height into the ground.</summary>
        private static void Sink(GameObject piece, float part)
        {
            if (piece == null)
            {
                return;
            }
            Renderer[] renderers = piece.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                return;
            }
            Bounds bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers)
            {
                bounds.Encapsulate(renderer.bounds);
            }
            piece.transform.position += Vector3.down * (bounds.size.y * part);
        }

        /// <summary>
        /// A besieged town stands behind its wall, at the far corner of the field on the defenders' side, where their
        /// hero would stand; the camera takes it in.
        /// </summary>
        private void Town(GameObject town)
        {
            townSpot = new Vector3(float.NaN, 0f, 0f);
            if (town == null)
            {
                return;
            }
            var at = new Vector3(size.x + 4f, 0f, size.y + 4.5f);
            at.y = HeightAt(at.x, at.z) - 0.2f;
            GameObject piece = Instantiate(town, props);
            piece.transform.position = at;
            piece.transform.rotation = Quaternion.Euler(0f, -28f, 0f);
            piece.transform.localScale *= 1.15f;
            townSpot = at;
        }

        private float HeightAt(float x, float z)
        {
            return BattlefieldGround.HeightAt(x, z, size, noiseX, noiseZ);
        }

        // ------------------------------------------------------------------ the camera

        /// <summary>The part of the screen the field is framed in: above <paramref name="bottom"/> (the battle bar), in viewport units.</summary>
        public void SetBottom(float bottom)
        {
            bottom = Mathf.Clamp(bottom, 0f, 0.5f);
            viewport = new Rect(0.015f, bottom + 0.01f, 0.97f, 0.985f - bottom - 0.01f);
            Frame();
        }

        /// <summary>Starts the camera gliding in from above and to the side, down to where it watches the battle from.</summary>
        public void Intro()
        {
            Frame();
            Vector3 center = new Vector3(size.x * 0.5f, 0f, size.y * 0.5f);
            introRotation = Quaternion.Euler(Pitch + 12f, -14f, 0f);
            float away = Vector3.Distance(restPosition, center) * 1.45f;
            introPosition = center - introRotation * Vector3.forward * away + Vector3.left * 6f;
            introStart = Time.unscaledTime;
            view.transform.SetPositionAndRotation(introPosition, introRotation);
        }

        private void Frame()
        {
            if (view == null || size.x <= 0f)
            {
                return;
            }
            framedFor = new Vector2Int(Screen.width, Screen.height);
            var points = new List<Vector3>
            {
                new Vector3(0f, 0f, 0f), new Vector3(size.x, 0f, 0f), new Vector3(0f, 1.6f, 0f), new Vector3(size.x, 1.6f, 0f),
                new Vector3(0f, 2.2f, size.y), new Vector3(size.x, 2.2f, size.y)
            };
            for (int side = 0; side < 2; side++)
            {
                HeroSpot(side, out Vector3 spot, out _);
                points.Add(spot);
                points.Add(spot + Vector3.up * 3.2f * UnitScale);
            }
            if (!float.IsNaN(townSpot.x))
            {
                // The town, up to the tips of its towers.
                points.Add(townSpot + Vector3.up * 7f);
            }
            restRotation = Quaternion.Euler(Pitch, 0f, 0f);
            Framing.Fit(view, restRotation, points, viewport, 0f, out Vector3 target, out float distance);
            restPosition = target - restRotation * Vector3.forward * distance;
            if (!InIntro)
            {
                view.transform.SetPositionAndRotation(restPosition, restRotation);
            }
        }

        private void LateUpdate()
        {
            if (view == null || size.x <= 0f)
            {
                return;
            }
            if (framedFor.x != Screen.width || framedFor.y != Screen.height)
            {
                Frame();
            }
            if (introStart >= 0f)
            {
                float t = Mathf.Clamp01((Time.unscaledTime - introStart) / IntroLength);
                float k = 1f - Mathf.Pow(1f - t, 3f);
                view.transform.SetPositionAndRotation(Vector3.Lerp(introPosition, restPosition, k),
                    Quaternion.Slerp(introRotation, restRotation, k));
                if (t >= 1f)
                {
                    introStart = -1f;
                }
            }
        }

        // ------------------------------------------------------------------ IBattlefield

        public Vector3 Point(int cell)
        {
            if (!field.Valid(cell))
            {
                return transform.position;
            }
            field.Center(cell, out double x, out double y);
            return transform.TransformPoint(new Vector3((float)x * Radius, 0f, (float)y * Radius));
        }

        public int CellAt(Vector3 screen, out Vector3 ground)
        {
            ground = default;
            if (view == null)
            {
                return -1;
            }
            Ray ray = view.ScreenPointToRay(screen);
            var plane = new Plane(Vector3.up, transform.position);
            if (!plane.Raycast(ray, out float enter))
            {
                return -1;
            }
            ground = ray.GetPoint(enter);
            Vector3 local = transform.InverseTransformPoint(ground);
            return field.CellAt(local.x / Radius, local.z / Radius);
        }

        public Vector3 Facing(HexSide side)
        {
            float angle = -(int)side * 60f * Mathf.Deg2Rad;
            return new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
        }

        public void HeroSpot(int side, out Vector3 position, out Vector3 facing)
        {
            bool left = side == 0;
            float x = left ? -3.3f : size.x + 3.3f;
            float z = (1f + 1.5f * 8f) * Radius;
            position = transform.TransformPoint(new Vector3(x, HeightAt(x, z), z));
            facing = left ? Vector3.right : Vector3.left;
        }

        public void ShowGrid()
        {
            if (grid != null)
            {
                grid.Visible = true;
            }
        }

        public void HideGrid()
        {
            if (grid != null)
            {
                grid.Visible = false;
            }
        }

        public void Paint(int cell, Color color)
        {
            grid?.Paint(cell, color);
        }

        public void Unpaint(int cell)
        {
            grid?.Unpaint(cell);
        }

        public void ClearPaint()
        {
            grid?.Clear();
        }

        public void Mark(int cell, Color color)
        {
            grid?.Mark(cell, color);
        }

        public void TowerFell(int cell)
        {
            if (props != null && !float.IsNaN(wallLine) && field.Valid(cell))
            {
                Heap(Point(cell), 1.5f, new System.Random(cell * 7919 + (int)(battle.fieldSeed & 0xffff)));
            }
        }
    }
}
