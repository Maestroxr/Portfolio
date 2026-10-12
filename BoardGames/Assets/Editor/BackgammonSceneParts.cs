using Gamebox;
using Gamebox.Editor;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace Portfolio.BoardGames.EditorTools
{
    /// <summary>
    /// The backgammon parts of the scene: the "Backgammon" board on the table (the box, the felt, the 24 points and their
    /// glows, the glows of the bar and the trays, thirty checkers with their shadows, the dice with their thrower, the
    /// cube, the discs and count labels of the marks), hidden until the manager shows it, and the HUD's
    /// "BackgammonHud" (the offer line and the Roll, Double, Undo, Take and Drop buttons), both wired to the manager.
    /// </summary>
    internal static class BackgammonSceneParts
    {
        private const int Discs = 24;
        private const int Labels = 8;

        public static void Build(BoardGamesManager manager, Transform world, RectTransform hudParts, BoardGamesThemeSpec spec, BoardGamesTheme classic)
        {
            BackgammonBoard board = BuildBoard(world, spec);
            board.transform.position = new Vector3(0f, BoardGamesSceneBuilder.Surface, 0f);
            board.ApplyTheme(classic);
            board.gameObject.SetActive(false);
            BackgammonHud hud = hudParts != null ? BuildHud(manager, hudParts, spec) : null;
            BoardGamesAssets.SetObject(manager, "backgammonBoard", board);
            BoardGamesAssets.SetObject(manager, "backgammonHud", hud);
        }

        /// <summary>The whole board under a new "Backgammon" object (at the parent's origin: the felt lies at its y = 0), in <paramref name="spec"/>'s look.</summary>
        public static BackgammonBoard BuildBoard(Transform parent, BoardGamesThemeSpec spec)
        {
            var root = new GameObject("Backgammon");
            root.transform.SetParent(parent, false);
            var board = root.AddComponent<BackgammonBoard>();
            Transform t = root.transform;
            Mesh square = BoardGamesArtBuilder.Mesh("Square");
            Material highlight = BoardGamesArtBuilder.Material("Highlight", spec);
            Material shadow = BoardGamesArtBuilder.Material("ContactShadow", spec);

            GameObject frame = MeshObject("Frame", t, BackgammonArt.Mesh("Frame"), BoardGamesArtBuilder.Material("Slab", spec), true);
            GameObject felt = MeshObject("Felt", t, BackgammonArt.Mesh("Felt"), BackgammonArt.Material("Felt", spec), false);

            Transform pointsRoot = Group("Points", t);
            Transform glowsRoot = Group("Glows", t);
            var points = new MeshRenderer[24];
            var pointGlows = new MeshRenderer[24];
            Mesh pointMesh = BackgammonArt.Mesh("Point");
            Material pointA = BackgammonArt.Material("PointA", spec);
            Material pointB = BackgammonArt.Material("PointB", spec);
            Material pointGlow = BackgammonArt.Material("PointGlow", spec);
            for (int i = 0; i < 24; i++)
            {
                Quaternion turn = BackgammonLayout.IsNear(i) ? Quaternion.identity : Quaternion.Euler(0f, 180f, 0f);
                GameObject point = MeshObject($"Point {i + 1}", pointsRoot, pointMesh, (i & 1) == 0 ? pointA : pointB, false);
                point.transform.SetLocalPositionAndRotation(BackgammonLayout.PointBase(i) + Vector3.up * 0.002f, turn);
                point.transform.localScale = new Vector3(BackgammonLayout.PointWidth * 0.94f, 1f, BackgammonLayout.PointLength);
                points[i] = point.GetComponent<MeshRenderer>();
                GameObject glow = MeshObject($"Glow {i + 1}", glowsRoot, pointMesh, pointGlow, false);
                glow.transform.SetLocalPositionAndRotation(BackgammonLayout.PointBase(i) + Vector3.up * 0.005f, turn);
                glow.transform.localScale = new Vector3(BackgammonLayout.PointWidth, 1f, BackgammonLayout.PointLength * 1.02f);
                glow.SetActive(false);
                pointGlows[i] = glow.GetComponent<MeshRenderer>();
            }
            float field = BackgammonLayout.FieldDepth;
            // The bar's glows lie under the first checkers waiting there; the trays' along the whole tray.
            float barMiddle = BackgammonLayout.CubeSize * 0.5f + 0.05f + BackgammonLayout.CheckerStep * 1.7f;
            float trayHalf = (field + BackgammonLayout.TraySplit * 0.5f) * 0.5f;
            var placeGlows = new[]
            {
                PlaceGlow("Bar Glow White", glowsRoot, square, highlight, new Vector3(BackgammonLayout.BarX, BackgammonLayout.FrameHeight + 0.006f, barMiddle),
                    new Vector3(BackgammonLayout.BarWidth * 1.25f, 1f, BackgammonLayout.CheckerStep * 4.4f)),
                PlaceGlow("Bar Glow Black", glowsRoot, square, highlight, new Vector3(BackgammonLayout.BarX, BackgammonLayout.FrameHeight + 0.006f, -barMiddle),
                    new Vector3(BackgammonLayout.BarWidth * 1.25f, 1f, BackgammonLayout.CheckerStep * 4.4f)),
                PlaceGlow("Tray Glow White", glowsRoot, square, highlight, new Vector3(BackgammonLayout.TrayX, 0.006f, -trayHalf),
                    new Vector3(BackgammonLayout.TrayWidth * 1.3f, 1f, field - BackgammonLayout.TraySplit * 0.5f)),
                PlaceGlow("Tray Glow Black", glowsRoot, square, highlight, new Vector3(BackgammonLayout.TrayX, 0.006f, trayHalf),
                    new Vector3(BackgammonLayout.TrayWidth * 1.3f, 1f, field - BackgammonLayout.TraySplit * 0.5f))
            };

            Transform checkersRoot = Group("Checkers", t);
            var checkers = new CheckerView[BackgammonGame.CheckersPerSide * 2];
            Mesh checkerMesh = BackgammonArt.Mesh("Checker");
            Material light = BackgammonArt.CheckerMaterial(spec, Side.Light);
            Material dark = BackgammonArt.CheckerMaterial(spec, Side.Dark);
            for (int i = 0; i < checkers.Length; i++)
            {
                bool white = i < BackgammonGame.CheckersPerSide;
                string label = white ? $"White {i + 1}" : $"Black {i + 1 - BackgammonGame.CheckersPerSide}";
                checkers[i] = Checker(label, checkersRoot, checkerMesh, white ? light : dark, square, shadow);
            }

            DiceThrower dice = BuildDice(t, spec, square, shadow);

            GameObject cube = MeshObject("Cube", t, BackgammonArt.Mesh("Cube"), BackgammonArt.Material("Cube", spec), true);
            cube.transform.localPosition = BackgammonLayout.CubePlace(-1, false);
            GameObject cubeShadow = MeshObject("Cube Shadow", t, square, shadow, false);
            cubeShadow.GetComponent<MeshRenderer>().receiveShadows = false;
            cubeShadow.transform.localPosition = new Vector3(BackgammonLayout.BarX, BackgammonLayout.FrameHeight + 0.004f, 0f);
            cubeShadow.transform.localScale = new Vector3(BackgammonLayout.CubeSize * 2f, 1f, BackgammonLayout.CubeSize * 2f);

            Transform marksRoot = Group("Marks", t);
            var discs = new MeshRenderer[Discs];
            for (int i = 0; i < discs.Length; i++)
            {
                GameObject disc = MeshObject($"Disc {i + 1}", marksRoot, square, highlight, false);
                disc.GetComponent<MeshRenderer>().receiveShadows = false;
                disc.SetActive(false);
                discs[i] = disc.GetComponent<MeshRenderer>();
            }
            Transform labelsRoot = Group("Labels", t);
            var labels = new TMP_Text[Labels];
            for (int i = 0; i < labels.Length; i++)
            {
                labels[i] = CountLabel($"Count {i + 1}", labelsRoot);
            }

            BoardGamesAssets.SetObject(board, "frame", frame.GetComponent<MeshRenderer>());
            BoardGamesAssets.SetObject(board, "felt", felt.GetComponent<MeshRenderer>());
            BoardGamesAssets.SetObjects(board, "points", points);
            BoardGamesAssets.SetObjects(board, "pointGlows", pointGlows);
            BoardGamesAssets.SetObjects(board, "placeGlows", placeGlows);
            BoardGamesAssets.SetObjects(board, "checkers", checkers);
            BoardGamesAssets.SetObject(board, "dice", dice);
            BoardGamesAssets.SetObject(board, "cube", cube.transform);
            BoardGamesAssets.SetObject(board, "cubeShadow", cubeShadow.transform);
            BoardGamesAssets.SetObjects(board, "discs", discs);
            BoardGamesAssets.SetObjects(board, "labels", labels);
            return board;
        }

        private static Transform Group(string name, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        private static GameObject MeshObject(string name, Transform parent, Mesh mesh, Material material, bool shadows)
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

        private static MeshRenderer PlaceGlow(string name, Transform parent, Mesh square, Material material, Vector3 position, Vector3 scale)
        {
            GameObject glow = MeshObject(name, parent, square, material, false);
            glow.transform.localPosition = position;
            glow.transform.localScale = scale;
            glow.GetComponent<MeshRenderer>().receiveShadows = false;
            glow.SetActive(false);
            return glow.GetComponent<MeshRenderer>();
        }

        /// <summary>One checker: the disc and its contact shadow (a quad on the surface under it).</summary>
        private static CheckerView Checker(string name, Transform parent, Mesh mesh, Material material, Mesh square, Material shadowMaterial)
        {
            GameObject root = MeshObject(name, parent, mesh, material, true);
            var view = root.AddComponent<CheckerView>();
            GameObject shadow = MeshObject("Shadow", root.transform, square, shadowMaterial, false);
            shadow.transform.localPosition = new Vector3(0f, 0.003f, 0f);
            float size = BackgammonLayout.CheckerRadius * 2.7f;
            shadow.transform.localScale = new Vector3(size, 1f, size);
            shadow.GetComponent<MeshRenderer>().receiveShadows = false;
            BoardGamesAssets.SetObject(view, "disc", root.GetComponent<MeshRenderer>());
            BoardGamesAssets.SetObject(view, "shadow", shadow.transform);
            return view;
        }

        /// <summary>The two dice and their shadows under a thrower at the felt's height; they rest where the board puts them.</summary>
        private static DiceThrower BuildDice(Transform parent, BoardGamesThemeSpec spec, Mesh square, Material shadowMaterial)
        {
            var root = new GameObject("Dice");
            root.transform.SetParent(parent, false);
            var thrower = root.AddComponent<DiceThrower>();
            Mesh mesh = BackgammonArt.Mesh("Die");
            Material material = BackgammonArt.Material("DieLight", spec);
            var dice = new Transform[2];
            var shadows = new Transform[2];
            for (int i = 0; i < dice.Length; i++)
            {
                GameObject die = MeshObject($"Die {i + 1}", root.transform, mesh, material, true);
                die.transform.localPosition = BackgammonLayout.DiceArea(Side.Light) + new Vector3((i - 0.5f) * 0.84f, BackgammonLayout.DieSize * 0.5f, 0f);
                die.SetActive(false);
                dice[i] = die.transform;
                GameObject shadow = MeshObject($"Die Shadow {i + 1}", root.transform, square, shadowMaterial, false);
                shadow.GetComponent<MeshRenderer>().receiveShadows = false;
                shadow.SetActive(false);
                shadows[i] = shadow.transform;
            }
            BoardGamesAssets.SetObjects(thrower, "dice", dice);
            BoardGamesAssets.SetObjects(thrower, "shadows", shadows);
            BoardGamesAssets.SetFloat(thrower, "halfSize", BackgammonLayout.DieSize * 0.5f);
            BoardGamesAssets.SetFloat(thrower, "shadowSize", BackgammonLayout.DieSize * 2f);
            return thrower;
        }

        /// <summary>A count written flat on a stack (a world text, hidden until a stack has more than five checkers).</summary>
        private static TMP_Text CountLabel(string name, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var text = go.AddComponent<TextMeshPro>();
            text.font = BoardGamesArtBuilder.BoldFont;
            text.text = "6";
            text.fontSize = 3.2f;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Overflow;
            text.rectTransform.sizeDelta = new Vector2(0.7f, 0.5f);
            text.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            go.SetActive(false);
            return text;
        }

        // ------------------------------------------------------------------ the HUD

        /// <summary>The offer line and the row of buttons at the bottom centre of the HUD, over the toast line.</summary>
        public static BackgammonHud BuildHud(BoardGamesManager manager, RectTransform hudParts, BoardGamesThemeSpec spec)
        {
            RectTransform root = UIBuildUtils.CreateRect(hudParts, "BackgammonHud");
            UIBuildUtils.Anchor(root, new Vector2(0.5f, 0f), new Vector2(0f, 104f), new Vector2(1180f, 140f));
            var hud = root.gameObject.AddComponent<BackgammonHud>();
            RectTransform panel = UIBuildUtils.CreateRect(root, "Panel");
            UIBuildUtils.Stretch(panel);

            Sprite rounded = BoardGamesArtBuilder.UI("Rounded");
            TMP_FontAsset bold = BoardGamesArtBuilder.BoldFont;

            Image box = UIBuildUtils.CreateImage(panel, "Offer", new Color(0f, 0f, 0f, 0.55f));
            box.sprite = rounded;
            box.type = Image.Type.Sliced;
            box.raycastTarget = false;
            UIBuildUtils.Anchor(box.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, 0f), new Vector2(760f, 52f));
            TextMeshProUGUI offer = UIBuildUtils.CreateText(box.rectTransform, "Text", "", 30f, Color.white, TextAlignmentOptions.Center);
            offer.font = bold;
            offer.textWrappingMode = TextWrappingModes.NoWrap;
            offer.overflowMode = TextOverflowModes.Overflow;
            offer.enableAutoSizing = true;
            offer.fontSizeMax = 30f;
            offer.fontSizeMin = 16f;
            UIBuildUtils.Stretch(offer.rectTransform, 20f, 4f, 20f, 4f);

            RectTransform row = UIBuildUtils.CreateRect(panel, "Buttons");
            UIBuildUtils.Anchor(row, new Vector2(0.5f, 0f), new Vector2(0f, 0f), new Vector2(1180f, 64f));
            var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 18f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            var size = new Vector2(210f, 62f);
            Button roll = RoundButton(row, "Roll", "Roll", size, rounded, bold, spec.Accent, Color.white);
            Button take = RoundButton(row, "Take", "Take", size, rounded, bold, spec.Accent, Color.white);
            Button drop = RoundButton(row, "Drop", "Drop", size, rounded, bold, spec.Panel, spec.Ink);
            Button @double = RoundButton(row, "Double", "Double", size, rounded, bold, spec.Panel, spec.Ink);
            Button undo = RoundButton(row, "Undo", "Undo", size, rounded, bold, spec.Panel, spec.Ink);

            BoardGamesAssets.SetObject(hud, "manager", manager);
            BoardGamesAssets.SetObject(hud, "panel", panel.gameObject);
            BoardGamesAssets.SetObject(hud, "offerBox", box.gameObject);
            BoardGamesAssets.SetObject(hud, "offerText", offer);
            BoardGamesAssets.SetObject(hud, "rollButton", roll);
            BoardGamesAssets.SetObject(hud, "doubleButton", @double);
            BoardGamesAssets.SetObject(hud, "undoButton", undo);
            BoardGamesAssets.SetObject(hud, "takeButton", take);
            BoardGamesAssets.SetObject(hud, "dropButton", drop);
            panel.gameObject.SetActive(false);
            return hud;
        }

        private static Button RoundButton(Transform parent, string name, string label, Vector2 size, Sprite rounded, TMP_FontAsset font, Color color, Color textColor)
        {
            Button button = UIBuildUtils.CreateButton(parent, name, label, size, 26f);
            var image = button.GetComponent<Image>();
            image.sprite = rounded;
            image.type = Image.Type.Sliced;
            image.color = color;
            var text = button.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
            {
                text.font = font;
                text.color = textColor;
                text.textWrappingMode = TextWrappingModes.NoWrap;
                text.overflowMode = TextOverflowModes.Overflow;
                text.enableAutoSizing = true;
                text.fontSizeMax = 26f;
                text.fontSizeMin = 12f;
            }
            return button;
        }
    }
}
