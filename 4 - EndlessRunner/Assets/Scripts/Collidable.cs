using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Collidable : MonoBehaviour
{
    public Manager GameManager { private get; set; }
    public TerrainBehaviour BelongedTerrain { get; set; }


    private void OnTriggerEnter(Collider other)
    {
        GameManager.ObjectCollided(this);

        BelongedTerrain.Collidables.Remove(this);
    }
}
