using UnityEngine;

namespace Portfolio.EndlessRunner
{
    public class Collidable : MonoBehaviour
    {
        public RunnerGameManager GameManager { private get; set; }
        public TerrainBehaviour BelongedTerrain { get; set; }


        private void OnTriggerEnter(Collider other)
        {
            if (GameManager != null)
            {
                GameManager.ObjectCollided(this);
            }
            if (BelongedTerrain != null)
            {
                BelongedTerrain.Collidables.Remove(this);
            }
        }
    }
}
