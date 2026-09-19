using UnityEngine;

namespace Portfolio.EndlessRunner
{
    /// <summary>
    /// One tile of the track: a stretch of three-lane road with its curbs and the ground on both sides. Chasm tiles
    /// leave a gap the runner has to jump, with water or lava below. The theme decides the materials.
    /// </summary>
    public class TerrainBehaviour : TrackPiece
    {
        [Tooltip("Renderer with the road, stripes, curbs and ground submeshes, in that order.")]
        [SerializeField] internal MeshRenderer surface;
        [Tooltip("Water or lava in the gap of a chasm tile.")]
        [SerializeField] internal Renderer chasmFill;
        [Tooltip("Start and end of the gap along the tile, for chasm tiles.")]
        [SerializeField] internal Vector2 gap;

        internal TerrainCache Cache { get; set; }

        public bool HasGap => gap.y > gap.x;

        public Vector2 Gap => gap;

        public RunnerTheme Theme { get; private set; }

        public void ApplyTheme(RunnerTheme theme)
        {
            if (theme == null || theme == Theme)
            {
                return;
            }
            Theme = theme;
            if (surface != null)
            {
                Material[] materials = surface.sharedMaterials;
                Material[] themed = theme.TileMaterials;
                for (int i = 0; i < themed.Length && i < materials.Length; i++)
                {
                    materials[i] = themed[i];
                }
                surface.sharedMaterials = materials;
            }
            if (chasmFill != null && theme.chasmFill != null)
            {
                chasmFill.sharedMaterial = theme.chasmFill;
            }
        }

        internal override void ReturnToPool()
        {
            if (Cache != null)
            {
                Cache.Undeploy(this);
            }
            else
            {
                base.ReturnToPool();
            }
        }
    }
}
