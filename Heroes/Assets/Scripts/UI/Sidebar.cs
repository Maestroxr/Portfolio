using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Portfolio.Heroes.UI
{
    /// <summary>
    /// A line in the right hand column: a hero with the movement he has left and the mana he carries, or a town with
    /// what it can still build today. Clicking it takes the map there; clicking it again opens its screen.
    /// </summary>
    public sealed class HeroButton : MonoBehaviour
    {
        private HeroesGameManager manager;
        private HeroesUI ui;
        private Image frame;
        private Image portrait;
        private TextMeshProUGUI title;
        private TextMeshProUGUI detail;
        private Image movement;
        private bool isHero;
        private int id;

        public static HeroButton Make(Transform parent, HeroesGameManager manager, HeroesUI ui)
        {
            Image frame = UIKit.Panel(parent, "Entry");
            var button = frame.gameObject.AddComponent<Button>();
            button.targetGraphic = frame;
            button.colors = UIKit.Tint();
            UIKit.Fit((RectTransform)frame.transform, 0f, 78f, true);

            var entry = frame.gameObject.AddComponent<HeroButton>();
            entry.manager = manager;
            entry.ui = ui;
            entry.frame = frame;

            entry.portrait = UIKit.Sprite(frame.transform, "Portrait", null, Color.white);
            UIKit.Pin((RectTransform)entry.portrait.transform, new Vector2(0f, 0.5f), new Vector2(8f, 0f), new Vector2(60f, 60f));

            entry.title = UIKit.Label(frame.transform, "Title", "", 20f, UIKit.Ink, TextAlignmentOptions.TopLeft);
            UIKit.Pin((RectTransform)entry.title.transform, new Vector2(0f, 1f), new Vector2(76f, -10f), new Vector2(150f, 24f));
            entry.title.textWrappingMode = TextWrappingModes.NoWrap;
            entry.title.overflowMode = TextOverflowModes.Overflow;

            entry.detail = UIKit.Label(frame.transform, "Detail", "", 17f, UIKit.Dim, TextAlignmentOptions.TopLeft);
            UIKit.Pin((RectTransform)entry.detail.transform, new Vector2(0f, 1f), new Vector2(76f, -34f), new Vector2(150f, 22f));
            entry.detail.textWrappingMode = TextWrappingModes.NoWrap;

            entry.movement = UIKit.Bar(frame.transform, "Movement", new Color(0.55f, 0.85f, 0.45f));
            UIKit.Pin((RectTransform)entry.movement.transform.parent, new Vector2(0f, 0f), new Vector2(76f, 12f), new Vector2(150f, 10f));

            button.onClick.AddListener(entry.Clicked);
            frame.gameObject.AddComponent<Clicker>();
            return entry;
        }

        public void Show(bool hero, int which)
        {
            isHero = hero;
            id = which;
            gameObject.SetActive(true);
            Refresh();
        }

        private void Refresh()
        {
            GameState state = manager.Game.State;
            if (isHero)
            {
                HeroState hero = state.Hero(id);
                if (hero == null)
                {
                    gameObject.SetActive(false);
                    return;
                }
                bool picked = manager.Selected != null && manager.Selected.id == hero.id;
                frame.color = picked ? new Color(1f, 0.92f, 0.7f) : Color.white;
                title.text = hero.Name;
                detail.text = hero.sleeping ? "Asleep" : $"Level {hero.level}  •  {hero.army.TotalCreatures} troops";
                portrait.sprite = manager.Art.Icon("hero");
                portrait.color = HeroesArt.PlayerColor(ColorOf(state, hero.owner, 0));
                movement.fillAmount = hero.maxMovement > 0 ? Mathf.Clamp01(hero.movement / (float)hero.maxMovement) : 0f;
                movement.transform.parent.gameObject.SetActive(true);
            }
            else
            {
                TownState town = state.Town(id);
                if (town == null)
                {
                    gameObject.SetActive(false);
                    return;
                }
                frame.color = Color.white;
                title.text = town.name;
                detail.text = town.builtToday ? "Built today" : $"{Land.FactionName(town.faction)} town";
                portrait.sprite = manager.Art.Icon("kingdom");
                portrait.color = HeroesArt.PlayerColor(ColorOf(state, town.owner, 4));
                movement.transform.parent.gameObject.SetActive(false);
            }
        }

        private static int ColorOf(GameState state, int player, int fallback)
        {
            PlayerState owner = state.Player(player);
            return owner != null ? (int)owner.color : fallback;
        }

        private void Clicked()
        {
            GameState state = manager.Game.State;
            if (isHero)
            {
                HeroState hero = state.Hero(id);
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
            else
            {
                TownState town = state.Town(id);
                if (town != null)
                {
                    manager.Rig?.Focus(manager.Map.Point(town.center));
                    ui.Town.Open(town);
                }
            }
        }

        private void LateUpdate()
        {
            if (manager != null && manager.Game != null && isActiveAndEnabled)
            {
                Refresh();
            }
        }
    }

    /// <summary>
    /// The little map in the corner: one dot for every cell, in the color of what stands there, with a box showing
    /// where the camera is looking. Clicking it takes the camera somewhere else.
    /// </summary>
    public sealed class Minimap : MonoBehaviour
    {
        private HeroesGameManager manager;
        private RawImage image;
        private Texture2D texture;
        private RectTransform box;
        private Color32[] pixels;
        private float next;

        public static Minimap Make(RectTransform parent, HeroesGameManager manager)
        {
            RectTransform rect = UIKit.Rect(parent, "Map");
            UIKit.Stretch(rect, 18f, 18f, 18f, 18f);
            var map = rect.gameObject.AddComponent<Minimap>();
            map.manager = manager;
            map.image = rect.gameObject.AddComponent<RawImage>();
            map.image.raycastTarget = true;

            Image frame = UIKit.Sprite(rect, "View", null, new Color(1f, 1f, 1f, 0.5f));
            map.box = (RectTransform)frame.transform;
            map.box.pivot = new Vector2(0.5f, 0.5f);
            frame.color = new Color(1f, 0.95f, 0.7f, 0.35f);

            var button = rect.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(map.Clicked);
            return map;
        }

        private void Build()
        {
            HexGrid grid = manager.Game.State.map.grid;
            texture = new Texture2D(grid.columns, grid.rows, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            pixels = new Color32[grid.columns * grid.rows];
            image.texture = texture;
        }

        /// <summary>Paints the map: the ground of every cell the player has seen, and who holds what.</summary>
        public void Refresh()
        {
            if (manager?.Game == null)
            {
                return;
            }
            if (texture == null)
            {
                Build();
            }
            if (Time.unscaledTime < next)
            {
                return;
            }
            next = Time.unscaledTime + 0.4f;

            GameState state = manager.Game.State;
            PlayerState viewer = manager.ViewerState;
            HexGrid grid = state.map.grid;
            for (int cell = 0; cell < pixels.Length; cell++)
            {
                // The rows are drawn from the bottom up, the way the map lies.
                int row = grid.Row(cell);
                int index = (grid.rows - 1 - row) * grid.columns + grid.Column(cell);
                pixels[index] = viewer != null && !viewer.Explored(cell) ? new Color32(14, 14, 20, 255) : Ground(state, cell);
            }
            foreach (MapObject what in state.objects)
            {
                if (what.removed || what.owner < 0 || viewer != null && !viewer.Explored(what.cell))
                {
                    continue;
                }
                Mark(grid, what.cell, HeroesArt.PlayerColor(ColorOf(state, what.owner)));
            }
            foreach (HeroState hero in state.heroes)
            {
                if (hero.alive && hero.cell >= 0 && (viewer == null || viewer.Explored(hero.cell)))
                {
                    Mark(grid, hero.cell, Color.Lerp(HeroesArt.PlayerColor(ColorOf(state, hero.owner)), Color.white, 0.45f));
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply(false);
            Frame();
        }

        private static int ColorOf(GameState state, int player)
        {
            PlayerState owner = state.Player(player);
            return owner != null ? (int)owner.color : 4;
        }

        private void Mark(HexGrid grid, int cell, Color color)
        {
            int index = (grid.rows - 1 - grid.Row(cell)) * grid.columns + grid.Column(cell);
            if (index >= 0 && index < pixels.Length)
            {
                pixels[index] = color;
            }
        }

        private static Color32 Ground(GameState state, int cell)
        {
            if (state.map.ObstacleAt(cell) == Obstacle.Mountain || state.map.ObstacleAt(cell) == Obstacle.Edge)
            {
                return new Color32(90, 88, 92, 255);
            }
            if (state.map.ObstacleAt(cell) == Obstacle.Forest)
            {
                return new Color32(34, 68, 34, 255);
            }
            if (state.map.HasRoad(cell))
            {
                return new Color32(128, 108, 78, 255);
            }
            switch (state.map.TerrainAt(cell))
            {
                case TerrainType.Water: return new Color32(38, 74, 110, 255);
                case TerrainType.Sand: return new Color32(190, 170, 110, 255);
                case TerrainType.Snow: return new Color32(210, 214, 220, 255);
                case TerrainType.Swamp: return new Color32(72, 88, 62, 255);
                case TerrainType.Rough: return new Color32(122, 106, 80, 255);
                case TerrainType.Wasteland: return new Color32(120, 78, 62, 255);
                case TerrainType.Dirt: return new Color32(110, 86, 60, 255);
                case TerrainType.Rock: return new Color32(84, 82, 86, 255);
                default: return new Color32(74, 112, 52, 255);
            }
        }

        /// <summary>Moves the box to where the camera is looking.</summary>
        private void Frame()
        {
            if (manager.Rig == null || manager.Map == null)
            {
                return;
            }
            var rect = (RectTransform)transform;
            Vector2 size = manager.Map.Layout.GridSize;
            Vector3 target = manager.Rig.Target;
            float x = Mathf.Clamp01(target.x / Mathf.Max(1f, size.x));
            float y = Mathf.Clamp01(target.z / Mathf.Max(1f, size.y));
            Vector2 area = rect.rect.size;
            box.anchorMin = box.anchorMax = new Vector2(0.5f, 0.5f);
            box.anchoredPosition = new Vector2((x - 0.5f) * area.x, (y - 0.5f) * area.y);
            float span = manager.Rig.Distance * 0.9f;
            box.sizeDelta = new Vector2(area.x * Mathf.Clamp01(span / Mathf.Max(1f, size.x)),
                area.y * Mathf.Clamp01(span / Mathf.Max(1f, size.y)));
        }

        private void Clicked()
        {
            var rect = (RectTransform)transform;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, Input.mousePosition, null, out Vector2 point))
            {
                return;
            }
            Vector2 area = rect.rect.size;
            Vector2 size = manager.Map.Layout.GridSize;
            float x = Mathf.Clamp01((point.x - rect.rect.xMin) / area.x) * size.x;
            float z = Mathf.Clamp01((point.y - rect.rect.yMin) / area.y) * size.y;
            manager.Rig?.Focus(new Vector3(x, 0f, z));
        }
    }
}
