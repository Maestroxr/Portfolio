using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Portfolio.Heroes
{
    /// <summary>
    /// The map as it is seen: the terrain built from the cells, the water, the rocks and mountains standing on the
    /// blocked ones, the mines and treasures and towns, and the heroes travelling between them. It knows where every
    /// cell lies in the world and walks a hero along a path; the rules underneath never look at any of it.
    /// </summary>
    public sealed class MapView : MonoBehaviour
    {
        private HeroesArt art;
        private GameState state;
        private Transform scenery;
        private Transform props;
        private readonly Dictionary<int, ObjectView> objects = new Dictionary<int, ObjectView>();
        private readonly Dictionary<int, HeroView> heroes = new Dictionary<int, HeroView>();
        private float[] heights = System.Array.Empty<float>();

        public HexLayout Layout { get; private set; }

        public Terrain Terrain { get; private set; }

        public GridView Grid { get; private set; }

        public FogOfWar Fog { get; private set; }

        /// <summary>The ground a hero walks on, for the camera's picking.</summary>
        public int GroundMask => 1 << Terrain.gameObject.layer;

        // ------------------------------------------------------------------ building

        public void Build(GameState game, HeroesArt catalog, Camera view)
        {
            state = game;
            art = catalog;
            Layout = new HexLayout(game.map.grid, HexLayout.DefaultRadius, HexLayout.DefaultRadius * 2f);

            var builder = new TerrainBuilder(game.map, Layout, art);
            var terrainObject = new GameObject("Terrain");
            terrainObject.transform.SetParent(transform, false);
            Terrain = terrainObject.AddComponent<Terrain>();
            Terrain.terrainData = builder.Build();
            // The pipeline's own terrain material knows how to bind the splat maps; a hand made one does not.
            if (art.ground != null)
            {
                Terrain.materialTemplate = art.ground;
            }
            // Instanced terrain needs shader variants a player build strips, and comes out flat and blank.
            Terrain.drawInstanced = false;
            Terrain.heightmapPixelError = 4f;
            Terrain.detailObjectDistance = 120f;
            Terrain.treeDistance = 600f;
            // The whole map is close enough to see, so the trees are never flat billboards.
            Terrain.treeBillboardDistance = 600f;
            terrainObject.AddComponent<TerrainCollider>().terrainData = Terrain.terrainData;
            terrainObject.transform.position = Layout.TerrainPosition;

            Heights();
            Water();
            Scenery();

            Grid = gameObject.AddComponent<GridView>();
            Grid.Setup(Terrain, Layout);

            Fog = gameObject.AddComponent<FogOfWar>();
            Fog.Setup(Layout, view, art.fog);

            props = new GameObject("Objects").transform;
            props.SetParent(transform, false);
            foreach (MapObject what in state.objects)
            {
                Spawn(what);
            }
            foreach (HeroState hero in state.heroes)
            {
                if (hero.alive && hero.cell >= 0)
                {
                    Place(hero);
                }
            }
        }

        /// <summary>The ground height of every cell, sampled once so nothing has to ask the terrain again.</summary>
        private void Heights()
        {
            heights = new float[state.map.grid.Count];
            for (int cell = 0; cell < heights.Length; cell++)
            {
                Vector3 flat = Layout.Flat(cell);
                heights[cell] = Terrain.SampleHeight(flat);
            }
        }

        private void Water()
        {
            if (art.water == null)
            {
                return;
            }
            var water = GameObject.CreatePrimitive(PrimitiveType.Plane);
            water.name = "Water";
            Destroy(water.GetComponent<Collider>());
            water.transform.SetParent(transform, false);
            Vector2 size = Layout.TerrainSize;
            water.transform.localScale = new Vector3(size.x * 0.1f + 2f, 1f, size.y * 0.1f + 2f);
            water.transform.position = new Vector3(size.x * 0.5f, TerrainBuilder.WaterLevel, size.y * 0.5f);
            water.GetComponent<Renderer>().sharedMaterial = art.water;
        }

        /// <summary>Stands the rocks, the mountains and the reeds on the cells no army can cross.</summary>
        private void Scenery()
        {
            scenery = new GameObject("Scenery").transform;
            scenery.SetParent(transform, false);
            for (int cell = 0; cell < state.map.grid.Count; cell++)
            {
                Obstacle obstacle = state.map.ObstacleAt(cell);
                GameObject[] choices;
                switch (obstacle)
                {
                    case Obstacle.Rocks:
                        choices = art.rocks;
                        break;
                    case Obstacle.Mountain:
                    case Obstacle.Edge:
                        // The terrain raises these cells into cliffs of its own; a tile on top would only float.
                        continue;
                    case Obstacle.Lake:
                        choices = art.waterPlants;
                        break;
                    default:
                        continue;
                }
                if (choices == null || choices.Length == 0)
                {
                    continue;
                }
                uint hash = TerrainBuilder.Hash(cell, 31);
                GameObject prefab = choices[hash % (uint)choices.Length];
                GameObject piece = Instantiate(prefab, scenery);
                piece.transform.position = Point(cell);
                piece.transform.rotation = Quaternion.Euler(0f, (hash >> 8) % 360, 0f);
                float scale = obstacle == Obstacle.Rocks ? 0.8f + (hash >> 16) % 60 / 100f : 1f;
                piece.transform.localScale *= scale;
                if (obstacle == Obstacle.Lake)
                {
                    piece.transform.position = new Vector3(piece.transform.position.x, TerrainBuilder.WaterLevel - 0.05f,
                        piece.transform.position.z);
                }
            }
        }

        // ------------------------------------------------------------------ where things are

        /// <summary>The middle of a cell, on the ground.</summary>
        public Vector3 Point(int cell)
        {
            Vector3 flat = Layout.Flat(cell);
            flat.y = cell >= 0 && cell < heights.Length ? heights[cell] : Terrain.SampleHeight(flat);
            return flat;
        }

        public Vector3 Point(int cell, float lift)
        {
            Vector3 point = Point(cell);
            point.y += lift;
            return point;
        }

        public int CellAt(Vector3 world)
        {
            return Layout.CellAt(world);
        }

        public HeroView Hero(int id)
        {
            return heroes.TryGetValue(id, out HeroView view) ? view : null;
        }

        public ObjectView Object(int id)
        {
            return objects.TryGetValue(id, out ObjectView view) ? view : null;
        }

        /// <summary>The point a label or a banner floats over for a cell.</summary>
        public Vector3 Over(int cell)
        {
            return Point(cell, 2.2f);
        }

        // ------------------------------------------------------------------ what stands on the map

        private void Spawn(MapObject what)
        {
            if (what.removed || objects.ContainsKey(what.id))
            {
                return;
            }
            GameObject prefab = what.kind == ObjectKind.Town
                ? TownPrefab(what)
                : what.kind == ObjectKind.Monster
                    ? null
                    : art.Object(what.kind, what.subtype);
            var view = new GameObject(MapObjects.Name(what.kind)).AddComponent<ObjectView>();
            view.transform.SetParent(props, false);
            view.transform.position = Point(what.cell);
            view.transform.rotation = Quaternion.Euler(0f, TerrainBuilder.Hash(what.cell, 7) % 24 - 12f, 0f);
            view.Setup(art, what, prefab, new Vector3(1.1f, 0f, -0.9f));
            objects[what.id] = view;

            if (what.kind == ObjectKind.Monster && what.subtype >= 0)
            {
                // A wandering army shows the creature that leads it.
                var unit = new GameObject("Guard").AddComponent<UnitView>();
                unit.transform.SetParent(view.transform, false);
                unit.Setup(art, (CreatureId)what.subtype);
                unit.Cell = what.cell;
                unit.Face(Vector3.back);
            }
        }

        private GameObject TownPrefab(MapObject what)
        {
            TownState town = state.Town(what.subtype);
            if (town == null)
            {
                return null;
            }
            return art.Town(town.faction, town.owner);
        }

        public void Place(HeroState hero)
        {
            if (heroes.ContainsKey(hero.id))
            {
                return;
            }
            var view = new GameObject($"Hero {hero.Name}").AddComponent<HeroView>();
            view.transform.SetParent(transform, false);
            view.transform.position = Point(hero.cell);
            view.Setup(art, hero);
            view.Face(Facing(hero.facing));
            heroes[hero.id] = view;
        }

        public void Remove(int heroId)
        {
            if (heroes.TryGetValue(heroId, out HeroView view))
            {
                heroes.Remove(heroId);
                Destroy(view.gameObject);
            }
        }

        /// <summary>The direction a hero is turned toward, from the neighbour he last stepped to.</summary>
        public Vector3 Facing(int facing)
        {
            float angle = 90f - facing * 60f;
            return new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), 0f, Mathf.Sin(angle * Mathf.Deg2Rad));
        }

        /// <summary>Brings the view back in line with the rules: owners, heroes raised or lost, objects taken.</summary>
        public void Sync()
        {
            foreach (MapObject what in state.objects)
            {
                if (what.removed)
                {
                    if (objects.TryGetValue(what.id, out ObjectView gone))
                    {
                        objects.Remove(what.id);
                        Destroy(gone.gameObject);
                    }
                    continue;
                }
                if (!objects.TryGetValue(what.id, out ObjectView view))
                {
                    Spawn(what);
                    continue;
                }
                if (what.kind == ObjectKind.Town)
                {
                    TownState town = state.Town(what.subtype);
                    if (town != null && view.Owner != town.owner)
                    {
                        // A taken town raises the colors of whoever holds it now.
                        objects.Remove(what.id);
                        Destroy(view.gameObject);
                        Spawn(what);
                        continue;
                    }
                }
                view.SetOwner(art, what.owner);
            }

            foreach (HeroState hero in state.heroes)
            {
                if (hero.alive && hero.cell >= 0)
                {
                    Place(hero);
                    HeroView view = Hero(hero.id);
                    if (view != null && view.Cell != hero.cell)
                    {
                        view.Cell = hero.cell;
                        view.transform.position = Point(hero.cell);
                    }
                }
                else
                {
                    Remove(hero.id);
                }
            }
            Fog.MarkDirty();
        }

        // ------------------------------------------------------------------ travel

        /// <summary>Walks a hero along the cells of a path, one step at a time, and keeps the fog opening ahead of him.</summary>
        public IEnumerator Walk(HeroState hero, IReadOnlyList<int> path, float speed, System.Action<int> onStep = null)
        {
            HeroView view = Hero(hero.id);
            if (view == null || path == null || path.Count == 0)
            {
                yield break;
            }
            foreach (int cell in path)
            {
                Vector3 point = Point(cell);
                yield return view.WalkTo(point, speed);
                view.Cell = cell;
                Fog.MarkDirty();
                onStep?.Invoke(cell);
            }
        }

        /// <summary>Slides a hero straight to a cell, for a step the rules made without a walk (a gate, a boat).</summary>
        public void Warp(int heroId, int cell)
        {
            HeroView view = Hero(heroId);
            if (view != null)
            {
                view.Cell = cell;
                view.transform.position = Point(cell);
                Fog.MarkDirty();
            }
        }
    }
}
