using System;
using UnityEngine;

namespace Portfolio.DeathRace
{
    /// <summary>The kinds of set piece a track places near its road (built by the track builder, not themed props).</summary>
    public enum SetPieceKind
    {
        /// <summary>A ring of stones round a fire that burns all race long (the Crucible).</summary>
        FirePit = 0,

        /// <summary>A stack of shipping containers (industrial yards).</summary>
        Containers = 1,

        /// <summary>A wall of old tyres stacked behind the barrier, where a corner catches the careless.</summary>
        Tyres = 2,

        /// <summary>A refinery's storage tank: a squat drum with a dome, a band of paint and a stair round it.</summary>
        StorageTank = 3,

        /// <summary>A rack of pipes on steel frames running along the back of the barrier (refineries).</summary>
        PipeRack = 4,

        /// <summary>A pile-up of burnt-out cars shoved against the barrier (the city's gridlock).</summary>
        Wrecks = 5,

        /// <summary>Rocks an avalanche brought down onto the roadside, capped with snow.</summary>
        Boulders = 6
    }

    /// <summary>
    /// The shape of a track: the control points of its centre line (metres, in the order of travel), how wide the road
    /// is, how many laps a race has, where the line is and where the pickups lie, and the seed of its scenery.
    ///
    /// It is plain data written in code (<see cref="TrackLayouts"/>), so a track is the same in the editor, in a player and
    /// online, and its rules can be checked without a scene (<see cref="TrackGeometry.Problems"/>).
    /// </summary>
    public sealed class TrackLayout
    {
        /// <summary>
        /// A stretch of road with a width of its own (a narrow gorge, say), from one distance along the centre line to
        /// another in the order of travel. The road blends into it over <see cref="TrackGeometry.StretchBlend"/> metres.
        /// </summary>
        [Serializable]
        public struct Stretch
        {
            public float From;
            public float To;
            public float Width;

            public Stretch(float from, float to, float width)
            {
                From = from;
                To = to;
                Width = width;
            }
        }

        /// <summary>A set piece beside the road: what it is, how far along the centre line, and on which side.</summary>
        [Serializable]
        public struct SetPiece
        {
            public SetPieceKind Kind;
            public float Distance;

            /// <summary>+1 left of the direction of travel, -1 right.</summary>
            public float Side;

            /// <summary>How far behind the barrier it stands (to its near edge for a tank), in metres (0 = the kind's own distance).</summary>
            public float Offset;

            public SetPiece(SetPieceKind kind, float distance, float side, float offset = 0f)
            {
                Kind = kind;
                Distance = distance;
                Side = side;
                Offset = offset;
            }
        }

        /// <summary>The track's name, an English key of the localization table.</summary>
        public string Name;

        public Biome Biome;

        /// <summary>The control points of the centre line in metres (x east, y north), in the order of travel, closed.</summary>
        public Vector2[] Points;

        /// <summary>The width of the road in metres (hairpins come out a little wider; <see cref="Stretches"/> differ).</summary>
        public float Width = 14f;

        public int Laps = 4;

        /// <summary>The seed of the scenery: the same track always looks the same.</summary>
        public int Seed;

        /// <summary>Where the start and finish line is, in metres along the centre line from the first point.</summary>
        public float StartDistance;

        /// <summary>Where clusters of pickups lie, in metres along the centre line.</summary>
        public float[] PickupDistances = new float[0];

        /// <summary>The race against the champion.</summary>
        public bool Showdown;

        /// <summary>One line about the track for the quick race screen (an English key).</summary>
        public string Description = "";

        public Stretch[] Stretches = new Stretch[0];

        public SetPiece[] SetPieces = new SetPiece[0];
    }
}
