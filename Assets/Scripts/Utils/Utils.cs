using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public static class Util
{
    public static GridManager gridManager; //Assigned by the GridManager's Awake()

    public static Vector3 RoundVectorToInt(Vector3 vector)
    {
        return new Vector3(Mathf.CeilToInt(vector.x), Mathf.CeilToInt(vector.y), Mathf.CeilToInt(vector.z));
    }

    

    public static bool CellIsBlocked(int start_x, int start_y, int end_x, int end_y)
    {
        /*var deltaCol = System.Math.Abs(end_x - start_x);
        //If start_x < end_x, stepCol is 1, else -1
        var stepCol = start_x < end_x ? 1 : -1;

        var deltaRow = System.Math.Abs(end_y - start_y);
        var stepRow = start_y < end_y ? 1 : -1;

        //Finds out which direction to go first (largest first)
        var error = (deltaCol > deltaRow ? deltaCol : -deltaRow) / 2;
        var wallFound = false;
        while (true)
        {
            // Ignore cells that are outside of the graph.

            if (start_x >= 0
                && start_x < gridManager.gridCells.GetLength(0)
                && start_y >= 0
                && start_y < gridManager.gridCells.GetLength(1)
            )


                if (start_y == end_y && start_x == end_x)
                {
                    break;
                }

            var errorTemp = error;
            if (errorTemp > -deltaCol)
            {
                error -= deltaRow;
                start_x += stepCol;
            }

            if (errorTemp < deltaRow)
            {
                error += deltaCol;
                start_y += stepRow;
            }

            {
                Vector2 pos = new Vector2(start_x, start_y);
                var gridCell = GetCellFromPos(pos);
                if (gridCell == null)
                {
                    return true;
                }

                if (wallFound)
                {
                    wallFound = false;
                    return true;
                }

                if (gridCell.GetComponent<Cell>().isBlocker)
                {
                    wallFound = true;
                }
            }
        }*/

        return false;
    }

    /*public static GameObject GetTargetForProjectile(int start_x, int start_y, int end_x, int end_y)
    {
        var deltaCol = System.Math.Abs(end_x - start_x);
        //If start_x < end_x, stepCol is 1, else -1
        var stepCol = start_x < end_x ? 1 : -1;

        var deltaRow = System.Math.Abs(end_y - start_y);
        var stepRow = start_y < end_y ? 1 : -1;

        //Finds out which direction to go first (largest first)
        var error = (deltaCol > deltaRow ? deltaCol : -deltaRow) / 2;


        while (true)
        {
            // Ignore cells that are outside of the graph.

            if (start_x >= 0
                && start_x < gridManager.gridCells.GetLength(0)
                && start_y >= 0
                && start_y < gridManager.gridCells.GetLength(1)
            )


                if (start_y == end_y && start_x == end_x)
                {
                    break;
                }

            var errorTemp = error;
            if (errorTemp > -deltaCol)
            {
                error -= deltaRow;
                start_x += stepCol;
            }

            if (errorTemp < deltaRow)
            {
                error += deltaCol;
                start_y += stepRow;
            }

            {
                Vector2 pos = new Vector2(start_x, start_y);
                var gridCell = GetCellFromPos(pos);

                if (gridCell.GetComponent<Cell>().isBlocker)
                {
                    return gridCell;
                }
            }
        }

        return GetCellFromPos(new Vector2(end_x, end_y));
    }*/

    public static Cell GetCellOfObj(GameObject obj)
    {
        int x = (int) obj.transform.position.x;
        int y = (int) obj.transform.position.y;

        if (x < 0 || x > gridManager.gridCells.GetLength(0) - 1)
        {
            return null;
        }

        if (y < 0 || y > gridManager.gridCells.GetLength(1) - 1)
        {
            return null;
        }

        Cell onCell = gridManager.gridCells[x, y];

        return onCell;
    }
    
    /*public static List<GameObject> GetClosestCells(GameObject obj, int cellsNeeded)
    {
        List<GameObject> cellList = new List<GameObject>(0);

        //Get cells of distance i from object.
        for (int i = 1; cellList.Count + 1 < cellsNeeded; i++)
        {
            foreach (GameObject cellObj in GetCellsInRange(obj, gridManager.gridCells, i))
            {
                Cell cell = cellObj.GetComponent<Cell>();

                //Do not add the cell if the cell is blocked.
                //if the cell has loot
                //if the cell has a character
                //if the cell is the same as the unit's cell
                //if the cell is already in the list
                if (!cell.isBlocker &&
                    //!cell.loot &&
                    !cell.m_residentTower &&
                    GetCellOfObj(obj) != cell &&
                    !cellList.Contains(cellObj))
                {
                    cellList.Add(cellObj);

                    if (cellList.Count == cellsNeeded)
                    {
                        return cellList;
                    }
                }
            }
        }

        return cellList;
    }*/

    /*public static List<GameObject> GetUnitsInRange(GameObject obj, int range)
    {
        List<GameObject> residentsInRange = new List<GameObject>();

        foreach (GameObject cell in GetCellsInRange(obj, gridManager.gridCells, range))
        {
            Cell cellConfig = cell.GetComponent<Cell>();


            if (cellConfig.m_residentTower != null && cellConfig.m_residentTower != obj)
            {
                residentsInRange.Add(cellConfig.m_residentTower);
            }
        }

        return residentsInRange;
    }

    public static List<GameObject> GetInteractablesInRange(GameObject obj, int range)
    {
        List<GameObject> interactablesInRange = new List<GameObject>();
        foreach (GameObject cell in GetCellsInRange(obj, gridManager.gridCells, range))
        {
            Cell cellConfig = cell.GetComponent<Cell>();

            if (cellConfig.interactable)
            {
                interactablesInRange.Add(cellConfig.interactable);
            }
        }

        return interactablesInRange;
    }*/

    public static Cell GetCellFromPos(Vector2 pos)
    {
        int x = (int) pos.x;
        int y = (int) pos.y;
        if (x < 0 || x > gridManager.gridCells.GetLength(0) - 1)
        {
            return null;
        }

        if (y < 0 || y > gridManager.gridCells.GetLength(1) - 1)
        {
            return null;
        }

        return gridManager.gridCells[x, y];
    }

    /*public static List<GameObject> GetCellsInRange(GameObject obj, GameObject[,] grid, int range)
    {
        int row = (int) obj.transform.position.x;
        int col = (int) obj.transform.position.y;

        List<GameObject> neighbors = new List<GameObject>();
        for (int x = row - range; x <= row + range; x++)
        {
            if (x < 0 || x > grid.GetLength(0) - 1)
            {
                continue;
            }

            for (int y = col - range; y <= col + range; y++)
            {
                if (y < 0 || y > grid.GetLength(1) - 1)
                {
                    continue;
                }

                //Manhattan distance to check range from obj
                if (Mathf.Abs(row - x) + Mathf.Abs(col - y) <= range)
                {
                    if (grid[x, y] != null)
                    {
                        neighbors.Add(grid[x, y]);
                    }
                }
            }
        }

        return neighbors;
    }*/

    /*public static List<GameObject> GetCellsInView(GameObject obj, GameObject[,] grid, int range)
    {
        int row = (int) obj.transform.position.x;
        int col = (int) obj.transform.position.y;

        List<GameObject> neighbors = new List<GameObject>();
        for (int x = row - range; x <= row + range; x++)
        {
            if (x < 0 || x > grid.GetLength(0) - 1)
            {
                continue;
            }

            for (int y = col - range; y <= col + range; y++)
            {
                if (y < 0 || y > grid.GetLength(1) - 1)
                {
                    continue;
                }

                //Manhattan distance to check range from obj
                if (Mathf.Abs(row - x) + Mathf.Abs(col - y) <= range)
                {
                    if (grid[x, y] != null)
                    {
                        int start_x = (int) obj.transform.position.x;
                        int start_y = (int) obj.transform.position.y;

                        int end_x = (int) grid[x, y].transform.position.x;
                        int end_y = (int) grid[x, y].transform.position.y;

                        if (!CellIsBlocked(start_x, start_y, end_x, end_y))
                        {
                            neighbors.Add(grid[x, y]);
                        }
                    }
                }
            }
        }

        return neighbors;
    }*/

    /*public static List<GameObject> DetermineEnemies(List<GameObject> things, string myFaction)
    {
        List<GameObject> enemies = new List<GameObject>();

        for (int i = 0; i < things.Count; i++)
        {
            Attributes thingsAttributes = things[i].GetComponent<Attributes>();
            foreach (string factions in gameState.enemiesOf[myFaction])

                if (factions == thingsAttributes.faction.ToString())
                {
                    enemies.Add(things[i]);
                }
        }

        return enemies;
    }*/

    /*public static bool DetermineEnemy(GameObject thing, string myFaction)
    {
        if (thing != null)
        {
            Attributes thingAttributes = thing.GetComponent<Attributes>();
            foreach (string factions in gameState.enemiesOf[myFaction])

                if (factions == thingAttributes.faction.ToString())
                {
                    return true;
                }
        }

        return false;
    }*/

    /*public static List<GameObject> AcceptableTargets(string targets, string myFaction)
    {
        List<GameObject> acceptableTargets = new List<GameObject>();

        foreach (GameObject character in charactersState.visibleCharacters)
        {
            if (targets == "Friendly")
            {
                foreach (string faction in gameState.enemiesOf[myFaction])
                {
                    if (faction != character.GetComponent<Attributes>().faction.ToString())
                    {
                        acceptableTargets.Add(character);
                    }
                }
            }

            if (targets == "Enemies")
            {
                foreach (string faction in gameState.enemiesOf[myFaction])
                {
                    if (faction == character.GetComponent<Attributes>().faction.ToString())
                    {
                        acceptableTargets.Add(character);
                    }
                }
            }

            if (targets == "All")
            {
                acceptableTargets.Add(character);
            }
        }

        return acceptableTargets;
    }*/

    /*public static GameObject ClosestObj(List<GameObject> objList, GameObject gameObject)
    {
        List<float> distances = new List<float>();

        GameObject closestObj = null;

        foreach (GameObject obj in objList)
        {
            int distance = GetDistance(obj, gameObject);
            distances.Add(distance);

            if (distance <= distances.Min())
            {
                closestObj = obj;
            }
        }

        return closestObj;
    }*/

    /*public static GameObject FindNextCell(GameObject startCell, GameObject endCell)
    {
        //Getting a next best direction will require pathfinding (should I go up around the wall or down towards the door?)

        int startX = (int) startCell.transform.position.x;
        int startY = (int) startCell.transform.position.y;

        int endX = (int) endCell.transform.position.x;
        int endY = (int) endCell.transform.position.y;

        //Which axis do we want to move on?
        int distX = Mathf.Abs(startX - endX);
        int distY = Mathf.Abs(startY - endY);

        GameObject direction = null;

        if (distX >= distY)
        {
            //Move Horizontal
            //Which way, horizontally?
            if (startX >= endX)
            {
                //Move Left
                direction = gridManager.gridCells[startX - 1, startY];
            }
            else
            {
                //Move Right
                direction = gridManager.gridCells[startX + 1, startY];
            }
        }
        else
        {
            //Move Vertical
            if (startY >= endY)
            {
                //Move Down
                direction = gridManager.gridCells[startX, startY - 1];
            }
            else
            {
                //Move Up
                direction = gridManager.gridCells[startX, startY + 1];
            }
        }

        return direction;
    }*/

    /*public static int GetDistance(GameObject objA, GameObject objB)
    {
        int objAX = (int) objA.transform.position.x;
        int objAY = (int) objA.transform.position.y;

        int objBX = (int) objB.transform.position.x;
        int objBY = (int) objB.transform.position.y;

        int distance = Mathf.Abs(objAX - objBX) + Mathf.Abs(objAY - objBY);
        return distance;
    }*/
    
 }