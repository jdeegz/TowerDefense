using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridCellOccupantUtil
{
    
    public static void SetOccupant(GameObject obj, bool isOccupied, int width, int height, ResourceNode resourceNode = null)
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
                
                if (cell.m_isOccupied && isOccupied)
                {
                    if (pos.x != 0 && pos.x != GridManager.Instance.m_gridWidth - 1 && pos.y != 0 && pos.y != GridManager.Instance.m_gridHeight - 1)
                    {
                        Debug.Log($"Cell is being double occupied at {pos} by First:{cell.m_occupant.name} Second:{obj.name}!");
                        obj.SetActive(false);
                    }
                }

                cell.UpdateOccupancyDisplay(isOccupied);

                cell.m_occupant = isOccupied ? obj : null;
                cell.m_cellResourceNode = isOccupied ? resourceNode : null;
                
                //Handle Tile Map update
                if (resourceNode != null)
                {
                    GridManager.Instance.ToggleTileMap(new Vector3Int(cell.m_cellPos.x, cell.m_cellPos.y, 0), isOccupied);
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
}
