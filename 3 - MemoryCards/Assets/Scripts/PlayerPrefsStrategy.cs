using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Portfolio
{
    /// <summary>
    /// This strategy uses Unity's PlayerPrefs storage solution.
    /// </summary>
    public class PlayerPrefsStrategy : IStorageStrategy
    {
        public int GetInt(string key)
        {
            return PlayerPrefs.GetInt(key);
        }


        public float GetFloat(string key)
        {
            return PlayerPrefs.GetFloat(key);
        }


        public string GetString(string key)
        {
            return PlayerPrefs.GetString(key);
        }


        public bool GetBool(string key)
        {
            return Convert.ToBoolean(PlayerPrefs.GetInt(key));
        }


        public void SetInt(string key, int value)
        {
            PlayerPrefs.SetInt(key, value);
        }


        public void SetFloat(string key, float value)
        {
            PlayerPrefs.SetFloat(key, value);
        }


        public void SetString(string key, string value)
        {
            PlayerPrefs.SetString(key, value);
        }


        public void SetBool(string key, bool value)
        {
            PlayerPrefs.SetInt(key, Convert.ToInt32(value));

        }


        public void Persist()
        {
            PlayerPrefs.Save();
        }


        public void DeleteByKey(string key)
        {
            PlayerPrefs.DeleteKey(key);
        }


        public void DeleteAll()
        {
            PlayerPrefs.DeleteAll();
        }
        

        public bool DoesKeyExist(string key)
        {
            return PlayerPrefs.HasKey(key);
        }


        public string Name()
        {
            return StorageStrategies.PlayerPrefs.ToString();
        }


        public void Mount()
        {

        }


        public void Unmount()
        {

        }
    }
}