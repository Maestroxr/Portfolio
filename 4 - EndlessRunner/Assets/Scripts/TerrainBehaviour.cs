using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainBehaviour : MonoBehaviour
{
    public Manager GameManager { private get;  set; }
    public List<Collidable> Collidables { get; private set; }
    public Vector3 CenterTerrainOffset { get; private set; }


    void Awake()
    {
        var terrain = GetComponent<Terrain>();
        Vector3 center = terrain.terrainData.size / 2;
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
