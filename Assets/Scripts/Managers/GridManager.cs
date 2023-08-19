using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using UnityEngine;
using UnityEngine.Serialization;

public class GridManager : MonoBehaviour
{
    public static GridManager Instance;
    public Cell[] m_gridCells;
    public int m_gridWidth = 10;
    public int m_gridHeight = 10;

    void Awake()
    {
        Instance = this;
        BuildGrid();
    }

    void BuildGrid()
    {
        m_gridCells = new Cell[m_gridWidth * m_gridHeight];
        
        
        for (int x = 0; x < m_gridWidth; ++x)
        {
            for (int z = 0; z < m_gridHeight; ++z)
            {
                int index = x * m_gridWidth + z;
                m_gridCells[index] = new Cell();
                //Debug.Log("New Cell created at: " + index);
            }
        }
    }
}
[System.Serializable]
public class Cell
{
    public bool m_isOccupied;
    public int m_actorCount;

    public void UpdateActorCount(int i)
    {
        m_actorCount += i;
    }

    public void UpdateOccupancy(bool b)
    {
        m_isOccupied = b;
    }
}