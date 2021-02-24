using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ICyclic<T> where T : MonoBehaviour
{
    T Mirror { get;  }
    bool IsMirror { get; }
    void Setup(T mirror, bool isMirror);
}
