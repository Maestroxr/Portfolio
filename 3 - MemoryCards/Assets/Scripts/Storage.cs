using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Portfolio
{
    /// <summary>
    /// Allows easy decoupled replacement of storage strategy.
    /// </summary>
    public class Storage : MonoBehaviour, IStorage
    {
        public IStorageStrategy SelectedStorage { get; private set; }
        [SerializeField] private StorageStrategies DefaultStorageStrategy;
        private Dictionary<string, IStorageStrategy> Strategies = new Dictionary<string, IStorageStrategy>();


        public void AddStrategy(IStorageStrategy strategy)
        {
            Strategies[strategy.Name()] = strategy;
        }


        public void ChooseStrategy(string name)
        {
            if (null != SelectedStorage)
            {
                SelectedStorage.Unmount();
            }
            SelectedStorage = Strategies[name];
            SelectedStorage.Mount();
        }

        
        public IEnumerable<IStorageStrategy> GetStrategies()
        {
            return Strategies.Values;
        }


        public void RemoveStrategy(string name)
        {
            Strategies.Remove(name);
        }

       
        void Awake()
        {
            AddStrategy(new PlayerPrefsStrategy());
            AddStrategy(new TransientStrategy());
            ChooseStrategy(DefaultStorageStrategy.ToString());
        }
    }
}