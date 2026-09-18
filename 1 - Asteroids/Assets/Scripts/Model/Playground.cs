using UnityEngine;

namespace Portfolio.Asteroids
{
    /// <summary>The wrapping playfield: its middle, size (from the mesh bounds) and the margin objects wrap at.</summary>
    public class Playground : MonoBehaviour
    {
        [field: SerializeField]
        public Vector3 Middle { get; private set; }
        [field: SerializeField]
        public Vector3 Margin { get; private set; }
        public Vector3 Size => playroundMesh.bounds.size;

        [SerializeField]
        private MeshRenderer playroundMesh;
    }
}
