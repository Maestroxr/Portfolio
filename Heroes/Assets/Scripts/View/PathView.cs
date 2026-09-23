using System.Collections.Generic;
using UnityEngine;

namespace Portfolio.Heroes
{
    /// <summary>
    /// The trail of arrows a hero would take to where the pointer is: white for the steps he has the movement for
    /// today, red for the ones he would reach on a later day, with a ring on the cell he would end at.
    /// </summary>
    public sealed class PathView : MonoBehaviour
    {
        private static readonly Color Today = new Color(1f, 0.95f, 0.7f, 0.95f);
        private static readonly Color Later = new Color(0.95f, 0.35f, 0.3f, 0.9f);

        private MapView map;
        private Material material;
        private Mesh arrow;
        private Mesh ring;
        private readonly List<MeshRenderer> pieces = new List<MeshRenderer>();
        private int used;

        public void Setup(MapView view, HeroesArt art)
        {
            map = view;
            material = art.marker;
            arrow = Arrow();
            ring = Ring();
        }

        /// <summary>Lays the trail out along <paramref name="path"/>, the first <paramref name="reach"/> cells in white.</summary>
        public void Show(int from, IReadOnlyList<int> path, int reach)
        {
            Clear();
            if (path == null || path.Count == 0)
            {
                return;
            }
            int previous = from;
            for (int i = 0; i < path.Count; i++)
            {
                int cell = path[i];
                Vector3 at = map.Point(cell, 0.12f);
                Vector3 back = map.Point(previous, 0.12f);
                bool last = i == path.Count - 1;
                Piece(last ? ring : arrow, at, at - back, i < reach ? Today : Later, last ? 1.5f : 1f);
                previous = cell;
            }
            for (int i = used; i < pieces.Count; i++)
            {
                pieces[i].gameObject.SetActive(false);
            }
        }

        public void Clear()
        {
            used = 0;
            foreach (MeshRenderer piece in pieces)
            {
                piece.gameObject.SetActive(false);
            }
        }

        private void Piece(Mesh mesh, Vector3 at, Vector3 direction, Color color, float scale)
        {
            MeshRenderer piece;
            if (used < pieces.Count)
            {
                piece = pieces[used];
            }
            else
            {
                var made = new GameObject("Step", typeof(MeshFilter), typeof(MeshRenderer));
                made.transform.SetParent(transform, false);
                piece = made.GetComponent<MeshRenderer>();
                piece.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                piece.receiveShadows = false;
                piece.sharedMaterial = material;
                pieces.Add(piece);
            }
            used++;
            piece.gameObject.SetActive(true);
            piece.GetComponent<MeshFilter>().sharedMesh = mesh;
            piece.transform.position = at;
            piece.transform.localScale = Vector3.one * scale;
            direction.y = 0f;
            piece.transform.rotation = direction.sqrMagnitude > 0.0001f
                ? Quaternion.LookRotation(Vector3.down, direction)
                : Quaternion.LookRotation(Vector3.down, Vector3.forward);
            var block = new MaterialPropertyBlock();
            block.SetColor("_BaseColor", color);
            piece.SetPropertyBlock(block);
        }

        /// <summary>A flat arrowhead pointing along +z, lying in the xz plane.</summary>
        private static Mesh Arrow()
        {
            var mesh = new Mesh { name = "PathArrow" };
            mesh.vertices = new[]
            {
                new Vector3(0f, 0.34f, 0f), new Vector3(-0.26f, -0.2f, 0f), new Vector3(0f, -0.06f, 0f),
                new Vector3(0.26f, -0.2f, 0f)
            };
            mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            mesh.RecalculateNormals();
            return mesh;
        }

        /// <summary>A flat ring for the cell a path ends on.</summary>
        private static Mesh Ring()
        {
            const int segments = 24;
            const float inner = 0.42f;
            const float outer = 0.52f;
            var vertices = new Vector3[segments * 2];
            var triangles = new int[segments * 6];
            for (int i = 0; i < segments; i++)
            {
                float angle = i / (float)segments * Mathf.PI * 2f;
                var direction = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
                vertices[i * 2] = direction * inner;
                vertices[i * 2 + 1] = direction * outer;
                int next = (i + 1) % segments;
                triangles[i * 6 + 0] = i * 2;
                triangles[i * 6 + 1] = i * 2 + 1;
                triangles[i * 6 + 2] = next * 2 + 1;
                triangles[i * 6 + 3] = i * 2;
                triangles[i * 6 + 4] = next * 2 + 1;
                triangles[i * 6 + 5] = next * 2;
            }
            var mesh = new Mesh { name = "PathRing", vertices = vertices, triangles = triangles };
            mesh.RecalculateNormals();
            return mesh;
        }
    }
}
