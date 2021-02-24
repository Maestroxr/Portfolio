using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Playground : MonoBehaviour
{
    
    [field: SerializeField]
    public Vector3 Middle { get; private set; }
    [field: SerializeField]
    public Vector3 Margin { get; private set; }
    public Vector3 Size => playroundMesh.bounds.size;

    [SerializeField]
    private MeshRenderer playroundMesh;
}
