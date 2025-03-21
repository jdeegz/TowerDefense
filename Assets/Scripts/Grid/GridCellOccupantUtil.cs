using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridCellOccupantUtil
{
    
    public static void SetOccupant(GameObject obj, bool isOccupied, int width, int height, ResourceNode resourceNode = null)
    {
        //Get the bottom left cell.
        Vector3 bottomLeftCell = obj.transform.position;
        bottomLeftCell = Util.RoundVectorToInt(bottomLeftCell);
        bottomLeftCell.x -= width / 2;
        bottomLeftCell.z -= height / 2;

        //Set all the cells to 'isOccupied' state
        for (int x = 0; x < width; ++x)
        {
            for (int z = 0; z < height; ++z)
            {
                Vector2Int pos = new Vector2Int((int)bottomLeftCell.x + x, (int)bottomLeftCell.z + z);
                Cell cell = Util.GetCellFromPos(pos);
                
                if(cell == null) Debug.Log($"Object {obj.name} at {pos} Is not on a valid Cell.");
                
                if (cell.m_isOccupied && isOccupied)
                {
                    if (pos.x != 0 && pos.x != GridManager.Instance.m_gridWidth - 1 && pos.y != 0 && pos.y != GridManager.Instance.m_gridHeight - 1)
                    {
                        obj.SetActive(false);
                    }
                }

                cell.UpdateOccupancy(isOccupied);

                Debug.Log($"Set Occupant: {obj.name} at {cell.m_cellPos} occupies: {isOccupied}.");

                cell.m_occupant = isOccupied ? obj : null;
                cell.m_cellResourceNode = isOccupied ? resourceNode : null;
                
                
                if (GameplayManager.Instance.m_gameplayState != GameplayManager.GameplayState.PlaceObstacles)
                {
                    GridManager.Instance.RefreshGrid();
                }
            }
        }
    }

    public static void SetActor(GameObject obj, int i, int width, int height)
    {
        //Get the bottom left cell.
        Vector3 m_bottomLeftCell = obj.transform.position;
        m_bottomLeftCell = Util.RoundVectorToInt(m_bottomLeftCell);
        m_bottomLeftCell.x -= width / 2;
        m_bottomLeftCell.z -= height / 2;

        //Set all the cells to 'isOccupied' state
        for (int x = 0; x < width; ++x)
        {
            for (int z = 0; z < height; ++z)
            {
                Vector2Int pos = new Vector2Int((int)m_bottomLeftCell.x + x, (int)m_bottomLeftCell.z + z);
                Cell cell = Util.GetCellFromPos(pos);
                cell.UpdateActorCount(i, obj.name);
            }
        }
    }

    public static void SetActor(GameObject obj, int i, Cell cell)
    {
        cell.UpdateActorCount(i, obj.name);
    }

    public static void SetBuildRestricted(GameObject obj, bool value, int width, int height)
    {
        //Get the bottom left cell.
        Vector3 m_bottomLeftCell = obj.transform.position;
        m_bottomLeftCell = Util.RoundVectorToInt(m_bottomLeftCell);
        m_bottomLeftCell.x -= width / 2;
        m_bottomLeftCell.z -= height / 2;

        //Set all the cells to 'isOccupied' state
        for (int x = 0; x < width; ++x)
        {
            for (int z = 0; z < height; ++z)
            {
                Vector2Int pos = new Vector2Int((int)m_bottomLeftCell.x + x, (int)m_bottomLeftCell.z + z);
                Cell cell = Util.GetCellFromPos(pos);
                cell.UpdateBuildRestrictedValue(value);
            }
        }
    }

    public static void SetPortalConnectionCell(GameObject portalEntranceObj, GameObject portalExitObj)
    {
        //Get the cell we need to update
        Cell cellEntrance = Util.GetCellFrom3DPos(portalEntranceObj.transform.position);
        Cell cellExit = Util.GetCellFrom3DPos(portalExitObj.transform.position);

        Debug.Log($"{cellEntrance.m_cellPos} connected to {cellExit.m_cellPos} by a portal!");
        
        cellEntrance.m_additionalNeighbors.Add(cellExit);
        SetBuildRestricted(portalEntranceObj, true, 1, 1);
        
        
        cellExit.m_additionalNeighbors.Add(cellEntrance);
        SetBuildRestricted(portalExitObj, true, 1, 1);
    }
}
