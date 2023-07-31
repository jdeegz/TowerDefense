using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    [HideInInspector] public Cell[,] gridCells;

    void Awake()
    {
        GameObject[] allCells = GameObject.FindGameObjectsWithTag("Cell");

        int x = 0;
        int z = 0;

        foreach (GameObject cell in allCells)
        {

            if (cell.transform.position.x >= x)
            {
                x = (int)cell.transform.position.x;
            }

            if (cell.transform.position.z >= z)
            {
                z = (int)cell.transform.position.z;
            }
        }

        gridCells = new Cell[x + 1, z + 1];

        foreach (GameObject cell in allCells)
        {
            int cellPosX = (int)cell.transform.position.x;
            int cellPosZ = (int)cell.transform.position.z;

            if (!gridCells[cellPosX, cellPosZ])
            {
                gridCells[cellPosX, cellPosZ] = cell.GetComponent<Cell>();
            }
            else
            {
                Debug.Log("Duplicate cell at " + cellPosX + ", " + cellPosZ);
            }
        }

        Util.gridManager = gameObject.GetComponent<GridManager>();
    }
}