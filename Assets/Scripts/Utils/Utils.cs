using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public static class Util
{
    public static Vector2Int[] m_directions = new Vector2Int[]
    {
        new Vector2Int(-1, -1), // Top-left
        new Vector2Int(0, -1), // Top
        new Vector2Int(1, -1), // Top-right
        new Vector2Int(-1, 0), // Left
        new Vector2Int(1, 0), // Right
        new Vector2Int(-1, 1), // Bottom-left
        new Vector2Int(0, 1), // Bottom
        new Vector2Int(1, 1) // Bottom-right
    };

    public static List<Vector2Int> GetBoxAround3x3Grid(Vector2Int center)
    {
        List<Vector2Int> result = new List<Vector2Int>();

        // Loop through the 5x5 grid centered around the target
        for (int x = -2; x <= 2; x++)
        {
            for (int z = -2; z <= 2; z++)
            {
                // Get the current grid cell
                Vector2Int currentCell = new Vector2Int(center.x + x, center.y + z);

                // Skip the inner 3x3 grid (which would be from -1 to 1 on both axes)
                if (Mathf.Abs(x) <= 1 && Mathf.Abs(z) <= 1)
                    continue;

                // Add outer cells to the list
                result.Add(currentCell);
            }
        }

        return result;
    }

    public static List<Cell> GetEmptyCellsAtDistance(Vector2Int center, int distance)
    {
        List<Cell> emptyCells = new List<Cell>();

        // Loop through the grid centered around the target
        for (int x = -distance; x <= distance; x++)
        {
            for (int z = -distance; z <= distance; z++)
            {
                // Get the current grid cell
                Vector2Int currentPos = new Vector2Int(center.x + x, center.y + z);

                // Skip the inner grid
                if (Mathf.Abs(x) < distance && Mathf.Abs(z) < distance) continue;

                // Add outer cells to the list
                Cell cell = GetCellFromPos(currentPos);
                if (!cell.m_isOccupied)
                {
                    emptyCells.Add(cell);
                }
            }
        }

        return emptyCells;
    }

    public static Vector3 GetRandomPosition(Vector3 objPosition, Vector3 offset)
    {
        //Position
        objPosition += Vector3.Lerp(-offset, offset, UnityEngine.Random.value);
        return objPosition;
    }

    public static Quaternion GetRandomRotation(Quaternion objRotation, Vector3 offset)
    {
        //Rotation
        objRotation *= Quaternion.Euler(Vector3.Lerp(-offset, offset, UnityEngine.Random.value));
        return objRotation;
    }

    public static Vector3 GetRandomScale(Vector3 objScale, Vector3 offset)
    {
        //Scale
        objScale += Vector3.Lerp(-offset, offset, UnityEngine.Random.value);
        return objScale;
    }

    public static Vector3 RoundVectorToInt(Vector3 vector)
    {
        return new Vector3(Mathf.FloorToInt(vector.x + 0.5f), Mathf.FloorToInt(vector.y), Mathf.FloorToInt(vector.z + 0.5f));
    }

    /*
    public static List<Cell> GetCellsInRadius(int centerX, int centerY, int radius)
    {
        List<Cell> cellsWithinRadius = new List<Cell>();

        /#1#/ Convert the 1D center index to 2D coordinates
        int centerX = centerIndex / gridWidth;
        int centerY = centerIndex % gridWidth;#1#

        int x = centerX;
        int y = centerY;
        int dx = 0;
        int dy = -1;

        for (int i = 0; i < (2 * radius + 1) * (2 * radius + 1); i++)
        {
            if (Math.Abs(x - centerX) <= radius && Math.Abs(y - centerY) <= radius)
            {
                // Convert the 2D coordinates back to a 1D index
                int index = y * GridManager.Instance.m_gridWidth + x;

                // Check if the current cell is within the specified radius
                cellsWithinRadius.Add(GridManager.Instance.m_gridCells[index]);
            }

            if (x == y || (x < 0 && x == -y) || (x > 0 && x == 1 - y))
            {
                // Change direction in the spiral
                if (x == y || (x < 0 && x == -y) || (x > 0 && x == 1 - y))
                {
                    int temp = dx;
                    dx = -dy;
                    dy = temp;
                }
            }

            x += dx;
            y += dy;
        }

        return cellsWithinRadius;
    }*/

    public static List<Cell> GetCellsInRadius(Cell startCell, int maxDistance)
    {
        List<Cell> cellsWithinRadius = new List<Cell>();

        int gridWidth = GridManager.Instance.m_gridWidth;

        int startX = startCell.m_cellPos.x;
        int startY = startCell.m_cellPos.y;

        // List to keep track of visited nodes
        HashSet<Cell> visited = new HashSet<Cell>();

        Queue<(int, int, int)> queue = new Queue<(int, int, int)>();

        // Check if the starting point is within the grid and has the original value
        Debug.Log($"Starting Flood Fill");

        if (IsValidPoint(startX, startY))
        {
            // Enqueue the starting point with distance 0
            queue.Enqueue((startX, startY, 0));

            while (queue.Count > 0)
            {
                var (x, y, distance) = queue.Dequeue();
                Cell curCell = GridManager.Instance.m_gridCells[x * gridWidth + y];


                // Check if the distance exceeds the maximum distance
                if (distance > maxDistance)
                {
                    Debug.Log($"Stopping Flood Fill, max distance of {distance} reached.");
                    break; // Stop filling if the distance exceeds the limit
                }

                // Check if the current point is within the grid and has the original value
                if (IsValidPoint(x, y) && !visited.Contains(curCell))
                {
                    Debug.Log($"Added a cell to list.");
                    cellsWithinRadius.Add(curCell);

                    // Mark the current node as visited
                    if (!visited.Add(curCell))
                    {
                        continue;
                    }

                    // Enqueue neighboring points with increased distance
                    queue.Enqueue((x + 1, y, distance + 1));
                    queue.Enqueue((x - 1, y, distance + 1));
                    queue.Enqueue((x, y + 1, distance + 1));
                    queue.Enqueue((x, y - 1, distance + 1));
                }
            }

            Debug.Log($"Flood fill Queue complete.");
        }

        Debug.Log($"Returning flood fill list.");
        return cellsWithinRadius;
    }

    static bool IsValidPoint(int x, int y)
    {
        bool valid = x >= 0 && x < GridManager.Instance.m_gridWidth && y >= 0 && y < GridManager.Instance.m_gridHeight;
        //Debug.Log($"Cell valid: {valid}");
        return valid;
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

    /*public static Cell GetCellOfObj(GameObject obj)
    {
        int x = (int) obj.transform.position.x;
        int y = (int) obj.transform.position.y;

        if (x < 0 || x > gridManager.m_gridCells.GetLength(0) - 1)
        {
            return null;
        }

        if (y < 0 || y > gridManager.m_gridCells.GetLength(1) - 1)
        {
            return null;
        }

        Cell onCell = gridManager.m_gridCells[x, y];

        return onCell;
    }*/

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

    public static int GetCellIndex(Vector3 pos)
    {
        Vector2Int vector2pos = GetVector2IntFrom3DPos(pos);
        return vector2pos.y * GridManager.Instance.m_gridWidth + vector2pos.x;
    }

    public static int GetCellIndex(Vector2Int pos)
    {
        return pos.y * GridManager.Instance.m_gridWidth + pos.x;
    }

    public static Cell GetCellFromPos(Vector2Int pos)
    {
        int x = pos.x;
        int z = pos.y;

        //Check we're within the grid width
        if (x < 0 || x >= GridManager.Instance.m_gridWidth)
        {
            //Debug.Log("X not within grid bounds.");
            return null;
        }

        //Check we're within the grid height
        if (z < 0 || z >= GridManager.Instance.m_gridHeight)
        {
            //Debug.Log("Z not within grid bounds.");
            return null;
        }

        int index = z * GridManager.Instance.m_gridWidth + x;
        //Debug.Log("Request Cell at: " + x + "," + z + " Index of: " + index);

        return GridManager.Instance.m_gridCells[index];
    }

    public static Cell GetCellFrom3DPos(Vector3 pos)
    {
        //Debug.Log($"Getting cell from {pos}");
        int x = Mathf.FloorToInt(pos.x + 0.5f);
        int z = Mathf.FloorToInt(pos.z + 0.5f);

        //Check we're within the grid width
        if (x < 0 || x >= GridManager.Instance.m_gridWidth)
        {
            //Debug.Log("X not within grid bounds.");
            return null;
        }

        //Check we're within the grid height
        if (z < 0 || z >= GridManager.Instance.m_gridHeight)
        {
            //Debug.Log("Z not within grid bounds.");
            return null;
        }

        //Debug.Log($"Returning cell from {x},{z}");
        int index = z * GridManager.Instance.m_gridWidth + x;
        //Debug.Log("Request Cell at: " + x + "," + z + " Index of: " + index);

        return GridManager.Instance.m_gridCells[index];
    }

    public static (List<Cell>, List<Vector2Int>) GetNeighborHarvestPointCells(Vector2Int pos)
    {
        List<Vector2Int> neighborPos = new List<Vector2Int>();
        neighborPos.Add(new Vector2Int(pos.x, pos.y + 1)); //N
        neighborPos.Add(new Vector2Int(pos.x + 1, pos.y + 1)); //NE
        neighborPos.Add(new Vector2Int(pos.x + 1, pos.y)); //E
        neighborPos.Add(new Vector2Int(pos.x + 1, pos.y - 1)); //SE
        neighborPos.Add(new Vector2Int(pos.x, pos.y - 1)); //S
        neighborPos.Add(new Vector2Int(pos.x - 1, pos.y - 1)); //SW
        neighborPos.Add(new Vector2Int(pos.x - 1, pos.y)); //W
        neighborPos.Add(new Vector2Int(pos.x - 1, pos.y + 1)); //NW

        List<Cell> neighborCells = new List<Cell>();
        List<Vector2Int> harvestPos = new List<Vector2Int>();

        foreach (Vector2Int neighborCellPos in neighborPos)
        {
            if (IsValidPoint(neighborCellPos.x, neighborCellPos.y))
            {
                Cell cell = GetCellFromPos(neighborCellPos);
                if (cell != null)
                {
                    neighborCells.Add(cell);
                    harvestPos.Add(neighborCellPos);
                }
            }
        }

        return (neighborCells, harvestPos);
    }

    public static string FormatAsPercentageString(float i)
    {
        float percentageValue = i * 100;
        string formattedPercentage = percentageValue.ToString("0") + "%";
        return formattedPercentage;
    }

    public static Cell[] GetNeighborCells(Cell startCell, Cell[] gridCells)
    {
        // Get the neighboring cells (North, East, South, and West)
        Vector2Int[] neighbors = new Vector2Int[4];

        Cell curCell = GetCellFromPos(new Vector2Int(startCell.m_cellPos.x, startCell.m_cellPos.y));
        if (curCell.m_canPathNorth) neighbors[0] = new Vector2Int(startCell.m_cellPos.x, startCell.m_cellPos.y + 1);
        if (curCell.m_canPathEast) neighbors[1] = new Vector2Int(startCell.m_cellPos.x + 1, startCell.m_cellPos.y);
        if (curCell.m_canPathSouth) neighbors[2] = new Vector2Int(startCell.m_cellPos.x, startCell.m_cellPos.y - 1);
        if (curCell.m_canPathWest) neighbors[3] = new Vector2Int(startCell.m_cellPos.x - 1, startCell.m_cellPos.y);

        Cell[] neighborCells = new Cell[4];

        for (int i = 0; i < neighbors.Length; i++)
        {
            if (IsValidPoint(neighbors[i].x, neighbors[i].y))
            {
                neighborCells[i] = (gridCells[GetCellIndex(neighbors[i])]);
            }
        }

        return neighborCells;
    }

    public static List<Vector2Int> GetAdjacentEmptyCellPos(Vector2Int center)
    {
        List<Vector2Int> adjacentCells = new List<Vector2Int>();

        foreach (Vector2Int direction in m_directions)
        {
            Vector2Int adjacentPosition = center + direction;
            Cell adjacentCell = GetCellFromPos(adjacentPosition);
            if (adjacentCell == null) continue;
            if (!adjacentCell.m_isOccupied)
            {
                adjacentCells.Add(adjacentCell.m_cellPos);
            }
        }

        return adjacentCells;
    }

    public static List<Vector2Int> GetAdjacentCellPos(Vector2Int center)
    {
        List<Vector2Int> adjacentCells = new List<Vector2Int>();

        foreach (Vector2Int direction in m_directions)
        {
            Vector2Int adjacentPosition = center + direction;
            Cell adjacentCell = GetCellFromPos(adjacentPosition);

            if (adjacentCell == null) continue;

            adjacentCells.Add(adjacentCell.m_cellPos);
        }

        return adjacentCells;
    }

    public static Vector2Int GetVector2IntFrom3DPos(Vector3 pos)
    {
        int x = Mathf.FloorToInt(pos.x + 0.5f);
        int z = Mathf.FloorToInt(pos.z + 0.5f);
        return new Vector2Int(x, z);
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