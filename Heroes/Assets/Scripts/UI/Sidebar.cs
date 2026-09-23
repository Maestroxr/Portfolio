using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Portfolio.Heroes.UI
{
    /// <summary>
    /// A hero in the right hand column, as the original game shows him: his portrait between a green bar of the
    /// movement he has left and a blue one of his mana, his name, his level and the strength of his army. Clicking it
    /// picks the hero up and takes the map to him; clicking it again opens his book.
    /// </summary>
    public sealed class HeroButton : MonoBehaviour
    {
        public const float Height = 76f;

        private HeroesGameManager manager;
        private HeroesUI ui;
        private Image card;
        private Image portrait;
        private Image pennant;
        private Image movement;
        private Image mana;
        private TextMeshProUGUI title;
        private TextMeshProUGUI detail;
        private Tooltip tip;
        private int id = -1;
        private string shown;

        public static HeroButton Make(Transform parent, HeroesGameManager manager, HeroesUI ui)
        {
            Button button = UIKit.CardButton(parent, "Hero", null);
            var card = (Image)button.targetGraphic;
            UIKit.Fit((RectTransform)card.transform, 0f, Height, true);
            var entry = card.gameObject.AddComponent<HeroButton>();
            entry.manager = manager;
            entry.ui = ui;
            entry.card = card;
            RectTransform content = UIKit.Content(card, 3f);

            // Movement on the left of the portrait, mana on its right, as upright bars.
            entry.movement = Upright(content, "Movement", new Color(0.45f, 0.85f, 0.35f), 0f);
            entry.portrait = UIKit.PortraitFrame(content, "Portrait", null);
            UIKit.Pin((RectTransform)entry.portrait.transform.parent.parent, new Vector2(0f, 0.5f), new Vector2(14f, 0f),
                new Vector2(62f, 62f));
            entry.mana = Upright(content, "Mana", new Color(0.35f, 0.6f, 1f), 80f);
            entry.pennant = UIKit.Pennant(content, "Pennant", Color.white);
            UIKit.Pin((RectTransform)entry.pennant.transform, new Vector2(1f, 1f), new Vector2(-2f, 0f), new Vector2(16f, 30f));

            entry.title = UIKit.Label(content, "Name", "", 20f, UIKit.Ink, TextAlignmentOptions.TopLeft, true);
            RectTransform titleRect = (RectTransform)entry.title.transform;
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0f, 1f);
            titleRect.offsetMin = new Vector2(100f, -28f);
            titleRect.offsetMax = new Vector2(-24f, -2f);
            UIKit.FitLine(entry.title, 20f, 14f);

            entry.detail = UIKit.Label(content, "Detail", "", 18f, UIKit.Dim, TextAlignmentOptions.BottomLeft);
            RectTransform detailRect = (RectTransform)entry.detail.transform;
            detailRect.anchorMin = new Vector2(0f, 0f);
            detailRect.anchorMax = new Vector2(1f, 0f);
            detailRect.pivot = new Vector2(0f, 0f);
            detailRect.offsetMin = new Vector2(100f, 2f);
            detailRect.offsetMax = new Vector2(-4f, 30f);
            UIKit.FitLine(entry.detail, 18f, 13f);

            entry.tip = Tooltip.Attach(card.gameObject, "");
            button.onClick.AddListener(entry.Clicked);
            return entry;
        }

        /// <summary>A thin upright bar that fills from the bottom, beside the portrait.</summary>
        private static Image Upright(RectTransform parent, string name, Color color, float x)
        {
            Image fill = UIKit.Bar(parent, name, color);
            fill.fillMethod = Image.FillMethod.Vertical;
            fill.fillOrigin = (int)Image.OriginVertical.Bottom;
            UIKit.Pin((RectTransform)fill.transform.parent, new Vector2(0f, 0.5f), new Vector2(x, 0f), new Vector2(10f, 60f));
            return fill;
        }

        public int HeroId => id;

        public void Show(int which)
        {
            id = which;
            gameObject.SetActive(true);
            Refresh();
        }

        public void Refresh()
        {
            HeroesGame game = manager.Game;
            HeroState hero = game != null ? game.State.Hero(id) : null;
            if (hero == null || !hero.alive)
            {
                gameObject.SetActive(false);
                return;
            }
            bool picked = manager.Selected != null && manager.Selected.id == hero.id;
            UIKit.Select(card, picked);
            HeroClass heroClass = hero.Def != null ? hero.Def.Class : HeroClass.Knight;
            portrait.sprite = manager.Art.HeroPortrait(heroClass);
            portrait.color = hero.sleeping ? new Color(0.55f, 0.55f, 0.6f) : Color.white;
            PlayerState owner = game.State.Player(hero.owner);
            pennant.color = HeroesArt.PlayerColor(owner != null ? (int)owner.color : 4);
            movement.fillAmount = hero.maxMovement > 0 ? Mathf.Clamp01(hero.movement / (float)hero.maxMovement) : 0f;
            int maxMana = game.MaxMana(hero);
            mana.fillAmount = maxMana > 0 ? Mathf.Clamp01(hero.mana / (float)maxMana) : 0f;
            int strength = game.Strength(hero);
            string line = hero.sleeping
                ? $"<i>Asleep</i>  {UIKit.Glyph("attack")} {Short(strength)}"
                : $"Level {hero.level}   {UIKit.Glyph("attack")} {Short(strength)}";
            if (line != shown || title.text != hero.Name)
            {
                shown = line;
                title.text = hero.Name;
                detail.text = line;
                tip.Title = hero.Name;
                tip.Picture = null;
                tip.Text = $"Level {hero.level} {HeroData.Class(heroClass).Name}\n" +
                           $"{UIKit.Glyph("movement")} {hero.movement} / {hero.maxMovement}   {UIKit.Glyph("mana")} {hero.mana} / {maxMana}\n" +
                           $"Army strength {strength:N0}{(hero.sleeping ? "\nAsleep: skipped by Next Hero." : "")}";
            }
        }

        /// <summary>A number short enough for the column: 12,400 as 12.4k.</summary>
        public static string Short(int value)
        {
            if (value >= 1000000)
            {
                return $"{value / 1000000f:0.#}M";
            }
            return value >= 10000 ? $"{value / 1000f:0.#}k" : value.ToString("N0");
        }

        private void Clicked()
        {
            HeroesGame game = manager.Game;
            HeroState hero = game != null ? game.State.Hero(id) : null;
            if (hero == null)
            {
                return;
            }
            if (manager.Selected != null && manager.Selected.id == hero.id)
            {
                ui.Sheet.Open(hero);
            }
            else
            {
                manager.Select(hero);
            }
        }
    }

    /// <summary>A town in the right hand column: its picture, its name and what it can still do today.</summary>
    public sealed class TownButton : MonoBehaviour
    {
        public const float Height = 64f;

        private HeroesGameManager manager;
        private HeroesUI ui;
        private Image picture;
        private TextMeshProUGUI title;
        private TextMeshProUGUI detail;
        private Tooltip tip;
        private int id = -1;

        public static TownButton Make(Transform parent, HeroesGameManager manager, HeroesUI ui)
        {
            Button button = UIKit.CardButton(parent, "Town", null);
            var card = (Image)button.targetGraphic;
            UIKit.Fit((RectTransform)card.transform, 0f, Height, true);
            var entry = card.gameObject.AddComponent<TownButton>();
            entry.manager = manager;
            entry.ui = ui;
            RectTransform content = UIKit.Content(card, 3f);
            entry.picture = UIKit.PortraitFrame(content, "Picture", null);
            UIKit.Pin((RectTransform)entry.picture.transform.parent.parent, new Vector2(0f, 0.5f), new Vector2(0f, 0f),
                new Vector2(50f, 50f));

            entry.title = UIKit.Label(content, "Name", "", 20f, UIKit.Ink, TextAlignmentOptions.TopLeft, true);
            RectTransform titleRect = (RectTransform)entry.title.transform;
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0f, 1f);
            titleRect.offsetMin = new Vector2(60f, -26f);
            titleRect.offsetMax = new Vector2(-4f, 0f);
            UIKit.FitLine(entry.title, 19f, 13f);

            entry.detail = UIKit.Label(content, "Detail", "", 17f, UIKit.Dim, TextAlignmentOptions.BottomLeft);
            RectTransform detailRect = (RectTransform)entry.detail.transform;
            detailRect.anchorMin = new Vector2(0f, 0f);
            detailRect.anchorMax = new Vector2(1f, 0f);
            detailRect.pivot = new Vector2(0f, 0f);
            detailRect.offsetMin = new Vector2(60f, 0f);
            detailRect.offsetMax = new Vector2(-4f, 24f);
            UIKit.FitLine(entry.detail, 17f, 12f);

            entry.tip = Tooltip.Attach(card.gameObject, "");
            button.onClick.AddListener(entry.Clicked);
            return entry;
        }

        public void Show(int which)
        {
            id = which;
            gameObject.SetActive(true);
            Refresh();
        }

        public void Refresh()
        {
            HeroesGame game = manager.Game;
            TownState town = game != null ? game.State.Town(id) : null;
            if (town == null)
            {
                gameObject.SetActive(false);
                return;
            }
            PlayerState owner = game.State.Player(town.owner);
            picture.sprite = manager.Art.TownPortrait(town.faction, owner != null ? (int)owner.color : 4);
            title.text = town.name;
            detail.text = town.builtToday
                ? "Built today"
                : $"{UIKit.Glyph("gold")} {Buildings.Income(town)} a day";
            tip.Title = town.name;
            tip.Text = $"A {Land.FactionName(town.faction)} town\n{UIKit.Glyph("gold")} {Buildings.Income(town)} a day" +
                       (town.builtToday ? "\nSomething was built here today." : "\nCan build today.");
        }

        private void Clicked()
        {
            HeroesGame game = manager.Game;
            TownState town = game != null ? game.State.Town(id) : null;
            if (town == null)
            {
                return;
            }
            if (manager.Map != null)
            {
                manager.Rig?.Focus(manager.Map.Point(town.center));
            }
            ui.Town.Open(town);
        }
    }

    /// <summary>
    /// The little map at the top of the column: the ground the player has seen in the colours of its terrain, the rest
    /// as dark parchment, towns, mines and heroes as markers in the colours of their owners, and a gold box where the
    /// camera looks. The rows of the hexagons are staggered as they are on the map, and the map keeps its shape. A
    /// click takes the camera there.
    /// </summary>
    public sealed class Minimap : MonoBehaviour
    {
        private static readonly Color32 Unknown = new Color32(58, 45, 31, 255);

        private HeroesGameManager manager;
        private RectTransform area;
        private RawImage image;
        private Texture2D texture;
        private Color32[] pixels;
        private RectTransform box;
        private RectTransform markers;
        private LayoutElement element;
        private readonly List<Image> marks = new List<Image>();
        private float next;
        private int columns;
        private int rows;

        /// <summary>The map inside its sunken frame, as wide as the column; its height follows the map's shape.</summary>
        public static Minimap Make(RectTransform parent, HeroesGameManager manager, float width)
        {
            Image frame = UIKit.Recess(parent, "Minimap");
            frame.raycastTarget = true;
            var map = frame.gameObject.AddComponent<Minimap>();
            map.manager = manager;
            map.element = frame.gameObject.AddComponent<LayoutElement>();
            map.element.preferredWidth = width;
            map.element.preferredHeight = width;
            map.area = UIKit.Content(frame, 1f, "Map");
            map.image = map.area.gameObject.AddComponent<RawImage>();
            map.image.raycastTarget = true;
            map.image.color = Color.white;
            map.markers = UIKit.Stretch(UIKit.Rect(map.area, "Markers"));
            map.box = UIKit.Rect(map.area, "View");
            map.box.anchorMin = map.box.anchorMax = Vector2.zero;
            map.box.pivot = new Vector2(0.5f, 0.5f);
            // The view box is four thin gold lines, so it never hides what it frames.
            for (int side = 0; side < 4; side++)
            {
                Image line = UIKit.Sprite(map.box, $"Edge{side}", null, new Color(1f, 0.86f, 0.45f, 0.95f));
                var rect = (RectTransform)line.transform;
                bool across = side < 2;
                rect.anchorMin = across ? new Vector2(0f, side) : new Vector2(side - 2, 0f);
                rect.anchorMax = across ? new Vector2(1f, side) : new Vector2(side - 2, 1f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = across ? new Vector2(0f, 2f) : new Vector2(2f, 0f);
                rect.anchoredPosition = Vector2.zero;
            }
            var button = frame.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            button.onClick.AddListener(map.Clicked);
            Tooltip.Attach(frame.gameObject, "The Land\nClick to look somewhere else.");
            return map;
        }

        private void Build()
        {
            HexGrid grid = manager.Game.State.map.grid;
            columns = grid.columns;
            rows = grid.rows;
            // Two texels to a cell across, so the odd rows can sit half a cell to the side like the hexagons do.
            texture = new Texture2D(columns * 2 + 1, rows, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            pixels = new Color32[texture.width * texture.height];
            image.texture = texture;
            grid.Size(out double width, out double height);
            float inner = element.preferredWidth - 2f * (UIKit.Inset((Image)element.GetComponent<Image>()).x + 1f);
            float aspect = (float)(height / width);
            element.preferredHeight = Mathf.Round(Mathf.Clamp(inner * aspect, inner * 0.6f, inner * 1.15f) + (element.preferredWidth - inner));
        }

        /// <summary>Paints the map: the ground of every cell the player has seen, and who holds what.</summary>
        public void Refresh(bool now = false)
        {
            if (manager == null || manager.Game == null)
            {
                return;
            }
            HexGrid grid = manager.Game.State.map.grid;
            if (texture == null || grid.columns != columns || grid.rows != rows)
            {
                if (texture != null)
                {
                    Destroy(texture);
                }
                Build();
                now = true;
            }
            Frame();
            if (!now && Time.unscaledTime < next)
            {
                return;
            }
            next = Time.unscaledTime + 0.4f;

            GameState state = manager.Game.State;
            PlayerState viewer = manager.ViewerState;
            int width = texture.width;
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Unknown;
            }
            for (int cell = 0; cell < grid.Count; cell++)
            {
                if (viewer != null && !viewer.Explored(cell))
                {
                    continue;
                }
                int row = grid.Row(cell);
                int x = 2 * grid.Column(cell) + 1 - (row & 1);
                Color32 ground = Ground(state, cell);
                pixels[row * width + x] = ground;
                pixels[row * width + x + 1] = ground;
            }
            texture.SetPixels32(pixels);
            texture.Apply(false);
            Markers(state, viewer);
        }

        /// <summary>Towns, mines and heroes the player has seen, as small squares and dots in their owners' colours.</summary>
        private void Markers(GameState state, PlayerState viewer)
        {
            int used = 0;
            foreach (MapObject what in state.objects)
            {
                bool marked = what.kind == ObjectKind.Town || what.kind == ObjectKind.Mine || what.kind == ObjectKind.Dwelling;
                if (what.removed || !marked || viewer != null && !viewer.Explored(what.cell))
                {
                    continue;
                }
                float size = what.kind == ObjectKind.Town ? 11f : 7f;
                Mark(used++, what.cell, HeroesArt.PlayerColor(ColorOf(state, what.owner)), size, false);
            }
            foreach (HeroState hero in state.heroes)
            {
                if (hero.alive && hero.cell >= 0 && (viewer == null || viewer.Explored(hero.cell)))
                {
                    bool mine = manager.Selected != null && manager.Selected.id == hero.id;
                    Mark(used++, hero.cell, HeroesArt.PlayerColor(ColorOf(state, hero.owner)), mine ? 9f : 7f, true);
                }
            }
            for (int i = used; i < marks.Count; i++)
            {
                marks[i].gameObject.SetActive(false);
            }
        }

        private void Mark(int index, int cell, Color color, float size, bool hero)
        {
            while (marks.Count <= index)
            {
                Image made = UIKit.Sprite(markers, "Mark", null, Color.white);
                made.rectTransform.anchorMin = made.rectTransform.anchorMax = Vector2.zero;
                made.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                Image core = UIKit.Sprite(made.transform, "Core", null, Color.white);
                UIKit.Stretch(core.rectTransform, 1.5f, 1.5f, 1.5f, 1.5f);
                marks.Add(made);
            }
            Image mark = marks[index];
            mark.gameObject.SetActive(true);
            // A rim around the colour so a marker reads on any ground: dark for a place, light for a hero.
            mark.color = hero ? new Color(1f, 0.95f, 0.82f) : new Color(0.08f, 0.05f, 0.02f, 0.95f);
            mark.transform.GetChild(0).GetComponent<Image>().color = color;
            mark.rectTransform.sizeDelta = new Vector2(size, size);
            mark.rectTransform.anchoredPosition = Position(cell);
        }

        /// <summary>Where the centre of a cell is on the little map, from its bottom left corner.</summary>
        private Vector2 Position(int cell)
        {
            HexGrid grid = manager.Game.State.map.grid;
            grid.Size(out double width, out double height);
            grid.Center(cell, out double x, out double y);
            Vector2 size = area.rect.size;
            return new Vector2((float)(x / width) * size.x, (float)(y / height) * size.y);
        }

        private static int ColorOf(GameState state, int player)
        {
            PlayerState owner = state.Player(player);
            return owner != null ? (int)owner.color : 4;
        }

        private static Color32 Ground(GameState state, int cell)
        {
            Obstacle obstacle = state.map.ObstacleAt(cell);
            if (obstacle == Obstacle.Mountain || obstacle == Obstacle.Edge)
            {
                return new Color32(104, 98, 92, 255);
            }
            if (obstacle == Obstacle.Forest)
            {
                return new Color32(38, 78, 40, 255);
            }
            if (state.map.HasRoad(cell))
            {
                return new Color32(150, 124, 86, 255);
            }
            switch (state.map.TerrainAt(cell))
            {
                case TerrainType.Water: return new Color32(44, 86, 128, 255);
                case TerrainType.Sand: return new Color32(206, 184, 120, 255);
                case TerrainType.Snow: return new Color32(222, 226, 232, 255);
                case TerrainType.Swamp: return new Color32(82, 98, 68, 255);
                case TerrainType.Rough: return new Color32(136, 118, 86, 255);
                case TerrainType.Wasteland: return new Color32(136, 86, 66, 255);
                case TerrainType.Dirt: return new Color32(124, 96, 64, 255);
                case TerrainType.Rock: return new Color32(92, 90, 94, 255);
                default: return new Color32(86, 130, 58, 255);
            }
        }

        /// <summary>Moves the gold box to what the camera sees.</summary>
        private void Frame()
        {
            if (manager.Rig == null || manager.Map == null)
            {
                box.gameObject.SetActive(false);
                return;
            }
            box.gameObject.SetActive(true);
            HexLayout layout = manager.Map.Layout;
            Vector2 world = layout.GridSize;
            Vector3 target = manager.Rig.Target;
            Vector2 size = area.rect.size;
            float x = (target.x - layout.Origin.x) / Mathf.Max(1f, world.x);
            float y = (target.z - layout.Origin.y) / Mathf.Max(1f, world.y);
            // What the camera takes in on the ground at its distance, at the rig's field of view and pitch.
            float distance = manager.Rig.Distance;
            float across = Mathf.Clamp01(distance * 1.36f / Mathf.Max(1f, world.x));
            float deep = Mathf.Clamp01(distance * 0.97f / Mathf.Max(1f, world.y));
            box.sizeDelta = new Vector2(Mathf.Max(8f, across * size.x), Mathf.Max(6f, deep * size.y));
            Vector2 half = box.sizeDelta * 0.5f;
            box.anchoredPosition = new Vector2(Mathf.Clamp(x * size.x, half.x, size.x - half.x),
                Mathf.Clamp(y * size.y, half.y, size.y - half.y));
        }

        private void Clicked()
        {
            if (manager.Map == null || manager.Rig == null)
            {
                return;
            }
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(area, Input.mousePosition, null, out Vector2 point))
            {
                return;
            }
            Rect rect = area.rect;
            HexLayout layout = manager.Map.Layout;
            float x = layout.Origin.x + Mathf.Clamp01((point.x - rect.xMin) / rect.width) * layout.GridSize.x;
            float z = layout.Origin.y + Mathf.Clamp01((point.y - rect.yMin) / rect.height) * layout.GridSize.y;
            manager.Rig.Focus(new Vector3(x, 0f, z));
        }

        private void OnDestroy()
        {
            if (texture != null)
            {
                Destroy(texture);
            }
        }
    }
}
