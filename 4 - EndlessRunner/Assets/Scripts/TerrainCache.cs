using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainCache : Cache<TerrainBehaviour>
{
    List<TerrainBehaviour> Terrains = new List<TerrainBehaviour>();


    public override TerrainBehaviour Deploy()
    {
        TerrainBehaviour newlyDeployed =  base.Deploy();
        Terrains.Add(newlyDeployed);
        return newlyDeployed;
    }


    public override ICollection<TerrainBehaviour> Deploy(int amount)
    {
        IList<TerrainBehaviour> terrains = new List<TerrainBehaviour>(base.Deploy(amount));
        Terrains.AddRange(terrains);
        return terrains;
    }


    public override void Undeploy(TerrainBehaviour item)
    {
        base.Undeploy(item);
        TerrainBehaviour terrain = item.GetComponent<TerrainBehaviour>();
        Terrains.Remove(terrain);
    }


    public override void AddDeployed(TerrainBehaviour item)
    {
        base.AddDeployed(item);
        Terrains.Add(item.GetComponent<TerrainBehaviour>());
    }


    public IList<TerrainBehaviour> GetOrderedDeployedCache()
    {
        return new List<TerrainBehaviour>(Terrains);
    }
}
