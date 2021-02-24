using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Portfolio
{
    /// <summary>
    /// IStorage interface abstracts the addition, removal and choice of storage strategies.
    /// </summary>
    public interface IStorage
    {
        IStorageStrategy SelectedStorage { get; }
        void ChooseStrategy(string name);
        void AddStrategy(IStorageStrategy strategy);
        void RemoveStrategy(string name);
        IEnumerable<IStorageStrategy> GetStrategies();
    }
}