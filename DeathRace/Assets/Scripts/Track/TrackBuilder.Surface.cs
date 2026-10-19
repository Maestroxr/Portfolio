using System;
using System.Collections.Generic;
using Gamebox.Racing;
using UnityEngine;

namespace Portfolio.DeathRace
{
    /// <summary>
    /// The ground half of a track's look: the road, the verges, the curbs, the barriers, the ground plane, the start line
    /// and the painted grid, laid out in the UV convention the art follows:
    ///
    /// road: U 0 at the left edge to 1 at the right (facing the way of travel), V metres along / 16; verge: U 0 at the road
    /// to 1 under the barrier, V metres / 8; curb: U 0 at the road to 1 outside, V metres / 4 (a red and a white stripe per
    /// repeat); barrier: U metres along / 4, V 0 at the foot to 0.9 at the top edge on both faces, and 0.9 to 1 across the
    /// top from the road side; ground: world x, z
    /// / 32; line: metres across / 4, metres along / 4 (one 4 m checker tile deep). Lengths round to whole repeats per lap.
    ///
    /// Each surface is cut into pieces of about 64 m along the track (the ground into 96 m tiles): the camera skips the
    /// pieces it cannot see, and URP's forward renderer, which lights each object with its four nearest lights only,
    /// lights each piece with the lamps and fires beside it.
    /// </summary>
    public static partial class TrackBuilder
    {
        private const float RoadTile = 16f;
        private const float ShoulderTile = 8f;
        private const float CurbTile = 4f;
        private const float BarrierTile = 4f;
        private const float GroundTile = 32f;
        private const float LineTile = 4f;
        private const float LineDepth = 4f;
        private const float CurbWidth = 1.3f;
        private const float GroundMargin = 160f;
        private const float GroundPiece = 96f;
        private const int PieceRows = 32;

        // Heights of the layers of paint (the road is y = 0): the paint just above it, the verges and curbs just below.
        private const float PaintHeight = 0.012f;
        private const float CurbHeight = -0.004f;
        private const float ShoulderHeight = -0.012f;

        /// <summary>Where the barrier's faces end and its top cap begins in V (the cap runs on to 1 across its width).</summary>
        private const float TopCapStart = 0.9f;

        /// <summary>The cross-section of a barrier (outwards from its face, height): a concrete jersey barrier.</summary>
        private static readonly Vector2[] BarrierProfile =
        {
            new Vector2(0f, 0f), new Vector2(0.06f, 0.3f), new Vector2(0.2f, 0.85f), new Vector2(0.24f, BarrierHeight),
            new Vector2(0.46f, BarrierHeight), new Vector2(0.5f, 0.85f), new Vector2(0.64f, 0.3f), new Vector2(0.7f, 0f)
        };

        private static void BuildSurface(Dressing dressing)
        {
            BuildRoad(dressing);
            BuildShoulders(dressing);
            BuildCurbs(dressing);
            BuildBarriers(dressing);
            BuildGround(dressing);
            BuildLine(dressing);
            BuildPaint(dressing);
        }

        private static void BuildRoad(Dressing d)
        {
            int rows = d.Count + 1;
            var left = new Vector3[rows];
            var right = new Vector3[rows];
            var uvLeft = new Vector2[rows];
            var uvRight = new Vector2[rows];
            float tile = Tile(d.Path.Length, RoadTile);
            for (int i = 0; i < rows; i++)
            {
                float v = i * d.Step / tile;
                left[i] = d.At(i, d.Half[i], 0f);
                right[i] = d.At(i, -d.Half[i], 0f);
                uvLeft[i] = new Vector2(0f, v);
                uvRight[i] = new Vector2(1f, v);
            }
            DrawPieces(d, "Road", d.Material(TrackSurface.Road), false, (mesh, first, last) =>
                mesh.Strip(Slice(left, first, last), Slice(right, first, last), Slice(uvLeft, first, last), Slice(uvRight, first, last)));
        }

        /// <summary>The verges: from the edge of the road to just under the foot of each barrier.</summary>
        private static void BuildShoulders(Dressing d)
        {
            int rows = d.Count + 1;
            var strips = new List<(Vector3[] inner, Vector3[] outer, Vector2[] uvInner, Vector2[] uvOuter)>();
            for (int side = 1; side >= -1; side -= 2)
            {
                var inner = new Vector3[rows];
                var outer = new Vector3[rows];
                for (int i = 0; i < rows; i++)
                {
                    inner[i] = d.At(i, side * d.Half[i], ShoulderHeight);
                    outer[i] = d.At(i, side * (d.Half[i] + TrackGeometry.Shoulder + 0.35f), ShoulderHeight);
                }
                float[] along = Along(outer);
                float tile = Tile(along[rows - 1], ShoulderTile);
                var uvInner = new Vector2[rows];
                var uvOuter = new Vector2[rows];
                for (int i = 0; i < rows; i++)
                {
                    uvInner[i] = new Vector2(0f, along[i] / tile);
                    uvOuter[i] = new Vector2(1f, along[i] / tile);
                }
                strips.Add((inner, outer, uvInner, uvOuter));
            }
            DrawPieces(d, "Verges", d.Material(TrackSurface.Shoulder), false, (mesh, first, last) =>
            {
                foreach ((Vector3[] inner, Vector3[] outer, Vector2[] uvInner, Vector2[] uvOuter) in strips)
                {
                    mesh.Strip(Slice(inner, first, last), Slice(outer, first, last), Slice(uvInner, first, last), Slice(uvOuter, first, last));
                }
            });
        }

        /// <summary>
        /// Curbs on the inside of every real corner and on the outside of the tight ones, a little longer than the bend,
        /// tapered at their ends.
        /// </summary>
        private static void BuildCurbs(Dressing d)
        {
            int count = d.Count;
            float width = d.Track.Layout.Width;
            float inside = 1f / (5f * width);
            float outside = 1f / (3f * width);
            var pieces = new Dictionary<int, TrackMesh>();
            for (int side = 1; side >= -1; side -= 2)
            {
                var wanted = new bool[count];
                for (int i = 0; i < count; i++)
                {
                    float curvature = d.Path.CurvatureAt(i * d.Step) * side;
                    wanted[i] = curvature > inside || curvature < -outside;
                }
                foreach (Vector2Int run in Runs(Dilate(wanted, 4), 6))
                {
                    int piece = (run.x % count) / PieceRows;
                    if (!pieces.TryGetValue(piece, out TrackMesh mesh))
                    {
                        mesh = new TrackMesh();
                        pieces[piece] = mesh;
                    }
                    AddCurb(d, mesh, side, run.x, run.y);
                }
            }
            Material material = d.Material(TrackSurface.Curb);
            foreach (KeyValuePair<int, TrackMesh> piece in pieces)
            {
                d.Draw($"Curbs {piece.Key}", piece.Value, material, false);
            }
        }

        private static void AddCurb(Dressing d, TrackMesh mesh, int side, int first, int last)
        {
            int rows = last - first + 1;
            var inner = new Vector3[rows];
            var outer = new Vector3[rows];
            for (int k = 0; k < rows; k++)
            {
                int i = (first + k) % d.Count;
                float taper = Mathf.Clamp01(Mathf.Min(k, rows - 1 - k) / 2f);
                inner[k] = d.At(i, side * d.Half[i], CurbHeight);
                outer[k] = d.At(i, side * (d.Half[i] + CurbWidth * Mathf.Lerp(0.25f, 1f, taper)), CurbHeight);
            }
            float[] along = Along(inner);
            var uvInner = new Vector2[rows];
            var uvOuter = new Vector2[rows];
            for (int k = 0; k < rows; k++)
            {
                uvInner[k] = new Vector2(0f, along[k] / CurbTile);
                uvOuter[k] = new Vector2(1f, along[k] / CurbTile);
            }
            mesh.Strip(inner, outer, uvInner, uvOuter);
        }

        /// <summary>Both barriers as closed ribbons of the jersey profile, flat shaded face by face.</summary>
        private static void BuildBarriers(Dressing d)
        {
            int rows = d.Count + 1;
            var alongs = new Dictionary<int, (float[] along, float tile)>();
            for (int side = 1; side >= -1; side -= 2)
            {
                var foot = new Vector3[rows];
                for (int i = 0; i < rows; i++)
                {
                    foot[i] = d.At(i, side * (d.Half[i] + TrackGeometry.Shoulder), 0f);
                }
                float[] along = Along(foot);
                alongs[side] = (along, Tile(along[rows - 1], BarrierTile));
            }
            DrawPieces(d, "Barriers", d.Material(TrackSurface.Barrier), true, (mesh, first, last) =>
            {
                for (int side = 1; side >= -1; side -= 2)
                {
                    (float[] along, float tile) = alongs[side];
                    AddBarrier(d, mesh, side, first, last, along, tile);
                }
            });
        }

        private static void AddBarrier(Dressing d, TrackMesh mesh, int side, int first, int last, float[] along, float tile)
        {
            for (int f = 0; f + 1 < BarrierProfile.Length; f++)
            {
                Vector2 p = BarrierProfile[f];
                Vector2 q = BarrierProfile[f + 1];
                Vector2 faceNormal = new Vector2(-(q.y - p.y), q.x - p.x).normalized;
                bool top = Mathf.Approximately(p.y, BarrierHeight) && Mathf.Approximately(q.y, BarrierHeight);
                int start = mesh.VertexCount;
                for (int i = first; i <= last; i++)
                {
                    Vector2 outward = d.Normal[i] * side;
                    Vector3 normal = (new Vector3(outward.x, 0f, outward.y) * faceNormal.x + Vector3.up * faceNormal.y).normalized;
                    float offset = d.Half[i] + TrackGeometry.Shoulder;
                    float u = along[i] / tile;
                    mesh.Vertex(d.At(i, side * (offset + p.x), p.y), normal, new Vector2(u, top ? TopCapStart : p.y / BarrierHeight * TopCapStart));
                    mesh.Vertex(d.At(i, side * (offset + q.x), q.y), normal, new Vector2(u, top ? 1f : q.y / BarrierHeight * TopCapStart));
                }
                for (int i = first; i < last; i++)
                {
                    int v = start + (i - first) * 2;
                    Vector2 outward = d.Normal[i] * side;
                    Vector3 facing = new Vector3(outward.x, 0f, outward.y) * faceNormal.x + Vector3.up * faceNormal.y;
                    mesh.Quad(v, v + 2, v + 3, v + 1, facing);
                }
            }
        }

        /// <summary>The ground plane under everything, reaching well past what any camera shows, in tiles.</summary>
        private static void BuildGround(Dressing d)
        {
            Bounds bounds = d.Track.Bounds;
            float minX = bounds.min.x - GroundMargin;
            float minZ = bounds.min.z - GroundMargin;
            int tilesX = Mathf.CeilToInt((bounds.size.x + GroundMargin * 2f) / GroundPiece);
            int tilesZ = Mathf.CeilToInt((bounds.size.z + GroundMargin * 2f) / GroundPiece);
            Material material = d.Material(TrackSurface.Ground);
            MaterialPropertyBlock tint = GroundTint(d);
            for (int x = 0; x < tilesX; x++)
            {
                for (int z = 0; z < tilesZ; z++)
                {
                    float x0 = minX + x * GroundPiece;
                    float z0 = minZ + z * GroundPiece;
                    Vector3[] corners =
                    {
                        new Vector3(x0, GroundHeight, z0), new Vector3(x0, GroundHeight, z0 + GroundPiece),
                        new Vector3(x0 + GroundPiece, GroundHeight, z0 + GroundPiece), new Vector3(x0 + GroundPiece, GroundHeight, z0)
                    };
                    var mesh = new TrackMesh();
                    var indices = new int[4];
                    for (int i = 0; i < 4; i++)
                    {
                        indices[i] = mesh.Vertex(corners[i], Vector3.up, new Vector2(corners[i].x / GroundTile, corners[i].z / GroundTile));
                    }
                    mesh.Quad(indices[0], indices[1], indices[2], indices[3], Vector3.up);
                    GameObject ground = d.Draw($"Ground {x},{z}", mesh, material, false);
                    if (tint != null)
                    {
                        ground.GetComponent<MeshRenderer>().SetPropertyBlock(tint);
                    }
                }
            }
        }

        /// <summary>The land's ground tint on top of its ground material's own colour (none when it is white).</summary>
        private static MaterialPropertyBlock GroundTint(Dressing d)
        {
            if (d.Look == null || d.Look.ground == null || d.Look.groundTint == Color.white)
            {
                return null;
            }
            var block = new MaterialPropertyBlock();
            Color baseColor = d.Look.ground.HasProperty("_BaseColor") ? d.Look.ground.GetColor("_BaseColor") : Color.white;
            block.SetColor("_BaseColor", baseColor * d.Look.groundTint);
            return block;
        }

        /// <summary>The start and finish line: a checkered band across the road, 4 m deep, centred on the line.</summary>
        private static void BuildLine(Dressing d)
        {
            const int rows = 5;
            var left = new Vector3[rows];
            var right = new Vector3[rows];
            var uvLeft = new Vector2[rows];
            var uvRight = new Vector2[rows];
            for (int k = 0; k < rows; k++)
            {
                float along = -LineDepth * 0.5f + LineDepth * k / (rows - 1);
                float distance = d.Track.LineDistance + along;
                Vector2 centre = d.Path.PointAt(distance);
                Vector2 normal = d.Path.NormalAt(distance);
                float half = d.Geometry.HalfWidthAt(distance);
                left[k] = RacePath.ToWorld(centre + normal * half, PaintHeight);
                right[k] = RacePath.ToWorld(centre - normal * half, PaintHeight);
                float v = (along + LineDepth * 0.5f) / LineTile;
                uvLeft[k] = new Vector2(0f, v);
                uvRight[k] = new Vector2(half * 2f / LineTile, v);
            }
            var mesh = new TrackMesh();
            mesh.Strip(left, right, uvLeft, uvRight);
            d.Draw("Start Line", mesh, d.Material(TrackSurface.Line), false);
        }

        /// <summary>The painted grid boxes (eight of them), and the edge lines when the theme's road has none of its own.</summary>
        private static void BuildPaint(Dressing d)
        {
            Material paint = d.Material(TrackSurface.Paint);
            var grid = new TrackMesh();
            for (int slot = 0; slot < 8; slot++)
            {
                Pose pose = d.Geometry.GridSlot(slot);
                Vector3 forward = pose.rotation * Vector3.forward;
                Vector3 right = pose.rotation * Vector3.right;
                var centre = new Vector3(pose.position.x, PaintHeight, pose.position.z);
                PaintBar(grid, centre + forward * 2.75f, right, forward, 3.5f, 0.3f);
                PaintBar(grid, centre + forward * 1.1f + right * 1.6f, right, forward, 0.25f, 3.6f);
                PaintBar(grid, centre + forward * 1.1f - right * 1.6f, right, forward, 0.25f, 3.6f);
            }
            d.Draw("Grid", grid, paint, false);
            if (TrackMaterials.HasRoad(d.Look))
            {
                return;
            }
            int rows = d.Count + 1;
            var lines = new List<(Vector3[] inner, Vector3[] outer)>();
            for (int side = 1; side >= -1; side -= 2)
            {
                var inner = new Vector3[rows];
                var outer = new Vector3[rows];
                for (int i = 0; i < rows; i++)
                {
                    inner[i] = d.At(i, side * (d.Half[i] - 0.45f), PaintHeight);
                    outer[i] = d.At(i, side * (d.Half[i] - 0.7f), PaintHeight);
                }
                lines.Add((inner, outer));
            }
            var uv = new Vector2[rows];
            DrawPieces(d, "Edge Lines", paint, false, (mesh, first, last) =>
            {
                foreach ((Vector3[] inner, Vector3[] outer) in lines)
                {
                    mesh.Strip(Slice(inner, first, last), Slice(outer, first, last), Slice(uv, first, last), Slice(uv, first, last));
                }
            });
        }

        private static void PaintBar(TrackMesh mesh, Vector3 centre, Vector3 right, Vector3 forward, float across, float along)
        {
            Vector3 x = right * (across * 0.5f);
            Vector3 z = forward * (along * 0.5f);
            int a = mesh.Vertex(centre - x - z, Vector3.up, Vector2.zero);
            int b = mesh.Vertex(centre - x + z, Vector3.up, Vector2.zero);
            int c = mesh.Vertex(centre + x + z, Vector3.up, Vector2.zero);
            int e = mesh.Vertex(centre + x - z, Vector3.up, Vector2.zero);
            mesh.Quad(a, b, c, e, Vector3.up);
        }

        /// <summary>Builds a surface piece by piece along the track (rows first..last of the closed frames, inclusive).</summary>
        private static void DrawPieces(Dressing d, string name, Material material, bool shadows, Action<TrackMesh, int, int> build)
        {
            for (int first = 0; first < d.Count; first += PieceRows)
            {
                int last = Mathf.Min(first + PieceRows, d.Count);
                var mesh = new TrackMesh();
                build(mesh, first, last);
                if (!mesh.IsEmpty)
                {
                    d.Draw($"{name} {first / PieceRows}", mesh, material, shadows);
                }
            }
        }

        private static ArraySegment<T> Slice<T>(T[] rows, int first, int last)
        {
            return new ArraySegment<T>(rows, first, last - first + 1);
        }

        /// <summary>The running length along a row of points (the first is 0).</summary>
        private static float[] Along(IReadOnlyList<Vector3> row)
        {
            var along = new float[row.Count];
            for (int i = 1; i < row.Count; i++)
            {
                along[i] = along[i - 1] + Vector3.Distance(row[i - 1], row[i]);
            }
            return along;
        }

        /// <summary>Widens every true run of a closed loop of flags by <paramref name="reach"/> on both ends.</summary>
        private static bool[] Dilate(bool[] flags, int reach)
        {
            int count = flags.Length;
            var result = new bool[count];
            for (int i = 0; i < count; i++)
            {
                if (!flags[i])
                {
                    continue;
                }
                for (int j = -reach; j <= reach; j++)
                {
                    result[((i + j) % count + count) % count] = true;
                }
            }
            return result;
        }

        /// <summary>The runs of true flags round a closed loop (first, last index; last may pass the end), at least <paramref name="minimum"/> long.</summary>
        private static List<Vector2Int> Runs(bool[] flags, int minimum)
        {
            int count = flags.Length;
            var runs = new List<Vector2Int>();
            int start = Array.IndexOf(flags, false);
            if (start < 0)
            {
                runs.Add(new Vector2Int(0, count));
                return runs;
            }
            int first = -1;
            for (int k = 1; k <= count; k++)
            {
                int i = start + k;
                bool on = flags[i % count];
                if (on && first < 0)
                {
                    first = i;
                }
                else if (!on && first >= 0)
                {
                    if (i - first >= minimum)
                    {
                        runs.Add(new Vector2Int(first, i - 1));
                    }
                    first = -1;
                }
            }
            return runs;
        }
    }
}
