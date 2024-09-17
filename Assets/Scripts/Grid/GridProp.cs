using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class GridProp : MonoBehaviour
{
    private Cell m_cell;
    private float m_scaleX = 1;
    private float m_scaleZ = 1;

    //Bool to allow us to make buildable, but walled cells.
    public bool m_isBuildable;
    
    [Header("Define Unobstructable Exit Directions")]
    public bool m_northExit;
    public bool m_eastExit;
    public bool m_southExit;
    public bool m_westExit;

    [Header("Define Walls")] 
    public bool m_canPathNorth = true;
    public bool m_canPathEast = true;
    public bool m_canPathSouth = true;
    public bool m_canPathWest = true;
    
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
            /*//Set up Unobstractable exits.
            int northExit = m_northExit ? 1 : 0;
            int eastExit = m_eastExit ? 1 : 0;
            int southExit = m_southExit ? 1 : 0;
            int westExit = m_westExit ? 1 : 0;
            for (int x = (int)transform.position.x - westExit; x < transform.position.x + m_scaleX + eastExit; ++x)
            {
                for (int z = (int)transform.position.z - southExit; z < transform.position.z + m_scaleZ + northExit; ++z)
                {
                    m_cell = Util.GetCellFromPos(new Vector2Int(x, z));
                    if(!m_isBuildable) m_cell.UpdateActorCount(1, gameObject.name);
                    m_cell.UpdateOccupancy(false);
                }
            }*/
            
            //Setup Cell Occupancy info.
            Vector2Int cellPos = Util.GetVector2IntFrom3DPos(transform.position);
            m_cell = Util.GetCellFromPos(cellPos);
            m_cell.UpdateBuildRestrictedValue(!m_isBuildable);
            m_cell.UpdateOccupancyDisplay(false);
            
            //Setup Walls
            m_cell.m_canPathNorth = m_canPathNorth;
            m_cell.m_canPathEast = m_canPathEast;
            m_cell.m_canPathSouth = m_canPathSouth;
            m_cell.m_canPathWest = m_canPathWest;
            
            //If we cannot path to the next cell, get the next cell and set it's opposite wall up.
            if (!m_canPathNorth) Util.GetCellFromPos(new Vector2Int(cellPos.x, cellPos.y + 1)).m_canPathSouth = false;
            if (!m_canPathEast) Util.GetCellFromPos(new Vector2Int(cellPos.x + 1, cellPos.y)).m_canPathWest = false;
            if (!m_canPathSouth) Util.GetCellFromPos(new Vector2Int(cellPos.x, cellPos.y - 1)).m_canPathNorth = false;
            if (!m_canPathWest) Util.GetCellFromPos(new Vector2Int(cellPos.x - 1, cellPos.y)).m_canPathEast = false;
        }
    }
}