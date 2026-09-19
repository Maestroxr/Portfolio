using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

namespace Portfolio.EndlessRunner
{
    internal struct PiecePlacement
    {
        public TrackPiece Prefab;
        public Vector3 Position;
        public float Yaw;
        public float Scale;
        public int Id;
        public int ParentId;
    }


    internal struct TilePlacement
    {
        public TerrainBehaviour Prefab;
        public float Z;
        public RunnerTheme Theme;
    }


    internal struct HintPlacement
    {
        public float Z;
        public string Text;
    }


    /// <summary>The generated content of a track, ordered along z. The generator spawns it as the runner approaches.</summary>
    internal sealed class TrackLayout
    {
        public readonly List<TilePlacement> Tiles = new List<TilePlacement>();
        public readonly List<PiecePlacement> Pieces = new List<PiecePlacement>();
        public readonly List<HintPlacement> Hints = new List<HintPlacement>();
        /// <summary>Start and end z of every chasm gap.</summary>
        public readonly List<Vector2> Gaps = new List<Vector2>();
        /// <summary>Coin value of the track (a gem counts as several coins).</summary>
        public int Coins;
        public float FinishZ = float.PositiveInfinity;
        public float GeneratedUntil = float.NegativeInfinity;
    }


    /// <summary>
    /// Builds a level's track from a library of obstacle patterns. Patterns are picked by the level's difficulty
    /// curve and features, spaced by the speed the runner will have there, and always leave a way through. The
    /// random generator is seeded by the level, so the same level always gets the same track.
    /// </summary>
    internal sealed class LayoutBuilder
    {
        public const float TileLength = 6f;
        public const float LaneWidth = 2.5f;
        public const float CoinHeight = 0.9f;
        public const float WagonHeight = 2.4f;
        public const float RampLength = 6f;
        public const float BlockHeight = 2.2f;
        public const float GapStart = 1.25f;
        public const float GapEnd = 4.75f;
        public const int FirstTile = -16;
        private const float ContentStart = 36f;

        private delegate float Pattern(float z, float difficulty);

        private sealed class PatternDef
        {
            public string Name;
            public TrackFeatures Needs;
            public float MinDifficulty;
            public float Weight;
            public float MaxLength;
            public bool Easy;
            public Pattern Place;
        }

        private static readonly TrackFeatures[] IntroOrder =
        {
            TrackFeatures.Hurdles, TrackFeatures.Blocks, TrackFeatures.Barriers, TrackFeatures.Ramps,
            TrackFeatures.Chasms, TrackFeatures.JumpPads, TrackFeatures.MovingCarts, TrackFeatures.PlatformChains
        };

        private readonly RunnerLevel level;
        private readonly RunnerSettings settings;
        private readonly PieceCatalog catalog;
        private readonly Random rng;
        private readonly Random sceneryRng;
        private readonly float gravity;
        private readonly float padHeight;
        private readonly List<PatternDef> patterns = new List<PatternDef>();
        private readonly List<PatternDef> candidates = new List<PatternDef>();
        private readonly List<float> candidateWeights = new List<float>();
        private readonly HashSet<int> gapTiles = new HashSet<int>();
        private readonly List<PiecePlacement> batch = new List<PiecePlacement>();
        private readonly TrackLayout layout = new TrackLayout();

        private float cursor = ContentStart;
        private int nextTile = FirstTile;
        private int nextId;
        private float nextPowerUp;
        private PatternDef last;
        private TrackFeatures introduced;
        private bool finished;

        public TrackLayout Layout => layout;

        public LayoutBuilder(RunnerLevel level, RunnerSettings settings, PieceCatalog catalog, float gravity, int seed)
        {
            this.level = level;
            this.settings = settings;
            this.catalog = catalog;
            this.gravity = Mathf.Max(1f, gravity);
            rng = new Random(seed);
            sceneryRng = new Random(seed * 7919 + 17);
            padHeight = catalog.jumpPad != null ? catalog.jumpPad.LaunchHeight : 5.5f;
            layout.FinishZ = level.IsEndless ? float.PositiveInfinity : Mathf.Round(level.Length / TileLength) * TileLength;
            nextPowerUp = 80f + Range(rng, 0f, 60f);
            RegisterPatterns();
        }

        /// <summary>Extends the layout to (at least) <paramref name="z"/>. Campaign levels are built to the finish at once.</summary>
        public void GenerateUntil(float z)
        {
            if (finished)
            {
                return;
            }
            batch.Clear();
            float limit = level.IsEndless ? z : Mathf.Min(z, layout.FinishZ - 24f);
            while (cursor < limit)
            {
                if (!PlaceNext(limit))
                {
                    break;
                }
            }
            float tileLimit = cursor;
            if (!level.IsEndless && z >= layout.FinishZ - 24f)
            {
                AddPiece(catalog.finishLine, new Vector3(0f, 0f, layout.FinishZ));
                tileLimit = layout.FinishZ + 96f;
                finished = true;
            }
            while (nextTile * TileLength < tileLimit)
            {
                AddTile(nextTile++);
            }
            batch.Sort((a, b) => a.Position.z.CompareTo(b.Position.z));
            layout.Pieces.AddRange(batch);
            batch.Clear();
            layout.GeneratedUntil = nextTile * TileLength;
        }

        private void RegisterPatterns()
        {
            Register("CoinTrail", TrackFeatures.None, 0f, 1.1f, 32f, CoinTrail, true);
            Register("CoinSnake", TrackFeatures.None, 0.05f, 0.8f, 36f, CoinSnake, true);
            Register("HurdleSingle", TrackFeatures.Hurdles, 0f, 1.2f, 22f, HurdleSingle);
            Register("HurdleRow", TrackFeatures.Hurdles, 0.2f, 1f, 22f, HurdleRow);
            Register("HurdleStagger", TrackFeatures.Hurdles, 0.35f, 0.8f, 60f, HurdleStagger);
            Register("BlockPair", TrackFeatures.Blocks, 0f, 1.2f, 20f, BlockPair);
            Register("BlockSlalom", TrackFeatures.Blocks, 0.3f, 1f, 70f, BlockSlalom);
            Register("BarrierSingle", TrackFeatures.Barriers, 0.05f, 1.1f, 22f, BarrierSingle);
            Register("BarrierRow", TrackFeatures.Barriers, 0.3f, 0.9f, 22f, BarrierRow);
            Register("MixedRow", TrackFeatures.Hurdles | TrackFeatures.Barriers | TrackFeatures.Blocks, 0.4f, 1f, 22f, MixedRow);
            Register("RampWagon", TrackFeatures.Ramps, 0f, 1.2f, 34f, RampWagon);
            Register("WagonPair", TrackFeatures.Ramps, 0.3f, 0.9f, 34f, WagonPair);
            Register("WagonChain", TrackFeatures.PlatformChains, 0.4f, 0.9f, 72f, WagonChain);
            Register("Chasm", TrackFeatures.Chasms, 0.1f, 1f, 24f, Chasm);
            Register("ChasmBridge", TrackFeatures.Chasms, 0.25f, 0.8f, 24f, ChasmBridge);
            Register("BouncePad", TrackFeatures.JumpPads, 0.1f, 0.9f, 60f, BouncePad);
            Register("Carts", TrackFeatures.MovingCarts, 0.3f, 1f, 80f, Carts);
            Register("Gauntlet", TrackFeatures.Hurdles | TrackFeatures.Barriers | TrackFeatures.Blocks, 0.6f, 0.9f, 80f, Gauntlet);
        }

        private void Register(string name, TrackFeatures needs, float minDifficulty, float weight, float maxLength, Pattern place, bool easy = false)
        {
            patterns.Add(new PatternDef
            {
                Name = name, Needs = needs, MinDifficulty = minDifficulty, Weight = weight, MaxLength = maxLength, Easy = easy, Place = place
            });
        }

        private bool PlaceNext(float limit)
        {
            float difficulty = level.DifficultyAt(cursor);
            float end;
            if (TryIntroduce(difficulty, limit, out end))
            {
            }
            else if (level.Has(TrackFeatures.PowerUps) && level.PowerUps.Length > 0 && cursor >= nextPowerUp && cursor + 20f < limit)
            {
                end = PowerUpGift(cursor, difficulty);
                nextPowerUp = end + Range(rng, 130f, 210f);
            }
            else
            {
                PatternDef pattern = Choose(cursor, difficulty, limit);
                if (pattern == null)
                {
                    return false;
                }
                end = pattern.Place(cursor, difficulty);
                last = pattern;
            }
            cursor = end + Spacing(end, difficulty);
            return true;
        }

        private PatternDef Choose(float z, float difficulty, float limit)
        {
            candidates.Clear();
            candidateWeights.Clear();
            float total = 0f;
            foreach (PatternDef pattern in patterns)
            {
                if (pattern == last || !level.Has(pattern.Needs) || difficulty + 0.001f < pattern.MinDifficulty || z + pattern.MaxLength > limit)
                {
                    continue;
                }
                float weight = pattern.Easy
                    ? pattern.Weight * (1f - 0.6f * difficulty)
                    : pattern.Weight * (1f + 1.5f * Mathf.Clamp01((difficulty - pattern.MinDifficulty) / 0.4f));
                candidates.Add(pattern);
                candidateWeights.Add(weight);
                total += weight;
            }
            if (candidates.Count == 0)
            {
                return null;
            }
            float roll = (float)rng.NextDouble() * total;
            for (int i = 0; i < candidates.Count; i++)
            {
                roll -= candidateWeights[i];
                if (roll <= 0f)
                {
                    return candidates[i];
                }
            }
            return candidates[candidates.Count - 1];
        }

        private bool TryIntroduce(float difficulty, float limit, out float end)
        {
            end = cursor;
            foreach (TrackFeatures feature in IntroOrder)
            {
                if ((level.Spotlight & feature) == 0 || (introduced & feature) != 0 || !level.Has(feature))
                {
                    continue;
                }
                introduced |= feature;
                if (cursor + 80f > limit)
                {
                    return false;
                }
                if (level.ShowHints)
                {
                    layout.Hints.Add(new HintPlacement { Z = cursor - 18f, Text = HintFor(feature, Gamebox.MobilePlatform.UsesTouch) });
                }
                end = IntroPattern(feature)(cursor, Mathf.Min(difficulty, 0.15f));
                return true;
            }
            return false;
        }

        private Pattern IntroPattern(TrackFeatures feature)
        {
            switch (feature)
            {
                case TrackFeatures.Hurdles: return HurdleSingle;
                case TrackFeatures.Blocks: return BlockPair;
                case TrackFeatures.Barriers: return BarrierSingle;
                case TrackFeatures.Ramps: return RampWagon;
                case TrackFeatures.Chasms: return Chasm;
                case TrackFeatures.JumpPads: return BouncePad;
                case TrackFeatures.MovingCarts: return Carts;
                case TrackFeatures.PlatformChains: return WagonChain;
                default: return CoinTrail;
            }
        }

        /// <summary>The tip shown before the first <paramref name="feature"/> of a level, worded for touch or the keyboard.</summary>
        private static string HintFor(TrackFeatures feature, bool touch)
        {
            switch (feature)
            {
                case TrackFeatures.Hurdles: return touch ? "Swipe up to jump over hurdles" : "Jump over hurdles: UP, SPACE or swipe up";
                case TrackFeatures.Blocks: return touch ? "Swipe sideways to dodge the crates" : "Dodge the crates: LEFT / RIGHT or swipe sideways";
                case TrackFeatures.Barriers: return touch ? "Swipe down to slide under barriers" : "Slide under barriers: DOWN or swipe down";
                case TrackFeatures.Ramps: return "Run up the ramp - coins are waiting on the wagon!";
                case TrackFeatures.Chasms: return "Mind the gap! Jump across";
                case TrackFeatures.JumpPads: return "Bounce pads launch you into the sky!";
                case TrackFeatures.MovingCarts: return "Runaway cart! Switch lanes or jump over it";
                case TrackFeatures.PlatformChains: return "Leap from wagon to wagon!";
                default: return string.Empty;
            }
        }

        // ------------------------------------------------------------------ patterns

        private float CoinTrail(float z, float difficulty)
        {
            int lane = Lane();
            int count = rng.Next(8, 13);
            CoinLine(lane, z + 2f, count, 2.2f, CoinHeight);
            return z + 2f + count * 2.2f;
        }

        private float CoinSnake(float z, float difficulty)
        {
            int lane = Lane();
            int target = OtherLane(lane);
            float coinZ = z + 2f;
            for (int i = 0; i < 14; i++)
            {
                float t = Mathf.Clamp01((i - 4) / 5f);
                t = t * t * (3f - 2f * t);
                Coin(Mathf.Lerp(X(lane), X(target), t), CoinHeight, coinZ);
                coinZ += 2.2f;
            }
            return coinZ;
        }

        private float HurdleSingle(float z, float difficulty)
        {
            int lane = Lane();
            float hurdleZ = z + 12f;
            AddPiece(catalog.hurdle, Pos(lane, hurdleZ));
            JumpArc(lane, hurdleZ + 0.15f);
            if (Chance(0.6f))
            {
                CoinLine(OtherLane(lane), hurdleZ - 6f, 5, 2.2f, CoinHeight);
            }
            return hurdleZ + 6f;
        }

        private float HurdleRow(float z, float difficulty)
        {
            float hurdleZ = z + 12f;
            for (int lane = -1; lane <= 1; lane++)
            {
                AddPiece(catalog.hurdle, Pos(lane, hurdleZ));
            }
            JumpArc(Lane(), hurdleZ + 0.15f);
            return hurdleZ + 6f;
        }

        private float HurdleStagger(float z, float difficulty)
        {
            float gap = RowGap(z, difficulty) * 0.8f;
            float hurdleZ = z + 10f;
            int lane = Lane();
            for (int i = 0; i < 3; i++)
            {
                int next = OtherLane(lane);
                AddPiece(catalog.hurdle, Pos(lane, hurdleZ));
                if (difficulty > 0.6f)
                {
                    AddPiece(catalog.hurdle, Pos(ThirdLane(lane, next), hurdleZ));
                }
                if (i == 1)
                {
                    JumpArc(lane, hurdleZ + 0.15f);
                }
                lane = next;
                hurdleZ += gap;
            }
            return hurdleZ - gap + 6f;
        }

        private float BlockPair(float z, float difficulty)
        {
            int free = Lane();
            float blockZ = z + 12f;
            bool gemPlaced = false;
            for (int lane = -1; lane <= 1; lane++)
            {
                if (lane == free)
                {
                    continue;
                }
                int id = AddPiece(Block(), Pos(lane, blockZ));
                if (!gemPlaced && level.Has(TrackFeatures.Gems) && Chance(0.3f))
                {
                    Gem(X(lane), BlockHeight + 0.9f, blockZ + 0.8f, id);
                    gemPlaced = true;
                }
            }
            CoinLine(free, blockZ - 8f, 6, 2.2f, CoinHeight);
            return blockZ + 4f;
        }

        private float BlockSlalom(float z, float difficulty)
        {
            float gap = RowGap(z, difficulty);
            float blockZ = z + 10f;
            int free = Lane();
            for (int row = 0; row < 3; row++)
            {
                for (int lane = -1; lane <= 1; lane++)
                {
                    if (lane != free)
                    {
                        AddPiece(Block(), Pos(lane, blockZ));
                    }
                }
                CoinLine(free, blockZ - 3f, 3, 2f, CoinHeight);
                free = OtherLane(free);
                blockZ += gap;
            }
            return blockZ - gap + 4f;
        }

        private float BarrierSingle(float z, float difficulty)
        {
            int lane = Lane();
            float barrierZ = z + 12f;
            AddPiece(catalog.barrier, Pos(lane, barrierZ));
            CoinLine(lane, barrierZ - 2.5f, 4, 1.6f, 0.45f);
            if (difficulty > 0.2f && level.Has(TrackFeatures.Blocks) && Chance(0.5f))
            {
                AddPiece(Block(), Pos(OtherLane(lane), barrierZ));
            }
            return barrierZ + 5f;
        }

        private float BarrierRow(float z, float difficulty)
        {
            float barrierZ = z + 12f;
            for (int lane = -1; lane <= 1; lane++)
            {
                AddPiece(catalog.barrier, Pos(lane, barrierZ));
            }
            CoinLine(Lane(), barrierZ - 2.5f, 4, 1.6f, 0.45f);
            return barrierZ + 5f;
        }

        private float MixedRow(float z, float difficulty)
        {
            int a = Lane();
            int b = OtherLane(a);
            int c = ThirdLane(a, b);
            float rowZ = z + 12f;
            AddPiece(catalog.hurdle, Pos(a, rowZ));
            AddPiece(catalog.barrier, Pos(b, rowZ));
            AddPiece(Block(), Pos(c, rowZ));
            JumpArc(a, rowZ + 0.15f);
            return rowZ + 6f;
        }

        /// <summary>A ramp with a wagon behind it; returns the end of the wagon.</summary>
        private float WagonWithRamp(int lane, float z, bool isLong, bool coins)
        {
            AddPiece(catalog.ramp, Pos(lane, z));
            Obstacle wagon = Pick(isLong ? catalog.longWagons : catalog.shortWagons);
            float wagonZ = z + RampLength;
            int id = AddPiece(wagon, Pos(lane, wagonZ));
            float wagonLength = wagon != null ? wagon.Length : 10f;
            if (coins)
            {
                int count = Mathf.Max(2, Mathf.FloorToInt((wagonLength - 2f) / 2f));
                CoinLine(lane, wagonZ + 1.5f, count, 2f, WagonHeight + CoinHeight, id);
            }
            return wagonZ + wagonLength;
        }

        private float RampWagon(float z, float difficulty)
        {
            int lane = Lane();
            float rampZ = z + 6f;
            float end = WagonWithRamp(lane, rampZ, Chance(0.5f), true);
            int other = OtherLane(lane);
            if (difficulty > 0.25f && level.Has(TrackFeatures.Blocks))
            {
                AddPiece(Block(), Pos(other, rampZ + RampLength + 2f));
            }
            else
            {
                CoinLine(other, rampZ, 6, 2.2f, CoinHeight);
            }
            if (difficulty > 0.5f && level.Has(TrackFeatures.Hurdles))
            {
                AddPiece(catalog.hurdle, Pos(ThirdLane(lane, other), rampZ + 8f));
            }
            return end + 2f;
        }

        private float WagonPair(float z, float difficulty)
        {
            int a = Lane();
            int b = OtherLane(a);
            int c = ThirdLane(a, b);
            float rampZ = z + 6f;
            float end = WagonWithRamp(a, rampZ, true, true);
            Obstacle wagon = Pick(catalog.longWagons);
            AddPiece(wagon, Pos(b, rampZ + RampLength));
            if (level.Has(TrackFeatures.Hurdles))
            {
                AddPiece(catalog.hurdle, Pos(c, rampZ + 10f));
                JumpArc(c, rampZ + 10.15f);
            }
            else
            {
                CoinLine(c, rampZ + 2f, 8, 2.2f, CoinHeight);
            }
            return end + 2f;
        }

        private float WagonChain(float z, float difficulty)
        {
            int lane = Lane();
            float wagonZ = z + 6f;
            AddPiece(catalog.ramp, Pos(lane, wagonZ));
            wagonZ += RampLength;
            int segments = difficulty > 0.7f ? 4 : 3;
            for (int i = 0; i < segments; i++)
            {
                Obstacle wagon = Pick(catalog.shortWagons);
                int id = AddPiece(wagon, Pos(lane, wagonZ));
                CoinLine(lane, wagonZ + 1.5f, 3, 2f, WagonHeight + CoinHeight, id);
                wagonZ += wagon != null ? wagon.Length : 10f;
                if (i < segments - 1)
                {
                    float gap = Mathf.Lerp(3f, 4.5f, difficulty);
                    JumpArc(lane, wagonZ + gap * 0.5f, WagonHeight, 5);
                    wagonZ += gap;
                }
            }
            int other = OtherLane(lane);
            int third = ThirdLane(lane, other);
            float step = RowGap(z, difficulty) * 1.2f;
            for (float obstacleZ = z + 14f; obstacleZ < wagonZ - 4f; obstacleZ += step)
            {
                AddPiece(Block(), Pos(other, obstacleZ));
                if (difficulty > 0.55f && level.Has(TrackFeatures.Hurdles))
                {
                    AddPiece(catalog.hurdle, Pos(third, obstacleZ + step * 0.5f));
                }
            }
            return wagonZ + 2f;
        }

        private float Chasm(float z, float difficulty)
        {
            return ChasmTile(z, false);
        }

        private float ChasmBridge(float z, float difficulty)
        {
            return ChasmTile(z, true);
        }

        private float ChasmTile(float z, bool bridge)
        {
            int tile = Mathf.CeilToInt((z + 8f) / TileLength);
            gapTiles.Add(tile);
            float tileZ = tile * TileLength;
            layout.Gaps.Add(new Vector2(tileZ + GapStart, tileZ + GapEnd));
            float middle = tileZ + (GapStart + GapEnd) * 0.5f;
            int lane = Lane();
            if (bridge && catalog.bridge != null)
            {
                AddPiece(catalog.bridge, Pos(lane, tileZ + GapStart - 0.5f));
                CoinLine(lane, tileZ + GapStart - 0.2f, 3, 1.8f, CoinHeight);
                JumpArc(OtherLane(lane), middle);
            }
            else
            {
                JumpArc(lane, middle);
            }
            return tileZ + TileLength + 2f;
        }

        private float BouncePad(float z, float difficulty)
        {
            int lane = Lane();
            float padZ = z + 10f;
            AddPiece(catalog.jumpPad, Pos(lane, padZ));
            float speed = SpeedAt(padZ);
            float launch = Mathf.Sqrt(2f * gravity * padHeight);
            float airTime = 2f * launch / gravity;
            const int count = 11;
            for (int i = 0; i < count; i++)
            {
                float t = airTime * (i + 1f) / (count + 1f);
                float y = launch * t - 0.5f * gravity * t * t + CoinHeight;
                float coinZ = padZ + 0.5f + speed * t;
                if (i == count / 2 && level.Has(TrackFeatures.Gems))
                {
                    Gem(X(lane), y + 0.3f, coinZ);
                }
                else
                {
                    Coin(X(lane), y, coinZ);
                }
            }
            float landing = padZ + 0.5f + speed * airTime;
            if (difficulty > 0.3f && level.Has(TrackFeatures.Blocks))
            {
                AddPiece(Block(), Pos(lane, padZ + speed * airTime * 0.45f));
            }
            if (difficulty > 0.45f && level.Has(TrackFeatures.Hurdles))
            {
                AddPiece(catalog.hurdle, Pos(OtherLane(lane), padZ + 8f));
            }
            return landing + 6f;
        }

        private float Carts(float z, float difficulty)
        {
            int lane = Lane();
            int other = OtherLane(lane);
            float cartZ = z + 44f;
            AddPiece(catalog.cart, Pos(lane, cartZ));
            CoinLine(other, z + 6f, 10, 2.4f, CoinHeight);
            float end = cartZ + 4f;
            if (difficulty > 0.6f)
            {
                AddPiece(catalog.cart, Pos(ThirdLane(lane, other), cartZ + 24f));
                end = cartZ + 28f;
            }
            return end;
        }

        private float Gauntlet(float z, float difficulty)
        {
            float gap = RowGap(z, difficulty);
            float rowZ = z + 10f;

            int a = Lane();
            int b = OtherLane(a);
            AddPiece(catalog.hurdle, Pos(a, rowZ));
            AddPiece(catalog.hurdle, Pos(b, rowZ));
            AddPiece(Block(), Pos(ThirdLane(a, b), rowZ));
            JumpArc(a, rowZ + 0.15f);
            rowZ += gap;

            int p = Lane();
            int q = OtherLane(p);
            AddPiece(catalog.barrier, Pos(p, rowZ));
            AddPiece(catalog.barrier, Pos(q, rowZ));
            AddPiece(Block(), Pos(ThirdLane(p, q), rowZ));
            CoinLine(p, rowZ - 2.5f, 4, 1.6f, 0.45f);
            rowZ += gap;

            int free = Lane();
            for (int lane = -1; lane <= 1; lane++)
            {
                if (lane != free)
                {
                    AddPiece(Block(), Pos(lane, rowZ));
                }
            }
            CoinLine(free, rowZ - 4f, 4, 2f, CoinHeight);
            return rowZ + 4f;
        }

        private float PowerUpGift(float z, float difficulty)
        {
            int lane = Lane();
            float powerUpZ = z + 10f;
            CoinLine(lane, z + 2f, 3, 2.2f, CoinHeight);
            PowerUpType[] types = level.PowerUps;
            PowerUpType type = types[rng.Next(types.Length)];
            AddPiece(catalog.PowerUp(type), new Vector3(X(lane), 1.1f, powerUpZ));
            return powerUpZ + 3f;
        }

        // ------------------------------------------------------------------ tiles and scenery

        private void AddTile(int index)
        {
            float z = index * TileLength;
            RunnerTheme theme = level.ThemeAt(z);
            bool gap = gapTiles.Contains(index);
            layout.Tiles.Add(new TilePlacement { Prefab = gap ? catalog.chasmTile : catalog.roadTile, Z = z, Theme = theme });
            if (theme != null)
            {
                AddScenery(theme, index, z, gap);
            }
        }

        private void AddScenery(RunnerTheme theme, int index, float z, bool gap)
        {
            SceneryItem[] items = theme.scenery;
            float total = 0f;
            if (items != null)
            {
                foreach (SceneryItem item in items)
                {
                    if (item != null && item.prefab != null)
                    {
                        total += Mathf.Max(0f, item.weight);
                    }
                }
            }
            for (int i = 0; i < theme.sceneryPerTile && total > 0f; i++)
            {
                SceneryItem item = PickScenery(items, total);
                float sceneryZ = z + Range(sceneryRng, 0f, TileLength);
                float side = sceneryRng.NextDouble() < 0.5 ? -1f : 1f;
                float x = side * Range(sceneryRng, item.distance.x, item.distance.y);
                float yaw = Range(sceneryRng, 0f, 360f);
                float scale = Range(sceneryRng, item.scale.x, item.scale.y);
                if (gap && sceneryZ > z + GapStart - 1.5f && sceneryZ < z + GapEnd + 1.5f)
                {
                    continue;
                }
                AddPiece(item.prefab, new Vector3(x, 0f, sceneryZ), yaw, scale);
            }
            if (theme.roadsideProp != null && !gap && theme.roadsideEvery > 0 && (index % theme.roadsideEvery + theme.roadsideEvery) % theme.roadsideEvery == 0)
            {
                float propLength = theme.roadsideProp.Length;
                AddPiece(theme.roadsideProp, new Vector3(-theme.roadsideOffset, 0f, z));
                AddPiece(theme.roadsideProp, new Vector3(theme.roadsideOffset, 0f, z + propLength), 180f);
            }
        }

        private SceneryItem PickScenery(SceneryItem[] items, float total)
        {
            float roll = (float)sceneryRng.NextDouble() * total;
            SceneryItem chosen = null;
            foreach (SceneryItem item in items)
            {
                if (item == null || item.prefab == null)
                {
                    continue;
                }
                chosen = item;
                roll -= Mathf.Max(0f, item.weight);
                if (roll <= 0f)
                {
                    break;
                }
            }
            return chosen;
        }

        // ------------------------------------------------------------------ helpers

        private int AddPiece(TrackPiece prefab, Vector3 position, float yaw = 0f, float scale = 1f, int parent = -1)
        {
            if (prefab == null)
            {
                return -1;
            }
            int id = nextId++;
            batch.Add(new PiecePlacement { Prefab = prefab, Position = position, Yaw = yaw, Scale = scale, Id = id, ParentId = parent });
            return id;
        }

        private void Coin(float x, float y, float z, int parent = -1)
        {
            if (AddPiece(catalog.coin, new Vector3(x, y, z), 0f, 1f, parent) >= 0)
            {
                layout.Coins += catalog.coin.Value;
            }
        }

        private void Gem(float x, float y, float z, int parent = -1)
        {
            if (catalog.gem == null)
            {
                Coin(x, y, z, parent);
                return;
            }
            AddPiece(catalog.gem, new Vector3(x, y, z), 0f, 1f, parent);
            layout.Coins += catalog.gem.Value;
        }

        private void CoinLine(int lane, float z, int count, float spacing, float y, int parent = -1)
        {
            for (int i = 0; i < count; i++)
            {
                Coin(X(lane), y, z + i * spacing, parent);
            }
        }

        /// <summary>Coins along the path of a normal jump that peaks over <paramref name="peakZ"/>.</summary>
        private void JumpArc(int lane, float peakZ, float baseY = 0f, int count = 7)
        {
            float speed = SpeedAt(peakZ);
            float launch = Mathf.Sqrt(2f * gravity * settings.JumpHeight);
            float airTime = 2f * launch / gravity;
            float takeoff = peakZ - speed * airTime * 0.5f;
            for (int i = 0; i < count; i++)
            {
                float t = airTime * (i + 0.5f) / count;
                float y = baseY + launch * t - 0.5f * gravity * t * t + CoinHeight;
                Coin(X(lane), y, takeoff + speed * t);
            }
        }

        private Obstacle Block()
        {
            return Pick(catalog.blocks);
        }

        private T Pick<T>(T[] items) where T : Object
        {
            if (items == null || items.Length == 0)
            {
                return null;
            }
            return items[rng.Next(items.Length)];
        }

        private float SpeedAt(float z)
        {
            return settings.SpeedAtDistance(z);
        }

        /// <summary>Distance between rows of obstacles inside a pattern: enough time to react at the speed there.</summary>
        private float RowGap(float z, float difficulty)
        {
            return Mathf.Max(9f, SpeedAt(z) * Mathf.Lerp(1.15f, 0.8f, difficulty));
        }

        /// <summary>Free stretch between two patterns.</summary>
        private float Spacing(float z, float difficulty)
        {
            return Mathf.Max(8f, SpeedAt(z) * Mathf.Lerp(1.3f, 0.75f, difficulty));
        }

        private int Lane()
        {
            return rng.Next(3) - 1;
        }

        private int OtherLane(int lane)
        {
            int other = rng.Next(2) - 1;
            return other >= lane ? other + 1 : other;
        }

        private static int ThirdLane(int a, int b)
        {
            return -(a + b);
        }

        private static float X(int lane)
        {
            return lane * LaneWidth;
        }

        private static Vector3 Pos(int lane, float z)
        {
            return new Vector3(X(lane), 0f, z);
        }

        private bool Chance(float probability)
        {
            return rng.NextDouble() < probability;
        }

        private static float Range(Random random, float min, float max)
        {
            return min + (float)random.NextDouble() * (max - min);
        }
    }
}
