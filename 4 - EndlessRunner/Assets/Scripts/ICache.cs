using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Interface used for caching objects
/// </summary>
/// <typeparam name="T">The script type expected to be on cached GameObjects</typeparam>
public interface ICache<T> where T : MonoBehaviour
{
    T Create();

    ICollection<T> Create(int amount);

    void Remove(T instance);

    T Deploy();

    ICollection<T> Deploy(int amount); 

    void Undeploy(T instance);

    void AddDeployed(T gameObject);

}
