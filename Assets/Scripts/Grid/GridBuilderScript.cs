using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class GridBuilder : MonoBehaviour
{
    public int gridWidth;
    public int gridHeight;
    public GameObject cellObject;
    public GameObject gridParent;


    //Build a GridCell(public class) of gameobject at position x,y. Put GridCell into a list.
    public void BuildGrid()
    {
        gridParent = GameObject.FindGameObjectWithTag("Grid");
        GameObject roomParent = new GameObject("Room");
        roomParent.transform.parent = gridParent.gameObject.transform;
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                BuildGridCell(x, y, roomParent);
            }
        }
    }

    private GameObject BuildGridCell(int x, int y, GameObject parent)
    {
        Vector3 pos = new Vector3(x, 0, y);
        GameObject newGridCell = Instantiate(cellObject, pos, Quaternion.identity);
        newGridCell.transform.parent = parent.gameObject.transform;
        newGridCell.name = x + "," + y;
        //This object ends up being saved out as a GridCell, and added to the List.
        return newGridCell;
    }
}