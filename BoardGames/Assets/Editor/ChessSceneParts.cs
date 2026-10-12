using Gamebox.Editor;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace Portfolio.BoardGames.EditorTools
{
    /// <summary>
    /// The chess parts of the scene: the piece prefab, "Board/ChessPieces" (the <see cref="ChessPiecesView"/> with its
    /// pool), the promotion picker "Hud/SafeArea/Parts/ChessHud", and the manager's fields <c>chessPieces</c> and
    /// <c>chessHud</c>. The scene builder calls <see cref="Build"/> once, after the board, the manager and the HUD exist.
    /// </summary>
    internal static class ChessSceneParts
    {
        public const string PiecePrefabPath = "Prefabs/ChessPiece.prefab";

        /// <summary>Most pieces deployed at once: the 32 of a game and a spare.</summary>
        private const int PoolSize = 34;

        public static void Build(BoardGamesManager manager, BoardView board, RectTransform hudParts, BoardGamesThemeSpec spec, BoardGamesTheme classic)
        {
            GameObject prefab = BoardGamesAssets.SavePrefab(PieceObject(spec), PiecePrefabPath);
            ChessPiecesView pieces = BuildPieces(board, prefab);
            ChessHud hud = hudParts != null ? BuildHud(manager, hudParts, spec) : null;
            if (manager != null)
            {
                BoardGamesAssets.SetObject(manager, "chessPieces", pieces);
                BoardGamesAssets.SetObject(manager, "chessHud", hud);
            }
        }

        /// <summary>
        /// One piece, not saved: the pawn's mesh with the body and trim materials of <paramref name="spec"/>'s White, a
        /// contact shadow under it and the glow of a piece a move may take (inactive). The piece prefab, and the preview's
        /// pieces.
        /// </summary>
        public static GameObject PieceObject(BoardGamesThemeSpec spec)
        {
            var root = new GameObject("ChessPiece");
            var filter = root.AddComponent<MeshFilter>();
            filter.sharedMesh = ChessArt.MeshOf(ChessPieceKind.Pawn);
            var renderer = root.AddComponent<MeshRenderer>();
            Material body = BoardGamesArtBuilder.Material("ChessLight", spec);
            renderer.sharedMaterials = new[] { body, spec.Emissive ? BoardGamesArtBuilder.Material("ChessLightTrim", spec) : body };
            renderer.shadowCastingMode = ShadowCastingMode.On;
            renderer.receiveShadows = true;
            var view = root.AddComponent<ChessPieceView>();

            var shadow = new GameObject("Shadow");
            shadow.transform.SetParent(root.transform, false);
            shadow.transform.localPosition = new Vector3(0f, 0.003f, 0f);
            shadow.transform.localScale = new Vector3(0.8f, 1f, 0.8f);
            shadow.AddComponent<MeshFilter>().sharedMesh = BoardGamesArtBuilder.Mesh("Square");
            var shadowRenderer = shadow.AddComponent<MeshRenderer>();
            shadowRenderer.sharedMaterial = BoardGamesArtBuilder.Material("ContactShadow", spec);
            shadowRenderer.shadowCastingMode = ShadowCastingMode.Off;
            shadowRenderer.receiveShadows = false;

            var glow = new GameObject("Glow");
            glow.transform.SetParent(root.transform, false);
            glow.transform.localPosition = new Vector3(0f, 0.007f, 0f);
            glow.transform.localScale = new Vector3(0.94f, 1f, 0.94f);
            glow.AddComponent<MeshFilter>().sharedMesh = BoardGamesArtBuilder.Mesh("Square");
            var glowRenderer = glow.AddComponent<MeshRenderer>();
            glowRenderer.sharedMaterial = BoardGamesArtBuilder.Material("Highlight", spec);
            glowRenderer.shadowCastingMode = ShadowCastingMode.Off;
            glowRenderer.receiveShadows = false;
            glow.SetActive(false);

            BoardGamesAssets.SetObject(view, "filter", filter);
            BoardGamesAssets.SetObject(view, "body", renderer);
            BoardGamesAssets.SetObject(view, "shadow", shadow.transform);
            BoardGamesAssets.SetObject(view, "glow", glowRenderer);
            return root;
        }

        /// <summary>"ChessPieces" under the board: the view and its pool of pieces.</summary>
        private static ChessPiecesView BuildPieces(BoardView board, GameObject prefab)
        {
            var root = new GameObject("ChessPieces");
            root.transform.SetParent(board != null ? board.transform : null, false);
            var view = root.AddComponent<ChessPiecesView>();
            var poolObject = new GameObject("Pool");
            poolObject.transform.SetParent(root.transform, false);
            poolObject.SetActive(false);
            var pool = poolObject.AddComponent<ChessPiecePool>();
            BoardGamesAssets.Set(pool, "MaxSize", p => p.intValue = PoolSize);
            BoardGamesAssets.SetObject(pool, "Instance", prefab);
            poolObject.SetActive(true);
            BoardGamesAssets.SetObject(view, "board", board);
            BoardGamesAssets.SetObject(view, "pool", pool);
            return view;
        }

        /// <summary>The promotion picker: a dim catcher over the screen, a panel with the question and four buttons.</summary>
        private static ChessHud BuildHud(BoardGamesManager manager, RectTransform parts, BoardGamesThemeSpec spec)
        {
            TMP_FontAsset bold = BoardGamesArtBuilder.BoldFont;
            Sprite rounded = BoardGamesArtBuilder.UI("Rounded");

            RectTransform root = UIBuildUtils.CreateRect(parts, "ChessHud");
            UIBuildUtils.Stretch(root);
            var hud = root.gameObject.AddComponent<ChessHud>();

            RectTransform picker = UIBuildUtils.CreateRect(root, "Picker");
            UIBuildUtils.Stretch(picker);
            Image catcher = UIBuildUtils.CreateImage(picker, "Blocker", new Color(0f, 0f, 0f, 0.28f), false);
            UIBuildUtils.Stretch(catcher.rectTransform);
            var blocker = catcher.gameObject.AddComponent<Button>();
            blocker.transition = Selectable.Transition.None;

            Image panel = UIBuildUtils.CreateImage(picker, "Panel", new Color(spec.Paper.r, spec.Paper.g, spec.Paper.b, 0.96f));
            panel.sprite = rounded;
            panel.type = Image.Type.Sliced;
            UIBuildUtils.Anchor(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -60f), new Vector2(780f, 250f));

            TextMeshProUGUI title = UIBuildUtils.CreateText(panel.transform, "Title", "Promote the pawn to:", 32f, spec.Ink);
            title.font = bold;
            title.textWrappingMode = TextWrappingModes.NoWrap;
            title.overflowMode = TextOverflowModes.Overflow;
            title.enableAutoSizing = true;
            title.fontSizeMax = 32f;
            title.fontSizeMin = 16f;
            UIBuildUtils.Anchor(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -22f), new Vector2(720f, 48f));

            var buttons = new Button[ChessPieces.Promotions.Length];
            var labels = new TMP_Text[ChessPieces.Promotions.Length];
            for (int i = 0; i < buttons.Length; i++)
            {
                string words = ChessPieces.NameKey(ChessPieces.Promotions[i]);
                Button button = UIBuildUtils.CreateButton(panel.transform, words + "Button", words, new Vector2(168f, 120f), 30f);
                var image = button.GetComponent<Image>();
                image.sprite = rounded;
                image.type = Image.Type.Sliced;
                image.color = Color.white;
                ColorBlock colors = button.colors;
                colors.highlightedColor = new Color(0.9f, 0.95f, 1f);
                colors.pressedColor = new Color(0.78f, 0.84f, 0.9f);
                button.colors = colors;
                UIBuildUtils.Anchor(button.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2((i - 1.5f) * 184f, -28f), new Vector2(168f, 120f));
                TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>();
                if (label != null)
                {
                    label.font = bold;
                    label.color = spec.ButtonInk;
                    label.textWrappingMode = TextWrappingModes.NoWrap;
                    label.overflowMode = TextOverflowModes.Overflow;
                    label.enableAutoSizing = true;
                    label.fontSizeMax = 30f;
                    label.fontSizeMin = 14f;
                }
                buttons[i] = button;
                labels[i] = label;
            }
            picker.gameObject.SetActive(false);

            BoardGamesAssets.SetObject(hud, "manager", manager);
            BoardGamesAssets.SetObject(hud, "picker", picker.gameObject);
            BoardGamesAssets.SetObject(hud, "blocker", blocker);
            BoardGamesAssets.SetObject(hud, "panel", panel);
            BoardGamesAssets.SetObject(hud, "title", title);
            BoardGamesAssets.SetObjects(hud, "buttons", buttons);
            BoardGamesAssets.SetObjects(hud, "labels", labels);
            return hud;
        }
    }
}
