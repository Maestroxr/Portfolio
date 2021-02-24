using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Portfolio
{
    /// <summary>
    /// Decoupler for MonoBehaviours to pure C# classes.
    /// </summary>
    public interface ILocalTransformAdapter
    {
        Vector3 LocalPosition { get; set; }
        Quaternion LocalRotation { get; set; }
        Vector3 Forward { get; }
    }
}