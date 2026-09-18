using System.Collections.Generic;
using UnityEngine;

namespace Portfolio.EndlessRunner
{
    public class TerrainBehaviour : MonoBehaviour
    {
        public RunnerGameManager GameManager { private get; set; }
        public List<Collidable> Collidables { get; private set; }
        public Vector3 CenterTerrainOffset { get; private set; }


        void Awake()
        {
            var terrain = GetComponent<Terrain>();
            Vector3 center = terrain != null ? terrain.terrainData.size / 2 : Vector3.zero;
            center.y = 1;
            CenterTerrainOffset = center;
            Collidables = new List<Collidable>();
        }


        public void PrepareCollidables(ICollection<Collidable> collidables, float CollidablesRadius)
        {
            Collidables.Clear();

            foreach (Collidable collidable in collidables)
            {
                Vector3 pos = transform.position + CenterTerrainOffset;
                pos.x += Random.Range(-CollidablesRadius, CollidablesRadius);
                pos.z += Random.Range(-CollidablesRadius, CollidablesRadius);
                collidable.transform.position = pos;
                collidable.GameManager = GameManager;
                collidable.BelongedTerrain = this;
            }

            Collidables.AddRange(collidables);
        }
    }
}
