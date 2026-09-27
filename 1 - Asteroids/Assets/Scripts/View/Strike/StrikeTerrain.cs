using System.Collections.Generic;
using UnityEngine;

namespace Portfolio.Asteroids
{
    /// <summary>
    /// The scrolling ground of a strike mission: the tiles of the level's theme, depth-placed at the ground depth, tile i
    /// at logical centre y = Top + 20 i - 10 - distance, pooled per tile prefab and covering the whole view on any screen.
    /// Holds the crater decals (parented to the tile under them), and applies the theme's lighting while shown. When the
    /// camera re-fits (the window changed shape) the tiles are placed again, also while the scroll stands (pause, results);
    /// it runs after the <see cref="CameraRig"/> for that.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public class StrikeTerrain : MonoBehaviour
    {
        [Tooltip("The playfield (for the top edge).")]
        [SerializeField] internal Playground playground;
        [Tooltip("The scene's sun, lit by the theme while the terrain shows.")]
        [SerializeField] internal Light sun;
        [Tooltip("The crater decal prefab (a quad) the theme's crater material goes on.")]
        [SerializeField] internal GameObject craterPrefab;
        [Tooltip("Meters of ground kept beyond the edges of the view, so tiles never pop in.")]
        [SerializeField] internal float margin = 2f;

        private readonly Dictionary<TerrainTile, Stack<TerrainTile>> pools = new Dictionary<TerrainTile, Stack<TerrainTile>>();
        private readonly Dictionary<TerrainTile, TerrainTile> prefabOf = new Dictionary<TerrainTile, TerrainTile>();
        private readonly Dictionary<int, TerrainTile> shown = new Dictionary<int, TerrainTile>();
        private readonly Dictionary<TerrainTile, List<Transform>> decals = new Dictionary<TerrainTile, List<Transform>>();
        private readonly Stack<Transform> spareDecals = new Stack<Transform>();
        private readonly List<int> leaving = new List<int>();
        private Camera view;
        private int tileCount;
        private int decalCount;
        private float placedDepth;
        private bool lit;
        private Color savedSunColor;
        private float savedSunIntensity;
        private Quaternion savedSunRotation;
        private UnityEngine.Rendering.AmbientMode savedAmbientMode;
        private Color savedAmbient;

        /// <summary>Whether the terrain of a level is shown.</summary>
        public bool IsShown { get; private set; }

        /// <summary>The level shown; null while hidden.</summary>
        public StrikeLevel Level { get; private set; }

        /// <summary>The scroll distance the tiles were last placed for.</summary>
        public float Distance { get; private set; }

        /// <summary>The number of tiles placed now (tests and the tour log).</summary>
        public int TilesShown => shown.Count;

        /// <summary>The number of crater decals lying on the tiles now.</summary>
        public int CratersShown
        {
            get
            {
                int count = 0;
                foreach (List<Transform> list in decals.Values)
                {
                    count += list.Count;
                }
                return count;
            }
        }

        /// <summary>The theme's shadow colour for the drop shadows (a soft black when nothing is shown).</summary>
        public Color ShadowColor => Level != null && Level.Terrain != null ? Level.Terrain.ShadowColor : new Color(0f, 0f, 0f, 0.45f);

        private float Top => playground != null ? playground.Top : StrikeRules.HalfSize.y;

        private Light Sun => sun != null ? sun : RenderSettings.sun;


        /// <summary>
        /// Shows the terrain of <paramref name="level"/> at distance 0 and applies its theme's lighting. The tiles start
        /// clean also when the level is shown already (a retry does not keep the craters of the run before).
        /// </summary>
        public void Show(StrikeLevel level)
        {
            RecycleAll();
            Level = level;
            IsShown = level != null;
            if (!IsShown)
            {
                RestoreLighting();
                return;
            }
            tileCount = 0;
            foreach (TerrainSegment segment in level.Segments)
            {
                if (segment != null)
                {
                    tileCount += Mathf.Max(0, segment.tiles);
                }
            }
            ApplyLighting(level.Terrain);
            SetDistance(0f);
        }


        /// <summary>Hides the terrain, recycles its tiles and decals and hands the lighting back.</summary>
        public void Hide()
        {
            RecycleAll();
            RestoreLighting();
            Level = null;
            IsShown = false;
            Distance = 0f;
        }


        /// <summary>Places the tiles for scroll distance <paramref name="distance"/> (recycling what left the view).</summary>
        public void SetDistance(float distance)
        {
            Distance = distance;
            if (!IsShown || Level == null || Level.Terrain == null)
            {
                return;
            }
            placedDepth = DepthLayer.Distance;
            float half = ViewHalfHeight() + margin;
            float top = Top;
            // Tile i covers logical y in [top + 20 i - 20 - distance, top + 20 i - distance).
            int first = Mathf.FloorToInt((-half - top + distance) / StrikeRules.TileLength);
            int last = Mathf.CeilToInt((half - top + distance + StrikeRules.TileLength) / StrikeRules.TileLength);
            leaving.Clear();
            foreach (KeyValuePair<int, TerrainTile> pair in shown)
            {
                if (pair.Key < first || pair.Key > last)
                {
                    leaving.Add(pair.Key);
                }
            }
            foreach (int index in leaving)
            {
                Recycle(shown[index]);
                shown.Remove(index);
            }
            for (int i = first; i <= last; i++)
            {
                if (!shown.TryGetValue(i, out TerrainTile tile) || tile == null)
                {
                    tile = Take(i);
                    if (tile == null)
                    {
                        continue;
                    }
                    shown[i] = tile;
                }
                DepthLayer.Place(tile.transform, new Vector2(0f, CentreOf(i, distance)), StrikeRules.GroundDepth);
            }
        }


        /// <summary>The camera moved to another depth since the tiles were placed: they are placed again for it.</summary>
        private void LateUpdate()
        {
            if (IsShown && !Mathf.Approximately(DepthLayer.Distance, placedDepth))
            {
                SetDistance(Distance);
            }
        }


        /// <summary>A crater or rubble decal of <paramref name="size"/> at <paramref name="logical"/>, on the tile under it.</summary>
        public void AddCrater(Vector2 logical, float size)
        {
            if (!IsShown || craterPrefab == null || size <= 0f)
            {
                return;
            }
            int index = Mathf.FloorToInt((logical.y - Top + Distance + StrikeRules.TileLength) / StrikeRules.TileLength);
            if (!shown.TryGetValue(index, out TerrainTile tile) || tile == null)
            {
                return;
            }
            Transform decal = spareDecals.Count > 0 ? spareDecals.Pop() : NewDecal();
            // Tile local metres (the tile is scaled to look its logical size), a hair above the ground; later decals lie on top.
            decal.SetParent(tile.DecalRoot, false);
            decal.localPosition = new Vector3(logical.x, logical.y - CentreOf(index, Distance), -0.02f - 0.0005f * (decalCount % 16));
            decal.localRotation = Quaternion.Euler(0f, 0f, decalCount * 137.5f % 360f);
            decal.localScale = new Vector3(size, size, 1f);
            decalCount++;
            Material material = Level != null && Level.Terrain != null ? Level.Terrain.CraterMaterial : null;
            if (material != null && decal.TryGetComponent(out Renderer renderer))
            {
                renderer.sharedMaterial = material;
            }
            decal.gameObject.SetActive(true);
            if (!decals.TryGetValue(tile, out List<Transform> list))
            {
                list = new List<Transform>();
                decals[tile] = list;
            }
            list.Add(decal);
        }


        /// <summary>The tile at level coordinate <paramref name="s"/>; null when none is shown there.</summary>
        public TerrainTile TileAt(float s)
        {
            int index = Mathf.FloorToInt(s / StrikeRules.TileLength) + 1;
            return shown.TryGetValue(index, out TerrainTile tile) ? tile : null;
        }


        /// <summary>The logical y of the centre of tile <paramref name="index"/> at scroll distance <paramref name="distance"/>.</summary>
        private float CentreOf(int index, float distance)
        {
            return Top + StrikeRules.TileLength * index - StrikeRules.TileLength * 0.5f - distance;
        }


        /// <summary>Half the height the camera shows (narrow screens show more of the ground than the playfield).</summary>
        private float ViewHalfHeight()
        {
            if (view == null)
            {
                view = Camera.main;
            }
            float fieldOfView = view != null && !view.orthographic ? view.fieldOfView : 40f;
            float half = DepthLayer.Distance * Mathf.Tan(fieldOfView * 0.5f * Mathf.Deg2Rad);
            return Mathf.Max(half, StrikeRules.HalfSize.y);
        }


        /// <summary>An instance of the prefab of tile <paramref name="index"/> (tiles before and after the level repeat its ends).</summary>
        private TerrainTile Take(int index)
        {
            if (Level == null || Level.Terrain == null || tileCount <= 0)
            {
                return null;
            }
            TerrainSegment segment = Level.SegmentOfTile(Mathf.Clamp(index, 0, tileCount - 1));
            if (segment == null)
            {
                return null;
            }
            TerrainTile prefab = Level.Terrain.Tile(segment.kind, segment.variant);
            if (prefab == null)
            {
                return null;
            }
            if (!pools.TryGetValue(prefab, out Stack<TerrainTile> pool))
            {
                pool = new Stack<TerrainTile>();
                pools[prefab] = pool;
            }
            TerrainTile tile = null;
            while (pool.Count > 0 && tile == null)
            {
                tile = pool.Pop();
            }
            if (tile == null)
            {
                tile = Instantiate(prefab, transform);
                tile.name = prefab.name;
                prefabOf[tile] = prefab;
            }
            tile.gameObject.SetActive(true);
            return tile;
        }


        private void Recycle(TerrainTile tile)
        {
            if (tile == null)
            {
                return;
            }
            if (decals.TryGetValue(tile, out List<Transform> list))
            {
                foreach (Transform decal in list)
                {
                    if (decal != null)
                    {
                        decal.gameObject.SetActive(false);
                        decal.SetParent(transform, false);
                        spareDecals.Push(decal);
                    }
                }
                list.Clear();
            }
            tile.gameObject.SetActive(false);
            if (prefabOf.TryGetValue(tile, out TerrainTile prefab) && prefab != null)
            {
                if (!pools.TryGetValue(prefab, out Stack<TerrainTile> pool))
                {
                    pool = new Stack<TerrainTile>();
                    pools[prefab] = pool;
                }
                pool.Push(tile);
            }
        }


        private void RecycleAll()
        {
            foreach (TerrainTile tile in shown.Values)
            {
                Recycle(tile);
            }
            shown.Clear();
        }


        private Transform NewDecal()
        {
            GameObject decal = Instantiate(craterPrefab, transform);
            decal.name = craterPrefab.name;
            return decal.transform;
        }


        /// <summary>The theme's sun and ambient light replace the sector's while the ground shows (kept to hand back).</summary>
        private void ApplyLighting(StrikeTheme theme)
        {
            if (theme == null)
            {
                return;
            }
            Light light = Sun;
            if (!lit)
            {
                lit = true;
                if (light != null)
                {
                    savedSunColor = light.color;
                    savedSunIntensity = light.intensity;
                    savedSunRotation = light.transform.rotation;
                }
                savedAmbientMode = RenderSettings.ambientMode;
                savedAmbient = RenderSettings.ambientLight;
            }
            if (light != null)
            {
                light.color = theme.SunColor;
                light.intensity = theme.Night ? theme.SunIntensity * 0.45f : theme.SunIntensity;
                light.transform.rotation = Quaternion.Euler(theme.SunAngles);
            }
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = theme.Night ? theme.AmbientColor * 0.6f : theme.AmbientColor;
        }


        private void RestoreLighting()
        {
            if (!lit)
            {
                return;
            }
            lit = false;
            Light light = Sun;
            if (light != null)
            {
                light.color = savedSunColor;
                light.intensity = savedSunIntensity;
                light.transform.rotation = savedSunRotation;
            }
            RenderSettings.ambientMode = savedAmbientMode;
            RenderSettings.ambientLight = savedAmbient;
        }
    }
}
