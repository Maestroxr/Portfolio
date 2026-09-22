using System.Collections.Generic;
using Gamebox;
using Gamebox.Editor;
using Portfolio.Monopoly.Server;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace Portfolio.Monopoly.EditorTools
{
    /// <summary>
    /// Builds Scenes/Monopoly.unity: the lighting, the camera rig and post processing, the wooden table, the board
    /// (surface, printed names and icons, owner tags, mortgage stamps, highlights, the logo and the card decks), the
    /// house and hotel pools, the dice, the four tokens, the audio, the manager and controller, (through
    /// <see cref="MonopolyInterfaceBuilder"/>) the whole interface, and online play (the server client, the online
    /// controller and the shared lobby, through <see cref="OnlineInstaller"/>). The scene file is replaced, its GUID kept.
    /// </summary>
    internal static class MonopolySceneBuilder
    {
        public const string ScenePath = "Scenes/Monopoly.unity";
        /// <summary>The name the module of Server/ is published under (spacetime.json of the game's project).</summary>
        public const string Database = "skinnerboxes-monopoly";
        public const string VolumePath = "Config/MonopolyVolume.asset";
        public const string HousePrefabPath = "Prefabs/House.prefab";
        public const string HotelPrefabPath = "Prefabs/Hotel.prefab";

        private const float Surface = 0.3f;
        /// <summary>Where the clock of an online match sits, from the top centre of the screen.</summary>
        private static readonly Vector2 TurnClockOffset = new Vector2(0f, -18f);
        private static readonly Color Background = MonopolyStyle.Hex(0x10151D);

        public static void Build()
        {
            BuildVolumeProfile();
            GameObject house = BuildBuildingPrefab(false);
            GameObject hotel = BuildBuildingPrefab(true);

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            BuildLighting();
            Camera camera = BuildCamera(out CameraRig rig);
            BuildTable();
            BoardView board = BuildBoard(house, hotel);
            MonopolyAssets.SetObject(rig, "view", camera);
            MonopolyAssets.SetObject(rig, "board", board);
            Dice dice = BuildDice(board);
            List<MonopolyPlayer> tokens = BuildTokens();
            ParticleSystem confetti = BuildConfetti();
            MonopolyAudio audio = BuildAudio();

            var managerObject = new GameObject("Monopoly");
            var storage = managerObject.AddComponent<Storage>();
            MonopolyAssets.Set(storage, "DefaultStorageStrategy", p => p.enumValueIndex = (int)StorageStrategies.PlayerPrefs);
            var manager = managerObject.AddComponent<MonopolyGameManager>();
            var controllerObject = new GameObject("Controller");
            var controller = controllerObject.AddComponent<MonopolyController>();

            MonopolyUI ui = MonopolyInterfaceBuilder.Build(manager, controller, rig, audio, camera);

            var input = managerObject.AddComponent<BoardInput>();
            MonopolyAssets.SetObject(input, "manager", manager);
            MonopolyAssets.SetObject(input, "board", board);
            MonopolyAssets.SetObject(input, "view", camera);

            MonopolyAssets.SetObject(manager, "settings", MonopolyContentBuilder.DefaultSettings);
            MonopolyAssets.SetObject(manager, "controller", controller);
            MonopolyAssets.SetObject(manager, "ui", ui);
            MonopolyAssets.SetObject(manager, "campaign", MonopolyContentBuilder.Campaign);
            MonopolyAssets.SetObject(manager, "board", board);
            MonopolyAssets.SetObject(manager, "dice", dice);
            MonopolyAssets.SetObject(manager, "cameraRig", rig);
            MonopolyAssets.SetObject(manager, "sound", audio);
            MonopolyAssets.SetObjects(manager, "tokens", tokens.ToArray());
            MonopolyAssets.SetObject(manager, "confetti", confetti);
            MonopolyAssets.SetObject(manager, "StorageBehaviour", storage);
            MonopolyAssets.Set(manager, "GameIdentifier", p => p.intValue = (int)GameType.Monopoly);
            MonopolyAssets.SetObject(controller, "UI", ui);
            MonopolyAssets.SetObject(controller, "BaseManager", manager);

            // Online play. The clock of the lobby (who the table waits for, and for how long) hangs into the middle of
            // the board under the far row of spaces, where no popup of a decision reaches.
            Component online = OnlineInstaller.Install(scene, typeof(GameServerClient), typeof(MonopolyOnlineController), manager, ui, Database,
                new OnlineInstaller.LobbyStyle
                {
                    Title = "PLAY ONLINE",
                    Font = MonopolyArtBuilder.Body,
                    Accent = MonopolyStyle.Red,
                    Window = MonopolyStyle.Ink,
                    Row = MonopolyStyle.Hex(0x2B3648),
                    TurnBannerOffset = TurnClockOffset
                });
            MonopolyAssets.SetObject(manager, "online", online);

            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

            GameMenuInstaller.EnsureUrpCameras();
            string path = MonopolyAssets.Path(ScenePath);
            EditorSceneManager.SaveScene(scene, path);
            GameSceneBuildSettings.Sync(true);
            Debug.Log($"Monopoly scene built at {path}.");
        }

        // ------------------------------------------------------------------ light and camera

        private static void BuildLighting()
        {
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = MonopolyStyle.Hex(0x98A2AE);
            RenderSettings.ambientEquatorColor = MonopolyStyle.Hex(0x767B82);
            RenderSettings.ambientGroundColor = MonopolyStyle.Hex(0x3C332C);
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
            RenderSettings.customReflectionTexture = MonopolyArtBuilder.Reflection;
            RenderSettings.reflectionIntensity = 0.85f;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = Background;
            RenderSettings.fogStartDistance = 26f;
            RenderSettings.fogEndDistance = 58f;

            var sunObject = new GameObject("Sun");
            var sun = sunObject.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.96f, 0.9f);
            sun.intensity = 0.9f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.65f;
            sun.shadowBias = 0.02f;
            sun.shadowNormalBias = 0.3f;
            sun.lightmapBakeType = LightmapBakeType.Realtime;
            sunObject.transform.rotation = Quaternion.Euler(52f, -32f, 0f);
            sunObject.AddComponent<UniversalAdditionalLightData>();
            RenderSettings.sun = sun;

            var fillObject = new GameObject("Fill Light");
            var fill = fillObject.AddComponent<Light>();
            fill.type = LightType.Directional;
            fill.color = new Color(0.8f, 0.87f, 1f);
            fill.intensity = 0.22f;
            fill.shadows = LightShadows.None;
            fillObject.transform.rotation = Quaternion.Euler(30f, 150f, 0f);
            fillObject.AddComponent<UniversalAdditionalLightData>();
        }

        private static Camera BuildCamera(out CameraRig rig)
        {
            var rigObject = new GameObject("CameraRig");
            rig = rigObject.AddComponent<CameraRig>();
            var cameraObject = new GameObject("Main Camera") { tag = "MainCamera" };
            cameraObject.transform.SetParent(rigObject.transform, false);
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Background;
            camera.fieldOfView = 30f;
            camera.nearClipPlane = 0.3f;
            camera.farClipPlane = 150f;
            camera.allowHDR = true;
            camera.allowMSAA = true;
            cameraObject.AddComponent<AudioListener>();
            UniversalAdditionalCameraData data = camera.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = true;
            data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            data.antialiasingQuality = AntialiasingQuality.High;
            data.renderShadows = true;
            cameraObject.transform.SetPositionAndRotation(new Vector3(0f, 18f, -12f), Quaternion.Euler(56f, 0f, 0f));

            var volumeObject = new GameObject("Global Volume");
            var volume = volumeObject.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = MonopolyAssets.Load<VolumeProfile>(VolumePath);
            return camera;
        }

        private static void BuildVolumeProfile()
        {
            string path = MonopolyAssets.Path(VolumePath);
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                MonopolyAssets.EnsureFolder("Config");
                AssetDatabase.CreateAsset(profile, path);
            }
            var bloom = Get<Bloom>(profile);
            bloom.threshold.Override(1f);
            bloom.intensity.Override(0.25f);
            bloom.scatter.Override(0.6f);
            var vignette = Get<Vignette>(profile);
            vignette.intensity.Override(0.32f);
            vignette.smoothness.Override(0.45f);
            var colors = Get<ColorAdjustments>(profile);
            colors.contrast.Override(8f);
            colors.saturation.Override(10f);
            colors.postExposure.Override(0f);
            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();
        }

        private static T Get<T>(VolumeProfile profile) where T : VolumeComponent
        {
            if (!profile.TryGet(out T component))
            {
                component = profile.Add<T>(true);
                component.name = typeof(T).Name;
                AssetDatabase.AddObjectToAsset(component, profile);
            }
            return component;
        }

        // ------------------------------------------------------------------ table and board

        private static GameObject MeshObject(string name, Transform parent, Mesh mesh, Material material, bool shadows = true)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = shadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
            renderer.receiveShadows = true;
            return go;
        }

        private static void BuildTable()
        {
            GameObject table = MeshObject("Table", null, MonopolyArtBuilder.Mesh("Table"), MonopolyArtBuilder.Material("Table"), false);
            table.transform.position = new Vector3(0f, -0.001f, 0f);
        }

        private static BoardView BuildBoard(GameObject housePrefab, GameObject hotelPrefab)
        {
            BoardGeometry geometry = MonopolyArtBuilder.Geometry;
            var root = new GameObject("Board");
            var board = root.AddComponent<BoardView>();
            MeshObject("Slab", root.transform, MonopolyArtBuilder.Mesh("BoardSlab"), MonopolyArtBuilder.Material("BoardSlab"));
            GameObject top = MeshObject("Surface", root.transform, MonopolyArtBuilder.Mesh("BoardTop"), MonopolyArtBuilder.Material("Board"), false);
            top.transform.localPosition = new Vector3(0f, Surface + 0.001f, 0f);

            BoardLayout layout = MonopolyContentBuilder.Board.Layout;
            var labels = new GameObject("Print").transform;
            labels.SetParent(root.transform, false);
            var tileRoot = new GameObject("Spaces").transform;
            tileRoot.SetParent(root.transform, false);
            var tiles = new List<Tile>();
            for (int space = 0; space < layout.Count; space++)
            {
                tiles.Add(BuildTile(tileRoot, labels, geometry, layout[space], space));
            }
            BuildCenter(root.transform);

            var pools = new GameObject("Buildings").transform;
            pools.SetParent(root.transform, false);
            BuildingPool houses = BuildPool(pools, "Houses", housePrefab, 96);
            BuildingPool hotels = BuildPool(pools, "Hotels", hotelPrefab, 24);

            MonopolyAssets.SetFloat(board, "corner", geometry.Corner);
            MonopolyAssets.SetFloat(board, "width", geometry.Width);
            MonopolyAssets.SetFloat(board, "surface", Surface);
            MonopolyAssets.SetObjects(board, "tiles", tiles.ToArray());
            MonopolyAssets.SetObjects(board, "ownerMaterials", MonopolyArtBuilder.Material("Seat_0"), MonopolyArtBuilder.Material("Seat_1"),
                MonopolyArtBuilder.Material("Seat_2"), MonopolyArtBuilder.Material("Seat_3"));
            MonopolyAssets.SetObject(board, "housePool", houses);
            MonopolyAssets.SetObject(board, "hotelPool", hotels);
            return board;
        }

        private static BuildingPool BuildPool(Transform parent, string name, GameObject prefab, int size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.SetActive(false);
            var pool = go.AddComponent<BuildingPool>();
            MonopolyAssets.Set(pool, "MaxSize", p => p.intValue = size);
            MonopolyAssets.SetObject(pool, "Instance", prefab);
            go.SetActive(true);
            return pool;
        }

        private static GameObject BuildBuildingPrefab(bool hotel)
        {
            var root = new GameObject(hotel ? "Hotel" : "House");
            var building = root.AddComponent<Building>();
            GameObject model = MeshObject("Model", root.transform, MonopolyArtBuilder.Mesh(hotel ? "Hotel" : "House"), MonopolyArtBuilder.Material(hotel ? "Hotel" : "House"));
            MonopolyAssets.SetBool(building, "hotel", hotel);
            MonopolyAssets.SetObject(building, "model", model.transform);
            return MonopolyAssets.SavePrefab(root, hotel ? HotelPrefabPath : HousePrefabPath);
        }

        /// <summary>A space: its print (name, price, icons as TextMesh Pro) and its changing pieces.</summary>
        private static Tile BuildTile(Transform tileRoot, Transform print, BoardGeometry geometry, SpaceData data, int space)
        {
            Vector2 center = geometry.Center(space);
            Vector2 size = geometry.Size(space);
            Quaternion frame = BoardGeometry.Frame(space);
            var go = new GameObject($"{space:00} {data.name}");
            go.transform.SetParent(tileRoot, false);
            go.transform.localPosition = new Vector3(center.x, Surface, center.y);
            go.transform.localRotation = frame;
            var tile = go.AddComponent<Tile>();
            MonopolyAssets.SetInt(tile, "index", space);

            // Highlight glow, a little larger than the space.
            GameObject highlight = MeshObject("Highlight", go.transform, MonopolyArtBuilder.Mesh("FlatQuad"), MonopolyArtBuilder.Material("Highlight"), false);
            highlight.transform.localPosition = new Vector3(0f, 0.012f, 0f);
            highlight.transform.localScale = new Vector3(size.x + 0.12f, 1f, size.y + 0.12f);
            highlight.SetActive(false);
            MonopolyAssets.SetObject(tile, "highlight", highlight.GetComponent<MeshRenderer>());

            Transform labels = new GameObject("Print").transform;
            labels.SetParent(print, false);
            labels.localPosition = go.transform.localPosition;
            labels.localRotation = frame;
            labels.name = go.name;

            if (data.IsProperty)
            {
                GameObject tag = MeshObject("OwnerTag", go.transform, MonopolyArtBuilder.Mesh("OwnerTag"), MonopolyArtBuilder.Material("Seat_0"));
                tag.transform.localPosition = new Vector3(0f, 0.002f, -size.y * 0.5f + 0.09f);
                tag.SetActive(false);
                MonopolyAssets.SetObject(tile, "ownerTag", tag.GetComponent<MeshRenderer>());

                var stamp = new GameObject("Mortgaged");
                stamp.transform.SetParent(go.transform, false);
                GameObject shade = MeshObject("Shade", stamp.transform, MonopolyArtBuilder.Mesh("FlatQuad"), MonopolyArtBuilder.Material("Mortgaged"), false);
                shade.transform.localPosition = new Vector3(0f, 0.008f, 0f);
                shade.transform.localScale = new Vector3(size.x - 0.04f, 1f, size.y - 0.04f);
                TextMeshPro word = WorldText(stamp.transform, "Word", "MORTGAGED", MonopolyArtBuilder.Heavy, MonopolyStyle.Hex(0xFF4D5E),
                    new Vector3(0f, 0.012f, 0f), Quaternion.Euler(90f, 0f, 58f), new Vector2(size.y * 0.95f, 0.3f), 1.6f);
                word.fontStyle = FontStyles.Bold;
                stamp.SetActive(false);
                MonopolyAssets.SetObject(tile, "mortgaged", stamp);
            }
            if (data.kind == SpaceKind.Street)
            {
                var anchor = new GameObject("Buildings");
                anchor.transform.SetParent(go.transform, false);
                float bar = size.y * 0.24f;
                anchor.transform.localPosition = new Vector3(0f, 0.003f, size.y * 0.5f - bar * 0.5f);
                MonopolyAssets.SetObject(tile, "buildingAnchor", anchor.transform);
            }
            PrintSpace(labels, data, space, size);
            return tile;
        }

        /// <summary>The printed names, prices and icons of a space, in its frame (x along the side, z to the centre).</summary>
        private static void PrintSpace(Transform parent, SpaceData data, int space, Vector2 size)
        {
            float w = size.x;
            float d = size.y;
            Color ink = MonopolyStyle.Ink;
            TMP_FontAsset bold = MonopolyArtBuilder.Bold;
            TMP_FontAsset heavy = MonopolyArtBuilder.Heavy;
            TMP_FontAsset icons = MonopolyArtBuilder.IconFont;
            string name = data.name.ToUpperInvariant();
            string price = MonopolyStyle.Money(data.price);
            Quaternion flat = Quaternion.Euler(90f, 0f, 0f);
            switch (data.kind)
            {
                case SpaceKind.Street:
                    WorldText(parent, "Name", name, bold, ink, new Vector3(0f, 0.004f, d * 0.07f), flat, new Vector2(w * 0.9f, d * 0.26f), 1.25f);
                    WorldText(parent, "City", data.city.ToUpperInvariant(), bold, MonopolyStyle.Shade(MonopolyStyle.Mint, 0.45f), new Vector3(0f, 0.004f, -d * 0.12f), flat, new Vector2(w * 0.9f, 0.1f), 0.62f);
                    WorldText(parent, "Price", price, heavy, ink, new Vector3(0f, 0.004f, -d * 0.3f), flat, new Vector2(w * 0.9f, 0.18f), 1.35f);
                    break;
                case SpaceKind.Railroad:
                case SpaceKind.Utility:
                    WorldText(parent, "Name", name, bold, ink, new Vector3(0f, 0.004f, d * 0.33f), flat, new Vector2(w * 0.9f, d * 0.2f), 1.15f);
                    WorldText(parent, "Icon", Icons.ForSpace(data), icons, data.kind == SpaceKind.Railroad ? ink : MonopolyStyle.Hex(0x1F6FB2),
                        new Vector3(0f, 0.004f, -d * 0.02f), flat, new Vector2(w * 0.8f, 0.5f), 4.2f);
                    WorldText(parent, "Price", price, heavy, ink, new Vector3(0f, 0.004f, -d * 0.3f), flat, new Vector2(w * 0.9f, 0.18f), 1.35f);
                    break;
                case SpaceKind.Chance:
                    WorldText(parent, "Name", "CHANCE", heavy, ink, new Vector3(0f, 0.004f, d * 0.36f), flat, new Vector2(w * 0.9f, 0.2f), 1.2f);
                    WorldText(parent, "Mark", "?", heavy, MonopolyStyle.ChanceOrange, new Vector3(0f, 0.004f, -d * 0.08f), flat, new Vector2(w * 0.9f, d * 0.6f), 9f);
                    break;
                case SpaceKind.CommunityChest:
                    WorldText(parent, "Name", "COMMUNITY\nCHEST", heavy, ink, new Vector3(0f, 0.004f, d * 0.3f), flat, new Vector2(w * 0.95f, 0.36f), 1.05f);
                    WorldText(parent, "Icon", Icons.Chest, icons, MonopolyStyle.ChestBlue, new Vector3(0f, 0.004f, -d * 0.12f), flat, new Vector2(w * 0.8f, 0.5f), 4.6f);
                    break;
                case SpaceKind.Tax:
                    WorldText(parent, "Name", name, heavy, ink, new Vector3(0f, 0.004f, d * 0.33f), flat, new Vector2(w * 0.9f, d * 0.2f), 1.15f);
                    WorldText(parent, "Icon", Icons.ForSpace(data), icons, MonopolyStyle.Shade(MonopolyStyle.Gold, 0.85f), new Vector3(0f, 0.004f, 0f), flat, new Vector2(w * 0.8f, 0.5f), 4f);
                    WorldText(parent, "Price", $"PAY {MonopolyStyle.Money(data.tax)}", heavy, ink, new Vector3(0f, 0.004f, -d * 0.3f), flat, new Vector2(w * 0.9f, 0.18f), 1.15f);
                    break;
                default:
                    PrintCorner(parent, data, space, size);
                    break;
            }
        }

        /// <summary>The corners, printed on the diagonal like the classic board.</summary>
        private static void PrintCorner(Transform parent, SpaceData data, int space, Vector2 size)
        {
            float c = size.x;
            Color ink = MonopolyStyle.Ink;
            TMP_FontAsset heavy = MonopolyArtBuilder.Heavy;
            TMP_FontAsset bold = MonopolyArtBuilder.Bold;
            TMP_FontAsset icons = MonopolyArtBuilder.IconFont;
            // Turned 45 degrees: text runs across the corner, reading from outside the board.
            Quaternion diagonal = Quaternion.Euler(90f, -45f, 0f);
            Vector3 up = Quaternion.Euler(0f, -45f, 0f) * Vector3.forward;
            switch (data.kind)
            {
                case SpaceKind.Go:
                    WorldText(parent, "Collect", "COLLECT $200 SALARY\nAS YOU PASS", bold, ink, up * (c * 0.3f) + Vector3.up * 0.004f, diagonal, new Vector2(c * 0.8f, 0.25f), 0.9f);
                    WorldText(parent, "Go", "GO", heavy, MonopolyStyle.Red, up * (c * 0.02f) + Vector3.up * 0.004f, diagonal, new Vector2(c * 0.8f, 0.6f), 6.2f);
                    WorldText(parent, "Arrow", Icons.ArrowLeft, MonopolyArtBuilder.IconFont, MonopolyStyle.Red, new Vector3(0f, 0.004f, -c * 0.33f), Quaternion.Euler(90f, 0f, 0f),
                        new Vector2(c * 0.9f, 0.4f), 5.5f);
                    break;
                case SpaceKind.Jail:
                {
                    // The cell sits towards the board centre; "just visiting" runs along the two outer strips.
                    // In the jail's frame local x runs south and local z east, so the cell (north east) is at -x, +z.
                    var cellCenter = new Vector3(-c * 0.15f, 0.004f, c * 0.15f);
                    WorldText(parent, "InJail", "IN\nJAIL", heavy, ink, cellCenter, Quaternion.Euler(90f, -45f, 0f), new Vector2(c * 0.6f, 0.6f), 2.4f);
                    WorldText(parent, "Just", "JUST", heavy, ink, new Vector3(-c * 0.08f, 0.004f, -c * 0.39f), Quaternion.Euler(90f, 0f, 0f), new Vector2(c * 0.6f, 0.2f), 1.25f);
                    WorldText(parent, "Visiting", "VISITING", heavy, ink, new Vector3(c * 0.39f, 0.004f, c * 0.08f), Quaternion.Euler(90f, -90f, 0f), new Vector2(c * 0.75f, 0.2f), 1.25f);
                    break;
                }
                case SpaceKind.FreeParking:
                    WorldText(parent, "Free", "FREE", heavy, ink, up * (c * 0.3f) + Vector3.up * 0.004f, diagonal, new Vector2(c * 0.8f, 0.25f), 1.9f);
                    WorldText(parent, "Car", Icons.Car, icons, MonopolyStyle.Red, Vector3.up * 0.004f, diagonal, new Vector2(c * 0.8f, 0.6f), 5.5f);
                    WorldText(parent, "Parking", "PARKING", heavy, ink, -up * (c * 0.3f) + Vector3.up * 0.004f, diagonal, new Vector2(c * 0.9f, 0.25f), 1.9f);
                    break;
                case SpaceKind.GoToJail:
                    WorldText(parent, "GoTo", "GO TO", heavy, ink, up * (c * 0.3f) + Vector3.up * 0.004f, diagonal, new Vector2(c * 0.8f, 0.25f), 1.9f);
                    WorldText(parent, "Officer", Icons.Officer, icons, MonopolyStyle.Blue, Vector3.up * 0.004f, diagonal, new Vector2(c * 0.8f, 0.6f), 5.2f);
                    WorldText(parent, "Jail", "JAIL", heavy, ink, -up * (c * 0.3f) + Vector3.up * 0.004f, diagonal, new Vector2(c * 0.8f, 0.25f), 1.9f);
                    break;
            }
        }

        /// <summary>
        /// A TextMesh Pro text lying on the board. <paramref name="size"/> is the box it fits into (world units) and
        /// <paramref name="fontSize"/> its largest size; longer names shrink to fit.
        /// </summary>
        private static TextMeshPro WorldText(Transform parent, string name, string text, TMP_FontAsset font, Color color, Vector3 position,
            Quaternion rotation, Vector2 size, float fontSize)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localRotation = rotation;
            var label = go.AddComponent<TextMeshPro>();
            label.font = font;
            label.text = text;
            label.color = color;
            label.fontSize = fontSize;
            label.enableAutoSizing = true;
            label.fontSizeMax = fontSize;
            label.fontSizeMin = fontSize * 0.35f;
            label.alignment = TextAlignmentOptions.Center;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.overflowMode = TextOverflowModes.Overflow;
            label.rectTransform.sizeDelta = size;
            label.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            return label;
        }

        /// <summary>The middle of the board: the logo across it and the two decks on their spots.</summary>
        private static void BuildCenter(Transform board)
        {
            var center = new GameObject("Center").transform;
            center.SetParent(board, false);
            center.localPosition = new Vector3(0f, Surface, 0f);

            var logo = new GameObject("Logo").transform;
            logo.SetParent(center, false);
            logo.localRotation = Quaternion.Euler(0f, -45f, 0f);
            GameObject banner = MeshObject("Banner", logo, MonopolyArtBuilder.Mesh("FlatQuad"), MonopolyArtBuilder.Material("Logo"), false);
            banner.transform.localPosition = new Vector3(0f, 0.01f, 0f);
            banner.transform.localScale = new Vector3(5.6f, 1f, 5.6f * 300f / 1024f);
            TextMeshPro word = WorldText(logo, "Word", "MONOPOLY", MonopolyArtBuilder.Heavy, Color.white, new Vector3(0f, 0.016f, -0.02f),
                Quaternion.Euler(90f, 0f, 0f), new Vector2(4.9f, 1.2f), 12f);
            word.characterSpacing = 4f;
            WorldText(logo, "Edition", "WORLD TOUR EDITION", MonopolyArtBuilder.Heavy, MonopolyStyle.Ink, new Vector3(0f, 0.012f, -1.12f),
                Quaternion.Euler(90f, 0f, 0f), new Vector2(4.6f, 0.4f), 3f).characterSpacing = 12f;

            BuildDeck(center, "Community Chest", new Vector3(-2.35f, 0f, 2.35f), MonopolyArtBuilder.Material("CardChest"), Icons.Chest, "COMMUNITY\nCHEST", true);
            BuildDeck(center, "Chance", new Vector3(2.35f, 0f, -2.35f), MonopolyArtBuilder.Material("CardChance"), "?", "CHANCE", false);
        }

        private static void BuildDeck(Transform parent, string name, Vector3 position, Material top, string icon, string label, bool iconFont)
        {
            var deck = new GameObject(name).transform;
            deck.SetParent(parent, false);
            deck.localPosition = position;
            deck.localRotation = Quaternion.Euler(0f, -45f, 0f);
            MeshObject("Stack", deck, MonopolyArtBuilder.Mesh("CardStack"), MonopolyArtBuilder.Material("CardEdge"));
            GameObject face = MeshObject("Top", deck, MonopolyArtBuilder.Mesh("FlatQuad"), top, false);
            face.transform.localPosition = new Vector3(0f, 0.142f, 0f);
            face.transform.localScale = new Vector3(1.16f, 1f, 1.76f);
            WorldText(deck, "Icon", icon, iconFont ? MonopolyArtBuilder.IconFont : MonopolyArtBuilder.Heavy, Color.white, new Vector3(0f, 0.146f, 0.16f),
                Quaternion.Euler(90f, 0f, 0f), new Vector2(1f, 0.9f), iconFont ? 6f : 9f);
            WorldText(deck, "Label", label, MonopolyArtBuilder.Heavy, Color.white, new Vector3(0f, 0.146f, -0.55f), Quaternion.Euler(90f, 0f, 0f),
                new Vector2(1.05f, 0.42f), 1.4f);
        }

        // ------------------------------------------------------------------ dice, tokens, effects

        private static Dice BuildDice(BoardView board)
        {
            var root = new GameObject("Dice");
            root.transform.position = new Vector3(0.1f, Surface, 1.9f);
            var dice = root.AddComponent<Dice>();
            GameObject a = MeshObject("Die A", root.transform, MonopolyArtBuilder.Mesh("Die"), MonopolyArtBuilder.Material("Die"));
            GameObject b = MeshObject("Die B", root.transform, MonopolyArtBuilder.Mesh("Die"), MonopolyArtBuilder.Material("Die"));
            GameObject speed = MeshObject("Speed Die", root.transform, MonopolyArtBuilder.Mesh("Die"), MonopolyArtBuilder.Material("SpeedDie"));
            speed.SetActive(false);
            var shadows = new List<Transform>();
            foreach (string label in new[] { "A", "B", "Speed" })
            {
                GameObject shadow = MeshObject($"Shadow {label}", root.transform, MonopolyArtBuilder.Mesh("FlatQuad"), MonopolyArtBuilder.Material("ContactShadow"), false);
                shadow.transform.localScale = new Vector3(0.9f, 1f, 0.9f);
                shadows.Add(shadow.transform);
            }
            MonopolyAssets.SetObject(dice, "dieA", a.transform);
            MonopolyAssets.SetObject(dice, "dieB", b.transform);
            MonopolyAssets.SetObject(dice, "speedDie", speed.transform);
            MonopolyAssets.SetFloat(dice, "halfSize", 0.23f);
            MonopolyAssets.SetObjects(dice, "shadows", shadows.ToArray());
            return dice;
        }

        private static List<MonopolyPlayer> BuildTokens()
        {
            var tokens = new List<MonopolyPlayer>();
            var meshes = new Mesh[MonopolyStyle.TokenCount];
            for (int i = 0; i < meshes.Length; i++)
            {
                meshes[i] = MonopolyArtBuilder.Mesh($"Token_{i}");
            }
            var seatMaterials = new Material[4];
            var ringMaterials = new Material[4];
            for (int i = 0; i < 4; i++)
            {
                seatMaterials[i] = MonopolyArtBuilder.Material($"Seat_{i}");
                ringMaterials[i] = MonopolyArtBuilder.Material($"TurnRing_{i}");
            }
            var parent = new GameObject("Tokens").transform;
            for (int seat = 0; seat < 4; seat++)
            {
                var root = new GameObject($"Token {seat + 1}");
                root.transform.SetParent(parent, false);
                root.transform.position = new Vector3(5f, Surface, -5f);
                var player = root.AddComponent<MonopolyPlayer>();
                var body = new GameObject("Body").transform;
                body.SetParent(root.transform, false);
                GameObject figure = MeshObject("Figure", body, meshes[Mathf.Min(seat, meshes.Length - 1)], MonopolyArtBuilder.Material("Pewter"));
                GameObject stand = MeshObject("Base", body, MonopolyArtBuilder.Mesh("TokenBase"), seatMaterials[seat]);
                GameObject ring = MeshObject("Ring", root.transform, MonopolyArtBuilder.Mesh("TurnRing"), ringMaterials[seat], false);
                ring.transform.localPosition = new Vector3(0f, 0.015f, 0f);
                ring.SetActive(false);
                GameObject shadow = MeshObject("Shadow", root.transform, MonopolyArtBuilder.Mesh("FlatQuad"), MonopolyArtBuilder.Material("ContactShadow"), false);
                shadow.transform.localScale = new Vector3(0.72f, 1f, 0.72f);
                MonopolyAssets.SetObject(player, "body", body);
                MonopolyAssets.SetObject(player, "figure", figure.GetComponent<MeshFilter>());
                MonopolyAssets.SetObject(player, "baseRenderer", stand.GetComponent<MeshRenderer>());
                MonopolyAssets.SetObject(player, "ring", ring.GetComponent<MeshRenderer>());
                MonopolyAssets.SetObjects(player, "tokenMeshes", meshes);
                MonopolyAssets.SetObjects(player, "seatMaterials", seatMaterials);
                MonopolyAssets.SetObjects(player, "ringMaterials", ringMaterials);
                MonopolyAssets.SetObject(player, "shadow", shadow.transform);
                MonopolyAssets.SetFloat(player, "groundHeight", Surface);
                tokens.Add(player);
            }
            return tokens;
        }

        private static ParticleSystem BuildConfetti()
        {
            var go = new GameObject("Confetti");
            go.transform.position = new Vector3(0f, 7f, 0f);
            go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            var system = go.AddComponent<ParticleSystem>();
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystem.MainModule main = system.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = 2.5f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(3f, 4.5f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1f, 3f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.22f);
            main.startRotation3D = true;
            main.startRotationX = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startRotationY = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startRotationZ = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.gravityModifier = 0.35f;
            main.maxParticles = 600;
            main.startColor = new ParticleSystem.MinMaxGradient(MonopolyStyle.Red, MonopolyStyle.Gold);
            var colors = new Gradient();
            colors.SetKeys(new[]
            {
                new GradientColorKey(MonopolyStyle.Red, 0f), new GradientColorKey(MonopolyStyle.Gold, 0.25f), new GradientColorKey(MonopolyStyle.Green, 0.5f),
                new GradientColorKey(MonopolyStyle.Blue, 0.75f), new GradientColorKey(MonopolyStyle.PlayerColor(0), 1f)
            }, new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
            main.startColor = new ParticleSystem.MinMaxGradient(colors) { mode = ParticleSystemGradientMode.RandomColor };
            ParticleSystem.EmissionModule emission = system.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 260), new ParticleSystem.Burst(0.6f, 180), new ParticleSystem.Burst(1.2f, 120) });
            ParticleSystem.ShapeModule shape = system.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(11f, 11f, 0.5f);
            ParticleSystem.RotationBySpeedModule spin = system.rotationBySpeed;
            spin.enabled = true;
            spin.separateAxes = true;
            spin.x = new ParticleSystem.MinMaxCurve(4f, 8f);
            spin.y = new ParticleSystem.MinMaxCurve(3f, 7f);
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Mesh;
            renderer.mesh = MonopolyArtBuilder.Mesh("FlatQuad");
            renderer.sharedMaterial = AssetDatabase.GetBuiltinExtraResource<Material>("Default-ParticleSystem.mat");
            renderer.alignment = ParticleSystemRenderSpace.Local;
            return system;
        }

        // ------------------------------------------------------------------ sound

        private static MonopolyAudio BuildAudio()
        {
            var go = new GameObject("Audio");
            var audio = go.AddComponent<MonopolyAudio>();
            var effects = go.AddComponent<AudioSource>();
            effects.playOnAwake = false;
            effects.spatialBlend = 0f;
            var musicObject = new GameObject("Music");
            musicObject.transform.SetParent(go.transform, false);
            var music = musicObject.AddComponent<AudioSource>();
            music.playOnAwake = false;
            music.loop = true;
            music.spatialBlend = 0f;
            MonopolyAssets.SetObject(audio, "effects", effects);
            MonopolyAssets.SetObject(audio, "music", music);
            MonopolyAssets.SetObject(audio, "menuMusic", MonopolyArtBuilder.Clip("MenuLoop"));
            MonopolyAssets.SetObject(audio, "gameMusic", MonopolyArtBuilder.Clip("GameLoop"));

            var banks = new (Sfx id, float volume, string[] clips)[]
            {
                (Sfx.Click, 0.5f, new[] { "click_002" }),
                (Sfx.Error, 0.6f, new[] { "error_004" }),
                (Sfx.DiceShake, 0.8f, new[] { "dice-shake-1", "dice-shake-2" }),
                (Sfx.DiceThrow, 0.9f, new[] { "dice-throw-1", "dice-throw-2", "dice-throw-3" }),
                (Sfx.DiceBounce, 0.6f, new[] { "die-throw-1", "die-throw-2", "die-throw-3", "die-throw-4" }),
                (Sfx.Hop, 0.7f, new[] { "chip-lay-1", "chip-lay-2", "chip-lay-3" }),
                (Sfx.CoinsIn, 0.8f, new[] { "chips-stack-1", "chips-stack-2", "chips-stack-3" }),
                (Sfx.CoinsOut, 0.8f, new[] { "chips-collide-1", "chips-collide-2", "chips-collide-3" }),
                (Sfx.Buy, 0.8f, new[] { "jingles_SAX02" }),
                (Sfx.Card, 0.8f, new[] { "card-slide-1", "card-slide-2", "card-slide-3" }),
                (Sfx.Jail, 0.9f, new[] { "JailDoor" }),
                (Sfx.Bankrupt, 0.9f, new[] { "SadTrombone" }),
                (Sfx.Win, 1f, new[] { "Fanfare" }),
                (Sfx.Lose, 0.9f, new[] { "jingles_SAX14" }),
                (Sfx.Build, 0.8f, new[] { "drop_002", "drop_004" }),
                (Sfx.Sell, 0.7f, new[] { "minimize_004" }),
                (Sfx.Mortgage, 0.7f, new[] { "card-shove-1", "card-shove-2" }),
                (Sfx.Gavel, 0.9f, new[] { "Gavel" }),
                (Sfx.Turn, 0.5f, new[] { "maximize_008" }),
                (Sfx.Doubles, 0.8f, new[] { "jingles_PIZZI10" }),
                (Sfx.Jackpot, 0.9f, new[] { "jingles_STEEL02" }),
                (Sfx.Trade, 0.8f, new[] { "confirmation_004" }),
                (Sfx.Whoosh, 0.6f, new[] { "Whoosh" })
            };
            MonopolyAssets.Set(audio, "banks", list =>
            {
                list.arraySize = banks.Length;
                for (int i = 0; i < banks.Length; i++)
                {
                    SerializedProperty entry = list.GetArrayElementAtIndex(i);
                    entry.FindPropertyRelative("id").enumValueIndex = (int)banks[i].id;
                    entry.FindPropertyRelative("volume").floatValue = banks[i].volume;
                    entry.FindPropertyRelative("pitchJitter").floatValue = banks[i].id == Sfx.Win || banks[i].id == Sfx.Bankrupt ? 0f : 0.05f;
                    SerializedProperty clips = entry.FindPropertyRelative("clips");
                    clips.arraySize = banks[i].clips.Length;
                    for (int c = 0; c < banks[i].clips.Length; c++)
                    {
                        clips.GetArrayElementAtIndex(c).objectReferenceValue = MonopolyArtBuilder.Clip(banks[i].clips[c]);
                    }
                }
            });
            return audio;
        }
    }
}
