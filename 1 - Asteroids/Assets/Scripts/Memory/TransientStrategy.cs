using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Portfolio
{
    /// <summary>
    /// This strategy is not meant to be persisted between application sessions,
    /// and as such it's persist implementation does not actually persist the information.
    /// </summary>
    public class TransientStrategy : IStorageStrategy
    {
        private Dictionary<string, int> IntMap = new Dictionary<string, int>();
        private Dictionary<string, float> FloatMap = new Dictionary<string, float>();
        private Dictionary<string, string> StringMap = new Dictionary<string, string>();
        private Dictionary<string, bool> BoolMap = new Dictionary<string, bool>();


        public int GetInt(string key)
        {
            return IntMap[key];
        }


        public float GetFloat(string key)
        {
            return FloatMap[key];
        }


        public string GetString(string key)
        {
            return StringMap[key];
        }


        public bool GetBool(string key)
        {
            return BoolMap[key];
        }


        public void SetInt(string key, int value)
        {
            IntMap[key] = value;
        }


        public void SetFloat(string key, float value)
        {
            FloatMap[key] = value;
        }


        public void SetString(string key, string value)
        {
            StringMap[key] = value;
        }


        public void SetBool(string key, bool value)
        {
            BoolMap[key] = value;
        }


        /// <summary>
        /// Two approaches can be used here, to signify the transient memory does not 
        /// persist the data. One can be silent fail which means having an empty function body and 
        /// have it do nothing. The other is having it throw NotImplementedException, but then code
        /// using the API would have to deal with it in every use.
        /// I chose the latter approach as it does not fail silently.
        /// </summary>
        public void Persist()
        {
            throw new System.NotImplementedException("Transient storage doesn't support persistence.");
        }


        public void DeleteByKey(string key)
        {
            IntMap.Remove(key);
            FloatMap.Remove(key);
            StringMap.Remove(key);
            BoolMap.Remove(key);
        }


        public void DeleteAll()
        {
            IntMap.Clear();
            FloatMap.Clear();
            StringMap.Clear();
            BoolMap.Clear();
        }

       
        public bool DoesKeyExist(string key)
        {
            return IntMap.ContainsKey(key) || FloatMap.ContainsKey(key) || StringMap.ContainsKey(key) || BoolMap.ContainsKey(key);
        }


        public string Name()
        {
            return StorageStrategies.Transient.ToString();
        }


        public void Mount()
        {
            
        }


        public void Unmount()
        {
            
        }
    }
}
