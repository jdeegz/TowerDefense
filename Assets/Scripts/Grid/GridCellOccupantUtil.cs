using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridCellOccupantUtil
{
    public static void SetOccupant(GameObject obj, bool isOccupied, int width, int height)
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
                Debug.Log("Grid Cell occupied: " + pos);
                Util.GetCellFromPos(pos).m_isOccupied = isOccupied;
            }
        }
    }
}