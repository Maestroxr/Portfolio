using System.Collections.Generic;
using Gamebox;

namespace Portfolio.EndlessRunner
{
    /// <summary>Pool of terrain tiles that also remembers the order in which the deployed tiles were laid out.</summary>
    public class TerrainCache : PrefabPool<TerrainBehaviour>
    {
        private readonly List<TerrainBehaviour> Terrains = new List<TerrainBehaviour>();


        public override TerrainBehaviour Deploy()
        {
            TerrainBehaviour newlyDeployed = base.Deploy();
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
            Terrains.Remove(item);
        }


        public override void AddDeployed(TerrainBehaviour item)
        {
            base.AddDeployed(item);
            Terrains.Add(item);
        }


        public IList<TerrainBehaviour> GetOrderedDeployedCache()
        {
            return new List<TerrainBehaviour>(Terrains);
        }
    }
}
