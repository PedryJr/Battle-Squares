using UnityEngine;
using NavMeshPlus;
using NavMeshPlus.Components;
using System.Collections;

public class NavMeshBaker : MonoBehaviour
{

    public static NavMeshBaker instance;

    public NavMeshSurface navMeshSurface;

    private void Awake()
    {
        navMeshSurface = GetComponent<NavMeshSurface>();
        instance = this;
    }


    [ContextMenu("Bake Arena")]
    public void BakeArena()
    {
        if(!navMeshSurface) navMeshSurface = GetComponent<NavMeshSurface>();
        navMeshSurface.BuildNavMesh();
    }

}
