using System.Collections.Generic;
using Gamebox;
using UnityEngine;

namespace Portfolio.EndlessRunner.Tests
{
    /// <summary>
    /// What a track is laid out from, made for a test and destroyed after it: a catalog of stand-in pieces (the layout
    /// only looks at their kind, length and value), a theme with scenery, settings and levels.
    /// </summary>
    internal sealed class Tracks
    {
        public const float Gravity = 32f;

        private readonly TemporaryObjects objects = new TemporaryObjects();

        public PieceCatalog Catalog { get; }

        public RunnerTheme Theme { get; }

        public Tracks()
        {
            Catalog = Asset<PieceCatalog>();
            Catalog.roadTile = Piece<TerrainBehaviour>("RoadTile", 6f);
            Catalog.chasmTile = Piece<TerrainBehaviour>("ChasmTile", 6f);
            Catalog.coin = Piece<Coin>("Coin", 1f);
            Catalog.gem = Piece<Coin>("Gem", 1f);
            Catalog.gem.value = 5;
            Catalog.gem.gem = true;
            Catalog.magnet = PowerUp(PowerUpType.Magnet);
            Catalog.shield = PowerUp(PowerUpType.Shield);
            Catalog.multiplier = PowerUp(PowerUpType.Multiplier);
            Catalog.superJump = PowerUp(PowerUpType.SuperJump);
            Catalog.jumpPad = Piece<JumpPad>("JumpPad", 2f);
            Catalog.hurdle = Obstacle(ObstacleKind.Hurdle, 1f);
            Catalog.barrier = Obstacle(ObstacleKind.Barrier, 1f);
            Catalog.ramp = Obstacle(ObstacleKind.Ramp, 6f);
            Catalog.bridge = Obstacle(ObstacleKind.Bridge, 4.5f);
            Catalog.cart = Obstacle(ObstacleKind.Cart, 3f);
            Catalog.blocks = new[] { Obstacle(ObstacleKind.Block, 2f), Obstacle(ObstacleKind.Block, 2f) };
            Catalog.shortWagons = new[] { Obstacle(ObstacleKind.Platform, 10f), Obstacle(ObstacleKind.Platform, 10f) };
            Catalog.longWagons = new[] { Obstacle(ObstacleKind.Platform, 16f), Obstacle(ObstacleKind.Platform, 16f) };
            Catalog.finishLine = Piece<TrackPiece>("FinishLine", 1f);

            Theme = Asset<RunnerTheme>();
            Theme.scenery = new[]
            {
                new SceneryItem { prefab = Piece<TrackPiece>("Tree", 1f), weight = 2f },
                new SceneryItem { prefab = Piece<TrackPiece>("Rock", 1f), weight = 1f }
            };
            Theme.sceneryPerTile = 3;
            Theme.roadsideProp = Piece<TrackPiece>("Fence", 6f);
            Theme.roadsideEvery = 2;
        }

        public RunnerSettings Settings(float start = 10f, float max = 16f, float jump = 1.8f)
        {
            RunnerSettings settings = Asset<RunnerSettings>();
            settings.ForwardSpeed = start;
            settings.MaxSpeed = max;
            settings.Acceleration = 0.12f;
            settings.JumpHeight = jump;
            return settings;
        }

        /// <summary>A level with everything on its track; a length of 0 makes it endless.</summary>
        public RunnerLevel Level(float length)
        {
            RunnerLevel level = Asset<RunnerLevel>();
            level.theme = Theme;
            level.length = length;
            level.startDifficulty = 0.2f;
            level.endDifficulty = 0.9f;
            level.features = TrackFeatures.All;
            level.powerUps = new[] { PowerUpType.Magnet, PowerUpType.Shield, PowerUpType.Multiplier, PowerUpType.SuperJump };
            return level;
        }

        public LayoutBuilder Builder(RunnerLevel level, RunnerSettings settings, int seed)
        {
            return new LayoutBuilder(level, settings, Catalog, Gravity, seed);
        }

        /// <summary>The layout of a whole campaign level, as the track generator asks for it.</summary>
        public TrackLayout Layout(RunnerLevel level, RunnerSettings settings, int seed)
        {
            LayoutBuilder builder = Builder(level, settings, seed);
            builder.GenerateUntil(level.Length + 120f);
            return builder.Layout;
        }

        public void Destroy()
        {
            objects.Dispose();
        }

        private T Asset<T>() where T : ScriptableObject
        {
            return objects.Asset<T>();
        }

        private T Piece<T>(string name, float length) where T : TrackPiece
        {
            T piece = objects.Component<T>(name);
            piece.length = length;
            return piece;
        }

        private Obstacle Obstacle(ObstacleKind kind, float length)
        {
            Obstacle obstacle = Piece<Obstacle>(kind.ToString(), length);
            obstacle.kind = kind;
            return obstacle;
        }

        private PowerUpPickup PowerUp(PowerUpType type)
        {
            PowerUpPickup pickup = Piece<PowerUpPickup>(type.ToString(), 1f);
            pickup.type = type;
            return pickup;
        }
    }
}
