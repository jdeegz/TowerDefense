using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class GridProp : MonoBehaviour
{
    private Cell m_cell;
    private float m_scaleX = 1;
    private float m_scaleZ = 1;

    // Bool to say this cell is completely blocked. (Stones for example)
    public bool m_isCellBlocker;

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
            //Setup Cell Occupancy info.
            Vector2Int cellPos = Util.GetVector2IntFrom3DPos(transform.position);
            m_cell = Util.GetCellFromPos(cellPos);

            if (m_isCellBlocker)
            {
                GridCellOccupantUtil.SetOccupant(gameObject, true, 1, 1);
                return;
            }

            //m_cell.UpdateOccupancy(false);

            m_cell.UpdateBuildRestrictedValue(!m_isBuildable);

            //Setup Walls
            m_cell.m_canPathNorth = m_canPathNorth;
            m_cell.m_canPathEast = m_canPathEast;
            m_cell.m_canPathSouth = m_canPathSouth;
            m_cell.m_canPathWest = m_canPathWest;

            //If we cannot path to the next cell, get the next cell and set it's opposite wall up.
            if (!m_canPathNorth)
            {
                Cell northCell = Util.GetCellFromPos(new Vector2Int(cellPos.x, cellPos.y + 1));
                if (northCell != null) northCell.m_canPathSouth = false;
            }

            if (!m_canPathEast)
            {
                Cell eastCell = Util.GetCellFromPos(new Vector2Int(cellPos.x + 1, cellPos.y));
                if (eastCell != null) eastCell.m_canPathWest = false;
            }

            if (!m_canPathSouth)
            {
                Cell southCell = Util.GetCellFromPos(new Vector2Int(cellPos.x, cellPos.y - 1));
                if (southCell != null) southCell.m_canPathNorth = false;
            }

            if (!m_canPathWest)
            {
                Cell westCell = Util.GetCellFromPos(new Vector2Int(cellPos.x - 1, cellPos.y));
                if (westCell != null) westCell.m_canPathEast = false;
            }
        }
    }
}