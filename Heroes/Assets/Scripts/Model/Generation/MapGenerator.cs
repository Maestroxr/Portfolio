using System;
using System.Collections.Generic;

namespace Portfolio.Heroes
{
    /// <summary>
    /// Makes a playable map from a <see cref="MapSpec"/>, the same one on every device (whole number arithmetic and the
    /// seeded random numbers only). The land is split into zones: a home zone per player with its town, and neutral zones
    /// between them. Mountains and forests wall the zones off except for a few passes, which monsters guard; the
    /// zones are filled with woods and rocks, lakes, mines, treasures, shrines and dwellings, the richer and the better
    /// guarded the farther they lie from the homes. Roads join the towns through the passes.
    /// </summary>
    public sealed class MapGenerator
    {
        private sealed class Zone
        {
            public int index;
            public int center;
            public int owner = -1;
            public TerrainType terrain;
            /// <summary>Steps through the passes to the nearest home zone (0 for a home).</summary>
            public int depth;
            public readonly List<int> cells = new List<int>();
            public readonly List<int> gates = new List<int>();
            public readonly HashSet<int> touching = new HashSet<int>();
            public int town = -1;
        }

        private readonly MapSpec spec;
        private readonly SeededRandom random;
        private readonly GameState state;
        private readonly MapData map;
        private readonly HexGrid grid;
        private readonly List<Zone> zones = new List<Zone>();
        private readonly HashSet<int> gateCells = new HashSet<int>();
        private readonly List<int> scratch = new List<int>(6);
        private bool[] reachable = Array.Empty<bool>();
        private HeroesGame game;

        private static readonly string[][] TownNames =
        {
            new[] { "Highcrest", "Aldermoor", "Brightwater", "Stonehaven", "Kingsford", "Silverkeep", "Rosewall", "Ashbury" },
            new[] { "Gravemire", "Dreadhollow", "Blackspire", "Ossuary", "Nightfen", "Vilecourt", "Mourncrypt", "Duskbarrow" },
            new[] { "Ironclaw", "Emberhold", "Skullrock", "Warhorn", "Thunderpeak", "Redfang", "Bonefire", "Ragecliff" },
        };

        private static readonly string[] PlayerNames = { "Red", "Blue", "Green", "Tan" };

        public static GameState Generate(MapSpec spec)
        {
            return new MapGenerator(spec).Run();
        }

        private MapGenerator(MapSpec spec)
        {
            this.spec = spec;
            random = new SeededRandom(spec.seed);
            state = new GameState { seed = spec.seed, scenario = spec.name };
            map = state.map;
            map.Allocate(Math.Max(16, spec.columns), Math.Max(16, spec.rows));
            grid = map.grid;
        }

        private GameState Run()
        {
            CreatePlayers();
            PlaceZones();
            AssignZones();
            PaintTerrain();
            BuildBorders();
            ScatterObstacles();
            PlaceTowns();
            CarveConnections();
            BuildRoads();
            RemovePockets();
            game = new HeroesGame(state, random);
            PlaceSpecials();
            PlaceMines();
            PlaceGateGuards();
            PlaceTreasures();
            PlaceBuildings();
            PlaceWanderers();
            PlaceHeroes();
            ShapeHeights();
            ResolveRules();
            return state;
        }

        // ------------------------------------------------------------------ players

        private void CreatePlayers()
        {
            for (int i = 0; i < spec.players.Count && i < 4; i++)
            {
                PlayerSpec seat = spec.players[i];
                var player = new PlayerState
                {
                    index = i,
                    name = string.IsNullOrEmpty(seat.name) ? PlayerNames[i] : seat.name,
                    color = (PlayerColor)i,
                    faction = seat.faction == Faction.Neutral ? Faction.Castle : seat.faction,
                    human = seat.human,
                    aiLevel = seat.aiLevel,
                    team = seat.team,
                    explored = new byte[grid.Count]
                };
                int gold = spec.startingGold;
                int common = 20;
                int rare = 5;
                if (!seat.human)
                {
                    gold = gold * (seat.aiLevel == 0 ? 60 : seat.aiLevel == 2 ? 130 : 100) / 100;
                }
                player.resources = new ResourceSet(gold, common, common, rare, rare, rare, rare);
                state.players.Add(player);
            }
            for (int i = 0; i < HeroData.RosterCount; i++)
            {
                state.freeHeroes.Add(i);
            }
        }

        // ------------------------------------------------------------------ zones

        private int CellAt(int x1000, int y1000)
        {
            int column = Clamp(x1000 * (grid.columns - 1) / 1000, 1, grid.columns - 2);
            int row = Clamp(y1000 * (grid.rows - 1) / 1000, 1, grid.rows - 2);
            return grid.Index(column, row);
        }

        private static int Clamp(int value, int min, int max)
        {
            return value < min ? min : value > max ? max : value;
        }

        private static readonly int[][][] HomeLayouts =
        {
            new[] { new[] { 500, 250 } },
            new[] { new[] { 200, 210 }, new[] { 800, 790 } },
            new[] { new[] { 180, 200 }, new[] { 820, 240 }, new[] { 500, 830 } },
            new[] { new[] { 180, 180 }, new[] { 820, 820 }, new[] { 820, 180 }, new[] { 180, 820 } },
        };

        private void PlaceZones()
        {
            int players = Math.Max(1, state.players.Count);
            int[][] homes = HomeLayouts[Math.Min(players, 4) - 1];
            for (int i = 0; i < players; i++)
            {
                int x = homes[i][0] + random.Range(-40, 41);
                int y = homes[i][1] + random.Range(-40, 41);
                zones.Add(new Zone { index = zones.Count, center = CellAt(x, y), owner = i });
            }
            // The middle, then neutral zones where they are farthest from everything placed so far.
            zones.Add(new Zone { index = zones.Count, center = CellAt(500 + random.Range(-40, 41), 500 + random.Range(-40, 41)) });
            int neutral = Math.Max(1, players * Math.Max(0, spec.zonesPerPlayer));
            var candidates = new List<int>();
            for (int gx = 100; gx <= 900; gx += 100)
            {
                for (int gy = 100; gy <= 900; gy += 100)
                {
                    candidates.Add(CellAt(gx + random.Range(-30, 31), gy + random.Range(-30, 31)));
                }
            }
            for (int n = 0; n < neutral; n++)
            {
                int best = -1;
                int bestDistance = -1;
                foreach (int cell in candidates)
                {
                    int nearest = int.MaxValue;
                    foreach (Zone zone in zones)
                    {
                        nearest = Math.Min(nearest, grid.Distance2x4(cell, zone.center));
                    }
                    if (nearest > bestDistance)
                    {
                        bestDistance = nearest;
                        best = cell;
                    }
                }
                if (best < 0)
                {
                    break;
                }
                candidates.Remove(best);
                zones.Add(new Zone { index = zones.Count, center = best });
            }
        }

        private void AssignZones()
        {
            for (int cell = 0; cell < grid.Count; cell++)
            {
                int wobble = Noise.At(grid, cell, 10, 11u) - 500;
                int best = 0;
                long bestDistance = long.MaxValue;
                foreach (Zone zone in zones)
                {
                    int bias = (Noise.At(grid, cell, 14, 101u + (uint)zone.index) - 500) * 2;
                    long d = grid.Distance2x4(cell, zone.center) * 10L + bias + wobble;
                    if (d < bestDistance)
                    {
                        bestDistance = d;
                        best = zone.index;
                    }
                }
                map.zone[cell] = (byte)best;
                zones[best].cells.Add(cell);
            }
            for (int cell = 0; cell < grid.Count; cell++)
            {
                grid.Neighbors(cell, scratch);
                foreach (int next in scratch)
                {
                    if (map.zone[next] != map.zone[cell])
                    {
                        zones[map.zone[cell]].touching.Add(map.zone[next]);
                    }
                }
            }
        }

        private void PaintTerrain()
        {
            var pool = new List<TerrainType> { TerrainType.Grass, TerrainType.Dirt, TerrainType.Sand, TerrainType.Snow, TerrainType.Swamp, TerrainType.Rough, TerrainType.Grass };
            foreach (Zone zone in zones)
            {
                if (zone.owner >= 0)
                {
                    zone.terrain = Land.Native(state.players[zone.owner].faction);
                }
                else if (spec.theme != 0 && random.Range(0, 100) < 70)
                {
                    zone.terrain = spec.themeTerrain;
                }
                else
                {
                    zone.terrain = pool[random.Range(0, pool.Count)];
                }
            }
            for (int cell = 0; cell < grid.Count; cell++)
            {
                map.terrain[cell] = (byte)zones[map.zone[cell]].terrain;
            }
        }

        private bool IsBorder(int cell)
        {
            grid.Neighbors(cell, scratch);
            if (scratch.Count < 6)
            {
                return true;
            }
            foreach (int next in scratch)
            {
                if (map.zone[next] != map.zone[cell])
                {
                    return true;
                }
            }
            return false;
        }

        private void BuildBorders()
        {
            // Walls between the zones: mountains mostly, forests where the noise says so.
            for (int cell = 0; cell < grid.Count; cell++)
            {
                int column = grid.Column(cell);
                int row = grid.Row(cell);
                bool edge = column == 0 || row == 0 || column == grid.columns - 1 || row == grid.rows - 1;
                if (edge)
                {
                    map.obstacle[cell] = (byte)Obstacle.Edge;
                    continue;
                }
                if (IsBorder(cell))
                {
                    int n = Noise.At(grid, cell, 6, 23u);
                    map.obstacle[cell] = (byte)(n < 420 ? Obstacle.Forest : n < 800 ? Obstacle.Mountain : Obstacle.Rocks);
                    if ((TerrainType)map.terrain[cell] == TerrainType.Swamp || (TerrainType)map.terrain[cell] == TerrainType.Wasteland)
                    {
                        if (map.obstacle[cell] == (byte)Obstacle.Forest)
                        {
                            map.obstacle[cell] = (byte)Obstacle.DeadTrees;
                        }
                    }
                }
            }
            // A second ring inside the edge: forest now and then, so the map fades out into wilderness.
            for (int cell = 0; cell < grid.Count; cell++)
            {
                int column = grid.Column(cell);
                int row = grid.Row(cell);
                bool nearEdge = column == 1 || row == 1 || column == grid.columns - 2 || row == grid.rows - 2;
                if (nearEdge && map.obstacle[cell] == 0 && Noise.At(grid, cell, 4, 31u) < 450)
                {
                    map.obstacle[cell] = (byte)Obstacle.Forest;
                }
            }
            // Passes between touching zones, where the border comes closest to the line between their centers.
            var done = new HashSet<long>();
            foreach (Zone a in zones)
            {
                // In a fixed order: a set's order is not the same everywhere, and the order of the passes shapes the roads.
                var touching = new List<int>(a.touching);
                touching.Sort();
                foreach (int bIndex in touching)
                {
                    Zone b = zones[bIndex];
                    long key = Math.Min(a.index, b.index) * 1000L + Math.Max(a.index, b.index);
                    if (!done.Add(key))
                    {
                        continue;
                    }
                    MakeGate(a, b);
                }
            }
        }

        private void MakeGate(Zone a, Zone b)
        {
            // The border cell pair closest to the midpoint of the two centers.
            int ax = grid.X2(a.center), ay = grid.Row(a.center);
            int bx = grid.X2(b.center), by = grid.Row(b.center);
            int mx = (ax + bx) / 2, my = (ay + by) / 2;
            int best = -1;
            long bestDistance = long.MaxValue;
            foreach (int cell in a.cells)
            {
                grid.Neighbors(cell, scratch);
                bool touches = false;
                foreach (int next in scratch)
                {
                    touches |= map.zone[next] == b.index;
                }
                if (!touches || grid.Column(cell) < 2 || grid.Row(cell) < 2 || grid.Column(cell) > grid.columns - 3 || grid.Row(cell) > grid.rows - 3)
                {
                    continue;
                }
                int dx = grid.X2(cell) - mx;
                int dy = grid.Row(cell) - my;
                long d = 3L * dx * dx + 9L * dy * dy;
                if (d < bestDistance || (d == bestDistance && cell < best))
                {
                    bestDistance = d;
                    best = cell;
                }
            }
            if (best < 0)
            {
                return;
            }
            foreach (int cell in grid.Disk(best, 1))
            {
                if (map.obstacle[cell] != (byte)Obstacle.Edge)
                {
                    map.obstacle[cell] = 0;
                    gateCells.Add(cell);
                }
            }
            a.gates.Add(best);
            b.gates.Add(best);
        }

        private void ScatterObstacles()
        {
            int lakes = spec.water;
            foreach (Zone zone in zones)
            {
                // Lakes in the neutral zones.
                int count = zone.owner >= 0 ? (lakes > 1 ? 1 : 0) : lakes;
                for (int i = 0; i < count; i++)
                {
                    int center = zone.cells[random.Range(0, zone.cells.Count)];
                    if (grid.Distance(center, zone.center) < 4 || IsBorder(center))
                    {
                        continue;
                    }
                    int radius = 1 + random.Range(0, 3);
                    foreach (int cell in grid.Disk(center, radius + 1))
                    {
                        if (map.zone[cell] != zone.index || gateCells.Contains(cell) || map.obstacle[cell] == (byte)Obstacle.Edge)
                        {
                            continue;
                        }
                        bool inside = grid.Distance(cell, center) <= radius || Noise.At(grid, cell, 3, 41u) < 450;
                        if (inside)
                        {
                            map.terrain[cell] = (byte)TerrainType.Water;
                            map.obstacle[cell] = (byte)Obstacle.Lake;
                        }
                    }
                }
            }
            // Woods and rocks inside the zones, in clusters.
            for (int cell = 0; cell < grid.Count; cell++)
            {
                if (map.obstacle[cell] != 0 || gateCells.Contains(cell))
                {
                    continue;
                }
                Zone zone = zones[map.zone[cell]];
                int woods = Noise.At(grid, cell, 7, 53u);
                int rocks = Noise.At(grid, cell, 5, 67u);
                int threshold = zone.owner >= 0 ? 690 : 650;
                if (woods > threshold)
                {
                    TerrainType land = (TerrainType)map.terrain[cell];
                    map.obstacle[cell] = (byte)(land == TerrainType.Swamp || land == TerrainType.Wasteland || land == TerrainType.Sand ? Obstacle.DeadTrees : Obstacle.Forest);
                }
                else if (rocks > 820)
                {
                    map.obstacle[cell] = (byte)(rocks > 880 ? Obstacle.Mountain : Obstacle.Rocks);
                }
            }
        }

        // ------------------------------------------------------------------ towns

        private void PlaceTowns()
        {
            foreach (Zone zone in zones)
            {
                if (zone.owner >= 0)
                {
                    zone.town = PlaceTown(zone, state.players[zone.owner].faction, zone.owner);
                }
            }
            // Neutral towns in the neutral zones nearest the middle.
            var neutral = new List<Zone>();
            foreach (Zone zone in zones)
            {
                if (zone.owner < 0)
                {
                    neutral.Add(zone);
                }
            }
            for (int i = 0; i < spec.neutralTowns && i < neutral.Count; i++)
            {
                Zone zone = neutral[neutral.Count - 1 - i];
                var faction = (Faction)random.Range(0, 3);
                zone.town = PlaceTown(zone, faction, -1);
            }
        }

        private int PlaceTown(Zone zone, Faction faction, int owner)
        {
            // Look for room near the zone's center: seven cells for the town and a clearing around it.
            int center = zone.center;
            int best = -1;
            int bestScore = int.MaxValue;
            foreach (int cell in grid.Disk(zone.center, 4))
            {
                if (map.zone[cell] != zone.index || IsBorder(cell) || grid.Column(cell) < 4 || grid.Row(cell) < 4 || grid.Column(cell) > grid.columns - 5 || grid.Row(cell) > grid.rows - 5)
                {
                    continue;
                }
                int score = grid.Distance(cell, zone.center) * 10;
                foreach (int near in grid.Disk(cell, 2))
                {
                    if (map.zone[near] != zone.index)
                    {
                        score += 30;
                    }
                }
                if (score < bestScore)
                {
                    bestScore = score;
                    best = cell;
                }
            }
            if (best >= 0)
            {
                center = best;
            }
            foreach (int cell in grid.Disk(center, 3))
            {
                if (map.obstacle[cell] == (byte)Obstacle.Edge)
                {
                    continue;
                }
                map.obstacle[cell] = 0;
                if ((TerrainType)map.terrain[cell] == TerrainType.Water)
                {
                    map.terrain[cell] = (byte)zone.terrain;
                }
            }
            int gate = grid.Neighbor(center, HexSide.SouthEast);
            var obj = new MapObject { id = state.objects.Count, kind = ObjectKind.Town, cell = gate, owner = owner };
            obj.footprint.AddRange(grid.Disk(center, 1));
            state.objects.Add(obj);
            foreach (int cell in obj.footprint)
            {
                map.occupant[cell] = obj.id;
            }
            string[] names = TownNames[(int)faction];
            int townId = state.towns.Count;
            var town = new TownState
            {
                id = townId,
                objectId = obj.id,
                name = names[(townId * 3 + (int)(spec.seed % 5)) % names.Length],
                faction = faction,
                owner = owner,
                cell = gate,
                center = center
            };
            obj.subtype = townId;
            town.built.Add((int)BuildingId.VillageHall);
            town.built.Add((int)BuildingId.Fort);
            town.built.Add((int)BuildingId.Tavern);
            town.built.Add((int)BuildingId.Dwelling1);
            town.built.Add((int)BuildingId.Dwelling2);
            if (owner >= 0)
            {
                foreach (BuildingId extra in spec.players[owner].buildings)
                {
                    if (!town.Has(extra))
                    {
                        town.built.Add((int)extra);
                    }
                }
            }
            else
            {
                town.built.Add((int)BuildingId.Dwelling3);
                // A garrison of the town's own creatures.
                int budget = 2500 + 1500 * zone.depth * spec.monsters / 2;
                for (int tier = 1; tier <= 4; tier++)
                {
                    CreatureDef def = Creatures.Get(Creatures.OfTier(faction, tier));
                    int count = Math.Max(1, budget / 4 / def.Value);
                    town.garrison.Add((int)def.Id, count);
                }
            }
            for (int tier = 1; tier <= 7; tier++)
            {
                if (town.Has(Buildings.DwellingOf(tier)))
                {
                    town.available[tier - 1] = Creatures.Get(Creatures.OfTier(faction, tier)).Growth;
                }
            }
            state.towns.Add(town);
            if (owner >= 0)
            {
                state.players[owner].towns.Add(townId);
            }
            return townId;
        }

        // ------------------------------------------------------------------ connections

        private bool Walkable(int cell)
        {
            return map.Open(cell) && (map.occupant[cell] < 0 || gateOfTown(cell));
        }

        private bool gateOfTown(int cell)
        {
            foreach (TownState town in state.towns)
            {
                if (town.cell == cell)
                {
                    return true;
                }
            }
            return false;
        }

        private void Flood(int start)
        {
            if (reachable.Length != grid.Count)
            {
                reachable = new bool[grid.Count];
            }
            Array.Clear(reachable, 0, reachable.Length);
            var queue = new Queue<int>();
            if (!Walkable(start))
            {
                return;
            }
            reachable[start] = true;
            queue.Enqueue(start);
            var around = new List<int>(6);
            while (queue.Count > 0)
            {
                int cell = queue.Dequeue();
                grid.Neighbors(cell, around);
                foreach (int next in around)
                {
                    if (!reachable[next] && Walkable(next))
                    {
                        reachable[next] = true;
                        queue.Enqueue(next);
                    }
                }
            }
        }

        /// <summary>Makes sure every zone center and every town can be walked to from the first town.</summary>
        private void CarveConnections()
        {
            int start = state.towns.Count > 0 ? state.towns[0].cell : zones[0].center;
            for (int pass = 0; pass < zones.Count + 2; pass++)
            {
                Flood(start);
                Zone missing = null;
                foreach (Zone zone in zones)
                {
                    int target = zone.town >= 0 ? state.towns[zone.town].cell : zone.center;
                    if (!reachable[target])
                    {
                        missing = zone;
                        break;
                    }
                }
                if (missing == null)
                {
                    return;
                }
                int goal = missing.town >= 0 ? state.towns[missing.town].cell : missing.center;
                if (!map.Open(goal) && map.occupant[goal] < 0)
                {
                    map.obstacle[goal] = 0;
                    if ((TerrainType)map.terrain[goal] == TerrainType.Water)
                    {
                        map.terrain[goal] = (byte)missing.terrain;
                    }
                }
                // Walk from the unreachable place toward the nearest reachable cell, clearing the way.
                int nearest = -1;
                int nearestDistance = int.MaxValue;
                for (int cell = 0; cell < grid.Count; cell++)
                {
                    if (reachable[cell])
                    {
                        int d = grid.Distance(cell, goal);
                        if (d < nearestDistance)
                        {
                            nearestDistance = d;
                            nearest = cell;
                        }
                    }
                }
                if (nearest < 0)
                {
                    return;
                }
                int current = goal;
                for (int guard = 0; guard < 200 && current != nearest; guard++)
                {
                    grid.Neighbors(current, scratch);
                    int step = current;
                    int stepDistance = int.MaxValue;
                    foreach (int next in scratch)
                    {
                        int d = grid.Distance2x4(next, nearest);
                        if (d < stepDistance)
                        {
                            stepDistance = d;
                            step = next;
                        }
                    }
                    current = step;
                    if (map.occupant[current] < 0 && map.obstacle[current] != (byte)Obstacle.Edge)
                    {
                        map.obstacle[current] = 0;
                        if ((TerrainType)map.terrain[current] == TerrainType.Water)
                        {
                            map.terrain[current] = (byte)missing.terrain;
                        }
                        gateCells.Add(current);
                    }
                }
            }
        }

        /// <summary>Open land nobody can walk to becomes forest, so nothing is placed out of reach.</summary>
        private void RemovePockets()
        {
            int start = state.towns.Count > 0 ? state.towns[0].cell : zones[0].center;
            Flood(start);
            for (int cell = 0; cell < grid.Count; cell++)
            {
                if (!reachable[cell] && map.Open(cell) && map.occupant[cell] < 0)
                {
                    map.obstacle[cell] = (byte)Obstacle.Forest;
                }
            }
        }

        private void BuildRoads()
        {
            foreach (Zone zone in zones)
            {
                int hub = zone.town >= 0 ? state.towns[zone.town].cell : zone.center;
                foreach (int gate in zone.gates)
                {
                    Road(hub, gate);
                }
            }
            // Depth of every zone from the homes through the passes, for the value of what lies in it.
            foreach (Zone zone in zones)
            {
                zone.depth = zone.owner >= 0 ? 0 : 99;
            }
            for (int pass = 0; pass < zones.Count; pass++)
            {
                foreach (Zone zone in zones)
                {
                    foreach (int other in zone.touching)
                    {
                        if (SharesGate(zone, zones[other]))
                        {
                            zone.depth = Math.Min(zone.depth, zones[other].depth + 1);
                        }
                    }
                }
            }
            foreach (Zone zone in zones)
            {
                if (zone.depth == 99)
                {
                    zone.depth = 2;
                }
            }
        }

        private static bool SharesGate(Zone a, Zone b)
        {
            foreach (int gate in a.gates)
            {
                if (b.gates.Contains(gate))
                {
                    return true;
                }
            }
            return false;
        }

        private void Road(int from, int to)
        {
            // A* on open land: roads keep away from woods and like to join other roads.
            var cost = new Dictionary<int, int> { [from] = 0 };
            var came = new Dictionary<int, int> { [from] = -1 };
            var queue = new CellQueue();
            queue.Push(0, from);
            var around = new List<int>(6);
            while (queue.Count > 0)
            {
                int cell = queue.Pop(out int c);
                if (cell == to)
                {
                    break;
                }
                if (c > cost[cell])
                {
                    continue;
                }
                grid.Neighbors(cell, around);
                foreach (int next in around)
                {
                    if (next != to && (!map.Open(next) || map.occupant[next] >= 0))
                    {
                        continue;
                    }
                    int step = map.road[next] != 0 ? 6 : 10;
                    grid.Neighbors(next, scratch);
                    foreach (int n in scratch)
                    {
                        if (!map.Open(n))
                        {
                            step += 2;
                        }
                    }
                    int total = c + step;
                    if (!cost.TryGetValue(next, out int known) || total < known)
                    {
                        cost[next] = total;
                        came[next] = cell;
                        queue.Push(total, next);
                    }
                }
            }
            if (!came.ContainsKey(to))
            {
                return;
            }
            for (int cell = to; cell >= 0; cell = came[cell])
            {
                if (map.Open(cell))
                {
                    map.road[cell] = 1;
                }
            }
        }

        // ------------------------------------------------------------------ objects

        private MapObject AddObject(ObjectKind kind, int cell, int subtype = 0, int amount = 0, int amount2 = 0)
        {
            var obj = new MapObject { id = state.objects.Count, kind = kind, cell = cell, subtype = subtype, amount = amount, amount2 = amount2 };
            obj.footprint.Add(cell);
            state.objects.Add(obj);
            map.occupant[cell] = obj.id;
            return obj;
        }

        /// <summary>A free cell for an object: open, reachable, off the roads and the passes, not crowding other things.</summary>
        private bool Free(int cell, int spacing)
        {
            if (!map.Open(cell) || map.occupant[cell] >= 0 || map.road[cell] != 0 || gateCells.Contains(cell) || !reachable[cell])
            {
                return false;
            }
            if (spacing > 0)
            {
                foreach (int near in grid.Disk(cell, spacing))
                {
                    if (map.occupant[near] >= 0)
                    {
                        return false;
                    }
                }
            }
            // Out in the open: room to walk up to it and to stand a guard on, and no narrow way it could close.
            int open = 0;
            grid.Neighbors(cell, scratch);
            foreach (int next in scratch)
            {
                if (map.Open(next) && map.occupant[next] < 0)
                {
                    open++;
                }
            }
            return open >= 5;
        }

        private int FindSpot(Zone zone, int minFromCenter, int maxFromCenter, int spacing = 1)
        {
            int anchor = zone.town >= 0 ? state.towns[zone.town].center : zone.center;
            for (int attempt = 0; attempt < 80; attempt++)
            {
                int cell = zone.cells[random.Range(0, zone.cells.Count)];
                int d = grid.Distance(cell, anchor);
                if (d >= minFromCenter && d <= maxFromCenter && Free(cell, spacing))
                {
                    return cell;
                }
            }
            foreach (int cell in zone.cells)
            {
                if (Free(cell, spacing))
                {
                    return cell;
                }
            }
            return -1;
        }

        private int SpotNear(int x1000, int y1000)
        {
            int target = CellAt(x1000, y1000);
            int best = -1;
            int bestDistance = int.MaxValue;
            for (int cell = 0; cell < grid.Count; cell++)
            {
                if (!Free(cell, 1))
                {
                    continue;
                }
                int d = grid.Distance2x4(cell, target);
                if (d < bestDistance)
                {
                    bestDistance = d;
                    best = cell;
                }
            }
            return best;
        }

        /// <summary>The value (in the computer's gold) of a monster stack guarding something at the given depth.</summary>
        private int GuardBudget(int depth, int value)
        {
            int budget = 900 + depth * 2200 + value / 3;
            budget = budget * (spec.monsters + 1) / 3;
            return Math.Max(400, budget);
        }

        private MapObject AddMonster(int cell, int budget, CreatureId forced = CreatureId.None)
        {
            CreatureDef def;
            if (forced != CreatureId.None)
            {
                def = Creatures.Get(forced);
            }
            else
            {
                int maxTier = budget < 1500 ? 2 : budget < 4000 ? 3 : budget < 9000 ? 5 : budget < 16000 ? 6 : 7;
                int minTier = Math.Max(1, maxTier - 2);
                var options = new List<CreatureDef>();
                foreach (CreatureDef candidate in Creatures.All)
                {
                    if (candidate.Id == CreatureId.ArrowTower || candidate.Tier < minTier || candidate.Tier > maxTier)
                    {
                        continue;
                    }
                    if (candidate.Value * 2 > budget && candidate.Tier > 1)
                    {
                        continue;
                    }
                    options.Add(candidate);
                }
                def = options.Count > 0 ? options[random.Range(0, options.Count)] : Creatures.Get(CreatureId.Bandit);
            }
            int count = Math.Max(1, budget / def.Value);
            MapObject monster = AddObject(ObjectKind.Monster, cell, (int)def.Id, count);
            return monster;
        }

        /// <summary>Stands a guard next to <paramref name="target"/> (on its most open side).</summary>
        private void Guard(MapObject target, int budget)
        {
            int best = -1;
            int bestOpen = -1;
            grid.Neighbors(target.cell, scratch);
            var around = new List<int>(scratch);
            foreach (int cell in around)
            {
                if (!map.Open(cell) || map.occupant[cell] >= 0 || !reachable[cell] || gateCells.Contains(cell))
                {
                    continue;
                }
                int open = 0;
                grid.Neighbors(cell, scratch);
                foreach (int next in scratch)
                {
                    if (map.Open(next) && map.occupant[next] < 0)
                    {
                        open++;
                    }
                }
                if (open > bestOpen)
                {
                    bestOpen = open;
                    best = cell;
                }
            }
            if (best < 0)
            {
                return;
            }
            MapObject monster = AddMonster(best, budget);
            target.guard = monster.id;
        }

        private void PlaceMines()
        {
            foreach (Zone zone in zones)
            {
                var kinds = new List<ResourceKind>();
                if (zone.owner >= 0)
                {
                    kinds.Add(ResourceKind.Wood);
                    kinds.Add(ResourceKind.Ore);
                    kinds.Add(Land.RareOf(state.players[zone.owner].faction));
                }
                else
                {
                    kinds.Add(zone.depth >= 2 || random.Range(0, 2) == 0 ? ResourceKind.Gold : (ResourceKind)random.Range(3, 7));
                    kinds.Add((ResourceKind)random.Range(3, 7));
                    if (spec.treasure >= 2)
                    {
                        kinds.Add(random.Range(0, 2) == 0 ? ResourceKind.Wood : ResourceKind.Ore);
                    }
                }
                for (int i = 0; i < kinds.Count; i++)
                {
                    int cell = FindSpot(zone, zone.owner >= 0 ? 4 : 2, zone.owner >= 0 ? 9 : 20, 2);
                    if (cell < 0)
                    {
                        continue;
                    }
                    MapObject mine = AddObject(ObjectKind.Mine, cell, (int)kinds[i]);
                    bool rare = kinds[i] != ResourceKind.Wood && kinds[i] != ResourceKind.Ore;
                    if (zone.owner < 0 || rare)
                    {
                        Guard(mine, GuardBudget(Math.Max(zone.depth, 1), kinds[i] == ResourceKind.Gold ? 6000 : 2500));
                    }
                }
            }
        }

        private void PlaceGateGuards()
        {
            var guarded = new HashSet<int>();
            foreach (Zone zone in zones)
            {
                foreach (int gate in zone.gates)
                {
                    if (!guarded.Add(gate) || map.occupant[gate] >= 0 || !map.Open(gate))
                    {
                        continue;
                    }
                    // The pass between two homes, or into the middle, is guarded best.
                    int depth = 99;
                    foreach (Zone other in zones)
                    {
                        if (other.gates.Contains(gate))
                        {
                            depth = Math.Min(depth, other.depth);
                        }
                    }
                    int budget = GuardBudget(depth + 1, 3000);
                    AddMonster(gate, budget);
                }
            }
        }

        private void PlaceTreasures()
        {
            foreach (Zone zone in zones)
            {
                int richness = spec.treasure + (zone.owner >= 0 ? 0 : 1);
                int piles = 3 + richness * 2;
                for (int i = 0; i < piles; i++)
                {
                    int cell = FindSpot(zone, 2, 30);
                    if (cell < 0)
                    {
                        break;
                    }
                    var kind = (ResourceKind)random.Range(0, 7);
                    int amount = kind == ResourceKind.Gold ? 500 + 100 * random.Range(0, 6) : kind == ResourceKind.Wood || kind == ResourceKind.Ore ? 5 + random.Range(0, 6) : 3 + random.Range(0, 4);
                    AddObject(ObjectKind.Resource, cell, (int)kind, amount);
                }
                int chests = 1 + richness;
                for (int i = 0; i < chests; i++)
                {
                    int cell = FindSpot(zone, 3, 30);
                    if (cell < 0)
                    {
                        break;
                    }
                    int tier = random.Range(0, 3);
                    bool artifact = random.Range(0, 100) < 12;
                    int subtype = artifact ? (int)RandomArtifact(1) : -1;
                    MapObject chest = AddObject(ObjectKind.Treasure, cell, subtype, 1000 + 500 * tier, 500 + 500 * tier);
                    if (zone.owner < 0 && random.Range(0, 3) == 0)
                    {
                        Guard(chest, GuardBudget(zone.depth, 1500));
                    }
                }
                int fires = zone.owner >= 0 ? 1 : 2;
                for (int i = 0; i < fires; i++)
                {
                    int cell = FindSpot(zone, 2, 30);
                    if (cell >= 0)
                    {
                        AddObject(ObjectKind.Campfire, cell, random.Range(1, 7), 400 + 100 * random.Range(0, 3), 4 + random.Range(0, 3));
                    }
                }
                if (zone.owner < 0)
                {
                    int artifacts = 1 + (zone.depth >= 2 ? 1 : 0);
                    for (int i = 0; i < artifacts; i++)
                    {
                        int cell = FindSpot(zone, 3, 30, 2);
                        if (cell < 0)
                        {
                            break;
                        }
                        int rank = Math.Min(3, Math.Max(1, zone.depth + random.Range(0, 2)));
                        ArtifactId id = RandomArtifact(rank);
                        MapObject artifact = AddObject(ObjectKind.Artifact, cell, (int)id);
                        Guard(artifact, GuardBudget(zone.depth + 1, Artifacts.Get(id).Value));
                    }
                    int cellScroll = FindSpot(zone, 3, 30);
                    if (cellScroll >= 0 && random.Range(0, 2) == 0)
                    {
                        List<SpellId> spells = Spells.OfLevel(Math.Min(4, 1 + zone.depth));
                        AddObject(ObjectKind.Scroll, cellScroll, (int)spells[random.Range(0, spells.Count)]);
                    }
                }
            }
        }

        private ArtifactId RandomArtifact(int rank)
        {
            List<ArtifactId> pool = Artifacts.OfRank(rank);
            if (pool.Count == 0)
            {
                pool = Artifacts.OfRank(1);
            }
            return pool[random.Range(0, pool.Count)];
        }

        private void PlaceBuildings()
        {
            ObjectKind[] homeKinds = { ObjectKind.Windmill, ObjectKind.WaterWheel, ObjectKind.Stables, ObjectKind.Fountain, ObjectKind.Temple, ObjectKind.Watchtower };
            ObjectKind[] wildKinds =
            {
                ObjectKind.MercenaryCamp, ObjectKind.MarlettoTower, ObjectKind.StarAxis, ObjectKind.GardenOfRevelation, ObjectKind.Arena,
                ObjectKind.LearningStone, ObjectKind.WitchHut, ObjectKind.Shrine, ObjectKind.Windmill, ObjectKind.Watchtower, ObjectKind.TreeOfKnowledge,
                ObjectKind.Temple, ObjectKind.Fountain
            };
            foreach (Zone zone in zones)
            {
                int count = zone.owner >= 0 ? 2 : 3 + spec.treasure / 2;
                ObjectKind[] pool = zone.owner >= 0 ? homeKinds : wildKinds;
                var used = new HashSet<ObjectKind>();
                for (int i = 0; i < count; i++)
                {
                    ObjectKind kind = pool[random.Range(0, pool.Length)];
                    if (!used.Add(kind))
                    {
                        continue;
                    }
                    int cell = FindSpot(zone, 3, 30, 2);
                    if (cell < 0)
                    {
                        break;
                    }
                    MapObject obj = AddObject(kind, cell);
                    switch (kind)
                    {
                        case ObjectKind.Shrine:
                        {
                            int level = Math.Min(3, 1 + zone.depth / 2 + random.Range(0, 2));
                            List<SpellId> spells = Spells.OfLevel(level);
                            obj.subtype = level;
                            obj.amount = (int)spells[random.Range(0, spells.Count)];
                            break;
                        }
                        case ObjectKind.WitchHut:
                        {
                            obj.subtype = random.Range(0, (int)SkillId.Necromancy);
                            break;
                        }
                    }
                    if (zone.owner < 0 && (kind == ObjectKind.TreeOfKnowledge || kind == ObjectKind.Arena || kind == ObjectKind.LearningStone))
                    {
                        Guard(obj, GuardBudget(zone.depth, 2000));
                    }
                }
                // A dwelling: creatures of the home's faction near a home, wild ones elsewhere.
                int spot = FindSpot(zone, 4, 30, 2);
                if (spot >= 0)
                {
                    CreatureId creature = zone.owner >= 0
                        ? Creatures.OfTier(state.players[zone.owner].faction, 1 + random.Range(0, 3))
                        : Creatures.Neutrals[Math.Min(Creatures.Neutrals.Length - 2, random.Range(0, 3) + zone.depth)];
                    MapObject dwelling = AddObject(ObjectKind.Dwelling, spot, (int)creature, Creatures.Get(creature).Growth);
                    if (zone.owner < 0)
                    {
                        Guard(dwelling, GuardBudget(zone.depth, 1500));
                    }
                }
            }
        }

        private void PlaceWanderers()
        {
            foreach (Zone zone in zones)
            {
                int count = zone.owner >= 0 ? 2 : 3;
                for (int i = 0; i < count; i++)
                {
                    int cell = FindSpot(zone, zone.owner >= 0 ? 5 : 2, 30, 2);
                    if (cell < 0)
                    {
                        break;
                    }
                    AddMonster(cell, GuardBudget(zone.depth, 0) * 2 / 3);
                }
            }
        }

        private void PlaceSpecials()
        {
            foreach (SpecialSpec special in spec.specials)
            {
                int cell = SpotNear(special.x, special.y);
                if (cell < 0)
                {
                    continue;
                }
                MapObject obj = null;
                switch (special.kind)
                {
                    case SpecialKind.Monster:
                        obj = AddMonster(cell, 0, special.creature);
                        obj.amount = Math.Max(1, special.count);
                        break;
                    case SpecialKind.Artifact:
                        obj = AddObject(ObjectKind.Artifact, cell, (int)special.artifact);
                        break;
                    case SpecialKind.Treasure:
                        obj = AddObject(ObjectKind.Treasure, cell, special.artifact != ArtifactId.None ? (int)special.artifact : -1, 2000, 1500);
                        break;
                    case SpecialKind.Mine:
                        obj = AddObject(ObjectKind.Mine, cell, (int)special.resource);
                        break;
                    case SpecialKind.Building:
                        obj = AddObject(special.building, cell);
                        break;
                    case SpecialKind.NeutralTown:
                    {
                        var zone = new Zone { index = map.zone[cell], center = cell, terrain = (TerrainType)map.terrain[cell], depth = 2 };
                        foreach (int c in zones[map.zone[cell]].cells)
                        {
                            zone.cells.Add(c);
                        }
                        int town = PlaceTown(zone, special.faction == Faction.Neutral ? Faction.Necropolis : special.faction, -1);
                        obj = state.Object(state.towns[town].objectId);
                        TownState placed = state.towns[town];
                        if (special.creature != CreatureId.None)
                        {
                            placed.garrison.Clear();
                            placed.garrison.Add((int)special.creature, Math.Max(1, special.count));
                        }
                        break;
                    }
                }
                if (obj == null)
                {
                    continue;
                }
                obj.tag = special.tag ?? "";
                if (special.guard != CreatureId.None && special.kind != SpecialKind.Monster)
                {
                    Guard(obj, 1);
                    MapObject guard = state.Object(obj.guard);
                    if (guard != null)
                    {
                        guard.subtype = (int)special.guard;
                        guard.amount = Math.Max(1, special.guardCount);
                    }
                }
            }
        }

        // ------------------------------------------------------------------ heroes and the land's shape

        private void PlaceHeroes()
        {
            foreach (PlayerState player in state.players)
            {
                PlayerSpec seat = spec.players[player.index];
                int def = seat.hero;
                if (def < 0 || !state.freeHeroes.Contains(def))
                {
                    var options = new List<int>();
                    foreach (int id in state.freeHeroes)
                    {
                        if (HeroData.Hero(id).Faction == player.faction)
                        {
                            options.Add(id);
                        }
                    }
                    def = options.Count > 0 ? options[random.Range(0, options.Count)] : state.freeHeroes[0];
                }
                int cell = player.towns.Count > 0 ? state.towns[player.towns[0]].cell : zones[player.index].center;
                game.SpawnHero(player.index, def, cell, true);
            }
        }

        private void ShapeHeights()
        {
            for (int cell = 0; cell < grid.Count; cell++)
            {
                int hills = Noise.At(grid, cell, 16, 77u);
                int height = 40 + hills * 50 / 1000;
                switch ((Obstacle)map.obstacle[cell])
                {
                    case Obstacle.Mountain:
                    case Obstacle.Edge:
                        height = 150 + Noise.At(grid, cell, 3, 79u) * 100 / 1000;
                        break;
                    case Obstacle.Rocks:
                        height += 25;
                        break;
                    case Obstacle.Forest:
                    case Obstacle.DeadTrees:
                        height += 8;
                        break;
                    case Obstacle.Lake:
                        height = 0;
                        break;
                }
                if ((TerrainType)map.terrain[cell] == TerrainType.Water)
                {
                    height = 0;
                }
                map.height[cell] = (byte)Clamp(height, 0, 255);
            }
            // Towns stand on level ground.
            foreach (TownState town in state.towns)
            {
                int level = map.height[town.center];
                foreach (int cell in grid.Disk(town.center, 2))
                {
                    if (map.Open(cell) || map.occupant[cell] >= 0)
                    {
                        map.height[cell] = (byte)level;
                    }
                }
            }
        }

        private void ResolveRules()
        {
            state.rules = spec.rules ?? new ScenarioRules();
            ScenarioRules rules = state.rules;
            if (rules.victory == VictoryKind.CaptureTown)
            {
                // The tag names a special town, or "player:N" the first town of seat N.
                if (!string.IsNullOrEmpty(rules.victoryTag) && rules.victoryTag.StartsWith("player:"))
                {
                    int seat = int.Parse(rules.victoryTag.Substring(7));
                    PlayerState owner = state.Player(seat);
                    rules.victoryValue = owner != null && owner.towns.Count > 0 ? owner.towns[0] : -1;
                }
                else
                {
                    foreach (TownState town in state.towns)
                    {
                        if (state.Object(town.objectId).tag == rules.victoryTag)
                        {
                            rules.victoryValue = town.id;
                        }
                    }
                }
            }
            if (rules.loss == LossKind.LoseHero)
            {
                // The starting hero of the first person.
                foreach (PlayerState player in state.players)
                {
                    if (player.human && player.heroes.Count > 0)
                    {
                        rules.lossValue = player.heroes[0];
                        break;
                    }
                }
            }
        }
    }
}
