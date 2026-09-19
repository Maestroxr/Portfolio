using System;
using System.Collections.Generic;
using UnityEngine;

namespace Portfolio.EndlessRunner
{
    /// <summary>
    /// Turns a level's <see cref="TrackLayout"/> into pooled objects: spawns tiles and pieces as the runner approaches,
    /// recycles what falls behind, and keeps the lists of live pickups and obstacles the manager tests the runner
    /// against. Endless levels are extended as the runner goes.
    /// </summary>
    public class TrackGenerator : MonoBehaviour
    {
        [SerializeField] internal PieceCatalog catalog;
        [SerializeField] internal TerrainCache[] tilePools = new TerrainCache[0];
        [SerializeField] internal TrackPiecePool[] piecePools = new TrackPiecePool[0];
        [Tooltip("How far ahead of the runner the track is spawned.")]
        [SerializeField] internal float spawnAhead = 170f;
        [Tooltip("How far behind the runner pieces are kept before they are recycled.")]
        [SerializeField] internal float keepBehind = 24f;
        [SerializeField] internal float endlessChunk = 300f;

        private readonly Dictionary<TrackPiece, TrackPiecePool> pools = new Dictionary<TrackPiece, TrackPiecePool>();
        private readonly Dictionary<TerrainBehaviour, TerrainCache> tileCaches = new Dictionary<TerrainBehaviour, TerrainCache>();
        private readonly List<TrackPiece> active = new List<TrackPiece>();
        private readonly List<Collidable> pickups = new List<Collidable>();
        private readonly List<Obstacle> obstacles = new List<Obstacle>();
        private readonly Dictionary<int, TrackPiece> spawnedById = new Dictionary<int, TrackPiece>();
        private readonly List<TrackPiece> attachedScratch = new List<TrackPiece>();
        private LayoutBuilder builder;
        private TrackLayout layout;
        private RunnerLevel level;
        private int nextTile;
        private int nextPiece;
        private int nextHint;
        private bool dirty;

        /// <summary>Raised when the runner reaches a tutorial hint.</summary>
        public event Action<string> HintReached;

        /// <summary>Live pickups. May contain pieces recycled this frame; check <see cref="TrackPiece.Live"/>.</summary>
        public IReadOnlyList<Collidable> Pickups => pickups;

        /// <summary>Live obstacles. May contain pieces recycled this frame; check <see cref="TrackPiece.Live"/>.</summary>
        public IReadOnlyList<Obstacle> Obstacles => obstacles;

        public PieceCatalog Catalog => catalog;

        public RunnerLevel Level => level;

        /// <summary>Coin value of the whole level (campaign levels).</summary>
        public int LevelCoins => layout != null ? layout.Coins : 0;

        public float FinishZ => layout != null ? layout.FinishZ : float.PositiveInfinity;

        private void Awake()
        {
            MapPools();
        }

        private void MapPools()
        {
            if (pools.Count > 0 || tileCaches.Count > 0)
            {
                return;
            }
            foreach (TrackPiecePool pool in piecePools)
            {
                if (pool != null && pool.Prefab != null)
                {
                    pools[pool.Prefab] = pool;
                }
            }
            foreach (TerrainCache cache in tilePools)
            {
                if (cache != null && cache.Prefab != null)
                {
                    tileCaches[cache.Prefab] = cache;
                }
            }
        }

        /// <summary>Clears the track and lays out <paramref name="runLevel"/> from scratch.</summary>
        public void Build(RunnerLevel runLevel, RunnerSettings settings, int seed, float gravity)
        {
            MapPools();
            Clear();
            level = runLevel;
            builder = new LayoutBuilder(runLevel, settings, catalog, gravity, seed);
            builder.GenerateUntil(runLevel.IsEndless ? endlessChunk : runLevel.Length + 120f);
            layout = builder.Layout;
            nextTile = 0;
            nextPiece = 0;
            nextHint = 0;
        }

        /// <summary>Spawns the track up to <see cref="spawnAhead"/> meters ahead and recycles pieces more than <paramref name="behind"/> meters back.</summary>
        public void UpdateTrack(float runnerZ, float behind)
        {
            if (layout == null)
            {
                return;
            }
            float ahead = runnerZ + spawnAhead;
            if (level.IsEndless && layout.GeneratedUntil < ahead + 60f)
            {
                builder.GenerateUntil(layout.GeneratedUntil + endlessChunk);
                TrimConsumed();
            }
            while (nextTile < layout.Tiles.Count && layout.Tiles[nextTile].Z < ahead)
            {
                SpawnTile(layout.Tiles[nextTile++]);
            }
            while (nextPiece < layout.Pieces.Count && layout.Pieces[nextPiece].Position.z < ahead)
            {
                SpawnPiece(layout.Pieces[nextPiece++]);
            }
            float cutoff = runnerZ - behind;
            float passed = runnerZ - 1.5f;
            foreach (TrackPiece piece in active)
            {
                if (!piece.Live)
                {
                    continue;
                }
                bool floating = piece is Coin || piece is PowerUpPickup;
                if (piece.Expired || piece.EndZ < cutoff || (floating && piece.transform.position.z < passed))
                {
                    RecycleInternal(piece);
                }
            }
            Compact();
        }

        /// <summary>Raises <see cref="HintReached"/> for the hints the runner has passed.</summary>
        public void CheckHints(float runnerZ)
        {
            while (layout != null && nextHint < layout.Hints.Count && layout.Hints[nextHint].Z <= runnerZ)
            {
                HintReached?.Invoke(layout.Hints[nextHint++].Text);
            }
        }

        /// <summary>Moves carts and animates knocked obstacles.</summary>
        public void Tick(float deltaTime, float runnerZ)
        {
            for (int i = 0; i < obstacles.Count; i++)
            {
                Obstacle obstacle = obstacles[i];
                if (obstacle.Live)
                {
                    obstacle.Tick(deltaTime, runnerZ);
                }
            }
        }

        /// <summary>The theme of the track at <paramref name="z"/>.</summary>
        public RunnerTheme ThemeAt(float z)
        {
            return level != null ? level.ThemeAt(z) : null;
        }

        /// <summary>The chasm gap the runner at <paramref name="z"/> fell into: the last one starting before it.</summary>
        public bool TryGetGap(float z, out Vector2 gap)
        {
            gap = Vector2.zero;
            if (layout == null)
            {
                return false;
            }
            bool found = false;
            foreach (Vector2 candidate in layout.Gaps)
            {
                if (candidate.x <= z + 0.5f && (!found || candidate.x > gap.x))
                {
                    gap = candidate;
                    found = true;
                }
            }
            return found;
        }

        /// <summary>The next chasm gap that starts within <paramref name="range"/> meters after <paramref name="z"/>.</summary>
        public bool TryGetGapAhead(float z, float range, out Vector2 gap)
        {
            gap = Vector2.zero;
            if (layout == null)
            {
                return false;
            }
            foreach (Vector2 candidate in layout.Gaps)
            {
                if (candidate.y >= z && candidate.x <= z + range)
                {
                    gap = candidate;
                    return true;
                }
            }
            return false;
        }

        /// <summary>Takes a piece off the track, together with everything placed on it.</summary>
        public void Recycle(TrackPiece piece)
        {
            RecycleInternal(piece);
            dirty = true;
        }

        /// <summary>Recycles every piece on the track.</summary>
        public void Clear()
        {
            foreach (TrackPiece piece in active)
            {
                RecycleInternal(piece);
            }
            dirty = true;
            Compact();
            spawnedById.Clear();
            layout = null;
            builder = null;
        }

        private void SpawnTile(TilePlacement placement)
        {
            if (placement.Prefab == null || !tileCaches.TryGetValue(placement.Prefab, out TerrainCache cache))
            {
                return;
            }
            TerrainBehaviour tile = cache.Deploy();
            tile.transform.SetPositionAndRotation(new Vector3(0f, 0f, placement.Z), Quaternion.identity);
            tile.transform.localScale = Vector3.one;
            tile.ApplyTheme(placement.Theme);
            tile.PlacementId = -1;
            Activate(tile);
        }

        private void SpawnPiece(PiecePlacement placement)
        {
            if (placement.Prefab == null || !pools.TryGetValue(placement.Prefab, out TrackPiecePool pool))
            {
                return;
            }
            TrackPiece piece = pool.Deploy();
            piece.transform.SetPositionAndRotation(placement.Position, Quaternion.Euler(0f, placement.Yaw, 0f));
            piece.transform.localScale = Vector3.one * placement.Scale;
            piece.PlacementId = placement.Id;
            spawnedById[placement.Id] = piece;
            if (placement.ParentId >= 0 && spawnedById.TryGetValue(placement.ParentId, out TrackPiece parent) && parent.Live)
            {
                parent.Attached.Add(piece);
            }
            Activate(piece);
            if (piece is Collidable pickup)
            {
                pickups.Add(pickup);
            }
            else if (piece is Obstacle obstacle)
            {
                obstacles.Add(obstacle);
            }
        }

        private void Activate(TrackPiece piece)
        {
            piece.Live = true;
            piece.Expired = false;
            active.Add(piece);
            piece.OnSpawned();
        }

        private void RecycleInternal(TrackPiece piece)
        {
            if (piece == null || !piece.Live)
            {
                return;
            }
            piece.Live = false;
            if (piece.Attached.Count > 0)
            {
                attachedScratch.Clear();
                attachedScratch.AddRange(piece.Attached);
                foreach (TrackPiece attached in attachedScratch)
                {
                    RecycleInternal(attached);
                }
                attachedScratch.Clear();
            }
            if (piece.PlacementId >= 0 && spawnedById.TryGetValue(piece.PlacementId, out TrackPiece mapped) && mapped == piece)
            {
                spawnedById.Remove(piece.PlacementId);
            }
            piece.OnRecycled();
            piece.ReturnToPool();
            dirty = true;
        }

        private void Compact()
        {
            if (!dirty)
            {
                return;
            }
            active.RemoveAll(piece => piece == null || !piece.Live);
            pickups.RemoveAll(piece => piece == null || !piece.Live);
            obstacles.RemoveAll(piece => piece == null || !piece.Live);
            dirty = false;
        }

        /// <summary>Drops the layout entries an endless run has already spawned, so the lists do not grow forever.</summary>
        private void TrimConsumed()
        {
            if (nextPiece > 2000)
            {
                layout.Pieces.RemoveRange(0, nextPiece);
                nextPiece = 0;
            }
            if (nextTile > 500)
            {
                layout.Tiles.RemoveRange(0, nextTile);
                nextTile = 0;
            }
            if (nextHint > 0)
            {
                layout.Hints.RemoveRange(0, nextHint);
                nextHint = 0;
            }
        }
    }
}
