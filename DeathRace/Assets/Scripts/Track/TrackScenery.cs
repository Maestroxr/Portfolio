using System.Collections.Generic;
using UnityEngine;

namespace Portfolio.DeathRace
{
    /// <summary>
    /// The root of one dressing of a track (everything the eye sees of it). It owns the meshes the builder made for it
    /// and frees them when it goes, so restyling a track or building one per race never piles up meshes.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class TrackScenery : MonoBehaviour
    {
        private readonly List<Object> owned = new List<Object>();

        internal T Own<T>(T asset) where T : Object
        {
            owned.Add(asset);
            return asset;
        }

        private void OnDestroy()
        {
            foreach (Object asset in owned)
            {
                TrackBuilder.Discard(asset);
            }
            owned.Clear();
        }
    }
}
