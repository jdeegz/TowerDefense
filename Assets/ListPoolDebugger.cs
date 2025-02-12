using System;
using System.Collections.Generic;
using UnityEngine;

public class ListPoolDebugger : MonoBehaviour
{
    /*void Update()
    {
        if (Input.GetKeyDown(KeyCode.Z))
        {
            LogAllListPools();
        }    
    }*/
    
    private void Awake()
    {
        //Debug.Log("=== List Pool Debug Log on Mission Start ===");
        LogAllListPools();
    }
    
    private void OnDestroy()
    {
        LogAllListPools();
    }

    public void LogAllListPools()
    {
        Dictionary<Type, int> poolStates = ListPool<object>.GetPoolStates(); // Generic object type to access static dictionary

        //Debug.Log("=== List Pool Debug Log ===");
        ListPool<EnemyController>.DebugPoolContents();
        ListPool<Vector2Int>.DebugPoolContents();
        ListPool<GameObject>.DebugPoolContents();
        ListPool<Cell>.DebugPoolContents();
        
        
        foreach (var entry in poolStates)
        {
            //Debug.Log($"ListPool<{entry.Key.Name}> - Pool Size: {entry.Value}");
        }
        //Debug.Log("===========================");
    }
}