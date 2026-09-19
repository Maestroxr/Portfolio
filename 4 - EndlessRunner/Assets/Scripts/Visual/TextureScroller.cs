using UnityEngine;

namespace Portfolio.EndlessRunner
{
    /// <summary>Scrolls the base texture of a shared material (flowing water and lava) through a property block.</summary>
    [RequireComponent(typeof(Renderer))]
    public class TextureScroller : MonoBehaviour
    {
        private static readonly int BaseMapStId = Shader.PropertyToID("_BaseMap_ST");

        [SerializeField] internal Vector2 speed = new Vector2(0.05f, 0.02f);
        [SerializeField] internal Vector2 tiling = new Vector2(1f, 1f);

        private Renderer target;
        private MaterialPropertyBlock block;

        private void Awake()
        {
            target = GetComponent<Renderer>();
            block = new MaterialPropertyBlock();
        }

        private void Update()
        {
            Vector2 offset = speed * Time.time;
            target.GetPropertyBlock(block);
            block.SetVector(BaseMapStId, new Vector4(tiling.x, tiling.y, offset.x % 1f, offset.y % 1f));
            target.SetPropertyBlock(block);
        }
    }
}
