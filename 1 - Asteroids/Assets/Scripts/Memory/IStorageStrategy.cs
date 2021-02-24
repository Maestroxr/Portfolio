using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Portfolio
{
    /// <summary>
    /// API that provides abstract storage interface.
    /// </summary>
    public interface IStorageStrategy
    {
        int GetInt(string key);
        float GetFloat(string key);
        string GetString(string key);
        bool GetBool(string key);
        void SetInt(string key, int value);
        void SetFloat(string key, float value);
        void SetString(string key, string value);
        void SetBool(string key, bool value);
        /// <summary>
        /// Persists all changes that have been made in session.
        /// </summary>
        void Persist();
        void DeleteByKey(string key);
        void DeleteAll();
        bool DoesKeyExist(string key);
        /// <summary>
        /// Unique name for storage solution. Used for storage lookup and selection.
        /// </summary>
        string Name();
        /// <summary>
        /// Should be called righrt before selecting the strategy for use.
        /// </summary>
        void Mount();
        /// <summary>
        /// Should be called right after another strategy is selected for use.
        /// </summary>
        void Unmount();
    }
}