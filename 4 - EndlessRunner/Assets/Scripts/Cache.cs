using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Cache<T> : MonoBehaviour, ICache<T> where T : MonoBehaviour
{
    public int MaxSize;
    public GameObject Instance;
    public Transform DeployedParent;
    public Transform UndeployedParent;
    private HashSet<T> deployed = new HashSet<T>();
    private HashSet<T> undeployed = new HashSet<T>();
    private T InstanceScript;


    protected virtual void Awake()
    {
        if (null == DeployedParent)
        {
            DeployedParent = new GameObject().transform;
            DeployedParent.SetParent(transform);
            DeployedParent.name = $"{name} deployed";
        }
        if (null == UndeployedParent)
        {
            UndeployedParent = new GameObject().transform;
            UndeployedParent.SetParent(transform);
            UndeployedParent.name = $"{name} undeployed";
        }
        InstanceScript = Instance.GetComponent<T>();
        if (null == InstanceScript)
        {
            throw new CacheException($"Instance GameObject does not have script of type {typeof(T).Name}");
        }
    }
    

    /// <summary>
    /// Counts both deployed and undeployed instances in cache
    /// </summary>
    /// <returns></returns>
    public virtual int Count()
    {
        return deployed.Count + undeployed.Count;
    }


    /// <summary>
    /// Creates a single cached object 
    /// </summary>
    /// <param name="amount"></param>
    /// <returns></returns>
    public virtual T Create()
    {
        var newItems = new List<T>();
        if (MaxSize < Count() + 1)
        {
            throw new CacheException($"Max cache size {MaxSize} reached.");
        }

        T script = Instantiate(InstanceScript);
        deployed.Add(script);
        newItems.Add(script);
        script.transform.SetParent(DeployedParent);

        return script;
    }


    /// <summary>
    /// Creates the cached objects 
    /// </summary>
    /// <param name="amount"></param>
    /// <returns></returns>
    public virtual ICollection<T> Create(int amount)
    {
        var newItems = new List<T>();
        if (MaxSize < Count() + amount)
        {
            throw new CacheException($"Max cache size {MaxSize} reached.");
        }
        for (int i = 0; i < amount; i++)
        {
            T script = Instantiate(InstanceScript);
            deployed.Add(script);
            newItems.Add(script);
            script.transform.SetParent(DeployedParent);
        }
        return newItems;
    }


    /// <summary>
    /// Removes the object from cache and destroys it
    /// </summary>
    /// <param name="item"></param>
    public virtual void Remove(T item)
    {
        deployed.Remove(item);
        undeployed.Remove(item);
        Destroy(item);
    }


    /// <summary>
    /// Deploys a single cached object
    /// </summary>
    /// <returns></returns>
    public virtual T Deploy()
    {
        T newlyDeployed;
        if (undeployed.Count == 0)
        {
            newlyDeployed = Create();
        }
        else
        {
            var iter = undeployed.GetEnumerator();
            if (iter.MoveNext())
            {
                newlyDeployed = iter.Current;
                undeployed.Remove(newlyDeployed);
            }
            else
            {
                throw new CacheException("Cache did not deploy objects as expected.");
            }
        }

        deployed.Add(newlyDeployed);
        newlyDeployed.gameObject.SetActive(true);
        newlyDeployed.transform.SetParent(DeployedParent);

        return newlyDeployed;
    }


    /// <summary>
    /// Deploys already cached objects
    /// </summary>
    /// <param name="amount"></param>
    /// <returns></returns>
    public virtual ICollection<T> Deploy(int amount)
    {
        var deployedObjects = new List<T>();
        if (undeployed.Count < amount)
        {
            ICollection<T> cachedList = Create(amount - undeployed.Count);
            deployedObjects.AddRange(cachedList);
            amount -= cachedList.Count;
        }

        deployedObjects.AddRange(undeployed.ToList().GetRange(0, amount));
        foreach (T item in deployedObjects)
        {
            deployed.Add(item);
            item.gameObject.SetActive(true);
            item.transform.SetParent(DeployedParent);
        }
        undeployed.ExceptWith(deployedObjects);
        return deployedObjects;
    }


    /// <summary>
    /// Puts cached objects back in the cach to be redeployed
    /// </summary>
    /// <param name="item">The game object instance to be made inactive and put back in the cache</param>
    public virtual void Undeploy(T item)
    {
        item.gameObject.SetActive(false);
        item.transform.SetParent(UndeployedParent);
        deployed.Remove(item);
        undeployed.Add(item);
    }

    /// <summary>
    /// Adds an already deployed item
    /// </summary>
    /// <param name="item"></param>
    public virtual void AddDeployed(T item)
    {
        deployed.Add(item);
        item.transform.SetParent(DeployedParent);
    }
}


public class CacheException : Exception
{
    public CacheException(string error) : base(error) { }
}