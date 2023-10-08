using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class GridProp : MonoBehaviour
{
    private Cell m_cell;
    
    [Header("Define Prop Scale in Grid Cells")]
    public float m_scaleX;
    public float m_scaleZ;

    [Header("Define Exit Directions")]
    public bool m_northExit;
    public bool m_eastExit;
    public bool m_southExit;
    public bool m_westExit;

    void Awake()
    {
        GameplayManager.OnGameplayStateChanged += GameplayManagerStateChanged;
    }

    void OnDestroy()
    {
        GameplayManager.OnGameplayStateChanged -= GameplayManagerStateChanged;
    }

    void GameplayManagerStateChanged(GameplayManager.GameplayState newState)
    {
        if (newState == GameplayManager.GameplayState.PlaceObstacles)
        {
            //Area Of Object
            int northExit = m_northExit ? 1 : 0;
            int eastExit = m_eastExit ? 1 : 0;
            int southExit = m_southExit ? 1 : 0;
            int westExit = m_westExit ? 1 : 0;
            for (int x = (int)transform.position.x - westExit; x < transform.position.x + m_scaleX + eastExit; ++x)
            {
                for (int z = (int)transform.position.z - southExit; z < transform.position.z + m_scaleZ + northExit; ++z)
                {
                    m_cell = Util.GetCellFromPos(new Vector2Int(x, z));
                    m_cell.UpdateActorCount(1, gameObject.name);
                    m_cell.m_isOccupied = false;
                }
            }
        }
    }
}